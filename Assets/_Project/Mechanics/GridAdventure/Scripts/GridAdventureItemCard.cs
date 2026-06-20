using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class GridAdventureItemCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Data")]
    public string itemId;
    public string displayName;

    [Header("Visuals")]
    public Image backgroundImage;
    public Image iconImage;
    public TextMeshProUGUI labelText;
    public GridAdventureItemDisplayMode displayMode = GridAdventureItemDisplayMode.ImageAndLabel;

    [Header("Drag")]
    public float wrongReturnDuration = 0.4f;
    public float correctSnapDuration = 0.25f;
    [Tooltip("When true, the temporary drag card is centered exactly on the mouse/touch pointer.")]
    public bool centerCloneOnPointer = true;

    public bool IsSolved { get; private set; }

    private GridAdventureManager manager;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private RectTransform dragClone;
    private Color originalBackgroundColor;
    private Vector2 originalSize;
    private bool isDragging;
    private bool isTemplate;
    private Vector2 pointerOffsetInDragLayer;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            return rectTransform;
        }
    }

    private void OnValidate()
    {
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        ApplyDisplayModeToVisuals();
    }

    public void ApplyDisplayModeToVisuals()
    {
        if (iconImage != null)
            iconImage.preserveAspect = true;

        if (labelText == null) return;

        bool showLabel = displayMode == GridAdventureItemDisplayMode.ImageAndLabel;
        labelText.gameObject.SetActive(showLabel);

        if (showLabel)
            labelText.text = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName;
    }

    public void MarkAsTemplate(bool value)
    {
        isTemplate = value;
    }

    public void Setup(GridAdventureManager owner, GridAdventureItemData data, GridAdventureItemDisplayMode mode)
    {
        manager = owner;
        displayMode = mode;
        canvasGroup = GetComponent<CanvasGroup>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (backgroundImage != null) originalBackgroundColor = backgroundImage.color;

        itemId = data != null ? data.itemId : string.Empty;
        displayName = data != null ? data.displayName : string.Empty;
        IsSolved = false;
        isDragging = false;
        isTemplate = false;
        dragClone = null;
        pointerOffsetInDragLayer = Vector2.zero;

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.sprite : null;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.preserveAspect = true;
        }

        ApplyDisplayModeToVisuals();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        RectTransform.localScale = Vector3.one;
        RectTransform.localRotation = Quaternion.identity;
        gameObject.SetActive(true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isTemplate || IsSolved || manager == null || manager.RootCanvas == null || manager.DragLayer == null) return;
        if (!manager.CanDragCards) return;

        isDragging = true;
        originalSize = RectTransform.rect.size;
        if (originalSize.x <= 1f || originalSize.y <= 1f)
            originalSize = RectTransform.sizeDelta;

        manager.PlayClick();

        GameObject cloneObject = Instantiate(gameObject, manager.DragLayer, false);
        cloneObject.name = "DragClone_" + itemId;

        GridAdventureItemCard cloneCard = cloneObject.GetComponent<GridAdventureItemCard>();
        if (cloneCard != null) Destroy(cloneCard);

        LayoutElement cloneLayout = cloneObject.GetComponent<LayoutElement>();
        if (cloneLayout != null) Destroy(cloneLayout);

        foreach (Graphic graphic in cloneObject.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        CanvasGroup cloneGroup = cloneObject.GetComponent<CanvasGroup>();
        if (cloneGroup == null) cloneGroup = cloneObject.AddComponent<CanvasGroup>();
        cloneGroup.blocksRaycasts = false;
        cloneGroup.interactable = false;
        cloneGroup.alpha = 0.95f;

        dragClone = cloneObject.transform as RectTransform;
        PrepareCloneRectTransform(eventData);
        dragClone.SetAsLastSibling();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.18f;
            canvasGroup.blocksRaycasts = false;
        }

        MoveCloneToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragClone == null) return;
        MoveCloneToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        GridAdventureCell targetCell = manager.GetCellUnderPointer(eventData);
        manager.ResolveDrop(this, targetCell, dragClone);
        dragClone = null;
    }

    public void PlayWrongReturn(RectTransform clone)
    {
        if (manager != null)
            manager.PlayWrong();

        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = false;

        RectTransform.DOKill();

        if (clone == null)
        {
            RestoreAfterWrongDrop();
            return;
        }

        clone.DOKill();
        Sequence wrongSequence = DOTween.Sequence();
        wrongSequence.Append(clone.DOShakeAnchorPos(0.16f, new Vector2(26f, 0f), 10, 0f).SetEase(Ease.OutQuad));
        wrongSequence.Append(clone.DOMove(RectTransform.position, wrongReturnDuration).SetEase(Ease.OutCubic));
        wrongSequence.Join(clone.DOScale(1f, wrongReturnDuration).SetEase(Ease.OutQuad));
        wrongSequence.OnComplete(delegate
        {
            if (clone != null) Destroy(clone.gameObject);
            RestoreAfterWrongDrop();
        });
    }

    public void SnapCorrectlyInto(RectTransform targetRoot, RectTransform clone)
    {
        if (targetRoot == null) return;

        IsSolved = true;

        Vector3 startWorldPosition = clone != null ? clone.position : RectTransform.position;
        Vector3 startLocalScale = clone != null ? clone.localScale : RectTransform.localScale;
        Vector2 sourceSize = clone != null ? clone.rect.size : originalSize;
        if (sourceSize.x <= 1f || sourceSize.y <= 1f)
            sourceSize = RectTransform.rect.size;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        RectTransform.DOKill();
        RectTransform.SetParent(targetRoot, true);
        RectTransform.position = startWorldPosition;
        RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        RectTransform.pivot = new Vector2(0.5f, 0.5f);
        RectTransform.sizeDelta = GetFittedSolvedSize(targetRoot, sourceSize);
        RectTransform.localScale = Vector3.one * Mathf.Clamp(startLocalScale.x, 0.9f, 1.12f);
        RectTransform.localRotation = Quaternion.identity;
        RectTransform.SetAsLastSibling();

        if (clone != null)
            Destroy(clone.gameObject);

        Sequence snap = DOTween.Sequence();
        snap.Append(RectTransform.DOAnchorPos(Vector2.zero, correctSnapDuration).SetEase(Ease.OutCubic));
        snap.Join(RectTransform.DOScale(1.08f, correctSnapDuration).SetEase(Ease.OutQuad));
        snap.Append(RectTransform.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
        snap.Join(RectTransform.DOPunchRotation(new Vector3(0f, 0f, 5f), 0.22f, 5, 0.45f));
    }

    public void PlayClue(Color clueColor)
    {
        if (IsSolved || isTemplate) return;

        RectTransform.DOKill();
        if (backgroundImage != null) backgroundImage.DOKill();

        Sequence clueSequence = DOTween.Sequence();
        clueSequence.Append(RectTransform.DOScale(1.2f, 0.15f).SetEase(Ease.OutBack));

        if (backgroundImage != null)
            clueSequence.Join(backgroundImage.DOColor(clueColor, 0.15f));

        clueSequence.Append(RectTransform.DOPunchRotation(new Vector3(0f, 0f, 10f), 1.15f, 8, 0.7f));
        clueSequence.Append(RectTransform.DOScale(1f, 0.18f).SetEase(Ease.OutBack));

        if (backgroundImage != null)
            clueSequence.Join(backgroundImage.DOColor(originalBackgroundColor, 0.2f));
    }

    private Vector2 GetFittedSolvedSize(RectTransform targetRoot, Vector2 preferredSourceSize)
    {
        Vector2 sourceSize = preferredSourceSize;
        if (sourceSize.x <= 1f || sourceSize.y <= 1f)
            sourceSize = originalSize == Vector2.zero ? RectTransform.rect.size : originalSize;

        if (sourceSize.x <= 1f || sourceSize.y <= 1f)
            sourceSize = new Vector2(86f, 86f);

        Rect targetRect = targetRoot.rect;
        if (targetRect.width <= 1f || targetRect.height <= 1f)
            return sourceSize;

        float maxWidth = targetRect.width * 0.92f;
        float maxHeight = targetRect.height * 0.92f;
        float scale = Mathf.Min(maxWidth / sourceSize.x, maxHeight / sourceSize.y, 1f);
        return sourceSize * Mathf.Max(0.1f, scale);
    }

    private void PrepareCloneRectTransform(PointerEventData eventData)
    {
        if (dragClone == null || manager == null || manager.DragLayer == null) return;

        dragClone.DOKill();
        dragClone.anchorMin = new Vector2(0.5f, 0.5f);
        dragClone.anchorMax = new Vector2(0.5f, 0.5f);
        dragClone.pivot = new Vector2(0.5f, 0.5f);
        dragClone.sizeDelta = originalSize == Vector2.zero ? new Vector2(86f, 86f) : originalSize;
        dragClone.localScale = Vector3.one * 1.06f;
        dragClone.localRotation = Quaternion.identity;

        Camera eventCamera = GetEventCamera(eventData);
        Vector2 pointerLocalPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.DragLayer, eventData.position, eventCamera, out pointerLocalPoint))
        {
            Vector2 cloneLocalPoint = WorldToDragLayerLocal(RectTransform.position, eventCamera);
            pointerOffsetInDragLayer = centerCloneOnPointer ? Vector2.zero : cloneLocalPoint - pointerLocalPoint;
            dragClone.anchoredPosition = pointerLocalPoint + pointerOffsetInDragLayer;
        }
    }

    private void MoveCloneToPointer(PointerEventData eventData)
    {
        if (dragClone == null || manager == null || manager.DragLayer == null) return;

        Camera eventCamera = GetEventCamera(eventData);
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.DragLayer, eventData.position, eventCamera, out localPoint))
            dragClone.anchoredPosition = localPoint + pointerOffsetInDragLayer;
    }

    private Vector2 WorldToDragLayerLocal(Vector3 worldPosition, Camera eventCamera)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPosition);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(manager.DragLayer, screenPoint, eventCamera, out localPoint);
        return localPoint;
    }

    private Camera GetEventCamera(PointerEventData eventData)
    {
        if (manager == null || manager.RootCanvas == null)
            return eventData != null ? eventData.pressEventCamera : null;

        if (manager.RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (eventData != null && eventData.pressEventCamera != null)
            return eventData.pressEventCamera;

        return manager.RootCanvas.worldCamera;
    }

    private void RestoreAfterWrongDrop()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }
}
