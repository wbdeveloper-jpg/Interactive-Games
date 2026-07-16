using DG.Tweening;
using RewardSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FillImage : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Intensity")]
    [Range(0f, 1f)] public float currentIntensity = 0.2f;
    public Image img;
    [Min(0f)] public float speed = 5f;

    [Tooltip("If true, completing the 1.0 round starts the final reward/completion flow.")]
    public bool completeWhenReachingMax = true;

    public Dictionary<float, string> intensityDict;

    [Header("Tooltip")]
    public TextMeshProUGUI toolTipTxt;
    public RectTransform toolTip;
    public float tooltipYOffset = 20f;
    public float tooltipXOffset = 0f;

    [Header("Floating Text Defaults")]
    public RectTransform defaultParent;
    public Vector2 defaultStartAnchoredPos = Vector2.zero;
    public GameObject prefab;

    [Header("Max Intensity UI Elements")]
    public CanvasGroup panel;
    public Image successImage;
    public Image continueImage;
    public Button continueButton;
    public ParticleSystem particle1;
    public ParticleSystem particle2;

    [Header("Max Intensity Settings")]
    [Min(0f)] public float waitBeforePanel = 2.5f;
    [Min(0f)] public float panelFadeDuration = 0.8f;
    [Min(0f)] public float successPopDuration = 0.4f;
    [Min(0f)] public float continueFadeDuration = 0.6f;

    [Header("Evaluation")]
    public AnswerDrop answer;
    public LoadingPage loading;
    public float avgTime;

    [Header("Events")]
    public UnityEvent onMainMenuRequested;

    private const float IntensityStep = 0.2f;
    private Coroutine fillCoroutine;
    private Coroutine maxIntensityCoroutine;
    private Tween continueBreathingTween;
    private bool completionSequenceStarted;
    private Button activeContinueButton;
    private UnityAction activeContinueClickAction;
    private GameEvaluationData gameEvaluationData = new GameEvaluationData();
    private float timeTaken;

    private void Awake()
    {
        BuildIntensityDictionary();
    }

    private void OnEnable()
    {
        currentIntensity = SnapIntensity(currentIntensity);
        AnimateFill(currentIntensity);
        ManageToolTip();
    }

    private void BuildIntensityDictionary()
    {
        intensityDict = new Dictionary<float, string>
        {
            { 0.2f, "Not at all" },
            { 0.4f, "Just a little" },
            { 0.6f, "More or less" },
            { 0.8f, "Quite a bit" },
            { 1.0f, "A Lot" }
        };
    }

    /// <summary>
    /// Called after the player correctly completes the current round.
    /// 1.0 is a playable final round. The game ends only after the correct 1.0 answer is dropped.
    /// </summary>
    public void IncreaseIntensity()
    {
        currentIntensity = SnapIntensity(currentIntensity);

        if (Mathf.Approximately(currentIntensity, 1.0f))
        {
            if (completeWhenReachingMax)
                HandleMaxIntensityReached();

            return;
        }

        currentIntensity = SnapIntensity(currentIntensity + IntensityStep);
        AnimateFill(currentIntensity);
        ManageToolTip();
    }

    public void ManageToolTip()
    {
        currentIntensity = SnapIntensity(currentIntensity);

        if (toolTipTxt != null)
            toolTipTxt.text = GetIntensityText(currentIntensity);

        UpdateTooltipPosition(currentIntensity);
    }

    public string GetIntensityText(float intensity)
    {
        if (intensityDict == null || intensityDict.Count == 0)
            BuildIntensityDictionary();

        float normalized = SnapIntensity(intensity);
        foreach (KeyValuePair<float, string> pair in intensityDict)
        {
            if (Mathf.Approximately(SnapIntensity(pair.Key), normalized))
                return pair.Value;
        }

        return normalized.ToString("0.0");
    }

    public void AnimateFill(float target)
    {
        if (fillCoroutine != null)
            StopCoroutine(fillCoroutine);

        fillCoroutine = StartCoroutine(AnimateFillRoutine(target));
    }

    private IEnumerator AnimateFillRoutine(float target)
    {
        target = Mathf.Clamp01(target);

        if (img == null)
        {
            Debug.LogWarning("FillImage: img is not assigned.", this);
            fillCoroutine = null;
            yield break;
        }

        while (!Mathf.Approximately(img.fillAmount, target))
        {
            img.fillAmount = Mathf.MoveTowards(img.fillAmount, target, Time.deltaTime * speed);
            UpdateTooltipPosition(img.fillAmount);
            yield return null;
        }

        img.fillAmount = target;
        UpdateTooltipPosition(target);
        fillCoroutine = null;
    }

    private void UpdateTooltipPosition(float normalizedFill)
    {
        if (img == null || toolTip == null)
            return;

        RectTransform imageRect = img.rectTransform;
        Vector3[] corners = new Vector3[4];
        imageRect.GetWorldCorners(corners);

        Vector3 worldLeft = (corners[0] + corners[1]) * 0.5f;
        Vector3 worldRight = (corners[2] + corners[3]) * 0.5f;
        Vector3 worldPos = Vector3.Lerp(worldLeft, worldRight, Mathf.Clamp01(normalizedFill));

        RectTransform parent = toolTip.parent as RectTransform;
        if (parent == null)
        {
            toolTip.position = worldPos + new Vector3(tooltipXOffset, tooltipYOffset, 0f);
            return;
        }

        Camera cam = GetCanvasCamera(img.GetComponentInParent<Canvas>());
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out local);

        local.x += tooltipXOffset;
        local.y += tooltipYOffset;
        toolTip.anchoredPosition = local;
    }

    public void ChangeEmotion(bool isHappy, Image targetImage, Sprite happySprite, Sprite sadSprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = isHappy ? happySprite : sadSprite;
        targetImage.transform.DOKill(false);
        targetImage.transform.localScale = Vector3.one;
        targetImage.transform
            .DOScale(1.15f, 0.15f)
            .SetEase(Ease.OutBack)
            .SetLink(targetImage.gameObject)
            .OnComplete(() =>
            {
                if (targetImage != null)
                    targetImage.transform.DOScale(1f, 0.12f).SetLink(targetImage.gameObject);
            });
    }

    public void FadeAndDestroy(GameObject obj, float duration = 2f, Action onFinished = null)
    {
        if (obj == null)
        {
            onFinished?.Invoke();
            return;
        }

        CanvasGroup canvasGroup = EnsureCanvasGroup(obj);
        canvasGroup.alpha = 1f;
        canvasGroup.DOKill(false);

        canvasGroup
            .DOFade(0f, Mathf.Max(0f, duration))
            .SetEase(Ease.Linear)
            .SetLink(obj)
            .OnComplete(() =>
            {
                if (obj != null)
                    Destroy(obj);

                onFinished?.Invoke();
            });
    }

    public void SpawnFloatingText(string text, RectTransform startParent = null, Vector2? startAnchoredPos = null, float moveDistance = 40f, float duration = 1.2f)
    {
        if (prefab == null)
        {
            Debug.LogWarning("FillImage: floating text prefab is not assigned.", this);
            return;
        }

        RectTransform parent = startParent != null ? startParent : defaultParent;
        if (parent == null)
        {
            Debug.LogWarning("FillImage: floating text parent is not assigned.", this);
            return;
        }

        GameObject instance = Instantiate(prefab, parent);
        TextMeshProUGUI textComponent = instance.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
            textComponent.text = text;

        RectTransform rect = instance.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogWarning("FillImage: floating text prefab needs a RectTransform.", instance);
            Destroy(instance);
            return;
        }

        rect.anchoredPosition = startAnchoredPos ?? defaultStartAnchoredPos;
        CanvasGroup canvasGroup = EnsureCanvasGroup(instance);
        canvasGroup.alpha = 1f;

        StartCoroutine(FloatingRoutine(instance, canvasGroup, moveDistance, Mathf.Max(0.01f, duration)));
    }

    private IEnumerator FloatingRoutine(GameObject obj, CanvasGroup canvasGroup, float moveDistance, float duration)
    {
        if (obj == null || canvasGroup == null)
            yield break;

        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
        {
            Destroy(obj);
            yield break;
        }

        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, moveDistance);
        float elapsed = 0f;

        while (elapsed < duration && obj != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);

            rect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);
            yield return null;
        }

        if (obj != null)
            Destroy(obj);
    }

    public void HandleMaxIntensityReached()
    {
        if (completionSequenceStarted)
            return;

        completionSequenceStarted = true;
        maxIntensityCoroutine = StartCoroutine(MaxIntensitySequence());
    }

    private IEnumerator MaxIntensitySequence()
    {
        if (!ValidateCompletionReferences())
        {
            completionSequenceStarted = false;
            maxIntensityCoroutine = null;
            yield break;
        }

        BuildEvaluationData();

        yield return new WaitForSeconds(waitBeforePanel);

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopBGM();

        panel.gameObject.SetActive(true);
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;

        yield return panel
            .DOFade(1f, panelFadeDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(panel.gameObject)
            .WaitForCompletion();

        successImage.transform.DOKill(false);
        successImage.transform.localScale = Vector3.zero;
        successImage.gameObject.SetActive(true);

        yield return successImage.transform
            .DOScale(1f, successPopDuration)
            .SetEase(Ease.OutBack)
            .SetLink(successImage.gameObject)
            .WaitForCompletion();

        particle1?.Play();
        particle2?.Play();

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(0);

        yield return new WaitForSeconds(1.5f);

        if (RewardManager.Instance != null)
        {
            if (loading != null)
                RewardManager.Instance.ShowPostGame(loading._skills, gameEvaluationData);
            else
                Debug.LogWarning("FillImage: loading reference is missing, cannot pass skills to RewardManager.ShowPostGame.", this);
        }

        maxIntensityCoroutine = null;
    }

    private void BuildEvaluationData()
    {
        if (GameTimer.Instance != null)
            timeTaken = GameTimer.Instance.StopTimer();
        else
            timeTaken = 0f;

        if (answer == null)
            answer = FindObjectOfType<AnswerDrop>();

        int wrongDropCount = answer != null ? answer.WrongDropCount : 0;

        gameEvaluationData.timeTaken = timeTaken;
        gameEvaluationData.timeScore = GameTimer.CalculateTimeScore(timeTaken, avgTime);
        gameEvaluationData.mistakeCount = wrongDropCount;
        gameEvaluationData.accuracyScore = 5f / (5f + wrongDropCount);
    }

    private bool ValidateCompletionReferences()
    {
        bool valid = true;

        if (panel == null)
        {
            Debug.LogWarning("FillImage: panel is not assigned.", this);
            valid = false;
        }

        if (successImage == null)
        {
            Debug.LogWarning("FillImage: successImage is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    private Button ResolveContinueButton()
    {
        if (continueButton != null)
            return continueButton;

        if (continueImage != null && continueImage.TryGetComponent(out Button imageButton))
            return imageButton;

        return panel != null ? panel.GetComponent<Button>() : null;
    }

    public void OnMainButton()
    {
        Debug.Log("FillImage: Main Menu requested.", this);
        onMainMenuRequested?.Invoke();
    }

    private void RemoveContinueListener()
    {
        if (activeContinueButton != null && activeContinueClickAction != null)
            activeContinueButton.onClick.RemoveListener(activeContinueClickAction);

        activeContinueButton = null;
        activeContinueClickAction = null;
    }

    private static float SnapIntensity(float value)
    {
        float snapped = Mathf.Round(value / IntensityStep) * IntensityStep;
        return Mathf.Clamp(Mathf.Round(snapped * 10f) / 10f, IntensityStep, 1f);
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject obj)
    {
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : obj.AddComponent<CanvasGroup>();
    }

    private static Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void OnDisable()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }

        if (maxIntensityCoroutine != null)
        {
            StopCoroutine(maxIntensityCoroutine);
            maxIntensityCoroutine = null;
        }

        RemoveContinueListener();
        continueBreathingTween?.Kill(false);
        continueBreathingTween = null;
        completionSequenceStarted = false;
    }

    private void OnValidate()
    {
        currentIntensity = SnapIntensity(currentIntensity);
        speed = Mathf.Max(0f, speed);
        waitBeforePanel = Mathf.Max(0f, waitBeforePanel);
        panelFadeDuration = Mathf.Max(0f, panelFadeDuration);
        successPopDuration = Mathf.Max(0f, successPopDuration);
        continueFadeDuration = Mathf.Max(0f, continueFadeDuration);
    }

    public void OnPlayAgain()
    {
        PlayAgain();
    }

    public void OnHome()
    {
        MainMenu();
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        if (RewardManager.Instance != null)
            RewardManager.Instance.HideAll();

        if (UnityAndroidMediator.Instance != null)
            UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

        //if (GameLoader.Instance != null)
        //    GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");

        SceneManager.LoadScene("Loader Scene");


    }

    public void OnRewardScreenOpen()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopBGM();
    }
}
