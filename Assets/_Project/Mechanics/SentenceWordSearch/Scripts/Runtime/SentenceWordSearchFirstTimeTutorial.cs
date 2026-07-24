using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SentenceWordSearchTutorialStage
{
    None,
    ReadSentence,
    FindGrid,
    DragDemonstration,
    FirstPractice,
    HintButton,
    HintPractice,
    Complete
}

[DisallowMultipleComponent]
public class SentenceWordSearchFirstTimeTutorial : MonoBehaviour
{
    [Header("Gameplay References")]
    public SentenceWordSearchManager manager;
    public SentenceWordSearchBoard board;
    public SentenceWordSearchInputController inputController;
    public SentenceWordSearchUI ui;

    [Header("Tutorial UI References")]
    public RectTransform tutorialCanvasRoot;
    public CanvasGroup tutorialCanvasGroup;
    public RectTransform instructionPanel;
    public CanvasGroup instructionPanelCanvasGroup;
    public TextMeshProUGUI instructionText;
    public RectTransform handPointer;
    public Image handPointerImage;
    public RectTransform focusFrame;

    [Header("First-Time Behaviour")]
    [Tooltip("Runs the tutorial regardless of the saved completion value. Successful completion is still saved.")]
    public bool forcePlay;
    [Tooltip("Testing option. Deletes this scene's tutorial completion once when entering Play Mode.")]
    public bool resetCompletionBeforePlay;

    [Header("Practice Content")]
    [Min(4)] public int practiceRows = 7;
    [Min(4)] public int practiceColumns = 7;
    [TextArea(1, 3)] public string firstPracticeSentence = "A bird can _________.";
    public string firstPracticeWord = "FLY";
    public Vector2Int firstPracticeStart = new Vector2Int(2, 1);
    public Vector2Int firstPracticeDirection = Vector2Int.right;
    [TextArea(1, 3)] public string hintPracticeSentence = "Ice feels _________.";
    public string hintPracticeWord = "COLD";
    public Vector2Int hintPracticeStart = new Vector2Int(4, 1);
    public Vector2Int hintPracticeDirection = Vector2Int.right;

    [Header("Hand Pointer")]
    public Vector2 sentencePointerOffset = new Vector2(0f, -100f);
    public Vector2 boardPointerOffset = new Vector2(0f, 80f);
    public Vector2 hintPointerOffset = new Vector2(0f, -85f);
    public Vector2 letterPointerOffset = new Vector2(0f, -70f);
    public Vector2 finalPointerOffset = new Vector2(0f, -95f);
    [Tooltip("Moves every hand-pointer position upward without changing the individual target offsets.")]
    public float handVerticalAdjustment = 45f;
    public float handTapScale = 0.82f;
    public float handTapDuration = 0.34f;
    public float handMoveDuration = 0.28f;

    [Header("Demonstration")]
    [Min(4)] public int initialDemonstrationRepeats = 4;
    [Min(1)] public int idleDemonstrationRepeats = 2;
    [Min(3f)] public float idleReplayDelay = 15f;
    [Range(0.05f, 0.8f)] public float demonstrationGhostAlpha = 0.34f;
    public float demonstrationCellTravelDuration = 0.32f;
    public float demonstrationPause = 0.45f;

    [Header("Focus And Instruction")]
    public float focusPadding = 24f;
    [Tooltip("Compact instruction shape used only in the free left or right side space.")]
    public Vector2 compactInstructionSize = new Vector2(430f, 220f);
    public Vector2 minimumInstructionSize = new Vector2(280f, 130f);
    public float instructionTargetGap = 28f;
    public float instructionScreenMargin = 24f;
    [Tooltip("Moves the side instruction panel slightly toward the centre while keeping its full size.")]
    public float instructionCenterNudge = 65f;
    public Vector2 normalInstructionPosition = new Vector2(0f, 250f);
    public Vector2 finalInstructionPosition = Vector2.zero;
    public float instructionPulseAmount = 1.025f;
    public float instructionPulseDuration = 0.55f;
    public float instructionMoveDuration = 0.32f;
    public float instructionFadeDuration = 0.16f;
    public float focusMoveDuration = 0.26f;
    public float stageTransitionPause = 0.14f;
    public float wrongFeedbackDuration = 0.6f;

