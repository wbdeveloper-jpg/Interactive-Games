#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WordShuffleDragSwap;

namespace WordShuffleDragSwap.EditorTools
{
    public static class WordShuffleSceneBuilder
    {
        private const string RootFolder = "Assets/WordShuffleDragSwap";
        private const string GeneratedFolder = "Assets/WordShuffleDragSwap/Generated";
        private const string DatabasePath = GeneratedFolder + "/WordShuffle_Grade3_4_Default.asset";
        private const string QuestionDatabasePath = GeneratedFolder + "/WordShuffle_GeneralQuestions_Default.asset";
        private const string Grade3VacationCancelletionDatabasePath = GeneratedFolder + "/Grade_3_Vacation Cancelletion.asset";

        private static readonly string[] DefaultGradeWords =
        {
            "apple", "mango", "orange", "banana", "grape", "lemon", "peach", "cherry", "carrot", "potato",
            "tomato", "onion", "pumpkin", "garden", "flower", "forest", "river", "mountain", "planet", "school",
            "pencil", "paper", "eraser", "ruler", "teacher", "student", "lesson", "number", "letter", "story",
            "window", "table", "chair", "button", "pocket", "family", "mother", "father", "sister", "brother",
            "friend", "people", "market", "farmer", "doctor", "nurse", "driver", "pilot", "artist", "camera",
            "rocket", "monkey", "tiger", "rabbit", "horse", "sheep", "zebra", "panda", "turtle", "parrot",
            "dolphin", "kitten", "puppy", "dragon", "castle", "bridge", "island", "village", "ocean", "cloud",
            "rain", "thunder", "winter", "summer", "spring", "yellow", "purple", "silver", "circle", "square",
            "puzzle", "cookie", "butter", "cheese", "bread", "honey", "sugar", "dinner", "bottle", "blanket",
            "pillow", "carpet", "mirror", "candle", "music", "guitar", "soccer", "tennis", "cricket", "ladder"
        };

        private static readonly string[] Grade3VacationCancelletionWords =
        {
            "action", "motion", "station", "nation", "vacation",
            "education", "information", "conversation", "question", "attention",
            "celebration", "invitation", "collection", "direction", "protection",
            "decoration", "population", "operation", "transportation", "donation",
            "pollution", "solution", "tradition", "competition", "condition",
            "position", "addition", "subtraction", "multiplication", "division",
            "observation", "imagination", "creation", "preparation", "organization",
            "communication", "recreation", "construction", "attraction", "instruction",
            "correction", "connection", "selection", "description", "production",
            "explanation", "permission", "admission", "graduation", "constitution"
        };

        private static readonly WordShuffleQuestionEntry[] DefaultGeneralQuestions =
        {
            new WordShuffleQuestionEntry { Question = "Which animal says meow?", Answer = "cat" },
            new WordShuffleQuestionEntry { Question = "What do we drink when we are thirsty?", Answer = "water" },
            new WordShuffleQuestionEntry { Question = "Which planet do we live on?", Answer = "earth" },
            new WordShuffleQuestionEntry { Question = "What color is grass usually?", Answer = "green" },
            new WordShuffleQuestionEntry { Question = "Which fruit is yellow and curved?", Answer = "banana" },
            new WordShuffleQuestionEntry { Question = "What do we use to write in a notebook?", Answer = "pencil" },
            new WordShuffleQuestionEntry { Question = "Which shape has three sides?", Answer = "triangle" },
            new WordShuffleQuestionEntry { Question = "What do bees make?", Answer = "honey" },
            new WordShuffleQuestionEntry { Question = "Which vehicle flies in the sky?", Answer = "plane" },
            new WordShuffleQuestionEntry { Question = "What comes after Monday?", Answer = "tuesday" }
        };

        private const string Grade3VacationCancellationGenralQuestionDatabasePath =
    GeneratedFolder + "/Grade3VacationCancellationGenralQuestion.asset";

