#if UNITY_EDITOR
using ClockLearningGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ClockLearningGameEditor
{
    public static class ClockLearningSceneBuilder
    {
        private static readonly Color BackgroundColor = new Color(1f, 0.93f, 0.66f, 1f);
        private static readonly Color HeaderColor = new Color(1f, 0.81f, 0.32f, 1f);
        private static readonly Color PanelColor = new Color(1f, 0.97f, 0.84f, 1f);
        private static readonly Color ButtonColor = new Color(1f, 0.62f, 0.2f, 1f);
        private static readonly Color SecondaryButtonColor = new Color(0.92f, 0.82f, 0.58f, 1f);
        private static readonly Color TextColor = new Color(0.23f, 0.17f, 0.08f, 1f);

        [MenuItem("Tools/Clock Learning Game/Create Rough Working Scene")]
        public static void CreateRoughWorkingScene()
        {
            EnsureEventSystem();

            Canvas canvas = CreateCanvas();
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            Image background = canvas.gameObject.AddComponent<Image>();
            background.color = BackgroundColor;
            background.raycastTarget = false;

            RectTransform safeRoot = CreateRect("Safe Area Root", canvasRect, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.97f), Vector2.zero, Vector2.zero);

            CanvasGroup modeMenuGroup = CreateModeMenuPanel(safeRoot, out TextMeshProUGUI menuTitleText, out Button singleModeButton, out Button doubleModeButton);
            RectTransform gameplayRoot = CreateRect("Gameplay Root", safeRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform header = CreatePanel("Top Header", gameplayRoot, new Vector2(0f, 0.9f), new Vector2(1f, 1f), HeaderColor);
            Button homeButton = CreateIconButton(header, "Home Button", new Vector2(0.018f, 0.18f), new Vector2(0.058f, 0.82f), SecondaryButtonColor);
            TextMeshProUGUI titleText = CreateText(header, "Title Text", "Clock Game", new Vector2(0.32f, 0f), new Vector2(0.68f, 1f), 42, FontStyles.Bold, TextAlignmentOptions.Center);
            TextMeshProUGUI questionText = CreateTextCard(header, "Question Card", "Question Text", "Question 1/10", new Vector2(0.65f, 0.18f), new Vector2(0.77f, 0.82f), 25);
            TextMeshProUGUI scoreText = CreateTextCard(header, "Score Card", "Score Text", "Score 0", new Vector2(0.775f, 0.18f), new Vector2(0.875f, 0.82f), 25);
            Button pauseButton = CreateIconButton(header, "Pause Button", new Vector2(0.89f, 0.18f), new Vector2(0.935f, 0.82f), SecondaryButtonColor);
            Button helpButton = CreateIconButton(header, "Help Button", new Vector2(0.945f, 0.18f), new Vector2(0.99f, 0.82f), SecondaryButtonColor);

            RectTransform timerBar = CreatePanel("Timer Fill", header, new Vector2(0.18f, 0.12f), new Vector2(0.30f, 0.32f), new Color(1f, 0.73f, 0.25f, 1f));
            Image timerFill = timerBar.GetComponent<Image>();
            timerFill.type = Image.Type.Filled;
            timerFill.fillMethod = Image.FillMethod.Horizontal;
            TextMeshProUGUI timerText = CreateText(header, "Timer Text", "", new Vector2(0.18f, 0.35f), new Vector2(0.30f, 0.82f), 22, FontStyles.Bold, TextAlignmentOptions.Center);

            RectTransform singleRoot = CreateRect("Single Clock Mode Root", gameplayRoot, new Vector2(0f, 0f), new Vector2(1f, 0.89f), Vector2.zero, Vector2.zero);
            ClockLearningClockView singleClock = CreateClock(singleRoot, "Single Draggable Clock", new Vector2(0.06f, 0.09f), new Vector2(0.56f, 0.91f));
            RectTransform singlePanel = CreatePanel("Single Instruction Panel", singleRoot, new Vector2(0.61f, 0.12f), new Vector2(0.96f, 0.88f), PanelColor);
            TextMeshProUGUI singlePrompt = CreateTextCard(singlePanel, "Single Prompt BG", "Single Prompt", "Set the clock to", new Vector2(0.08f, 0.75f), new Vector2(0.92f, 0.91f), 34);
            TextMeshProUGUI singleTarget = CreateTextCard(singlePanel, "Single Target BG", "Single Target", "3:45 PM", new Vector2(0.08f, 0.53f), new Vector2(0.92f, 0.73f), 54);
            TextMeshProUGUI singleLegend = CreateTextCard(singlePanel, "Single Legend BG", "Single Legend", "Hour Hand = short hand\nMinute Hand = long hand", new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.49f), 25);
            Button singleSubmit = CreateButton(singlePanel, "Single Submit Button", "Submit", new Vector2(0.12f, 0.13f), new Vector2(0.88f, 0.28f), ButtonColor, 34);
            Button singleReset = CreateButton(singlePanel, "Single Reset Button", "Reset", new Vector2(0.25f, 0.035f), new Vector2(0.75f, 0.115f), SecondaryButtonColor, 25);

            RectTransform doubleRoot = CreateRect("Double Clock Mode Root", gameplayRoot, new Vector2(0f, 0f), new Vector2(1f, 0.89f), Vector2.zero, Vector2.zero);
            RectTransform diffPanel = CreatePanel("Difference Instruction Card", doubleRoot, new Vector2(0.27f, 0.76f), new Vector2(0.73f, 0.91f), PanelColor);
            TextMeshProUGUI diffPrompt = CreateText(diffPanel, "Difference Prompt", "Make a time difference of", new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.93f), 28, FontStyles.Bold, TextAlignmentOptions.Center);
            TextMeshProUGUI diffTarget = CreateTextCard(diffPanel, "Difference Target BG", "Difference Target", "2h 30m", new Vector2(0.18f, 0.06f), new Vector2(0.82f, 0.52f), 40);

            ClockLearningClockView clockA = CreateClock(doubleRoot, "Clock A", new Vector2(0.07f, 0.25f), new Vector2(0.42f, 0.75f));
            ClockLearningClockView clockB = CreateClock(doubleRoot, "Clock B", new Vector2(0.58f, 0.25f), new Vector2(0.93f, 0.75f));
            CreateText(doubleRoot, "Clock A Label", "Clock A", new Vector2(0.15f, 0.18f), new Vector2(0.34f, 0.24f), 24, FontStyles.Bold, TextAlignmentOptions.Center);
            CreateText(doubleRoot, "Clock B Label", "Clock B", new Vector2(0.66f, 0.18f), new Vector2(0.85f, 0.24f), 24, FontStyles.Bold, TextAlignmentOptions.Center);

            TextMeshProUGUI clockALabel;
            TextMeshProUGUI clockBLabel;
            Toggle clockAToggle = CreateToggle(doubleRoot, "Clock A AM PM Toggle", "AM", new Vector2(0.20f, 0.10f), new Vector2(0.30f, 0.17f), out clockALabel);
            Toggle clockBToggle = CreateToggle(doubleRoot, "Clock B AM PM Toggle", "PM", new Vector2(0.71f, 0.10f), new Vector2(0.81f, 0.17f), out clockBLabel);
            clockBToggle.isOn = true;

            TextMeshProUGUI diffChip = CreateText(doubleRoot, "Difference Chip", "Difference Target: 2h 30m", new Vector2(0.39f, 0.16f), new Vector2(0.61f, 0.23f), 24, FontStyles.Bold, TextAlignmentOptions.Center);
            Button doubleSubmit = CreateButton(doubleRoot, "Double Submit Button", "Submit", new Vector2(0.40f, 0.05f), new Vector2(0.60f, 0.135f), ButtonColor, 32);
            Button doubleReset = CreateButton(doubleRoot, "Double Reset Button", "Reset", new Vector2(0.44f, 0.005f), new Vector2(0.56f, 0.045f), SecondaryButtonColor, 22);
            doubleRoot.gameObject.SetActive(false);
            gameplayRoot.gameObject.SetActive(false);

            CanvasGroup feedbackGroup = CreatePopupPanel(safeRoot, "Feedback Panel", new Vector2(0.34f, 0.41f), new Vector2(0.66f, 0.59f), out TextMeshProUGUI feedbackText);
            feedbackText.text = "Great job!";
            feedbackText.fontSize = 46;

            CanvasGroup pauseGroup = CreatePausePanel(safeRoot, "Pause Panel", "Paused", out Button resumeButton, out Button restartButton, out Button howToButton, out Button pauseHomeButton);
            CanvasGroup howToGroup = CreateHowToPanel(safeRoot, out TextMeshProUGUI howToText, out Image howToImage, out TextMeshProUGUI pageCounterText, out Button previousHowToButton, out Button nextHowToButton, out Button closeHowToButton);
            CanvasGroup resultGroup = CreateResultPanel(safeRoot, out TextMeshProUGUI resultTitle, out TextMeshProUGUI resultScore, out Button resultRestart, out Button resultHome);
            CanvasGroup tutorialGroup = CreateInteractiveTutorialOverlay(safeRoot, out Image tutorialBackgroundImage, out Button tutorialClickAnywhereButton, out RectTransform tutorialPointer, out Image tutorialPointerImage, out RectTransform tutorialGhostHand, out Image tutorialGhostHandImage, out RectTransform tutorialPromptCard, out TextMeshProUGUI tutorialPrompt);

            GameObject managerObject = new GameObject("ClockLearningGameManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create Clock Learning Manager");
            ClockLearningAudioManager audioManager = managerObject.AddComponent<ClockLearningAudioManager>();
            managerObject.AddComponent<AudioSource>();
            managerObject.AddComponent<AudioSource>();
            ClockLearningGameManager manager = managerObject.AddComponent<ClockLearningGameManager>();
            ClockLearningTutorialController tutorialController = managerObject.AddComponent<ClockLearningTutorialController>();

            SerializedObject so = new SerializedObject(manager);
            SetObj(so, "gameplayRoot", gameplayRoot.gameObject);
            SetObj(so, "modeMenuPanelGroup", modeMenuGroup);
            SetObj(so, "modeMenuTitleText", menuTitleText);
            SetObj(so, "singleModeButton", singleModeButton);
            SetObj(so, "doubleModeButton", doubleModeButton);
            SetObj(so, "singleClock", singleClock);
            SetObj(so, "doubleClockA", clockA);
            SetObj(so, "doubleClockB", clockB);
            SetObj(so, "titleText", titleText);
            SetObj(so, "questionCounterText", questionText);
            SetObj(so, "scoreText", scoreText);
            SetObj(so, "timerText", timerText);
            SetObj(so, "timerFillImage", timerFill);
            SetObj(so, "homeButton", homeButton);
            SetObj(so, "pauseButton", pauseButton);
            SetObj(so, "helpButton", helpButton);
            SetObj(so, "singleModeRoot", singleRoot.gameObject);
            SetObj(so, "singlePromptText", singlePrompt);
            SetObj(so, "singleTargetText", singleTarget);
            SetObj(so, "singleLegendText", singleLegend);
            SetObj(so, "singleSubmitButton", singleSubmit);
            SetObj(so, "singleResetButton", singleReset);
            SetObj(so, "doubleModeRoot", doubleRoot.gameObject);
            SetObj(so, "differencePromptText", diffPrompt);
            SetObj(so, "differenceTargetText", diffTarget);
            SetObj(so, "differenceChipText", diffChip);
            SetObj(so, "clockAPmToggle", clockAToggle);
            SetObj(so, "clockBPmToggle", clockBToggle);
            SetObj(so, "clockAAmPmLabel", clockALabel);
            SetObj(so, "clockBAmPmLabel", clockBLabel);
            SetObj(so, "doubleSubmitButton", doubleSubmit);
            SetObj(so, "doubleResetButton", doubleReset);
            SetObj(so, "feedbackPanelGroup", feedbackGroup);
            SetObj(so, "feedbackText", feedbackText);
            SetObj(so, "pausePanelGroup", pauseGroup);
            SetObj(so, "resumeButton", resumeButton);
            SetObj(so, "pauseRestartButton", restartButton);
            SetObj(so, "pauseHowToPlayButton", howToButton);
            SetObj(so, "pauseHomeButton", pauseHomeButton);
            SetObj(so, "howToPlayPanelGroup", howToGroup);
            SetObj(so, "howToPlayText", howToText);
            SetObj(so, "howToPlayImage", howToImage);
            SetObj(so, "howToPageCounterText", pageCounterText);
            SetObj(so, "howToPreviousButton", previousHowToButton);
            SetObj(so, "howToNextButton", nextHowToButton);
            SetObj(so, "closeHowToPlayButton", closeHowToButton);
            SetObj(so, "resultPanelGroup", resultGroup);
            SetObj(so, "resultTitleText", resultTitle);
            SetObj(so, "resultScoreText", resultScore);
            SetObj(so, "resultRestartButton", resultRestart);
            SetObj(so, "resultHomeButton", resultHome);
            SetObj(so, "tutorialController", tutorialController);
            SetObj(so, "audioManager", audioManager);
            so.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject tutorialSo = new SerializedObject(tutorialController);
            SetObj(tutorialSo, "singleClock", singleClock);
            SetObj(tutorialSo, "doubleClockA", clockA);
            SetObj(tutorialSo, "doubleClockB", clockB);
            SetObj(tutorialSo, "homeButton", homeButton);
            SetObj(tutorialSo, "pauseButton", pauseButton);
            SetObj(tutorialSo, "helpButton", helpButton);
            SetObj(tutorialSo, "singleSubmitButton", singleSubmit);
            SetObj(tutorialSo, "singleResetButton", singleReset);
            SetObj(tutorialSo, "doubleSubmitButton", doubleSubmit);
            SetObj(tutorialSo, "doubleResetButton", doubleReset);
            SetObj(tutorialSo, "clockAPmToggle", clockAToggle);
            SetObj(tutorialSo, "clockBPmToggle", clockBToggle);
            SetObj(tutorialSo, "overlayGroup", tutorialGroup);
            SetObj(tutorialSo, "backgroundImage", tutorialBackgroundImage);
            SetObj(tutorialSo, "clickAnywhereButton", tutorialClickAnywhereButton);
            SetObj(tutorialSo, "pointer", tutorialPointer);
            SetObj(tutorialSo, "pointerImage", tutorialPointerImage);
            SetObj(tutorialSo, "ghostHand", tutorialGhostHand);
            SetObj(tutorialSo, "ghostHandImage", tutorialGhostHandImage);
            SetObj(tutorialSo, "promptCard", tutorialPromptCard);
            SetObj(tutorialSo, "promptText", tutorialPrompt);
            SetFloat(tutorialSo, "promptBackgroundOpacity", 0.86f);
            SetBool(tutorialSo, "copyActualHandSpriteForGhost", true);
            SetObj(tutorialSo, "singleQuestionTarget", singleTarget.GetComponent<RectTransform>());
            SetObj(tutorialSo, "doubleQuestionTarget", diffTarget.GetComponent<RectTransform>());
            SetObj(tutorialSo, "singleClockTarget", singleClock.GetComponent<RectTransform>());
            SetObj(tutorialSo, "doubleClockATarget", clockA.GetComponent<RectTransform>());
            SetObj(tutorialSo, "doubleClockBTarget", clockB.GetComponent<RectTransform>());
            tutorialSo.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = manager.gameObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Clock Learning Game v15 rough UI created. Assign How To images and tutorial pointer sprite in the Tutorial Controller, then press Play.");
        }

        private static Canvas CreateCanvas()
        {
            GameObject go = new GameObject("Clock Learning Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create Clock Learning Canvas");
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static CanvasGroup CreateModeMenuPanel(RectTransform parent, out TextMeshProUGUI title, out Button singleButton, out Button doubleButton)
        {
            RectTransform panel = CreatePanel("Mode Menu Panel", parent, Vector2.zero, Vector2.one, BackgroundColor);
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            RectTransform card = CreatePanel("Menu Card", panel, new Vector2(0.26f, 0.16f), new Vector2(0.74f, 0.86f), PanelColor);
            title = CreateText(card, "Game Name", "Clock Game", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.92f), 64, FontStyles.Bold, TextAlignmentOptions.Center);
            CreateText(card, "Subtitle", "Choose a mode", new Vector2(0.10f, 0.61f), new Vector2(0.90f, 0.70f), 30, FontStyles.Normal, TextAlignmentOptions.Center);

            RectTransform buttonArea = CreateRect("Mode Button Area", card, new Vector2(0.14f, 0.18f), new Vector2(0.86f, 0.56f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup layout = buttonArea.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 26f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            singleButton = CreateLayoutButton(buttonArea, "Single Mode Button", "Single Clock\nSet the Time", ButtonColor, 34);
            doubleButton = CreateLayoutButton(buttonArea, "Double Mode Button", "Double Clock\nTime Difference", SecondaryButtonColor, 34);
            return group;
        }

        private static ClockLearningClockView CreateClock(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            ClockLearningClockView view = rect.gameObject.AddComponent<ClockLearningClockView>();
            view.BuildPlaceholderClock();
            return view;
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color color, int fontSize)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, color);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            TextMeshProUGUI text = CreateText(rect, "Label", label, Vector2.zero, Vector2.one, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private static Button CreateIconButton(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, color);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            RectTransform icon = CreateRect("Icon", rect, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), Vector2.zero, Vector2.zero);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.color = new Color(0.23f, 0.17f, 0.08f, 0.75f);
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            return button;
        }

        private static TextMeshProUGUI CreateTextCard(RectTransform parent, string cardName, string textName, string value, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            RectTransform card = CreatePanel(cardName, parent, anchorMin, anchorMax, new Color(1f, 0.91f, 0.56f, 1f));
            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null) cardImage.raycastTarget = false;
            TextMeshProUGUI text = CreateText(card, textName, value, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateLayoutButton(RectTransform parent, string name, string label, Color color, int fontSize)
        {
            RectTransform rect = CreatePanel(name, parent, Vector2.zero, Vector2.one, color);
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 110f;
            layout.preferredHeight = 140f;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            TextMeshProUGUI text = CreateText(rect, "Label", label, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.92f), fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private static Toggle CreateToggle(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI labelText)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, SecondaryButtonColor);
            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = rect.GetComponent<Image>();
            labelText = CreateText(rect, "Label", label, Vector2.zero, Vector2.one, 24, FontStyles.Bold, TextAlignmentOptions.Center);
            labelText.raycastTarget = false;
            return toggle;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = TextColor;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static CanvasGroup CreatePopupPanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI messageText)
        {
            RectTransform panel = CreatePanel(name, parent, anchorMin, anchorMax, new Color(1f, 0.88f, 0.38f, 1f));
            CanvasGroup group = panel.gameObject.AddComponent<CanvasGroup>();
            messageText = CreateText(panel, "Message", "", Vector2.zero, Vector2.one, 38, FontStyles.Bold, TextAlignmentOptions.Center);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
            return group;
        }

        private static CanvasGroup CreatePausePanel(RectTransform parent, string name, string titleText, out Button resume, out Button restart, out Button howTo, out Button home)
        {
            RectTransform overlay = CreatePanel(name, parent, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.48f));
            CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
            RectTransform card = CreatePanel("Card", overlay, new Vector2(0.36f, 0.18f), new Vector2(0.64f, 0.82f), PanelColor);
            CreateText(card, "Title", titleText, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f), 42, FontStyles.Bold, TextAlignmentOptions.Center);
            resume = CreateButton(card, "Resume Button", "Resume", new Vector2(0.15f, 0.62f), new Vector2(0.85f, 0.75f), ButtonColor, 30);
            howTo = CreateButton(card, "How To Button", "How to Play", new Vector2(0.15f, 0.46f), new Vector2(0.85f, 0.59f), SecondaryButtonColor, 28);
            restart = CreateButton(card, "Restart Button", "Restart", new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.43f), SecondaryButtonColor, 28);
            home = CreateButton(card, "Home Button", "Mode Menu", new Vector2(0.15f, 0.14f), new Vector2(0.85f, 0.27f), SecondaryButtonColor, 28);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            overlay.gameObject.SetActive(false);
            return group;
        }

        private static CanvasGroup CreateHowToPanel(RectTransform parent, out TextMeshProUGUI howToText, out Image howToImage, out TextMeshProUGUI pageCounter, out Button previousButton, out Button nextButton, out Button closeButton)
        {
            RectTransform overlay = CreatePanel("How To Play Panel", parent, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.48f));
            CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
            RectTransform card = CreatePanel("Card", overlay, new Vector2(0.19f, 0.10f), new Vector2(0.81f, 0.90f), PanelColor);
            CreateText(card, "Title", "How to Play", new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f), 44, FontStyles.Bold, TextAlignmentOptions.Center);

            RectTransform imageFrame = CreatePanel("How To Image Frame", card, new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.84f), new Color(1f, 0.92f, 0.66f, 1f));
            howToImage = imageFrame.GetComponent<Image>();
            howToImage.preserveAspect = true;
            howToImage.raycastTarget = false;
            howToText = CreateText(imageFrame, "Fallback Text", "Assign mode-wise How To images in the manager Inspector.", new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.90f), 30, FontStyles.Normal, TextAlignmentOptions.Center);

            pageCounter = CreateText(card, "Page Counter", "", new Vector2(0.38f, 0.18f), new Vector2(0.62f, 0.22f), 24, FontStyles.Bold, TextAlignmentOptions.Center);
            previousButton = CreateButton(card, "Previous Button", "Prev", new Vector2(0.06f, 0.05f), new Vector2(0.22f, 0.16f), SecondaryButtonColor, 26);
            nextButton = CreateButton(card, "Next Button", "Next", new Vector2(0.78f, 0.05f), new Vector2(0.94f, 0.16f), SecondaryButtonColor, 26);
            closeButton = CreateButton(card, "Close Button", "Start Game", new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.16f), ButtonColor, 28);

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            overlay.gameObject.SetActive(false);
            return group;
        }

        private static CanvasGroup CreateInteractiveTutorialOverlay(RectTransform parent, out Image backgroundImage, out Button clickAnywhereButton, out RectTransform pointer, out Image pointerImage, out RectTransform ghostHand, out Image ghostHandImage, out RectTransform promptCard, out TextMeshProUGUI promptText)
        {
            RectTransform overlay = CreateRect("Interactive Tutorial Overlay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform background = CreateRect("Tutorial Optional Background Image", overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 0f);
            backgroundImage.raycastTarget = false;

            promptCard = CreatePanel("Tutorial Instruction Line", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(1f, 0.89f, 0.42f, 0.86f));
            promptCard.sizeDelta = new Vector2(920f, 92f);
            promptCard.GetComponent<Image>().raycastTarget = false;
            promptText = CreateText(promptCard, "Prompt Text", "Read the time. Click anywhere to continue.", new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.92f), 34, FontStyles.Bold, TextAlignmentOptions.Center);
            promptText.raycastTarget = false;

            ghostHand = CreateRect("Tutorial Fake Clock Hand", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ghostHand.pivot = new Vector2(0.5f, 0f);
            ghostHand.sizeDelta = new Vector2(16f, 165f);
            ghostHandImage = ghostHand.gameObject.AddComponent<Image>();
            ghostHandImage.color = new Color(1f, 1f, 1f, 0.35f);
            ghostHandImage.preserveAspect = true;
            ghostHandImage.raycastTarget = false;
            ghostHand.gameObject.SetActive(false);

            pointer = CreateRect("Tutorial Pointer Image", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            pointer.sizeDelta = new Vector2(110f, 110f);
            pointerImage = pointer.gameObject.AddComponent<Image>();
            pointerImage.color = new Color(1f, 1f, 1f, 0.85f);
            pointerImage.preserveAspect = true;
            pointerImage.raycastTarget = false;

            RectTransform clickRect = CreateRect("Tutorial Click Anywhere Button", overlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image clickImage = clickRect.gameObject.AddComponent<Image>();
            clickImage.color = new Color(1f, 1f, 1f, 0f);
            clickImage.raycastTarget = true;
            clickAnywhereButton = clickRect.gameObject.AddComponent<Button>();
            clickAnywhereButton.transition = Selectable.Transition.None;
            clickAnywhereButton.targetGraphic = clickImage;
            clickRect.gameObject.SetActive(false);

            overlay.gameObject.SetActive(false);
            return group;
        }

        private static CanvasGroup CreateResultPanel(RectTransform parent, out TextMeshProUGUI title, out TextMeshProUGUI score, out Button restart, out Button home)
        {
            RectTransform overlay = CreatePanel("Result Panel", parent, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.48f));
            CanvasGroup group = overlay.gameObject.AddComponent<CanvasGroup>();
            RectTransform card = CreatePanel("Card", overlay, new Vector2(0.32f, 0.22f), new Vector2(0.68f, 0.78f), PanelColor);
            title = CreateText(card, "Result Title", "Well done!", new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.92f), 46, FontStyles.Bold, TextAlignmentOptions.Center);
            score = CreateText(card, "Result Score", "Final Score: 0", new Vector2(0.05f, 0.49f), new Vector2(0.95f, 0.66f), 34, FontStyles.Bold, TextAlignmentOptions.Center);
            restart = CreateButton(card, "Restart Button", "Restart", new Vector2(0.20f, 0.27f), new Vector2(0.80f, 0.42f), ButtonColor, 30);
            home = CreateButton(card, "Continue Button", "Continue", new Vector2(0.28f, 0.12f), new Vector2(0.72f, 0.23f), SecondaryButtonColor, 26);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            overlay.gameObject.SetActive(false);
            return group;
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create UI Object");
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void SetObj(SerializedObject so, string fieldName, Object value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.objectReferenceValue = value;
            else Debug.LogWarning($"Missing serialized field: {fieldName}");
        }

        private static void SetFloat(SerializedObject so, string fieldName, float value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.floatValue = value;
            else Debug.LogWarning($"Missing serialized field: {fieldName}");
        }

        private static void SetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.boolValue = value;
            else Debug.LogWarning($"Missing serialized field: {fieldName}");
        }
    }
}
#endif
