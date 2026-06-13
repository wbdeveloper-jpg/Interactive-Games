using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class FractionPortionBasketCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text countText;

    private FractionPortionFillManager manager;
    private CanvasGroup canvasGroup;
    private FractionPortionFillManager.PortionItemData itemData;
    private int remainingStock;
    private bool dragStarted;
    private bool currentDragConsumed;
    private RectTransform rectTransform;

    public FractionPortionFillManager.PortionItemData ItemData => itemData;
    public string ItemId => itemData != null ? itemData.id : string.Empty;
    public int RemainingStock => remainingStock;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(FractionPortionFillManager owner, FractionPortionFillManager.PortionItemData data, int stock)
    {
        manager = owner;
        itemData = data;
        remainingStock = Mathf.Max(0, stock);
        dragStarted = false;
        currentDragConsumed = false;
        RefreshVisual();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manager == null || itemData == null || !manager.CanDragItems() || remainingStock <= 0)
            return;

        dragStarted = true;
        currentDragConsumed = false;
        remainingStock--;
        RefreshVisual();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.75f;
        }

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.DOKill();
            rectTransform.DOScale(0.96f, 0.12f).SetEase(Ease.OutQuad);
        }

        manager.BeginBasketDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragStarted || manager == null)
            return;

        manager.UpdateBasketDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragStarted)
            return;

        if (!currentDragConsumed)
            remainingStock++;

        dragStarted = false;
        currentDragConsumed = false;
        RefreshVisual();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.DOKill();
            rectTransform.DOScale(1f, 0.16f).SetEase(Ease.OutBack);
        }

        if (manager != null)
            manager.EndBasketDrag();
    }

    public void MarkCurrentDragConsumed()
    {
        currentDragConsumed = true;
        RefreshVisual();
    }

    public void AddStock(int amount)
    {
        remainingStock = Mathf.Max(0, remainingStock + amount);
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (itemData != null)
        {
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.color = itemData.color;
                iconImage.sprite = itemData.icon;
                iconImage.preserveAspect = true;
                iconImage.enabled = true;
            }

            if (nameText != null)
                nameText.text = itemData.displayName;
        }

        if (countText != null)
            countText.text = "x" + remainingStock;

        if (canvasGroup != null && !dragStarted)
        {
            bool hasStock = remainingStock > 0;
            canvasGroup.alpha = hasStock ? 1f : 0.45f;
            canvasGroup.interactable = hasStock;
            canvasGroup.blocksRaycasts = hasStock;
        }
    }
}