    public SentenceWordSearchTutorialStage CurrentStage { get; private set; }
    public bool IsRunning { get; private set; }
    public bool CompletedSuccessfully { get; private set; }
    public bool CanAcceptPracticeInput =>
        IsRunning &&
        !abortRequested &&
        !demonstrationPlaying &&
        !selectionProcessing &&
        (CurrentStage == SentenceWordSearchTutorialStage.FirstPractice ||
         CurrentStage == SentenceWordSearchTutorialStage.HintPractice);

    private readonly Dictionary<Selectable, bool> savedInteractableStates = new Dictionary<Selectable, bool>();
    private Sequence instructionPulse;
    private Sequence instructionTransition;
    private Sequence handTapSequence;
    private Tween instructionMoveTween;
    private Tween instructionSizeTween;
    private bool abortRequested;
    private bool resetApplied;
    private bool demonstrationPlaying;
    private bool practiceSucceeded;
    private bool selectionProcessing;
    private bool hintPressed;
    private float lastMeaningfulInputTime;
    private string expectedPracticeWord;
    private string defaultPracticeInstruction;

    private string CompletionKey =>
        $"SentenceWordSearch.InteractiveTutorial.Completed.{SceneManager.GetActiveScene().name}";

    private Camera EventCamera => inputController != null ? inputController.EventCamera : null;

    private void Awake()
    {
        if (tutorialCanvasRoot == null)
            tutorialCanvasRoot = transform as RectTransform;

        SetTutorialVisualsVisible(false);
    }

    public void AssignGameplayReferences(
        SentenceWordSearchManager gameManager,
        SentenceWordSearchBoard gameBoard,
        SentenceWordSearchInputController gameInput,
        SentenceWordSearchUI gameUi)
    {
        manager = gameManager;
        board = gameBoard;
        inputController = gameInput;
        ui = gameUi;

        if (inputController != null)
            inputController.tutorialController = this;
    }

    public bool ShouldPlayTutorial()
    {
        if (resetCompletionBeforePlay && !resetApplied)
        {
            ResetCompletionForThisScene();
            resetApplied = true;
        }

        return forcePlay || PlayerPrefs.GetInt(CompletionKey, 0) == 0;
    }

    public IEnumerator RunTutorial()
    {
        if (IsRunning)
            yield break;

        ResolveMissingReferences();

        if (!HasRequiredReferences())
        {
            Debug.LogError("Sentence Word Search tutorial is missing required references. Run the tutorial installer again.");
            yield break;
        }

        abortRequested = false;
        CompletedSuccessfully = false;
        IsRunning = true;
        CurrentStage = SentenceWordSearchTutorialStage.None;

        SaveAndDisableNormalButtons();
        HookTutorialButtons();
        SetTutorialVisualsVisible(true);

        if (instructionPanel != null)
            instructionPanel.sizeDelta = compactInstructionSize;

        List<string> words = new List<string> { firstPracticeWord, hintPracticeWord };
        List<Vector2Int> starts = new List<Vector2Int> { firstPracticeStart, hintPracticeStart };
        List<Vector2Int> directions = new List<Vector2Int> { firstPracticeDirection, hintPracticeDirection };

        bool boardReady = board.BuildPracticeBoard(
            practiceRows,
            practiceColumns,
            words,
            starts,
            directions,
            ui != null ? ui.primaryFont : null);

        yield return null;
        Canvas.ForceUpdateCanvases();

        if (!boardReady || !HasPracticePaths())
        {
            Debug.LogError("Sentence Word Search tutorial could not build a safe practice board.");
            AbortTutorial();
            yield break;
        }

        if (handPointerImage != null && handPointerImage.sprite == null)
            Debug.LogWarning("Tutorial Hand Pointer Image has no sprite. Assign the hand sprite in the Inspector.");

        yield return RunReadSentenceStage();
        if (abortRequested) { FinishAbortedRun(); yield break; }

        yield return RunFindGridStage();
        if (abortRequested) { FinishAbortedRun(); yield break; }

        yield return RunInitialDemonstrationStage();
        if (abortRequested) { FinishAbortedRun(); yield break; }

        yield return RunPracticeStage(firstPracticeWord, false);
        if (abortRequested) { FinishAbortedRun(); yield break; }

        yield return RunHintStage();
        if (abortRequested) { FinishAbortedRun(); yield break; }

        yield return RunPracticeStage(hintPracticeWord, true);
        if (abortRequested) { FinishAbortedRun(); yield break; }

        yield return RunCompleteStage();
        if (abortRequested) { FinishAbortedRun(); yield break; }

        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();
        CompletedSuccessfully = true;
        CleanupTutorialRuntime();
    }

