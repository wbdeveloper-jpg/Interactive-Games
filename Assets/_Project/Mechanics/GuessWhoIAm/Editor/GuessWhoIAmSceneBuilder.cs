using System.Collections.Generic;
using System.IO;
using GuessWhoIAm;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuessWhoIAm.EditorTools
{
    public static class GuessWhoIAmSceneBuilder
    {
        private const string CanvasName = "GuessWhoIAm_GeneratedCanvas";
        private const string ManagerName = "GuessWhoIAm_GameManager";
        private const string DatabaseFolder = "Assets/GuessWhoIAm/Data";
        private const string DatabasePath = "Assets/GuessWhoIAm/Data/GuessWhoIAmDemoDatabase.asset";
        private const string GeneratedFolder = "Assets/GuessWhoIAm/Generated";
        private const string RoundedRectSpritePath = "Assets/GuessWhoIAm/Generated/GWI_RoundedRect.png";

        private static Sprite cachedRoundedRectSprite;

        private static readonly Color BackgroundColor = new Color32(32, 25, 60, 255);
        private static readonly Color TopBarColor = new Color32(17, 16, 43, 250);
        private static readonly Color LeftPanelColor = new Color32(0, 0, 0, 0);
        private static readonly Color RightPanelColor = new Color32(55, 43, 86, 125);
        private static readonly Color CardColor = new Color32(255, 252, 245, 255);
        private static readonly Color LockedCardColor = new Color32(71, 55, 105, 170);
        private static readonly Color AccentYellow = new Color32(255, 188, 46, 255);
        private static readonly Color AccentOrange = new Color32(255, 183, 44, 255);
        private static readonly Color AccentBlue = new Color32(141, 37, 221, 255);
        private static readonly Color AccentPurple = new Color32(111, 63, 182, 255);
        private static readonly Color DarkText = new Color32(23, 18, 35, 255);
        private static readonly Color MutedText = new Color32(205, 195, 225, 255);
        private static readonly Color LightText = new Color32(255, 255, 255, 255);
        private static readonly Color BorderColor = new Color32(160, 142, 196, 145);

        [MenuItem("Tools/Guess Who I Am/Create Mockup Matched Responsive Game UI")]
        public static void CreateMockupMatchedResponsiveGameUI()
        {
            DeleteExistingGeneratedObjects();
            EnsureEventSystem();

            GuessWhoQuestionDatabase database = CreateOrUpdateDemoDatabase();
            cachedRoundedRectSprite = GetOrCreateRoundedRectSprite();

            GameObject canvasGo = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasGo.transform as RectTransform;
            SetStretch(canvasRect);

            GameObject safeRoot = CreateUIObject("SafeResponsiveRoot", canvasGo.transform);
            SetStretch(safeRoot.transform as RectTransform);
            Image safeBg = safeRoot.AddComponent<Image>();
            safeBg.color = BackgroundColor;

            VerticalLayoutGroup rootLayout = safeRoot.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(28, 28, 12, 28);
            rootLayout.spacing = 16;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            GameObject topBar = CreatePanel("TopHUDBar_Thin", safeRoot.transform, TopBarColor);
            LayoutElement topBarLayout = topBar.AddComponent<LayoutElement>();
            topBarLayout.minHeight = 65;
            topBarLayout.preferredHeight = 75;
            topBarLayout.flexibleHeight = 0;

            HorizontalLayoutGroup topGroup = topBar.AddComponent<HorizontalLayoutGroup>();
            topGroup.padding = new RectOffset(20, 20, 8, 8);
            topGroup.spacing = 18;
            topGroup.childAlignment = TextAnchor.MiddleCenter;
            topGroup.childControlWidth = true;
            topGroup.childControlHeight = true;
            topGroup.childForceExpandWidth = false;
            topGroup.childForceExpandHeight = true;

            TMP_Text scoreText;
            TMP_Text coinText;
            TMP_Text questionProgressText;
            Image progressFillImage;
            List<Image> progressMarkers;
            Button pauseButton;
            Button helpButton;
            BuildTopBar(topBar.transform, out scoreText, out coinText, out questionProgressText, out progressFillImage, out progressMarkers, out pauseButton, out helpButton);

            GameObject body = CreateUIObject("LandscapeBody", safeRoot.transform);
            LayoutElement bodyLayout = body.AddComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1;
            bodyLayout.flexibleWidth = 1;

            HorizontalLayoutGroup bodyGroup = body.AddComponent<HorizontalLayoutGroup>();
            bodyGroup.spacing = 28;
            bodyGroup.childAlignment = TextAnchor.MiddleCenter;
            bodyGroup.childControlWidth = true;
            bodyGroup.childControlHeight = true;
            bodyGroup.childForceExpandWidth = false;
            bodyGroup.childForceExpandHeight = true;

            GameObject leftArea = CreatePanel("LeftGameplayArea", body.transform, LeftPanelColor);
            LayoutElement leftLayout = leftArea.AddComponent<LayoutElement>();
            leftLayout.flexibleWidth = 1;
            leftLayout.flexibleHeight = 1;

            VerticalLayoutGroup leftGroup = leftArea.AddComponent<VerticalLayoutGroup>();
            leftGroup.padding = new RectOffset(18, 18, 26, 22);
            leftGroup.spacing = 30;
            leftGroup.childControlWidth = true;
            leftGroup.childControlHeight = true;
            leftGroup.childForceExpandWidth = true;
            leftGroup.childForceExpandHeight = false;

            List<GuessWhoIAmOptionGameManager.GuessWhoClueCardUI> clueCardRefs;
            LayoutElement clueRowLayout;
            HorizontalLayoutGroup clueRowGroup;
            RectTransform clueRowRect;
            BuildClueRow(leftArea.transform, out clueCardRefs, out clueRowLayout, out clueRowGroup, out clueRowRect);

            List<GuessWhoIAmOptionGameManager.GuessWhoOptionButtonUI> optionRefs;
            GridLayoutGroup optionsGrid;
            LayoutElement optionsGridLayout;
            RectTransform optionsGridRect;
            BuildOptionsGrid(leftArea.transform, out optionRefs, out optionsGrid, out optionsGridLayout, out optionsGridRect);

            GameObject rightPanel;
            LayoutElement rightPanelLayout;
            CanvasGroup guideBubbleGroup;
            TMP_Text guideText;
            Button revealButton;
            TMP_Text revealMainText;
            TMP_Text revealSubText;
            Button nextButton;
            TMP_Text nextButtonText;
            Slider nextButtonSlider;
            BuildRightMascotPanel(body.transform, out rightPanel, out rightPanelLayout, out guideBubbleGroup, out guideText, out revealButton, out revealMainText, out revealSubText, out nextButton, out nextButtonText, out nextButtonSlider);

            GameObject loadingPanel;
            GameObject howToPanel;
            GameObject pausePanel;
            GameObject resultPanel;
            Button resumeButton;
            Button restartButton;
            Button closeHowToButton;
            Button resultRestartButton;
            Button resultContinueButton;
            TMP_Text resultTitleText;
            TMP_Text resultScoreText;
            TMP_Text resultMessageText;
            TMP_Text loadingTitleText;
            Slider loadingProgressSlider;
            TMP_Text loadingStatusText;
            Image howToGuideImage;
            TMP_Text howToBackupText;
            TMP_Text howToPageText;
            Button howToPreviousButton;
            Button howToNextButton;
            Button howToStartButton;
            BuildOverlayPanels(canvasGo.transform, out loadingPanel, out howToPanel, out pausePanel, out resultPanel, out resumeButton, out restartButton, out closeHowToButton, out resultRestartButton, out resultContinueButton, out resultTitleText, out resultScoreText, out resultMessageText, out loadingTitleText, out loadingProgressSlider, out loadingStatusText, out howToGuideImage, out howToBackupText, out howToPageText, out howToPreviousButton, out howToNextButton, out howToStartButton);

            GameObject managerGo = new GameObject(ManagerName, typeof(AudioSource), typeof(GuessWhoIAmAudioManager), typeof(GuessWhoIAmOptionGameManager));
            GuessWhoIAmOptionGameManager manager = managerGo.GetComponent<GuessWhoIAmOptionGameManager>();

            WireManager(manager, database, scoreText, coinText, questionProgressText, progressFillImage, progressMarkers, clueCardRefs, optionRefs, guideBubbleGroup, guideText, revealButton, revealMainText, revealSubText, nextButton, nextButtonText, nextButtonSlider, pauseButton, helpButton, resumeButton, restartButton, closeHowToButton, resultRestartButton, resultContinueButton, loadingPanel, howToPanel, pausePanel, resultPanel, resultTitleText, resultScoreText, resultMessageText, loadingTitleText, loadingProgressSlider, loadingStatusText, howToGuideImage, howToBackupText, howToPageText, howToPreviousButton, howToNextButton, howToStartButton, managerGo.GetComponent<GuessWhoIAmAudioManager>());

            GuessWhoIAmResponsiveLayout responsive = canvasGo.AddComponent<GuessWhoIAmResponsiveLayout>();
            WireResponsiveLayout(responsive, canvas, canvasRect, topBarLayout, rightPanelLayout, clueRowLayout, optionsGridRect, optionsGridLayout, optionsGrid, clueRowRect, clueRowGroup, clueCardRefs);
            responsive.ApplyResponsiveLayout();

            GuessWhoIAmUIStyler styler = canvasGo.AddComponent<GuessWhoIAmUIStyler>();
            styler.CollectTextsFromChildren();
            styler.ApplyFonts();

            Selection.activeGameObject = canvasGo;
            EditorUtility.DisplayDialog("Guess Who I Am", "Responsive landscape quiz UI created and wired. Press Play to test.", "OK");
        }

        private static void DeleteExistingGeneratedObjects()
        {
            DeleteObjectByName(CanvasName);
            DeleteObjectByName(ManagerName);
        }

        private static void DeleteObjectByName(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
                Object.DestroyImmediate(existing);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Selection.activeGameObject = eventSystem;
        }

        private static GuessWhoQuestionDatabase CreateOrUpdateDemoDatabase()
        {
            EnsureFolder("Assets", "GuessWhoIAm");
            EnsureFolder("Assets/GuessWhoIAm", "Data");

            GuessWhoQuestionDatabase database = AssetDatabase.LoadAssetAtPath<GuessWhoQuestionDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<GuessWhoQuestionDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.questions = new List<GuessWhoQuestionData>
            {
                new GuessWhoQuestionData
                {
                    questionId = "animal_001",
                    answer = "Cat",
                    clue1 = "I am a small animal that people may keep at home.",
                    clue2 = "I have soft fur and I like to chase small moving things.",
                    clue3 = "I say meow.",
                    manualWrongOptions = new List<string>{ "Dog", "Rabbit", "Goat" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_002",
                    answer = "Dog",
                    clue1 = "I am a four-legged animal that people may keep at home.",
                    clue2 = "I am loyal and like to guard the house.",
                    clue3 = "I say bow-wow.",
                    manualWrongOptions = new List<string>{ "Cat", "Rabbit", "Cow" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_003",
                    answer = "Cow",
                    clue1 = "I am a big farm animal.",
                    clue2 = "I eat grass and give something people drink.",
                    clue3 = "I give milk.",
                    manualWrongOptions = new List<string>{ "Goat", "Buffalo", "Horse" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_004",
                    answer = "Goat",
                    clue1 = "I am a farm animal that eats grass and leaves.",
                    clue2 = "I have small horns and can climb on rocks.",
                    clue3 = "I say bleat bleat.",
                    manualWrongOptions = new List<string>{ "Cow", "Sheep", "Deer" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_005",
                    answer = "Elephant",
                    clue1 = "I am a very big animal.",
                    clue2 = "I have large ears and a long body part on my face.",
                    clue3 = "I use my trunk to pick things.",
                    manualWrongOptions = new List<string>{ "Rhino", "Hippo", "Giraffe" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_006",
                    answer = "Lion",
                    clue1 = "I am a wild animal.",
                    clue2 = "I am called the king of the jungle.",
                    clue3 = "The male has a big mane.",
                    manualWrongOptions = new List<string>{ "Tiger", "Leopard", "Bear" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_007",
                    answer = "Tiger",
                    clue1 = "I am a big wild cat.",
                    clue2 = "I have stripes on my body.",
                    clue3 = "I am orange with black stripes.",
                    manualWrongOptions = new List<string>{ "Lion", "Leopard", "Cheetah" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_008",
                    answer = "Monkey",
                    clue1 = "I am an animal that can climb trees.",
                    clue2 = "I like bananas and swing with my hands.",
                    clue3 = "I have a long tail and can copy people.",
                    manualWrongOptions = new List<string>{ "Squirrel", "Bear", "Kangaroo" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_009",
                    answer = "Rabbit",
                    clue1 = "I am a small animal with soft fur.",
                    clue2 = "I hop and like to eat carrots.",
                    clue3 = "I have long ears.",
                    manualWrongOptions = new List<string>{ "Cat", "Squirrel", "Hamster" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_010",
                    answer = "Giraffe",
                    clue1 = "I am a tall wild animal.",
                    clue2 = "I eat leaves from high trees.",
                    clue3 = "I have a very long neck.",
                    manualWrongOptions = new List<string>{ "Elephant", "Camel", "Zebra" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_011",
                    answer = "Zebra",
                    clue1 = "I am a wild animal that looks like a horse.",
                    clue2 = "My body has black and white lines.",
                    clue3 = "I have black and white stripes.",
                    manualWrongOptions = new List<string>{ "Horse", "Donkey", "Giraffe" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_012",
                    answer = "Camel",
                    clue1 = "I am an animal that can live in hot dry places.",
                    clue2 = "I can walk on sand and carry people.",
                    clue3 = "I have a hump on my back.",
                    manualWrongOptions = new List<string>{ "Horse", "Donkey", "Giraffe" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_013",
                    answer = "Fish",
                    clue1 = "I am an animal that lives in water.",
                    clue2 = "I swim using fins.",
                    clue3 = "I breathe with gills.",
                    manualWrongOptions = new List<string>{ "Frog", "Turtle", "Duck" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_014",
                    answer = "Frog",
                    clue1 = "I am a small animal that can live near water.",
                    clue2 = "I jump and catch insects with my tongue.",
                    clue3 = "I say croak.",
                    manualWrongOptions = new List<string>{ "Fish", "Turtle", "Lizard" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "animal_015",
                    answer = "Butterfly",
                    clue1 = "I am a small flying creature.",
                    clue2 = "I have colorful wings and sit on flowers.",
                    clue3 = "I grow from a caterpillar.",
                    manualWrongOptions = new List<string>{ "Bee", "Dragonfly", "Bird" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_001",
                    answer = "Apple",
                    clue1 = "I am a fruit that many children eat.",
                    clue2 = "I can be red or green and crunchy.",
                    clue3 = "People say one of me a day keeps the doctor away.",
                    manualWrongOptions = new List<string>{ "Pear", "Guava", "Peach" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_002",
                    answer = "Banana",
                    clue1 = "I am a fruit that is usually yellow.",
                    clue2 = "I am long and soft inside.",
                    clue3 = "Monkeys are often shown eating me.",
                    manualWrongOptions = new List<string>{ "Mango", "Pineapple", "Papaya" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_003",
                    answer = "Mango",
                    clue1 = "I am a sweet fruit.",
                    clue2 = "I am yellow or orange inside and have one big seed.",
                    clue3 = "People call me the king of fruits.",
                    manualWrongOptions = new List<string>{ "Banana", "Papaya", "Orange" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_004",
                    answer = "Orange",
                    clue1 = "I am a round fruit.",
                    clue2 = "I am juicy and full of vitamin C.",
                    clue3 = "My name is also my color.",
                    manualWrongOptions = new List<string>{ "Apple", "Lemon", "Mango" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_005",
                    answer = "Grapes",
                    clue1 = "I am a fruit that grows in bunches.",
                    clue2 = "I can be green or purple and very small.",
                    clue3 = "Many of me are joined on one stem.",
                    manualWrongOptions = new List<string>{ "Cherry", "Berries", "Plum" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_006",
                    answer = "Watermelon",
                    clue1 = "I am a big fruit.",
                    clue2 = "I am green outside and red inside.",
                    clue3 = "I have many black seeds and lots of water.",
                    manualWrongOptions = new List<string>{ "Muskmelon", "Papaya", "Pumpkin" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_007",
                    answer = "Pineapple",
                    clue1 = "I am a tropical fruit.",
                    clue2 = "I have a rough outside and a leafy top.",
                    clue3 = "My outside looks spiky.",
                    manualWrongOptions = new List<string>{ "Jackfruit", "Mango", "Papaya" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_008",
                    answer = "Strawberry",
                    clue1 = "I am a small red fruit.",
                    clue2 = "I have tiny seeds on my outside.",
                    clue3 = "My shape looks like a heart.",
                    manualWrongOptions = new List<string>{ "Cherry", "Apple", "Pomegranate" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_009",
                    answer = "Papaya",
                    clue1 = "I am a soft fruit.",
                    clue2 = "I am orange inside and have many black seeds in the middle.",
                    clue3 = "My name starts with Pa.",
                    manualWrongOptions = new List<string>{ "Mango", "Muskmelon", "Watermelon" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_010",
                    answer = "Pomegranate",
                    clue1 = "I am a fruit with many small juicy parts inside.",
                    clue2 = "My inside has many red seeds.",
                    clue3 = "People eat my red seeds one by one.",
                    manualWrongOptions = new List<string>{ "Apple", "Orange", "Guava" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_011",
                    answer = "Guava",
                    clue1 = "I am a round fruit.",
                    clue2 = "I can be green outside and pink or white inside.",
                    clue3 = "I have many tiny hard seeds.",
                    manualWrongOptions = new List<string>{ "Apple", "Pear", "Peach" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "fruit_012",
                    answer = "Lemon",
                    clue1 = "I am a small fruit.",
                    clue2 = "I am yellow and sour.",
                    clue3 = "People squeeze me into water or food.",
                    manualWrongOptions = new List<string>{ "Orange", "Mango", "Guava" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_001",
                    answer = "Carrot",
                    clue1 = "I am a vegetable that grows under the ground.",
                    clue2 = "I am orange and rabbits are shown eating me.",
                    clue3 = "I am long and crunchy.",
                    manualWrongOptions = new List<string>{ "Radish", "Potato", "Beetroot" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_002",
                    answer = "Potato",
                    clue1 = "I am a vegetable that grows under the ground.",
                    clue2 = "I am brown outside and used to make chips.",
                    clue3 = "French fries are made from me.",
                    manualWrongOptions = new List<string>{ "Carrot", "Onion", "Turnip" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_003",
                    answer = "Tomato",
                    clue1 = "I am used in salads and cooking.",
                    clue2 = "I am red and juicy.",
                    clue3 = "Some people call me a fruit, but we use me like a vegetable.",
                    manualWrongOptions = new List<string>{ "Red Capsicum", "Carrot", "Beetroot" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_004",
                    answer = "Onion",
                    clue1 = "I am a vegetable used in cooking.",
                    clue2 = "I have many layers.",
                    clue3 = "I can make your eyes water when cut.",
                    manualWrongOptions = new List<string>{ "Garlic", "Cabbage", "Potato" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_005",
                    answer = "Cabbage",
                    clue1 = "I am a leafy vegetable.",
                    clue2 = "I am round and have many leaves packed together.",
                    clue3 = "People chop me for salad and noodles.",
                    manualWrongOptions = new List<string>{ "Lettuce", "Spinach", "Cauliflower" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_006",
                    answer = "Cauliflower",
                    clue1 = "I am a vegetable that looks like a flower.",
                    clue2 = "I am usually white with green leaves around me.",
                    clue3 = "My name has flower in it.",
                    manualWrongOptions = new List<string>{ "Cabbage", "Broccoli", "Turnip" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_007",
                    answer = "Spinach",
                    clue1 = "I am a leafy vegetable.",
                    clue2 = "I am green and soft when cooked.",
                    clue3 = "Popeye is famous for eating me.",
                    manualWrongOptions = new List<string>{ "Coriander", "Mint", "Cabbage" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_008",
                    answer = "Brinjal",
                    clue1 = "I am a vegetable used in curry.",
                    clue2 = "I am usually purple and shiny.",
                    clue3 = "I am also called eggplant.",
                    manualWrongOptions = new List<string>{ "Beetroot", "Purple Cabbage", "Onion" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_009",
                    answer = "Cucumber",
                    clue1 = "I am a cool vegetable eaten raw.",
                    clue2 = "I am long, green, and watery.",
                    clue3 = "People put my slices in salad.",
                    manualWrongOptions = new List<string>{ "Bottle Gourd", "Zucchini", "Green Chilli" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_010",
                    answer = "Pumpkin",
                    clue1 = "I am a large vegetable.",
                    clue2 = "I am round and orange.",
                    clue3 = "People make lantern faces from me in some countries.",
                    manualWrongOptions = new List<string>{ "Watermelon", "Papaya", "Carrot" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_011",
                    answer = "Peas",
                    clue1 = "I am a green vegetable.",
                    clue2 = "I am small and round, and live inside a pod.",
                    clue3 = "Many of me come together in one pod.",
                    manualWrongOptions = new List<string>{ "Beans", "Corn", "Green Gram" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "veg_012",
                    answer = "Corn",
                    clue1 = "I am a yellow food grown on a plant.",
                    clue2 = "My small yellow grains are attached to a cob.",
                    clue3 = "People eat me as sweet corn or popcorn.",
                    manualWrongOptions = new List<string>{ "Peas", "Wheat", "Rice" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_001",
                    answer = "Pencil",
                    clue1 = "I am a school item.",
                    clue2 = "You use me to write and can erase my marks.",
                    clue3 = "I have graphite inside.",
                    manualWrongOptions = new List<string>{ "Pen", "Crayon", "Marker" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_002",
                    answer = "Eraser",
                    clue1 = "I am a school item.",
                    clue2 = "I help remove pencil marks.",
                    clue3 = "Students rub me on paper.",
                    manualWrongOptions = new List<string>{ "Sharpener", "Pencil", "Ruler" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_003",
                    answer = "Sharpener",
                    clue1 = "I am a school item.",
                    clue2 = "I help make a pencil tip pointy.",
                    clue3 = "You turn a pencil inside me.",
                    manualWrongOptions = new List<string>{ "Eraser", "Ruler", "Glue" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_004",
                    answer = "Ruler",
                    clue1 = "I am a school item.",
                    clue2 = "I help draw straight lines and measure length.",
                    clue3 = "I have numbers and centimetres on me.",
                    manualWrongOptions = new List<string>{ "Pencil", "Scale Balance", "Compass" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_005",
                    answer = "Book",
                    clue1 = "I am used for learning or stories.",
                    clue2 = "I have many pages.",
                    clue3 = "You read me.",
                    manualWrongOptions = new List<string>{ "Notebook", "Newspaper", "Diary" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_006",
                    answer = "Clock",
                    clue1 = "I am found in homes and classrooms.",
                    clue2 = "I show hours and minutes.",
                    clue3 = "People look at me to know the time.",
                    manualWrongOptions = new List<string>{ "Calendar", "Watch", "Calculator" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_007",
                    answer = "Umbrella",
                    clue1 = "I am useful outside.",
                    clue2 = "I protect you from rain and strong sun.",
                    clue3 = "You open me above your head.",
                    manualWrongOptions = new List<string>{ "Raincoat", "Hat", "Bag" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_008",
                    answer = "Chair",
                    clue1 = "I am furniture.",
                    clue2 = "I have legs and a back.",
                    clue3 = "You sit on me.",
                    manualWrongOptions = new List<string>{ "Table", "Sofa", "Bed" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_009",
                    answer = "Table",
                    clue1 = "I am furniture.",
                    clue2 = "You keep books, food, or things on me.",
                    clue3 = "I have a flat top.",
                    manualWrongOptions = new List<string>{ "Chair", "Desk", "Shelf" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_010",
                    answer = "Spoon",
                    clue1 = "I am used while eating.",
                    clue2 = "I help you eat rice, soup, or dessert.",
                    clue3 = "I have a small bowl-shaped end.",
                    manualWrongOptions = new List<string>{ "Fork", "Plate", "Cup" }
                },
                new GuessWhoQuestionData
                {
                    questionId = "object_011",
                    answer = "Bottle",
                    clue1 = "I am used to carry something.",
                    clue2 = "I can hold water for school or travel.",
                    clue3 = "You drink from me after opening my cap.",
                    manualWrongOptions = new List<string>{ "Glass", "Cup", "Jug" }
                }
            };

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return database;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void BuildTopBar(Transform parent, out TMP_Text scoreText, out TMP_Text coinText, out TMP_Text questionProgressText, out Image progressFill, out List<Image> markers, out Button pauseButton, out Button helpButton)
        {
            markers = new List<Image>();
            scoreText = null;

            GameObject progressBlock = CreateUIObject("QuestionProgressBlock", parent);
            LayoutElement progressLayout = progressBlock.AddComponent<LayoutElement>();
            progressLayout.flexibleWidth = 1;
            VerticalLayoutGroup progressVertical = progressBlock.AddComponent<VerticalLayoutGroup>();
            progressVertical.spacing = 5;
            progressVertical.childAlignment = TextAnchor.MiddleCenter;
            progressVertical.childControlWidth = true;
            progressVertical.childControlHeight = true;
            progressVertical.childForceExpandWidth = true;
            progressVertical.childForceExpandHeight = false;

            questionProgressText = CreateText(progressBlock.transform, "ProgressText", "Question 1 / 10", 24, TextAlignmentOptions.Center, LightText, FontStyles.Bold);
            LayoutElement progressTextLayout = questionProgressText.gameObject.AddComponent<LayoutElement>();
            progressTextLayout.preferredHeight = 28;

            GameObject progressLine = CreatePanel("SlimProgressLine", progressBlock.transform, new Color32(255, 255, 255, 42));
            LayoutElement lineLayout = progressLine.AddComponent<LayoutElement>();
            lineLayout.preferredHeight = 14;
            lineLayout.minHeight = 12;
            RectTransform progressLineRect = progressLine.transform as RectTransform;

            GameObject fillGo = CreateUIObject("ProgressFill", progressLine.transform);
            SetStretch(fillGo.transform as RectTransform);
            progressFill = fillGo.AddComponent<Image>();
            progressFill.color = AccentYellow;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressFill.fillAmount = 0f;

            GameObject markerRow = CreateUIObject("ProgressStepMarkers", progressLine.transform);
            SetStretch(markerRow.transform as RectTransform);
            HorizontalLayoutGroup markerGroup = markerRow.AddComponent<HorizontalLayoutGroup>();
            markerGroup.childAlignment = TextAnchor.MiddleCenter;
            markerGroup.childControlWidth = false;
            markerGroup.childControlHeight = false;
            markerGroup.childForceExpandWidth = true;
            markerGroup.childForceExpandHeight = false;
            markerGroup.spacing = 0;
            markerGroup.padding = new RectOffset(4, 4, 0, 0);

            for (int i = 0; i < 10; i++)
            {
                GameObject markerSlot = CreateUIObject("StepSlot_" + (i + 1), markerRow.transform);
                LayoutElement slotLayout = markerSlot.AddComponent<LayoutElement>();
                slotLayout.flexibleWidth = 1;
                slotLayout.preferredHeight = 12;

                GameObject markerGo = CreateUIObject("ProgressStepMarker_" + (i + 1), markerSlot.transform);
                RectTransform markerRect = markerGo.transform as RectTransform;
                markerRect.anchorMin = markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.sizeDelta = new Vector2(8, 8);
                Image marker = markerGo.AddComponent<Image>();
                marker.color = new Color32(105, 117, 139, 255);
                markers.Add(marker);
            }

            GameObject rightHud = CreateUIObject("RightHudActions", parent);
            LayoutElement rightLayout = rightHud.AddComponent<LayoutElement>();
            rightLayout.minWidth = 285;
            rightLayout.preferredWidth = 330;
            AddHorizontal(rightHud, 8, 0, TextAnchor.MiddleRight);

            GameObject coinBlock = CreatePanel("CoinBlock_Compact", rightHud.transform, new Color32(255, 255, 255, 26));
            LayoutElement coinLayout = coinBlock.AddComponent<LayoutElement>();
            coinLayout.minWidth = 145;
            coinLayout.preferredWidth = 160;
            AddHorizontal(coinBlock, 8, 8, TextAnchor.MiddleCenter);
            GameObject coinIcon = CreateIconPlaceholder(coinBlock.transform, "CoinIconWhitePlaceholder_ReplaceSpriteHere", 44, Color.white);
            coinText = CreateText(coinBlock.transform, "CoinText", "0", 28, TextAlignmentOptions.Left, LightText, FontStyles.Bold);

            helpButton = CreateSmallHudButton(rightHud.transform, "HelpButton", string.Empty);
            pauseButton = CreateSmallHudButton(rightHud.transform, "PauseButton", string.Empty);
        }

        private static void BuildClueRow(Transform parent, out List<GuessWhoIAmOptionGameManager.GuessWhoClueCardUI> cards, out LayoutElement rowLayout, out HorizontalLayoutGroup rowGroup, out RectTransform rowRect)
        {
            cards = new List<GuessWhoIAmOptionGameManager.GuessWhoClueCardUI>();

            GameObject clueRow = CreateUIObject("ClueCardsHorizontalRow", parent);
            rowRect = clueRow.transform as RectTransform;
            rowLayout = clueRow.AddComponent<LayoutElement>();
            rowLayout.minHeight = 390;
            rowLayout.preferredHeight = 470;
            rowLayout.flexibleHeight = 5.5f;

            rowGroup = clueRow.AddComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 34;
            rowGroup.padding = new RectOffset(20, 20, 30, 22);
            rowGroup.childAlignment = TextAnchor.MiddleCenter;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = false;

            for (int i = 0; i < 3; i++)
                cards.Add(CreateClueCard(clueRow.transform, i));
        }

        private static GuessWhoIAmOptionGameManager.GuessWhoClueCardUI CreateClueCard(Transform parent, int index)
        {
            bool active = index == 0;
            GameObject cardGo = CreatePanel("ClueCard_" + (index + 1), parent, active ? CardColor : LockedCardColor);
            RectTransform cardRect = cardGo.transform as RectTransform;
            Button button = cardGo.AddComponent<Button>();
            Image bg = cardGo.GetComponent<Image>();
            button.targetGraphic = bg;
            CanvasGroup group = cardGo.AddComponent<CanvasGroup>();

            Outline outline = cardGo.AddComponent<Outline>();
            outline.effectColor = active ? AccentYellow : BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            Shadow shadow = cardGo.AddComponent<Shadow>();
            shadow.effectColor = active ? new Color32(255, 188, 46, 95) : new Color32(0, 0, 0, 80);
            shadow.effectDistance = new Vector2(0f, -3f);

            LayoutElement layout = cardGo.AddComponent<LayoutElement>();
            layout.flexibleWidth = 0;
            layout.flexibleHeight = 0;
            layout.minWidth = 370;
            layout.preferredWidth = 430;
            layout.minHeight = 370;
            layout.preferredHeight = 430;

            GameObject pointer = CreateUIObject("SelectedPointer", cardGo.transform);
            RectTransform pointerRect = pointer.transform as RectTransform;
            pointerRect.anchorMin = pointerRect.anchorMax = new Vector2(0.5f, 0f);
            pointerRect.pivot = new Vector2(0.5f, 0.5f);
            pointerRect.sizeDelta = new Vector2(38, 38);
            pointerRect.anchoredPosition = new Vector2(0, -14);
            pointerRect.localRotation = Quaternion.Euler(0, 0, 45f);
            Image pointerImage = pointer.AddComponent<Image>();
            pointerImage.color = AccentYellow;
            pointerImage.raycastTarget = false;
            pointer.SetActive(active);

            GameObject badge = CreatePanel("ClueNumberBadge", cardGo.transform, active ? AccentYellow : new Color32(156, 141, 184, 255));
            RectTransform badgeRect = badge.transform as RectTransform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.sizeDelta = new Vector2(58, 58);
            badgeRect.anchoredPosition = new Vector2(0, 14);
            TMP_Text badgeText = CreateText(badge.transform, "BadgeNumber", (index + 1).ToString(), 27, TextAlignmentOptions.Center, DarkText, FontStyles.Bold);
            SetStretch(badgeText.rectTransform);

            TMP_Text titleText = CreateText(cardGo.transform, "ClueTitleText", "CLUE " + (index + 1), 29, TextAlignmentOptions.Center, active ? DarkText : MutedText, FontStyles.Bold);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.12f, 0.71f);
            titleRect.anchorMax = new Vector2(0.88f, 0.84f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            GameObject lockBg = CreatePanel("LockWhiteIconPlaceholder_ReplaceSpriteHere", cardGo.transform, Color.white);
            RectTransform lockRect = lockBg.transform as RectTransform;
            lockRect.anchorMin = lockRect.anchorMax = new Vector2(0.5f, 0.63f);
            lockRect.pivot = new Vector2(0.5f, 0.5f);
            lockRect.sizeDelta = new Vector2(64, 64);
            lockRect.anchoredPosition = Vector2.zero;
            TMP_Text lockText = CreateText(lockBg.transform, "LockIconText", string.Empty, 1, TextAlignmentOptions.Center, DarkText, FontStyles.Normal);
            SetStretch(lockText.rectTransform);
            lockBg.SetActive(!active);

            TMP_Text clueText = CreateText(cardGo.transform, "ClueText", active ? "Clue text appears here." : "Reveal next clue\nto unlock", active ? 29 : 24, TextAlignmentOptions.Center, active ? DarkText : MutedText, FontStyles.Normal);
            clueText.enableAutoSizing = true;
            clueText.fontSizeMin = 18;
            clueText.fontSizeMax = active ? 32 : 26;
            clueText.enableWordWrapping = true;
            clueText.overflowMode = TextOverflowModes.Ellipsis;
            RectTransform clueRect = clueText.rectTransform;
            clueRect.anchorMin = new Vector2(0.12f, 0.34f);
            clueRect.anchorMax = new Vector2(0.88f, 0.66f);
            clueRect.offsetMin = Vector2.zero;
            clueRect.offsetMax = Vector2.zero;

            GameObject chip = CreatePanel("ValueChip", cardGo.transform, active ? AccentYellow : new Color32(154, 137, 181, 220));
            RectTransform chipRect = chip.transform as RectTransform;
            chipRect.anchorMin = chipRect.anchorMax = new Vector2(0.5f, 0.18f);
            chipRect.pivot = new Vector2(0.5f, 0.5f);
            chipRect.sizeDelta = new Vector2(128, 48);
            chipRect.anchoredPosition = Vector2.zero;
            TMP_Text chipText = CreateText(chip.transform, "ChipPoints", index == 0 ? "+10" : index == 1 ? "+7" : "+5", 24, TextAlignmentOptions.Center, DarkText, FontStyles.Bold);
            SetStretch(chipText.rectTransform);

            return new GuessWhoIAmOptionGameManager.GuessWhoClueCardUI
            {
                root = cardRect,
                button = button,
                background = bg,
                canvasGroup = group,
                layoutElement = layout,
                badgeText = badgeText,
                badgeBackground = badge.GetComponent<Image>(),
                outline = outline,
                titleText = titleText,
                clueText = clueText,
                valueChipText = chipText,
                valueChipBackground = chip.GetComponent<Image>(),
                selectedPointer = pointerImage,
                lockIconBackground = lockBg.GetComponent<Image>(),
                lockIconText = lockText
            };
        }

        private static void BuildOptionsGrid(Transform parent, out List<GuessWhoIAmOptionGameManager.GuessWhoOptionButtonUI> options, out GridLayoutGroup grid, out LayoutElement gridLayout, out RectTransform gridRect)
        {
            options = new List<GuessWhoIAmOptionGameManager.GuessWhoOptionButtonUI>();

            GameObject gridGo = CreateUIObject("AnswerOptions_2x2Grid", parent);
            gridRect = gridGo.transform as RectTransform;
            gridLayout = gridGo.AddComponent<LayoutElement>();
            gridLayout.flexibleHeight = 0f;
            gridLayout.minHeight = 315f;
            gridLayout.preferredHeight = 325f;
            gridLayout.flexibleWidth = 1;

            grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(26, 24);
            grid.cellSize = new Vector2(610, 148);
            grid.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < 4; i++)
            {
                GameObject optionGo = CreatePanel("AnswerOptionButton_" + (i + 1), gridGo.transform, new Color32(54, 41, 86, 225));
                Button button = optionGo.AddComponent<Button>();
                Image bg = optionGo.GetComponent<Image>();
                button.targetGraphic = bg;

                Outline outline = optionGo.AddComponent<Outline>();
                outline.effectColor = BorderColor;
                outline.effectDistance = new Vector2(2f, -2f);

                GameObject badge = CreatePanel("OptionLetterBadge", optionGo.transform, AccentPurple);
                RectTransform badgeRect = badge.transform as RectTransform;
                badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0f, 0.5f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.sizeDelta = new Vector2(64, 64);
                badgeRect.anchoredPosition = new Vector2(76, 0);
                TMP_Text badgeText = CreateText(badge.transform, "OptionLetterText", ((char)('A' + i)).ToString(), 32, TextAlignmentOptions.Center, LightText, FontStyles.Bold);
                SetStretch(badgeText.rectTransform);

                TMP_Text label = CreateText(optionGo.transform, "OptionText", "Option " + (i + 1), 30, TextAlignmentOptions.MidlineLeft, LightText, FontStyles.Bold);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = new Vector2(140, 14);
                labelRect.offsetMax = new Vector2(-24, -14);
                label.enableAutoSizing = true;
                label.fontSizeMin = 20;
                label.fontSizeMax = 34;
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Ellipsis;

                options.Add(new GuessWhoIAmOptionGameManager.GuessWhoOptionButtonUI
                {
                    root = optionGo.transform as RectTransform,
                    button = button,
                    background = bg,
                    letterBadgeBackground = badge.GetComponent<Image>(),
                    letterBadgeText = badgeText,
                    labelText = label
                });
            }
        }

        private static void BuildRightMascotPanel(Transform parent, out GameObject rightPanel, out LayoutElement panelLayout, out CanvasGroup guideBubbleGroup, out TMP_Text guideText, out Button revealButton, out TMP_Text revealMainText, out TMP_Text revealSubText, out Button nextButton, out TMP_Text nextButtonText, out Slider nextSlider)
        {
            rightPanel = CreatePanel("RightMascotGuidePanel", parent, RightPanelColor);
            Outline panelOutline = rightPanel.AddComponent<Outline>();
            panelOutline.effectColor = BorderColor;
            panelOutline.effectDistance = new Vector2(2f, -2f);

            panelLayout = rightPanel.AddComponent<LayoutElement>();
            panelLayout.minWidth = 420;
            panelLayout.preferredWidth = 560;
            panelLayout.flexibleWidth = 0;
            panelLayout.flexibleHeight = 1;

            VerticalLayoutGroup rightGroup = rightPanel.AddComponent<VerticalLayoutGroup>();
            rightGroup.padding = new RectOffset(28, 28, 26, 26);
            rightGroup.spacing = 14;
            rightGroup.childAlignment = TextAnchor.LowerCenter;
            rightGroup.childControlWidth = true;
            rightGroup.childControlHeight = true;
            rightGroup.childForceExpandWidth = true;
            rightGroup.childForceExpandHeight = false;

            GameObject mascotFrame = CreatePanel("MascotImageOnlyFrame", rightPanel.transform, new Color32(255, 255, 255, 0));
            LayoutElement mascotLayout = mascotFrame.AddComponent<LayoutElement>();
            mascotLayout.minHeight = 330;
            mascotLayout.preferredHeight = 430;
            mascotLayout.flexibleHeight = 1;
            Image mascotFrameImage = mascotFrame.GetComponent<Image>();
            mascotFrameImage.raycastTarget = false;

            GameObject mascotPlaceholder = CreatePanel("MascotWhiteImagePlaceholder_ReplaceSpriteHere", mascotFrame.transform, Color.white);
            SetStretch(mascotPlaceholder.transform as RectTransform, 36, 14, 36, 8);
            Image mascotImage = mascotPlaceholder.GetComponent<Image>();
            mascotImage.preserveAspect = true;
            mascotImage.raycastTarget = false;
            AspectRatioFitter mascotAspect = mascotPlaceholder.AddComponent<AspectRatioFitter>();
            mascotAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            mascotAspect.aspectRatio = 1.05f;

            GameObject speechBubble = CreatePanel("MascotSpeechBubble", rightPanel.transform, new Color32(73, 54, 111, 245));
            Outline bubbleOutline = speechBubble.AddComponent<Outline>();
            bubbleOutline.effectColor = BorderColor;
            bubbleOutline.effectDistance = new Vector2(2f, -2f);
            guideBubbleGroup = speechBubble.AddComponent<CanvasGroup>();
            LayoutElement bubbleLayout = speechBubble.AddComponent<LayoutElement>();
            bubbleLayout.minHeight = 82;
            bubbleLayout.preferredHeight = 100;
            bubbleLayout.flexibleHeight = 0f;

            HorizontalLayoutGroup bubbleGroup = speechBubble.AddComponent<HorizontalLayoutGroup>();
            bubbleGroup.padding = new RectOffset(18, 18, 14, 14);
            bubbleGroup.spacing = 16;
            bubbleGroup.childAlignment = TextAnchor.MiddleCenter;
            bubbleGroup.childControlWidth = true;
            bubbleGroup.childControlHeight = true;
            bubbleGroup.childForceExpandWidth = false;
            bubbleGroup.childForceExpandHeight = true;

            GameObject hintIcon = CreatePanel("SpeechIconWhitePlaceholder_ReplaceSpriteHere", speechBubble.transform, Color.white);
            LayoutElement hintLayout = hintIcon.AddComponent<LayoutElement>();
            hintLayout.minWidth = 56;
            hintLayout.preferredWidth = 56;
            hintLayout.minHeight = 56;
            hintLayout.preferredHeight = 56;
            hintLayout.flexibleWidth = 0f;
            hintLayout.flexibleHeight = 0f;
            

            guideText = CreateText(speechBubble.transform, "GuideHelperText", "Reveal more clues if you're not sure!\nYour points will drop.", 22, TextAlignmentOptions.MidlineLeft, LightText, FontStyles.Bold);
            guideText.enableAutoSizing = true;
            guideText.fontSizeMin = 16;
            guideText.fontSizeMax = 23;
            guideText.enableWordWrapping = true;
            LayoutElement guideLayout = guideText.gameObject.AddComponent<LayoutElement>();
            guideLayout.flexibleWidth = 1;

            revealButton = CreateLargeActionButton(rightPanel.transform, "RevealNextClueButton", AccentOrange, out revealMainText, out revealSubText);
            revealMainText.text = "Reveal Next Clue";
            revealSubText.text = "Your answer points will be 7";

            GameObject nextGo = CreatePanel("NextQuestionButton_WithSlider", rightPanel.transform, AccentBlue);
            LayoutElement nextLayout = nextGo.AddComponent<LayoutElement>();
            nextLayout.minHeight = 82;
            nextLayout.preferredHeight = 96;
            nextLayout.flexibleHeight = 0f;
            nextButton = nextGo.AddComponent<Button>();
            nextButton.targetGraphic = nextGo.GetComponent<Image>();

            GameObject sliderGo = CreateUIObject("IntegratedAutoNextSlider", nextGo.transform);
            SetStretch(sliderGo.transform as RectTransform);
            nextSlider = sliderGo.AddComponent<Slider>();
            nextSlider.interactable = false;
            nextSlider.minValue = 0;
            nextSlider.maxValue = 1;
            nextSlider.value = 0;
            nextSlider.transition = Selectable.Transition.None;

            GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform);
            SetStretch(fillArea.transform as RectTransform);
            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            SetStretch(fill.transform as RectTransform);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color32(255, 255, 255, 64);
            nextSlider.fillRect = fill.transform as RectTransform;

            GameObject nextIcon = CreatePanel("NextIconWhitePlaceholder_ReplaceSpriteHere", nextGo.transform, Color.white);
            RectTransform nextIconRect = nextIcon.transform as RectTransform;
            nextIconRect.anchorMin = nextIconRect.anchorMax = new Vector2(0f, 0.5f);
            nextIconRect.pivot = new Vector2(0.5f, 0.5f);
            nextIconRect.sizeDelta = new Vector2(58, 58);
            nextIconRect.anchoredPosition = new Vector2(58, 0);
            nextButtonText = CreateText(nextGo.transform, "NextButtonText", "Next 4s", 28, TextAlignmentOptions.MidlineLeft, LightText, FontStyles.Bold);
            RectTransform nextTextRect = nextButtonText.rectTransform;
            nextTextRect.anchorMin = new Vector2(0f, 0f);
            nextTextRect.anchorMax = new Vector2(1f, 1f);
            nextTextRect.offsetMin = new Vector2(118, 12);
            nextTextRect.offsetMax = new Vector2(-20, -12);
            nextButtonText.enableAutoSizing = true;
            nextButtonText.fontSizeMin = 18;
            nextButtonText.fontSizeMax = 30;
            nextGo.SetActive(false);
        }

        private static Button CreateLargeActionButton(Transform parent, string name, Color bgColor, out TMP_Text mainText, out TMP_Text subText)
        {
            GameObject buttonGo = CreatePanel(name, parent, bgColor);
            LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
            layout.minHeight = 82;
            layout.preferredHeight = 98;
            layout.flexibleHeight = 0f;

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonGo.GetComponent<Image>();

            HorizontalLayoutGroup horizontal = buttonGo.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(20, 20, 12, 12);
            horizontal.spacing = 18;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = true;

            GameObject iconCircle = CreatePanel("RevealIconWhitePlaceholder_ReplaceSpriteHere", buttonGo.transform, Color.white);
            LayoutElement iconLayout = iconCircle.AddComponent<LayoutElement>();
            iconLayout.minWidth = 58;
            iconLayout.preferredWidth = 58;
            iconLayout.minHeight = 58;
            iconLayout.preferredHeight = 58;
            iconLayout.flexibleWidth = 0f;
            iconLayout.flexibleHeight = 0f;
            GameObject textStack = CreateUIObject("ActionTextStack", buttonGo.transform);
            LayoutElement stackLayout = textStack.AddComponent<LayoutElement>();
            stackLayout.flexibleWidth = 1;
            VerticalLayoutGroup vertical = textStack.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 0;
            vertical.childAlignment = TextAnchor.MiddleLeft;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            mainText = CreateText(textStack.transform, "ButtonMainText", "Main", 26, TextAlignmentOptions.Left, DarkText, FontStyles.Bold);
            subText = CreateText(textStack.transform, "ButtonSubText", "Sub", 18, TextAlignmentOptions.Left, DarkText, FontStyles.Normal);
            subText.enableAutoSizing = true;
            subText.fontSizeMin = 14;
            subText.fontSizeMax = 20;

            GameObject arrowPlaceholder = CreateIconPlaceholder(buttonGo.transform, "ActionArrowWhitePlaceholder_ReplaceSpriteHere", 34, Color.white);
            return button;
        }

        private static void BuildOverlayPanels(Transform canvasParent, out GameObject loadingPanel, out GameObject howToPanel, out GameObject pausePanel, out GameObject resultPanel, out Button resumeButton, out Button restartButton, out Button closeHowToButton, out Button resultRestartButton, out Button resultContinueButton, out TMP_Text resultTitleText, out TMP_Text resultScoreText, out TMP_Text resultMessageText, out TMP_Text loadingTitleText, out Slider loadingProgressSlider, out TMP_Text loadingStatusText, out Image howToGuideImage, out TMP_Text howToBackupText, out TMP_Text howToPageText, out Button howToPreviousButton, out Button howToNextButton, out Button howToStartButton)
        {
            loadingPanel = CreateLoadingPanel(canvasParent, out loadingTitleText, out loadingProgressSlider, out loadingStatusText);
            howToPanel = CreateHowToGuidePanel(canvasParent, out howToGuideImage, out howToBackupText, out howToPageText, out howToPreviousButton, out howToNextButton, out howToStartButton);
            closeHowToButton = howToStartButton;
            pausePanel = CreateOverlayPanel(canvasParent, "PausePanel", "Paused", "Take a short break. Resume when ready.", out resumeButton, "Resume");

            GameObject restartButtonGo = CreateOverlayButton((pausePanel.transform.GetChild(0) as RectTransform), "RestartRoundButton", "Restart Round");
            restartButton = restartButtonGo.GetComponent<Button>();

            resultPanel = CreateOverlayPanel(canvasParent, "ResultPanel", "Round Complete", "Score: 0", out resultContinueButton, "Continue");
            RectTransform resultCard = resultPanel.transform.GetChild(0) as RectTransform;
            resultTitleText = resultCard.Find("OverlayTitleText").GetComponent<TMP_Text>();
            resultScoreText = resultCard.Find("OverlayBodyText").GetComponent<TMP_Text>();
            resultMessageText = CreateText(resultCard, "ResultMessageText", "Great job!", 24, TextAlignmentOptions.Center, DarkText, FontStyles.Normal);

            GameObject playAgainGo = CreateOverlayButton(resultCard, "PlayAgainLocalButton", "Play Again");
            resultRestartButton = playAgainGo.GetComponent<Button>();

            loadingPanel.SetActive(false);
            howToPanel.SetActive(false);
            pausePanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        private static GameObject CreateLoadingPanel(Transform canvasParent, out TMP_Text loadingTitleText, out Slider loadingProgressSlider, out TMP_Text loadingStatusText)
        {
            GameObject overlay = CreateUIObject("LoadingPanel", canvasParent);
            SetStretch(overlay.transform as RectTransform);
            Image overlayBg = overlay.AddComponent<Image>();
            overlayBg.color = new Color32(14, 12, 36, 240);
            overlay.AddComponent<CanvasGroup>();

            GameObject card = CreatePanel("LoadingCard", overlay.transform, new Color32(255, 255, 255, 245));
            RectTransform cardRect = card.transform as RectTransform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(700, 340);
            cardRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(54, 54, 48, 48);
            layout.spacing = 28;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            loadingTitleText = CreateText(card.transform, "LoadingGameTitleText", "Guess Who I Am", 58, TextAlignmentOptions.Center, DarkText, FontStyles.Bold);
            LayoutElement titleLayout = loadingTitleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 82;

            GameObject sliderGo = CreateUIObject("LoadingProgressSlider_UserSlider", card.transform);
            LayoutElement sliderLayout = sliderGo.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 34;
            sliderLayout.preferredWidth = 560;
            loadingProgressSlider = sliderGo.AddComponent<Slider>();
            loadingProgressSlider.minValue = 0f;
            loadingProgressSlider.maxValue = 1f;
            loadingProgressSlider.value = 0f;
            loadingProgressSlider.interactable = false;

            RectTransform sliderRect = sliderGo.transform as RectTransform;
            sliderRect.sizeDelta = new Vector2(560, 34);

            GameObject bg = CreatePanel("Background", sliderGo.transform, new Color32(65, 52, 96, 255));
            SetStretch(bg.transform as RectTransform);

            GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform);
            RectTransform fillAreaRect = fillArea.transform as RectTransform;
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(5, 5);
            fillAreaRect.offsetMax = new Vector2(-5, -5);

            GameObject fill = CreatePanel("Fill", fillArea.transform, AccentYellow);
            Image fillImage = fill.GetComponent<Image>();
            RectTransform fillRect = fill.transform as RectTransform;
            SetStretch(fillRect);
            loadingProgressSlider.fillRect = fillRect;
            loadingProgressSlider.targetGraphic = fillImage;

            loadingStatusText = CreateText(card.transform, "LoadingStatusText", "Loading...", 30, TextAlignmentOptions.Center, DarkText, FontStyles.Bold);
            LayoutElement statusLayout = loadingStatusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 50;

            return overlay;
        }

        private static GameObject CreateHowToGuidePanel(Transform canvasParent, out Image guideImage, out TMP_Text backupText, out TMP_Text pageText, out Button previousButton, out Button nextButton, out Button startButton)
        {
            GameObject overlay = CreateUIObject("HowToPlayPanel", canvasParent);
            SetStretch(overlay.transform as RectTransform);
            Image overlayBg = overlay.AddComponent<Image>();
            overlayBg.color = new Color32(0, 0, 0, 165);
            overlay.AddComponent<CanvasGroup>();

            GameObject card = CreatePanel("HowToGuideCard", overlay.transform, new Color32(255, 255, 255, 255));
            RectTransform cardRect = card.transform as RectTransform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(980, 690);
            cardRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(44, 44, 36, 36);
            cardLayout.spacing = 18;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            TMP_Text titleText = CreateText(card.transform, "HowToTitleText", "How To Play", 44, TextAlignmentOptions.Center, DarkText, FontStyles.Bold);
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 58;

            GameObject imageFrame = CreatePanel("HowToGuideImageFrame", card.transform, new Color32(241, 238, 248, 255));
            LayoutElement imageLayout = imageFrame.AddComponent<LayoutElement>();
            imageLayout.preferredHeight = 410;
            imageLayout.flexibleHeight = 1;

            guideImage = CreatePanel("HowToGuideMainImage_ReplaceSpriteHere", imageFrame.transform, Color.white).GetComponent<Image>();
            guideImage.preserveAspect = true;
            guideImage.raycastTarget = false;
            SetStretch(guideImage.rectTransform);

            backupText = CreateText(imageFrame.transform, "HowToBackupText", "Read clue 1 and choose the correct answer. Reveal more clues only when needed. Fewer clues means more points.", 30, TextAlignmentOptions.Center, DarkText, FontStyles.Normal);
            backupText.enableWordWrapping = true;
            SetStretch(backupText.rectTransform, 48, 48, 42, 42);
            guideImage.gameObject.SetActive(false);
            backupText.gameObject.SetActive(true);

            pageText = CreateText(card.transform, "HowToPageText", "", 22, TextAlignmentOptions.Center, DarkText, FontStyles.Bold);
            LayoutElement pageLayout = pageText.gameObject.AddComponent<LayoutElement>();
            pageLayout.preferredHeight = 28;
            pageText.gameObject.SetActive(false);

            GameObject buttonRow = CreateUIObject("HowToNavigationRow", card.transform);
            LayoutElement rowLayout = buttonRow.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 80;
            HorizontalLayoutGroup rowGroup = buttonRow.AddComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 18;
            rowGroup.childAlignment = TextAnchor.MiddleCenter;
            rowGroup.childControlWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = false;

            previousButton = CreateOverlayButton(buttonRow.transform, "HowToPreviousButton", "Previous").GetComponent<Button>();
            nextButton = CreateOverlayButton(buttonRow.transform, "HowToNextButton", "Next").GetComponent<Button>();
            startButton = CreateOverlayButton(buttonRow.transform, "HowToStartButton", "Continue").GetComponent<Button>();

            previousButton.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);

            return overlay;
        }

        private static GameObject CreateOverlayPanel(Transform canvasParent, string name, string title, string body, out Button primaryButton, string buttonText)
        {
            GameObject overlay = CreateUIObject(name, canvasParent);
            SetStretch(overlay.transform as RectTransform);
            Image overlayBg = overlay.AddComponent<Image>();
            overlayBg.color = new Color32(0, 0, 0, 150);
            overlay.AddComponent<CanvasGroup>();

            GameObject card = CreatePanel("OverlayCard", overlay.transform, new Color32(255, 255, 255, 255));
            RectTransform cardRect = card.transform as RectTransform;
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(620, 470);
            cardRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(44, 44, 40, 40);
            cardLayout.spacing = 18;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandWidth = true;
            cardLayout.childForceExpandHeight = false;

            TMP_Text titleText = CreateText(card.transform, "OverlayTitleText", title, 42, TextAlignmentOptions.Center, DarkText, FontStyles.Bold);
            LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 60;

            TMP_Text bodyText = CreateText(card.transform, "OverlayBodyText", body, 28, TextAlignmentOptions.Center, DarkText, FontStyles.Normal);
            bodyText.enableWordWrapping = true;
            LayoutElement bodyLayout = bodyText.gameObject.AddComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1;

            GameObject buttonGo = CreateOverlayButton(card.transform, "PrimaryOverlayButton", buttonText);
            primaryButton = buttonGo.GetComponent<Button>();
            return overlay;
        }

        private static GameObject CreateOverlayButton(Transform parent, string name, string text)
        {
            GameObject buttonGo = CreatePanel(name, parent, AccentBlue);
            LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
            layout.minHeight = 66;
            layout.preferredHeight = 74;
            layout.preferredWidth = 300;

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonGo.GetComponent<Image>();

            TMP_Text label = CreateText(buttonGo.transform, "ButtonText", text, 26, TextAlignmentOptions.Center, LightText, FontStyles.Bold);
            SetStretch(label.rectTransform);
            return buttonGo;
        }

        private static Button CreateSmallHudButton(Transform parent, string name, string label)
        {
            GameObject buttonGo = CreatePanel(name, parent, new Color32(255, 255, 255, 35));
            LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
            layout.minWidth = 58;
            layout.preferredWidth = 62;
            layout.minHeight = 50;
            layout.preferredHeight = 54;

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonGo.GetComponent<Image>();

            GameObject icon = CreatePanel(name + "WhiteIconPlaceholder_ReplaceSpriteHere", buttonGo.transform, Color.white);
            RectTransform iconRect = icon.transform as RectTransform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(34, 34);
            iconRect.anchoredPosition = Vector2.zero;
            Image iconImage = icon.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            return button;
        }

        private static void WireManager(
            GuessWhoIAmOptionGameManager manager,
            GuessWhoQuestionDatabase database,
            TMP_Text scoreText,
            TMP_Text coinText,
            TMP_Text questionProgressText,
            Image progressFill,
            List<Image> progressMarkers,
            List<GuessWhoIAmOptionGameManager.GuessWhoClueCardUI> clueCards,
            List<GuessWhoIAmOptionGameManager.GuessWhoOptionButtonUI> optionButtons,
            CanvasGroup guideBubbleGroup,
            TMP_Text guideText,
            Button revealButton,
            TMP_Text revealMainText,
            TMP_Text revealSubText,
            Button nextButton,
            TMP_Text nextButtonText,
            Slider nextSlider,
            Button pauseButton,
            Button helpButton,
            Button resumeButton,
            Button restartButton,
            Button closeHowToButton,
            Button resultRestartButton,
            Button resultContinueButton,
            GameObject loadingPanel,
            GameObject howToPanel,
            GameObject pausePanel,
            GameObject resultPanel,
            TMP_Text resultTitleText,
            TMP_Text resultScoreText,
            TMP_Text resultMessageText,
            TMP_Text loadingTitleText,
            Slider loadingProgressSlider,
            TMP_Text loadingStatusText,
            Image howToGuideImage,
            TMP_Text howToBackupText,
            TMP_Text howToPageText,
            Button howToPreviousButton,
            Button howToNextButton,
            Button howToStartButton,
            GuessWhoIAmAudioManager audioManager)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("questionDatabase").objectReferenceValue = database;
            so.FindProperty("roundQuestionCount").intValue = 10;
            so.FindProperty("optionCount").intValue = 4;
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("coinText").objectReferenceValue = coinText;
            so.FindProperty("questionProgressText").objectReferenceValue = questionProgressText;
            so.FindProperty("progressFillImage").objectReferenceValue = progressFill;
            so.FindProperty("guideBubbleCanvasGroup").objectReferenceValue = guideBubbleGroup;
            so.FindProperty("guideMessageText").objectReferenceValue = guideText;
            so.FindProperty("revealButton").objectReferenceValue = revealButton;
            so.FindProperty("revealButtonMainText").objectReferenceValue = revealMainText;
            so.FindProperty("revealButtonSubText").objectReferenceValue = revealSubText;
            so.FindProperty("nextButton").objectReferenceValue = nextButton;
            so.FindProperty("nextButtonText").objectReferenceValue = nextButtonText;
            so.FindProperty("nextButtonProgressSlider").objectReferenceValue = nextSlider;
            so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            so.FindProperty("helpButton").objectReferenceValue = helpButton;
            so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            so.FindProperty("restartButton").objectReferenceValue = restartButton;
            so.FindProperty("closeHowToButton").objectReferenceValue = closeHowToButton;
            so.FindProperty("resultRestartButton").objectReferenceValue = resultRestartButton;
            so.FindProperty("resultContinueButton").objectReferenceValue = resultContinueButton;
            so.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;
            so.FindProperty("howToPlayPanel").objectReferenceValue = howToPanel;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
            so.FindProperty("resultTitleText").objectReferenceValue = resultTitleText;
            so.FindProperty("resultScoreText").objectReferenceValue = resultScoreText;
            so.FindProperty("resultMessageText").objectReferenceValue = resultMessageText;
            so.FindProperty("loadingTitleText").objectReferenceValue = loadingTitleText;
            so.FindProperty("loadingProgressSlider").objectReferenceValue = loadingProgressSlider;
            so.FindProperty("loadingStatusText").objectReferenceValue = loadingStatusText;
            so.FindProperty("howToGuideImage").objectReferenceValue = howToGuideImage;
            so.FindProperty("howToBackupText").objectReferenceValue = howToBackupText;
            so.FindProperty("howToPageText").objectReferenceValue = howToPageText;
            so.FindProperty("howToPreviousButton").objectReferenceValue = howToPreviousButton;
            so.FindProperty("howToNextButton").objectReferenceValue = howToNextButton;
            so.FindProperty("howToStartButton").objectReferenceValue = howToStartButton;
            so.FindProperty("audioManager").objectReferenceValue = audioManager;

            SerializedProperty markerProp = so.FindProperty("progressStepMarkers");
            markerProp.arraySize = progressMarkers.Count;
            for (int i = 0; i < progressMarkers.Count; i++)
                markerProp.GetArrayElementAtIndex(i).objectReferenceValue = progressMarkers[i];

            SerializedProperty clueProp = so.FindProperty("clueCards");
            clueProp.arraySize = clueCards.Count;
            for (int i = 0; i < clueCards.Count; i++)
            {
                SerializedProperty element = clueProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = clueCards[i].root;
                element.FindPropertyRelative("button").objectReferenceValue = clueCards[i].button;
                element.FindPropertyRelative("background").objectReferenceValue = clueCards[i].background;
                element.FindPropertyRelative("canvasGroup").objectReferenceValue = clueCards[i].canvasGroup;
                element.FindPropertyRelative("layoutElement").objectReferenceValue = clueCards[i].layoutElement;
                element.FindPropertyRelative("badgeText").objectReferenceValue = clueCards[i].badgeText;
                element.FindPropertyRelative("badgeBackground").objectReferenceValue = clueCards[i].badgeBackground;
                element.FindPropertyRelative("outline").objectReferenceValue = clueCards[i].outline;
                element.FindPropertyRelative("titleText").objectReferenceValue = clueCards[i].titleText;
                element.FindPropertyRelative("clueText").objectReferenceValue = clueCards[i].clueText;
                element.FindPropertyRelative("valueChipText").objectReferenceValue = clueCards[i].valueChipText;
                element.FindPropertyRelative("valueChipBackground").objectReferenceValue = clueCards[i].valueChipBackground;
                element.FindPropertyRelative("selectedPointer").objectReferenceValue = clueCards[i].selectedPointer;
                element.FindPropertyRelative("lockIconBackground").objectReferenceValue = clueCards[i].lockIconBackground;
                element.FindPropertyRelative("lockIconText").objectReferenceValue = clueCards[i].lockIconText;
            }

            SerializedProperty optionProp = so.FindProperty("optionButtons");
            optionProp.arraySize = optionButtons.Count;
            for (int i = 0; i < optionButtons.Count; i++)
            {
                SerializedProperty element = optionProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = optionButtons[i].root;
                element.FindPropertyRelative("button").objectReferenceValue = optionButtons[i].button;
                element.FindPropertyRelative("background").objectReferenceValue = optionButtons[i].background;
                element.FindPropertyRelative("letterBadgeBackground").objectReferenceValue = optionButtons[i].letterBadgeBackground;
                element.FindPropertyRelative("letterBadgeText").objectReferenceValue = optionButtons[i].letterBadgeText;
                element.FindPropertyRelative("labelText").objectReferenceValue = optionButtons[i].labelText;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static void WireResponsiveLayout(GuessWhoIAmResponsiveLayout responsive, Canvas canvas, RectTransform canvasRect, LayoutElement topBarLayout, LayoutElement rightPanelLayout, LayoutElement clueRowLayout, RectTransform optionsGridRect, LayoutElement optionsGridLayout, GridLayoutGroup optionsGrid, RectTransform clueRowRect, HorizontalLayoutGroup clueRowGroup, List<GuessWhoIAmOptionGameManager.GuessWhoClueCardUI> clueCards)
        {
            SerializedObject so = new SerializedObject(responsive);
            so.FindProperty("canvas").objectReferenceValue = canvas;
            so.FindProperty("canvasRect").objectReferenceValue = canvasRect;
            so.FindProperty("topBarLayout").objectReferenceValue = topBarLayout;
            so.FindProperty("rightMascotPanelLayout").objectReferenceValue = rightPanelLayout;
            so.FindProperty("clueRowLayout").objectReferenceValue = clueRowLayout;
            so.FindProperty("optionsGridRect").objectReferenceValue = optionsGridRect;
            so.FindProperty("optionsGridLayout").objectReferenceValue = optionsGridLayout;
            so.FindProperty("optionsGrid").objectReferenceValue = optionsGrid;
            so.FindProperty("clueRowRect").objectReferenceValue = clueRowRect;
            so.FindProperty("clueRowGroup").objectReferenceValue = clueRowGroup;

            SerializedProperty cardLayouts = so.FindProperty("clueCardLayoutElements");
            cardLayouts.arraySize = clueCards.Count;
            for (int i = 0; i < clueCards.Count; i++)
                cardLayouts.GetArrayElementAtIndex(i).objectReferenceValue = clueCards[i].layoutElement;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(responsive);
        }

        private static Sprite GetOrCreateRoundedRectSprite()
        {
            if (cachedRoundedRectSprite != null)
                return cachedRoundedRectSprite;

            EnsureFolder("Assets/GuessWhoIAm", "Generated");

            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectSpritePath);
            if (existingSprite != null)
            {
                cachedRoundedRectSprite = existingSprite;
                return cachedRoundedRectSprite;
            }

            const int size = 96;
            const int radius = 20;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "GWI_RoundedRect_Texture";

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x, 0, x - (size - radius - 1));
                    float dy = Mathf.Max(radius - y, 0, y - (size - radius - 1));
                    bool outside = dx * dx + dy * dy > radius * radius;
                    texture.SetPixel(x, y, outside ? new Color(1f, 1f, 1f, 0f) : Color.white);
                }
            }

            texture.Apply();
            File.WriteAllBytes(RoundedRectSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RoundedRectSpritePath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(RoundedRectSpritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = new Vector4(22, 22, 22, 22);
                importer.SaveAndReimport();
            }

            cachedRoundedRectSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RoundedRectSpritePath);
            return cachedRoundedRectSprite;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.transform as RectTransform;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            if (cachedRoundedRectSprite != null)
            {
                image.sprite = cachedRoundedRectSprite;
                image.type = Image.Type.Sliced;
            }

            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static GameObject CreateIconPlaceholder(Transform parent, string name, float size, Color color)
        {
            GameObject icon = CreatePanel(name, parent, color);
            LayoutElement layout = icon.AddComponent<LayoutElement>();
            layout.minWidth = size;
            layout.preferredWidth = size;
            layout.minHeight = size;
            layout.preferredHeight = size;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            Image image = icon.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return icon;
        }

        private static HorizontalLayoutGroup AddHorizontal(GameObject go, int spacing, int padding, TextAnchor alignment)
        {
            HorizontalLayoutGroup group = go.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childAlignment = alignment;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = true;
            return group;
        }

        private static void SetStretch(RectTransform rect)
        {
            SetStretch(rect, 0, 0, 0, 0);
        }

        private static void SetStretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetFixedWidth(GameObject go, float width)
        {
            LayoutElement layout = go.GetComponent<LayoutElement>();
            if (layout == null)
                layout = go.AddComponent<LayoutElement>();

            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.flexibleWidth = 0;
        }
    }
}
