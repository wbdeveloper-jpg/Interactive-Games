using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OrderSortDropSlot : MonoBehaviour, IDropHandler
{
    [Header("Slot Visuals")]
    public TMP_Text indexText;
    public Image backgroundImage;
    public Image indexBadgeBackground;

    [Tooltip("Cards are placed inside this holder so the feedback overlay can always stay above the card.")]
    public RectTransform itemHolder;

    [Tooltip("Must stay above Item Holder. Created by the scene builder, but also repaired at runtime if needed.")]
    public Image feedbackOverlay;

    public OrderSortDragItem PlacedItem { get; private set; }
    public RectTransform ItemHolder => itemHolder != null ? itemHolder : GetOrCreateItemHolder();

    private OrderSortDragManager manager;

    public void Init(OrderSortDragManager sortManager, int slotNumber)
    {
        manager = sortManager;

        if (indexText != null)
            indexText.text = slotNumber.ToString();

        RepairVisualHierarchy();
        HideOverlayInstant();
        PlacedItem = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!manager.IsGameInputEnabled)
            return;

        OrderSortDragItem item = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<OrderSortDragItem>()
            : null;

        if (item == null)
            return;

        manager.DropItemOnSlot(item, this);
    }

    public void SetItem(OrderSortDragItem item)
    {
        PlacedItem = item;
        BringOverlayToFront();
    }

    public void ClearItemOnly()
    {
        PlacedItem = null;
    }

    public void BringOverlayToFront()
    {
        if (feedbackOverlay == null)
            return;

        feedbackOverlay.raycastTarget = false;
        feedbackOverlay.transform.SetAsLastSibling();
    }

    public Tween PlayCheckingThenResult(
        Color checkingColor,
        Color finalColor,
        float checkingDuration,
        float lerpDuration,
        float holdDuration,
        float fadeOutDuration,
        bool keepVisibleAfterResult)
    {
        if (feedbackOverlay == null)
            return null;

        RepairVisualHierarchy();

        feedbackOverlay.DOKill();
        feedbackOverlay.gameObject.SetActive(true);
        feedbackOverlay.raycastTarget = false;
        BringOverlayToFront();

        Color hiddenChecking = checkingColor;
        hiddenChecking.a = 0f;
        feedbackOverlay.color = hiddenChecking;

        Color dimChecking = checkingColor;
        dimChecking.a = Mathf.Max(0.1f, checkingColor.a * 0.35f);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(feedbackOverlay.DOFade(checkingColor.a, checkingDuration * 0.45f).SetEase(Ease.OutQuad));
        sequence.Append(feedbackOverlay.DOColor(dimChecking, checkingDuration * 0.25f).SetEase(Ease.InOutSine));
        sequence.Append(feedbackOverlay.DOColor(checkingColor, checkingDuration * 0.30f).SetEase(Ease.OutQuad));
        sequence.Append(feedbackOverlay.DOColor(finalColor, lerpDuration).SetEase(Ease.InOutSine));
        sequence.AppendInterval(holdDuration);

        if (!keepVisibleAfterResult)
            sequence.Append(feedbackOverlay.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));
        else
            sequence.AppendCallback(() => feedbackOverlay.color = finalColor);
        sequence.Pause();
        return sequence;
    }

    private void HideOverlayInstant()
    {
        if (feedbackOverlay == null)
            return;

        Color color = feedbackOverlay.color;
        color.a = 0f;
        feedbackOverlay.color = color;
        feedbackOverlay.gameObject.SetActive(true);
        feedbackOverlay.raycastTarget = false;
        BringOverlayToFront();
    }

    private void RepairVisualHierarchy()
    {
        GetOrCreateItemHolder();
        BringOverlayToFront();
    }

    private RectTransform GetOrCreateItemHolder()
    {
        if (itemHolder != null)
            return itemHolder;

        Transform existing = transform.Find("ItemHolder");
        if (existing != null)
        {
            itemHolder = existing.GetComponent<RectTransform>();
            if (itemHolder != null)
                return itemHolder;
        }

        GameObject holderObj = new GameObject("ItemHolder", typeof(RectTransform));
        itemHolder = holderObj.GetComponent<RectTransform>();
        itemHolder.SetParent(transform, false);
        Stretch(itemHolder);
        itemHolder.SetAsFirstSibling();
        return itemHolder;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