    private IEnumerator RunReadSentenceStage()
    {
        CurrentStage = SentenceWordSearchTutorialStage.ReadSentence;
        ui.ShowQuestion(CreatePracticeQuestion(firstPracticeSentence, firstPracticeWord), 1, 2);
        SetInstruction("Read the sentence. Which word is missing? Tap anywhere to continue.");
        PositionInstructionAround(ui.sentenceText != null ? ui.sentenceText.rectTransform : null);
        FocusOn(ui.sentenceText != null ? ui.sentenceText.rectTransform : null);
        PointHandAt(ui.sentenceText != null ? ui.sentenceText.rectTransform : null, sentencePointerOffset, true);
        yield return WaitForTapAnywhere();
    }

    private IEnumerator RunFindGridStage()
    {
        CurrentStage = SentenceWordSearchTutorialStage.FindGrid;
        SetInstruction("The missing word is hiding in the letter grid. Tap anywhere to continue.");
        PositionInstructionAround(board.gridParent);
        FocusOn(board.gridParent);
        PointHandAt(board.gridParent, boardPointerOffset, true);
        yield return WaitForTapAnywhere();
    }

    private IEnumerator RunInitialDemonstrationStage()
    {
        CurrentStage = SentenceWordSearchTutorialStage.DragDemonstration;
        SetInstruction("Hold the first letter and drag to the last letter.");
        PositionInstructionAround(board.gridParent);
        FocusOn(board.gridParent);
        yield return PlayDragDemonstration(firstPracticeWord, Mathf.Max(4, initialDemonstrationRepeats));
        SetInstruction("Tap anywhere when you are ready to try.");
        PositionInstructionAround(board.gridParent);
        HideHand();
        yield return WaitForTapAnywhere();
    }

    private IEnumerator RunPracticeStage(string word, bool isHintPractice)
    {
        CurrentStage = isHintPractice
            ? SentenceWordSearchTutorialStage.HintPractice
            : SentenceWordSearchTutorialStage.FirstPractice;

        if (isHintPractice)
            ui.ShowQuestion(CreatePracticeQuestion(hintPracticeSentence, hintPracticeWord), 2, 2);

        expectedPracticeWord = SentenceWordSearchManager.CleanWordStatic(word);
        practiceSucceeded = false;
        selectionProcessing = false;
        defaultPracticeInstruction = isHintPractice
            ? $"Now drag across {FormatLetters(expectedPracticeWord)}."
            : $"Now you try! Drag across {FormatLetters(expectedPracticeWord)}.";

        SetInstruction(defaultPracticeInstruction);
        PositionInstructionAround(board.gridParent);
        FocusOn(board.gridParent);
        HideHand();

        yield return new WaitForSecondsRealtime(instructionFadeDuration * 1.65f);

        lastMeaningfulInputTime = Time.unscaledTime;
        inputController.SetInputEnabled(true);

        while (!practiceSucceeded && !abortRequested)
        {
            if (!selectionProcessing && Time.unscaledTime - lastMeaningfulInputTime >= idleReplayDelay)
            {
                inputController.SetInputEnabled(false);
                SetInstruction("Watch once more, then you try.");
                yield return PlayDragDemonstration(word, Mathf.Max(1, idleDemonstrationRepeats));
                SetInstruction(defaultPracticeInstruction);
                PositionInstructionAround(board.gridParent);
                HideHand();
                yield return new WaitForSecondsRealtime(instructionFadeDuration * 1.65f);
                lastMeaningfulInputTime = Time.unscaledTime;
                inputController.SetInputEnabled(true);
            }

            yield return null;
        }

        inputController.SetInputEnabled(false);
        HideHand();
    }

