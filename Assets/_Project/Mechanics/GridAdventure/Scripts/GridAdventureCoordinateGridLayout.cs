using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GridAdventureCoordinateGridLayout : MonoBehaviour
{
    [Header("References")]
    public RectTransform columnHeaderRow;
    public RectTransform bodyRow;
    public RectTransform rowHeaderColumn;
    public RectTransform gridRoot;

    [Header("Grid Size")]
    [Min(1)] public int columns = 3;
    [Min(1)] public int rows = 3;

    [Header("Adaptive Spacing")]
    [Range(0.04f, 0.22f)] public float headerSizeRatio = 0.105f;
    [Min(12f)] public float minHeaderSize = 22f;
    [Min(12f)] public float maxHeaderSize = 42f;

    [Range(0.01f, 0.12f)] public float gapRatio = 0.025f;
    [Min(0f)] public float minGap = 4f;
    [Min(0f)] public float maxGap = 12f;

    [Range(0f, 0.10f)] public float outerPaddingRatio = 0.035f;
    [Min(0f)] public float minOuterPadding = 6f;
    [Min(0f)] public float maxOuterPadding = 18f;

    [Range(0f, 0.10f)] public float cellPaddingRatio = 0.018f;
    [Min(0f)] public float minCellPadding = 2f;
    [Min(0f)] public float maxCellPadding = 8f;

    [Range(0f, 0.10f)] public float cellSpacingRatio = 0.035f;
    [Min(0f)] public float minCellSpacing = 5f;
    [Min(0f)] public float maxCellSpacing = 14f;

    [Header("Text")]
    [Min(8f)] public float minHeaderFontSize = 14f;
    [Min(8f)] public float maxHeaderFontSize = 25f;
    [Min(7f)] public float minCellCoordinateFontSize = 9f;
    [Min(7f)] public float maxCellCoordinateFontSize = 17f;

    [Header("Behaviour")]
    public bool disableUnityLayoutGroupsInsideGrid = true;
    public bool refreshEveryFrame = true;
    public bool forceGridLayoutRefresh = true;

    private RectTransform _shell;
    private GridLayoutGroup _gridLayout;
    private GridAdventureResponsiveGrid _responsiveGrid;
    private Vector2 _lastShellSize = new Vector2(-9999f, -9999f);

    private void Awake()
    {
        Cache();
        ApplyLayoutNow();
    }

    private void OnEnable()
    {
        Cache();
        Canvas.willRenderCanvases += HandleWillRenderCanvases;
        ApplyLayoutNow();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= HandleWillRenderCanvases;
    }

    private void LateUpdate()
    {
        if (refreshEveryFrame)
            ApplyLayoutNow();
    }

    private void OnRectTransformDimensionsChange()
    {
        ForceRefresh();
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        maxHeaderSize = Mathf.Max(minHeaderSize, maxHeaderSize);
        maxGap = Mathf.Max(minGap, maxGap);
        maxOuterPadding = Mathf.Max(minOuterPadding, maxOuterPadding);
        maxCellPadding = Mathf.Max(minCellPadding, maxCellPadding);
        maxCellSpacing = Mathf.Max(minCellSpacing, maxCellSpacing);
        maxHeaderFontSize = Mathf.Max(minHeaderFontSize, maxHeaderFontSize);
        maxCellCoordinateFontSize = Mathf.Max(minCellCoordinateFontSize, maxCellCoordinateFontSize);
        ApplyLayoutNow();
    }

    private void HandleWillRenderCanvases()
    {
        ApplyLayoutIfNeeded();
    }

    [ContextMenu("Apply Coordinate Grid Layout")]
    public void ForceRefresh()
    {
        _lastShellSize = new Vector2(-9999f, -9999f);
        ApplyLayoutNow();
    }

    public void ApplyLayoutIfNeeded()
    {
        Cache();
        if (_shell == null) return;

        Vector2 currentSize = _shell.rect.size;
        if ((currentSize - _lastShellSize).sqrMagnitude > 0.01f)
            ApplyLayoutNow();
    }

    public void ApplyLayoutNow()
    {
        Cache();
        if (_shell == null || columnHeaderRow == null || bodyRow == null || rowHeaderColumn == null || gridRoot == null)
            return;

        Rect rect = _shell.rect;
        if (rect.width <= 4f || rect.height <= 4f)
            return;

        if (disableUnityLayoutGroupsInsideGrid)
            DisableConflictingLayoutGroups();

        float baseSize = Mathf.Min(rect.width, rect.height);
        float outerPadding = ClampByRatio(baseSize, outerPaddingRatio, minOuterPadding, maxOuterPadding);
        float gap = ClampByRatio(baseSize, gapRatio, minGap, maxGap);
        float headerSize = ClampByRatio(baseSize, headerSizeRatio, minHeaderSize, maxHeaderSize);
        float cellPadding = ClampByRatio(baseSize, cellPaddingRatio, minCellPadding, maxCellPadding);
        float cellSpacing = ClampByRatio(baseSize, cellSpacingRatio, minCellSpacing, maxCellSpacing);

        float innerWidth = Mathf.Max(1f, rect.width - outerPadding * 2f);
        float innerHeight = Mathf.Max(1f, rect.height - outerPadding * 2f);

        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, rows);

        float gridAvailableWidth = innerWidth - headerSize - gap;
        float gridAvailableHeight = innerHeight - headerSize - gap;
        float cellAvailableWidth = gridAvailableWidth - cellPadding * 2f - cellSpacing * Mathf.Max(0, safeColumns - 1);
        float cellAvailableHeight = gridAvailableHeight - cellPadding * 2f - cellSpacing * Mathf.Max(0, safeRows - 1);
        float cellSize = Mathf.Min(cellAvailableWidth / safeColumns, cellAvailableHeight / safeRows);

        // On very narrow screens, reduce headers/gaps before sacrificing cell size too much.
        if (cellSize < 1f)
        {
            headerSize = Mathf.Max(minHeaderSize, headerSize * 0.75f);
            gap = Mathf.Max(minGap, gap * 0.75f);
            cellPadding = Mathf.Max(minCellPadding, cellPadding * 0.75f);
            cellSpacing = Mathf.Max(minCellSpacing, cellSpacing * 0.75f);

            gridAvailableWidth = innerWidth - headerSize - gap;
            gridAvailableHeight = innerHeight - headerSize - gap;
            cellAvailableWidth = gridAvailableWidth - cellPadding * 2f - cellSpacing * Mathf.Max(0, safeColumns - 1);
            cellAvailableHeight = gridAvailableHeight - cellPadding * 2f - cellSpacing * Mathf.Max(0, safeRows - 1);
            cellSize = Mathf.Min(cellAvailableWidth / safeColumns, cellAvailableHeight / safeRows);
        }

        cellSize = Mathf.Max(1f, Mathf.Floor(cellSize));

        float gridWidth = cellPadding * 2f + cellSize * safeColumns + cellSpacing * Mathf.Max(0, safeColumns - 1);
        float gridHeight = cellPadding * 2f + cellSize * safeRows + cellSpacing * Mathf.Max(0, safeRows - 1);
        float contentWidth = headerSize + gap + gridWidth;
        float contentHeight = headerSize + gap + gridHeight;

        float startX = (rect.width - contentWidth) * 0.5f;
        float startY = (rect.height - contentHeight) * 0.5f;

        SetTopLeftRect(columnHeaderRow, startX + headerSize + gap, startY, gridWidth, headerSize);
        SetTopLeftRect(bodyRow, startX, startY + headerSize + gap, contentWidth, gridHeight);
        SetTopLeftRect(rowHeaderColumn, 0f, 0f, headerSize, gridHeight);
        SetTopLeftRect(gridRoot, headerSize + gap, 0f, gridWidth, gridHeight);

        ApplyGridLayout(cellSize, cellPadding, cellSpacing, safeColumns);
        LayoutColumnHeaders(cellSize, cellPadding, cellSpacing, headerSize, safeColumns);
        LayoutRowHeaders(cellSize, cellPadding, cellSpacing, headerSize, safeRows);
        ResizeCellCoordinateLabels(cellSize);

        _lastShellSize = rect.size;
    }

    private void ApplyGridLayout(float cellSize, float cellPadding, float cellSpacing, int safeColumns)
    {
        if (_gridLayout == null) return;

        int padding = Mathf.RoundToInt(cellPadding);
        _gridLayout.padding = new RectOffset(padding, padding, padding, padding);
        _gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
        _gridLayout.cellSize = new Vector2(cellSize, cellSize);
        _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _gridLayout.constraintCount = safeColumns;
        _gridLayout.childAlignment = TextAnchor.MiddleCenter;

        if (_responsiveGrid != null)
        {
            _responsiveGrid.columns = safeColumns;
            _responsiveGrid.rows = Mathf.Max(1, rows);
            _responsiveGrid.allowShrinkBelowMinWhenNeeded = true;
            _responsiveGrid.maxCellSize = new Vector2(9999f, 9999f);
            _responsiveGrid.refreshEveryFrame = false;
        }

        if (forceGridLayoutRefresh)
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridRoot);
    }

    private void LayoutColumnHeaders(float cellSize, float cellPadding, float cellSpacing, float headerHeight, int safeColumns)
    {
        HideCornerSpacerIfPresent();
        int headerIndex = 0;
        for (int i = 0; i < columnHeaderRow.childCount; i++)
        {
            RectTransform child = columnHeaderRow.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;
            if (child.name.ToLowerInvariant().Contains("corner")) continue;

            if (headerIndex >= safeColumns) break;
            float x = cellPadding + headerIndex * (cellSize + cellSpacing);
            SetTopLeftRect(child, x, 0f, cellSize, headerHeight);
            ApplyHeaderTextSize(child, cellSize);
            headerIndex++;
        }
    }

    private void LayoutRowHeaders(float cellSize, float cellPadding, float cellSpacing, float headerWidth, int safeRows)
    {
        int headerIndex = 0;
        for (int i = 0; i < rowHeaderColumn.childCount; i++)
        {
            RectTransform child = rowHeaderColumn.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            if (headerIndex >= safeRows) break;
            float y = cellPadding + headerIndex * (cellSize + cellSpacing);
            SetTopLeftRect(child, 0f, y, headerWidth, cellSize);
            ApplyHeaderTextSize(child, cellSize);
            headerIndex++;
        }
    }

    private void ResizeCellCoordinateLabels(float cellSize)
    {
        float fontSize = Mathf.Clamp(cellSize * 0.17f, minCellCoordinateFontSize, maxCellCoordinateFontSize);
        Vector2 labelSize = new Vector2(Mathf.Clamp(cellSize * 0.42f, 24f, 52f), Mathf.Clamp(cellSize * 0.24f, 16f, 28f));
        float inset = Mathf.Clamp(cellSize * 0.07f, 3f, 8f);

        for (int i = 0; i < gridRoot.childCount; i++)
        {
            GridAdventureCell cell = gridRoot.GetChild(i).GetComponent<GridAdventureCell>();
            if (cell == null || cell.coordinateLabel == null) continue;

            cell.coordinateLabel.fontSize = fontSize;
            RectTransform labelRect = cell.coordinateLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.sizeDelta = labelSize;
            labelRect.anchoredPosition = new Vector2(inset, -inset);
        }
    }

    private void ApplyHeaderTextSize(RectTransform root, float cellSize)
    {
        TextMeshProUGUI text = root.GetComponent<TextMeshProUGUI>();
        if (text == null)
            text = root.GetComponentInChildren<TextMeshProUGUI>(true);

        if (text == null) return;
        text.fontSize = Mathf.Clamp(cellSize * 0.26f, minHeaderFontSize, maxHeaderFontSize);
        text.enableAutoSizing = false;
    }

    private void HideCornerSpacerIfPresent()
    {
        for (int i = 0; i < columnHeaderRow.childCount; i++)
        {
            RectTransform child = columnHeaderRow.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (!child.name.ToLowerInvariant().Contains("corner")) continue;

            LayoutElement layout = child.GetComponent<LayoutElement>();
            if (layout == null) layout = child.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            child.gameObject.SetActive(false);
        }
    }

    private static float ClampByRatio(float baseSize, float ratio, float min, float max)
    {
        return Mathf.Clamp(baseSize * ratio, min, Mathf.Max(min, max));
    }

    private static void SetTopLeftRect(RectTransform target, float left, float top, float width, float height)
    {
        if (target == null) return;

        target.anchorMin = new Vector2(0f, 1f);
        target.anchorMax = new Vector2(0f, 1f);
        target.pivot = new Vector2(0f, 1f);
        target.anchoredPosition = new Vector2(left, -top);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        target.sizeDelta = new Vector2(width, height);
    }

    private void DisableConflictingLayoutGroups()
    {
        DisableLayoutGroup(_shell);
        DisableLayoutGroup(columnHeaderRow);
        DisableLayoutGroup(bodyRow);
        DisableLayoutGroup(rowHeaderColumn);

        DisableContentSizeFitter(_shell);
        DisableContentSizeFitter(columnHeaderRow);
        DisableContentSizeFitter(bodyRow);
        DisableContentSizeFitter(rowHeaderColumn);
    }

    private static void DisableLayoutGroup(RectTransform rect)
    {
        if (rect == null) return;
        LayoutGroup group = rect.GetComponent<LayoutGroup>();
        if (group != null && group.enabled)
            group.enabled = false;
    }

    private static void DisableContentSizeFitter(RectTransform rect)
    {
        if (rect == null) return;
        ContentSizeFitter fitter = rect.GetComponent<ContentSizeFitter>();
        if (fitter != null && fitter.enabled)
            fitter.enabled = false;
    }

    private void Cache()
    {
        if (_shell == null)
            _shell = transform as RectTransform;

        if (gridRoot != null)
        {
            if (_gridLayout == null)
                _gridLayout = gridRoot.GetComponent<GridLayoutGroup>();

            if (_responsiveGrid == null)
                _responsiveGrid = gridRoot.GetComponent<GridAdventureResponsiveGrid>();
        }
    }
}
