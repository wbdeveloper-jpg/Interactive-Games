using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GridAdventureSquareRootFitter : MonoBehaviour
{
    public enum SquareFitSource
    {
        CurrentLayoutSlot,
        ParentRect
    }

    public enum SquareApplyMode
    {
        VisualSizeOnly,
        LayoutElementPreferredSize
    }

    [Header("Square Fit")]
    [Tooltip("CurrentLayoutSlot is safest when this RectTransform is already controlled by a LayoutGroup.")]
    public SquareFitSource fitSource = SquareFitSource.CurrentLayoutSlot;

    [Tooltip("VisualSizeOnly avoids fighting existing LayoutGroups. Use LayoutElementPreferredSize only if the parent layout still overrides the basket.")]
    public SquareApplyMode applyMode = SquareApplyMode.VisualSizeOnly;

    [Range(0.5f, 1f)] public float sizeMultiplier = 1f;
    [Min(0f)] public float minSize = 120f;
    [Min(0f)] public float maxSize = 2000f;
    public Vector2 padding = Vector2.zero;

    [Header("Behaviour")]
    public bool refreshEveryFrame = true;
    public bool refreshResponsiveGridAfterFit = true;

    private RectTransform _rectTransform;
    private LayoutElement _layoutElement;
    private GridAdventureResponsiveGrid _responsiveGrid;
    private Vector2 _lastAppliedSize;

    private void Awake()
    {
        Cache();
        ApplySquareSize();
    }

    private void OnEnable()
    {
        Cache();
        Canvas.willRenderCanvases += ApplySquareSize;
        ApplySquareSize();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= ApplySquareSize;
    }

    private void LateUpdate()
    {
        if (refreshEveryFrame)
            ApplySquareSize();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplySquareSize();
    }

    [ContextMenu("Apply Square Size")]
    public void ApplySquareSizeFromContext()
    {
        ForceRefresh();
    }

    public void ForceRefresh()
    {
        _lastAppliedSize = new Vector2(-9999f, -9999f);
        ApplySquareSize();
    }

    public void ApplySquareSize()
    {
        Cache();
        if (_rectTransform == null) return;

        RectTransform source = GetSourceRectTransform();
        if (source == null) return;

        Rect sourceRect = source.rect;
        float availableWidth = sourceRect.width - padding.x;
        float availableHeight = sourceRect.height - padding.y;

        if (availableWidth <= 1f || availableHeight <= 1f)
            return;

        float targetSize = Mathf.Min(availableWidth, availableHeight) * sizeMultiplier;
        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);

        if (targetSize <= 1f || float.IsNaN(targetSize) || float.IsInfinity(targetSize))
            return;

        Vector2 target = new Vector2(targetSize, targetSize);
        if ((_lastAppliedSize - target).sqrMagnitude < 0.01f && Mathf.Abs(_rectTransform.rect.width - targetSize) < 0.01f && Mathf.Abs(_rectTransform.rect.height - targetSize) < 0.01f)
            return;

        if (applyMode == SquareApplyMode.LayoutElementPreferredSize)
        {
            if (_layoutElement == null)
                _layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

            _layoutElement.minWidth = targetSize;
            _layoutElement.minHeight = targetSize;
            _layoutElement.preferredWidth = targetSize;
            _layoutElement.preferredHeight = targetSize;
            _layoutElement.flexibleWidth = 0f;
            _layoutElement.flexibleHeight = 0f;
        }

        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize);
        _lastAppliedSize = target;

        if (refreshResponsiveGridAfterFit && _responsiveGrid != null)
            _responsiveGrid.Refresh();
    }

    private RectTransform GetSourceRectTransform()
    {
        if (fitSource == SquareFitSource.ParentRect)
            return _rectTransform.parent as RectTransform;

        return _rectTransform;
    }

    private void Cache()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;

        if (_layoutElement == null)
            _layoutElement = GetComponent<LayoutElement>();

        if (_responsiveGrid == null)
            _responsiveGrid = GetComponent<GridAdventureResponsiveGrid>();
    }
}