    private IEnumerator RunHintStage()
    {
        CurrentStage = SentenceWordSearchTutorialStage.HintButton;
        ui.ShowQuestion(CreatePracticeQuestion(hintPracticeSentence, hintPracticeWord), 2, 2);
        board.ClearPreview();
        hintPressed = false;

        if (ui.hintButton != null)
            ui.hintButton.interactable = true;

        SetInstruction("Need help? Tap HINT.");
        RectTransform hintRect = ui.hintButton != null ? ui.hintButton.transform as RectTransform : null;
        PositionInstructionAround(hintRect);
        FocusOn(hintRect);
        PointHandAt(hintRect, hintPointerOffset, true);

        while (!hintPressed && !abortRequested)
            yield return null;

        if (ui.hintButton != null)
            ui.hintButton.interactable = false;

        if (abortRequested)
            yield break;

        SetInstruction("Hint shows the first and last letters. Tap anywhere to continue.");
        List<SentenceWordSearchCell> path = board.GetPlacedWordPath(hintPracticeWord);
        PositionInstructionAround(board.gridParent);
        FocusOn(board.gridParent);

        if (path.Count > 0)
        {
            PointHandAt(path[0].RectTransform, letterPointerOffset, true);
            yield return new WaitForSecondsRealtime(0.9f);
            PointHandAt(path[path.Count - 1].RectTransform, letterPointerOffset, true);
            yield return new WaitForSecondsRealtime(0.9f);
        }

        yield return WaitForTapAnywhere();
    }

    private IEnumerator RunCompleteStage()
    {
        CurrentStage = SentenceWordSearchTutorialStage.Complete;
        board.StopAllHintPulses();
        HideFocus();

        if (instructionPanel != null)
            instructionPanel.sizeDelta = compactInstructionSize;

        SetInstruction("Great job! You’re ready to play. Tap anywhere to start.");
        PositionInstructionAround(board.gridParent);
        yield return new WaitForSecondsRealtime(instructionMoveDuration);
        PointHandAt(instructionPanel, finalPointerOffset, true);
        yield return WaitForTapAnywhere(0.25f);
    }

    private IEnumerator PlayDragDemonstration(string word, int repeats)
    {
        List<SentenceWordSearchCell> path = board.GetPlacedWordPath(word);

        if (path.Count == 0)
            yield break;

        demonstrationPlaying = true;
        inputController.SetInputEnabled(false);
        StopHandTweens();

        for (int repeat = 0; repeat < repeats && !abortRequested; repeat++)
        {
            board.ClearPreview();
            PointHandAt(path[0].RectTransform, letterPointerOffset, false);
            yield return new WaitForSecondsRealtime(0.35f);

            if (handPointer != null && HandSpriteAvailable())
            {
                Tween holdTween = handPointer.DOScale(Vector3.one * handTapScale, handTapDuration * 0.5f)
                    .SetEase(Ease.OutSine)
                    .SetUpdate(true);
                yield return holdTween.WaitForCompletion();
            }

            List<SentenceWordSearchCell> ghostPath = new List<SentenceWordSearchCell>();

            for (int i = 0; i < path.Count && !abortRequested; i++)
            {
                ghostPath.Add(path[i]);
                board.SetPreviewPath(ghostPath);
                ApplyDemonstrationGhostAlpha(ghostPath);

                if (handPointer != null && HandSpriteAvailable())
                {
                    Vector2 destination = GetAnchoredPosition(path[i].RectTransform) + letterPointerOffset;
                    Tween moveTween = handPointer.DOAnchorPos(destination, demonstrationCellTravelDuration)
                        .SetEase(Ease.InOutSine)
                        .SetUpdate(true);
                    yield return moveTween.WaitForCompletion();
                }
                else
                {
                    yield return new WaitForSecondsRealtime(demonstrationCellTravelDuration);
                }
            }

            if (handPointer != null && HandSpriteAvailable())
            {
                Tween releaseTween = handPointer.DOScale(Vector3.one, handTapDuration * 0.5f)
                    .SetEase(Ease.OutSine)
                    .SetUpdate(true);
                yield return releaseTween.WaitForCompletion();
            }

            yield return new WaitForSecondsRealtime(demonstrationPause);
            board.ClearPreview();
        }

        demonstrationPlaying = false;
    }

