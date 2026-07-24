using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using DG.Tweening;

namespace ClockLearningGame
{
    internal enum ClockLearningTutorialStep
    {
        None,
        SingleIntro,
        SingleHourHand,
        SingleMinuteHand,
        SingleHint,
        SingleSubmit,
        DoubleIntro,
        DoubleAmPm,
        DoubleClockAHour,
        DoubleClockAMinute,
        DoubleClockBHour,
        DoubleClockBMinute,
        DoubleHint,
        DoubleSubmit
    }

    [DisallowMultipleComponent]
    public sealed class ClockLearningTutorialController : MonoBehaviour
    {
        [Header("Flow")]
        [SerializeField] private bool enableTutorialOverlay = true;
        [Tooltip("When enabled, this tutorial is shown only once per mode during this scene run. Turn off for repeated testing.")]
        [SerializeField] private bool showOnlyOncePerMode = true;
        [Tooltip("Recommended ON for final builds. Once a player completes this tutorial for a mode, it will not show again after app restarts.")]
        [SerializeField] private bool rememberSeenInPlayerPrefs = true;
        [SerializeField] private string seenPlayerPrefsKey = "ClockLearningGame_TutorialSeen";
        [Tooltip("Uses a small guided practice question before the real round starts. No score, no round progress.")]
        [SerializeField] private bool useDummyPracticeTutorial = true;

        [Header("Dummy Practice - Single Clock")]
        [SerializeField, Range(1, 12)] private int singlePracticeHour = 3;
        [SerializeField, Range(0, 59)] private int singlePracticeMinute = 30;
        [SerializeField, Range(1, 12)] private int singlePracticeStartHour = 12;
        [SerializeField, Range(0, 59)] private int singlePracticeStartMinute = 15;
        [SerializeField] private string singlePracticeDisplayText = "3:30";
        [SerializeField] private string singlePracticePromptText = "Practice time";
        [SerializeField, Range(1, 12)] private int singleHintWrongHour = 8;
        [SerializeField, Range(0, 59)] private int singleHintWrongMinute = 10;

        [Header("Dummy Practice - Double Clock")]
        [SerializeField, Range(1, 12)] private int doublePracticeClockAHour = 3;
        [SerializeField, Range(0, 59)] private int doublePracticeClockAMinute = 30;
        [SerializeField] private bool doublePracticeClockAIsPm;
        [SerializeField, Range(1, 12)] private int doublePracticeClockBHour = 4;
        [SerializeField, Range(0, 59)] private int doublePracticeClockBMinute = 30;
        [SerializeField] private bool doublePracticeClockBIsPm;
        [SerializeField] private string doublePracticeDifferenceText = "1 hour";
        [SerializeField] private string doublePracticePromptText = "Practice difference";
        [Tooltip("The tutorial starts Clock B on PM so the child can practise changing AM/PM before setting the hands.")]
        [SerializeField] private bool doublePracticeStartClockBAsPm = true;
        [SerializeField, Range(1, 12)] private int doubleHintWrongClockAHour = 1;
        [SerializeField, Range(0, 59)] private int doubleHintWrongClockAMinute = 10;
        [SerializeField, Range(1, 12)] private int doubleHintWrongClockBHour = 8;
        [SerializeField, Range(0, 59)] private int doubleHintWrongClockBMinute = 25;

        [Header("Clocks")]
        [SerializeField] private ClockLearningClockView singleClock;
        [SerializeField] private ClockLearningClockView doubleClockA;
        [SerializeField] private ClockLearningClockView doubleClockB;

