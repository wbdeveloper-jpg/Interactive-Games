using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    public enum BehaviourWheelTutorialInstructionSide
    {
        Auto,
        Left,
        Right
    }

    /// <summary>
    /// Isolated first-time practice for the Behaviour Wheel game.
    /// It uses the existing wheel visuals but never starts or mutates a real round.
    /// </summary>
    public class BehaviourWheelFirstTimeTutorial : MonoBehaviour
    {
        private enum TutorialStage
        {
            None,
            ReadQuestion,
            FindAnswer,
            LearnPointer,
            WatchWheel,
            StopDemonstration,
            PlayerPractice,
            Retry,
            Success
        }

        [Header("Game References")]
        public BehaviourWheelGameManager gameManager;
        public BehaviourWheelSpinner spinner;
        public BehaviourWheelUI ui;
        [Tooltip("Assign the scene's fixed wheel pointer when possible. If empty, the top-centre of Wheel Root is used.")]
        public RectTransform wheelPointerTarget;

        [Header("Tutorial-Owned UI")]
        public RectTransform overlayRoot;
        public RectTransform instructionPanel;
        public TMP_Text instructionText;
        public CanvasGroup instructionCanvasGroup;
        [Tooltip("Leave the sprite empty. Assign your own hand sprite to this Image in the Inspector.")]
        public Image handPointerImage;
        public RectTransform focusFrame;
        public CanvasGroup focusCanvasGroup;

        [Header("Practice Question")]
        [TextArea(2, 3)] public string practiceQuestion = "Which behaviour means helping someone?";
        public string practiceCorrectAnswer = "Kind";
        public List<string> practiceOptions = new List<string> { "Kind", "Selfish", "Ignorant" };
        [Range(30f, 180f)] public float practiceSpinSpeed = 75f;
        public float practiceStartRotation;

        [Header("Child-Friendly Text")]
        [TextArea(2, 4)] public string readQuestionInstruction = "First, read the question.\nTap anywhere to continue.";
        [TextArea(2, 4)] public string findAnswerInstruction = "Find the correct answer on the wheel.\nTap anywhere to continue.";
        [TextArea(2, 4)] public string pointerInstruction = "The answer under this pointer is selected.\nTap anywhere to continue.";
        [TextArea(2, 4)] public string watchInstruction = "Watch the correct answer move around the wheel.";
        [TextArea(2, 4)] public string stopInstruction = "When the correct answer reaches the pointer, tap STOP.";
        [TextArea(2, 4)] public string practiceInstruction = "Your turn! Tap STOP when the hand shows the correct moment.";
        [TextArea(2, 4)] public string secondTrialInstruction = "Great! Now try one more time without the hand.";
        [TextArea(2, 4)] public string unguidedPracticeInstruction = "Now do it yourself! Tap STOP when the correct answer reaches the pointer.";
        [TextArea(2, 4)] public string retryInstruction = "Good try! Watch for the correct answer and try again.";
        [TextArea(2, 4)] public string successInstruction = "Great job! You stopped on the correct answer. You are ready to play!";
        [TextArea(2, 4)] public string finalTransitionInstruction = "Great job! Tutorial complete.\nThe real game starts now!";

        [Header("Instruction Placement")]
        public BehaviourWheelTutorialInstructionSide preferredInstructionSide = BehaviourWheelTutorialInstructionSide.Auto;
        public Vector2 instructionPanelSize = new Vector2(480f, 200f);
        public Vector2 finalInstructionPanelSize = new Vector2(620f, 260f);
        [Min(0f)] public float sideMargin = 24f;
        [Tooltip("Distance between the wheel and the instruction card.")]
        [Min(0f)] public float instructionWheelGap = 28f;
        [Min(8f)] public float instructionFontSizeMin = 26f;
        [Min(8f)] public float instructionFontSizeMax = 38f;
        [Min(8f)] public float finalInstructionFontSizeMin = 34f;
        [Min(8f)] public float finalInstructionFontSizeMax = 48f;

        [Header("Smooth Transitions")]
        [Min(0.01f)] public float instructionFadeOutDuration = 0.12f;
        [Min(0.01f)] public float instructionFadeInDuration = 0.22f;
        [Min(0.01f)] public float handFadeDuration = 0.16f;
        [Min(0.01f)] public float focusFadeDuration = 0.16f;

        [Header("Hand Positioning")]
        [Tooltip("Set this to the fingertip position inside your hand sprite. The hand's pivot is placed on the intended target.")]
        public Vector2 handFingerPivot = new Vector2(0.5f, 0.92f);
        public Vector2 handSize = new Vector2(92f, 112f);
        public Vector2 questionHandOffset = Vector2.zero;
        public Vector2 answerHandOffset = Vector2.zero;
        public Vector2 pointerHandOffset = Vector2.zero;
        public Vector2 stopButtonHandOffset = Vector2.zero;
        public float questionHandRotation;
        public float answerHandRotation;
        public float pointerHandRotation;
        public float stopButtonHandRotation;

        [Header("Focus And Timing")]
        public Vector2 focusPadding = new Vector2(18f, 14f);
        [Min(0.1f)] public float tapInputDelay = 0.35f;
        [Min(0.5f)] public float watchDuration = 3.2f;
        [Tooltip("Time to read the STOP instruction before the hand demonstrates it.")]
        [Min(0.2f)] public float stopInstructionLeadInDuration = 1.4f;
        [Min(0.5f)] public float demonstrationDuration = 1.4f;
        [Tooltip("Keeps the STOP instruction visible after the hand tap before changing to the player's turn.")]
        [Min(0.1f)] public float stopInstructionAfterTapHold = 0.8f;
        [Min(0.5f)] public float secondTrialReadyDuration = 1.6f;
        [Min(1f)] public float inactivityReminderDelay = 15f;
        [Min(0.5f)] public float retryMessageDuration = 1.4f;
        [Min(0.5f)] public float finalTransitionHoldDuration = 2f;

        [Header("Testing")]
        public bool forcePlayForTesting;
        public bool resetCompletionOnPlay;

        public bool IsRunning => isRunning;
        public bool CanAcceptPlayerStop => isRunning && stage == TutorialStage.PlayerPractice && canAcceptPlayerStop;

        private TutorialStage stage;
        private bool isRunning;
        private bool waitingForScreenTap;
        private bool canAcceptPlayerStop;
        private bool reminderPlaying;
        private bool isGuidedPractice;
        private bool guidedCueVisible;
        private float acceptTapAfterTime;
        private float inactivityDeadline;
        private float originalSpinSpeed;
        private bool originalPauseInteractable;
        private int correctOptionIndex;
        private Action completionCallback;
        private Coroutine activeRoutine;
        private RectTransform handFollowTarget;
        private Vector2 handFollowNormalizedPoint = new Vector2(0.5f, 0.5f);
        private Vector2 handFollowOffset;
        private RectTransform focusFollowTarget;
        private Bounds cachedStableWheelBounds;
        private Vector2 cachedWheelRectSize = new Vector2(-1f, -1f);
        private Vector3 cachedWheelWorldCenter;
        private Vector2Int cachedScreenSize;
        private bool stableWheelBoundsCached;
        private bool centerInstructionPanel;
        private Sequence instructionTransition;

        private string CompletionKey =>
            $"BehaviourWheelStop.InteractiveTutorial.Completed.{SceneManager.GetActiveScene().name}";

        private void Awake()
        {
            ResolveReferences();
            SetTutorialVisualsVisible(false);
        }

        public void PrepareSavedStateForTesting()
        {
            if (resetCompletionOnPlay)
                ResetSavedTutorialForThisScene();
        }

        public bool ShouldPlayTutorial()
        {
            return isActiveAndEnabled && (forcePlayForTesting || PlayerPrefs.GetInt(CompletionKey, 0) == 0);
        }

        public void BeginTutorial(Action onCompleted)
        {
            if (isRunning)
                return;

            ResolveReferences();
            if (spinner == null || ui == null || overlayRoot == null || instructionPanel == null || instructionText == null)
            {
                Debug.LogWarning("Behaviour Wheel tutorial is missing required references. Starting the normal round instead.", this);
                onCompleted?.Invoke();
                return;
            }

            completionCallback = onCompleted;
            isRunning = true;
            isGuidedPractice = true;
            guidedCueVisible = false;
            centerInstructionPanel = false;
            originalSpinSpeed = spinner.spinSpeed;
            originalPauseInteractable = ui.pauseButton != null && ui.pauseButton.interactable;

            if (ui.pauseButton != null)
                ui.pauseButton.interactable = false;

            ui.ShowGameplay();
            ui.HideFeedback();
            ui.SetStopButtonInteractable(false);
            ui.SetGameplayTexts(1, 1, practiceQuestion, 0);

            BuildPracticeWheel();
            stableWheelBoundsCached = false;
            spinner.StoppedOnSlice += OnTutorialWheelStopped;

            SetTutorialVisualsVisible(true);
            ShowReadQuestionStage();
        }

        public void HandlePlayerStop()
        {
            if (!CanAcceptPlayerStop || spinner == null)
                return;

            canAcceptPlayerStop = false;
            spinner.StopNow();
        }

        private void Update()
        {
            if (!isRunning)
                return;

            if (waitingForScreenTap && Time.unscaledTime >= acceptTapAfterTime && WasScreenPressedThisFrame())
            {
                waitingForScreenTap = false;
                AdvanceFromTapStage();
                return;
            }

            if (stage == TutorialStage.PlayerPractice && canAcceptPlayerStop && !reminderPlaying &&
                Time.unscaledTime >= inactivityDeadline)
            {
                StartInactivityReminder();
            }
        }

        private void LateUpdate()
        {
            if (!isRunning)
                return;

            PositionInstructionInSideSpace();

            if (stage == TutorialStage.PlayerPractice && isGuidedPractice && canAcceptPlayerStop)
                UpdateGuidedStopCue();

            if (focusFollowTarget != null)
                PositionFocusOver(focusFollowTarget);

            if (handFollowTarget != null)
                PositionHandAt(handFollowTarget, handFollowNormalizedPoint, handFollowOffset);
        }

        private void ShowReadQuestionStage()
        {
            stage = TutorialStage.ReadQuestion;
            ShowInstruction(readQuestionInstruction);
            RectTransform target = ui.questionText != null ? ui.questionText.rectTransform : null;
            ShowFocus(target);
            ShowHand(target, new Vector2(0.5f, 0.5f), questionHandOffset, questionHandRotation);
            WaitForScreenTap();
        }

        private void ShowFindAnswerStage()
        {
            stage = TutorialStage.FindAnswer;
            ShowInstruction(findAnswerInstruction);
            RectTransform answerTarget = spinner.GetOptionTarget(correctOptionIndex);
            ShowFocus(answerTarget);
            ShowHand(answerTarget, new Vector2(0.5f, 0.5f), answerHandOffset, answerHandRotation);
            WaitForScreenTap();
        }

        private void ShowLearnPointerStage()
        {
            stage = TutorialStage.LearnPointer;
            ShowInstruction(pointerInstruction);
            RectTransform target = wheelPointerTarget != null ? wheelPointerTarget : spinner.wheelRoot;
            Vector2 point = wheelPointerTarget != null ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
            ShowFocus(wheelPointerTarget);
            ShowHand(target, point, pointerHandOffset, pointerHandRotation);
            WaitForScreenTap();
        }

        private void ShowWatchWheelStage()
        {
            stage = TutorialStage.WatchWheel;
            waitingForScreenTap = false;
            ShowInstruction(watchInstruction);

            // Keep tutorial overlays attached to a stable target while the wheel rotates.
            // Following a rotating label makes world bounds constantly change and can visibly jitter.
            HideFocus();
            RectTransform target = wheelPointerTarget != null ? wheelPointerTarget : spinner.wheelRoot;
            Vector2 point = wheelPointerTarget != null ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 1f);
            ShowHand(target, point, pointerHandOffset, pointerHandRotation);
            spinner.StartSpin();
            StartManagedRoutine(WatchThenDemonstrateRoutine());
        }

        private IEnumerator WatchThenDemonstrateRoutine()
        {
            yield return new WaitForSecondsRealtime(watchDuration);
            yield return ShowStopDemonstrationRoutine();
        }

        private IEnumerator ShowStopDemonstrationRoutine()
        {
            stage = TutorialStage.StopDemonstration;
            canAcceptPlayerStop = false;
            if (ui.stopButton != null)
                ui.stopButton.interactable = false;

            ShowInstruction(stopInstruction);
            HideHand();
            HideFocus();

            // Give the child time to read the instruction before showing the gesture.
            yield return new WaitForSecondsRealtime(stopInstructionLeadInDuration);

            float timeout = Mathf.Max(5f, 360f / Mathf.Max(1f, spinner.spinSpeed) + 1f);
            float elapsed = 0f;
            while (elapsed < timeout && !string.Equals(spinner.GetSelectedAnswer(), practiceCorrectAnswer, StringComparison.OrdinalIgnoreCase))
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            RectTransform stopTarget = ui.stopButton != null ? ui.stopButton.transform as RectTransform : null;
            ShowFocus(stopTarget);
            ShowHand(stopTarget, new Vector2(0.5f, 0.5f), stopButtonHandOffset, stopButtonHandRotation);
            PlayHandTapAnimation();

            yield return new WaitForSecondsRealtime(demonstrationDuration);
            HideHand();
            HideFocus();

            // Do not replace the instruction immediately after the tap animation.
            yield return new WaitForSecondsRealtime(stopInstructionAfterTapHold);

            BeginPlayerPractice(true);
        }

        private void BeginPlayerPractice(bool guided)
        {
            stage = TutorialStage.PlayerPractice;
            reminderPlaying = false;
            canAcceptPlayerStop = true;
            isGuidedPractice = guided;
            guidedCueVisible = false;
            ShowInstruction(guided ? practiceInstruction : unguidedPracticeInstruction);
            HideHand();
            HideFocus();

            if (!spinner.IsSpinning)
                spinner.StartSpin();

            ui.SetStopButtonInteractable(true);
            inactivityDeadline = Time.unscaledTime + inactivityReminderDelay;
        }

        private void StartInactivityReminder()
        {
            // The guided cue already repeats on every correct pass. During the second
            // trial, keep it genuinely independent and only refresh the instruction.
            reminderPlaying = true;
            ShowInstruction(isGuidedPractice ? practiceInstruction : unguidedPracticeInstruction);
            inactivityDeadline = Time.unscaledTime + inactivityReminderDelay;
            reminderPlaying = false;
        }

        private void OnTutorialWheelStopped(int sliceIndex, string selectedAnswer)
        {
            if (!isRunning || stage != TutorialStage.PlayerPractice)
                return;

            bool correct = string.Equals(selectedAnswer, practiceCorrectAnswer, StringComparison.OrdinalIgnoreCase);
            if (correct)
            {
                if (gameManager != null && gameManager.audioController != null)
                    gameManager.audioController.PlayCorrect();

                if (isGuidedPractice)
                    StartManagedRoutine(FirstPracticeSuccessRoutine(sliceIndex));
                else
                    StartManagedRoutine(SuccessRoutine(sliceIndex));
            }
            else
            {
                if (gameManager != null && gameManager.audioController != null)
                    gameManager.audioController.PlayWrong();

                StartManagedRoutine(RetryRoutine(sliceIndex, isGuidedPractice));
            }
        }

        private IEnumerator RetryRoutine(int selectedIndex, bool retryGuided)
        {
            stage = TutorialStage.Retry;
            canAcceptPlayerStop = false;
            ui.SetStopButtonInteractable(false);
            ShowInstruction(retryInstruction);
            ShowFocus(spinner.GetOptionTarget(selectedIndex));
            HideHand();

            yield return new WaitForSecondsRealtime(retryMessageDuration);

            HideFocus();
            spinner.StartSpin();
            BeginPlayerPractice(retryGuided);
        }

        private IEnumerator FirstPracticeSuccessRoutine(int selectedIndex)
        {
            stage = TutorialStage.Success;
            canAcceptPlayerStop = false;
            guidedCueVisible = false;
            ui.SetStopButtonInteractable(false);
            HideHand();
            ShowFocus(spinner.GetOptionTarget(selectedIndex));
            ShowInstruction(secondTrialInstruction);

            yield return new WaitForSecondsRealtime(secondTrialReadyDuration);

            HideFocus();
            spinner.StartSpin();
            BeginPlayerPractice(false);
        }

        private IEnumerator SuccessRoutine(int selectedIndex)
        {
            stage = TutorialStage.Success;
            canAcceptPlayerStop = false;
            ui.SetStopButtonInteractable(false);
            HideHand();
            HideFocus();
            string finalMessage = string.IsNullOrWhiteSpace(finalTransitionInstruction)
                ? successInstruction
                : finalTransitionInstruction;
            ShowInstruction(finalMessage, true);

            float finalVisibleTime = instructionFadeOutDuration + instructionFadeInDuration + finalTransitionHoldDuration;
            yield return new WaitForSecondsRealtime(finalVisibleTime);
            activeRoutine = null;
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            PlayerPrefs.SetInt(CompletionKey, 1);
            PlayerPrefs.Save();

            Action callback = completionCallback;
            CleanupTutorial();
            callback?.Invoke();
        }

        private void CleanupTutorial()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }

            if (spinner != null)
            {
                spinner.StoppedOnSlice -= OnTutorialWheelStopped;
                spinner.StopSilently();
                spinner.spinSpeed = originalSpinSpeed;
            }

            if (ui != null)
            {
                if (ui.pauseButton != null)
                    ui.pauseButton.interactable = originalPauseInteractable;

                ui.SetStopButtonInteractable(false);
            }

            completionCallback = null;
            isRunning = false;
            stage = TutorialStage.None;
            waitingForScreenTap = false;
            canAcceptPlayerStop = false;
            reminderPlaying = false;
            isGuidedPractice = false;
            guidedCueVisible = false;
            centerInstructionPanel = false;
            SetTutorialVisualsVisible(false);
        }

        private void BuildPracticeWheel()
        {
            List<BehaviourWheelOptionData> options = new List<BehaviourWheelOptionData>();
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(practiceCorrectAnswer))
            {
                options.Add(new BehaviourWheelOptionData(practiceCorrectAnswer.Trim()));
                used.Add(practiceCorrectAnswer.Trim());
            }

            if (practiceOptions != null)
            {
                for (int i = 0; i < practiceOptions.Count && options.Count < 6; i++)
                {
                    string option = practiceOptions[i];
                    if (string.IsNullOrWhiteSpace(option) || used.Contains(option.Trim()))
                        continue;

                    options.Add(new BehaviourWheelOptionData(option.Trim()));
                    used.Add(option.Trim());
                }
            }

            while (options.Count < 3)
            {
                string fallback = $"Option {options.Count + 1}";
                options.Add(new BehaviourWheelOptionData(fallback));
            }

            correctOptionIndex = 0;
            spinner.StopSilently();
            spinner.spinSpeed = practiceSpinSpeed;
            spinner.SetRotation(practiceStartRotation);
            spinner.SetupOptions(options);
        }

        private void AdvanceFromTapStage()
        {
            switch (stage)
            {
                case TutorialStage.ReadQuestion:
                    ShowFindAnswerStage();
                    break;

                case TutorialStage.FindAnswer:
                    ShowLearnPointerStage();
                    break;

                case TutorialStage.LearnPointer:
                    ShowWatchWheelStage();
                    break;
            }
        }

        private void WaitForScreenTap()
        {
            waitingForScreenTap = true;
            acceptTapAfterTime = Time.unscaledTime + tapInputDelay;
        }

        private void ShowInstruction(string message, bool centered = false)
        {
            if (instructionPanel == null || instructionText == null)
                return;

            bool wasVisible = instructionPanel.gameObject.activeSelf;
            if (instructionTransition != null && instructionTransition.IsActive())
                instructionTransition.Kill();
            instructionPanel.DOKill();
            if (instructionCanvasGroup != null)
                instructionCanvasGroup.DOKill();

            instructionPanel.gameObject.SetActive(true);

            if (instructionCanvasGroup == null)
            {
                ApplyInstructionContent(message, centered);
                StartInstructionBreathing();
                return;
            }

            if (!wasVisible)
            {
                ApplyInstructionContent(message, centered);
                instructionCanvasGroup.alpha = 0f;
                instructionPanel.localScale = Vector3.one * 0.97f;
                instructionTransition = DOTween.Sequence().SetUpdate(true);
                instructionTransition.Append(instructionCanvasGroup.DOFade(1f, instructionFadeInDuration));
                instructionTransition.Join(instructionPanel.DOScale(1f, instructionFadeInDuration).SetEase(Ease.OutQuad));
                instructionTransition.OnComplete(StartInstructionBreathing);
                return;
            }

            instructionTransition = DOTween.Sequence().SetUpdate(true);
            instructionTransition.Append(instructionCanvasGroup.DOFade(0f, instructionFadeOutDuration));
            instructionTransition.AppendCallback(() =>
            {
                ApplyInstructionContent(message, centered);
                instructionPanel.localScale = Vector3.one * 0.97f;
            });
            instructionTransition.Append(instructionCanvasGroup.DOFade(1f, instructionFadeInDuration));
            instructionTransition.Join(instructionPanel.DOScale(1f, instructionFadeInDuration).SetEase(Ease.OutQuad));
            instructionTransition.OnComplete(StartInstructionBreathing);
        }

        private void ApplyInstructionContent(string message, bool centered)
        {
            centerInstructionPanel = centered;
            instructionText.text = message;
            instructionText.enableAutoSizing = true;
            float minimumFinalFont = Mathf.Max(finalInstructionFontSizeMin, instructionFontSizeMin + 10f);
            float maximumFinalFont = Mathf.Max(finalInstructionFontSizeMax, instructionFontSizeMax + 14f);
            instructionText.fontSizeMin = centered ? minimumFinalFont : instructionFontSizeMin;
            instructionText.fontSizeMax = centered ? maximumFinalFont : instructionFontSizeMax;

            if (centered && overlayRoot != null)
                ApplyFinalInstructionLayout(overlayRoot.rect);
        }

        private void StartInstructionBreathing()
        {
            if (instructionPanel == null || !instructionPanel.gameObject.activeInHierarchy)
                return;

            instructionPanel.DOKill();
            instructionPanel.localScale = Vector3.one;
            instructionPanel.DOScale(1.015f, 0.9f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void ShowHand(RectTransform target, Vector2 normalizedPoint, Vector2 offset, float rotation)
        {
            if (handPointerImage == null || target == null)
            {
                HideHand();
                return;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            handRect.pivot = handFingerPivot;
            handRect.sizeDelta = handSize;
            handRect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            handRect.localScale = Vector3.one;
            handPointerImage.enabled = handPointerImage.sprite != null;
            handPointerImage.gameObject.SetActive(handPointerImage.sprite != null);
            handPointerImage.DOKill();
            Color handColor = handPointerImage.color;
            handColor.a = 0f;
            handPointerImage.color = handColor;

            handFollowTarget = target;
            handFollowNormalizedPoint = normalizedPoint;
            handFollowOffset = offset;
            PositionHandAt(target, normalizedPoint, offset);
            handPointerImage.DOFade(1f, handFadeDuration).SetUpdate(true);
            PlayHandAttentionAnimation();
        }

        private void HideHand(bool immediate = false)
        {
            handFollowTarget = null;
            if (handPointerImage == null)
                return;

            handPointerImage.rectTransform.DOKill();
            handPointerImage.DOKill();
            if (immediate || !handPointerImage.gameObject.activeSelf)
            {
                handPointerImage.gameObject.SetActive(false);
                return;
            }

            handPointerImage.DOFade(0f, handFadeDuration).SetUpdate(true).OnComplete(() =>
            {
                if (handPointerImage != null)
                    handPointerImage.gameObject.SetActive(false);
            });
        }

        private void PlayHandTapAnimation()
        {
            if (handPointerImage == null || !handPointerImage.gameObject.activeSelf)
                return;

            RectTransform handRect = handPointerImage.rectTransform;
            handRect.DOKill();
            handRect.localScale = Vector3.one;
            handRect.DOScale(0.82f, 0.18f)
                .SetEase(Ease.InOutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void PlayGuidedHandTapLoop()
        {
            if (handPointerImage == null || !handPointerImage.gameObject.activeSelf)
                return;

            RectTransform handRect = handPointerImage.rectTransform;
            handRect.DOKill();
            handRect.localScale = Vector3.one;
            handRect.DOScale(0.82f, 0.22f)
                .SetEase(Ease.InOutQuad)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void PlayHandAttentionAnimation()
        {
            if (handPointerImage == null || !handPointerImage.gameObject.activeSelf)
                return;

            RectTransform handRect = handPointerImage.rectTransform;
            handRect.DOKill();
            handRect.localScale = Vector3.one;
            handRect.DOScale(1.08f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void ShowFocus(RectTransform target)
        {
            if (focusFrame == null || target == null)
            {
                HideFocus();
                return;
            }

            focusFollowTarget = target;
            focusFrame.gameObject.SetActive(true);
            PositionFocusOver(target);

            if (focusCanvasGroup != null)
            {
                focusCanvasGroup.DOKill();
                focusCanvasGroup.alpha = 0f;
                focusCanvasGroup.DOFade(1f, focusFadeDuration).SetUpdate(true);
            }
        }

        private void HideFocus(bool immediate = false)
        {
            focusFollowTarget = null;
            if (focusFrame == null)
                return;

            if (focusCanvasGroup == null || immediate || !focusFrame.gameObject.activeSelf)
            {
                if (focusCanvasGroup != null)
                    focusCanvasGroup.DOKill();
                focusFrame.gameObject.SetActive(false);
                return;
            }

            focusCanvasGroup.DOKill();
            focusCanvasGroup.DOFade(0f, focusFadeDuration).SetUpdate(true).OnComplete(() =>
            {
                if (focusFrame != null)
                    focusFrame.gameObject.SetActive(false);
            });
        }

        private void UpdateGuidedStopCue()
        {
            bool correctIsUnderPointer = string.Equals(spinner.GetSelectedAnswer(), practiceCorrectAnswer,
                StringComparison.OrdinalIgnoreCase);

            if (correctIsUnderPointer && !guidedCueVisible)
            {
                guidedCueVisible = true;
                RectTransform stopTarget = ui.stopButton != null ? ui.stopButton.transform as RectTransform : null;
                ShowFocus(stopTarget);
                ShowHand(stopTarget, new Vector2(0.5f, 0.5f), stopButtonHandOffset, stopButtonHandRotation);
                PlayGuidedHandTapLoop();
            }
            else if (!correctIsUnderPointer && guidedCueVisible)
            {
                guidedCueVisible = false;
                HideHand();
                HideFocus();
            }
        }

        private void PositionInstructionInSideSpace()
        {
            if (overlayRoot == null || instructionPanel == null || spinner == null || spinner.wheelRoot == null)
                return;

            Rect rootRect = overlayRoot.rect;

            if (centerInstructionPanel)
            {
                ApplyFinalInstructionLayout(rootRect);
                return;
            }

            Bounds wheelBounds = GetStableWheelBounds();
            float leftSpace = Mathf.Max(0f, wheelBounds.min.x - rootRect.xMin);
            float rightSpace = Mathf.Max(0f, rootRect.xMax - wheelBounds.max.x);

            bool useRight;
            switch (preferredInstructionSide)
            {
                case BehaviourWheelTutorialInstructionSide.Left:
                    useRight = false;
                    break;
                case BehaviourWheelTutorialInstructionSide.Right:
                    useRight = true;
                    break;
                default:
                    useRight = rightSpace >= leftSpace;
                    break;
            }

            // Keep every instruction card at the configured large size. The scene has dedicated
            // side space, so do not silently shrink normal cards back to their former dimensions.
            float width = Mathf.Min(instructionPanelSize.x, Mathf.Max(80f, rootRect.width - sideMargin * 2f));
            float height = Mathf.Min(instructionPanelSize.y, Mathf.Max(80f, rootRect.height - sideMargin * 2f));
            instructionPanel.anchorMin = new Vector2(0.5f, 0.5f);
            instructionPanel.anchorMax = new Vector2(0.5f, 0.5f);
            instructionPanel.pivot = new Vector2(0.5f, 0.5f);
            instructionPanel.sizeDelta = new Vector2(width, height);

            // Keep the card visually connected to the wheel instead of pushing it to the screen edge.
            float x = useRight
                ? wheelBounds.max.x + instructionWheelGap + width * 0.5f
                : wheelBounds.min.x - instructionWheelGap - width * 0.5f;

            float minX = rootRect.xMin + sideMargin + width * 0.5f;
            float maxX = rootRect.xMax - sideMargin - width * 0.5f;
            x = Mathf.Clamp(x, minX, maxX);
            float y = Mathf.Clamp(wheelBounds.center.y, rootRect.yMin + sideMargin + height * 0.5f,
                rootRect.yMax - sideMargin - height * 0.5f);

            instructionPanel.anchoredPosition = new Vector2(x, y);
        }

        private void ApplyFinalInstructionLayout(Rect rootRect)
        {
            // Existing scene components can retain older serialized values after a script upgrade.
            // Always keep the final card clearly larger than the normal side instruction card.
            float requestedWidth = Mathf.Max(finalInstructionPanelSize.x, instructionPanelSize.x + 180f);
            float requestedHeight = Mathf.Max(finalInstructionPanelSize.y, instructionPanelSize.y + 80f);
            float centeredWidth = Mathf.Min(requestedWidth,
                Mathf.Max(120f, rootRect.width - sideMargin * 2f));
            float centeredHeight = Mathf.Min(requestedHeight,
                Mathf.Max(80f, rootRect.height - sideMargin * 2f));

            instructionPanel.anchorMin = new Vector2(0.5f, 0.5f);
            instructionPanel.anchorMax = new Vector2(0.5f, 0.5f);
            instructionPanel.pivot = new Vector2(0.5f, 0.5f);
            instructionPanel.sizeDelta = new Vector2(centeredWidth, centeredHeight);
            instructionPanel.anchoredPosition = Vector2.zero;
        }

        private Bounds GetStableWheelBounds()
        {
            RectTransform wheel = spinner.wheelRoot;
            Rect wheelRect = wheel.rect;
            Vector2 currentRectSize = new Vector2(Mathf.Abs(wheelRect.width), Mathf.Abs(wheelRect.height));
            Vector3 worldCenter = wheel.TransformPoint(wheelRect.center);
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

            if (stableWheelBoundsCached && cachedScreenSize == screenSize &&
                cachedWheelRectSize == currentRectSize &&
                (cachedWheelWorldCenter - worldCenter).sqrMagnitude < 0.01f)
            {
                return cachedStableWheelBounds;
            }

            Transform stableAxes = wheel.parent != null ? wheel.parent : wheel;
            Vector3 worldRight = stableAxes.TransformVector(Vector3.right *
                (currentRectSize.x * Mathf.Abs(wheel.localScale.x) * 0.5f));
            Vector3 worldUp = stableAxes.TransformVector(Vector3.up *
                (currentRectSize.y * Mathf.Abs(wheel.localScale.y) * 0.5f));

            Vector3 localCenter = overlayRoot.InverseTransformPoint(worldCenter);
            Vector3 localRight = overlayRoot.InverseTransformPoint(worldCenter + worldRight) - localCenter;
            Vector3 localUp = overlayRoot.InverseTransformPoint(worldCenter + worldUp) - localCenter;
            float halfWidth = Mathf.Max(1f, new Vector2(localRight.x, localRight.y).magnitude);
            float halfHeight = Mathf.Max(1f, new Vector2(localUp.x, localUp.y).magnitude);

            cachedStableWheelBounds = new Bounds(localCenter,
                new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));
            cachedWheelRectSize = currentRectSize;
            cachedWheelWorldCenter = worldCenter;
            cachedScreenSize = screenSize;
            stableWheelBoundsCached = true;
            return cachedStableWheelBounds;
        }

        private void PositionHandAt(RectTransform target, Vector2 normalizedPoint, Vector2 offset)
        {
            if (overlayRoot == null || handPointerImage == null || target == null)
                return;

            Rect rect = target.rect;
            Vector3 worldPoint = target.TransformPoint(new Vector3(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPoint.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPoint.y),
                0f));
            Vector3 localPoint = overlayRoot.InverseTransformPoint(worldPoint);
            handPointerImage.rectTransform.anchoredPosition = new Vector2(localPoint.x, localPoint.y) + offset;
        }

        private void PositionFocusOver(RectTransform target)
        {
            if (overlayRoot == null || focusFrame == null || target == null)
                return;

            Bounds bounds = GetLocalBounds(target);
            focusFrame.anchorMin = new Vector2(0.5f, 0.5f);
            focusFrame.anchorMax = new Vector2(0.5f, 0.5f);
            focusFrame.pivot = new Vector2(0.5f, 0.5f);
            focusFrame.anchoredPosition = new Vector2(bounds.center.x, bounds.center.y);
            focusFrame.sizeDelta = new Vector2(bounds.size.x + focusPadding.x * 2f, bounds.size.y + focusPadding.y * 2f);
        }

        private Bounds GetLocalBounds(RectTransform target)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Vector3 first = overlayRoot.InverseTransformPoint(corners[0]);
            Bounds bounds = new Bounds(first, Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(overlayRoot.InverseTransformPoint(corners[i]));

            return bounds;
        }

        private void StartManagedRoutine(IEnumerator routine)
        {
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(routine);
        }

        private void SetTutorialVisualsVisible(bool visible)
        {
            if (!visible)
            {
                if (instructionTransition != null && instructionTransition.IsActive())
                    instructionTransition.Kill();
                if (instructionPanel != null)
                {
                    instructionPanel.DOKill();
                    if (instructionCanvasGroup != null)
                        instructionCanvasGroup.DOKill();
                    instructionPanel.gameObject.SetActive(false);
                }
                HideHand(true);
                HideFocus(true);
                return;
            }

            HideHand(true);
            HideFocus(true);
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
                gameManager = FindObjectOfType<BehaviourWheelGameManager>();
            if (spinner == null && gameManager != null)
                spinner = gameManager.spinner;
            if (ui == null && gameManager != null)
                ui = gameManager.ui;
            if (overlayRoot == null)
                overlayRoot = transform as RectTransform;
            if (instructionCanvasGroup == null && instructionPanel != null)
                instructionCanvasGroup = instructionPanel.GetComponent<CanvasGroup>();
        }

        private static bool WasScreenPressedThisFrame()
        {
            if (Input.GetMouseButtonDown(0))
                return true;

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                    return true;
            }

            return false;
        }

        [ContextMenu("Reset Saved Tutorial For This Scene")]
        public void ResetSavedTutorialForThisScene()
        {
            PlayerPrefs.DeleteKey(CompletionKey);
            PlayerPrefs.Save();
        }

        [ContextMenu("Mark Tutorial Complete For This Scene")]
        public void MarkTutorialCompleteForThisScene()
        {
            PlayerPrefs.SetInt(CompletionKey, 1);
            PlayerPrefs.Save();
        }

        private void OnDisable()
        {
            if (!isRunning)
                return;

            // Exiting halfway deliberately does not save completion.
            CleanupTutorial();
        }
    }
}
