#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WordFillSceneBuilder
{
    private const string RootFolder = "Assets/WordFillGame";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string LetterPrefabPath = PrefabFolder + "/LetterTilePrefab.prefab";

    [MenuItem("Tools/Word Fill Game/Create V5 How To Play Scene UI")]
    public static void CreateFinalSceneUI()
    {
        EnsureFolder("Assets", "WordFillGame");
        EnsureFolder(RootFolder, "Prefabs");

        LetterTile letterPrefab = CreateOrLoadLetterTilePrefab();

        Canvas canvas = CreateCanvas();
        CreateEventSystemIfMissing();

        GameObject root = CreateUIObject("WordFillGameRoot", canvas.transform);
        Stretch(root.GetComponent<RectTransform>());
        AddVerticalLayout(root, 14, 24, TextAnchor.UpperCenter);

        GameObject topPanel = CreateUIObject("TopPanel", root.transform);
        SetSize(topPanel.GetComponent<RectTransform>(), 0, 105);
        AddHorizontalLayout(topPanel, 12, 8, TextAnchor.MiddleCenter);

        TMP_Text gameHeadingText = CreateText("GameHeadingText", topPanel.transform, "Affirmation Words", 36, TextAlignmentOptions.Left);
        TMP_Text scoreText = CreateText("ScoreText", topPanel.transform, "Score: 0", 28, TextAlignmentOptions.Left);
        TMP_Text timerText = CreateText("TimerText", topPanel.transform, "01:00", 40, TextAlignmentOptions.Center);
        Button howToButton = CreateButton("HowToPlayButton", topPanel.transform, "How?", 26);
        Button hintButton = CreateButton("HintButton_TopBar", topPanel.transform, "Hint", 26);
        Button pauseButton = CreateButton("PauseButton", topPanel.transform, "Pause", 26);
        TMP_Text feedbackText = CreateText("FeedbackText", topPanel.transform, "", 26, TextAlignmentOptions.Right);

        AddLayoutElement(gameHeadingText.gameObject, 360, 78, 1);
        AddLayoutElement(scoreText.gameObject, 200, 78, 0);
        AddLayoutElement(timerText.gameObject, 165, 78, 0);
        AddLayoutElement(howToButton.gameObject, 120, 66, 0);
        AddLayoutElement(hintButton.gameObject, 120, 66, 0);
        AddLayoutElement(pauseButton.gameObject, 135, 66, 0);
        AddLayoutElement(feedbackText.gameObject, 230, 78, 0);

        TMP_Text objectiveText = CreateText("GameObjectiveText", root.transform, "Fill in the missing letters to complete the affirmation.", 30, TextAlignmentOptions.Center);
        AddLayoutElement(objectiveText.gameObject, 0, 56, 0);

        GameObject centerPanel = CreateUIObject("CenterPanel", root.transform);
        AddLayoutElement(centerPanel, 0, 560, 1);
        AddVerticalLayout(centerPanel, 12, 12, TextAnchor.MiddleCenter);

        Image clueImage = CreateImage("ClueImage_Drag_AanyaSprite_Here", centerPanel.transform);
        AddLayoutElement(clueImage.gameObject, 500, 300, 0);

        TMP_Text clueText = CreateText("HiddenHintText", centerPanel.transform, "Aanya is showing courage while facing a challenge.", 30, TextAlignmentOptions.Center);
        CanvasGroup clueCanvasGroup = clueText.gameObject.AddComponent<CanvasGroup>();
        clueText.gameObject.SetActive(false);
        AddLayoutElement(clueText.gameObject, 950, 72, 0);

        TMP_Text wordText = CreateText("WordText", centerPanel.transform, "I am b _ _ _ _", 50, TextAlignmentOptions.Center);
        AddLayoutElement(wordText.gameObject, 950, 90, 0);

        GameObject bottomPanel = CreateUIObject("BottomPanel", root.transform);
        SetSize(bottomPanel.GetComponent<RectTransform>(), 0, 250);
        AddVerticalLayout(bottomPanel, 15, 10, TextAnchor.MiddleCenter);

        GameObject letterParent = CreateUIObject("LetterButtonParent", bottomPanel.transform);
        GridLayoutGroup grid = letterParent.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(92, 80);
        grid.spacing = new Vector2(16, 14);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;
        grid.childAlignment = TextAnchor.MiddleCenter;
        AddLayoutElement(letterParent, 880, 120, 0);

        GameObject controls = CreateUIObject("ControlButtons", bottomPanel.transform);
        AddHorizontalLayout(controls, 20, 0, TextAnchor.MiddleCenter);
        AddLayoutElement(controls, 700, 85, 0);

        Button backspaceButton = CreateButton("BackspaceButton", controls.transform, "Backspace", 30);
        Button clearButton = CreateButton("ClearButton", controls.transform, "Clear", 30);
        AddLayoutElement(backspaceButton.gameObject, 230, 72, 0);
        AddLayoutElement(clearButton.gameObject, 180, 72, 0);

        TMP_Text centerFeedbackText = CreateCenterFeedback(canvas.transform, out CanvasGroup centerFeedbackCanvasGroup);
        GameObject howToPanelRoot = CreateHowToPlayPanel(canvas.transform, out WordFillHowToPlayPanel howToPanel);
        GameObject pausePanel = CreatePausePanel(canvas.transform, out TMP_Text pauseTitleText, out Button continueButton);
        GameObject completePanel = CreateCompletePanel(canvas.transform, out TMP_Text completeTitleText, out TMP_Text completeBodyText, out Button playAgainButton);

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
        so.FindProperty("gameHeading").stringValue = "Affirmation Words";
        so.FindProperty("gameHeadingText").objectReferenceValue = gameHeadingText;
        so.FindProperty("gameObjectiveLine").stringValue = "Fill in the missing letters to complete the affirmation.";
        so.FindProperty("gameObjectiveText").objectReferenceValue = objectiveText;
        so.FindProperty("clueImage").objectReferenceValue = clueImage;
        so.FindProperty("clueText").objectReferenceValue = clueText;
        so.FindProperty("clueTextCanvasGroup").objectReferenceValue = clueCanvasGroup;
        so.FindProperty("wordText").objectReferenceValue = wordText;
        so.FindProperty("scoreText").objectReferenceValue = scoreText;
        so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
        so.FindProperty("timerText").objectReferenceValue = timerText;
        so.FindProperty("howToPlayButton").objectReferenceValue = howToButton;
        so.FindProperty("hintButton").objectReferenceValue = hintButton;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
        so.FindProperty("uiAnimator").objectReferenceValue = animator;
        so.FindProperty("audioManager").objectReferenceValue = audioManager;
        so.FindProperty("howToPlayPanel").objectReferenceValue = howToPanel;
        so.FindProperty("letterButtonParent").objectReferenceValue = letterParent.transform;
        so.FindProperty("letterTilePrefab").objectReferenceValue = letterPrefab;
        so.FindProperty("backspaceButton").objectReferenceValue = backspaceButton;
        so.FindProperty("clearButton").objectReferenceValue = clearButton;
        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.FindProperty("pauseTitleText").objectReferenceValue = pauseTitleText;
        so.FindProperty("continueButton").objectReferenceValue = continueButton;
        so.FindProperty("completePanel").objectReferenceValue = completePanel;
        so.FindProperty("completeTitleText").objectReferenceValue = completeTitleText;
        so.FindProperty("completeBodyText").objectReferenceValue = completeBodyText;
        so.FindProperty("playAgainButton").objectReferenceValue = playAgainButton;
        so.FindProperty("questionsPerRound").intValue = 5;
        so.FindProperty("maxTimeSeconds").floatValue = 60f;
        so.FindProperty("randomQuestionOrder").boolValue = true;
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

        Debug.Log("Word Fill Game V5 How To Play layout created. Add DOTween, sprites, narration clips, how-to images, fonts, SFX, and music.");
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
        TMP_Text instruction = CreateText("HowToInstructionText", card.transform, "Look at the picture and fill in the missing letters.", 32, TextAlignmentOptions.Center);
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

    private static GameObject CreateCompletePanel(Transform parent, out TMP_Text titleText, out TMP_Text bodyText, out Button playAgainButton)
    {
        GameObject panel = new GameObject("CompletePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create Complete Panel");
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        GameObject card = CreateCenteredCard("ResultCard", panel.transform, new Vector2(720, 530));
        AddVerticalLayout(card, 22, 35, TextAnchor.MiddleCenter);

        titleText = CreateText("CompleteTitleText", card.transform, "Game Complete!", 46, TextAlignmentOptions.Center);
        bodyText = CreateText("CompleteBodyText", card.transform, "Correct Answers: 5 / 5\nWrong Attempts: 0\nHints Used: 0\nHint Penalty: -0\nFinal Score: 50\nTime Used: 35 / 60 sec", 32, TextAlignmentOptions.Center);
        playAgainButton = CreateButton("PlayAgainButton", card.transform, "Play Again", 32);

        AddLayoutElement(titleText.gameObject, 640, 75, 0);
        AddLayoutElement(bodyText.gameObject, 640, 285, 0);
        AddLayoutElement(playAgainButton.gameObject, 260, 75, 0);

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

    private static LetterTile CreateOrLoadLetterTilePrefab()
    {
        LetterTile existing = AssetDatabase.LoadAssetAtPath<LetterTile>(LetterPrefabPath);
        if (existing != null)
            return existing;

        GameObject buttonObject = new GameObject("LetterTilePrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
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

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonObject, LetterPrefabPath);
        Object.DestroyImmediate(buttonObject);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return prefab.GetComponent<LetterTile>();
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

    private static void SetSize(RectTransform rect, float width, float height)
    {
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;

        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