        [Header("Controls Disabled During Tutorial")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button pauseButton;
        [FormerlySerializedAs("helpButton")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Button singleSubmitButton;
        [SerializeField] private Button singleResetButton;
        [SerializeField] private Button doubleSubmitButton;
        [SerializeField] private Button doubleResetButton;
        [SerializeField] private Toggle clockAPmToggle;
        [SerializeField] private Toggle clockBPmToggle;

        [Header("Skip Tutorial")]
        [SerializeField] private bool enableSkipButton = true;
        [SerializeField] private Button skipTutorialButton;
        [SerializeField] private ClockLearningConfirmationDialog confirmationDialog;
        [SerializeField] private string skipTutorialConfirmMessage = "Skip the tutorial?\nThe game will start now.";

        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [Tooltip("Optional full-screen background. Default opacity is 0, so you can enable it later without rebuilding UI.")]
        [SerializeField] private Image backgroundImage;
        [SerializeField, Range(0f, 1f)] private float backgroundOpacity = 0f;
        [Tooltip("Invisible full-screen button used only for first 'tap anywhere' tutorial steps.")]
        [SerializeField] private Button clickAnywhereButton;
        [SerializeField] private RectTransform pointer;
        [SerializeField] private Image pointerImage;
        [SerializeField] private Sprite pointerSprite;
        [SerializeField] private RectTransform ghostHand;
        [SerializeField] private Image ghostHandImage;
        [SerializeField] private Sprite ghostHandSprite;
        [Tooltip("When enabled, the tutorial ghost hand copies the real hour/minute hand sprite and image type. Keep enabled for final art.")]
        [SerializeField] private bool copyActualHandSpriteForGhost = true;
        [SerializeField] private Color ghostHandColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Vector2 ghostHandFallbackSize = new Vector2(16f, 165f);
        [SerializeField] private RectTransform promptCard;
        [SerializeField, Range(0f, 1f)] private float promptBackgroundOpacity = 0.86f;
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("Dummy Question UI Texts")]
        [SerializeField] private TextMeshProUGUI singlePromptTextTarget;
        [SerializeField] private TextMeshProUGUI singleTargetTextTarget;
        [SerializeField] private TextMeshProUGUI doublePromptTextTarget;
        [SerializeField] private TextMeshProUGUI doubleTargetTextTarget;
        [SerializeField] private TextMeshProUGUI doubleChipTextTarget;

        [Header("Targets")]
        [SerializeField] private RectTransform singleQuestionTarget;
        [SerializeField] private RectTransform doubleQuestionTarget;
        [Tooltip("Optional. If not assigned, the tutorial points to the top-bar Hint button reference.")]
        [SerializeField] private RectTransform hintButtonTarget;
        [Tooltip("Optional. If not assigned, the tutorial points to the Clock B AM/PM toggle for the guided practice step.")]
        [SerializeField] private RectTransform doubleAmPmTarget;
        [SerializeField] private RectTransform singleClockTarget;
        [SerializeField] private RectTransform doubleClockATarget;
        [SerializeField] private RectTransform doubleClockBTarget;

        [Header("Pointer Position")]
        [SerializeField] private Vector2 questionPointerOffset = new Vector2(70f, -115f);
        [SerializeField] private Vector2 normalPointerOffset = new Vector2(70f, -55f);
        [SerializeField, Range(0.55f, 1.05f)] private float pointerHandTipFollow = 1f;
        [SerializeField] private Vector2 pointerHandTipOffset = Vector2.zero;

        [Header("Prompt Position")]
        [SerializeField] private bool autoPositionPrompt = true;
        [SerializeField] private Vector2 promptCardSize = new Vector2(920f, 92f);
        [SerializeField] private Vector2 questionPromptOffset = new Vector2(0f, -185f);
        [SerializeField] private Vector2 clockPromptOffset = new Vector2(0f, -245f);
        [SerializeField] private Vector2 readyPromptOffset = Vector2.zero;
        [SerializeField] private Vector2 hintPromptOffset = new Vector2(0f, -120f);
        [SerializeField] private Vector2 amPmPromptOffset = new Vector2(0f, 190f);
        [SerializeField] private Vector2 promptClampMargin = new Vector2(60f, 45f);

        [Header("Motion Polish")]
        [SerializeField, Range(0.15f, 1.2f)] private float pointerMoveDuration = 0.52f;
        [SerializeField, Range(4f, 40f)] private float pointerHoverPixels = 10f;
        [SerializeField, Range(0.6f, 2.5f)] private float fakeHandMoveDuration = 1.25f;
        [SerializeField, Range(0f, 1.25f)] private float stepTransitionDelay = 0.65f;
        [SerializeField, Range(0.2f, 1.4f)] private float retryPromptDelay = 0.65f;
        [SerializeField, Range(0.25f, 1.5f)] private float gameStartingMessageDelay = 0.75f;

        [Header("Text - Single Practice")]
        [SerializeField] private string singleIntroPrompt = "Practice time: set the clock to half past three. Tap anywhere to begin.";
        [SerializeField] private string singleHourPrompt = "First, move the short hand near 3.";
        [SerializeField] private string singleHourSuccessPrompt = "Great! The short hand shows the hour.";
        [SerializeField] private string singleHourRetryPrompt = "Almost. Move the short hand near 3.";
        [SerializeField] private string singleMinutePrompt = "Now move the long hand to 6 for 30 minutes.";
        [SerializeField] private string singleMinuteSuccessPrompt = "Good! The long hand shows 30 minutes.";
        [SerializeField] private string singleMinuteRetryPrompt = "Almost. Move the long hand to 6.";
        [SerializeField] private string singleHintPrompt = "This clock is not right yet. Tap Hint to see help.";
        [SerializeField] private string singleSubmitPrompt = "All set! Tap Submit to start the real game.";

        [Header("Text - Double Practice")]
        [SerializeField] private string doubleIntroPrompt = "Practice: make a difference of 1 hour. Tap anywhere to begin.";
        [SerializeField] private string doubleAmPmPrompt = "First, set Clock B to AM.";
        [SerializeField] private string doubleAmPmSuccessPrompt = "Good! Both clocks are using AM.";
        [SerializeField] private string doubleAmPmRetryPrompt = "Tap the Clock B AM/PM button until it shows AM.";
        [SerializeField] private string doubleClockAHourPrompt = "Clock A: move the short hand near 3.";
        [SerializeField] private string doubleClockAMinutePrompt = "Clock A: move the long hand to 6.";
        [SerializeField] private string doubleClockBHourPrompt = "Clock B: move the short hand near 4.";
        [SerializeField] private string doubleClockBMinutePrompt = "Clock B: move the long hand to 6.";
        [SerializeField] private string doubleHandSuccessPrompt = "Good! That hand is in place.";
        [SerializeField] private string doubleHandRetryPrompt = "Almost. Try the highlighted hand again.";
        [SerializeField] private string doubleHintPrompt = "These clocks are not right yet. Tap Hint to see help.";
        [SerializeField] private string doubleSubmitPrompt = "Now the clocks are 1 hour apart. Tap Submit to start.";
        [SerializeField] private string gameStartingPrompt = "The game is starting!";

        private ClockLearningMode _mode;
        private ClockLearningTutorialStep _step = ClockLearningTutorialStep.None;
        private bool _running;
        private bool _singleSeenThisSession;
        private bool _doubleSeenThisSession;
        private bool _userDraggingCurrentStep;
        private bool _waitingForDelay;
        private bool _subscribedToClockEvents;
        private Action _onComplete;
        private Sequence _pointerSequence;
        private Sequence _ghostSequence;
        private Coroutine _stepDelayRoutine;
        private Coroutine _hintDemoRoutine;
        private ClockLearningTutorialStep _preparedWrongHintStep = ClockLearningTutorialStep.None;
        private CanvasGroup _pointerCanvasGroup;

        private string _cachedSinglePrompt;
        private string _cachedSingleTarget;
        private string _cachedDoublePrompt;
        private string _cachedDoubleTarget;
        private string _cachedDoubleChip;
        private bool _cachedUiText;

        public bool IsRunning => _running;

        private void Awake()
        {
            EnsureSafeValues();
            EnsureRuntimeObjects();
            TryAutoFindDummyTextReferences();
            ApplyBackgroundVisuals();
            HideInstant();
        }

        private void OnEnable()
        {
            SubscribeClockEvents();
            if (clickAnywhereButton != null)
            {
                clickAnywhereButton.onClick.RemoveListener(AdvanceFromClickAnywhere);
                clickAnywhereButton.onClick.AddListener(AdvanceFromClickAnywhere);
            }
            RegisterSkipButton(true);
            RegisterExpectedControlListeners(true);
        }

        private void OnDisable()
        {
            UnsubscribeClockEvents();
            if (clickAnywhereButton != null) clickAnywhereButton.onClick.RemoveListener(AdvanceFromClickAnywhere);
            RegisterSkipButton(false);
            RegisterExpectedControlListeners(false);
            KillTweens();
        }

        private void OnValidate()
        {
            EnsureSafeValues();
        }

        private void EnsureSafeValues()
        {
            pointerMoveDuration = Mathf.Clamp(pointerMoveDuration, 0.15f, 1.2f);
            pointerHoverPixels = Mathf.Clamp(pointerHoverPixels, 4f, 40f);
            fakeHandMoveDuration = Mathf.Clamp(fakeHandMoveDuration, 0.6f, 2.5f);
            stepTransitionDelay = Mathf.Clamp(stepTransitionDelay, 0f, 1.25f);
            retryPromptDelay = Mathf.Clamp(retryPromptDelay, 0.2f, 1.4f);
            gameStartingMessageDelay = Mathf.Clamp(gameStartingMessageDelay, 0.25f, 1.5f);
            backgroundOpacity = Mathf.Clamp01(backgroundOpacity);
            promptBackgroundOpacity = Mathf.Clamp01(promptBackgroundOpacity);
            ghostHandFallbackSize.x = Mathf.Max(2f, ghostHandFallbackSize.x);
            ghostHandFallbackSize.y = Mathf.Max(40f, ghostHandFallbackSize.y);
            if (string.IsNullOrWhiteSpace(seenPlayerPrefsKey)) seenPlayerPrefsKey = "ClockLearningGame_TutorialSeen";
            promptCardSize.x = Mathf.Max(220f, promptCardSize.x);
            promptCardSize.y = Mathf.Max(50f, promptCardSize.y);
            promptClampMargin.x = Mathf.Max(0f, promptClampMargin.x);
            promptClampMargin.y = Mathf.Max(0f, promptClampMargin.y);
            singlePracticeMinute = Mathf.Clamp(singlePracticeMinute, 0, 59);
            singlePracticeStartMinute = Mathf.Clamp(singlePracticeStartMinute, 0, 59);
            singleHintWrongMinute = Mathf.Clamp(singleHintWrongMinute, 0, 59);
            doublePracticeClockAMinute = Mathf.Clamp(doublePracticeClockAMinute, 0, 59);
            doublePracticeClockBMinute = Mathf.Clamp(doublePracticeClockBMinute, 0, 59);
            doubleHintWrongClockAMinute = Mathf.Clamp(doubleHintWrongClockAMinute, 0, 59);
            doubleHintWrongClockBMinute = Mathf.Clamp(doubleHintWrongClockBMinute, 0, 59);
        }

        public bool ShouldRun(ClockLearningMode mode)
        {
            if (!enableTutorialOverlay) return false;
            if (!showOnlyOncePerMode) return true;
            return !HasSeen(mode);
        }

        public void Run(ClockLearningMode mode, Action onComplete)
        {
            if (!enableTutorialOverlay)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureRuntimeObjects();
            TryAutoFindDummyTextReferences();
            SubscribeClockEvents();
            KillTweens();
            CancelStepDelay();

            _mode = mode;
            _onComplete = onComplete;
            _running = true;
            _waitingForDelay = false;
            _userDraggingCurrentStep = false;
            _preparedWrongHintStep = ClockLearningTutorialStep.None;
            _step = mode == ClockLearningMode.SingleClockSetTime ? ClockLearningTutorialStep.SingleIntro : ClockLearningTutorialStep.DoubleIntro;

            CacheOriginalUiText();
            PrepareDummyPracticeVisuals(mode);
            ShowOverlay(true, false);
            RefreshStep();
        }

        public void HideInstant()
        {
            CancelStepDelay();
            _running = false;
            _waitingForDelay = false;
            _userDraggingCurrentStep = false;
            _preparedWrongHintStep = ClockLearningTutorialStep.None;
            if (_hintDemoRoutine != null)
            {
                StopCoroutine(_hintDemoRoutine);
                _hintDemoRoutine = null;
            }
            _step = ClockLearningTutorialStep.None;
            KillTweens();
            RestoreOriginalUiText();
            if (ghostHand != null) ghostHand.gameObject.SetActive(false);
            SetPointerVisible(false, true);
            if (clickAnywhereButton != null) clickAnywhereButton.gameObject.SetActive(false);
            if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(false);
            RestoreControlsAfterTutorial();
            ShowOverlay(false, true);
        }

        public void KillTweens()
        {
            _pointerSequence?.Kill();
            _ghostSequence?.Kill();
            if (pointer != null) pointer.DOKill();
            if (ghostHand != null) ghostHand.DOKill();
            if (overlayGroup != null)
            {
                overlayGroup.DOKill();
                overlayGroup.transform.DOKill();
            }
            if (promptCard != null) promptCard.DOKill();
        }

        public void ApplyFont(TMP_FontAsset font)
        {
            if (font != null && promptText != null) promptText.font = font;
        }

        private void SubscribeClockEvents()
        {
            if (_subscribedToClockEvents) return;
            if (singleClock != null)
            {
                singleClock.UserStartedDrag += HandleClockStartedDrag;
                singleClock.UserChangedTimeByDrag += HandleClockChangedByDrag;
                singleClock.UserFinishedDrag += HandleClockFinishedDrag;
            }
            if (doubleClockA != null)
            {
                doubleClockA.UserStartedDrag += HandleClockStartedDrag;
                doubleClockA.UserChangedTimeByDrag += HandleClockChangedByDrag;
                doubleClockA.UserFinishedDrag += HandleClockFinishedDrag;
            }
            if (doubleClockB != null)
            {
                doubleClockB.UserStartedDrag += HandleClockStartedDrag;
                doubleClockB.UserChangedTimeByDrag += HandleClockChangedByDrag;
                doubleClockB.UserFinishedDrag += HandleClockFinishedDrag;
            }
            _subscribedToClockEvents = true;
        }

        private void UnsubscribeClockEvents()
        {
            if (!_subscribedToClockEvents) return;
            if (singleClock != null)
            {
                singleClock.UserStartedDrag -= HandleClockStartedDrag;
                singleClock.UserChangedTimeByDrag -= HandleClockChangedByDrag;
                singleClock.UserFinishedDrag -= HandleClockFinishedDrag;
            }
            if (doubleClockA != null)
            {
                doubleClockA.UserStartedDrag -= HandleClockStartedDrag;
                doubleClockA.UserChangedTimeByDrag -= HandleClockChangedByDrag;
                doubleClockA.UserFinishedDrag -= HandleClockFinishedDrag;
            }
            if (doubleClockB != null)
            {
                doubleClockB.UserStartedDrag -= HandleClockStartedDrag;
                doubleClockB.UserChangedTimeByDrag -= HandleClockChangedByDrag;
                doubleClockB.UserFinishedDrag -= HandleClockFinishedDrag;
            }
            _subscribedToClockEvents = false;
        }

        private void EnsureRuntimeObjects()
        {
            if (overlayGroup == null) return;
            RectTransform overlayRect = overlayGroup.transform as RectTransform;
            if (overlayRect == null) return;

            if (backgroundImage == null)
            {
                RectTransform bg = CreateRuntimeRect("Tutorial Optional Background", overlayRect, true);
                bg.SetAsFirstSibling();
                backgroundImage = bg.gameObject.AddComponent<Image>();
                backgroundImage.raycastTarget = false;
            }

            if (clickAnywhereButton == null)
            {
                RectTransform click = CreateRuntimeRect("Tutorial Click Anywhere Button", overlayRect, true);
                click.SetAsLastSibling();
                Image clickImage = click.gameObject.AddComponent<Image>();
                clickImage.color = new Color(1f, 1f, 1f, 0f);
                clickImage.raycastTarget = true;
                clickAnywhereButton = click.gameObject.AddComponent<Button>();
                clickAnywhereButton.transition = Selectable.Transition.None;
                clickAnywhereButton.targetGraphic = clickImage;
                clickAnywhereButton.onClick.RemoveListener(AdvanceFromClickAnywhere);
                clickAnywhereButton.onClick.AddListener(AdvanceFromClickAnywhere);
            }

            if (skipTutorialButton == null && enableSkipButton)
            {
                RectTransform skip = CreateRuntimeRect("Tutorial Skip Button", overlayRect, false);
                skip.anchorMin = new Vector2(0.84f, 0.88f);
                skip.anchorMax = new Vector2(0.97f, 0.96f);
                skip.offsetMin = Vector2.zero;
                skip.offsetMax = Vector2.zero;
                Image skipImage = skip.gameObject.AddComponent<Image>();
                skipImage.color = new Color(1f, 0.86f, 0.36f, 0.95f);
                skipImage.raycastTarget = true;
                skipTutorialButton = skip.gameObject.AddComponent<Button>();
                skipTutorialButton.targetGraphic = skipImage;
                TextMeshProUGUI skipText = skip.gameObject.AddComponent<TextMeshProUGUI>();
                skipText = skip.GetComponent<TextMeshProUGUI>();
                if (skipText == null)
                    skipText = skip.gameObject.AddComponent<TextMeshProUGUI>();

                if (skipText != null)
                {
                    skipText.text = "Skip";
                    skipText.fontSize = 26f;
                    skipText.fontStyle = FontStyles.Bold;
                    skipText.alignment = TextAlignmentOptions.Center;
                    skipText.color = new Color(0.23f, 0.17f, 0.08f, 1f);
                    skipText.raycastTarget = false;
                }
                RegisterSkipButton(true);
            }

            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(enableSkipButton && _running);
                skipTutorialButton.interactable = enableSkipButton && _running;
            }

            if (ghostHand == null)
            {
                GameObject ghost = new GameObject("Tutorial Fake Clock Hand", typeof(RectTransform));
                ghostHand = ghost.GetComponent<RectTransform>();
                ghostHand.SetParent(overlayRect, false);
                ghostHand.anchorMin = new Vector2(0.5f, 0.5f);
                ghostHand.anchorMax = new Vector2(0.5f, 0.5f);
                ghostHand.pivot = new Vector2(0.5f, 0f);
                ghostHand.sizeDelta = ghostHandFallbackSize;
                ghostHandImage = ghost.AddComponent<Image>();
                ghostHandImage.raycastTarget = false;
                ghostHand.gameObject.SetActive(false);
            }

            if (pointer != null) pointer.SetAsLastSibling();
            ApplyBackgroundVisuals();
            ApplyPointerVisual();
            DisableRaycastsForGraphicTree(promptCard);
        }

