using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Transform parentAfterDrag;
    public float intensity;
    public string label;
    public AudioClip correspondingClip;

    private Image image;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;

    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas != null)
            canvasRectTransform = parentCanvas.transform as RectTransform;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Drag Started");

        parentAfterDrag = transform.parent;

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
            canvasRectTransform = parentCanvas.transform as RectTransform;

        transform.SetParent(parentCanvas.transform);
        transform.SetAsLastSibling();

        if (image != null)
            image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
            return;

        if (parentCanvas == null || canvasRectTransform == null)
        {
            transform.position = eventData.position;
            return;
        }

        Camera eventCamera = null;

        if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = parentCanvas.worldCamera;

        Vector2 localPointerPosition;

        bool positionFound = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            eventData.position,
            eventCamera,
            out localPointerPosition
        );

        if (positionFound)
        {
            rectTransform.anchoredPosition = localPointerPosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Drag End");

        transform.SetParent(parentAfterDrag);
        SetStretchWithMargins(rectTransform);

        if (image != null)
            image.raycastTarget = true;
    }

    public static void SetStretchWithMargins(RectTransform rt, float margin = 35f)
    {
        if (rt == null) return;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);

        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);

        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }
}