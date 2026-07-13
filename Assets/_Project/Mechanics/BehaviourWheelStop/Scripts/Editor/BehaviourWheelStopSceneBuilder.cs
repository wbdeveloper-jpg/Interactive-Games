#if UNITY_EDITOR
using System.Collections.Generic;
using BehaviourWheelStop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BehaviourWheelStopEditor
{
    public static class BehaviourWheelStopSceneBuilder
    {
        private static readonly Color Background = new Color(0.95f, 0.96f, 1f, 1f);
        private static readonly Color Card = new Color(1f, 1f, 1f, 0.96f);
        private static readonly Color Primary = new Color(0.28f, 0.36f, 0.72f, 1f);
        private static readonly Color Accent = new Color(1f, 0.73f, 0.30f, 1f);
        private static readonly Color Soft = new Color(0.86f, 0.90f, 1f, 1f);

        [MenuItem("Tools/Behaviour Wheel Stop/Create Rough UI")]
        public static void CreateRoughUI()
        {
            EnsureEventSystem();

            GameObject canvasGo = new GameObject("BehaviourWheelStopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Behaviour Wheel Stop UI");

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            Stretch(canvasRect);

            BehaviourWheelSafeArea safeArea = canvasGo.AddComponent<BehaviourWheelSafeArea>();
            safeArea.target = canvasRect;

            BehaviourWheelFontTheme fontTheme = canvasGo.AddComponent<BehaviourWheelFontTheme>();
            BehaviourWheelQuestionBank questionBank = canvasGo.AddComponent<BehaviourWheelQuestionBank>();
            questionBank.PopulateDefaultQuestions();
            BehaviourWheelAudioController audioController = canvasGo.AddComponent<BehaviourWheelAudioController>();
            BehaviourWheelGameManager gameManager = canvasGo.AddComponent<BehaviourWheelGameManager>();

            List<TMP_Text> primaryTexts = new List<TMP_Text>();
            List<TMP_Text> secondaryTexts = new List<TMP_Text>();

            CanvasGroup loadingPanel = CreatePanel("LoadingPanel", canvasRect, Background);
            BuildLoadingPanel(loadingPanel.transform, primaryTexts, secondaryTexts, out Slider loadingSlider, out TMP_Text loadingTitle);

            CanvasGroup howToPlayPanel = CreatePanel("HowToPlayPanel", canvasRect, Background);
            BuildHowToPlayPanel(howToPlayPanel.transform, primaryTexts, secondaryTexts,
                out Image htpImage, out TMP_Text htpTitle, out TMP_Text htpDesc, out TMP_Text htpPage,
                out Button htpPrev, out Button htpNext, out Button htpStart);

            CanvasGroup gameplayPanel = CreatePanel("GameplayPanel", canvasRect, Background);
            BuildGameplayPanel(gameplayPanel.transform, primaryTexts, secondaryTexts,
                out Image qCounterBg, out TMP_Text qCounter, out TMP_Text questionText, out Image scoreBg, out TMP_Text scoreText, out Button pauseButton,
                out Button stopButton, out TMP_Text instructionText, out CanvasGroup feedbackPanel, out TMP_Text feedbackText, out Image feedbackCardImage,
                out RectTransform centerArea, out RectTransform wheelRoot, out BehaviourWheelSpinner spinner);

            CanvasGroup pausePanel = CreatePanel("PausePanel", canvasRect, new Color(0f, 0f, 0f, 0.55f));
            BehaviourWheelPausePanel pause = BuildPausePanel(pausePanel.transform, primaryTexts, secondaryTexts);

            CanvasGroup resultPanel = CreatePanel("ResultPanel", canvasRect, Background);
            BehaviourWheelResultPanel result = BuildResultPanel(resultPanel.transform, primaryTexts, secondaryTexts);

            BehaviourWheelUI ui = canvasGo.AddComponent<BehaviourWheelUI>();
            ui.loadingPanel = loadingPanel;
            ui.howToPlayPanel = howToPlayPanel;
            ui.gameplayPanel = gameplayPanel;
            ui.pausePanel = pausePanel;
            ui.resultPanel = resultPanel;
            ui.feedbackPanel = feedbackPanel;
            ui.loadingSlider = loadingSlider;
            ui.loadingTitleText = loadingTitle;
            ui.questionCounterBackgroundImage = qCounterBg;
            ui.questionCounterText = qCounter;
            ui.questionText = questionText;
            ui.scoreBackgroundImage = scoreBg;
            ui.scoreText = scoreText;
            ui.pauseButton = pauseButton;
            ui.stopButton = stopButton;
            ui.instructionText = instructionText;
            ui.feedbackText = feedbackText;
            ui.feedbackBackgroundImage = feedbackCardImage;
            ui.howToPlayImage = htpImage;
            ui.howToPlayTitleText = htpTitle;
            ui.howToPlayDescriptionText = htpDesc;
            ui.howToPlayPageText = htpPage;
            ui.howToPlayPrevButton = htpPrev;
            ui.howToPlayNextButton = htpNext;
            ui.howToPlayStartButton = htpStart;
            ui.EnsureDefaultHowToPlayPages();

            BehaviourWheelResponsiveLayout responsive = gameplayPanel.gameObject.AddComponent<BehaviourWheelResponsiveLayout>();
            responsive.availableCenterArea = centerArea;
            responsive.wheelRoot = wheelRoot;
            responsive.spinner = spinner;

            gameManager.questionBank = questionBank;
            gameManager.spinner = spinner;
            gameManager.ui = ui;
            gameManager.pausePanel = pause;
            gameManager.resultPanel = result;
            gameManager.fontTheme = fontTheme;
            gameManager.audioController = audioController;

            fontTheme.primaryTexts = primaryTexts;
            fontTheme.secondaryTexts = secondaryTexts;

            SetPanelState(loadingPanel, true);
            SetPanelState(howToPlayPanel, false);
            SetPanelState(gameplayPanel, false);
            SetPanelState(pausePanel, false);
            SetPanelState(resultPanel, false);
            SetPanelState(feedbackPanel, false);

            Selection.activeGameObject = canvasGo;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static CanvasGroup CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateUI(name, parent);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        private static void BuildLoadingPanel(Transform parent, List<TMP_Text> primaryTexts, List<TMP_Text> secondaryTexts,
            out Slider loadingSlider, out TMP_Text title)
        {
            RectTransform card = CreateCard("LoadingCard", parent, new Vector2(620f, 360f));
            title = CreateText("Title_Loading", card, "Behaviour Wheel Stop", 52, FontStyles.Bold, TextAlignmentOptions.Center, Primary);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(560, 80));
            primaryTexts.Add(title);

            TMP_Text subtitle = CreateText("Body_Loading", card, "Get ready to stop the wheel!", 30, FontStyles.Normal, TextAlignmentOptions.Center, Color.black);
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 0.49f), new Vector2(0.5f, 0.49f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 60));
            secondaryTexts.Add(subtitle);

            loadingSlider = CreateSlider("LoadingSlider", card);
            SetRect(loadingSlider.GetComponent<RectTransform>(), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(480, 28));
        }

        private static void BuildHowToPlayPanel(Transform parent, List<TMP_Text> primaryTexts, List<TMP_Text> secondaryTexts,
            out Image pageImage, out TMP_Text titleText, out TMP_Text descText, out TMP_Text pageText,
            out Button prevButton, out Button nextButton, out Button startButton)
        {
            RectTransform card = CreateCard("HowToPlayCard", parent, new Vector2(980f, 680f));

            titleText = CreateText("Title_HowToPlay", card, "Read", 46, FontStyles.Bold, TextAlignmentOptions.Center, Primary);
            SetRect(titleText.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850, 60));
            primaryTexts.Add(titleText);

            RectTransform imageBox = CreateUI("HowToPlayImageSlot", card);
            SetRect(imageBox, new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.57f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 270));
            pageImage = imageBox.gameObject.AddComponent<Image>();
            pageImage.color = new Color(1f, 1f, 1f, 0.28f);

            TMP_Text imageSlotText = CreateText("Body_HTP_ImageSlotLabel", imageBox, "Image Slot\n(assign page sprites in Inspector)", 28, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.2f, 0.25f, 0.38f));
            Stretch(imageSlotText.rectTransform, 20, 20, 20, 20);
            secondaryTexts.Add(imageSlotText);

            descText = CreateText("Body_HowToPlayDescription", card, "Read the question carefully.", 34, FontStyles.Normal, TextAlignmentOptions.Center, Color.black);
            SetRect(descText.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 90));
            secondaryTexts.Add(descText);

            pageText = CreateText("Counter_HowToPlayPage", card, "1/3", 26, FontStyles.Bold, TextAlignmentOptions.Center, Primary);
            SetRect(pageText.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 46));
            primaryTexts.Add(pageText);

            prevButton = CreateButton("Button_Previous", card, "PREV", new Color(0.82f, 0.86f, 0.95f, 1f), primaryTexts);
            SetRect(prevButton.GetComponent<RectTransform>(), new Vector2(0.21f, 0.12f), new Vector2(0.21f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 58));

            nextButton = CreateButton("Button_Next", card, "NEXT", Accent, primaryTexts);
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.79f, 0.12f), new Vector2(0.79f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 58));

            startButton = CreateButton("Button_Start", card, "START", Accent, primaryTexts);
            SetRect(startButton.GetComponent<RectTransform>(), new Vector2(0.79f, 0.12f), new Vector2(0.79f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 60));
        }

        private static void BuildGameplayPanel(Transform parent, List<TMP_Text> primaryTexts, List<TMP_Text> secondaryTexts,
            out Image questionCounterBackground, out TMP_Text questionCounter, out TMP_Text questionText, out Image scoreBackground, out TMP_Text scoreText, out Button pauseButton,
            out Button stopButton, out TMP_Text instructionText, out CanvasGroup feedbackPanel, out TMP_Text feedbackText, out Image feedbackCardImage,
            out RectTransform centerArea, out RectTransform wheelRoot, out BehaviourWheelSpinner spinner)
        {
            RectTransform topBar = CreateUI("TopBar", parent);
            SetRect(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -18), new Vector2(-80, 170));
            Image topBg = topBar.gameObject.AddComponent<Image>();
            topBg.color = new Color(1f, 1f, 1f, 0.80f);

            RectTransform questionCounterBgRect = CreateUI("QuestionCounterBg_ImageSlot", topBar);
            SetRect(questionCounterBgRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26, 0), new Vector2(180, 84));
            questionCounterBackground = questionCounterBgRect.gameObject.AddComponent<Image>();
            questionCounterBackground.color = new Color(0.86f, 0.90f, 1f, 1f);
            questionCounterBackground.type = Image.Type.Sliced;
            questionCounterBackground.raycastTarget = false;

            questionCounter = CreateText("Counter_Question", questionCounterBgRect, "Q 1/5", 34, FontStyles.Bold, TextAlignmentOptions.Center, Primary);
            Stretch(questionCounter.rectTransform, 10, 4, 10, 4);
            primaryTexts.Add(questionCounter);

            RectTransform questionCard = CreateUI("QuestionTextCard", topBar);
            SetRect(questionCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080, 110));
            Image qCardImage = questionCard.gameObject.AddComponent<Image>();
            qCardImage.color = Card;
            questionText = CreateText("Question_Text", questionCard, "Question appears here", 36, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            Stretch(questionText.rectTransform, 28, 12, 28, 12);
            primaryTexts.Add(questionText);

            RectTransform scoreBgRect = CreateUI("ScoreBg_ImageSlot", topBar);
            SetRect(scoreBgRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-122, 0), new Vector2(240, 84));
            scoreBackground = scoreBgRect.gameObject.AddComponent<Image>();
            scoreBackground.color = new Color(0.86f, 0.90f, 1f, 1f);
            scoreBackground.type = Image.Type.Sliced;
            scoreBackground.raycastTarget = false;

            scoreText = CreateText("Score_Text", scoreBgRect, "Score: 0", 32, FontStyles.Bold, TextAlignmentOptions.Center, Primary);
            Stretch(scoreText.rectTransform, 10, 4, 10, 4);
            primaryTexts.Add(scoreText);

            pauseButton = CreateButton("Button_Pause", topBar, "II", Soft, primaryTexts);
            SetRect(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28, 0), new Vector2(72, 72));

            centerArea = CreateUI("CenterArea", parent);
            SetRect(centerArea, new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(900, 680));

            wheelRoot = CreateUI("WheelRoot_Square", centerArea);
            SetRect(wheelRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(590, 590));
            spinner = wheelRoot.gameObject.AddComponent<BehaviourWheelSpinner>();
            spinner.wheelRoot = wheelRoot;
            spinner.instantStop = true;
            spinner.spinSpeed = 180f;
            spinner.contentLayoutMode = BehaviourWheelContentLayoutMode.RadialPrintedOnWheel;
            spinner.showIcons = true;
            spinner.labelRadiusMultiplier = 0.66f;
            spinner.labelRadiusWithoutIconsMultiplier = 0.60f;
            spinner.iconRadiusMultiplier = 0.42f;
            spinner.labelWidthMultiplier = 0.68f;
            spinner.labelHeightMultiplier = 0.15f;
            spinner.iconSizeMultiplier = 0.15f;

            RectTransform wheelGraphicRect = CreateUI("WheelVisualMesh_WheelGraphic", wheelRoot);
            Stretch(wheelGraphicRect);
            BehaviourWheelWheelGraphic wheelGraphic = wheelGraphicRect.gameObject.AddComponent<BehaviourWheelWheelGraphic>();
            spinner.wheelGraphic = wheelGraphic;

            RectTransform contentRoot = CreateUI("SliceContentRoot", wheelRoot);
            Stretch(contentRoot);
            spinner.sliceContentRoot = contentRoot;

            for (int i = 0; i < 6; i++)
            {
                RectTransform sliceRect = CreateUI($"Slice_{i}_Content", contentRoot);
                SetRect(sliceRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 120));
                BehaviourWheelSlice slice = sliceRect.gameObject.AddComponent<BehaviourWheelSlice>();
                slice.contentRoot = sliceRect;
                slice.SetIndex(i);

                RectTransform iconRect = CreateUI("Icon", sliceRect);
                Image icon = iconRect.gameObject.AddComponent<Image>();
                icon.color = new Color(1f, 1f, 1f, 0.38f);
                slice.iconImage = icon;

                TMP_Text label = CreateText("Label", sliceRect, "Option", 25, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
                slice.labelText = label;
                primaryTexts.Add(label);

                spinner.slices.Add(slice);
            }

            RectTransform centerCapHint = CreateUI("CenterCap_Image_Editable", wheelRoot);
            SetRect(centerCapHint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86, 86));
            Image centerCapImage = centerCapHint.gameObject.AddComponent<Image>();
            centerCapImage.color = Color.white;
            centerCapImage.type = Image.Type.Sliced;
            centerCapImage.raycastTarget = false;
            spinner.editableCenterCapImage = centerCapImage;

            RectTransform borderHint = CreateUI("OuterBorder_DrawsInsideWheelGraphic", wheelRoot);
            SetRect(borderHint, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1, 1));

            RectTransform pointerRect = CreateUI("FixedPointerImage", centerArea);
            SetRect(pointerRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -26), new Vector2(92, 120));
            Image pointer = pointerRect.gameObject.AddComponent<Image>();
            pointer.color = new Color(1f, 0.30f, 0.20f, 1f);

            RectTransform bottomBar = CreateUI("BottomBar", parent);
            SetRect(bottomBar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(-160, 170));

            stopButton = CreateButton("Button_STOP", bottomBar, "STOP", new Color(1f, 0.33f, 0.25f, 1f), primaryTexts);
            SetRect(stopButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 82));

            instructionText = CreateText("Body_Instruction", bottomBar, "Tap STOP when the correct behaviour reaches the pointer.", 30, FontStyles.Normal, TextAlignmentOptions.Center, Color.black);
            SetRect(instructionText.rectTransform, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 52));
            secondaryTexts.Add(instructionText);

            feedbackPanel = CreatePanel("FeedbackPanel", parent, new Color(0f, 0f, 0f, 0.38f));
            RectTransform feedbackCard = CreateCard("FeedbackCard", feedbackPanel.transform, new Vector2(720, 220));
            feedbackCardImage = feedbackCard.GetComponent<Image>();
            feedbackText = CreateText("Feedback_Text", feedbackCard, "Correct!", 36, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            Stretch(feedbackText.rectTransform, 35, 20, 35, 20);
            primaryTexts.Add(feedbackText);
        }

        private static BehaviourWheelPausePanel BuildPausePanel(Transform parent, List<TMP_Text> primaryTexts, List<TMP_Text> secondaryTexts)
        {
            RectTransform card = CreateCard("PauseCard", parent, new Vector2(520f, 560f));
            TMP_Text title = CreateText("Title_Pause", card, "Paused", 48, FontStyles.Bold, TextAlignmentOptions.Center, Primary);
            SetRect(title.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440, 70));
            primaryTexts.Add(title);

            BehaviourWheelPausePanel panel = card.gameObject.AddComponent<BehaviourWheelPausePanel>();
            panel.resumeButton = CreateMenuButton(card, "Button_Resume", "RESUME", 0.66f, primaryTexts);
            panel.howToPlayButton = CreateMenuButton(card, "Button_HowToPlay", "HOW TO PLAY", 0.51f, primaryTexts);
            panel.restartRoundButton = CreateMenuButton(card, "Button_RestartRound", "RESTART ROUND", 0.36f, primaryTexts);
            panel.homeButton = CreateMenuButton(card, "Button_Home", "HOME", 0.21f, primaryTexts);
            return panel;
        }

        private static BehaviourWheelResultPanel BuildResultPanel(Transform parent, List<TMP_Text> primaryTexts, List<TMP_Text> secondaryTexts)
        {
            RectTransform card = CreateCard("ResultCard", parent, new Vector2(760f, 670f));
            BehaviourWheelResultPanel panel = card.gameObject.AddComponent<BehaviourWheelResultPanel>();

            panel.titleText = CreateText("Title_Result", card, "Round Complete", 52, FontStyles.Bold, TextAlignmentOptions.Center, Primary);
            SetRect(panel.titleText.rectTransform, new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(650, 70));
            primaryTexts.Add(panel.titleText);

            panel.starRatingText = CreateText("Stars_Result", card, "★★★", 56, FontStyles.Bold, TextAlignmentOptions.Center, Accent);
            SetRect(panel.starRatingText.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 72));
            primaryTexts.Add(panel.starRatingText);

            panel.scoreText = CreateText("Score_Result", card, "Score: 0", 36, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            SetRect(panel.scoreText.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 60));
            primaryTexts.Add(panel.scoreText);

            panel.correctText = CreateText("Correct_Result", card, "Correct: 0", 32, FontStyles.Normal, TextAlignmentOptions.Center, Color.black);
            SetRect(panel.correctText.rectTransform, new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.47f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 52));
            secondaryTexts.Add(panel.correctText);

            panel.wrongText = CreateText("Wrong_Result", card, "Wrong: 0", 32, FontStyles.Normal, TextAlignmentOptions.Center, Color.black);
            SetRect(panel.wrongText.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 52));
            secondaryTexts.Add(panel.wrongText);

            panel.playAgainButton = CreateButton("Button_PlayAgain", card, "PLAY AGAIN", Accent, primaryTexts);
            SetRect(panel.playAgainButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 70));

            panel.continueButton = CreateButton("Button_Continue", card, "CONTINUE", Soft, primaryTexts);
            SetRect(panel.continueButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 70));

            return panel;
        }

        private static Button CreateMenuButton(Transform parent, string name, string label, float yAnchor, List<TMP_Text> primaryTexts)
        {
            Button button = CreateButton(name, parent, label, Accent, primaryTexts);
            SetRect(button.GetComponent<RectTransform>(), new Vector2(0.5f, yAnchor), new Vector2(0.5f, yAnchor), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330, 64));
            return button;
        }

        private static RectTransform CreateCard(string name, Transform parent, Vector2 size)
        {
            RectTransform card = CreateUI(name, parent);
            SetRect(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image image = card.gameObject.AddComponent<Image>();
            image.color = Card;
            return card;
        }

        private static Button CreateButton(string name, Transform parent, string label, Color color, List<TMP_Text> primaryTexts)
        {
            RectTransform rect = CreateUI(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();

            TMP_Text text = CreateText("Button_Label", rect, label, 30, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            Stretch(text.rectTransform, 10, 6, 10, 6);
            primaryTexts.Add(text);
            return button;
        }

        private static Slider CreateSlider(string name, Transform parent)
        {
            RectTransform root = CreateUI(name, parent);
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;

            RectTransform background = CreateUI("Background", root);
            Stretch(background);
            Image bgImage = background.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.75f, 0.78f, 0.86f, 1f);

            RectTransform fillArea = CreateUI("Fill Area", root);
            Stretch(fillArea, 4, 4, 4, 4);

            RectTransform fill = CreateUI("Fill", fillArea);
            Stretch(fill);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = Accent;

            slider.fillRect = fill;
            slider.targetGraphic = fillImage;
            return slider;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateUI(name, parent);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static RectTransform CreateUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0, 0, 0, 0);
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void SetPanelState(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
            group.gameObject.SetActive(visible);
        }
    }
}
#endif
