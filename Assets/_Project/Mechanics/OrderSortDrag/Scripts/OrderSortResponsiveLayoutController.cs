using System;
using UnityEngine;
using UnityEngine.UI;

public enum OrderSortResponsiveMode
{
    Auto,
    ForceWidePhone,
    ForcePhone,
    ForceTablet,
    Disabled
}

[Serializable]
public class OrderSortResponsiveLayoutPreset
{
    [Header("Main Heights")]
    public float headerHeight = 180f;
    public float titleBarHeight = 80f;
    public float statusBarHeight = 54f;
    public float horizontalSlotsAreaHeight = 172f;
    public float horizontalBasketAreaHeight = 292f;
    public float verticalGameplayRowHeight = 468f;
    public float actionBarHeight = 68f;
    public float feedbackHeight = 48f;

    [Header("Spacing")]
    public int rootPaddingHorizontal = 28;
    public int rootPaddingVertical = 22;
    public float rootSpacing = 14f;
    public float headerSpacing = 12f;
    public float titleBarSpacing = 18f;
    public float statusBarSpacing = 16f;
    public float actionBarSpacing = 18f;
    public float slotSpacing = 14f;

    [Header("Buttons")]
    public float sideButtonWidth = 220f;
    public float sideButtonHeight = 64f;
    public float checkButtonWidth = 260f;
    public float nextButtonWidth = 220f;
    public float actionButtonHeight = 62f;

    [Header("Cards / Slots")]
    [Tooltip("Scales text/image card sizes from the values stored on OrderSortDragManager at startup.")]
    public float cardScale = 1f;

    [Tooltip("Scales minimum slot size from the values stored on OrderSortDragManager at startup.")]
    public float minSlotScale = 1f;

    [Tooltip("Optional scale for manager's preferred slot size. In responsive slot mode this mainly affects templates and non-responsive fallback.")]
    public float preferredSlotScale = 1f;
}

public class OrderSortResponsiveLayoutController : MonoBehaviour
{
    [Header("Mode")]
    public OrderSortResponsiveMode responsiveMode = OrderSortResponsiveMode.Auto;
    public bool applyOnAwake = true;
    public bool applyOnStart = true;
    public bool updateOnResolutionChange = true;
    public bool autoFindMissingReferences = true;
    public bool applySafeAreaToRoot = true;
    public bool refreshExistingRuntimeCardsAndSlots = true;

    [Header("Auto Mode Aspect Thresholds")]
    [Tooltip("Landscape aspect ratio >= this value uses Wide Phone preset. 20:9 phones are usually here.")]
    public float widePhoneAspectThreshold = 1.85f;

    [Tooltip("Landscape aspect ratio <= this value uses Tablet preset. 4:3 and 16:10 tablets are usually here.")]
    public float tabletAspectThreshold = 1.62f;

    [Header("References")]
    public OrderSortDragManager manager;
    public RectTransform root;
    public RectTransform header;
    public RectTransform titleBar;
    public RectTransform statusBar;
    public RectTransform gameplayRow;
    public RectTransform slotsArea;
    public RectTransform basketWrapper;
    public RectTransform actionBar;
    public RectTransform feedbackArea;

    [Header("Optional Layout Groups")]
    public VerticalLayoutGroup rootLayout;
    public VerticalLayoutGroup headerLayout;
    public HorizontalOrVerticalLayoutGroup titleBarLayout;
    public HorizontalOrVerticalLayoutGroup statusBarLayout;
    public HorizontalOrVerticalLayoutGroup slotsParentLayout;
    public HorizontalOrVerticalLayoutGroup actionBarLayout;
    public GridLayoutGroup bankGridLayout;

    [Header("Optional Buttons")]
    public Button pauseButton;
    public Button howToPlayButton;
    public Button checkButton;
    public Button nextButton;

    [Header("Presets")]
    public OrderSortResponsiveLayoutPreset widePhonePreset = new OrderSortResponsiveLayoutPreset
    {
        headerHeight = 148f,
        titleBarHeight = 60f,
        statusBarHeight = 42f,
        horizontalSlotsAreaHeight = 132f,
        horizontalBasketAreaHeight = 212f,
        verticalGameplayRowHeight = 350f,
        actionBarHeight = 52f,
        feedbackHeight = 34f,
        rootPaddingHorizontal = 18,
        rootPaddingVertical = 10,
        rootSpacing = 8f,
        headerSpacing = 6f,
        titleBarSpacing = 10f,
        statusBarSpacing = 8f,
        actionBarSpacing = 10f,
        slotSpacing = 8f,
        sideButtonWidth = 170f,
        sideButtonHeight = 48f,
        checkButtonWidth = 210f,
        nextButtonWidth = 170f,
        actionButtonHeight = 48f,
        cardScale = 0.88f,
        minSlotScale = 0.88f,
        preferredSlotScale = 0.9f
    };

