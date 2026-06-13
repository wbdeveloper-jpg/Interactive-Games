using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FractionPortionSafeAreaFitter : MonoBehaviour
{
    [Tooltip("Extra padding inside the device safe area. Values are Left, Bottom, Right, Top.")]
    public Vector4 extraPadding = new Vector4(32f, 22f, 32f, 22f);

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea(true);
    }

    private void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea(true);
    }

    private void Update()
    {
        ApplySafeArea(false);
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplySafeArea(false);
    }

    public void ApplySafeArea(bool force)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);

        if (!force && safeArea == lastSafeArea && screenSize == lastScreenSize)
            return;

        lastSafeArea = safeArea;
        lastScreenSize = screenSize;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = new Vector2(extraPadding.x, extraPadding.y);
        rectTransform.offsetMax = new Vector2(-extraPadding.z, -extraPadding.w);
    }
}
