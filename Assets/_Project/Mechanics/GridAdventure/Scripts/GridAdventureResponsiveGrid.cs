using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
public class GridAdventureResponsiveGrid : MonoBehaviour
{
    [Header("Grid")]
    [Min(1)] public int columns = 3;
    [Min(1)] public int rows = 3;
    public bool keepCellsSquare = true;

    [Header("Responsive Size")]
    public Vector2 minCellSize = new Vector2(72f, 72f);
    public Vector2 maxCellSize = new Vector2(180f, 180f);
    [Tooltip("Recommended ON for mobile. Allows cells/cards to shrink below min size if the available square is smaller.")]
    public bool allowShrinkBelowMinWhenNeeded = true;

    [Header("Auto Fit Children")]
    [Tooltip("Useful for the basket. It chooses the best columns/rows from active child count and available space.")]
    public bool autoFitToActiveChildren;
    [Min(1)] public int autoFitMaxColumns = 4;
    [Min(1)] public int autoFitMaxRows = 4;

    [Header("Refresh")]
    public bool refreshEveryFrame = true;

    private RectTransform rectTransform;
    private GridLayoutGroup gridLayout;
    private Vector2 lastSize = new Vector2(-9999f, -9999f);
    private int lastActiveChildCount = -1;

    private void Awake()
    {
        Cache();
        Refresh();
    }

    private void OnEnable()
    {
        Cache();
        Canvas.willRenderCanvases += HandleWillRenderCanvases;
        Refresh();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= HandleWillRenderCanvases;
    }

    private void LateUpdate()
    {
        if (refreshEveryFrame)
            RefreshIfNeeded();
    }

    private void OnRectTransformDimensionsChange()
    {
        Refresh();
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        autoFitMaxColumns = Mathf.Max(1, autoFitMaxColumns);
        autoFitMaxRows = Mathf.Max(1, autoFitMaxRows);
        Refresh();
    }

    private void HandleWillRenderCanvases()
    {
        RefreshIfNeeded();
    }

    public void ForceRefresh()
    {
        lastSize = new Vector2(-9999f, -9999f);
        lastActiveChildCount = -1;
        Refresh();
    }

    public void Refresh()
    {
        Cache();
        if (rectTransform == null || gridLayout == null) return;

        Rect rect = rectTransform.rect;
        if (rect.width <= 1f || rect.height <= 1f) return;

        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, rows);

        if (autoFitToActiveChildren)
            PickBestGridForActiveChildren(rect, out safeColumns, out safeRows);

        Vector2 cellSize = CalculateCellSize(rect, safeColumns, safeRows);
        gridLayout.cellSize = cellSize;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = safeColumns;

        if (!autoFitToActiveChildren)
        {
            safeColumns = Mathf.Max(1, columns);
            safeRows = Mathf.Max(1, rows);
        }
        else
        {
            columns = safeColumns;
            rows = safeRows;
        }

        lastSize = rect.size;
        lastActiveChildCount = CountActiveLayoutChildren();
    }

    private void PickBestGridForActiveChildren(Rect rect, out int bestColumns, out int bestRows)
    {
        int count = Mathf.Max(1, CountActiveLayoutChildren());
        int maxColumns = Mathf.Max(1, autoFitMaxColumns);
        int maxRows = Mathf.Max(1, autoFitMaxRows);

        bestColumns = Mathf.Clamp(columns, 1, maxColumns);
        bestRows = Mathf.Clamp(Mathf.CeilToInt((float)count / bestColumns), 1, maxRows);

        float bestScore = -1f;
        for (int candidateColumns = 1; candidateColumns <= maxColumns; candidateColumns++)
        {
            int candidateRows = Mathf.CeilToInt((float)count / candidateColumns);
            if (candidateRows < 1 || candidateRows > maxRows) continue;

            Vector2 candidateSize = CalculateCellSize(rect, candidateColumns, candidateRows);
            float score = candidateSize.x * candidateSize.y;

            // Prefer fewer empty visual gaps and larger square cards.
            int capacity = candidateColumns * candidateRows;
            score -= Mathf.Max(0, capacity - count) * 100f;

            if (keepCellsSquare)
                score += candidateSize.x * 0.25f;

            if (score > bestScore)
            {
                bestScore = score;
                bestColumns = candidateColumns;
                bestRows = candidateRows;
            }
        }
    }

    private Vector2 CalculateCellSize(Rect rect, int safeColumns, int safeRows)
    {
        float horizontalPadding = gridLayout.padding.left + gridLayout.padding.right;
        float verticalPadding = gridLayout.padding.top + gridLayout.padding.bottom;
        float spacingX = gridLayout.spacing.x * Mathf.Max(0, safeColumns - 1);
        float spacingY = gridLayout.spacing.y * Mathf.Max(0, safeRows - 1);

        float availableWidth = Mathf.Max(1f, rect.width - horizontalPadding - spacingX);
        float availableHeight = Mathf.Max(1f, rect.height - verticalPadding - spacingY);

        float cellWidth = availableWidth / Mathf.Max(1, safeColumns);
        float cellHeight = availableHeight / Mathf.Max(1, safeRows);

        if (keepCellsSquare)
        {
            float square = Mathf.Min(cellWidth, cellHeight);
            cellWidth = square;
            cellHeight = square;
        }

        if (allowShrinkBelowMinWhenNeeded)
        {
            cellWidth = Mathf.Min(cellWidth, maxCellSize.x);
            cellHeight = Mathf.Min(cellHeight, maxCellSize.y);
        }
        else
        {
            cellWidth = Mathf.Clamp(cellWidth, minCellSize.x, maxCellSize.x);
            cellHeight = Mathf.Clamp(cellHeight, minCellSize.y, maxCellSize.y);
        }

        cellWidth = Mathf.Max(1f, cellWidth);
        cellHeight = Mathf.Max(1f, cellHeight);
        return new Vector2(cellWidth, cellHeight);
    }

    private int CountActiveLayoutChildren()
    {
        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child.gameObject.activeSelf) continue;

            LayoutElement layoutElement = child.GetComponent<LayoutElement>();
            if (layoutElement != null && layoutElement.ignoreLayout) continue;

            count++;
        }

        return count;
    }

    private void RefreshIfNeeded()
    {
        Cache();
        if (rectTransform == null) return;

        Vector2 currentSize = rectTransform.rect.size;
        int activeCount = CountActiveLayoutChildren();

        if ((currentSize - lastSize).sqrMagnitude > 0.01f || activeCount != lastActiveChildCount)
            Refresh();
    }

    private void Cache()
    {
        if (rectTransform == null) rectTransform = transform as RectTransform;
        if (gridLayout == null) gridLayout = GetComponent<GridLayoutGroup>();
    }
}
