using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WordShuffleDragSwap
{
    public enum WordShuffleTutorialContentMode
    {
        AutoFromGameMode,
        EnglishLetters,
        MathsDigits
    }

    public class WordShuffleFirstTimeTutorialController : MonoBehaviour
    {
        private enum TutorialStage
        {
            Inactive,
            QuestionFocus,
            ShuffledLettersFocus,
            SwapDemonstration,
            GuidedSwap,
            PracticeWord,
            HintPractice,
            Complete
        }

        [Header("Tutorial Behaviour")]
        [SerializeField] private bool tutorialEnabled = true;
        [SerializeField] private bool forcePlayForTesting;
        [SerializeField] private WordShuffleTutorialContentMode tutorialContentMode =
            WordShuffleTutorialContentMode.AutoFromGameMode;

        [Header("Game References")]
        [SerializeField] private WordShuffleDragSwapManager gameManager;
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private WordShuffleLetterTile letterTileTemplate;
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private RectTransform questionFocusTarget;
        [SerializeField] private Button hintButton;

        [Header("Tutorial-Owned UI")]
        [SerializeField] private CanvasGroup tutorialCanvasGroup;
        [SerializeField] private Image dimOverlay;
        [SerializeField] private Image focusHighlight;
        [SerializeField] private RectTransform instructionCard;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private Image handPointer;
        [SerializeField] private RectTransform practiceArea;
        [SerializeField] private RectTransform practiceFocusTarget;
        [SerializeField] private RectTransform practiceSlotLayer;
        [SerializeField] private RectTransform practiceTileLayer;

        [Header("Child-Friendly Text")]
        [SerializeField] private string questionInstruction = "Read the clue. It tells you which word to make.\nClick anywhere to continue.";
        [SerializeField] private string lettersInstruction = "These letters are mixed up.\nClick anywhere to continue.";
        [SerializeField] private string demonstrationInstruction = "Drag one letter onto another. They will swap places.";
        [SerializeField] private string guidedSwapInstruction = "Your turn! Drag T onto C.";
        [SerializeField] private string practiceInstruction = "Now make DOG by yourself.";
        [SerializeField] private string hintInstruction = "Tap HINT when you need help.";
        [SerializeField] private string completeInstruction = "Great job! You can swap letters and use a hint.\nClick anywhere to continue.";

        [Header("Maths Tutorial Text")]
        [SerializeField] private string mathsQuestionInstruction =
            "Read the number words. They tell you which number to make.\nClick anywhere to continue.";
        [SerializeField] private string mathsDigitsInstruction =
            "These digits are mixed up.\nClick anywhere to continue.";
        [SerializeField] private string mathsDemonstrationInstruction =
            "Drag one digit onto another. They will swap places.";
        [SerializeField] private string mathsGuidedSwapInstruction =
            "Your turn! Drag 5 onto 3.";
        [SerializeField] private string mathsPracticeInstruction =
            "Now make 6172 by yourself.";
        [SerializeField] private string mathsCompleteInstruction =
            "Great job! You can swap digits and use a hint.\nClick anywhere to continue.";

        [Header("Maths Tutorial Practice")]
        [SerializeField] private string mathsGuidedQuestion =
            "Three Thousand Four Hundred and Twenty-five";
        [SerializeField] private string mathsGuidedShuffled = "5423";
        [SerializeField] private string mathsGuidedAnswer = "3425";
        [SerializeField] private string mathsPracticeQuestion =
            "Six Thousand One Hundred and Seventy-two";
        [SerializeField] private string mathsPracticeShuffled = "2716";
        [SerializeField] private string mathsPracticeAnswer = "6172";
        [SerializeField] private string mathsHintQuestion =
            "Two Thousand Eight Hundred and Forty-six";
        [SerializeField] private string mathsHintShuffled = "6842";
        [SerializeField] private string mathsHintAnswer = "2846";

        [Header("Hand Pointer")]
        [SerializeField] private Vector2 questionHandOffset = new Vector2(0f, -105f);
        [SerializeField] private Vector2 lettersHandOffset = new Vector2(0f, -180f);
        [SerializeField] private Vector2 hintHandOffset = new Vector2(0f, -105f);
        [SerializeField] private Vector2 gestureHandOffset = new Vector2(0f, -72f);
        [SerializeField, Min(0.1f)] private float handMoveDuration = 0.9f;
        [SerializeField, Min(0.05f)] private float handPulseDuration = 0.55f;
        [SerializeField, Min(0f)] private float demonstrationPause = 0.35f;
        [SerializeField, Min(2)] private int demonstrationRepeatCount = 2;
        [SerializeField, Min(3f)] private float guidedReminderDelay = 15f;
        [SerializeField, Range(0.05f, 1f)] private float ghostTransparency = 0.42f;

        [Header("Focus And Instruction Animation")]
        [SerializeField] private Vector2 focusPadding = new Vector2(28f, 22f);
        [SerializeField, Min(0.1f)] private float breathingDuration = 0.75f;
        [SerializeField, Range(1f, 1.15f)] private float breathingScale = 1.035f;

        [Header("Practice Layout")]
        [SerializeField] private Vector2 practiceTileSize = new Vector2(138f, 138f);
        [SerializeField, Min(0f)] private float practiceTileSpacing = 28f;
        [SerializeField] private Sprite practiceSlotSprite;
        [SerializeField] private Color practiceSlotColor = new Color(1f, 1f, 1f, 0.24f);
        [SerializeField] private Color hintedPracticeTileColor = new Color(0.25f, 0.86f, 0.42f, 1f);
        [SerializeField, Min(0.05f)] private float practiceSwapDuration = 0.34f;

        private readonly List<WordShuffleTutorialPracticeTile> practiceTiles =
            new List<WordShuffleTutorialPracticeTile>();

        private readonly List<RectTransform> practiceSlots = new List<RectTransform>();

        private TutorialStage currentStage = TutorialStage.Inactive;
        private Action continueRealGame;
        private string targetAnswer = string.Empty;
        private string originalQuestionText = string.Empty;
        private Vector2 originalInstructionPosition;
        private bool isRunning;
        private bool allowPracticeDrag;
        private bool transitionLocked;
        private float guidedIdleTimer;
        private RectTransform activeFocusTarget;
        private RectTransform stationaryHandTarget;
        private Vector2 stationaryHandOffset;
        private Sequence stageSequence;
        private Tween instructionTween;
        private Tween focusTween;
        private Tween handTween;
        private GameObject ghostTile;

        public bool ShouldPlayTutorial =>
            tutorialEnabled &&
            (forcePlayForTesting || PlayerPrefs.GetInt(GetCompletionKeyForActiveScene(), 0) == 0);

        public RectTransform PracticeTileLayer => practiceTileLayer;

        public Camera UICamera
        {
            get
            {
                if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return null;

                return rootCanvas.worldCamera;
            }
        }

        private RectTransform TutorialRect => transform as RectTransform;

        private bool UseMathsTutorial
        {
            get
            {
                if (tutorialContentMode == WordShuffleTutorialContentMode.MathsDigits)
                    return true;

                return tutorialContentMode == WordShuffleTutorialContentMode.AutoFromGameMode &&
                       gameManager != null &&
                       gameManager.RoundMode == WordShuffleRoundMode.MathLargeNumbers;
            }
        }

        private void OnDisable()
        {
            KillTutorialTweens();
        }

        private void Update()
        {
            if (!isRunning || transitionLocked)
                return;

            if ((currentStage == TutorialStage.QuestionFocus ||
                 currentStage == TutorialStage.ShuffledLettersFocus ||
                 currentStage == TutorialStage.Complete) &&
                Input.GetMouseButtonDown(0))
            {
                HandleAnywhereClick();
                return;
            }

            if (currentStage != TutorialStage.GuidedSwap || !allowPracticeDrag)
                return;

            guidedIdleTimer += Time.unscaledDeltaTime;
            if (guidedIdleTimer >= guidedReminderDelay)
                BeginStage(TutorialStage.SwapDemonstration);
        }

        private void LateUpdate()
        {
            if (!isRunning)
                return;

            if (activeFocusTarget != null && focusHighlight != null)
                PositionOverlayOverTarget(activeFocusTarget, focusHighlight.rectTransform, focusPadding);

            if (stationaryHandTarget != null && handPointer != null && handPointer.gameObject.activeSelf)
                handPointer.rectTransform.anchoredPosition = GetTargetCenter(stationaryHandTarget) + stationaryHandOffset;
        }

        public void BeginTutorial(WordShuffleDragSwapManager owner, Action onCompleted)
        {
            if (isRunning)
                return;

            if (!ShouldPlayTutorial)
            {
                onCompleted?.Invoke();
                return;
            }

            gameObject.SetActive(true);

            gameManager = owner != null ? owner : gameManager;
            continueRealGame = onCompleted;
            isRunning = true;
            transitionLocked = false;
            currentStage = TutorialStage.Inactive;

            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            originalQuestionText = questionText != null ? questionText.text : string.Empty;
            originalInstructionPosition = instructionCard != null
                ? instructionCard.anchoredPosition
                : Vector2.zero;

            if (tutorialCanvasGroup != null)
            {
                tutorialCanvasGroup.alpha = 1f;
                tutorialCanvasGroup.interactable = true;
                tutorialCanvasGroup.blocksRaycasts = true;
            }

            if (gameManager != null)
                gameManager.BeginTutorialHold();

            BeginStage(TutorialStage.QuestionFocus);
        }

        public bool CanDragPracticeTile(WordShuffleTutorialPracticeTile tile)
        {
            return isRunning &&
                   allowPracticeDrag &&
                   !transitionLocked &&
                   tile != null &&
                   !tile.IsLocked &&
                   (currentStage == TutorialStage.GuidedSwap || currentStage == TutorialStage.PracticeWord);
        }

        public void NotifyPracticeDragStarted(WordShuffleTutorialPracticeTile tile)
        {
            if (!CanDragPracticeTile(tile))
                return;

            guidedIdleTimer = 0f;
            HideStationaryHand();
            tile.RectTransform.DOKill();
            tile.RectTransform.DOScale(1.1f, 0.16f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void NotifyPracticeTileDropped(WordShuffleTutorialPracticeTile tile, Vector2 screenPosition)
        {
            if (tile == null)
                return;

            guidedIdleTimer = 0f;

            if (!CanDragPracticeTile(tile))
            {
                SnapPracticeTile(tile);
                return;
            }

            WordShuffleTutorialPracticeTile target = FindPracticeTileAtScreenPosition(screenPosition, tile);
            if (target == null || target.IsLocked)
            {
                SnapPracticeTile(tile);

                if (currentStage == TutorialStage.GuidedSwap)
                    ShowGuidedSwapPointer();

                return;
            }

            SwapPracticeTiles(tile, target, OnMeaningfulPracticeSwapCompleted);
        }

        public void HandleHintButtonPressed()
        {
            if (!isRunning || currentStage != TutorialStage.HintPractice || transitionLocked)
                return;

            transitionLocked = true;
            HideStationaryHand();
            HideFocus();

            if (dimOverlay != null)
                dimOverlay.raycastTarget = true;

            if (gameManager != null)
                gameManager.SetTutorialHintButtonInteractable(false);

            if (!TryFindTutorialHintMove(out WordShuffleTutorialPracticeTile correctTile,
                    out WordShuffleTutorialPracticeTile displacedTile))
            {
                ShowCompleteStageAfterDelay();
                return;
            }

            SwapPracticeTiles(correctTile, displacedTile, () =>
            {
                correctTile.SetLocked(true, hintedPracticeTileColor);
                SetInstruction("Nice! A hint puts one letter in the right place.", false);
                ShowCompleteStageAfterDelay();
            });
        }

        [ContextMenu("Reset Tutorial Completion For Active Scene")]
        public void ResetCompletionForActiveScene()
        {
            PlayerPrefs.DeleteKey(GetCompletionKeyForActiveScene());
            PlayerPrefs.Save();
            Debug.Log($"Reset first-time tutorial for scene '{SceneManager.GetActiveScene().name}'.", this);
        }

        public static string GetCompletionKeyForActiveScene()
        {
            return $"WordShuffle.InteractiveTutorial.Completed.{SceneManager.GetActiveScene().name}";
        }

        private void BeginStage(TutorialStage stage)
        {
            KillStageTweens();
            currentStage = stage;
            transitionLocked = false;
            allowPracticeDrag = false;
            guidedIdleTimer = 0f;

            if (dimOverlay != null)
                dimOverlay.raycastTarget = true;

            if (gameManager != null)
                gameManager.SetTutorialHintButtonInteractable(false);

            HideFocus();
            HideStationaryHand();

            if (instructionCard != null)
                instructionCard.anchoredPosition = originalInstructionPosition;

            switch (stage)
            {
                case TutorialStage.QuestionFocus:
                    ClearPracticeObjects();
                    SetQuestion(GetGuidedQuestion());
                    SetInstruction(UseMathsTutorial ? mathsQuestionInstruction : questionInstruction, true);
                    ShowFocus(questionFocusTarget != null ? questionFocusTarget : questionText?.rectTransform);
                    ShowStationaryHand(questionFocusTarget != null ? questionFocusTarget : questionText?.rectTransform,
                        questionHandOffset);
                    break;

                case TutorialStage.ShuffledLettersFocus:
                    SetupPracticeWord(GetGuidedShuffled(), GetGuidedAnswer());
                    SetInstruction(UseMathsTutorial ? mathsDigitsInstruction : lettersInstruction, true);
                    ShowFocus(practiceFocusTarget != null ? practiceFocusTarget : practiceArea);
                    ShowStationaryHand(practiceFocusTarget != null ? practiceFocusTarget : practiceArea,
                        lettersHandOffset);
                    break;

                case TutorialStage.SwapDemonstration:
                    SetupPracticeWord(GetGuidedShuffled(), GetGuidedAnswer());
                    SetInstruction(
                        UseMathsTutorial ? mathsDemonstrationInstruction : demonstrationInstruction,
                        true);
                    StartGhostSwapDemonstration();
                    break;

                case TutorialStage.GuidedSwap:
                    SetupPracticeWord(GetGuidedShuffled(), GetGuidedAnswer());
                    SetInstruction(UseMathsTutorial ? mathsGuidedSwapInstruction : guidedSwapInstruction, true);
                    allowPracticeDrag = true;
                    SetPracticeTilesRaycastState(true);
                    ShowGuidedSwapPointer();
                    break;

                case TutorialStage.PracticeWord:
                    SetQuestion(GetIndependentPracticeQuestion());
                    SetupPracticeWord(GetIndependentPracticeShuffled(), GetIndependentPracticeAnswer());
                    SetInstruction(UseMathsTutorial ? mathsPracticeInstruction : practiceInstruction, true);
                    allowPracticeDrag = true;
                    SetPracticeTilesRaycastState(true);
                    break;

                case TutorialStage.HintPractice:
                    SetQuestion(GetHintQuestion());
                    SetupPracticeWord(GetHintShuffled(), GetHintAnswer());
                    SetInstruction(hintInstruction, true);
                    ShowFocus(hintButton != null ? hintButton.transform as RectTransform : null);
                    ShowStationaryHand(hintButton != null ? hintButton.transform as RectTransform : null,
                        hintHandOffset);

                    if (dimOverlay != null)
                        dimOverlay.raycastTarget = false;

                    if (gameManager != null)
                        gameManager.SetTutorialHintButtonInteractable(true);
                    break;

                case TutorialStage.Complete:
                    ClearPracticeObjects();
                    SetQuestion(originalQuestionText);
                    SetInstruction(UseMathsTutorial ? mathsCompleteInstruction : completeInstruction, true);

                    if (instructionCard != null)
                        instructionCard.anchoredPosition = Vector2.zero;
                    break;
            }
        }

        private void HandleAnywhereClick()
        {
            if (!isRunning || transitionLocked)
                return;

            switch (currentStage)
            {
                case TutorialStage.QuestionFocus:
                    BeginStage(TutorialStage.ShuffledLettersFocus);
                    break;

                case TutorialStage.ShuffledLettersFocus:
                    BeginStage(TutorialStage.SwapDemonstration);
                    break;

                case TutorialStage.Complete:
                    CompleteTutorial();
                    break;
            }
        }

        private string GetGuidedQuestion()
        {
            return UseMathsTutorial
                ? mathsGuidedQuestion
                : "Unscramble the word.\nClue: A pet that says meow.";
        }

        private string GetGuidedShuffled()
        {
            return UseMathsTutorial ? mathsGuidedShuffled : "TAC";
        }

        private string GetGuidedAnswer()
        {
            return UseMathsTutorial ? mathsGuidedAnswer : "CAT";
        }

        private int GetGuidedSwapTargetIndex()
        {
            return UseMathsTutorial ? 3 : 2;
        }

        private string GetIndependentPracticeQuestion()
        {
            return UseMathsTutorial
                ? mathsPracticeQuestion
                : "Unscramble the word.\nClue: A pet that barks.";
        }

        private string GetIndependentPracticeShuffled()
        {
            return UseMathsTutorial ? mathsPracticeShuffled : "GDO";
        }

        private string GetIndependentPracticeAnswer()
        {
            return UseMathsTutorial ? mathsPracticeAnswer : "DOG";
        }

        private string GetHintQuestion()
        {
            return UseMathsTutorial
                ? mathsHintQuestion
                : "Unscramble the word.\nClue: A pet that says meow.";
        }

        private string GetHintShuffled()
        {
            return UseMathsTutorial ? mathsHintShuffled : "ACT";
        }

        private string GetHintAnswer()
        {
            return UseMathsTutorial ? mathsHintAnswer : "CAT";
        }

        private void StartGhostSwapDemonstration()
        {
            WordShuffleTutorialPracticeTile source = GetPracticeTileAtIndex(0);
            WordShuffleTutorialPracticeTile target = GetPracticeTileAtIndex(
                Mathf.Clamp(GetGuidedSwapTargetIndex(), 0, practiceTiles.Count - 1));

            if (source == null || target == null || handPointer == null)
            {
                BeginStage(TutorialStage.GuidedSwap);
                return;
            }

            ghostTile = Instantiate(source.gameObject, practiceTileLayer);
            ghostTile.name = "TutorialGhostTile";
            WordShuffleTutorialPracticeTile ghostPracticeTile =
                ghostTile.GetComponent<WordShuffleTutorialPracticeTile>();
            if (ghostPracticeTile != null)
                ghostPracticeTile.enabled = false;

            WordShuffleLetterTile ghostRealTile = ghostTile.GetComponent<WordShuffleLetterTile>();
            if (ghostRealTile != null)
                ghostRealTile.enabled = false;

            CanvasGroup ghostCanvasGroup = ghostTile.GetComponent<CanvasGroup>();
            if (ghostCanvasGroup != null)
            {
                ghostCanvasGroup.alpha = ghostTransparency;
                ghostCanvasGroup.interactable = false;
                ghostCanvasGroup.blocksRaycasts = false;
            }

            RectTransform ghostRect = ghostTile.transform as RectTransform;
            ghostRect.anchoredPosition = source.RectTransform.anchoredPosition;
            ghostRect.localScale = Vector3.one;

            handPointer.gameObject.SetActive(true);
            handPointer.raycastTarget = false;
            handPointer.rectTransform.anchoredPosition = GetTargetCenter(source.RectTransform) + gestureHandOffset;
            handPointer.rectTransform.localScale = Vector3.one;
            handPointer.transform.SetAsLastSibling();

            Vector2 handTarget = GetTargetCenter(target.RectTransform) + gestureHandOffset;
            Vector2 ghostTarget = target.RectTransform.anchoredPosition;

            stageSequence = DOTween.Sequence().SetUpdate(true);
            stageSequence.AppendInterval(demonstrationPause);
            stageSequence.Append(handPointer.rectTransform.DOScale(0.88f, 0.14f).SetEase(Ease.OutQuad));
            stageSequence.Append(handPointer.rectTransform.DOAnchorPos(handTarget, handMoveDuration).SetEase(Ease.InOutSine));
            stageSequence.Join(ghostRect.DOAnchorPos(ghostTarget, handMoveDuration).SetEase(Ease.InOutSine));
            stageSequence.Append(handPointer.rectTransform.DOScale(1f, 0.14f).SetEase(Ease.OutBack));
            stageSequence.AppendInterval(demonstrationPause);
            stageSequence.SetLoops(Mathf.Max(2, demonstrationRepeatCount), LoopType.Restart);
            stageSequence.OnComplete(() =>
            {
                DestroyGhostTile();
                BeginStage(TutorialStage.GuidedSwap);
            });
        }

        private void ShowGuidedSwapPointer()
        {
            string sourceCharacter = UseMathsTutorial ? "5" : "T";
            WordShuffleTutorialPracticeTile source =
                practiceTiles.FirstOrDefault(tile => tile.Letter == sourceCharacter);
            if (source != null)
                ShowStationaryHand(source.RectTransform, gestureHandOffset);
        }

        private void OnMeaningfulPracticeSwapCompleted()
        {
            transitionLocked = false;

            if (BuildPracticeAnswer() == targetAnswer)
            {
                allowPracticeDrag = false;
                SetPracticeTilesRaycastState(false);

                if (currentStage == TutorialStage.GuidedSwap)
                {
                    SetInstruction("Great swap!", false);
                    ScheduleStage(TutorialStage.PracticeWord, 0.65f);
                }
                else if (currentStage == TutorialStage.PracticeWord)
                {
                    SetInstruction($"You made {GetIndependentPracticeAnswer()}!", false);
                    ScheduleStage(TutorialStage.HintPractice, 0.7f);
                }

                return;
            }

            allowPracticeDrag = true;
            SetPracticeTilesRaycastState(true);

            if (currentStage == TutorialStage.GuidedSwap)
            {
                SetInstruction($"Good drag! Now make {GetGuidedAnswer()}.", true);
                ShowGuidedSwapPointer();
            }
            else
            {
                SetInstruction($"Keep trying. Make {GetIndependentPracticeAnswer()}.", true);
            }
        }

        private void ScheduleStage(TutorialStage nextStage, float delay)
        {
            transitionLocked = true;
            stageSequence = DOTween.Sequence().SetUpdate(true);
            stageSequence.AppendInterval(delay);
            stageSequence.AppendCallback(() => BeginStage(nextStage));
        }

        private void ShowCompleteStageAfterDelay()
        {
            SetPracticeTilesRaycastState(false);
            stageSequence = DOTween.Sequence().SetUpdate(true);
            stageSequence.AppendInterval(0.75f);
            stageSequence.AppendCallback(() => BeginStage(TutorialStage.Complete));
        }

        private void SetupPracticeWord(string shuffledWord, string answer)
        {
            ClearPracticeObjects();
            targetAnswer = answer;

            if (practiceSlotLayer == null || practiceTileLayer == null)
                return;

            if (practiceFocusTarget != null)
            {
                float focusWidth = practiceTileSize.x * shuffledWord.Length +
                                   practiceTileSpacing * Mathf.Max(0, shuffledWord.Length - 1) +
                                   focusPadding.x * 2f;
                practiceFocusTarget.sizeDelta = new Vector2(
                    focusWidth,
                    practiceTileSize.y + focusPadding.y * 2f);
            }

            for (int i = 0; i < shuffledWord.Length; i++)
            {
                Vector2 position = GetPracticePosition(i, shuffledWord.Length);

                GameObject slotObject = new GameObject(
                    $"TutorialSlot_{i + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                slotObject.transform.SetParent(practiceSlotLayer, false);

                RectTransform slotRect = slotObject.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotRect.pivot = new Vector2(0.5f, 0.5f);
                slotRect.sizeDelta = practiceTileSize;
                slotRect.anchoredPosition = position;

                Image slotImage = slotObject.GetComponent<Image>();
                slotImage.sprite = practiceSlotSprite;
                slotImage.color = practiceSlotColor;
                slotImage.raycastTarget = false;
                practiceSlots.Add(slotRect);

                GameObject tileObject = CreatePracticeTileVisual(i);
                RectTransform tileRect = tileObject.transform as RectTransform;
                tileRect.anchorMin = new Vector2(0.5f, 0.5f);
                tileRect.anchorMax = new Vector2(0.5f, 0.5f);
                tileRect.pivot = new Vector2(0.5f, 0.5f);
                tileRect.sizeDelta = practiceTileSize;
                tileRect.anchoredPosition = position;
                tileRect.localScale = Vector3.one;

                WordShuffleTutorialPracticeTile practiceTile =
                    tileObject.GetComponent<WordShuffleTutorialPracticeTile>();
                practiceTile.Initialize(this, shuffledWord[i].ToString(), i);
                practiceTiles.Add(practiceTile);
            }

            SetPracticeTilesRaycastState(false);
        }

        private GameObject CreatePracticeTileVisual(int index)
        {
            GameObject tileObject;

            if (letterTileTemplate != null)
            {
                tileObject = Instantiate(letterTileTemplate.gameObject, practiceTileLayer);
                WordShuffleLetterTile realTile = tileObject.GetComponent<WordShuffleLetterTile>();
                if (realTile != null)
                    realTile.enabled = false;
            }
            else
            {
                tileObject = CreateFallbackTileVisual();
            }

            tileObject.name = $"TutorialPracticeTile_{index + 1}";

            if (tileObject.GetComponent<CanvasGroup>() == null)
                tileObject.AddComponent<CanvasGroup>();

            if (tileObject.GetComponent<WordShuffleTutorialPracticeTile>() == null)
                tileObject.AddComponent<WordShuffleTutorialPracticeTile>();

            tileObject.SetActive(true);
            return tileObject;
        }

        private GameObject CreateFallbackTileVisual()
        {
            GameObject tileObject = new GameObject(
                "TutorialPracticeTile_Fallback",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            tileObject.transform.SetParent(practiceTileLayer, false);
            tileObject.GetComponent<Image>().color = new Color(1f, 0.64f, 0.18f, 1f);

            GameObject textObject = new GameObject(
                "LetterText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(tileObject.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 66f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.1f, 0.08f, 0.04f, 1f);
            label.raycastTarget = false;
            return tileObject;
        }

        private void SwapPracticeTiles(
            WordShuffleTutorialPracticeTile first,
            WordShuffleTutorialPracticeTile second,
            Action onComplete)
        {
            if (first == null || second == null)
                return;

            transitionLocked = true;
            SetPracticeTilesRaycastState(false);

            int firstIndex = first.CurrentIndex;
            int secondIndex = second.CurrentIndex;
            first.SetIndex(secondIndex);
            second.SetIndex(firstIndex);

            first.RectTransform.DOKill();
            second.RectTransform.DOKill();

            stageSequence?.Kill();
            stageSequence = DOTween.Sequence().SetUpdate(true);
            stageSequence.Join(first.RectTransform
                .DOAnchorPos(GetPracticePosition(first.CurrentIndex, practiceTiles.Count), practiceSwapDuration)
                .SetEase(Ease.InOutCubic));
            stageSequence.Join(second.RectTransform
                .DOAnchorPos(GetPracticePosition(second.CurrentIndex, practiceTiles.Count), practiceSwapDuration)
                .SetEase(Ease.InOutCubic));
            stageSequence.Join(first.RectTransform.DOScale(1f, practiceSwapDuration).SetEase(Ease.OutBack));
            stageSequence.Join(second.RectTransform.DOScale(1f, practiceSwapDuration).SetEase(Ease.OutBack));
            stageSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void SnapPracticeTile(WordShuffleTutorialPracticeTile tile)
        {
            if (tile == null)
                return;

            tile.RectTransform.DOKill();
            tile.RectTransform
                .DOAnchorPos(GetPracticePosition(tile.CurrentIndex, practiceTiles.Count), 0.22f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            tile.RectTransform.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private WordShuffleTutorialPracticeTile FindPracticeTileAtScreenPosition(
            Vector2 screenPosition,
            WordShuffleTutorialPracticeTile ignoredTile)
        {
            return practiceTiles.FirstOrDefault(tile =>
                tile != null &&
                tile != ignoredTile &&
                RectTransformUtility.RectangleContainsScreenPoint(tile.RectTransform, screenPosition, UICamera));
        }

        private WordShuffleTutorialPracticeTile GetPracticeTileAtIndex(int index)
        {
            return practiceTiles.FirstOrDefault(tile => tile != null && tile.CurrentIndex == index);
        }

        private bool TryFindTutorialHintMove(
            out WordShuffleTutorialPracticeTile correctTile,
            out WordShuffleTutorialPracticeTile displacedTile)
        {
            correctTile = null;
            displacedTile = null;

            for (int index = 0; index < targetAnswer.Length; index++)
            {
                WordShuffleTutorialPracticeTile current = GetPracticeTileAtIndex(index);
                string requiredLetter = targetAnswer[index].ToString();

                if (current != null && current.Letter == requiredLetter)
                    continue;

                correctTile = practiceTiles.FirstOrDefault(tile =>
                    tile != null && tile.CurrentIndex != index && tile.Letter == requiredLetter);
                displacedTile = current;

                if (correctTile != null && displacedTile != null)
                    return true;
            }

            return false;
        }

        private string BuildPracticeAnswer()
        {
            string[] letters = new string[practiceTiles.Count];

            foreach (WordShuffleTutorialPracticeTile tile in practiceTiles)
            {
                if (tile != null && tile.CurrentIndex >= 0 && tile.CurrentIndex < letters.Length)
                    letters[tile.CurrentIndex] = tile.Letter;
            }

            return string.Concat(letters);
        }

        private Vector2 GetPracticePosition(int index, int count)
        {
            float totalWidth = practiceTileSize.x * count + practiceTileSpacing * Mathf.Max(0, count - 1);
            float startX = -totalWidth * 0.5f + practiceTileSize.x * 0.5f;
            return new Vector2(startX + index * (practiceTileSize.x + practiceTileSpacing), 0f);
        }

        private void SetPracticeTilesRaycastState(bool enabled)
        {
            foreach (WordShuffleTutorialPracticeTile tile in practiceTiles)
            {
                if (tile != null)
                    tile.SetRaycastState(enabled);
            }
        }

        private void SetQuestion(string message)
        {
            if (questionText != null)
                questionText.text = message ?? string.Empty;
        }

        private void SetInstruction(string message, bool breathe)
        {
            if (instructionText != null)
                instructionText.text = message ?? string.Empty;

            instructionTween?.Kill();

            if (instructionCard == null)
                return;

            instructionCard.localScale = Vector3.one;
            if (breathe)
            {
                instructionTween = instructionCard
                    .DOScale(breathingScale, breathingDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private void ShowFocus(RectTransform target)
        {
            activeFocusTarget = target;

            if (focusHighlight == null || target == null)
                return;

            focusHighlight.gameObject.SetActive(true);
            focusHighlight.raycastTarget = false;
            PositionOverlayOverTarget(target, focusHighlight.rectTransform, focusPadding);
            focusHighlight.rectTransform.localScale = Vector3.one;
            focusTween = focusHighlight.rectTransform
                .DOScale(1.045f, breathingDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void HideFocus()
        {
            activeFocusTarget = null;
            focusTween?.Kill();

            if (focusHighlight != null)
            {
                focusHighlight.rectTransform.localScale = Vector3.one;
                focusHighlight.gameObject.SetActive(false);
            }
        }

        private void ShowStationaryHand(RectTransform target, Vector2 offset)
        {
            stationaryHandTarget = target;
            stationaryHandOffset = offset;

            if (handPointer == null || target == null)
                return;

            handPointer.gameObject.SetActive(true);
            handPointer.raycastTarget = false;
            handPointer.rectTransform.anchoredPosition = GetTargetCenter(target) + offset;
            handPointer.rectTransform.localScale = Vector3.one;
            handPointer.transform.SetAsLastSibling();
            handTween = handPointer.rectTransform
                .DOScale(0.9f, handPulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void HideStationaryHand()
        {
            stationaryHandTarget = null;
            handTween?.Kill();

            if (handPointer != null)
            {
                handPointer.rectTransform.localScale = Vector3.one;
                handPointer.gameObject.SetActive(false);
            }
        }

        private Vector2 GetTargetCenter(RectTransform target)
        {
            if (target == null || TutorialRect == null)
                return Vector2.zero;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(UICamera, target.TransformPoint(target.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                TutorialRect,
                screenPoint,
                UICamera,
                out Vector2 localPoint);
            return localPoint;
        }

        private void PositionOverlayOverTarget(RectTransform source, RectTransform overlay, Vector2 padding)
        {
            if (source == null || overlay == null || TutorialRect == null)
                return;

            Vector3[] corners = new Vector3[4];
            source.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(UICamera, corners[i]);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    TutorialRect,
                    screenPoint,
                    UICamera,
                    out Vector2 localPoint);
                min = Vector2.Min(min, localPoint);
                max = Vector2.Max(max, localPoint);
            }

            overlay.anchorMin = new Vector2(0.5f, 0.5f);
            overlay.anchorMax = new Vector2(0.5f, 0.5f);
            overlay.pivot = new Vector2(0.5f, 0.5f);
            overlay.anchoredPosition = (min + max) * 0.5f;
            overlay.sizeDelta = max - min + padding * 2f;
        }

        private void CompleteTutorial()
        {
            if (!isRunning || currentStage != TutorialStage.Complete)
                return;

            PlayerPrefs.SetInt(GetCompletionKeyForActiveScene(), 1);
            PlayerPrefs.Save();

            Action continuation = continueRealGame;
            continueRealGame = null;
            isRunning = false;
            currentStage = TutorialStage.Inactive;

            KillTutorialTweens();
            DestroyGhostTile();
            ClearPracticeObjects();
            SetQuestion(originalQuestionText);

            if (instructionCard != null)
                instructionCard.anchoredPosition = originalInstructionPosition;

            if (gameManager != null)
                gameManager.EndTutorialHold();

            gameObject.SetActive(false);
            continuation?.Invoke();
        }

        private void ClearPracticeObjects()
        {
            DestroyGhostTile();

            foreach (WordShuffleTutorialPracticeTile tile in practiceTiles)
            {
                if (tile == null)
                    continue;

                tile.RectTransform.DOKill();
                tile.transform.SetParent(null, false);
                Destroy(tile.gameObject);
            }

            foreach (RectTransform slot in practiceSlots)
            {
                if (slot == null)
                    continue;

                slot.SetParent(null, false);
                Destroy(slot.gameObject);
            }

            practiceTiles.Clear();
            practiceSlots.Clear();
        }

        private void DestroyGhostTile()
        {
            if (ghostTile == null)
                return;

            ghostTile.transform.SetParent(null, false);
            Destroy(ghostTile);
            ghostTile = null;
        }

        private void KillStageTweens()
        {
            stageSequence?.Kill();
            stageSequence = null;
            instructionTween?.Kill();
            instructionTween = null;
            focusTween?.Kill();
            focusTween = null;
            handTween?.Kill();
            handTween = null;
            DestroyGhostTile();
        }

        private void KillTutorialTweens()
        {
            KillStageTweens();

            foreach (WordShuffleTutorialPracticeTile tile in practiceTiles)
            {
                if (tile != null)
                    tile.RectTransform.DOKill();
            }
        }
    }
}
