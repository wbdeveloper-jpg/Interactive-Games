using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmotionTimerQuiz
{
    public class EmotionFirstTimeTutorialController : MonoBehaviour
    {
        private enum TutorialStage
        {
            None,
            Situation,
            FirstCard,
            SecondCard,
            ThirdCard,
            ChooseAnswer,
            NextRound,
            Success
        }

        [Header("Core References")]
        public EmotionTimerQuizManager gameManager;
        public CanvasGroup tutorialCanvasGroup;
        public Image dimOverlay;
        public RectTransform promptPanel;
        public TextMeshProUGUI instructionText;
        public Image handPointer;
        public Button clickCatcher;

        [Header("First-Time Behaviour")]
        public string tutorialCompletedPrefsKey = "EmotionTimerQuiz_FirstTimeTutorialCompleted";
        public bool forcePlayTutorial;
        [Min(0)] public int practiceQuestionIndex;

        [Header("Instruction Text")]
        [TextArea(2, 4)] public string situationInstruction = "Look carefully at this situation and think about what it means. Click to continue.";
        [TextArea(2, 4)] public string firstCardInstruction = "These are the emotion choices. Look at the first card. Click to continue.";
        [TextArea(2, 4)] public string secondCardInstruction = "Now look at the second emotion card. Click to continue.";
        [TextArea(2, 4)] public string thirdCardInstruction = "And this is the third emotion card. Click to continue.";
        [TextArea(2, 4)] public string chooseInstruction = "Which emotion matches the situation? Tap the correct card.";
        [TextArea(2, 4)] public string wrongInstruction = "Not quite. Try again and tap the card the hand is pointing to.";
        [TextArea(2, 4)] public string nextInstruction = "Great job! Tap NEXT ROUND to continue.";
        [TextArea(2, 4)] public string successInstruction = "You completed the tutorial successfully! Click anywhere to start the actual game.";

        [Header("Instruction Prompt Layout")]
        [Tooltip("When enabled, these values control the instruction card RectTransform at runtime.")]
        public bool applyPromptLayoutFromInspector = true;
        public Vector2 promptAnchoredPosition = new Vector2(0f, 55f);
        public Vector2 promptSize = new Vector2(620f, 180f);
        [Tooltip("Moves the prompt at runtime to avoid covering the situation and emotion cards.")]
        public bool autoPositionPromptToAvoidGameplay = true;
        [Min(0f)] public float promptCanvasPadding = 18f;
        [Min(0f)] public float promptTargetGap = 18f;
        [Min(0f)] public float promptMoveDuration = 0.25f;

        [Header("Hand Position - Canvas Safe")]
        [HideInInspector] public int positioningDefaultsVersion;
        [Tooltip("Final fine adjustment applied to every pointer position.")]
        public Vector2 globalHandFineTune = Vector2.zero;
        [Tooltip("Normalized point inside the situation card: (0,0) bottom-left, (1,1) top-right.")]
        public Vector2 situationTargetAnchor = new Vector2(0.5f, 0.30f);
        public Vector2 situationHandOffset = Vector2.zero;
        public Vector2 firstCardTargetAnchor = new Vector2(0.65f, 0.5f);
        public Vector2 firstCardHandOffset = Vector2.zero;
        public Vector2 secondCardTargetAnchor = new Vector2(0.65f, 0.5f);
        public Vector2 secondCardHandOffset = Vector2.zero;
        public Vector2 thirdCardTargetAnchor = new Vector2(0.65f, 0.5f);
        public Vector2 thirdCardHandOffset = Vector2.zero;
        public Vector2 correctCardTargetAnchor = new Vector2(0.65f, 0.5f);
        public Vector2 correctCardHandOffset = Vector2.zero;
        public Vector2 nextButtonTargetAnchor = new Vector2(0.5f, 0.20f);
        public Vector2 nextButtonHandOffset = new Vector2(0f, -20f);
        [Tooltip("The fingertip position inside the hand sprite: (0,0) bottom-left, (1,1) top-right.")]
        public Vector2 handPointerTipNormalized = new Vector2(0.25f, 0.82f);
        public bool clampHandInsideCanvas = true;
        [Min(0f)] public float handCanvasPadding = 12f;

        [Header("Hand Animation")]
        [Min(0.05f)] public float handMoveDuration = 0.35f;
        [Min(0.05f)] public float handPulseDuration = 0.6f;
        [Min(0f)] public float handPulseAmount = 0.1f;
        [Min(0f)] public float handFloatDistance = 12f;

        [Header("Tutorial Timing")]
        [Min(0f)] public float correctAnswerHintDelay = 1.5f;
        [Min(1f)] public float nextRoundAutoContinueSeconds = 10f;
        [Min(0.05f)] public float promptPulseDuration = 0.8f;
        [Min(0f)] public float promptPulseAmount = 0.025f;

        private TutorialStage currentStage;
        private EmotionOptionCard correctCard;
        private Coroutine correctHintCoroutine;
        private Coroutine nextRoundCoroutine;
        private bool tutorialActive;
        private Vector3 handBaseScale = Vector3.one;
        private Vector3 promptBaseScale = Vector3.one;
        private Tween promptMoveTween;
        private Tween promptPulseTween;

        public bool IsTutorialActive
        {
            get { return tutorialActive; }
        }

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponentInParent<EmotionTimerQuizManager>();
            }

            if (tutorialCanvasGroup == null)
            {
                tutorialCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (handPointer != null)
            {
                handBaseScale = handPointer.rectTransform.localScale == Vector3.zero
                    ? Vector3.one
                    : handPointer.rectTransform.localScale;
            }

            if (promptPanel != null)
            {
                ApplyPromptLayout();
                promptBaseScale = promptPanel.localScale == Vector3.zero ? Vector3.one : promptPanel.localScale;
            }

            BindClickCatcher();
            SetTutorialVisible(false);
        }

        private void OnDisable()
        {
            if (tutorialActive)
            {
                CleanupTutorialVisuals();
            }
        }

        private void OnDestroy()
        {
            if (clickCatcher != null)
            {
                clickCatcher.onClick.RemoveListener(HandleContinueClick);
            }

            RemoveNextRoundListener();
        }

        public bool ShouldPlayTutorial()
        {
            if (forcePlayTutorial)
            {
                return true;
            }

            return string.IsNullOrEmpty(tutorialCompletedPrefsKey) ||
                   PlayerPrefs.GetInt(tutorialCompletedPrefsKey, 0) != 1;
        }

        public void BeginTutorial()
        {
            if (tutorialActive || gameManager == null)
            {
                if (gameManager == null)
                {
                    Debug.LogWarning("EmotionFirstTimeTutorialController: Game manager is not assigned.");
                }

                return;
            }

            StopAllCoroutines();
            tutorialActive = true;
            currentStage = TutorialStage.None;
            correctCard = null;
            BindClickCatcher();
            SetTutorialVisible(true);

            bool prepared = gameManager.PrepareTutorialPracticeRound(
                practiceQuestionIndex,
                HandlePracticeCardSelected,
                out correctCard);

            if (!prepared || correctCard == null)
            {
                Debug.LogWarning("EmotionFirstTimeTutorialController: Practice round could not be prepared. Starting the normal game.");
                CleanupTutorialVisuals();
                gameManager.StartGame();
                return;
            }

            Canvas.ForceUpdateCanvases();
            ShowSituationStage();
        }

        [ContextMenu("Reset Tutorial Completion")]
        public void ResetTutorialCompletion()
        {
            if (!string.IsNullOrEmpty(tutorialCompletedPrefsKey))
            {
                PlayerPrefs.DeleteKey(tutorialCompletedPrefsKey);
                PlayerPrefs.Save();
            }
        }

        [ContextMenu("Apply Instruction Prompt Layout")]
        public void ApplyPromptLayout()
        {
            if (!applyPromptLayoutFromInspector || promptPanel == null)
            {
                return;
            }

            promptPanel.anchorMin = new Vector2(0.5f, 0.5f);
            promptPanel.anchorMax = new Vector2(0.5f, 0.5f);
            promptPanel.pivot = new Vector2(0.5f, 0.5f);
            promptPanel.anchoredPosition = promptAnchoredPosition;
            promptPanel.sizeDelta = new Vector2(
                Mathf.Max(100f, promptSize.x),
                Mathf.Max(80f, promptSize.y));
        }

        private void BindClickCatcher()
        {
            if (clickCatcher == null)
            {
                return;
            }

            clickCatcher.onClick.RemoveListener(HandleContinueClick);
            clickCatcher.onClick.AddListener(HandleContinueClick);
        }

        private void HandleContinueClick()
        {
            if (!tutorialActive)
            {
                return;
            }

            switch (currentStage)
            {
                case TutorialStage.Situation:
                    ShowCardStage(0, TutorialStage.FirstCard, firstCardInstruction);
                    break;

                case TutorialStage.FirstCard:
                    ShowCardStage(1, TutorialStage.SecondCard, secondCardInstruction);
                    break;

                case TutorialStage.SecondCard:
                    ShowCardStage(2, TutorialStage.ThirdCard, thirdCardInstruction);
                    break;

                case TutorialStage.ThirdCard:
                    BeginChooseAnswerStage();
                    break;

                case TutorialStage.Success:
                    CompleteTutorialAndStartGame();
                    break;
            }
        }

        private void ShowSituationStage()
        {
            currentStage = TutorialStage.Situation;
            SetClickCatcherActive(true);
            SetInstruction(situationInstruction);
            PointHandAt(gameManager.situationCardTransform, situationTargetAnchor, situationHandOffset);
        }

        private void ShowCardStage(int index, TutorialStage stage, string message)
        {
            currentStage = stage;
            if (gameManager.optionCards == null || index < 0 || index >= gameManager.optionCards.Length || gameManager.optionCards[index] == null)
            {
                HandleContinueClick();
                return;
            }

            SetClickCatcherActive(true);
            SetInstruction(message);
            PointHandAt(
                gameManager.optionCards[index].transform as RectTransform,
                GetCardTargetAnchor(index),
                GetCardHandOffset(index));
        }

        private void BeginChooseAnswerStage()
        {
            currentStage = TutorialStage.ChooseAnswer;
            SetClickCatcherActive(false);
            HideHand();
            CompletePromptMovement();
            SetInstruction(chooseInstruction);
            gameManager.SetTutorialOptionCardsInteractable(true);

            if (correctHintCoroutine != null)
            {
                StopCoroutine(correctHintCoroutine);
            }

            correctHintCoroutine = StartCoroutine(ShowCorrectAnswerHintAfterDelay());
        }

        private IEnumerator ShowCorrectAnswerHintAfterDelay()
        {
            if (correctAnswerHintDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(correctAnswerHintDelay);
            }

            correctHintCoroutine = null;
            if (tutorialActive && currentStage == TutorialStage.ChooseAnswer)
            {
                PointToCorrectCard();
            }
        }

        private void HandlePracticeCardSelected(EmotionOptionCard selectedCard)
        {
            if (!tutorialActive || currentStage != TutorialStage.ChooseAnswer || selectedCard == null || selectedCard.OptionData == null)
            {
                return;
            }

            if (!selectedCard.OptionData.isCorrect)
            {
                selectedCard.ShowWrong();
                SetInstruction(wrongInstruction);
                PointToCorrectCard();
                StartCoroutine(RestoreWrongCard(selectedCard));
                return;
            }

            if (correctHintCoroutine != null)
            {
                StopCoroutine(correctHintCoroutine);
                correctHintCoroutine = null;
            }

            selectedCard.ShowCorrect();
            gameManager.SetTutorialOptionCardsInteractable(false);
            if (gameManager.feedbackText != null)
            {
                gameManager.feedbackText.text = "Correct!";
            }

            BeginNextRoundStage();
        }

        private IEnumerator RestoreWrongCard(EmotionOptionCard card)
        {
            yield return new WaitForSecondsRealtime(0.7f);
            if (tutorialActive && currentStage == TutorialStage.ChooseAnswer && card != null)
            {
                card.ShowNormal();
            }
        }

        private void PointToCorrectCard()
        {
            if (correctCard != null)
            {
                PointHandAt(correctCard.transform as RectTransform, correctCardTargetAnchor, correctCardHandOffset);
            }
        }

        private void BeginNextRoundStage()
        {
            currentStage = TutorialStage.NextRound;
            SetClickCatcherActive(false);
            SetInstruction(nextInstruction);

            if (gameManager.nextRoundButton == null)
            {
                ShowSuccessStage();
                return;
            }

            gameManager.nextRoundButton.interactable = true;
            RemoveNextRoundListener();
            gameManager.nextRoundButton.onClick.AddListener(HandleTutorialNextRoundClicked);
            PointHandAt(
                gameManager.nextRoundButton.transform as RectTransform,
                nextButtonTargetAnchor,
                nextButtonHandOffset,
                false);
            PositionPromptAtCanvasCenter();

            if (nextRoundCoroutine != null)
            {
                StopCoroutine(nextRoundCoroutine);
            }

            nextRoundCoroutine = StartCoroutine(NextRoundAutoContinueRoutine());
        }

        private IEnumerator NextRoundAutoContinueRoutine()
        {
            float duration = Mathf.Max(1f, nextRoundAutoContinueSeconds);
            float remaining = duration;

            while (remaining > 0f && tutorialActive && currentStage == TutorialStage.NextRound)
            {
                int visibleSeconds = Mathf.Max(1, Mathf.CeilToInt(remaining));
                if (gameManager.nextRoundButtonText != null)
                {
                    gameManager.nextRoundButtonText.text = "NEXT ROUND (" + visibleSeconds + "s)";
                }

                if (gameManager.nextRoundCountdownFillImage != null)
                {
                    gameManager.nextRoundCountdownFillImage.fillAmount = Mathf.Clamp01(remaining / duration);
                }

                remaining -= Time.unscaledDeltaTime;
                yield return null;
            }

            nextRoundCoroutine = null;
            if (tutorialActive && currentStage == TutorialStage.NextRound)
            {
                ShowSuccessStage();
            }
        }

        private void HandleTutorialNextRoundClicked()
        {
            if (tutorialActive && currentStage == TutorialStage.NextRound)
            {
                ShowSuccessStage();
            }
        }

        private void ShowSuccessStage()
        {
            currentStage = TutorialStage.Success;
            if (nextRoundCoroutine != null)
            {
                StopCoroutine(nextRoundCoroutine);
                nextRoundCoroutine = null;
            }

            RemoveNextRoundListener();
            if (gameManager.nextRoundButton != null)
            {
                gameManager.nextRoundButton.interactable = false;
            }

            if (gameManager.nextRoundButtonText != null)
            {
                gameManager.nextRoundButtonText.text = "NEXT ROUND";
            }

            if (gameManager.nextRoundCountdownFillImage != null)
            {
                gameManager.nextRoundCountdownFillImage.fillAmount = 0f;
            }

            HideHand();
            SetInstruction(successInstruction);
            PositionPromptAtCanvasCenter();
            SetClickCatcherActive(true);
        }

        private void PositionPromptAtCanvasCenter()
        {
            if (promptPanel == null)
            {
                return;
            }

            RectTransform promptSpace = promptPanel.parent as RectTransform;
            if (promptSpace == null)
            {
                return;
            }

            MovePromptSmoothly(promptSpace.rect.center);
        }

        private void CompleteTutorialAndStartGame()
        {
            if (!string.IsNullOrEmpty(tutorialCompletedPrefsKey))
            {
                PlayerPrefs.SetInt(tutorialCompletedPrefsKey, 1);
                PlayerPrefs.Save();
            }

            CleanupTutorialVisuals();
            gameManager.StartGame();
        }

        private void SetInstruction(string message)
        {
            if (instructionText != null)
            {
                instructionText.text = message;
            }

            if (promptPanel == null)
            {
                return;
            }

            if (!autoPositionPromptToAvoidGameplay)
            {
                ApplyPromptLayout();
            }
            promptPanel.gameObject.SetActive(true);
            if (promptPulseTween != null && promptPulseTween.IsActive())
            {
                promptPulseTween.Kill();
                promptPulseTween = null;
            }
            promptPanel.localScale = promptBaseScale;

            if (promptPulseAmount > 0f)
            {
                promptPulseTween = promptPanel.DOScale(promptBaseScale * (1f + promptPulseAmount), promptPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private Vector2 GetCardTargetAnchor(int cardIndex)
        {
            switch (cardIndex)
            {
                case 0:
                    return firstCardTargetAnchor;
                case 1:
                    return secondCardTargetAnchor;
                case 2:
                    return thirdCardTargetAnchor;
                default:
                    return new Vector2(0.5f, 0.5f);
            }
        }

        private Vector2 GetCardHandOffset(int cardIndex)
        {
            switch (cardIndex)
            {
                case 0:
                    return firstCardHandOffset;
                case 1:
                    return secondCardHandOffset;
                case 2:
                    return thirdCardHandOffset;
                default:
                    return Vector2.zero;
            }
        }

        private void PointHandAt(
            RectTransform target,
            Vector2 normalizedTargetAnchor,
            Vector2 stageOffset,
            bool positionPrompt = true)
        {
            if (handPointer == null || target == null)
            {
                HideHand();
                return;
            }

            RectTransform handTransform = handPointer.rectTransform;
            handTransform.DOKill();
            handTransform.gameObject.SetActive(true);
            handPointer.enabled = handPointer.sprite != null;

            RectTransform handParent = handTransform.parent as RectTransform;
            if (handParent == null)
            {
                Debug.LogWarning("EmotionFirstTimeTutorialController: Hand Pointer must be a child of a RectTransform.");
                HideHand();
                return;
            }

            Vector2 handParentLocalPoint;
            if (!TryGetTargetPointInSpace(target, handParent, normalizedTargetAnchor, out handParentLocalPoint))
            {
                Debug.LogWarning("EmotionFirstTimeTutorialController: Could not convert the pointer target into Canvas space.");
                HideHand();
                return;
            }

            handPointerTipNormalized.x = Mathf.Clamp01(handPointerTipNormalized.x);
            handPointerTipNormalized.y = Mathf.Clamp01(handPointerTipNormalized.y);
            Vector2 handTipFromPivot = new Vector2(
                Mathf.Lerp(handTransform.rect.xMin, handTransform.rect.xMax, handPointerTipNormalized.x) * handBaseScale.x,
                Mathf.Lerp(handTransform.rect.yMin, handTransform.rect.yMax, handPointerTipNormalized.y) * handBaseScale.y);

            Vector2 destination = handParentLocalPoint + stageOffset + globalHandFineTune - handTipFromPivot;
            if (clampHandInsideCanvas)
            {
                destination = ClampHandPositionInsideParent(destination, handParent, handTransform);
            }

            handTransform.localPosition = new Vector3(destination.x, destination.y, 0f);
            if (positionPrompt)
            {
                PositionPromptToAvoidGameplay(target);
            }
            handTransform.localScale = handMoveDuration > 0f ? handBaseScale * 0.82f : handBaseScale;

            Sequence scaleSequence = DOTween.Sequence().SetUpdate(true);
            if (handMoveDuration > 0f)
            {
                scaleSequence.Append(handTransform.DOScale(handBaseScale, handMoveDuration).SetEase(Ease.OutBack));
            }

            if (handPulseAmount > 0f)
            {
                scaleSequence.Append(handTransform.DOScale(handBaseScale * (1f + handPulseAmount), handPulseDuration).SetEase(Ease.InOutSine));
                scaleSequence.Append(handTransform.DOScale(handBaseScale, handPulseDuration).SetEase(Ease.InOutSine));
                scaleSequence.SetLoops(-1);
            }
            else if (handMoveDuration <= 0f)
            {
                scaleSequence.Kill();
            }

            if (handFloatDistance > 0f)
            {
                float baseLocalY = handTransform.localPosition.y;
                handTransform.DOLocalMoveY(baseLocalY + handFloatDistance, handPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private bool TryGetTargetPointInSpace(
            RectTransform target,
            RectTransform destinationSpace,
            Vector2 normalizedTargetAnchor,
            out Vector2 destinationPoint)
        {
            normalizedTargetAnchor.x = Mathf.Clamp01(normalizedTargetAnchor.x);
            normalizedTargetAnchor.y = Mathf.Clamp01(normalizedTargetAnchor.y);

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();
            Canvas destinationCanvas = destinationSpace.GetComponentInParent<Canvas>();
            if (targetCanvas != null && destinationCanvas != null && targetCanvas.rootCanvas == destinationCanvas.rootCanvas)
            {
                Bounds relativeBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(destinationSpace, target);
                destinationPoint = new Vector2(
                    Mathf.Lerp(relativeBounds.min.x, relativeBounds.max.x, normalizedTargetAnchor.x),
                    Mathf.Lerp(relativeBounds.min.y, relativeBounds.max.y, normalizedTargetAnchor.y));
                return IsFinite(destinationPoint.x) && IsFinite(destinationPoint.y);
            }

            Vector2 targetLocalPoint = new Vector2(
                Mathf.Lerp(target.rect.xMin, target.rect.xMax, normalizedTargetAnchor.x),
                Mathf.Lerp(target.rect.yMin, target.rect.yMax, normalizedTargetAnchor.y));
            Vector3 targetWorldPosition = target.TransformPoint(targetLocalPoint);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(GetCanvasCamera(targetCanvas), targetWorldPosition);

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                destinationSpace,
                screenPoint,
                GetCanvasCamera(destinationCanvas),
                out destinationPoint);
        }

        private bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void PositionPromptToAvoidGameplay(RectTransform pointerTarget)
        {
            if (!autoPositionPromptToAvoidGameplay || promptPanel == null || pointerTarget == null)
            {
                return;
            }

            RectTransform promptSpace = promptPanel.parent as RectTransform;
            if (promptSpace == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Rect canvasRect = promptSpace.rect;
            Vector2 promptDimensions = promptPanel.rect.size;
            Vector2 halfSize = promptDimensions * 0.5f;
            float leftX = canvasRect.xMin + halfSize.x + promptCanvasPadding;
            float rightX = canvasRect.xMax - halfSize.x - promptCanvasPadding;
            float topY = canvasRect.yMax - halfSize.y - promptCanvasPadding;
            float bottomY = canvasRect.yMin + halfSize.y + promptCanvasPadding;

            Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(promptSpace, pointerTarget);
            Bounds situationBounds = gameManager.situationCardTransform != null
                ? RectTransformUtility.CalculateRelativeRectTransformBounds(promptSpace, gameManager.situationCardTransform)
                : targetBounds;

            float preferredX = targetBounds.center.x <= canvasRect.center.x ? rightX : leftX;
            float oppositeX = preferredX == rightX ? leftX : rightX;
            float aboveSituationY = situationBounds.max.y + promptTargetGap + halfSize.y;
            float belowSituationY = situationBounds.min.y - promptTargetGap - halfSize.y;

            if (pointerTarget == gameManager.situationCardTransform)
            {
                Bounds handBounds = handPointer != null
                    ? RectTransformUtility.CalculateRelativeRectTransformBounds(promptSpace, handPointer.rectTransform)
                    : situationBounds;
                float referenceBottom = Mathf.Min(situationBounds.min.y, handBounds.min.y);
                Vector2 belowHandPosition = ClampPromptCenter(
                    new Vector2(handBounds.center.x, referenceBottom - promptTargetGap - halfSize.y),
                    canvasRect,
                    halfSize);
                MovePromptSmoothly(belowHandPosition);
                return;
            }

            bool isEmotionCardTarget = IsEmotionCard(pointerTarget);
            List<Vector2> candidates;
            if (isEmotionCardTarget)
            {
                // The open strip above SituationCard is the most reliable free area
                // in this scene. Keep card instructions horizontally centered there.
                candidates = new List<Vector2>
                {
                    ClampPromptCenter(new Vector2(canvasRect.center.x, aboveSituationY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(canvasRect.center.x, topY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(preferredX, aboveSituationY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(oppositeX, aboveSituationY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(canvasRect.center.x, belowSituationY), canvasRect, halfSize)
                };
            }
            else
            {
                candidates = new List<Vector2>
                {
                    ClampPromptCenter(new Vector2(preferredX, aboveSituationY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(oppositeX, aboveSituationY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(preferredX, belowSituationY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(oppositeX, belowSituationY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(preferredX, topY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(oppositeX, topY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(preferredX, bottomY), canvasRect, halfSize),
                    ClampPromptCenter(new Vector2(oppositeX, bottomY), canvasRect, halfSize)
                };
            }

            if (isEmotionCardTarget)
            {
                // The user-facing card steps intentionally stay centered at this
                // measured height instead of moving sideways between cards.
                MovePromptSmoothly(candidates[0]);
                return;
            }

            List<Rect> protectedRects = BuildPromptProtectedRects(promptSpace);
            Vector2 bestPosition = candidates[0];
            float bestScore = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Rect candidateRect = Rect.MinMaxRect(
                    candidates[i].x - halfSize.x,
                    candidates[i].y - halfSize.y,
                    candidates[i].x + halfSize.x,
                    candidates[i].y + halfSize.y);
                float score = 0f;

                for (int p = 0; p < protectedRects.Count; p++)
                {
                    score += GetOverlapArea(candidateRect, protectedRects[p]);
                }

                Rect targetRect = BoundsToRect(targetBounds);
                score += GetOverlapArea(candidateRect, targetRect) * 2f;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPosition = candidates[i];
                }
            }

            MovePromptSmoothly(bestPosition);
        }

        private void MovePromptSmoothly(Vector2 targetPosition)
        {
            if (promptPanel == null)
            {
                return;
            }

            if (promptMoveTween != null && promptMoveTween.IsActive())
            {
                promptMoveTween.Kill();
                promptMoveTween = null;
            }

            Vector3 destination = new Vector3(targetPosition.x, targetPosition.y, 0f);
            if (!tutorialActive || !promptPanel.gameObject.activeInHierarchy || promptMoveDuration <= 0f ||
                Vector3.SqrMagnitude(promptPanel.localPosition - destination) < 0.25f)
            {
                promptPanel.localPosition = destination;
                return;
            }

            promptMoveTween = promptPanel
                .DOLocalMove(destination, promptMoveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => promptMoveTween = null);
        }

        private void CompletePromptMovement()
        {
            if (promptMoveTween == null || !promptMoveTween.IsActive())
            {
                promptMoveTween = null;
                return;
            }

            promptMoveTween.Complete();
            promptMoveTween = null;
        }

        private bool IsEmotionCard(RectTransform target)
        {
            if (target == null || gameManager.optionCards == null)
            {
                return false;
            }

            for (int i = 0; i < gameManager.optionCards.Length; i++)
            {
                if (gameManager.optionCards[i] != null && gameManager.optionCards[i].transform == target)
                {
                    return true;
                }
            }

            return false;
        }

        private List<Rect> BuildPromptProtectedRects(RectTransform promptSpace)
        {
            List<Rect> result = new List<Rect>();
            AddProtectedRect(result, promptSpace, gameManager.situationCardTransform);

            if (gameManager.optionCards != null)
            {
                for (int i = 0; i < gameManager.optionCards.Length; i++)
                {
                    if (gameManager.optionCards[i] != null)
                    {
                        AddProtectedRect(result, promptSpace, gameManager.optionCards[i].transform as RectTransform);
                    }
                }
            }

            if (gameManager.roundText != null)
            {
                AddProtectedRect(result, promptSpace, gameManager.roundText.rectTransform);
            }

            if (gameManager.scoreText != null)
            {
                AddProtectedRect(result, promptSpace, gameManager.scoreText.rectTransform);
            }

            if (gameManager.timerText != null)
            {
                AddProtectedRect(result, promptSpace, gameManager.timerText.rectTransform);
            }

            if (gameManager.timerSlider != null)
            {
                AddProtectedRect(result, promptSpace, gameManager.timerSlider.transform as RectTransform);
            }

            if (gameManager.nextRoundButton != null)
            {
                AddProtectedRect(result, promptSpace, gameManager.nextRoundButton.transform as RectTransform);
            }

            if (handPointer != null && handPointer.gameObject.activeInHierarchy)
            {
                AddProtectedRect(result, promptSpace, handPointer.rectTransform);
            }

            return result;
        }

        private void AddProtectedRect(List<Rect> list, RectTransform space, RectTransform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(space, target);
            list.Add(BoundsToRect(bounds));
        }

        private Rect BoundsToRect(Bounds bounds)
        {
            return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
        }

        private Vector2 ClampPromptCenter(Vector2 center, Rect canvasRect, Vector2 halfSize)
        {
            float minX = canvasRect.xMin + halfSize.x + promptCanvasPadding;
            float maxX = canvasRect.xMax - halfSize.x - promptCanvasPadding;
            float minY = canvasRect.yMin + halfSize.y + promptCanvasPadding;
            float maxY = canvasRect.yMax - halfSize.y - promptCanvasPadding;

            if (minX <= maxX)
            {
                center.x = Mathf.Clamp(center.x, minX, maxX);
            }

            if (minY <= maxY)
            {
                center.y = Mathf.Clamp(center.y, minY, maxY);
            }

            return center;
        }

        private float GetOverlapArea(Rect a, Rect b)
        {
            float width = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
            float height = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            return width * height;
        }

        private Camera GetCanvasCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        private Vector2 ClampHandPositionInsideParent(Vector2 position, RectTransform parent, RectTransform handTransform)
        {
            float halfWidth = Mathf.Abs(handTransform.rect.width * handBaseScale.x) * 0.5f;
            float halfHeight = Mathf.Abs(handTransform.rect.height * handBaseScale.y) * 0.5f;
            Rect parentRect = parent.rect;

            float minX = parentRect.xMin + halfWidth + handCanvasPadding;
            float maxX = parentRect.xMax - halfWidth - handCanvasPadding;
            float minY = parentRect.yMin + halfHeight + handCanvasPadding;
            float maxY = parentRect.yMax - halfHeight - handCanvasPadding;

            if (minX <= maxX)
            {
                position.x = Mathf.Clamp(position.x, minX, maxX);
            }

            if (minY <= maxY)
            {
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }

            return position;
        }

        private void HideHand()
        {
            if (handPointer == null)
            {
                return;
            }

            handPointer.rectTransform.DOKill();
            handPointer.gameObject.SetActive(false);
        }

        private void SetClickCatcherActive(bool active)
        {
            if (clickCatcher != null)
            {
                clickCatcher.gameObject.SetActive(active);
                clickCatcher.interactable = active;
            }
        }

        private void SetTutorialVisible(bool visible)
        {
            if (tutorialCanvasGroup != null)
            {
                tutorialCanvasGroup.alpha = visible ? 1f : 0f;
                tutorialCanvasGroup.interactable = visible;
                tutorialCanvasGroup.blocksRaycasts = visible;
            }

            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                HideHand();
                SetClickCatcherActive(false);
                if (promptPanel != null)
                {
                    promptPanel.gameObject.SetActive(false);
                }
            }
        }

        private void RemoveNextRoundListener()
        {
            if (gameManager != null && gameManager.nextRoundButton != null)
            {
                gameManager.nextRoundButton.onClick.RemoveListener(HandleTutorialNextRoundClicked);
            }
        }

        private void CleanupTutorialVisuals()
        {
            tutorialActive = false;
            currentStage = TutorialStage.None;
            correctCard = null;
            StopAllCoroutines();
            correctHintCoroutine = null;
            nextRoundCoroutine = null;
            RemoveNextRoundListener();

            if (promptPanel != null)
            {
                promptPanel.DOKill();
                promptPanel.localScale = promptBaseScale;
            }
            promptMoveTween = null;
            promptPulseTween = null;

            HideHand();
            SetTutorialVisible(false);
        }
    }
}
