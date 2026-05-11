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

    [Header("Events")]
    public UnityEvent onMainMenuRequested;

    private const float IntensityStep = 0.2f;
    private Coroutine fillCoroutine;
    private Coroutine maxIntensityCoroutine;
    private Tween continueBreathingTween;
    private bool completionSequenceStarted;
    private Button activeContinueButton;
    private UnityAction activeContinueClickAction;

    GameEvaluationData gameEvaluationData = new GameEvaluationData();
    public AnswerDrop answer;
    public LoadingPage loading;
    float timetaken;
    public float avgTime;
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

    public void IncreaseIntensity()
    {
        currentIntensity = NormalizeIntensity(currentIntensity);

        // If player has just correctly completed the 1.0 round,
        // now the game should end.
        if (Mathf.Approximately(currentIntensity, 1.0f))
        {
            HandleMaxIntensityReached();
            return;
        }

        // Otherwise move to the next round.
        currentIntensity = NormalizeIntensity(currentIntensity + 0.2f);

        AnimateFill(currentIntensity);
        ManageToolTip();
    }

    private float NormalizeIntensity(float value)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp01(value) / 0.2f);
        step = Mathf.Clamp(step, 1, 5);
        return step * 0.2f;
    }

    public void ManageToolTip()
    {
        currentIntensity = NormalizeIntensity(currentIntensity);

        if (toolTipTxt != null)
            toolTipTxt.text = GetIntensityText(currentIntensity);

        UpdateTooltipPosition(currentIntensity);
    }

    public string GetIntensityText(float intensity)
    {
        intensity = NormalizeIntensity(intensity);

        foreach (var pair in intensityDict)
        {
            if (Mathf.Approximately(NormalizeIntensity(pair.Key), intensity))
                return pair.Value;
        }

        return intensity.ToString("0.0");
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out Vector2 local);

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

    public void SpawnFloatingText(
        string text,
        RectTransform startParent = null,
        Vector2? startAnchoredPos = null,
        float moveDistance = 40f,
        float duration = 1.2f)
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
        RectTransform rect = obj.GetComponent<RectTransform>();
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

        timetaken = GameTimer.Instance.StopTimer();

        gameEvaluationData.timeTaken = timetaken;
        gameEvaluationData.timeScore = GameTimer.CalculateTimeScore(timetaken, avgTime);
        answer = FindObjectOfType<AnswerDrop>();
        gameEvaluationData.mistakeCount = answer.WrongDropCount;
        gameEvaluationData.accuracyScore = 5f / (5f + answer.WrongDropCount);

        yield return new WaitForSeconds(waitBeforePanel);
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
        RewardManager.Instance.ShowPostGame(loading._skills, gameEvaluationData);

        // no need of this part
        /**
        continueImage.gameObject.SetActive(true);
        CanvasGroup continueCanvasGroup = EnsureCanvasGroup(continueImage.gameObject);
        continueCanvasGroup.alpha = 0f;

        yield return continueCanvasGroup
            .DOFade(1f, continueFadeDuration)
            .SetEase(Ease.OutCubic)
            .SetLink(continueImage.gameObject)
            .WaitForCompletion();

        continueBreathingTween = continueImage.transform
            .DOScale(1.05f, 1.2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(continueImage.gameObject);

        Button button = ResolveContinueButton();
        bool clicked = false;
        if (button != null)
        {
            RemoveContinueListener();
            activeContinueButton = button;
            activeContinueClickAction = () => clicked = true;
            activeContinueButton.onClick.AddListener(activeContinueClickAction);
            activeContinueButton.interactable = true;
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }
        else
        {
            Debug.LogWarning("FillImage: no continue Button assigned/found. Calling OnMainButton automatically.", this);
            clicked = true;
        }

        yield return new WaitUntil(() => clicked);
        RemoveContinueListener();
        OnMainButton();
        **/
        // no need of this part
        maxIntensityCoroutine = null;
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

        if (continueImage == null)
        {
            Debug.LogWarning("FillImage: continueImage is not assigned.", this);
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
        Debug.Log("FillImage: Main Menu requested.");
        onMainMenuRequested?.Invoke();
    }

    private void RemoveContinueListener()
    {
        if (activeContinueButton != null && activeContinueClickAction != null)
            activeContinueButton.onClick.RemoveListener(activeContinueClickAction);

        activeContinueButton = null;
        activeContinueClickAction = null;
    }

    private string GetIntensityLabel(float intensity)
    {
        float snapped = SnapIntensity(intensity);
        if (intensityDict == null || intensityDict.Count == 0)
            BuildIntensityDictionary();

        if (intensityDict.TryGetValue(snapped, out string labelText))
            return labelText;

        return string.Empty;
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
        RewardManager.Instance.HideAll();
        SceneManager.LoadScene("Loader Scene");
        UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");
        GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");
    }

    public void OnRewardScreenOpen()
    {
        AudioManager.Instance.StopBGM();
    }
}
