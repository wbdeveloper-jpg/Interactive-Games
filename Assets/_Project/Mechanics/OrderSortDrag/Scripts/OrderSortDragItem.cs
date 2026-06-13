using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OrderSortDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public TMP_Text labelText;
    public Image objectImage;
    public Image cardBackgroundImage;
    public Image cardFaceImage;
    public CanvasGroup canvasGroup;

    public string Value { get; private set; }
    public OrderSortDropSlot CurrentSlot { get; private set; }
    public OrderSortDropSlot PreviousSlot { get; private set; }

    private OrderSortDragManager manager;
    private RectTransform rectTransform;
    private bool placedThisDrag;

    private Vector2 bankAnchoredPosition;
    private float bankRotationZ;
    private Vector3 bankScale = Vector3.one;

    public void Init(OrderSortDragManager sortManager, OrderSortItemData data, OrderSortContentMode contentMode)
    {
        manager = sortManager;
        rectTransform = GetComponent<RectTransform>();

        Value = data.value;
        bool imageMode = contentMode == OrderSortContentMode.ImageOnly;

        if (labelText != null)
        {
            labelText.text = data.value;
            labelText.gameObject.SetActive(!imageMode);
        }

        if (objectImage != null)
        {
            objectImage.sprite = data.image;
            objectImage.preserveAspect = true;
            objectImage.gameObject.SetActive(imageMode && data.image != null);
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        MarkSlot(null);
        MarkPlacedThisDrag(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!manager.IsGameInputEnabled)
            return;

        manager.PrepareItemForDrag(this);

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.85f;
        }

        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!manager.IsGameInputEnabled)
            return;

        MoveToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (!placedThisDrag)
            manager.ReturnItemToPreviousPlace(this);

        MarkPlacedThisDrag(false);
    }

    // If this card is in a slot, dropping on it swaps/places into that slot.
    // If this card is still in the basket, dropping on it counts as dropping into the basket.
    public void OnDrop(PointerEventData eventData)
    {
        if (!manager.IsGameInputEnabled)
            return;

        OrderSortDragItem incomingItem = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<OrderSortDragItem>()
            : null;

        if (incomingItem == null || incomingItem == this)
            return;

        if (CurrentSlot != null)
            manager.DropItemOnSlot(incomingItem, CurrentSlot);
        else
            manager.DropItemOnBank(incomingItem);
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        RectTransform dragRoot = manager.dragLayer;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                dragRoot,
                eventData.position,
                eventData.pressEventCamera,
                out Vector3 worldPosition))
        {
            rectTransform.position = worldPosition;
        }
    }

    public void ApplyCardColor(Color color)
    {
        if (cardFaceImage != null)
        {
            cardFaceImage.color = color;
            return;
        }

        if (cardBackgroundImage != null)
        {
            cardBackgroundImage.color = color;
            return;
        }

        Image fallbackImage = GetComponent<Image>();
        if (fallbackImage != null)
            fallbackImage.color = color;
    }

    public void StorePreviousSlot()
    {
        PreviousSlot = CurrentSlot;
    }

    public void MarkSlot(OrderSortDropSlot slot)
    {
        CurrentSlot = slot;
    }

    public void MarkPlacedThisDrag(bool value)
    {
        placedThisDrag = value;
    }

    public void StoreBankVisual(Vector2 anchoredPosition, float rotationZ, Vector3 scale)
    {
        bankAnchoredPosition = anchoredPosition;
        bankRotationZ = rotationZ;
        bankScale = scale;
    }

    public void RestoreBankVisual()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        rectTransform.anchoredPosition = bankAnchoredPosition;
        rectTransform.localEulerAngles = new Vector3(0f, 0f, bankRotationZ);
        rectTransform.localScale = bankScale;
    }
}
