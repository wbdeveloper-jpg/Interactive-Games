using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class GridAdventureTutorialPracticeCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string itemId;

    private GridAdventureFirstTimeTutorialController controller;
    private RectTransform rectTransform;
    private RectTransform dragLayer;
    private RectTransform targetCell;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Vector2 homeAnchoredPosition;
    private Vector2 pointerOffset;
    private bool isDragging;
    private bool inputEnabled;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    public void Configure(
        GridAdventureFirstTimeTutorialController owner,
        string id,
        RectTransform practiceDragLayer,
        RectTransform practiceTarget,
        Canvas canvas)
    {
        controller = owner;
        itemId = id;
        dragLayer = practiceDragLayer;
        targetCell = practiceTarget;
        rootCanvas = canvas;
        canvasGroup = GetComponent<CanvasGroup>();
        homeAnchoredPosition = RectTransform.anchoredPosition;
        isDragging = false;
        SetInputEnabled(false);
    }

    public void CaptureHomePosition()
    {
        homeAnchoredPosition = RectTransform.anchoredPosition;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!inputEnabled || controller == null || !controller.CanAcceptPracticeInput)
            return;

        isDragging = true;
        RectTransform.DOKill();
        RectTransform.SetAsLastSibling();
        controller.NotifyPracticeDragState(true);

        Vector2 pointerLocal;
        Camera eventCamera = GetEventCamera(eventData);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, eventCamera, out pointerLocal))
            pointerOffset = RectTransform.anchoredPosition - pointerLocal;
        else
            pointerOffset = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || !inputEnabled || controller == null)
            return;

        controller.NotifyPracticeActivity();

        Vector2 pointerLocal;
        Camera eventCamera = GetEventCamera(eventData);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, eventCamera, out pointerLocal))
            RectTransform.anchoredPosition = pointerLocal + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;
        SetInputEnabled(false);
        controller.NotifyPracticeDragState(false);

        Camera eventCamera = GetEventCamera(eventData);
        bool droppedOnTarget = targetCell != null &&
                               RectTransformUtility.RectangleContainsScreenPoint(targetCell, eventData.position, eventCamera);

        controller.ResolvePracticeDrop(this, droppedOnTarget);
    }

    public void ReturnHome(float duration, System.Action onComplete)
    {
        RectTransform.DOKill();
        RectTransform.DOAnchorPos(homeAnchoredPosition, Mathf.Max(0.05f, duration))
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void SnapIntoTarget(float duration, System.Action onComplete)
    {
        if (targetCell == null)
        {
            onComplete?.Invoke();
            return;
        }

        RectTransform.DOKill();
        Sequence snap = DOTween.Sequence().SetUpdate(true);
        snap.Append(
            RectTransform.DOAnchorPos(targetCell.anchoredPosition, Mathf.Max(0.05f, duration))
                .SetEase(Ease.OutCubic));
        snap.Join(RectTransform.DOScale(1.08f, Mathf.Max(0.05f, duration)).SetEase(Ease.OutQuad));
        snap.Append(RectTransform.DOScale(1f, 0.18f).SetEase(Ease.OutBack));
        snap.OnComplete(() => onComplete?.Invoke());
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (eventData != null && eventData.pressEventCamera != null)
            return eventData.pressEventCamera;

        return rootCanvas.worldCamera;
    }
}
