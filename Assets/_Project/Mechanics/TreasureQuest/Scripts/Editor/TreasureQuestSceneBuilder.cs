#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class TreasureQuestSceneBuilder
{
    private static readonly Color Parchment = new Color(0.88f, 0.76f, 0.52f, 1f);
    private static readonly Color Brown = new Color(0.38f, 0.22f, 0.10f, 1f);
    private static readonly Color DarkBrown = new Color(0.20f, 0.12f, 0.06f, 1f);
    private static readonly Color Gold = new Color(0.94f, 0.70f, 0.24f, 1f);
    private static readonly Color Cream = new Color(1f, 0.94f, 0.80f, 1f);
    private static readonly Color SoftPanel = new Color(1f, 0.90f, 0.67f, 0.96f);
    private static readonly Color PanelBrown = new Color(0.82f, 0.62f, 0.34f, 0.72f);

    [MenuItem("Tools/Treasure Quest/Create Rough Single Scene UI")]
    public static void CreateRoughSingleSceneUI()
    {
        GameObject oldCanvas = GameObject.Find("TreasureQuest_Canvas");
        GameObject oldSystem = GameObject.Find("TreasureQuest_System");

        if ((oldCanvas != null || oldSystem != null) && !EditorUtility.DisplayDialog(
                "Rebuild Treasure Quest UI",
                "A Treasure Quest setup already exists. Delete and rebuild it?",
                "Rebuild",
                "Cancel"))
        {
            return;
        }

        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);
        if (oldSystem != null) Object.DestroyImmediate(oldSystem);

        EnsureEventSystem();

        Canvas canvas = CreateCanvas();
        TreasureQuestUIManager ui = canvas.gameObject.AddComponent<TreasureQuestUIManager>();

        GameObject system = new GameObject("TreasureQuest_System");
        Undo.RegisterCreatedObjectUndo(system, "Create TreasureQuest System");

        TreasureQuestGameManager gameManager = system.AddComponent<TreasureQuestGameManager>();
        TreasureQuestLevelManager levelManager = system.AddComponent<TreasureQuestLevelManager>();
        TreasureQuestQuizManager quizManager = system.AddComponent<TreasureQuestQuizManager>();
        TreasureQuestQuestionDatabase database = system.AddComponent<TreasureQuestQuestionDatabase>();
        TreasureQuestAudioManager audioManager = system.AddComponent<TreasureQuestAudioManager>();

        AudioSource sfx = system.GetComponent<AudioSource>();
        if (sfx == null) sfx = system.AddComponent<AudioSource>();
        audioManager.sfxSource = sfx;
        quizManager.requireAllCorrectToUnlock = true;
        quizManager.requiredCorrectToUnlock = 5;

        gameManager.uiManager = ui;
        gameManager.levelManager = levelManager;
        gameManager.quizManager = quizManager;
        gameManager.questionDatabase = database;
        gameManager.audioManager = audioManager;

        levelManager.gameManager = gameManager;
        levelManager.uiManager = ui;
        levelManager.audioManager = audioManager;

        quizManager.gameManager = gameManager;
        quizManager.levelManager = levelManager;
        quizManager.uiManager = ui;
        quizManager.audioManager = audioManager;
        quizManager.questionDatabase = database;

        CreatePanels(canvas.transform, ui);

        Selection.activeGameObject = system;
        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(system);
        Debug.Log("Treasure Quest rough single-scene UI created. No prefabs used. Press Play to test.");
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("TreasureQuest_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create TreasureQuest Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreatePanels(Transform root, TreasureQuestUIManager ui)
    {
        GameObject loadingPanel = CreatePanel(root, "LoadingPanel", Parchment);
        ui.loadingPanel = loadingPanel;
        ui.loadingTitleImage = CreateImage(loadingPanel.transform, "LoadingTitleLogoImage", AnchorStretchTop(), AnchorStretchTop(), new Vector2(0, -210), new Vector2(900, 150), Gold);
        ui.loadingTitleImage.raycastTarget = false;
        CreateText(loadingPanel.transform, "LoadingText", "Loading...", 42, TextAlignmentOptions.Center, AnchorCenter(), new Vector2(0, -30), new Vector2(600, 80), Brown);

        GameObject menuPanel = CreatePanel(root, "MenuPanel", Parchment);
        ui.menuPanel = menuPanel;
        BuildMenuPanel(menuPanel.transform, ui);

        GameObject gameplayPanel = CreatePanel(root, "GameplayPanel", Parchment);
        ui.gameplayPanel = gameplayPanel;
        BuildGameplayPanel(gameplayPanel.transform, ui);

        GameObject resultPanel = CreatePanel(root, "GateUnlockedPanel", new Color(0f, 0f, 0f, 0.35f));
        ui.gateUnlockedPanel = resultPanel;
        BuildResultPanel(resultPanel.transform, ui);

        GameObject pausePanel = CreatePanel(root, "PausePanel", new Color(0f, 0f, 0f, 0.45f));
        ui.pausePanel = pausePanel;
        BuildPausePanel(pausePanel.transform, ui);

        GameObject htpPanel = CreatePanel(root, "HowToPlayPanel", new Color(0f, 0f, 0f, 0.45f));
        ui.howToPlayPanel = htpPanel;
        BuildHowToPlayPanel(htpPanel.transform, ui);

        loadingPanel.SetActive(true);
        menuPanel.SetActive(false);
        gameplayPanel.SetActive(false);
        resultPanel.SetActive(false);
        pausePanel.SetActive(false);
        htpPanel.SetActive(false);
    }

    private static void BuildMenuPanel(Transform parent, TreasureQuestUIManager ui)
    {
        GameObject safe = CreateStretchContainer(parent, "MenuSafeArea", new Vector2(54, 42), new Vector2(-54, -36));

        ui.menuTitleImage = CreateImage(safe.transform, "TitleLogoImage", AnchorStretchTop(), AnchorStretchTop(), new Vector2(0, -68), new Vector2(720, 116), Gold);
        ui.menuTitleImage.raycastTarget = false;

        ui.homeButton = CreateIconButton(safe.transform, "HomeButton", new Vector2(0, 1), new Vector2(0, 1), new Vector2(46, -52), new Vector2(76, 76), Brown);
        ui.menuHowToPlayButton = CreateIconButton(safe.transform, "HowToPlayButton", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-46, -52), new Vector2(76, 76), Brown);

        Image mapArea = CreateImage(safe.transform, "MapContentArea", AnchorCenter(), AnchorCenter(), new Vector2(0, -25), new Vector2(1680, 710), new Color(1f, 0.88f, 0.58f, 0.25f));
        mapArea.raycastTarget = false;

        CreateImage(mapArea.transform, "DottedPathPlaceholder", AnchorCenter(), AnchorCenter(), new Vector2(0, 10), new Vector2(1380, 80), new Color(0.38f, 0.22f, 0.10f, 0.18f)).raycastTarget = false;

        ui.lockedFeedbackText = CreateText(safe.transform, "LockedFeedbackText", "", 32, TextAlignmentOptions.Center, AnchorStretchBottom(), new Vector2(0, 142), new Vector2(1200, 70), new Color(0.68f, 0.16f, 0.10f, 1f));
        ui.lockedFeedbackText.gameObject.SetActive(false);

        Vector2[] positions =
        {
            new Vector2(-610, 155),
            new Vector2(-340, -118),
            new Vector2(-15, 118),
            new Vector2(330, -105),
            new Vector2(625, 140)
        };

        ui.gateButtons = new TreasureQuestGateButton[5];
        for (int i = 0; i < 5; i++)
        {
            Button gateButton = CreateButton(mapArea.transform, "GateButton_" + (i + 1), "", AnchorCenter(), AnchorCenter(), positions[i], new Vector2(178, 138), Gold, DarkBrown);
            TreasureQuestGateButton gate = gateButton.gameObject.AddComponent<TreasureQuestGateButton>();
            gate.gateNumber = i + 1;
            gate.button = gateButton;
            gate.gateImage = gateButton.GetComponent<Image>();
            gate.gateLabel = CreateText(gateButton.transform, "GateNumber", (i + 1).ToString(), 34, TextAlignmentOptions.Center, AnchorStretchFull(), Vector2.zero, Vector2.zero, DarkBrown);
            ui.gateButtons[i] = gate;
        }

        ui.treasureChestImage = CreateImage(mapArea.transform, "FinalTreasureChest", AnchorCenter(), AnchorCenter(), new Vector2(745, -245), new Vector2(230, 160), Gold);
        CreateText(ui.treasureChestImage.transform, "ChestLabel", "Treasure", 25, TextAlignmentOptions.Center, AnchorStretchFull(), Vector2.zero, Vector2.zero, DarkBrown);

        ui.playButton = CreateButton(safe.transform, "PlayButton", "Play Highest Gate", AnchorStretchBottom(), AnchorStretchBottom(), new Vector2(0, 62), new Vector2(430, 82), Brown, Cream);
    }

    private static void BuildGameplayPanel(Transform parent, TreasureQuestUIManager ui)
    {
        Image topBar = CreateImage(parent, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -107.5f), new Vector2(0, 215), new Color(0.40f, 0.24f, 0.11f, 0.95f));
        AddHorizontalLayout(topBar.gameObject, new RectOffset(32, 32, 20, 20), 24, TextAnchor.MiddleCenter, true, true, false, false);

        ui.gameplayHomeButton = CreateIconButton(topBar.transform, "GameplayHomeButton", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(96, 96), Cream);
        AddLayoutElement(ui.gameplayHomeButton.gameObject, 96, 96, 0, 0);

        GameObject headerGroup = CreateLayoutContainer(topBar.transform, "CenterHeaderGroup");
        AddLayoutElement(headerGroup, 1080, 175, 1, 0);
        AddVerticalLayout(headerGroup, new RectOffset(0, 0, 0, 0), 12, TextAnchor.MiddleCenter, true, true, false, false);

        ui.gateTitleBackgroundImage = CreateImage(headerGroup.transform, "GateTitleBadge", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(520, 64), new Color(0.62f, 0.38f, 0.16f, 1f));
        AddLayoutElement(ui.gateTitleBackgroundImage.gameObject, 520, 64, 0, 0);
        ui.gateTitleText = CreateText(ui.gateTitleBackgroundImage.transform, "GateTitleText", "Gate 1", 44, TextAlignmentOptions.Center, AnchorStretchFull(), Vector2.zero, Vector2.zero, Cream);

        Image progressBlock = CreateImage(headerGroup.transform, "ProgressBlock", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(800, 88), new Color(0.18f, 0.10f, 0.05f, 0.28f));
        AddLayoutElement(progressBlock.gameObject, 800, 88, 0, 0);

        ui.progressSlider = CreateSlider(progressBlock.transform, "ProgressSlider_BackTrack", AnchorCenter(), AnchorCenter(), new Vector2(-45, 0), new Vector2(610, 30), new Color(0.18f, 0.10f, 0.05f, 0.55f), Gold);
        ui.progressSlider.interactable = false;
        ui.progressSlider.minValue = 0;
        ui.progressSlider.maxValue = 5;
        ui.progressSlider.value = 0;

        GameObject progressStepOverlay = CreateImage(progressBlock.transform, "ProgressStepsOverlay", AnchorCenter(), AnchorCenter(), new Vector2(-45, 0), new Vector2(650, 58), new Color(1f, 1f, 1f, 0f)).gameObject;
        AddHorizontalLayout(progressStepOverlay, new RectOffset(0, 0, 0, 0), 92, TextAnchor.MiddleCenter, true, true, false, false);

        ui.progressSteps = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            Image step = CreateImage(progressStepOverlay.transform, "ProgressStep_" + (i + 1), AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(44, 44), Cream);
            AddLayoutElement(step.gameObject, 44, 44, 0, 0);
            ui.progressSteps[i] = step;
        }

        ui.progressText = CreateText(progressBlock.transform, "ProgressText", "0 / 5", 32, TextAlignmentOptions.Center, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-72, 0), new Vector2(120, 50), Cream);

        GameObject rightGroup = CreateLayoutContainer(topBar.transform, "RightHudGroup");
        AddLayoutElement(rightGroup, 430, 120, 0, 0);
        AddHorizontalLayout(rightGroup, new RectOffset(0, 0, 0, 0), 18, TextAnchor.MiddleRight, true, true, false, false);

        ui.coinGroupBackgroundImage = CreateImage(rightGroup.transform, "CoinGroup_BG", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(280, 82), new Color(0.18f, 0.10f, 0.05f, 0.35f));
        AddLayoutElement(ui.coinGroupBackgroundImage.gameObject, 280, 82, 0, 0);
        AddHorizontalLayout(ui.coinGroupBackgroundImage.gameObject, new RectOffset(18, 18, 10, 10), 12, TextAnchor.MiddleCenter, true, true, false, false);
        CreateText(ui.coinGroupBackgroundImage.transform, "CoinLabel", "Coins", 30, TextAlignmentOptions.Right, AnchorCenter(), Vector2.zero, new Vector2(118, 48), Cream);
        ui.coinText = CreateText(ui.coinGroupBackgroundImage.transform, "CoinText", "0", 34, TextAlignmentOptions.Left, AnchorCenter(), Vector2.zero, new Vector2(94, 48), Cream);

        ui.pauseButton = CreateIconButton(rightGroup.transform, "PauseButton", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(96, 96), Cream);
        AddLayoutElement(ui.pauseButton.gameObject, 96, 96, 0, 0);

        GameObject content = CreateStretchContainer(parent, "GameplayContent", new Vector2(56, 50), new Vector2(-56, -245));
        AddHorizontalLayout(content, new RectOffset(0, 0, 0, 0), 34, TextAnchor.MiddleCenter, true, true, true, true);

        GameObject quizColumn = CreateLayoutContainer(content.transform, "QuizColumn_Left");
        AddLayoutElement(quizColumn, 1020, -1, 1, 1, 840, -1);
        AddVerticalLayout(quizColumn, new RectOffset(0, 0, 0, 0), 28, TextAnchor.UpperCenter, true, true, true, true);

        Image questionCard = CreateImage(quizColumn.transform, "QuestionCardPanel", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(1000, 220), SoftPanel);
        AddLayoutElement(questionCard.gameObject, -1, 220, 1, 0, -1, 200);
        ui.questionText = CreateStretchText(questionCard.transform, "QuestionText", "Question appears here", 52, TextAlignmentOptions.Center, new Vector2(34, 22), new Vector2(-34, -22), DarkBrown);
        ui.questionText.enableAutoSizing = true;
        ui.questionText.fontSizeMin = 34;
        ui.questionText.fontSizeMax = 56;

        Image answerArea = CreateImage(quizColumn.transform, "AnswerAreaPanel", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(1000, 460), new Color(0.92f, 0.78f, 0.52f, 0.65f));
        AddLayoutElement(answerArea.gameObject, -1, 460, 1, 1, -1, 400);
        AddVerticalLayout(answerArea.gameObject, new RectOffset(30, 30, 30, 30), 24, TextAnchor.MiddleCenter, true, true, true, true);

        ui.answerButtons = new TreasureQuestAnswerButton[4];
        for (int row = 0; row < 2; row++)
        {
            GameObject answerRow = CreateLayoutContainer(answerArea.transform, "AnswerRow_" + (row + 1));
            AddLayoutElement(answerRow, -1, 165, 1, 1, -1, 140);
            AddHorizontalLayout(answerRow, new RectOffset(0, 0, 0, 0), 24, TextAnchor.MiddleCenter, true, true, true, true);

            for (int column = 0; column < 2; column++)
            {
                int index = row * 2 + column;
                Button answer = CreateButton(answerRow.transform, "AnswerButton_" + (index + 1), "Answer " + (index + 1), AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(440, 150), Cream, DarkBrown);
                AddLayoutElement(answer.gameObject, -1, -1, 1, 1, 320, 130);
                TreasureQuestAnswerButton answerButton = answer.gameObject.AddComponent<TreasureQuestAnswerButton>();
                answerButton.button = answer;
                answerButton.backgroundImage = answer.GetComponent<Image>();
                answerButton.answerText = answer.GetComponentInChildren<TMP_Text>();
                ui.answerButtons[index] = answerButton;
            }
        }

        Image gateColumn = CreateImage(content.transform, "GateColumn_Right", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(520, 710), PanelBrown);
        AddLayoutElement(gateColumn.gameObject, 520, -1, 0, 1, 430, -1);
        AddVerticalLayout(gateColumn.gameObject, new RectOffset(30, 30, 24, 24), 16, TextAnchor.MiddleCenter, true, true, true, true);

        ui.gameplayGateImage = CreateImage(gateColumn.transform, "GameplayGateImage", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(430, 510), Gold);
        AddLayoutElement(ui.gameplayGateImage.gameObject, -1, 510, 1, 1, -1, 430);
        CreateText(ui.gameplayGateImage.transform, "GatePlaceholderText", "Closed\nGate", 42, TextAlignmentOptions.Center, AnchorStretchFull(), Vector2.zero, Vector2.zero, DarkBrown);

        Image statusPanel = CreateImage(gateColumn.transform, "GateStatusPanel", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(460, 92), SoftPanel);
        AddLayoutElement(statusPanel.gameObject, -1, 92, 1, 0, -1, 78);
        ui.gateStatusText = CreateStretchText(statusPanel.transform, "GateStatusText", "Answer all 5 correctly to unlock the gate!", 26, TextAlignmentOptions.Center, new Vector2(22, 8), new Vector2(-22, -8), DarkBrown);
        ui.gateStatusText.enableAutoSizing = true;
        ui.gateStatusText.fontSizeMin = 18;
        ui.gateStatusText.fontSizeMax = 28;
    }

    private static void BuildResultPanel(Transform parent, TreasureQuestUIManager ui)
    {
        Image card = CreateImage(parent, "ResultCard", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(980, 780), SoftPanel);
        ui.resultTitleText = CreateText(card.transform, "ResultTitleText", "Gate Completed!", 62, TextAlignmentOptions.Center, AnchorStretchTop(), new Vector2(0, -62), new Vector2(760, 90), DarkBrown);
        ui.resultGateImage = CreateImage(card.transform, "ResultGateImage", AnchorCenter(), AnchorCenter(), new Vector2(0, 110), new Vector2(360, 320), Gold);
        CreateText(ui.resultGateImage.transform, "ResultGateLabel", "Open\nGate", 38, TextAlignmentOptions.Center, AnchorStretchFull(), Vector2.zero, Vector2.zero, DarkBrown);
        ui.resultDetailsText = CreateText(card.transform, "ResultDetailsText", "Correct: 0 / 5", 36, TextAlignmentOptions.Center, AnchorCenter(), new Vector2(0, -145), new Vector2(760, 170), DarkBrown);
        ui.resultContinueButton = CreateButton(card.transform, "ResultContinueButton", "Continue to Map", AnchorStretchBottom(), AnchorStretchBottom(), new Vector2(0, 70), new Vector2(430, 86), Brown, Cream);
        ui.resultContinueButtonText = ui.resultContinueButton.GetComponentInChildren<TMP_Text>();
    }

    private static void BuildPausePanel(Transform parent, TreasureQuestUIManager ui)
    {
        Image card = CreateImage(parent, "PauseCard", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(520, 560), SoftPanel);
        CreateText(card.transform, "PauseTitle", "Paused", 54, TextAlignmentOptions.Center, AnchorStretchTop(), new Vector2(0, -55), new Vector2(400, 80), DarkBrown);
        ui.resumeButton = CreateButton(card.transform, "ResumeButton", "Resume", AnchorCenter(), AnchorCenter(), new Vector2(0, 110), new Vector2(350, 68), Brown, Cream);
        ui.pauseHowToPlayButton = CreateButton(card.transform, "HowToPlayButton", "How To Play", AnchorCenter(), AnchorCenter(), new Vector2(0, 25), new Vector2(350, 68), Brown, Cream);
        ui.restartGateButton = CreateButton(card.transform, "RestartGateButton", "Restart Gate", AnchorCenter(), AnchorCenter(), new Vector2(0, -60), new Vector2(350, 68), Brown, Cream);
        ui.backToMapButton = CreateButton(card.transform, "BackToMapButton", "Back to Map", AnchorCenter(), AnchorCenter(), new Vector2(0, -145), new Vector2(350, 68), Brown, Cream);
    }

    private static void BuildHowToPlayPanel(Transform parent, TreasureQuestUIManager ui)
    {
        Image card = CreateImage(parent, "HowToPlayCard", AnchorCenter(), AnchorCenter(), Vector2.zero, new Vector2(1120, 820), SoftPanel);
        CreateText(card.transform, "HowToPlayTitle", "How To Play", 56, TextAlignmentOptions.Center, AnchorStretchTop(), new Vector2(0, -52), new Vector2(760, 78), DarkBrown);

        Image guideFrame = CreateImage(card.transform, "GuideImageFrame", AnchorCenter(), AnchorCenter(), new Vector2(0, 48), new Vector2(980, 560), new Color(0.92f, 0.78f, 0.52f, 0.75f));
        ui.howToPlayGuideImage = CreateImage(guideFrame.transform, "GuideImage", AnchorStretchFull(), Vector2.zero, Vector2.zero, SoftPanel);
        ui.howToPlayGuideImage.preserveAspect = true;
        ui.howToPlayGuideImage.raycastTarget = false;

        ui.howToPlayPageText = CreateText(card.transform, "GuidePageText", "", 28, TextAlignmentOptions.Center, AnchorStretchBottom(), new Vector2(0, 132), new Vector2(200, 44), DarkBrown);

        ui.howToPlayPrevButton = CreateButton(card.transform, "PrevButton", "Prev", AnchorStretchBottom(), AnchorStretchBottom(), new Vector2(-350, 60), new Vector2(230, 74), Brown, Cream);
        ui.howToPlayContinueButton = CreateButton(card.transform, "ContinueButton", "Continue", AnchorStretchBottom(), AnchorStretchBottom(), new Vector2(0, 60), new Vector2(280, 74), Brown, Cream);
        ui.howToPlayNextButton = CreateButton(card.transform, "NextButton", "Next", AnchorStretchBottom(), AnchorStretchBottom(), new Vector2(350, 60), new Vector2(230, 74), Brown, Cream);
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static GameObject CreateLayoutContainer(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = AnchorCenter();
        rect.anchorMax = AnchorCenter();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return obj;
    }

    private static GameObject CreateStretchContainer(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return obj;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        return CreateImage(parent, name, anchor, anchor, anchoredPosition, size, color);
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        ApplyRect(rect, anchorMin, anchorMax, anchoredPosition, size);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color fillColor)
    {
        Image background = CreateImage(parent, name, anchorMin, anchorMax, anchoredPosition, size, backgroundColor);
        Slider slider = background.gameObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(background.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 5);
        fillAreaRect.offsetMax = new Vector2(-5, -5);

        Image fill = CreateImage(fillArea.transform, "Fill", AnchorStretchFull(), Vector2.zero, Vector2.zero, fillColor);
        fill.raycastTarget = false;

        slider.fillRect = fill.rectTransform;
        slider.targetGraphic = fill;
        return slider;
    }

    private static Button CreateIconButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color backgroundColor)
    {
        Image image = CreateImage(parent, name, anchorMin, anchorMax, anchoredPosition, size, backgroundColor);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color textColor)
    {
        return CreateButton(parent, name, label, anchor, anchor, anchoredPosition, size, backgroundColor, textColor);
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color backgroundColor, Color textColor)
    {
        Image image = CreateImage(parent, name, anchorMin, anchorMax, anchoredPosition, size, backgroundColor);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        if (!string.IsNullOrEmpty(label))
        {
            TMP_Text text = CreateStretchText(image.transform, "Label", label, 30, TextAlignmentOptions.Center, new Vector2(14, 8), new Vector2(-14, -8), textColor);
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = 32;
        }

        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchor, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        return CreateText(parent, name, text, fontSize, alignment, anchor, anchor, anchoredPosition, size, color);
    }

    private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        ApplyRect(rect, anchorMin, anchorMax, anchoredPosition, size);

        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.enableAutoSizing = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TMP_Text CreateStretchText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        TMP_Text tmp = CreateText(parent, name, text, fontSize, alignment, AnchorStretchFull(), Vector2.zero, Vector2.zero, color);
        RectTransform rect = tmp.GetComponent<RectTransform>();
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return tmp;
    }

    private static void AddHorizontalLayout(GameObject obj, RectOffset padding, float spacing, TextAnchor alignment, bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight)
    {
        HorizontalLayoutGroup group = obj.AddComponent<HorizontalLayoutGroup>();
        group.padding = padding;
        group.spacing = spacing;
        group.childAlignment = alignment;
        group.childControlWidth = controlWidth;
        group.childControlHeight = controlHeight;
        group.childForceExpandWidth = expandWidth;
        group.childForceExpandHeight = expandHeight;
    }

    private static void AddVerticalLayout(GameObject obj, RectOffset padding, float spacing, TextAnchor alignment, bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight)
    {
        VerticalLayoutGroup group = obj.AddComponent<VerticalLayoutGroup>();
        group.padding = padding;
        group.spacing = spacing;
        group.childAlignment = alignment;
        group.childControlWidth = controlWidth;
        group.childControlHeight = controlHeight;
        group.childForceExpandWidth = expandWidth;
        group.childForceExpandHeight = expandHeight;
    }

    private static void AddLayoutElement(GameObject obj, float preferredWidth = -1, float preferredHeight = -1, float flexibleWidth = -1, float flexibleHeight = -1, float minWidth = -1, float minHeight = -1)
    {
        LayoutElement element = obj.GetComponent<LayoutElement>();
        if (element == null) element = obj.AddComponent<LayoutElement>();

        if (preferredWidth >= 0) element.preferredWidth = preferredWidth;
        if (preferredHeight >= 0) element.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0) element.flexibleWidth = flexibleWidth;
        if (flexibleHeight >= 0) element.flexibleHeight = flexibleHeight;
        if (minWidth >= 0) element.minWidth = minWidth;
        if (minHeight >= 0) element.minHeight = minHeight;
    }

    private static void ApplyRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        if (IsFullStretch(anchorMin) || IsFullStretch(anchorMax))
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static bool IsFullStretch(Vector2 value)
    {
        return Mathf.Approximately(value.x, -1f) && Mathf.Approximately(value.y, -1f);
    }

    private static Vector2 AnchorCenter() => new Vector2(0.5f, 0.5f);
    private static Vector2 AnchorStretchTop() => new Vector2(0.5f, 1f);
    private static Vector2 AnchorStretchBottom() => new Vector2(0.5f, 0f);
    private static Vector2 AnchorStretchFull() => new Vector2(-1f, -1f);

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
    }
}
#endif
