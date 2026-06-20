using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class GridAdventureMainScreenLayout : MonoBehaviour
{
    [Header("Regions")]
    public RectTransform topBar;
    public RectTransform centerContent;
    public RectTransform clueBanner;

    [Header("Safe Padding")]
    [Min(0)] public int paddingLeft = 30;
    [Min(0)] public int paddingRight = 30;
    [Min(0)] public int paddingTop = 18;
    [Min(0)] public int paddingBottom = 22;
    [Min(0f)] public float verticalSpacing = 12f;

    [Header("Region Heights")]
    [Tooltip("Top bar is capped to this height so it never steals center gameplay space after resizing.")]
    [Min(56f)] public float topBarHeight = 100f;

    [Tooltip("Bottom clue banner height.")]
    [Min(56f)] public float clueBannerHeight = 96f;

    [Tooltip("If screen height becomes very small, top and clue heights can shrink proportionally to preserve gameplay space.")]
    public bool allowHeightCompressionOnTinyScreens = true;

    [Range(0.35f, 0.9f)] public float minimumCenterHeightRatio = 0.58f;

    [Header("Behaviour")]
    public bool disableLayoutGroupOnThisObject = true;
    public bool applyEveryFrame = true;
    public bool refreshCenterSquareLayout = true;
    public bool forceCanvasUpdateBeforeLayout = false;

    private RectTransform _rectTransform;
    private Coroutine _delayedRefreshCoroutine;

    private void Awake()
    {
        Cache();
        ApplyLayoutNow();
    }

    private void OnEnable()
    {
        Cache();
        Canvas.willRenderCanvases += HandleWillRenderCanvases;
        RequestMultiFrameRefresh();
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= HandleWillRenderCanvases;
        if (_delayedRefreshCoroutine != null)
        {
            StopCoroutine(_delayedRefreshCoroutine);
            _delayedRefreshCoroutine = null;
        }
    }

    private void LateUpdate()
    {
        if (applyEveryFrame)
            ApplyLayoutNow();
    }

    private void OnRectTransformDimensionsChange()
    {
        RequestMultiFrameRefresh();
    }

    private void OnValidate()
    {
        paddingLeft = Mathf.Max(0, paddingLeft);
        paddingRight = Mathf.Max(0, paddingRight);
        paddingTop = Mathf.Max(0, paddingTop);
        paddingBottom = Mathf.Max(0, paddingBottom);
        topBarHeight = Mathf.Max(56f, topBarHeight);
        clueBannerHeight = Mathf.Max(56f, clueBannerHeight);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);
        minimumCenterHeightRatio = Mathf.Clamp(minimumCenterHeightRatio, 0.35f, 0.9f);
        ApplyLayoutNow();
    }

    private void HandleWillRenderCanvases()
    {
        ApplyLayoutNow();
    }

    [ContextMenu("Apply Main Screen Layout")]
    public void ApplyLayoutFromContext()
    {
        ForceRefresh();
    }

    public void ForceRefresh()
    {
        ApplyLayoutNow();
        RequestMultiFrameRefresh();
    }

    public void ApplyLayout(bool force)
    {
        ApplyLayoutNow();
    }

    public void ApplyLayoutNow()
    {
        Cache();
        if (_rectTransform == null || topBar == null || centerContent == null || clueBanner == null)
            return;

        if (disableLayoutGroupOnThisObject)
            DisableParentLayoutDrivers();

        if (forceCanvasUpdateBeforeLayout)
            Canvas.ForceUpdateCanvases();

        Rect rect = _rectTransform.rect;
        if (rect.width <= 1f || rect.height <= 1f)
            return;

        int left = Mathf.Max(0, paddingLeft);
        int right = Mathf.Max(0, paddingRight);
        int top = Mathf.Max(0, paddingTop);
        int bottom = Mathf.Max(0, paddingBottom);

        float innerWidth = Mathf.Max(1f, rect.width - left - right);
        float innerHeight = Mathf.Max(1f, rect.height - top - bottom);
        float spacing = Mathf.Max(0f, verticalSpacing);
        float topHeight = Mathf.Max(56f, topBarHeight);
        float clueHeight = Mathf.Max(56f, clueBannerHeight);

        ResolveHeights(innerHeight, ref topHeight, ref clueHeight, ref spacing);

        SetTopRegion(topBar, left, right, top, topHeight);
        SetBottomRegion(clueBanner, left, right, bottom, clueHeight);
        SetMiddleRegion(centerContent, left, right, top + topHeight + spacing, bottom + clueHeight + spacing);

        // Keep child layout elements from re-expanding back to desktop-generated values.
        NormalizeRegionLayout(topBar, topHeight, innerWidth);
        NormalizeRegionLayout(clueBanner, clueHeight, innerWidth);

        if (refreshCenterSquareLayout)
        {
            GridAdventureCenterSquareLayout squareLayout = centerContent.GetComponent<GridAdventureCenterSquareLayout>();
            if (squareLayout != null)
                squareLayout.ApplyLayoutImmediate();
        }
    }

    private void ResolveHeights(float innerHeight, ref float topHeight, ref float clueHeight, ref float spacing)
    {
        float required = topHeight + clueHeight + spacing * 2f;
        float desiredCenterMin = innerHeight * minimumCenterHeightRatio;
        float maximumFixedAllowed = Mathf.Max(1f, innerHeight - desiredCenterMin);

        if (!allowHeightCompressionOnTinyScreens || required <= maximumFixedAllowed)
            return;

        float fixedWithoutSpacing = Mathf.Max(1f, topHeight + clueHeight);
        float spacingShare = Mathf.Min(spacing * 2f, maximumFixedAllowed * 0.18f);
        float heightAvailableForBars = Mathf.Max(112f, maximumFixedAllowed - spacingShare);
        float scale = Mathf.Clamp01(heightAvailableForBars / fixedWithoutSpacing);

        topHeight = Mathf.Max(56f, topHeight * scale);
        clueHeight = Mathf.Max(56f, clueHeight * scale);
        spacing = Mathf.Max(4f, spacing * scale);
    }

    private static void SetTopRegion(RectTransform target, int left, int right, int top, float height)
    {
        target.anchorMin = new Vector2(0f, 1f);
        target.anchorMax = new Vector2(1f, 1f);
        target.pivot = new Vector2(0.5f, 1f);
        target.anchoredPosition = Vector2.zero;
        target.offsetMin = new Vector2(left, -top - height);
        target.offsetMax = new Vector2(-right, -top);
    }

    private static void SetBottomRegion(RectTransform target, int left, int right, int bottom, float height)
    {
        target.anchorMin = new Vector2(0f, 0f);
        target.anchorMax = new Vector2(1f, 0f);
        target.pivot = new Vector2(0.5f, 0f);
        target.anchoredPosition = Vector2.zero;
        target.offsetMin = new Vector2(left, bottom);
        target.offsetMax = new Vector2(-right, bottom + height);
    }

    private static void SetMiddleRegion(RectTransform target, int left, int right, float topInset, float bottomInset)
    {
        target.anchorMin = new Vector2(0f, 0f);
        target.anchorMax = new Vector2(1f, 1f);
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;
        target.offsetMin = new Vector2(left, bottomInset);
        target.offsetMax = new Vector2(-right, -topInset);
    }

    private void NormalizeRegionLayout(RectTransform region, float height, float width)
    {
        LayoutElement layout = region.GetComponent<LayoutElement>();
        if (layout == null) return;

        layout.minWidth = 0f;
        layout.minHeight = 0f;
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    private void DisableParentLayoutDrivers()
    {
        LayoutGroup group = GetComponent<LayoutGroup>();
        if (group != null && group.enabled)
            group.enabled = false;

        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        if (fitter != null && fitter.enabled)
            fitter.enabled = false;
    }

    private void RequestMultiFrameRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (Application.isPlaying)
        {
            if (_delayedRefreshCoroutine != null)
                StopCoroutine(_delayedRefreshCoroutine);
            _delayedRefreshCoroutine = StartCoroutine(ApplyForNextFrames());
        }
        else
        {
            ApplyLayoutNow();
        }
    }

    private IEnumerator ApplyForNextFrames()
    {
        for (int i = 0; i < 6; i++)
        {
            ApplyLayoutNow();
            yield return null;
        }
        _delayedRefreshCoroutine = null;
    }

    private void Cache()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;
    }
}
