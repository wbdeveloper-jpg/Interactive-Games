using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class FractionPortionDragVisual : MonoBehaviour
{
    public Image backgroundImage;
    public Image iconImage;
    public TMP_Text nameText;
    public bool showName = false;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.alpha = 0f;
        }
    }

    public void Setup(FractionPortionFillManager.PortionItemData itemData)
    {
        Setup(itemData, showName);
    }

    public void Setup(FractionPortionFillManager.PortionItemData itemData, bool showNameLabel)
    {
        if (itemData == null)
            return;

        showName = showNameLabel;

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(true);
            iconImage.color = itemData.color;
            iconImage.sprite = itemData.icon;
            iconImage.preserveAspect = true;
            iconImage.enabled = true;
            iconImage.raycastTarget = false;
        }

        if (nameText != null)
        {
            nameText.gameObject.SetActive(showNameLabel);
            nameText.text = showNameLabel ? itemData.displayName : string.Empty;
            nameText.raycastTarget = false;
        }

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (rectTransform != null)
        {
            rectTransform.DOKill();
            rectTransform.localScale = Vector3.one * 0.85f;
            rectTransform.DOScale(1f, 0.14f).SetEase(Ease.OutBack);
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0.95f, 0.1f);
        }
    }

    public void FollowPointer(Canvas canvas, PointerEventData eventData)
    {
        if (canvas == null || eventData == null)
            return;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
            rectTransform.anchoredPosition = localPoint;
    }
}
