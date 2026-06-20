#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GridAdventureSceneBuilder
{
    private const string RootFolder = "Assets/GridAdventure";
    private const string LevelFolder = "Assets/GridAdventure/Levels";
    private const string ArtFolder = "Assets/GridAdventure/Art/Placeholders";

    [MenuItem("Tools/Grid Adventure/Create Rough Working UI")]
    public static void CreateRoughWorkingUI()
    {
        EnsureFolders();
        GridAdventureLevelData sampleLevel = CreateOrLoadSampleLevel();

        Canvas canvas = CreateCanvas();
        GridAdventureCanvasResizeRefresher resizeRefresher = canvas.gameObject.AddComponent<GridAdventureCanvasResizeRefresher>();
        resizeRefresher.watchRoot = canvas.transform as RectTransform;
        resizeRefresher.refreshPasses = 8;
        resizeRefresher.refreshEveryFrame = true;

        GridAdventureManager manager = canvas.gameObject.AddComponent<GridAdventureManager>();
        GridAdventureAudioManager audioManager = canvas.gameObject.AddComponent<GridAdventureAudioManager>();
        manager.audioManager = audioManager;
        manager.rootCanvas = canvas;
        manager.graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        manager.levelData = sampleLevel;
        manager.loadingDuration = 2f;
        manager.useBloomRewardSystem = true;
        manager.applyFontsOnStart = true;

        RectTransform background = AddStretchPanel("Cozy Pastel Background", canvas.transform, new Color(0.97f, 0.94f, 0.88f, 1f));
        background.SetAsFirstSibling();
        background.GetComponent<Image>().raycastTarget = false;

        RectTransform safeArea = AddStretchRect("SafeAreaRoot", canvas.transform);

        RectTransform topBar = AddPanel("TopBar", safeArea, new Color(0.94f, 0.97f, 1f, 0.96f));
        HorizontalLayoutGroup topLayout = AddHorizontalLayout(topBar, new RectOffset(14, 14, 10, 10), 12f, TextAnchor.MiddleCenter);
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = true;

        CreateTopBar(topBar, manager);

        RectTransform centerContent = AddStretchRect("CenterContent", safeArea);

        RectTransform gridPanel = CreateGridPanel(centerContent, manager);
        RectTransform basketPanel = CreateBasketPanel(centerContent, manager);

        GridAdventureCenterSquareLayout centerSquareLayout = centerContent.gameObject.AddComponent<GridAdventureCenterSquareLayout>();
        centerSquareLayout.leftSquarePanel = gridPanel;
        centerSquareLayout.rightSquarePanel = basketPanel;
        centerSquareLayout.layoutMode = GridAdventureCenterSquareLayout.LayoutMode.Auto;
        centerSquareLayout.spacing = 28f;
        centerSquareLayout.minSquareSize = 180f;
        centerSquareLayout.maxSquareSize = 820f;
        centerSquareLayout.horizontalAspectThreshold = 1.2f;
        centerSquareLayout.sizeMultiplier = 1f;

        CreateGameplayInstructionOverlay(centerContent, manager);

        RectTransform clueBanner = CreateClueBanner(safeArea, manager);

        GridAdventureMainScreenLayout mainScreenLayout = safeArea.gameObject.AddComponent<GridAdventureMainScreenLayout>();
        mainScreenLayout.topBar = topBar;
        mainScreenLayout.centerContent = centerContent;
        mainScreenLayout.clueBanner = clueBanner;
        mainScreenLayout.paddingLeft = 30;
        mainScreenLayout.paddingRight = 30;
        mainScreenLayout.paddingTop = 18;
        mainScreenLayout.paddingBottom = 22;
        mainScreenLayout.verticalSpacing = 12f;
        mainScreenLayout.topBarHeight = 100f;
        mainScreenLayout.clueBannerHeight = 96f;
        mainScreenLayout.allowHeightCompressionOnTinyScreens = true;
        mainScreenLayout.minimumCenterHeightRatio = 0.58f;
        resizeRefresher.ForceRefreshNow();

        RectTransform dragLayer = AddStretchRect("DragLayer", canvas.transform);
        dragLayer.SetAsLastSibling();
        manager.dragLayer = dragLayer;

        CreateLoadingOverlay(canvas.transform, manager);
        CreatePauseOverlay(canvas.transform, manager);
        CreateResultOverlay(canvas.transform, manager);
        CreateHowToPlayOverlay(canvas.transform, manager);

        EnsureEventSystem();
        LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.transform as RectTransform);

        Selection.activeGameObject = canvas.gameObject;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Grid Adventure production responsive UI created. Assign final art/audio, then press Play.");
    }

    [MenuItem("Tools/Grid Adventure/Repair Current Scene Responsive Layout")]
    public static void RepairCurrentSceneResponsiveLayout()
    {
        RectTransform canvasRect = FindSceneRect("GridAdventure_Canvas");
        if (canvasRect == null)
            canvasRect = Object.FindObjectOfType<Canvas>() != null ? Object.FindObjectOfType<Canvas>().transform as RectTransform : null;

        RectTransform safeArea = FindSceneRect("SafeAreaRoot");
        RectTransform topBar = FindSceneRect("TopBar");
        RectTransform centerContent = FindSceneRect("CenterContent");
        RectTransform clueBanner = FindSceneRect("ClueBanner");
        RectTransform mainGridPanel = FindSceneRect("MainGridPanel");
        RectTransform itemBasketPanel = FindSceneRect("ItemBasketPanel");
        RectTransform gridShell = FindSceneRect("GridShell");
        RectTransform columnHeaderRow = FindSceneRect("ColumnHeaderRow");
        RectTransform bodyRow = FindSceneRect("GridBodyRow");
        RectTransform rowHeaderColumn = FindSceneRect("RowHeaderColumn");
        RectTransform gridRoot = FindSceneRect("GridRoot");

        if (safeArea != null && topBar != null && centerContent != null && clueBanner != null)
        {
            GridAdventureMainScreenLayout mainLayout = safeArea.GetComponent<GridAdventureMainScreenLayout>();
            if (mainLayout == null) mainLayout = safeArea.gameObject.AddComponent<GridAdventureMainScreenLayout>();
            mainLayout.topBar = topBar;
            mainLayout.centerContent = centerContent;
            mainLayout.clueBanner = clueBanner;
            mainLayout.topBarHeight = 100f;
            mainLayout.clueBannerHeight = 96f;
            mainLayout.applyEveryFrame = true;
        }

        if (centerContent != null && mainGridPanel != null && itemBasketPanel != null)
        {
            GridAdventureCenterSquareLayout centerLayout = centerContent.GetComponent<GridAdventureCenterSquareLayout>();
            if (centerLayout == null) centerLayout = centerContent.gameObject.AddComponent<GridAdventureCenterSquareLayout>();
            centerLayout.leftSquarePanel = mainGridPanel;
            centerLayout.rightSquarePanel = itemBasketPanel;
            centerLayout.applyEveryFrame = true;
            centerLayout.disableLayoutDriversOnPanels = true;
        }

        if (gridShell != null && columnHeaderRow != null && bodyRow != null && rowHeaderColumn != null && gridRoot != null)
        {
            GridAdventureCoordinateGridLayout coordinateLayout = gridShell.GetComponent<GridAdventureCoordinateGridLayout>();
            if (coordinateLayout == null) coordinateLayout = gridShell.gameObject.AddComponent<GridAdventureCoordinateGridLayout>();
            coordinateLayout.columnHeaderRow = columnHeaderRow;
            coordinateLayout.bodyRow = bodyRow;
            coordinateLayout.rowHeaderColumn = rowHeaderColumn;
            coordinateLayout.gridRoot = gridRoot;
            coordinateLayout.columns = 3;
            coordinateLayout.rows = 3;
            coordinateLayout.refreshEveryFrame = true;
            coordinateLayout.forceGridLayoutRefresh = true;
            coordinateLayout.ForceRefresh();
        }

        if (canvasRect != null)
        {
            GridAdventureCanvasResizeRefresher refresher = canvasRect.GetComponent<GridAdventureCanvasResizeRefresher>();
            if (refresher == null) refresher = canvasRect.gameObject.AddComponent<GridAdventureCanvasResizeRefresher>();
            refresher.watchRoot = canvasRect;
            refresher.refreshEveryFrame = true;
            refresher.refreshPasses = 10;
            refresher.ForceRefreshNow();
        }

        RectTransform rebuildTarget = canvasRect != null ? canvasRect : safeArea;
        if (rebuildTarget != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rebuildTarget);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Grid Adventure responsive layout repaired. The coordinate grid is now controlled by GridAdventureCoordinateGridLayout.");
    }

    private static RectTransform FindSceneRect(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform as RectTransform : null;
    }

    private static void CreateTopBar(RectTransform parent, GridAdventureManager manager)
    {
        RectTransform timerCard = AddPanel("TimerCard", parent, new Color(1f, 0.98f, 0.93f, 1f));
        AddLayout(timerCard, minWidth: 170f, preferredWidth: 250f, flexibleHeight: 1f);
        VerticalLayoutGroup timerLayout = AddVerticalLayout(timerCard, new RectOffset(14, 14, 8, 8), 4f, TextAnchor.MiddleLeft);
        timerLayout.childForceExpandWidth = true;
        timerLayout.childForceExpandHeight = false;

        manager.levelText = AddText("Static Timer Label", timerCard, "TIME REMAINING", 17, TextAlignmentOptions.Left, true);
        manager.levelText.fontStyle = FontStyles.Bold;
        SetTextFontRole(manager.levelText, GridAdventureFontRole.Primary);
        AddLayout(manager.levelText.rectTransform, preferredHeight: 22f, flexibleWidth: 1f);

        RectTransform timerRow = AddRect("TimerRow", timerCard);
        AddLayout(timerRow, preferredHeight: 26f, flexibleWidth: 1f);
        HorizontalLayoutGroup timerRowLayout = AddHorizontalLayout(timerRow, new RectOffset(0, 0, 0, 0), 0f, TextAnchor.MiddleCenter);
        timerRowLayout.childForceExpandWidth = true;
        timerRowLayout.childForceExpandHeight = true;

        manager.timerSlider = AddSlider("Timer Slider", timerRow, new Color(0.82f, 0.83f, 0.84f, 0.7f), new Color(0.55f, 0.86f, 0.68f, 1f));
        AddLayout(manager.timerSlider.transform as RectTransform, flexibleWidth: 1f, flexibleHeight: 1f);

        RectTransform gameNameCard = AddPanel("GameNameCard", parent, new Color(0.97f, 0.95f, 1f, 1f));
        AddLayout(gameNameCard, minWidth: 120f, flexibleWidth: 1f, flexibleHeight: 1f);
        HorizontalLayoutGroup gameNameLayout = AddHorizontalLayout(gameNameCard, new RectOffset(20, 20, 6, 6), 0f, TextAnchor.MiddleCenter);
        gameNameLayout.childForceExpandWidth = true;
        gameNameLayout.childForceExpandHeight = true;

        manager.topBarGameNameText = AddText("Top Bar Game Name", gameNameCard, manager.gameName, 32, TextAlignmentOptions.Center, true);
        manager.topBarGameNameText.fontStyle = FontStyles.Bold;
        SetTextFontRole(manager.topBarGameNameText, GridAdventureFontRole.Primary);
        AddLayout(manager.topBarGameNameText.rectTransform, flexibleWidth: 1f, flexibleHeight: 1f);

        RectTransform systemCard = AddPanel("TopControlsCard", parent, new Color(0.93f, 0.91f, 1f, 1f));
        AddLayout(systemCard, minWidth: 96f, preferredWidth: 112f, flexibleHeight: 1f);
        HorizontalLayoutGroup systemLayout = AddHorizontalLayout(systemCard, new RectOffset(8, 8, 14, 14), 8f, TextAnchor.MiddleCenter);
        systemLayout.childForceExpandWidth = false;
        systemLayout.childForceExpandHeight = false;

        manager.pauseButton = AddButton("Pause Button", systemCard, "II", new Color(0.84f, 0.87f, 1f, 1f));
        AddLayout(manager.pauseButton.transform as RectTransform, preferredWidth: 38f, preferredHeight: 38f);
        SetButtonLabelFontSize(manager.pauseButton, 18f);

        manager.helpButton = AddButton("Help Button", systemCard, "?", new Color(1f, 0.87f, 0.47f, 1f));
        AddLayout(manager.helpButton.transform as RectTransform, preferredWidth: 38f, preferredHeight: 38f);
        SetButtonLabelFontSize(manager.helpButton, 20f);
    }

    private static RectTransform CreateGridPanel(RectTransform parent, GridAdventureManager manager)
    {
        RectTransform panel = AddPanel("MainGridPanel", parent, new Color(1f, 0.98f, 0.92f, 0.97f));

        RectTransform gridShell = AddStretchPanel("GridShell", panel, new Color(0.95f, 0.93f, 0.86f, 0.72f));
        gridShell.offsetMin = new Vector2(22f, 22f);
        gridShell.offsetMax = new Vector2(-22f, -22f);
        VerticalLayoutGroup shellLayout = AddVerticalLayout(gridShell, new RectOffset(16, 18, 16, 18), 8f, TextAnchor.MiddleCenter);
        shellLayout.childForceExpandWidth = true;
        shellLayout.childForceExpandHeight = false;

        RectTransform columnRow = AddRect("ColumnHeaderRow", gridShell);
        AddLayout(columnRow, preferredHeight: 34f, flexibleWidth: 1f);
        HorizontalLayoutGroup columnLayout = AddHorizontalLayout(columnRow, new RectOffset(0, 0, 0, 0), 8f, TextAnchor.MiddleCenter);
        columnLayout.childForceExpandWidth = true;
        columnLayout.childForceExpandHeight = true;

        RectTransform cornerSpacer = AddRect("CornerSpacer", columnRow);
        AddLayout(cornerSpacer, preferredWidth: 42f, flexibleHeight: 1f);

        string[] columns = { "A", "B", "C" };
        for (int i = 0; i < columns.Length; i++)
        {
            TextMeshProUGUI header = AddText("Column Header " + columns[i], columnRow, columns[i], 25, TextAlignmentOptions.Center, true);
            header.fontStyle = FontStyles.Bold;
            SetTextFontRole(header, GridAdventureFontRole.Primary);
            AddLayout(header.rectTransform, flexibleWidth: 1f, flexibleHeight: 1f);
        }

        RectTransform bodyRow = AddRect("GridBodyRow", gridShell);
        AddLayout(bodyRow, flexibleHeight: 1f, flexibleWidth: 1f);
        HorizontalLayoutGroup bodyLayout = AddHorizontalLayout(bodyRow, new RectOffset(0, 0, 0, 0), 8f, TextAnchor.MiddleCenter);
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;

        RectTransform rowHeaderColumn = AddRect("RowHeaderColumn", bodyRow);
        AddLayout(rowHeaderColumn, preferredWidth: 42f, flexibleHeight: 1f);
        VerticalLayoutGroup rowLayout = AddVerticalLayout(rowHeaderColumn, new RectOffset(0, 0, 8, 8), 14f, TextAnchor.MiddleCenter);
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;

        for (int row = 1; row <= 3; row++)
        {
            TextMeshProUGUI rowHeader = AddText("Row Header " + row, rowHeaderColumn, row.ToString(), 25, TextAlignmentOptions.Right, true);
            rowHeader.fontStyle = FontStyles.Bold;
            SetTextFontRole(rowHeader, GridAdventureFontRole.Primary);
            AddLayout(rowHeader.rectTransform, flexibleHeight: 1f, flexibleWidth: 1f);
        }

        RectTransform gridRoot = AddRect("GridRoot", bodyRow);
        AddLayout(gridRoot, flexibleHeight: 1f, flexibleWidth: 1f);
        GridLayoutGroup gridLayout = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.spacing = new Vector2(14f, 14f);
        gridLayout.padding = new RectOffset(8, 8, 8, 8);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        GridAdventureResponsiveGrid responsiveGrid = gridRoot.gameObject.AddComponent<GridAdventureResponsiveGrid>();
        responsiveGrid.columns = 3;
        responsiveGrid.rows = 3;
        responsiveGrid.minCellSize = new Vector2(86f, 86f);
        responsiveGrid.maxCellSize = new Vector2(260f, 260f);

        for (int row = 1; row <= 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                string coordinate = string.Format("{0}{1}", (char)('A' + col), row);
                CreateCell(gridRoot, manager, coordinate);
            }
        }

        GridAdventureCoordinateGridLayout coordinateLayout = gridShell.gameObject.AddComponent<GridAdventureCoordinateGridLayout>();
        coordinateLayout.columnHeaderRow = columnRow;
        coordinateLayout.bodyRow = bodyRow;
        coordinateLayout.rowHeaderColumn = rowHeaderColumn;
        coordinateLayout.gridRoot = gridRoot;
        coordinateLayout.columns = 3;
        coordinateLayout.rows = 3;
        coordinateLayout.refreshEveryFrame = true;
        coordinateLayout.forceGridLayoutRefresh = true;
        coordinateLayout.ApplyLayoutNow();

        manager.gridRoot = gridRoot;
        return panel;
    }

    private static void CreateCell(RectTransform parent, GridAdventureManager manager, string coordinate)
    {
        RectTransform cellRect = AddPanel("Cell " + coordinate, parent, new Color(0.94f, 0.94f, 0.88f, 1f));
        GridAdventureCell cell = cellRect.gameObject.AddComponent<GridAdventureCell>();
        cell.coordinate = coordinate;
        cell.backgroundImage = cellRect.GetComponent<Image>();
        cell.normalColor = new Color(0.94f, 0.94f, 0.88f, 1f);
        cell.activeColor = new Color(0.78f, 0.91f, 1f, 1f);
        cell.completedColor = new Color(0.78f, 0.95f, 0.78f, 1f);

        RectTransform outline = AddStretchPanel("Active Outline", cellRect, new Color(0.45f, 0.76f, 1f, 0f));
        outline.offsetMin = new Vector2(-5f, -5f);
        outline.offsetMax = new Vector2(5f, 5f);
        outline.GetComponent<Image>().raycastTarget = false;
        cell.activeOutlineImage = outline.GetComponent<Image>();

        RectTransform placedRoot = AddStretchRect("Placed Item Root", cellRect);
        placedRoot.offsetMin = new Vector2(12f, 12f);
        placedRoot.offsetMax = new Vector2(-12f, -12f);
        cell.placedItemRoot = placedRoot;

        cell.showCoordinateLabel = false;
        cell.coordinateLabel = null;
        cell.Init(manager);
    }

    private static RectTransform CreateBasketPanel(RectTransform parent, GridAdventureManager manager)
    {
        RectTransform panel = AddPanel("ItemBasketPanel", parent, new Color(0.93f, 0.98f, 0.94f, 0.97f));

        RectTransform basketRoot = AddStretchRect("BasketRoot", panel);
        basketRoot.offsetMin = new Vector2(22f, 22f);
        basketRoot.offsetMax = new Vector2(-22f, -22f);

        RectTransform basketBackground = AddStretchPanel("BasketBackgroundImage", basketRoot, new Color(1f, 1f, 1f, 0.45f));
        LayoutElement backgroundLayout = basketBackground.gameObject.AddComponent<LayoutElement>();
        backgroundLayout.ignoreLayout = true;
        Image basketBackgroundImage = basketBackground.GetComponent<Image>();
        basketBackgroundImage.raycastTarget = false;
        manager.basketBackgroundImage = basketBackgroundImage;
        basketBackground.SetAsFirstSibling();

        GridLayoutGroup layoutGroup = basketRoot.gameObject.AddComponent<GridLayoutGroup>();
        layoutGroup.spacing = new Vector2(14f, 14f);
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childAlignment = TextAnchor.MiddleCenter;

        GridAdventureResponsiveGrid responsiveGrid = basketRoot.gameObject.AddComponent<GridAdventureResponsiveGrid>();
        responsiveGrid.columns = 3;
        responsiveGrid.rows = 3;
        responsiveGrid.autoFitToActiveChildren = true;
        responsiveGrid.autoFitMaxColumns = 3;
        responsiveGrid.autoFitMaxRows = 3;
        responsiveGrid.minCellSize = new Vector2(76f, 76f);
        responsiveGrid.maxCellSize = new Vector2(260f, 260f);

        manager.basketRoot = basketRoot;
        manager.itemCardTemplate = CreateItemCardTemplate(basketRoot);
        return panel;
    }

    private static void CreateGameplayInstructionOverlay(RectTransform parent, GridAdventureManager manager)
    {
        RectTransform root = AddRect("GameplayInstructionOverlayRoot", parent);
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(780f, 78f);
        root.anchoredPosition = new Vector2(0f, -14f);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        RectTransform card = AddPanel("InstructionBreathCard", root, new Color(1f, 0.93f, 0.70f, 0.96f));
        card.anchorMin = Vector2.zero;
        card.anchorMax = Vector2.one;
        card.offsetMin = Vector2.zero;
        card.offsetMax = Vector2.zero;
        Image cardImage = card.GetComponent<Image>();
        cardImage.raycastTarget = false;

        TextMeshProUGUI instruction = AddText("Instruction Text", card, "Tap the glowing cell, then drag the matching image.", 26, TextAlignmentOptions.Center, true);
        instruction.fontStyle = FontStyles.Bold;
        SetTextFontRole(instruction, GridAdventureFontRole.Primary);
        RectTransform textRect = instruction.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 10f);
        textRect.offsetMax = new Vector2(-24f, -10f);

        root.gameObject.SetActive(false);
        manager.gameplayInstructionOverlayRoot = root.gameObject;
        manager.gameplayInstructionMotionRoot = card;
        manager.gameplayInstructionText = instruction;
    }

    private static GridAdventureItemCard CreateItemCardTemplate(RectTransform parent)
    {
        RectTransform cardRect = AddPanel("ItemCardTemplate", parent, new Color(1f, 0.98f, 0.9f, 1f));
        cardRect.gameObject.AddComponent<CanvasGroup>();

        LayoutElement templateLayout = cardRect.gameObject.AddComponent<LayoutElement>();
        templateLayout.ignoreLayout = true;

        GridAdventureItemCard card = cardRect.gameObject.AddComponent<GridAdventureItemCard>();
        card.backgroundImage = cardRect.GetComponent<Image>();
        card.displayMode = GridAdventureItemDisplayMode.ImageAndLabel;
        card.MarkAsTemplate(true);

        VerticalLayoutGroup cardLayout = AddVerticalLayout(cardRect, new RectOffset(8, 8, 8, 8), 4f, TextAnchor.MiddleCenter);
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        RectTransform iconFrame = AddPanel("Icon Frame", cardRect, Color.white);
        AddLayout(iconFrame, flexibleHeight: 1f, flexibleWidth: 1f);
        Image icon = iconFrame.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        card.iconImage = icon;

        TextMeshProUGUI label = AddText("Label", cardRect, "Item", 15, TextAlignmentOptions.Center, true);
        AddLayout(label.rectTransform, preferredHeight: 26f, flexibleWidth: 1f);
        card.labelText = label;

        cardRect.gameObject.SetActive(false);
        return card;
    }

    private static RectTransform CreateClueBanner(RectTransform parent, GridAdventureManager manager)
    {
        RectTransform panel = AddPanel("ClueBanner", parent, new Color(1f, 0.9f, 0.76f, 0.98f));

        RectTransform contentRoot = AddStretchRect("ClueContentRoot", panel);
        contentRoot.offsetMin = new Vector2(28f, 14f);
        contentRoot.offsetMax = new Vector2(-28f, -14f);

        manager.clueText = AddText("Clue Text", contentRoot, "CLUE FOR A1: Find the matching image.", 27, TextAlignmentOptions.Left, true);
        manager.clueText.fontStyle = FontStyles.Bold;
        SetTextFontRole(manager.clueText, GridAdventureFontRole.Secondary);
        RectTransform textRect = manager.clueText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.pivot = new Vector2(0.5f, 0.5f);

        manager.clueBanner = panel;
        manager.clueMotionRoot = contentRoot;
        return panel;
    }

    private static void CreateLoadingOverlay(Transform canvasRoot, GridAdventureManager manager)
    {
        RectTransform root = CreateOverlayRoot("LoadingOverlayRoot", canvasRoot);
        RectTransform card = AddPanel("MainCard", root, new Color(0.91f, 0.96f, 1f, 0.99f));
        SetCenterCard(card, new Vector2(660f, 360f));
        VerticalLayoutGroup layout = AddVerticalLayout(card, new RectOffset(46, 46, 42, 42), 24f, TextAnchor.MiddleCenter);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        manager.loadingGameNameText = AddText("Game Name Text", card, "Grid Adventure", 48, TextAlignmentOptions.Center, true);
        manager.loadingGameNameText.fontStyle = FontStyles.Bold;
        SetTextFontRole(manager.loadingGameNameText, GridAdventureFontRole.Primary);
        AddLayout(manager.loadingGameNameText.rectTransform, preferredHeight: 82f, flexibleWidth: 1f);

        manager.loadingSlider = AddSlider("Loading Slider", card, new Color(0.82f, 0.83f, 0.84f, 0.7f), new Color(0.76f, 0.87f, 1f, 1f));
        AddLayout(manager.loadingSlider.transform as RectTransform, preferredHeight: 44f, flexibleWidth: 1f);

        TextMeshProUGUI label = AddText("Loading Label", card, "Loading...", 25, TextAlignmentOptions.Center, true);
        AddLayout(label.rectTransform, preferredHeight: 46f, flexibleWidth: 1f);

        root.gameObject.SetActive(false);
        manager.loadingOverlayRoot = root.gameObject;
        manager.loadingMainCard = card;
    }

    private static void CreatePauseOverlay(Transform canvasRoot, GridAdventureManager manager)
    {
        RectTransform root = CreateOverlayRoot("PauseOverlayRoot", canvasRoot);
        RectTransform card = AddPanel("MainCard", root, new Color(0.87f, 0.92f, 1f, 0.99f));
        SetCenterCard(card, new Vector2(520f, 470f));
        VerticalLayoutGroup layout = AddVerticalLayout(card, new RectOffset(42, 42, 34, 34), 18f, TextAnchor.MiddleCenter);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = AddText("Pause Title", card, "Paused", 44, TextAlignmentOptions.Center, true);
        title.fontStyle = FontStyles.Bold;
        SetTextFontRole(title, GridAdventureFontRole.Primary);
        AddLayout(title.rectTransform, preferredHeight: 72f, flexibleWidth: 1f);

        manager.resumeButton = AddButton("Resume Button", card, "Resume", new Color(0.84f, 1f, 0.91f, 1f));
        AddLayout(manager.resumeButton.transform as RectTransform, preferredHeight: 64f, flexibleWidth: 1f);

        manager.pauseHowToPlayButton = AddButton("How To Play Button", card, "How To Play", new Color(0.88f, 0.91f, 1f, 1f));
        AddLayout(manager.pauseHowToPlayButton.transform as RectTransform, preferredHeight: 64f, flexibleWidth: 1f);

        manager.restartButton = AddButton("Restart Button", card, "Restart", new Color(1f, 0.89f, 0.72f, 1f));
        AddLayout(manager.restartButton.transform as RectTransform, preferredHeight: 64f, flexibleWidth: 1f);

        root.gameObject.SetActive(false);
        manager.pauseOverlayRoot = root.gameObject;
        manager.pauseMainCard = card;
    }

    private static void CreateResultOverlay(Transform canvasRoot, GridAdventureManager manager)
    {
        RectTransform root = CreateOverlayRoot("ResultOverlayRoot", canvasRoot);
        RectTransform card = AddPanel("MainCard", root, new Color(0.91f, 1f, 0.92f, 0.99f));
        SetCenterCard(card, new Vector2(600f, 540f));
        VerticalLayoutGroup layout = AddVerticalLayout(card, new RectOffset(44, 44, 36, 36), 20f, TextAnchor.MiddleCenter);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        manager.resultTitleText = AddText("Result Title", card, "Great Job!", 46, TextAlignmentOptions.Center, true);
        manager.resultTitleText.fontStyle = FontStyles.Bold;
        SetTextFontRole(manager.resultTitleText, GridAdventureFontRole.Primary);
        AddLayout(manager.resultTitleText.rectTransform, preferredHeight: 72f, flexibleWidth: 1f);

        manager.resultScoreText = AddText("Result Score", card, "Score: 0", 29, TextAlignmentOptions.Center, true);
        AddLayout(manager.resultScoreText.rectTransform, preferredHeight: 120f, flexibleWidth: 1f);

        manager.resultContinueButton = AddButton("Result Continue Button", card, "Continue", new Color(0.84f, 1f, 0.91f, 1f));
        AddLayout(manager.resultContinueButton.transform as RectTransform, preferredHeight: 72f, flexibleWidth: 1f);

        manager.resultRestartButton = AddButton("Result Restart Button", card, "Play Again", new Color(1f, 0.89f, 0.72f, 1f));
        AddLayout(manager.resultRestartButton.transform as RectTransform, preferredHeight: 72f, flexibleWidth: 1f);

        root.gameObject.SetActive(false);
        manager.resultOverlayRoot = root.gameObject;
        manager.resultMainCard = card;
    }

    private static void CreateHowToPlayOverlay(Transform canvasRoot, GridAdventureManager manager)
    {
        RectTransform root = CreateOverlayRoot("HowToPlayOverlayRoot", canvasRoot);
        RectTransform card = AddPanel("MainCard", root, new Color(1f, 0.96f, 0.86f, 0.99f));
        SetCenterCard(card, new Vector2(980f, 760f));
        VerticalLayoutGroup layout = AddVerticalLayout(card, new RectOffset(34, 34, 28, 28), 16f, TextAnchor.MiddleCenter);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI title = AddText("Guide Title", card, "How To Play", 42, TextAlignmentOptions.Center, true);
        title.fontStyle = FontStyles.Bold;
        SetTextFontRole(title, GridAdventureFontRole.Primary);
        AddLayout(title.rectTransform, preferredHeight: 60f, flexibleWidth: 1f);

        RectTransform imageFrame = AddPanel("GuideImageFrame", card, Color.white);
        AddLayout(imageFrame, flexibleHeight: 1f, flexibleWidth: 1f);
        manager.guideImage = imageFrame.GetComponent<Image>();
        manager.guideImage.preserveAspect = true;

        RectTransform footer = AddRect("GuideFooter", card);
        AddLayout(footer, preferredHeight: 86f, flexibleWidth: 1f);
        HorizontalLayoutGroup footerLayout = AddHorizontalLayout(footer, new RectOffset(0, 0, 4, 4), 14f, TextAnchor.MiddleCenter);
        footerLayout.childForceExpandWidth = false;
        footerLayout.childForceExpandHeight = true;

        manager.guidePrevButton = AddButton("Previous Button", footer, "Prev", new Color(0.88f, 0.91f, 1f, 1f));
        AddLayout(manager.guidePrevButton.transform as RectTransform, preferredWidth: 160f, flexibleHeight: 1f);

        manager.guideCounterText = AddText("Guide Counter", footer, "1 / 3", 24, TextAlignmentOptions.Center, true);
        AddLayout(manager.guideCounterText.rectTransform, preferredWidth: 170f, flexibleHeight: 1f);

        manager.guideNextButton = AddButton("Next Button", footer, "Next", new Color(0.88f, 0.91f, 1f, 1f));
        AddLayout(manager.guideNextButton.transform as RectTransform, preferredWidth: 160f, flexibleHeight: 1f);

        manager.guideStartButton = AddButton("Start Button", footer, "Start", new Color(0.84f, 1f, 0.91f, 1f));
        AddLayout(manager.guideStartButton.transform as RectTransform, preferredWidth: 190f, flexibleHeight: 1f);

        manager.guideImages = CreateGuideSprites();
        if (manager.guideImages.Count > 0)
            manager.guideImage.sprite = manager.guideImages[0];

        root.gameObject.SetActive(false);
        manager.howToPlayOverlayRoot = root.gameObject;
        manager.howToPlayMainCard = card;
    }

    private static RectTransform CreateOverlayRoot(string name, Transform parent)
    {
        RectTransform root = AddStretchPanel(name, parent, new Color(0.18f, 0.18f, 0.22f, 0.55f));
        root.SetAsLastSibling();
        return root;
    }

    private static void SetCenterCard(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    private static Image[] CreateStars(RectTransform parent)
    {
        List<Image> fills = new List<Image>();
        for (int i = 0; i < 3; i++)
        {
            RectTransform starBack = AddPanel("Star Back " + (i + 1), parent, new Color(0.75f, 0.75f, 0.75f, 0.35f));
            AddLayout(starBack, flexibleWidth: 1f, flexibleHeight: 1f);

            RectTransform starFill = AddStretchPanel("Star Fill " + (i + 1), starBack, new Color(1f, 0.78f, 0.18f, 1f));
            Image fillImage = starFill.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
            fills.Add(fillImage);
        }
        return fills.ToArray();
    }

    private static Slider AddSlider(string name, Transform parent, Color backgroundColor, Color fillColor)
    {
        RectTransform root = AddRect(name, parent);
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        RectTransform background = AddStretchPanel("Background", root, backgroundColor);
        background.offsetMin = new Vector2(0f, 8f);
        background.offsetMax = new Vector2(0f, -8f);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.raycastTarget = false;

        RectTransform fillArea = AddStretchRect("Fill Area", root);
        fillArea.offsetMin = new Vector2(6f, 12f);
        fillArea.offsetMax = new Vector2(-6f, -12f);

        RectTransform fill = AddStretchPanel("Fill", fillArea, fillColor);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.raycastTarget = false;

        slider.targetGraphic = backgroundImage;
        slider.fillRect = fill;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static void SetButtonLabelFontSize(Button button, float fontSize)
    {
        if (button == null) return;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            text.fontSize = fontSize;
    }

    private static Button AddButton(string name, Transform parent, string label, Color color)
    {
        RectTransform rect = AddPanel(name, parent, color);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.22f);
        colors.pressedColor = Color.Lerp(color, Color.gray, 0.12f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI text = AddText("Text", rect, label, 22, TextAlignmentOptions.Center, false);
        text.fontStyle = FontStyles.Bold;
        SetTextFontRole(text, GridAdventureFontRole.Primary);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        return button;
    }

    private static RectTransform AddPanel(string name, Transform parent, Color color)
    {
        RectTransform rect = AddRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static RectTransform AddStretchPanel(string name, Transform parent, Color color)
    {
        RectTransform rect = AddStretchRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static RectTransform AddRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    private static RectTransform AddStretchRect(string name, Transform parent)
    {
        RectTransform rect = AddRect(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TextMeshProUGUI AddText(string name, Transform parent, string value, int fontSize, TextAlignmentOptions alignment, bool wordWrap)
    {
        RectTransform rect = AddRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.21f, 0.24f, 0.29f, 1f);
        text.enableWordWrapping = wordWrap;
        text.raycastTarget = false;
        SetTextFontRole(text, IsPrimaryFontTextName(name) ? GridAdventureFontRole.Primary : GridAdventureFontRole.Secondary);
        return text;
    }

    private static void SetTextFontRole(TextMeshProUGUI text, GridAdventureFontRole role)
    {
        if (text == null) return;

        GridAdventureTextFontRole roleComponent = text.GetComponent<GridAdventureTextFontRole>();
        if (roleComponent == null)
            roleComponent = text.gameObject.AddComponent<GridAdventureTextFontRole>();

        roleComponent.fontRole = role;
    }

    private static bool IsPrimaryFontTextName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        string lower = name.ToLowerInvariant();
        return lower.Contains("title")
            || lower.Contains("header")
            || lower.Contains("game name")
            || lower.Contains("timer label")
            || lower.Contains("button");
    }

    private static HorizontalLayoutGroup AddHorizontalLayout(RectTransform rect, RectOffset padding, float spacing, TextAnchor alignment)
    {
        HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static VerticalLayoutGroup AddVerticalLayout(RectTransform rect, RectOffset padding, float spacing, TextAnchor alignment)
    {
        VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static void AddLayout(RectTransform rect, float minWidth = -1f, float minHeight = -1f, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
    {
        LayoutElement layout = rect.gameObject.GetComponent<LayoutElement>();
        if (layout == null) layout = rect.gameObject.AddComponent<LayoutElement>();
        if (minWidth >= 0f) layout.minWidth = minWidth;
        if (minHeight >= 0f) layout.minHeight = minHeight;
        if (preferredWidth >= 0f) layout.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f) layout.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0f) layout.flexibleWidth = flexibleWidth;
        if (flexibleHeight >= 0f) layout.flexibleHeight = flexibleHeight;
    }

    private static Canvas CreateCanvas()
    {
        GameObject existing = GameObject.Find("GridAdventure_Canvas");
        if (existing != null)
            Object.DestroyImmediate(existing);

        GameObject canvasObject = new GameObject("GridAdventure_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static GridAdventureLevelData CreateOrLoadSampleLevel()
    {
        string levelPath = LevelFolder + "/GridAdventure_SampleLevel.asset";
        GridAdventureLevelData level = AssetDatabase.LoadAssetAtPath<GridAdventureLevelData>(levelPath);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<GridAdventureLevelData>();
            AssetDatabase.CreateAsset(level, levelPath);
        }

        if (level.items == null || level.items.Count < 12)
            PopulateSampleLevel(level);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return level;
    }

    private static void PopulateSampleLevel(GridAdventureLevelData level)
    {
        level.levelTitle = "Grid Adventure";
        level.columns = 3;
        level.rows = 3;
        level.basketTitle = "ITEM BASKET";
        level.randomizeWhenMoreThanGridCells = true;
        level.randomSeed = 0;
        level.itemDisplayMode = GridAdventureItemDisplayMode.ImageAndLabel;
        level.items.Clear();

        string[] ids = { "leaf", "sun", "cloud", "flower", "tree", "bird", "rain", "apple", "butterfly", "mushroom", "bee", "moon" };
        string[] names = { "Leaf", "Sun", "Cloud", "Flower", "Tree", "Bird", "Rain", "Apple", "Butterfly", "Mushroom", "Bee", "Moon" };
        string[] coordinates = { "A1", "A2", "A3", "B1", "B2", "B3", "C1", "C2", "C3", "A1", "B2", "C3" };
        Color[] colors =
        {
            new Color(0.55f, 0.85f, 0.45f, 1f),
            new Color(1f, 0.78f, 0.23f, 1f),
            new Color(0.72f, 0.88f, 1f, 1f),
            new Color(1f, 0.63f, 0.76f, 1f),
            new Color(0.39f, 0.73f, 0.49f, 1f),
            new Color(0.68f, 0.58f, 1f, 1f),
            new Color(0.39f, 0.70f, 1f, 1f),
            new Color(1f, 0.42f, 0.36f, 1f),
            new Color(1f, 0.68f, 0.88f, 1f),
            new Color(0.86f, 0.55f, 0.36f, 1f),
            new Color(1f, 0.86f, 0.22f, 1f),
            new Color(0.62f, 0.68f, 0.95f, 1f)
        };

        for (int i = 0; i < ids.Length; i++)
        {
            GridAdventureItemData item = new GridAdventureItemData();
            item.itemId = ids[i];
            item.displayName = names[i];
            item.gridCoordinate = coordinates[i];
            item.clueText = "Find the " + names[i].ToLower() + " and place it here.";
            item.sprite = CreatePlaceholderSprite(ids[i], colors[i]);
            level.items.Add(item);
        }

        EditorUtility.SetDirty(level);
    }

    private static List<Sprite> CreateGuideSprites()
    {
        List<Sprite> sprites = new List<Sprite>();
        sprites.Add(CreateGuideSprite("guide_step_1", new Color(0.82f, 0.92f, 1f, 1f)));
        sprites.Add(CreateGuideSprite("guide_step_2", new Color(1f, 0.91f, 0.72f, 1f)));
        sprites.Add(CreateGuideSprite("guide_step_3", new Color(0.84f, 1f, 0.91f, 1f)));
        return sprites;
    }

    private static Sprite CreateGuideSprite(string id, Color color)
    {
        string path = ArtFolder + "/" + id + ".png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int width = 960;
        const int height = 520;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color border = new Color(0.22f, 0.25f, 0.30f, 1f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool edge = x < 10 || y < 10 || x >= width - 10 || y >= height - 10;
                texture.SetPixel(x, y, edge ? border : color);
            }
        }
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite CreatePlaceholderSprite(string id, Color color)
    {
        string path = ArtFolder + "/" + id + "_placeholder.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(color.r, color.g, color.b, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - size * 0.5f;
                float dy = y - size * 0.5f;
                bool inside = dx * dx + dy * dy < 38f * 38f;
                texture.SetPixel(x, y, inside ? color : clear);
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void EnsureFolders()
    {
        CreateFolderIfMissing("Assets", "GridAdventure");
        CreateFolderIfMissing(RootFolder, "Levels");
        CreateFolderIfMissing(RootFolder, "Art");
        CreateFolderIfMissing(RootFolder + "/Art", "Placeholders");
    }

    private static void CreateFolderIfMissing(string parent, string child)
    {
        string fullPath = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(fullPath))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Selection.activeGameObject = eventSystem;
    }
}
#endif