    public void NotifyPracticeDragStarted()
    {
        if (!CanAcceptPracticeInput)
            return;

        lastMeaningfulInputTime = Time.unscaledTime;
        HideHand();
    }

    public void NotifyPracticeDragMoved()
    {
        if (CanAcceptPracticeInput)
            lastMeaningfulInputTime = Time.unscaledTime;
    }

    public void SubmitPracticeSelection(string selectedWord, List<SentenceWordSearchCell> selectedPath)
    {
        if (!CanAcceptPracticeInput)
            return;

        lastMeaningfulInputTime = Time.unscaledTime;
        StartCoroutine(EvaluatePracticeSelection(selectedWord, selectedPath));
    }

    private IEnumerator EvaluatePracticeSelection(string selectedWord, List<SentenceWordSearchCell> selectedPath)
    {
        selectionProcessing = true;
        string selected = SentenceWordSearchManager.CleanWordStatic(selectedWord);

        if (selected == expectedPracticeWord)
        {
            inputController.SetInputEnabled(false);
            board.ClearPreview();
            board.MarkWordSolved(expectedPracticeWord);

            Vector2 popupPosition = board.GetPathCenterScreenPosition(selectedPath, EventCamera);

            if (ui != null)
                yield return ui.AnimateWordToSentence(expectedPracticeWord, popupPosition, EventCamera);

            SetInstruction("Great job!");
            yield return new WaitForSecondsRealtime(0.65f);
            practiceSucceeded = true;
        }
        else
        {
            board.FlashWrongPath(selectedPath, wrongFeedbackDuration);
            SetInstruction($"Almost! Start at {expectedPracticeWord[0]} and drag to {expectedPracticeWord[expectedPracticeWord.Length - 1]}.");
            PositionInstructionAround(board.gridParent);
            yield return new WaitForSecondsRealtime(wrongFeedbackDuration);
            board.ClearPreview();
            SetInstruction(defaultPracticeInstruction);
            PositionInstructionAround(board.gridParent);
            HideHand();
        }

        selectionProcessing = false;
        lastMeaningfulInputTime = Time.unscaledTime;
    }

    private void OnTutorialHintPressed()
    {
        if (!IsRunning || CurrentStage != SentenceWordSearchTutorialStage.HintButton)
            return;

        hintPressed = true;
        board.PulseHintForWord(hintPracticeWord);
        HideHand();
    }

    private IEnumerator WaitForTapAnywhere(float minimumDelay = 0.18f)
    {
        float readyTime = Time.unscaledTime + minimumDelay;

        while (!abortRequested)
        {
            if (Time.unscaledTime >= readyTime && WasPointerReleasedThisFrame())
            {
                if (stageTransitionPause > 0f)
                    yield return new WaitForSecondsRealtime(stageTransitionPause);

                yield break;
            }

            yield return null;
        }
    }

