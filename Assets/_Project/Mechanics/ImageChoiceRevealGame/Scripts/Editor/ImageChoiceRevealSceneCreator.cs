#if UNITY_EDITOR
using ImageChoiceRevealGame;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ImageChoiceRevealGame.Editor
{
    public static class ImageChoiceRevealSceneCreator
    {
        private const string RootFolder = "Assets/ImageChoiceRevealGame";

        [MenuItem("Tools/Image Choice Reveal/Create Scene Template UI")]
        public static void CreateSceneTemplateUI()
        {
            EnsureFolder();

            Canvas canvas = CreateCanvas();
            EnsureEventSystem();

            GameObject managerObject = new GameObject("ImageChoiceRevealGameManager");
            ImageChoiceRevealGameManager manager = managerObject.AddComponent<ImageChoiceRevealGameManager>();

            RectTransform root = CreateUIObject("Root", canvas.transform);
            Stretch(root);
            Image rootBg = root.gameObject.AddComponent<Image>();
            rootBg.color = new Color(0.94f, 0.95f, 1f, 1f);

            GameObject loadingPanel;
            CanvasGroup loadingCanvasGroup;
            TMP_Text loadingHeadingText;
            Slider loadingSlider;
            TMP_Text loadingText;
            CreateLoadingPanel(root, out loadingPanel, out loadingCanvasGroup, out loadingHeadingText, out loadingSlider, out loadingText);

            RectTransform gameplayPanel = CreateUIObject("GameplayPanel", root);
            Stretch(gameplayPanel);
            CanvasGroup gameplayCanvasGroup = gameplayPanel.gameObject.AddComponent<CanvasGroup>();

            RectTransform topRow = CreatePanel("TopInfoRow", gameplayPanel, new Color(0.14f, 0.18f, 0.35f, 1f));
            Anchor(topRow, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            topRow.sizeDelta = new Vector2(0f, 88f);
            topRow.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup rowLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.padding = new RectOffset(20, 20, 12, 12);

            TMP_Text scoreText = CreateRowText("ScoreText", topRow, "Score: 0", 24, TextAlignmentOptions.Center, Color.white, 145f);
            TMP_Text instructionText = CreateRowText("GameInstructionText", topRow, "Choose the correct object.", 22, TextAlignmentOptions.Center, new Color(0.86f, 0.9f, 1f, 1f), 430f);
            TMP_Text timerText = CreateRowText("TimerText", topRow, "01:00", 25, TextAlignmentOptions.Center, Color.white, 115f);
            TMP_Text questionCounterText = CreateRowText("QuestionCounterText", topRow, "1 / 5", 24, TextAlignmentOptions.Center, Color.white, 115f);
            Button hintButton = CreateInlineButton("HintButton", topRow, "Hint", 130f, 54f);

            RectTransform questionFrame = CreatePanel("QuestionFrameImage", gameplayPanel, new Color(1f, 1f, 1f, 0.95f));
            questionFrame.sizeDelta = new Vector2(770f, 660f);
            questionFrame.anchorMin = new Vector2(0.5f, 1f);
            questionFrame.anchorMax = new Vector2(0.5f, 1f);
            questionFrame.pivot = new Vector2(0.5f, 1f);
            questionFrame.anchoredPosition = new Vector2(0f, -110f);

            RectTransform questionViewport = CreatePanel("QuestionViewport_Masked", questionFrame, new Color(0.94f, 0.97f, 1f, 1f));
            Stretch(questionViewport);
            questionViewport.offsetMin = new Vector2(28f, 28f);
            questionViewport.offsetMax = new Vector2(-28f, -28f);
            Mask mask = questionViewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            Image questionImage = CreateImage("QuestionImage", questionViewport);
            questionImage.gameObject.AddComponent<CanvasGroup>();
            questionImage.rectTransform.sizeDelta = new Vector2(580f, 560f);
            questionImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            questionImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            questionImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            questionImage.rectTransform.anchoredPosition = Vector2.zero;
            questionImage.preserveAspect = true;
            questionImage.color = Color.white;

            TMP_Text feedbackText = CreateText("FeedbackText", gameplayPanel, "", 40, TextAlignmentOptions.Center, new Color(0.1f, 0.12f, 0.22f, 1f));
            feedbackText.rectTransform.sizeDelta = new Vector2(700f, 65f);
            feedbackText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            feedbackText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            feedbackText.rectTransform.pivot = new Vector2(0.5f, 1f);
            feedbackText.rectTransform.anchoredPosition = new Vector2(0f, -745f);

            TMP_Text scorePopupText = CreateText("ScorePopupText", gameplayPanel, "+10", 48, TextAlignmentOptions.Center, new Color(0.1f, 0.45f, 0.15f, 1f));
            scorePopupText.gameObject.AddComponent<CanvasGroup>();
            scorePopupText.rectTransform.sizeDelta = new Vector2(320f, 85f);
            scorePopupText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            scorePopupText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            scorePopupText.rectTransform.pivot = new Vector2(0.5f, 1f);
            scorePopupText.rectTransform.anchoredPosition = new Vector2(0f, -680f);

            RectTransform optionsParent = CreateUIObject("OptionsParent_Grid", gameplayPanel);
            optionsParent.sizeDelta = new Vector2(860f, 560f);
            optionsParent.anchorMin = new Vector2(0.5f, 0f);
            optionsParent.anchorMax = new Vector2(0.5f, 0f);
            optionsParent.pivot = new Vector2(0.5f, 0f);
            optionsParent.anchoredPosition = new Vector2(0f, 130f);

            GridLayoutGroup grid = optionsParent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(380f, 215f);
            grid.spacing = new Vector2(30f, 30f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.MiddleCenter;

            ImageChoiceRevealOptionButton optionTemplate = CreateSceneOptionTemplate(optionsParent);
            optionTemplate.gameObject.SetActive(false);

            Button pauseButton = CreateFloatingCornerButton("PauseButton", gameplayPanel, "II", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(36f, 36f));
            Button howToPlayButton = CreateFloatingCornerButton("HowToPlayButton", gameplayPanel, "?", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-36f, 36f));

            GameObject resultPanel;
            TMP_Text resultTitleText;
            TMP_Text resultScoreText;
            TMP_Text resultCorrectText;
            TMP_Text resultWrongText;
            Button restartButton;
            Button continueButton;
            CreateResultPanel(root, out resultPanel, out resultTitleText, out resultScoreText, out resultCorrectText, out resultWrongText, out restartButton, out continueButton);

            GameObject pausePanel;
            Button resumeButton;
            CreatePausePanel(root, out pausePanel, out resumeButton);

            GameObject howToPlayPanel;
            TMP_Text howToPlayText;
            Button closeHowToPlayButton;
            CreateHowToPlayPanel(root, out howToPlayPanel, out howToPlayText, out closeHowToPlayButton);

            AudioSource sfxSource = managerObject.AddComponent<AudioSource>();
            AudioSource questionAudioSource = managerObject.AddComponent<AudioSource>();
            AudioSource musicSource = managerObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            questionAudioSource.playOnAwake = false;
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            AssignManagerReferences(
                manager,
                root,
                loadingHeadingText,
                instructionText,
                loadingPanel,
                loadingCanvasGroup,
                loadingSlider,
                loadingText,
                gameplayPanel.gameObject,
                gameplayCanvasGroup,
                questionImage,
                optionsParent,
                optionTemplate,
                scoreText,
                timerText,
                questionCounterText,
                feedbackText,
                scorePopupText,
                resultPanel,
                resultTitleText,
                resultScoreText,
                resultCorrectText,
                resultWrongText,
                pausePanel,
                howToPlayPanel,
                howToPlayText,
                hintButton,
                pauseButton,
                resumeButton,
                restartButton,
                continueButton,
                howToPlayButton,
                closeHowToPlayButton,
                sfxSource,
                questionAudioSource,
                musicSource
            );

            Selection.activeGameObject = managerObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[ImageChoiceReveal] Scene Template UI created. Customize inactive OptionButtonTemplate inside OptionsParent_Grid. It supports Image OR Text options.");
        }

        private static void CreateLoadingPanel(Transform parent, out GameObject panel, out CanvasGroup canvasGroup, out TMP_Text headingText, out Slider loadingSlider, out TMP_Text loadingText)
        {
            RectTransform loading = CreatePanel("LoadingPanel", parent, new Color(0.85f, 0.93f, 1f, 1f));
            Stretch(loading);
            canvasGroup = loading.gameObject.AddComponent<CanvasGroup>();

            headingText = CreateText("LoadingGameHeadingText", loading, "Guess The Object", 64, TextAlignmentOptions.Center, new Color(0.08f, 0.16f, 0.38f, 1f));
            headingText.rectTransform.sizeDelta = new Vector2(850f, 130f);
            headingText.rectTransform.anchorMin = new Vector2(0.5f, 0.58f);
            headingText.rectTransform.anchorMax = new Vector2(0.5f, 0.58f);
            headingText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            headingText.rectTransform.anchoredPosition = Vector2.zero;

            loadingText = CreateText("LoadingText", loading, "Loading...", 30, TextAlignmentOptions.Center, new Color(0.18f, 0.28f, 0.55f, 1f));
            loadingText.rectTransform.sizeDelta = new Vector2(500f, 70f);
            loadingText.rectTransform.anchorMin = new Vector2(0.5f, 0.46f);
            loadingText.rectTransform.anchorMax = new Vector2(0.5f, 0.46f);
            loadingText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            loadingText.rectTransform.anchoredPosition = Vector2.zero;

            RectTransform sliderRoot = CreatePanel("LoadingSlider", loading, new Color(1f, 1f, 1f, 0.9f));
            sliderRoot.sizeDelta = new Vector2(620f, 34f);
            sliderRoot.anchorMin = new Vector2(0.5f, 0.4f);
            sliderRoot.anchorMax = new Vector2(0.5f, 0.4f);
            sliderRoot.pivot = new Vector2(0.5f, 0.5f);
            sliderRoot.anchoredPosition = Vector2.zero;

            loadingSlider = sliderRoot.gameObject.AddComponent<Slider>();
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = 0f;
            loadingSlider.interactable = false;

            RectTransform fillArea = CreateUIObject("Fill Area", sliderRoot);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(6f, 6f);
            fillArea.offsetMax = new Vector2(-6f, -6f);

            RectTransform fill = CreatePanel("Fill", fillArea, new Color(0.28f, 0.55f, 1f, 1f));
            Stretch(fill);

            loadingSlider.fillRect = fill;
            loadingSlider.targetGraphic = sliderRoot.GetComponent<Image>();

            panel = loading.gameObject;
        }

        private static ImageChoiceRevealOptionButton CreateSceneOptionTemplate(Transform parent)
        {
            GameObject root = new GameObject("OptionButtonTemplate");
            root.transform.SetParent(parent, false);

            RectTransform rect = root.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(380f, 215f);

            Image background = root.AddComponent<Image>();
            background.color = Color.white;

            Button button = root.AddComponent<Button>();
            button.targetGraphic = background;

            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            Image optionImage = CreateImage("OptionImage", rect);
            Stretch(optionImage.rectTransform);
            optionImage.rectTransform.offsetMin = new Vector2(28f, 24f);
            optionImage.rectTransform.offsetMax = new Vector2(-28f, -24f);
            optionImage.preserveAspect = true;

            TMP_Text optionText = CreateText("OptionText", rect, "Option", 42, TextAlignmentOptions.Center, new Color(0.08f, 0.12f, 0.26f, 1f));
            Stretch(optionText.rectTransform);
            optionText.rectTransform.offsetMin = new Vector2(26f, 20f);
            optionText.rectTransform.offsetMax = new Vector2(-26f, -20f);
            optionText.enableAutoSizing = true;
            optionText.fontSizeMin = 22f;
            optionText.fontSizeMax = 54f;
            optionText.enableWordWrapping = true;
            optionText.gameObject.SetActive(false);

            Image overlay = CreateImage("FeedbackOverlay", rect);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(1f, 1f, 1f, 0f);
            overlay.raycastTarget = false;

            ImageChoiceRevealOptionButton optionButton = root.AddComponent<ImageChoiceRevealOptionButton>();

            SerializedObject so = new SerializedObject(optionButton);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("optionImage").objectReferenceValue = optionImage;
            so.FindProperty("optionText").objectReferenceValue = optionText;
            so.FindProperty("feedbackOverlay").objectReferenceValue = overlay;
            so.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            return optionButton;
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("ImageChoiceRevealCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateUIObject(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<Image>();
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.enableWordWrapping = true;
            return text;
        }

        private static TMP_Text CreateRowText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment, Color color, float width)
        {
            TMP_Text text = CreateText(name, parent, value, fontSize, alignment, color);
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width * 0.75f;
            layout.flexibleWidth = 0f;
            return text;
        }

        private static Button CreateInlineButton(string name, Transform parent, string text, float width, float height)
        {
            RectTransform rect = CreatePanel(name, parent, new Color(0.2f, 0.25f, 0.5f, 1f));
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.minWidth = width;
            layout.preferredHeight = height;
            layout.minHeight = height;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            TMP_Text buttonText = CreateText("Text", rect, text, 24, TextAlignmentOptions.Center, Color.white);
            Stretch(buttonText.rectTransform);
            return button;
        }

        private static Button CreateFloatingCornerButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
        {
            RectTransform rect = CreatePanel(name, parent, new Color(0.2f, 0.25f, 0.5f, 1f));
            rect.sizeDelta = new Vector2(86f, 66f);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            TMP_Text buttonText = CreateText("Text", rect, text, 25, TextAlignmentOptions.Center, Color.white);
            Stretch(buttonText.rectTransform);
            return button;
        }

        private static Button CreateLargeButton(string name, Transform parent, string text)
        {
            RectTransform rect = CreatePanel(name, parent, new Color(0.2f, 0.25f, 0.5f, 1f));
            rect.sizeDelta = new Vector2(360f, 90f);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            TMP_Text buttonText = CreateText("Text", rect, text, 34, TextAlignmentOptions.Center, Color.white);
            Stretch(buttonText.rectTransform);
            return button;
        }

        private static void CreateResultPanel(Transform parent, out GameObject panel, out TMP_Text titleText, out TMP_Text scoreText, out TMP_Text correctText, out TMP_Text wrongText, out Button restartButton, out Button continueButton)
        {
            RectTransform overlay = CreatePanel("ResultPanel", parent, new Color(0f, 0f, 0f, 0.78f));
            overlay.gameObject.AddComponent<CanvasGroup>();
            Stretch(overlay);

            RectTransform card = CreatePanel("Card", overlay, Color.white);
            card.sizeDelta = new Vector2(820f, 690f);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;

            titleText = CreateText("ResultTitleText", card, "Game Complete!", 52, TextAlignmentOptions.Center, new Color(0.1f, 0.12f, 0.22f, 1f));
            titleText.rectTransform.sizeDelta = new Vector2(720f, 100f);
            titleText.rectTransform.anchorMin = titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -65f);

            scoreText = CreateText("ResultScoreText", card, "Score: 0", 42, TextAlignmentOptions.Center, new Color(0.1f, 0.12f, 0.22f, 1f));
            scoreText.rectTransform.sizeDelta = new Vector2(700f, 85f);
            scoreText.rectTransform.anchorMin = scoreText.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
            scoreText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            scoreText.rectTransform.anchoredPosition = Vector2.zero;

            RectTransform statsRow = CreateUIObject("StatsRow", card);
            statsRow.sizeDelta = new Vector2(650f, 90f);
            statsRow.anchorMin = statsRow.anchorMax = new Vector2(0.5f, 0.47f);
            statsRow.pivot = new Vector2(0.5f, 0.5f);
            statsRow.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup statsLayout = statsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            statsLayout.spacing = 28f;
            statsLayout.childAlignment = TextAnchor.MiddleCenter;
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = true;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = true;

            correctText = CreateText("ResultCorrectText", statsRow, "Correct: 0", 32, TextAlignmentOptions.Center, new Color(0.08f, 0.48f, 0.18f, 1f));
            wrongText = CreateText("ResultWrongText", statsRow, "Wrong: 0", 32, TextAlignmentOptions.Center, new Color(0.78f, 0.12f, 0.12f, 1f));

            restartButton = CreateLargeButton("RestartButton", card, "Restart");
            RectTransform restartRect = restartButton.GetComponent<RectTransform>();
            restartRect.anchorMin = restartRect.anchorMax = new Vector2(0.32f, 0f);
            restartRect.pivot = new Vector2(0.5f, 0f);
            restartRect.anchoredPosition = new Vector2(0f, 75f);
            restartRect.sizeDelta = new Vector2(300f, 90f);

            continueButton = CreateLargeButton("ContinueButton", card, "Continue");
            RectTransform continueRect = continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = continueRect.anchorMax = new Vector2(0.68f, 0f);
            continueRect.pivot = new Vector2(0.5f, 0f);
            continueRect.anchoredPosition = new Vector2(0f, 75f);
            continueRect.sizeDelta = new Vector2(300f, 90f);

            panel = overlay.gameObject;
            panel.SetActive(false);
        }

        private static void CreatePausePanel(Transform parent, out GameObject panel, out Button resumeButton)
        {
            RectTransform overlay = CreatePanel("PausePanel", parent, new Color(0f, 0f, 0f, 0.72f));
            overlay.gameObject.AddComponent<CanvasGroup>();
            Stretch(overlay);
            RectTransform card = CreatePanel("Card", overlay, Color.white);
            card.sizeDelta = new Vector2(650f, 430f);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            TMP_Text title = CreateText("PauseTitleText", card, "Paused", 56, TextAlignmentOptions.Center, new Color(0.1f, 0.12f, 0.22f, 1f));
            title.rectTransform.sizeDelta = new Vector2(600f, 130f);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            resumeButton = CreateLargeButton("ResumeButton", card, "Resume");
            RectTransform resumeRect = resumeButton.GetComponent<RectTransform>();
            resumeRect.anchorMin = resumeRect.anchorMax = new Vector2(0.5f, 0f);
            resumeRect.pivot = new Vector2(0.5f, 0f);
            resumeRect.anchoredPosition = new Vector2(0f, 80f);
            panel = overlay.gameObject;
            panel.SetActive(false);
        }

        private static void CreateHowToPlayPanel(Transform parent, out GameObject panel, out TMP_Text howToPlayText, out Button closeButton)
        {
            RectTransform overlay = CreatePanel("HowToPlayPanel", parent, new Color(0f, 0f, 0f, 0.72f));
            overlay.gameObject.AddComponent<CanvasGroup>();
            Stretch(overlay);
            RectTransform card = CreatePanel("Card", overlay, Color.white);
            card.sizeDelta = new Vector2(820f, 650f);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            TMP_Text title = CreateText("HowToPlayTitleText", card, "How To Play", 52, TextAlignmentOptions.Center, new Color(0.1f, 0.12f, 0.22f, 1f));
            title.rectTransform.sizeDelta = new Vector2(720f, 100f);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -55f);
            howToPlayText = CreateText("HowToPlayText", card, "Look carefully and choose the correct option.", 34, TextAlignmentOptions.Top, new Color(0.12f, 0.13f, 0.18f, 1f));
            howToPlayText.rectTransform.sizeDelta = new Vector2(700f, 300f);
            howToPlayText.rectTransform.anchorMin = howToPlayText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            howToPlayText.rectTransform.pivot = new Vector2(0.5f, 1f);
            howToPlayText.rectTransform.anchoredPosition = new Vector2(0f, -180f);
            closeButton = CreateLargeButton("CloseHowToPlayButton", card, "Close");
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 65f);
            panel = overlay.gameObject;
            panel.SetActive(false);
        }

        private static void AssignManagerReferences(ImageChoiceRevealGameManager manager, Transform fontApplyRoot, TMP_Text loadingHeadingText, TMP_Text gameInstructionText, GameObject loadingPanel, CanvasGroup loadingCanvasGroup, Slider loadingSlider, TMP_Text loadingText, GameObject gameplayPanel, CanvasGroup gameplayCanvasGroup, Image questionImage, RectTransform optionsParent, ImageChoiceRevealOptionButton optionButtonTemplate, TMP_Text scoreText, TMP_Text timerText, TMP_Text questionCounterText, TMP_Text feedbackText, TMP_Text scorePopupText, GameObject resultPanel, TMP_Text resultTitleText, TMP_Text resultScoreText, TMP_Text resultCorrectText, TMP_Text resultWrongText, GameObject pausePanel, GameObject howToPlayPanel, TMP_Text howToPlayText, Button hintButton, Button pauseButton, Button resumeButton, Button restartButton, Button continueButton, Button howToPlayButton, Button closeHowToPlayButton, AudioSource sfxSource, AudioSource questionAudioSource, AudioSource musicSource)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("loadingHeadingText").objectReferenceValue = loadingHeadingText;
            so.FindProperty("gameInstructionText").objectReferenceValue = gameInstructionText;
            so.FindProperty("fontApplyRoot").objectReferenceValue = fontApplyRoot;

            SerializedProperty primaryFontTexts = so.FindProperty("primaryFontTexts");
            primaryFontTexts.arraySize = 1;
            primaryFontTexts.GetArrayElementAtIndex(0).objectReferenceValue = loadingHeadingText;
            so.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;
            so.FindProperty("loadingCanvasGroup").objectReferenceValue = loadingCanvasGroup;
            so.FindProperty("loadingSlider").objectReferenceValue = loadingSlider;
            so.FindProperty("loadingText").objectReferenceValue = loadingText;
            so.FindProperty("gameplayPanel").objectReferenceValue = gameplayPanel;
            so.FindProperty("gameplayCanvasGroup").objectReferenceValue = gameplayCanvasGroup;
            so.FindProperty("questionImage").objectReferenceValue = questionImage;
            so.FindProperty("optionsParent").objectReferenceValue = optionsParent;
            so.FindProperty("optionButtonTemplate").objectReferenceValue = optionButtonTemplate;
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("questionCounterText").objectReferenceValue = questionCounterText;
            so.FindProperty("feedbackText").objectReferenceValue = feedbackText;
            so.FindProperty("scorePopupText").objectReferenceValue = scorePopupText;
            so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            so.FindProperty("resultTitleText").objectReferenceValue = resultTitleText;
            so.FindProperty("resultScoreText").objectReferenceValue = resultScoreText;
            so.FindProperty("resultCorrectText").objectReferenceValue = resultCorrectText;
            so.FindProperty("resultWrongText").objectReferenceValue = resultWrongText;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("howToPlayPanel").objectReferenceValue = howToPlayPanel;
            so.FindProperty("howToPlayText").objectReferenceValue = howToPlayText;
            so.FindProperty("hintButton").objectReferenceValue = hintButton;
            so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            so.FindProperty("restartButton").objectReferenceValue = restartButton;
            so.FindProperty("continueButton").objectReferenceValue = continueButton;
            so.FindProperty("howToPlayButton").objectReferenceValue = howToPlayButton;
            so.FindProperty("closeHowToPlayButton").objectReferenceValue = closeHowToPlayButton;
            so.FindProperty("sfxSource").objectReferenceValue = sfxSource;
            so.FindProperty("questionAudioSource").objectReferenceValue = questionAudioSource;
            so.FindProperty("musicSource").objectReferenceValue = musicSource;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder)) AssetDatabase.CreateFolder("Assets", "ImageChoiceRevealGame");
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
        }
    }
}
#endif
