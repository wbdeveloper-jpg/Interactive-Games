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
        SingleQuestionFocus,
        SingleHourHandDemo,
        SingleMinuteHandDemo,
        SingleReady,
        DoubleQuestionFocus,
        DoubleClockAHourHandDemo,
        DoubleClockAMinuteHandDemo,
        DoubleClockBHourHandDemo,
        DoubleClockBMinuteHandDemo,
        DoubleReady
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

        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [Tooltip("Optional full-screen background. Default opacity is 0, so you can enable it later without rebuilding UI.")]
        [SerializeField] private Image backgroundImage;
        [SerializeField, Range(0f, 1f)] private float backgroundOpacity = 0f;
        [Tooltip("Invisible full-screen button used only for 'click anywhere' tutorial steps.")]
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

        [Header("Targets")]
        [SerializeField] private RectTransform singleQuestionTarget;
        [SerializeField] private RectTransform doubleQuestionTarget;
        [SerializeField] private RectTransform singleClockTarget;
        [SerializeField] private RectTransform doubleClockATarget;
        [SerializeField] private RectTransform doubleClockBTarget;

        [Header("Pointer Position")]
        [Tooltip("Used for question/time target. Negative Y keeps the pointer below the number card instead of covering text.")]
        [SerializeField] private Vector2 questionPointerOffset = new Vector2(70f, -95f);
        [SerializeField] private Vector2 normalPointerOffset = new Vector2(70f, -55f);
        [Tooltip("1 means pointer follows the exact fake-hand tip. Reduce slightly only if your pointer sprite visually over-shoots.")]
        [SerializeField, Range(0.55f, 1.05f)] private float pointerHandTipFollow = 1f;
        [Tooltip("Keep this 0,0 first. Use only for final custom finger sprite alignment.")]
        [SerializeField] private Vector2 pointerHandTipOffset = Vector2.zero;

        [Header("Prompt Position")]
        [SerializeField] private bool autoPositionPrompt = true;
        [SerializeField] private Vector2 promptCardSize = new Vector2(920f, 92f);
        [SerializeField] private Vector2 questionPromptOffset = new Vector2(0f, -130f);
        [SerializeField] private Vector2 clockPromptOffset = new Vector2(0f, -230f);
        [SerializeField] private Vector2 readyPromptOffset = new Vector2(0f, -75f);
        [SerializeField] private Vector2 promptClampMargin = new Vector2(60f, 45f);

        [Header("Motion Polish")]
        [SerializeField, Range(0.15f, 1.2f)] private float pointerMoveDuration = 0.48f;
        [SerializeField, Range(4f, 40f)] private float pointerHoverPixels = 10f;
        [SerializeField, Range(0.6f, 2.5f)] private float fakeHandMoveDuration = 1.1f;
        [Tooltip("Delay after a player action before the next tutorial step. Prevents robotic instant changes.")]
        [SerializeField, Range(0f, 1.25f)] private float stepTransitionDelay = 0.6f;
        [SerializeField, Range(10f, 120f)] private float fakeHourMoveDegrees = 35f;
        [SerializeField, Range(20f, 180f)] private float fakeMinuteMoveDegrees = 85f;

        [Header("Text")]
        [SerializeField] private string singleQuestionFocusPrompt = "Read the time. Click anywhere to continue.";
        [SerializeField] private string singleHourHandPrompt = "Watch the short hour hand. Then drag it yourself.";
        [SerializeField] private string singleMinuteHandPrompt = "Good! Now drag the long minute hand.";
        [SerializeField] private string singleReadyPrompt = "You are ready! Click anywhere to start.";
        [SerializeField] private string gameStartingPrompt = "The game is starting!";
        [SerializeField, Range(0.25f, 1.5f)] private float gameStartingMessageDelay = 0.75f;
        [SerializeField] private string doubleQuestionFocusPrompt = "Read the time difference. Click anywhere to continue.";
        [SerializeField] private string doubleClockAHourPrompt = "Clock A: drag the short hour hand.";
        [SerializeField] private string doubleClockAMinutePrompt = "Clock A: now drag the long minute hand.";
        [SerializeField] private string doubleClockBHourPrompt = "Clock B: drag the short hour hand.";
        [SerializeField] private string doubleClockBMinutePrompt = "Clock B: now drag the long minute hand.";
        [SerializeField] private string doubleReadyPrompt = "You are ready! Click anywhere to start.";

        private ClockLearningMode _mode;
        private ClockLearningTutorialStep _step = ClockLearningTutorialStep.None;
        private bool _running;
        private bool _singleSeenThisSession;
        private bool _doubleSeenThisSession;
        private Action _onComplete;
        private Sequence _pointerSequence;
        private Sequence _ghostSequence;
        private Coroutine _stepDelayRoutine;
        private bool _waitingForDelay;
        private bool _userDraggingCurrentStep;
        private bool _subscribedToClockEvents;

        public bool IsRunning => _running;

        private void Awake()
        {
            EnsureSafeValues();
            EnsureRuntimeObjects();
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
        }

        private void OnDisable()
        {
            UnsubscribeClockEvents();
            if (clickAnywhereButton != null) clickAnywhereButton.onClick.RemoveListener(AdvanceFromClickAnywhere);
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
            fakeHourMoveDegrees = Mathf.Clamp(fakeHourMoveDegrees, 10f, 120f);
            fakeMinuteMoveDegrees = Mathf.Clamp(fakeMinuteMoveDegrees, 20f, 180f);
            backgroundOpacity = Mathf.Clamp01(backgroundOpacity);
            promptBackgroundOpacity = Mathf.Clamp01(promptBackgroundOpacity);
            ghostHandFallbackSize.x = Mathf.Max(2f, ghostHandFallbackSize.x);
            ghostHandFallbackSize.y = Mathf.Max(40f, ghostHandFallbackSize.y);
            if (string.IsNullOrWhiteSpace(seenPlayerPrefsKey)) seenPlayerPrefsKey = "ClockLearningGame_TutorialSeen";
            promptCardSize.x = Mathf.Max(220f, promptCardSize.x);
            promptCardSize.y = Mathf.Max(50f, promptCardSize.y);
            promptClampMargin.x = Mathf.Max(0f, promptClampMargin.x);
            promptClampMargin.y = Mathf.Max(0f, promptClampMargin.y);
            gameStartingMessageDelay = Mathf.Clamp(gameStartingMessageDelay, 0.25f, 1.5f);
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
            SubscribeClockEvents();
            KillTweens();
            CancelStepDelay();

            _mode = mode;
            _onComplete = onComplete;
            _running = true;
            _waitingForDelay = false;
            _userDraggingCurrentStep = false;
            _step = mode == ClockLearningMode.SingleClockSetTime
                ? ClockLearningTutorialStep.SingleQuestionFocus
                : ClockLearningTutorialStep.DoubleQuestionFocus;

            ShowOverlay(true, false);
            RefreshStep();
        }

        public void HideInstant()
        {
            CancelStepDelay();
            _running = false;
            _waitingForDelay = false;
            _userDraggingCurrentStep = false;
            _step = ClockLearningTutorialStep.None;
            KillTweens();
            if (ghostHand != null) ghostHand.gameObject.SetActive(false);
            SetPointerVisible(false);
            if (clickAnywhereButton != null) clickAnywhereButton.gameObject.SetActive(false);
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
            if (singleClock != null) singleClock.UserStartedDrag += HandleClockStartedDrag;
            if (singleClock != null) singleClock.UserChangedTimeByDrag += HandleClockChangedByDrag;
            if (singleClock != null) singleClock.UserFinishedDrag += HandleClockFinishedDrag;
            if (doubleClockA != null) doubleClockA.UserStartedDrag += HandleClockStartedDrag;
            if (doubleClockA != null) doubleClockA.UserChangedTimeByDrag += HandleClockChangedByDrag;
            if (doubleClockA != null) doubleClockA.UserFinishedDrag += HandleClockFinishedDrag;
            if (doubleClockB != null) doubleClockB.UserStartedDrag += HandleClockStartedDrag;
            if (doubleClockB != null) doubleClockB.UserChangedTimeByDrag += HandleClockChangedByDrag;
            if (doubleClockB != null) doubleClockB.UserFinishedDrag += HandleClockFinishedDrag;
            _subscribedToClockEvents = true;
        }

        private void UnsubscribeClockEvents()
        {
            if (!_subscribedToClockEvents) return;
            if (singleClock != null) singleClock.UserStartedDrag -= HandleClockStartedDrag;
            if (singleClock != null) singleClock.UserChangedTimeByDrag -= HandleClockChangedByDrag;
            if (singleClock != null) singleClock.UserFinishedDrag -= HandleClockFinishedDrag;
            if (doubleClockA != null) doubleClockA.UserStartedDrag -= HandleClockStartedDrag;
            if (doubleClockA != null) doubleClockA.UserChangedTimeByDrag -= HandleClockChangedByDrag;
            if (doubleClockA != null) doubleClockA.UserFinishedDrag -= HandleClockFinishedDrag;
            if (doubleClockB != null) doubleClockB.UserStartedDrag -= HandleClockStartedDrag;
            if (doubleClockB != null) doubleClockB.UserChangedTimeByDrag -= HandleClockChangedByDrag;
            if (doubleClockB != null) doubleClockB.UserFinishedDrag -= HandleClockFinishedDrag;
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

            if (pointer != null)
            {
                pointer.SetAsLastSibling();
            }

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

        private void AdvanceFromClickAnywhere()
        {
            if (!_running || _waitingForDelay || !IsClickAnywhereStep()) return;

            switch (_step)
            {
                case ClockLearningTutorialStep.SingleQuestionFocus:
                    ScheduleMoveToStep(ClockLearningTutorialStep.SingleHourHandDemo);
                    break;
                case ClockLearningTutorialStep.SingleReady:
                    ScheduleCompleteTutorial();
                    break;
                case ClockLearningTutorialStep.DoubleQuestionFocus:
                    ScheduleMoveToStep(ClockLearningTutorialStep.DoubleClockAHourHandDemo);
                    break;
                case ClockLearningTutorialStep.DoubleReady:
                    ScheduleCompleteTutorial();
                    break;
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

            switch (_step)
            {
                case ClockLearningTutorialStep.SingleHourHandDemo:
                    if (sourceClock == singleClock && handType == ClockLearningHandType.Hour)
                        ScheduleMoveToStep(ClockLearningTutorialStep.SingleMinuteHandDemo, true);
                    break;
                case ClockLearningTutorialStep.SingleMinuteHandDemo:
                    if (sourceClock == singleClock && handType == ClockLearningHandType.Minute)
                        ScheduleMoveToStep(ClockLearningTutorialStep.SingleReady, true);
                    break;
                case ClockLearningTutorialStep.DoubleClockAHourHandDemo:
                    if (sourceClock == doubleClockA && handType == ClockLearningHandType.Hour)
                        ScheduleMoveToStep(ClockLearningTutorialStep.DoubleClockAMinuteHandDemo, true);
                    break;
                case ClockLearningTutorialStep.DoubleClockAMinuteHandDemo:
                    if (sourceClock == doubleClockA && handType == ClockLearningHandType.Minute)
                        ScheduleMoveToStep(ClockLearningTutorialStep.DoubleClockBHourHandDemo, true);
                    break;
                case ClockLearningTutorialStep.DoubleClockBHourHandDemo:
                    if (sourceClock == doubleClockB && handType == ClockLearningHandType.Hour)
                        ScheduleMoveToStep(ClockLearningTutorialStep.DoubleClockBMinuteHandDemo, true);
                    break;
                case ClockLearningTutorialStep.DoubleClockBMinuteHandDemo:
                    if (sourceClock == doubleClockB && handType == ClockLearningHandType.Minute)
                        ScheduleMoveToStep(ClockLearningTutorialStep.DoubleReady, true);
                    break;
            }
        }

        private bool IsCurrentRequiredHand(ClockLearningClockView sourceClock, ClockLearningHandType handType)
        {
            if (!IsHandDemoStep(_step)) return false;
            return GetRequiredClock(_step) == sourceClock && GetRequiredHand(_step) == handType;
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

        private void ScheduleMoveToStep(ClockLearningTutorialStep nextStep, bool lockClockDuringDelay = false)
        {
            if (!_running) return;
            CancelStepDelay();
            _stepDelayRoutine = StartCoroutine(StepDelayRoutine(nextStep, false, lockClockDuringDelay));
        }

        private void ScheduleCompleteTutorial()
        {
            if (!_running) return;
            CancelStepDelay();
            _stepDelayRoutine = StartCoroutine(StepDelayRoutine(ClockLearningTutorialStep.None, true, true));
        }

        private IEnumerator StepDelayRoutine(ClockLearningTutorialStep nextStep, bool completeTutorial, bool lockClockDuringDelay)
        {
            _waitingForDelay = true;
            _userDraggingCurrentStep = false;
            if (lockClockDuringDelay) SetAllClockInput(false);
            StopFakeHandDemo(true);
            SetPointerVisible(false);

            if (completeTutorial)
            {
                if (promptText != null)
                {
                    promptText.text = string.IsNullOrWhiteSpace(gameStartingPrompt) ? "The game is starting!" : gameStartingPrompt;
                }

                if (promptCard != null)
                {
                    promptCard.DOKill();
                    promptCard.localScale = Vector3.one * 0.96f;
                    promptCard.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
                }

                yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, gameStartingMessageDelay));
            }
            else if (stepTransitionDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(stepTransitionDelay);
            }

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
            SetPointerVisible(true);
            ApplyInputRules();
            ApplyPointerVisual();
            ApplyBackgroundVisuals();

            if (promptText != null) promptText.text = GetPrompt(_step);
            RectTransform target = GetTarget(_step);
            MovePromptToTarget(target, _step);

            if (IsHandDemoStep(_step))
            {
                StartFakeHandDemo(_step);
            }
            else
            {
                StopFakeHandDemo(true);
                MovePointerToTarget(target);
            }
        }

        private string GetPrompt(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleQuestionFocus:
                    return string.IsNullOrWhiteSpace(singleQuestionFocusPrompt) ? "Read the time. Click anywhere to continue." : singleQuestionFocusPrompt;
                case ClockLearningTutorialStep.SingleHourHandDemo:
                    return string.IsNullOrWhiteSpace(singleHourHandPrompt) ? "Watch the short hour hand. Now try dragging it." : singleHourHandPrompt;
                case ClockLearningTutorialStep.SingleMinuteHandDemo:
                    return string.IsNullOrWhiteSpace(singleMinuteHandPrompt) ? "Good! Now try dragging the long minute hand." : singleMinuteHandPrompt;
                case ClockLearningTutorialStep.SingleReady:
                    return string.IsNullOrWhiteSpace(singleReadyPrompt) ? "You are ready! Click anywhere to start." : singleReadyPrompt;
                case ClockLearningTutorialStep.DoubleQuestionFocus:
                    return string.IsNullOrWhiteSpace(doubleQuestionFocusPrompt) ? "Read the time difference. Click anywhere to continue." : doubleQuestionFocusPrompt;
                case ClockLearningTutorialStep.DoubleClockAHourHandDemo:
                    return string.IsNullOrWhiteSpace(doubleClockAHourPrompt) ? "Clock A: try moving the short hour hand." : doubleClockAHourPrompt;
                case ClockLearningTutorialStep.DoubleClockAMinuteHandDemo:
                    return string.IsNullOrWhiteSpace(doubleClockAMinutePrompt) ? "Clock A: now move the long minute hand." : doubleClockAMinutePrompt;
                case ClockLearningTutorialStep.DoubleClockBHourHandDemo:
                    return string.IsNullOrWhiteSpace(doubleClockBHourPrompt) ? "Clock B: try moving the short hour hand." : doubleClockBHourPrompt;
                case ClockLearningTutorialStep.DoubleClockBMinuteHandDemo:
                    return string.IsNullOrWhiteSpace(doubleClockBMinutePrompt) ? "Clock B: now move the long minute hand." : doubleClockBMinutePrompt;
                case ClockLearningTutorialStep.DoubleReady:
                    return string.IsNullOrWhiteSpace(doubleReadyPrompt) ? "You are ready! Click anywhere to start." : doubleReadyPrompt;
                default:
                    return string.Empty;
            }
        }

        private RectTransform GetTarget(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleQuestionFocus:
                case ClockLearningTutorialStep.SingleReady:
                    return singleQuestionTarget;
                case ClockLearningTutorialStep.SingleHourHandDemo:
                case ClockLearningTutorialStep.SingleMinuteHandDemo:
                    return singleClockTarget != null ? singleClockTarget : GetRect(singleClock);
                case ClockLearningTutorialStep.DoubleQuestionFocus:
                case ClockLearningTutorialStep.DoubleReady:
                    return doubleQuestionTarget;
                case ClockLearningTutorialStep.DoubleClockAHourHandDemo:
                case ClockLearningTutorialStep.DoubleClockAMinuteHandDemo:
                    return doubleClockATarget != null ? doubleClockATarget : GetRect(doubleClockA);
                case ClockLearningTutorialStep.DoubleClockBHourHandDemo:
                case ClockLearningTutorialStep.DoubleClockBMinuteHandDemo:
                    return doubleClockBTarget != null ? doubleClockBTarget : GetRect(doubleClockB);
                default:
                    return null;
            }
        }

        private static RectTransform GetRect(Component component)
        {
            return component == null ? null : component.transform as RectTransform;
        }

        private bool IsClickAnywhereStep()
        {
            return _step == ClockLearningTutorialStep.SingleQuestionFocus
                || _step == ClockLearningTutorialStep.SingleReady
                || _step == ClockLearningTutorialStep.DoubleQuestionFocus
                || _step == ClockLearningTutorialStep.DoubleReady;
        }

        private static bool IsHandDemoStep(ClockLearningTutorialStep step)
        {
            return step == ClockLearningTutorialStep.SingleHourHandDemo
                || step == ClockLearningTutorialStep.SingleMinuteHandDemo
                || step == ClockLearningTutorialStep.DoubleClockAHourHandDemo
                || step == ClockLearningTutorialStep.DoubleClockAMinuteHandDemo
                || step == ClockLearningTutorialStep.DoubleClockBHourHandDemo
                || step == ClockLearningTutorialStep.DoubleClockBMinuteHandDemo;
        }

        private static ClockLearningHandType GetRequiredHand(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleHourHandDemo:
                case ClockLearningTutorialStep.DoubleClockAHourHandDemo:
                case ClockLearningTutorialStep.DoubleClockBHourHandDemo:
                    return ClockLearningHandType.Hour;
                case ClockLearningTutorialStep.SingleMinuteHandDemo:
                case ClockLearningTutorialStep.DoubleClockAMinuteHandDemo:
                case ClockLearningTutorialStep.DoubleClockBMinuteHandDemo:
                    return ClockLearningHandType.Minute;
                default:
                    return ClockLearningHandType.None;
            }
        }

        private ClockLearningClockView GetRequiredClock(ClockLearningTutorialStep step)
        {
            switch (step)
            {
                case ClockLearningTutorialStep.SingleHourHandDemo:
                case ClockLearningTutorialStep.SingleMinuteHandDemo:
                    return singleClock;
                case ClockLearningTutorialStep.DoubleClockAHourHandDemo:
                case ClockLearningTutorialStep.DoubleClockAMinuteHandDemo:
                    return doubleClockA;
                case ClockLearningTutorialStep.DoubleClockBHourHandDemo:
                case ClockLearningTutorialStep.DoubleClockBMinuteHandDemo:
                    return doubleClockB;
                default:
                    return null;
            }
        }

        private void ApplyInputRules()
        {
            bool clickStep = IsClickAnywhereStep();
            bool handStep = IsHandDemoStep(_step);
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
        }

        private static void ConfigureClockInput(ClockLearningClockView clock, bool canDrag, ClockLearningHandType requiredHand)
        {
            if (clock == null) return;
            clock.SetDraggable(canDrag);
            clock.SetAllowedHands(requiredHand == ClockLearningHandType.Hour, requiredHand == ClockLearningHandType.Minute);
        }

        private void SetAllClockInput(bool enabled)
        {
            if (singleClock != null) singleClock.SetDraggable(enabled);
            if (doubleClockA != null) doubleClockA.SetDraggable(enabled);
            if (doubleClockB != null) doubleClockB.SetDraggable(enabled);
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

        private void RestoreControlsAfterTutorial()
        {
            if (singleClock != null) singleClock.SetAllowedHands(true, true);
            if (doubleClockA != null) doubleClockA.SetAllowedHands(true, true);
            if (doubleClockB != null) doubleClockB.SetAllowedHands(true, true);
            SetGameplayButtons(true);
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

            Vector2 targetPos = target != null ? GetAnchoredPositionInOverlay(target, overlayRect) : Vector2.zero;
            Vector2 desired = targetPos + GetPromptOffset(step);
            desired = ClampInsideOverlay(desired, overlayRect, promptCard, promptClampMargin);

            promptCard.DOKill();
            promptCard.DOAnchorPos(desired, 0.28f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private Vector2 GetPromptOffset(ClockLearningTutorialStep step)
        {
            if (step == ClockLearningTutorialStep.SingleQuestionFocus || step == ClockLearningTutorialStep.DoubleQuestionFocus)
                return questionPromptOffset;
            if (step == ClockLearningTutorialStep.SingleReady || step == ClockLearningTutorialStep.DoubleReady)
                return readyPromptOffset;
            return clockPromptOffset;
        }

        private static void DisableRaycastsForGraphicTree(RectTransform root)
        {
            if (root == null) return;
            Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
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

            float width = Vector2.Distance(leftMid, rightMid);
            float height = Vector2.Distance(bottomMid, topMid);
            return new Vector2(width, height);
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

        private void MovePointerToTarget(RectTransform target)
        {
            if (pointer == null) return;
            SetPointerVisible(true);

            _pointerSequence?.Kill();
            pointer.DOKill();

            RectTransform overlayRect = overlayGroup != null ? overlayGroup.transform as RectTransform : pointer.parent as RectTransform;
            Vector2 targetPos = Vector2.zero;
            if (target != null && overlayRect != null) targetPos = GetAnchoredPositionInOverlay(target, overlayRect);

            Vector2 offset = (_step == ClockLearningTutorialStep.SingleQuestionFocus || _step == ClockLearningTutorialStep.DoubleQuestionFocus)
                ? questionPointerOffset
                : normalPointerOffset;

            targetPos += offset;
            if (overlayRect != null) targetPos = ClampInsideOverlay(targetPos, overlayRect, pointer, Vector2.zero);

            pointer.DOAnchorPos(targetPos, pointerMoveDuration).SetEase(Ease.OutCubic).SetUpdate(true)
                .OnComplete(() =>
                {
                    if (pointer == null) return;
                    _pointerSequence?.Kill();
                    _pointerSequence = DOTween.Sequence().SetUpdate(true)
                        .Append(pointer.DOAnchorPosY(targetPos.y + pointerHoverPixels, 0.5f).SetEase(Ease.InOutSine))
                        .Append(pointer.DOAnchorPosY(targetPos.y, 0.5f).SetEase(Ease.InOutSine))
                        .SetLoops(-1);
                });
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

            RectTransform faceTarget = tutorialClock != null && tutorialClock.ClockFaceRect != null
                ? tutorialClock.ClockFaceRect
                : GetTarget(step);

            RectTransform overlayRect = overlayGroup != null ? overlayGroup.transform as RectTransform : ghostHand.parent as RectTransform;
            if (overlayRect == null || (faceTarget == null && actualHand == null)) return;

            Vector2 handBase = actualHand != null
                ? GetWorldPositionInOverlay(actualHand.position, overlayRect)
                : GetAnchoredPositionInOverlay(faceTarget, overlayRect);

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

            Image actualHandImage = actualHand != null ? actualHand.GetComponent<Image>() : null;
            if (ghostHandImage != null)
            {
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

            float startAngle = tutorialClock != null ? tutorialClock.GetRenderedHandClockwiseAngle(handType) : 0f;
            float moveDegrees = handType == ClockLearningHandType.Minute ? fakeMinuteMoveDegrees : fakeHourMoveDegrees;
            float endAngle = startAngle + moveDegrees;
            float tipLength = Mathf.Max(20f, length * Mathf.Clamp01(1f - ghostPivot.y) * Mathf.Clamp(pointerHandTipFollow, 0.55f, 1.05f));

            ghostHand.localRotation = Quaternion.Euler(0f, 0f, -startAngle);
            pointer.localScale = Vector3.one;
            UpdatePointerAtGhostTip(handBase, tipLength);

            _ghostSequence = DOTween.Sequence().SetUpdate(true);
            _ghostSequence.Append(pointer.DOScale(0.86f, 0.16f).SetEase(Ease.OutCubic));
            _ghostSequence.Append(pointer.DOScale(1f, 0.16f).SetEase(Ease.OutCubic));
            _ghostSequence.AppendInterval(0.16f);
            _ghostSequence.Append(ghostHand.DOLocalRotate(new Vector3(0f, 0f, -endAngle), fakeHandMoveDuration, RotateMode.FastBeyond360).SetEase(Ease.InOutCubic));
            _ghostSequence.OnUpdate(() => UpdatePointerAtGhostTip(handBase, tipLength));
            _ghostSequence.AppendInterval(0.55f);
            _ghostSequence.SetLoops(-1, LoopType.Restart);
        }

        private void SetPointerVisible(bool visible)
        {
            if (pointer == null) return;
            pointer.gameObject.SetActive(visible);
        }

        private void UpdatePointerAtGhostTip(Vector2 handBase, float tipLength)
        {
            if (pointer == null || ghostHand == null) return;
            float clockwiseAngle = NormalizeClockwiseAngle(-ghostHand.localEulerAngles.z);
            pointer.anchoredPosition = handBase + ClockPoint(clockwiseAngle, tipLength) + pointerHandTipOffset;
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
            if (ghostHand != null) ghostHand.gameObject.SetActive(false);
            SetPointerVisible(false);
            if (clickAnywhereButton != null) clickAnywhereButton.gameObject.SetActive(false);
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

            if (showOnlyOncePerMode || rememberSeenInPlayerPrefs)
            {
                return PlayerPrefs.GetInt(GetPrefsKey(mode), 0) == 1;
            }

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

            overlayGroup.alpha = visible ? overlayGroup.alpha : overlayGroup.alpha;
            overlayGroup.DOFade(visible ? 1f : 0f, 0.2f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    overlayGroup.interactable = visible && IsClickAnywhereStep();
                    overlayGroup.blocksRaycasts = visible && IsClickAnywhereStep();
                    if (!visible) overlayGroup.gameObject.SetActive(false);
                });
        }
    }
}
