using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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

    [Header("Start Info Panel")]
    [FormerlySerializedAs("showFirstTimeInfo")]
    [SerializeField] private bool showStartInfo = true;
    [FormerlySerializedAs("rememberFirstTimeWithPlayerPrefs")]
    [SerializeField] private bool rememberStartInfoWithPlayerPrefs = false;
    [FormerlySerializedAs("firstTimePrefsKey")]
    [SerializeField] private string startInfoPrefsKey = "EmotionGame_InfoPanel_Seen";

    [Tooltip("How long the start info panel stays open.")]
    [FormerlySerializedAs("firstTimeInfoDuration")]
    [Min(0.1f)] [SerializeField] private float startInfoDuration = 20f;

    [Header("Start Info Narration")]
    [SerializeField] private bool playStartInfoNarration = true;
    [SerializeField] private bool stopStartInfoNarrationWhenPanelCloses = true;
    [SerializeField] private AudioClip startInfoNarrationClip;
    [SerializeField] private AudioSource startInfoNarrationSource;

    [Header("Hint")]
    [SerializeField] private Button hintButton;

    [Tooltip("How long hint panel stays visible when opened using hint button.")]
    [Min(0.1f)] [SerializeField] private float hintPanelDuration = 5f;

    [Tooltip("After user watches hint, hint becomes available again after this many seconds.")]
    [Min(0f)] [SerializeField] private float hintCooldownDuration = 20f;

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

    [Header("Hint Attention On Wrong Drop")]
    [SerializeField] private bool animateHintButtonOnWrongDrop = true;
    [Min(1)] [SerializeField] private int hintAttentionPulseCount = 2;
    [Min(1f)] [SerializeField] private float hintAttentionScale = 1.16f;
    [Min(0.05f)] [SerializeField] private float hintAttentionDuration = 0.42f;
    [SerializeField] private bool showHintAvailableTextOnWrongDrop = false;
    [SerializeField] private string hintAvailableWrongDropMessage = "Hint is available!";

    [Header("Info / Hint Panel UI")]
    [Tooltip("Full-screen background panel. This object should have Image + CanvasGroup + Button.")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private CanvasGroup infoPanelCanvasGroup;

    [Tooltip("Child info card inside infoPanel.")]
    [SerializeField] private RectTransform infoCard;
    [SerializeField] private CanvasGroup infoCardCanvasGroup;

    [Tooltip("Text like: Closes in 20 seconds")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Tooltip("Button on the background panel, not on the card.")]
    [SerializeField] private Button infoBackgroundCloseButton;

    [Header("Animation")]
    [SerializeField] private bool useUnscaledTime = true;
    [Min(0f)] [SerializeField] private float fadeInDuration = 0.28f;
    [Min(0f)] [SerializeField] private float fadeOutDuration = 0.18f;
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
    public UnityEvent onHintAttentionPlayed;

    private Coroutine countdownRoutine;
    private Coroutine hintCooldownRoutine;
    private Sequence activePanelSequence;
    private Tween hintButtonAttentionTween;

    private bool gameplayStarted;
    private bool startGameplayAfterPanelCloses;
    private bool isPanelOpen;
    private bool isClosingPanel;
    private bool currentPanelOpenedFromHint;
    private bool currentPanelIsStartInfo;
    private bool hintAvailable = true;
    private float hintCooldownRemaining;
    private Vector3 hintButtonOriginalScale = Vector3.one;

    private void Awake()
    {
        PrepareReferences();
        RegisterButtonEvents();

        if (hintButton != null)
            hintButtonOriginalScale = hintButton.transform.localScale;

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
        StopHintButtonAttention();
        StopStartInfoNarration();
    }

    /// <summary>
    /// Connect LoadingPage.onComplete to this method.
    /// </summary>
    public void OnLoadingComplete()
    {
        if (ShouldShowStartInfo())
        {
            MarkStartInfoSeen();
            ShowPanel(startInfoDuration, shouldStartGameplayAfterClose: true, openedFromHint: false);
        }
        else
        {
            StartGameplay();
        }
    }

    /// <summary>
    /// Connect hint button OnClick to this method, or assign hintButton in the inspector.
    /// </summary>
    public void OnHintButtonClicked()
    {
        TryOpenHint();
    }

    public void TryOpenHint()
    {
        if (!gameplayStarted || isPanelOpen)
            return;

        if (!hintAvailable)
        {
            ShowHintCooldownMessage();
            return;
        }

        StopHintButtonAttention();
        hintAvailable = false;

        if (!keepHintButtonClickableDuringCooldown)
            SetHintButtonInteractable(false);

        UpdateHintButtonTimerText();
        ShowPanel(hintPanelDuration, shouldStartGameplayAfterClose: false, openedFromHint: true);
        onHintOpened?.Invoke();
    }

    /// <summary>
    /// Called by AnswerDrop after a wrong answer. If hint is currently available, the hint button gets attention animation.
    /// </summary>
    public void NotifyWrongDrop()
    {
        if (!animateHintButtonOnWrongDrop || !gameplayStarted || !hintAvailable || isPanelOpen)
            return;

        PlayHintButtonAttention();

        if (showHintAvailableTextOnWrongDrop && fillImage != null && !string.IsNullOrWhiteSpace(hintAvailableWrongDropMessage))
            fillImage.SpawnFloatingText(hintAvailableWrongDropMessage);

        onHintAttentionPlayed?.Invoke();
    }

    public void ClosePanel()
    {
        if (!isPanelOpen || isClosingPanel)
            return;

        isClosingPanel = true;

        if (currentPanelIsStartInfo && stopStartInfoNarrationWhenPanelCloses)
            StopStartInfoNarration();

        StopCountdown();
        KillPanelSequence();

        activePanelSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        if (infoPanelCanvasGroup != null)
        {
            infoPanelCanvasGroup.interactable = false;
            infoPanelCanvasGroup.blocksRaycasts = false;
            activePanelSequence.Join(infoPanelCanvasGroup.DOFade(0f, fadeOutDuration).SetEase(fadeOutEase).SetUpdate(useUnscaledTime));
        }

        if (infoCard != null)
        {
            activePanelSequence.Join(infoCard.DOScale(cardStartScale, fadeOutDuration).SetEase(fadeOutEase).SetUpdate(useUnscaledTime));
        }

        activePanelSequence.OnComplete(() =>
        {
            bool wasHintPanel = currentPanelOpenedFromHint;

            SetInfoPanelImmediate(false);

            isPanelOpen = false;
            isClosingPanel = false;
            currentPanelOpenedFromHint = false;
            currentPanelIsStartInfo = false;

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

    public void ResetStartInfoSave()
    {
        PlayerPrefs.DeleteKey(startInfoPrefsKey);
        PlayerPrefs.Save();
    }

    // Legacy method name kept in case a button/event already points to it.
    public void ResetFirstTimeInfoSave()
    {
        ResetStartInfoSave();
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
        currentPanelIsStartInfo = shouldStartGameplayAfterClose && !openedFromHint;
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

        if (currentPanelIsStartInfo)
            PlayStartInfoNarration();

        activePanelSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        if (infoPanelCanvasGroup != null)
            activePanelSequence.Join(infoPanelCanvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeInEase).SetUpdate(useUnscaledTime));

        if (infoCardCanvasGroup != null)
            activePanelSequence.Join(infoCardCanvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeInEase).SetUpdate(useUnscaledTime));

        if (infoCard != null)
            activePanelSequence.Join(infoCard.DOScale(1f, fadeInDuration).SetEase(cardPopEase).SetUpdate(useUnscaledTime));

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

        SetHintButtonInteractable(keepHintButtonClickableDuringCooldown && gameplayStarted);
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
            Debug.Log(message, this);
    }

    private void PlayHintButtonAttention()
    {
        if (hintButton == null)
            return;

        Transform buttonTransform = hintButton.transform;
        StopHintButtonAttention();
        buttonTransform.localScale = hintButtonOriginalScale;
        buttonTransform.localRotation = Quaternion.identity;

        Sequence sequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(hintButton.gameObject, LinkBehaviour.KillOnDisable);

        int pulses = Mathf.Max(1, hintAttentionPulseCount);
        for (int i = 0; i < pulses; i++)
        {
            sequence.Append(buttonTransform.DOScale(hintButtonOriginalScale * hintAttentionScale, hintAttentionDuration * 0.5f).SetEase(Ease.OutBack).SetUpdate(useUnscaledTime));
            sequence.Append(buttonTransform.DOScale(hintButtonOriginalScale, hintAttentionDuration * 0.5f).SetEase(Ease.InOutSine).SetUpdate(useUnscaledTime));
        }

        sequence.Join(buttonTransform.DOPunchRotation(new Vector3(0f, 0f, 8f), hintAttentionDuration, 8, 0.75f).SetUpdate(useUnscaledTime));
        sequence.OnKill(() => hintButtonAttentionTween = null);
        hintButtonAttentionTween = sequence;
    }

    private void StopHintButtonAttention()
    {
        if (hintButtonAttentionTween != null && hintButtonAttentionTween.IsActive())
            hintButtonAttentionTween.Kill(false);

        hintButtonAttentionTween = null;

        if (hintButton != null)
        {
            hintButton.transform.localScale = hintButtonOriginalScale;
            hintButton.transform.localRotation = Quaternion.identity;
        }
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

    private bool ShouldShowStartInfo()
    {
        if (!showStartInfo)
            return false;

        if (!rememberStartInfoWithPlayerPrefs)
            return true;

        return PlayerPrefs.GetInt(startInfoPrefsKey, 0) == 0;
    }

    private void MarkStartInfoSeen()
    {
        if (!rememberStartInfoWithPlayerPrefs)
            return;

        PlayerPrefs.SetInt(startInfoPrefsKey, 1);
        PlayerPrefs.Save();
    }

    private void PlayStartInfoNarration()
    {
        if (!playStartInfoNarration || startInfoNarrationClip == null)
            return;

        if (startInfoNarrationSource == null)
        {
            startInfoNarrationSource = GetComponent<AudioSource>();
            if (startInfoNarrationSource == null)
                startInfoNarrationSource = gameObject.AddComponent<AudioSource>();
        }

        startInfoNarrationSource.playOnAwake = false;
        startInfoNarrationSource.loop = false;
        startInfoNarrationSource.clip = startInfoNarrationClip;
        startInfoNarrationSource.Play();
    }

    private void StopStartInfoNarration()
    {
        if (startInfoNarrationSource != null && startInfoNarrationSource.isPlaying)
            startInfoNarrationSource.Stop();
    }

    private void UpdateCountdownText(float remaining)
    {
        if (countdownText == null)
            return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(remaining));
        countdownText.text = "Closes in " + seconds + " second" + (seconds == 1 ? "" : "s");
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
        startInfoDuration = Mathf.Max(0.1f, startInfoDuration);
        hintPanelDuration = Mathf.Max(0.1f, hintPanelDuration);
        hintCooldownDuration = Mathf.Max(0f, hintCooldownDuration);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        cardStartScale = Mathf.Max(0.01f, cardStartScale);
        hintAttentionPulseCount = Mathf.Max(1, hintAttentionPulseCount);
        hintAttentionScale = Mathf.Max(1f, hintAttentionScale);
        hintAttentionDuration = Mathf.Max(0.05f, hintAttentionDuration);
    }
}