        private static RectTransform CreateRuntimeRect(string objectName, RectTransform parent, bool stretch)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            if (stretch)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;
            }
            return rect;
        }

        private void RegisterExpectedControlListeners(bool register)
        {
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(AdvanceFromHintButton);
                if (register) hintButton.onClick.AddListener(AdvanceFromHintButton);
            }
            if (singleSubmitButton != null)
            {
                singleSubmitButton.onClick.RemoveListener(AdvanceFromSubmitButton);
                if (register) singleSubmitButton.onClick.AddListener(AdvanceFromSubmitButton);
            }
            if (doubleSubmitButton != null)
            {
                doubleSubmitButton.onClick.RemoveListener(AdvanceFromSubmitButton);
                if (register) doubleSubmitButton.onClick.AddListener(AdvanceFromSubmitButton);
            }
            if (clockAPmToggle != null)
            {
                clockAPmToggle.onValueChanged.RemoveListener(AdvanceFromAmPmToggle);
                if (register) clockAPmToggle.onValueChanged.AddListener(AdvanceFromAmPmToggle);
            }
            if (clockBPmToggle != null)
            {
                clockBPmToggle.onValueChanged.RemoveListener(AdvanceFromAmPmToggle);
                if (register) clockBPmToggle.onValueChanged.AddListener(AdvanceFromAmPmToggle);
            }
        }

        private void RegisterSkipButton(bool register)
        {
            if (skipTutorialButton == null) return;
            skipTutorialButton.onClick.RemoveListener(RequestSkipTutorial);
            if (register) skipTutorialButton.onClick.AddListener(RequestSkipTutorial);
        }

        public void RequestSkipTutorial()
        {
            if (!_running || !enableSkipButton) return;

            CancelStepDelay();
            StopFakeHandDemo(true);
            SetPointerVisible(false);
            if (skipTutorialButton != null) skipTutorialButton.interactable = false;

            if (confirmationDialog != null)
            {
                confirmationDialog.Show(
                    skipTutorialConfirmMessage,
                    SkipTutorialNow,
                    ResumeTutorialAfterSkipCancel,
                    "Skip tutorial?",
                    "Skip",
                    "Stay");
                return;
            }

            SkipTutorialNow();
        }

        private void ResumeTutorialAfterSkipCancel()
        {
            if (!_running) return;
            if (skipTutorialButton != null) skipTutorialButton.interactable = true;
            RefreshStep();
        }

        private void SkipTutorialNow()
        {
            if (!_running) return;
            if (skipTutorialButton != null) skipTutorialButton.interactable = false;
            ScheduleCompleteTutorial();
        }

        private void AdvanceFromClickAnywhere()
        {
            if (!_running || _waitingForDelay || !IsClickAnywhereStep()) return;
            if (_step == ClockLearningTutorialStep.SingleIntro) ScheduleMoveToStep(ClockLearningTutorialStep.SingleHourHand);
            else if (_step == ClockLearningTutorialStep.DoubleIntro) ScheduleMoveToStep(ClockLearningTutorialStep.DoubleAmPm);
        }

        private void AdvanceFromHintButton()
        {
            if (!_running || _waitingForDelay || !IsHintButtonStep()) return;

            CancelStepDelay();
            if (_hintDemoRoutine != null) StopCoroutine(_hintDemoRoutine);
            _hintDemoRoutine = StartCoroutine(RunPracticeHintDemoRoutine(_step));
        }

        private void AdvanceFromSubmitButton()
        {
            if (!_running || _waitingForDelay || !IsSubmitButtonStep()) return;
            ScheduleCompleteTutorial();
        }

        private void AdvanceFromAmPmToggle(bool value)
        {
            if (!_running || _waitingForDelay || _step != ClockLearningTutorialStep.DoubleAmPm) return;

            if (IsDoubleAmPmCorrect())
            {
                ScheduleMoveToStep(ClockLearningTutorialStep.DoubleClockAHour, doubleAmPmSuccessPrompt, true);
            }
            else
            {
                ScheduleRepeatCurrentStep(doubleAmPmRetryPrompt);
            }
        }

        private void HandleClockStartedDrag(ClockLearningClockView sourceClock, ClockLearningHandType handType)
        {
            if (!_running || sourceClock == null || _waitingForDelay) return;
            if (!IsCurrentRequiredHand(sourceClock, handType)) return;

            _userDraggingCurrentStep = true;
            StopFakeHandDemo(true);
            SetPointerVisible(false);
        }

        private void HandleClockChangedByDrag(ClockLearningClockView sourceClock, ClockLearningHandType handType)
        {
            if (!_running || sourceClock == null || _waitingForDelay) return;
            if (!IsCurrentRequiredHand(sourceClock, handType)) return;

            _userDraggingCurrentStep = true;
            StopFakeHandDemo(true);
            SetPointerVisible(false);
        }

        private void HandleClockFinishedDrag(ClockLearningClockView sourceClock, ClockLearningHandType handType)
        {
            if (!_running || sourceClock == null || _waitingForDelay) return;
            if (!IsCurrentRequiredHand(sourceClock, handType)) return;

            _userDraggingCurrentStep = false;
            if (IsCurrentHandCorrect())
            {
                ScheduleMoveToStep(GetNextStepAfterCorrectHand(), GetSuccessPromptForCurrentHand(), true);
            }
            else
            {
                ScheduleRepeatCurrentStep(GetRetryPromptForCurrentHand());
            }
        }

        private void CancelStepDelay()
        {
            if (_stepDelayRoutine != null)
            {
                StopCoroutine(_stepDelayRoutine);
                _stepDelayRoutine = null;
            }
            _waitingForDelay = false;
        }

        private void ScheduleMoveToStep(ClockLearningTutorialStep nextStep, string duringDelayPrompt = null, bool lockClockDuringDelay = false)
        {
            if (!_running) return;
            CancelStepDelay();
            _stepDelayRoutine = StartCoroutine(StepDelayRoutine(nextStep, false, lockClockDuringDelay, duringDelayPrompt, stepTransitionDelay));
        }

        private void ScheduleRepeatCurrentStep(string duringDelayPrompt)
        {
            if (!_running) return;
            CancelStepDelay();
            _stepDelayRoutine = StartCoroutine(StepDelayRoutine(_step, false, true, duringDelayPrompt, retryPromptDelay));
        }

        private void ScheduleCompleteTutorial()
        {
            if (!_running) return;
            CancelStepDelay();
            _stepDelayRoutine = StartCoroutine(StepDelayRoutine(ClockLearningTutorialStep.None, true, true, gameStartingPrompt, gameStartingMessageDelay));
        }

        private IEnumerator StepDelayRoutine(ClockLearningTutorialStep nextStep, bool completeTutorial, bool lockClockDuringDelay, string duringDelayPrompt, float delay)
        {
            _waitingForDelay = true;
            _userDraggingCurrentStep = false;
            if (lockClockDuringDelay) SetAllClockInput(false);
            StopFakeHandDemo(true);
            if (completeTutorial || lockClockDuringDelay) SetPointerVisible(false);

            if (!string.IsNullOrWhiteSpace(duringDelayPrompt) && promptText != null)
            {
                promptText.text = duringDelayPrompt;
                PulsePromptCard();
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, delay));

            _waitingForDelay = false;
            _stepDelayRoutine = null;
            if (!_running) yield break;

            if (completeTutorial) CompleteTutorial();
            else MoveToStep(nextStep);
        }

        private void MoveToStep(ClockLearningTutorialStep nextStep)
        {
            _step = nextStep;
            RefreshStep();
        }

        private void RefreshStep()
        {
            _userDraggingCurrentStep = false;
            PrepareWrongHintStateIfNeeded();
            ApplyInputRules();
            ApplyPointerVisual();
            ApplyBackgroundVisuals();
            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(enableSkipButton && _running);
                skipTutorialButton.interactable = enableSkipButton && _running;
            }

            if (promptText != null) promptText.text = GetPrompt(_step);
            RectTransform target = GetTarget(_step);
            MovePromptToTarget(target, _step);

            if (IsHandDemoStep(_step)) StartFakeHandDemo(_step);
            else
            {
                StopFakeHandDemo(true);
                MovePointerToTarget(target);
            }
        }

        private void PrepareWrongHintStateIfNeeded()
        {
            if (!IsHintButtonStep() || _preparedWrongHintStep == _step) return;

            _preparedWrongHintStep = _step;
            StopFakeHandDemo(true);
            if (_step == ClockLearningTutorialStep.SingleHint)
            {
                if (singleClock != null) singleClock.SetTime(singleHintWrongHour, singleHintWrongMinute, false);
                return;
            }

            if (_step == ClockLearningTutorialStep.DoubleHint)
            {
                if (doubleClockA != null) doubleClockA.SetTime(doubleHintWrongClockAHour, doubleHintWrongClockAMinute, false);
                if (doubleClockB != null) doubleClockB.SetTime(doubleHintWrongClockBHour, doubleHintWrongClockBMinute, false);
                if (clockAPmToggle != null) clockAPmToggle.isOn = doublePracticeClockAIsPm;
                if (clockBPmToggle != null) clockBPmToggle.isOn = doublePracticeClockBIsPm;
            }
        }

        private IEnumerator RunPracticeHintDemoRoutine(ClockLearningTutorialStep hintStep)
        {
            _waitingForDelay = true;
            _userDraggingCurrentStep = false;
            SetAllClockInput(false);
            SetExpectedTutorialControlInteractable(false, false, false);
            StopFakeHandDemo(true);
            SetPointerVisible(false);

            if (hintStep == ClockLearningTutorialStep.SingleHint)
            {
                yield return AnimatePracticeHintClock(singleClock, singlePracticeHour, singlePracticeMinute,
                    "Hint moves the short hand first.",
                    "Now Hint moves the long hand.");
                _waitingForDelay = false;
                _hintDemoRoutine = null;
                ScheduleMoveToStep(ClockLearningTutorialStep.SingleSubmit, singleSubmitPrompt, true);
                yield break;
            }

            if (hintStep == ClockLearningTutorialStep.DoubleHint)
            {
                yield return AnimatePracticeHintClock(doubleClockA, doublePracticeClockAHour, doublePracticeClockAMinute,
                    "Hint sets Clock A's short hand first.",
                    "Hint sets Clock A's long hand next.");

                if (stepTransitionDelay > 0f) yield return new WaitForSecondsRealtime(Mathf.Min(0.5f, stepTransitionDelay));

                yield return AnimatePracticeHintClock(doubleClockB, doublePracticeClockBHour, doublePracticeClockBMinute,
                    "Hint sets Clock B's short hand first.",
                    "Hint sets Clock B's long hand next.");

                _waitingForDelay = false;
                _hintDemoRoutine = null;
                ScheduleMoveToStep(ClockLearningTutorialStep.DoubleSubmit, doubleSubmitPrompt, true);
                yield break;
            }

            _waitingForDelay = false;
            _hintDemoRoutine = null;
        }

        private IEnumerator AnimatePracticeHintClock(ClockLearningClockView clock, int targetHour, int targetMinute, string hourPrompt, string minutePrompt)
        {
            if (clock == null) yield break;

            if (promptText != null && !string.IsNullOrWhiteSpace(hourPrompt))
            {
                promptText.text = hourPrompt;
                PulsePromptCard();
            }

            Tween hourTween = clock.AnimateHandToTimeForHint(ClockLearningHandType.Hour, targetHour, targetMinute, fakeHandMoveDuration * 0.85f, false);
            if (hourTween != null) yield return hourTween.WaitForCompletion();

            if (stepTransitionDelay > 0f) yield return new WaitForSecondsRealtime(Mathf.Min(0.45f, stepTransitionDelay));

            if (promptText != null && !string.IsNullOrWhiteSpace(minutePrompt))
            {
                promptText.text = minutePrompt;
                PulsePromptCard();
            }

            Tween minuteTween = clock.AnimateHandToTimeForHint(ClockLearningHandType.Minute, targetHour, targetMinute, fakeHandMoveDuration * 0.85f, true);
            if (minuteTween != null) yield return minuteTween.WaitForCompletion();
        }

        private string GetPrompt(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleIntro: return singleIntroPrompt;
                case ClockLearningTutorialStep.SingleHourHand: return singleHourPrompt;
                case ClockLearningTutorialStep.SingleMinuteHand: return singleMinutePrompt;
                case ClockLearningTutorialStep.SingleHint: return singleHintPrompt;
                case ClockLearningTutorialStep.SingleSubmit: return singleSubmitPrompt;
                case ClockLearningTutorialStep.DoubleIntro: return doubleIntroPrompt;
                case ClockLearningTutorialStep.DoubleAmPm: return doubleAmPmPrompt;
                case ClockLearningTutorialStep.DoubleClockAHour: return doubleClockAHourPrompt;
                case ClockLearningTutorialStep.DoubleClockAMinute: return doubleClockAMinutePrompt;
                case ClockLearningTutorialStep.DoubleClockBHour: return doubleClockBHourPrompt;
                case ClockLearningTutorialStep.DoubleClockBMinute: return doubleClockBMinutePrompt;
                case ClockLearningTutorialStep.DoubleHint: return doubleHintPrompt;
                case ClockLearningTutorialStep.DoubleSubmit: return doubleSubmitPrompt;
                default: return string.Empty;
            }
        }

        private RectTransform GetTarget(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleIntro: return singleQuestionTarget;
                case ClockLearningTutorialStep.SingleHint: return hintButtonTarget != null ? hintButtonTarget : GetRect(hintButton);
                case ClockLearningTutorialStep.SingleSubmit: return GetRect(singleSubmitButton) != null ? GetRect(singleSubmitButton) : singleQuestionTarget;
                case ClockLearningTutorialStep.SingleHourHand:
                case ClockLearningTutorialStep.SingleMinuteHand: return singleClockTarget != null ? singleClockTarget : GetRect(singleClock);
                case ClockLearningTutorialStep.DoubleIntro: return doubleQuestionTarget;
                case ClockLearningTutorialStep.DoubleAmPm: return doubleAmPmTarget != null ? doubleAmPmTarget : GetDoubleAmPmFallbackTarget();
                case ClockLearningTutorialStep.DoubleHint: return hintButtonTarget != null ? hintButtonTarget : GetRect(hintButton);
                case ClockLearningTutorialStep.DoubleSubmit: return GetRect(doubleSubmitButton) != null ? GetRect(doubleSubmitButton) : doubleQuestionTarget;
                case ClockLearningTutorialStep.DoubleClockAHour:
                case ClockLearningTutorialStep.DoubleClockAMinute: return doubleClockATarget != null ? doubleClockATarget : GetRect(doubleClockA);
                case ClockLearningTutorialStep.DoubleClockBHour:
                case ClockLearningTutorialStep.DoubleClockBMinute: return doubleClockBTarget != null ? doubleClockBTarget : GetRect(doubleClockB);
                default: return null;
            }
        }

        private static RectTransform GetRect(Component component)
        {
            return component == null ? null : component.transform as RectTransform;
        }

        private RectTransform GetDoubleAmPmFallbackTarget()
        {
            if (clockBPmToggle != null) return clockBPmToggle.transform as RectTransform;
            if (clockAPmToggle != null) return clockAPmToggle.transform as RectTransform;
            return doubleQuestionTarget;
        }

        private bool IsClickAnywhereStep()
        {
            return _step == ClockLearningTutorialStep.SingleIntro || _step == ClockLearningTutorialStep.DoubleIntro;
        }

        private bool IsHintButtonStep()
        {
            return _step == ClockLearningTutorialStep.SingleHint || _step == ClockLearningTutorialStep.DoubleHint;
        }

        private bool IsSubmitButtonStep()
        {
            return _step == ClockLearningTutorialStep.SingleSubmit || _step == ClockLearningTutorialStep.DoubleSubmit;
        }

        private bool IsAmPmButtonStep()
        {
            return _step == ClockLearningTutorialStep.DoubleAmPm;
        }

        private static bool IsHandDemoStep(ClockLearningTutorialStep step)
        {
            return step == ClockLearningTutorialStep.SingleHourHand
                || step == ClockLearningTutorialStep.SingleMinuteHand
                || step == ClockLearningTutorialStep.DoubleClockAHour
                || step == ClockLearningTutorialStep.DoubleClockAMinute
                || step == ClockLearningTutorialStep.DoubleClockBHour
                || step == ClockLearningTutorialStep.DoubleClockBMinute;
        }

        private static ClockLearningHandType GetRequiredHand(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleHourHand:
                case ClockLearningTutorialStep.DoubleClockAHour:
                case ClockLearningTutorialStep.DoubleClockBHour:
                    return ClockLearningHandType.Hour;
                case ClockLearningTutorialStep.SingleMinuteHand:
                case ClockLearningTutorialStep.DoubleClockAMinute:
                case ClockLearningTutorialStep.DoubleClockBMinute:
                    return ClockLearningHandType.Minute;
                default:
                    return ClockLearningHandType.None;
            }
        }

        private ClockLearningClockView GetRequiredClock(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleHourHand:
                case ClockLearningTutorialStep.SingleMinuteHand:
                    return singleClock;
                case ClockLearningTutorialStep.DoubleClockAHour:
                case ClockLearningTutorialStep.DoubleClockAMinute:
                    return doubleClockA;
                case ClockLearningTutorialStep.DoubleClockBHour:
                case ClockLearningTutorialStep.DoubleClockBMinute:
                    return doubleClockB;
                default:
                    return null;
            }
        }

        private bool IsCurrentRequiredHand(ClockLearningClockView sourceClock, ClockLearningHandType handType)
        {
            return IsHandDemoStep(_step) && GetRequiredClock(_step) == sourceClock && GetRequiredHand(_step) == handType;
        }

        private ClockLearningTutorialStep GetNextStepAfterCorrectHand()
        {
            switch (_step)
            {
                case ClockLearningTutorialStep.SingleHourHand: return ClockLearningTutorialStep.SingleMinuteHand;
                case ClockLearningTutorialStep.SingleMinuteHand: return ClockLearningTutorialStep.SingleHint;
                case ClockLearningTutorialStep.DoubleClockAHour: return ClockLearningTutorialStep.DoubleClockAMinute;
                case ClockLearningTutorialStep.DoubleClockAMinute: return ClockLearningTutorialStep.DoubleClockBHour;
                case ClockLearningTutorialStep.DoubleClockBHour: return ClockLearningTutorialStep.DoubleClockBMinute;
                case ClockLearningTutorialStep.DoubleClockBMinute: return ClockLearningTutorialStep.DoubleHint;
                default: return _step;
            }
        }

        private string GetSuccessPromptForCurrentHand()
        {
            if (_step == ClockLearningTutorialStep.SingleHourHand) return singleHourSuccessPrompt;
            if (_step == ClockLearningTutorialStep.SingleMinuteHand) return singleMinuteSuccessPrompt;
            return doubleHandSuccessPrompt;
        }

        private string GetRetryPromptForCurrentHand()
        {
            if (_step == ClockLearningTutorialStep.SingleHourHand) return singleHourRetryPrompt;
            if (_step == ClockLearningTutorialStep.SingleMinuteHand) return singleMinuteRetryPrompt;
            return doubleHandRetryPrompt;
        }

        private bool IsCurrentHandCorrect()
        {
            switch (_step)
            {
                case ClockLearningTutorialStep.SingleHourHand:
                    return IsHourCorrect(singleClock, singlePracticeHour);
                case ClockLearningTutorialStep.SingleMinuteHand:
                    return IsHourCorrect(singleClock, singlePracticeHour) && IsMinuteCorrect(singleClock, singlePracticeMinute);
                case ClockLearningTutorialStep.DoubleClockAHour:
                    return IsHourCorrect(doubleClockA, doublePracticeClockAHour);
                case ClockLearningTutorialStep.DoubleClockAMinute:
                    return IsHourCorrect(doubleClockA, doublePracticeClockAHour) && IsMinuteCorrect(doubleClockA, doublePracticeClockAMinute);
                case ClockLearningTutorialStep.DoubleClockBHour:
                    return IsHourCorrect(doubleClockB, doublePracticeClockBHour);
                case ClockLearningTutorialStep.DoubleClockBMinute:
                    return IsHourCorrect(doubleClockB, doublePracticeClockBHour) && IsMinuteCorrect(doubleClockB, doublePracticeClockBMinute);
                default:
                    return false;
            }
        }

        private static bool IsHourCorrect(ClockLearningClockView clock, int targetHour)
        {
            return clock != null && clock.Hour1To12 == Mathf.Clamp(targetHour, 1, 12);
        }

        private static bool IsMinuteCorrect(ClockLearningClockView clock, int targetMinute)
        {
            if (clock == null) return false;
            int difference = Mathf.Abs(clock.Minute - Mathf.Clamp(targetMinute, 0, 59));
            difference = Mathf.Min(difference, 60 - difference);
            return difference <= 2;
        }

        private bool IsDoubleAmPmCorrect()
        {
            bool aCorrect = clockAPmToggle == null || clockAPmToggle.isOn == doublePracticeClockAIsPm;
            bool bCorrect = clockBPmToggle == null || clockBPmToggle.isOn == doublePracticeClockBIsPm;
            return aCorrect && bCorrect;
        }

        private void ApplyInputRules()
        {
            bool clickStep = IsClickAnywhereStep();
            bool handStep = IsHandDemoStep(_step);
            bool hintStep = IsHintButtonStep();
            bool submitStep = IsSubmitButtonStep();
            bool amPmStep = IsAmPmButtonStep();
            ClockLearningClockView requiredClock = GetRequiredClock(_step);
            ClockLearningHandType requiredHand = GetRequiredHand(_step);

            if (overlayGroup != null)
            {
                overlayGroup.interactable = clickStep;
                overlayGroup.blocksRaycasts = clickStep;
            }

            if (clickAnywhereButton != null)
            {
                clickAnywhereButton.gameObject.SetActive(clickStep);
                clickAnywhereButton.interactable = clickStep;
            }

            ConfigureClockInput(singleClock, handStep && requiredClock == singleClock, requiredHand);
            ConfigureClockInput(doubleClockA, handStep && requiredClock == doubleClockA, requiredHand);
            ConfigureClockInput(doubleClockB, handStep && requiredClock == doubleClockB, requiredHand);

            SetGameplayButtons(false);
            SetExpectedTutorialControlInteractable(hintStep, amPmStep, submitStep);
        }

        private static void ConfigureClockInput(ClockLearningClockView clock, bool canDrag, ClockLearningHandType requiredHand)
        {
            if (clock == null) return;
            clock.SetDraggable(canDrag);
            clock.SetAllowedHands(requiredHand == ClockLearningHandType.Hour, requiredHand == ClockLearningHandType.Minute);
        }

        private void SetExpectedTutorialControlInteractable(bool hintStep, bool amPmStep, bool submitStep)
        {
            if (hintButton != null) hintButton.interactable = hintStep;
            if (singleSubmitButton != null) singleSubmitButton.interactable = submitStep && _mode == ClockLearningMode.SingleClockSetTime;
            if (doubleSubmitButton != null) doubleSubmitButton.interactable = submitStep && _mode == ClockLearningMode.DoubleClockTimeDifference;
            if (clockBPmToggle != null)
            {
                if (clockAPmToggle != null) clockAPmToggle.interactable = false;
                clockBPmToggle.interactable = amPmStep;
            }
            else if (clockAPmToggle != null)
            {
                clockAPmToggle.interactable = amPmStep;
            }
        }

        private void SetGameplayButtons(bool enabled)
        {
            if (homeButton != null) homeButton.interactable = enabled;
            if (pauseButton != null) pauseButton.interactable = enabled;
            if (hintButton != null) hintButton.interactable = enabled;
            if (singleSubmitButton != null) singleSubmitButton.interactable = enabled;
            if (singleResetButton != null) singleResetButton.interactable = enabled;
            if (doubleSubmitButton != null) doubleSubmitButton.interactable = enabled;
            if (doubleResetButton != null) doubleResetButton.interactable = enabled;
            if (clockAPmToggle != null) clockAPmToggle.interactable = enabled;
            if (clockBPmToggle != null) clockBPmToggle.interactable = enabled;
        }

        private void SetAllClockInput(bool enabled)
        {
            if (singleClock != null) singleClock.SetDraggable(enabled);
            if (doubleClockA != null) doubleClockA.SetDraggable(enabled);
            if (doubleClockB != null) doubleClockB.SetDraggable(enabled);
        }

        private void RestoreControlsAfterTutorial()
        {
            if (singleClock != null) singleClock.SetAllowedHands(true, true);
            if (doubleClockA != null) doubleClockA.SetAllowedHands(true, true);
            if (doubleClockB != null) doubleClockB.SetAllowedHands(true, true);
            SetGameplayButtons(true);
        }

        private void PrepareDummyPracticeVisuals(ClockLearningMode mode)
        {
            if (!useDummyPracticeTutorial) return;

            if (mode == ClockLearningMode.SingleClockSetTime)
            {
                if (singlePromptTextTarget != null) singlePromptTextTarget.text = singlePracticePromptText;
                if (singleTargetTextTarget != null) singleTargetTextTarget.text = singlePracticeDisplayText;
                if (singleClock != null) singleClock.SetTime(singlePracticeStartHour, singlePracticeStartMinute, false);
                return;
            }

            if (doublePromptTextTarget != null) doublePromptTextTarget.text = doublePracticePromptText;
            if (doubleTargetTextTarget != null) doubleTargetTextTarget.text = doublePracticeDifferenceText;
            if (doubleChipTextTarget != null) doubleChipTextTarget.text = "Practice Target: " + doublePracticeDifferenceText;
            if (doubleClockA != null) doubleClockA.SetTime(1, 15, false);
            if (doubleClockB != null) doubleClockB.SetTime(9, 15, false);
            if (clockAPmToggle != null) clockAPmToggle.isOn = doublePracticeClockAIsPm;
            if (clockBPmToggle != null) clockBPmToggle.isOn = doublePracticeStartClockBAsPm;
        }

        private void CacheOriginalUiText()
        {
            if (_cachedUiText) return;
            _cachedSinglePrompt = singlePromptTextTarget != null ? singlePromptTextTarget.text : null;
            _cachedSingleTarget = singleTargetTextTarget != null ? singleTargetTextTarget.text : null;
            _cachedDoublePrompt = doublePromptTextTarget != null ? doublePromptTextTarget.text : null;
            _cachedDoubleTarget = doubleTargetTextTarget != null ? doubleTargetTextTarget.text : null;
            _cachedDoubleChip = doubleChipTextTarget != null ? doubleChipTextTarget.text : null;
            _cachedUiText = true;
        }

        private void RestoreOriginalUiText()
        {
            if (!_cachedUiText) return;
            if (singlePromptTextTarget != null && _cachedSinglePrompt != null) singlePromptTextTarget.text = _cachedSinglePrompt;
            if (singleTargetTextTarget != null && _cachedSingleTarget != null) singleTargetTextTarget.text = _cachedSingleTarget;
            if (doublePromptTextTarget != null && _cachedDoublePrompt != null) doublePromptTextTarget.text = _cachedDoublePrompt;
            if (doubleTargetTextTarget != null && _cachedDoubleTarget != null) doubleTargetTextTarget.text = _cachedDoubleTarget;
            if (doubleChipTextTarget != null && _cachedDoubleChip != null) doubleChipTextTarget.text = _cachedDoubleChip;
            _cachedUiText = false;
        }

        private void TryAutoFindDummyTextReferences()
        {
            if (singlePromptTextTarget == null) singlePromptTextTarget = FindTextByName("Single Prompt");
            if (singleTargetTextTarget == null) singleTargetTextTarget = FindTextByName("Single Target");
            if (doublePromptTextTarget == null) doublePromptTextTarget = FindTextByName("Difference Prompt");
            if (doubleTargetTextTarget == null) doubleTargetTextTarget = FindTextByName("Difference Target");
            if (doubleChipTextTarget == null) doubleChipTextTarget = FindTextByName("Difference Chip");
        }

        private static TextMeshProUGUI FindTextByName(string objectName)
        {
            TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            for (int i = 0; i < texts.Length; i++)
            {
                TextMeshProUGUI text = texts[i];
                if (text == null || !text.gameObject.scene.IsValid()) continue;
                if (text.name == objectName) return text;
            }
            return null;
        }

        private void ApplyPointerVisual()
        {
            if (pointer == null) return;
            TextMeshProUGUI legacyPointerText = pointer.GetComponent<TextMeshProUGUI>();
            if (legacyPointerText != null) legacyPointerText.text = string.Empty;
            if (pointerImage == null) pointerImage = pointer.GetComponent<Image>();
            if (pointerImage == null) pointerImage = pointer.gameObject.AddComponent<Image>();
            pointerImage.raycastTarget = false;
            pointerImage.sprite = pointerSprite;
            pointerImage.preserveAspect = true;
            pointerImage.color = pointerSprite != null ? Color.white : new Color(1f, 1f, 1f, 0.18f);
            if (_pointerCanvasGroup == null) _pointerCanvasGroup = pointer.GetComponent<CanvasGroup>();
            if (_pointerCanvasGroup == null) _pointerCanvasGroup = pointer.gameObject.AddComponent<CanvasGroup>();
            _pointerCanvasGroup.interactable = false;
            _pointerCanvasGroup.blocksRaycasts = false;
        }

        private void ApplyBackgroundVisuals()
        {
            if (backgroundImage != null)
            {
                Color color = backgroundImage.color;
                color.a = backgroundOpacity;
                backgroundImage.color = color;
                backgroundImage.raycastTarget = false;
            }

            if (promptCard != null)
            {
                Image image = promptCard.GetComponent<Image>();
                if (image != null)
                {
                    Color color = image.color;
                    color.a = promptBackgroundOpacity;
                    image.color = color;
                    image.raycastTarget = false;
                }
                DisableRaycastsForGraphicTree(promptCard);
            }
        }

        private void MovePromptToTarget(RectTransform target, ClockLearningTutorialStep step)
        {
            if (promptCard == null || !autoPositionPrompt) return;
            RectTransform overlayRect = overlayGroup != null ? overlayGroup.transform as RectTransform : promptCard.parent as RectTransform;
            if (overlayRect == null) return;

            promptCard.anchorMin = new Vector2(0.5f, 0.5f);
            promptCard.anchorMax = new Vector2(0.5f, 0.5f);
            promptCard.pivot = new Vector2(0.5f, 0.5f);
            promptCard.sizeDelta = promptCardSize;

            Vector2 desired;
            Rect overlay = overlayRect.rect;

            if (step == ClockLearningTutorialStep.SingleSubmit || step == ClockLearningTutorialStep.DoubleSubmit)
            {
                desired = readyPromptOffset;
            }
            else if (step == ClockLearningTutorialStep.DoubleAmPm)
            {
                Vector2 targetPos = target != null ? GetAnchoredPositionInOverlay(target, overlayRect) : Vector2.zero;
                Vector2 targetSize = target != null ? GetRectSizeInOverlay(target, overlayRect) : Vector2.zero;
                desired = targetPos + new Vector2(0f, Mathf.Max(amPmPromptOffset.y, targetSize.y * 0.5f + promptCardSize.y * 0.5f + 48f));
            }
            else if (IsHandDemoStep(step))
            {
                desired = new Vector2(0f, overlay.yMax - promptCardSize.y * 0.5f - 118f);
            }
            else
            {
                Vector2 targetPos = target != null ? GetAnchoredPositionInOverlay(target, overlayRect) : Vector2.zero;
                desired = targetPos + GetPromptOffset(step);
            }

            desired = ClampInsideOverlay(desired, overlayRect, promptCard, promptClampMargin);
            promptCard.DOKill();
            promptCard.DOAnchorPos(desired, 0.34f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private Vector2 GetPromptOffset(ClockLearningTutorialStep step)
        {
            if (step == ClockLearningTutorialStep.SingleIntro || step == ClockLearningTutorialStep.DoubleIntro) return questionPromptOffset;
            if (step == ClockLearningTutorialStep.SingleHint || step == ClockLearningTutorialStep.DoubleHint) return hintPromptOffset;
            if (step == ClockLearningTutorialStep.DoubleAmPm) return amPmPromptOffset;
            if (step == ClockLearningTutorialStep.SingleSubmit || step == ClockLearningTutorialStep.DoubleSubmit) return readyPromptOffset;
            return clockPromptOffset;
        }

        private void MovePointerToTarget(RectTransform target)
        {
            if (pointer == null) return;
            SetPointerVisible(true);
            _pointerSequence?.Kill();
            pointer.DOKill();

            RectTransform overlayRect = overlayGroup != null ? overlayGroup.transform as RectTransform : pointer.parent as RectTransform;
            Vector2 targetPos = Vector2.zero;
            if (target != null && overlayRect != null) targetPos = GetAnchoredPositionInOverlay(target, overlayRect);

            Vector2 offset = (_step == ClockLearningTutorialStep.SingleIntro || _step == ClockLearningTutorialStep.DoubleIntro)
                ? questionPointerOffset
                : normalPointerOffset;

            if (_step == ClockLearningTutorialStep.SingleHint || _step == ClockLearningTutorialStep.DoubleHint) offset = new Vector2(46f, -54f);
            else if (_step == ClockLearningTutorialStep.DoubleAmPm) offset = new Vector2(62f, -26f);
            else if (_step == ClockLearningTutorialStep.SingleSubmit || _step == ClockLearningTutorialStep.DoubleSubmit) offset = new Vector2(82f, -52f);

            targetPos += offset;
            if (overlayRect != null) targetPos = ClampInsideOverlay(targetPos, overlayRect, pointer, Vector2.zero);
            pointer.DOAnchorPos(targetPos, pointerMoveDuration).SetEase(Ease.InOutCubic).SetUpdate(true)
                .OnComplete(() => StartPointerHover(targetPos));
        }

        private void StartPointerHover(Vector2 targetPos)
        {
            if (pointer == null) return;
            _pointerSequence?.Kill();
            _pointerSequence = DOTween.Sequence().SetUpdate(true)
                .Append(pointer.DOAnchorPosY(targetPos.y + pointerHoverPixels, 0.55f).SetEase(Ease.InOutSine))
                .Append(pointer.DOAnchorPosY(targetPos.y, 0.55f).SetEase(Ease.InOutSine))
                .SetLoops(-1);
        }

        private void StartFakeHandDemo(ClockLearningTutorialStep step)
        {
            StopFakeHandDemo(true);
            if (_userDraggingCurrentStep) return;
            SetPointerVisible(true);
            _pointerSequence?.Kill();
            if (pointer != null) pointer.DOKill();
            if (ghostHand == null || pointer == null) return;

            ClockLearningClockView tutorialClock = GetRequiredClock(step);
            ClockLearningHandType handType = GetRequiredHand(step);
            RectTransform actualHand = tutorialClock != null ? tutorialClock.GetHandRect(handType) : null;
            RectTransform faceTarget = tutorialClock != null && tutorialClock.ClockFaceRect != null ? tutorialClock.ClockFaceRect : GetTarget(step);
            RectTransform overlayRect = overlayGroup != null ? overlayGroup.transform as RectTransform : ghostHand.parent as RectTransform;
            if (overlayRect == null || (faceTarget == null && actualHand == null)) return;

            Vector2 handBase = actualHand != null ? GetWorldPositionInOverlay(actualHand.position, overlayRect) : GetAnchoredPositionInOverlay(faceTarget, overlayRect);
            float radius = faceTarget != null ? Mathf.Max(80f, Mathf.Min(faceTarget.rect.width, faceTarget.rect.height) * 0.5f) : 120f;
            float fallbackLength = handType == ClockLearningHandType.Minute ? radius * 0.70f : radius * 0.47f;
            float fallbackWidth = handType == ClockLearningHandType.Minute ? ghostHandFallbackSize.x : ghostHandFallbackSize.x * 1.25f;
            Vector2 actualSize = actualHand != null ? GetRectSizeInOverlay(actualHand, overlayRect) : Vector2.zero;
            float length = actualSize.y > 1f ? actualSize.y : fallbackLength;
            float width = actualSize.x > 1f ? actualSize.x : fallbackWidth;
            Vector2 ghostPivot = actualHand != null ? actualHand.pivot : new Vector2(0.5f, 0f);

            ghostHand.gameObject.SetActive(true);
            ghostHand.anchorMin = new Vector2(0.5f, 0.5f);
            ghostHand.anchorMax = new Vector2(0.5f, 0.5f);
            ghostHand.pivot = ghostPivot;
            ghostHand.anchoredPosition = handBase;
            ghostHand.sizeDelta = new Vector2(Mathf.Max(3f, width), Mathf.Max(40f, length));
            CopyGhostImage(actualHand);

            float startAngle = tutorialClock != null ? tutorialClock.GetRenderedHandClockwiseAngle(handType) : 0f;
            float endAngle = GetTargetAngleForStep(step);
            float tipLength = Mathf.Max(20f, length * Mathf.Clamp01(1f - ghostPivot.y) * Mathf.Clamp(pointerHandTipFollow, 0.55f, 1.05f));

            ghostHand.localRotation = Quaternion.Euler(0f, 0f, -startAngle);
            pointer.localScale = Vector3.one;
            UpdatePointerAtGhostTip(handBase, tipLength);

            _ghostSequence = DOTween.Sequence().SetUpdate(true);
            _ghostSequence.Append(pointer.DOScale(0.86f, 0.16f).SetEase(Ease.OutCubic));
            _ghostSequence.Append(pointer.DOScale(1f, 0.16f).SetEase(Ease.OutCubic));
            _ghostSequence.AppendInterval(0.18f);
            _ghostSequence.Append(ghostHand.DOLocalRotate(new Vector3(0f, 0f, -endAngle), fakeHandMoveDuration, RotateMode.FastBeyond360).SetEase(Ease.InOutCubic));
            _ghostSequence.OnUpdate(() => UpdatePointerAtGhostTip(handBase, tipLength));
            _ghostSequence.AppendInterval(0.55f);
            _ghostSequence.SetLoops(-1, LoopType.Restart);
        }

        private void CopyGhostImage(RectTransform actualHand)
        {
            if (ghostHandImage == null) return;
            Image actualHandImage = actualHand != null ? actualHand.GetComponent<Image>() : null;
            ghostHandImage.raycastTarget = false;

            bool copiedActualSprite = false;
            if (copyActualHandSpriteForGhost && actualHandImage != null)
            {
                ghostHandImage.sprite = actualHandImage.sprite;
                ghostHandImage.type = actualHandImage.type;
                ghostHandImage.preserveAspect = actualHandImage.preserveAspect;
                ghostHandImage.pixelsPerUnitMultiplier = actualHandImage.pixelsPerUnitMultiplier;
                ghostHandImage.fillCenter = actualHandImage.fillCenter;
                ghostHandImage.material = actualHandImage.material;
                copiedActualSprite = true;
            }

            if (!copiedActualSprite)
            {
                ghostHandImage.sprite = ghostHandSprite != null ? ghostHandSprite : (actualHandImage != null ? actualHandImage.sprite : null);
                ghostHandImage.type = Image.Type.Simple;
                ghostHandImage.preserveAspect = true;
            }

            ghostHandImage.color = ghostHandColor;
        }

        private float GetTargetAngleForStep(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleHourHand: return GetHourAngle(singlePracticeHour, singlePracticeMinute);
                case ClockLearningTutorialStep.SingleMinuteHand: return singlePracticeMinute * 6f;
                case ClockLearningTutorialStep.DoubleClockAHour: return GetHourAngle(doublePracticeClockAHour, doublePracticeClockAMinute);
                case ClockLearningTutorialStep.DoubleClockAMinute: return doublePracticeClockAMinute * 6f;
                case ClockLearningTutorialStep.DoubleClockBHour: return GetHourAngle(doublePracticeClockBHour, doublePracticeClockBMinute);
                case ClockLearningTutorialStep.DoubleClockBMinute: return doublePracticeClockBMinute * 6f;
                default: return 0f;
            }
        }

        private static float GetHourAngle(int hour1To12, int minute)
        {
            int hour0 = Mathf.Clamp(hour1To12, 1, 12) == 12 ? 0 : Mathf.Clamp(hour1To12, 1, 12);
            return ((hour0 * 60f) + Mathf.Clamp(minute, 0, 59)) * 0.5f;
        }

        private void SetPointerVisible(bool visible, bool instant = false)
        {
            if (pointer == null) return;
            if (_pointerCanvasGroup == null) _pointerCanvasGroup = pointer.GetComponent<CanvasGroup>();
            if (_pointerCanvasGroup == null) _pointerCanvasGroup = pointer.gameObject.AddComponent<CanvasGroup>();
            pointer.gameObject.SetActive(true);
            _pointerCanvasGroup.DOKill();
            _pointerCanvasGroup.interactable = false;
            _pointerCanvasGroup.blocksRaycasts = false;
            if (instant) _pointerCanvasGroup.alpha = visible ? 1f : 0f;
            else _pointerCanvasGroup.DOFade(visible ? 1f : 0f, visible ? 0.14f : 0.08f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void UpdatePointerAtGhostTip(Vector2 handBase, float tipLength)
        {
            if (pointer == null || ghostHand == null) return;
            float clockwiseAngle = NormalizeClockwiseAngle(-ghostHand.localEulerAngles.z);
            pointer.anchoredPosition = handBase + ClockPoint(clockwiseAngle, tipLength) + pointerHandTipOffset;
        }

        private void StopFakeHandDemo(bool hide)
        {
            _ghostSequence?.Kill();
            if (ghostHand != null)
            {
                ghostHand.DOKill();
                if (hide) ghostHand.gameObject.SetActive(false);
            }
        }

        private void CompleteTutorial()
        {
            if (!_running) return;

            MarkSeen(_mode);
            _running = false;
            _step = ClockLearningTutorialStep.None;
            KillTweens();
            RestoreOriginalUiText();
            if (ghostHand != null) ghostHand.gameObject.SetActive(false);
            SetPointerVisible(false);
            if (clickAnywhereButton != null) clickAnywhereButton.gameObject.SetActive(false);
            if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(false);
            RestoreControlsAfterTutorial();
            ShowOverlay(false, false);

            Action complete = _onComplete;
            _onComplete = null;
            complete?.Invoke();
        }

        private bool HasSeen(ClockLearningMode mode)
        {
            bool seenSession = mode == ClockLearningMode.SingleClockSetTime ? _singleSeenThisSession : _doubleSeenThisSession;
            if (seenSession) return true;
            if (showOnlyOncePerMode || rememberSeenInPlayerPrefs) return PlayerPrefs.GetInt(GetPrefsKey(mode), 0) == 1;
            return false;
        }

        private void MarkSeen(ClockLearningMode mode)
        {
            if (mode == ClockLearningMode.SingleClockSetTime) _singleSeenThisSession = true;
            else _doubleSeenThisSession = true;
            if (showOnlyOncePerMode || rememberSeenInPlayerPrefs)
            {
                PlayerPrefs.SetInt(GetPrefsKey(mode), 1);
                PlayerPrefs.Save();
            }
        }

        private string GetPrefsKey(ClockLearningMode mode)
        {
            return $"{seenPlayerPrefsKey}_{mode}";
        }

        private void ShowOverlay(bool visible, bool instant)
        {
            if (overlayGroup == null) return;
            overlayGroup.gameObject.SetActive(true);
            overlayGroup.DOKill();
            overlayGroup.transform.DOKill();

            if (instant)
            {
                overlayGroup.alpha = visible ? 1f : 0f;
                overlayGroup.interactable = visible;
                overlayGroup.blocksRaycasts = visible;
                if (!visible) overlayGroup.gameObject.SetActive(false);
                return;
            }

            overlayGroup.DOFade(visible ? 1f : 0f, 0.2f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    overlayGroup.interactable = visible;
                    overlayGroup.blocksRaycasts = visible;
                    if (!visible) overlayGroup.gameObject.SetActive(false);
                });
        }

        private void PulsePromptCard()
        {
            if (promptCard == null) return;
            promptCard.DOKill();
            promptCard.localScale = Vector3.one * 0.98f;
            promptCard.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private static void DisableRaycastsForGraphicTree(RectTransform root)
        {
            if (root == null) return;
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;
        }

        private static Vector2 GetAnchoredPositionInOverlay(RectTransform target, RectTransform overlayRect)
        {
            return target == null ? Vector2.zero : GetWorldPositionInOverlay(target.position, overlayRect);
        }

        private static Vector2 GetWorldPositionInOverlay(Vector3 worldPosition, RectTransform overlayRect)
        {
            if (overlayRect == null) return Vector2.zero;
            Camera camera = null;
            Canvas canvas = overlayRect.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) camera = canvas.worldCamera;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, screenPoint, camera, out Vector2 anchoredPosition);
            return anchoredPosition;
        }

        private static Vector2 GetRectSizeInOverlay(RectTransform rect, RectTransform overlayRect)
        {
            if (rect == null || overlayRect == null) return Vector2.zero;
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 bottomMid = GetWorldPositionInOverlay((corners[0] + corners[3]) * 0.5f, overlayRect);
            Vector2 topMid = GetWorldPositionInOverlay((corners[1] + corners[2]) * 0.5f, overlayRect);
            Vector2 leftMid = GetWorldPositionInOverlay((corners[0] + corners[1]) * 0.5f, overlayRect);
            Vector2 rightMid = GetWorldPositionInOverlay((corners[2] + corners[3]) * 0.5f, overlayRect);
            return new Vector2(Vector2.Distance(leftMid, rightMid), Vector2.Distance(bottomMid, topMid));
        }

        private static Vector2 ClampInsideOverlay(Vector2 position, RectTransform overlayRect, RectTransform itemRect, Vector2 margin)
        {
            if (overlayRect == null || itemRect == null) return position;
            Rect overlay = overlayRect.rect;
            Vector2 itemSize = itemRect.rect.size;
            if (itemSize.x <= 0f || itemSize.y <= 0f) itemSize = itemRect.sizeDelta;
            float halfWidth = Mathf.Max(0f, itemSize.x * 0.5f);
            float halfHeight = Mathf.Max(0f, itemSize.y * 0.5f);
            float minX = overlay.xMin + halfWidth + margin.x;
            float maxX = overlay.xMax - halfWidth - margin.x;
            float minY = overlay.yMin + halfHeight + margin.y;
            float maxY = overlay.yMax - halfHeight - margin.y;
            if (minX <= maxX) position.x = Mathf.Clamp(position.x, minX, maxX);
            if (minY <= maxY) position.y = Mathf.Clamp(position.y, minY, maxY);
            return position;
        }

        private static float NormalizeClockwiseAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f) angle += 360f;
            return angle;
        }

        private static Vector2 ClockPoint(float clockwiseAngle, float radius)
        {
            float radians = clockwiseAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;
        }
    }
}
