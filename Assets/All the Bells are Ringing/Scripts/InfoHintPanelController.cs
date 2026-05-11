using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InfoHintPanelController : MonoBehaviour
{
    [Header("Gameplay Flow")]
    [Tooltip("Optional. Use this if your game switches panels through PanelSwitcher.")]
    [SerializeField] private PanelSwitcher panelSwitcher;

    [Tooltip("Main gameplay panel. This panel should contain SetQuestions.")]
    [SerializeField] private GameObject gameplayPanel;

    [Tooltip("Used only if PanelSwitcher is not assigned.")]
    [SerializeField] private bool enableGameplayPanelDirectly = true;

    [Header("First-Time Info")]
    [SerializeField] private bool showFirstTimeInfo = true;
    [SerializeField] private bool rememberFirstTimeWithPlayerPrefs = true;
    [SerializeField] private string firstTimePrefsKey = "EmotionGame_InfoPanel_Seen";

    [Tooltip("How long the first-time info panel stays open.")]
    [Min(0.1f)]
    [SerializeField] private float firstTimeInfoDuration = 20f;

    [Header("Hint")]
    [SerializeField] private Button hintButton;

    [Tooltip("How long hint panel stays visible when opened using hint button.")]
    [Min(0.1f)]
    [SerializeField] private float hintPanelDuration = 5f;

    [Tooltip("After user watches hint, hint becomes available again after this many seconds.")]
    [Min(0f)]
    [SerializeField] private float hintCooldownDuration = 20f;

    [Tooltip("Keep hint button clickable during cooldown so user gets feedback.")]
    [SerializeField] private bool keepHintButtonClickableDuringCooldown = true;

    [Tooltip("Floating message when hint is clicked during cooldown. {0} = seconds remaining.")]
    [SerializeField] private string hintCooldownMessageFormat = "Hint available in {0}s";

    [Tooltip("Floating message when hint becomes available again.")]
    [SerializeField] private string hintAvailableMessage = "Hint available!";

    [Tooltip("Optional TMP text on hint button to show cooldown timer.")]
    [SerializeField] private TextMeshProUGUI hintButtonTimerText;

    [Tooltip("Used for SpawnFloatingText messages.")]
    [SerializeField] private FillImage fillImage;

    [Header("Info / Hint Panel UI")]
    [Tooltip("Full-screen background panel. This object should have Image + CanvasGroup + Button.")]
    [SerializeField] private GameObject infoPanel;

    [SerializeField] private CanvasGroup infoPanelCanvasGroup;

    [Tooltip("Child info card inside infoPanel.")]
    [SerializeField] private RectTransform infoCard;

    [SerializeField] private CanvasGroup infoCardCanvasGroup;

    [Tooltip("Text like: Closes in 20 seconds")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Tooltip("Button on background panel, not on the card.")]
    [SerializeField] private Button infoBackgroundCloseButton;

    [Header("Animation")]
    [SerializeField] private bool useUnscaledTime = true;

    [Min(0f)]
    [SerializeField] private float fadeInDuration = 0.28f;

    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 0.18f;

    [SerializeField] private float cardStartScale = 0.94f;

    [SerializeField] private Ease fadeInEase = Ease.OutCubic;
    [SerializeField] private Ease fadeOutEase = Ease.InCubic;
    [SerializeField] private Ease cardPopEase = Ease.OutBack;

    [Header("Events")]
    public UnityEvent onGameplayStarted;
    public UnityEvent onInfoPanelOpened;
    public UnityEvent onInfoPanelClosed;
    public UnityEvent onHintOpened;
    public UnityEvent onHintCooldownStarted;
    public UnityEvent onHintAvailable;

    private Coroutine countdownRoutine;
    private Coroutine hintCooldownRoutine;
    private Sequence activePanelSequence;

    private bool gameplayStarted;
    private bool startGameplayAfterPanelCloses;
    private bool isPanelOpen;
    private bool isClosingPanel;
    private bool currentPanelOpenedFromHint;

    private bool hintAvailable = true;
    private float hintCooldownRemaining;

    private void Awake()
    {
        PrepareReferences();
        RegisterButtonEvents();

        SetInfoPanelImmediate(false);

        hintAvailable = true;
        hintCooldownRemaining = 0f;

        SetHintButtonInteractable(false);
        UpdateHintButtonTimerText();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
        StopCountdown();
        StopHintCooldown();
        KillPanelSequence();
    }

    /// <summary>
    /// Connect LoadingPage.onComplete to this method.
    /// </summary>
    public void OnLoadingComplete()
    {
        if (ShouldShowFirstTimeInfo())
        {
            MarkFirstTimeInfoSeen();
            ShowPanel(
                duration: firstTimeInfoDuration,
                shouldStartGameplayAfterClose: true,
                openedFromHint: false
            );
        }
        else
        {
            StartGameplay();
        }
    }

    /// <summary>
    /// Connect hint button OnClick to this method,
    /// or assign hintButton in the inspector.
    /// </summary>
    public void OnHintButtonClicked()
    {
        TryOpenHint();
    }

    public void TryOpenHint()
    {
        if (!gameplayStarted)
            return;

        if (isPanelOpen)
            return;

        if (!hintAvailable)
        {
            ShowHintCooldownMessage();
            return;
        }

        hintAvailable = false;

        if (!keepHintButtonClickableDuringCooldown)
            SetHintButtonInteractable(false);

        UpdateHintButtonTimerText();

        ShowPanel(
            duration: hintPanelDuration,
            shouldStartGameplayAfterClose: false,
            openedFromHint: true
        );

        onHintOpened?.Invoke();
    }

    public void ClosePanel()
    {
        if (!isPanelOpen || isClosingPanel)
            return;

        isClosingPanel = true;

        StopCountdown();
        KillPanelSequence();

        activePanelSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        if (infoPanelCanvasGroup != null)
        {
            infoPanelCanvasGroup.interactable = false;
            infoPanelCanvasGroup.blocksRaycasts = false;

            activePanelSequence.Join(
                infoPanelCanvasGroup
                    .DOFade(0f, fadeOutDuration)
                    .SetEase(fadeOutEase)
                    .SetUpdate(useUnscaledTime)
            );
        }

        if (infoCard != null)
        {
            activePanelSequence.Join(
                infoCard
                    .DOScale(cardStartScale, fadeOutDuration)
                    .SetEase(fadeOutEase)
                    .SetUpdate(useUnscaledTime)
            );
        }

        activePanelSequence.OnComplete(() =>
        {
            bool wasHintPanel = currentPanelOpenedFromHint;

            SetInfoPanelImmediate(false);

            isPanelOpen = false;
            isClosingPanel = false;
            currentPanelOpenedFromHint = false;

            onInfoPanelClosed?.Invoke();

            if (startGameplayAfterPanelCloses)
            {
                startGameplayAfterPanelCloses = false;
                StartGameplay();
                return;
            }

            if (wasHintPanel)
                StartHintCooldown();
        });
    }

    public void ResetFirstTimeInfoSave()
    {
        PlayerPrefs.DeleteKey(firstTimePrefsKey);
        PlayerPrefs.Save();
    }

    private void ShowPanel(float duration, bool shouldStartGameplayAfterClose, bool openedFromHint)
    {
        if (infoPanel == null)
        {
            Debug.LogWarning("InfoHintPanelController: infoPanel is not assigned.", this);

            if (shouldStartGameplayAfterClose)
                StartGameplay();

            return;
        }

        StopCountdown();
        KillPanelSequence();

        startGameplayAfterPanelCloses = shouldStartGameplayAfterClose;
        currentPanelOpenedFromHint = openedFromHint;

        isPanelOpen = true;
        isClosingPanel = false;

        infoPanel.SetActive(true);

        if (infoPanelCanvasGroup != null)
        {
            infoPanelCanvasGroup.alpha = 0f;
            infoPanelCanvasGroup.interactable = false;
            infoPanelCanvasGroup.blocksRaycasts = true;
        }

        if (infoCardCanvasGroup != null)
            infoCardCanvasGroup.alpha = 0f;

        if (infoCard != null)
            infoCard.localScale = Vector3.one * cardStartScale;

        UpdateCountdownText(duration);

        activePanelSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        if (infoPanelCanvasGroup != null)
        {
            activePanelSequence.Join(
                infoPanelCanvasGroup
                    .DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase)
                    .SetUpdate(useUnscaledTime)
            );
        }

        if (infoCardCanvasGroup != null)
        {
            activePanelSequence.Join(
                infoCardCanvasGroup
                    .DOFade(1f, fadeInDuration)
                    .SetEase(fadeInEase)
                    .SetUpdate(useUnscaledTime)
            );
        }

        if (infoCard != null)
        {
            activePanelSequence.Join(
                infoCard
                    .DOScale(1f, fadeInDuration)
                    .SetEase(cardPopEase)
                    .SetUpdate(useUnscaledTime)
            );
        }

        activePanelSequence.OnComplete(() =>
        {
            if (infoPanelCanvasGroup != null)
            {
                infoPanelCanvasGroup.interactable = true;
                infoPanelCanvasGroup.blocksRaycasts = true;
            }

            countdownRoutine = StartCoroutine(CountdownRoutine(duration));
        });

        onInfoPanelOpened?.Invoke();
    }

    private IEnumerator CountdownRoutine(float duration)
    {
        float remaining = Mathf.Max(0.1f, duration);

        while (remaining > 0f && isPanelOpen)
        {
            UpdateCountdownText(remaining);

            remaining -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        countdownRoutine = null;

        if (isPanelOpen)
            ClosePanel();
    }

    private void StartHintCooldown()
    {
        StopHintCooldown();

        hintCooldownRemaining = hintCooldownDuration;

        onHintCooldownStarted?.Invoke();

        if (hintCooldownDuration <= 0f)
        {
            MakeHintAvailable();
            return;
        }

        if (!keepHintButtonClickableDuringCooldown)
            SetHintButtonInteractable(false);
        else
            SetHintButtonInteractable(gameplayStarted);

        UpdateHintButtonTimerText();

        hintCooldownRoutine = StartCoroutine(HintCooldownRoutine());
    }

    private IEnumerator HintCooldownRoutine()
    {
        while (hintCooldownRemaining > 0f)
        {
            UpdateHintButtonTimerText();

            hintCooldownRemaining -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        hintCooldownRemaining = 0f;
        hintCooldownRoutine = null;

        MakeHintAvailable();
    }

    private void MakeHintAvailable()
    {
        hintAvailable = true;
        hintCooldownRemaining = 0f;

        SetHintButtonInteractable(gameplayStarted);
        UpdateHintButtonTimerText();

        if (fillImage != null && !string.IsNullOrWhiteSpace(hintAvailableMessage))
            fillImage.SpawnFloatingText(hintAvailableMessage);

        onHintAvailable?.Invoke();
    }

    private void ShowHintCooldownMessage()
    {
        int seconds = Mathf.Max(1, Mathf.CeilToInt(hintCooldownRemaining));
        string message = string.Format(hintCooldownMessageFormat, seconds);

        if (fillImage != null)
            fillImage.SpawnFloatingText(message);
        else
            Debug.Log(message);
    }

    private void StartGameplay()
    {
        if (gameplayStarted)
            return;

        gameplayStarted = true;

        if (panelSwitcher != null && gameplayPanel != null)
        {
            panelSwitcher.Switch(gameplayPanel);
        }
        else if (enableGameplayPanelDirectly && gameplayPanel != null)
        {
            gameplayPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("InfoHintPanelController: gameplayPanel is not assigned.", this);
        }

        hintAvailable = true;
        hintCooldownRemaining = 0f;

        SetHintButtonInteractable(true);
        UpdateHintButtonTimerText();

        onGameplayStarted?.Invoke();
    }

    private bool ShouldShowFirstTimeInfo()
    {
        if (!showFirstTimeInfo)
            return false;

        if (!rememberFirstTimeWithPlayerPrefs)
            return true;

        return PlayerPrefs.GetInt(firstTimePrefsKey, 0) == 0;
    }

    private void MarkFirstTimeInfoSeen()
    {
        if (!rememberFirstTimeWithPlayerPrefs)
            return;

        PlayerPrefs.SetInt(firstTimePrefsKey, 1);
        PlayerPrefs.Save();
    }

    private void UpdateCountdownText(float remaining)
    {
        if (countdownText == null)
            return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
        countdownText.text = $"Closes in {seconds} second{(seconds == 1 ? "" : "s")}";
    }

    private void UpdateHintButtonTimerText()
    {
        if (hintButtonTimerText == null)
            return;

        if (hintAvailable || hintCooldownRemaining <= 0f)
        {
            hintButtonTimerText.text = string.Empty;
            return;
        }

        int seconds = Mathf.Max(1, Mathf.CeilToInt(hintCooldownRemaining));
        hintButtonTimerText.text = seconds + "s";
    }

    private void SetHintButtonInteractable(bool interactable)
    {
        if (hintButton != null)
            hintButton.interactable = interactable;
    }

    private void PrepareReferences()
    {
        if (infoPanel != null && infoPanelCanvasGroup == null)
            infoPanelCanvasGroup = EnsureCanvasGroup(infoPanel);

        if (infoCard != null && infoCardCanvasGroup == null)
            infoCardCanvasGroup = EnsureCanvasGroup(infoCard.gameObject);
    }

    private void RegisterButtonEvents()
    {
        if (infoBackgroundCloseButton != null)
            infoBackgroundCloseButton.onClick.AddListener(ClosePanel);

        if (hintButton != null)
            hintButton.onClick.AddListener(TryOpenHint);
    }

    private void UnregisterButtonEvents()
    {
        if (infoBackgroundCloseButton != null)
            infoBackgroundCloseButton.onClick.RemoveListener(ClosePanel);

        if (hintButton != null)
            hintButton.onClick.RemoveListener(TryOpenHint);
    }

    private void StopCountdown()
    {
        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }
    }

    private void StopHintCooldown()
    {
        if (hintCooldownRoutine != null)
        {
            StopCoroutine(hintCooldownRoutine);
            hintCooldownRoutine = null;
        }
    }

    private void KillPanelSequence()
    {
        if (activePanelSequence != null && activePanelSequence.IsActive())
            activePanelSequence.Kill(false);

        activePanelSequence = null;
    }

    private void SetInfoPanelImmediate(bool visible)
    {
        if (infoPanel != null)
            infoPanel.SetActive(visible);

        if (infoPanelCanvasGroup != null)
        {
            infoPanelCanvasGroup.alpha = visible ? 1f : 0f;
            infoPanelCanvasGroup.interactable = visible;
            infoPanelCanvasGroup.blocksRaycasts = visible;
        }

        if (infoCardCanvasGroup != null)
            infoCardCanvasGroup.alpha = visible ? 1f : 0f;

        if (infoCard != null)
            infoCard.localScale = Vector3.one;
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : target.AddComponent<CanvasGroup>();
    }

    private void OnValidate()
    {
        firstTimeInfoDuration = Mathf.Max(0.1f, firstTimeInfoDuration);
        hintPanelDuration = Mathf.Max(0.1f, hintPanelDuration);
        hintCooldownDuration = Mathf.Max(0f, hintCooldownDuration);

        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        cardStartScale = Mathf.Max(0.01f, cardStartScale);
    }
}