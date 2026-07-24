using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using RewardSystem;

namespace WordShuffleDragSwap
{
    public enum WordShuffleRoundMode
    {
        EnglishWords,
        MathLargeNumbers,
        GeneralQuestions
    }

    public enum WordShuffleNumberWordStyle
    {
        International,
        Indian,
        Mixed
    }

    public enum WordShuffleNumberWordGrammar
    {
        BritishAnd,
        AmericanNoAnd
    }

    public enum WordShuffleHowToPlayMode
    {
        FirstTimeAutomatically,
        EveryGameStartAutomatically,
        ManualButtonOnly
    }

    public class WordShuffleDragSwapManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("Mode Settings")]
        [SerializeField] private WordShuffleRoundMode roundMode = WordShuffleRoundMode.EnglishWords;

        [Header("English Word Mode")]
        [SerializeField] private WordShuffleWordDatabase wordDatabase;

        [Header("General Question Mode")]
        [SerializeField] private WordShuffleQuestionDatabase questionDatabase;

        [Header("General Hint Text")]
        public string backupHint = "Unscamble the Word.";

        [Header("Game Settings")]
        [SerializeField, Min(1)] private int roundsPerGame = 10;
        [SerializeField] private bool allowRepeatedWords;
        [SerializeField] private bool autoStartOnPlay;
        [SerializeField, Min(1)] private int minWordLength = 3;
        [SerializeField, Min(1)] private int maxWordLength = 14;
        [SerializeField, Min(1)] private int scorePerCorrectWord = 10;
        [SerializeField] private bool useTimer = true;
        [SerializeField, Min(5f)] private float timePerRound = 45f;
        [SerializeField, Min(0f)] private float nextRoundDelay = 0.85f;
        [SerializeField, Min(1)] private int shuffleRetryCount = 20;

        [Header("How To Play Flow")]
        [SerializeField] private WordShuffleHowToPlayMode howToPlayMode = WordShuffleHowToPlayMode.FirstTimeAutomatically;

        [Header("First-Time Interactive Tutorial")]
        [SerializeField] private WordShuffleFirstTimeTutorialController firstTimeTutorial;

        [Header("Timeout Reveal Transition")]
        [SerializeField] private bool revealAnswerOnTimeout = true;
        [SerializeField, Min(0.05f)] private float timeoutRevealMoveDuration = 0.48f;
        [SerializeField, Min(0f)] private float timeoutRevealHoldDuration = 0.85f;
        [SerializeField] private Color timeoutRevealTileColor = new Color(0.92f, 0.22f, 0.18f, 1f);
        [SerializeField, Min(0.01f)] private float timeoutRevealPunchScale = 0.14f;

        [Header("Math Large Number Mode")]
        [SerializeField, Min(2)] private int mathMinDigitLength = 4;
        [SerializeField, Min(2)] private int mathMaxDigitLength = 5;
        [SerializeField] private WordShuffleNumberWordStyle mathNumberWordStyle = WordShuffleNumberWordStyle.Mixed;
        [SerializeField] private WordShuffleNumberWordGrammar mathNumberWordGrammar = WordShuffleNumberWordGrammar.BritishAnd;
        [SerializeField] private bool mathEnforceDigitRepeatLimit = true;
        [SerializeField, Min(50)] private int mathGenerationAttempts = 400;

        [Header("Hint Settings")]
        [SerializeField, Min(0)] private int maxHintsPerGame = 3;
        [SerializeField] private bool useAnswerLengthBasedHints = true;
        [SerializeField, Min(1)] private int minHintsPerQuestion = 1;
        [SerializeField, Min(1)] private int maxHintsPerQuestion = 5;
        [SerializeField, Min(0)] private int scorePenaltyPerHint = 1;
        [SerializeField] private bool showHintScoreMessageInInstruction = true;
        [SerializeField] private bool lockHintedLetters = true;
        [SerializeField] private bool consumeHintOnlyWhenUseful = true;
        [SerializeField, Min(0.05f)] private float hintMoveDuration = 0.42f;
        [SerializeField, Min(0.01f)] private float hintButtonPunchScale = 0.12f;
        [SerializeField] private Color hintedTileColor = new Color(0.25f, 0.86f, 0.42f, 1f);
        [SerializeField] private Color hintSlotGlowColor = new Color(1f, 0.9f, 0.22f, 0.65f);
        [SerializeField] private bool compactHintCountText;
        [SerializeField] private bool compactScoreText;
        [SerializeField] private bool compactTimerText;

        [Header("Scene References")]
        [FormerlySerializedAs("letterTilePrefab")]
        [SerializeField] private WordShuffleLetterTile letterTileTemplate;
        [SerializeField] private bool hideTileTemplateOnPlay = true;
        [SerializeField] private RectTransform tileLayer;
        [SerializeField] private RectTransform slotParent;
        [SerializeField] private Canvas rootCanvas;

        [Header("Global Fonts")]
        [SerializeField] private TMP_FontAsset primaryFont;
        [SerializeField] private TMP_FontAsset secondaryFont;
        [SerializeField] private bool applyGlobalFontsOnAwake = true;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private TextMeshProUGUI hintCountText;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultScoreText;

        [Header("Optional Image")]
        [SerializeField] private Image wordImage;

        [Header("UI Layout Progress")]
        [SerializeField] private bool showModeBadge;
        [SerializeField] private bool showTimerProgressVisuals;
        [SerializeField] private WordShuffleCircularProgressUI roundProgressCircle;
        [SerializeField] private WordShuffleCircularProgressUI timerProgressCircle;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField, Min(0.01f)] private float hudProgressTweenDuration = 0.18f;

        [Header("Panels")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject howToPlayPanel;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button pauseHowToPlayButton;
        [SerializeField] private Button closeHowToPlayButton;
        [SerializeField] private Button resultContinueButton;

        [Header("Slot Visuals")]
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Color slotColor = new Color(1f, 1f, 1f, 0.22f);
        [SerializeField] private Vector2 slotSize = new Vector2(118f, 118f);
        [SerializeField] private float slotSpacing = 16f;

        [Header("Responsive Layout")]
        [SerializeField] private bool autoFitTilesToAvailableWidth = true;
        [SerializeField, Min(40f)] private float minimumAutoFitTileWidth = 68f;
        [SerializeField, Min(0f)] private float horizontalSafePadding = 48f;
        [SerializeField, Min(0f)] private float minimumAutoFitSpacing = 4f;

        [Header("Dynamic Tile Sizing")]
        [SerializeField] private bool useDynamicTileSizingByAnswerLength = true;
        [SerializeField, Min(40f)] private float minDynamicTileSize = 76f;
        [SerializeField, Min(40f)] private float maxDynamicTileSize = 172f;
        [SerializeField, Min(1)] private int shortAnswerLargeTileThreshold = 5;
        [SerializeField, Min(1)] private int longAnswerSmallTileThreshold = 14;
        [SerializeField, Min(0f)] private float shortAnswerSpacing = 26f;
        [SerializeField, Min(0f)] private float longAnswerSpacing = 8f;
        [SerializeField, Range(0.35f, 0.75f)] private float dynamicTileTextFontRatio = 0.48f;

        [Header("Premium DOTween Animation")]
        [SerializeField] private bool useAnimations = true;
        [SerializeField, Min(0.01f)] private float spawnDuration = 0.28f;
        [SerializeField, Min(0f)] private float spawnStagger = 0.035f;
        [SerializeField, Min(0.01f)] private float snapDuration = 0.22f;
        [SerializeField, Min(0.01f)] private float swapDuration = 0.26f;
        [SerializeField, Min(0.01f)] private float dragScale = 1.12f;
        [SerializeField, Min(0.01f)] private float normalScale = 1f;
        [SerializeField, Min(0.01f)] private float correctPunchScale = 0.18f;
        [SerializeField] private Color correctAnswerTileColor = new Color(0.22f, 0.48f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float correctTileColorDuration = 0.16f;
        [SerializeField, Min(0.01f)] private float checkPulseScale = 0.06f;
        [SerializeField, Min(0f)] private float incorrectShakeStrength = 12f;
        [SerializeField] private Ease spawnEase = Ease.OutBack;
        [SerializeField] private Ease snapEase = Ease.OutBack;
        [SerializeField] private Ease swapEase = Ease.InOutCubic;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource backgroundMusicSource;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.45f;
        [SerializeField] private bool playBackgroundMusicOnGameplayStart = true;
        [SerializeField] private AudioClip dragSound;
        [SerializeField] private AudioClip swapSound;
        [SerializeField] private AudioClip snapBackSound;
        [SerializeField] private AudioClip hintSound;
        [SerializeField] private AudioClip correctSound;
        [SerializeField] private AudioClip wrongSound;
        [SerializeField] private AudioClip completeSound;

        [Header("Bloom Reward System")]
        [SerializeField] private bool useBloomRewardSystem = true;
        [SerializeField] private string homeSceneName = "Loader Scene";
        [SerializeField, Min(1f)] private float fallbackExpectedMaxTime = 120f;

        private readonly List<SkillEntry> bloomSkills = new List<SkillEntry>
        {
            new SkillEntry(BloomSkillType.Remember, 50f),
            new SkillEntry(BloomSkillType.Understand, 75f),
            new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: 0.3f, accuracyWeight: 0.7f)
        };

        private readonly List<WordShuffleLetterTile> activeTiles = new List<WordShuffleLetterTile>();
        private readonly List<RectTransform> activeSlots = new List<RectTransform>();
        private readonly List<WordShuffleRuntimeRound> sessionRounds = new List<WordShuffleRuntimeRound>();

        private Vector2 currentRoundSlotSize;
        private float currentRoundSlotSpacing;

        private WordShuffleRuntimeRound currentEntry;
        private string currentWord;
        private int currentRoundIndex;
        private int score;
        private int hintsRemaining;
        private int currentRoundMaxHints;
        private int currentRoundHintsUsed;
        private float remainingTime;
        private bool gameRunning;
        private bool inputLocked;
        private bool isPaused;
        private bool roundSolved;
        private bool startGameAfterHowToPlayClose;
        private Sequence activeSequence;
        private Coroutine bloomPreGameCoroutine;
        private float gameplayStartTime;
        private float finalGameplayTimeTaken;
        private int correctAnswerCount;
        private int mistakeCount;
        private bool bloomPreGameReady;
        private bool bloomPostGameShown;
        private bool tutorialFlowActive;

        public RectTransform TileLayer => tileLayer;
        public WordShuffleRoundMode RoundMode => roundMode;
        public Camera UICamera { get; private set; }

        [ContextMenu("Apply Global Fonts Now")]
        public void ApplyGlobalFonts()
        {
            if (primaryFont == null && secondaryFont == null)
                return;

            TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI label in labels)
            {
                if (label == null)
                    continue;

                TMP_FontAsset selectedFont = ShouldUseSecondaryFont(label)
                    ? (secondaryFont != null ? secondaryFont : primaryFont)
                    : (primaryFont != null ? primaryFont : secondaryFont);

                if (selectedFont != null)
                    label.font = selectedFont;
            }
        }

        private bool ShouldUseSecondaryFont(TextMeshProUGUI label)
        {
            if (label == null)
                return false;

            string objectName = label.gameObject.name.ToLowerInvariant();
            string parentName = label.transform.parent != null
                ? label.transform.parent.name.ToLowerInvariant()
                : string.Empty;

            return objectName.Contains("title") ||
                   objectName.Contains("score") ||
                   objectName.Contains("round") ||
                   objectName.Contains("timer") ||
                   objectName.Contains("hintcount") ||
                   objectName.Contains("feedback") ||
                   parentName.Contains("button") ||
                   parentName.Contains("tile");
        }

        private struct WordShuffleRuntimeRound
        {
            public string QuestionText;
            public string Answer;
            public Sprite Picture;
            public AudioClip VoiceOver;
        }

        private void Awake()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            UICamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (hideTileTemplateOnPlay && letterTileTemplate != null)
                letterTileTemplate.gameObject.SetActive(false);

            if (applyGlobalFontsOnAwake)
                ApplyGlobalFonts();

            HookButtons();
        }

        private void Start()
        {
            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                ShowBloomPreGameBeforeStartPanel();
                return;
            }

            bloomPreGameReady = true;
            ShowStartState();

            if (autoStartOnPlay)
                RequestStartGame();
        }

        private void Update()
        {
            if (tutorialFlowActive || !gameRunning || isPaused || roundSolved || !useTimer)
                return;

            remainingTime -= Time.deltaTime;
            UpdateTimerText();

            if (remainingTime <= 0f)
                HandleRoundTimeUp();
        }

        private void OnDisable()
        {
            activeSequence?.Kill();
        }

        public void RequestStartGame()
        {
            ClearRoundObjects();

            gameRunning = false;
            isPaused = false;
            inputLocked = true;
            roundSolved = false;
            bloomPostGameShown = false;

            SetPanel(resultPanel, false);
            SetPanel(pausePanel, false);

            if (bloomPreGameCoroutine != null)
                StopCoroutine(bloomPreGameCoroutine);

            if (useBloomRewardSystem && RewardManager.Instance != null && !bloomPreGameReady)
            {
                SetPanel(startPanel, false);
                SetPanel(gamePanel, false);
                SetPanel(howToPlayPanel, false);
                bloomPreGameCoroutine = StartCoroutine(WaitForBloomPreGameThenContinue());
                return;
            }

            ContinueAfterBloomPreGame();
        }

        private IEnumerator WaitForBloomPreGameThenContinue()
        {
            yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
            bloomPreGameReady = true;
            bloomPreGameCoroutine = null;
            ContinueAfterBloomPreGame();
        }

        private void ShowBloomPreGameBeforeStartPanel()
        {
            bloomPreGameReady = false;
            gameRunning = false;
            inputLocked = true;
            isPaused = false;
            roundSolved = false;
            ClearRoundObjects();

            SetPanel(startPanel, false);
            SetPanel(gamePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);

            if (bloomPreGameCoroutine != null)
                StopCoroutine(bloomPreGameCoroutine);

            RewardManager.Instance.ShowPreGame(bloomSkills);
            bloomPreGameCoroutine = StartCoroutine(WaitForInitialBloomPreGameThenShowStart());
        }

        private IEnumerator WaitForInitialBloomPreGameThenShowStart()
        {
            yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
            bloomPreGameReady = true;
            bloomPreGameCoroutine = null;
            ShowStartState();

            if (autoStartOnPlay)
                RequestStartGame();
        }

        private void ContinueAfterBloomPreGame()
        {
            if (ShouldShowHowToPlayAutomatically())
            {
                startGameAfterHowToPlayClose = true;
                SetPanel(startPanel, false);
                SetPanel(gamePanel, true);
                OpenHowToPlay();
                return;
            }

            ContinueAfterHowToPlayFlow();
        }

        private void ContinueAfterHowToPlayFlow()
        {
            if (firstTimeTutorial != null && firstTimeTutorial.ShouldPlayTutorial)
            {
                firstTimeTutorial.BeginTutorial(this, StartGame);
                return;
            }

            StartGame();
        }

        public void StartGame()
        {
            tutorialFlowActive = false;

            if (!ValidateGame())
                return;

            BuildSessionWords();

            if (sessionRounds.Count == 0)
            {
                Debug.LogError("WordShuffleDragSwapManager: No rounds could be created for the selected mode/settings.", this);
                return;
            }

            startGameAfterHowToPlayClose = false;
            currentRoundIndex = 0;
            score = 0;
            correctAnswerCount = 0;
            mistakeCount = 0;
            finalGameplayTimeTaken = 0f;
            gameplayStartTime = Time.time;
            bloomPostGameShown = false;
            hintsRemaining = 0;
            currentRoundMaxHints = 0;
            currentRoundHintsUsed = 0;
            gameRunning = true;
            isPaused = false;
            inputLocked = false;
            roundSolved = false;

            SetPanel(startPanel, false);
            SetPanel(resultPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(gamePanel, true);

            StartBackgroundMusic();
            UpdateScoreText();
            UpdateModeText();
            UpdateInstructionText();
            UpdateHintCountText();
            UpdateHudProgress(true);
            StartRound();
        }

        public bool CanDragTile(WordShuffleLetterTile tile)
        {
            return tile != null &&
                   !tile.IsLockedByHint &&
                   !tutorialFlowActive &&
                   gameRunning &&
                   !inputLocked &&
                   !isPaused &&
                   !roundSolved;
        }

        public void NotifyTileDragStarted(WordShuffleLetterTile tile)
        {
            if (!CanDragTile(tile))
                return;

            PlaySound(dragSound);

            if (!useAnimations)
                return;

            tile.RectTransform.DOKill();
            tile.RectTransform
                .DOScale(dragScale, snapDuration * 0.65f)
                .SetEase(Ease.OutBack);
        }

        public void NotifyTileDropped(WordShuffleLetterTile tile, Vector2 screenPosition)
        {
            if (tile == null)
                return;

            if (!CanDragTile(tile))
            {
                SnapTileToSlot(tile, true);
                return;
            }

            WordShuffleLetterTile targetTile = FindTileAtScreenPosition(screenPosition, tile);

            if (targetTile != null)
            {
                if (targetTile.IsLockedByHint)
                {
                    PlaySound(snapBackSound);
                    ShakeTile(targetTile);
                    SnapTileToSlot(tile, true);
                    return;
                }

                SwapTiles(tile, targetTile);
                return;
            }

            PlaySound(snapBackSound);
            SnapTileToSlot(tile, true);
        }

        public void RestartGame()
        {
            RequestStartGame();
        }

        public void UseHint()
        {
            if (tutorialFlowActive)
            {
                if (firstTimeTutorial != null)
                    firstTimeTutorial.HandleHintButtonPressed();

                return;
            }

            if (!gameRunning || isPaused || inputLocked || roundSolved)
            {
                ShakeButton(hintButton);
                return;
            }

            if (hintsRemaining <= 0)
            {
                SetFeedback("No hints left");
                ShakeButton(hintButton);
                return;
            }

            if (!TryFindHintMove(out int targetIndex, out WordShuffleLetterTile correctTile, out WordShuffleLetterTile displacedTile))
            {
                SetFeedback("No useful hint now");
                ShakeButton(hintButton);
                return;
            }

            if (!consumeHintOnlyWhenUseful)
                RegisterHintUsedForCurrentRound();

            PlayHintMove(targetIndex, correctTile, displacedTile);
        }

        public void PauseGame()
        {
            if (!gameRunning || isPaused)
                return;

            isPaused = true;
            inputLocked = true;
            SetPanel(pausePanel, true);
            UpdateHintCountText();
            AnimatePanelIn(pausePanel);
        }

        public void ResumeGame()
        {
            if (!gameRunning)
                return;

            isPaused = false;
            inputLocked = false;
            SetPanel(pausePanel, false);
            UpdateHintCountText();
        }

        public void OpenHowToPlay()
        {
            SetPanel(howToPlayPanel, true);
            AnimatePanelIn(howToPlayPanel);
        }

        public void CloseHowToPlay()
        {
            SetPanel(howToPlayPanel, false);
            MarkHowToPlaySeenForActiveScene();

            if (startGameAfterHowToPlayClose)
            {
                startGameAfterHowToPlayClose = false;
                ContinueAfterHowToPlayFlow();
            }
        }

        public void BeginTutorialHold()
        {
            ClearRoundObjects();
            tutorialFlowActive = true;
            gameRunning = false;
            isPaused = false;
            inputLocked = true;
            roundSolved = false;
            startGameAfterHowToPlayClose = false;

            SetPanel(startPanel, false);
            SetPanel(resultPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(gamePanel, true);

            if (hintButton != null)
                hintButton.interactable = false;
        }

        public void EndTutorialHold()
        {
            tutorialFlowActive = false;
            gameRunning = false;
            isPaused = false;
            inputLocked = true;
            roundSolved = false;

            if (hintButton != null)
                hintButton.interactable = false;
        }

        public void SetTutorialHintButtonInteractable(bool interactable)
        {
            if (hintButton != null)
                hintButton.interactable = tutorialFlowActive && interactable;
        }

        private bool ShouldShowHowToPlayAutomatically()
        {
            switch (howToPlayMode)
            {
                case WordShuffleHowToPlayMode.EveryGameStartAutomatically:
                    return true;

                case WordShuffleHowToPlayMode.FirstTimeAutomatically:
                    return PlayerPrefs.GetInt(GetHowToPlaySeenKey(), 0) == 0;

                default:
                    return false;
            }
        }

        private void MarkHowToPlaySeenForActiveScene()
        {
            PlayerPrefs.SetInt(GetHowToPlaySeenKey(), 1);
            PlayerPrefs.Save();
        }

        private static string GetHowToPlaySeenKey()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return $"WordShuffle.HowToPlay.Seen.{sceneName}";
        }

        [ContextMenu("Reset How To Play First-Time Status For Active Scene")]
        private void ResetHowToPlaySeenForActiveScene()
        {
            PlayerPrefs.DeleteKey(GetHowToPlaySeenKey());
            PlayerPrefs.Save();
            Debug.Log($"Reset How to Play first-time status for scene '{SceneManager.GetActiveScene().name}'.", this);
        }

        public void ContinueFromResult()
        {
            ShowBloomPostGame();
        }

        private void ShowBloomPostGame()
        {
            if (bloomPostGameShown)
                return;

            bloomPostGameShown = true;

            if (!useBloomRewardSystem || RewardManager.Instance == null)
            {
                Debug.LogWarning("WordShuffleDragSwapManager: Bloom RewardManager.Instance was not found. Keep RewardManager in LoadingScene with DontDestroyOnLoad.", this);
                return;
            }

            StopBackgroundMusic();
            GameEvaluationData evaluationData = BuildBloomEvaluationData();
            RewardManager.Instance.ShowPostGame(bloomSkills, evaluationData);
            SetPanel(resultPanel, false);
        }

        private GameEvaluationData BuildBloomEvaluationData()
        {
            float timeTaken = finalGameplayTimeTaken > 0f ? finalGameplayTimeTaken : GetGameplayTimeTaken();
            float expectedMaxTime = useTimer
                ? Mathf.Max(1f, sessionRounds.Count * timePerRound)
                : fallbackExpectedMaxTime;

            float timeScore = Mathf.Clamp01(1f - (timeTaken / expectedMaxTime));
            float accuracyScore = sessionRounds.Count > 0
                ? Mathf.Clamp01((float)correctAnswerCount / sessionRounds.Count)
                : 0f;

            return new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = mistakeCount,
                timeTaken = timeTaken
            };
        }

        private float GetGameplayTimeTaken()
        {
            return gameplayStartTime > 0f ? Mathf.Max(0f, Time.time - gameplayStartTime) : 0f;
        }

        private void StartRound()
        {
            ClearRoundObjects();

            if (currentRoundIndex >= sessionRounds.Count)
            {
                ShowResult();
                return;
            }

            currentEntry = sessionRounds[currentRoundIndex];
            currentWord = currentEntry.Answer;
            SetupRoundHints();
            remainingTime = timePerRound;
            roundSolved = false;
            inputLocked = true;

            UpdateRoundText();
            UpdateScoreText();
            UpdateTimerText();
            UpdateModeText();
            UpdateInstructionText();
            UpdateHintCountText();
            UpdateHudProgress(true);
            SetFeedback(roundMode == WordShuffleRoundMode.EnglishWords ? "Arrange the letters" : "Arrange the answer");
            SetQuestionAndImage(currentEntry);

            string shuffledWord = ShuffleWord(currentWord);
            CreateSlots(currentWord.Length);
            CreateTiles(shuffledWord);
            AnimateRoundStart();

            if (currentEntry.VoiceOver != null)
                PlaySound(currentEntry.VoiceOver);
        }

        private void SwapTiles(WordShuffleLetterTile firstTile, WordShuffleLetterTile secondTile)
        {
            if (firstTile == null || secondTile == null || firstTile.IsLockedByHint || secondTile.IsLockedByHint)
            {
                SnapTileToSlot(firstTile, true);
                ShakeTile(secondTile);
                return;
            }

            inputLocked = true;
            PlaySound(swapSound);

            int firstIndex = firstTile.CurrentIndex;
            int secondIndex = secondTile.CurrentIndex;

            firstTile.SetIndex(secondIndex);
            secondTile.SetIndex(firstIndex);

            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();

            Vector2 firstTarget = GetSlotLocalPosition(firstTile.CurrentIndex);
            Vector2 secondTarget = GetSlotLocalPosition(secondTile.CurrentIndex);

            if (!useAnimations)
            {
                firstTile.RectTransform.anchoredPosition = firstTarget;
                secondTile.RectTransform.anchoredPosition = secondTarget;
                firstTile.RectTransform.localScale = Vector3.one * normalScale;
                secondTile.RectTransform.localScale = Vector3.one * normalScale;
                inputLocked = false;
                UpdateHintCountText();
                CheckCurrentAnswer();
                return;
            }

            activeSequence.Join(firstTile.RectTransform.DOAnchorPos(firstTarget, swapDuration).SetEase(swapEase));
            activeSequence.Join(secondTile.RectTransform.DOAnchorPos(secondTarget, swapDuration).SetEase(swapEase));
            activeSequence.Join(firstTile.RectTransform.DOScale(normalScale, swapDuration * 0.8f).SetEase(Ease.OutBack));
            activeSequence.Join(secondTile.RectTransform.DOScale(normalScale, swapDuration * 0.8f).SetEase(Ease.OutBack));

            activeSequence.OnComplete(() =>
            {
                inputLocked = false;
                UpdateHintCountText();
                PlayCheckPulse();
                CheckCurrentAnswer();
            });
        }

        private bool TryFindHintMove(out int targetIndex, out WordShuffleLetterTile correctTile, out WordShuffleLetterTile displacedTile)
        {
            targetIndex = -1;
            correctTile = null;
            displacedTile = null;

            if (string.IsNullOrEmpty(currentWord))
                return false;

            List<int> wrongUnlockedIndices = new List<int>();

            for (int i = 0; i < currentWord.Length; i++)
            {
                WordShuffleLetterTile tileAtIndex = GetTileAtIndex(i);
                if (tileAtIndex == null || tileAtIndex.IsLockedByHint)
                    continue;

                if (tileAtIndex.Letter != currentWord[i].ToString())
                    wrongUnlockedIndices.Add(i);
            }

            if (wrongUnlockedIndices.Count == 0)
                return false;

            ShuffleList(wrongUnlockedIndices);

            foreach (int index in wrongUnlockedIndices)
            {
                string neededLetter = currentWord[index].ToString();

                correctTile = activeTiles.FirstOrDefault(tile =>
                    tile != null &&
                    !tile.IsLockedByHint &&
                    tile.CurrentIndex != index &&
                    tile.Letter == neededLetter &&
                    !IsTileCorrect(tile));

                if (correctTile == null)
                {
                    correctTile = activeTiles.FirstOrDefault(tile =>
                        tile != null &&
                        !tile.IsLockedByHint &&
                        tile.CurrentIndex != index &&
                        tile.Letter == neededLetter);
                }

                if (correctTile == null)
                    continue;

                displacedTile = GetTileAtIndex(index);
                if (displacedTile == null || displacedTile.IsLockedByHint)
                    continue;

                targetIndex = index;
                return true;
            }

            return false;
        }

        private void PlayHintMove(int targetIndex, WordShuffleLetterTile correctTile, WordShuffleLetterTile displacedTile)
        {
            if (correctTile == null || displacedTile == null)
                return;

            inputLocked = true;
            PlaySound(hintSound != null ? hintSound : swapSound);

            if (consumeHintOnlyWhenUseful)
                RegisterHintUsedForCurrentRound();
            else
                ShowHintScoreMessage();

            SetFeedback($"Hint used. Score now {GetCurrentRoundAwardScore()}/{scorePerCorrectWord}");
            UpdateHintCountText();
            AnimateHintButton();

            int correctStartIndex = correctTile.CurrentIndex;
            int displacedStartIndex = displacedTile.CurrentIndex;

            correctTile.SetIndex(displacedStartIndex);
            displacedTile.SetIndex(correctStartIndex);

            Vector2 correctTarget = GetSlotLocalPosition(correctTile.CurrentIndex);
            Vector2 displacedTarget = GetSlotLocalPosition(displacedTile.CurrentIndex);
            Image targetSlotImage = GetSlotImage(targetIndex);
            Color originalSlotColor = targetSlotImage != null ? targetSlotImage.color : slotColor;

            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();

            if (!useAnimations)
            {
                correctTile.RectTransform.anchoredPosition = correctTarget;
                displacedTile.RectTransform.anchoredPosition = displacedTarget;

                if (lockHintedLetters)
                    correctTile.SetLockedByHint(true, hintedTileColor, false, correctPunchScale);

                inputLocked = false;
                UpdateHintCountText();
                CheckCurrentAnswer();
                return;
            }

            correctTile.RectTransform.DOKill();
            displacedTile.RectTransform.DOKill();

            correctTile.transform.SetAsLastSibling();

            if (targetSlotImage != null)
            {
                targetSlotImage.DOKill();
                activeSequence.Join(targetSlotImage.DOColor(hintSlotGlowColor, 0.12f));
            }

            activeSequence.Join(correctTile.RectTransform.DOScale(dragScale + 0.08f, hintMoveDuration * 0.4f).SetEase(Ease.OutBack));
            activeSequence.Append(correctTile.RectTransform.DOAnchorPos(correctTarget + new Vector2(0f, 22f), hintMoveDuration * 0.55f).SetEase(Ease.OutCubic));
            activeSequence.Join(displacedTile.RectTransform.DOAnchorPos(displacedTarget, hintMoveDuration).SetEase(swapEase));
            activeSequence.Append(correctTile.RectTransform.DOAnchorPos(correctTarget, hintMoveDuration * 0.45f).SetEase(snapEase));
            activeSequence.Join(correctTile.RectTransform.DOScale(normalScale, hintMoveDuration * 0.45f).SetEase(Ease.OutBack));
            activeSequence.Join(displacedTile.RectTransform.DOScale(normalScale, hintMoveDuration * 0.35f).SetEase(Ease.OutBack));

            if (targetSlotImage != null)
                activeSequence.Append(targetSlotImage.DOColor(originalSlotColor, 0.16f));

            activeSequence.OnComplete(() =>
            {
                if (lockHintedLetters)
                    correctTile.SetLockedByHint(true, hintedTileColor, useAnimations, correctPunchScale);

                inputLocked = false;
                UpdateHintCountText();
                PlayCheckPulse();
                CheckCurrentAnswer();
            });
        }

        private bool IsTileCorrect(WordShuffleLetterTile tile)
        {
            return tile != null &&
                   !string.IsNullOrEmpty(currentWord) &&
                   tile.CurrentIndex >= 0 &&
                   tile.CurrentIndex < currentWord.Length &&
                   tile.Letter == currentWord[tile.CurrentIndex].ToString();
        }

        private WordShuffleLetterTile GetTileAtIndex(int index)
        {
            return activeTiles.FirstOrDefault(tile => tile != null && tile.CurrentIndex == index);
        }

        private Image GetSlotImage(int index)
        {
            if (index < 0 || index >= activeSlots.Count || activeSlots[index] == null)
                return null;

            return activeSlots[index].GetComponent<Image>();
        }

        private void CheckCurrentAnswer()
        {
            if (roundSolved || !gameRunning)
                return;

            string answer = BuildCurrentAnswer();

            if (answer == currentWord)
            {
                HandleCorrectAnswer();
                return;
            }

            SetFeedback("Not yet");
        }

        private void HandleCorrectAnswer()
        {
            roundSolved = true;
            inputLocked = true;
            correctAnswerCount++;
            int earnedScore = GetCurrentRoundAwardScore();
            UpdateHintCountText();
            score += earnedScore;

            UpdateScoreText();
            SetFeedback(earnedScore >= scorePerCorrectWord ? "Correct!" : $"Correct! +{earnedScore} after hint penalty");
            PlaySound(correctSound);
            AnimateCorrectAnswer(() =>
            {
                currentRoundIndex++;
                StartRound();
            });
        }

        private void HandleRoundTimeUp()
        {
            remainingTime = 0f;
            UpdateTimerText();
            roundSolved = true;
            inputLocked = true;
            mistakeCount++;
            UpdateHintCountText();
            SetFeedback($"Time up! Correct answer: {currentWord}");
            PlaySound(wrongSound);

            System.Action goNext = () =>
            {
                currentRoundIndex++;
                StartRound();
            };

            if (revealAnswerOnTimeout)
                AnimateTimeoutReveal(goNext);
            else
                AnimateWrongAnswer(goNext);
        }

        private void ShowResult()
        {
            finalGameplayTimeTaken = GetGameplayTimeTaken();
            ClearRoundObjects();
            gameRunning = false;
            inputLocked = true;
            UpdateHintCountText();

            SetPanel(gamePanel, false);
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(resultPanel, true);

            if (resultTitleText != null)
                resultTitleText.text = "Great Job!";

            if (resultScoreText != null)
                resultScoreText.text = $"Score: {score}";

            PlaySound(completeSound);
            AnimatePanelIn(resultPanel);
        }

        private void BuildSessionWords()
        {
            sessionRounds.Clear();

            switch (roundMode)
            {
                case WordShuffleRoundMode.MathLargeNumbers:
                    BuildMathGeneratedRounds();
                    break;

                case WordShuffleRoundMode.GeneralQuestions:
                    BuildGeneralQuestionRounds();
                    break;

                default:
                    BuildEnglishWordRounds();
                    break;
            }
        }

        private void BuildEnglishWordRounds()
        {
            List<WordShuffleWordEntry> validWords = wordDatabase.GetValidEntries(minWordLength, maxWordLength);
            ShuffleList(validWords);

            int targetCount = Mathf.Min(roundsPerGame, validWords.Count);

            if (allowRepeatedWords)
            {
                for (int i = 0; i < roundsPerGame; i++)
                {
                    WordShuffleWordEntry entry = validWords[Random.Range(0, validWords.Count)];
                    sessionRounds.Add(new WordShuffleRuntimeRound
                    {
                        QuestionText = string.IsNullOrWhiteSpace(entry.Hint) ? backupHint : entry.Hint,
                        Answer = entry.CleanWord(),
                        Picture = entry.Picture,
                        VoiceOver = entry.VoiceOver
                    });
                }
            }
            else
            {
                for (int i = 0; i < targetCount; i++)
                {
                    WordShuffleWordEntry entry = validWords[i];
                    sessionRounds.Add(new WordShuffleRuntimeRound
                    {
                        QuestionText = string.IsNullOrWhiteSpace(entry.Hint) ? backupHint : entry.Hint,
                        Answer = entry.CleanWord(),
                        Picture = entry.Picture,
                        VoiceOver = entry.VoiceOver
                    });
                }
            }
        }

        private void BuildGeneralQuestionRounds()
        {
            List<WordShuffleQuestionEntry> validQuestions = questionDatabase.GetValidEntries(minWordLength, maxWordLength);
            ShuffleList(validQuestions);

            int targetCount = Mathf.Min(roundsPerGame, validQuestions.Count);

            if (allowRepeatedWords)
            {
                for (int i = 0; i < roundsPerGame; i++)
                {
                    WordShuffleQuestionEntry entry = validQuestions[Random.Range(0, validQuestions.Count)];
                    sessionRounds.Add(CreateRoundFromQuestion(entry));
                }
            }
            else
            {
                for (int i = 0; i < targetCount; i++)
                    sessionRounds.Add(CreateRoundFromQuestion(validQuestions[i]));
            }
        }

        private WordShuffleRuntimeRound CreateRoundFromQuestion(WordShuffleQuestionEntry entry)
        {
            string question = entry.Question.Trim();
            if (!string.IsNullOrWhiteSpace(entry.Hint))
                question += $"\nHint: {entry.Hint.Trim()}";

            return new WordShuffleRuntimeRound
            {
                QuestionText = question,
                Answer = entry.CleanAnswer(),
                Picture = entry.Picture,
                VoiceOver = entry.VoiceOver
            };
        }

        private void BuildMathGeneratedRounds()
        {
            HashSet<string> usedNumbers = new HashSet<string>();
            int attempts = 0;

            while (sessionRounds.Count < roundsPerGame && attempts < mathGenerationAttempts)
            {
                attempts++;
                string numberText = GenerateNumberText();

                if (string.IsNullOrEmpty(numberText) || usedNumbers.Contains(numberText))
                    continue;

                usedNumbers.Add(numberText);
                WordShuffleNumberWordStyle selectedStyle = ResolveMathWordStyleForRound();
                sessionRounds.Add(new WordShuffleRuntimeRound
                {
                    QuestionText = NumberToWords(long.Parse(numberText), selectedStyle, mathNumberWordGrammar),
                    Answer = numberText,
                    Picture = null,
                    VoiceOver = null
                });
            }
        }

        private string ShuffleWord(string word)
        {
            if (word.Length <= 1)
                return word;

            char[] letters = word.ToCharArray();

            for (int attempt = 0; attempt < shuffleRetryCount; attempt++)
            {
                for (int i = 0; i < letters.Length; i++)
                {
                    int randomIndex = Random.Range(i, letters.Length);
                    (letters[i], letters[randomIndex]) = (letters[randomIndex], letters[i]);
                }

                string shuffled = new string(letters);
                if (shuffled != word)
                    return shuffled;
            }

            if (letters.Length > 1)
                (letters[0], letters[1]) = (letters[1], letters[0]);

            return new string(letters);
        }

        private void CreateSlots(int count)
        {
            CalculateResponsiveSlotLayout(count);

            for (int i = 0; i < count; i++)
            {
                GameObject slotObject = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                slotObject.transform.SetParent(slotParent, false);

                RectTransform slotRect = slotObject.GetComponent<RectTransform>();
                slotRect.sizeDelta = currentRoundSlotSize;

                Image image = slotObject.GetComponent<Image>();
                image.sprite = slotSprite;
                image.color = slotColor;
                image.raycastTarget = false;

                activeSlots.Add(slotRect);
            }

            HorizontalLayoutGroup layoutGroup = slotParent.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.spacing = currentRoundSlotSpacing;
                layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = false;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childForceExpandWidth = false;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);
        }

        private void CreateTiles(string shuffledWord)
        {
            for (int i = 0; i < shuffledWord.Length; i++)
            {
                WordShuffleLetterTile tile = Instantiate(letterTileTemplate, tileLayer);
                tile.gameObject.SetActive(true);
                tile.name = $"LetterTile_{shuffledWord[i]}_{i + 1}";
                tile.ApplyFont(secondaryFont != null ? secondaryFont : primaryFont);
                tile.Initialize(this, shuffledWord[i].ToString(), i);
                tile.ApplyResponsiveVisualSize(currentRoundSlotSize, dynamicTileTextFontRatio);

                RectTransform tileRect = tile.RectTransform;
                tileRect.sizeDelta = currentRoundSlotSize;
                tileRect.anchoredPosition = GetSlotLocalPosition(i);
                tileRect.localScale = Vector3.one * normalScale;

                activeTiles.Add(tile);
            }
        }

        private void AnimateRoundStart()
        {
            activeSequence?.Kill();

            if (!useAnimations)
            {
                foreach (WordShuffleLetterTile tile in activeTiles)
                {
                    Vector2 targetPosition = GetSlotLocalPosition(tile.CurrentIndex);
                    tile.RectTransform.anchoredPosition = targetPosition;
                    tile.RectTransform.localScale = Vector3.one * normalScale;
                }

                inputLocked = false;
                UpdateHintCountText();
                return;
            }

            activeSequence = DOTween.Sequence();

            for (int i = 0; i < activeTiles.Count; i++)
            {
                WordShuffleLetterTile tile = activeTiles[i];
                Vector2 targetPosition = GetSlotLocalPosition(tile.CurrentIndex);

                tile.RectTransform.DOKill();
                tile.RectTransform.localScale = Vector3.zero;
                tile.RectTransform.anchoredPosition = targetPosition + new Vector2(0f, -85f);

                activeSequence.Insert(i * spawnStagger, tile.RectTransform.DOAnchorPos(targetPosition, spawnDuration).SetEase(spawnEase));
                activeSequence.Insert(i * spawnStagger, tile.RectTransform.DOScale(normalScale, spawnDuration).SetEase(spawnEase));
            }

            activeSequence.OnComplete(() =>
            {
                inputLocked = false;
                UpdateHintCountText();
            });
        }

        private void AnimateCorrectAnswer(System.Action onComplete)
        {
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();

            if (useAnimations)
            {
                for (int i = 0; i < activeTiles.Count; i++)
                {
                    WordShuffleLetterTile tile = activeTiles[i];
                    tile.AnimateTileColor(correctAnswerTileColor, correctTileColorDuration);
                    activeSequence.Insert(
                        i * 0.045f,
                        tile.RectTransform.DOPunchScale(Vector3.one * correctPunchScale, 0.32f, 8, 0.75f));
                }

                if (feedbackText != null)
                {
                    feedbackText.rectTransform.DOKill();
                    feedbackText.rectTransform.localScale = Vector3.one;
                    activeSequence.Join(feedbackText.rectTransform.DOPunchScale(Vector3.one * 0.18f, 0.28f, 6, 0.7f));
                }
            }

            activeSequence.AppendInterval(nextRoundDelay);
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void AnimateTimeoutReveal(System.Action onComplete)
        {
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();

            Dictionary<WordShuffleLetterTile, int> revealTargets = BuildCorrectRevealTargets();

            if (!useAnimations)
            {
                foreach (KeyValuePair<WordShuffleLetterTile, int> pair in revealTargets)
                {
                    WordShuffleLetterTile tile = pair.Key;
                    int targetIndex = pair.Value;
                    tile.SetIndex(targetIndex);
                    tile.RectTransform.anchoredPosition = GetSlotLocalPosition(targetIndex);
                    tile.RectTransform.localScale = Vector3.one * normalScale;
                    tile.AnimateTileColor(correctAnswerTileColor, 0.01f);
                }

                onComplete?.Invoke();
                return;
            }

            foreach (WordShuffleLetterTile tile in activeTiles)
            {
                if (tile == null)
                    continue;

                tile.RectTransform.DOKill();
                tile.AnimateTileColor(timeoutRevealTileColor, correctTileColorDuration);
                activeSequence.Join(tile.RectTransform.DOShakeAnchorPos(0.26f, incorrectShakeStrength * 0.75f, 14, 90f, false, true));
            }

            if (feedbackText != null)
            {
                feedbackText.rectTransform.DOKill();
                feedbackText.rectTransform.localScale = Vector3.one;
                activeSequence.Join(feedbackText.rectTransform.DOPunchScale(Vector3.one * 0.12f, 0.28f, 6, 0.7f));
            }

            activeSequence.AppendInterval(0.08f);

            float arrangeStartTime = activeSequence.Duration();
            int order = 0;
            foreach (KeyValuePair<WordShuffleLetterTile, int> pair in revealTargets.OrderBy(pair => pair.Value))
            {
                WordShuffleLetterTile tile = pair.Key;
                int targetIndex = pair.Value;
                Vector2 targetPosition = GetSlotLocalPosition(targetIndex);

                tile.SetIndex(targetIndex);
                tile.transform.SetAsLastSibling();

                float startTime = order * 0.035f;
                activeSequence.Insert(arrangeStartTime + startTime, tile.RectTransform.DOAnchorPos(targetPosition + new Vector2(0f, 18f), timeoutRevealMoveDuration * 0.62f).SetEase(Ease.OutCubic));
                activeSequence.Insert(arrangeStartTime + startTime + timeoutRevealMoveDuration * 0.54f, tile.RectTransform.DOAnchorPos(targetPosition, timeoutRevealMoveDuration * 0.38f).SetEase(snapEase));
                activeSequence.Insert(arrangeStartTime + startTime, tile.RectTransform.DOScale(normalScale, timeoutRevealMoveDuration * 0.8f).SetEase(Ease.OutBack));
                order++;
            }

            float correctedRevealStart = activeSequence.Duration() + 0.04f;
            activeSequence.InsertCallback(correctedRevealStart, () =>
            {
                foreach (WordShuffleLetterTile tile in activeTiles)
                {
                    if (tile != null)
                        tile.AnimateTileColor(correctAnswerTileColor, correctTileColorDuration);
                }
            });

            int celebrationOrder = 0;
            foreach (KeyValuePair<WordShuffleLetterTile, int> pair in revealTargets.OrderBy(pair => pair.Value))
            {
                WordShuffleLetterTile tile = pair.Key;
                float startTime = correctedRevealStart + celebrationOrder * 0.045f;
                activeSequence.Insert(
                    startTime,
                    tile.RectTransform.DOPunchScale(Vector3.one * timeoutRevealPunchScale, 0.32f, 8, 0.75f));
                celebrationOrder++;
            }

            activeSequence.AppendInterval(timeoutRevealHoldDuration);
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        private Dictionary<WordShuffleLetterTile, int> BuildCorrectRevealTargets()
        {
            Dictionary<WordShuffleLetterTile, int> targets = new Dictionary<WordShuffleLetterTile, int>();
            List<WordShuffleLetterTile> unusedTiles = activeTiles.Where(tile => tile != null).ToList();

            for (int i = 0; i < currentWord.Length; i++)
            {
                string neededLetter = currentWord[i].ToString();

                WordShuffleLetterTile selectedTile = unusedTiles.FirstOrDefault(tile =>
                    tile.CurrentIndex == i && tile.Letter == neededLetter);

                if (selectedTile == null)
                    selectedTile = unusedTiles.FirstOrDefault(tile => tile.Letter == neededLetter);

                if (selectedTile == null)
                    continue;

                targets[selectedTile] = i;
                unusedTiles.Remove(selectedTile);
            }

            return targets;
        }

        private void AnimateWrongAnswer(System.Action onComplete)
        {
            activeSequence?.Kill();
            activeSequence = DOTween.Sequence();

            if (useAnimations)
            {
                foreach (WordShuffleLetterTile tile in activeTiles)
                {
                    activeSequence.Join(tile.RectTransform.DOShakeAnchorPos(0.34f, incorrectShakeStrength, 14, 90f, false, true));
                }
            }

            activeSequence.AppendInterval(nextRoundDelay);
            activeSequence.OnComplete(() => onComplete?.Invoke());
        }

        private void ShakeTile(WordShuffleLetterTile tile)
        {
            if (!useAnimations || tile == null || tile.RectTransform == null)
                return;

            tile.RectTransform.DOKill();
            tile.RectTransform.DOShakeAnchorPos(0.22f, incorrectShakeStrength * 0.65f, 12, 90f, false, true)
                .OnComplete(() => SnapTileToSlot(tile, false));
        }

        private void ShakeButton(Button button)
        {
            if (!useAnimations || button == null)
                return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.DOKill();
            rect.DOShakeAnchorPos(0.22f, 9f, 12, 90f, false, true);
        }

        private void AnimateHintButton()
        {
            if (!useAnimations || hintButton == null)
                return;

            RectTransform rect = hintButton.GetComponent<RectTransform>();
            if (rect == null)
                return;

            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * hintButtonPunchScale, 0.24f, 8, 0.72f);
        }

        private void PlayCheckPulse()
        {
            if (!useAnimations)
                return;

            if (feedbackText != null)
            {
                feedbackText.rectTransform.DOKill();
                feedbackText.rectTransform.localScale = Vector3.one;
                feedbackText.rectTransform.DOPunchScale(Vector3.one * checkPulseScale, 0.18f, 4, 0.55f);
            }
        }

        private void SnapTileToSlot(WordShuffleLetterTile tile, bool animate)
        {
            if (tile == null || tile.CurrentIndex < 0 || tile.CurrentIndex >= activeSlots.Count)
                return;

            Vector2 targetPosition = GetSlotLocalPosition(tile.CurrentIndex);
            tile.RectTransform.DOKill();

            if (useAnimations && animate)
            {
                inputLocked = true;
                tile.RectTransform
                    .DOAnchorPos(targetPosition, snapDuration)
                    .SetEase(snapEase)
                    .OnComplete(() =>
                    {
                        inputLocked = false;
                        UpdateHintCountText();
                    });

                tile.RectTransform
                    .DOScale(normalScale, snapDuration * 0.8f)
                    .SetEase(Ease.OutBack);
            }
            else
            {
                tile.RectTransform.anchoredPosition = targetPosition;
                tile.RectTransform.localScale = Vector3.one * normalScale;
                UpdateHintCountText();
            }
        }

        private WordShuffleLetterTile FindTileAtScreenPosition(Vector2 screenPosition, WordShuffleLetterTile ignoredTile)
        {
            foreach (WordShuffleLetterTile tile in activeTiles)
            {
                if (tile == null || tile == ignoredTile)
                    continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(tile.RectTransform, screenPosition, UICamera))
                    return tile;
            }

            return null;
        }

        private Vector2 GetSlotLocalPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= activeSlots.Count || tileLayer == null)
                return Vector2.zero;

            RectTransform slot = activeSlots[slotIndex];
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(UICamera, slot.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(tileLayer, screenPoint, UICamera, out Vector2 localPoint);
            return localPoint;
        }

        private string BuildCurrentAnswer()
        {
            string[] letters = new string[currentWord.Length];

            foreach (WordShuffleLetterTile tile in activeTiles)
            {
                if (tile.CurrentIndex >= 0 && tile.CurrentIndex < letters.Length)
                    letters[tile.CurrentIndex] = tile.Letter;
            }

            return string.Concat(letters);
        }

        private bool ValidateGame()
        {
            if (letterTileTemplate == null || tileLayer == null || slotParent == null)
            {
                Debug.LogError("WordShuffleDragSwapManager: Missing scene tile template, tile layer, or slot parent.", this);
                return false;
            }

            minWordLength = Mathf.Max(1, minWordLength);
            maxWordLength = Mathf.Max(minWordLength, maxWordLength);

            switch (roundMode)
            {
                case WordShuffleRoundMode.MathLargeNumbers:
                    if (mathMaxDigitLength < mathMinDigitLength)
                        mathMaxDigitLength = mathMinDigitLength;
                    return true;

                case WordShuffleRoundMode.GeneralQuestions:
                    if (questionDatabase == null)
                    {
                        Debug.LogError("WordShuffleDragSwapManager: Missing Question Database for General Questions mode.", this);
                        return false;
                    }

                    List<WordShuffleQuestionEntry> validQuestions = questionDatabase.GetValidEntries(minWordLength, maxWordLength);
                    if (validQuestions.Count == 0)
                    {
                        Debug.LogError("WordShuffleDragSwapManager: Question database has no valid questions for selected answer length range.", this);
                        return false;
                    }

                    if (!allowRepeatedWords && validQuestions.Count < roundsPerGame)
                        Debug.LogWarning("WordShuffleDragSwapManager: Fewer valid questions than rounds. The game will end after available unique questions.", this);

                    return true;

                default:
                    if (wordDatabase == null)
                    {
                        Debug.LogError("WordShuffleDragSwapManager: Missing Word Database.", this);
                        return false;
                    }

                    List<WordShuffleWordEntry> validWords = wordDatabase.GetValidEntries(minWordLength, maxWordLength);
                    if (validWords.Count == 0)
                    {
                        Debug.LogError("WordShuffleDragSwapManager: Word database has no valid words for selected length range.", this);
                        return false;
                    }

                    if (!allowRepeatedWords && validWords.Count < roundsPerGame)
                        Debug.LogWarning("WordShuffleDragSwapManager: Fewer valid words than rounds. The game will end after available unique words.", this);

                    return true;
            }
        }

        private void SetQuestionAndImage(WordShuffleRuntimeRound entry)
        {
            if (hintText != null)
                hintText.text = string.IsNullOrWhiteSpace(entry.QuestionText) ? "Arrange the answer" : entry.QuestionText;

            if (wordImage != null)
            {
                wordImage.sprite = entry.Picture;
                wordImage.enabled = entry.Picture != null;
            }
        }

        private void ShowStartState()
        {
            startGameAfterHowToPlayClose = false;
            ClearRoundObjects();
            SetPanel(startPanel, true);
            SetPanel(gamePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);
            SetFeedback(string.Empty);
            UpdateModeText();
            UpdateInstructionText();
            UpdateHudProgress(false);
            UpdateHintCountText();
        }

        private void ClearRoundObjects()
        {
            activeSequence?.Kill();

            foreach (WordShuffleLetterTile tile in activeTiles)
            {
                if (tile == null)
                    continue;

                tile.KillTweens();
                SafeDestroyRoundObject(tile.gameObject);
            }

            foreach (RectTransform slot in activeSlots)
            {
                if (slot == null)
                    continue;

                SafeDestroyRoundObject(slot.gameObject);
            }

            activeTiles.Clear();
            activeSlots.Clear();

            // Safety cleanup: if a round changes inside the same frame, Unity's Destroy() waits
            // until end-of-frame. Detaching generated objects immediately prevents the next
            // HorizontalLayoutGroup rebuild from counting old slots and pushing letters off-screen.
            RemoveGeneratedChildren(slotParent, "Slot_");
            RemoveGeneratedChildren(tileLayer, "LetterTile_");

            Canvas.ForceUpdateCanvases();
            if (slotParent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);
        }

        private void CalculateResponsiveSlotLayout(int letterCount)
        {
            currentRoundSlotSize = slotSize;
            currentRoundSlotSpacing = slotSpacing;

            if (!autoFitTilesToAvailableWidth || letterCount <= 0 || slotParent == null)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent);

            float availableWidth = slotParent.rect.width - horizontalSafePadding * 2f;
            if (availableWidth <= 0f && tileLayer != null)
                availableWidth = tileLayer.rect.width - horizontalSafePadding * 2f;

            if (availableWidth <= 0f)
                return;

            int spacingGaps = Mathf.Max(0, letterCount - 1);

            if (!useDynamicTileSizingByAnswerLength)
            {
                float desiredTotalWidth = slotSize.x * letterCount + slotSpacing * spacingGaps;
                if (desiredTotalWidth <= availableWidth)
                    return;

                float legacyFittedWidth = (availableWidth - slotSpacing * spacingGaps) / letterCount;
                float fittedSpacing = slotSpacing;

                if (legacyFittedWidth < minimumAutoFitTileWidth && letterCount > 1)
                {
                    fittedSpacing = Mathf.Min(slotSpacing, minimumAutoFitSpacing);
                    legacyFittedWidth = (availableWidth - fittedSpacing * spacingGaps) / letterCount;
                }

                legacyFittedWidth = Mathf.Clamp(legacyFittedWidth, 40f, slotSize.x);
                float legacyHeightScale = slotSize.x > 0f ? legacyFittedWidth / slotSize.x : 1f;
                currentRoundSlotSize = new Vector2(legacyFittedWidth, Mathf.Max(40f, slotSize.y * legacyHeightScale));
                currentRoundSlotSpacing = Mathf.Max(0f, fittedSpacing);
                return;
            }

            int shortThreshold = Mathf.Max(1, shortAnswerLargeTileThreshold);
            int longThreshold = Mathf.Max(shortThreshold + 1, longAnswerSmallTileThreshold);
            float length01 = Mathf.InverseLerp(shortThreshold, longThreshold, letterCount);

            float dynamicSpacing = Mathf.Lerp(shortAnswerSpacing, longAnswerSpacing, length01);
            dynamicSpacing = Mathf.Max(minimumAutoFitSpacing, dynamicSpacing);

            float maxSizeForCount = Mathf.Lerp(maxDynamicTileSize, slotSize.x, length01);
            if (letterCount <= shortThreshold)
                maxSizeForCount = maxDynamicTileSize;

            float minSize = Mathf.Max(40f, Mathf.Min(minDynamicTileSize, minimumAutoFitTileWidth));
            float fittedWidth = (availableWidth - dynamicSpacing * spacingGaps) / letterCount;

            if (fittedWidth < minSize && letterCount > 1)
            {
                dynamicSpacing = Mathf.Min(dynamicSpacing, minimumAutoFitSpacing);
                fittedWidth = (availableWidth - dynamicSpacing * spacingGaps) / letterCount;
            }

            float finalWidth = Mathf.Clamp(fittedWidth, minSize, Mathf.Max(minSize, maxSizeForCount));
            float heightScale = slotSize.x > 0f ? finalWidth / slotSize.x : 1f;
            float finalHeight = Mathf.Max(40f, slotSize.y * heightScale);

            currentRoundSlotSize = new Vector2(finalWidth, finalHeight);
            currentRoundSlotSpacing = Mathf.Max(0f, dynamicSpacing);
        }

        private void RemoveGeneratedChildren(RectTransform parent, string namePrefix)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null || !child.name.StartsWith(namePrefix, System.StringComparison.Ordinal))
                    continue;

                SafeDestroyRoundObject(child.gameObject);
            }
        }

        private void SafeDestroyRoundObject(GameObject target)
        {
            if (target == null)
                return;

            target.transform.SetParent(null, false);
            target.SetActive(false);

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void HookButtons()
        {
            if (startButton != null)
                startButton.onClick.AddListener(RequestStartGame);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            if (hintButton != null)
                hintButton.onClick.AddListener(UseHint);

            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseGame);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (howToPlayButton != null)
                howToPlayButton.onClick.AddListener(OpenHowToPlay);

            if (pauseHowToPlayButton != null)
                pauseHowToPlayButton.onClick.AddListener(OpenHowToPlay);

            if (closeHowToPlayButton != null)
                closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);

            if (resultContinueButton != null)
                resultContinueButton.onClick.AddListener(ContinueFromResult);
        }

        private void UpdateRoundText()
        {
            int totalRounds = sessionRounds.Count > 0 ? sessionRounds.Count : roundsPerGame;
            int visibleRound = gameRunning ? Mathf.Clamp(currentRoundIndex + 1, 1, Mathf.Max(1, totalRounds)) : 1;

            if (roundText != null)
                roundText.text = $"{visibleRound}/{Mathf.Max(1, totalRounds)}";

            UpdateRoundProgress(true);
        }

        private void UpdateScoreText()
        {
            if (scoreText != null)
                scoreText.text = compactScoreText ? score.ToString() : $"Score: {score}";
        }

        private void UpdateTimerText()
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(useTimer);

                int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                timerText.text = compactTimerText ? $"{minutes:00}:{seconds:00}" : $"Time {minutes:00}:{seconds:00}";
            }

            UpdateTimerProgress();
        }

        private void UpdateModeText()
        {
            if (modeText == null)
                return;

            GameObject modeRoot = modeText.transform.parent != null && modeText.transform.parent.name.Contains("ModeBadge")
                ? modeText.transform.parent.gameObject
                : modeText.gameObject;

            modeRoot.SetActive(showModeBadge);
            if (!showModeBadge)
                return;

            switch (roundMode)
            {
                case WordShuffleRoundMode.MathLargeNumbers:
                    modeText.text = "Math Mode";
                    break;
                case WordShuffleRoundMode.GeneralQuestions:
                    modeText.text = "Question Mode";
                    break;
                default:
                    modeText.text = "English Mode";
                    break;
            }
        }

        private void UpdateInstructionText()
        {
            if (instructionText == null)
                return;

            instructionText.text = roundMode == WordShuffleRoundMode.EnglishWords
                ? "Drag one letter onto another. They will change places."
                : "Drag one letter onto another. They will change places.";
        }

        private void SetupRoundHints()
        {
            currentRoundHintsUsed = 0;
            currentRoundMaxHints = CalculateHintsForAnswer(currentWord);
            hintsRemaining = currentRoundMaxHints;
        }

        private int CalculateHintsForAnswer(string answer)
        {
            if (!useAnswerLengthBasedHints)
                return Mathf.Max(0, maxHintsPerGame);

            int answerLength = string.IsNullOrEmpty(answer) ? 0 : answer.Length;
            int calculatedHints = answerLength - 2;
            int minHintCap = Mathf.Max(1, minHintsPerQuestion);
            int maxHintCap = Mathf.Max(minHintCap, maxHintsPerQuestion);
            return Mathf.Clamp(calculatedHints, minHintCap, maxHintCap);
        }

        private int GetCurrentRoundAwardScore()
        {
            int penalty = Mathf.Max(0, currentRoundHintsUsed) * Mathf.Max(0, scorePenaltyPerHint);
            return Mathf.Max(0, scorePerCorrectWord - penalty);
        }

        private void RegisterHintUsedForCurrentRound()
        {
            if (hintsRemaining <= 0)
                return;

            hintsRemaining = Mathf.Max(0, hintsRemaining - 1);
            currentRoundHintsUsed++;
            ShowHintScoreMessage();
        }

        private void ShowHintScoreMessage()
        {
            string message = $"Hint used: this answer is now worth {GetCurrentRoundAwardScore()}/{scorePerCorrectWord} (-{scorePenaltyPerHint} per hint).";

            if (showHintScoreMessageInInstruction && instructionText != null)
                instructionText.text = message;

            SetFeedback(message);
        }

        private void UpdateHudProgress(bool animate)
        {
            UpdateRoundProgress(animate);
            UpdateTimerProgress();
        }

        private void UpdateRoundProgress(bool animate)
        {
            if (roundProgressCircle == null)
                return;

            int totalRounds = sessionRounds.Count > 0 ? sessionRounds.Count : roundsPerGame;
            float progress = totalRounds <= 0 ? 0f : Mathf.Clamp01((currentRoundIndex + 1f) / totalRounds);
            if (!gameRunning)
                progress = 0f;

            roundProgressCircle.SetProgress(progress, animate && useAnimations && Application.isPlaying, hudProgressTweenDuration);
        }

        private void UpdateTimerProgress()
        {
            float normalized = !useTimer || timePerRound <= 0f ? 1f : Mathf.Clamp01(remainingTime / timePerRound);
            bool showProgress = useTimer && showTimerProgressVisuals;

            if (timerProgressCircle != null)
            {
                timerProgressCircle.gameObject.SetActive(showProgress);
                if (showProgress)
                    timerProgressCircle.SetProgress(normalized, false);
            }

            if (timerFillImage != null)
            {
                Transform timerBarRoot = timerFillImage.transform.parent;
                if (timerBarRoot != null)
                    timerBarRoot.gameObject.SetActive(showProgress);

                timerFillImage.gameObject.SetActive(showProgress);
                if (showProgress)
                {
                    timerFillImage.type = Image.Type.Filled;
                    timerFillImage.fillMethod = Image.FillMethod.Horizontal;
                    timerFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                    timerFillImage.fillAmount = normalized;
                }
            }
        }

        private void UpdateHintCountText()
        {
            int visibleMaxHints = Mathf.Max(0, currentRoundMaxHints);

            if (hintCountText != null)
            {
                hintCountText.text = compactHintCountText
                    ? hintsRemaining.ToString()
                    : $"Hints {hintsRemaining}/{visibleMaxHints}";
            }

            if (hintButton != null)
                hintButton.interactable = gameRunning && !inputLocked && !isPaused && hintsRemaining > 0 && !roundSolved;
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        private void SetPanel(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        private void AnimatePanelIn(GameObject panel)
        {
            if (!useAnimations || panel == null)
                return;

            RectTransform panelRect = panel
                .GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.name.Contains("MainCard"));

            if (panelRect == null)
                panelRect = panel.GetComponent<RectTransform>();

            if (panelRect == null)
                return;

            panelRect.DOKill();
            panelRect.localScale = Vector3.one * 0.92f;
            panelRect.DOScale(1f, 0.22f).SetEase(Ease.OutBack);
        }

        private void StartBackgroundMusic()
        {
            if (!playBackgroundMusicOnGameplayStart || backgroundMusic == null)
                return;

            if (backgroundMusicSource == null)
                backgroundMusicSource = audioSource;

            if (backgroundMusicSource == null)
                return;

            backgroundMusicSource.loop = true;
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.volume = backgroundMusicVolume;

            if (backgroundMusicSource.clip != backgroundMusic)
                backgroundMusicSource.clip = backgroundMusic;

            if (!backgroundMusicSource.isPlaying)
                backgroundMusicSource.Play();
        }

        private void StopBackgroundMusic()
        {
            if (backgroundMusicSource != null && backgroundMusicSource.isPlaying)
                backgroundMusicSource.Stop();
        }

        public void OnRewardScreenOpen()
        {
            StopBackgroundMusic();
        }

        public void OnPlayAgain()
        {
            StopBackgroundMusic();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnHome()
        {
            StopBackgroundMusic();

            if (RewardManager.Instance != null)
                RewardManager.Instance.HideAll();

            if (UnityAndroidMediator.Instance != null)
                UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

            //if (GameLoader.Instance != null)
            //    GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");

            SceneManager.LoadScene(homeSceneName);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null)
                return;

            audioSource.PlayOneShot(clip);
        }


        private string GenerateNumberText()
        {
            int digitLength = Random.Range(mathMinDigitLength, mathMaxDigitLength + 1);
            digitLength = Mathf.Clamp(digitLength, 2, 9);

            char[] digits = new char[digitLength];
            digits[0] = (char)('1' + Random.Range(0, 9));

            for (int i = 1; i < digitLength; i++)
                digits[i] = (char)('0' + Random.Range(0, 10));

            string candidate = new string(digits);
            return IsNumberAllowed(candidate) ? candidate : string.Empty;
        }

        private bool IsNumberAllowed(string numberText)
        {
            if (string.IsNullOrEmpty(numberText))
                return false;

            if (numberText[0] == '0')
                return false;

            if (!mathEnforceDigitRepeatLimit)
                return true;

            int maxAllowedRepeats = Mathf.Max(1, numberText.Length - 2);
            Dictionary<char, int> counts = new Dictionary<char, int>();

            foreach (char digit in numberText)
            {
                if (!counts.ContainsKey(digit))
                    counts[digit] = 0;

                counts[digit]++;
                if (counts[digit] > maxAllowedRepeats)
                    return false;
            }

            return true;
        }

        private WordShuffleNumberWordStyle ResolveMathWordStyleForRound()
        {
            if (mathNumberWordStyle != WordShuffleNumberWordStyle.Mixed)
                return mathNumberWordStyle;

            return Random.value < 0.5f ? WordShuffleNumberWordStyle.International : WordShuffleNumberWordStyle.Indian;
        }

        private string NumberToWords(long number, WordShuffleNumberWordStyle style, WordShuffleNumberWordGrammar grammar)
        {
            if (number == 0)
                return "Zero";

            if (style == WordShuffleNumberWordStyle.Mixed)
                style = ResolveMathWordStyleForRound();

            string words = style == WordShuffleNumberWordStyle.Indian
                ? NumberToIndianWords(number, grammar)
                : NumberToInternationalWords(number, grammar);

            return string.IsNullOrWhiteSpace(words) ? number.ToString() : words;
        }

        private string NumberToInternationalWords(long number, WordShuffleNumberWordGrammar grammar)
        {
            if (number < 1000)
                return NumberBelowThousandToWords((int)number, grammar);

            string[] units = { "", "Thousand", "Million", "Billion" };
            List<string> parts = new List<string>();
            int unitIndex = 0;
            int lastChunk = (int)(number % 1000);

            while (number > 0 && unitIndex < units.Length)
            {
                int chunk = (int)(number % 1000);
                if (chunk > 0)
                {
                    string chunkWords = NumberBelowThousandToWords(chunk, grammar);
                    if (!string.IsNullOrEmpty(units[unitIndex]))
                        chunkWords += " " + units[unitIndex];
                    parts.Insert(0, chunkWords);
                }

                number /= 1000;
                unitIndex++;
            }

            return JoinNumberParts(parts, lastChunk, grammar);
        }

        private string NumberToIndianWords(long number, WordShuffleNumberWordGrammar grammar)
        {
            if (number < 1000)
                return NumberBelowThousandToWords((int)number, grammar);

            List<string> parts = new List<string>();

            int crore = (int)(number / 10000000);
            number %= 10000000;
            int lakh = (int)(number / 100000);
            number %= 100000;
            int thousand = (int)(number / 1000);
            int last = (int)(number % 1000);

            if (crore > 0)
                parts.Add(NumberBelowThousandToWords(crore, grammar) + " Crore");
            if (lakh > 0)
                parts.Add(NumberBelowThousandToWords(lakh, grammar) + " Lakh");
            if (thousand > 0)
                parts.Add(NumberBelowThousandToWords(thousand, grammar) + " Thousand");
            if (last > 0)
                parts.Add(NumberBelowThousandToWords(last, grammar));

            return JoinNumberParts(parts, last, grammar);
        }

        private string JoinNumberParts(List<string> parts, int lastChunk, WordShuffleNumberWordGrammar grammar)
        {
            if (parts == null || parts.Count == 0)
                return string.Empty;

            if (parts.Count > 1 && grammar == WordShuffleNumberWordGrammar.BritishAnd && lastChunk > 0 && lastChunk < 100)
            {
                int lastIndex = parts.Count - 1;
                parts[lastIndex] = "and " + parts[lastIndex];
            }

            return string.Join(" ", parts);
        }

        private string NumberBelowThousandToWords(int number, WordShuffleNumberWordGrammar grammar)
        {
            string[] ones =
            {
                "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
                "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
                "Seventeen", "Eighteen", "Nineteen"
            };

            string[] tens =
            {
                "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
            };

            List<string> parts = new List<string>();

            if (number >= 100)
            {
                parts.Add(ones[number / 100] + " Hundred");
                number %= 100;

                if (number > 0 && grammar == WordShuffleNumberWordGrammar.BritishAnd)
                    parts.Add("and");
            }

            if (number >= 20)
            {
                int tensDigit = number / 10;
                int onesDigit = number % 10;

                if (onesDigit > 0)
                    parts.Add(tens[tensDigit] + "-" + ones[onesDigit].ToLowerInvariant());
                else
                    parts.Add(tens[tensDigit]);
            }
            else if (number > 0)
            {
                parts.Add(ones[number]);
            }

            return string.Join(" ", parts);
        }

        private void ShuffleList<T>(IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
    }
}