        private static readonly WordShuffleQuestionEntry[]
            Grade3VacationCancellationGenralQuestion =
        {
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something you do.",
        Answer = "action"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Moving from one place to another.",
        Answer = "motion"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A place where trains stop.",
        Answer = "station"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A country and its people.",
        Answer = "nation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: One part of something bigger.",
        Answer = "section"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: One choice you can make.",
        Answer = "option"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A cream used on the skin.",
        Answer = "lotion"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A magic drink in a story.",
        Answer = "potion"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: One part of a larger amount.",
        Answer = "portion"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Care taken to stay safe.",
        Answer = "caution"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Words written under a picture.",
        Answer = "caption"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A made-up story.",
        Answer = "fiction"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: The job something does.",
        Answer = "function"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A part of a whole number.",
        Answer = "fraction"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A break from school or work.",
        Answer = "vacation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something you ask.",
        Answer = "question"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Putting numbers together.",
        Answer = "addition"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: The answer to a problem.",
        Answer = "solution"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: The place where something is.",
        Answer = "position"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: The place where something can be found.",
        Answer = "location"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A way two things are connected.",
        Answer = "relation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: What you do after something happens.",
        Answer = "reaction"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Turning around in a circle.",
        Answer = "rotation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something that has been made.",
        Answer = "creation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something given to help others.",
        Answer = "donation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: The way you need to go.",
        Answer = "direction"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Listening and looking carefully.",
        Answer = "attention"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A group of things kept together.",
        Answer = "collection"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: How something is right now.",
        Answer = "condition"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something people have done for many years.",
        Answer = "tradition"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Learning at school.",
        Answer = "education"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Harmful waste in air, water, or land.",
        Answer = "pollution"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Keeping someone or something safe.",
        Answer = "protection"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A message asking someone to come.",
        Answer = "invitation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A happy event for a special day.",
        Answer = "celebration"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something used to make a place look nice.",
        Answer = "decoration"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A medical job done by a doctor.",
        Answer = "operation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: An event where people try to win.",
        Answer = "competition"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something you notice by watching.",
        Answer = "observation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Making pictures or ideas in your mind.",
        Answer = "imagination"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Getting ready for something.",
        Answer = "preparation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Fun activities done in free time.",
        Answer = "recreation"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Something that makes people want to visit.",
        Answer = "attraction"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A step that tells you what to do.",
        Answer = "instruction"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Fixing something that is wrong.",
        Answer = "correction"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A link between two things.",
        Answer = "connection"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A group of things that were chosen.",
        Answer = "selection"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: The making of something.",
        Answer = "production"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: Facts that help you learn something.",
        Answer = "information"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Unscramble the letters to make the word.\nClue: A talk between two or more people.",
        Answer = "conversation"
    }
};

        private const string Grade4NeverLoseHopeQuestionDatabasePath =
    GeneratedFolder + "/Grade4NeverLoseHopeQuestions.asset";

        private static readonly WordShuffleQuestionEntry[]
            Grade4NeverLoseHopeQuestions =
        {
            // Keep the same 35 question entries here
            // Questions based on the textbook
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nHello! Is it an _______ medical service?",
        Answer = "emergency"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nPlease send an _______ to Broadway near Children's Park.",
        Answer = "ambulance"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nMy friend is badly _______.",
        Answer = "injured"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nMy friend's knee is _______.",
        Answer = "bleeding"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nHer _______ is normal.",
        Answer = "pulse"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nHer eyes are open, so she is _______.",
        Answer = "awake"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nMy friend went to the nearby _______.",
        Answer = "pharmacy"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nWe need a gauze _______ for the wound.",
        Answer = "dressing"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nShall I allow her to drink _______?",
        Answer = "water"
    },

