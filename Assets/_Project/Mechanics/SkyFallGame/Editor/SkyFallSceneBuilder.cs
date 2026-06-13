#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SkyFallSceneBuilder
{
    private static readonly Color TopHudColor = new Color(0.08f, 0.12f, 0.24f, 0.92f);
    private static readonly Color PanelColor = new Color(0.12f, 0.16f, 0.31f, 0.96f);
    private static readonly Color PlayAreaColor = new Color(0.58f, 0.80f, 1f, 1f);

    [MenuItem("Tools/SkyFall/Create Final Production Math Scene")]
    public static void CreateFinalProductionMathScene()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("SkyFallCanvas_Final", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create SkyFall Final Scene");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        SkyFallFontThemeApplier fontTheme = canvasObject.AddComponent<SkyFallFontThemeApplier>();

        RectTransform safeArea = CreateRect("SafeArea", canvasRect);
        Stretch(safeArea);
        safeArea.gameObject.AddComponent<SkyFallSafeAreaFitter>();

        RectTransform backgroundLayer = CreateLayer("BackgroundLayer", safeArea);
        RectTransform gameplayLayer = CreateLayer("GameplayLayer", safeArea);
        RectTransform trailFxLayer = CreateLayer("TrailFxLayer", safeArea);
        RectTransform hudLayer = CreateLayer("HUDLayer", safeArea);
        RectTransform overlayLayer = CreateLayer("OverlayLayer", safeArea);

        CreateBackground(backgroundLayer);

        TMP_Text scoreText;
        TMP_Text questionText;
        GameObject timerGroup;
        TMP_Text timerText;
        GameObject livesGroup;
        RectTransform livesIconParent;
        Image lifeIconPrefab;
        Button pauseButton;

        BuildTopHud(
            hudLayer,
            fontTheme,
            out scoreText,
            out questionText,
            out timerGroup,
            out timerText,
            out livesGroup,
            out livesIconParent,
            out lifeIconPrefab,
            out pauseButton
        );

        RectTransform playArea = BuildPlayArea(gameplayLayer);
        RectTransform itemsLayer = CreateLayer("ItemsLayer", playArea);

        RectTransform carrier = BuildCarrier(playArea, trailFxLayer);
        RectTransform carrierVisual = carrier.Find("CarrierDirectionVisual") as RectTransform;

        RectTransform basket = BuildBasket(playArea, trailFxLayer);

        SkyFallBasketDrag basketDrag = playArea.gameObject.AddComponent<SkyFallBasketDrag>();
        basketDrag.playArea = playArea;
        basketDrag.basket = basket;
        basketDrag.requireStartOnBasket = true;
        basketDrag.useSmoothMovement = true;
        basketDrag.followSpeed = 22f;

        SkyFallFallingItem fallingItemPrefab = BuildFallingItemPrefab(itemsLayer, trailFxLayer, fontTheme);

        TMP_Text feedbackText;
        RectTransform feedbackCard;
        BuildFeedback(overlayLayer, fontTheme, out feedbackText, out feedbackCard);

        SkyFallUiPanelAnimator resultAnimator;
        TMP_Text resultTitle;
        TMP_Text resultScore;
        Button restartButton;
        RectTransform resultPanel = BuildResultPanel(overlayLayer, fontTheme, out resultAnimator, out resultTitle, out resultScore, out restartButton);

        SkyFallScreenFlowController flowController = BuildScreenFlow(overlayLayer, pauseButton, fontTheme);

        GameObject managerObject = new GameObject("SkyFallGameManager", typeof(RectTransform));
        managerObject.transform.SetParent(safeArea, false);

        SkyFallGameManager manager = managerObject.AddComponent<SkyFallGameManager>();
        SkyFallMathContentProvider mathProvider = managerObject.AddComponent<SkyFallMathContentProvider>();
        AudioSource sfxSource = managerObject.AddComponent<AudioSource>();
        AudioSource musicSource = managerObject.AddComponent<AudioSource>();

        manager.contentProvider = mathProvider;
        manager.sfxSource = sfxSource;
        manager.musicSource = musicSource;

        manager.playArea = playArea;
        manager.carrier = carrier;
        manager.carrierDirectionVisual = carrierVisual;
        manager.basket = basket;
        manager.itemParent = itemsLayer;
        manager.trailFxLayer = trailFxLayer;
        manager.itemPrefab = fallingItemPrefab;

        manager.questionText = questionText;
        manager.scoreText = scoreText;
        manager.timerGroup = timerGroup;
        manager.timerText = timerText;
        manager.livesGroup = livesGroup;
        manager.livesIconParent = livesIconParent;
        manager.lifeIconPrefab = lifeIconPrefab;

        manager.feedbackText = feedbackText;
        manager.feedbackCard = feedbackCard;

        manager.resultPanel = resultPanel.gameObject;
        manager.resultPanelAnimator = resultAnimator;
        manager.resultTitleText = resultTitle;
        manager.resultScoreText = resultScore;
        manager.restartButton = restartButton;

        manager.gameOverMode = SkyFallGameOverMode.TimeLimited;
        manager.dropSpawnMode = SkyFallDropSpawnMode.SingleActiveItem;
        manager.autoStart = false;

        flowController.gameManager = manager;

        fontTheme.ApplyFonts();

        Selection.activeGameObject = canvasObject;
        Debug.Log("SkyFall final production scene created. Assign fonts on SkyFallCanvas_Final and guide images on HowToPlayPanelRoot.");
    }

    private static void CreateBackground(RectTransform parent)
    {
        RectTransform bg = CreatePanel("Background", parent, PlayAreaColor, false);
        Stretch(bg);
    }

    private static void BuildTopHud(
        RectTransform hudLayer,
        SkyFallFontThemeApplier fontTheme,
        out TMP_Text scoreText,
        out TMP_Text questionText,
        out GameObject timerGroup,
        out TMP_Text timerText,
        out GameObject livesGroup,
        out RectTransform livesIconParent,
        out Image lifeIconPrefab,
        out Button pauseButton)
    {
        RectTransform root = CreatePanel("TopHUDRoot", hudLayer, TopHudColor, true);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(1f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(0f, 88f);
        root.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 12, 12);
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        RectTransform scoreGroup = CreateHudCard("ScoreGroup", root, 240f);
        TMP_Text scoreLabel = CreateTMP("ScoreLabel", scoreGroup, "SCORE", 22, TextAlignmentOptions.MidlineLeft, Color.white);
        scoreLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        scoreLabel.rectTransform.anchorMax = new Vector2(0.45f, 1f);
        scoreLabel.rectTransform.offsetMin = new Vector2(18f, 0f);
        scoreLabel.rectTransform.offsetMax = Vector2.zero;
        fontTheme.secondaryTexts.Add(scoreLabel);

        scoreText = CreateTMP("ScoreText", scoreGroup, "0", 34, TextAlignmentOptions.MidlineRight, Color.white);
        scoreText.rectTransform.anchorMin = new Vector2(0.45f, 0f);
        scoreText.rectTransform.anchorMax = new Vector2(1f, 1f);
        scoreText.rectTransform.offsetMin = Vector2.zero;
        scoreText.rectTransform.offsetMax = new Vector2(-18f, 0f);
        fontTheme.secondaryTexts.Add(scoreText);

        RectTransform questionGroup = CreateHudCard("QuestionGroup", root, 900f);
        LayoutElement qElement = questionGroup.GetComponent<LayoutElement>();
        qElement.flexibleWidth = 1f;

        questionText = CreateTMP("QuestionText", questionGroup, "Catch only EVEN numbers", 34, TextAlignmentOptions.Center, Color.white);
        Stretch(questionText.rectTransform, 16f, 6f, 16f, 6f);
        questionText.enableAutoSizing = true;
        questionText.fontSizeMin = 24f;
        questionText.fontSizeMax = 38f;
        fontTheme.primaryTexts.Add(questionText);

        RectTransform modeInfo = CreateHudCard("ModeInfoGroup", root, 270f);

        RectTransform timerRect = CreateRect("TimerGroup", modeInfo);
        Stretch(timerRect);
        timerGroup = timerRect.gameObject;

        TMP_Text timerLabel = CreateTMP("TimerLabel", timerRect, "TIME", 22, TextAlignmentOptions.MidlineLeft, Color.white);
        timerLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        timerLabel.rectTransform.anchorMax = new Vector2(0.45f, 1f);
        timerLabel.rectTransform.offsetMin = new Vector2(18f, 0f);
        timerLabel.rectTransform.offsetMax = Vector2.zero;
        fontTheme.secondaryTexts.Add(timerLabel);

        timerText = CreateTMP("TimerText", timerRect, "60", 34, TextAlignmentOptions.MidlineRight, Color.white);
        timerText.rectTransform.anchorMin = new Vector2(0.45f, 0f);
        timerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        timerText.rectTransform.offsetMin = Vector2.zero;
        timerText.rectTransform.offsetMax = new Vector2(-18f, 0f);
        fontTheme.secondaryTexts.Add(timerText);

        RectTransform livesRect = CreateRect("LivesGroup", modeInfo);
        Stretch(livesRect);
        livesGroup = livesRect.gameObject;

        TMP_Text livesLabel = CreateTMP("LivesLabel", livesRect, "LIVES", 22, TextAlignmentOptions.MidlineLeft, Color.white);
        livesLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        livesLabel.rectTransform.anchorMax = new Vector2(0.42f, 1f);
        livesLabel.rectTransform.offsetMin = new Vector2(18f, 0f);
        livesLabel.rectTransform.offsetMax = Vector2.zero;
        fontTheme.secondaryTexts.Add(livesLabel);

        livesIconParent = CreateRect("LivesIconsContainer", livesRect);
        livesIconParent.anchorMin = new Vector2(0.42f, 0f);
        livesIconParent.anchorMax = new Vector2(1f, 1f);
        livesIconParent.offsetMin = Vector2.zero;
        livesIconParent.offsetMax = new Vector2(-12f, 0f);

        HorizontalLayoutGroup lifeLayout = livesIconParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        lifeLayout.spacing = 8f;
        lifeLayout.childAlignment = TextAnchor.MiddleRight;
        lifeLayout.childForceExpandWidth = false;
        lifeLayout.childForceExpandHeight = false;

        RectTransform lifePrefabRoot = CreatePanel("LifeIconPrefab", livesRect, new Color(1f, 0.25f, 0.36f, 1f), false);
        lifePrefabRoot.anchorMin = new Vector2(0.5f, 0.5f);
        lifePrefabRoot.anchorMax = new Vector2(0.5f, 0.5f);
        lifePrefabRoot.sizeDelta = new Vector2(32f, 32f);
        lifePrefabRoot.gameObject.SetActive(false);
        lifeIconPrefab = lifePrefabRoot.GetComponent<Image>();

        RectTransform pauseRoot = CreatePanel("PauseButtonRoot", root, new Color(0.17f, 0.21f, 0.39f, 1f), true);
        LayoutElement pauseLayout = pauseRoot.gameObject.AddComponent<LayoutElement>();
        pauseLayout.preferredWidth = 76f;
        pauseLayout.minWidth = 76f;
        pauseLayout.flexibleWidth = 0f;

        pauseButton = pauseRoot.gameObject.AddComponent<Button>();
        pauseButton.targetGraphic = pauseRoot.GetComponent<Image>();

        TMP_Text pauseText = CreateTMP("PauseIconText", pauseRoot, "II", 32, TextAlignmentOptions.Center, Color.white);
        Stretch(pauseText.rectTransform);
        pauseText.fontStyle = FontStyles.Bold;
        fontTheme.primaryTexts.Add(pauseText);

        livesGroup.SetActive(false);
    }

    private static RectTransform BuildPlayArea(RectTransform gameplayLayer)
    {
        RectTransform playArea = CreatePanel("PlayArea", gameplayLayer, new Color(0.58f, 0.80f, 1f, 1f), true);
        playArea.anchorMin = new Vector2(0f, 0f);
        playArea.anchorMax = new Vector2(1f, 1f);
        playArea.offsetMin = Vector2.zero;
        playArea.offsetMax = new Vector2(0f, -88f);
        playArea.GetComponent<Image>().raycastTarget = true;
        return playArea;
    }

    private static RectTransform BuildCarrier(RectTransform playArea, RectTransform trailFxLayer)
    {
        RectTransform carrier = CreateRect("FlyingCarrier", playArea);
        carrier.anchorMin = new Vector2(0.5f, 0.5f);
        carrier.anchorMax = new Vector2(0.5f, 0.5f);
        carrier.sizeDelta = new Vector2(190f, 90f);
        carrier.anchoredPosition = new Vector2(-600f, 300f);

        RectTransform visual = CreatePanel("CarrierDirectionVisual", carrier, new Color(1f, 0.72f, 0.22f, 1f), true);
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.sizeDelta = new Vector2(180f, 78f);
        visual.anchoredPosition = Vector2.zero;

        TMP_Text label = CreateTMP("CarrierPlaceholderText", visual, "FLYER", 22, TextAlignmentOptions.Center, Color.white);
        Stretch(label.rectTransform);

        RectTransform trailAnchor = CreateRect("FlyerTrailAnchor", visual);
        trailAnchor.anchorMin = new Vector2(0.5f, 0.5f);
        trailAnchor.anchorMax = new Vector2(0.5f, 0.5f);
        trailAnchor.sizeDelta = new Vector2(30f, 30f);
        trailAnchor.anchoredPosition = new Vector2(-72f, 0f);

        SkyFallUiTrailEmitter emitter = trailAnchor.gameObject.AddComponent<SkyFallUiTrailEmitter>();
        emitter.source = trailAnchor;
        emitter.emissionSpace = trailFxLayer;
        emitter.emissionMode = SkyFallTrailEmissionMode.Always;
        emitter.emissionRate = 22f;
        emitter.lifeTime = 0.55f;
        emitter.startSize = 18f;
        emitter.endSize = 2f;
        emitter.maxParticles = 70;
        emitter.randomSpawnOffset = new Vector2(10f, 8f);
        emitter.driftMin = new Vector2(-30f, -8f);
        emitter.driftMax = new Vector2(6f, 18f);
        emitter.startColor = new Color(0.75f, 0.95f, 1f, 0.72f);
        emitter.endColor = new Color(0.75f, 0.95f, 1f, 0f);

        return carrier;
    }

    private static RectTransform BuildBasket(RectTransform playArea, RectTransform trailFxLayer)
    {
        RectTransform basketRoot = CreateRect("BasketRoot", playArea);
        basketRoot.anchorMin = new Vector2(0.5f, 0f);
        basketRoot.anchorMax = new Vector2(0.5f, 0f);
        basketRoot.sizeDelta = new Vector2(280f, 120f);
        basketRoot.anchoredPosition = new Vector2(0f, 80f);

        RectTransform card = CreatePanel("BasketCard", basketRoot, new Color(0.16f, 0.56f, 0.34f, 1f), true);
        Stretch(card);

        RectTransform visual = CreatePanel("BasketVisual", card, new Color(0.23f, 0.70f, 0.42f, 1f), true);
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.sizeDelta = new Vector2(250f, 90f);
        visual.anchoredPosition = Vector2.zero;

        TMP_Text label = CreateTMP("BasketPlaceholderText", visual, "BASKET", 28, TextAlignmentOptions.Center, Color.white);
        Stretch(label.rectTransform);

        RectTransform trailAnchor = CreateRect("BasketTrailAnchor", visual);
        trailAnchor.anchorMin = new Vector2(0.5f, 0.5f);
        trailAnchor.anchorMax = new Vector2(0.5f, 0.5f);
        trailAnchor.sizeDelta = new Vector2(40f, 28f);
        trailAnchor.anchoredPosition = new Vector2(0f, -8f);

        SkyFallUiTrailEmitter emitter = trailAnchor.gameObject.AddComponent<SkyFallUiTrailEmitter>();
        emitter.source = basketRoot;
        emitter.emissionSpace = trailFxLayer;
        emitter.emissionMode = SkyFallTrailEmissionMode.WhileMoving;
        emitter.movementThreshold = 45f;
        emitter.emissionRate = 28f;
        emitter.lifeTime = 0.28f;
        emitter.startSize = 14f;
        emitter.endSize = 1f;
        emitter.maxParticles = 40;
        emitter.randomSpawnOffset = new Vector2(65f, 8f);
        emitter.driftMin = new Vector2(-6f, -18f);
        emitter.driftMax = new Vector2(6f, 8f);
        emitter.startColor = new Color(1f, 1f, 1f, 0.45f);
        emitter.endColor = new Color(1f, 1f, 1f, 0f);

        return basketRoot;
    }

    private static SkyFallFallingItem BuildFallingItemPrefab(RectTransform itemLayer, RectTransform trailFxLayer, SkyFallFontThemeApplier fontTheme)
    {
        RectTransform root = CreateRect("FallingItemPrefab", itemLayer);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(140f, 120f);
        root.anchoredPosition = Vector2.zero;
        CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

        RectTransform hitBox = CreatePanel("CatchHitBox", root, new Color(1f, 0f, 0f, 0.08f), false);
        hitBox.anchorMin = new Vector2(0.5f, 0.5f);
        hitBox.anchorMax = new Vector2(0.5f, 0.5f);
        hitBox.sizeDelta = new Vector2(130f, 120f);
        hitBox.anchoredPosition = Vector2.zero;

        RectTransform visualRoot = CreateRect("VisualRoot", root);
        Stretch(visualRoot);

        RectTransform fallingTrail = CreateRect("FallingTrailAnchor", visualRoot);
        fallingTrail.anchorMin = new Vector2(0.5f, 0.5f);
        fallingTrail.anchorMax = new Vector2(0.5f, 0.5f);
        fallingTrail.sizeDelta = new Vector2(30f, 30f);
        fallingTrail.anchoredPosition = new Vector2(0f, 38f);

        SkyFallUiTrailEmitter trailEmitter = fallingTrail.gameObject.AddComponent<SkyFallUiTrailEmitter>();
        trailEmitter.source = fallingTrail;
        trailEmitter.emissionSpace = trailFxLayer;
        trailEmitter.emissionMode = SkyFallTrailEmissionMode.Always;
        trailEmitter.emissionRate = 12f;
        trailEmitter.lifeTime = 0.32f;
        trailEmitter.startSize = 12f;
        trailEmitter.endSize = 1f;
        trailEmitter.maxParticles = 35;
        trailEmitter.startColor = new Color(1f, 0.92f, 0.55f, 0.55f);
        trailEmitter.endColor = new Color(1f, 0.92f, 0.55f, 0f);

        RectTransform outerCard = CreatePanel("OuterCard", visualRoot, new Color(0.72f, 0.47f, 0.23f, 1f), true);
        outerCard.anchorMin = new Vector2(0.5f, 0.5f);
        outerCard.anchorMax = new Vector2(0.5f, 0.5f);
        outerCard.sizeDelta = new Vector2(140f, 120f);
        outerCard.anchoredPosition = Vector2.zero;

        RectTransform innerCard = CreatePanel("InnerCard", outerCard, new Color(1f, 0.93f, 0.72f, 1f), true);
        innerCard.anchorMin = new Vector2(0.5f, 0.5f);
        innerCard.anchorMax = new Vector2(0.5f, 0.5f);
        innerCard.sizeDelta = new Vector2(122f, 102f);
        innerCard.anchoredPosition = Vector2.zero;

        RectTransform icon = CreatePanel("ItemIcon", innerCard, Color.white, false);
        icon.anchorMin = new Vector2(0.5f, 0.5f);
        icon.anchorMax = new Vector2(0.5f, 0.5f);
        icon.sizeDelta = new Vector2(70f, 70f);
        icon.anchoredPosition = Vector2.zero;
        icon.gameObject.SetActive(false);

        TMP_Text itemText = CreateTMP("ItemText", innerCard, "2", 44, TextAlignmentOptions.Center, new Color(0.20f, 0.12f, 0.08f, 1f));
        Stretch(itemText.rectTransform, 10f, 6f, 10f, 6f);
        itemText.enableAutoSizing = true;
        itemText.fontSizeMin = 28f;
        itemText.fontSizeMax = 50f;
        fontTheme.primaryTexts.Add(itemText);

        SkyFallFallingItem item = root.gameObject.AddComponent<SkyFallFallingItem>();
        item.rectTransform = root;
        item.catchHitBox = hitBox;
        item.visualRoot = visualRoot;
        item.outerCard = outerCard;
        item.innerCard = innerCard;
        item.outerCardImage = outerCard.GetComponent<Image>();
        item.innerCardImage = innerCard.GetComponent<Image>();
        item.iconImage = icon.GetComponent<Image>();
        item.labelText = itemText;
        item.canvasGroup = canvasGroup;
        item.trailEmitter = trailEmitter;

        root.gameObject.SetActive(false);
        return item;
    }

    private static void BuildFeedback(RectTransform overlayLayer, SkyFallFontThemeApplier fontTheme, out TMP_Text feedbackText, out RectTransform feedbackCard)
    {
        RectTransform root = CreateRect("FeedbackPanelRoot", overlayLayer);
        Stretch(root);

        feedbackCard = CreatePanel("FeedbackCard", root, new Color(0f, 0f, 0f, 0f), false);
        feedbackCard.anchorMin = new Vector2(0.5f, 0.5f);
        feedbackCard.anchorMax = new Vector2(0.5f, 0.5f);
        feedbackCard.sizeDelta = new Vector2(260f, 90f);
        feedbackCard.anchoredPosition = new Vector2(0f, 120f);
        feedbackCard.gameObject.AddComponent<CanvasGroup>();

        feedbackText = CreateTMP("FeedbackText", feedbackCard, "+10", 58, TextAlignmentOptions.Center, Color.white);
        Stretch(feedbackText.rectTransform);
        feedbackText.fontStyle = FontStyles.Bold;
        fontTheme.primaryTexts.Add(feedbackText);

        feedbackCard.gameObject.SetActive(false);
    }

    private static RectTransform BuildResultPanel(RectTransform overlayLayer, SkyFallFontThemeApplier fontTheme, out SkyFallUiPanelAnimator animator, out TMP_Text title, out TMP_Text score, out Button restart)
    {
        RectTransform root = CreateFullOverlay("ResultPanelRoot", overlayLayer, new Color(0f, 0f, 0f, 0.68f));

        RectTransform card = CreatePanel("ResultCard", root, PanelColor, true);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(620f, 470f);
        card.anchoredPosition = Vector2.zero;

        animator = root.gameObject.AddComponent<SkyFallUiPanelAnimator>();
        animator.canvasGroup = root.GetComponent<CanvasGroup>();
        animator.cardRoot = card;

        title = CreateTMP("ResultTitleText", card, "Game Over", 58, TextAlignmentOptions.Center, Color.white);
        title.rectTransform.anchorMin = new Vector2(0f, 0.72f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(30f, 0f);
        title.rectTransform.offsetMax = new Vector2(-30f, -20f);
        title.fontStyle = FontStyles.Bold;
        fontTheme.primaryTexts.Add(title);

        score = CreateTMP("ResultScoreText", card, "Score: 0", 34, TextAlignmentOptions.Center, Color.white);
        score.rectTransform.anchorMin = new Vector2(0f, 0.28f);
        score.rectTransform.anchorMax = new Vector2(1f, 0.72f);
        score.rectTransform.offsetMin = new Vector2(30f, 0f);
        score.rectTransform.offsetMax = new Vector2(-30f, 0f);
        fontTheme.secondaryTexts.Add(score);

        restart = CreateButton("RestartButton", card, "RESTART", new Vector2(0f, -160f), new Vector2(300f, 82f), new Color(0.18f, 0.60f, 0.36f, 1f), fontTheme);

        root.gameObject.SetActive(false);
        return root;
    }

    private static SkyFallScreenFlowController BuildScreenFlow(RectTransform overlayLayer, Button pauseButton, SkyFallFontThemeApplier fontTheme)
    {
        RectTransform flowRoot = CreateRect("SkyFallScreenFlowController", overlayLayer);
        SkyFallScreenFlowController flow = flowRoot.gameObject.AddComponent<SkyFallScreenFlowController>();
        flow.pauseButton = pauseButton;
        flow.gameTitle = "SkyFall";
        flow.showLoadingScreen = true;
        flow.loadingDuration = 0.9f;
        flow.showHowToPlayBeforeFirstGame = true;

        BuildLoadingPanel(overlayLayer, flow, fontTheme);
        BuildHowToPlayPanel(overlayLayer, flow, fontTheme);
        BuildPausePanel(overlayLayer, flow, fontTheme);

        return flow;
    }

    private static void BuildLoadingPanel(RectTransform overlayLayer, SkyFallScreenFlowController flow, SkyFallFontThemeApplier fontTheme)
    {
        RectTransform root = CreateFullOverlay("LoadingPanelRoot", overlayLayer, new Color(0.04f, 0.06f, 0.12f, 0.96f));

        RectTransform card = CreateRect("LoadingCard", root);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(760f, 320f);
        card.anchoredPosition = Vector2.zero;

        SkyFallUiPanelAnimator animator = root.gameObject.AddComponent<SkyFallUiPanelAnimator>();
        animator.canvasGroup = root.GetComponent<CanvasGroup>();
        animator.cardRoot = card;

        TMP_Text title = CreateTMP("GameTitleText", card, "SkyFall", 82, TextAlignmentOptions.Center, Color.white);
        title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;
        title.fontStyle = FontStyles.Bold;
        fontTheme.primaryTexts.Add(title);

        RectTransform sliderRoot = CreateRect("LoadingSlider", card);
        sliderRoot.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRoot.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRoot.sizeDelta = new Vector2(540f, 32f);
        sliderRoot.anchoredPosition = new Vector2(0f, -75f);

        Slider slider = sliderRoot.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        RectTransform bg = CreatePanel("Background", sliderRoot, new Color(1f, 1f, 1f, 0.22f), true);
        Stretch(bg);

        RectTransform fillArea = CreateRect("Fill Area", sliderRoot);
        Stretch(fillArea);

        RectTransform fill = CreatePanel("Fill", fillArea, new Color(0.55f, 0.85f, 1f, 1f), true);
        Stretch(fill);

        slider.targetGraphic = bg.GetComponent<Image>();
        slider.fillRect = fill;

        flow.loadingPanel = animator;
        flow.loadingGameTitleText = title;
        flow.loadingSlider = slider;

        root.gameObject.SetActive(false);
    }

    private static void BuildHowToPlayPanel(RectTransform overlayLayer, SkyFallScreenFlowController flow, SkyFallFontThemeApplier fontTheme)
    {
        RectTransform root = CreateFullOverlay("HowToPlayPanelRoot", overlayLayer, new Color(0f, 0f, 0f, 0.68f));

        RectTransform card = CreatePanel("HowToPlayCard", root, new Color(0.98f, 0.94f, 0.82f, 0.98f), true);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(900f, 610f);
        card.anchoredPosition = Vector2.zero;

        SkyFallUiPanelAnimator animator = root.gameObject.AddComponent<SkyFallUiPanelAnimator>();
        animator.canvasGroup = root.GetComponent<CanvasGroup>();
        animator.cardRoot = card;

        TMP_Text title = CreateTMP("HowToPlayTitleText", card, "How To Play", 52, TextAlignmentOptions.Center, new Color(0.18f, 0.15f, 0.26f, 1f));
        title.rectTransform.anchorMin = new Vector2(0f, 0.86f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(30f, 0f);
        title.rectTransform.offsetMax = new Vector2(-30f, -12f);
        title.fontStyle = FontStyles.Bold;
        fontTheme.primaryTexts.Add(title);

        RectTransform imageFrame = CreatePanel("GuideImageFrame", card, Color.white, true);
        imageFrame.anchorMin = new Vector2(0.5f, 0.5f);
        imageFrame.anchorMax = new Vector2(0.5f, 0.5f);
        imageFrame.sizeDelta = new Vector2(650f, 330f);
        imageFrame.anchoredPosition = new Vector2(0f, 35f);

        RectTransform guideImageRect = CreatePanel("GuideImage", imageFrame, new Color(0.86f, 0.91f, 1f, 1f), true);
        Stretch(guideImageRect, 18f, 18f, 18f, 18f);
        Image guideImage = guideImageRect.GetComponent<Image>();
        guideImage.preserveAspect = true;

        TMP_Text emptyText = CreateTMP("EmptyGuideMessageText", imageFrame, "Assign guide images in Inspector", 30, TextAlignmentOptions.Center, new Color(0.25f, 0.25f, 0.32f, 1f));
        Stretch(emptyText.rectTransform, 30f, 30f, 30f, 30f);
        fontTheme.secondaryTexts.Add(emptyText);

        TMP_Text counter = CreateTMP("GuidePageCounterText", card, "0 / 0", 26, TextAlignmentOptions.Center, new Color(0.20f, 0.17f, 0.27f, 1f));
        counter.rectTransform.anchorMin = new Vector2(0.4f, 0.18f);
        counter.rectTransform.anchorMax = new Vector2(0.6f, 0.26f);
        counter.rectTransform.offsetMin = Vector2.zero;
        counter.rectTransform.offsetMax = Vector2.zero;
        fontTheme.secondaryTexts.Add(counter);

        Button prev = CreateButton("GuidePreviousButton", card, "<", new Vector2(-390f, 35f), new Vector2(90f, 90f), new Color(0.23f, 0.24f, 0.44f, 1f), fontTheme);
        Button next = CreateButton("GuideNextButton", card, ">", new Vector2(390f, 35f), new Vector2(90f, 90f), new Color(0.23f, 0.24f, 0.44f, 1f), fontTheme);
        Button start = CreateButton("GuideStartButton", card, "START", new Vector2(0f, -240f), new Vector2(280f, 86f), new Color(0.18f, 0.60f, 0.36f, 1f), fontTheme);

        SkyFallImageGuidePanel guide = root.gameObject.AddComponent<SkyFallImageGuidePanel>();
        guide.guideImage = guideImage;
        guide.pageCounterText = counter;
        guide.previousButton = prev;
        guide.nextButton = next;
        guide.emptyGuideMessageRoot = emptyText.gameObject;
        guide.BindButtons();

        flow.howToPlayPanel = animator;
        flow.imageGuidePanel = guide;
        flow.howToPlayStartButton = start;
        flow.howToPlayStartButtonText = start.GetComponentInChildren<TMP_Text>(true);

        root.gameObject.SetActive(false);
    }

    private static void BuildPausePanel(RectTransform overlayLayer, SkyFallScreenFlowController flow, SkyFallFontThemeApplier fontTheme)
    {
        RectTransform root = CreateFullOverlay("PausePanelRoot", overlayLayer, new Color(0f, 0f, 0f, 0.68f));

        RectTransform card = CreatePanel("PauseCard", root, PanelColor, true);
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.sizeDelta = new Vector2(560f, 470f);
        card.anchoredPosition = Vector2.zero;

        SkyFallUiPanelAnimator animator = root.gameObject.AddComponent<SkyFallUiPanelAnimator>();
        animator.canvasGroup = root.GetComponent<CanvasGroup>();
        animator.cardRoot = card;

        TMP_Text title = CreateTMP("PauseTitleText", card, "Paused", 58, TextAlignmentOptions.Center, Color.white);
        title.rectTransform.anchorMin = new Vector2(0f, 0.72f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.offsetMin = new Vector2(30f, 0f);
        title.rectTransform.offsetMax = new Vector2(-30f, -20f);
        title.fontStyle = FontStyles.Bold;
        fontTheme.primaryTexts.Add(title);

        Button resume = CreateButton("ResumeButton", card, "RESUME", new Vector2(0f, 45f), new Vector2(330f, 78f), new Color(0.18f, 0.60f, 0.36f, 1f), fontTheme);
        Button howTo = CreateButton("PauseHowToPlayButton", card, "HOW TO PLAY", new Vector2(0f, -55f), new Vector2(330f, 78f), new Color(0.24f, 0.31f, 0.62f, 1f), fontTheme);
        Button restart = CreateButton("RestartFromPauseButton", card, "RESTART", new Vector2(0f, -155f), new Vector2(330f, 78f), new Color(0.74f, 0.40f, 0.16f, 1f), fontTheme);

        flow.pausePanel = animator;
        flow.resumeButton = resume;
        flow.pauseHowToPlayButton = howTo;
        flow.restartFromPauseButton = restart;

        root.gameObject.SetActive(false);
    }

    private static RectTransform CreateHudCard(string name, Transform parent, float width)
    {
        RectTransform card = CreatePanel(name, parent, new Color(1f, 1f, 1f, 0.13f), true);
        LayoutElement element = card.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.minWidth = width * 0.75f;
        element.flexibleWidth = 0f;
        return card;
    }

    private static RectTransform CreateLayer(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent);
        Stretch(rect);
        return rect;
    }

    private static RectTransform CreateFullOverlay(string name, Transform parent, Color color)
    {
        RectTransform root = CreatePanel(name, parent, color, true);
        Stretch(root);
        root.gameObject.AddComponent<CanvasGroup>();
        return root;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color, bool raycastTarget)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rect;
    }

    private static TMP_Text CreateTMP(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size, Color color, SkyFallFontThemeApplier fontTheme)
    {
        RectTransform rect = CreatePanel(name, parent, color, true);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();

        TMP_Text text = CreateTMP("Label", rect, label, Mathf.RoundToInt(size.y * 0.36f), TextAlignmentOptions.Center, Color.white);
        Stretch(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        fontTheme.primaryTexts.Add(text);

        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        Stretch(rect, 0f, 0f, 0f, 0f);
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindObjectOfType<EventSystem>();

        if (existing != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create Event System");
    }
}
#endif
