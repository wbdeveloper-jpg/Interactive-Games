#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OrderSortDragSceneBuilder
{
    [MenuItem("Tools/Mini Games/Order Sort Drag/Text Mode/Create Horizontal Organic Scene")]
    public static void CreateTextHorizontalOrganicScene()
    {
        CreateScene(OrderSortLayoutMode.HorizontalSlots, OrderSortBankPlacementMode.OrganicRandom, OrderSortContentMode.TextOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Text Mode/Create Vertical Organic Scene")]
    public static void CreateTextVerticalOrganicScene()
    {
        CreateScene(OrderSortLayoutMode.VerticalSteps, OrderSortBankPlacementMode.OrganicRandom, OrderSortContentMode.TextOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Image Mode/Create Horizontal Organic Scene")]
    public static void CreateImageHorizontalOrganicScene()
    {
        CreateScene(OrderSortLayoutMode.HorizontalSlots, OrderSortBankPlacementMode.OrganicRandom, OrderSortContentMode.ImageOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Image Mode/Create Vertical Organic Scene")]
    public static void CreateImageVerticalOrganicScene()
    {
        CreateScene(OrderSortLayoutMode.VerticalSteps, OrderSortBankPlacementMode.OrganicRandom, OrderSortContentMode.ImageOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Math Text Mode/Create Math Expressions 1-3 Scene")]
    public static void CreateMathExpressionScene()
    {
        CreateScene(OrderSortLayoutMode.HorizontalSlots, OrderSortBankPlacementMode.OrganicRandom, OrderSortContentMode.TextOnly, SampleSet.MathExpressions);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Grid Versions/Create Text Horizontal Grid Scene")]
    public static void CreateTextHorizontalGridScene()
    {
        CreateScene(OrderSortLayoutMode.HorizontalSlots, OrderSortBankPlacementMode.GridLayoutGroup, OrderSortContentMode.TextOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Grid Versions/Create Text Vertical Grid Scene")]
    public static void CreateTextVerticalGridScene()
    {
        CreateScene(OrderSortLayoutMode.VerticalSteps, OrderSortBankPlacementMode.GridLayoutGroup, OrderSortContentMode.TextOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Grid Versions/Create Image Horizontal Grid Scene")]
    public static void CreateImageHorizontalGridScene()
    {
        CreateScene(OrderSortLayoutMode.HorizontalSlots, OrderSortBankPlacementMode.GridLayoutGroup, OrderSortContentMode.ImageOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Grid Versions/Create Image Vertical Grid Scene")]
    public static void CreateImageVerticalGridScene()
    {
        CreateScene(OrderSortLayoutMode.VerticalSteps, OrderSortBankPlacementMode.GridLayoutGroup, OrderSortContentMode.ImageOnly, SampleSet.Standard);
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Create Text Horizontal Scene")]
    public static void CreateTextHorizontalQuickScene()
    {
        CreateTextHorizontalOrganicScene();
    }

    [MenuItem("Tools/Mini Games/Order Sort Drag/Create Image Horizontal Scene")]
    public static void CreateImageHorizontalQuickScene()
    {
        CreateImageHorizontalOrganicScene();
    }

    private enum OrderSortLayoutMode
    {
        HorizontalSlots,
        VerticalSteps
    }

    private enum SampleSet
    {
        Standard,
        MathExpressions
    }

    private struct OverlayPanelParts
    {
        public GameObject panelRoot;
        public RectTransform rootCard;
    }

    private static void CreateScene(OrderSortLayoutMode layoutMode, OrderSortBankPlacementMode bankPlacementMode, OrderSortContentMode contentMode, SampleSet sampleSet)
    {
        CreateEventSystemIfNeeded();

        Canvas canvas = CreateCanvas();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        GameObject managerObj = new GameObject("OrderSortDragManager");
        OrderSortDragManager manager = managerObj.AddComponent<OrderSortDragManager>();

        AudioSource sfxAudioSource = managerObj.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;
        manager.sfxSource = sfxAudioSource;

        AudioSource bgmAudioSource = managerObj.AddComponent<AudioSource>();
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = 0.4f;
        manager.bgmSource = bgmAudioSource;
        manager.playBgmOnGameStart = true;
        manager.loopBackgroundMusic = true;
        manager.backgroundMusicVolume = 0.4f;
        manager.pauseBgmWithGamePause = true;
        manager.stopBgmOnRewardScreen = true;

        manager.contentMode = contentMode;
        manager.bankPlacementMode = bankPlacementMode;

        manager.textCardSize = sampleSet == SampleSet.MathExpressions ? new Vector2(168f, 82f) : new Vector2(190f, 74f);
        manager.textSlotSize = sampleSet == SampleSet.MathExpressions ? new Vector2(178f, 96f) : new Vector2(190f, 90f);
        manager.imageCardSize = new Vector2(116f, 116f);
        manager.imageSlotSize = new Vector2(128f, 128f);

        manager.questionLimit = 0;
        manager.objectsPerQuestion = sampleSet == SampleSet.MathExpressions ? 3 : 5;
        manager.randomizeObjectsPerQuestion = sampleSet != SampleSet.MathExpressions;
        manager.secondsPerObject = sampleSet == SampleSet.MathExpressions ? 8f : 6f;
        manager.minimumQuestionTime = 12f;
        manager.maximumQuestionTime = 90f;
        manager.allowReturnToBasket = false;
        manager.allowSwap = true;
        manager.gameTitle = sampleSet == SampleSet.MathExpressions ? "Math Order" : "Order Sort";
        manager.scorePerCorrectPosition = 10;
        manager.penaltyPerWrongPosition = -20;
        manager.scorePerEmptySlot = 0;
        manager.allowNegativeScore = false;
        manager.autoShowResultAfterFinalQuestion = true;
        manager.betweenSlotCheckDelay = 0.45f;
        manager.checkingFlashDuration = 0.75f;
        manager.resultColorLerpDuration = 0.55f;
        manager.resultHoldDuration = 0.75f;
        manager.resultFadeOutDuration = 0.3f;
        manager.keepWrongOverlayVisible = true;
        manager.keepAllResultOverlaysVisible = true;
        manager.updateScoreDuringSlotEvaluation = true;
        manager.spawnScorePopupAtCenter = true;
        manager.scorePopupCenterOffset = new Vector2(0f, 20f);
        manager.scorePopupStackSpacing = 42f;
        manager.useRandomTextCardColors = true;
        manager.textCardPastelColors = new System.Collections.Generic.List<Color>
        {
            new Color(1f, 0.86f, 0.71f, 1f),
            new Color(0.86f, 0.94f, 1f, 1f),
            new Color(0.89f, 1f, 0.86f, 1f),
            new Color(1f, 0.90f, 0.96f, 1f),
            new Color(0.95f, 0.90f, 1f, 1f),
        };
        manager.useResponsiveSlots = true;
        manager.useBloomRewardSystem = true;
        manager.showHowToPlayBeforeGame = true;

        manager.organicBankPadding = contentMode == OrderSortContentMode.ImageOnly
            ? new Vector2(64f, 54f)
            : new Vector2(90f, 58f);
        manager.organicRotationRange = contentMode == OrderSortContentMode.ImageOnly ? 10f : 7f;
        manager.organicScaleRange = new Vector2(0.94f, 1.06f);
        manager.avoidOverlapInOrganicBank = true;
        manager.maxOrganicPlacementAttempts = 100;

        RectTransform root = CreateUIObject("OrderSortRoot", canvasRect);
        Stretch(root);

        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(28, 28, 22, 22);
        rootLayout.spacing = 14;
        rootLayout.childControlHeight = false;
        rootLayout.childControlWidth = true;
        rootLayout.childForceExpandHeight = false;

        string instruction = GetDefaultQuestionText(contentMode, sampleSet);
        CreateHeader(root, manager, instruction);

        if (layoutMode == OrderSortLayoutMode.HorizontalSlots)
            CreateHorizontalGameplayArea(root, manager, bankPlacementMode, contentMode);
        else
            CreateVerticalGameplayArea(root, manager, bankPlacementMode, contentMode);

        CreateActionBar(root, manager);

        manager.feedbackText = CreateText("FeedbackText", root, "", 30, TextAlignmentOptions.Center, Color.black);
        AddLayoutElement(manager.feedbackText.gameObject, -1, 56);

        manager.dragLayer = CreateUIObject("DragLayer", canvasRect);
        Stretch(manager.dragLayer);
        manager.dragLayer.SetAsLastSibling();

        RectTransform scoreAnchor = CreateUIObject("CenterScorePopupAnchor", manager.dragLayer);
        scoreAnchor.anchorMin = new Vector2(0.5f, 0.5f);
        scoreAnchor.anchorMax = new Vector2(0.5f, 0.5f);
        scoreAnchor.pivot = new Vector2(0.5f, 0.5f);
        scoreAnchor.anchoredPosition = new Vector2(0f, 40f);
        scoreAnchor.sizeDelta = new Vector2(10f, 10f);
        manager.scorePopupCenterAnchor = scoreAnchor;

        CreateSceneTemplates(canvasRect, manager, contentMode);
        CreateScorePopupTemplate(canvasRect, manager);
        CreatePanels(canvasRect, manager, contentMode, sampleSet);
        AddSampleQuestion(manager, contentMode, sampleSet);

        Selection.activeGameObject = managerObj;
        Debug.Log("Order Sort Drag FINAL layout scene created. Mode: " + contentMode + ", Bank: " + bankPlacementMode + ", Sample: " + sampleSet + ", No prefabs used.");
    }

    private static string GetDefaultQuestionText(OrderSortContentMode contentMode, SampleSet sampleSet)
    {
        if (sampleSet == SampleSet.MathExpressions)
            return "Arrange the math expressions from smallest to largest value.";

        return contentMode == OrderSortContentMode.ImageOnly
            ? "Arrange the pictures in alphabetical order by object name."
            : "Arrange the words in alphabetical order.";
    }

    private static void CreateHeader(RectTransform root, OrderSortDragManager manager, string instruction)
    {
        RectTransform header = CreatePanel("Header", root, new Color(0.98f, 0.93f, 0.88f, 1f));
        VerticalLayoutGroup headerVertical = header.gameObject.AddComponent<VerticalLayoutGroup>();
        headerVertical.padding = new RectOffset(24, 24, 16, 16);
        headerVertical.spacing = 12;
        headerVertical.childControlWidth = true;
        headerVertical.childControlHeight = false;
        headerVertical.childForceExpandHeight = false;
        AddLayoutElement(header.gameObject, -1, 186);

        RectTransform titleRow = CreateUIObject("TitleBar", header);
        HorizontalLayoutGroup titleLayout = titleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        titleLayout.spacing = 18;
        titleLayout.childAlignment = TextAnchor.MiddleCenter;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = true;
        titleLayout.childForceExpandWidth = false;
        titleLayout.childForceExpandHeight = false;
        AddLayoutElement(titleRow.gameObject, -1, 84);

        manager.pauseButton = CreateButton("PauseButton", titleRow, "Pause", 220f, 64f);

        RectTransform titlePanel = CreatePanel("GameTitlePanel", titleRow, new Color(1f, 0.98f, 0.96f, 1f));
        AddLayoutElement(titlePanel.gameObject, 960f, 78f, 1f, 0f);
        manager.gameTitleText = CreateText("GameTitleText", titlePanel, manager.gameTitle, 58, TextAlignmentOptions.Center, Color.black);
        manager.gameTitleText.enableAutoSizing = true;
        manager.gameTitleText.fontSizeMin = 36;
        manager.gameTitleText.fontSizeMax = 64;
        Stretch(manager.gameTitleText.GetComponent<RectTransform>());

        manager.howToPlayOpenButton = CreateButton("HowToPlayButton", titleRow, "How To Play", 240f, 64f);

        RectTransform statusRow = CreateUIObject("StatusBar", header);
        HorizontalLayoutGroup statusLayout = statusRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        statusLayout.spacing = 16;
        statusLayout.childAlignment = TextAnchor.MiddleCenter;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = true;
        statusLayout.childForceExpandWidth = false;
        statusLayout.childForceExpandHeight = false;
        AddLayoutElement(statusRow.gameObject, -1, 56);

        RectTransform scorePanel;
        RectTransform timePanel;
        RectTransform progressPanel;
        RectTransform instructionPanel;
        manager.scoreText = CreateInfoPill(statusRow, "ScorePanel", "Score: 0", 240f, out scorePanel);
        manager.timerText = CreateInfoPill(statusRow, "TimePanel", "Time: 0", 220f, out timePanel);
        manager.progressText = CreateInfoPill(statusRow, "ProgressPanel", "Question 1/1", 250f, out progressPanel);
        manager.progressText.gameObject.SetActive(false);
        progressPanel.gameObject.SetActive(false);
        manager.questionText = CreateInfoPill(statusRow, "InstructionPanel", instruction, 880f, out instructionPanel);
        manager.questionText.alignment = TextAlignmentOptions.Center;
        manager.questionText.enableAutoSizing = true;
        manager.questionText.fontSizeMin = 18;
        manager.questionText.fontSizeMax = 28;
        AddLayoutElement(instructionPanel.gameObject, 880f, 52f, 1f, 0f);
    }

    private static void CreateHorizontalGameplayArea(RectTransform root, OrderSortDragManager manager, OrderSortBankPlacementMode bankPlacementMode, OrderSortContentMode contentMode)
    {
        Vector2 cardSize = GetCardSize(manager, contentMode);
        float slotAreaHeight = contentMode == OrderSortContentMode.ImageOnly ? 182f : 172f;
        float bankAreaHeight = contentMode == OrderSortContentMode.ImageOnly ? 330f : 292f;

        RectTransform slotsWrapper = CreatePanel("SlotsArea", root, new Color(0.88f, 0.93f, 0.98f, 1f));
        AddLayoutElement(slotsWrapper.gameObject, -1, slotAreaHeight);

        RectTransform slotsBackground = CreatePanel("SlotsBackground", slotsWrapper, new Color(0.95f, 0.98f, 1f, 1f));
        StretchWithPadding(slotsBackground, 10f, 10f, 10f, 10f);

        manager.slotsParent = CreateUIObject("SlotsParent_Horizontal", slotsBackground);
        StretchWithPadding(manager.slotsParent, 16f, 16f, 16f, 16f);
        HorizontalLayoutGroup slotsLayout = manager.slotsParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        slotsLayout.padding = new RectOffset(4, 4, 4, 4);
        slotsLayout.spacing = 14;
        slotsLayout.childControlWidth = true;
        slotsLayout.childControlHeight = true;
        slotsLayout.childForceExpandWidth = true;
        slotsLayout.childForceExpandHeight = true;

        RectTransform cardArea;
        RectTransform bankWrapper = CreateBankContainer(GetBankName(bankPlacementMode, contentMode, "Bottom"), root, manager, bankPlacementMode, cardSize, out cardArea);
        manager.bankParent = cardArea;
        AddLayoutElement(bankWrapper.gameObject, -1, bankAreaHeight);
    }

    private static void CreateVerticalGameplayArea(RectTransform root, OrderSortDragManager manager, OrderSortBankPlacementMode bankPlacementMode, OrderSortContentMode contentMode)
    {
        Vector2 cardSize = GetCardSize(manager, contentMode);

        RectTransform gameplayRow = CreateUIObject("GameplayRow", root);
        HorizontalLayoutGroup rowLayout = gameplayRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 20;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        AddLayoutElement(gameplayRow.gameObject, -1, contentMode == OrderSortContentMode.ImageOnly ? 500f : 468f);

        RectTransform cardArea;
        RectTransform bankWrapper = CreateBankContainer(GetBankName(bankPlacementMode, contentMode, "Left"), gameplayRow, manager, bankPlacementMode, cardSize, out cardArea);
        manager.bankParent = cardArea;
        AddLayoutElement(bankWrapper.gameObject, 0f, 0f, 1f, 1f);

        RectTransform slotsWrapper = CreatePanel("SlotsArea_Right", gameplayRow, new Color(0.88f, 0.93f, 0.98f, 1f));
        AddLayoutElement(slotsWrapper.gameObject, 0f, 0f, 1f, 1f);
        RectTransform slotsBackground = CreatePanel("SlotsBackground", slotsWrapper, new Color(0.95f, 0.98f, 1f, 1f));
        StretchWithPadding(slotsBackground, 10f, 10f, 10f, 10f);

        manager.slotsParent = CreateUIObject("SlotsParent_RightVertical", slotsBackground);
        StretchWithPadding(manager.slotsParent, 16f, 16f, 16f, 16f);
        VerticalLayoutGroup slotsLayout = manager.slotsParent.gameObject.AddComponent<VerticalLayoutGroup>();
        slotsLayout.padding = new RectOffset(4, 4, 4, 4);
        slotsLayout.spacing = 14;
        slotsLayout.childControlWidth = true;
        slotsLayout.childControlHeight = true;
        slotsLayout.childForceExpandWidth = true;
        slotsLayout.childForceExpandHeight = true;
    }

    private static Vector2 GetCardSize(OrderSortDragManager manager, OrderSortContentMode contentMode)
    {
        return contentMode == OrderSortContentMode.ImageOnly ? manager.imageCardSize : manager.textCardSize;
    }

    private static Vector2 GetSlotSize(OrderSortDragManager manager, OrderSortContentMode contentMode)
    {
        return contentMode == OrderSortContentMode.ImageOnly ? manager.imageSlotSize : manager.textSlotSize;
    }

    private static RectTransform CreateBankContainer(string name, Transform parent, OrderSortDragManager manager, OrderSortBankPlacementMode bankPlacementMode, Vector2 cellSize, out RectTransform cardArea)
    {
        RectTransform wrapper = CreatePanel(name, parent, new Color(1f, 0.90f, 0.82f, 1f));

        RectTransform basketBackground = CreatePanel("BasketBackground", wrapper, new Color(1f, 0.97f, 0.93f, 1f));
        StretchWithPadding(basketBackground, 10f, 10f, 10f, 10f);

        RectTransform cardAreaFrame = CreatePanel("CardAreaFrame", basketBackground, new Color(1f, 0.94f, 0.88f, 1f));
        StretchWithPadding(cardAreaFrame, 18f, 18f, 18f, 18f);

        cardArea = CreatePanel("CardArea", cardAreaFrame, new Color(1f, 1f, 1f, 0.02f));
        StretchWithPadding(cardArea, 8f, 8f, 8f, 8f);

        OrderSortBankDropArea wrapperDrop = wrapper.gameObject.AddComponent<OrderSortBankDropArea>();
        wrapperDrop.Init(manager);

        OrderSortBankDropArea areaDrop = cardArea.gameObject.AddComponent<OrderSortBankDropArea>();
        areaDrop.Init(manager);

        if (bankPlacementMode == OrderSortBankPlacementMode.GridLayoutGroup)
        {
            GridLayoutGroup bankGrid = cardArea.gameObject.AddComponent<GridLayoutGroup>();
            bankGrid.cellSize = cellSize;
            bankGrid.spacing = new Vector2(14, 14);
            bankGrid.padding = new RectOffset(16, 16, 16, 16);
        }

        return wrapper;
    }

    private static string GetBankName(OrderSortBankPlacementMode bankPlacementMode, OrderSortContentMode contentMode, string position)
    {
        string contentName = contentMode == OrderSortContentMode.ImageOnly ? "ImageBasket" : "TextBank";
        string bankName = bankPlacementMode == OrderSortBankPlacementMode.OrganicRandom ? "Organic" : "Grid";
        return contentName + "Parent_" + bankName + "_" + position;
    }

    private static void CreateActionBar(RectTransform root, OrderSortDragManager manager)
    {
        RectTransform actionBar = CreateUIObject("ActionBar", root);
        HorizontalLayoutGroup actionLayout = actionBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 18;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.childControlWidth = false;
        actionLayout.childControlHeight = true;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;
        AddLayoutElement(actionBar.gameObject, -1, 68);

        manager.checkButton = CreateButton("CheckButton", actionBar, "Check", 260f, 62f);
        manager.nextButton = CreateButton("NextButton", actionBar, "Next", 220f, 62f);
        manager.nextButton.gameObject.SetActive(false);
    }

    private static void CreateSceneTemplates(RectTransform canvasRect, OrderSortDragManager manager, OrderSortContentMode contentMode)
    {
        RectTransform templateRoot = CreateUIObject("OrderSortSceneTemplates_DO_NOT_DELETE", canvasRect);
        templateRoot.gameObject.SetActive(false);

        Vector2 cardSize = GetCardSize(manager, contentMode);
        Vector2 slotSize = GetSlotSize(manager, contentMode);

        RectTransform card = CreateUIObject(contentMode == OrderSortContentMode.ImageOnly ? "ImageObjectSceneTemplate" : "TextCardSceneTemplate", templateRoot);
        card.sizeDelta = cardSize;
        Image cardBg = card.gameObject.AddComponent<Image>();
        cardBg.color = new Color(1f, 0.86f, 0.76f, 1f);

        CanvasGroup cardGroup = card.gameObject.AddComponent<CanvasGroup>();
        OrderSortDragItem dragItem = card.gameObject.AddComponent<OrderSortDragItem>();
        dragItem.canvasGroup = cardGroup;
        dragItem.cardBackgroundImage = cardBg;

        RectTransform cardFace = CreatePanel("CardFace", card, Color.white);
        dragItem.cardFaceImage = cardFace.GetComponent<Image>();
        StretchWithPadding(cardFace, 5f, 5f, 5f, 5f);

        RectTransform imageRect = CreateUIObject("ObjectImage", cardFace);
        StretchWithPadding(imageRect, 14f, 14f, 14f, 14f);
        Image objectImage = imageRect.gameObject.AddComponent<Image>();
        objectImage.preserveAspect = true;
        objectImage.raycastTarget = false;
        objectImage.gameObject.SetActive(contentMode == OrderSortContentMode.ImageOnly);
        dragItem.objectImage = objectImage;

        RectTransform cardLabelRect = CreateUIObject("TextLabel", cardFace);
        StretchWithPadding(cardLabelRect, 14f, 10f, 14f, 10f);
        TMP_Text cardLabel = cardLabelRect.gameObject.AddComponent<TextMeshProUGUI>();
        cardLabel.text = "Word";
        cardLabel.fontSize = 28;
        cardLabel.enableAutoSizing = true;
        cardLabel.fontSizeMin = 18;
        cardLabel.fontSizeMax = 32;
        cardLabel.alignment = TextAlignmentOptions.Center;
        cardLabel.color = Color.black;
        cardLabel.raycastTarget = false;
        cardLabel.gameObject.SetActive(contentMode == OrderSortContentMode.TextOnly);
        dragItem.labelText = cardLabel;

        LayoutElement cardLayout = card.gameObject.AddComponent<LayoutElement>();
        cardLayout.preferredWidth = cardSize.x;
        cardLayout.preferredHeight = cardSize.y;

        RectTransform slot = CreateUIObject("SlotSceneTemplate", templateRoot);
        slot.sizeDelta = slotSize;
        Image slotRaycastImage = slot.gameObject.AddComponent<Image>();
        slotRaycastImage.color = new Color(1f, 1f, 1f, 0.001f);

        OrderSortDropSlot dropSlot = slot.gameObject.AddComponent<OrderSortDropSlot>();

        RectTransform slotBackground = CreatePanel("SlotBackground", slot, new Color(0.79f, 0.87f, 1f, 1f));
        Stretch(slotBackground);
        dropSlot.backgroundImage = slotBackground.GetComponent<Image>();

        RectTransform itemHolder = CreateUIObject("ItemHolder", slotBackground);
        StretchWithPadding(itemHolder, 12f, 12f, 16f, 12f);
        dropSlot.itemHolder = itemHolder;

        RectTransform indexBadge = CreatePanel("IndexBadge", slotBackground, new Color(1f, 1f, 1f, 0.92f));
        indexBadge.anchorMin = new Vector2(0f, 1f);
        indexBadge.anchorMax = new Vector2(0f, 1f);
        indexBadge.pivot = new Vector2(0f, 1f);
        indexBadge.anchoredPosition = new Vector2(8f, -8f);
        indexBadge.sizeDelta = new Vector2(38f, 34f);
        dropSlot.indexBadgeBackground = indexBadge.GetComponent<Image>();

        RectTransform indexRect = CreateUIObject("IndexText", indexBadge);
        Stretch(indexRect);
        TMP_Text indexText = indexRect.gameObject.AddComponent<TextMeshProUGUI>();
        indexText.text = "1";
        indexText.fontSize = 20;
        indexText.alignment = TextAlignmentOptions.Center;
        indexText.color = new Color(0.35f, 0.35f, 0.35f, 1f);
        indexText.raycastTarget = false;
        dropSlot.indexText = indexText;

        RectTransform overlayRect = CreateUIObject("ResultOverlay", slotBackground);
        Stretch(overlayRect);
        Image overlay = overlayRect.gameObject.AddComponent<Image>();
        overlay.color = new Color(1f, 0.55f, 0.05f, 0f);
        overlay.raycastTarget = false;
        dropSlot.feedbackOverlay = overlay;
        overlayRect.SetAsLastSibling();

        LayoutElement slotLayout = slot.gameObject.AddComponent<LayoutElement>();
        slotLayout.preferredWidth = slotSize.x;
        slotLayout.preferredHeight = slotSize.y;

        manager.cardSceneTemplate = card;
        manager.slotSceneTemplate = slot;
    }

    private static void CreateScorePopupTemplate(RectTransform canvasRect, OrderSortDragManager manager)
    {
        TMP_Text popup = CreateText("ScorePopupSceneTemplate", canvasRect, "+10", 34, TextAlignmentOptions.Center, Color.green);
        popup.gameObject.AddComponent<CanvasGroup>();
        RectTransform rect = popup.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(130f, 60f);
        popup.gameObject.SetActive(false);
        manager.scorePopupSceneTemplate = popup;
    }

    private static void CreatePanels(RectTransform canvasRect, OrderSortDragManager manager, OrderSortContentMode contentMode, SampleSet sampleSet)
    {
        OverlayPanelParts howParts = CreateOverlayPanel("HowToPlayPanel", canvasRect, new Vector2(820f, 680f));
        manager.howToPlayPanel = howParts.panelRoot;

        CreateText("HowToTitle", howParts.rootCard, "How To Play", 42, TextAlignmentOptions.Center, Color.black);

        string howMessage = GetHowToMessage(contentMode, sampleSet);
        manager.howToPlayMessage = howMessage;
        manager.howToPlayText = CreateText("HowToPlayText", howParts.rootCard, howMessage, 28, TextAlignmentOptions.Center, Color.black);
        manager.howToPlayText.enableAutoSizing = true;
        manager.howToPlayText.fontSizeMin = 18;
        manager.howToPlayText.fontSizeMax = 28;
        AddLayoutElement(manager.howToPlayText.gameObject, -1, 128);

        RectTransform guideBox = CreatePanel("HowToGuideImageBox", howParts.rootCard, new Color(0.96f, 0.96f, 0.96f, 1f));
        AddLayoutElement(guideBox.gameObject, -1, 230);
        manager.howToPlayImageView = guideBox.GetComponent<Image>();
        manager.howToPlayImageView.preserveAspect = true;

        RectTransform howNav = CreateUIObject("HowToImageNav", howParts.rootCard);
        HorizontalLayoutGroup howNavLayout = howNav.gameObject.AddComponent<HorizontalLayoutGroup>();
        howNavLayout.spacing = 14;
        howNavLayout.childAlignment = TextAnchor.MiddleCenter;
        howNavLayout.childControlWidth = false;
        howNavLayout.childControlHeight = true;
        AddLayoutElement(howNav.gameObject, -1, 54);

        manager.howToPrevButton = CreateButton("HowToPrevButton", howNav, "Prev", 160f, 50f);
        manager.howToPlayCounterText = CreateText("HowToCounterText", howNav, "No guide image", 24, TextAlignmentOptions.Center, Color.black);
        AddLayoutElement(manager.howToPlayCounterText.gameObject, 180f, 50f);
        manager.howToNextButton = CreateButton("HowToNextButton", howNav, "Next", 160f, 50f);

        manager.howToPrimaryButton = CreateButton("HowToPrimaryButton", howParts.rootCard, "Start", 260f, 58f, out TMP_Text howButtonText);
        manager.howToPrimaryButtonText = howButtonText;

        OverlayPanelParts pauseParts = CreateOverlayPanel("PausePanel", canvasRect, new Vector2(620f, 360f));
        manager.pausePanel = pauseParts.panelRoot;
        CreateText("PauseTitle", pauseParts.rootCard, "Paused", 44, TextAlignmentOptions.Center, Color.black);
        CreateText("PauseMessage", pauseParts.rootCard, "Take a break. Resume when ready.", 26, TextAlignmentOptions.Center, Color.black);
        manager.resumeButton = CreateButton("ResumeButton", pauseParts.rootCard, "Resume", 260f, 58f);

        OverlayPanelParts resultParts = CreateOverlayPanel("ResultPanel", canvasRect, new Vector2(720f, 500f));
        manager.resultPanel = resultParts.panelRoot;
        CreateText("ResultTitle", resultParts.rootCard, "Result", 44, TextAlignmentOptions.Center, Color.black);
        manager.resultText = CreateText("ResultText", resultParts.rootCard, "Game Complete!\nScore: 0", 36, TextAlignmentOptions.Center, Color.black);
        AddLayoutElement(manager.resultText.gameObject, -1, 150);

        RectTransform resultButtons = CreateUIObject("ResultButtonRow", resultParts.rootCard);
        HorizontalLayoutGroup resultLayout = resultButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
        resultLayout.spacing = 18;
        resultLayout.childAlignment = TextAnchor.MiddleCenter;
        resultLayout.childControlWidth = false;
        resultLayout.childControlHeight = true;
        AddLayoutElement(resultButtons.gameObject, -1, 62);

        manager.continueButton = CreateButton("ContinueButton", resultButtons, "Continue", 240f, 58f);
        manager.restartButton = CreateButton("RestartButton", resultButtons, "Restart", 220f, 58f);
    }

    private static OverlayPanelParts CreateOverlayPanel(string panelName, RectTransform parent, Vector2 cardSize)
    {
        RectTransform panelRoot = CreatePanel(panelName, parent, new Color(0f, 0f, 0f, 0.72f));
        Stretch(panelRoot);
        CanvasGroup group = panelRoot.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
        group.interactable = true;

        RectTransform rootCard = CreatePanel("RootCard", panelRoot, Color.white);
        rootCard.anchorMin = new Vector2(0.5f, 0.5f);
        rootCard.anchorMax = new Vector2(0.5f, 0.5f);
        rootCard.pivot = new Vector2(0.5f, 0.5f);
        rootCard.anchoredPosition = Vector2.zero;
        rootCard.sizeDelta = cardSize;

        VerticalLayoutGroup cardLayout = rootCard.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(38, 38, 32, 32);
        cardLayout.spacing = 18;
        cardLayout.childAlignment = TextAnchor.MiddleCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = false;
        cardLayout.childForceExpandHeight = false;

        panelRoot.gameObject.SetActive(false);

        return new OverlayPanelParts
        {
            panelRoot = panelRoot.gameObject,
            rootCard = rootCard
        };
    }

    private static string GetHowToMessage(OrderSortContentMode contentMode, SampleSet sampleSet)
    {
        if (sampleSet == SampleSet.MathExpressions)
            return "Arrange the math expressions from smallest value to largest value.\nYou can swap two placed cards. Press Check to score each position.";

        if (contentMode == OrderSortContentMode.ImageOnly)
            return "Drag each picture into the correct slot order.\nYou can swap two placed pictures. Press Check to see correct, wrong, and empty feedback.";

        return "Drag word cards into the correct order.\nYou can swap two placed cards. Press Check to score each position.";
    }

    private static void AddSampleQuestion(OrderSortDragManager manager, OrderSortContentMode contentMode, SampleSet sampleSet)
    {
        OrderSortQuestion question = new OrderSortQuestion();

        if (sampleSet == SampleSet.MathExpressions)
        {
            question.questionText = "Arrange the math expressions from smallest to largest value.";
            question.sortRule = OrderSortRule.ManualOrder;
            question.comparisonMode = OrderSortComparisonMode.NumericValue;
            question.items.Add(new OrderSortItemData { value = "√1" });
            question.items.Add(new OrderSortItemData { value = "√4" });
            question.items.Add(new OrderSortItemData { value = "√9" });
            question.manualCorrectOrder.Add("√1");
            question.manualCorrectOrder.Add("√4");
            question.manualCorrectOrder.Add("√9");
        }
        else if (contentMode == OrderSortContentMode.ImageOnly)
        {
            question.questionText = "Arrange the pictures in alphabetical order by object name.";
            question.sortRule = OrderSortRule.AlphabeticalAZ;
            question.items.Add(new OrderSortItemData { value = "Apple" });
            question.items.Add(new OrderSortItemData { value = "Ball" });
            question.items.Add(new OrderSortItemData { value = "Cat" });
            question.items.Add(new OrderSortItemData { value = "Dog" });
            question.items.Add(new OrderSortItemData { value = "Egg" });
        }
        else
        {
            question.questionText = "Arrange the words in alphabetical order.";
            question.sortRule = OrderSortRule.AlphabeticalAZ;
            question.items.Add(new OrderSortItemData { value = "Apple" });
            question.items.Add(new OrderSortItemData { value = "Anaconda" });
            question.items.Add(new OrderSortItemData { value = "Aniket" });
            question.items.Add(new OrderSortItemData { value = "Boland" });
            question.items.Add(new OrderSortItemData { value = "Appy" });
            question.items.Add(new OrderSortItemData { value = "Ant" });
            question.items.Add(new OrderSortItemData { value = "Banana" });
        }

        manager.questions.Add(question);
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObj = new GameObject("OrderSortCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystemIfNeeded()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static RectTransform CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform panel = CreateUIObject(name, parent);
        Image image = panel.gameObject.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateUIObject(name, parent);
        TMP_Text tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TMP_Text CreateInfoPill(Transform parent, string name, string value, float preferredWidth, out RectTransform panel)
    {
        panel = CreatePanel(name, parent, new Color(1f, 0.98f, 0.96f, 1f));
        AddLayoutElement(panel.gameObject, preferredWidth, 52f);
        TMP_Text text = CreateText(name + "Text", panel, value, 24, TextAlignmentOptions.Center, Color.black);
        text.enableAutoSizing = true;
        text.fontSizeMin = 18;
        text.fontSizeMax = 28;
        StretchWithPadding(text.GetComponent<RectTransform>(), 12f, 8f, 12f, 8f);
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, float preferredWidth, float preferredHeight)
    {
        return CreateButton(name, parent, label, preferredWidth, preferredHeight, out _);
    }

    private static Button CreateButton(string name, Transform parent, string label, float preferredWidth, float preferredHeight, out TMP_Text labelText)
    {
        RectTransform rect = CreateUIObject(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.45f, 0.95f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        AddLayoutElement(rect.gameObject, preferredWidth, preferredHeight);

        labelText = CreateText("Text", rect, label, 28, TextAlignmentOptions.Center, Color.white);
        labelText.raycastTarget = false;
        Stretch(labelText.GetComponent<RectTransform>());

        return button;
    }

    private static void AddLayoutElement(GameObject obj, float preferredWidth, float preferredHeight)
    {
        AddLayoutElement(obj, preferredWidth, preferredHeight, 0f, 0f);
    }

    private static void AddLayoutElement(GameObject obj, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
    {
        LayoutElement layout = obj.GetComponent<LayoutElement>();

        if (layout == null)
            layout = obj.AddComponent<LayoutElement>();

        if (preferredWidth > 0)
            layout.preferredWidth = preferredWidth;

        if (preferredHeight > 0)
            layout.preferredHeight = preferredHeight;

        layout.flexibleWidth = flexibleWidth;
        layout.flexibleHeight = flexibleHeight;
    }

    private static void StretchWithPadding(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
