using DG.Tweening;
using RewardSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleLoader : MonoBehaviour
{
    public Image fillImage;
    public RectTransform circle;
    public CanvasGroup loaderCanvas;

    public float loadDuration = 2f;

    private float width;

    [Header("Gameplay")]
    public GameObject player;
    public GameObject[] gameObjects;

    [Header("Device UI")]
    public GameObject mobileUIController;
    public GameObject tabletUIController;

    [Header("Intro Panel")]
    public IntroPanelController introPanel;

    [Header("Tap To Start Gate")]
    [Tooltip("Assign your Tap To Start panel CanvasGroup here. It will appear after intro, objects, and controls are ready.")]
    public CanvasGroup tapToStartPanel;

    [Tooltip("Optional. If empty, SimpleLoader will try to find a Button on/inside tapToStartPanel.")]
    public Button tapToStartButton;

    [SerializeField] private bool waitForTapToStart = true;
    [SerializeField] private float tapToStartFadeDuration = 0.35f;
    [SerializeField] private bool blockPlayerMovementUntilTap = true;

    [Header("Loader Motion")]
    [SerializeField] private float circleEndOffsetFromRight = 450f;

    public List<SkillEntry> _skills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: 0.2f, accuracyWeight: 0.8f),
        new SkillEntry(BloomSkillType.Analyze, 50f, timeWeight: 0.4f, accuracyWeight: 0.6f),
    };

    private GameManager gameManager;
    private PlayerMovement cachedPlayerMovement;
    private bool playerMovementWasEnabledBeforeGate;
    private bool tapToStartClicked;
    private Button resolvedTapToStartButton;

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        CacheLoaderWidth();
        ResolveTapToStartButton();
        SetTapToStartPanelVisible(false, true);
        SetGameplayVisible(false);

        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPreGame(_skills);
        }

        StartCoroutine(StartLoading());
    }

    private void CacheLoaderWidth()
    {
        if (fillImage != null && fillImage.transform.parent is RectTransform parentRect)
        {
            width = parentRect.rect.width;
        }
    }

    private bool IsTablet()
    {
        float dpi = Screen.dpi;

        if (dpi <= 0f)
        {
            dpi = 160f;
        }

        float widthInches = Screen.width / dpi;
        float heightInches = Screen.height / dpi;
        float diagonalInches = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);

        return diagonalInches >= 7f;
    }

    private void SetDeviceUIVisible(bool visible)
    {
        bool tablet = IsTablet();

        if (mobileUIController != null)
        {
            mobileUIController.SetActive(visible && !tablet);
        }

        if (tabletUIController != null)
        {
            tabletUIController.SetActive(visible && tablet);
        }
    }

    private GameObject GetActiveDeviceUI()
    {
        return IsTablet() ? tabletUIController : mobileUIController;
    }

    private GameObject GetInactiveDeviceUI()
    {
        return IsTablet() ? mobileUIController : tabletUIController;
    }

    private void SetGameplayVisible(bool visible)
    {
        if (player != null)
        {
            player.SetActive(visible);
        }

        SetDeviceUIVisible(visible);

        if (gameObjects == null)
        {
            return;
        }

        foreach (GameObject obj in gameObjects)
        {
            if (obj != null)
            {
                obj.SetActive(visible);
            }
        }
    }

    private IEnumerator StartLoading()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }

        if (RewardManager.Instance != null)
        {
            yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(1, false);
        }

        PlayLoaderAnimation();
        yield return new WaitForSeconds(Mathf.Max(0f, loadDuration));

        yield return IntroSequence();
    }

    private void PlayLoaderAnimation()
    {
        float safeDuration = Mathf.Max(0f, loadDuration);

        if (fillImage != null)
        {
            DOTween.To(() => fillImage.fillAmount, x => fillImage.fillAmount = x, 1f, safeDuration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }

        if (circle != null)
        {
            circle.DOAnchorPosX(width - circleEndOffsetFromRight, safeDuration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
        }
    }

    private IEnumerator IntroSequence()
    {
        if (circle != null)
        {
            DOTween.Kill(circle);
        }

        DOTween.Kill(gameObject);

        if (loaderCanvas != null)
        {
            yield return loaderCanvas.DOFade(0f, 0.5f)
                .SetEase(Ease.InOutCubic)
                .SetLink(loaderCanvas.gameObject)
                .WaitForCompletion();

            loaderCanvas.gameObject.SetActive(false);
        }

        if (introPanel != null)
        {
            yield return introPanel.PlayIntro();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(0, true);
        }

        if (gameManager != null)
        {
            gameManager.ResetGameState(false);
        }

        if (player != null)
        {
            player.SetActive(true);
            CachePlayerMovement();
            PreparePlayerForStartGate();
        }

        yield return new WaitForSeconds(0.2f);

        yield return RevealGameplayObjects();
        yield return RevealControls();

        yield return WaitForTapToStartGate();
        BeginGameplayTimerAndInput();
    }

    private void CachePlayerMovement()
    {
        cachedPlayerMovement = player != null ? player.GetComponent<PlayerMovement>() : null;
    }

    private void PreparePlayerForStartGate()
    {
        if (cachedPlayerMovement == null)
        {
            return;
        }

        playerMovementWasEnabledBeforeGate = cachedPlayerMovement.enabled;
        cachedPlayerMovement.StopMovement();

        if (waitForTapToStart && blockPlayerMovementUntilTap)
        {
            cachedPlayerMovement.enabled = false;
        }
    }

    private IEnumerator RevealGameplayObjects()
    {
        if (gameObjects == null)
        {
            yield break;
        }

        foreach (GameObject obj in gameObjects)
        {
            if (obj == null)
            {
                continue;
            }

            obj.SetActive(true);
            obj.transform.DOKill(false);
            obj.transform.localScale = Vector3.zero;

            obj.transform.DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack)
                .SetLink(obj);

            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator RevealControls()
    {
        GameObject activeControlsUI = GetActiveDeviceUI();
        GameObject inactiveControlsUI = GetInactiveDeviceUI();

        if (inactiveControlsUI != null)
        {
            inactiveControlsUI.SetActive(false);
        }

        if (activeControlsUI == null)
        {
            yield break;
        }

        activeControlsUI.SetActive(true);

        CanvasGroup cg = activeControlsUI.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = activeControlsUI.AddComponent<CanvasGroup>();
        }

        cg.alpha = 0f;

        yield return cg.DOFade(1f, 0.4f)
            .SetEase(Ease.OutCubic)
            .SetLink(activeControlsUI)
            .WaitForCompletion();
    }

    private IEnumerator WaitForTapToStartGate()
    {
        if (!waitForTapToStart)
        {
            yield break;
        }

        ResolveTapToStartButton();

        if (tapToStartPanel == null && resolvedTapToStartButton == null)
        {
            Debug.LogWarning("Tap To Start is enabled, but no tapToStartPanel/tapToStartButton is assigned. Gameplay will start immediately.", this);
            yield break;
        }

        if (resolvedTapToStartButton == null)
        {
            Debug.LogWarning("Tap To Start panel is assigned, but no Button was found. Gameplay will start immediately to avoid a soft-lock.", this);
            yield break;
        }

        tapToStartClicked = false;

        resolvedTapToStartButton.onClick.RemoveListener(OnTapToStartClicked);
        resolvedTapToStartButton.onClick.AddListener(OnTapToStartClicked);

        yield return FadeTapToStartPanel(true);
        yield return new WaitUntil(() => tapToStartClicked);
        yield return FadeTapToStartPanel(false);

        resolvedTapToStartButton.onClick.RemoveListener(OnTapToStartClicked);
    }

    private void OnTapToStartClicked()
    {
        tapToStartClicked = true;

        if (tapToStartPanel != null)
        {
            tapToStartPanel.interactable = false;
            tapToStartPanel.blocksRaycasts = false;
        }

        if (resolvedTapToStartButton != null)
        {
            resolvedTapToStartButton.interactable = false;
        }
    }

    private IEnumerator FadeTapToStartPanel(bool show)
    {
        if (tapToStartPanel == null)
        {
            if (resolvedTapToStartButton != null)
            {
                resolvedTapToStartButton.gameObject.SetActive(show);
                resolvedTapToStartButton.interactable = show;
            }

            yield break;
        }

        tapToStartPanel.gameObject.SetActive(true);
        tapToStartPanel.DOKill(false);
        tapToStartPanel.interactable = show;
        tapToStartPanel.blocksRaycasts = show;

        if (resolvedTapToStartButton != null)
        {
            resolvedTapToStartButton.interactable = show;
        }

        float targetAlpha = show ? 1f : 0f;
        float duration = Mathf.Max(0f, tapToStartFadeDuration);

        if (duration <= 0f)
        {
            tapToStartPanel.alpha = targetAlpha;
        }
        else
        {
            yield return tapToStartPanel.DOFade(targetAlpha, duration)
                .SetEase(Ease.OutCubic)
                .SetLink(tapToStartPanel.gameObject)
                .WaitForCompletion();
        }

        if (!show)
        {
            tapToStartPanel.gameObject.SetActive(false);
        }
    }

    private void SetTapToStartPanelVisible(bool visible, bool immediate)
    {
        ResolveTapToStartButton();

        if (tapToStartPanel != null)
        {
            tapToStartPanel.DOKill(false);
            tapToStartPanel.alpha = visible ? 1f : 0f;
            tapToStartPanel.interactable = visible;
            tapToStartPanel.blocksRaycasts = visible;
            tapToStartPanel.gameObject.SetActive(visible);
        }

        if (resolvedTapToStartButton != null)
        {
            resolvedTapToStartButton.interactable = visible;

            if (tapToStartPanel == null)
            {
                resolvedTapToStartButton.gameObject.SetActive(visible);
            }
        }
    }

    private void ResolveTapToStartButton()
    {
        if (tapToStartButton != null)
        {
            resolvedTapToStartButton = tapToStartButton;
            return;
        }

        if (tapToStartPanel != null)
        {
            resolvedTapToStartButton = tapToStartPanel.GetComponentInChildren<Button>(true);
            return;
        }

        resolvedTapToStartButton = null;
    }

    private void BeginGameplayTimerAndInput()
    {
        if (cachedPlayerMovement != null && blockPlayerMovementUntilTap)
        {
            cachedPlayerMovement.enabled = playerMovementWasEnabledBeforeGate;
            cachedPlayerMovement.StopMovement();
        }

        if (gameManager != null)
        {
            gameManager.StartTimer();
        }

        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StartTimer();
        }
    }

    private void OnDestroy()
    {
        if (resolvedTapToStartButton != null)
        {
            resolvedTapToStartButton.onClick.RemoveListener(OnTapToStartClicked);
        }
    }

    private void OnValidate()
    {
        loadDuration = Mathf.Max(0f, loadDuration);
        circleEndOffsetFromRight = Mathf.Max(0f, circleEndOffsetFromRight);
        tapToStartFadeDuration = Mathf.Max(0f, tapToStartFadeDuration);
    }
}