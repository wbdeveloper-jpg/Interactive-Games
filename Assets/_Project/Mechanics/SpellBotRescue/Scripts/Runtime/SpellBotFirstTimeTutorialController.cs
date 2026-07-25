using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NarayanaGames.SpellBotRescue
{
    [DisallowMultipleComponent]
    public sealed class SpellBotFirstTimeTutorialController : MonoBehaviour, IPointerClickHandler
    {
        private enum TutorialStage
        {
            Inactive,
            Intro,
            BackDemonstration,
            BackPractice,
            CaretDemonstration,
            CaretPractice,
            LetterDemonstration,
            LetterPractice,
            HintDemonstration,
            HintPractice,
            FixedDemonstration,
            FixedPractice,
            PracticeIntro,
            FinalPractice,
            Success
        }

        private enum FinalPracticeStep
        {
            RemoveExtraLetter,
            PlaceCaret,
            AddMissingLetter,
            Submit
        }

        [Header("Game Reference")]
        public SpellBotRescueManager manager;

        [Header("Tutorial-Owned UI")]
        public RectTransform tutorialRoot;
        public Image fullScreenTapCatcher;
        public Image dimOverlay;
        public RectTransform instructionPanel;
        public CanvasGroup instructionCanvasGroup;
        public TextMeshProUGUI instructionText;
        [Tooltip("Leave the sprite empty. Assign your own hand pointer sprite in the Inspector.")]
        public Image handPointerImage;
        public TextMeshProUGUI ghostWordText;
        public CanvasGroup ghostWordCanvasGroup;
        public Image ghostCaretImage;
        public Button skipTutorialButton;
        public TextMeshProUGUI skipTutorialButtonText;

        [Header("Optional Skip")]
        [Tooltip("Shows a tutorial-owned Skip Tutorial button while the tutorial is active.")]
        public bool showSkipButton = true;
        [Tooltip("When enabled, choosing Skip Tutorial saves this scene's tutorial as completed.")]
        public bool skipMarksTutorialComplete = true;
        public string skipButtonLabel = "SKIP TUTORIAL";

        [Header("Practice Word")]
        public string startingPracticeWord = "frendd";
        public string wordAfterBackspace = "frend";
        public string correctPracticeWord = "friend";
        public string requiredLetter = "I";
        [Min(0)] public int requiredCaretIndex = 2;

        [Header("Child-Friendly Instructions")]
        [TextArea(2, 3)] public string introInstruction = "This word is spelt wrong. Let's fix it!\nTap anywhere to continue.";
        [TextArea(2, 3)] public string backDemoInstruction = "BACK removes the extra letter.\nWatch closely.";
        [TextArea(2, 3)] public string backPracticeInstruction = "Tap BACK to remove the extra letter.";
        [TextArea(2, 3)] public string backRetryInstruction = "Try the BACK key.";
        [TextArea(2, 3)] public string caretDemoInstruction = "Tap inside a word to choose where to type.\nWatch closely.";
        [TextArea(2, 3)] public string caretPracticeInstruction = "Tap just after r.";
        [TextArea(2, 3)] public string caretRetryInstruction = "Nearly! Tap just after r.";
        [TextArea(2, 3)] public string letterDemoInstruction = "The missing letter is i.\nWatch where it goes.";
        [TextArea(2, 3)] public string letterPracticeInstruction = "Now tap the letter I.";
        [TextArea(2, 3)] public string letterRetryInstruction = "Try the letter I.";
        [TextArea(2, 3)] public string hintDemoInstruction = "HINT can help when you are stuck.\nWatch closely.";
        [TextArea(2, 3)] public string hintPracticeInstruction = "Now tap HINT.";
        [TextArea(2, 3)] public string hintMessage = "Hint: The correct word is friend.";
        [TextArea(2, 3)] public string fixedDemoInstruction = "FIXED checks the word.\nWatch closely.";
        [TextArea(2, 3)] public string fixedPracticeInstruction = "Great! Tap FIXED to check the word.";
        [TextArea(2, 3)] public string fixedRetryInstruction = "Let's check the word again.";
        [TextArea(2, 3)] public string practiceIntroInstruction = "Now try it by yourself.\nTap anywhere to begin.";
        [TextArea(2, 3)] public string finalPracticeInstruction = "Fix the word by yourself.";
        [TextArea(2, 3)] public string successInstruction = "You succeeded!\nThe game is starting.";

        [Header("Pointer Tip Alignment")]
        [Tooltip("Position of the visible fingertip inside the hand sprite: 0,0 is bottom-left and 1,1 is top-right.")]
        public Vector2 handTipNormalised = new Vector2(0.5f, 1f);
        [Tooltip("Point used on normal UI targets. The default points to the exact centre.")]
        public Vector2 targetPointNormalised = new Vector2(0.5f, 0.5f);
        [Tooltip("Leave off for exact fingertip alignment. Enable only if a large hand sprite goes off-screen.")]
        public bool keepWholeHandOnScreen;

        [Header("Pointer Fine-Tuning Offsets")]
        public Vector2 wordPointerOffset = Vector2.zero;
        public Vector2 backPointerOffset = Vector2.zero;
        public Vector2 caretPointerOffset = Vector2.zero;
        public Vector2 letterPointerOffset = Vector2.zero;
        public Vector2 hintPointerOffset = Vector2.zero;
        public Vector2 fixedPointerOffset = Vector2.zero;

        [Header("Instruction Offsets")]
        public Vector2 wordInstructionOffset = new Vector2(0f, 155f);
        public Vector2 keyboardInstructionOffset = new Vector2(0f, 155f);
        public Vector2 hintInstructionOffset = new Vector2(0f, -145f);
        public Vector2 finalInstructionPosition = Vector2.zero;
        [Min(10f)] public float screenEdgePadding = 30f;

        [Header("Ghost Guide")]
        public Vector2 ghostWordOffset = new Vector2(0f, 95f);
        [Range(0f, 1f)] public float ghostAlpha = 0.68f;
        public Color ghostWordColor = new Color(0.16f, 0.48f, 0.92f, 1f);
        public Color ghostCaretColor = new Color(0.16f, 0.48f, 0.92f, 0.82f);

        [Header("Animation")]
        [Min(0.05f)] public float handMoveDuration = 0.42f;
        [Min(0.03f)] public float tapDownDuration = 0.10f;
        [Range(0.5f, 1f)] public float tapScale = 0.82f;
        [Min(0.05f)] public float instructionFadeOutDuration = 0.12f;
        [Min(0.1f)] public float instructionFadeDuration = 0.18f;
        [Tooltip("Minimum time a demonstration instruction stays readable before the hand begins moving.")]
        [Min(0.3f)] public float instructionReadDelay = 1.35f;
        [Min(0.1f)] public float instructionBreathDuration = 0.85f;
        [Range(1f, 1.12f)] public float instructionBreathScale = 1.025f;
        [Min(0.1f)] public float ghostFadeDuration = 0.18f;
        [Min(0.1f)] public float demonstrationHoldDuration = 0.75f;
        [Tooltip("Pause after a correct child action before introducing the next idea.")]
        [Min(0.2f)] public float actionAdvanceDelay = 0.85f;
        [Min(0.5f)] public float successMessageDuration = 1.8f;
        [Min(3f)] public float idleRepeatDelay = 15f;

        [HideInInspector] public int installedTutorialVersion;

        [Header("First-Time Save")]
        public string tutorialPlayerPrefsPrefix = "SpellBotRescue.InteractiveTutorial.Completed";
        [Tooltip("Testing option. Plays even when the current scene is already completed.")]
        public bool forcePlayEveryTime;
        [Tooltip("Useful while testing. When off, completion is not saved.")]
        public bool saveCompletion = true;

        public bool IsRunning { get; private set; }

        private TutorialStage currentStage = TutorialStage.Inactive;
        private FinalPracticeStep finalPracticeStep;
        private Action completionCallback;
        private Coroutine stageRoutine;
        private Tween instructionBreathTween;
        private Tween instructionTransitionTween;
        private string practiceText = string.Empty;
        private int practiceCaretIndex;
        private float lastMeaningfulInputTime;
        private bool completing;
        private Canvas rootCanvas;
        private Vector3 handBaseScale = Vector3.one;
        private Vector3 instructionBaseScale = Vector3.one;

        private void Awake()
        {
            ResolveReferences();
            HideVisualsInstant();
        }

        private void OnDisable()
        {
            UnregisterSkipButton();

            if (!IsRunning || completing)
            {
                return;
            }

            StopRuntimeAnimations();
            IsRunning = false;
            currentStage = TutorialStage.Inactive;

            if (manager != null)
            {
                manager.ExitFirstTimeTutorialMode(this);
            }

            completionCallback = null;
        }

        private void OnDestroy()
        {
            UnregisterSkipButton();
            StopRuntimeAnimations();
        }

        private void Update()
        {
            if (!IsRunning || stageRoutine != null || !IsPracticeStage(currentStage))
            {
                return;
            }

            if (Time.unscaledTime - lastMeaningfulInputTime < idleRepeatDelay)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = StartCoroutine(PlayIdleReminder());
        }

        public bool ShouldPlayForCurrentScene()
        {
            return forcePlayEveryTime || PlayerPrefs.GetInt(GetTutorialPlayerPrefsKey(), 0) == 0;
        }

        public void BeginTutorial(SpellBotRescueManager owner, Action onCompleted)
        {
            if (IsRunning)
            {
                return;
            }

            manager = owner != null ? owner : manager;
            completionCallback = onCompleted;
            completing = false;

            if (manager == null)
            {
                Debug.LogError("SpellBot tutorial cannot start because the SpellBotRescueManager reference is missing.", this);
                ContinueWithoutTutorial();
                return;
            }

            ResolveReferences();

            if (tutorialRoot == null ||
                instructionText == null ||
                manager.wordText == null ||
                manager.keyboardView == null)
            {
                Debug.LogError("SpellBot tutorial UI references are incomplete. Run the Install or Upgrade toolbar command.", this);
                ContinueWithoutTutorial();
                return;
            }

            gameObject.SetActive(true);
            tutorialRoot.gameObject.SetActive(true);
            tutorialRoot.SetAsLastSibling();

            IsRunning = true;
            currentStage = TutorialStage.Intro;
            practiceText = SanitizePracticeWord(startingPracticeWord);
            practiceCaretIndex = practiceText.Length;
            lastMeaningfulInputTime = Time.unscaledTime;
            RegisterSkipButton();
            RefreshSkipButton();

            manager.EnterFirstTimeTutorialMode(this);
            manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, false);
            SetKeyboardForStage(TutorialStage.Intro);
            SetTapCatcherActive(true);
            SetDimAlpha(0f);
            HideGhostInstant();
            ShowHandAtWord();
            ShowInstruction(introInstruction, manager.wordText != null ? manager.wordText.rectTransform : null, wordInstructionOffset);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;

            if (currentStage == TutorialStage.Intro)
            {
                StartStageRoutine(RunBackspaceDemonstration());
            }
            else if (currentStage == TutorialStage.PracticeIntro)
            {
                StartFinalPractice();
            }
        }

        public void HandleBackspaceInput()
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;

            if (currentStage == TutorialStage.FinalPractice)
            {
                if (finalPracticeStep != FinalPracticeStep.RemoveExtraLetter)
                {
                    ShowFinalPracticeReminder();
                    return;
                }

                practiceText = SanitizePracticeWord(wordAfterBackspace);
                practiceCaretIndex = practiceText.Length;
                finalPracticeStep = FinalPracticeStep.PlaceCaret;
                manager.PlayTutorialKeyPressSound();
                manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, true);
                SetKeyboardForStage(TutorialStage.FinalPractice);
                return;
            }

            if (currentStage != TutorialStage.BackPractice)
            {
                ShowWrongAction(GetCurrentPracticeInstruction(), GetCurrentTarget(), GetCurrentPointerOffset());
                return;
            }

            practiceText = SanitizePracticeWord(wordAfterBackspace);
            practiceCaretIndex = practiceText.Length;
            manager.PlayTutorialKeyPressSound();
            manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, true);
            SetKeyboardForStage(TutorialStage.BackDemonstration);
            StartStageRoutine(AdvanceToCaretDemonstration());
        }

        public void HandleLetterInput(string letter)
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;

            string cleanRequiredLetter = string.IsNullOrWhiteSpace(requiredLetter)
                ? "i"
                : requiredLetter.Substring(0, 1);

            if (currentStage == TutorialStage.FinalPractice)
            {
                if (finalPracticeStep != FinalPracticeStep.AddMissingLetter ||
                    string.IsNullOrWhiteSpace(letter) ||
                    !string.Equals(letter.Substring(0, 1), cleanRequiredLetter, StringComparison.OrdinalIgnoreCase))
                {
                    ShowFinalPracticeReminder();
                    return;
                }

                practiceCaretIndex = Mathf.Clamp(practiceCaretIndex, 0, practiceText.Length);
                practiceText = practiceText.Insert(practiceCaretIndex, cleanRequiredLetter.ToLowerInvariant());
                practiceCaretIndex++;
                finalPracticeStep = FinalPracticeStep.Submit;
                manager.PlayTutorialKeyPressSound();
                manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, true);
                SetKeyboardForStage(TutorialStage.FinalPractice);
                return;
            }

            if (currentStage != TutorialStage.LetterPractice)
            {
                ShowWrongAction(GetCurrentPracticeInstruction(), GetCurrentTarget(), GetCurrentPointerOffset());
                return;
            }

            if (string.IsNullOrWhiteSpace(letter) ||
                !string.Equals(letter.Substring(0, 1), cleanRequiredLetter, StringComparison.OrdinalIgnoreCase))
            {
                ShowWrongAction(letterRetryInstruction, GetRequiredLetterKeyRect(), letterPointerOffset);
                return;
            }

            string cleanLetter = cleanRequiredLetter.ToLowerInvariant();
            practiceCaretIndex = Mathf.Clamp(practiceCaretIndex, 0, practiceText.Length);
            practiceText = practiceText.Insert(practiceCaretIndex, cleanLetter);
            practiceCaretIndex++;

            manager.PlayTutorialKeyPressSound();
            manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, true);
            SetKeyboardForStage(TutorialStage.LetterDemonstration);
            StartStageRoutine(AdvanceToHintDemonstration());
        }

        public void HandleCaretInput(int requestedIndex)
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;

            if (currentStage == TutorialStage.FinalPractice)
            {
                if (finalPracticeStep != FinalPracticeStep.PlaceCaret)
                {
                    return;
                }

                practiceCaretIndex = Mathf.Clamp(requestedIndex, 0, practiceText.Length);
                manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, true);

                if (practiceCaretIndex == requiredCaretIndex)
                {
                    finalPracticeStep = FinalPracticeStep.AddMissingLetter;
                    SetKeyboardForStage(TutorialStage.FinalPractice);
                }
                else
                {
                    ShowFinalPracticeReminder();
                }

                return;
            }

            if (currentStage != TutorialStage.CaretPractice)
            {
                return;
            }

            practiceCaretIndex = Mathf.Clamp(requestedIndex, 0, practiceText.Length);
            manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, true);

            if (practiceCaretIndex != requiredCaretIndex)
            {
                ShowInstruction(caretRetryInstruction, manager.wordText != null ? manager.wordText.rectTransform : null, wordInstructionOffset);
                ShowHandAtRequiredCaret();
                return;
            }

            SetKeyboardForStage(TutorialStage.CaretDemonstration);
            StartStageRoutine(AdvanceToLetterDemonstration());
        }

        public void HandleFixedInput()
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;

            if (currentStage == TutorialStage.FinalPractice)
            {
                if (finalPracticeStep != FinalPracticeStep.Submit ||
                    !string.Equals(practiceText, SanitizePracticeWord(correctPracticeWord), StringComparison.OrdinalIgnoreCase))
                {
                    ShowFinalPracticeReminder();
                    return;
                }

                manager.PlayTutorialKeyPressSound();
                ShowSuccess();
                return;
            }

            if (currentStage != TutorialStage.FixedPractice)
            {
                return;
            }

            if (!string.Equals(practiceText, SanitizePracticeWord(correctPracticeWord), StringComparison.OrdinalIgnoreCase))
            {
                ShowInstruction(fixedRetryInstruction, manager.wordText != null ? manager.wordText.rectTransform : null, wordInstructionOffset);
                return;
            }

            manager.PlayTutorialKeyPressSound();
            StartStageRoutine(ShowPracticeIntro());
        }

        public void HandleHintInput()
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;

            if (currentStage != TutorialStage.HintPractice)
            {
                ShowWrongAction(GetCurrentPracticeInstruction(), GetCurrentTarget(), GetCurrentPointerOffset());
                return;
            }

            manager.PlayTutorialKeyPressSound();

            if (manager.feedback != null)
            {
                manager.feedback.ShowHint(hintMessage, manager.hintText);
            }
            else if (manager.hintText != null)
            {
                manager.hintText.text = hintMessage;
            }

            SetKeyboardForStage(TutorialStage.HintDemonstration);
            StartStageRoutine(AdvanceToFixedDemonstration());
        }

        public void HandleDeleteForwardInput()
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;
            ShowInstruction(GetCurrentPracticeInstruction(), GetCurrentTarget(), GetCurrentInstructionOffset());
        }

        public void HandleClearInput()
        {
            if (!IsRunning)
            {
                return;
            }

            lastMeaningfulInputTime = Time.unscaledTime;
            ShowInstruction(GetCurrentPracticeInstruction(), GetCurrentTarget(), GetCurrentInstructionOffset());
        }

        [ContextMenu("SpellBot Tutorial/Reset Current Scene")]
        public void ResetTutorialForCurrentScene()
        {
            PlayerPrefs.DeleteKey(GetTutorialPlayerPrefsKey());
            PlayerPrefs.Save();
        }

        [ContextMenu("SpellBot Tutorial/Mark Current Scene Complete")]
        public void MarkTutorialCompleteForCurrentScene()
        {
            PlayerPrefs.SetInt(GetTutorialPlayerPrefsKey(), 1);
            PlayerPrefs.Save();
        }

        private IEnumerator RunBackspaceDemonstration()
        {
            currentStage = TutorialStage.BackDemonstration;
            SetTapCatcherActive(false);
            SetKeyboardForStage(currentStage);

            RectTransform target = GetKeyboardKeyRect(SpellBotKeyType.Backspace);
            ShowInstruction(backDemoInstruction, target, keyboardInstructionOffset);
            yield return new WaitForSecondsRealtime(instructionReadDelay);
            yield return MoveHandToTarget(target, backPointerOffset);

            ShowGhostWord(startingPracticeWord);
            yield return PlayHandTap();
            yield return new WaitForSecondsRealtime(0.12f);
            SetGhostWord(wordAfterBackspace);
            yield return new WaitForSecondsRealtime(demonstrationHoldDuration);
            HideGhost();

            currentStage = TutorialStage.BackPractice;
            SetKeyboardForStage(currentStage);
            ShowInstruction(backPracticeInstruction, target, keyboardInstructionOffset);
            PlaceHandAtTarget(target, backPointerOffset, false);
            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = null;
        }

        private IEnumerator AdvanceToCaretDemonstration()
        {
            yield return new WaitForSecondsRealtime(actionAdvanceDelay);
            yield return RunCaretDemonstration();
        }

        private IEnumerator RunCaretDemonstration()
        {
            currentStage = TutorialStage.CaretDemonstration;
            SetKeyboardForStage(currentStage);
            RectTransform wordTarget = manager.wordText != null ? manager.wordText.rectTransform : null;
            ShowInstruction(caretDemoInstruction, wordTarget, wordInstructionOffset);

            yield return new WaitForSecondsRealtime(instructionReadDelay);
            yield return MoveHandToRequiredCaret();
            ShowGhostCaret();
            yield return PlayHandTap();
            yield return new WaitForSecondsRealtime(demonstrationHoldDuration);
            HideGhostCaret();

            currentStage = TutorialStage.CaretPractice;
            SetKeyboardForStage(currentStage);
            ShowInstruction(caretPracticeInstruction, wordTarget, wordInstructionOffset);
            ShowHandAtRequiredCaret();
            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = null;
        }

        private IEnumerator AdvanceToLetterDemonstration()
        {
            yield return new WaitForSecondsRealtime(actionAdvanceDelay);
            yield return RunLetterDemonstration();
        }

        private IEnumerator RunLetterDemonstration()
        {
            currentStage = TutorialStage.LetterDemonstration;
            SetKeyboardForStage(currentStage);
            RectTransform target = GetRequiredLetterKeyRect();
            ShowInstruction(letterDemoInstruction, target, keyboardInstructionOffset);
            yield return new WaitForSecondsRealtime(instructionReadDelay);
            yield return MoveHandToTarget(target, letterPointerOffset);

            ShowGhostWord(wordAfterBackspace);
            yield return PlayHandTap();
            yield return new WaitForSecondsRealtime(0.12f);
            SetGhostWord(correctPracticeWord);
            yield return new WaitForSecondsRealtime(demonstrationHoldDuration);
            HideGhost();

            currentStage = TutorialStage.LetterPractice;
            SetKeyboardForStage(currentStage);
            ShowInstruction(letterPracticeInstruction, target, keyboardInstructionOffset);
            PlaceHandAtTarget(target, letterPointerOffset, false);
            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = null;
        }

        private IEnumerator AdvanceToHintDemonstration()
        {
            yield return new WaitForSecondsRealtime(actionAdvanceDelay);
            yield return RunHintDemonstration();
        }

        private IEnumerator RunHintDemonstration()
        {
            currentStage = TutorialStage.HintDemonstration;
            SetKeyboardForStage(currentStage);
            RectTransform target = manager.hintButton != null
                ? manager.hintButton.transform as RectTransform
                : null;

            ShowInstruction(hintDemoInstruction, target, hintInstructionOffset);
            yield return new WaitForSecondsRealtime(instructionReadDelay);
            yield return MoveHandToTarget(target, hintPointerOffset);
            yield return PlayHandTap();
            yield return new WaitForSecondsRealtime(demonstrationHoldDuration * 0.65f);

            currentStage = TutorialStage.HintPractice;
            SetKeyboardForStage(currentStage);
            ShowInstruction(hintPracticeInstruction, target, hintInstructionOffset);
            PlaceHandAtTarget(target, hintPointerOffset, false);
            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = null;
        }

        private IEnumerator AdvanceToFixedDemonstration()
        {
            yield return new WaitForSecondsRealtime(actionAdvanceDelay);
            yield return RunFixedDemonstration();
        }

        private IEnumerator RunFixedDemonstration()
        {
            currentStage = TutorialStage.FixedDemonstration;
            SetKeyboardForStage(currentStage);
            RectTransform target = GetKeyboardKeyRect(SpellBotKeyType.Fixed);
            ShowInstruction(fixedDemoInstruction, target, keyboardInstructionOffset);
            yield return new WaitForSecondsRealtime(instructionReadDelay);
            yield return MoveHandToTarget(target, fixedPointerOffset);
            yield return PlayHandTap();
            yield return new WaitForSecondsRealtime(demonstrationHoldDuration * 0.65f);

            currentStage = TutorialStage.FixedPractice;
            SetKeyboardForStage(currentStage);
            ShowInstruction(fixedPracticeInstruction, target, keyboardInstructionOffset);
            PlaceHandAtTarget(target, fixedPointerOffset, false);
            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = null;
        }

        private IEnumerator ShowPracticeIntro()
        {
            currentStage = TutorialStage.PracticeIntro;
            SetKeyboardForStage(currentStage);
            SetTapCatcherActive(false);
            HideHand();
            HideGhostInstant();

            if (manager.feedback != null)
            {
                manager.feedback.HideHint();
            }

            yield return new WaitForSecondsRealtime(actionAdvanceDelay);
            ShowInstructionAtPosition(practiceIntroInstruction, finalInstructionPosition);
            SetTapCatcherActive(true);
            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = null;
        }

        private void StartFinalPractice()
        {
            StopStageRoutine();
            currentStage = TutorialStage.FinalPractice;
            finalPracticeStep = FinalPracticeStep.RemoveExtraLetter;
            practiceText = SanitizePracticeWord(startingPracticeWord);
            practiceCaretIndex = practiceText.Length;

            SetTapCatcherActive(false);
            HideHand();
            HideGhostInstant();
            manager.SetTutorialWordDisplay(practiceText, practiceCaretIndex, false);
            SetKeyboardForStage(currentStage);
            ShowInstruction(finalPracticeInstruction,
                manager.wordText != null ? manager.wordText.rectTransform : null,
                wordInstructionOffset);
            lastMeaningfulInputTime = Time.unscaledTime;
        }

        private void ShowFinalPracticeReminder()
        {
            ShowInstruction(finalPracticeInstruction,
                manager != null && manager.wordText != null ? manager.wordText.rectTransform : null,
                wordInstructionOffset);

            if (Time.unscaledTime - lastMeaningfulInputTime >= idleRepeatDelay)
            {
                ShowFinalPracticeHandHint();
            }
        }

        private void ShowFinalPracticeHandHint()
        {
            RectTransform target = GetFinalPracticeTarget();

            if (finalPracticeStep == FinalPracticeStep.PlaceCaret)
            {
                ShowHandAtRequiredCaret();
            }
            else
            {
                PlaceHandAtTarget(target, GetFinalPracticePointerOffset(), true);
            }
        }

        private IEnumerator PlayIdleReminder()
        {
            RectTransform target = GetCurrentTarget();
            ShowInstruction(GetCurrentPracticeInstruction(), target, GetCurrentInstructionOffset());

            bool pointsToCaret = currentStage == TutorialStage.CaretPractice ||
                                 (currentStage == TutorialStage.FinalPractice &&
                                  finalPracticeStep == FinalPracticeStep.PlaceCaret);

            if (pointsToCaret)
            {
                yield return MoveHandToRequiredCaret();
                ShowGhostCaret();
                yield return PlayHandTap();
                yield return new WaitForSecondsRealtime(0.45f);
                HideGhostCaret();
            }
            else
            {
                yield return MoveHandToTarget(target, GetCurrentPointerOffset());
                yield return PlayHandTap();

                if (currentStage == TutorialStage.BackPractice)
                {
                    ShowGhostWord(startingPracticeWord);
                    yield return new WaitForSecondsRealtime(0.12f);
                    SetGhostWord(wordAfterBackspace);
                    yield return new WaitForSecondsRealtime(0.5f);
                    HideGhost();
                }
                else if (currentStage == TutorialStage.LetterPractice)
                {
                    ShowGhostWord(wordAfterBackspace);
                    yield return new WaitForSecondsRealtime(0.12f);
                    SetGhostWord(correctPracticeWord);
                    yield return new WaitForSecondsRealtime(0.5f);
                    HideGhost();
                }
            }

            lastMeaningfulInputTime = Time.unscaledTime;
            stageRoutine = null;
        }

        private void ShowSuccess()
        {
            StopStageRoutine();
            currentStage = TutorialStage.Success;
            RefreshSkipButton();
            SetKeyboardForStage(currentStage);
            SetTapCatcherActive(false);
            HideHand();
            HideGhostInstant();

            string cleanCorrectWord = SanitizePracticeWord(correctPracticeWord);
            manager.SetTutorialWordDisplay(cleanCorrectWord, cleanCorrectWord.Length, false);

            if (manager.wordText != null)
            {
                manager.wordText.color = manager.correctWordColor;
            }

            if (manager.wordInputField != null && manager.wordInputField.textComponent != null)
            {
                manager.wordInputField.textComponent.color = manager.correctWordColor;
            }

            if (manager.robotView != null)
            {
                manager.robotView.PlayHappy();
            }

            if (manager.audioSource != null && manager.correctClip != null)
            {
                manager.audioSource.PlayOneShot(manager.correctClip, 1f);
            }

            if (manager.feedback != null)
            {
                manager.feedback.PlayCorrectWord(manager.wordText);
                manager.feedback.PlayCorrectMonitorGlow();
            }

            ShowInstructionAtPosition(successInstruction, finalInstructionPosition);
            lastMeaningfulInputTime = Time.unscaledTime;
            StartStageRoutine(CompleteAfterSuccessMessage());
        }

        private IEnumerator CompleteAfterSuccessMessage()
        {
            yield return new WaitForSecondsRealtime(successMessageDuration);
            stageRoutine = null;
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            if (!IsRunning || currentStage != TutorialStage.Success)
            {
                return;
            }

            FinishTutorial(saveCompletion);
        }

        public void SkipTutorial()
        {
            if (!IsRunning)
            {
                return;
            }

            FinishTutorial(saveCompletion && skipMarksTutorialComplete);
        }

        private void FinishTutorial(bool markCompleted)
        {
            completing = true;

            if (markCompleted)
            {
                PlayerPrefs.SetInt(GetTutorialPlayerPrefsKey(), 1);
                PlayerPrefs.Save();
            }

            if (manager != null && manager.feedback != null)
            {
                manager.feedback.HideHintInstant();
            }

            StopRuntimeAnimations();
            HideVisualsInstant();
            IsRunning = false;
            currentStage = TutorialStage.Inactive;

            Action callback = completionCallback;
            completionCallback = null;

            if (manager != null)
            {
                manager.ExitFirstTimeTutorialMode(this);
            }

            tutorialRoot.gameObject.SetActive(false);
            completing = false;
            callback?.Invoke();
        }

        private void ContinueWithoutTutorial()
        {
            Action callback = completionCallback;
            completionCallback = null;
            IsRunning = false;
            currentStage = TutorialStage.Inactive;
            callback?.Invoke();
        }

        private void SetKeyboardForStage(TutorialStage stage)
        {
            if (manager == null || manager.keyboardView == null)
            {
                return;
            }

            for (int i = 0; i < manager.keyboardView.keys.Count; i++)
            {
                SpellBotKeyboardKey key = manager.keyboardView.keys[i];
                if (key == null || key.button == null)
                {
                    continue;
                }

                bool interactable = false;

                if (stage == TutorialStage.BackPractice)
                {
                    interactable = key.keyType == SpellBotKeyType.Backspace;
                }
                else if (stage == TutorialStage.LetterPractice)
                {
                    interactable = key.keyType == SpellBotKeyType.Letter &&
                                   string.Equals(key.letterValue, requiredLetter, StringComparison.OrdinalIgnoreCase);
                }
                else if (stage == TutorialStage.FixedPractice)
                {
                    interactable = key.keyType == SpellBotKeyType.Fixed;
                }
                else if (stage == TutorialStage.FinalPractice)
                {
                    if (finalPracticeStep == FinalPracticeStep.RemoveExtraLetter)
                    {
                        interactable = key.keyType == SpellBotKeyType.Backspace;
                    }
                    else if (finalPracticeStep == FinalPracticeStep.AddMissingLetter)
                    {
                        interactable = key.keyType == SpellBotKeyType.Letter &&
                                       string.Equals(key.letterValue, requiredLetter, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (finalPracticeStep == FinalPracticeStep.Submit)
                    {
                        interactable = key.keyType == SpellBotKeyType.Fixed;
                    }
                }

                key.button.interactable = interactable;
            }

            bool fixedReady = stage == TutorialStage.FixedPractice ||
                              (stage == TutorialStage.FinalPractice &&
                               finalPracticeStep == FinalPracticeStep.Submit);
            manager.keyboardView.SetFixedReady(fixedReady);

            if (manager.hintButton != null)
            {
                manager.hintButton.interactable = stage == TutorialStage.HintPractice;
            }

            if (manager.showAnswerButton != null)
            {
                manager.showAnswerButton.interactable = false;
                manager.showAnswerButton.gameObject.SetActive(false);
            }
        }

        private void ShowWrongAction(string message, RectTransform target, Vector2 pointerOffset)
        {
            if (!IsRunning || !IsPracticeStage(currentStage))
            {
                return;
            }

            ShowInstruction(message, target, GetCurrentInstructionOffset());
            PlaceHandAtTarget(target, pointerOffset, true);
        }

        private void ShowInstruction(string message, RectTransform target, Vector2 offset)
        {
            if (instructionPanel == null || instructionText == null)
            {
                return;
            }

            Vector2 destination = instructionPanel.anchoredPosition;
            if (target != null)
            {
                Vector2 localPoint = WorldToTutorialLocal(GetTargetWorldPoint(target));
                destination = ClampInsideRoot(localPoint + offset, instructionPanel);
            }

            TransitionInstruction(message, destination);
        }

        private void ShowInstructionAtPosition(string message, Vector2 anchoredPosition)
        {
            if (instructionPanel == null || instructionText == null)
            {
                return;
            }

            TransitionInstruction(message, ClampInsideRoot(anchoredPosition, instructionPanel));
        }

        private void TransitionInstruction(string message, Vector2 anchoredPosition)
        {
            instructionTransitionTween?.Kill();
            instructionTransitionTween = null;
            instructionBreathTween?.Kill();
            instructionBreathTween = null;

            bool wasVisible = instructionPanel.gameObject.activeSelf &&
                              instructionCanvasGroup != null &&
                              instructionCanvasGroup.alpha > 0.01f;

            instructionPanel.gameObject.SetActive(true);

            if (instructionCanvasGroup == null)
            {
                ApplyInstructionContent(message, anchoredPosition);
                RestartInstructionBreathing();
                return;
            }

            instructionCanvasGroup.DOKill();
            Sequence transition = DOTween.Sequence().SetUpdate(true);

            if (wasVisible)
            {
                transition.Append(instructionCanvasGroup.DOFade(0f, instructionFadeOutDuration).SetUpdate(true));
            }
            else
            {
                instructionCanvasGroup.alpha = 0f;
            }

            transition.AppendCallback(() => ApplyInstructionContent(message, anchoredPosition));
            transition.Append(instructionCanvasGroup.DOFade(1f, instructionFadeDuration).SetUpdate(true));
            transition.OnComplete(RestartInstructionBreathing);
            instructionTransitionTween = transition;
        }

        private void ApplyInstructionContent(string message, Vector2 anchoredPosition)
        {
            instructionText.text = message;
            instructionPanel.anchoredPosition = anchoredPosition;
            instructionPanel.localScale = instructionBaseScale;
        }

        private void RestartInstructionBreathing()
        {
            instructionBreathTween?.Kill();
            instructionPanel.DOKill();
            instructionPanel.localScale = instructionBaseScale;
            instructionBreathTween = instructionPanel
                .DOScale(instructionBaseScale * instructionBreathScale, instructionBreathDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private IEnumerator MoveHandToTarget(RectTransform target, Vector2 offset)
        {
            if (handPointerImage == null || target == null)
            {
                yield break;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            handPointerImage.gameObject.SetActive(true);
            handRect.DOKill();
            handRect.localScale = handBaseScale;
            Vector2 destination = GetHandPositionForWorldPoint(GetTargetWorldPoint(target), offset);
            yield return handRect.DOAnchorPos(destination, handMoveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .WaitForCompletion();
        }

        private IEnumerator MoveHandToRequiredCaret()
        {
            if (handPointerImage == null)
            {
                yield break;
            }

            Vector2 destination = GetHandPositionForTutorialPoint(GetRequiredCaretLocalPosition(), caretPointerOffset);
            RectTransform handRect = handPointerImage.rectTransform;
            handPointerImage.gameObject.SetActive(true);
            handRect.DOKill();
            handRect.localScale = handBaseScale;
            yield return handRect.DOAnchorPos(destination, handMoveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .WaitForCompletion();
        }

        private IEnumerator PlayHandTap()
        {
            if (handPointerImage == null || !handPointerImage.gameObject.activeSelf)
            {
                yield break;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            handRect.DOKill();
            handRect.localScale = handBaseScale;
            yield return handRect.DOScale(handBaseScale * tapScale, tapDownDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .WaitForCompletion();
            handRect.localScale = handBaseScale;
        }

        private void PlaceHandAtTarget(RectTransform target, Vector2 offset, bool animate)
        {
            if (handPointerImage == null || target == null)
            {
                return;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            handPointerImage.gameObject.SetActive(true);
            handRect.DOKill();
            handRect.localScale = handBaseScale;
            Vector2 destination = GetHandPositionForWorldPoint(GetTargetWorldPoint(target), offset);

            if (animate)
            {
                handRect.DOAnchorPos(destination, handMoveDuration).SetEase(Ease.OutCubic).SetUpdate(true);
            }
            else
            {
                handRect.anchoredPosition = destination;
            }
        }

        private void ShowHandAtWord()
        {
            PlaceHandAtTarget(manager != null && manager.wordText != null ? manager.wordText.rectTransform : null, wordPointerOffset, false);
        }

        private void ShowHandAtRequiredCaret()
        {
            if (handPointerImage == null)
            {
                return;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            handPointerImage.gameObject.SetActive(true);
            handRect.DOKill();
            handRect.localScale = handBaseScale;
            handRect.anchoredPosition = GetHandPositionForTutorialPoint(GetRequiredCaretLocalPosition(), caretPointerOffset);
        }

        private Vector2 GetHandPositionForWorldPoint(Vector3 worldPoint, Vector2 fineOffset)
        {
            return GetHandPositionForTutorialPoint(WorldToTutorialLocal(worldPoint), fineOffset);
        }

        private Vector2 GetHandPositionForTutorialPoint(Vector2 tutorialPoint, Vector2 fineOffset)
        {
            if (handPointerImage == null)
            {
                return tutorialPoint + fineOffset;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            Vector2 tipFromPivot = new Vector2(
                (handTipNormalised.x - handRect.pivot.x) * handRect.rect.width * handBaseScale.x,
                (handTipNormalised.y - handRect.pivot.y) * handRect.rect.height * handBaseScale.y);

            Vector2 handPosition = tutorialPoint - tipFromPivot + fineOffset;
            return keepWholeHandOnScreen
                ? ClampInsideRoot(handPosition, handRect)
                : handPosition;
        }

        private Vector3 GetTargetWorldPoint(RectTransform target)
        {
            if (target == null)
            {
                return Vector3.zero;
            }

            Rect rect = target.rect;
            Vector3 localPoint = new Vector3(
                Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(targetPointNormalised.x)),
                Mathf.Lerp(rect.yMin, rect.yMax, Mathf.Clamp01(targetPointNormalised.y)),
                0f);
            return target.TransformPoint(localPoint);
        }

        private void HideHand()
        {
            if (handPointerImage == null)
            {
                return;
            }

            handPointerImage.rectTransform.DOKill();
            handPointerImage.rectTransform.localScale = handBaseScale;
            handPointerImage.gameObject.SetActive(false);
        }

        private void ShowGhostWord(string word)
        {
            if (ghostWordText == null)
            {
                return;
            }

            CopyWordTextStyleToGhost();
            ghostWordText.text = SanitizePracticeWord(word);
            ghostWordText.rectTransform.anchoredPosition =
                WorldToTutorialLocal(manager.wordText.rectTransform.position) + ghostWordOffset;
            ghostWordText.gameObject.SetActive(true);

            if (ghostWordCanvasGroup != null)
            {
                ghostWordCanvasGroup.DOKill();
                ghostWordCanvasGroup.alpha = 0f;
                ghostWordCanvasGroup.DOFade(ghostAlpha, ghostFadeDuration).SetUpdate(true);
            }
            else
            {
                Color color = ghostWordColor;
                color.a = ghostAlpha;
                ghostWordText.color = color;
            }
        }

        private void SetGhostWord(string word)
        {
            if (ghostWordText == null)
            {
                return;
            }

            ghostWordText.transform.DOKill();
            ghostWordText.text = SanitizePracticeWord(word);
            ghostWordText.transform.localScale = Vector3.one;
            ghostWordText.transform.DOPunchScale(Vector3.one * 0.09f, 0.24f, 6, 0.8f).SetUpdate(true);
        }

        private void HideGhost()
        {
            if (ghostWordText == null)
            {
                return;
            }

            if (ghostWordCanvasGroup != null)
            {
                ghostWordCanvasGroup.DOKill();
                ghostWordCanvasGroup.DOFade(0f, ghostFadeDuration).SetUpdate(true)
                    .OnComplete(() => ghostWordText.gameObject.SetActive(false));
            }
            else
            {
                ghostWordText.gameObject.SetActive(false);
            }
        }

        private void ShowGhostCaret()
        {
            if (ghostCaretImage == null)
            {
                return;
            }

            RectTransform caretRect = ghostCaretImage.rectTransform;
            ghostCaretImage.color = ghostCaretColor;
            ghostCaretImage.gameObject.SetActive(true);
            caretRect.DOKill();
            caretRect.anchoredPosition = GetRequiredCaretLocalPosition();
            caretRect.localScale = Vector3.one;
            caretRect.DOScaleY(1.18f, 0.32f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        private void HideGhostCaret()
        {
            if (ghostCaretImage == null)
            {
                return;
            }

            ghostCaretImage.rectTransform.DOKill();
            ghostCaretImage.rectTransform.localScale = Vector3.one;
            ghostCaretImage.gameObject.SetActive(false);
        }

        private void HideGhostInstant()
        {
            if (ghostWordText != null)
            {
                ghostWordText.DOKill();
                ghostWordText.transform.DOKill();
                ghostWordText.gameObject.SetActive(false);
            }

            if (ghostWordCanvasGroup != null)
            {
                ghostWordCanvasGroup.DOKill();
                ghostWordCanvasGroup.alpha = 0f;
            }

            HideGhostCaret();
        }

        private void CopyWordTextStyleToGhost()
        {
            if (manager == null || manager.wordText == null || ghostWordText == null)
            {
                return;
            }

            ghostWordText.font = manager.wordText.font;
            ghostWordText.fontSize = manager.wordText.fontSize;
            ghostWordText.fontStyle = manager.wordText.fontStyle;
            ghostWordText.alignment = TextAlignmentOptions.Center;
            ghostWordText.color = ghostWordColor;
        }

        private RectTransform GetKeyboardKeyRect(SpellBotKeyType keyType)
        {
            if (manager == null || manager.keyboardView == null)
            {
                return null;
            }

            for (int i = 0; i < manager.keyboardView.keys.Count; i++)
            {
                SpellBotKeyboardKey key = manager.keyboardView.keys[i];
                if (key != null && key.keyType == keyType)
                {
                    return key.transform as RectTransform;
                }
            }

            return null;
        }

        private RectTransform GetRequiredLetterKeyRect()
        {
            if (manager == null || manager.keyboardView == null)
            {
                return null;
            }

            for (int i = 0; i < manager.keyboardView.keys.Count; i++)
            {
                SpellBotKeyboardKey key = manager.keyboardView.keys[i];
                if (key != null &&
                    key.keyType == SpellBotKeyType.Letter &&
                    string.Equals(key.letterValue, requiredLetter, StringComparison.OrdinalIgnoreCase))
                {
                    return key.transform as RectTransform;
                }
            }

            return null;
        }

        private RectTransform GetCurrentTarget()
        {
            switch (currentStage)
            {
                case TutorialStage.BackPractice:
                    return GetKeyboardKeyRect(SpellBotKeyType.Backspace);
                case TutorialStage.CaretPractice:
                    return manager != null && manager.wordText != null ? manager.wordText.rectTransform : null;
                case TutorialStage.LetterPractice:
                    return GetRequiredLetterKeyRect();
                case TutorialStage.HintPractice:
                    return manager != null && manager.hintButton != null
                        ? manager.hintButton.transform as RectTransform
                        : null;
                case TutorialStage.FixedPractice:
                    return GetKeyboardKeyRect(SpellBotKeyType.Fixed);
                case TutorialStage.FinalPractice:
                    return GetFinalPracticeTarget();
                default:
                    return manager != null && manager.wordText != null ? manager.wordText.rectTransform : null;
            }
        }

        private Vector2 GetCurrentPointerOffset()
        {
            switch (currentStage)
            {
                case TutorialStage.BackPractice:
                    return backPointerOffset;
                case TutorialStage.CaretPractice:
                    return caretPointerOffset;
                case TutorialStage.LetterPractice:
                    return letterPointerOffset;
                case TutorialStage.HintPractice:
                    return hintPointerOffset;
                case TutorialStage.FixedPractice:
                    return fixedPointerOffset;
                case TutorialStage.FinalPractice:
                    return GetFinalPracticePointerOffset();
                default:
                    return wordPointerOffset;
            }
        }

        private Vector2 GetCurrentInstructionOffset()
        {
            if (currentStage == TutorialStage.CaretPractice ||
                currentStage == TutorialStage.FinalPractice)
            {
                return wordInstructionOffset;
            }

            return currentStage == TutorialStage.HintPractice
                ? hintInstructionOffset
                : keyboardInstructionOffset;
        }

        private string GetCurrentPracticeInstruction()
        {
            switch (currentStage)
            {
                case TutorialStage.BackPractice:
                    return backPracticeInstruction;
                case TutorialStage.CaretPractice:
                    return caretPracticeInstruction;
                case TutorialStage.LetterPractice:
                    return letterPracticeInstruction;
                case TutorialStage.HintPractice:
                    return hintPracticeInstruction;
                case TutorialStage.FixedPractice:
                    return fixedPracticeInstruction;
                case TutorialStage.FinalPractice:
                    return finalPracticeInstruction;
                default:
                    return introInstruction;
            }
        }

        private RectTransform GetFinalPracticeTarget()
        {
            switch (finalPracticeStep)
            {
                case FinalPracticeStep.RemoveExtraLetter:
                    return GetKeyboardKeyRect(SpellBotKeyType.Backspace);
                case FinalPracticeStep.PlaceCaret:
                    return manager != null && manager.wordText != null ? manager.wordText.rectTransform : null;
                case FinalPracticeStep.AddMissingLetter:
                    return GetRequiredLetterKeyRect();
                case FinalPracticeStep.Submit:
                    return GetKeyboardKeyRect(SpellBotKeyType.Fixed);
                default:
                    return null;
            }
        }

        private Vector2 GetFinalPracticePointerOffset()
        {
            switch (finalPracticeStep)
            {
                case FinalPracticeStep.RemoveExtraLetter:
                    return backPointerOffset;
                case FinalPracticeStep.PlaceCaret:
                    return caretPointerOffset;
                case FinalPracticeStep.AddMissingLetter:
                    return letterPointerOffset;
                case FinalPracticeStep.Submit:
                    return fixedPointerOffset;
                default:
                    return Vector2.zero;
            }
        }

        private Vector2 GetRequiredCaretLocalPosition()
        {
            if (manager != null && manager.TryGetTutorialCaretWorldPosition(requiredCaretIndex, out Vector3 worldPosition))
            {
                return WorldToTutorialLocal(worldPosition);
            }

            return manager != null && manager.wordText != null
                ? WorldToTutorialLocal(manager.wordText.rectTransform.position)
                : Vector2.zero;
        }

        private Vector2 WorldToTutorialLocal(Vector3 worldPosition)
        {
            if (tutorialRoot == null)
            {
                return Vector2.zero;
            }

            Camera eventCamera = null;
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = rootCanvas.worldCamera;
            }

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tutorialRoot, screenPoint, eventCamera, out Vector2 localPoint);
            return localPoint;
        }

        private Vector2 ClampInsideRoot(Vector2 desiredPosition, RectTransform target)
        {
            if (tutorialRoot == null || target == null)
            {
                return desiredPosition;
            }

            Rect rootRect = tutorialRoot.rect;
            Vector2 halfSize = target.rect.size * 0.5f;
            float minimumX = rootRect.xMin + halfSize.x + screenEdgePadding;
            float maximumX = rootRect.xMax - halfSize.x - screenEdgePadding;
            float minimumY = rootRect.yMin + halfSize.y + screenEdgePadding;
            float maximumY = rootRect.yMax - halfSize.y - screenEdgePadding;

            return new Vector2(
                Mathf.Clamp(desiredPosition.x, minimumX, maximumX),
                Mathf.Clamp(desiredPosition.y, minimumY, maximumY));
        }

        private void SetTapCatcherActive(bool active)
        {
            if (fullScreenTapCatcher != null)
            {
                fullScreenTapCatcher.raycastTarget = active;
            }
        }

        private void SetDimAlpha(float alpha)
        {
            if (dimOverlay == null)
            {
                return;
            }

            Color color = dimOverlay.color;
            color.a = Mathf.Clamp01(alpha);
            dimOverlay.color = color;
            dimOverlay.raycastTarget = false;
        }

        private void StartStageRoutine(IEnumerator routine)
        {
            StopStageRoutine();
            stageRoutine = StartCoroutine(routine);
        }

        private void StopStageRoutine()
        {
            if (stageRoutine == null)
            {
                return;
            }

            StopCoroutine(stageRoutine);
            stageRoutine = null;
        }

        private void StopRuntimeAnimations()
        {
            StopStageRoutine();
            instructionBreathTween?.Kill();
            instructionBreathTween = null;
            instructionTransitionTween?.Kill();
            instructionTransitionTween = null;

            if (instructionPanel != null)
            {
                instructionPanel.DOKill();
                instructionPanel.localScale = instructionBaseScale;
            }

            if (instructionCanvasGroup != null)
            {
                instructionCanvasGroup.DOKill();
            }

            if (handPointerImage != null)
            {
                handPointerImage.rectTransform.DOKill();
                handPointerImage.rectTransform.localScale = handBaseScale;
            }

            HideGhostInstant();
        }

        private void HideVisualsInstant()
        {
            SetTapCatcherActive(false);
            HideHand();
            HideGhostInstant();

            if (skipTutorialButton != null)
            {
                skipTutorialButton.gameObject.SetActive(false);
            }

            if (instructionPanel != null)
            {
                instructionPanel.DOKill();
                instructionPanel.localScale = instructionBaseScale;
                instructionPanel.gameObject.SetActive(false);
            }

            if (instructionCanvasGroup != null)
            {
                instructionCanvasGroup.alpha = 0f;
            }
        }

        private void ResolveReferences()
        {
            if (tutorialRoot == null)
            {
                tutorialRoot = transform as RectTransform;
            }

            if (manager == null)
            {
                manager = FindObjectOfType<SpellBotRescueManager>();
            }

            rootCanvas = GetComponentInParent<Canvas>();

            if (handPointerImage != null)
            {
                handPointerImage.raycastTarget = false;
                handBaseScale = handPointerImage.rectTransform.localScale;
            }

            if (instructionPanel != null)
            {
                instructionBaseScale = instructionPanel.localScale;
            }

            if (ghostWordText != null)
            {
                ghostWordText.raycastTarget = false;
            }

            if (ghostCaretImage != null)
            {
                ghostCaretImage.raycastTarget = false;
            }
        }

        private void RegisterSkipButton()
        {
            if (skipTutorialButton == null)
            {
                return;
            }

            skipTutorialButton.onClick.RemoveListener(SkipTutorial);
            skipTutorialButton.onClick.AddListener(SkipTutorial);
        }

        private void UnregisterSkipButton()
        {
            if (skipTutorialButton != null)
            {
                skipTutorialButton.onClick.RemoveListener(SkipTutorial);
            }
        }

        private void RefreshSkipButton()
        {
            if (skipTutorialButton == null)
            {
                return;
            }

            if (skipTutorialButtonText != null)
            {
                skipTutorialButtonText.text = string.IsNullOrWhiteSpace(skipButtonLabel)
                    ? "SKIP TUTORIAL"
                    : skipButtonLabel;
            }

            bool shouldShow = showSkipButton && IsRunning;
            skipTutorialButton.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                skipTutorialButton.transform.SetAsLastSibling();
            }
        }

        private string GetTutorialPlayerPrefsKey()
        {
            return tutorialPlayerPrefsPrefix + "." + SceneManager.GetActiveScene().name;
        }

        private static string SanitizePracticeWord(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int count = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char character = char.ToLowerInvariant(value[i]);
                if (character >= 'a' && character <= 'z')
                {
                    buffer[count++] = character;
                }
            }

            return new string(buffer, 0, count);
        }

        private static bool IsPracticeStage(TutorialStage stage)
        {
            return stage == TutorialStage.BackPractice ||
                   stage == TutorialStage.CaretPractice ||
                   stage == TutorialStage.LetterPractice ||
                   stage == TutorialStage.HintPractice ||
                   stage == TutorialStage.FixedPractice ||
                   stage == TutorialStage.FinalPractice;
        }
    }
}
