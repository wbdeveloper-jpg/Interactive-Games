#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class FractionPortionFillSceneCreator
{
    private static readonly Color PageBg = new Color(0.94f, 0.88f, 0.78f, 1f);
    private static readonly Color CardBg = new Color(1f, 0.96f, 0.88f, 1f);
    private static readonly Color CardBgAlt = new Color(0.98f, 0.91f, 0.76f, 1f);
    private static readonly Color Ink = new Color(0.18f, 0.13f, 0.09f, 1f);
    private static readonly Color MutedInk = new Color(0.36f, 0.28f, 0.2f, 1f);
    private static readonly Color PrimaryButton = new Color(0.28f, 0.48f, 0.36f, 1f);
    private static readonly Color NeutralButton = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color HintButton = new Color(0.25f, 0.45f, 0.9f, 1f);
    private static readonly Color DangerButton = new Color(0.85f, 0.24f, 0.18f, 1f);

    [MenuItem("Tools/Mini Games/Fraction Portion Fill/Create Updated Game Play")]
    public static void CreateWorkingSceneTemplate()
    {
        EnsureEventSystem();

        GameObject canvasGO = new GameObject("Fraction Portion Fill Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRoot = canvasGO.GetComponent<RectTransform>();

        RectTransform background = CreatePanel("Background Root", canvasRoot, PageBg, true);
        Stretch(background, 0f, 0f, 0f, 0f);

        RectTransform safeAreaRoot = CreateRoot("Safe Area Root", canvasRoot);
        Stretch(safeAreaRoot, 24f, 20f, 24f, 20f);
        FractionPortionSafeAreaFitter safeAreaFitter = safeAreaRoot.gameObject.AddComponent<FractionPortionSafeAreaFitter>();
        safeAreaFitter.extraPadding = new Vector4(28f, 18f, 28f, 30f);
        safeAreaFitter.ApplySafeArea(true);

        GameObject managerGO = new GameObject("Fraction Portion Fill Manager", typeof(AudioSource));
        managerGO.transform.SetParent(safeAreaRoot, false);
        FractionPortionFillManager manager = managerGO.AddComponent<FractionPortionFillManager>();
        AudioSource audioSource = managerGO.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        GameObject musicSourceGO = new GameObject("Background Music Source", typeof(AudioSource));
        musicSourceGO.transform.SetParent(managerGO.transform, false);
        AudioSource musicSource = musicSourceGO.GetComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0.28f;

        RectTransform gameplayRoot = CreateRoot("Gameplay Root", safeAreaRoot);
        Stretch(gameplayRoot, 0f, 0f, 0f, 0f);
        FractionPortionResponsiveLayout responsive = gameplayRoot.gameObject.AddComponent<FractionPortionResponsiveLayout>();

        RectTransform mainContentRoot = CreateRoot("Main Content Root", gameplayRoot);
        Stretch(mainContentRoot, 0f, 0f, 0f, 0f);
        HorizontalLayoutGroup mainLayout = mainContentRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        mainLayout.spacing = 18;
        mainLayout.padding = new RectOffset(0, 0, 0, 0);
        mainLayout.childAlignment = TextAnchor.MiddleRight;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = true;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = true;

        RectTransform portionSectionRoot = CreatePanel("Portion Section Root", mainContentRoot, CardBg, true);
        LayoutElement portionLayout = AddLayout(portionSectionRoot, -1f, -1f, 0.74f, 1f);
        Image portionBg = portionSectionRoot.GetComponent<Image>();
        portionBg.raycastTarget = false;

        RectTransform pizzaBoardRoot = CreatePanel("Pizza Board Background Root", portionSectionRoot, new Color(1f, 0.91f, 0.72f, 1f), true);
        Stretch(pizzaBoardRoot, 0f, 0f, 0f, 0f);
        pizzaBoardRoot.GetComponent<Image>().raycastTarget = false;

        // Keep the pizza board clean. Gameplay controls sit on Portion Section Root,
        // not inside Pizza Board Background Root, so the board can keep full usable height.
        RectTransform portionHudRoot = CreateRoot("Portion HUD Root", portionSectionRoot);
        Stretch(portionHudRoot, 0f, 0f, 0f, 0f);
        portionHudRoot.SetAsLastSibling();

        RectTransform timerCard = CreateInfoCard("Time Section Root", portionHudRoot, "Time Text", "Time: 45", 26);
        AnchorTopLeft(timerCard, 18f, 16f, 190f, 54f);
        TMP_Text timerText = timerCard.GetComponentInChildren<TMP_Text>(true);

        RectTransform profitCard = CreateInfoCard("Profit Section Root", portionHudRoot, "Profit Text", "Profit: 0", 26);
        AnchorTopRight(profitCard, 18f, 16f, 230f, 54f);
        TMP_Text scoreText = profitCard.GetComponentInChildren<TMP_Text>(true);

        Button pauseButton = CreateButton("Pause Button", portionHudRoot, "Pause", 24, NeutralButton, Color.white);
        AnchorBottomLeft(pauseButton.GetComponent<RectTransform>(), 18f, 18f, 160f, 58f);

        RectTransform sliceBadgeRoot = CreatePanel("Pizza Slice Count Badge Root", pizzaBoardRoot, new Color(1f, 0.96f, 0.86f, 0.88f), false);
        AnchorTop(sliceBadgeRoot, 760f, 16f, 760f, 44f);
        TMP_Text portionCountText = CreateText("Pizza Slice Count Text", sliceBadgeRoot, "8 slices", 22, TextAlignmentOptions.Center, MutedInk);
        Stretch(portionCountText.GetComponent<RectTransform>(), 8f, 2f, 8f, 2f);

        RectTransform pizzaTrayRoot = CreatePanel("Pizza Tray Visual Root", pizzaBoardRoot, new Color(0.78f, 0.66f, 0.54f, 1f), false);
        CenterSize(pizzaTrayRoot, 740f, 740f);
        RectTransform pizzaEdgeRoot = CreatePanel("Pizza Edge Visual Root", pizzaBoardRoot, new Color(0.86f, 0.55f, 0.24f, 1f), false);
        CenterSize(pizzaEdgeRoot, 670f, 670f);
        RectTransform portionDropZonesRoot = CreateRoot("Portion Drop Zones Root", pizzaBoardRoot);
        CenterSize(portionDropZonesRoot, 590f, 590f);

        RectTransform pizzaFeedbackOverlay = CreatePanel("Pizza Feedback Overlay Root", pizzaBoardRoot, new Color(0f, 0f, 0f, 0f), false);
        Stretch(pizzaFeedbackOverlay, 0f, 0f, 0f, 0f);
        CanvasGroup pizzaFeedbackGroup = pizzaFeedbackOverlay.gameObject.AddComponent<CanvasGroup>();
        pizzaFeedbackGroup.alpha = 0f;
        pizzaFeedbackGroup.blocksRaycasts = false;
        pizzaFeedbackGroup.interactable = false;
        Image pizzaFeedbackImage = pizzaFeedbackOverlay.GetComponent<Image>();
        pizzaFeedbackImage.raycastTarget = false;
        TMP_Text pizzaFeedbackText = CreateText("Pizza Feedback Overlay Text", pizzaFeedbackOverlay, "", 58, TextAlignmentOptions.Center, Color.white);
        Stretch(pizzaFeedbackText.GetComponent<RectTransform>(), 20f, 20f, 20f, 20f);
        pizzaFeedbackText.raycastTarget = false;
        pizzaFeedbackOverlay.SetAsLastSibling();

        RectTransform basketSectionRoot = CreatePanel("Basket Section Root", mainContentRoot, CardBgAlt, true);
        LayoutElement basketLayout = AddLayout(basketSectionRoot, -1f, -1f, 0.26f, 1f);
        VerticalLayoutGroup basketOuterLayout = basketSectionRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        basketOuterLayout.padding = new RectOffset(12, 12, 12, 12);
        basketOuterLayout.spacing = 10;
        basketOuterLayout.childControlWidth = true;
        basketOuterLayout.childControlHeight = true;
        basketOuterLayout.childForceExpandWidth = true;
        basketOuterLayout.childForceExpandHeight = false;

        RectTransform basketHeaderRoot = CreatePanel("Basket Header Root", basketSectionRoot, new Color(1f, 0.96f, 0.86f, 0.82f), false);
        AddLayout(basketHeaderRoot, -1f, 48f, 1f, 0f);
        TMP_Text basketHeaderText = CreateText("Basket Header Text", basketHeaderRoot, "Ingredients", 26, TextAlignmentOptions.Center, Ink);
        Stretch(basketHeaderText.GetComponent<RectTransform>(), 12f, 4f, 12f, 4f);

        RectTransform basketScrollRoot = CreatePanel("Basket Cards Scroll Root", basketSectionRoot, new Color(1f, 1f, 1f, 0f), false);
        AddLayout(basketScrollRoot, -1f, -1f, 1f, 1f);
        ScrollRect basketScroll = basketScrollRoot.gameObject.AddComponent<ScrollRect>();
        basketScroll.horizontal = false;
        basketScroll.vertical = true;
        basketScroll.movementType = ScrollRect.MovementType.Clamped;
        basketScroll.inertia = true;

        RectTransform basketViewport = CreatePanel("Basket Cards Viewport", basketScrollRoot, new Color(1f, 1f, 1f, 0f), false);
        Stretch(basketViewport, 0f, 0f, 0f, 0f);
        basketViewport.gameObject.AddComponent<RectMask2D>();

        RectTransform basketCardsRoot = CreatePanel("Basket Cards Grid Root", basketViewport, new Color(1f, 1f, 1f, 0f), false);
        basketCardsRoot.anchorMin = new Vector2(0f, 1f);
        basketCardsRoot.anchorMax = new Vector2(1f, 1f);
        basketCardsRoot.pivot = new Vector2(0.5f, 1f);
        basketCardsRoot.offsetMin = new Vector2(0f, 0f);
        basketCardsRoot.offsetMax = new Vector2(0f, 0f);
        basketCardsRoot.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup basketGrid = basketCardsRoot.gameObject.AddComponent<GridLayoutGroup>();
        basketGrid.padding = new RectOffset(4, 4, 4, 4);
        basketGrid.spacing = new Vector2(0f, 14f);
        basketGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        basketGrid.constraintCount = 1;
        basketGrid.cellSize = new Vector2(320f, 108f);
        basketGrid.childAlignment = TextAnchor.UpperCenter;

        FractionPortionBasketGridAutoSizer basketAutoSizer = basketCardsRoot.gameObject.AddComponent<FractionPortionBasketGridAutoSizer>();
        basketAutoSizer.viewportRoot = basketViewport;
        basketAutoSizer.minCardWidth = 300f;
        basketAutoSizer.sidePadding = 14f;
        basketAutoSizer.minCardHeight = 96f;
        basketAutoSizer.maxCardHeight = 124f;
        basketAutoSizer.heightPercentOfViewport = 0.18f;

        ContentSizeFitter basketContentFitter = basketCardsRoot.gameObject.AddComponent<ContentSizeFitter>();
        basketContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        basketContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        basketScroll.viewport = basketViewport;
        basketScroll.content = basketCardsRoot;

        RectTransform basketButtonRoot = CreateRoot("Basket Action Buttons Root", basketSectionRoot);
        AddLayout(basketButtonRoot, -1f, 238f, 1f, 0f);
        VerticalLayoutGroup buttonStack = basketButtonRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonStack.padding = new RectOffset(0, 0, 0, 0);
        buttonStack.spacing = 10;
        buttonStack.childAlignment = TextAnchor.MiddleCenter;
        buttonStack.childControlWidth = true;
        buttonStack.childControlHeight = true;
        buttonStack.childForceExpandWidth = true;
        buttonStack.childForceExpandHeight = true;

        Button orderDetailsButton = CreateButton("Order Details Button", basketButtonRoot, "Order Details", 24, PrimaryButton, Color.white);
        AddLayout(orderDetailsButton.GetComponent<RectTransform>(), -1f, 68f, 1f, 0f);
        Button hintButton = CreateButton("Hint Button", basketButtonRoot, "Hint -20", 24, HintButton, Color.white);
        AddLayout(hintButton.GetComponent<RectTransform>(), -1f, 68f, 1f, 0f);
        Button cannotButton = CreateButton("Order Cannot Be Done Button", basketButtonRoot, "Can't Serve", 23, DangerButton, Color.white);
        AddLayout(cannotButton.GetComponent<RectTransform>(), -1f, 68f, 1f, 0f);

        RectTransform overlayRoot = CreateRoot("Overlay Root", safeAreaRoot);
        Stretch(overlayRoot, 0f, 0f, 0f, 0f);
        overlayRoot.SetAsLastSibling();

        GameObject resultPanel = CreateResultOverlay(overlayRoot, out TMP_Text resultText, out Button resultRestart, out Button resultContinue);
        GameObject howToPanel = CreateHowToOverlay(overlayRoot, out FractionPortionHowToGuidePanel howToGuidePanel, out Button closeHowTo);
        GameObject pausePanel = CreatePauseOverlay(overlayRoot, out TMP_Text pauseProfitText, out Button resumeButton, out Button pauseHowToButton, out Button pauseRestartButton);
        GameObject orderDetailsPanel = CreateOrderDetailsOverlay(overlayRoot, out CanvasGroup orderDetailsGroup, out TMP_Text orderTitleText, out TMP_Text orderBodyText, out TMP_Text orderMascotText, out Image orderMascotImage, out TMP_Text orderMascotPlaceholderText, out Button orderContinueButton);

        RectTransform loadingPanel = CreateLoadingPanel(safeAreaRoot, out Slider loadingSlider, out TMP_Text loadingTitleText, out TMP_Text loadingProgressText);
        loadingPanel.SetAsLastSibling();

        FractionPortionFeedbackPopup feedbackPopup = CreateFeedbackPopup(overlayRoot);

        GameObject templatesRoot = new GameObject("Scene Templates - Keep In Scene");
        templatesRoot.transform.SetParent(safeAreaRoot, false);
        FractionPortionDropZone dropZoneTemplate = FractionPortionRuntimeTemplateFactory.CreateDropZoneTemplate(templatesRoot.transform);
        if (dropZoneTemplate.GetComponent<CanvasRenderer>() == null)
            dropZoneTemplate.gameObject.AddComponent<CanvasRenderer>();
        FractionPortionBasketCard basketCardTemplate = FractionPortionRuntimeTemplateFactory.CreateBasketCardTemplate(templatesRoot.transform);
        FractionPortionDragVisual dragVisualTemplate = FractionPortionRuntimeTemplateFactory.CreateDragVisualTemplate(templatesRoot.transform);
        templatesRoot.SetActive(false);

        RectTransform dragLayer = new GameObject("Drag Layer", typeof(RectTransform)).GetComponent<RectTransform>();
        dragLayer.SetParent(safeAreaRoot, false);
        Stretch(dragLayer, 0f, 0f, 0f, 0f);
        dragLayer.SetAsLastSibling();

        responsive.mainContentRoot = mainContentRoot;
        responsive.portionSectionRoot = portionSectionRoot;
        responsive.basketSectionRoot = basketSectionRoot;
        responsive.portionLayoutElement = portionLayout;
        responsive.basketLayoutElement = basketLayout;
        responsive.basketGrid = basketGrid;
        responsive.basketCardsRoot = basketCardsRoot;
        responsive.pizzaBoardRoot = pizzaBoardRoot;
        responsive.pizzaTrayVisualRoot = pizzaTrayRoot;
        responsive.pizzaEdgeVisualRoot = pizzaEdgeRoot;
        responsive.portionDropZoneRoot = portionDropZonesRoot;
        responsive.portionWidthWeight = 0.74f;
        responsive.basketWidthWeight = 0.26f;
        responsive.pizzaBoardFillPercent = 0.96f;
        responsive.edgeToTrayPercent = 0.9f;
        responsive.dropZoneToEdgePercent = 0.88f;
        responsive.pizzaBoardInnerPadding = 6f;

        manager.rootCanvas = canvas;
        manager.audioSource = audioSource;
        manager.musicSource = musicSource;
        manager.gameplayRoot = gameplayRoot.gameObject;
        manager.overlayRoot = overlayRoot.gameObject;
        manager.loadingPanel = loadingPanel.gameObject;
        manager.loadingSlider = loadingSlider;
        manager.loadingTitleText = loadingTitleText;
        manager.loadingProgressText = loadingProgressText;
        manager.portionRoot = portionDropZonesRoot;
        manager.pizzaFeedbackTarget = pizzaBoardRoot;
        manager.pizzaFeedbackOverlayGroup = pizzaFeedbackGroup;
        manager.pizzaFeedbackOverlayImage = pizzaFeedbackImage;
        manager.pizzaFeedbackOverlayText = pizzaFeedbackText;
        manager.basketRoot = basketCardsRoot;
        manager.dragLayer = dragLayer;
        manager.dropZoneTemplate = dropZoneTemplate;
        manager.basketCardTemplate = basketCardTemplate;
        manager.dragVisualTemplate = dragVisualTemplate;
        manager.scoreText = scoreText;
        manager.timerText = timerText;
        manager.portionCountText = portionCountText;
        manager.cannotCompleteButton = cannotButton;
        manager.hintButton = hintButton;
        manager.orderDetailsButton = orderDetailsButton;
        manager.orderDetailsPanel = orderDetailsPanel;
        manager.orderDetailsCanvasGroup = orderDetailsGroup;
        manager.orderDetailsTitleText = orderTitleText;
        manager.orderDetailsBodyText = orderBodyText;
        manager.orderDetailsMascotText = orderMascotText;
        manager.orderDetailsMascotImage = orderMascotImage;
        manager.orderDetailsMascotPlaceholderText = orderMascotPlaceholderText;
        manager.orderDetailsContinueButton = orderContinueButton;
        manager.resultPanel = resultPanel;
        manager.resultText = resultText;
        manager.resultContinueButton = resultContinue;
        manager.howToPlayPanel = howToPanel;
        manager.howToGuidePanel = howToGuidePanel;
        manager.pausePanel = pausePanel;
        manager.pauseProfitText = pauseProfitText;
        manager.feedbackPopup = feedbackPopup;

        manager.rounds = 10;
        manager.perfectOrderReward = 100;
        manager.wrongOrderPenalty = 50;
        manager.hintCost = 20;
        manager.useCommonPortionCountsOnly = true;
        manager.avoidWholePizzaFractions = true;
        manager.keepOperationFractionsOnPizzaDenominator = true;
        manager.minPortionCount = 4;
        manager.maxPortionCount = 12;
        manager.minRequestsPerQuestion = 1;
        manager.maxRequestsPerQuestion = 2;
        manager.questionMode = FractionPortionFillManager.QuestionMode.MixedRuntime;
        manager.allowImpossibleOrders = true;
        manager.impossibleOrderChance = 0.2f;
        manager.showCannotCompleteButton = true;
        manager.solvableExtraStockMin = 0;
        manager.solvableExtraStockMax = 2;
        manager.returnClearedItemsToBasket = false;
        manager.showLoadingBeforeGame = true;
        manager.loadingDuration = 1.2f;
        manager.showHowToBeforeFirstOrder = true;
        manager.showOrderDetailsBeforeEachOrder = true;
        manager.orderDetailsIntroAutoCloseSeconds = 30f;
        manager.orderDetailsReviewAutoCloseSeconds = 15f;
        manager.orderDetailsAnimationDuration = 0.34f;
        manager.customerMascotLabel = "Customer";
        manager.chefMascotLabel = "Chef";
        manager.playBackgroundMusic = true;
        manager.loopBackgroundMusic = true;
        manager.backgroundMusicVolume = 0.28f;
        manager.useSequentialResultFeedback = true;
        manager.portionCutGapPercent = 0.012f;
        manager.fillEntireSliceWithDroppedTopping = false;
        manager.filledSliceToppingAlpha = 0.45f;
        manager.scatterToppingCopiesOnPlacedSlice = true;
        manager.useDynamicToppingCopyCount = true;
        manager.toppingCopiesPerPlacedSlice = 7;
        manager.toppingCopiesAtFourSlices = 20;
        manager.toppingCopiesAtTwelveSlices = 6;
        manager.toppingCopyMinSize = 28f;
        manager.toppingCopyMaxSize = 44f;
        manager.toppingScatterInnerRadiusPercent = 0.16f;
        manager.toppingScatterOuterRadiusPercent = 0.88f;
        manager.toppingScatterAnglePaddingPercent = 0.06f;
        manager.toppingScatterMinDistance = 30f;
        manager.toppingScatterPlacementAttempts = 22;
        manager.randomizeToppingCopyRotation = true;
        manager.showPlacedItemIconOnPizza = false;
        manager.showPlacedItemLabelOnPizza = false;
        manager.showDragVisualName = false;
        manager.pizzaFeedbackScaleAmount = 1.06f;
        manager.pizzaFeedbackPulseDuration = 0.22f;
        manager.pizzaFeedbackHoldDuration = 0.32f;
        manager.popupAfterPizzaDelay = 0.12f;
        manager.wrongFeedbackShakeDistance = 16f;
        manager.neutralFeedbackColor = Ink;
        manager.nextDelay = 1.05f;
        feedbackPopup.fadeInDuration = 0.18f;
        feedbackPopup.scaleInDuration = 0.22f;
        feedbackPopup.holdDuration = 1.25f;
        feedbackPopup.moveUpDuration = 0.45f;
        feedbackPopup.fadeOutDuration = 0.35f;
        feedbackPopup.moveUpDistance = 42f;
        manager.items.Clear();
        AddDefaultItems(manager);

        UnityEventTools.AddPersistentListener(pauseButton.onClick, manager.TogglePause);
        UnityEventTools.AddPersistentListener(orderDetailsButton.onClick, manager.OpenOrderDetailsFromButton);
        UnityEventTools.AddPersistentListener(hintButton.onClick, manager.ShowHint);
        UnityEventTools.AddPersistentListener(cannotButton.onClick, manager.OnCannotCompletePressed);
        UnityEventTools.AddPersistentListener(orderContinueButton.onClick, manager.CloseOrderDetailsPanel);
        UnityEventTools.AddPersistentListener(closeHowTo.onClick, manager.CloseHowToPlay);
        UnityEventTools.AddPersistentListener(resumeButton.onClick, manager.TogglePause);
        UnityEventTools.AddPersistentListener(pauseHowToButton.onClick, manager.OpenHowToPlay);
        UnityEventTools.AddPersistentListener(pauseRestartButton.onClick, manager.RestartGame);
        UnityEventTools.AddPersistentListener(resultRestart.onClick, manager.RestartGame);
        UnityEventTools.AddPersistentListener(resultContinue.onClick, manager.ShowBloomPostGameFromResultButton);

        resultPanel.SetActive(false);
        howToPanel.SetActive(false);
        pausePanel.SetActive(false);
        orderDetailsPanel.SetActive(false);
        loadingPanel.gameObject.SetActive(true);
        gameplayRoot.gameObject.SetActive(true);

        responsive.Apply();
        manager.ApplyConfiguredFonts();

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;

        Debug.Log("Fraction Portion Fill latest mobile order-flow scene created. Gameplay area is maximized and order/hint/pause info uses overlays.");
    }

    private static GameObject CreateOrderDetailsOverlay(Transform parent, out CanvasGroup canvasGroup, out TMP_Text titleText, out TMP_Text bodyText, out TMP_Text mascotText, out Image mascotImage, out TMP_Text mascotPlaceholderText, out Button continueButton)
    {
        RectTransform overlay = CreatePanel("Order Details Overlay Root", parent, new Color(0f, 0f, 0f, 0.54f), true);
        Stretch(overlay, 0f, 0f, 0f, 0f);
        canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        RectTransform card = CreatePanel("Order Details Card Root", overlay, new Color(1f, 0.96f, 0.88f, 0.98f), true);
        CenterSize(card, 980f, 600f);
        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(34, 34, 28, 28);
        cardLayout.spacing = 18f;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        RectTransform headerRoot = CreatePanel("Order Details Header Root", card, new Color(1f, 1f, 1f, 0f), false);
        AddLayout(headerRoot, -1f, 94f, 1f, 0f);

        TMP_Text mainHeading = CreateText("Order Details Main Heading Text", headerRoot, "Order Details", 40, TextAlignmentOptions.Center, Ink);
        AnchorTop(mainHeading.GetComponent<RectTransform>(), 20f, 0f, 20f, 48f);

        RectTransform orderBadgeRoot = CreatePanel("Order Number Badge Root", headerRoot, new Color(0.28f, 0.48f, 0.36f, 0.96f), false);
        AnchorBottom(orderBadgeRoot, 320f, 0f, 320f, 38f);
        titleText = CreateText("Order Details Title Text", orderBadgeRoot, "Order 1/10", 24, TextAlignmentOptions.Center, Color.white);
        Stretch(titleText.GetComponent<RectTransform>(), 12f, 2f, 12f, 2f);

        RectTransform contentRoot = CreateRoot("Order Details Content Root", card);
        AddLayout(contentRoot, -1f, -1f, 1f, 1f);
        HorizontalLayoutGroup contentLayout = contentRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing = 22f;
        contentLayout.childAlignment = TextAnchor.MiddleCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = true;

        RectTransform mascotSectionRoot = CreatePanel("Mascot Section Root", contentRoot, new Color(1f, 0.91f, 0.74f, 0.82f), true);
        AddLayout(mascotSectionRoot, 285f, -1f, 0f, 1f);
        VerticalLayoutGroup mascotLayout = mascotSectionRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        mascotLayout.padding = new RectOffset(18, 18, 18, 18);
        mascotLayout.spacing = 12f;
        mascotLayout.childAlignment = TextAnchor.MiddleCenter;
        mascotLayout.childControlWidth = true;
        mascotLayout.childControlHeight = true;
        mascotLayout.childForceExpandWidth = true;
        mascotLayout.childForceExpandHeight = false;

        RectTransform mascotArtRoot = CreatePanel("Mascot Art Placeholder Root", mascotSectionRoot, new Color(0.28f, 0.48f, 0.36f, 1f), false);
        AddLayout(mascotArtRoot, -1f, 250f, 1f, 0f);
        mascotImage = mascotArtRoot.GetComponent<Image>();
        if (mascotImage != null)
            mascotImage.preserveAspect = true;
        TMP_Text mascotIconText = CreateText("Mascot Icon Placeholder Text", mascotArtRoot, "Mascot", 44, TextAlignmentOptions.Center, Color.white);
        Stretch(mascotIconText.GetComponent<RectTransform>(), 10f, 10f, 10f, 10f);
        mascotPlaceholderText = mascotIconText;

        mascotText = CreateText("Mascot Label Text", mascotSectionRoot, "Customer", 30, TextAlignmentOptions.Center, Ink);
        AddLayout(mascotText.GetComponent<RectTransform>(), -1f, 52f, 1f, 0f);

        RectTransform orderDetailsSectionRoot = CreatePanel("Order Details Section Root", contentRoot, new Color(1f, 0.98f, 0.92f, 0.96f), true);
        AddLayout(orderDetailsSectionRoot, -1f, -1f, 1f, 1f);
        VerticalLayoutGroup orderLayout = orderDetailsSectionRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        orderLayout.padding = new RectOffset(28, 28, 24, 24);
        orderLayout.spacing = 14f;
        orderLayout.childAlignment = TextAnchor.UpperCenter;
        orderLayout.childControlWidth = true;
        orderLayout.childControlHeight = true;
        orderLayout.childForceExpandWidth = true;
        orderLayout.childForceExpandHeight = false;

        TMP_Text sectionHeading = CreateText("Order Details Section Heading Text", orderDetailsSectionRoot, "Customer Order", 30, TextAlignmentOptions.Center, MutedInk);
        AddLayout(sectionHeading.GetComponent<RectTransform>(), -1f, 42f, 1f, 0f);

        RectTransform orderTextCardRoot = CreatePanel("Order Text Card Root", orderDetailsSectionRoot, new Color(1f, 1f, 1f, 0.72f), false);
        AddLayout(orderTextCardRoot, -1f, -1f, 1f, 1f);
        bodyText = CreateText("Order Details Body Text", orderTextCardRoot, "8-slice pizza\nMake: Capsicum = 1/4 of pizza", 34, TextAlignmentOptions.Center, Ink);
        Stretch(bodyText.GetComponent<RectTransform>(), 26f, 18f, 26f, 18f);
        bodyText.richText = true;
        bodyText.enableWordWrapping = true;
        bodyText.enableAutoSizing = true;
        bodyText.fontSizeMin = 24f;
        bodyText.fontSizeMax = 38f;
        bodyText.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform footerRoot = CreatePanel("Order Details Footer Root", card, new Color(1f, 1f, 1f, 0f), false);
        AddLayout(footerRoot, -1f, 78f, 1f, 0f);
        continueButton = CreateButton("Order Details Continue Button", footerRoot, "Continue", 30, PrimaryButton, Color.white);
        CenterSize(continueButton.GetComponent<RectTransform>(), 330f, 66f);
        return overlay.gameObject;
    }

    private static GameObject CreateResultOverlay(Transform parent, out TMP_Text resultText, out Button restartButton, out Button continueButton)
    {
        RectTransform overlay = CreatePanel("Result Overlay Root", parent, new Color(0f, 0f, 0f, 0.58f), true);
        Stretch(overlay, 0f, 0f, 0f, 0f);
        RectTransform card = CreatePanel("Result Card Root", overlay, new Color(0.12f, 0.1f, 0.08f, 0.96f), true);
        CenterSize(card, 760f, 500f);
        TMP_Text title = CreateText("Result Title Text", card, "Order Summary", 42, TextAlignmentOptions.Center, Color.white);
        AnchorTop(title.GetComponent<RectTransform>(), 28f, 28f, 28f, 82f);
        resultText = CreateText("Result Details Text", card, "Result", 32, TextAlignmentOptions.Center, Color.white);
        Stretch(resultText.GetComponent<RectTransform>(), 40f, 120f, 40f, 120f);
        RectTransform buttonsRoot = CreatePanel("Result Buttons Root", card, new Color(1f, 1f, 1f, 0f), false);
        AnchorBottom(buttonsRoot, 110f, 34f, 110f, 84f);

        restartButton = CreateButton("Result Restart Button", buttonsRoot, "Restart", 30, NeutralButton, Color.white);
        RectTransform restartRect = restartButton.GetComponent<RectTransform>();
        restartRect.anchorMin = new Vector2(0f, 0.5f);
        restartRect.anchorMax = new Vector2(0f, 0.5f);
        restartRect.pivot = new Vector2(0f, 0.5f);
        restartRect.anchoredPosition = new Vector2(0f, 0f);
        restartRect.sizeDelta = new Vector2(250f, 72f);

        continueButton = CreateButton("Result Continue Button", buttonsRoot, "Continue", 30, PrimaryButton, Color.white);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(1f, 0.5f);
        continueRect.anchorMax = new Vector2(1f, 0.5f);
        continueRect.pivot = new Vector2(1f, 0.5f);
        continueRect.anchoredPosition = new Vector2(0f, 0f);
        continueRect.sizeDelta = new Vector2(250f, 72f);
        return overlay.gameObject;
    }

    private static GameObject CreateHowToOverlay(Transform parent, out FractionPortionHowToGuidePanel guidePanel, out Button continueButton)
    {
        RectTransform overlay = CreatePanel("How To Overlay Root", parent, new Color(0f, 0f, 0f, 0.58f), true);
        Stretch(overlay, 0f, 0f, 0f, 0f);

        RectTransform card = CreatePanel("How To Card Root", overlay, new Color(0.12f, 0.1f, 0.08f, 0.96f), true);
        CenterSize(card, 980f, 660f);
        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(34, 34, 28, 28);
        cardLayout.spacing = 16f;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        RectTransform headerRoot = CreatePanel("How To Header Root", card, new Color(1f, 1f, 1f, 0f), false);
        AddLayout(headerRoot, -1f, 78f, 1f, 0f);
        TMP_Text title = CreateText("How To Title Text", headerRoot, "How To Play", 46, TextAlignmentOptions.Center, Color.white);
        Stretch(title.GetComponent<RectTransform>(), 20f, 0f, 20f, 0f);

        RectTransform guideImageRoot = CreatePanel("How To Guide Image Root", card, new Color(1f, 0.96f, 0.88f, 0.12f), true);
        AddLayout(guideImageRoot, -1f, -1f, 1f, 1f);
        RectTransform imageFrame = CreatePanel("Guide Image Frame Root", guideImageRoot, new Color(1f, 1f, 1f, 0.96f), true);
        Stretch(imageFrame, 18f, 14f, 18f, 14f);
        Image guideImage = CreateImage("Guide Image", imageFrame, Vector2.zero, new Vector2(760f, 390f), Color.white, false);
        Stretch(guideImage.rectTransform, 18f, 18f, 18f, 18f);
        guideImage.preserveAspect = true;
        guideImage.enabled = false;

        TMP_Text emptyText = CreateText("Guide Image Empty State Text", imageFrame, "Assign guide images in Inspector", 30, TextAlignmentOptions.Center, MutedInk);
        Stretch(emptyText.GetComponent<RectTransform>(), 24f, 24f, 24f, 24f);

        RectTransform footerRoot = CreatePanel("How To Footer Root", card, new Color(1f, 1f, 1f, 0f), false);
        AddLayout(footerRoot, -1f, 92f, 1f, 0f);

        Button previousButton = CreateButton("How To Previous Button", footerRoot, "Prev", 28, NeutralButton, Color.white);
        AnchorBottomLeft(previousButton.GetComponent<RectTransform>(), 0f, 10f, 190f, 66f);

        TMP_Text stepText = CreateText("How To Step Text", footerRoot, "Step 1 / 1", 26, TextAlignmentOptions.Center, Color.white);
        CenterSize(stepText.GetComponent<RectTransform>(), 260f, 52f);
        stepText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 8f);

        Button nextButton = CreateButton("How To Next Button", footerRoot, "Next", 28, NeutralButton, Color.white);
        RectTransform nextRect = nextButton.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(1f, 0f);
        nextRect.anchorMax = new Vector2(1f, 0f);
        nextRect.pivot = new Vector2(1f, 0f);
        nextRect.anchoredPosition = new Vector2(-220f, 10f);
        nextRect.sizeDelta = new Vector2(190f, 66f);

        continueButton = CreateButton("Close How To Button", footerRoot, "Continue", 28, PrimaryButton, Color.white);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(1f, 0f);
        continueRect.anchorMax = new Vector2(1f, 0f);
        continueRect.pivot = new Vector2(1f, 0f);
        continueRect.anchoredPosition = new Vector2(0f, 10f);
        continueRect.sizeDelta = new Vector2(200f, 66f);

        guidePanel = overlay.gameObject.AddComponent<FractionPortionHowToGuidePanel>();
        guidePanel.guideImage = guideImage;
        guidePanel.stepText = stepText;
        guidePanel.emptyStateObject = emptyText.gameObject;
        guidePanel.previousButton = previousButton;
        guidePanel.nextButton = nextButton;
        guidePanel.continueButton = continueButton;
        guidePanel.preserveImageAspect = true;
        guidePanel.pageFadeDuration = 0.18f;
        guidePanel.pageScaleDuration = 0.18f;

        return overlay.gameObject;
    }

    private static GameObject CreatePauseOverlay(Transform parent, out TMP_Text profitText, out Button resumeButton, out Button howToButton, out Button restartButton)
    {
        RectTransform overlay = CreatePanel("Pause Overlay Root", parent, new Color(0f, 0f, 0f, 0.58f), true);
        Stretch(overlay, 0f, 0f, 0f, 0f);
        RectTransform card = CreatePanel("Pause Card Root", overlay, new Color(0.12f, 0.1f, 0.08f, 0.96f), true);
        CenterSize(card, 620f, 500f);
        TMP_Text title = CreateText("Pause Title Text", card, "Paused", 48, TextAlignmentOptions.Center, Color.white);
        AnchorTop(title.GetComponent<RectTransform>(), 32f, 30f, 32f, 72f);
        profitText = CreateText("Pause Profit Text", card, "Current Profit: 0", 30, TextAlignmentOptions.Center, Color.white);
        AnchorTop(profitText.GetComponent<RectTransform>(), 32f, 108f, 32f, 54f);
        resumeButton = CreateButton("Resume Button", card, "Resume", 30, PrimaryButton, Color.white);
        CenterSize(resumeButton.GetComponent<RectTransform>(), 300f, 68f);
        resumeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 34f);
        howToButton = CreateButton("Pause How To Play Button", card, "How To Play", 28, NeutralButton, Color.white);
        CenterSize(howToButton.GetComponent<RectTransform>(), 300f, 64f);
        howToButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -46f);
        restartButton = CreateButton("Pause Restart Button", card, "Restart", 28, DangerButton, Color.white);
        CenterSize(restartButton.GetComponent<RectTransform>(), 300f, 64f);
        restartButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -124f);
        return overlay.gameObject;
    }

    private static RectTransform CreateLoadingPanel(Transform parent, out Slider slider, out TMP_Text titleText, out TMP_Text progressText)
    {
        RectTransform overlay = CreatePanel("Loading Page Root", parent, PageBg, true);
        Stretch(overlay, 0f, 0f, 0f, 0f);
        RectTransform card = CreatePanel("Loading Card Root", overlay, new Color(1f, 0.96f, 0.88f, 1f), true);
        CenterSize(card, 760f, 420f);
        titleText = CreateText("Loading Game Title Text", card, "Pizza Fraction Chef", 56, TextAlignmentOptions.Center, Ink);
        AnchorTop(titleText.GetComponent<RectTransform>(), 36f, 54f, 36f, 110f);
        slider = CreateSlider("Loading Slider", card);
        CenterSize(slider.GetComponent<RectTransform>(), 520f, 34f);
        slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -34f);
        progressText = CreateText("Loading Progress Text", card, "Loading 0%", 28, TextAlignmentOptions.Center, MutedInk);
        AnchorBottom(progressText.GetComponent<RectTransform>(), 110f, 60f, 110f, 46f);
        return overlay;
    }

    private static FractionPortionFeedbackPopup CreateFeedbackPopup(Transform parent)
    {
        GameObject go = new GameObject("Feedback Popup", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        CenterSize(rect, 780f, 90f);
        rect.anchoredPosition = new Vector2(0f, -250f);
        FractionPortionFeedbackPopup popup = go.AddComponent<FractionPortionFeedbackPopup>();
        popup.canvasGroup = go.GetComponent<CanvasGroup>();
        popup.popupRoot = rect;
        popup.popupText = CreateText("Popup Text", go.transform, "", 36, TextAlignmentOptions.Center, Color.white);
        Stretch(popup.popupText.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        return popup;
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        GameObject sliderGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderGO.transform.SetParent(parent, false);
        Image bg = sliderGO.GetComponent<Image>();
        bg.color = new Color(0.84f, 0.75f, 0.62f, 1f);
        bg.raycastTarget = false;
        Slider slider = sliderGO.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;
        RectTransform fillArea = CreateRoot("Fill Area", sliderGO.transform);
        Stretch(fillArea, 4f, 4f, 4f, 4f);
        RectTransform fill = CreatePanel("Fill", fillArea, PrimaryButton, false);
        Stretch(fill, 0f, 0f, 0f, 0f);
        slider.fillRect = fill;
        slider.targetGraphic = fill.GetComponent<Image>();
        return slider;
    }

    private static RectTransform CreateInfoCard(string rootName, Transform parent, string textName, string textValue, int fontSize)
    {
        RectTransform card = CreatePanel(rootName, parent, new Color(1f, 0.96f, 0.86f, 0.9f), true);
        TMP_Text text = CreateText(textName, card, textValue, fontSize, TextAlignmentOptions.Center, Ink);
        Stretch(text.GetComponent<RectTransform>(), 10f, 4f, 10f, 4f);
        return card;
    }

    private static Button CreateButton(string name, Transform parent, string label, int fontSize, Color backgroundColor, Color textColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = true;
        Button button = go.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.12f);
        colors.selectedColor = backgroundColor;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
        button.colors = colors;
        TMP_Text text = CreateText("Button Label Text", go.transform, label, fontSize, TextAlignmentOptions.Center, textColor);
        Stretch(text.GetComponent<RectTransform>(), 10f, 4f, 10f, 4f);
        return button;
    }


    private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool raycastTarget)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget && color.a > 0.01f;
        return rect;
    }

    private static RectTransform CreateRoot(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private static LayoutElement AddLayout(RectTransform rect, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
    {
        LayoutElement layout = rect.gameObject.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();
        if (preferredWidth >= 0f)
            layout.preferredWidth = preferredWidth;
        if (preferredHeight >= 0f)
            layout.preferredHeight = preferredHeight;
        layout.flexibleWidth = flexibleWidth;
        layout.flexibleHeight = flexibleHeight;
        return layout;
    }

    private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void CenterSize(RectTransform rect, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void AnchorTopLeft(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void AnchorTopRight(RectTransform rect, float right, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-right, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void AnchorBottomLeft(RectTransform rect, float left, float bottom, float width, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(left, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void AnchorTop(RectTransform rect, float left, float top, float right, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void AnchorBottom(RectTransform rect, float left, float bottom, float right, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, bottom + height);
    }

    private static void AddDefaultItems(FractionPortionFillManager manager)
    {
        manager.items.Add(new FractionPortionFillManager.PortionItemData { id = "capsicum", displayName = "Capsicum", color = new Color(0.25f, 0.75f, 0.25f), distractorStockMin = 1, distractorStockMax = 4 });
        manager.items.Add(new FractionPortionFillManager.PortionItemData { id = "olive", displayName = "Olive", color = new Color(0.08f, 0.08f, 0.08f), distractorStockMin = 1, distractorStockMax = 4 });
        manager.items.Add(new FractionPortionFillManager.PortionItemData { id = "tomato", displayName = "Tomato", color = new Color(0.9f, 0.12f, 0.1f), distractorStockMin = 1, distractorStockMax = 4 });
        manager.items.Add(new FractionPortionFillManager.PortionItemData { id = "cheese", displayName = "Cheese", color = new Color(1f, 0.9f, 0.2f), distractorStockMin = 1, distractorStockMax = 4 });
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
#endif
