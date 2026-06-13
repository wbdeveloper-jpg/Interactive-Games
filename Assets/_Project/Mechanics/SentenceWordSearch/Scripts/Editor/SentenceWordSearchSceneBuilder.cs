#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SentenceWordSearchSceneBuilder
{
    [MenuItem("Mini Games/Sentence Word Search/Create V2.5 UI Refine Ready Scene")]
    public static void CreateScene()
    {
        SentenceWordSearchCell cellPrefab = SentenceWordSearchPrefabCreator.LoadOrCreateCellPrefab();

        GameObject canvasObject = CreateCanvas();
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        CreateEventSystemIfMissing();

        GameObject root = CreateUIObject("SentenceWordSearchRoot", canvasObject.transform);
        Stretch(root);

        Image rootBg = root.AddComponent<Image>();
        rootBg.color = new Color(1f, 0.965f, 0.955f, 1f);

        VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(42, 42, 32, 32);
        rootLayout.spacing = 16;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;

        // HEADER ROW: Pause left, title middle, how-to-play right.
        GameObject header = CreateUIObject("HeaderRow", root.transform);
        HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = false;
        headerLayout.childForceExpandWidth = false;
        AddLayout(header, 0, 92);

        Button pauseButton = CreateSquareButton("PauseButton", header.transform, "II");
        AddLayout(pauseButton.gameObject, 84, 84);

        GameObject titleCard = CreatePanel("TitleCard", header.transform, new Color(1f, 0.90f, 0.90f, 1f));
        AddLayout(titleCard, 724, 84);

        TextMeshProUGUI titleText = CreateText("TitleText", titleCard.transform, "Sentence Word Search", 44, TextAlignmentOptions.Center);
        Stretch(titleText.rectTransform);

        Button howToPlayButton = CreateSquareButton("HowToPlayButton", header.transform, "?");
        AddLayout(howToPlayButton.gameObject, 84, 84);

        // TOP INFO ROW: Score, instruction line, time, hint.
        GameObject topInfoRow = CreateUIObject("TopInfoRow", root.transform);
        HorizontalLayoutGroup topInfoLayout = topInfoRow.AddComponent<HorizontalLayoutGroup>();
        topInfoLayout.spacing = 12;
        topInfoLayout.childAlignment = TextAnchor.MiddleCenter;
        topInfoLayout.childControlWidth = false;
        topInfoLayout.childForceExpandWidth = false;
        AddLayout(topInfoRow, 0, 76);

        TextMeshProUGUI scoreText = CreateInfoTextCard("ScoreCard", topInfoRow.transform, "Score: 0", 190);
        TextMeshProUGUI instructionText = CreateInfoTextCard("InstructionCard", topInfoRow.transform, "Find the missing word", 440);
        TextMeshProUGUI timerText = CreateInfoTextCard("TimerCard", topInfoRow.transform, "02:00", 170);
        Button hintButton = CreateInfoButton("HintButton", topInfoRow.transform, "Hint", 144);

        // SENTENCE CARD: Question count belongs here, not in info row.
        GameObject sentenceCard = CreatePanel("SentenceCard", root.transform, Color.white);
        AddLayout(sentenceCard, 0, 205);

        VerticalLayoutGroup sentenceLayout = sentenceCard.AddComponent<VerticalLayoutGroup>();
        sentenceLayout.padding = new RectOffset(28, 28, 20, 20);
        sentenceLayout.spacing = 8;
        sentenceLayout.childAlignment = TextAnchor.MiddleCenter;
        sentenceLayout.childControlWidth = true;
        sentenceLayout.childForceExpandWidth = true;

        TextMeshProUGUI questionCounterText = CreateText("QuestionCounterText", sentenceCard.transform, "Question 1/5", 27, TextAlignmentOptions.Center);
        AddLayout(questionCounterText.gameObject, 0, 38);

        TextMeshProUGUI sentenceText = CreateText("SentenceText", sentenceCard.transform, "The wind is _________.", 40, TextAlignmentOptions.Center);
        AddLayout(sentenceText.gameObject, 0, 112);

        GameObject answerTarget = CreateUIObject("AnswerFlyTarget", sentenceCard.transform);
        AddLayout(answerTarget, 220, 8);

        Image answerTargetLine = answerTarget.AddComponent<Image>();
        answerTargetLine.color = new Color(0.9f, 0.22f, 0.22f, 0.6f);

        Image questionImage = CreateImage("QuestionImage", sentenceCard.transform, new Color(1f, 1f, 1f, 0f));
        AddLayout(questionImage.gameObject, 0, 1);
        questionImage.gameObject.SetActive(false);

        GameObject boardFrame = CreatePanel("AlphabetBoardFrame", root.transform, new Color(1f, 0.925f, 0.925f, 1f));
        AddLayout(boardFrame, 900, 900);

        GameObject gridParent = CreateUIObject("GridParent", boardFrame.transform);
        RectTransform gridRect = gridParent.GetComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.offsetMin = new Vector2(26, 26);
        gridRect.offsetMax = new Vector2(-26, -26);

        GridLayoutGroup gridLayout = gridParent.AddComponent<GridLayoutGroup>();

        SentenceWordSearchBoard board = boardFrame.AddComponent<SentenceWordSearchBoard>();
        board.gridParent = gridRect;
        board.gridLayout = gridLayout;
        board.cellPrefab = cellPrefab;
        board.rows = 8;
        board.columns = 8;
        board.gridPadding = 4;
        board.gridSpacing = new Vector2(8, 8);
        board.difficulty = SentenceWordSearchDifficulty.Medium;

        GameObject overlayRootObject = CreateUIObject("OverlayRoot", canvasObject.transform);
        Stretch(overlayRootObject);

        GameObject systems = new GameObject("SentenceWordSearchSystems");
        SentenceWordSearchManager manager = systems.AddComponent<SentenceWordSearchManager>();
        SentenceWordSearchInputController input = systems.AddComponent<SentenceWordSearchInputController>();
        SentenceWordSearchAudio audio = systems.AddComponent<SentenceWordSearchAudio>();
        SentenceWordSearchUI ui = systems.AddComponent<SentenceWordSearchUI>();

        input.manager = manager;
        input.board = board;
        input.targetCanvas = canvas;

        manager.board = board;
        manager.inputController = input;
        manager.audioController = audio;
        manager.ui = ui;
        manager.questionCount = 5;
        manager.randomizeQuestions = true;
        manager.gameTime = 120f;
        manager.correctScore = 10;
        manager.wrongPenalty = 1;

        ui.titleText = titleText;
        ui.sentenceText = sentenceText;
        ui.questionCounterText = questionCounterText;
        ui.instructionText = instructionText;
        ui.scoreText = scoreText;
        ui.timerText = timerText;
        ui.questionImage = questionImage;
        ui.overlayRoot = overlayRootObject.GetComponent<RectTransform>();
        ui.sentenceAnswerTarget = answerTarget.GetComponent<RectTransform>();

        ui.pauseButton = pauseButton;
        ui.howToPlayButton = howToPlayButton;
        ui.hintButton = hintButton;

        ui.primaryTexts.Add(titleText);
        ui.primaryTexts.Add(questionCounterText);
        ui.primaryTexts.Add(scoreText);
        ui.primaryTexts.Add(timerText);
        AddButtonLabelToPrimary(ui, pauseButton);
        AddButtonLabelToPrimary(ui, howToPlayButton);
        AddButtonLabelToPrimary(ui, hintButton);
        ui.secondaryTexts.Add(sentenceText);
        ui.secondaryTexts.Add(instructionText);

        CreateResultPanel(overlayRootObject.transform, ui);
        CreateHowToPlayPanel(overlayRootObject.transform, ui);
        CreatePausePanel(overlayRootObject.transform, ui);

        AddDefaultQuestions(manager);

        Selection.activeGameObject = systems;
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(board);
        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(input);
        EditorUtility.SetDirty(audio);

        Debug.Log("Sentence Word Search V2.5 UI Refine Ready scene created.");
    }

    private static void AddDefaultQuestions(SentenceWordSearchManager manager)
    {
        manager.questionBank.Clear();
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The wind is _________.", answer = "STRONG" });
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The sun is _________.", answer = "BRIGHT" });
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "Ice feels _________.", answer = "COLD" });
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A baby cat is called a _________.", answer = "KITTEN" });
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "We drink _________.", answer = "WATER" });
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A bird can _________.", answer = "FLY" });
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "Grass is usually _________.", answer = "GREEN" });
        manager.questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "We see with our _________.", answer = "EYES" });
    }

    private static void CreateResultPanel(Transform parent, SentenceWordSearchUI ui)
    {
        GameObject panel = CreateOverlayPanel("ResultPanel", parent);
        GameObject box = CreateCenteredBox(panel.transform, new Vector2(720, 520));

        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 45, 45);
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI title = CreateText("ResultTitleText", box.transform, "Great Job!", 50, TextAlignmentOptions.Center);
        TextMeshProUGUI score = CreateText("ResultScoreText", box.transform, "Score: 0", 36, TextAlignmentOptions.Center);
        Button restart = CreateWideButton("ResultRestartButton", box.transform, "Play Again");

        ui.resultPanel = panel;
        ui.resultTitleText = title;
        ui.resultScoreText = score;
        ui.resultRestartButton = restart;

        ui.primaryTexts.Add(title);
        ui.primaryTexts.Add(score);
        AddButtonLabelToPrimary(ui, restart);

        panel.SetActive(false);
    }

    private static void CreateHowToPlayPanel(Transform parent, SentenceWordSearchUI ui)
    {
        GameObject panel = CreateOverlayPanel("HowToPlayPanel", parent);
        GameObject box = CreateCenteredBox(panel.transform, new Vector2(780, 560));

        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(45, 45, 45, 45);
        layout.spacing = 26;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI title = CreateText("HowToTitleText", box.transform, "How To Play", 48, TextAlignmentOptions.Center);
        TextMeshProUGUI body = CreateText("HowToBodyText", box.transform, "Read the sentence.\nFind the missing word in the grid.\nDrag across the letters to select it.", 30, TextAlignmentOptions.Center);
        Button close = CreateWideButton("CloseHowToPlayButton", box.transform, "Start");

        ui.howToPlayPanel = panel;
        ui.howToPlayBodyText = body;
        ui.closeHowToPlayButton = close;

        ui.primaryTexts.Add(title);
        ui.secondaryTexts.Add(body);
        AddButtonLabelToPrimary(ui, close);

        panel.SetActive(false);
    }

    private static void CreatePausePanel(Transform parent, SentenceWordSearchUI ui)
    {
        GameObject panel = CreateOverlayPanel("PausePanel", parent);
        GameObject box = CreateCenteredBox(panel.transform, new Vector2(620, 500));

        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(45, 45, 45, 45);
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI title = CreateText("PauseTitleText", box.transform, "Paused", 50, TextAlignmentOptions.Center);
        Button resume = CreateWideButton("ResumeButton", box.transform, "Resume");
        Button restart = CreateWideButton("PauseRestartButton", box.transform, "Restart");

        ui.pausePanel = panel;
        ui.resumeButton = resume;
        ui.restartButton = restart;

        ui.primaryTexts.Add(title);
        AddButtonLabelToPrimary(ui, resume);
        AddButtonLabelToPrimary(ui, restart);

        panel.SetActive(false);
    }

    private static GameObject CreateCanvas()
    {
        GameObject canvasObject = new GameObject("SentenceWordSearchCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvasObject;
    }

    private static void CreateEventSystemIfMissing()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject panel = CreateUIObject(name, parent);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static GameObject CreateOverlayPanel(string name, Transform parent)
    {
        GameObject panel = CreateUIObject(name, parent);
        Stretch(panel);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.58f);
        return panel;
    }

    private static GameObject CreateCenteredBox(Transform parent, Vector2 size)
    {
        GameObject box = CreatePanel("Box", parent, Color.white);
        RectTransform rect = box.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        return box;
    }

    private static TextMeshProUGUI CreateInfoTextCard(string name, Transform parent, string text, float preferredWidth)
    {
        GameObject card = CreatePanel(name, parent, new Color(1f, 0.90f, 0.90f, 1f));
        AddLayout(card, preferredWidth, 70);
        TextMeshProUGUI label = CreateText("Text", card.transform, text, 24, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return label;
    }

    private static Button CreateInfoButton(string name, Transform parent, string text, float preferredWidth)
    {
        GameObject obj = CreatePanel(name, parent, new Color(0.88f, 0.18f, 0.18f, 1f));
        Button button = obj.AddComponent<Button>();
        AddLayout(obj, preferredWidth, 70);
        TextMeshProUGUI label = CreateText("Label", obj.transform, text, 27, TextAlignmentOptions.Center);
        label.color = Color.white;
        Stretch(label.rectTransform);
        return button;
    }

    private static Button CreateSquareButton(string name, Transform parent, string text)
    {
        GameObject obj = CreatePanel(name, parent, new Color(0.88f, 0.18f, 0.18f, 1f));
        Button button = obj.AddComponent<Button>();
        TextMeshProUGUI label = CreateText("Label", obj.transform, text, 30, TextAlignmentOptions.Center);
        label.color = Color.white;
        Stretch(label.rectTransform);
        return button;
    }

    private static Button CreateWideButton(string name, Transform parent, string text)
    {
        GameObject obj = CreatePanel(name, parent, new Color(0.88f, 0.18f, 0.18f, 1f));
        Button button = obj.AddComponent<Button>();
        TextMeshProUGUI label = CreateText("Label", obj.transform, text, 30, TextAlignmentOptions.Center);
        label.color = Color.white;
        Stretch(label.rectTransform);
        AddLayout(obj, 250, 72);
        return button;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = new Color(0.22f, 0.18f, 0.18f, 1f);
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = CreateUIObject(name, parent);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        return image;
    }

    private static void AddButtonLabelToPrimary(SentenceWordSearchUI ui, Button button)
    {
        if (ui == null || button == null)
            return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label != null && !ui.primaryTexts.Contains(label))
            ui.primaryTexts.Add(label);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void Stretch(GameObject obj)
    {
        Stretch(obj.GetComponent<RectTransform>());
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddLayout(GameObject obj, float preferredWidth, float preferredHeight)
    {
        LayoutElement layout = obj.GetComponent<LayoutElement>();

        if (layout == null)
            layout = obj.AddComponent<LayoutElement>();

        if (preferredWidth > 0)
            layout.preferredWidth = preferredWidth;

        if (preferredHeight > 0)
            layout.preferredHeight = preferredHeight;
    }
}

#endif