    public OrderSortResponsiveLayoutPreset phonePreset = new OrderSortResponsiveLayoutPreset
    {
        headerHeight = 180f,
        titleBarHeight = 80f,
        statusBarHeight = 54f,
        horizontalSlotsAreaHeight = 172f,
        horizontalBasketAreaHeight = 292f,
        verticalGameplayRowHeight = 468f,
        actionBarHeight = 68f,
        feedbackHeight = 48f,
        rootPaddingHorizontal = 28,
        rootPaddingVertical = 18,
        rootSpacing = 12f,
        headerSpacing = 10f,
        titleBarSpacing = 16f,
        statusBarSpacing = 14f,
        actionBarSpacing = 16f,
        slotSpacing = 12f,
        sideButtonWidth = 220f,
        sideButtonHeight = 62f,
        checkButtonWidth = 260f,
        nextButtonWidth = 220f,
        actionButtonHeight = 60f,
        cardScale = 1f,
        minSlotScale = 1f,
        preferredSlotScale = 1f
    };

    public OrderSortResponsiveLayoutPreset tabletPreset = new OrderSortResponsiveLayoutPreset
    {
        headerHeight = 212f,
        titleBarHeight = 96f,
        statusBarHeight = 64f,
        horizontalSlotsAreaHeight = 230f,
        horizontalBasketAreaHeight = 376f,
        verticalGameplayRowHeight = 620f,
        actionBarHeight = 82f,
        feedbackHeight = 62f,
        rootPaddingHorizontal = 42,
        rootPaddingVertical = 30,
        rootSpacing = 18f,
        headerSpacing = 14f,
        titleBarSpacing = 22f,
        statusBarSpacing = 18f,
        actionBarSpacing = 20f,
        slotSpacing = 18f,
        sideButtonWidth = 260f,
        sideButtonHeight = 72f,
        checkButtonWidth = 310f,
        nextButtonWidth = 250f,
        actionButtonHeight = 72f,
        cardScale = 1.12f,
        minSlotScale = 1.12f,
        preferredSlotScale = 1.12f
    };

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private Rect lastSafeArea;

    private bool capturedBaseSizes;
    private Vector2 baseTextCardSize;
    private Vector2 baseTextSlotSize;
    private Vector2 baseImageCardSize;
    private Vector2 baseImageSlotSize;
    private Vector2 baseMinTextSlotSize;
    private Vector2 baseMinImageSlotSize;

    private void Awake()
    {
        if (autoFindMissingReferences)
            FindMissingReferences();

        CaptureBaseManagerSizes();

        if (applyOnAwake)
            ApplyResponsiveLayout();
    }

    private void Start()
    {
        if (autoFindMissingReferences)
            FindMissingReferences();

        CaptureBaseManagerSizes();

        if (applyOnStart)
            ApplyResponsiveLayout();
    }

