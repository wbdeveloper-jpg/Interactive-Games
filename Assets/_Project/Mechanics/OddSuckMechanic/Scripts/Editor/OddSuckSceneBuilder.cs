#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OddSuckMechanic.EditorTools
{
    public static class OddSuckSceneBuilder
    {
        private const string DemoSpriteRoot = "Assets/_Project/Mechanics/OddSuckMechanic/DemoSprites";

        [MenuItem("Tools/Odd Suck/Create V5.4 Production Structured Scene")]
        public static void CreateScene()
        {
            EnsureEventSystem();

            GameObject canvasGo = new GameObject("OddSuckCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();

            GameObject managerGo = new GameObject("OddSuckManager", typeof(OddSuckManager), typeof(OddSuckAudioManager));
            managerGo.transform.SetParent(canvasGo.transform, false);
            OddSuckManager manager = managerGo.GetComponent<OddSuckManager>();
            OddSuckAudioManager audioManager = managerGo.GetComponent<OddSuckAudioManager>();
            OddSuckMathQuestionGenerator mathGenerator = managerGo.AddComponent<OddSuckMathQuestionGenerator>();
            OddSuckSpriteCategoryQuestionGenerator spriteGenerator = managerGo.AddComponent<OddSuckSpriteCategoryQuestionGenerator>();
            PopulateDemoSpriteGenerator(spriteGenerator);

            CreatePanel("Background", canvasRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.08f, 0.18f, 0.38f, 1f), false);
            RectTransform skyArea = CreatePanel("SkyArea", canvasRect, new Vector2(0f, 0.36f), new Vector2(1f, 0.88f), Vector2.zero, Vector2.zero, new Color(0.22f, 0.48f, 0.78f, 0.55f), false);
            RectTransform groundArea = CreatePanel("GroundItemArea", canvasRect, new Vector2(0f, 0f), new Vector2(1f, 0.36f), Vector2.zero, Vector2.zero, new Color(0.12f, 0.45f, 0.18f, 0.9f), false);

            Button pullInputButton = CreateInvisibleInputButton("TapAnywherePullInput", canvasRect, new Vector2(0f, 0f), new Vector2(1f, 0.88f));

            RectTransform beamParticleLayer = CreatePlainRect("BeamParticleLayer", skyArea, Vector2.zero, Vector2.one);
            beamParticleLayer.SetAsFirstSibling();
            OddSuckUiParticleEmitter beamParticleEmitter = beamParticleLayer.gameObject.AddComponent<OddSuckUiParticleEmitter>();

            RectTransform ufoRoot = CreatePlainRect("UFORoot", skyArea, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f));
            ufoRoot.sizeDelta = new Vector2(250f, 170f);
            OddSuckUfoAutoMover ufoMover = ufoRoot.gameObject.AddComponent<OddSuckUfoAutoMover>();

            RectTransform beam = CreatePanel("UFOBeam", ufoRoot, new Vector2(0.25f, -1.36f), new Vector2(0.75f, 0.08f), Vector2.zero, Vector2.zero, new Color(0.75f, 1f, 1f, 0.45f), false);
            beam.SetAsFirstSibling();
            CanvasGroup beamGroup = beam.gameObject.AddComponent<CanvasGroup>();

            RectTransform ufoVisual = CreatePlainRect("UFOVisual", ufoRoot, Vector2.zero, Vector2.one);
            Image ufoBodyImage = CreateImage("UFOBody", ufoVisual, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.82f), new Color(0.9f, 0.92f, 1f, 1f));
            ufoBodyImage.raycastTarget = false;

            OddSuckItemView leftTextItemTemplate = CreateItemTemplate(groundArea, "OddSuckTextItemTemplate_Left", OddSuckItemTemplateSide.Left);
            OddSuckItemView centerTextItemTemplate = CreateItemTemplate(groundArea, "OddSuckTextItemTemplate_Center", OddSuckItemTemplateSide.Center);
            OddSuckItemView rightTextItemTemplate = CreateItemTemplate(groundArea, "OddSuckTextItemTemplate_Right", OddSuckItemTemplateSide.Right);
            OddSuckItemView imageItemTemplate = CreateItemTemplate(groundArea, "OddSuckImageItemTemplate", OddSuckItemTemplateSide.ImageMode);
            leftTextItemTemplate.gameObject.SetActive(false);
            centerTextItemTemplate.gameObject.SetActive(false);
            rightTextItemTemplate.gameObject.SetActive(false);
            imageItemTemplate.gameObject.SetActive(false);

            RectTransform topBar = CreatePanel("TopBar", canvasRect, new Vector2(0f, 0.88f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.38f), false);
            TMP_Text questionText = CreateText("QuestionText", topBar, "Pick the odd number", 40, TextAlignmentOptions.Center, new Vector2(0.23f, 0.52f), new Vector2(0.77f, 0.96f), false);
            TMP_Text scoreText = CreateText("ScoreText", topBar, "Score: 0", 26, TextAlignmentOptions.Left, new Vector2(0.03f, 0.62f), new Vector2(0.24f, 0.93f), false);

            RectTransform healthGroup = CreateLayoutPanel("HealthGroup", topBar, new Vector2(0.03f, 0.12f), new Vector2(0.36f, 0.43f), true, 12f, 0, 0, 0, 0);
            TMP_Text healthText = CreateLayoutText("HealthLabel", healthGroup, "Health", 21, TextAlignmentOptions.Left, 92f, -1f, false);
            healthText.fontStyle = FontStyles.Bold;
            RectTransform healthRoot = CreateSliderBar("HealthBar", healthGroup, new Color(0.12f, 0.1f, 0.12f, 0.88f), new Color(1f, 0.86f, 0.18f, 0.95f), new Color(0.2f, 1f, 0.42f, 1f), out Slider healthDamageSlider, out Slider healthSlider, out Image healthDamageFill, out Image healthFill);
            AddLayout(healthRoot.gameObject, -1f, 46f, 1f, 0f);

            RectTransform waveTimerGroup = CreateLayoutPanel("WaveTimerGroup", topBar, new Vector2(0.39f, 0.12f), new Vector2(0.74f, 0.43f), true, 12f, 0, 0, 0, 0);
            TMP_Text waveText = CreateLayoutText("WaveText", waveTimerGroup, "Wave 1", 22, TextAlignmentOptions.Left, 110f, -1f, false);
            waveText.fontStyle = FontStyles.Bold;
            RectTransform timerRoot = CreateSingleSliderBar("TimerBar", waveTimerGroup, new Color(0.07f, 0.08f, 0.12f, 0.88f), new Color(0.35f, 0.82f, 1f, 1f), out Slider timerSlider, out Image timerFill);
            AddLayout(timerRoot.gameObject, -1f, 42f, 1f, 0f);
            TMP_Text timerText = null;

            TMP_Text speedText = CreateText("SpeedText", topBar, "Speed x1.0", 31, TextAlignmentOptions.Center, new Vector2(0.735f, 0.28f), new Vector2(0.885f, 0.78f), false);
            speedText.fontStyle = FontStyles.Bold;
            Button pauseButton = CreatePauseIconButton("PauseButton", topBar, new Vector2(0.902f, 0.22f), new Vector2(0.982f, 0.82f));

            RectTransform promptRoot = CreatePanel("StartPullPrompt", canvasRect, new Vector2(0.12f, 0.46f), new Vector2(0.88f, 0.54f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), false);
            CanvasGroup promptGroup = promptRoot.gameObject.AddComponent<CanvasGroup>();
            TMP_Text startPromptText = CreateText("PromptText", promptRoot, "Click anywhere to start pulling object", 38, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, false);
            startPromptText.fontStyle = FontStyles.Bold;

            OddSuckFeedbackPopup feedbackPopup = CreateFeedbackPopup(canvasRect);
            GameObject loadingPanel = CreateLoadingPanel(canvasRect, out TMP_Text loadingGameNameText, out Slider loadingSlider);
            GameObject howToPanel = CreateHowToPanel(canvasRect, out Button howToPreviousButton, out Button howToNextButton, out Button howToStartButton, out TMP_Text howToText, out Image howToImage, out TMP_Text howToButtonLabel, out TMP_Text howToStepCounterText);
            GameObject pausePanel = CreatePausePanel(canvasRect, out Button resumeButton, out Button pauseHowToButton, out Button pauseRestartButton);
            GameObject resultPanel = CreateResultPanel(canvasRect, out TMP_Text resultTitleText, out TMP_Text resultScoreText, out Button resultRestartButton, out Button resultContinueButton);
            loadingPanel.SetActive(false);
            howToPanel.SetActive(false);
            pausePanel.SetActive(false);
            resultPanel.SetActive(false);

            AssignUfoMover(ufoMover, ufoRoot, ufoVisual, skyArea, ufoBodyImage);
            AssignManager(manager, mathGenerator, spriteGenerator, ufoMover, audioManager, feedbackPopup, ufoRoot, ufoVisual, ufoBodyImage, beam, beamGroup, beamParticleEmitter, groundArea, leftTextItemTemplate, centerTextItemTemplate, rightTextItemTemplate, imageItemTemplate, pullInputButton, questionText, scoreText, healthText, waveText, speedText, timerText, healthRoot, healthSlider, healthDamageSlider, healthFill, healthDamageFill, timerRoot, timerSlider, timerFill, startPromptText, promptGroup, loadingPanel, loadingGameNameText, loadingSlider, howToPanel, howToText, howToImage, howToButtonLabel, howToPreviousButton, howToNextButton, howToStartButton, howToStepCounterText, pausePanel, resultPanel, resultTitleText, resultScoreText, resultContinueButton);

            UnityEventTools.AddPersistentListener(pauseButton.onClick, manager.PauseGame);
            UnityEventTools.AddPersistentListener(resumeButton.onClick, manager.ResumeGame);
            UnityEventTools.AddPersistentListener(pauseHowToButton.onClick, manager.ShowHowToFromPause);
            UnityEventTools.AddPersistentListener(pauseRestartButton.onClick, manager.RestartGame);
            UnityEventTools.AddPersistentListener(howToPreviousButton.onClick, manager.ShowPreviousHowToStep);
            UnityEventTools.AddPersistentListener(howToNextButton.onClick, manager.ShowNextHowToStep);
            UnityEventTools.AddPersistentListener(howToStartButton.onClick, manager.CloseHowToPanel);
            UnityEventTools.AddPersistentListener(resultRestartButton.onClick, manager.RestartGame);

            Selection.activeGameObject = canvasGo;
            EditorGUIUtility.PingObject(canvasGo);
            Debug.Log("Odd Suck V5.4 created with production overlay structure, card/layout groups, square icon pause button, direct item template backgrounds, Bloom flow, and beam energy particles.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static RectTransform CreatePlainRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color, bool raycastTarget)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return rect;
        }

        private static RectTransform CreateLayoutPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, bool horizontal, float spacing, int left, int right, int top, int bottom)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), false);
            if (horizontal)
            {
                HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = spacing;
                layout.padding = new RectOffset(left, right, top, bottom);
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = true;
            }
            else
            {
                VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = spacing;
                layout.padding = new RectOffset(left, right, top, bottom);
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, color, false);
            return rect.GetComponent<Image>();
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax, bool raycastTarget)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = raycastTarget;
            label.enableWordWrapping = true;
            return label;
        }

        private static TMP_Text CreateLayoutText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, float preferredWidth, float preferredHeight, bool raycastTarget)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = raycastTarget;
            label.enableWordWrapping = true;
            AddLayout(go, preferredWidth, preferredHeight, 0f, 0f);
            return label;
        }

        private static void AddLayout(GameObject go, float preferredWidth, float preferredHeight, float flexibleWidth, float flexibleHeight)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = go.AddComponent<LayoutElement>();
            }

            if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
        }

        private static Button CreateInvisibleInputButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.92f), true);
            return ConfigureButton(rect, text, fontSize);
        }

        private static Button CreateLayoutButton(string name, Transform parent, string text, int fontSize, float preferredWidth, float preferredHeight)
        {
            RectTransform rect = CreatePanel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.92f), true);
            AddLayout(rect.gameObject, preferredWidth, preferredHeight, preferredWidth < 0f ? 1f : 0f, 0f);
            return ConfigureButton(rect, text, fontSize);
        }

        private static Button ConfigureButton(RectTransform rect, string text, int fontSize)
        {
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.86f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.65f, 0.78f, 1f, 1f);
            button.colors = colors;

            TMP_Text label = CreateText("Label", rect, text, fontSize, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, false);
            label.color = new Color(0.08f, 0.1f, 0.16f, 1f);
            label.fontStyle = FontStyles.Bold;
            return button;
        }

        private static Button CreatePauseIconButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.92f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.86f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.65f, 0.78f, 1f, 1f);
            button.colors = colors;

            Image icon = CreateImage("PauseIcon", rect, new Vector2(0.26f, 0.22f), new Vector2(0.74f, 0.78f), Color.white);
            icon.sprite = CreatePauseIconSprite();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            return button;
        }

        private static RectTransform CreateSliderBar(string name, Transform parent, Color backgroundColor, Color damageColor, Color fillColor, out Slider damageSlider, out Slider mainSlider, out Image damageFill, out Image mainFill)
        {
            RectTransform root = CreatePanel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, backgroundColor, false);
            Slider damage = CreateSliderLayer("DamageSlider", root, damageColor, out damageFill);
            Slider main = CreateSliderLayer("MainSlider", root, fillColor, out mainFill);
            damageSlider = damage;
            mainSlider = main;
            return root;
        }

        private static RectTransform CreateSingleSliderBar(string name, Transform parent, Color backgroundColor, Color fillColor, out Slider slider, out Image fillImage)
        {
            RectTransform root = CreatePanel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, backgroundColor, false);
            slider = CreateSliderLayer("MainSlider", root, fillColor, out fillImage);
            return root;
        }

        private static Slider CreateSliderLayer(string name, Transform parent, Color fillColor, out Image fillImage)
        {
            RectTransform sliderRoot = CreatePlainRect(name, parent, Vector2.zero, Vector2.one);
            Slider slider = sliderRoot.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;

            RectTransform fillArea = CreatePlainRect("Fill Area", sliderRoot, Vector2.zero, Vector2.one);
            fillImage = CreateImage("Fill", fillArea, Vector2.zero, Vector2.one, fillColor);
            fillImage.raycastTarget = false;
            slider.fillRect = fillImage.rectTransform;
            slider.targetGraphic = null;
            return slider;
        }

        private static OddSuckItemView CreateItemTemplate(RectTransform parent, string templateName, OddSuckItemTemplateSide defaultSide)
        {
            bool imageModeTemplate = defaultSide == OddSuckItemTemplateSide.ImageMode;
            Color templateColor = imageModeTemplate ? new Color(1f, 1f, 1f, 0f) : new Color(1f, 1f, 1f, 0.92f);
            RectTransform itemRect = CreatePanel(templateName, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, templateColor, false);
            itemRect.sizeDelta = imageModeTemplate ? new Vector2(132f, 132f) : new Vector2(150f, 150f);
            CanvasGroup canvasGroup = itemRect.gameObject.AddComponent<CanvasGroup>();
            OddSuckItemView itemView = itemRect.gameObject.AddComponent<OddSuckItemView>();
            Image background = itemRect.GetComponent<Image>();
            background.sprite = imageModeTemplate ? null : CreateDemoTextBoxSprite(defaultSide);

            RectTransform iconRect = CreatePanel("IconImage", itemRect, new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero, Color.clear, false);
            Image icon = iconRect.GetComponent<Image>();
            icon.preserveAspect = true;

            TMP_Text label = CreateText("LabelText", itemRect, "12+3", 32, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, false);
            label.color = new Color(0.08f, 0.1f, 0.16f, 1f);
            label.fontStyle = FontStyles.Bold;

            RectTransform highlightRect = CreatePanel("HighlightImage", itemRect, Vector2.zero, Vector2.one, new Vector2(-10f, -10f), new Vector2(10f, 10f), new Color(1f, 0.95f, 0.2f, 0.35f), false);
            highlightRect.SetAsFirstSibling();
            Image highlight = highlightRect.GetComponent<Image>();
            highlight.gameObject.SetActive(false);

            SerializedObject itemSo = new SerializedObject(itemView);
            SetObject(itemSo, "rectTransform", itemRect);
            SetObject(itemSo, "backgroundImage", background);
            SetObject(itemSo, "iconImage", icon);
            SetObject(itemSo, "highlightImage", highlight);
            SetObject(itemSo, "labelText", label);
            SetObject(itemSo, "canvasGroup", canvasGroup);
            SetBool(itemSo, "hideTextInSpriteMode", true);
            SetFloat(itemSo, "spriteModeBackgroundAlpha", imageModeTemplate ? 0f : 1f);
            itemSo.ApplyModifiedPropertiesWithoutUndo();

            return itemView;
        }

        private static OddSuckFeedbackPopup CreateFeedbackPopup(RectTransform parent)
        {
            RectTransform popup = CreatePanel("FeedbackPopup", parent, new Vector2(0.18f, 0.55f), new Vector2(0.82f, 0.68f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), false);
            CanvasGroup group = popup.gameObject.AddComponent<CanvasGroup>();
            TMP_Text label = CreateText("MessageText", popup, "Correct!", 48, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, false);
            label.fontStyle = FontStyles.Bold;
            OddSuckFeedbackPopup feedback = popup.gameObject.AddComponent<OddSuckFeedbackPopup>();

            SerializedObject so = new SerializedObject(feedback);
            SetObject(so, "popupRoot", popup);
            SetObject(so, "canvasGroup", group);
            SetObject(so, "messageText", label);
            so.ApplyModifiedPropertiesWithoutUndo();
            return feedback;
        }

        private static GameObject CreateLoadingPanel(RectTransform parent, out TMP_Text gameNameText, out Slider loadingSlider)
        {
            RectTransform root = CreateOverlayRoot("LoadingPanel", parent);
            RectTransform card = CreateOverlayCard("PanelCard", root, new Vector2(0.13f, 0.33f), new Vector2(0.87f, 0.67f), 26f, 44, 44, 44, 44);

            RectTransform header = CreateLayoutPanel("Header", card, Vector2.zero, Vector2.one, false, 0f, 0, 0, 0, 0);
            AddLayout(header.gameObject, -1f, 128f, 1f, 0f);
            gameNameText = CreateLayoutText("GameNameText", header, "ODD SUCK", 78, TextAlignmentOptions.Center, -1f, -1f, false);
            gameNameText.fontStyle = FontStyles.Bold;

            RectTransform body = CreateLayoutPanel("Body", card, Vector2.zero, Vector2.one, false, 18f, 20, 20, 0, 0);
            AddLayout(body.gameObject, -1f, -1f, 1f, 1f);
            RectTransform sliderRoot = CreateSingleSliderBar("LoadingSlider", body, new Color(1f, 1f, 1f, 0.16f), new Color(0.4f, 0.95f, 1f, 1f), out loadingSlider, out _);
            AddLayout(sliderRoot.gameObject, -1f, 42f, 1f, 0f);
            TMP_Text loadingText = CreateLayoutText("LoadingText", body, "Loading...", 30, TextAlignmentOptions.Center, -1f, 48f, false);
            loadingText.fontStyle = FontStyles.Bold;
            return root.gameObject;
        }

        private static GameObject CreateHowToPanel(RectTransform parent, out Button previousButton, out Button nextButton, out Button startButton, out TMP_Text howToText, out Image howToImage, out TMP_Text buttonLabel, out TMP_Text stepCounterText)
        {
            RectTransform root = CreateOverlayRoot("HowToPlayPanel", parent);
            RectTransform card = CreateOverlayCard("PanelCard", root, new Vector2(0.07f, 0.15f), new Vector2(0.93f, 0.83f), 20f, 38, 38, 34, 34);

            RectTransform header = CreateLayoutPanel("Header", card, Vector2.zero, Vector2.one, false, 0f, 0, 0, 0, 0);
            AddLayout(header.gameObject, -1f, 92f, 1f, 0f);
            TMP_Text title = CreateLayoutText("Title", header, "HOW TO PLAY", 50, TextAlignmentOptions.Center, -1f, -1f, false);
            title.fontStyle = FontStyles.Bold;

            RectTransform body = CreatePanel("Body", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.04f), false);
            AddLayout(body.gameObject, -1f, -1f, 1f, 1f);
            howToImage = CreateImage("HowToImage", body, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Color.clear);
            howToImage.preserveAspect = true;
            howToImage.gameObject.SetActive(false);
            howToText = CreateText("HowToText", body, "The UFO moves automatically.\n\nTap anywhere when the UFO light is above the odd item.\n\nWrong item or timeout reduces health.\n\nEach wave has less time, but never below 15 seconds.", 32, TextAlignmentOptions.Center, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), false);
            howToText.fontStyle = FontStyles.Bold;

            RectTransform footer = CreateLayoutPanel("Footer", card, Vector2.zero, Vector2.one, true, 16f, 0, 0, 0, 0);
            AddLayout(footer.gameObject, -1f, 88f, 1f, 0f);
            previousButton = CreateLayoutButton("PreviousButton", footer, "PREV", 26, 180f, 74f);
            stepCounterText = CreateLayoutText("StepCounterText", footer, "1 / 1", 24, TextAlignmentOptions.Center, 120f, 74f, false);
            stepCounterText.fontStyle = FontStyles.Bold;
            nextButton = CreateLayoutButton("NextButton", footer, "NEXT", 26, 180f, 74f);
            startButton = CreateLayoutButton("StartButton", footer, "START", 30, 220f, 74f);
            buttonLabel = startButton.GetComponentInChildren<TMP_Text>();
            return root.gameObject;
        }

        private static GameObject CreatePausePanel(RectTransform parent, out Button resumeButton, out Button howToButton, out Button restartButton)
        {
            RectTransform root = CreateOverlayRoot("PausePanel", parent);
            RectTransform card = CreateOverlayCard("PanelCard", root, new Vector2(0.15f, 0.3f), new Vector2(0.85f, 0.72f), 24f, 42, 42, 34, 34);

            RectTransform header = CreateLayoutPanel("Header", card, Vector2.zero, Vector2.one, false, 0f, 0, 0, 0, 0);
            AddLayout(header.gameObject, -1f, 100f, 1f, 0f);
            TMP_Text title = CreateLayoutText("Title", header, "PAUSED", 52, TextAlignmentOptions.Center, -1f, -1f, false);
            title.fontStyle = FontStyles.Bold;

            RectTransform body = CreateLayoutPanel("Body", card, Vector2.zero, Vector2.one, false, 18f, 80, 80, 0, 0);
            AddLayout(body.gameObject, -1f, -1f, 1f, 1f);
            resumeButton = CreateLayoutButton("ResumeButton", body, "RESUME", 30, -1f, 78f);
            howToButton = CreateLayoutButton("HowToButton", body, "HOW TO PLAY", 30, -1f, 78f);
            restartButton = CreateLayoutButton("RestartButton", body, "RESTART", 30, -1f, 78f);
            return root.gameObject;
        }

        private static GameObject CreateResultPanel(RectTransform parent, out TMP_Text titleText, out TMP_Text scoreText, out Button restartButton, out Button continueButton)
        {
            RectTransform root = CreateOverlayRoot("ResultPanel", parent);
            RectTransform card = CreateOverlayCard("PanelCard", root, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.76f), 22f, 42, 42, 34, 34);

            RectTransform header = CreateLayoutPanel("Header", card, Vector2.zero, Vector2.one, false, 0f, 0, 0, 0, 0);
            AddLayout(header.gameObject, -1f, 110f, 1f, 0f);
            titleText = CreateLayoutText("ResultTitleText", header, "Game Over", 54, TextAlignmentOptions.Center, -1f, -1f, false);
            titleText.fontStyle = FontStyles.Bold;

            RectTransform body = CreateLayoutPanel("Body", card, Vector2.zero, Vector2.one, false, 12f, 30, 30, 0, 0);
            AddLayout(body.gameObject, -1f, -1f, 1f, 1f);
            scoreText = CreateLayoutText("ResultScoreText", body, "Score: 0", 36, TextAlignmentOptions.Center, -1f, -1f, false);

            RectTransform footer = CreateLayoutPanel("Footer", card, Vector2.zero, Vector2.one, false, 14f, 90, 90, 0, 0);
            AddLayout(footer.gameObject, -1f, 170f, 1f, 0f);
            restartButton = CreateLayoutButton("PlayAgainButton", footer, "PLAY AGAIN", 30, -1f, 72f);
            continueButton = CreateLayoutButton("ContinueButton", footer, "CONTINUE", 28, -1f, 72f);
            return root.gameObject;
        }

        private static RectTransform CreateOverlayRoot(string name, Transform parent)
        {
            RectTransform root = CreatePanel(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f), true);
            Image rootImage = root.GetComponent<Image>();
            rootImage.raycastTarget = true;
            RectTransform dim = CreatePanel("OverlayDim", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.62f), false);
            dim.SetAsFirstSibling();
            return root;
        }

        private static RectTransform CreateOverlayCard(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float spacing, int left, int right, int top, int bottom)
        {
            RectTransform card = CreatePanel(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, new Color(0.04f, 0.06f, 0.12f, 0.98f), false);
            VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return card;
        }

        private static void AssignUfoMover(OddSuckUfoAutoMover mover, RectTransform ufoRoot, RectTransform ufoVisual, RectTransform skyArea, Image ufoSpriteImage)
        {
            SerializedObject so = new SerializedObject(mover);
            SetObject(so, "ufoRoot", ufoRoot);
            SetObject(so, "ufoVisualTransform", ufoVisual);
            SetObject(so, "moveBounds", skyArea);
            SetObject(so, "ufoSpriteImage", ufoSpriteImage);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PopulateDemoSpriteGenerator(OddSuckSpriteCategoryQuestionGenerator generator)
        {
            if (generator == null)
            {
                return;
            }

            Sprite[] fruitSprites = CreateDemoSpriteSet("Fruit", new Color(1f, 0.35f, 0.2f, 1f), DemoSpriteShape.Circle, 5);
            Sprite[] animalSprites = CreateDemoSpriteSet("Animal", new Color(0.35f, 0.75f, 1f, 1f), DemoSpriteShape.RoundedSquare, 5);
            Sprite[] vehicleSprites = CreateDemoSpriteSet("Vehicle", new Color(1f, 0.82f, 0.22f, 1f), DemoSpriteShape.Triangle, 5);

            SerializedObject so = new SerializedObject(generator);
            SerializedProperty questionText = so.FindProperty("questionText");
            if (questionText != null)
            {
                questionText.stringValue = "Pick the odd picture";
            }

            SerializedProperty repeat = so.FindProperty("allowRepeatedSpritesInSameWave");
            if (repeat != null)
            {
                repeat.boolValue = true;
            }

            SerializedProperty categories = so.FindProperty("categories");
            if (categories != null)
            {
                categories.ClearArray();
                AddSpriteCategory(categories, 0, "Fruits Demo", fruitSprites);
                AddSpriteCategory(categories, 1, "Animals Demo", animalSprites);
                AddSpriteCategory(categories, 2, "Vehicles Demo", vehicleSprites);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSpriteCategory(SerializedProperty categories, int index, string categoryName, Sprite[] sprites)
        {
            categories.InsertArrayElementAtIndex(index);
            SerializedProperty category = categories.GetArrayElementAtIndex(index);
            category.FindPropertyRelative("categoryName").stringValue = categoryName;
            SerializedProperty spriteList = category.FindPropertyRelative("sprites");
            spriteList.ClearArray();

            for (int i = 0; i < sprites.Length; i++)
            {
                spriteList.InsertArrayElementAtIndex(i);
                spriteList.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        private enum DemoSpriteShape
        {
            Circle,
            RoundedSquare,
            Triangle
        }

        private static Sprite[] CreateDemoSpriteSet(string categoryName, Color color, DemoSpriteShape shape, int count)
        {
            EnsureAssetFolder("Assets", "_Project");
            EnsureAssetFolder("Assets/_Project", "Mechanics");
            EnsureAssetFolder("Assets/_Project/Mechanics", "OddSuckMechanic");
            EnsureAssetFolder("Assets/_Project/Mechanics/OddSuckMechanic", "DemoSprites");

            Sprite[] sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                Color finalColor = Color.Lerp(color, Color.white, i * 0.08f);
                string path = $"{DemoSpriteRoot}/{categoryName}_{i + 1}.png";
                CreateDemoSpritePng(path, finalColor, shape, i);
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return sprites;
        }

        private static void EnsureAssetFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CreateDemoSpritePng(string path, Color color, DemoSpriteShape shape, int variant)
        {
            const int size = 96;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = 34f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = IsInsideDemoShape(x, y, center, radius, shape);
                    texture.SetPixel(x, y, inside ? color : transparent);
                }
            }

            Color markerColor = Color.white;
            int markerSize = 8 + variant * 2;
            int markerX = 14 + variant * 10;
            int markerY = 12;
            for (int y = markerY; y < markerY + markerSize && y < size; y++)
            {
                for (int x = markerX; x < markerX + markerSize && x < size; x++)
                {
                    texture.SetPixel(x, y, markerColor);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            ImportAsSprite(path);
        }

        private static bool IsInsideDemoShape(int x, int y, Vector2 center, float radius, DemoSpriteShape shape)
        {
            switch (shape)
            {
                case DemoSpriteShape.RoundedSquare:
                    return Mathf.Abs(x - center.x) <= radius && Mathf.Abs(y - center.y) <= radius;
                case DemoSpriteShape.Triangle:
                    float height = radius * 1.7f;
                    float normalizedY = (y - (center.y - height * 0.5f)) / height;
                    if (normalizedY < 0f || normalizedY > 1f)
                    {
                        return false;
                    }
                    float halfWidth = radius * normalizedY;
                    return Mathf.Abs(x - center.x) <= halfWidth;
                default:
                    return Vector2.Distance(new Vector2(x, y), center) <= radius;
            }
        }

        private static Sprite CreateDemoTextBoxSprite(OddSuckItemTemplateSide side)
        {
            EnsureAssetFolder("Assets", "_Project");
            EnsureAssetFolder("Assets/_Project", "Mechanics");
            EnsureAssetFolder("Assets/_Project/Mechanics", "OddSuckMechanic");
            EnsureAssetFolder("Assets/_Project/Mechanics/OddSuckMechanic", "DemoSprites");
            EnsureAssetFolder(DemoSpriteRoot, "Templates");

            string spriteName = side == OddSuckItemTemplateSide.Left ? "TextBox_Left" : side == OddSuckItemTemplateSide.Right ? "TextBox_Right" : "TextBox_Center";
            string path = $"{DemoSpriteRoot}/Templates/{spriteName}.png";
            Color color = side == OddSuckItemTemplateSide.Center ? new Color(1f, 0.96f, 0.82f, 1f) : new Color(1f, 0.94f, 0.78f, 1f);
            CreateDemoTextBoxPng(path, color, side);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite CreatePauseIconSprite()
        {
            EnsureAssetFolder("Assets", "_Project");
            EnsureAssetFolder("Assets/_Project", "Mechanics");
            EnsureAssetFolder("Assets/_Project/Mechanics", "OddSuckMechanic");
            EnsureAssetFolder("Assets/_Project/Mechanics/OddSuckMechanic", "DemoSprites");
            EnsureAssetFolder(DemoSpriteRoot, "UI");
            string path = $"{DemoSpriteRoot}/UI/PauseIcon.png";

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color iconColor = new Color(0.08f, 0.1f, 0.16f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool left = x >= 18 && x <= 27 && y >= 12 && y <= 52;
                    bool right = x >= 37 && x <= 46 && y >= 12 && y <= 52;
                    texture.SetPixel(x, y, left || right ? iconColor : transparent);
                }
            }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            ImportAsSprite(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void CreateDemoTextBoxPng(string path, Color color, OddSuckItemTemplateSide side)
        {
            const int width = 160;
            const int height = 120;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color border = new Color(0.18f, 0.13f, 0.08f, color.a);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            Vector2[] points = GetTemplatePolygon(width, height, side);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsPointInPolygon(new Vector2(x, y), points))
                    {
                        continue;
                    }

                    bool nearBorder = x < 7 || x > width - 8 || y < 7 || y > height - 8;
                    texture.SetPixel(x, y, nearBorder ? border : color);
                }
            }

            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            ImportAsSprite(path);
        }

        private static Vector2[] GetTemplatePolygon(int width, int height, OddSuckItemTemplateSide side)
        {
            switch (side)
            {
                case OddSuckItemTemplateSide.Left:
                    return new[] { new Vector2(20f, 8f), new Vector2(width - 8f, 18f), new Vector2(width - 22f, height - 10f), new Vector2(6f, height - 22f) };
                case OddSuckItemTemplateSide.Right:
                    return new[] { new Vector2(8f, 18f), new Vector2(width - 20f, 8f), new Vector2(width - 6f, height - 22f), new Vector2(22f, height - 10f) };
                default:
                    return new[] { new Vector2(12f, 10f), new Vector2(width - 12f, 10f), new Vector2(width - 12f, height - 10f), new Vector2(12f, height - 10f) };
            }
        }

        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                bool intersects = ((polygon[i].y > point.y) != (polygon[j].y > point.y)) &&
                    (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / Mathf.Max(0.0001f, polygon[j].y - polygon[i].y) + polygon[i].x);

                if (intersects)
                {
                    inside = !inside;
                }

                j = i;
            }

            return inside;
        }

        private static void ImportAsSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void AssignManager(OddSuckManager manager, OddSuckMathQuestionGenerator mathGenerator, OddSuckSpriteCategoryQuestionGenerator spriteGenerator, OddSuckUfoAutoMover ufoMover, OddSuckAudioManager audioManager, OddSuckFeedbackPopup feedbackPopup, RectTransform ufoRoot, RectTransform ufoVisual, Image ufoBodyImage, RectTransform beam, CanvasGroup beamGroup, OddSuckUiParticleEmitter beamParticleEmitter, RectTransform groundArea, OddSuckItemView leftTextItemTemplate, OddSuckItemView centerTextItemTemplate, OddSuckItemView rightTextItemTemplate, OddSuckItemView imageItemTemplate, Button pullInputButton, TMP_Text questionText, TMP_Text scoreText, TMP_Text healthText, TMP_Text waveText, TMP_Text speedText, TMP_Text timerText, RectTransform healthRoot, Slider healthSlider, Slider healthDamageSlider, Image healthFill, Image healthDamageFill, RectTransform timerRoot, Slider timerSlider, Image timerFill, TMP_Text startPromptText, CanvasGroup promptGroup, GameObject loadingPanel, TMP_Text loadingGameNameText, Slider loadingSlider, GameObject howToPanel, TMP_Text howToText, Image howToImage, TMP_Text howToButtonLabel, Button howToPreviousButton, Button howToNextButton, Button howToStartButton, TMP_Text howToStepCounterText, GameObject pausePanel, GameObject resultPanel, TMP_Text resultTitleText, TMP_Text resultScoreText, Button resultContinueButton)
        {
            SerializedObject so = new SerializedObject(manager);
            SetObject(so, "ufoMover", ufoMover);
            SetObject(so, "audioManager", audioManager);
            SetObject(so, "feedbackPopup", feedbackPopup);
            SetObject(so, "ufoMoveTransform", ufoRoot);
            SetObject(so, "ufoVisualTransform", ufoVisual);
            SetObject(so, "ufoBodyImage", ufoBodyImage);
            SetObject(so, "beamTransform", beam);
            SetObject(so, "beamCanvasGroup", beamGroup);
            SetObject(so, "beamParticleEmitter", beamParticleEmitter);
            SetObject(so, "itemParent", groundArea);
            SetObject(so, "itemTemplate", centerTextItemTemplate);
            SetObject(so, "leftTextItemTemplate", leftTextItemTemplate);
            SetObject(so, "centerTextItemTemplate", centerTextItemTemplate);
            SetObject(so, "rightTextItemTemplate", rightTextItemTemplate);
            SetObject(so, "imageItemTemplate", imageItemTemplate);
            SetObject(so, "pullInputButton", pullInputButton);
            SetObject(so, "questionText", questionText);
            SetObject(so, "scoreText", scoreText);
            SetObject(so, "healthText", healthText);
            SetObject(so, "waveText", waveText);
            SetObject(so, "speedText", speedText);
            SetObject(so, "timerText", timerText);
            SetObject(so, "healthBarRoot", healthRoot);
            SetObject(so, "healthSlider", healthSlider);
            SetObject(so, "healthDamageSlider", healthDamageSlider);
            SetObject(so, "healthFillImage", healthFill);
            SetObject(so, "healthDamageFillImage", healthDamageFill);
            SetObject(so, "timerBarRoot", timerRoot);
            SetObject(so, "timerSlider", timerSlider);
            SetObject(so, "timerFillImage", timerFill);
            SetObject(so, "startPromptText", startPromptText);
            SetObject(so, "startPromptCanvasGroup", promptGroup);
            SetObject(so, "loadingPanel", loadingPanel);
            SetObject(so, "loadingGameNameText", loadingGameNameText);
            SetObject(so, "loadingSlider", loadingSlider);
            SetObject(so, "howToPlayPanel", howToPanel);
            SetObject(so, "howToText", howToText);
            SetObject(so, "howToImage", howToImage);
            SetObject(so, "howToButtonLabelText", howToButtonLabel);
            SetObject(so, "howToPreviousButton", howToPreviousButton);
            SetObject(so, "howToNextButton", howToNextButton);
            SetObject(so, "howToStartButton", howToStartButton);
            SetObject(so, "howToStepCounterText", howToStepCounterText);
            SetObject(so, "pausePanel", pausePanel);
            SetObject(so, "resultPanel", resultPanel);
            SetObject(so, "resultTitleText", resultTitleText);
            SetObject(so, "resultScoreText", resultScoreText);
            SetObject(so, "resultContinueButton", resultContinueButton);
            SetBool(so, "showHowToOnStart", true);
            SetBool(so, "showLoadingPanel", true);
            SetBool(so, "useBloomRewardSystem", true);
            SetBool(so, "useWaveTimer", true);
            SetEnum(so, "playMode", (int)OddSuckPlayMode.SpriteOnly);

            SerializedProperty secondaryTexts = so.FindProperty("secondaryFontTargets");
            if (secondaryTexts != null)
            {
                secondaryTexts.ClearArray();
                Object[] targets = { scoreText, healthText, waveText, speedText, startPromptText, loadingGameNameText, howToText, howToButtonLabel, howToStepCounterText };
                int added = 0;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i] == null)
                    {
                        continue;
                    }

                    secondaryTexts.InsertArrayElementAtIndex(added);
                    secondaryTexts.GetArrayElementAtIndex(added).objectReferenceValue = targets[i];
                    added++;
                }
            }

            SerializedProperty generators = so.FindProperty("questionGenerators");
            generators.ClearArray();
            generators.InsertArrayElementAtIndex(0);
            generators.GetArrayElementAtIndex(0).objectReferenceValue = mathGenerator;
            generators.InsertArrayElementAtIndex(1);
            generators.GetArrayElementAtIndex(1).objectReferenceValue = spriteGenerator;

            so.ApplyModifiedPropertiesWithoutUndo();

            if (beamParticleEmitter != null)
            {
                SerializedObject particleSo = new SerializedObject(beamParticleEmitter);
                SetObject(particleSo, "particleRoot", beamParticleEmitter.transform as RectTransform);
                SetObject(particleSo, "beamTarget", beam);
                particleSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetEnum(SerializedObject so, string propertyName, int enumIndex)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = enumIndex;
            }
        }
    }
}
#endif
