#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EmotionTimerQuiz.EditorTools
{
    public static class EmotionTimerQuizSceneBuilder
    {
        private const string RootFolder = "Assets/EmotionTimerQuiz";
        private const string GeneratedFolder = "Assets/EmotionTimerQuiz/Generated";
        private const string TextureFolder = "Assets/EmotionTimerQuiz/Generated/Textures";
        private const string SampleQuestionSetPath = "Assets/EmotionTimerQuiz/Generated/SampleEmotionTimerQuizQuestionSet.asset";

        [MenuItem("Tools/Emotion Timer Quiz/Create Clean Scene")]
        public static void CreateCleanScene()
        {
            EnsureFolders();
            Sprite roundedSprite = EnsureRoundedRectSprite();
            EmotionTimerQuizQuestionSet questionSet = CreateOrRefreshSampleQuestionSet();

            GameObject existingCanvas = GameObject.Find("EmotionTimerQuizCanvas");
            if (existingCanvas != null)
            {
                Object.DestroyImmediate(existingCanvas);
            }

            EnsureEventSystem();

            GameObject canvasObject = new GameObject("EmotionTimerQuizCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;

            GameObject backgroundRoot = CreateStretchObject("BackgroundRoot_PutYourBGHere", canvasObject.transform);
            backgroundRoot.transform.SetAsFirstSibling();

            GameObject safeArea = CreateStretchObject("SafeAreaRoot_ContentOnly", canvasObject.transform);
            safeArea.AddComponent<EmotionSafeAreaFitter>();

            GameObject managerObject = new GameObject("EmotionTimerQuizManager");
            managerObject.transform.SetParent(canvasObject.transform, false);
            EmotionTimerQuizManager manager = managerObject.AddComponent<EmotionTimerQuizManager>();
            EmotionTimerQuizAudioManager audioManager = managerObject.AddComponent<EmotionTimerQuizAudioManager>();
            manager.audioManager = audioManager;
            manager.questionSet = questionSet;
            manager.assetRegistry = EmotionTimerQuizUtility.CreateEmptySpriteRegistry();
            manager.questionLimit = 25;
            manager.showLoadingPanelOnStart = true;
            manager.loadingDurationSeconds = 1.5f;
            manager.gameTitle = "Emotion Timer Quiz";
            manager.showHowToPlayOnStart = true;
            manager.autoContinueAfterAnswer = true;
            manager.autoContinueDelaySeconds = 10;
            manager.guideFallbackTexts = new List<string>
            {
                "Read the situation in the yellow card. Think about how the character feels.",
                "Tap one emotion card before TIME LEFT reaches zero. All options use the same character.",
                "After answering, tap NEXT ROUND or wait for its 10-second countdown to continue automatically."
            };

            BuildTopHud(safeArea.transform, roundedSprite, manager);
            BuildSituationCard(safeArea.transform, roundedSprite, manager);
            BuildOptionCards(safeArea.transform, roundedSprite, manager);
            BuildFooterActions(safeArea.transform, roundedSprite, manager);
            BuildOverlayPanels(canvasObject.transform, roundedSprite, manager);

            EditorUtility.SetDirty(managerObject);
            Selection.activeGameObject = canvasObject;
            Debug.Log("Emotion Timer Quiz clean scene created. No prefab used. BackgroundRoot is outside safe area; gameplay content is under SafeAreaRoot_ContentOnly.");
        }

        [MenuItem("Tools/Emotion Timer Quiz/Create 25 Questions")]
        public static void CreateSampleQuestionSetMenu()
        {
            EnsureFolders();
            EmotionTimerQuizQuestionSet asset = CreateOrRefreshSampleQuestionSet();
            Selection.activeObject = asset;
            Debug.Log("25-question set ready: " + SampleQuestionSetPath);
        }

        private static void BuildTopHud(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject hud = CreateRectObject("TopHUD", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(1600f, 90f));

            GameObject roundCapsule = CreateImage("RoundCapsule", hud.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(175f, 0f), new Vector2(290f, 62f), new Color(0.79f, 0.94f, 0.83f, 1f), roundedSprite);
            manager.roundText = CreateText("RoundText", roundCapsule.transform, "ROUND 1 / 25", 28, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));

            GameObject scoreCapsule = CreateImage("ScoreCapsule", hud.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-455f, 0f), new Vector2(255f, 62f), new Color(0.88f, 0.90f, 0.99f, 1f), roundedSprite);
            manager.scoreText = CreateText("ScoreText", scoreCapsule.transform, "SCORE: 0", 28, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));

            GameObject timerCapsule = CreateImage("TimerCapsule", hud.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-165f, 0f), new Vector2(300f, 62f), new Color(1f, 0.76f, 0.72f, 1f), roundedSprite);
            manager.timerText = CreateText("TimerText", timerCapsule.transform, "TIME LEFT: 15s", 27, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));

            GameObject sliderBack = CreateImage("TimerProgressSlider", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -118f), new Vector2(1120f, 16f), new Color(0.94f, 0.94f, 0.94f, 1f), roundedSprite);
            Slider timerSlider = sliderBack.AddComponent<Slider>();
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
            timerSlider.value = 1f;
            timerSlider.wholeNumbers = false;
            timerSlider.transition = Selectable.Transition.None;
            timerSlider.direction = Slider.Direction.LeftToRight;

            GameObject fill = CreateImage("Fill", sliderBack.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 0.63f, 0.60f, 1f), roundedSprite);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.raycastTarget = false;
            timerSlider.fillRect = fill.GetComponent<RectTransform>();
            timerSlider.targetGraphic = fillImage;
            manager.timerSlider = timerSlider;
        }

        private static void BuildSituationCard(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject card = CreateImage("SituationCard", parent, new Vector2(0.5f, 0.61f), new Vector2(0.5f, 0.61f), Vector2.zero, new Vector2(1260f, 175f), new Color(1f, 0.93f, 0.74f, 1f), roundedSprite);
            manager.situationCardTransform = card.GetComponent<RectTransform>();

            TextMeshProUGUI text = CreateText("SituationText", card.transform, "Rajes sees a massive spider on his bed!", 42, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));
            text.margin = new Vector4(60f, 20f, 60f, 20f);
            manager.situationText = text;

            GameObject feedback = CreateRectObject("FeedbackTextHolder", parent, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(900f, 60f));
            TextMeshProUGUI feedbackText = CreateText("FeedbackText", feedback.transform, string.Empty, 30, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));
            manager.feedbackText = feedbackText;
        }

        private static void BuildOptionCards(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject row = CreateRectObject("OptionCardsRow", parent, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), Vector2.zero, new Vector2(1360f, 350f));
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 35f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            EmotionOptionCard cardA = CreateOptionCard("OptionCard_A", row.transform, roundedSprite, new Color(0.82f, 0.95f, 0.84f, 1f), "A");
            EmotionOptionCard cardB = CreateOptionCard("OptionCard_B", row.transform, roundedSprite, new Color(0.89f, 0.86f, 0.98f, 1f), "B");
            EmotionOptionCard cardC = CreateOptionCard("OptionCard_C", row.transform, roundedSprite, new Color(1f, 0.85f, 0.85f, 1f), "C");

            manager.optionCards = new EmotionOptionCard[] { cardA, cardB, cardC };
        }

        private static void BuildFooterActions(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject pauseHolder = CreateRectObject("PauseActionLeft", parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(115f, 72f), new Vector2(210f, 68f));
            Button pauseButton = CreateButton("PauseButton", pauseHolder.transform, "PAUSE", new Vector2(180f, 62f), new Color(0.92f, 0.96f, 0.94f, 1f), roundedSprite);

            GameObject nextHolder = CreateRectObject("NextRoundActionRight", parent, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-210f, 72f), new Vector2(360f, 68f));
            Button nextButton = CreateButton("NextRoundButton", nextHolder.transform, "NEXT ROUND", new Vector2(330f, 62f), new Color(0.79f, 0.94f, 0.83f, 1f), roundedSprite);

            GameObject countdownFill = CreateImage("CountdownFill", nextButton.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.62f, 0.88f, 0.71f, 0.55f), roundedSprite);
            Image countdownImage = countdownFill.GetComponent<Image>();
            countdownImage.type = Image.Type.Filled;
            countdownImage.fillMethod = Image.FillMethod.Horizontal;
            countdownImage.fillOrigin = 0;
            countdownImage.fillAmount = 0f;
            countdownImage.raycastTarget = false;
            countdownFill.transform.SetAsFirstSibling();

            manager.pauseButton = pauseButton;
            manager.pauseButtonText = pauseButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            manager.nextRoundButton = nextButton;
            manager.nextRoundButtonText = nextButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            manager.nextRoundCountdownFillImage = countdownImage;
        }

        private static void BuildOverlayPanels(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject timeoutBanner = CreateImage("TimeoutBanner", parent, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(520f, 90f), new Color(1f, 0.76f, 0.72f, 0.96f), roundedSprite);
            CreateText("TimeoutBannerText", timeoutBanner.transform, "Time's Up!", 42, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));
            timeoutBanner.SetActive(false);
            manager.timeoutBanner = timeoutBanner;

            BuildLoadingPanel(parent, roundedSprite, manager);
            BuildHowToPlayPanel(parent, roundedSprite, manager);
            BuildPausePanel(parent, roundedSprite, manager);
            BuildResultPanel(parent, roundedSprite, manager);
        }

        private static void BuildLoadingPanel(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject overlay = CreateImage("LoadingPanel", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white, null);
            GameObject panel = CreateImage("Panel", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 420f), new Color(1f, 0.96f, 0.86f, 1f), roundedSprite);

            TextMeshProUGUI title = CreateText("LoadingTitleText", panel.transform, "Emotion Timer Quiz", 64, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f), new Vector2(0.5f, 0.66f), new Vector2(760f, 100f));
            TextMeshProUGUI hint = CreateText("LoadingBodyText", panel.transform, "Get ready to choose the correct feeling.", 28, FontStyles.Normal, new Color(0.38f, 0.45f, 0.53f, 1f), new Vector2(0.5f, 0.49f), new Vector2(720f, 55f));
            hint.enableWordWrapping = true;

            GameObject sliderBack = CreateImage("LoadingSlider", panel.transform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(640f, 28f), new Color(0.92f, 0.92f, 0.92f, 1f), roundedSprite);
            Slider slider = sliderBack.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.wholeNumbers = false;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;

            GameObject fill = CreateImage("LoadingSliderFill", sliderBack.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.79f, 0.94f, 0.83f, 1f), roundedSprite);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.raycastTarget = false;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            overlay.SetActive(false);
            manager.loadingPanel = overlay;
            manager.loadingTitleText = title;
            manager.loadingSlider = slider;
        }

        private static void BuildHowToPlayPanel(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject overlay = CreateImage("HowToPlayPanel", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.78f), null);
            GameObject panel = CreateImage("Panel", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(940f, 680f), new Color(1f, 0.96f, 0.86f, 1f), roundedSprite);

            CreateText("TitleText", panel.transform, "How To Play", 52, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f), new Vector2(0.5f, 0.88f), new Vector2(760f, 80f));

            GameObject imageFrame = CreateImage("GuideImageFrame", panel.transform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(720f, 310f), new Color(1f, 1f, 1f, 0.65f), roundedSprite);
            GameObject imageObj = CreateRectObject("GuideImage", imageFrame.transform, Vector2.zero, Vector2.one, new Vector2(0f, 0f), new Vector2(-40f, -36f));
            Image guideImage = imageObj.AddComponent<Image>();
            guideImage.preserveAspect = true;
            guideImage.enabled = false;
            guideImage.raycastTarget = false;

            TextMeshProUGUI guideText = CreateText("GuideText", panel.transform, "Read the situation. Tap the matching emotion before the timer ends.", 30, FontStyles.Normal, new Color(0.16f, 0.23f, 0.33f, 1f), new Vector2(0.5f, 0.29f), new Vector2(760f, 115f));
            guideText.enableWordWrapping = true;

            TextMeshProUGUI counterText = CreateText("GuideCounterText", panel.transform, "1 / 3", 24, FontStyles.Bold, new Color(0.38f, 0.45f, 0.53f, 1f), new Vector2(0.5f, 0.18f), new Vector2(220f, 40f));

            Button prevButton = CreateSmallPanelButton(panel.transform, roundedSprite, "PREV", new Vector2(-270f, -250f), new Vector2(170f, 62f));
            Button nextButton = CreateSmallPanelButton(panel.transform, roundedSprite, "NEXT", new Vector2(0f, -250f), new Vector2(170f, 62f));
            Button startButton = CreateSmallPanelButton(panel.transform, roundedSprite, "START", new Vector2(270f, -250f), new Vector2(190f, 62f));

            manager.howToPlayPanel = overlay;
            manager.guideImage = guideImage;
            manager.guideText = guideText;
            manager.guideCounterText = counterText;
            manager.guidePrevButton = prevButton;
            manager.guideNextButton = nextButton;
            manager.guideStartButton = startButton;
            manager.guideStartButtonText = startButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        }

        private static void BuildPausePanel(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject overlay = CreateImage("PausePanel", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.72f), null);
            GameObject panel = CreateImage("Panel", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 500f), new Color(1f, 0.96f, 0.86f, 1f), roundedSprite);

            CreateText("TitleText", panel.transform, "Paused", 52, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f), new Vector2(0.5f, 0.75f), new Vector2(650f, 80f));
            TextMeshProUGUI bodyText = CreateText("BodyText", panel.transform, "Take a small break. You can review the guide or continue the question.", 28, FontStyles.Normal, new Color(0.16f, 0.23f, 0.33f, 1f), new Vector2(0.5f, 0.57f), new Vector2(620f, 95f));
            bodyText.enableWordWrapping = true;

            Button resumeButton = CreateSmallPanelButton(panel.transform, roundedSprite, "RESUME", new Vector2(0f, -65f), new Vector2(280f, 62f));
            Button howToPlayButton = CreateSmallPanelButton(panel.transform, roundedSprite, "HOW TO PLAY", new Vector2(0f, -145f), new Vector2(280f, 62f));
            Button restartButton = CreateSmallPanelButton(panel.transform, roundedSprite, "RESTART", new Vector2(0f, -225f), new Vector2(280f, 62f));

            overlay.SetActive(false);
            manager.pausePanel = overlay;
            manager.resumeButton = resumeButton;
            manager.pauseHowToPlayButton = howToPlayButton;
            manager.restartButton = restartButton;
        }

        private static void BuildResultPanel(Transform parent, Sprite roundedSprite, EmotionTimerQuizManager manager)
        {
            GameObject overlay = CreateImage("ResultPanel", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.72f), null);
            GameObject panel = CreateImage("Panel", overlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 560f), new Color(1f, 0.96f, 0.86f, 1f), roundedSprite);

            TextMeshProUGUI title = CreateText("TitleText", panel.transform, "Great Work!", 52, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f), new Vector2(0.5f, 0.80f), new Vector2(680f, 80f));
            TextMeshProUGUI score = CreateText("ScoreText", panel.transform, "Final Score: 0", 34, FontStyles.Bold, new Color(0.16f, 0.23f, 0.33f, 1f), new Vector2(0.5f, 0.64f), new Vector2(680f, 60f));
            TextMeshProUGUI stats = CreateText("StatsText", panel.transform, "Questions: 0\nCorrect: 0\nWrong: 0\nTimed Out: 0\nTime Taken: 0s", 27, FontStyles.Normal, new Color(0.16f, 0.23f, 0.33f, 1f), new Vector2(0.5f, 0.43f), new Vector2(680f, 160f));
            stats.alignment = TextAlignmentOptions.Center;
            stats.enableWordWrapping = true;

            Button continueButton = CreateSmallPanelButton(panel.transform, roundedSprite, "CONTINUE", new Vector2(-160f, -205f), new Vector2(250f, 65f));
            Button playAgain = CreateSmallPanelButton(panel.transform, roundedSprite, "PLAY AGAIN", new Vector2(160f, -205f), new Vector2(250f, 65f));

            overlay.SetActive(false);
            manager.resultPanel = overlay;
            manager.resultContinueButton = continueButton;
            manager.resultRestartButton = playAgain;
            manager.resultTitleText = title;
            manager.resultScoreText = score;
            manager.resultStatsText = stats;
        }

        private static EmotionOptionCard CreateOptionCard(string name, Transform parent, Sprite roundedSprite, Color color, string letter)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(LayoutElement));
            card.transform.SetParent(parent, false);

            RectTransform rect = card.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(420f, 330f);

            LayoutElement layoutElement = card.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 420f;
            layoutElement.preferredHeight = 330f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 1f;

            Image image = card.GetComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            Button button = card.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            EmotionOptionCard optionCard = card.AddComponent<EmotionOptionCard>();
            optionCard.button = button;
            optionCard.backgroundImage = image;
            optionCard.canvasGroup = card.GetComponent<CanvasGroup>();

            GameObject letterObj = CreateRectObject("LetterText", card.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -35f), new Vector2(60f, 45f));
            TextMeshProUGUI letterText = CreateText("Text", letterObj.transform, letter, 32, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));
            optionCard.letterText = letterText;

            GameObject imageObj = CreateRectObject("CharacterImage", card.transform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(250f, 185f));
            Image characterImage = imageObj.AddComponent<Image>();
            characterImage.preserveAspect = true;
            characterImage.enabled = false;
            characterImage.raycastTarget = false;
            optionCard.characterImage = characterImage;

            GameObject emotionObj = CreateRectObject("EmotionTextHolder", card.transform, new Vector2(0.5f, 0.21f), new Vector2(0.5f, 0.21f), Vector2.zero, new Vector2(320f, 48f));
            TextMeshProUGUI emotionText = CreateText("EmotionText", emotionObj.transform, "HAPPY", 34, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));
            optionCard.emotionText = emotionText;

            GameObject tapObj = CreateRectObject("TapTextHolder", card.transform, new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(320f, 35f));
            TextMeshProUGUI tapText = CreateText("TapText", tapObj.transform, "Tap to Select", 20, FontStyles.Normal, new Color(0.38f, 0.45f, 0.53f, 1f));
            optionCard.tapText = tapText;

            GameObject overlayObj = CreateImage("FeedbackOverlayImage", card.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f), roundedSprite);
            Image overlay = overlayObj.GetComponent<Image>();
            overlay.type = Image.Type.Sliced;
            overlay.raycastTarget = false;
            overlay.enabled = false;
            optionCard.feedbackOverlayImage = overlay;
            optionCard.correctOverlaySprite = roundedSprite;
            optionCard.wrongOverlaySprite = roundedSprite;
            optionCard.correctRevealOverlaySprite = roundedSprite;

            return optionCard;
        }

        private static Button CreateSmallPanelButton(Transform panel, Sprite roundedSprite, string label, Vector2 anchoredPosition, Vector2 size)
        {
            Button button = CreateButton(label.Replace(" ", string.Empty) + "Button", panel, label, size, new Color(0.79f, 0.94f, 0.83f, 1f), roundedSprite);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            return button;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 size, Color color, Sprite roundedSprite)
        {
            GameObject buttonObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            LayoutElement layoutElement = buttonObj.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;

            Image image = buttonObj.GetComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            Button button = buttonObj.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            CreateText("Text", buttonObj.transform, label, 25, FontStyles.Bold, new Color(0.08f, 0.15f, 0.25f, 1f));
            return button;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, FontStyles style, Color color)
        {
            return CreateText(name, parent, text, fontSize, style, color, new Vector2(0.5f, 0.5f), Vector2.zero);
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, FontStyles style, Color color, Vector2 anchor, Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();

            if (size == Vector2.zero)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = size;
            }

            TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMax = fontSize;
            tmp.fontSizeMin = Mathf.Max(12, fontSize - 12);
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateBlobBackground(Transform parent, Sprite circleSprite)
        {
            GameObject holder = CreateStretchObject("PastelBlobBackground", parent);
            CreateBlob("Blob_Mint_Left", holder.transform, circleSprite, new Vector2(0.08f, 0.78f), new Vector2(520f, 320f), new Color(0.74f, 0.94f, 0.84f, 0.55f));
            CreateBlob("Blob_Lavender_Right", holder.transform, circleSprite, new Vector2(0.92f, 0.76f), new Vector2(520f, 350f), new Color(0.84f, 0.80f, 0.96f, 0.48f));
            CreateBlob("Blob_Blush_BottomLeft", holder.transform, circleSprite, new Vector2(0.14f, 0.15f), new Vector2(540f, 360f), new Color(1f, 0.77f, 0.78f, 0.40f));
            CreateBlob("Blob_Peach_BottomRight", holder.transform, circleSprite, new Vector2(0.87f, 0.18f), new Vector2(520f, 330f), new Color(1f, 0.86f, 0.68f, 0.45f));
            return holder;
        }

        private static void CreateBlob(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 size, Color color)
        {
            GameObject blob = CreateImage(name, parent, anchor, anchor, Vector2.zero, size, color, sprite);
            blob.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-12f, 12f));
        }

        private static GameObject CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, Sprite sprite)
        {
            GameObject obj = CreateRectObject(name, parent, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Image image = obj.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sprite != null)
            {
                image.type = Image.Type.Sliced;
            }
            return obj;
        }

        private static GameObject CreateRectObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            if (anchorMin == Vector2.zero && anchorMax == Vector2.one && sizeDelta == Vector2.zero)
            {
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            return obj;
        }

        private static GameObject CreateStretchObject(string name, Transform parent)
        {
            return CreateRectObject(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Selection.activeGameObject = eventSystem;
        }

        private static EmotionTimerQuizQuestionSet CreateOrRefreshSampleQuestionSet()
        {
            EnsureFolders();
            EmotionTimerQuizQuestionSet asset = AssetDatabase.LoadAssetAtPath<EmotionTimerQuizQuestionSet>(SampleQuestionSetPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<EmotionTimerQuizQuestionSet>();
                AssetDatabase.CreateAsset(asset, SampleQuestionSetPath);
            }

            asset.questions.Clear();

            asset.questions.Add(new SituationQuestion
            {
                id = "Q001",
                situationText = "Rajes sees a massive spider on his bed!",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q002",
                situationText = "Tina gets a new box of crayons from her teacher.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q003",
                situationText = "Raj's paper boat tears before the race starts.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q004",
                situationText = "Tanvi practices hard and speaks clearly on stage.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q005",
                situationText = "Raj finds out his class is going on a picnic tomorrow!",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q006",
                situationText = "Tina drops her ice cream before taking a bite.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q007",
                situationText = "Rajes tells the truth even when it feels difficult.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 14
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q008",
                situationText = "Tanvi sees her little brother break her favorite pencil on purpose.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.ANGRY,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q009",
                situationText = "Raj hears thunder loudly while walking home.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 12
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q010",
                situationText = "Tina wins the classroom drawing star badge.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q011",
                situationText = "Rajes shares his lunch with a friend who forgot food.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q012",
                situationText = "Tanvi cannot find her school project before class.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 13
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q013",
                situationText = "Raj finishes reading a story aloud without mistakes.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q014",
                situationText = "Tina waits quietly while others get a turn first.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q015",
                situationText = "Rajes sees someone push his friend in the line.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.ANGRY,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q016",
                situationText = "Tanvi gets invited to play a new game at recess.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q017",
                situationText = "Raj opens a gift and finds the toy he wanted.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q018",
                situationText = "Tina has to speak in front of the whole class for the first time.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 12
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q019",
                situationText = "Rajes solves a hard maths puzzle by himself.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q020",
                situationText = "Tanvi loses a race after trying her best.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q021",
                situationText = "Raj sees his friend take his eraser without asking.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.ANGRY,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q022",
                situationText = "Tina helps a new student find the classroom.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q023",
                situationText = "Rajes hears that tomorrow is the school fun fair.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q024",
                situationText = "Tanvi stands up and answers the teacher clearly.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            asset.questions.Add(new SituationQuestion
            {
                id = "Q025",
                situationText = "Raj notices a puppy stuck near a busy road.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 12
            });
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
            {
                Directory.CreateDirectory(RootFolder);
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
            {
                Directory.CreateDirectory(GeneratedFolder);
            }

            if (!AssetDatabase.IsValidFolder(TextureFolder))
            {
                Directory.CreateDirectory(TextureFolder);
            }

            AssetDatabase.Refresh();
        }

        private static Sprite EnsureRoundedRectSprite()
        {
            string path = TextureFolder + "/RoundedRectWhite.png";
            if (!File.Exists(path))
            {
                Texture2D texture = GenerateRoundedRectTexture(96, 96, 28);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            ImportSprite(path, new Vector4(28f, 28f, 28f, 28f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite EnsureCircleSprite()
        {
            string path = TextureFolder + "/SoftCircleWhite.png";
            if (!File.Exists(path))
            {
                Texture2D texture = GenerateCircleTexture(128, 128);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            ImportSprite(path, Vector4.zero);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ImportSprite(string path, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        private static Texture2D GenerateRoundedRectTexture(int width, int height, int radius)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color white = Color.white;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, width, height, radius);
                    texture.SetPixel(x, y, inside ? white : clear);
                }
            }

            texture.Apply();
            return texture;
        }

        private static bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            int left = radius;
            int right = width - radius - 1;
            int bottom = radius;
            int top = height - radius - 1;

            if (x >= left && x <= right)
            {
                return true;
            }

            if (y >= bottom && y <= top)
            {
                return true;
            }

            int cx = x < left ? left : right;
            int cy = y < bottom ? bottom : top;
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static Texture2D GenerateCircleTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float radius = Mathf.Min(width, height) * 0.5f - 2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - ((distance - radius + 4f) / 4f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
#endif