    private void Update()
    {
        if (!updateOnResolutionChange || responsiveMode == OrderSortResponsiveMode.Disabled)
            return;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.safeArea != lastSafeArea)
            ApplyResponsiveLayout();
    }

    [ContextMenu("Apply Responsive Layout Now")]
    public void ApplyResponsiveLayout()
    {
        if (responsiveMode == OrderSortResponsiveMode.Disabled)
            return;

        if (autoFindMissingReferences)
            FindMissingReferences();

        CaptureBaseManagerSizes();

        OrderSortResponsiveLayoutPreset preset = GetActivePreset();

        ApplySafeArea();
        ApplyRootLayout(preset);
        ApplyHeights(preset);
        ApplySpacings(preset);
        ApplyButtonSizes(preset);
        ApplyManagerSizeScales(preset);
        RefreshExistingCardsAndSlots(preset);

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastSafeArea = Screen.safeArea;

        Canvas.ForceUpdateCanvases();

        if (root != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
    }

    private OrderSortResponsiveLayoutPreset GetActivePreset()
    {
        switch (responsiveMode)
        {
            case OrderSortResponsiveMode.ForceWidePhone:
                return widePhonePreset;
            case OrderSortResponsiveMode.ForceTablet:
                return tabletPreset;
            case OrderSortResponsiveMode.ForcePhone:
                return phonePreset;
        }

        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.777f;

        if (aspect >= widePhoneAspectThreshold)
            return widePhonePreset;

        if (aspect <= tabletAspectThreshold)
            return tabletPreset;

        return phonePreset;
    }

    private void FindMissingReferences()
    {
        if (manager == null)
            manager = FindObjectOfType<OrderSortDragManager>();

        if (root == null)
            root = FindRect("OrderSortRoot");

        if (header == null)
            header = FindRect("Header");

        if (titleBar == null)
            titleBar = FindRect("TitleBar");

        if (statusBar == null)
            statusBar = FindRect("StatusBar");

        if (gameplayRow == null)
            gameplayRow = FindRect("GameplayRow");

        if (slotsArea == null)
            slotsArea = FindRect("SlotsArea");

        if (slotsArea == null)
            slotsArea = FindRect("SlotsArea_Right");

        if (basketWrapper == null)
            basketWrapper = FindRectContains("ImageBasketParent_");

        if (basketWrapper == null)
            basketWrapper = FindRectContains("TextBankParent_");

        if (actionBar == null)
            actionBar = FindRect("ActionBar");

        if (feedbackArea == null)
            feedbackArea = FindRect("FeedbackText");

        if (rootLayout == null && root != null)
            rootLayout = root.GetComponent<VerticalLayoutGroup>();

        if (headerLayout == null && header != null)
            headerLayout = header.GetComponent<VerticalLayoutGroup>();

        if (titleBarLayout == null && titleBar != null)
            titleBarLayout = titleBar.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (statusBarLayout == null && statusBar != null)
            statusBarLayout = statusBar.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (slotsParentLayout == null && manager != null && manager.slotsParent != null)
            slotsParentLayout = manager.slotsParent.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (actionBarLayout == null && actionBar != null)
            actionBarLayout = actionBar.GetComponent<HorizontalOrVerticalLayoutGroup>();

        if (bankGridLayout == null && manager != null && manager.bankParent != null)
            bankGridLayout = manager.bankParent.GetComponent<GridLayoutGroup>();

        if (pauseButton == null && manager != null)
            pauseButton = manager.pauseButton;

        if (howToPlayButton == null && manager != null)
            howToPlayButton = manager.howToPlayOpenButton;

        if (checkButton == null && manager != null)
            checkButton = manager.checkButton;

        if (nextButton == null && manager != null)
            nextButton = manager.nextButton;
    }

    private RectTransform FindRect(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.GetComponent<RectTransform>() : null;
    }

    private RectTransform FindRectContains(string partialName)
    {
        RectTransform[] rects = Resources.FindObjectsOfTypeAll<RectTransform>();

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];

            if (rect == null || rect.gameObject == null)
                continue;

            if (!rect.gameObject.scene.IsValid())
                continue;

            if (rect.name.Contains(partialName))
                return rect;
        }

        return null;
    }

    private void CaptureBaseManagerSizes()
    {
        if (capturedBaseSizes || manager == null)
            return;

        baseTextCardSize = manager.textCardSize;
        baseTextSlotSize = manager.textSlotSize;
        baseImageCardSize = manager.imageCardSize;
        baseImageSlotSize = manager.imageSlotSize;
        baseMinTextSlotSize = manager.minTextSlotSize;
        baseMinImageSlotSize = manager.minImageSlotSize;

        capturedBaseSizes = true;
    }

    private void ApplySafeArea()
    {
        if (!applySafeAreaToRoot || root == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safe = Screen.safeArea;

        Vector2 anchorMin = safe.position;
        Vector2 anchorMax = safe.position + safe.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        root.anchorMin = anchorMin;
        root.anchorMax = anchorMax;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
    }

    private void ApplyRootLayout(OrderSortResponsiveLayoutPreset preset)
    {
        if (rootLayout == null)
            return;

        rootLayout.padding = new RectOffset(
            preset.rootPaddingHorizontal,
            preset.rootPaddingHorizontal,
            preset.rootPaddingVertical,
            preset.rootPaddingVertical);

        rootLayout.spacing = preset.rootSpacing;
    }

    private void ApplyHeights(OrderSortResponsiveLayoutPreset preset)
    {
        SetPreferredHeight(header, preset.headerHeight);
        SetPreferredHeight(titleBar, preset.titleBarHeight);
        SetPreferredHeight(statusBar, preset.statusBarHeight);
        SetPreferredHeight(actionBar, preset.actionBarHeight);
        SetPreferredHeight(feedbackArea, preset.feedbackHeight);

        if (gameplayRow != null)
        {
            SetPreferredHeight(gameplayRow, preset.verticalGameplayRowHeight);
        }
        else
        {
            SetPreferredHeight(slotsArea, preset.horizontalSlotsAreaHeight);
            SetPreferredHeight(basketWrapper, preset.horizontalBasketAreaHeight);
        }
    }

    private void ApplySpacings(OrderSortResponsiveLayoutPreset preset)
    {
        if (headerLayout != null)
            headerLayout.spacing = preset.headerSpacing;

        if (titleBarLayout != null)
            titleBarLayout.spacing = preset.titleBarSpacing;

        if (statusBarLayout != null)
            statusBarLayout.spacing = preset.statusBarSpacing;

        if (actionBarLayout != null)
            actionBarLayout.spacing = preset.actionBarSpacing;

        if (slotsParentLayout != null)
            slotsParentLayout.spacing = preset.slotSpacing;
    }

    private void ApplyButtonSizes(OrderSortResponsiveLayoutPreset preset)
    {
        SetPreferredSize(pauseButton != null ? pauseButton.GetComponent<RectTransform>() : null, preset.sideButtonWidth, preset.sideButtonHeight);
        SetPreferredSize(howToPlayButton != null ? howToPlayButton.GetComponent<RectTransform>() : null, preset.sideButtonWidth, preset.sideButtonHeight);
        SetPreferredSize(checkButton != null ? checkButton.GetComponent<RectTransform>() : null, preset.checkButtonWidth, preset.actionButtonHeight);
        SetPreferredSize(nextButton != null ? nextButton.GetComponent<RectTransform>() : null, preset.nextButtonWidth, preset.actionButtonHeight);
    }

    private void ApplyManagerSizeScales(OrderSortResponsiveLayoutPreset preset)
    {
        if (manager == null || !capturedBaseSizes)
            return;

        manager.textCardSize = Scale(baseTextCardSize, preset.cardScale);
        manager.imageCardSize = Scale(baseImageCardSize, preset.cardScale);
        manager.textSlotSize = Scale(baseTextSlotSize, preset.preferredSlotScale);
        manager.imageSlotSize = Scale(baseImageSlotSize, preset.preferredSlotScale);
        manager.minTextSlotSize = Scale(baseMinTextSlotSize, preset.minSlotScale);
        manager.minImageSlotSize = Scale(baseMinImageSlotSize, preset.minSlotScale);

        if (bankGridLayout != null)
        {
            Vector2 activeCardSize = manager.contentMode == OrderSortContentMode.ImageOnly
                ? manager.imageCardSize
                : manager.textCardSize;

            bankGridLayout.cellSize = activeCardSize;
            bankGridLayout.spacing = new Vector2(Mathf.Max(8f, 14f * preset.cardScale), Mathf.Max(8f, 14f * preset.cardScale));
        }
    }

    private void RefreshExistingCardsAndSlots(OrderSortResponsiveLayoutPreset preset)
    {
        if (!refreshExistingRuntimeCardsAndSlots || manager == null)
            return;

        Vector2 activeCardSize = manager.contentMode == OrderSortContentMode.ImageOnly
            ? manager.imageCardSize
            : manager.textCardSize;

        Vector2 activeMinSlotSize = manager.contentMode == OrderSortContentMode.ImageOnly
            ? manager.minImageSlotSize
            : manager.minTextSlotSize;

        OrderSortDragItem[] cards = Resources.FindObjectsOfTypeAll<OrderSortDragItem>();
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null || cards[i].name.Contains("SceneTemplate") || !cards[i].gameObject.scene.IsValid())
                continue;

            RectTransform rect = cards[i].GetComponent<RectTransform>();
            SetRuntimeSize(rect, activeCardSize, false);
        }

        OrderSortDropSlot[] slots = Resources.FindObjectsOfTypeAll<OrderSortDropSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].name.Contains("SceneTemplate") || !slots[i].gameObject.scene.IsValid())
                continue;

            RectTransform rect = slots[i].GetComponent<RectTransform>();
            SetRuntimeSize(rect, activeMinSlotSize, true);
        }
    }

    private Vector2 Scale(Vector2 source, float scale)
    {
        return new Vector2(
            Mathf.Max(1f, source.x * scale),
            Mathf.Max(1f, source.y * scale));
    }

    private void SetPreferredHeight(RectTransform rect, float height)
    {
        if (rect == null || height <= 0f)
            return;

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();

        layout.preferredHeight = height;
    }

    private void SetPreferredSize(RectTransform rect, float width, float height)
    {
        if (rect == null)
            return;

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();

        if (width > 0f)
            layout.preferredWidth = width;

        if (height > 0f)
            layout.preferredHeight = height;

        rect.sizeDelta = new Vector2(width > 0f ? width : rect.sizeDelta.x, height > 0f ? height : rect.sizeDelta.y);
    }

    private void SetRuntimeSize(RectTransform rect, Vector2 size, bool flexible)
    {
        if (rect == null)
            return;

        rect.sizeDelta = size;

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();

        layout.minWidth = size.x;
        layout.minHeight = size.y;
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.flexibleWidth = flexible ? 1f : 0f;
        layout.flexibleHeight = flexible ? 1f : 0f;
    }
}