    private bool WasPointerReleasedThisFrame()
    {
        if (Input.GetMouseButtonUp(0))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended)
                return true;
        }

        return false;
    }

    private void SetInstruction(string value)
    {
        if (instructionTransition != null)
        {
            instructionTransition.Kill();
            instructionTransition = null;
        }

        if (instructionPanelCanvasGroup != null)
        {
            instructionPanelCanvasGroup.DOKill();
            instructionTransition = DOTween.Sequence()
                .Append(instructionPanelCanvasGroup.DOFade(0f, instructionFadeDuration * 0.65f).SetEase(Ease.OutSine))
                .AppendCallback(() =>
                {
                    if (instructionText != null)
                        instructionText.text = value;
                })
                .Append(instructionPanelCanvasGroup.DOFade(1f, instructionFadeDuration).SetEase(Ease.InSine))
                .SetUpdate(true);
        }
        else if (instructionText != null)
        {
            instructionText.text = value;
        }

        if (instructionPanel == null)
            return;

        if (instructionPulse != null)
            instructionPulse.Kill();

        instructionPanel.localScale = Vector3.one;
        instructionPulse = DOTween.Sequence()
            .SetDelay(instructionFadeDuration)
            .Append(instructionPanel.DOScale(instructionPulseAmount, instructionPulseDuration).SetEase(Ease.InOutSine))
            .Append(instructionPanel.DOScale(1f, instructionPulseDuration).SetEase(Ease.InOutSine))
            .SetLoops(-1)
            .SetUpdate(true);
    }

    private void PositionInstructionAround(RectTransform target)
    {
        if (target == null && board != null)
            target = board.gridParent;

        if (instructionPanel == null || tutorialCanvasRoot == null || target == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (!TryGetLocalBounds(target, out Vector2 targetMin, out Vector2 targetMax))
            return;

        Rect rootRect = tutorialCanvasRoot.rect;
        float left = rootRect.xMin + instructionScreenMargin;
        float right = rootRect.xMax - instructionScreenMargin;
        float bottom = rootRect.yMin + instructionScreenMargin;
        float top = rootRect.yMax - instructionScreenMargin;
        float targetCenterX = (targetMin.x + targetMax.x) * 0.5f;
        bool useLeftSide = targetCenterX >= 0f;
        Vector2 panelSize = new Vector2(
            Mathf.Min(compactInstructionSize.x, Mathf.Max(1f, right - left)),
            Mathf.Min(compactInstructionSize.y, Mathf.Max(1f, top - bottom)));

        Vector2 targetCenter = (targetMin + targetMax) * 0.5f;
        Vector2 panelPosition = new Vector2(
            useLeftSide
                ? left + panelSize.x * 0.5f + instructionCenterNudge
                : right - panelSize.x * 0.5f - instructionCenterNudge,
            targetCenter.y);

        float minX = left + panelSize.x * 0.5f;
        float maxX = right - panelSize.x * 0.5f;
        panelPosition.x = Mathf.Clamp(panelPosition.x, minX, maxX);

        float minY = bottom + panelSize.y * 0.5f;
        float maxY = top - panelSize.y * 0.5f;
        panelPosition.y = Mathf.Clamp(targetCenter.y, minY, maxY);

        if (instructionMoveTween != null)
            instructionMoveTween.Kill();

        if (instructionSizeTween != null)
            instructionSizeTween.Kill();

        instructionMoveTween = instructionPanel.DOAnchorPos(panelPosition, instructionMoveDuration)
            .SetEase(Ease.InOutCubic)
            .SetUpdate(true);

        instructionSizeTween = instructionPanel.DOSizeDelta(panelSize, instructionMoveDuration)
            .SetEase(Ease.InOutCubic)
            .SetUpdate(true);
    }

    private void PointHandAt(RectTransform target, Vector2 offset, bool tap)
    {
        StopHandTweens();

        if (handPointer == null || target == null || !HandSpriteAvailable())
        {
            HideHand();
            return;
        }

        bool wasVisible = handPointer.gameObject.activeSelf;
        Vector2 destination = GetAnchoredPosition(target) + offset + new Vector2(0f, handVerticalAdjustment);
        handPointer.gameObject.SetActive(true);
        handPointer.localScale = Vector3.one;

        if (wasVisible)
        {
            handPointer.DOAnchorPos(destination, handMoveDuration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }
        else
        {
            handPointer.anchoredPosition = destination;
        }

        if (tap)
        {
            handTapSequence = DOTween.Sequence()
                .Append(handPointer.DOScale(handTapScale, handTapDuration).SetEase(Ease.InOutSine))
                .Append(handPointer.DOScale(1f, handTapDuration).SetEase(Ease.InOutSine))
                .SetLoops(-1)
                .SetUpdate(true);
        }
    }

    private void FocusOn(RectTransform target)
    {
        if (focusFrame == null || target == null || tutorialCanvasRoot == null)
        {
            HideFocus();
            return;
        }

        if (!TryGetLocalBounds(target, out Vector2 min, out Vector2 max))
        {
            HideFocus();
            return;
        }

        Vector2 destination = (min + max) * 0.5f;
        Vector2 destinationSize = max - min + Vector2.one * focusPadding * 2f;
        bool wasVisible = focusFrame.gameObject.activeSelf;

        focusFrame.DOKill();
        focusFrame.gameObject.SetActive(true);

        if (!wasVisible)
        {
            focusFrame.anchoredPosition = destination;
            focusFrame.sizeDelta = destinationSize;
            focusFrame.localScale = Vector3.one * 0.96f;
            focusFrame.DOScale(1f, focusMoveDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }
        else
        {
            focusFrame.DOAnchorPos(destination, focusMoveDuration)
                .SetEase(Ease.InOutCubic)
                .SetUpdate(true);

            focusFrame.DOSizeDelta(destinationSize, focusMoveDuration)
                .SetEase(Ease.InOutCubic)
                .SetUpdate(true);
        }
    }

    private bool TryGetLocalBounds(RectTransform target, out Vector2 min, out Vector2 max)
    {
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);

        if (target == null || tutorialCanvasRoot == null)
            return false;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(EventCamera, corners[i]);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    tutorialCanvasRoot,
                    screen,
                    EventCamera,
                    out Vector2 local))
                continue;

            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        return min.x != float.MaxValue && max.x != float.MinValue;
    }

    private Vector2 GetAnchoredPosition(RectTransform target)
    {
        if (target == null || tutorialCanvasRoot == null)
            return Vector2.zero;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(EventCamera, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(tutorialCanvasRoot, screen, EventCamera, out Vector2 local);
        return local;
    }

    private void ApplyDemonstrationGhostAlpha(List<SentenceWordSearchCell> path)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Image preview = path[i] != null ? path[i].previewOverlayImage : null;

            if (preview == null)
                continue;

            Color color = preview.color;
            color.a = demonstrationGhostAlpha;
            preview.color = color;
        }
    }

    private RectTransform GetFirstCellRect(string word)
    {
        List<SentenceWordSearchCell> path = board.GetPlacedWordPath(word);
        return path.Count > 0 ? path[0].RectTransform : board.gridParent;
    }

    private bool HasPracticePaths()
    {
        return board.GetPlacedWordPath(firstPracticeWord).Count > 0 &&
               board.GetPlacedWordPath(hintPracticeWord).Count > 0;
    }

    private SentenceWordSearchQuestion CreatePracticeQuestion(string sentence, string answer)
    {
        return new SentenceWordSearchQuestion
        {
            sentenceWithBlank = sentence,
            answer = answer
        };
    }

    private string FormatLetters(string word)
    {
        if (string.IsNullOrEmpty(word))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(word.Length * 2 - 1);

        for (int i = 0; i < word.Length; i++)
        {
            if (i > 0)
                builder.Append('–');

            builder.Append(word[i]);
        }

        return builder.ToString();
    }

    private void SaveAndDisableNormalButtons()
    {
        savedInteractableStates.Clear();
        SaveAndSetButton(ui != null ? ui.pauseButton : null, false);
        SaveAndSetButton(ui != null ? ui.howToPlayButton : null, false);
        SaveAndSetButton(ui != null ? ui.resumeButton : null, false);
        SaveAndSetButton(ui != null ? ui.restartButton : null, false);
        SaveAndSetButton(ui != null ? ui.resultRestartButton : null, false);
        SaveAndSetButton(ui != null ? ui.resultContinueButton : null, false);
        SaveAndSetButton(ui != null ? ui.hintButton : null, false);
    }

    private void SaveAndSetButton(Selectable selectable, bool interactable)
    {
        if (selectable == null)
            return;

        if (!savedInteractableStates.ContainsKey(selectable))
            savedInteractableStates.Add(selectable, selectable.interactable);

        selectable.interactable = interactable;
    }

    private void RestoreNormalButtons()
    {
        foreach (KeyValuePair<Selectable, bool> state in savedInteractableStates)
        {
            if (state.Key != null)
                state.Key.interactable = state.Value;
        }

        savedInteractableStates.Clear();
    }

    private void HookTutorialButtons()
    {
        if (ui != null && ui.hintButton != null)
        {
            ui.hintButton.onClick.RemoveListener(OnTutorialHintPressed);
            ui.hintButton.onClick.AddListener(OnTutorialHintPressed);
        }
    }

    private void UnhookTutorialButtons()
    {
        if (ui != null && ui.hintButton != null)
            ui.hintButton.onClick.RemoveListener(OnTutorialHintPressed);
    }

    private void SetTutorialVisualsVisible(bool visible)
    {
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.alpha = visible ? 1f : 0f;
            tutorialCanvasGroup.interactable = false;
            tutorialCanvasGroup.blocksRaycasts = false;
        }

        if (!visible)
        {
            HideHand();
            HideFocus();
        }
    }

    private bool HandSpriteAvailable()
    {
        return handPointerImage != null && handPointerImage.sprite != null;
    }

    private void HideHand()
    {
        StopHandTweens();

        if (handPointer != null)
            handPointer.gameObject.SetActive(false);
    }

    private void StopHandTweens()
    {
        if (handTapSequence != null)
        {
            handTapSequence.Kill();
            handTapSequence = null;
        }

        if (handPointer != null)
        {
            handPointer.DOKill();
            handPointer.localScale = Vector3.one;
        }
    }

    private void HideFocus()
    {
        if (focusFrame != null)
        {
            focusFrame.DOKill();
            focusFrame.localScale = Vector3.one;
            focusFrame.gameObject.SetActive(false);
        }
    }

    private void ResolveMissingReferences()
    {
        if (manager == null)
            manager = FindObjectOfType<SentenceWordSearchManager>();

        if (board == null)
            board = FindObjectOfType<SentenceWordSearchBoard>();

        if (inputController == null)
            inputController = FindObjectOfType<SentenceWordSearchInputController>();

        if (ui == null)
            ui = FindObjectOfType<SentenceWordSearchUI>();

        if (tutorialCanvasRoot == null)
            tutorialCanvasRoot = transform as RectTransform;

        if (instructionPanelCanvasGroup == null && instructionPanel != null)
            instructionPanelCanvasGroup = instructionPanel.GetComponent<CanvasGroup>();
    }

    private bool HasRequiredReferences()
    {
        return manager != null && board != null && inputController != null && ui != null &&
               tutorialCanvasRoot != null && tutorialCanvasGroup != null &&
               instructionPanel != null && instructionText != null;
    }

    private void FinishAbortedRun()
    {
        CompletedSuccessfully = false;
        CleanupTutorialRuntime();
    }

    private void CleanupTutorialRuntime()
    {
        if (inputController != null)
            inputController.SetInputEnabled(false);

        if (instructionPulse != null)
        {
            instructionPulse.Kill();
            instructionPulse = null;
        }

        if (instructionTransition != null)
        {
            instructionTransition.Kill();
            instructionTransition = null;
        }

        if (instructionMoveTween != null)
        {
            instructionMoveTween.Kill();
            instructionMoveTween = null;
        }

        if (instructionSizeTween != null)
        {
            instructionSizeTween.Kill();
            instructionSizeTween = null;
        }

        if (instructionPanelCanvasGroup != null)
        {
            instructionPanelCanvasGroup.DOKill();
            instructionPanelCanvasGroup.alpha = 1f;
        }

        StopHandTweens();
        board?.ClearPreview();
        board?.StopAllHintPulses();
        UnhookTutorialButtons();
        RestoreNormalButtons();
        SetTutorialVisualsVisible(false);

        if (instructionPanel != null)
        {
            instructionPanel.localScale = Vector3.one;
            instructionPanel.sizeDelta = compactInstructionSize;
            instructionPanel.anchoredPosition = normalInstructionPosition;
        }

        IsRunning = false;
        CurrentStage = SentenceWordSearchTutorialStage.None;
        demonstrationPlaying = false;
        selectionProcessing = false;
    }

    public void AbortTutorial()
    {
        if (!IsRunning)
            return;

        abortRequested = true;
        StopAllCoroutines();
        CompletedSuccessfully = false;
        CleanupTutorialRuntime();
    }

    [ContextMenu("Reset Tutorial Completion For This Scene")]
    public void ResetCompletionForThisScene()
    {
        PlayerPrefs.DeleteKey(CompletionKey);
        PlayerPrefs.Save();
    }

    [ContextMenu("Mark Tutorial Complete For This Scene")]
    public void MarkCompleteForThisScene()
    {
        PlayerPrefs.SetInt(CompletionKey, 1);
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        if (IsRunning)
            AbortTutorial();
    }
}