    // Additional easy questions
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nA _______ helps sick people.",
        Answer = "doctor"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nA _______ takes care of patients.",
        Answer = "nurse"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nSick people are treated in a _______.",
        Answer = "hospital"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nThe doctor gave me some _______.",
        Answer = "medicine"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nA sick person is called a _______.",
        Answer = "patient"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nCover the cut with a _______.",
        Answer = "bandage"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nThe fall left a _______ on his knee.",
        Answer = "wound"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nThe doctor works at a small _______.",
        Answer = "clinic"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nThe doctor gave me a _______ to swallow.",
        Answer = "tablet"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nThe child took a spoonful of cough _______.",
        Answer = "syrup"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nA high body temperature is called a _______.",
        Answer = "fever"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nA bad _______ can make your throat hurt.",
        Answer = "cough"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nGood food helps us stay _______.",
        Answer = "healthy"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nCall for _______ during an emergency.",
        Answer = "help"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nFollow the rules to stay _______.",
        Answer = "safe"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nA sick person needs plenty of _______.",
        Answer = "rest"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nDoctors take _______ of sick people.",
        Answer = "care"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nThe cut is causing me _______.",
        Answer = "pain"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nMedicine can help the wound _______.",
        Answer = "heal"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nAlways keep the wound _______.",
        Answer = "clean"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nAlways _______ your hands before eating.",
        Answer = "wash"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nUse _______ to wash your hands.",
        Answer = "soap"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nWear a _______ over your nose and mouth.",
        Answer = "mask"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nDoctors may wear _______ on their hands.",
        Answer = "gloves"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nClean the cut with a piece of _______.",
        Answer = "gauze"
    },
    new WordShuffleQuestionEntry
    {
        Question = "Rearrange letters to fill the blank\nThe team came to _______ the injured boy.",
        Answer = "rescue"
    }
        };

        [MenuItem("Tools/Word Shuffle Drag Swap/Create Complete Scene")]
        [MenuItem("Tools/Word Shuffle Drag Swap/Create Complete Scene - V6.2 Professional Overlays")]
        [MenuItem("Tools/Word Shuffle Drag Swap/Create Complete Scene - V7 UI Layout Progress")]
        [MenuItem("Tools/Word Shuffle Drag Swap/Create Complete Scene - V7.1 UI Hint Button Layout")]
        [MenuItem("Tools/Word Shuffle Drag Swap/Create Complete Scene - V7.3 Larger Responsive UI")]
        [MenuItem("Tools/Word Shuffle Drag Swap/Create Complete Scene - V7.4 Dynamic Tile Sizing")]
        [MenuItem("Tools/Word Shuffle Drag Swap/Create Complete Scene - V7.5 Bloom Minimal HUD")]
        public static void CreateCompleteScene()
        {
            EnsureFolders();
            EnsureEventSystem();

            WordShuffleWordDatabase database = CreateOrLoadDatabase();
            WordShuffleQuestionDatabase questionDatabase = CreateOrLoadQuestionDatabase();

            GameObject root = new GameObject("WordShuffleDragSwap_Game");
            Undo.RegisterCreatedObjectUndo(root, "Create Word Shuffle Drag Swap Game");

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler canvasScaler = root.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            AudioSource backgroundMusicSource = root.AddComponent<AudioSource>();
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.loop = true;
            WordShuffleDragSwapManager manager = root.AddComponent<WordShuffleDragSwapManager>();

            GameObject background = CreateFullPanel("Background_EMPTY_REPLACE_ART", root.transform, new Color(1f, 1f, 1f, 0f));
            background.GetComponent<Image>().raycastTarget = false;

            GameObject gamePanel = CreateFullPanel("GamePanel", root.transform, new Color(1f, 1f, 1f, 0f));

            GameObject topHud = CreateAnchoredPanel(
                "TopHUD_Responsive",
                gamePanel.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -28f),
                new Vector2(-96f, 142f),
                new Color(1f, 1f, 1f, 0.06f));

            RectTransform roundCluster = CreateRect("RoundProgressCluster", topHud.transform, new Vector2(-815f, -2f), new Vector2(122f, 122f));
            WordShuffleCircularProgressUI roundProgressBackground = CreateCircularProgress("RoundProgressBackground", roundCluster, 1f, 14f, new Color(1f, 1f, 1f, 0.16f));
            WordShuffleCircularProgressUI roundProgressCircle = CreateCircularProgress("RoundProgressCircle", roundCluster, 0f, 14f, new Color(0.22f, 0.48f, 1f, 1f));
            TextMeshProUGUI roundText = CreateText("RoundText", roundCluster, "1/10", 35, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(108f, 62f));
            TextMeshProUGUI roundLabel = CreateText("RoundLabel", roundCluster, "ROUND", 15, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -36f), new Vector2(108f, 30f));
            roundLabel.color = new Color(1f, 1f, 1f, 0.74f);

            TextMeshProUGUI modeText = null;
            TextMeshProUGUI scoreText = CreateDesignedScoreBlock(topHud.transform, new Vector2(-525f, 0f));

            Image timerFillImage;
            TextMeshProUGUI timerText = CreateDesignedTimerPill(topHud.transform, new Vector2(-110f, 0f), out timerFillImage);

            Button hintButton = CreateDesignedHintButton(topHud.transform, new Vector2(330f, 0f), out TextMeshProUGUI hintCountText);
            Button pauseButton = CreateDesignedPauseButton(topHud.transform, new Vector2(730f, 0f));

            GameObject questionCard = CreatePanel("QuestionCard_EMPTY_ART_HERE", gamePanel.transform, new Vector2(0f, 220f), new Vector2(1480f, 215f), new Color(1f, 1f, 1f, 0.10f));
            TextMeshProUGUI hintText = CreateText("QuestionText", questionCard.transform, "Unscramble the word", 48, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(1370f, 158f));
            hintText.enableAutoSizing = true;
            hintText.fontSizeMin = 28f;
            hintText.fontSizeMax = 48f;

            TextMeshProUGUI instructionText = CreateText("InstructionText", gamePanel.transform, "Drag a tile onto another tile to swap letters", 30, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 72f), new Vector2(1280f, 54f));
            instructionText.color = new Color(1f, 1f, 1f, 0.82f);

            GameObject gameplayCard = CreatePanel("GameplayAreaCard_EMPTY_ART_HERE", gamePanel.transform, new Vector2(0f, -170f), new Vector2(1660f, 440f), new Color(1f, 1f, 1f, 0.06f));
            RectTransform slotParent = CreateRect("SlotParent", gameplayCard.transform, Vector2.zero, new Vector2(1560f, 250f));
            HorizontalLayoutGroup slotLayout = slotParent.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childControlHeight = false;
            slotLayout.childControlWidth = false;
            slotLayout.childForceExpandHeight = false;
            slotLayout.childForceExpandWidth = false;
            slotLayout.spacing = 16f;

            TextMeshProUGUI feedbackText = CreateText("FeedbackText", gamePanel.transform, "Arrange the letters", 40, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, -435f), new Vector2(1280f, 76f));

            Image wordImage = CreateImageBox("OptionalWordImage_EMPTY", gamePanel.transform, new Vector2(790f, 220f), new Vector2(178f, 178f), new Color(1f, 1f, 1f, 0.08f));
            wordImage.enabled = false;

            RectTransform tileLayer = CreateStretchRect("TileLayer", gamePanel.transform);
            RectTransform sceneTemplates = CreateRect("SceneTemplates_DO_NOT_DELETE", gamePanel.transform, new Vector2(0f, -470f), new Vector2(140f, 140f));
            WordShuffleLetterTile tileTemplate = CreateSceneTileTemplate(sceneTemplates);
            sceneTemplates.gameObject.SetActive(false);
            tileLayer.SetAsLastSibling();
            topHud.transform.SetAsLastSibling();

            GameObject startPanel = CreateFullPanel("StartOverlayPanel", root.transform, new Color(0.02f, 0.03f, 0.07f, 0.82f));
            GameObject startCard = CreateCenteredPanel("StartMainCard", startPanel.transform, new Vector2(900f, 560f), new Color(0.07f, 0.1f, 0.2f, 0.98f));
            CreateText("GameTitle", startCard.transform, "WORD SHUFFLE", 64, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 160f), new Vector2(760f, 95f));
            CreateText("GameSubtitle", startCard.transform, "Drag one tile onto another to swap.\nEnglish, Maths, and Question modes use the same mechanic.", 29, FontStyles.Normal, TextAlignmentOptions.Center, new Vector2(0f, 40f), new Vector2(760f, 110f));
            Button startButton = CreateButton("StartButton", startCard.transform, "START", new Vector2(0f, -95f), new Vector2(360f, 86f), new Color(1f, 0.64f, 0.18f, 1f));
            Button howButton = CreateButton("HowToPlayButton", startCard.transform, "HOW TO PLAY", new Vector2(0f, -205f), new Vector2(360f, 76f), new Color(0.22f, 0.45f, 1f, 1f));

            GameObject pausePanel = CreateFullPanel("PauseOverlayPanel", root.transform, new Color(0.02f, 0.03f, 0.07f, 0.78f));
            GameObject pauseCard = CreateCenteredPanel("PauseMainCard", pausePanel.transform, new Vector2(720f, 520f), new Color(0.07f, 0.1f, 0.2f, 0.98f));
            CreateText("PauseTitle", pauseCard.transform, "PAUSED", 56, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 170f), new Vector2(640f, 80f));
            Button resumeButton = CreateButton("ResumeButton", pauseCard.transform, "RESUME", new Vector2(0f, 55f), new Vector2(360f, 76f), new Color(0.25f, 0.66f, 0.32f, 1f));
            Button pauseHowButton = CreateButton("PauseHowToPlayButton", pauseCard.transform, "HOW TO PLAY", new Vector2(0f, -45f), new Vector2(360f, 76f), new Color(0.22f, 0.45f, 1f, 1f));
            Button pauseRestartButton = CreateButton("PauseRestartButton", pauseCard.transform, "RESTART", new Vector2(0f, -145f), new Vector2(360f, 76f), new Color(1f, 0.64f, 0.18f, 1f));

            GameObject howToPlayPanel = CreateFullPanel("HowToPlayOverlayPanel", root.transform, new Color(0.02f, 0.03f, 0.07f, 0.82f));
            GameObject howToPlayCard = CreateCenteredPanel("HowToPlayMainCard", howToPlayPanel.transform, new Vector2(980f, 620f), new Color(0.07f, 0.1f, 0.2f, 0.98f));
            CreateText("HowTitle", howToPlayCard.transform, "HOW TO PLAY", 52, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 225f), new Vector2(860f, 80f));
            CreateText("HowBody", howToPlayCard.transform, "1. Read the word or question.\n2. Drag one tile onto another tile.\n3. The two tiles swap with a snap animation.\n4. Complete the correct answer before time ends.\n5. Use hints to lock helpful tiles.", 30, FontStyles.Normal, TextAlignmentOptions.Left, new Vector2(0f, 35f), new Vector2(800f, 310f));
            Button closeHowButton = CreateButton("CloseHowButton", howToPlayCard.transform, "CONTINUE", new Vector2(0f, -235f), new Vector2(360f, 76f), new Color(1f, 0.64f, 0.18f, 1f));

            GameObject resultPanel = CreateFullPanel("ResultOverlayPanel", root.transform, new Color(0.02f, 0.03f, 0.07f, 0.78f));
            GameObject resultCard = CreateCenteredPanel("ResultMainCard", resultPanel.transform, new Vector2(820f, 560f), new Color(0.07f, 0.1f, 0.2f, 0.98f));
            TextMeshProUGUI resultTitleText = CreateText("ResultTitleText", resultCard.transform, "Great Job!", 66, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 175f), new Vector2(720f, 90f));
            TextMeshProUGUI resultScoreText = CreateText("ResultScoreText", resultCard.transform, "Score: 0", 46, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(0f, 55f), new Vector2(720f, 75f));
            Button resultRestartButton = CreateButton("ResultRestartButton", resultCard.transform, "PLAY AGAIN", new Vector2(-195f, -125f), new Vector2(330f, 82f), new Color(1f, 0.64f, 0.18f, 1f));
            Button resultContinueButton = CreateButton("ResultContinueButton_Future", resultCard.transform, "CONTINUE", new Vector2(195f, -125f), new Vector2(330f, 82f), new Color(0.22f, 0.45f, 1f, 1f));

            AssignManagerReferences(
                manager,
                database,
                questionDatabase,
                tileTemplate,
                canvas,
                audioSource,
                backgroundMusicSource,
                tileLayer,
                slotParent,
                roundText,
                scoreText,
                timerText,
                hintText,
                hintCountText,
                feedbackText,
                resultTitleText,
                resultScoreText,
                wordImage,
                roundProgressCircle,
                null,
                timerFillImage,
                instructionText,
                modeText,
                startPanel,
                gamePanel,
                resultPanel,
                pausePanel,
                howToPlayPanel,
                startButton,
                resultRestartButton,
                hintButton,
                pauseButton,
                resumeButton,
                howButton,
                pauseHowButton,
                closeHowButton,
                resultContinueButton);

            UnityEventTools.AddPersistentListener(pauseRestartButton.onClick, manager.RestartGame);
            EditorUtility.SetDirty(pauseRestartButton);

            startPanel.SetActive(true);
            gamePanel.SetActive(true);
            resultPanel.SetActive(false);
            pausePanel.SetActive(false);
            howToPlayPanel.SetActive(false);

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("Word Shuffle Drag Swap V7.5 Bloom Minimal HUD scene created. Mode badge and timer fill are removed, BGM slot is wired, Bloom pre/post flow is integrated, and core mechanics stay unchanged.");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
                AssetDatabase.CreateFolder("Assets", "WordShuffleDragSwap");

            if (!AssetDatabase.IsValidFolder(GeneratedFolder))
                AssetDatabase.CreateFolder(RootFolder, "Generated");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
        }

        private static WordShuffleWordDatabase CreateOrLoadDatabase()
        {
            WordShuffleWordDatabase database = AssetDatabase.LoadAssetAtPath<WordShuffleWordDatabase>(DatabasePath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<WordShuffleWordDatabase>();
                database.EditorSetWords(DefaultGradeWords);
                AssetDatabase.CreateAsset(database, DatabasePath);
            }
            else if (database.Words == null || database.Words.Count == 0)
            {
                database.EditorSetWords(DefaultGradeWords);
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            return database;
        }

        private static WordShuffleQuestionDatabase CreateOrLoadQuestionDatabase()
        {
            WordShuffleQuestionDatabase database = AssetDatabase.LoadAssetAtPath<WordShuffleQuestionDatabase>(QuestionDatabasePath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<WordShuffleQuestionDatabase>();
                database.EditorSetQuestions(DefaultGeneralQuestions);
                AssetDatabase.CreateAsset(database, QuestionDatabasePath);
            }
            else if (database.Questions == null || database.Questions.Count == 0)
            {
                database.EditorSetQuestions(DefaultGeneralQuestions);
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            return database;
        }

        private static WordShuffleLetterTile CreateSceneTileTemplate(Transform parent)
        {
            GameObject tileObject = new GameObject("LetterTileSceneTemplate_DO_NOT_DELETE", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(WordShuffleLetterTile));
            tileObject.transform.SetParent(parent, false);

            RectTransform tileRect = tileObject.GetComponent<RectTransform>();
            tileRect.anchorMin = new Vector2(0.5f, 0.5f);
            tileRect.anchorMax = new Vector2(0.5f, 0.5f);
            tileRect.pivot = new Vector2(0.5f, 0.5f);
            tileRect.anchoredPosition = Vector2.zero;
            tileRect.sizeDelta = new Vector2(118f, 118f);

            Image tileImage = tileObject.GetComponent<Image>();
            tileImage.color = new Color(1f, 0.64f, 0.18f, 1f);
            tileImage.raycastTarget = true;

            CanvasGroup canvasGroup = tileObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            TextMeshProUGUI letterText = CreateText("LetterText", tileObject.transform, "A", 66, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110f, 110f));
            letterText.color = new Color(0.1f, 0.08f, 0.04f, 1f);

            WordShuffleLetterTile tile = tileObject.GetComponent<WordShuffleLetterTile>();
            SerializedObject serializedTile = new SerializedObject(tile);
            SetObject(serializedTile, "letterText", letterText);
            SetObject(serializedTile, "tileImage", tileImage);
            SetObject(serializedTile, "canvasGroup", canvasGroup);
            serializedTile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tileObject);

            return tile;
        }

        private static void AssignManagerReferences(
            WordShuffleDragSwapManager manager,
            WordShuffleWordDatabase database,
            WordShuffleQuestionDatabase questionDatabase,
            WordShuffleLetterTile tileTemplate,
            Canvas canvas,
            AudioSource audioSource,
            AudioSource backgroundMusicSource,
            RectTransform tileLayer,
            RectTransform slotParent,
            TextMeshProUGUI roundText,
            TextMeshProUGUI scoreText,
            TextMeshProUGUI timerText,
            TextMeshProUGUI hintText,
            TextMeshProUGUI hintCountText,
            TextMeshProUGUI feedbackText,
            TextMeshProUGUI resultTitleText,
            TextMeshProUGUI resultScoreText,
            Image wordImage,
            WordShuffleCircularProgressUI roundProgressCircle,
            WordShuffleCircularProgressUI timerProgressCircle,
            Image timerFillImage,
            TextMeshProUGUI instructionText,
            TextMeshProUGUI modeText,
            GameObject startPanel,
            GameObject gamePanel,
            GameObject resultPanel,
            GameObject pausePanel,
            GameObject howToPlayPanel,
            Button startButton,
            Button restartButton,
            Button hintButton,
            Button pauseButton,
            Button resumeButton,
            Button howToPlayButton,
            Button pauseHowToPlayButton,
            Button closeHowToPlayButton,
            Button resultContinueButton)
        {
            SerializedObject serializedManager = new SerializedObject(manager);

            SetObject(serializedManager, "wordDatabase", database);
            SetObject(serializedManager, "questionDatabase", questionDatabase);
            SetObject(serializedManager, "letterTileTemplate", tileTemplate);
            SetObject(serializedManager, "rootCanvas", canvas);
            SetObject(serializedManager, "audioSource", audioSource);
            SetObject(serializedManager, "backgroundMusicSource", backgroundMusicSource);
            SetObject(serializedManager, "tileLayer", tileLayer);
            SetObject(serializedManager, "slotParent", slotParent);
            SetObject(serializedManager, "roundText", roundText);
            SetObject(serializedManager, "scoreText", scoreText);
            SetObject(serializedManager, "timerText", timerText);
            SetObject(serializedManager, "hintText", hintText);
            SetObject(serializedManager, "hintCountText", hintCountText);
            SetObject(serializedManager, "feedbackText", feedbackText);
            SetObject(serializedManager, "resultTitleText", resultTitleText);
            SetObject(serializedManager, "resultScoreText", resultScoreText);
            SetObject(serializedManager, "wordImage", wordImage);
            SetObject(serializedManager, "roundProgressCircle", roundProgressCircle);
            SetObject(serializedManager, "timerProgressCircle", timerProgressCircle);
            SetObject(serializedManager, "timerFillImage", timerFillImage);
            SetObject(serializedManager, "instructionText", instructionText);
            SetObject(serializedManager, "modeText", modeText);
            SetObject(serializedManager, "startPanel", startPanel);
            SetObject(serializedManager, "gamePanel", gamePanel);
            SetObject(serializedManager, "resultPanel", resultPanel);
            SetObject(serializedManager, "pausePanel", pausePanel);
            SetObject(serializedManager, "howToPlayPanel", howToPlayPanel);
            SetObject(serializedManager, "startButton", startButton);
            SetObject(serializedManager, "restartButton", restartButton);
            SetObject(serializedManager, "hintButton", hintButton);
            SetObject(serializedManager, "pauseButton", pauseButton);
            SetObject(serializedManager, "resumeButton", resumeButton);
            SetObject(serializedManager, "howToPlayButton", howToPlayButton);
            SetObject(serializedManager, "pauseHowToPlayButton", pauseHowToPlayButton);
            SetObject(serializedManager, "closeHowToPlayButton", closeHowToPlayButton);
            SetObject(serializedManager, "resultContinueButton", resultContinueButton);

            SetInt(serializedManager, "roundsPerGame", 10);
            SetInt(serializedManager, "maxWordLength", 14);
            SetBool(serializedManager, "autoStartOnPlay", false);
            SetBool(serializedManager, "showModeBadge", false);
            SetBool(serializedManager, "showTimerProgressVisuals", false);
            SetBool(serializedManager, "useBloomRewardSystem", true);
            SetBool(serializedManager, "showHowToPlayBeforeGameplay", true);
            SetBool(serializedManager, "useTimer", true);
            SetFloat(serializedManager, "timePerRound", 45f);
            SetBool(serializedManager, "revealAnswerOnTimeout", true);
            SetFloat(serializedManager, "timeoutRevealMoveDuration", 0.48f);
            SetFloat(serializedManager, "timeoutRevealHoldDuration", 0.85f);
            SetFloat(serializedManager, "timeoutRevealPunchScale", 0.14f);
            SetFloat(serializedManager, "spawnDuration", 0.28f);
            SetFloat(serializedManager, "snapDuration", 0.22f);
            SetFloat(serializedManager, "swapDuration", 0.26f);
            SetFloat(serializedManager, "slotSpacing", 16f);
            SetVector2(serializedManager, "slotSize", new Vector2(118f, 118f));
            SetInt(serializedManager, "maxHintsPerGame", 3);
            SetBool(serializedManager, "lockHintedLetters", true);
            SetBool(serializedManager, "compactHintCountText", true);
            SetBool(serializedManager, "compactScoreText", true);
            SetBool(serializedManager, "compactTimerText", true);
            SetFloat(serializedManager, "hintMoveDuration", 0.42f);
            SetInt(serializedManager, "mathMinDigitLength", 4);
            SetInt(serializedManager, "mathMaxDigitLength", 5);
            SetBool(serializedManager, "mathEnforceDigitRepeatLimit", true);
            SetBool(serializedManager, "applyGlobalFontsOnAwake", true);
            SetBool(serializedManager, "hideTileTemplateOnPlay", true);
            SetFloat(serializedManager, "horizontalSafePadding", 48f);
            SetFloat(serializedManager, "minimumAutoFitTileWidth", 68f);
            SetBool(serializedManager, "useDynamicTileSizingByAnswerLength", true);
            SetFloat(serializedManager, "minDynamicTileSize", 76f);
            SetFloat(serializedManager, "maxDynamicTileSize", 172f);
            SetInt(serializedManager, "shortAnswerLargeTileThreshold", 5);
            SetInt(serializedManager, "longAnswerSmallTileThreshold", 14);
            SetFloat(serializedManager, "shortAnswerSpacing", 26f);
            SetFloat(serializedManager, "longAnswerSpacing", 8f);
            SetFloat(serializedManager, "dynamicTileTextFontRatio", 0.48f);

            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static RectTransform CreateStretchRect(string name, Transform parent)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);

            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);

            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchoredPosition, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private static GameObject CreateAnchoredPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private static GameObject CreateFullPanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            image.color = color;
            return panel;
        }

        private static GameObject CreateCenteredPanel(string name, Transform parent, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect.gameObject;
        }

        private static WordShuffleCircularProgressUI CreateFilledCircle(string name, Transform parent, Vector2 anchoredPosition, float size, Color color)
        {
            GameObject circleObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(WordShuffleCircularProgressUI));
            circleObject.transform.SetParent(parent, false);

            RectTransform rect = circleObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(size, size);

            WordShuffleCircularProgressUI circle = circleObject.GetComponent<WordShuffleCircularProgressUI>();
            circle.color = color;
            circle.Thickness = size * 0.5f;
            circle.SetProgress(1f, false);
            circle.raycastTarget = false;
            return circle;
        }

        private static WordShuffleCircularProgressUI CreateCircularProgress(string name, RectTransform parent, float fillAmount, float thickness, Color color)
        {
            GameObject progressObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(WordShuffleCircularProgressUI));
            progressObject.transform.SetParent(parent, false);

            RectTransform rect = progressObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            Vector2 progressSize = parent.sizeDelta.sqrMagnitude > 0.01f ? parent.sizeDelta : new Vector2(104f, 104f);
            rect.sizeDelta = progressSize;

            WordShuffleCircularProgressUI progress = progressObject.GetComponent<WordShuffleCircularProgressUI>();
            progress.color = color;
            progress.Thickness = thickness;
            progress.SetProgress(fillAmount, false);
            progress.raycastTarget = false;
            return progress;
        }

        private static TextMeshProUGUI CreateBadgeText(string name, Transform parent, string text, Vector2 anchoredPosition, Vector2 size, Color bgColor, int fontSize)
        {
            GameObject badge = CreatePanel(name + "Card", parent, anchoredPosition, size, bgColor);
            TextMeshProUGUI label = CreateText(name, badge.transform, text, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, size);
            return label;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int fontSize, FontStyles style, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            return label;
        }

        private static TextMeshProUGUI CreateModeBadge(Transform parent, Vector2 anchoredPosition)
        {
            GameObject badge = CreatePanel("ModeBadgeCard", parent, anchoredPosition, new Vector2(250f, 72f), new Color(0.90f, 0.95f, 1f, 0.92f));
            TextMeshProUGUI label = CreateText("ModeBadgeText", badge.transform, "English Mode", 25, FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(226f, 54f));
            label.color = new Color(0.08f, 0.24f, 0.52f, 1f);
            return label;
        }

        private static TextMeshProUGUI CreateDesignedScoreBlock(Transform parent, Vector2 anchoredPosition)
        {
            RectTransform root = CreateRect("ScoreBlock_Designed", parent, anchoredPosition, new Vector2(300f, 112f));

            WordShuffleCircularProgressUI iconCircle = CreateFilledCircle("ScoreIcon_EMPTY_REPLACE_TROPHY", root, new Vector2(-108f, 0f), 82f, new Color(1f, 0.93f, 0.72f, 1f));
            iconCircle.raycastTarget = false;

            TextMeshProUGUI label = CreateText("ScoreLabelText", root, "Score", 25, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(20f, 26f), new Vector2(180f, 38f));
            label.color = new Color(0.08f, 0.24f, 0.52f, 1f);

            TextMeshProUGUI value = CreateText("ScoreText", root, "0", 49, FontStyles.Bold, TextAlignmentOptions.Left, new Vector2(20f, -18f), new Vector2(176f, 60f));
            value.color = new Color(0.05f, 0.12f, 0.28f, 1f);

            WordShuffleCircularProgressUI starPlaceholder = CreateFilledCircle("ScoreStar_EMPTY_REPLACE_ICON", root, new Vector2(148f, -18f), 32f, new Color(1f, 0.78f, 0.12f, 1f));
            starPlaceholder.raycastTarget = false;
            return value;
        }

        private static TextMeshProUGUI CreateDesignedTimerPill(Transform parent, Vector2 anchoredPosition, out Image timerFillImage)
        {
            timerFillImage = null;
            GameObject timerCard = CreatePanel("TimerPill_Designed", parent, anchoredPosition, new Vector2(350f, 88f), new Color(0.11f, 0.50f, 0.93f, 1f));

            WordShuffleCircularProgressUI iconSlot = CreateFilledCircle("TimerIcon_EMPTY_REPLACE_CLOCK", timerCard.transform, new Vector2(-116f, 0f), 64f, new Color(1f, 1f, 1f, 0.24f));
            iconSlot.raycastTarget = false;

            TextMeshProUGUI timerText = CreateText("TimerText", timerCard.transform, "00:45", 52, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(42f, 0f), new Vector2(206f, 68f));
            timerText.color = Color.white;
            return timerText;
        }

        private static Button CreateDesignedPauseButton(Transform parent, Vector2 anchoredPosition)
        {
            GameObject buttonObject = new GameObject("PauseButton_Designed", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(92f, 88f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.86f, 0.91f, 0.98f, 1f);
            colors.selectedColor = image.color;
            button.colors = colors;

            CreateImageBox("PauseBarLeft", buttonObject.transform, new Vector2(-12f, 0f), new Vector2(10f, 42f), new Color(0.05f, 0.19f, 0.45f, 1f));
            CreateImageBox("PauseBarRight", buttonObject.transform, new Vector2(12f, 0f), new Vector2(10f, 42f), new Color(0.05f, 0.19f, 0.45f, 1f));
            return button;
        }

        private static Button CreateDesignedHintButton(Transform parent, Vector2 anchoredPosition, out TextMeshProUGUI hintCountText)
        {
            GameObject buttonObject = new GameObject("HintButton_Designed", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(330f, 88f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.78f, 0.94f, 0.82f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.86f, 1f, 0.89f, 1f);
            colors.pressedColor = new Color(0.64f, 0.86f, 0.70f, 1f);
            colors.selectedColor = image.color;
            button.colors = colors;

            WordShuffleCircularProgressUI iconSlot = CreateFilledCircle("HintIcon_EMPTY_REPLACE_ICON", buttonObject.transform, new Vector2(-122f, 0f), 58f, new Color(1f, 0.92f, 0.36f, 0.45f));
            iconSlot.raycastTarget = false;

            TextMeshProUGUI label = CreateText("HintLabelText", buttonObject.transform, "Hints", 34, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(-16f, 0f), new Vector2(142f, 54f));
            label.color = new Color(0.05f, 0.44f, 0.19f, 1f);

            WordShuffleCircularProgressUI countBadge = CreateFilledCircle("HintCountBadge", buttonObject.transform, new Vector2(126f, 0f), 60f, new Color(0.10f, 0.65f, 0.25f, 1f));
            countBadge.raycastTarget = false;
            hintCountText = CreateText("HintCountText", buttonObject.transform, "3", 32, FontStyles.Bold, TextAlignmentOptions.Center, new Vector2(126f, 0f), new Vector2(58f, 58f));
            hintCountText.color = Color.white;

            return button;
        }

        private static Button CreateButton(string name, Transform parent, string labelText, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = color * 1.12f;
            colors.pressedColor = color * 0.86f;
            colors.selectedColor = color;
            button.colors = colors;

            TextMeshProUGUI label = CreateText("Label", buttonObject.transform, labelText, Mathf.RoundToInt(Mathf.Min(size.y * 0.48f, 34f)), FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, size);
            label.color = Color.white;

            return button;
        }

        private static Image CreateImageBox(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        private static void SetVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.vector2Value = value;
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        [MenuItem("Tools/Word Shuffle Drag Swap/Create Grade 3 Vacation Cancelletion Database")]
        public static void CreateGrade3VacationCancelletionDatabase()
        {
            EnsureFolders();

            WordShuffleWordDatabase database = AssetDatabase.LoadAssetAtPath<WordShuffleWordDatabase>(Grade3VacationCancelletionDatabasePath);

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<WordShuffleWordDatabase>();
                database.EditorSetWords(Grade3VacationCancelletionWords);
                AssetDatabase.CreateAsset(database, Grade3VacationCancelletionDatabasePath);
            }
            else
            {
                database.EditorSetWords(Grade3VacationCancelletionWords);
                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = database;

            Debug.Log("Created/updated word database: " + Grade3VacationCancelletionDatabasePath);
        }

        [MenuItem(
    "Tools/Word Shuffle Drag Swap/Create Grade 3 Vacation Cancellation General Questions")]
        public static void CreateGrade3VacationCancellationGenralQuestionDatabase()
        {
            EnsureFolders();

            WordShuffleQuestionDatabase database =
                AssetDatabase.LoadAssetAtPath<WordShuffleQuestionDatabase>(
                    Grade3VacationCancellationGenralQuestionDatabasePath);

            if (database == null)
            {
                database =
                    ScriptableObject.CreateInstance<WordShuffleQuestionDatabase>();

                database.EditorSetQuestions(
                    Grade3VacationCancellationGenralQuestion);

                AssetDatabase.CreateAsset(
                    database,
                    Grade3VacationCancellationGenralQuestionDatabasePath);
            }
            else
            {
                database.EditorSetQuestions(
                    Grade3VacationCancellationGenralQuestion);

                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);

            Debug.Log(
                "Created or updated Grade 3 Vacation Cancellation General Question database: "
                + Grade3VacationCancellationGenralQuestionDatabasePath);
        }

        [MenuItem(
    "Tools/Word Shuffle Drag Swap/Create Grade 4 Never Lose Hope Questions")]
        public static void CreateGrade4NeverLoseHopeQuestionDatabase()
        {
            EnsureFolders();

            WordShuffleQuestionDatabase database =
                AssetDatabase.LoadAssetAtPath<WordShuffleQuestionDatabase>(
                    Grade4NeverLoseHopeQuestionDatabasePath);

            if (database == null)
            {
                database =
                    ScriptableObject.CreateInstance<WordShuffleQuestionDatabase>();

                database.EditorSetQuestions(
                    Grade4NeverLoseHopeQuestions);

                AssetDatabase.CreateAsset(
                    database,
                    Grade4NeverLoseHopeQuestionDatabasePath);
            }
            else
            {
                database.EditorSetQuestions(
                    Grade4NeverLoseHopeQuestions);

                EditorUtility.SetDirty(database);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = database;
            EditorGUIUtility.PingObject(database);

            Debug.Log(
                "Created or updated Grade 4 Never Lose Hope question database: "
                + Grade4NeverLoseHopeQuestionDatabasePath);
        }
    }
}
#endif
