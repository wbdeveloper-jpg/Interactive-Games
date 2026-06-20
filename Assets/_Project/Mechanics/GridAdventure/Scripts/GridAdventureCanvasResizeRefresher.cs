using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class GridAdventureCanvasResizeRefresher : MonoBehaviour
{
    [Header("Watch")]
    [Tooltip("Usually the Canvas RectTransform. Leave empty to use this object's RectTransform.")]
    public RectTransform watchRoot;

    [Header("Refresh")]
    [Min(1)] public int refreshPasses = 8;
    [Min(0f)] public float sizeChangeThreshold = 0.5f;
    public bool refreshInEditMode = true;
    public bool forceCanvasUpdate = true;
    public bool refreshEveryFrame = true;

    private Vector2 _lastRootSize = new Vector2(-9999f, -9999f);
    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private int _pendingPasses;
    private bool _isRefreshing;

    private void Awake()
    {
        Cache();
        RequestRefresh();
    }

    private void OnEnable()
    {
        Cache();
        Canvas.willRenderCanvases += HandleWillRenderCanvases;
        RectTransform.reapplyDrivenProperties += HandleReapplyDrivenProperties;
        RequestRefresh();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= HandleWillRenderCanvases;
        RectTransform.reapplyDrivenProperties -= HandleReapplyDrivenProperties;
    }

    private void Update()
    {
        if (!Application.isPlaying && !refreshInEditMode)
            return;

        Cache();

        if (HasSizeChanged())
            RequestRefresh();

        if (refreshEveryFrame || _pendingPasses > 0)
            RunRefreshPass(refreshEveryFrame ? 1 : _pendingPasses);
    }

    private void OnRectTransformDimensionsChange()
    {
        RequestRefresh();
    }

    private void HandleWillRenderCanvases()
    {
        if (_isRefreshing)
            return;

        if (_pendingPasses > 0)
            RunRefreshPass(1);
    }

    private void HandleReapplyDrivenProperties(RectTransform driven)
    {
        RectTransform root = GetWatchRoot();
        if (root == null || driven == null || _isRefreshing)
            return;

        if ((driven == root || driven.IsChildOf(root)) && HasSizeChanged())
            RequestRefresh();
    }

    [ContextMenu("Force Refresh Responsive Layout")]
    public void ForceRefreshNow()
    {
        Cache();
        _pendingPasses = Mathf.Max(_pendingPasses, refreshPasses);
        RunRefreshPass(refreshPasses);
        RememberCurrentSize();
    }

    public void RequestRefresh()
    {
        _pendingPasses = Mathf.Max(_pendingPasses, Mathf.Max(1, refreshPasses));
    }

    private void RunRefreshPass(int passes)
    {
        if (passes <= 0)
            return;

        if (_pendingPasses > 0)
            _pendingPasses = Mathf.Max(0, _pendingPasses - passes);

        for (int i = 0; i < passes; i++)
            RefreshAllResponsiveLayouts();

        RememberCurrentSize();
    }

    private bool HasSizeChanged()
    {
        RectTransform root = GetWatchRoot();
        Vector2 size = root != null ? root.rect.size : Vector2.zero;

        bool rectChanged = (_lastRootSize - size).sqrMagnitude > sizeChangeThreshold * sizeChangeThreshold;
        bool screenChanged = _lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height;

        return rectChanged || screenChanged;
    }

    private void RememberCurrentSize()
    {
        RectTransform root = GetWatchRoot();
        _lastRootSize = root != null ? root.rect.size : Vector2.zero;
        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
    }

    private void RefreshAllResponsiveLayouts()
    {
        RectTransform root = GetWatchRoot();
        if (root == null)
            return;

        _isRefreshing = true;
        try
        {
            if (forceCanvasUpdate)
                Canvas.ForceUpdateCanvases();

            GridAdventureMainScreenLayout[] mainLayouts = root.GetComponentsInChildren<GridAdventureMainScreenLayout>(true);
            for (int i = 0; i < mainLayouts.Length; i++)
                mainLayouts[i].ApplyLayoutNow();

            GridAdventureCenterSquareLayout[] centerLayouts = root.GetComponentsInChildren<GridAdventureCenterSquareLayout>(true);
            for (int i = 0; i < centerLayouts.Length; i++)
                centerLayouts[i].ApplyLayoutImmediate();

            GridAdventureSquareRootFitter[] squareFitters = root.GetComponentsInChildren<GridAdventureSquareRootFitter>(true);
            for (int i = 0; i < squareFitters.Length; i++)
                squareFitters[i].ApplySquareSize();

            GridAdventureCoordinateGridLayout[] coordinateGrids = root.GetComponentsInChildren<GridAdventureCoordinateGridLayout>(true);
            for (int i = 0; i < coordinateGrids.Length; i++)
                coordinateGrids[i].ForceRefresh();

            GridAdventureResponsiveGrid[] responsiveGrids = root.GetComponentsInChildren<GridAdventureResponsiveGrid>(true);
            for (int i = 0; i < responsiveGrids.Length; i++)
                responsiveGrids[i].ForceRefresh();

            for (int i = 0; i < coordinateGrids.Length; i++)
                coordinateGrids[i].ForceRefresh();

            LayoutRebuilder.ForceRebuildLayoutImmediate(root);

            if (forceCanvasUpdate)
                Canvas.ForceUpdateCanvases();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private RectTransform GetWatchRoot()
    {
        if (watchRoot != null)
            return watchRoot;

        return transform as RectTransform;
    }

    private void Cache()
    {
        if (watchRoot == null)
            watchRoot = transform as RectTransform;
    }
}
