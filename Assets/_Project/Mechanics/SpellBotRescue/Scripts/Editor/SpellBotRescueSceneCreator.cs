#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NarayanaGames.SpellBotRescue
{
    public static class SpellBotRescueSceneCreator
    {
        private const string RootFolder = "Assets/Mechanics/SpellBotRescue";
        private const string DatabaseFolder = RootFolder + "/ScriptableObjects";
        private const string DatabasePath = DatabaseFolder + "/SpellBotSampleWordDatabase.asset";

        [MenuItem("Tools/Spell Bot Rescue/Create Rough Scene UI")]
        public static void CreateRoughSceneUI()
        {
            EnsureFolders();
            SpellBotWordDatabase database = EnsureSampleDatabase();

            GameObject canvasObject = CreateCanvas();
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

            GameObject safeRoot = CreateUIObject("SafeAreaRoot", canvasRect);
            RectTransform safeRect = safeRoot.GetComponent<RectTransform>();
            Stretch(safeRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safeRoot.AddComponent<SpellBotSafeAreaFitter>();

            GameObject managerObject = new GameObject("SpellBotRescueManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Create SpellBot Rescue Manager");
            SpellBotRescueManager manager = managerObject.AddComponent<SpellBotRescueManager>();
            manager.wordDatabase = database;
            manager.roundsPerSession = 10;
            manager.fontApplyRoot = safeRoot.transform;
            manager.showHomePageOnStart = true;
            manager.showHowToPlayOnStart = false;
            manager.showHowToPlayBeforeFirstRound = true;
            manager.resetHowToPlayToFirstPageOnOpen = true;

            AudioSource audioSource = managerObject.AddComponent<AudioSource>();
            manager.audioSource = audioSource;

            GameObject bgmObject = new GameObject("SpellBotBGM");
            Undo.RegisterCreatedObjectUndo(bgmObject, "Create SpellBot BGM");
            SpellBotBgmPlayer bgmPlayer = bgmObject.AddComponent<SpellBotBgmPlayer>();
            SerializedObject bgmSerialized = new SerializedObject(bgmPlayer);
            bgmSerialized.FindProperty("playOnStart").boolValue = false;
            bgmSerialized.ApplyModifiedPropertiesWithoutUndo();

            Image overdriveGlow = CreatePanel("OverdriveGlow", safeRect, new Color(1f, 0.72f, 0.12f, 0.16f));
            Stretch(overdriveGlow.rectTransform, new Vector2(0.03f, 0.72f), new Vector2(0.97f, 0.97f), Vector2.zero, Vector2.zero);
            overdriveGlow.gameObject.SetActive(false);

            RectTransform topBar = CreatePanel("TopBar", safeRect, new Color(0.96f, 0.96f, 0.91f, 1f)).rectTransform;
            Stretch(topBar, new Vector2(0.03f, 0.875f), new Vector2(0.97f, 0.975f), Vector2.zero, Vector2.zero);
            HorizontalLayoutGroup topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(30, 30, 12, 12);
            topLayout.spacing = 24;
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = true;

            RectTransform roundColumn = CreateUIObject("RoundProgressGroup", topBar).GetComponent<RectTransform>();
            AddLayout(roundColumn.gameObject, 360, -1, 1);
            VerticalLayoutGroup roundColumnLayout = roundColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            roundColumnLayout.spacing = 7;
            roundColumnLayout.childAlignment = TextAnchor.MiddleCenter;
            roundColumnLayout.childControlWidth = true;
            roundColumnLayout.childControlHeight = true;
            roundColumnLayout.childForceExpandWidth = true;
            roundColumnLayout.childForceExpandHeight = false;

            TextMeshProUGUI roundText = CreateText("RoundText", roundColumn, "Round 1 of 10", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            AddLayout(roundText.gameObject, -1, 36, 1);

            Slider progressSlider = CreateProgressSlider("RoundProgressSlider", roundColumn);
            AddLayout(progressSlider.gameObject, -1, 18, 1);

            RectTransform streakColumn = CreateUIObject("StreakGroup", topBar).GetComponent<RectTransform>();
            AddLayout(streakColumn.gameObject, 260, -1, 0);
            VerticalLayoutGroup streakColumnLayout = streakColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            streakColumnLayout.spacing = 5;
            streakColumnLayout.childAlignment = TextAnchor.MiddleCenter;
            streakColumnLayout.childControlWidth = true;
            streakColumnLayout.childControlHeight = true;
            streakColumnLayout.childForceExpandWidth = true;
            streakColumnLayout.childForceExpandHeight = false;

            TextMeshProUGUI streakLabelText = CreateText("StreakLabelText", streakColumn, "Streak: 0/3", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            AddLayout(streakLabelText.gameObject, -1, 28, 1);

            RectTransform streakRoot = CreateUIObject("StreakStars", streakColumn).GetComponent<RectTransform>();
            AddLayout(streakRoot.gameObject, -1, 44, 0);
            HorizontalLayoutGroup streakLayout = streakRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            streakLayout.spacing = 10;
            streakLayout.childAlignment = TextAnchor.MiddleCenter;
            streakLayout.childControlWidth = true;
            streakLayout.childControlHeight = true;
            streakLayout.childForceExpandWidth = false;
            streakLayout.childForceExpandHeight = false;

            Image[] stars = new Image[3];
            for (int i = 0; i < stars.Length; i++)
            {
                Image starSlot = CreatePanel("Star" + (i + 1), streakRoot, new Color(0.78f, 0.78f, 0.78f, 1f));
                starSlot.preserveAspect = true;
                AddLayout(starSlot.gameObject, 40, 40, 0);
                stars[i] = starSlot;
            }

            TextMeshProUGUI scoreText = CreateText("ScoreText", topBar, "Score: 0", 34, FontStyles.Bold, TextAlignmentOptions.Center);
            AddLayout(scoreText.gameObject, 240, -1, 1);

            GameObject overdriveLabel = CreateUIObject("OverdriveLabel", topBar);
            TextMeshProUGUI overdriveText = overdriveLabel.AddComponent<TextMeshProUGUI>();
            overdriveText.text = "OVERDRIVE";
            overdriveText.fontSize = 28;
            overdriveText.fontStyle = FontStyles.Bold;
            overdriveText.alignment = TextAlignmentOptions.Center;
            overdriveText.color = new Color(0.92f, 0.52f, 0.04f, 1f);
            AddLayout(overdriveLabel, 230, -1, 0);
            overdriveLabel.SetActive(false);

            Button hintTopButton = CreateButton("HintButton", topBar, "HINT", new Color(0.18f, 0.42f, 0.62f, 1f), Color.white, 28);
            AddLayout(hintTopButton.gameObject, 120, 70, 0);
            UnityEventTools.AddPersistentListener(hintTopButton.onClick, manager.OnHintButtonClicked);

            Button pauseButton = CreateButton("PauseButton", topBar, "II", new Color(0.22f, 0.18f, 0.35f, 1f), Color.white, 32);
            AddLayout(pauseButton.gameObject, 86, 70, 0);
            UnityEventTools.AddPersistentListener(pauseButton.onClick, manager.PauseGame);

            RectTransform middleZone = CreateUIObject("MiddleZone", safeRect).GetComponent<RectTransform>();
            Stretch(middleZone, new Vector2(0.03f, 0.36f), new Vector2(0.97f, 0.86f), Vector2.zero, Vector2.zero);

            Image robotImage = CreatePanel("RobotPlaceholder", middleZone, new Color(0.65f, 0.82f, 0.95f, 1f));
            Anchor(robotImage.rectTransform, new Vector2(0.05f, 0.15f), new Vector2(0.32f, 0.9f), Vector2.zero, Vector2.zero);
            TextMeshProUGUI robotLabel = CreateText("RobotLabel", robotImage.rectTransform, "ROBOT", 44, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(robotLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SpellBotRobotView robotView = robotImage.gameObject.AddComponent<SpellBotRobotView>();
            robotView.robotImage = robotImage;

            Image monitorPanel = CreatePanel("MonitorPanel", middleZone, new Color(0.12f, 0.16f, 0.23f, 1f));
            Anchor(monitorPanel.rectTransform, new Vector2(0.34f, 0.08f), new Vector2(0.96f, 0.90f), Vector2.zero, Vector2.zero);

            Image monitorScreen = CreatePanel("MonitorScreen", monitorPanel.rectTransform, new Color(0.92f, 0.96f, 0.93f, 1f));
            Stretch(monitorScreen.rectTransform, new Vector2(0.06f, 0.36f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);

            TMP_InputField wordInputField = CreateWordInputField(monitorScreen.rectTransform, manager, out TextMeshProUGUI wordText);

            Image monitorGlow = CreatePanel("CorrectGlow", monitorScreen.rectTransform, new Color(0.16f, 0.95f, 0.48f, 0f));
            monitorGlow.raycastTarget = false;
            Stretch(monitorGlow.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            monitorGlow.gameObject.SetActive(false);

            Image hintPanel = CreatePanel("HintPopup", monitorPanel.rectTransform, new Color(1f, 0.95f, 0.75f, 1f));
            Stretch(hintPanel.rectTransform, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.31f), Vector2.zero, Vector2.zero);
            CanvasGroup hintCanvasGroup = hintPanel.gameObject.AddComponent<CanvasGroup>();
            TextMeshProUGUI hintText = CreateText("HintText", hintPanel.rectTransform, "Hint or correct spelling appears here.", 26, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            hintText.color = new Color(0.16f, 0.12f, 0.20f, 1f);
            Stretch(hintText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.67f, 0.92f), Vector2.zero, Vector2.zero);

            Button showAnswerButton = CreateButton("ShowAnswerButton", hintPanel.rectTransform, "SHOW ANSWER", new Color(0.82f, 0.38f, 0.18f, 1f), Color.white, 20);
            Anchor(showAnswerButton.GetComponent<RectTransform>(), new Vector2(0.70f, 0.18f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(showAnswerButton.onClick, manager.OnShowAnswerButtonClicked);
            showAnswerButton.gameObject.SetActive(false);

            RectTransform keyboardZone = CreatePanel("KeyboardZone", safeRect, new Color(0.94f, 0.95f, 0.98f, 1f)).rectTransform;
            Stretch(keyboardZone, new Vector2(0.03f, 0.035f), new Vector2(0.97f, 0.34f), Vector2.zero, Vector2.zero);
            VerticalLayoutGroup keyboardLayout = keyboardZone.gameObject.AddComponent<VerticalLayoutGroup>();
            keyboardLayout.padding = new RectOffset(24, 24, 18, 18);
            keyboardLayout.spacing = 12;
            keyboardLayout.childAlignment = TextAnchor.MiddleCenter;
            keyboardLayout.childControlWidth = true;
            keyboardLayout.childControlHeight = true;
            keyboardLayout.childForceExpandWidth = false;
            keyboardLayout.childForceExpandHeight = true;

            SpellBotKeyboardView keyboardView = keyboardZone.gameObject.AddComponent<SpellBotKeyboardView>();
            List<SpellBotKeyboardKey> keyList = new List<SpellBotKeyboardKey>();

            CreateKeyboardLetterRow(keyboardZone, "Row_QWERTY", "QWERTYUIOP", keyList);
            CreateKeyboardLetterRow(keyboardZone, "Row_ASDF", "ASDFGHJKL", keyList);
            CreateKeyboardThirdRow(keyboardZone, keyList);
            SpellBotKeyboardKey fixedKey = CreateUtilityRow(keyboardZone, keyList);

            keyboardView.keys = keyList;
            keyboardView.fixedKey = fixedKey;

            SpellBotUIFeedback feedback = managerObject.AddComponent<SpellBotUIFeedback>();
            feedback.monitorRoot = monitorPanel.rectTransform;
            feedback.fixedButtonRoot = fixedKey.transform as RectTransform;
            feedback.hintPopupRoot = hintPanel.rectTransform;
            feedback.hintCanvasGroup = hintCanvasGroup;
            feedback.scoreTextTransform = scoreText.transform;
            feedback.overdriveGlow = overdriveGlow;

            GameObject homePagePanel = CreateHomePagePanel(safeRect, manager, robotImage.sprite);
            homePagePanel.SetActive(false);

            GameObject howToPlayPanel = CreateHowToPlayPanel(safeRect, manager);
            howToPlayPanel.SetActive(false);

            GameObject pausePanel = CreateOverlayPanel(safeRect, "PausePanel", "Paused", "Take a short break.");
            Button resumeButton = CreateButton("ResumeButton", pausePanel.transform, "RESUME", new Color(0.10f, 0.70f, 0.38f, 1f), Color.white, 34);
            Anchor(resumeButton.GetComponent<RectTransform>(), new Vector2(0.25f, 0.18f), new Vector2(0.49f, 0.28f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(resumeButton.onClick, manager.ResumeGame);
            Button pauseHowToPlayButton = CreateButton("HowToPlayButton", pausePanel.transform, "HOW TO PLAY", new Color(0.18f, 0.42f, 0.62f, 1f), Color.white, 28);
            Anchor(pauseHowToPlayButton.GetComponent<RectTransform>(), new Vector2(0.51f, 0.18f), new Vector2(0.75f, 0.28f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(pauseHowToPlayButton.onClick, manager.OpenHowToPlayPanel);
            pausePanel.SetActive(false);

            GameObject resultPanel = CreateOverlayPanel(safeRect, "ResultPanel", "Rescue Complete!", "Result summary appears here.");
            TextMeshProUGUI resultTitleText = resultPanel.transform.Find("Card/TitleText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI resultBodyText = resultPanel.transform.Find("Card/BodyText").GetComponent<TextMeshProUGUI>();
            Button playAgainButton = CreateButton("PlayAgainButton", resultPanel.transform, "PLAY AGAIN", new Color(0.10f, 0.70f, 0.38f, 1f), Color.white, 30);
            Anchor(playAgainButton.GetComponent<RectTransform>(), new Vector2(0.22f, 0.14f), new Vector2(0.46f, 0.24f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(playAgainButton.onClick, manager.RestartGame);
            Button continueButton = CreateButton("ContinueButton", resultPanel.transform, "CONTINUE", new Color(0.18f, 0.42f, 0.62f, 1f), Color.white, 30);
            Anchor(continueButton.GetComponent<RectTransform>(), new Vector2(0.54f, 0.14f), new Vector2(0.78f, 0.24f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(continueButton.onClick, manager.ContinueFromResult);
            resultPanel.SetActive(false);

            manager.roundText = roundText;
            manager.progressSlider = progressSlider;
            manager.streakLabelText = streakLabelText;
            manager.scoreText = scoreText;
            manager.wordInputField = wordInputField;
            manager.wordText = wordText;
            manager.hintText = hintText;
            manager.resultTitleText = resultTitleText;
            manager.resultBodyText = resultBodyText;
            manager.homePagePanel = homePagePanel;
            manager.howToPlayPanel = howToPlayPanel;
            manager.pausePanel = pausePanel;
            manager.resultPanel = resultPanel;
            manager.overdriveLabelRoot = overdriveLabel;
            manager.hintButton = hintTopButton;
            manager.showAnswerButton = showAnswerButton;
            manager.streakStars = stars;
            manager.keyboardView = keyboardView;
            manager.feedback = feedback;
            manager.robotView = robotView;
            feedback.monitorGlow = monitorGlow;

            Selection.activeObject = managerObject;
            EditorUtility.SetDirty(managerObject);
            EditorUtility.SetDirty(canvasObject);
            AssetDatabase.SaveAssets();

            Debug.Log("Spell-Bot Rescue rough scene UI created. Press Play, then click START.");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Mechanics"))
            {
                AssetDatabase.CreateFolder("Assets", "Mechanics");
            }

            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                AssetDatabase.CreateFolder("Assets/Mechanics", "SpellBotRescue");
            }

            if (!AssetDatabase.IsValidFolder(DatabaseFolder))
            {
                AssetDatabase.CreateFolder(RootFolder, "ScriptableObjects");
            }
        }

        private static SpellBotWordDatabase EnsureSampleDatabase()
        {
            SpellBotWordDatabase database = AssetDatabase.LoadAssetAtPath<SpellBotWordDatabase>(DatabasePath);
            if (database != null)
            {
                return database;
            }

            database = ScriptableObject.CreateInstance<SpellBotWordDatabase>();
            database.entries = BuildSampleEntries();
            AssetDatabase.CreateAsset(database, DatabasePath);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return database;
        }

        private static List<SpellBotWordEntry> BuildSampleEntries()
        {
            return new List<SpellBotWordEntry>
            {
                Entry("frend", "friend", "A person you like and spend time with.", 3),
                Entry("becaus", "because", "A word used to give a reason.", 3),
                Entry("animel", "animal", "A living creature like a dog, cat, or elephant.", 3),
                Entry("famly", "family", "People who live with or care for you.", 3),
                Entry("skool", "school", "A place where children learn.", 3),
                Entry("happpy", "happy", "Feeling joy or gladness.", 3),
                Entry("brithday", "birthday", "The day someone was born.", 3),
                Entry("buterfly", "butterfly", "A colorful flying insect.", 3),
                Entry("watter", "water", "A clear liquid we drink.", 3),
                Entry("pepole", "people", "More than one person.", 3),
                Entry("rainbo", "rainbow", "Colorful arc seen after rain.", 3),
                Entry("libary", "library", "A place with many books.", 3),
                Entry("faverite", "favorite", "Something you like the most.", 3),
                Entry("monky", "monkey", "An animal that can climb and swing.", 3),
                Entry("kittenn", "kitten", "A baby cat.", 3),
                Entry("diffrent", "different", "Not the same.", 4),
                Entry("anser", "answer", "A reply to a question.", 4),
                Entry("quikly", "quickly", "Doing something fast.", 4),
                Entry("tomorow", "tomorrow", "The day after today.", 4),
                Entry("yestarday", "yesterday", "The day before today.", 4),
                Entry("importent", "important", "Something that matters a lot.", 4),
                Entry("minit", "minute", "Sixty seconds.", 4),
                Entry("suddnly", "suddenly", "Happening quickly without warning.", 4),
                Entry("saftey", "safety", "Being protected from danger.", 4),
                Entry("jungel", "jungle", "A thick forest with many plants and animals.", 4),
                Entry("mountin", "mountain", "A very high hill.", 4),
                Entry("seperate", "separate", "To keep apart.", 4),
                Entry("nervus", "nervous", "Feeling worried or afraid.", 4),
                Entry("curius", "curious", "Wanting to know or learn.", 4),
                Entry("stomack", "stomach", "The body part where food goes.", 4),
                Entry("magestic", "majestic", "Grand, royal, or magnificent.", 5),
                Entry("majestick", "majestic", "Grand, royal, or magnificent.", 5),
                Entry("exellent", "excellent", "Extremely good.", 5),
                Entry("beutiful", "beautiful", "Very pretty or lovely.", 5),
                Entry("choclate", "chocolate", "A sweet brown treat.", 5),
                Entry("enviroment", "environment", "The world around us.", 5),
                Entry("neccesary", "necessary", "Something that is needed.", 5),
                Entry("knowlege", "knowledge", "Information and understanding.", 5),
                Entry("adventur", "adventure", "An exciting journey or experience.", 5),
                Entry("misterious", "mysterious", "Hard to explain or understand.", 5),
                Entry("imaginashun", "imagination", "The ability to create ideas in your mind.", 5),
                Entry("confidense", "confidence", "Believing you can do something.", 5),
                Entry("responsable", "responsible", "Able to be trusted to do the right thing.", 5),
                Entry("generous", "generous", "Willing to share or give.", 5),
                Entry("genrous", "generous", "Willing to share or give.", 5),
                Entry("obediant", "obedient", "Following rules or instructions.", 5),
                Entry("patiense", "patience", "Waiting calmly without getting upset.", 5),
                Entry("bravery", "bravery", "Being brave.", 5),
                Entry("bravry", "bravery", "Being brave.", 5),
                Entry("discoverd", "discovered", "Found something new.", 5),
                Entry("scientest", "scientist", "A person who studies science.", 5),
                Entry("creativ", "creative", "Able to make new ideas or things.", 5),
                Entry("achievment", "achievement", "Something good gained by effort.", 5)
            };
        }

        private static SpellBotWordEntry Entry(string incorrect, string correct, string hint, int tier)
        {
            return new SpellBotWordEntry
            {
                incorrectWord = incorrect,
                correctWord = correct,
                hintText = hint,
                difficultyTier = tier
            };
        }

        private static GameObject CreateCanvas()
        {
            GameObject canvasObject = new GameObject("SpellBotRescue_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create SpellBot Rescue Canvas");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            return canvasObject;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject gameObject = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = new Color(0.08f, 0.10f, 0.14f, 1f);
            tmp.enableAutoSizing = false;
            return tmp;
        }

        private static Slider CreateProgressSlider(string name, Transform parent)
        {
            GameObject root = CreateUIObject(name, parent);
            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 10f;
            slider.value = 0f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            Image background = CreatePanel("Background", root.transform, new Color(0.78f, 0.80f, 0.84f, 1f));
            Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform fillArea = CreateUIObject("Fill Area", root.transform).GetComponent<RectTransform>();
            Stretch(fillArea, Vector2.zero, Vector2.one, new Vector2(5f, 0f), new Vector2(-5f, 0f));

            Image fill = CreatePanel("Fill", fillArea, new Color(0.10f, 0.70f, 0.38f, 1f));
            Stretch(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            slider.targetGraphic = fill;
            slider.fillRect = fill.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static TMP_InputField CreateWordInputField(Transform parent, SpellBotRescueManager manager, out TextMeshProUGUI wordText)
        {
            Image inputBackground = CreatePanel("WordInputField", parent, new Color(1f, 1f, 1f, 0f));
            inputBackground.raycastTarget = true;
            Stretch(inputBackground.rectTransform, new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f), Vector2.zero, Vector2.zero);

            TMP_InputField inputField = inputBackground.gameObject.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.SingleLine;
            inputField.contentType = TMP_InputField.ContentType.Custom;
            inputField.inputType = TMP_InputField.InputType.Standard;
            inputField.characterValidation = TMP_InputField.CharacterValidation.None;
            inputField.keyboardType = TouchScreenKeyboardType.Default;
            inputField.shouldHideMobileInput = true;
            inputField.readOnly = false;
            inputField.richText = false;
            inputField.caretWidth = 5;
            inputField.customCaretColor = true;
            inputField.caretColor = new Color(0.02f, 0.10f, 0.22f, 1f);
            inputField.selectionColor = new Color(0.28f, 0.62f, 0.95f, 0.32f);
            inputField.text = "majestick";

            RectTransform textArea = CreateUIObject("Text Area", inputBackground.transform).GetComponent<RectTransform>();
            Stretch(textArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectMask2D mask = textArea.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;

            wordText = CreateText("WordText", textArea, "majestick", 72, FontStyles.Bold, TextAlignmentOptions.Center);
            wordText.color = new Color(0.10f, 0.14f, 0.20f, 1f);
            wordText.raycastTarget = false;
            Stretch(wordText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            inputField.textViewport = textArea;
            inputField.textComponent = wordText;
            inputField.placeholder = null;

            SpellBotWordCaretInput caretInput = inputBackground.gameObject.AddComponent<SpellBotWordCaretInput>();
            caretInput.manager = manager;
            caretInput.targetInputField = inputField;
            caretInput.targetText = wordText;

            return inputField;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color backgroundColor, Color textColor, float fontSize)
        {
            Image image = CreatePanel(name, parent, backgroundColor);
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = backgroundColor * 1.08f;
            colors.pressedColor = backgroundColor * 0.88f;
            colors.selectedColor = backgroundColor;
            button.colors = colors;

            TextMeshProUGUI text = CreateText("Label", image.transform, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            text.color = textColor;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void CreateKeyboardLetterRow(Transform parent, string rowName, string letters, List<SpellBotKeyboardKey> keys)
        {
            RectTransform row = CreateUIObject(rowName, parent).GetComponent<RectTransform>();
            AddLayout(row.gameObject, -1, 60, 1);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            foreach (char letter in letters)
            {
                SpellBotKeyboardKey key = CreateKey(row, letter.ToString(), SpellBotKeyType.Letter, letter.ToString(), 90);
                keys.Add(key);
            }
        }

        private static void CreateKeyboardThirdRow(Transform parent, List<SpellBotKeyboardKey> keys)
        {
            RectTransform row = CreateUIObject("Row_ZXCV", parent).GetComponent<RectTransform>();
            AddLayout(row.gameObject, -1, 60, 1);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            foreach (char letter in "ZXCVBNM")
            {
                keys.Add(CreateKey(row, letter.ToString(), SpellBotKeyType.Letter, letter.ToString(), 90));
            }

            keys.Add(CreateKey(row, "BACK", SpellBotKeyType.Backspace, string.Empty, 150));
        }

        private static SpellBotKeyboardKey CreateUtilityRow(Transform parent, List<SpellBotKeyboardKey> keys)
        {
            RectTransform row = CreateUIObject("Row_Utility", parent).GetComponent<RectTransform>();
            AddLayout(row.gameObject, -1, 66, 1);
            HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            SpellBotKeyboardKey clearKey = CreateKey(row, "CLEAR", SpellBotKeyType.Clear, string.Empty, 230);
            keys.Add(clearKey);

            GameObject spacer = CreateUIObject("FlexibleSpacer", row);
            AddLayout(spacer, -1, 1, 1);

            SpellBotKeyboardKey fixedKey = CreateKey(row, "FIXED", SpellBotKeyType.Fixed, string.Empty, 300);
            keys.Add(fixedKey);
            return fixedKey;
        }

        private static SpellBotKeyboardKey CreateKey(Transform parent, string label, SpellBotKeyType type, string letter, float width)
        {
            Button button = CreateButton("Key_" + label, parent, label, KeyColor(type), type == SpellBotKeyType.Letter ? new Color(0.08f, 0.10f, 0.14f, 1f) : Color.white, 28);
            AddLayout(button.gameObject, width, 58, 0);

            SpellBotKeyboardKey key = button.gameObject.AddComponent<SpellBotKeyboardKey>();
            key.button = button;
            key.keyBackground = button.GetComponent<Image>();
            key.label = button.GetComponentInChildren<TextMeshProUGUI>();
            key.keyType = type;
            key.letterValue = letter;
            key.RefreshLabel();
            return key;
        }

        private static Color KeyColor(SpellBotKeyType type)
        {
            switch (type)
            {
                case SpellBotKeyType.Letter:
                    return new Color(0.62f, 0.78f, 0.92f, 1f);
                case SpellBotKeyType.Backspace:
                case SpellBotKeyType.Clear:
                    return new Color(0.22f, 0.18f, 0.35f, 1f);
                default:
                    return new Color(0.55f, 0.57f, 0.60f, 1f);
            }
        }

        private static GameObject CreateHomePagePanel(Transform parent, SpellBotRescueManager manager, Sprite robotSprite)
        {
            Image overlay = CreatePanel("HomePagePanel", parent, new Color(0.91f, 0.96f, 0.98f, 1f));
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TextMeshProUGUI title = CreateText("GameTitleText", overlay.transform, "Spell-Bot Rescue!", 64, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = new Color(0.09f, 0.14f, 0.22f, 1f);
            Anchor(title.rectTransform, new Vector2(0.08f, 0.76f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero);

            Image robot = CreatePanel("HomeRobotImage", overlay.transform, new Color(0.65f, 0.82f, 0.95f, 1f));
            robot.sprite = robotSprite;
            robot.preserveAspect = true;
            Anchor(robot.rectTransform, new Vector2(0.16f, 0.24f), new Vector2(0.48f, 0.72f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI robotLabel = CreateText("RobotReplaceText", robot.transform, "ROBOT IMAGE", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            robotLabel.color = new Color(0.12f, 0.18f, 0.26f, 0.82f);
            Stretch(robotLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image menuCard = CreatePanel("HomeMenuCard", overlay.transform, new Color(0.98f, 0.96f, 0.90f, 1f));
            Anchor(menuCard.rectTransform, new Vector2(0.54f, 0.25f), new Vector2(0.84f, 0.70f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI body = CreateText("HomeBodyText", menuCard.transform, "Fix the spelling and rescue the robot.", 30, FontStyles.Normal, TextAlignmentOptions.Center);
            body.color = new Color(0.16f, 0.16f, 0.20f, 1f);
            Anchor(body.rectTransform, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);

            Button startButton = CreateButton("StartButton", menuCard.transform, "START", new Color(0.10f, 0.70f, 0.38f, 1f), Color.white, 34);
            Anchor(startButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.34f), new Vector2(0.82f, 0.50f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(startButton.onClick, manager.StartGameFromHome);

            Button howToPlayButton = CreateButton("HowToPlayButton", menuCard.transform, "HOW TO PLAY", new Color(0.18f, 0.42f, 0.62f, 1f), Color.white, 28);
            Anchor(howToPlayButton.GetComponent<RectTransform>(), new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.30f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(howToPlayButton.onClick, manager.OpenHowToPlayPanel);

            return overlay.gameObject;
        }

        private static GameObject CreateHowToPlayPanel(Transform parent, SpellBotRescueManager manager)
        {
            Image overlay = CreatePanel("HowToPlayPanel", parent, new Color(0.05f, 0.06f, 0.09f, 0.72f));
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image card = CreatePanel("Card", overlay.transform, new Color(0.98f, 0.96f, 0.90f, 1f));
            Anchor(card.rectTransform, new Vector2(0.13f, 0.10f), new Vector2(0.87f, 0.90f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI titleText = CreateText("TitleText", card.transform, "How To Play", 46, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.color = new Color(0.10f, 0.14f, 0.20f, 1f);
            Anchor(titleText.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            Image pageImage = CreatePanel("MainInstructionImage", card.transform, new Color(0.86f, 0.90f, 0.96f, 1f));
            pageImage.preserveAspect = true;
            Anchor(pageImage.rectTransform, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.83f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI placeholder = CreateText("ImagePlaceholderText", pageImage.transform, "ADD HOW-TO-PLAY SPRITES\nFROM INSPECTOR", 34, FontStyles.Bold, TextAlignmentOptions.Center);
            placeholder.color = new Color(0.14f, 0.18f, 0.26f, 0.72f);
            Stretch(placeholder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Button previousButton = CreateButton("PreviousButton", card.transform, "PREV", new Color(0.22f, 0.18f, 0.35f, 1f), Color.white, 26);
            Anchor(previousButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.10f), new Vector2(0.24f, 0.19f), Vector2.zero, Vector2.zero);

            Button nextButton = CreateButton("NextButton", card.transform, "NEXT", new Color(0.22f, 0.18f, 0.35f, 1f), Color.white, 26);
            Anchor(nextButton.GetComponent<RectTransform>(), new Vector2(0.28f, 0.10f), new Vector2(0.44f, 0.19f), Vector2.zero, Vector2.zero);

            Button backButton = CreateButton("BackButton", card.transform, "BACK", new Color(0.55f, 0.57f, 0.60f, 1f), Color.white, 26);
            Anchor(backButton.GetComponent<RectTransform>(), new Vector2(0.56f, 0.10f), new Vector2(0.72f, 0.19f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(backButton.onClick, manager.CloseHowToPlayPanel);

            Button startButton = CreateButton("StartGameButton", card.transform, "START GAME", new Color(0.10f, 0.70f, 0.38f, 1f), Color.white, 24);
            Anchor(startButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.10f), new Vector2(0.92f, 0.19f), Vector2.zero, Vector2.zero);
            UnityEventTools.AddPersistentListener(startButton.onClick, manager.StartGameFromHowToPlay);

            SpellBotHowToPlayImagePager pager = overlay.gameObject.AddComponent<SpellBotHowToPlayImagePager>();
            pager.mainImage = pageImage;
            pager.emptyPlaceholder = placeholder.gameObject;
            pager.previousButton = previousButton;
            pager.nextButton = nextButton;

            return overlay.gameObject;
        }

        private static GameObject CreateOverlayPanel(Transform parent, string name, string title, string body)
        {
            Image overlay = CreatePanel(name, parent, new Color(0.05f, 0.06f, 0.09f, 0.72f));
            Stretch(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image card = CreatePanel("Card", overlay.transform, new Color(0.98f, 0.96f, 0.90f, 1f));
            Anchor(card.rectTransform, new Vector2(0.26f, 0.27f), new Vector2(0.74f, 0.73f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI titleText = CreateText("TitleText", card.transform, title, 46, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.color = new Color(0.10f, 0.14f, 0.20f, 1f);
            Anchor(titleText.rectTransform, new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);

            TextMeshProUGUI bodyText = CreateText("BodyText", card.transform, body, 30, FontStyles.Normal, TextAlignmentOptions.Center);
            bodyText.color = new Color(0.16f, 0.16f, 0.20f, 1f);
            Anchor(bodyText.rectTransform, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.66f), Vector2.zero, Vector2.zero);

            return overlay.gameObject;
        }

        private static void AddLayout(GameObject gameObject, float preferredWidth, float preferredHeight, float flexibleWidth)
        {
            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            if (preferredWidth > 0)
            {
                layoutElement.preferredWidth = preferredWidth;
            }

            if (preferredHeight > 0)
            {
                layoutElement.preferredHeight = preferredHeight;
            }

            layoutElement.flexibleWidth = flexibleWidth;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            Stretch(rect, anchorMin, anchorMax, offsetMin, offsetMax);
        }
    }
}
#endif
