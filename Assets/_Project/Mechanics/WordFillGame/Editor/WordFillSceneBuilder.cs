#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WordFillSceneBuilder
{
    [MenuItem("Tools/Word Fill Game/Create V7 Final Layout Scene UI")]
    public static void CreateV7SceneUI()
    {
        Canvas canvas = CreateCanvas();
        CreateEventSystemIfMissing();

        GameObject root = CreateUIObject("WordFillGameRoot", canvas.transform);
        Stretch(root.GetComponent<RectTransform>());

        WordFillFontApplier fontApplier = root.AddComponent<WordFillFontApplier>();

        Button howToButton = CreateBottomCornerButton("HowToPlayButton_BottomLeft", root.transform, "How?", true);
        Button pauseButton = CreateBottomCornerButton("PauseButton_BottomRight", root.transform, "Pause", false);

        GameObject gameplayArea = CreateUIObject("MainGameplayArea", root.transform);
        RectTransform gameplayRect = gameplayArea.GetComponent<RectTransform>();
        gameplayRect.anchorMin = new Vector2(0.04f, 0.08f);
        gameplayRect.anchorMax = new Vector2(0.96f, 0.96f);
        gameplayRect.offsetMin = Vector2.zero;
        gameplayRect.offsetMax = Vector2.zero;
        AddVerticalLayout(gameplayArea, 14, 14, TextAnchor.UpperCenter);

        GameObject topBar = CreateUIObject("TopBarSingleLine", gameplayArea.transform);
        AddHorizontalLayout(topBar, 16, 12, TextAnchor.MiddleCenter);
        AddLayoutElement(topBar, 0, 82, 0);

        TMP_Text instructionText = CreateText("InstructionLineText", topBar.transform, "Fill in the missing letters to complete the affirmation.", 30, TextAlignmentOptions.Left);
        TMP_Text scoreText = CreateText("ScoreText", topBar.transform, "Score: 0", 28, TextAlignmentOptions.Center);
        TMP_Text timerText = CreateText("TimerText", topBar.transform, "01:00", 38, TextAlignmentOptions.Center);
        Button hintButton = CreateButton("HintButton_TopBar", topBar.transform, "Hint", 26);
        TMP_Text feedbackText = CreateText("FeedbackText", topBar.transform, "", 26, TextAlignmentOptions.Right);

        AddLayoutElement(instructionText.gameObject, 560, 66, 1);
        AddLayoutElement(scoreText.gameObject, 170, 66, 0);
        AddLayoutElement(timerText.gameObject, 150, 66, 0);
        AddLayoutElement(hintButton.gameObject, 120, 60, 0);
        AddLayoutElement(feedbackText.gameObject, 200, 66, 0);

        GameObject centerPanel = CreateUIObject("CenterPanel", gameplayArea.transform);
        AddLayoutElement(centerPanel, 0, 570, 1);
        AddVerticalLayout(centerPanel, 12, 10, TextAnchor.MiddleCenter);

        Image clueImage = CreateImage("ClueImage_DragSpriteHere", centerPanel.transform);
        AddLayoutElement(clueImage.gameObject, 520, 310, 0);

        TMP_Text clueText = CreateText("HiddenHintText", centerPanel.transform, "Aanya is showing courage while facing a challenge.", 30, TextAlignmentOptions.Center);
        CanvasGroup clueCanvasGroup = clueText.gameObject.AddComponent<CanvasGroup>();
        clueText.gameObject.SetActive(false);
        AddLayoutElement(clueText.gameObject, 950, 72, 0);

        TMP_Text wordText = CreateText("WordText", centerPanel.transform, "I am b _ _ _ _", 50, TextAlignmentOptions.Center);
        AddLayoutElement(wordText.gameObject, 950, 90, 0);

        GameObject bottomPanel = CreateUIObject("BottomPanel", gameplayArea.transform);
        AddLayoutElement(bottomPanel, 0, 235, 0);
        AddVerticalLayout(bottomPanel, 14, 8, TextAnchor.MiddleCenter);

        GameObject letterParent = CreateUIObject("LetterButtonParent", bottomPanel.transform);
        GridLayoutGroup grid = letterParent.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(92, 80);
        grid.spacing = new Vector2(16, 14);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.childAlignment = TextAnchor.MiddleCenter;
        AddLayoutElement(letterParent, 880, 122, 0);

        GameObject controls = CreateUIObject("ControlButtons", bottomPanel.transform);
        AddHorizontalLayout(controls, 22, 0, TextAnchor.MiddleCenter);
        AddLayoutElement(controls, 700, 78, 0);

        Button backspaceButton = CreateButton("BackspaceButton", controls.transform, "Backspace", 30);
        Button clearButton = CreateButton("ClearButton", controls.transform, "Clear", 30);
        AddLayoutElement(backspaceButton.gameObject, 230, 70, 0);
        AddLayoutElement(clearButton.gameObject, 180, 70, 0);

        GameObject sceneTemplates = CreateUIObject("SceneTemplates_DoNotDelete", root.transform);
        sceneTemplates.SetActive(false);
        LetterTile letterTemplate = CreateLetterTileTemplate(sceneTemplates.transform);

        TMP_Text centerFeedbackText = CreateCenterFeedback(canvas.transform, out CanvasGroup centerFeedbackCanvasGroup);

        GameObject loadingPanel = CreateLoadingPanel(canvas.transform, out WordFillLoadingPanel loadingPanelComponent);
        GameObject howToPanelRoot = CreateHowToPlayPanel(canvas.transform, out WordFillHowToPlayPanel howToPanel);
        GameObject pausePanel = CreatePausePanel(canvas.transform, out TMP_Text pauseTitleText, out Button pauseContinueButton);
        GameObject completePanel = CreateCompletePanel(canvas.transform, out TMP_Text completeTitleText, out TMP_Text completeBodyText, out Button playAgainButton, out Button completeContinueButton);

        loadingPanel.SetActive(false);
        howToPanelRoot.SetActive(false);
        pausePanel.SetActive(false);
        completePanel.SetActive(false);

        GameObject animatorObject = new GameObject("WordFillUIAnimator");
        Undo.RegisterCreatedObjectUndo(animatorObject, "Create UI Animator");
        WordFillUIAnimator animator = animatorObject.AddComponent<WordFillUIAnimator>();

        SerializedObject animatorSo = new SerializedObject(animator);
        animatorSo.FindProperty("centerFeedbackText").objectReferenceValue = centerFeedbackText;
        animatorSo.FindProperty("centerFeedbackCanvasGroup").objectReferenceValue = centerFeedbackCanvasGroup;
        animatorSo.ApplyModifiedProperties();

        GameObject audioObject = new GameObject("WordFillAudioManager");
        Undo.RegisterCreatedObjectUndo(audioObject, "Create Audio Manager");
        WordFillAudioManager audioManager = audioObject.AddComponent<WordFillAudioManager>();

        GameObject controllerObject = new GameObject("WordFillGameController");
        Undo.RegisterCreatedObjectUndo(controllerObject, "Create Word Fill Controller");
        WordFillGameController controller = controllerObject.AddComponent<WordFillGameController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("fontApplier").objectReferenceValue = fontApplier;
        so.FindProperty("gameInstructionLine").stringValue = "Fill in the missing letters to complete the affirmation.";
        so.FindProperty("gameInstructionText").objectReferenceValue = instructionText;

        so.FindProperty("clueImage").objectReferenceValue = clueImage;
        so.FindProperty("clueText").objectReferenceValue = clueText;
        so.FindProperty("clueTextCanvasGroup").objectReferenceValue = clueCanvasGroup;
        so.FindProperty("wordText").objectReferenceValue = wordText;
        so.FindProperty("scoreText").objectReferenceValue = scoreText;
        so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        so.FindProperty("timerText").objectReferenceValue = timerText;

        so.FindProperty("howToPlayButton").objectReferenceValue = howToButton;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
        so.FindProperty("hintButton").objectReferenceValue = hintButton;

        so.FindProperty("uiAnimator").objectReferenceValue = animator;
        so.FindProperty("audioManager").objectReferenceValue = audioManager;
        so.FindProperty("howToPlayPanel").objectReferenceValue = howToPanel;
        so.FindProperty("loadingPanel").objectReferenceValue = loadingPanelComponent;

        so.FindProperty("letterTileTemplate").objectReferenceValue = letterTemplate;
        so.FindProperty("letterButtonParent").objectReferenceValue = letterParent.transform;

        so.FindProperty("backspaceButton").objectReferenceValue = backspaceButton;
        so.FindProperty("clearButton").objectReferenceValue = clearButton;

        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.FindProperty("pauseTitleText").objectReferenceValue = pauseTitleText;
        so.FindProperty("continueButton").objectReferenceValue = pauseContinueButton;

        so.FindProperty("completePanel").objectReferenceValue = completePanel;
        so.FindProperty("completeTitleText").objectReferenceValue = completeTitleText;
        so.FindProperty("completeBodyText").objectReferenceValue = completeBodyText;
        so.FindProperty("playAgainButton").objectReferenceValue = playAgainButton;
        so.FindProperty("completeContinueButton").objectReferenceValue = completeContinueButton;

        so.FindProperty("questionsPerRound").intValue = 5;
        so.FindProperty("maxTimeSeconds").floatValue = 60f;
        so.FindProperty("randomQuestionOrder").boolValue = true;
        so.FindProperty("showLoadingPanelOnRoundStart").boolValue = true;
        so.FindProperty("showHowToPlayOnRoundStart").boolValue = true;
        so.FindProperty("timerWarningSeconds").floatValue = 10f;
        so.FindProperty("hintPenaltyPoints").intValue = 5;
        so.FindProperty("nextQuestionDelay").floatValue = 0.45f;
        so.FindProperty("fallbackNarrationDuration").floatValue = 1.2f;

        SerializedProperty questions = so.FindProperty("questions");
        questions.arraySize = 8;
        SetQuestion(questions.GetArrayElementAtIndex(0), "Aanya is showing courage while facing a challenge.", "brave", "I am brave.", 10, 2);
        SetQuestion(questions.GetArrayElementAtIndex(1), "Rishi is extremely happy and joyous.", "blissful", "I am blissful.", 10, 3);
        SetQuestion(questions.GetArrayElementAtIndex(2), "Aanya is using imagination while writing something original.", "creative", "I am creative.", 10, 3);
        SetQuestion(questions.GetArrayElementAtIndex(3), "Rishi feels fully satisfied after working hard.", "fulfilled", "I am fulfilled.", 10, 3);
        SetQuestion(questions.GetArrayElementAtIndex(4), "Aanya is thanking Rishi and feeling gratitude.", "grateful", "I am grateful.", 10, 3);
        SetQuestion(questions.GetArrayElementAtIndex(5), "Aanya is aware and conscious of what is happening.", "mindful", "I am mindful.", 10, 3);
        SetQuestion(questions.GetArrayElementAtIndex(6), "Rishi is sitting calmly and peacefully in a park.", "peaceful", "I am peaceful.", 10, 3);
        SetQuestion(questions.GetArrayElementAtIndex(7), "Aanya is full of great energy and enthusiasm.", "zealous", "I am zealous.", 10, 3);

        so.ApplyModifiedProperties();

        Selection.activeGameObject = controllerObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("Word Fill Game V7 final layout created. No prefab asset is created; letter tiles use an inactive scene template.");
    }

    private static Button CreateBottomCornerButton(string name, Transform parent, string label, bool left)
    {
        Button button = CreateButton(name, parent, label, 26);
        RectTransform rect = button.GetComponent<RectTransform>();

        rect.anchorMin = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rect.anchorMax = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rect.pivot = left ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(140f, 66f);
        rect.anchoredPosition = left ? new Vector2(24f, 24f) : new Vector2(-24f, 24f);

        return button;
    }

    private static GameObject CreateLoadingPanel(Transform parent, out WordFillLoadingPanel panelComponent)
    {
        GameObject panel = new GameObject("LoadingPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create Loading Panel");
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0.92f, 0.88f, 1f, 1f);

        GameObject card = CreateCenteredCard("LoadingCard", panel.transform, new Vector2(760, 430));
        AddVerticalLayout(card, 28, 45, TextAnchor.MiddleCenter);

        TMP_Text gameName = CreateText("LoadingGameNameText", card.transform, "Affirmation Words", 58, TextAlignmentOptions.Center);
        AddLayoutElement(gameName.gameObject, 680, 120, 0);

        GameObject sliderObject = new GameObject("LoadingSlider", typeof(RectTransform), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(sliderObject, "Create Loading Slider");
        sliderObject.transform.SetParent(card.transform, false);
        AddLayoutElement(sliderObject, 560, 42, 0);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.transition = Selectable.Transition.None;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = new Color(0.75f, 0.68f, 0.95f, 0.45f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = new Color(0.47f, 0.30f, 0.85f, 1f);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.targetGraphic = fill.GetComponent<Image>();

        TMP_Text loadingText = CreateText("LoadingLineText", card.transform, "Loading", 30, TextAlignmentOptions.Center);
        AddLayoutElement(loadingText.gameObject, 680, 70, 0);

        panelComponent = panel.AddComponent<WordFillLoadingPanel>();
        SerializedObject so = new SerializedObject(panelComponent);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("gameNameText").objectReferenceValue = gameName;
        so.FindProperty("loadingSlider").objectReferenceValue = slider;
        so.FindProperty("loadingLineText").objectReferenceValue = loadingText;
        so.FindProperty("gameName").stringValue = "Affirmation Words";
        so.FindProperty("loadingBaseText").stringValue = "Loading";
        so.FindProperty("loadingDuration").floatValue = 1.5f;
        so.FindProperty("dotAnimationSpeed").floatValue = 0.35f;
        so.ApplyModifiedProperties();

        return panel;
    }

    private static GameObject CreateHowToPlayPanel(Transform parent, out WordFillHowToPlayPanel panelComponent)
    {
        GameObject panel = new GameObject("HowToPlayPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create How To Play Panel");
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        GameObject card = CreateCenteredCard("HowToPlayCard", panel.transform, new Vector2(860, 720));
        AddVerticalLayout(card, 18, 35, TextAnchor.MiddleCenter);

        TMP_Text title = CreateText("HowToTitleText", card.transform, "How To Play", 46, TextAlignmentOptions.Center);
        TMP_Text instruction = CreateText("HowToInstructionText", card.transform, "Look carefully at the picture and fill the word.", 32, TextAlignmentOptions.Center);
        Image image = CreateImage("HowToInstructionImage", card.transform);
        TMP_Text page = CreateText("HowToPageText", card.transform, "1 / 3", 26, TextAlignmentOptions.Center);

        AddLayoutElement(title.gameObject, 760, 70, 0);
        AddLayoutElement(instruction.gameObject, 760, 100, 0);
        AddLayoutElement(image.gameObject, 700, 340, 0);
        AddLayoutElement(page.gameObject, 760, 42, 0);

        GameObject buttons = CreateUIObject("HowToButtons", card.transform);
        AddHorizontalLayout(buttons, 22, 0, TextAnchor.MiddleCenter);
        AddLayoutElement(buttons, 760, 80, 0);

        Button previous = CreateButton("PreviousButton", buttons.transform, "Prev", 30);
        Button next = CreateButton("NextButton", buttons.transform, "Next", 30);
        Button continueButton = CreateButton("ContinueButton", buttons.transform, "Continue", 30);

        AddLayoutElement(previous.gameObject, 180, 70, 0);
        AddLayoutElement(next.gameObject, 180, 70, 0);
        AddLayoutElement(continueButton.gameObject, 220, 70, 0);

        panelComponent = panel.AddComponent<WordFillHowToPlayPanel>();
        SerializedObject so = new SerializedObject(panelComponent);
        so.FindProperty("panelRoot").objectReferenceValue = panel;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("instructionText").objectReferenceValue = instruction;
        so.FindProperty("instructionImage").objectReferenceValue = image;
        so.FindProperty("pageText").objectReferenceValue = page;
        so.FindProperty("previousButton").objectReferenceValue = previous;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.FindProperty("panelTitle").stringValue = "How To Play";

        SerializedProperty steps = so.FindProperty("steps");
        steps.arraySize = 3;
        SetHowToStep(steps.GetArrayElementAtIndex(0), "Look carefully at the picture.", null);
        SetHowToStep(steps.GetArrayElementAtIndex(1), "Tap the letter tiles to complete the missing word.", null);
        SetHowToStep(steps.GetArrayElementAtIndex(2), "Use Hint if you need help, but it reduces your score.", null);

        so.ApplyModifiedProperties();
        return panel;
    }

    private static LetterTile CreateLetterTileTemplate(Transform parent)
    {
        GameObject buttonObject = new GameObject("LetterTileTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonObject, "Create Letter Tile Template");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(92, 80);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        GameObject textObject = new GameObject("LetterText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        Stretch(textObject.GetComponent<RectTransform>());

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = "A";
        tmp.fontSize = 38;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        LetterTile tile = buttonObject.AddComponent<LetterTile>();
        SerializedObject so = new SerializedObject(tile);
        so.FindProperty("letterText").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();

        buttonObject.SetActive(false);
        return tile;
    }

    private static void SetHowToStep(SerializedProperty step, string instruction, Sprite image)
    {
        step.FindPropertyRelative("instructionText").stringValue = instruction;
        step.FindPropertyRelative("instructionImage").objectReferenceValue = image;
    }

    private static void SetQuestion(SerializedProperty question, string clue, string answer, string completedLine, int points, int extraLetters)
    {
        question.FindPropertyRelative("questionSprite").objectReferenceValue = null;
        question.FindPropertyRelative("clueText").stringValue = clue;
        question.FindPropertyRelative("answerWord").stringValue = answer;
        question.FindPropertyRelative("completedLineText").stringValue = completedLine;
        question.FindPropertyRelative("completedLineNarration").objectReferenceValue = null;
        question.FindPropertyRelative("points").intValue = points;
        question.FindPropertyRelative("extraLetters").intValue = extraLetters;
    }

    private static TMP_Text CreateCenterFeedback(Transform parent, out CanvasGroup canvasGroup)
    {
        GameObject obj = new GameObject("CenterFeedbackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(obj, "Create Center Feedback");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(540, 190);
        rect.anchoredPosition = Vector2.zero;

        TMP_Text text = obj.GetComponent<TextMeshProUGUI>();
        text.text = "+10\nCorrect!";
        text.fontSize = 56;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.05f, 0.45f, 0.1f, 1f);

        canvasGroup = obj.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        obj.SetActive(false);

        return text;
    }

    private static GameObject CreatePausePanel(Transform parent, out TMP_Text titleText, out Button continueButton)
    {
        GameObject panel = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create Pause Panel");
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = CreateCenteredCard("PauseCard", panel.transform, new Vector2(560, 320));
        AddVerticalLayout(card, 25, 35, TextAnchor.MiddleCenter);

        titleText = CreateText("PauseTitleText", card.transform, "Paused", 48, TextAlignmentOptions.Center);
        continueButton = CreateButton("ContinueButton", card.transform, "Continue", 34);

        AddLayoutElement(titleText.gameObject, 500, 100, 0);
        AddLayoutElement(continueButton.gameObject, 260, 80, 0);

        return panel;
    }

    private static GameObject CreateCompletePanel(Transform parent, out TMP_Text titleText, out TMP_Text bodyText, out Button playAgainButton, out Button completeContinueButton)
    {
        GameObject panel = new GameObject("CompletePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create Complete Panel");
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        GameObject card = CreateCenteredCard("ResultCard", panel.transform, new Vector2(760, 560));
        AddVerticalLayout(card, 18, 35, TextAnchor.MiddleCenter);

        titleText = CreateText("CompleteTitleText", card.transform, "Game Complete!", 46, TextAlignmentOptions.Center);
        bodyText = CreateText("CompleteBodyText", card.transform, "Correct Answers: 5 / 5\nWrong Attempts: 0\nHints Used: 0\nHint Penalty: -0\nFinal Score: 50\nTime Used: 35 / 60 sec", 32, TextAlignmentOptions.Center);

        GameObject buttons = CreateUIObject("CompleteButtons", card.transform);
        AddHorizontalLayout(buttons, 22, 0, TextAnchor.MiddleCenter);

        playAgainButton = CreateButton("PlayAgainButton", buttons.transform, "Play Again", 32);
        completeContinueButton = CreateButton("CompleteContinueButton", buttons.transform, "Continue", 32);

        AddLayoutElement(titleText.gameObject, 660, 75, 0);
        AddLayoutElement(bodyText.gameObject, 660, 285, 0);
        AddLayoutElement(buttons, 660, 82, 0);
        AddLayoutElement(playAgainButton.gameObject, 240, 75, 0);
        AddLayoutElement(completeContinueButton.gameObject, 240, 75, 0);

        return panel;
    }

    private static GameObject CreateCenteredCard(string name, Transform parent, Vector2 size)
    {
        GameObject card = CreateUIObject(name, parent);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;

        Image image = card.AddComponent<Image>();
        image.color = Color.white;

        return card;
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("WordFillCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Word Fill Canvas");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreateEventSystemIfMissing()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create Event System");
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.black;
        text.enableWordWrapping = true;

        return text;
    }

    private static Image CreateImage(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.2f);
        image.preserveAspect = true;

        return image;
    }

    private static Button CreateButton(string name, Transform parent, string text, float fontSize)
    {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonObj, "Create " + name);
        buttonObj.transform.SetParent(parent, false);

        Image image = buttonObj.GetComponent<Image>();
        image.color = Color.white;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(buttonObj.transform, false);
        Stretch(textObj.GetComponent<RectTransform>());

        TextMeshProUGUI label = textObj.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;

        return buttonObj.GetComponent<Button>();
    }

    private static void AddVerticalLayout(GameObject obj, int spacing, int padding, TextAnchor alignment)
    {
        VerticalLayoutGroup layout = obj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void AddHorizontalLayout(GameObject obj, int spacing, int padding, TextAnchor alignment)
    {
        HorizontalLayoutGroup layout = obj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.childAlignment = alignment;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private static void AddLayoutElement(GameObject obj, float preferredWidth, float preferredHeight, float flexibleWidth)
    {
        LayoutElement element = obj.GetComponent<LayoutElement>();
        if (element == null)
            element = obj.AddComponent<LayoutElement>();

        if (preferredWidth > 0)
            element.preferredWidth = preferredWidth;

        if (preferredHeight > 0)
            element.preferredHeight = preferredHeight;

        element.flexibleWidth = flexibleWidth;
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
