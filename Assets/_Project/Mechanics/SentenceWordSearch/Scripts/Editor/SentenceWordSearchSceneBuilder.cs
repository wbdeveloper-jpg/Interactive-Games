#if UNITY_EDITOR

using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SentenceWordSearchSceneBuilder
{
    [MenuItem("Mini Games/Sentence Word Search/Create V2.3 Review Fixed Scene")]
    public static void CreateScene()
    {
        List<TextMeshProUGUI> primaryTexts = new List<TextMeshProUGUI>();
        List<TextMeshProUGUI> secondaryTexts = new List<TextMeshProUGUI>();

        GameObject canvasObject = CreateCanvas();
        CreateEventSystemIfMissing();

        GameObject root = CreateUIObject("SentenceWordSearchRoot", canvasObject.transform);
        Stretch(root);

        Image rootBg = root.AddComponent<Image>();
        rootBg.color = new Color(0.94f, 0.96f, 1f);

        VerticalLayoutGroup rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(44, 44, 30, 34);
        rootLayout.spacing = 14;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;

        GameObject titleRow = CreateUIObject("TitleRow", root.transform);
        HorizontalLayoutGroup titleLayout = titleRow.AddComponent<HorizontalLayoutGroup>();
        titleLayout.spacing = 14;
        titleLayout.childAlignment = TextAnchor.MiddleCenter;
        titleLayout.childControlWidth = true;
        titleLayout.childForceExpandWidth = false;
        AddLayout(titleRow, 0, 76);

        CreateSpacer(titleRow.transform, 72, 72);
        TextMeshProUGUI titleText = CreateText("TitleText", titleRow.transform, "Sentence Word Search", 44, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        AddFlexible(titleText.gameObject, 0, 72, 1);
        primaryTexts.Add(titleText);
        Button howToPlayButton = CreateButton("HowToPlayButton", titleRow.transform, "?", secondaryTexts, 34);
        AddLayout(howToPlayButton.gameObject, 72, 72);

        TextMeshProUGUI sentenceText = CreateText("SentenceText", root.transform, "The wind is _________.", 36, TextAlignmentOptions.Center);
        sentenceText.fontStyle = FontStyles.Bold;
        AddLayout(sentenceText.gameObject, 0, 96);
        primaryTexts.Add(sentenceText);

        Image questionImage = CreateImage("QuestionImage", root.transform, new Color(1f, 1f, 1f, 0.2f));
        AddLayout(questionImage.gameObject, 0, 96);
        questionImage.gameObject.SetActive(false);

        GameObject topRow = CreateUIObject("TopInfoRow", root.transform);
        HorizontalLayoutGroup topLayout = topRow.AddComponent<HorizontalLayoutGroup>();
        topLayout.spacing = 12;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childControlWidth = true;
        topLayout.childForceExpandWidth = false;
        AddLayout(topRow, 0, 66);

        Button pauseButton = CreateButton("PauseButton", topRow.transform, "II", secondaryTexts, 26);
        AddLayout(pauseButton.gameObject, 74, 60);

        TextMeshProUGUI progressText = CreateInfoBox("ProgressInfoBox", topRow.transform, "1 / 5", secondaryTexts);
        TextMeshProUGUI scoreText = CreateInfoBox("ScoreInfoBox", topRow.transform, "Score: 0", secondaryTexts);
        TextMeshProUGUI timerText = CreateInfoBox("TimerInfoBox", topRow.transform, "02:00", secondaryTexts);

        Button hintButton = CreateButton("HintButton", topRow.transform, "Hint", secondaryTexts, 24);
        AddLayout(hintButton.gameObject, 110, 60);

        GameObject boardArea = CreateUIObject("BoardArea_FixedSize", root.transform);
        AddLayout(boardArea, 860, 860);
        Image boardBg = boardArea.AddComponent<Image>();
        boardBg.color = new Color(1f, 1f, 1f, 0.82f);

        GameObject gridParent = CreateUIObject("AlphabetGrid", boardArea.transform);
        Stretch(gridParent);
        GridLayoutGroup gridLayout = gridParent.AddComponent<GridLayoutGroup>();
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 8;
        gridLayout.spacing = new Vector2(6, 6);
        gridLayout.cellSize = new Vector2(88, 88);

        GameObject inputLayer = CreateUIObject("GridInputLayer", boardArea.transform);
        Stretch(inputLayer);
        Image inputImage = inputLayer.AddComponent<Image>();
        inputImage.color = new Color(1f, 1f, 1f, 0f);
        inputImage.raycastTarget = true;
        SentenceWordSearchInputController inputController = inputLayer.AddComponent<SentenceWordSearchInputController>();

        GameObject templates = new GameObject("SentenceWordSearchTemplates");
        templates.transform.SetParent(canvasObject.transform, false);
        templates.SetActive(false);
        SentenceWordSearchCell cellPrefab = CreateCellTemplate(templates.transform, primaryTexts, secondaryTexts);

        GameObject systems = new GameObject("SentenceWordSearchSystems");
        SentenceWordSearchManager manager = systems.AddComponent<SentenceWordSearchManager>();
        SentenceWordSearchBoard board = systems.AddComponent<SentenceWordSearchBoard>();
        SentenceWordSearchUI ui = systems.AddComponent<SentenceWordSearchUI>();
        SentenceWordSearchAudio audioController = systems.AddComponent<SentenceWordSearchAudio>();

        board.gridParent = gridParent.GetComponent<RectTransform>();
        board.gridLayout = gridLayout;
        board.cellPrefab = cellPrefab;
        board.rows = 8;
        board.columns = 8;
        board.padding = 8;
        board.spacing = new Vector2(6, 6);
        board.autoResizeCellsToParent = true;

        inputController.board = board;

        ui.canvas = canvasObject.GetComponent<Canvas>();
        ui.sentenceText = sentenceText;
        ui.progressText = progressText;
        ui.scoreText = scoreText;
        ui.timerText = timerText;
        ui.questionImage = questionImage;

        CreateResultPanel(canvasObject.transform, ui, primaryTexts, secondaryTexts, out Button resultRestartButton);
        CreateHowToPlayPanel(canvasObject.transform, ui, primaryTexts, secondaryTexts, out Button closeHowToPlayButton);
        CreatePausePanel(canvasObject.transform, ui, primaryTexts, secondaryTexts, out Button resumeButton, out Button pauseRestartButton);

        ui.primaryFontTexts = primaryTexts.ToArray();
        ui.secondaryFontTexts = secondaryTexts.ToArray();

        manager.board = board;
        manager.inputController = inputController;
        manager.ui = ui;
        manager.audioController = audioController;
        manager.restartButton = pauseRestartButton;
        manager.resultRestartButton = resultRestartButton;
        manager.howToPlayButton = howToPlayButton;
        manager.closeHowToPlayButton = closeHowToPlayButton;
        manager.pauseButton = pauseButton;
        manager.resumeButton = resumeButton;
        manager.hintButton = hintButton;
        manager.maxQuestions = 5;
        manager.randomizeQuestions = true;
        manager.gameTime = 120f;
        manager.scorePerCorrectAnswer = 10;
        manager.wrongPenalty = 1;
        manager.difficulty = SentenceWordSearchDifficulty.Hard;
        manager.allowReverseSelection = true;

        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The wind is _________.", answer = "STRONG" });
        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The sun is _________.", answer = "BRIGHT" });
        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "Ice feels _________.", answer = "COLD" });
        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A baby cat is called a _________.", answer = "KITTEN" });
        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "We drink _________.", answer = "WATER" });
        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The grass is _________.", answer = "GREEN" });
        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A bird can _________.", answer = "FLY" });
        manager.questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A fish can _________.", answer = "SWIM" });

        Selection.activeGameObject = systems;
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(board);
        EditorUtility.SetDirty(ui);
        EditorUtility.SetDirty(audioController);

        Debug.Log("Sentence Word Search V2.3 scene created. Install DOTween before pressing Play.");
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

    private static SentenceWordSearchCell CreateCellTemplate(Transform parent, List<TextMeshProUGUI> primaryTexts, List<TextMeshProUGUI> secondaryTexts)
    {
        GameObject cellObject = CreateUIObject("SentenceWordSearchCellTemplate", parent);
        Image bg = cellObject.AddComponent<Image>();
        bg.color = Color.white;
        bg.raycastTarget = false;

        SentenceWordSearchCell cell = cellObject.AddComponent<SentenceWordSearchCell>();
        cell.rectTransform = cellObject.GetComponent<RectTransform>();
        cell.backgroundImage = bg;

        Image solved = CreateChildOverlay("SolvedOverlay", cellObject.transform, new Color(0.26f, 0.95f, 0.48f, 0.65f));
        Image hint = CreateChildOverlay("HintOverlay", cellObject.transform, new Color(0.35f, 0.72f, 1f, 0.7f));
        Image preview = CreateChildOverlay("PreviewOverlay", cellObject.transform, new Color(1f, 0.84f, 0.18f, 0.8f));
        Image wrong = CreateChildOverlay("WrongOverlay", cellObject.transform, new Color(1f, 0.25f, 0.25f, 0.75f));

        TextMeshProUGUI letter = CreateText("LetterText", cellObject.transform, "A", 38, TextAlignmentOptions.Center);
        Stretch(letter.gameObject);
        letter.raycastTarget = false;
        letter.fontStyle = FontStyles.Bold;
        primaryTexts.Add(letter);

        cell.previewOverlay = preview;
        cell.solvedOverlay = solved;
        cell.wrongOverlay = wrong;
        cell.hintOverlay = hint;
        cell.letterText = letter;

        solved.gameObject.SetActive(false);
        hint.gameObject.SetActive(false);
        preview.gameObject.SetActive(false);
        wrong.gameObject.SetActive(false);
        cellObject.SetActive(false);

        return cell;
    }

    private static Image CreateChildOverlay(string name, Transform parent, Color color)
    {
        GameObject obj = CreateUIObject(name, parent);
        Stretch(obj);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void CreateResultPanel(Transform parent, SentenceWordSearchUI ui, List<TextMeshProUGUI> primaryTexts, List<TextMeshProUGUI> secondaryTexts, out Button restartButton)
    {
        GameObject panel = CreateOverlayPanel("ResultPanel", parent);
        GameObject box = CreateCenteredBox(panel.transform, new Vector2(720, 520));

        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI title = CreateText("ResultTitleText", box.transform, "Great Job!", 48, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        primaryTexts.Add(title);

        TextMeshProUGUI score = CreateText("ResultScoreText", box.transform, "Score: 0", 36, TextAlignmentOptions.Center);
        secondaryTexts.Add(score);

        restartButton = CreateButton("ResultRestartButton", box.transform, "Play Again", secondaryTexts, 30);

        ui.resultPanel = panel;
        ui.resultTitleText = title;
        ui.resultScoreText = score;
        panel.SetActive(false);
    }

    private static void CreateHowToPlayPanel(Transform parent, SentenceWordSearchUI ui, List<TextMeshProUGUI> primaryTexts, List<TextMeshProUGUI> secondaryTexts, out Button closeButton)
    {
        GameObject panel = CreateOverlayPanel("HowToPlayPanel", parent);
        GameObject box = CreateCenteredBox(panel.transform, new Vector2(780, 560));

        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(42, 42, 42, 42);
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI title = CreateText("HowToTitleText", box.transform, "How To Play", 46, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        primaryTexts.Add(title);

        TextMeshProUGUI body = CreateText("HowToBodyText", box.transform, "Read the sentence.\nFind the missing word in the same grid.\nDrag straight across letters to select the word.\nUse Hint to pulse the first and last letters.", 30, TextAlignmentOptions.Center);
        secondaryTexts.Add(body);

        closeButton = CreateButton("CloseHowToPlayButton", box.transform, "Start", secondaryTexts, 30);

        ui.howToPlayPanel = panel;
        panel.SetActive(true);
    }

    private static void CreatePausePanel(Transform parent, SentenceWordSearchUI ui, List<TextMeshProUGUI> primaryTexts, List<TextMeshProUGUI> secondaryTexts, out Button resumeButton, out Button restartButton)
    {
        GameObject panel = CreateOverlayPanel("PausePanel", parent);
        GameObject box = CreateCenteredBox(panel.transform, new Vector2(640, 460));

        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(42, 42, 42, 42);
        layout.spacing = 24;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TextMeshProUGUI title = CreateText("PauseTitleText", box.transform, "Paused", 48, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        primaryTexts.Add(title);

        resumeButton = CreateButton("ResumeButton", box.transform, "Resume", secondaryTexts, 30);
        restartButton = CreateButton("PauseRestartButton", box.transform, "Restart", secondaryTexts, 30);

        ui.pausePanel = panel;
        panel.SetActive(false);
    }

    private static TextMeshProUGUI CreateInfoBox(string name, Transform parent, string label, List<TextMeshProUGUI> secondaryTexts)
    {
        GameObject box = CreateUIObject(name, parent);
        Image image = box.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.88f);
        AddFlexible(box, 0, 60, 1);

        TextMeshProUGUI text = CreateText("Text", box.transform, label, 27, TextAlignmentOptions.Center);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.gameObject);
        secondaryTexts.Add(text);
        return text;
    }

    private static GameObject CreateOverlayPanel(string name, Transform parent)
    {
        GameObject panel = CreateUIObject(name, parent);
        Stretch(panel);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.65f);
        return panel;
    }

    private static GameObject CreateCenteredBox(Transform parent, Vector2 size)
    {
        GameObject box = CreateUIObject("Box", parent);
        RectTransform rect = box.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = box.AddComponent<Image>();
        image.color = Color.white;
        return box;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions alignment)
    {
        GameObject obj = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = new Color(0.08f, 0.1f, 0.16f);
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
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

    private static Button CreateButton(string name, Transform parent, string label, List<TextMeshProUGUI> secondaryTexts, float fontSize)
    {
        GameObject buttonObject = CreateUIObject(name, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.42f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        TextMeshProUGUI labelText = CreateText("Label", buttonObject.transform, label, fontSize, TextAlignmentOptions.Center);
        labelText.color = Color.white;
        labelText.fontStyle = FontStyles.Bold;
        Stretch(labelText.gameObject);
        secondaryTexts.Add(labelText);

        AddLayout(buttonObject, 0, 64);
        return button;
    }

    private static void CreateSpacer(Transform parent, float width, float height)
    {
        GameObject spacer = CreateUIObject("Spacer", parent);
        AddLayout(spacer, width, height);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void Stretch(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
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

    private static void AddFlexible(GameObject obj, float preferredWidth, float preferredHeight, float flexibleWidth)
    {
        AddLayout(obj, preferredWidth, preferredHeight);
        LayoutElement layout = obj.GetComponent<LayoutElement>();
        layout.flexibleWidth = flexibleWidth;
    }
}

#endif
