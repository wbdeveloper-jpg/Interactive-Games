using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Drag Runtime")]
    public Transform parentAfterDrag;

    [Header("Answer Data")]
    [Tooltip("Use only 0.2, 0.4, 0.6, 0.8, or 1.0")]
    public float intensity = 0.2f;

    [Tooltip("Emotion label. Must match SetQuestions / EmotionAudioMapper labels. Example: Happy, Sad, Angry.")]
    public string label;

    [Tooltip("Legacy field kept so old prefab references are not lost. New flow uses intensity + label through EmotionAudioMapper.")]
    public AudioClip correspondingClip;

    [Header("Layout")]
    [SerializeField] private float returnMargin = 35f;

    [Header("Debug")]
    [SerializeField] private bool logDragEvents = false;

    private Image image;
    private RectTransform rectTransform;
    private Canvas dragCanvas;
    private RectTransform dragCanvasRect;
    private Camera dragCamera;

    private void Awake()
    {
        CacheReferences();
        NormalizeIntensityInPlace();
    }

    private void OnValidate()
    {
        NormalizeIntensityInPlace();
        returnMargin = Mathf.Max(0f, returnMargin);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (logDragEvents)
            Debug.Log("Draggable: Drag Started", this);

        CacheReferences();
        parentAfterDrag = transform.parent;
        dragCamera = ResolveEventCamera(eventData);

        if (dragCanvas != null)
        {
            transform.SetParent(dragCanvas.transform, true);
        }
        else
        {
            transform.SetParent(transform.root, true);
        }

        transform.SetAsLastSibling();

        if (image != null)
            image.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
            return;

        if (dragCanvas != null && dragCanvasRect != null)
        {
            Camera cameraToUse = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : dragCamera;

            Vector2 localPointerPosition;
            bool positionFound = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvasRect,
                eventData.position,
                cameraToUse,
                out localPointerPosition
            );

            if (positionFound)
            {
                rectTransform.anchoredPosition = localPointerPosition;
                return;
            }
        }

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (logDragEvents)
            Debug.Log("Draggable: Drag End", this);

        ReturnToParent();

        if (image != null)
            image.raycastTarget = true;
    }

    public void SetParentAfterDrag(Transform newParent)
    {
        parentAfterDrag = newParent;
    }

    public void ReturnToParent()
    {
        if (parentAfterDrag == null)
            return;

        transform.SetParent(parentAfterDrag, false);
        SetStretchWithMargins(rectTransform, returnMargin);
    }

    private void CacheReferences()
    {
        if (image == null)
            image = GetComponent<Image>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        dragCanvas = GetComponentInParent<Canvas>();
        dragCanvasRect = dragCanvas != null ? dragCanvas.transform as RectTransform : null;
    }

    private Camera ResolveEventCamera(PointerEventData eventData)
    {
        if (dragCanvas == null || dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (dragCanvas.worldCamera != null)
            return dragCanvas.worldCamera;

        if (eventData != null && eventData.pressEventCamera != null)
            return eventData.pressEventCamera;

        return Camera.main;
    }

    private void NormalizeIntensityInPlace()
    {
        intensity = NormalizeIntensity(intensity);
    }

    public static float NormalizeIntensity(float value)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp01(value) / 0.2f);
        step = Mathf.Clamp(step, 1, 5);
        return step * 0.2f;
    }

    public static string NormalizeLabel(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static bool LabelsMatch(string a, string b)
    {
        return string.Equals(NormalizeLabel(a), NormalizeLabel(b), System.StringComparison.OrdinalIgnoreCase);
    }

    public static void SetStretchWithMargins(RectTransform rt, float margin = 35f)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(margin, margin);
        rt.offsetMax = new Vector2(-margin, -margin);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
    }
}
