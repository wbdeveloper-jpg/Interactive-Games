using UnityEngine;
using UnityEngine.EventSystems;

public class SkyFallBasketDrag : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("References")]
    public RectTransform playArea;
    public RectTransform basket;

    [Header("Input Rules")]
    public bool requireStartOnBasket = true;
    public bool lockYPosition = true;
    public float sidePadding = 20f;

    [Header("Movement Feel")]
    public bool useSmoothMovement = true;
    public float followSpeed = 22f;

    private bool isDragging;
    private float targetX;
    private float lockedY;
    private bool hasTarget;

    private void Awake()
    {
        if (basket == null)
            basket = transform as RectTransform;

        if (basket != null)
        {
            targetX = basket.anchoredPosition.x;
            lockedY = basket.anchoredPosition.y;
            hasTarget = true;
        }
    }

    private void Update()
    {
        if (!useSmoothMovement || !hasTarget || basket == null)
            return;

        Vector2 position = basket.anchoredPosition;
        position.x = Mathf.Lerp(position.x, targetX, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));

        if (lockYPosition)
            position.y = lockedY;

        basket.anchoredPosition = position;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (basket == null || playArea == null)
            return;

        if (requireStartOnBasket &&
            !RectTransformUtility.RectangleContainsScreenPoint(basket, eventData.position, eventData.pressEventCamera))
        {
            isDragging = false;
            return;
        }

        isDragging = true;
        lockedY = basket.anchoredPosition.y;
        MoveBasket(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        MoveBasket(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void MoveBasket(PointerEventData eventData)
    {
        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        if (!success)
            return;

        float playHalfWidth = playArea.rect.width * 0.5f;
        float basketHalfWidth = basket.rect.width * 0.5f;

        float minX = -playHalfWidth + basketHalfWidth + sidePadding;
        float maxX = playHalfWidth - basketHalfWidth - sidePadding;

        targetX = Mathf.Clamp(localPoint.x, minX, maxX);
        hasTarget = true;

        if (!useSmoothMovement)
        {
            Vector2 position = basket.anchoredPosition;
            position.x = targetX;

            if (lockYPosition)
                position.y = lockedY;

            basket.anchoredPosition = position;
        }
    }
}
