using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GridAdventureFirstTimeTutorialController : MonoBehaviour
{
    private enum TutorialStage
    {
        None,
        Clue,
        ActiveCell,
        Basket,
        Hint,
        DragPractice,
        Success
    }

    [Header("Core References")]
    public GridAdventureManager manager;
    public Canvas rootCanvas;
    public Button backgroundContinueButton;
    public Image dimmerImage;
    public RectTransform practiceLayer;
    public RectTransform focusImage;

    [Header("Instruction")]
    public RectTransform instructionMotionRoot;
    public TextMeshProUGUI instructionText;
    public Image handImage;

    [Header("First-Time Behaviour")]
    public bool tutorialEnabled = true;
    public bool forcePlayForTesting;
    public bool resetSavedStatusOnPlay;
    [Tooltip("Base PlayerPrefs key. The current scene name is appended automatically.")]
    public string completionSaveKey = "GridAdventure.InteractiveTutorial.Completed";

    [Header("Stage Text")]
    [TextArea(2, 3)] public string clueInstruction = "Read the clue.\nTap anywhere to continue.";
    [TextArea(2, 3)] public string cellInstruction = "Place the picture in the glowing square.\nTap anywhere to continue.";
    [TextArea(2, 3)] public string basketInstruction = "Find the picture that matches the clue.\nTap anywhere to continue.";
    [TextArea(2, 3)] public string hintInstruction = "Need help? Tap the ? button.";
    [TextArea(2, 3)] public string dragInstruction = "Drag the matching picture into the glowing square.";
    [TextArea(2, 3)] public string retryInstruction = "Try again. Match the picture to the clue.";
    [TextArea(2, 3)] public string successInstruction = "You have successfully completed the tutorial!\nClick anywhere to start the game.";
    [HideInInspector] public int successStageVersion;

    [Header("Hand Placement")]
    [Tooltip("Normalised point inside the hand image that represents the visible fingertip. (0,1) is top-left.")]
    public Vector2 handTipNormalized = new Vector2(0f, 1f);
    public Vector2 clueHandOffset = new Vector2(0f, 8f);
    public float clueHandRotation = 180f;
    public Vector2 cellHandOffset = new Vector2(65f, -55f);
    public Vector2 basketHandOffset = new Vector2(0f, 20f);
    public Vector2 hintHandOffset = Vector2.zero;
    public float hintHandRotation = -90f;
    public Vector2 dragHandOffset = new Vector2(50f, -45f);
    [HideInInspector] public int pointerPlacementVersion;

    [Header("Animation")]
    [Min(0.1f)] public float pointerMoveDuration = 0.28f;
    [Min(0.2f)] public float dragDemoDuration = 2.2f;
    [Min(0f)] public float dragDemoStartHold = 0.65f;
    [Min(0f)] public float dragDemoTargetHold = 0.9f;
    [Min(0.05f)] public float wrongReturnDuration = 0.4f;
    [Min(0.05f)] public float correctSnapDuration = 0.28f;
    [HideInInspector] public float successHoldDuration = 1.2f;
    [Min(2f)] public float idleRepeatSeconds = 15f;
    [Range(0.1f, 0.9f)] public float ghostTransparency = 0.48f;
    [Range(0f, 0.5f)] public float dimmerAlpha = 0.16f;
    public Vector2 focusPadding = new Vector2(24f, 24f);
    [Min(0f)] public float handScreenEdgePadding = 24f;

    public bool CanAcceptPracticeInput
    {
        get { return isRunning && currentStage == TutorialStage.DragPractice && !isDemonstrating && !isUserDragging; }
    }

    private readonly List<GridAdventureItemCard> sourceCards = new List<GridAdventureItemCard>();
    private readonly List<GridAdventureTutorialPracticeCard> practiceCards = new List<GridAdventureTutorialPracticeCard>();
    private readonly List<GameObject> runtimeTutorialObjects = new List<GameObject>();

    private TutorialStage currentStage;
    private GridAdventureItemData practiceQuestion;
    private GridAdventureCell sourceTargetCell;
    private GridAdventureItemCard sourceCorrectCard;
    private GridAdventureTutorialPracticeCard correctPracticeCard;
    private RectTransform practiceTargetCell;
    private Button practiceHintButton;
    private RectTransform ghostCard;
    private Action completionCallback;
    private Sequence handSequence;
    private Sequence instructionSequence;
    private Sequence demonstrationSequence;
    private float lastPracticeActivityTime;
    private bool isRunning;
    private bool isDemonstrating;
    private bool isUserDragging;
    private bool isCleaningUp;
    private bool didApplyResetOnPlay;

    private void Awake()
    {
        CacheReferences();
        WireBackgroundButton();
        ApplyPointerPlacementUpgradeIfNeeded();
        ApplySuccessStageUpgradeIfNeeded();
        ApplyResetOnPlayIfNeeded();
    }

    private void OnEnable()
    {
        CacheReferences();
        WireBackgroundButton();
    }

    private void OnDisable()
    {
        if (!isCleaningUp && isRunning)
            StopTutorialWithoutCompleting();
    }

    private void OnDestroy()
    {
        if (backgroundContinueButton != null)
            backgroundContinueButton.onClick.RemoveListener(HandleBackgroundContinue);

        DOTween.Kill(this);
    }

    private void Update()
    {
        if (!isRunning || currentStage != TutorialStage.DragPractice || isDemonstrating || isUserDragging)
            return;

        if (Time.unscaledTime - lastPracticeActivityTime < idleRepeatSeconds)
            return;

        PlayDragDemonstration(EnablePracticeInput);
    }

    public bool TryStartTutorial(Action onCompleted)
    {
        CacheReferences();
        ApplyPointerPlacementUpgradeIfNeeded();
        ApplySuccessStageUpgradeIfNeeded();
        ApplyResetOnPlayIfNeeded();

        if (!tutorialEnabled || manager == null)
            return false;

        if (!forcePlayForTesting && PlayerPrefs.GetInt(GetCompletionKey(), 0) == 1)
            return false;

        if (rootCanvas == null || practiceLayer == null || backgroundContinueButton == null)
        {
            Debug.LogWarning(
                "Grid Adventure tutorial is missing required UI references. Run the additive tutorial installer again. Real gameplay will start normally.",
                this);
            return false;
        }

        if (!manager.TryGetTutorialPracticeQuestion(out practiceQuestion, out sourceTargetCell, out sourceCorrectCard))
        {
            Debug.LogWarning("Grid Adventure tutorial could not find a valid current-round question. Real gameplay will start normally.", this);
            return false;
        }

        manager.GetRuntimeCardsForTutorial(sourceCards);
        if (sourceCards.Count == 0)
        {
            Debug.LogWarning("Grid Adventure tutorial could not find runtime basket cards. Real gameplay will start normally.", this);
            return false;
        }

        completionCallback = onCompleted;
        manager.SetTutorialHold(true);
        isRunning = true;
        isDemonstrating = false;
        isUserDragging = false;

        gameObject.SetActive(true);
        if (dimmerImage != null)
        {
            Color dimColor = dimmerImage.color;
            dimColor.a = dimmerAlpha;
            dimmerImage.color = dimColor;
        }

        BuildPracticeVisuals();
        if (correctPracticeCard == null || practiceTargetCell == null)
        {
            Debug.LogWarning("Grid Adventure tutorial practice visuals could not be created. Real gameplay will start normally.", this);
            StopTutorialWithoutCompleting();
            return false;
        }

        ShowStage(TutorialStage.Clue);
        return true;
    }

    public void StopTutorialWithoutCompleting()
    {
        if (isCleaningUp)
            return;

        completionCallback = null;
        CleanupTutorialObjects();

        if (manager != null)
            manager.SetTutorialHold(false);
    }

    [ContextMenu("Reset Interactive Tutorial Completion")]
    public void ResetTutorialCompletion()
    {
        PlayerPrefs.DeleteKey(GetCompletionKey());
        PlayerPrefs.Save();
    }

    [ContextMenu("Mark Interactive Tutorial Complete")]
    public void MarkTutorialCompleteForTesting()
    {
        PlayerPrefs.SetInt(GetCompletionKey(), 1);
        PlayerPrefs.Save();
    }

    public void NotifyPracticeActivity()
    {
        lastPracticeActivityTime = Time.unscaledTime;
    }

    public void NotifyPracticeDragState(bool dragging)
    {
        isUserDragging = dragging;
        NotifyPracticeActivity();
    }

    public void ResolvePracticeDrop(GridAdventureTutorialPracticeCard card, bool droppedOnTarget)
    {
        if (!isRunning || currentStage != TutorialStage.DragPractice || card == null)
            return;

        NotifyPracticeActivity();
        bool correctItem = practiceQuestion != null &&
                           string.Equals(card.itemId, practiceQuestion.itemId, StringComparison.OrdinalIgnoreCase);

        if (!droppedOnTarget || !correctItem)
        {
            SetInstruction(retryInstruction);
            card.ReturnHome(wrongReturnDuration, delegate
            {
                if (isRunning && currentStage == TutorialStage.DragPractice)
                {
                    card.SetInputEnabled(true);
                    SetAllPracticeCardsInput(true);
                }
            });
            return;
        }

        SetAllPracticeCardsInput(false);
        card.SnapIntoTarget(correctSnapDuration, ShowPracticeSuccess);
    }

    private void HandleBackgroundContinue()
    {
        if (!isRunning)
            return;

        switch (currentStage)
        {
            case TutorialStage.Clue:
                ShowStage(TutorialStage.ActiveCell);
                break;

            case TutorialStage.ActiveCell:
                ShowStage(TutorialStage.Basket);
                break;

            case TutorialStage.Basket:
                ShowStage(TutorialStage.Hint);
                break;

            case TutorialStage.Success:
                CompleteTutorial();
                break;
        }
    }

    private void ShowStage(TutorialStage stage)
    {
        currentStage = stage;
        SetAllPracticeCardsInput(false);

        if (practiceHintButton != null)
            practiceHintButton.gameObject.SetActive(false);

        if (backgroundContinueButton != null)
            backgroundContinueButton.interactable =
                stage == TutorialStage.Clue ||
                stage == TutorialStage.ActiveCell ||
                stage == TutorialStage.Basket ||
                stage == TutorialStage.Success;

        switch (stage)
        {
            case TutorialStage.Clue:
                SetInstruction(clueInstruction);
                FocusTarget(manager != null ? manager.clueBanner : null);
                PointHandWithTip(
                    manager != null ? manager.clueBanner : null,
                    new Vector2(0.5f, 1f),
                    clueHandOffset,
                    clueHandRotation);
                break;

            case TutorialStage.ActiveCell:
                SetInstruction(cellInstruction);
                FocusTarget(practiceTargetCell);
                PointHand(practiceTargetCell, cellHandOffset);
                break;

            case TutorialStage.Basket:
                SetInstruction(basketInstruction);
                FocusTarget(manager != null ? manager.basketRoot as RectTransform : null);
                PointHand(manager != null ? manager.basketRoot as RectTransform : null, basketHandOffset);
                break;

            case TutorialStage.Hint:
                if (practiceHintButton == null)
                {
                    Debug.LogWarning(
                        "Grid Adventure tutorial could not create the practice Hint button. Skipping that stage so the tutorial remains finishable.",
                        this);
                    ShowStage(TutorialStage.DragPractice);
                    return;
                }

                SetInstruction(hintInstruction);
                FocusTarget(practiceHintButton.transform as RectTransform);
                practiceHintButton.gameObject.SetActive(true);
                practiceHintButton.interactable = true;
                PointHandWithTip(
                    practiceHintButton.transform as RectTransform,
                    new Vector2(0.5f, 0.5f),
                    hintHandOffset,
                    hintHandRotation);
                break;

            case TutorialStage.DragPractice:
                SetInstruction(dragInstruction);
                FocusTarget(practiceTargetCell);
                PlayDragDemonstration(EnablePracticeInput);
                break;

            case TutorialStage.Success:
                SetInstruction(successInstruction);
                HideHand();
                HideFocus();
                break;
        }
    }

    private void HandlePracticeHint()
    {
        if (!isRunning || currentStage != TutorialStage.Hint)
            return;

        if (practiceHintButton != null)
            practiceHintButton.interactable = false;

        HideHand();
        PulseCorrectPracticeCard();
        DOVirtual.DelayedCall(0.75f, delegate
        {
            if (isRunning)
                ShowStage(TutorialStage.DragPractice);
        }).SetUpdate(true).SetId(this);
    }

    private void EnablePracticeInput()
    {
        if (!isRunning || currentStage != TutorialStage.DragPractice)
            return;

        isDemonstrating = false;
        isUserDragging = false;
        lastPracticeActivityTime = Time.unscaledTime;
        SetAllPracticeCardsInput(true);
        PointHand(correctPracticeCard != null ? correctPracticeCard.RectTransform : null, dragHandOffset);
    }

    private void ShowPracticeSuccess()
    {
        if (!isRunning)
            return;

        currentStage = TutorialStage.Success;
        ShowStage(TutorialStage.Success);
    }

    private void CompleteTutorial()
    {
        PlayerPrefs.SetInt(GetCompletionKey(), 1);
        PlayerPrefs.Save();

        Action callback = completionCallback;
        completionCallback = null;
        CleanupTutorialObjects();

        if (manager != null)
            manager.SetTutorialHold(false);

        callback?.Invoke();
    }

    private void BuildPracticeVisuals()
    {
        ClearRuntimeTutorialObjects();
        CreatePracticeTarget();
        CreatePracticeCards();
        CreatePracticeHintButton();
    }

    private void CreatePracticeTarget()
    {
        if (sourceTargetCell == null || practiceLayer == null)
            return;

        GameObject clone = Instantiate(sourceTargetCell.gameObject, practiceLayer, true);
        clone.name = "Tutorial Practice Target";
        runtimeTutorialObjects.Add(clone);

        GridAdventureCell clonedCell = clone.GetComponent<GridAdventureCell>();
        if (clonedCell != null)
            Destroy(clonedCell);

        LayoutElement layout = clone.GetComponent<LayoutElement>();
        if (layout != null)
            layout.ignoreLayout = true;

        foreach (Graphic graphic in clone.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        practiceTargetCell = clone.transform as RectTransform;
        MatchRectToSource(practiceTargetCell, sourceTargetCell.RectTransform);
        practiceTargetCell.SetAsFirstSibling();
    }

    private void CreatePracticeCards()
    {
        if (practiceLayer == null || practiceTargetCell == null)
            return;

        for (int i = 0; i < sourceCards.Count; i++)
        {
            GridAdventureItemCard source = sourceCards[i];
            if (source == null)
                continue;

            GameObject clone = Instantiate(source.gameObject, practiceLayer, true);
            clone.name = "Tutorial Practice Card " + (i + 1);
            runtimeTutorialObjects.Add(clone);

            GridAdventureItemCard clonedRealCard = clone.GetComponent<GridAdventureItemCard>();
            if (clonedRealCard != null)
            {
                clonedRealCard.MarkAsTemplate(true);
                Destroy(clonedRealCard);
            }

            LayoutElement layout = clone.GetComponent<LayoutElement>();
            if (layout != null)
                layout.ignoreLayout = true;

            RectTransform cloneRect = clone.transform as RectTransform;
            MatchRectToSource(cloneRect, source.RectTransform);

            CanvasGroup group = clone.GetComponent<CanvasGroup>();
            if (group == null)
                group = clone.AddComponent<CanvasGroup>();
            group.alpha = 1f;

            GridAdventureTutorialPracticeCard practiceCard = clone.AddComponent<GridAdventureTutorialPracticeCard>();
            practiceCard.Configure(this, source.itemId, practiceLayer, practiceTargetCell, rootCanvas);
            practiceCard.CaptureHomePosition();
            practiceCards.Add(practiceCard);

            if (practiceQuestion != null &&
                string.Equals(source.itemId, practiceQuestion.itemId, StringComparison.OrdinalIgnoreCase))
            {
                correctPracticeCard = practiceCard;
            }
        }
    }

    private void CreatePracticeHintButton()
    {
        if (manager == null || manager.helpButton == null || practiceLayer == null)
            return;

        GameObject clone = Instantiate(manager.helpButton.gameObject, practiceLayer, true);
        clone.name = "Tutorial Hint Button";
        runtimeTutorialObjects.Add(clone);

        LayoutElement layout = clone.GetComponent<LayoutElement>();
        if (layout != null)
            layout.ignoreLayout = true;

        RectTransform cloneRect = clone.transform as RectTransform;
        MatchRectToSource(cloneRect, manager.helpButton.transform as RectTransform);

        practiceHintButton = clone.GetComponent<Button>();
        if (practiceHintButton == null)
            practiceHintButton = clone.AddComponent<Button>();

        practiceHintButton.onClick.RemoveAllListeners();
        practiceHintButton.onClick.AddListener(HandlePracticeHint);
        practiceHintButton.gameObject.SetActive(false);
    }

    private void MatchRectToSource(RectTransform target, RectTransform source)
    {
        if (target == null || source == null)
            return;

        target.SetParent(practiceLayer, true);
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.position = source.position;
        target.sizeDelta = source.rect.size;
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;
    }

    private void PlayDragDemonstration(Action onComplete)
    {
        if (!isRunning || correctPracticeCard == null || practiceTargetCell == null)
        {
            onComplete?.Invoke();
            return;
        }

        SetAllPracticeCardsInput(false);
        isDemonstrating = true;
        isUserDragging = false;

        if (demonstrationSequence != null && demonstrationSequence.IsActive())
            demonstrationSequence.Kill();

        DestroyGhostCard();
        GameObject ghostObject = Instantiate(correctPracticeCard.gameObject, practiceLayer, true);
        ghostObject.name = "Tutorial Drag Ghost";
        runtimeTutorialObjects.Add(ghostObject);

        GridAdventureTutorialPracticeCard ghostPractice = ghostObject.GetComponent<GridAdventureTutorialPracticeCard>();
        if (ghostPractice != null)
            Destroy(ghostPractice);

        foreach (Graphic graphic in ghostObject.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        CanvasGroup ghostGroup = ghostObject.GetComponent<CanvasGroup>();
        if (ghostGroup == null)
            ghostGroup = ghostObject.AddComponent<CanvasGroup>();
        ghostGroup.alpha = ghostTransparency;
        ghostGroup.blocksRaycasts = false;
        ghostGroup.interactable = false;

        ghostCard = ghostObject.transform as RectTransform;
        ghostCard.anchoredPosition = correctPracticeCard.RectTransform.anchoredPosition;
        ghostCard.localScale = Vector3.one;
        ghostCard.SetAsLastSibling();

        if (handImage != null)
        {
            if (handSequence != null && handSequence.IsActive())
                handSequence.Kill();
            handSequence = null;
            handImage.rectTransform.DOKill();
            handImage.rectTransform.localScale = Vector3.one;
            handImage.rectTransform.localRotation = Quaternion.identity;
            NormalizeHandAnchors();
        }

        Vector2 handStart = Vector2.zero;
        Vector2 handDestination = Vector2.zero;
        bool hasHandPositions =
            TryGetHandAnchoredPosition(correctPracticeCard.RectTransform, dragHandOffset, out handStart) &&
            TryGetHandAnchoredPosition(practiceTargetCell, dragHandOffset, out handDestination);

        if (handImage != null)
        {
            handImage.gameObject.SetActive(true);
            if (hasHandPositions)
                handImage.rectTransform.anchoredPosition = handStart;
            handImage.rectTransform.SetAsLastSibling();
        }

        demonstrationSequence = DOTween.Sequence().SetUpdate(true).SetId(this);
        demonstrationSequence.AppendInterval(Mathf.Max(0f, dragDemoStartHold));
        demonstrationSequence.Append(
            ghostCard.DOAnchorPos(practiceTargetCell.anchoredPosition, dragDemoDuration)
                .SetEase(Ease.InOutSine));
        if (handImage != null && hasHandPositions)
        {
            demonstrationSequence.Join(
                handImage.rectTransform.DOAnchorPos(handDestination, dragDemoDuration)
                    .SetEase(Ease.InOutSine));
        }
        demonstrationSequence.AppendInterval(Mathf.Max(0f, dragDemoTargetHold));
        demonstrationSequence.Append(ghostGroup.DOFade(0f, 0.18f));
        demonstrationSequence.OnComplete(delegate
        {
            DestroyGhostCard();
            isDemonstrating = false;
            onComplete?.Invoke();
        });
    }

    private void PulseCorrectPracticeCard()
    {
        if (correctPracticeCard == null)
            return;

        RectTransform target = correctPracticeCard.RectTransform;
        target.DOKill();
        target.localScale = Vector3.one;
        target.DOPunchScale(Vector3.one * 0.18f, 0.65f, 8, 0.65f).SetUpdate(true);
    }

    private void SetAllPracticeCardsInput(bool enabled)
    {
        for (int i = 0; i < practiceCards.Count; i++)
        {
            if (practiceCards[i] != null)
                practiceCards[i].SetInputEnabled(enabled);
        }
    }

    private void SetInstruction(string value)
    {
        if (instructionText != null)
            instructionText.text = value ?? string.Empty;

        if (instructionMotionRoot == null)
            return;

        if (instructionSequence != null && instructionSequence.IsActive())
            instructionSequence.Kill();

        instructionMotionRoot.DOKill();
        instructionMotionRoot.localScale = Vector3.one;
        instructionSequence = DOTween.Sequence().SetUpdate(true).SetId(this);
        instructionSequence.Append(instructionMotionRoot.DOScale(1.035f, 0.7f).SetEase(Ease.InOutSine));
        instructionSequence.Append(instructionMotionRoot.DOScale(1f, 0.7f).SetEase(Ease.InOutSine));
        instructionSequence.SetLoops(-1);
    }

    private void FocusTarget(RectTransform target)
    {
        if (focusImage == null || target == null)
        {
            HideFocus();
            return;
        }

        focusImage.gameObject.SetActive(true);
        focusImage.position = target.position;
        focusImage.sizeDelta = target.rect.size + focusPadding;
        focusImage.localScale = Vector3.one;
        focusImage.SetAsFirstSibling();
    }

    private void HideFocus()
    {
        if (focusImage != null)
            focusImage.gameObject.SetActive(false);
    }

    private void PointHand(RectTransform target, Vector2 offset)
    {
        if (handImage == null || target == null)
        {
            HideHand();
            return;
        }

        handImage.gameObject.SetActive(true);
        handImage.rectTransform.DOKill();
        handImage.rectTransform.SetAsLastSibling();
        NormalizeHandAnchors();
        handImage.rectTransform.localRotation = Quaternion.identity;

        Vector2 destination;
        if (!TryGetHandAnchoredPosition(target, offset, out destination))
        {
            HideHand();
            return;
        }

        handImage.rectTransform.DOAnchorPos(destination, pointerMoveDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        if (handSequence != null && handSequence.IsActive())
            handSequence.Kill();

        handImage.rectTransform.localScale = Vector3.one;
        handSequence = DOTween.Sequence().SetUpdate(true).SetId(this);
        handSequence.Append(handImage.rectTransform.DOScale(1.08f, 0.5f).SetEase(Ease.InOutSine));
        handSequence.Append(handImage.rectTransform.DOScale(1f, 0.5f).SetEase(Ease.InOutSine));
        handSequence.SetLoops(-1);
    }

    private void PointHandWithTip(
        RectTransform target,
        Vector2 targetPointNormalized,
        Vector2 offset,
        float rotationDegrees)
    {
        if (handImage == null || target == null)
        {
            HideHand();
            return;
        }

        handImage.gameObject.SetActive(true);
        handImage.rectTransform.DOKill();
        handImage.rectTransform.SetAsLastSibling();
        NormalizeHandAnchors();
        handImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);

        Vector2 destination;
        if (!TryGetHandAnchoredPositionWithTip(target, targetPointNormalized, offset, out destination))
        {
            HideHand();
            return;
        }

        handImage.rectTransform.DOAnchorPos(destination, pointerMoveDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);

        if (handSequence != null && handSequence.IsActive())
            handSequence.Kill();

        handImage.rectTransform.localScale = Vector3.one;
        handSequence = DOTween.Sequence().SetUpdate(true).SetId(this);
        handSequence.Append(handImage.rectTransform.DOScale(1.08f, 0.5f).SetEase(Ease.InOutSine));
        handSequence.Append(handImage.rectTransform.DOScale(1f, 0.5f).SetEase(Ease.InOutSine));
        handSequence.SetLoops(-1);
    }

    private void HideHand()
    {
        if (handSequence != null && handSequence.IsActive())
            handSequence.Kill();
        handSequence = null;

        if (handImage != null)
        {
            handImage.rectTransform.DOKill();
            handImage.gameObject.SetActive(false);
        }
    }

    private bool TryGetHandAnchoredPosition(RectTransform target, Vector2 offset, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;
        if (target == null || handImage == null)
            return false;

        RectTransform handParent = handImage.rectTransform.parent as RectTransform;
        if (handParent == null)
            return false;

        Canvas.ForceUpdateCanvases();

        Camera canvasCamera = GetCanvasCamera();
        Vector3 targetWorldCenter = target.TransformPoint(target.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, targetWorldCenter);

        Vector2 parentLocalPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handParent,
                screenPoint,
                canvasCamera,
                out parentLocalPoint))
        {
            return false;
        }

        anchoredPosition = ClampHandInsideParent(parentLocalPoint + offset, handParent);
        return true;
    }

    private bool TryGetHandAnchoredPositionWithTip(
        RectTransform target,
        Vector2 targetPointNormalized,
        Vector2 offset,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;
        if (target == null || handImage == null)
            return false;

        RectTransform handParent = handImage.rectTransform.parent as RectTransform;
        if (handParent == null)
            return false;

        Canvas.ForceUpdateCanvases();

        Vector2 safeTargetPoint = new Vector2(
            Mathf.Clamp01(targetPointNormalized.x),
            Mathf.Clamp01(targetPointNormalized.y));
        Vector3 targetLocalPoint = new Vector3(
            Mathf.Lerp(target.rect.xMin, target.rect.xMax, safeTargetPoint.x),
            Mathf.Lerp(target.rect.yMin, target.rect.yMax, safeTargetPoint.y),
            0f);

        Camera canvasCamera = GetCanvasCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            target.TransformPoint(targetLocalPoint));

        Vector2 parentLocalPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                handParent,
                screenPoint,
                canvasCamera,
                out parentLocalPoint))
        {
            return false;
        }

        RectTransform handRect = handImage.rectTransform;
        Vector2 safeTip = new Vector2(
            Mathf.Clamp01(handTipNormalized.x),
            Mathf.Clamp01(handTipNormalized.y));
        Vector2 tipOffsetFromPivot = new Vector2(
            Mathf.Lerp(handRect.rect.xMin, handRect.rect.xMax, safeTip.x) * handRect.localScale.x,
            Mathf.Lerp(handRect.rect.yMin, handRect.rect.yMax, safeTip.y) * handRect.localScale.y);
        Vector3 rotatedTipOffset = handRect.localRotation *
                                   new Vector3(tipOffsetFromPivot.x, tipOffsetFromPivot.y, 0f);
        tipOffsetFromPivot = new Vector2(rotatedTipOffset.x, rotatedTipOffset.y);

        Vector2 desiredHandPivot = parentLocalPoint + offset - tipOffsetFromPivot;
        anchoredPosition = ClampHandInsideParent(desiredHandPivot, handParent);
        return true;
    }

    private Vector2 ClampHandInsideParent(Vector2 desired, RectTransform handParent)
    {
        if (handImage == null || handParent == null)
            return desired;

        Rect parentRect = handParent.rect;
        RectTransform handRect = handImage.rectTransform;
        float halfWidth = Mathf.Abs(handRect.rect.width * handRect.localScale.x) * 0.5f;
        float halfHeight = Mathf.Abs(handRect.rect.height * handRect.localScale.y) * 0.5f;
        float padding = Mathf.Max(0f, handScreenEdgePadding);

        float minX = parentRect.xMin + halfWidth + padding;
        float maxX = parentRect.xMax - halfWidth - padding;
        float minY = parentRect.yMin + halfHeight + padding;
        float maxY = parentRect.yMax - halfHeight - padding;

        if (minX > maxX)
            desired.x = parentRect.center.x;
        else
            desired.x = Mathf.Clamp(desired.x, minX, maxX);

        if (minY > maxY)
            desired.y = parentRect.center.y;
        else
            desired.y = Mathf.Clamp(desired.y, minY, maxY);

        return desired;
    }

    private void NormalizeHandAnchors()
    {
        if (handImage == null)
            return;

        RectTransform handRect = handImage.rectTransform;
        handRect.anchorMin = new Vector2(0.5f, 0.5f);
        handRect.anchorMax = new Vector2(0.5f, 0.5f);
    }

    private Camera GetCanvasCamera()
    {
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return rootCanvas.worldCamera;
    }

    private void WireBackgroundButton()
    {
        if (backgroundContinueButton == null)
            return;

        backgroundContinueButton.onClick.RemoveListener(HandleBackgroundContinue);
        backgroundContinueButton.onClick.AddListener(HandleBackgroundContinue);
    }

    private void CacheReferences()
    {
        if (manager == null)
            manager = FindObjectOfType<GridAdventureManager>();

        if (rootCanvas == null && manager != null)
            rootCanvas = manager.RootCanvas;

        if (practiceLayer == null)
            practiceLayer = transform as RectTransform;
    }

    private void ApplyResetOnPlayIfNeeded()
    {
        if (didApplyResetOnPlay || !resetSavedStatusOnPlay)
            return;

        didApplyResetOnPlay = true;
        ResetTutorialCompletion();
    }

    private void ApplyPointerPlacementUpgradeIfNeeded()
    {
        if (pointerPlacementVersion >= 1)
            return;

        handTipNormalized = new Vector2(0f, 1f);
        clueHandOffset = new Vector2(0f, 8f);
        clueHandRotation = 180f;
        hintHandOffset = Vector2.zero;
        hintHandRotation = -90f;
        pointerPlacementVersion = 1;
    }

    private void ApplySuccessStageUpgradeIfNeeded()
    {
        if (successStageVersion >= 1)
            return;

        successInstruction =
            "You have successfully completed the tutorial!\nClick anywhere to start the game.";
        successStageVersion = 1;
    }

    private string GetCompletionKey()
    {
        string baseKey = string.IsNullOrWhiteSpace(completionSaveKey)
            ? "GridAdventure.InteractiveTutorial.Completed"
            : completionSaveKey.Trim();
        return baseKey + "." + SceneManager.GetActiveScene().name;
    }

    private void DestroyGhostCard()
    {
        if (ghostCard == null)
            return;

        runtimeTutorialObjects.Remove(ghostCard.gameObject);
        Destroy(ghostCard.gameObject);
        ghostCard = null;
    }

    private void ClearRuntimeTutorialObjects()
    {
        for (int i = runtimeTutorialObjects.Count - 1; i >= 0; i--)
        {
            GameObject runtimeObject = runtimeTutorialObjects[i];
            if (runtimeObject != null)
                Destroy(runtimeObject);
        }

        runtimeTutorialObjects.Clear();
        practiceCards.Clear();
        correctPracticeCard = null;
        practiceTargetCell = null;
        practiceHintButton = null;
        ghostCard = null;
    }

    private void CleanupTutorialObjects()
    {
        isCleaningUp = true;

        DOTween.Kill(this);
        if (handSequence != null && handSequence.IsActive()) handSequence.Kill();
        if (instructionSequence != null && instructionSequence.IsActive()) instructionSequence.Kill();
        if (demonstrationSequence != null && demonstrationSequence.IsActive()) demonstrationSequence.Kill();
        handSequence = null;
        instructionSequence = null;
        demonstrationSequence = null;

        HideHand();
        HideFocus();
        ClearRuntimeTutorialObjects();

        currentStage = TutorialStage.None;
        isRunning = false;
        isDemonstrating = false;
        isUserDragging = false;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);

        isCleaningUp = false;
    }
}
