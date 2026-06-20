using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GridAdventureCenterSquareLayout : MonoBehaviour
{
    public enum LayoutMode
    {
        Auto,
        Horizontal,
        Vertical
    }

    [Header("Targets")]
    public RectTransform leftSquarePanel;
    public RectTransform rightSquarePanel;

    [Header("Layout")]
    public LayoutMode layoutMode = LayoutMode.Auto;
    [Min(0f)] public float spacing = 28f;
    [Range(0.5f, 1f)] public float sizeMultiplier = 1f;
    [Min(0f)] public float minSquareSize = 180f;
    [Min(0f)] public float maxSquareSize = 860f;

    [Tooltip("Total horizontal/vertical padding removed from the CenterContent rect before fitting squares.")]
    public Vector2 padding = Vector2.zero;

    [Tooltip("Auto uses horizontal only when the center area is clearly wide enough.")]
    [Min(1f)] public float horizontalAspectThreshold = 1.2f;

    [Header("Refresh")]
    public bool applyEveryFrame = true;
    public bool refreshChildGrids = true;
    public bool disableLayoutDriversOnPanels = true;

    private RectTransform _rectTransform;

    private void Awake()
    {
        Cache();
        ApplyLayoutImmediate();
    }

    private void OnEnable()
    {
        Cache();
        Canvas.willRenderCanvases += HandleWillRenderCanvases;
        ApplyLayoutImmediate();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= HandleWillRenderCanvases;
    }

    private void LateUpdate()
    {
        if (applyEveryFrame)
            ApplyLayoutImmediate();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayoutImmediate();
    }

    private void OnValidate()
    {
        spacing = Mathf.Max(0f, spacing);
        minSquareSize = Mathf.Max(0f, minSquareSize);
        maxSquareSize = Mathf.Max(minSquareSize, maxSquareSize);
        sizeMultiplier = Mathf.Clamp(sizeMultiplier, 0.5f, 1f);
        horizontalAspectThreshold = Mathf.Max(1f, horizontalAspectThreshold);
        ApplyLayoutImmediate();
    }

    private void HandleWillRenderCanvases()
    {
        ApplyLayoutImmediate();
    }

    [ContextMenu("Apply Center Square Layout")]
    public void ApplyLayoutFromContext()
    {
        ForceRefresh();
    }

    public void ForceRefresh()
    {
        ApplyLayoutImmediate();
    }

    public void ApplyLayout()
    {
        ApplyLayoutImmediate();
    }

    public void ApplyLayoutImmediate()
    {
        Cache();
        if (_rectTransform == null || leftSquarePanel == null || rightSquarePanel == null)
            return;

        Rect rect = _rectTransform.rect;
        if (rect.width <= 2f || rect.height <= 2f)
            return;

        if (disableLayoutDriversOnPanels)
        {
            PreparePanelForManualLayout(leftSquarePanel);
            PreparePanelForManualLayout(rightSquarePanel);
        }

        float availableWidth = Mathf.Max(1f, rect.width - Mathf.Max(0f, padding.x));
        float availableHeight = Mathf.Max(1f, rect.height - Mathf.Max(0f, padding.y));
        float safeSpacing = Mathf.Max(0f, spacing);

        LayoutMode resolvedMode = ResolveMode(availableWidth, availableHeight, safeSpacing);
        float squareSize = CalculateSquareSize(availableWidth, availableHeight, safeSpacing, resolvedMode);

        if (squareSize <= 1f || float.IsNaN(squareSize) || float.IsInfinity(squareSize))
            return;

        ApplySquareTransform(leftSquarePanel, squareSize);
        ApplySquareTransform(rightSquarePanel, squareSize);

        if (resolvedMode == LayoutMode.Horizontal)
        {
            float totalWidth = squareSize * 2f + safeSpacing;
            leftSquarePanel.anchoredPosition = new Vector2(-totalWidth * 0.5f + squareSize * 0.5f, 0f);
            rightSquarePanel.anchoredPosition = new Vector2(totalWidth * 0.5f - squareSize * 0.5f, 0f);
        }
        else
        {
            float totalHeight = squareSize * 2f + safeSpacing;
            leftSquarePanel.anchoredPosition = new Vector2(0f, totalHeight * 0.5f - squareSize * 0.5f);
            rightSquarePanel.anchoredPosition = new Vector2(0f, -totalHeight * 0.5f + squareSize * 0.5f);
        }

        if (refreshChildGrids)
            RefreshChildResponsiveGrids();
    }

    private LayoutMode ResolveMode(float availableWidth, float availableHeight, float safeSpacing)
    {
        if (layoutMode == LayoutMode.Horizontal || layoutMode == LayoutMode.Vertical)
            return layoutMode;

        float horizontalSquare = Mathf.Min((availableWidth - safeSpacing) * 0.5f, availableHeight);
        float verticalSquare = Mathf.Min(availableWidth, (availableHeight - safeSpacing) * 0.5f);

        bool wideEnough = availableWidth >= availableHeight * horizontalAspectThreshold;
        bool horizontalMakesBetterUse = horizontalSquare >= verticalSquare * 0.9f;

        return wideEnough && horizontalMakesBetterUse ? LayoutMode.Horizontal : LayoutMode.Vertical;
    }

    private float CalculateSquareSize(float availableWidth, float availableHeight, float safeSpacing, LayoutMode resolvedMode)
    {
        float fitLimit;
        if (resolvedMode == LayoutMode.Horizontal)
            fitLimit = Mathf.Min((availableWidth - safeSpacing) * 0.5f, availableHeight);
        else
            fitLimit = Mathf.Min(availableWidth, (availableHeight - safeSpacing) * 0.5f);

        fitLimit = Mathf.Max(1f, fitLimit);
        float target = fitLimit * sizeMultiplier;
        float maxAllowed = Mathf.Min(Mathf.Max(1f, maxSquareSize), fitLimit);

        if (maxAllowed >= minSquareSize)
            return Mathf.Clamp(target, minSquareSize, maxAllowed);

        // On small mobile screens, fitting is more important than honoring old desktop min sizes.
        return maxAllowed;
    }

    private void ApplySquareTransform(RectTransform target, float size)
    {
        target.anchorMin = new Vector2(0.5f, 0.5f);
        target.anchorMax = new Vector2(0.5f, 0.5f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
        target.sizeDelta = new Vector2(size, size);

        LayoutElement layout = target.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = true;
            layout.minWidth = 0f;
            layout.minHeight = 0f;
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }
    }

    private void PreparePanelForManualLayout(RectTransform panel)
    {
        LayoutGroup layoutGroup = panel.GetComponent<LayoutGroup>();
        if (layoutGroup != null && layoutGroup.enabled)
            layoutGroup.enabled = false;

        ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
        if (fitter != null && fitter.enabled)
            fitter.enabled = false;

        LayoutElement layout = panel.GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.ignoreLayout = true;
            layout.minWidth = 0f;
            layout.minHeight = 0f;
        }
    }

    private void RefreshChildResponsiveGrids()
    {
        RefreshGrids(leftSquarePanel);
        RefreshGrids(rightSquarePanel);
    }

    private static void RefreshGrids(RectTransform root)
    {
        if (root == null) return;

        GridAdventureCoordinateGridLayout[] coordinateGrids = root.GetComponentsInChildren<GridAdventureCoordinateGridLayout>(true);
        for (int i = 0; i < coordinateGrids.Length; i++)
            coordinateGrids[i].ForceRefresh();

        GridAdventureResponsiveGrid[] grids = root.GetComponentsInChildren<GridAdventureResponsiveGrid>(true);
        for (int i = 0; i < grids.Length; i++)
            grids[i].ForceRefresh();

        for (int i = 0; i < coordinateGrids.Length; i++)
            coordinateGrids[i].ForceRefresh();
    }

    private void Cache()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;
    }
}
