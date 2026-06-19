using System.Collections.Generic;
using TagBasketSorter;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TagBasketSorter.EditorTools
{
    public static class TagBasketSortSceneBuilder
    {
        private static readonly string[] DefaultTags = { "Common Noun", "Collective Noun" };
        private static readonly string[] CommonWords = { "Boy", "Girl", "Dog", "Tree", "Chair", "Book", "River", "Apple" };
        private static readonly string[] CollectiveWords = { "Team", "Herd", "Flock", "Bunch", "Class", "Fleet", "Pack", "Swarm" };

        [MenuItem("Tools/Tag Basket Sorter/Create Rough 5-Level UI")]
        public static void CreateRoughFiveLevelUi()
        {
            EnsureEventSystem();

            GameObject canvasObject = new GameObject("TagBasketSorter_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Tag Basket Sorter UI");

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            Stretch(canvasRect);

            TagBasketSortGameManager manager = canvasObject.AddComponent<TagBasketSortGameManager>();
            manager.rootCanvas = canvas;
            manager.useTimer = true;
            manager.secondsPerLevel = 60f;
            manager.pointsPerCorrectDrop = 10;
            manager.wrongDropPenalty = 2;
            manager.progressPrefsKey = "TagBasketSorter_DemoProgress";
            manager.showHowToPlayOnStart = true;
            manager.showTutorialOnFirstPlayableLevel = true;
            manager.tutorialMessage = "Drag an object into the matching basket.";
            manager.tutorialBreathDuration = 0.85f;
            manager.tutorialBreathScale = 1.06f;
            manager.hintPulseDuration = 0.45f;
            manager.hintPulseLoopCount = 4;
            manager.useBloomRewardSystem = true;
            manager.showBloomPreGameBeforeLanding = true;

            AudioSource sfx = canvasObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            manager.sfxAudioSource = sfx;

            AudioSource bgm = canvasObject.AddComponent<AudioSource>();
            bgm.playOnAwake = false;
            bgm.loop = true;
            manager.bgmAudioSource = bgm;

            GameObject landingPage = CreatePanel("LandingPage_LevelSelect", canvasRect, new Color(0.11f, 0.13f, 0.18f, 1f));
            manager.landingPage = landingPage;
            CreateText("TitleText", landingPage.transform, "Tag Basket Sorter", 56, TextAlignmentOptions.Center, new Vector2(0.5f, 0.88f), new Vector2(900f, 90f));
            CreateText("SubtitleText", landingPage.transform, "Complete one level to unlock the next.", 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0.79f), new Vector2(900f, 60f));

            RectTransform levelButtonRoot = CreateRect("LevelButtonHolder", landingPage.transform);
            Anchor(levelButtonRoot, new Vector2(0.5f, 0.47f), new Vector2(980f, 470f));
            manager.levelButtonContainer = levelButtonRoot;
            manager.levelButtons = new List<TagBasketLevelButton>();
            for (int i = 0; i < 5; i++)
            {
                TagBasketLevelButton levelButton = CreateLevelButton(levelButtonRoot, i);
                manager.levelButtons.Add(levelButton);
                if (i == 0)
                    manager.levelButtonTemplate = levelButton;
            }

            GameObject gameplayPage = CreatePanel("GameplayPage", canvasRect, new Color(0.08f, 0.09f, 0.12f, 1f));
            manager.gameplayPage = gameplayPage;

            RectTransform topBar = CreateRect("TopBar", gameplayPage.transform);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.offsetMin = new Vector2(0f, -92f);
            topBar.offsetMax = new Vector2(0f, 0f);
            Image topBarImage = topBar.gameObject.AddComponent<Image>();
            topBarImage.color = new Color(0f, 0f, 0f, 0.45f);

            manager.scoreText = CreateText("ScoreText", topBar, "Score: 0", 34, TextAlignmentOptions.Left, new Vector2(0.125f, 0.5f), new Vector2(340f, 70f));
            manager.progressText = CreateText("ProgressText", topBar, "0/0", 32, TextAlignmentOptions.Center, new Vector2(0.305f, 0.5f), new Vector2(150f, 70f));
            CreateTimerSlider(topBar, manager);
            CreateHintContainer(topBar, manager);
            manager.showHintTextOverlay = false;
            manager.pauseButton = CreateButton("PauseButton", topBar, "||", new Vector2(0.955f, 0.5f), new Vector2(68f, 68f));

            RectTransform levelRoot = CreateRect("LevelPanelsRoot", gameplayPage.transform);
            Stretch(levelRoot, 0f, 0f, 0f, 92f);
            manager.levelPanelsRoot = levelRoot;

            RectTransform dragLayer = CreateRect("DragLayer", gameplayPage.transform);
            Stretch(dragLayer);
            dragLayer.SetAsLastSibling();
            manager.dragLayer = dragLayer;

            manager.levels = new List<TagBasketLevelPanel>();
            for (int i = 0; i < 5; i++)
            {
                TagBasketLevelPanel level = CreateLevelPanel(levelRoot, i);
                manager.levels.Add(level);
            }

            CreateTutorialOverlay(gameplayPage.transform, manager);
            CreateHintOverlay(canvasRect, manager);
            CreateFeedbackPopup(canvasRect, manager);
            CreateScoreDeltaPopup(canvasRect, manager);
            CreatePausePanel(canvasRect, manager);
            CreateResultPanel(canvasRect, manager);
            CreateHowToPlayPanel(canvasRect, manager);

            gameplayPage.SetActive(false);
            if (manager.pausePanel != null) manager.pausePanel.SetActive(false);
            if (manager.resultPanel != null) manager.resultPanel.SetActive(false);
            if (manager.howToPlayPanel != null) manager.howToPlayPanel.SetActive(false);
            if (manager.feedbackPopup != null) manager.feedbackPopup.gameObject.SetActive(false);
            if (manager.scoreDeltaPopup != null) manager.scoreDeltaPopup.gameObject.SetActive(false);
            if (manager.hintOverlay != null) manager.hintOverlay.gameObject.SetActive(false);
            if (manager.tutorialOverlay != null) manager.tutorialOverlay.gameObject.SetActive(false);

            Selection.activeGameObject = canvasObject;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Tag Basket Sorter v12 UI created. Tutorial card breathing, longer hint pulse, timer slider, hint counter, basket title badges and Bloom flow are included.");
        }

        [MenuItem("Tools/Tag Basket Sorter/Refresh Selected Manager Level Buttons")]
        public static void RefreshSelectedManagerButtons()
        {
            TagBasketSortGameManager manager = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<TagBasketSortGameManager>()
                : null;

            if (manager == null)
            {
                Debug.LogWarning("Select TagBasketSorter_Canvas or any child under it, then run this menu.");
                return;
            }

            Undo.RecordObject(manager, "Refresh Tag Basket Level Buttons");
            manager.RefreshLevelsAndButtonsManual();
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("Tag Basket Sorter level buttons refreshed in Edit Mode.");
        }

        private static TagBasketLevelPanel CreateLevelPanel(RectTransform levelRoot, int levelIndex)
        {
            GameObject panel = CreatePanel($"Level_{levelIndex + 1}_Panel", levelRoot, new Color(0.16f + levelIndex * 0.025f, 0.19f, 0.24f, 1f));
            TagBasketLevelPanel level = panel.AddComponent<TagBasketLevelPanel>();
            level.levelTitle = $"Level {levelIndex + 1}";
            level.maxHintsAllowed = 3;
            level.useBasketOrganicPlacement = true;

            Image background = panel.GetComponent<Image>();
            background.raycastTarget = false;
            level.backgroundImage = background;

            RectTransform objectsHolder = CreateRect("ObjectsHolder_ManualPositions", panel.transform);
            Stretch(objectsHolder, 520f, 520f, 330f, 150f);
            level.objectsHolder = objectsHolder;

            RectTransform basketsHolder = CreateRect("BasketsHolder", panel.transform);
            basketsHolder.anchorMin = new Vector2(0f, 0f);
            basketsHolder.anchorMax = new Vector2(1f, 0f);
            basketsHolder.pivot = new Vector2(0.5f, 0f);
            basketsHolder.offsetMin = new Vector2(160f, 65f);
            basketsHolder.offsetMax = new Vector2(-160f, 330f);
            level.basketsHolder = basketsHolder;

            level.dropZones = new List<TagBasketDropZone>();
            level.dropZones.Add(CreateBasket(basketsHolder, DefaultTags[0], new Vector2(0.28f, 0.5f)));
            level.dropZones.Add(CreateBasket(basketsHolder, DefaultTags[1], new Vector2(0.72f, 0.5f)));

            level.draggableItems = new List<TagBasketDraggableItem>();
            for (int i = 0; i < 6; i++)
            {
                bool common = i % 2 == 0;
                string tag = common ? DefaultTags[0] : DefaultTags[1];
                string label = common ? CommonWords[(levelIndex + i) % CommonWords.Length] : CollectiveWords[(levelIndex + i) % CollectiveWords.Length];
                Vector2 position = GetObjectPosition(i);
                level.draggableItems.Add(CreateDraggableItem(objectsHolder, label, tag, position, i));
            }

            panel.SetActive(false);
            return level;
        }

        private static TagBasketDropZone CreateBasket(Transform parent, string tag, Vector2 normalizedAnchor)
        {
            RectTransform basket = CreateRect($"Basket_{tag.Replace(" ", "_")}", parent);
            Anchor(basket, normalizedAnchor, new Vector2(500f, 240f));
            Image image = basket.gameObject.AddComponent<Image>();
            image.color = new Color(0.95f, 0.72f, 0.32f, 0.28f);
            image.raycastTarget = true;

            TagBasketDropZone dropZone = basket.gameObject.AddComponent<TagBasketDropZone>();
            dropZone.acceptedTag = tag;
            dropZone.basketImage = image;
            dropZone.maxItemsPerRow = 3;
            dropZone.placementCellSize = new Vector2(76f, 76f);
            dropZone.placementSpacing = new Vector2(10f, 8f);
            dropZone.placementStartOffset = new Vector2(0f, 34f);
            dropZone.placedItemPositionJitter = new Vector2(8f, 6f);
            dropZone.placedItemRotationRange = 7f;

            RectTransform titleBackground = CreateRect("BasketTitleBackground", basket);
            Anchor(titleBackground, new Vector2(0.5f, 0.91f), new Vector2(380f, 56f));
            Image titleBackgroundImage = titleBackground.gameObject.AddComponent<Image>();
            titleBackgroundImage.color = new Color(1f, 0.92f, 0.68f, 0.94f);
            titleBackgroundImage.raycastTarget = false;
            dropZone.titleBackgroundImage = titleBackgroundImage;

            TMP_Text title = CreateText("BasketTitle", titleBackground, tag, 30, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(348f, 46f));
            title.color = new Color(0.22f, 0.11f, 0.03f, 1f);
            title.fontStyle = FontStyles.Bold;
            dropZone.titleText = title;

            RectTransform placementRoot = CreateRect("PlacedItemsRoot", basket);
            Stretch(placementRoot, 26f, 26f, 32f, 70f);
            dropZone.placementRoot = placementRoot;

            RectTransform front = CreateRect("BasketFrontOverlay", basket);
            front.anchorMin = new Vector2(0f, 0f);
            front.anchorMax = new Vector2(1f, 0f);
            front.pivot = new Vector2(0.5f, 0f);
            front.offsetMin = new Vector2(0f, 0f);
            front.offsetMax = new Vector2(0f, 76f);
            Image frontImage = front.gameObject.AddComponent<Image>();
            frontImage.color = new Color(0.35f, 0.19f, 0.05f, 0.55f);
            frontImage.raycastTarget = false;
            dropZone.basketFrontOverlay = frontImage;
            placementRoot.SetAsLastSibling();
            front.SetAsLastSibling();
            titleBackground.SetAsLastSibling();

            return dropZone;
        }

        private static TagBasketDraggableItem CreateDraggableItem(Transform parent, string label, string tag, Vector2 anchoredPosition, int index)
        {
            RectTransform item = CreateRect($"Item_{label}", parent);
            Anchor(item, new Vector2(0.5f, 0.5f), new Vector2(130f, 130f));
            item.anchoredPosition = anchoredPosition;
            item.localRotation = Quaternion.identity;

            Image image = item.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.92f);
            image.raycastTarget = true;

            CanvasGroup canvasGroup = item.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;

            TagBasketDraggableItem draggable = item.gameObject.AddComponent<TagBasketDraggableItem>();
            draggable.itemTag = tag;
            draggable.itemId = label;
            draggable.iconImage = image;
            draggable.visualMode = TagBasketItemVisualMode.ImageAndLabel;
            draggable.labelColor = Color.red;

            TMP_Text text = CreateText("Label", item, label, 26, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(118f, 70f));
            text.color = Color.red;
            draggable.labelText = text;

            return draggable;
        }

        private static TagBasketLevelButton CreateLevelButton(Transform parent, int index)
        {
            RectTransform root = CreateRect($"LevelButton_{index + 1}", parent);
            int row = index / 3;
            int column = index % 3;
            int itemsInRow = row == 0 ? 3 : 2;
            float rowWidth = (itemsInRow - 1) * 280f;
            float x = column * 280f - rowWidth * 0.5f;
            float y = row == 0 ? 95f : -105f;
            Anchor(root, new Vector2(0.5f, 0.5f), new Vector2(230f, 150f));
            root.anchoredPosition = new Vector2(x, y);

            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.18f);
            Button button = root.gameObject.AddComponent<Button>();

            TMP_Text title = CreateText("LevelText", root, $"Level {index + 1}", 30, TextAlignmentOptions.Center, new Vector2(0.5f, 0.53f), new Vector2(210f, 80f));

            GameObject overlay = CreatePanel("LockOverlay", root, new Color(0f, 0f, 0f, 0.55f));
            TMP_Text lockText = CreateText("LockText", overlay.transform, "LOCKED", 24, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(190f, 60f));

            TagBasketLevelButton levelButton = root.gameObject.AddComponent<TagBasketLevelButton>();
            levelButton.button = button;
            levelButton.levelText = title;
            levelButton.lockOverlay = overlay;
            levelButton.lockText = lockText;
            return levelButton;
        }

        private static void CreateTutorialOverlay(Transform parent, TagBasketSortGameManager manager)
        {
            RectTransform root = CreateRect("FirstLevelTutorialOverlay", parent);
            root.anchorMin = new Vector2(0.5f, 1f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(920f, 92f);
            root.anchoredPosition = new Vector2(0f, -118f);

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.08f, 0.78f);
            background.raycastTarget = false;

            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            manager.tutorialOverlay = group;
            manager.tutorialBreathTarget = root;
            manager.tutorialText = CreateText("TutorialText", root, "Drag an object into the matching basket.", 34, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(860f, 72f));
            manager.tutorialText.fontStyle = FontStyles.Bold;
        }

        private static void CreateHintOverlay(RectTransform parent, TagBasketSortGameManager manager)
        {
            GameObject panel = CreatePanel("HintOverlay", parent, new Color(0f, 0f, 0f, 0.58f));
            RectTransform rect = panel.GetComponent<RectTransform>();
            Anchor(rect, new Vector2(0.5f, 0.72f), new Vector2(640f, 94f));
            CanvasGroup group = panel.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            manager.hintOverlay = group;
            manager.hintText = CreateText("HintText", panel.transform, "Try this one", 30, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(590f, 66f));
        }

        private static void CreateFeedbackPopup(RectTransform parent, TagBasketSortGameManager manager)
        {
            RectTransform popup = CreateRect("FeedbackPopup_TextOnly", parent);
            Anchor(popup, new Vector2(0.5f, 0.61f), new Vector2(620f, 96f));
            CanvasGroup canvasGroup = popup.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            manager.feedbackPopup = canvasGroup;
            manager.feedbackText = CreateText("FeedbackText", popup, "Correct!", 38, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(600f, 86f));
        }

        private static void CreateScoreDeltaPopup(RectTransform parent, TagBasketSortGameManager manager)
        {
            RectTransform popup = CreateRect("ScoreDeltaPopup_TextOnly", parent);
            Anchor(popup, new Vector2(0.82f, 0.83f), new Vector2(220f, 80f));
            CanvasGroup canvasGroup = popup.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            manager.scoreDeltaPopup = canvasGroup;
            manager.scoreDeltaText = CreateText("ScoreDeltaText", popup, "+10", 40, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(210f, 72f));
        }

        private static void CreatePausePanel(RectTransform parent, TagBasketSortGameManager manager)
        {
            GameObject panel = CreateOverlayPanel("PausePanel", parent, "Paused");
            manager.pausePanel = panel;
            Transform card = panel.transform.Find("OverlayCard");
            manager.resumeButton = CreateButton("ResumeButton", card, "RESUME", new Vector2(0.5f, 0.55f), new Vector2(320f, 70f));
            manager.howToPlayButton = CreateButton("HowToPlayButton", card, "HOW TO PLAY", new Vector2(0.5f, 0.41f), new Vector2(320f, 70f));
            Button pauseHome = CreateButton("HomeButton", card, "HOME", new Vector2(0.5f, 0.27f), new Vector2(320f, 70f));
            manager.homeButtons.Add(pauseHome);
        }

        private static void CreateResultPanel(RectTransform parent, TagBasketSortGameManager manager)
        {
            GameObject panel = CreateOverlayPanel("ResultPanel", parent, "Level Complete!");
            manager.resultPanel = panel;
            Transform card = panel.transform.Find("OverlayCard");
            manager.resultTitleText = card.Find("OverlayTitle").GetComponent<TMP_Text>();
            manager.resultBodyText = CreateText("ResultBodyText", card, "Score", 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0.55f), new Vector2(650f, 190f));
            manager.continueButton = CreateButton("ContinueButton", card, "CONTINUE", new Vector2(0.36f, 0.18f), new Vector2(250f, 64f));
            manager.playAgainButton = CreateButton("PlayAgainButton", card, "PLAY AGAIN", new Vector2(0.64f, 0.18f), new Vector2(250f, 64f));
            manager.retryButton = manager.playAgainButton;
        }

        private static void CreateHowToPlayPanel(RectTransform parent, TagBasketSortGameManager manager)
        {
            GameObject panel = CreateOverlayPanel("HowToPlayPanel", parent, "How To Play");
            manager.howToPlayPanel = panel;
            CreateText("HowToBodyText", panel.transform.Find("OverlayCard"), "1. Drag each object.\n2. Drop it into the basket with the matching tag.\n3. Hint repeats the same object until you solve it.\n4. Complete the level to unlock the next.", 28, TextAlignmentOptions.Left, new Vector2(0.5f, 0.52f), new Vector2(680f, 260f));
            manager.closeHowToPlayButton = CreateButton("CloseHowToPlayButton", panel.transform.Find("OverlayCard"), "CLOSE", new Vector2(0.5f, 0.16f), new Vector2(280f, 66f));
        }

        private static GameObject CreateOverlayPanel(string name, RectTransform parent, string title)
        {
            GameObject overlay = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0.62f));
            RectTransform card = CreateRect("OverlayCard", overlay.transform);
            Anchor(card, new Vector2(0.5f, 0.5f), new Vector2(780f, 560f));
            Image cardImage = card.gameObject.AddComponent<Image>();
            cardImage.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);
            CreateText("OverlayTitle", card, title, 44, TextAlignmentOptions.Center, new Vector2(0.5f, 0.82f), new Vector2(700f, 80f));
            return overlay;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private static Button CreateButton(string name, Transform parent, string text, Vector2 normalizedAnchor, Vector2 size)
        {
            RectTransform root = CreateRect(name, parent);
            Anchor(root, normalizedAnchor, size);
            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.22f);
            Button button = root.gameObject.AddComponent<Button>();
            CreateText("Text", root, text, 26, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), size - new Vector2(16f, 12f));
            return button;
        }

        private static void CreateTimerSlider(Transform parent, TagBasketSortGameManager manager)
        {
            RectTransform sliderRoot = CreateRect("TimerSlider", parent);
            Anchor(sliderRoot, new Vector2(0.565f, 0.5f), new Vector2(640f, 34f));

            Slider slider = sliderRoot.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            RectTransform background = CreateRect("Background", sliderRoot);
            Stretch(background);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(1f, 1f, 1f, 0.14f);

            RectTransform fillArea = CreateRect("Fill Area", sliderRoot);
            Stretch(fillArea, 5f, 5f, 5f, 5f);

            RectTransform fill = CreateRect("Fill", fillArea);
            Stretch(fill);
            Image fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = manager.timerNormalFillColor;

            slider.fillRect = fill;
            slider.targetGraphic = fillImage;
            manager.timerSlider = slider;
            manager.timerSliderFillImage = fillImage;
            manager.timerText = null;
        }

        private static void CreateHintContainer(Transform parent, TagBasketSortGameManager manager)
        {
            RectTransform container = CreateRect("HintContainer", parent);
            Anchor(container, new Vector2(0.84f, 0.5f), new Vector2(220f, 68f));
            Image containerImage = container.gameObject.AddComponent<Image>();
            containerImage.color = new Color(1f, 1f, 1f, 0.12f);
            manager.hintContainer = container;

            Button hintButton = CreateButton("HintButton", container, "HINT", new Vector2(0.36f, 0.5f), new Vector2(118f, 54f));
            manager.hintButton = hintButton;

            TMP_Text counter = CreateText("HintCounterText", container, "3/3", 26, TextAlignmentOptions.Center, new Vector2(0.77f, 0.5f), new Vector2(82f, 54f));
            counter.fontStyle = FontStyles.Bold;
            manager.hintCounterText = counter;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, int fontSize, TextAlignmentOptions alignment, Vector2 normalizedAnchor, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent);
            Anchor(rect, normalizedAnchor, size);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float bottom = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void Anchor(RectTransform rect, Vector2 normalizedAnchor, Vector2 size)
        {
            rect.anchorMin = normalizedAnchor;
            rect.anchorMax = normalizedAnchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static Vector2 GetObjectPosition(int index)
        {
            switch (index)
            {
                case 0: return new Vector2(-330f, 90f);
                case 1: return new Vector2(-115f, 145f);
                case 2: return new Vector2(120f, 90f);
                case 3: return new Vector2(335f, 140f);
                case 4: return new Vector2(-190f, -75f);
                default: return new Vector2(205f, -70f);
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }
    }

    [CustomEditor(typeof(TagBasketSortGameManager))]
    public sealed class TagBasketSortGameManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            TagBasketSortGameManager manager = (TagBasketSortGameManager)target;

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Tag Basket Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Refresh Levels And Level Buttons Now"))
            {
                Undo.RecordObject(manager, "Refresh Tag Basket Levels And Buttons");
                manager.RefreshLevelsAndButtonsManual();
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            if (GUILayout.Button("Apply Primary/Secondary Fonts To Texts"))
            {
                Undo.RecordObject(manager, "Apply Tag Basket Fonts");
                manager.ApplyConfiguredFontsToAllTexts();
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            if (GUILayout.Button("Reset Saved Level Progress"))
            {
                manager.ResetSavedProgress();
            }
        }
    }
}
