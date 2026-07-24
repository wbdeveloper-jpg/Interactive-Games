using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OddSuckMechanic
{
    public enum OddSuckTutorialContentMode
    {
        AutoFromGameMode,
        TextBased,
        ImageBased
    }

    public enum OddSuckTutorialMixedModeFallback
    {
        TextBased,
        ImageBased
    }

    [Serializable]
    public class OddSuckTutorialOption
    {
        public string text;
        public Sprite image;
        public bool isCorrect;

        public OddSuckTutorialOption()
        {
        }

        public OddSuckTutorialOption(string text, bool isCorrect)
        {
            this.text = text;
            this.isCorrect = isCorrect;
        }
    }

    [Serializable]
    public class OddSuckTutorialPracticeContent
    {
        [TextArea(1, 3)] public string question = "Find the one that is different";
        public List<OddSuckTutorialOption> options = new List<OddSuckTutorialOption>
        {
            new OddSuckTutorialOption("Apple", false),
            new OddSuckTutorialOption("Apple", false),
            new OddSuckTutorialOption("Car", true)
        };

        public bool HasEnoughOptions => options != null && options.Count >= 2;
    }

    public class OddSuckFirstTimeTutorialController : MonoBehaviour
    {
        private enum TutorialStage
        {
            Inactive,
            QuestionFocus,
            OptionTour,
            GuessPrompt,
            CorrectReveal,
            UfoMovement,
            Demonstration,
            GuidedPractice,
            IndependentPractice,
            Completed
        }

        [Header("Core References")]
        [SerializeField] private RectTransform tutorialRoot;
        [SerializeField] private CanvasGroup tutorialCanvasGroup;
        [SerializeField] private Button tutorialInputButton;
        [SerializeField] private Image focusDimImage;
        [SerializeField] private RectTransform instructionCard;
        [SerializeField] private CanvasGroup instructionCanvasGroup;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Image handPointerImage;

        [Header("Scene Gameplay References")]
        [SerializeField] private OddSuckManager manager;
        [SerializeField] private OddSuckUfoAutoMover ufoMover;
        [SerializeField] private RectTransform ufoMoveTransform;
        [SerializeField] private RectTransform ufoVisualTransform;
        [SerializeField] private RectTransform beamTransform;
        [SerializeField] private CanvasGroup beamCanvasGroup;
        [SerializeField] private OddSuckPullVisualController pullVisualController;
        [SerializeField] private RectTransform practiceItemParent;
        [SerializeField] private TMP_Text gameplayQuestionText;

        [Header("Scene Item Templates")]
        [SerializeField] private OddSuckItemView fallbackItemTemplate;
        [SerializeField] private OddSuckItemView leftTextItemTemplate;
        [SerializeField] private OddSuckItemView centerTextItemTemplate;
        [SerializeField] private OddSuckItemView rightTextItemTemplate;
        [SerializeField] private OddSuckItemView imageItemTemplate;

        [Header("Mode and Scene-Specific Practice Content")]
        [SerializeField] private OddSuckTutorialContentMode contentMode = OddSuckTutorialContentMode.AutoFromGameMode;
        [SerializeField] private OddSuckTutorialMixedModeFallback mixedModeFallback = OddSuckTutorialMixedModeFallback.ImageBased;
        [SerializeField] private OddSuckTutorialPracticeContent guidedPractice = new OddSuckTutorialPracticeContent();
        [SerializeField] private OddSuckTutorialPracticeContent independentPractice = new OddSuckTutorialPracticeContent
        {
            question = "Find the different one",
            options = new List<OddSuckTutorialOption>
            {
                new OddSuckTutorialOption("Dog", false),
                new OddSuckTutorialOption("Bird", true),
                new OddSuckTutorialOption("Dog", false)
            }
        };

        [Header("Child-Friendly Instructions")]
        [SerializeField] private string questionFocusInstruction = "Look at the question.";
        [SerializeField] private string optionTourInstruction = "Look at each option.";
        [SerializeField] private string guessInstruction = "Which one is different? Take a guess!";
        [SerializeField] private string correctRevealInstruction = "This one is different.";
        [SerializeField] private string ufoInstruction = "The UFO moves by itself.";
        [SerializeField] private string demonstrationInstruction = "Tap when the UFO is above it.";
        [SerializeField] private string guidedInstruction = "Now you try!";
        [SerializeField] private string independentInstruction = "Great! Try one by yourself.";
        [SerializeField] private string correctMessage = "Great job!";
        [SerializeField] private string wrongMessage = "Try the different one.";
        [SerializeField] private string noAlignmentMessage = "Wait for the UFO!";
        [SerializeField] private string completionMessage = "Great job! You're ready!\nThe real game starts now.";
        [SerializeField] private string continueInstruction = "Tap anywhere to continue.";

        [Header("Practice Layout")]
        [SerializeField] private List<Vector2> practicePositions = new List<Vector2>
        {
            new Vector2(-300f, 0f),
            new Vector2(0f, 0f),
            new Vector2(300f, 0f)
        };
        [SerializeField, Min(0f)] private float automaticLayoutPadding = 100f;
        [SerializeField, Min(10f)] private float practiceCatchZoneWidth = 120f;

        [Header("Hand Pointer")]
        [SerializeField] private Vector2 handTargetOffset = new Vector2(35f, 55f);
        [SerializeField] private Vector2 questionHandTargetOffset = new Vector2(35f, -50f);
        [SerializeField, Min(0.05f)] private float handTapDuration = 0.22f;
        [SerializeField, Range(0.5f, 1f)] private float handPressedScale = 0.82f;
        [SerializeField, Min(1f)] private float independentHintDelay = 8f;

        [Header("Smooth Tutorial Transitions")]
        [SerializeField, Min(0.1f)] private float handMoveDuration = 0.42f;
        [SerializeField, Range(1f, 1.15f)] private float optionFocusScale = 1.06f;
        [SerializeField, Min(0.1f)] private float optionScaleDuration = 0.24f;
        [SerializeField, Min(0.05f)] private float instructionFadeOutDuration = 0.16f;
        [SerializeField, Min(0.05f)] private float instructionFadeInDuration = 0.28f;
        [SerializeField, Range(45, 85)] private int continueTextSizePercent = 62;

        [Header("Demonstration and Timing")]
        [SerializeField, Min(1)] private int demonstrationRepeats = 2;
        [SerializeField, Range(0.1f, 1f)] private float tutorialUfoSpeedMultiplier = 0.45f;
        [SerializeField, Min(0.05f)] private float pullVisualOpenDuration = 0.18f;
        [SerializeField, Min(0.1f)] private float demonstratedPullDuration = 0.72f;
        [SerializeField, Min(0.05f)] private float feedbackHoldDuration = 0.8f;
        [SerializeField, Min(0.2f)] private float completionHoldDuration = 2.5f;
        [SerializeField, Min(0.05f)] private float completionFadeOutDuration = 0.35f;
        [SerializeField, Min(0.8f)] private float optionFocusDuration = 1.35f;
        [SerializeField, Range(0.05f, 1f)] private float ghostTransparency = 0.48f;

        [Header("Responsive Instruction Placement")]
        [SerializeField] private bool useResponsiveInstructionPlacement = true;
        [SerializeField] private Vector2 instructionCardAnchor = new Vector2(0.5f, 0.56f);
        [SerializeField, Min(20f)] private float instructionHorizontalScreenPadding = 60f;
        [SerializeField, Min(20f)] private float instructionVerticalScreenPadding = 40f;
        [SerializeField] private Vector2 preferredInstructionCardSize = new Vector2(740f, 180f);

        [Header("Optional Screen Dimming")]
        [SerializeField] private bool useDimOverlay = false;
        [SerializeField, Range(0f, 0.75f)] private float dimAlpha = 0f;

        [Header("Testing")]
        [SerializeField] private bool forcePlayForTesting;

        private readonly List<OddSuckItemView> practiceItems = new List<OddSuckItemView>();
        private TutorialStage stage;
        private OddSuckItemView correctPracticeItem;
        private Action completionCallback;
        private Coroutine stageRoutine;
        private Tween instructionTween;
        private Tween instructionTransitionTween;
        private Tween handTween;
        private Tween feedbackTween;
        private Transform currentHandTarget;
        private Vector2 currentHandTargetOffset;
        private string originalQuestionText;
        private bool originalQuestionActive;
        private bool initialized;
        private bool tutorialRunning;
        private bool usingImageContent;
        private bool inputLocked;
        private float lastInteractionTime;

        private RectTransform Root => tutorialRoot != null ? tutorialRoot : transform as RectTransform;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            if (!tutorialRunning)
            {
                return;
            }

            UpdateHandPosition();

            if (stage == TutorialStage.GuidedPractice)
            {
                UpdateAlignmentHint(true);
            }
            else if (stage == TutorialStage.IndependentPractice)
            {
                bool hintReady = Time.unscaledTime - lastInteractionTime >= independentHintDelay;
                UpdateAlignmentHint(hintReady);
            }
        }

        private void OnDisable()
        {
            KillVisualTweens();
        }

        private void OnDestroy()
        {
            if (tutorialInputButton != null)
            {
                tutorialInputButton.onClick.RemoveListener(HandleTutorialInput);
            }

            KillVisualTweens();
        }

        public bool ShouldPlayAutomatically()
        {
            return forcePlayForTesting || PlayerPrefs.GetInt(GetCompletionPrefsKey(), 0) == 0;
        }

        public void BeginTutorial(OddSuckManager owningManager, Action onCompleted, bool forcePlay = false)
        {
            InitializeIfNeeded();

            if (!forcePlay && !ShouldPlayAutomatically())
            {
                onCompleted?.Invoke();
                return;
            }

            manager = owningManager != null ? owningManager : manager;
            completionCallback = onCompleted;
            tutorialRunning = true;
            inputLocked = false;
            lastInteractionTime = Time.unscaledTime;

            CaptureQuestionState();
            SetRootVisible(true);
            SetInputEnabled(true);
            SetHandVisible(false);
            HidePullVisual(true);

            if (ufoMover != null)
            {
                ufoMover.SetMovementEnabled(false);
                ufoMover.SetSpeedMultiplier(tutorialUfoSpeedMultiplier);
                ufoMover.ResetToCenter();
            }

            ShowQuestionFocusStage();
        }

        [ContextMenu("Reset Saved Tutorial Completion")]
        public void ResetSavedCompletion()
        {
            PlayerPrefs.DeleteKey(GetCompletionPrefsKey());
            PlayerPrefs.Save();
        }

        [ContextMenu("Force Play Tutorial Now")]
        public void ForcePlayNow()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("OddSuck tutorial: Force Play is available while the game is running.", this);
                return;
            }

            ResolveManager();
            manager?.ForcePlayFirstTimeTutorial();
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            ResolveManager();
            EnsureInstructionCardLayout();

            if (tutorialInputButton != null)
            {
                tutorialInputButton.onClick.RemoveListener(HandleTutorialInput);
                tutorialInputButton.onClick.AddListener(HandleTutorialInput);
            }

            if (focusDimImage != null)
            {
                Color color = focusDimImage.color;
                color.a = useDimOverlay ? dimAlpha : 0f;
                focusDimImage.color = color;
                focusDimImage.raycastTarget = false;
                focusDimImage.gameObject.SetActive(useDimOverlay && dimAlpha > 0f);
            }

            if (handPointerImage != null)
            {
                handPointerImage.raycastTarget = false;
                handPointerImage.preserveAspect = true;
            }
        }

        private void EnsureInstructionCardLayout()
        {
            if (instructionCard != null && useResponsiveInstructionPlacement)
            {
                Vector2 safeAnchor = new Vector2(
                    Mathf.Clamp01(instructionCardAnchor.x),
                    Mathf.Clamp(instructionCardAnchor.y, 0.2f, 0.8f));

                instructionCard.anchorMin = safeAnchor;
                instructionCard.anchorMax = safeAnchor;
                instructionCard.pivot = new Vector2(0.5f, 0.5f);
                instructionCard.anchoredPosition = Vector2.zero;

                float availableWidth = Root != null && Root.rect.width > 1f
                    ? Mathf.Max(300f, Root.rect.width - instructionHorizontalScreenPadding * 2f)
                    : preferredInstructionCardSize.x;
                float availableHeight = Root != null && Root.rect.height > 1f
                    ? Mathf.Max(140f, Root.rect.height - instructionVerticalScreenPadding * 2f)
                    : preferredInstructionCardSize.y;

                instructionCard.sizeDelta = new Vector2(
                    Mathf.Min(preferredInstructionCardSize.x, availableWidth),
                    Mathf.Min(Mathf.Max(170f, preferredInstructionCardSize.y), availableHeight));
            }

            if (instructionText != null)
            {
                instructionText.enableAutoSizing = true;
                instructionText.fontSizeMin = 26f;
                instructionText.fontSizeMax = 40f;
                instructionText.enableWordWrapping = true;
            }
        }

        private void ResolveManager()
        {
            if (manager == null)
            {
                manager = FindObjectOfType<OddSuckManager>();
            }
        }

        private void ShowQuestionFocusStage()
        {
            stage = TutorialStage.QuestionFocus;
            usingImageContent = ResolveImageContentMode(guidedPractice);
            if (!SpawnPractice(guidedPractice, usingImageContent))
            {
                AbortWithoutSaving("Tutorial needs at least two options and a valid item template.");
                return;
            }

            correctPracticeItem?.MarkSelected(false);
            SetInstruction(WithContinue(questionFocusInstruction), true);
            SetHandTarget(gameplayQuestionText != null ? gameplayQuestionText.transform : null, false, questionHandTargetOffset);
            StartHandPointPulse();
        }

        private void StartOptionTourStage()
        {
            stage = TutorialStage.OptionTour;
            SetInputEnabled(false);
            SetInstruction(optionTourInstruction, true);
            StartStageRoutine(OptionTourRoutine());
        }

        private IEnumerator OptionTourRoutine()
        {
            for (int i = 0; i < practiceItems.Count; i++)
            {
                OddSuckItemView item = practiceItems[i];
                if (item == null)
                {
                    continue;
                }

                yield return MoveHandSmoothlyTo(item.transform, handTargetOffset);
                SmoothFocusOption(item, true);
                StartHandPointPulse();
                yield return new WaitForSecondsRealtime(Mathf.Max(1.2f, optionFocusDuration));
                SmoothFocusOption(item, false);
                yield return new WaitForSecondsRealtime(optionScaleDuration);
            }

            SetHandVisible(false);
            ShowGuessPromptStage();
        }

        private void ShowGuessPromptStage()
        {
            stage = TutorialStage.GuessPrompt;
            SetInstruction(WithContinue(guessInstruction), true);
            SetHandVisible(false);
            SetInputEnabled(true);
        }

        private void ShowCorrectRevealStage()
        {
            stage = TutorialStage.CorrectReveal;
            correctPracticeItem?.MarkSelected(true);
            SetInstruction(WithContinue(correctRevealInstruction), true);
            SetHandTarget(correctPracticeItem != null ? correctPracticeItem.transform : null, false);
            StartHandPointPulse();
        }

        private void ShowUfoMovementStage()
        {
            stage = TutorialStage.UfoMovement;
            correctPracticeItem?.MarkSelected(false);
            SetInstruction(WithContinue(ufoInstruction), true);
            ufoMover?.SetMovementEnabled(true);
            SetHandVisible(false);
        }

        private void StartDemonstrationStage()
        {
            stage = TutorialStage.Demonstration;
            SetInputEnabled(false);
            SetHandVisible(false);
            SetInstruction(demonstrationInstruction, true);
            StartStageRoutine(DemonstrationRoutine());
        }

        private IEnumerator DemonstrationRoutine()
        {
            int repeats = Mathf.Max(1, demonstrationRepeats);
            for (int repeat = 0; repeat < repeats; repeat++)
            {
                ufoMover?.SetMovementEnabled(true);
                yield return WaitForCorrectAlignment();

                if (!tutorialRunning || correctPracticeItem == null)
                {
                    yield break;
                }

                ufoMover?.SetMovementEnabled(false);
                correctPracticeItem.MarkSelected(true);
                SetHandTarget(correctPracticeItem.transform, true);
                yield return PlaySingleHandTap();
                yield return PlayDemonstratedGhostPull(correctPracticeItem);
                correctPracticeItem.MarkSelected(false);
                SetHandVisible(false);

                if (repeat < repeats - 1)
                {
                    ufoMover?.SetMovementEnabled(true);
                    yield return WaitUntilUfoMovesAway(2.1f, 4f);
                }
            }

            ShowGuidedPracticeStage();
        }

        private void ShowGuidedPracticeStage()
        {
            stage = TutorialStage.GuidedPractice;
            lastInteractionTime = Time.unscaledTime;
            SetInstruction(guidedInstruction, true);
            SetHandVisible(false);
            SetInputEnabled(true);
            ufoMover?.SetMovementEnabled(true);
        }

        private void ShowIndependentPracticeStage()
        {
            stage = TutorialStage.IndependentPractice;
            lastInteractionTime = Time.unscaledTime;
            SetHandVisible(false);
            HidePullVisual(true);
            OddSuckTutorialPracticeContent content = GetIndependentContent(usingImageContent);
            usingImageContent = ResolveImageContentMode(content);

            if (!SpawnPractice(content, usingImageContent))
            {
                AbortWithoutSaving("Independent tutorial practice could not be created.");
                return;
            }

            SetInstruction(independentInstruction, true);
            SetInputEnabled(true);
            ufoMover?.SetMovementEnabled(true);
        }

        private void HandleTutorialInput()
        {
            if (!tutorialRunning || inputLocked)
            {
                return;
            }

            lastInteractionTime = Time.unscaledTime;

            switch (stage)
            {
                case TutorialStage.QuestionFocus:
                    StartOptionTourStage();
                    break;
                case TutorialStage.GuessPrompt:
                    ShowCorrectRevealStage();
                    break;
                case TutorialStage.CorrectReveal:
                    ShowUfoMovementStage();
                    break;
                case TutorialStage.UfoMovement:
                    StartDemonstrationStage();
                    break;
                case TutorialStage.GuidedPractice:
                case TutorialStage.IndependentPractice:
                    EvaluatePracticeTap();
                    break;
            }
        }

        private void EvaluatePracticeTap()
        {
            OddSuckItemView alignedItem = FindAlignedPracticeItem();
            if (alignedItem == null)
            {
                ShowTemporaryFeedback(noAlignmentMessage);
                ufoMover?.PlayWrongUfoAnimation(ufoVisualTransform, null);
                return;
            }

            if (alignedItem != correctPracticeItem)
            {
                ShowTemporaryFeedback(wrongMessage);
                alignedItem.RectTransform?.DOKill();
                alignedItem.RectTransform?.DOPunchAnchorPos(Vector2.down * 32f, 0.28f, 8, 0.75f).SetUpdate(true).SetLink(alignedItem.gameObject);
                ufoMover?.PlayWrongUfoAnimation(ufoVisualTransform, null);
                return;
            }

            inputLocked = true;
            SetInputEnabled(false);
            SetHandVisible(false);
            ufoMover?.SetMovementEnabled(false);
            StartStageRoutine(CompleteSuccessfulPracticeTap(alignedItem));
        }

        private IEnumerator CompleteSuccessfulPracticeTap(OddSuckItemView selectedItem)
        {
            SetInstruction(correctMessage, true);
            yield return PullTutorialItem(selectedItem, false);
            yield return new WaitForSecondsRealtime(feedbackHoldDuration);

            inputLocked = false;
            if (stage == TutorialStage.GuidedPractice)
            {
                ShowIndependentPracticeStage();
            }
            else
            {
                ShowCompletionStage();
            }
        }

        private void ShowCompletionStage()
        {
            stage = TutorialStage.Completed;
            tutorialRunning = false;
            SetInputEnabled(false);
            SetHandVisible(false);
            ufoMover?.SetMovementEnabled(false);
            ClearPracticeItems();
            HidePullVisual(true);
            SetInstruction(completionMessage, true);
            StartStageRoutine(CompletionRoutine());
        }

        private IEnumerator CompletionRoutine()
        {
            yield return new WaitForSecondsRealtime(completionHoldDuration);

            if (tutorialCanvasGroup != null && completionFadeOutDuration > 0f)
            {
                bool fadeComplete = false;
                tutorialCanvasGroup.DOFade(0f, completionFadeOutDuration)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true)
                    .OnComplete(() => fadeComplete = true)
                    .SetLink(gameObject);

                while (!fadeComplete)
                {
                    yield return null;
                }
            }

            PlayerPrefs.SetInt(GetCompletionPrefsKey(), 1);
            PlayerPrefs.Save();

            Action callback = completionCallback;
            completionCallback = null;
            RestoreQuestionState();
            stage = TutorialStage.Inactive;
            KillVisualTweens();
            SetRootVisible(false);
            callback?.Invoke();
        }

        private bool SpawnPractice(OddSuckTutorialPracticeContent content, bool imageMode)
        {
            ClearPracticeItems();

            if (content == null || !content.HasEnoughOptions || practiceItemParent == null)
            {
                return false;
            }

            if (gameplayQuestionText != null)
            {
                gameplayQuestionText.gameObject.SetActive(true);
                gameplayQuestionText.text = string.IsNullOrWhiteSpace(content.question) ? "Find the different one" : content.question;
            }

            int correctIndex = FindCorrectIndex(content.options);
            int count = content.options.Count;

            for (int i = 0; i < count; i++)
            {
                OddSuckItemTemplateSide side = GetTemplateSide(i, count, imageMode);
                OddSuckItemView template = GetTemplate(imageMode, side);
                if (template == null)
                {
                    ClearPracticeItems();
                    return false;
                }

                OddSuckTutorialOption option = content.options[i] ?? new OddSuckTutorialOption();
                OddSuckItemView view = Instantiate(template, practiceItemParent);
                view.name = "TutorialDummyItem_" + i;
                view.gameObject.SetActive(true);
                view.Setup(new OddSuckItemData
                {
                    displayText = option.text,
                    icon = imageMode ? option.image : null,
                    isOdd = i == correctIndex
                }, imageMode ? OddSuckItemDisplayMode.Sprite : OddSuckItemDisplayMode.Text, false, side);

                view.RectTransform.anchoredPosition = GetPracticePosition(i, count);
                view.PlaySpawn(i * 0.04f, false, 0f, 0.24f);
                practiceItems.Add(view);

                if (i == correctIndex)
                {
                    correctPracticeItem = view;
                }
            }

            return practiceItems.Count >= 2 && correctPracticeItem != null;
        }

        private OddSuckItemView GetTemplate(bool imageMode, OddSuckItemTemplateSide side)
        {
            if (imageMode)
            {
                return imageItemTemplate != null ? imageItemTemplate : fallbackItemTemplate;
            }

            switch (side)
            {
                case OddSuckItemTemplateSide.Left:
                    return leftTextItemTemplate != null ? leftTextItemTemplate : GetFallbackTextTemplate();
                case OddSuckItemTemplateSide.Right:
                    return rightTextItemTemplate != null ? rightTextItemTemplate : GetFallbackTextTemplate();
                default:
                    return centerTextItemTemplate != null ? centerTextItemTemplate : GetFallbackTextTemplate();
            }
        }

        private OddSuckItemView GetFallbackTextTemplate()
        {
            return centerTextItemTemplate != null ? centerTextItemTemplate : fallbackItemTemplate;
        }

        private static OddSuckItemTemplateSide GetTemplateSide(int index, int count, bool imageMode)
        {
            if (imageMode)
            {
                return OddSuckItemTemplateSide.ImageMode;
            }

            if (index == 0)
            {
                return OddSuckItemTemplateSide.Left;
            }

            if (index == count - 1)
            {
                return OddSuckItemTemplateSide.Right;
            }

            return OddSuckItemTemplateSide.Center;
        }

        private Vector2 GetPracticePosition(int index, int count)
        {
            if (practicePositions != null && practicePositions.Count >= count)
            {
                return practicePositions[index];
            }

            Rect rect = practiceItemParent.rect;
            float minX = rect.xMin + automaticLayoutPadding;
            float maxX = rect.xMax - automaticLayoutPadding;
            float t = count <= 1 ? 0.5f : index / (float)(count - 1);
            return new Vector2(Mathf.Lerp(minX, maxX, t), 0f);
        }

        private static int FindCorrectIndex(List<OddSuckTutorialOption> options)
        {
            if (options == null || options.Count == 0)
            {
                return -1;
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (options[i] != null && options[i].isCorrect)
                {
                    return i;
                }
            }

            return 0;
        }

        private OddSuckItemView FindAlignedPracticeItem()
        {
            RectTransform reference = beamTransform != null ? beamTransform : ufoMoveTransform;
            if (reference == null || practiceItemParent == null)
            {
                return null;
            }

            float referenceX = practiceItemParent.InverseTransformPoint(reference.position).x;
            float allowedDistance = practiceCatchZoneWidth * 0.5f;
            float bestDistance = float.MaxValue;
            OddSuckItemView best = null;

            for (int i = 0; i < practiceItems.Count; i++)
            {
                OddSuckItemView item = practiceItems[i];
                if (item == null || !item.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float itemX = practiceItemParent.InverseTransformPoint(item.RectTransform.position).x;
                float distance = Mathf.Abs(referenceX - itemX);
                if (distance <= allowedDistance && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }

            return best;
        }

        private bool IsCorrectItemAligned(float widthMultiplier = 1f)
        {
            if (correctPracticeItem == null || practiceItemParent == null)
            {
                return false;
            }

            RectTransform reference = beamTransform != null ? beamTransform : ufoMoveTransform;
            if (reference == null)
            {
                return false;
            }

            float referenceX = practiceItemParent.InverseTransformPoint(reference.position).x;
            float itemX = practiceItemParent.InverseTransformPoint(correctPracticeItem.RectTransform.position).x;
            return Mathf.Abs(referenceX - itemX) <= practiceCatchZoneWidth * 0.5f * Mathf.Max(1f, widthMultiplier);
        }

        private IEnumerator WaitForCorrectAlignment()
        {
            while (tutorialRunning && correctPracticeItem != null && !IsCorrectItemAligned())
            {
                yield return null;
            }
        }

        private IEnumerator WaitUntilUfoMovesAway(float widthMultiplier, float timeout)
        {
            float elapsed = 0f;
            while (tutorialRunning && IsCorrectItemAligned(widthMultiplier) && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator PlaySingleHandTap()
        {
            if (!CanShowHand())
            {
                yield return new WaitForSecondsRealtime(handTapDuration * 2f);
                yield break;
            }

            handTween?.Kill();
            RectTransform handRect = handPointerImage.rectTransform;
            handRect.localScale = Vector3.one;
            bool complete = false;
            handTween = DOTween.Sequence()
                .Append(handRect.DOScale(Vector3.one * handPressedScale, handTapDuration).SetEase(Ease.InOutSine))
                .Append(handRect.DOScale(Vector3.one, handTapDuration).SetEase(Ease.OutBack))
                .OnComplete(() => complete = true)
                .SetUpdate(true)
                .SetLink(handPointerImage.gameObject);

            while (!complete && tutorialRunning)
            {
                yield return null;
            }
        }

        private IEnumerator PlayDemonstratedGhostPull(OddSuckItemView source)
        {
            if (source == null)
            {
                yield break;
            }

            OddSuckItemView ghost = Instantiate(source, practiceItemParent);
            ghost.name = "TutorialPullGhost";
            ghost.gameObject.SetActive(true);
            ghost.MarkSelected(false);
            ghost.transform.position = source.transform.position;
            ghost.transform.localScale = source.transform.localScale;

            CanvasGroup group = ghost.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = ghost.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = ghostTransparency;
            group.blocksRaycasts = false;
            group.interactable = false;

            yield return PullTutorialItem(ghost, true);
        }

        private IEnumerator PullTutorialItem(OddSuckItemView item, bool destroyWhenDone)
        {
            if (item == null || ufoMoveTransform == null)
            {
                yield break;
            }

            float openDuration = GetVisualOpenDuration();
            float pullDuration = GetVisualPullDuration();
            ShowPullVisual(item, openDuration);
            yield return new WaitForSecondsRealtime(openDuration);
            ActivatePullVisual(item, pullDuration);

            bool complete = false;
            Sequence pull = DOTween.Sequence();
            pull.Join(item.transform.DOMove(ufoMoveTransform.position, pullDuration).SetEase(Ease.InBack));
            pull.Join(item.transform.DOScale(Vector3.one * 0.14f, pullDuration).SetEase(Ease.InBack));
            pull.OnComplete(() => complete = true).SetUpdate(true).SetLink(item.gameObject);

            while (!complete && item != null)
            {
                yield return null;
            }

            HidePullVisual(false);
            if (item != null)
            {
                item.gameObject.SetActive(false);
                if (destroyWhenDone)
                {
                    Destroy(item.gameObject);
                }
            }
        }

        private float GetVisualOpenDuration()
        {
            if (pullVisualController != null && manager != null)
            {
                return pullVisualController.GetPullStartDuration(manager.PullVisualStyle, pullVisualOpenDuration);
            }

            return pullVisualOpenDuration;
        }

        private float GetVisualPullDuration()
        {
            if (pullVisualController != null && manager != null)
            {
                return pullVisualController.GetItemPullDuration(manager.PullVisualStyle, demonstratedPullDuration);
            }

            return demonstratedPullDuration;
        }

        private void ShowPullVisual(OddSuckItemView item, float duration)
        {
            if (pullVisualController != null && manager != null)
            {
                pullVisualController.PlayPullStart(manager.PullVisualStyle, item.RectTransform, ufoMoveTransform, false, duration);
                return;
            }

            SetFallbackBeamVisible(true, false, duration);
        }

        private void ActivatePullVisual(OddSuckItemView item, float duration)
        {
            if (pullVisualController != null && manager != null)
            {
                pullVisualController.PlayPullActive(manager.PullVisualStyle, item.RectTransform, ufoMoveTransform, duration);
            }
        }

        private void HidePullVisual(bool instant)
        {
            if (pullVisualController != null && manager != null)
            {
                pullVisualController.HidePullVisual(manager.PullVisualStyle, instant, pullVisualOpenDuration);
                return;
            }

            SetFallbackBeamVisible(false, instant, pullVisualOpenDuration);
        }

        private void SetFallbackBeamVisible(bool visible, bool instant, float duration)
        {
            if (beamTransform == null)
            {
                return;
            }

            beamTransform.DOKill();
            if (instant)
            {
                beamTransform.gameObject.SetActive(visible);
                beamTransform.localScale = visible ? Vector3.one : new Vector3(1f, 0f, 1f);
                if (beamCanvasGroup != null)
                {
                    beamCanvasGroup.alpha = visible ? 1f : 0f;
                }
                return;
            }

            if (visible)
            {
                beamTransform.gameObject.SetActive(true);
                beamTransform.localScale = new Vector3(1f, 0f, 1f);
                beamTransform.DOScaleY(1f, duration).SetEase(Ease.OutBack).SetUpdate(true).SetLink(beamTransform.gameObject);
                if (beamCanvasGroup != null)
                {
                    DOTween.To(() => beamCanvasGroup.alpha, value => beamCanvasGroup.alpha = value, 1f, duration).SetUpdate(true).SetLink(beamTransform.gameObject);
                }
            }
            else
            {
                beamTransform.DOScaleY(0f, duration).SetEase(Ease.InBack).SetUpdate(true)
                    .OnComplete(() => beamTransform.gameObject.SetActive(false)).SetLink(beamTransform.gameObject);
                if (beamCanvasGroup != null)
                {
                    DOTween.To(() => beamCanvasGroup.alpha, value => beamCanvasGroup.alpha = value, 0f, duration).SetUpdate(true).SetLink(beamTransform.gameObject);
                }
            }
        }

        private void UpdateAlignmentHint(bool allowed)
        {
            if (!allowed || !IsCorrectItemAligned())
            {
                SetHandVisible(false);
                return;
            }

            SetHandTarget(correctPracticeItem != null ? correctPracticeItem.transform : null, true);
        }

        private void SetHandTarget(Transform target, bool tapLoop)
        {
            SetHandTarget(target, tapLoop, handTargetOffset);
        }

        private void SetHandTarget(Transform target, bool tapLoop, Vector2 targetOffset)
        {
            currentHandTarget = target;
            currentHandTargetOffset = targetOffset;
            UpdateHandPosition();
            SetHandVisible(target != null);

            if (!tapLoop || !CanShowHand())
            {
                handTween?.Kill();
                return;
            }

            if (handTween != null && handTween.IsActive() && handTween.IsPlaying())
            {
                return;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            handRect.localScale = Vector3.one;
            handTween = DOTween.Sequence()
                .Append(handRect.DOScale(Vector3.one * handPressedScale, handTapDuration).SetEase(Ease.InOutSine))
                .Append(handRect.DOScale(Vector3.one, handTapDuration).SetEase(Ease.OutBack))
                .AppendInterval(0.35f)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetLink(handPointerImage.gameObject);
        }

        private IEnumerator MoveHandSmoothlyTo(Transform target, Vector2 targetOffset)
        {
            if (target == null || !CanShowHand())
            {
                yield break;
            }

            handTween?.Kill();
            currentHandTarget = null;
            currentHandTargetOffset = targetOffset;
            SetHandVisible(true);

            RectTransform handRect = handPointerImage.rectTransform;
            Vector3 worldOffset = Root != null ? Root.TransformVector(targetOffset) : (Vector3)targetOffset;
            Vector3 destination = target.position + worldOffset;
            bool complete = false;

            handTween = handRect.DOMove(destination, handMoveDuration)
                .SetEase(Ease.InOutCubic)
                .SetUpdate(true)
                .OnComplete(() => complete = true)
                .SetLink(handPointerImage.gameObject);

            while (!complete && tutorialRunning)
            {
                yield return null;
            }

            currentHandTarget = target;
            currentHandTargetOffset = targetOffset;
            UpdateHandPosition();
        }

        private void SmoothFocusOption(OddSuckItemView item, bool focused)
        {
            if (item == null)
            {
                return;
            }

            item.transform.DOKill();
            float targetScale = focused ? optionFocusScale : 1f;
            item.transform.DOScale(Vector3.one * targetScale, optionScaleDuration)
                .SetEase(focused ? Ease.OutCubic : Ease.InOutCubic)
                .SetUpdate(true)
                .SetLink(item.gameObject);
        }

        private void StartHandPointPulse()
        {
            if (!CanShowHand())
            {
                return;
            }

            handTween?.Kill();
            RectTransform handRect = handPointerImage.rectTransform;
            handRect.localScale = Vector3.one;
            handTween = handRect.DOScale(Vector3.one * 1.07f, 0.42f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(handPointerImage.gameObject);
        }

        private void UpdateHandPosition()
        {
            if (!CanShowHand() || currentHandTarget == null)
            {
                return;
            }

            RectTransform handRect = handPointerImage.rectTransform;
            handRect.position = currentHandTarget.position;
            handRect.anchoredPosition += currentHandTargetOffset;
        }

        private bool CanShowHand()
        {
            return handPointerImage != null && handPointerImage.sprite != null;
        }

        private void SetHandVisible(bool visible)
        {
            bool finalVisible = visible && CanShowHand();
            if (handPointerImage != null)
            {
                handPointerImage.gameObject.SetActive(finalVisible);
                if (!finalVisible)
                {
                    currentHandTarget = null;
                    handTween?.Kill();
                    handPointerImage.rectTransform.localScale = Vector3.one;
                }
            }
        }

        private void SetInstruction(string message, bool breathe)
        {
            EnsureInstructionCardLayout();
            feedbackTween?.Kill();
            instructionTween?.Kill();
            instructionTransitionTween?.Kill();

            if (instructionCard == null)
            {
                if (instructionText != null)
                {
                    instructionText.text = message ?? string.Empty;
                }

                return;
            }

            bool wasVisible = instructionCard.gameObject.activeInHierarchy
                && instructionCanvasGroup != null
                && instructionCanvasGroup.alpha > 0.05f
                && instructionText != null
                && !string.IsNullOrWhiteSpace(instructionText.text);

            instructionCard.gameObject.SetActive(true);
            instructionCard.localScale = Vector3.one;

            if (instructionCanvasGroup == null)
            {
                if (instructionText != null)
                {
                    instructionText.text = message ?? string.Empty;
                }

                StartInstructionBreathing(breathe);
                return;
            }

            Sequence transition = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(instructionCard.gameObject);

            if (wasVisible)
            {
                transition.Append(instructionCanvasGroup.DOFade(0f, instructionFadeOutDuration)
                    .SetEase(Ease.InOutSine));
            }
            else
            {
                instructionCanvasGroup.alpha = 0f;
            }

            transition.AppendCallback(() =>
            {
                if (instructionText != null)
                {
                    instructionText.text = message ?? string.Empty;
                }

                instructionCard.localScale = Vector3.one * 0.985f;
            });
            transition.Append(instructionCanvasGroup.DOFade(1f, instructionFadeInDuration)
                .SetEase(Ease.OutSine));
            transition.Join(instructionCard.DOScale(Vector3.one, instructionFadeInDuration)
                .SetEase(Ease.OutCubic));
            transition.OnComplete(() => StartInstructionBreathing(breathe));
            instructionTransitionTween = transition;
        }

        private void StartInstructionBreathing(bool breathe)
        {
            if (!breathe || instructionCard == null)
            {
                return;
            }

            instructionTween = instructionCard.DOScale(Vector3.one * 1.025f, 0.9f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .SetLink(instructionCard.gameObject);
        }

        private void ShowTemporaryFeedback(string message)
        {
            string returnMessage = stage == TutorialStage.GuidedPractice ? guidedInstruction : independentInstruction;
            SetInstruction(message, true);
            feedbackTween = DOVirtual.DelayedCall(feedbackHoldDuration, () => SetInstruction(returnMessage, true), true)
                .SetLink(gameObject);
        }

        private string WithContinue(string message)
        {
            if (string.IsNullOrWhiteSpace(continueInstruction))
            {
                return message;
            }

            int sizePercent = Mathf.Clamp(continueTextSizePercent, 45, 85);
            return message + "\n<size=" + sizePercent + "%>" + continueInstruction + "</size>";
        }

        private bool ResolveImageContentMode(OddSuckTutorialPracticeContent content)
        {
            bool imageMode;
            switch (contentMode)
            {
                case OddSuckTutorialContentMode.TextBased:
                    imageMode = false;
                    break;
                case OddSuckTutorialContentMode.ImageBased:
                    imageMode = true;
                    break;
                default:
                    ResolveManager();
                    if (manager != null && manager.PlayMode == OddSuckPlayMode.SpriteOnly)
                    {
                        imageMode = true;
                    }
                    else if (manager != null && (manager.PlayMode == OddSuckPlayMode.MathOnly || manager.PlayMode == OddSuckPlayMode.EnglishOnly))
                    {
                        imageMode = false;
                    }
                    else
                    {
                        imageMode = mixedModeFallback == OddSuckTutorialMixedModeFallback.ImageBased;
                    }
                    break;
            }

            if (imageMode && !HasEnoughImages(content))
            {
                Debug.LogWarning("OddSuck tutorial: Image mode was selected but fewer than two option images were assigned. Falling back to text practice.", this);
                imageMode = false;
            }

            return imageMode;
        }

        private static bool HasEnoughImages(OddSuckTutorialPracticeContent content)
        {
            if (content == null || content.options == null)
            {
                return false;
            }

            int count = 0;
            for (int i = 0; i < content.options.Count; i++)
            {
                if (content.options[i] != null && content.options[i].image != null)
                {
                    count++;
                }
            }

            return count >= 2 && count == content.options.Count;
        }

        private OddSuckTutorialPracticeContent GetIndependentContent(bool preferImageContent)
        {
            if (independentPractice == null || !independentPractice.HasEnoughOptions)
            {
                return guidedPractice;
            }

            if (preferImageContent && !HasEnoughImages(independentPractice))
            {
                return guidedPractice;
            }

            return independentPractice;
        }

        private void CaptureQuestionState()
        {
            if (gameplayQuestionText == null)
            {
                return;
            }

            originalQuestionText = gameplayQuestionText.text;
            originalQuestionActive = gameplayQuestionText.gameObject.activeSelf;
        }

        private void RestoreQuestionState()
        {
            if (gameplayQuestionText == null)
            {
                return;
            }

            gameplayQuestionText.text = originalQuestionText;
            gameplayQuestionText.gameObject.SetActive(originalQuestionActive);
        }

        private void ClearPracticeItems()
        {
            for (int i = practiceItems.Count - 1; i >= 0; i--)
            {
                if (practiceItems[i] == null)
                {
                    continue;
                }

                practiceItems[i].gameObject.SetActive(false);
                Destroy(practiceItems[i].gameObject);
            }

            practiceItems.Clear();
            correctPracticeItem = null;
        }

        private void SetInputEnabled(bool enabled)
        {
            if (tutorialInputButton != null)
            {
                tutorialInputButton.interactable = enabled;
            }
        }

        private void SetRootVisible(bool visible)
        {
            if (Root == null)
            {
                return;
            }

            Root.gameObject.SetActive(visible);
            if (visible)
            {
                Root.SetAsLastSibling();
            }

            if (tutorialCanvasGroup != null)
            {
                tutorialCanvasGroup.alpha = visible ? 1f : 0f;
                tutorialCanvasGroup.blocksRaycasts = visible;
                tutorialCanvasGroup.interactable = visible;
            }
        }

        private void StartStageRoutine(IEnumerator routine)
        {
            if (stageRoutine != null)
            {
                StopCoroutine(stageRoutine);
            }

            stageRoutine = StartCoroutine(routine);
        }

        private void AbortWithoutSaving(string reason)
        {
            Debug.LogWarning("OddSuck tutorial skipped: " + reason, this);
            tutorialRunning = false;
            stage = TutorialStage.Inactive;
            ClearPracticeItems();
            HidePullVisual(true);
            RestoreQuestionState();

            Action callback = completionCallback;
            completionCallback = null;
            callback?.Invoke();
            SetRootVisible(false);
        }

        private void KillVisualTweens()
        {
            instructionTween?.Kill();
            handTween?.Kill();
            feedbackTween?.Kill();
            instructionTransitionTween?.Kill();
        }

        private static string GetCompletionPrefsKey()
        {
            return $"OddSuck.FirstTimeTutorialCompleted.{SceneManager.GetActiveScene().name}";
        }
    }
}
