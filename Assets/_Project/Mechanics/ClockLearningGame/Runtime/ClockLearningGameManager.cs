using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Reflection;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;
using RewardSystem;

namespace ClockLearningGame
{
    public enum ClockLearningMode
    {
        SingleClockSetTime,
        DoubleClockTimeDifference
    }

    public enum ClockLearningDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum ClockLearningAnswerState
    {
        Correct,
        Close,
        Wrong
    }

    public enum ClockLearningTimeTextMode
    {
        DirectNumeric,
        TimePhrase,
        Mixed
    }

    [Serializable]
    public sealed class SingleClockQuestion
    {
        public string prompt = "Set the clock to";
        [Range(1, 12)] public int hour = 3;
        [Range(0, 59)] public int minute = 45;
        public bool showAmPm = true;
        public bool isPm = true;
        [Tooltip("Optional override. Used by runtime generator for phrases like quarter to four.")]
        public string displayText;
        [TextArea] public string hint = "Hour hand = short hand\nMinute hand = long hand";

        public int TargetMinutes12 => ((hour == 12 ? 0 : hour) * 60) + Mathf.Clamp(minute, 0, 59);

        public string DisplayTime
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayText)) return displayText;
                return $"{hour}:{minute:00}" + (showAmPm ? (isPm ? " PM" : " AM") : string.Empty);
            }
        }
    }

    [Serializable]
    public sealed class TimeDifferenceQuestion
    {
        public string prompt = "Make a time difference of";
        [Min(0)] public int targetHours = 2;
        [Range(0, 59)] public int targetMinutes = 30;
        [TextArea] public string hint = "Set both clocks so the difference matches the target. Use AM and PM for each clock.";

        public int TargetDifferenceMinutes => Mathf.Clamp((targetHours * 60) + targetMinutes, 0, 1439);

        public string DisplayDifference
        {
            get
            {
                int total = TargetDifferenceMinutes;
                int hours = total / 60;
                int minutes = total % 60;
                if (hours > 0 && minutes > 0) return $"{hours}h {minutes}m";
                if (hours > 0) return hours == 1 ? "1 hour" : $"{hours} hours";
                return minutes == 1 ? "1 minute" : $"{minutes} minutes";
            }
        }
    }

    [Serializable]
    public sealed class ClockLearningQuestionGenerationProfile
    {
        [Header("Single Clock")]
        public string singlePrompt = "Set the clock to";
        [Range(1, 12)] public int singleMinHour = 1;
        [Range(1, 12)] public int singleMaxHour = 12;
        [Range(0, 59)] public int singleMinMinute = 0;
        [Range(0, 59)] public int singleMaxMinute = 55;
        [Range(1, 30)] public int singleMinuteStep = 5;
        public ClockLearningTimeTextMode timeTextMode = ClockLearningTimeTextMode.Mixed;
        [Range(0f, 1f)] public float phraseChance = 0.55f;
        public bool showAmPmForSingle;
        public bool randomizeSingleAmPm;
        public bool defaultSingleIsPm = true;
        [TextArea] public string singleHint = "Hour hand = short hand\nMinute hand = long hand";

        [Header("Double Clock Difference")]
        [Min(0)] public int differenceMinMinutes = 30;
        [Min(1)] public int differenceMaxMinutes = 240;
        [Range(1, 60)] public int differenceStepMinutes = 15;
        public bool avoidZeroDifference = true;

        public static ClockLearningQuestionGenerationProfile Easy()
        {
            return new ClockLearningQuestionGenerationProfile
            {
                singleMinHour = 1,
                singleMaxHour = 12,
                singleMinMinute = 0,
                singleMaxMinute = 45,
                singleMinuteStep = 15,
                timeTextMode = ClockLearningTimeTextMode.Mixed,
                phraseChance = 0.75f,
                showAmPmForSingle = false,
                differenceMinMinutes = 30,
                differenceMaxMinutes = 180,
                differenceStepMinutes = 30,
                avoidZeroDifference = true
            };
        }

        public static ClockLearningQuestionGenerationProfile Normal()
        {
            return new ClockLearningQuestionGenerationProfile
            {
                singleMinHour = 1,
                singleMaxHour = 12,
                singleMinMinute = 0,
                singleMaxMinute = 55,
                singleMinuteStep = 5,
                timeTextMode = ClockLearningTimeTextMode.Mixed,
                phraseChance = 0.55f,
                showAmPmForSingle = false,
                differenceMinMinutes = 15,
                differenceMaxMinutes = 360,
                differenceStepMinutes = 15,
                avoidZeroDifference = true
            };
        }

        public static ClockLearningQuestionGenerationProfile Hard()
        {
            return new ClockLearningQuestionGenerationProfile
            {
                singleMinHour = 1,
                singleMaxHour = 12,
                singleMinMinute = 0,
                singleMaxMinute = 59,
                singleMinuteStep = 1,
                timeTextMode = ClockLearningTimeTextMode.Mixed,
                phraseChance = 0.25f,
                showAmPmForSingle = true,
                randomizeSingleAmPm = true,
                differenceMinMinutes = 5,
                differenceMaxMinutes = 720,
                differenceStepMinutes = 5,
                avoidZeroDifference = true
            };
        }
    }

    [Serializable]
    public sealed class ClockLearningBloomSkillConfig
    {
        public BloomSkillType skillType = BloomSkillType.Apply;
        [Min(1f)] public float maxScore = 100f;
        [Tooltip("-1 uses RewardManager global default.")] public float timeWeight = -1f;
        [Tooltip("-1 uses RewardManager global default.")] public float accuracyWeight = -1f;
    }

    [DisallowMultipleComponent]
    public sealed class ClockLearningGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("Mode")]
        [SerializeField] private ClockLearningMode gameMode = ClockLearningMode.SingleClockSetTime;
        [SerializeField] private ClockLearningDifficulty difficulty = ClockLearningDifficulty.Easy;
        [SerializeField, Min(1)] private int questionCount = 10;
        [SerializeField] private bool shuffleQuestions = true;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset primaryFont;
        [SerializeField] private TMP_FontAsset secondaryFont;
        [SerializeField] private bool applyFontsOnAwake = true;
        [SerializeField] private List<TextMeshProUGUI> extraPrimaryTexts = new List<TextMeshProUGUI>();
        [SerializeField] private List<TextMeshProUGUI> extraSecondaryTexts = new List<TextMeshProUGUI>();

        [Header("Mode Menu")]
        [SerializeField] private bool showModeMenuOnStart = true;
        [SerializeField] private bool mandatoryHowToBeforeGameplay = true;
        [SerializeField] private bool singleModeAvailable = true;
        [SerializeField] private bool doubleModeAvailable = true;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private CanvasGroup modeMenuPanelGroup;
        [SerializeField] private TextMeshProUGUI modeMenuTitleText;
        [SerializeField] private Button singleModeButton;
        [SerializeField] private Button doubleModeButton;
        [SerializeField] private Button menuHomeButton;

        [Header("Mode-wise How To Images")]
        [SerializeField] private List<Sprite> singleModeHowToImages = new List<Sprite>();
        [SerializeField] private List<Sprite> doubleModeHowToImages = new List<Sprite>();

        [Header("Clock Behavior")]
        [SerializeField] private ClockLearningHandRelationMode singleClockHandMode = ClockLearningHandRelationMode.RealisticLinked;
        [SerializeField] private ClockLearningHandRelationMode doubleClockHandMode = ClockLearningHandRelationMode.RealisticLinked;
        [SerializeField] private bool smoothDrivenHands = true;
        [Tooltip("Realistic mode option. When enabled, dragging the minute hand across 12 changes the hour like a real clock. Keep off for simpler set-time gameplay.")]
        [SerializeField] private bool carryHourWhenMinuteCrosses12 = false;
        [SerializeField, Range(0.02f, 0.35f)] private float drivenHandSmoothDuration = 0.08f;

        [Header("Optional Shared Clock Visual Style")]
        [Tooltip("Turn this on if all three clocks should use the same mark spacing and hand size values. Leave off when each clock needs separate art tuning.")]
        [SerializeField] private bool applySameClockVisualStyleToAllClocks = false;
        [SerializeField, Range(0.05f, 0.35f)] private float sharedNumberInsetFromClockEdge = 0.24f;
        [SerializeField, Range(0.03f, 0.25f)] private float sharedTickInsetFromClockEdge = 0.14f;
        [SerializeField, Min(0f)] private float sharedExtraMarkInsetPixels = 6f;
        [SerializeField, Min(1f)] private float sharedHourHandWidth = 18f;
        [SerializeField, Min(1f)] private float sharedHourHandHeight = 120f;
        [SerializeField, Min(1f)] private float sharedMinuteHandWidth = 12f;
        [SerializeField, Min(1f)] private float sharedMinuteHandHeight = 170f;

        [Header("Final Polish Safety")]
        [SerializeField] private bool lockClockInputWhenPanelsOpen = true;
        [SerializeField, Range(0.05f, 0.75f)] private float buttonDebounceSeconds = 0.25f;
        [SerializeField] private bool pauseBackgroundMusicDuringPauseMenu = true;
        [SerializeField] private bool pauseBackgroundMusicDuringHowTo = false;

        [Header("Tutorial")]
        [SerializeField] private ClockLearningTutorialController tutorialController;

        [Header("Runtime Question Generation")]
        [SerializeField] private bool generateQuestionsAtRuntime = true;
        [Tooltip("0 = random every round. Any other value = repeatable testing seed.")]
        [SerializeField] private int randomSeed;
        [SerializeField] private ClockLearningQuestionGenerationProfile easyGeneration = ClockLearningQuestionGenerationProfile.Easy();
        [SerializeField] private ClockLearningQuestionGenerationProfile normalGeneration = ClockLearningQuestionGenerationProfile.Normal();
        [SerializeField] private ClockLearningQuestionGenerationProfile hardGeneration = ClockLearningQuestionGenerationProfile.Hard();

        [Header("Score")]
        [SerializeField] private int correctScore = 100;
        [SerializeField] private int closeScore = 25;
        [SerializeField] private int retryPenalty = 0;
        [SerializeField] private bool addTimerBonus;

        [Header("Timer")]
        [SerializeField] private bool useTimer;
        [SerializeField, Min(5f)] private float questionTimeSeconds = 60f;

        [Header("Manual Questions - Single Clock Fallback")]
        [SerializeField] private List<SingleClockQuestion> singleClockQuestions = new List<SingleClockQuestion>();

        [Header("Manual Questions - Double Clock Fallback")]
        [SerializeField] private List<TimeDifferenceQuestion> differenceQuestions = new List<TimeDifferenceQuestion>();

        [Header("Clock Views")]
        [SerializeField] private ClockLearningClockView singleClock;
        [SerializeField] private ClockLearningClockView doubleClockA;
        [SerializeField] private ClockLearningClockView doubleClockB;

        [Header("Top UI")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI questionCounterText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button pauseButton;
        [FormerlySerializedAs("helpButton")]
        [SerializeField] private Button hintButton;

        [Header("Hint System")]
        [SerializeField] private bool enableHintButton = true;
        [SerializeField, Range(0.25f, 2.5f)] private float hintHourHandMoveDuration = 0.9f;
        [SerializeField, Range(0.25f, 2.5f)] private float hintMinuteHandMoveDuration = 1.05f;
        [SerializeField, Range(0f, 0.8f)] private float hintBetweenHandsDelay = 0.18f;
        [SerializeField, Range(0.05f, 0.6f)] private float hintBetweenClocksDelay = 0.22f;
        [SerializeField] private int doubleHintClockAStartMinutes24 = 180;
        [SerializeField, Range(0.35f, 1.5f)] private float hintStepPromptDuration = 0.85f;
        [SerializeField, TextArea] private string hintHourPlacedPrompt = "Good! The short hand is in place.";
        [SerializeField, TextArea] private string hintMinutePlacedPrompt = "Good! The long hand is in place.";
        [SerializeField, TextArea] private string singleHintLearningPrompt = "The short hand shows the hour. The long hand shows the minutes. All set! Press Submit.";
        [SerializeField, TextArea] private string doubleHintLearningPrompt = "Both clocks are set to show the target difference. All set! Press Submit.";
        [SerializeField] private bool pulseHintButtonOnWrongAnswer = true;
        [SerializeField, Range(0.05f, 0.4f)] private float hintAttentionPunchScale = 0.14f;
        [SerializeField, Range(0.15f, 1.2f)] private float hintAttentionDuration = 0.55f;

        [Header("Single Clock UI")]
        [SerializeField] private GameObject singleModeRoot;
        [SerializeField] private TextMeshProUGUI singlePromptText;
        [SerializeField] private TextMeshProUGUI singleTargetText;
        [SerializeField] private TextMeshProUGUI singleLegendText;
        [SerializeField] private Button singleSubmitButton;
        [SerializeField] private Button singleResetButton;

        [Header("Double Clock UI")]
        [SerializeField] private GameObject doubleModeRoot;
        [SerializeField] private TextMeshProUGUI differencePromptText;
        [SerializeField] private TextMeshProUGUI differenceTargetText;
        [SerializeField] private TextMeshProUGUI differenceChipText;
        [SerializeField] private Toggle clockAPmToggle;
        [SerializeField] private Toggle clockBPmToggle;
        [SerializeField] private TextMeshProUGUI clockAAmPmLabel;
        [SerializeField] private TextMeshProUGUI clockBAmPmLabel;
        [SerializeField] private Button doubleSubmitButton;
        [SerializeField] private Button doubleResetButton;

        [Header("Panels")]
        [SerializeField] private CanvasGroup feedbackPanelGroup;
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private CanvasGroup pausePanelGroup;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseHowToPlayButton;
        [SerializeField] private Button pauseHomeButton;
        [SerializeField] private CanvasGroup howToPlayPanelGroup;
        [SerializeField] private TextMeshProUGUI howToPlayText;
        [SerializeField] private Image howToPlayImage;
        [SerializeField] private TextMeshProUGUI howToPageCounterText;
        [SerializeField] private Button howToPreviousButton;
        [SerializeField] private Button howToNextButton;
        [SerializeField] private Button closeHowToPlayButton;
        [SerializeField] private CanvasGroup resultPanelGroup;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultScoreText;
        [SerializeField] private Button resultRestartButton;
        [SerializeField] private Button resultHomeButton;

        [Header("Audio")]
        [SerializeField] private ClockLearningAudioManager audioManager;
        [SerializeField] private bool startBackgroundMusicAfterBloomPreGame = true;

        [Header("Bloom Reward System")]
        [SerializeField] private bool useBloomRewardSystem = true;
        [SerializeField] private List<ClockLearningBloomSkillConfig> bloomSkills = new List<ClockLearningBloomSkillConfig>
        {
            new ClockLearningBloomSkillConfig { skillType = BloomSkillType.Remember, maxScore = 50f },
            new ClockLearningBloomSkillConfig { skillType = BloomSkillType.Understand, maxScore = 75f },
            new ClockLearningBloomSkillConfig { skillType = BloomSkillType.Apply, maxScore = 100f, timeWeight = 0.3f, accuracyWeight = 0.7f }
        };
        [SerializeField, Min(1f)] private float expectedMaxTimeSeconds = 180f;
        [SerializeField] private string homeSceneName = "Loader Scene";
        [SerializeField] private bool stopBackgroundMusicWhenRewardScreenOpens = true;

        [Header("Host Exit Integration")]
        [SerializeField] private string hostGameName = "Clock Game";
        [SerializeField] private string finishedMessage = "Game Done";
        [SerializeField] private string unfinishedMessage = "Game Unfinished";
        [SerializeField] private bool sendAndroidMediatorMessage = true;
        [SerializeField] private bool sendGameLoaderJsEvent = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onHomePressed;

        private readonly List<int> _questionOrder = new List<int>();
        private readonly List<SingleClockQuestion> _runtimeSingleQuestions = new List<SingleClockQuestion>();
        private readonly List<TimeDifferenceQuestion> _runtimeDifferenceQuestions = new List<TimeDifferenceQuestion>();
        private int _questionIndex;
        private int _score;
        private int _roundSeedOffset;
        private float _timerRemaining;
        private bool _timerRunning;
        private bool _acceptInput;
        private Sequence _feedbackSequence;
        private UnityAction<bool> _clockAPmListener;
        private UnityAction<bool> _clockBPmListener;
        private ClockLearningMode _howToMode;
        private bool _howToIsPreGame;
        private int _howToPageIndex;
        private float _roundStartTime;
        private int _correctCount;
        private int _mistakeCount;
        private int _attemptCount;
        private bool _bloomPostGameShown;
        private float _lastButtonPressRealtime = -999f;
        private bool _timerWasRunningBeforePause;
        private bool _timerWasRunningBeforeHowTo;
        private bool _hintUsedThisQuestion;
        private int _hintUsedCount;
        private bool _hintAnimating;
        private Sequence _hintAttentionSequence;
        private Coroutine _hintRoutine;

        private bool IsTutorialRunning => tutorialController != null && tutorialController.IsRunning;

        private void EnsureSafeInspectorValues()
        {
            questionCount = Mathf.Max(1, questionCount);
            questionTimeSeconds = Mathf.Max(5f, questionTimeSeconds);
            expectedMaxTimeSeconds = Mathf.Max(1f, expectedMaxTimeSeconds);
            buttonDebounceSeconds = Mathf.Clamp(buttonDebounceSeconds, 0.05f, 0.75f);
            drivenHandSmoothDuration = Mathf.Clamp(drivenHandSmoothDuration, 0.02f, 0.35f);
            sharedNumberInsetFromClockEdge = Mathf.Clamp(sharedNumberInsetFromClockEdge, 0.05f, 0.35f);
            sharedTickInsetFromClockEdge = Mathf.Clamp(sharedTickInsetFromClockEdge, 0.03f, 0.25f);
            sharedExtraMarkInsetPixels = Mathf.Max(0f, sharedExtraMarkInsetPixels);
            sharedHourHandWidth = Mathf.Max(1f, sharedHourHandWidth);
            sharedHourHandHeight = Mathf.Max(1f, sharedHourHandHeight);
            sharedMinuteHandWidth = Mathf.Max(1f, sharedMinuteHandWidth);
            sharedMinuteHandHeight = Mathf.Max(1f, sharedMinuteHandHeight);
            hintHourHandMoveDuration = Mathf.Clamp(hintHourHandMoveDuration, 0.25f, 2.5f);
            hintMinuteHandMoveDuration = Mathf.Clamp(hintMinuteHandMoveDuration, 0.25f, 2.5f);
            hintBetweenHandsDelay = Mathf.Clamp(hintBetweenHandsDelay, 0f, 0.8f);
            hintBetweenClocksDelay = Mathf.Clamp(hintBetweenClocksDelay, 0.05f, 0.6f);
            hintAttentionPunchScale = Mathf.Clamp(hintAttentionPunchScale, 0.05f, 0.4f);
            hintAttentionDuration = Mathf.Clamp(hintAttentionDuration, 0.15f, 1.2f);
            hintStepPromptDuration = Mathf.Clamp(hintStepPromptDuration, 0.35f, 1.5f);

            if (!singleModeAvailable && !doubleModeAvailable)
            {
                singleModeAvailable = true;
            }
        }

        private int ExactToleranceMinutes
        {
            get
            {
                switch (difficulty)
                {
                    case ClockLearningDifficulty.Easy: return 10;
                    case ClockLearningDifficulty.Normal: return 5;
                    default: return 2;
                }
            }
        }

        private int CloseToleranceMinutes => ExactToleranceMinutes * 2;
        private int MinuteSnapInterval => difficulty == ClockLearningDifficulty.Hard ? 1 : 5;

        private void Awake()
        {
            EnsureSafeInspectorValues();
            EnsureManualFallbackQuestions();
            ConfigureClocksForDifficulty();
            HideAllPanelsInstant();
            RefreshModeMenuAvailability();
            EnsureResultButtonLabels();

            if (applyFontsOnAwake)
            {
                ApplyConfiguredFonts();
            }

            if (useBloomRewardSystem)
            {
                ShowGroup(modeMenuPanelGroup, false, true);
                if (gameplayRoot != null) gameplayRoot.SetActive(false);
                if (singleModeRoot != null) singleModeRoot.SetActive(false);
                if (doubleModeRoot != null) doubleModeRoot.SetActive(false);
            }
        }

        private void OnValidate()
        {
            EnsureSafeInspectorValues();
        }

        private void OnEnable()
        {
            AddListeners();
        }

        private void Start()
        {
            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPreGame(BuildBloomSkillEntries());
                StartCoroutine(WaitForBloomPreGameThenStartLocalFlow());
                return;
            }

            if (useBloomRewardSystem && RewardManager.Instance == null)
            {
                Debug.LogWarning("Clock Learning Game: Bloom RewardManager.Instance was not found. Continuing without Bloom pre-game panel.");
            }

            if (startBackgroundMusicAfterBloomPreGame)
            {
                audioManager?.PlayBackgroundMusic();
            }

            StartLocalFlow();
        }

        private IEnumerator WaitForBloomPreGameThenStartLocalFlow()
        {
            yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);

            if (startBackgroundMusicAfterBloomPreGame)
            {
                audioManager?.PlayBackgroundMusic();
            }

            StartLocalFlow();
        }

        private void StartLocalFlow()
        {
            if (showModeMenuOnStart && modeMenuPanelGroup != null)
            {
                ShowMainMenu();
                return;
            }

            if (mandatoryHowToBeforeGameplay && howToPlayPanelGroup != null)
            {
                OpenModeHowTo(gameMode, true);
                return;
            }

            StartGame();
        }

        private void Update()
        {
            UpdateTimer();
        }

        private void OnDisable()
        {
            RemoveListeners();
            _feedbackSequence?.Kill();
            _hintAttentionSequence?.Kill();
            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }
            KillPanelTweens();
        }

        private void OnDestroy()
        {
            _feedbackSequence?.Kill();
            _hintAttentionSequence?.Kill();
            KillPanelTweens();
            Time.timeScale = 1f;
        }

        private void KillPanelTweens()
        {
            KillGroupTween(feedbackPanelGroup);
            KillGroupTween(pausePanelGroup);
            KillGroupTween(howToPlayPanelGroup);
            KillGroupTween(resultPanelGroup);
            KillGroupTween(modeMenuPanelGroup);
            if (tutorialController != null) tutorialController.KillTweens();
        }

        private static void KillGroupTween(CanvasGroup group)
        {
            if (group == null) return;
            group.DOKill();
            group.transform.DOKill();
        }

        public void ShowMainMenu()
        {
            Time.timeScale = 1f;
            _acceptInput = false;
            _timerRunning = false;
            SetClockInputEnabled(false);
            HideAllPanelsInstant();
            RefreshModeMenuAvailability();

            if (gameplayRoot != null) gameplayRoot.SetActive(false);
            else
            {
                if (singleModeRoot != null) singleModeRoot.SetActive(false);
                if (doubleModeRoot != null) doubleModeRoot.SetActive(false);
            }

            ShowGroup(modeMenuPanelGroup, true, false);
        }

        public void SelectSingleMode()
        {
            SelectMode(ClockLearningMode.SingleClockSetTime);
        }

        public void SelectDoubleMode()
        {
            SelectMode(ClockLearningMode.DoubleClockTimeDifference);
        }

        public void SelectMode(ClockLearningMode mode)
        {
            if (!TryConsumeButtonPress()) return;
            if (mode == ClockLearningMode.SingleClockSetTime && !singleModeAvailable) return;
            if (mode == ClockLearningMode.DoubleClockTimeDifference && !doubleModeAvailable) return;

            audioManager?.PlayClick();
            gameMode = mode;

            if (mandatoryHowToBeforeGameplay && howToPlayPanelGroup != null)
            {
                OpenModeHowTo(mode, true);
            }
            else
            {
                StartGame();
            }
        }

        public void StartGame()
        {
            Time.timeScale = 1f;
            _questionIndex = 0;
            _score = 0;
            _correctCount = 0;
            _mistakeCount = 0;
            _attemptCount = 0;
            _hintUsedCount = 0;
            _hintUsedThisQuestion = false;
            _hintAnimating = false;
            _roundStartTime = Time.time;
            _bloomPostGameShown = false;
            StopHintAttention();
            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }
            BuildRuntimeQuestionsIfNeeded();
            BuildQuestionOrder();
            ConfigureClocksForDifficulty();
            HideAllPanelsInstant();
            ShowGroup(modeMenuPanelGroup, false, true);
            if (gameplayRoot != null) gameplayRoot.SetActive(true);
            LoadCurrentQuestion();

            if (tutorialController != null && tutorialController.ShouldRun(gameMode))
            {
                BeginTutorialInputLock();
                tutorialController.Run(gameMode, OnTutorialComplete);
            }
            else
            {
                _roundStartTime = Time.time;
            }
        }

        public void SetMode(ClockLearningMode mode)
        {
            gameMode = mode;
            StartGame();
        }

        public void SubmitAnswer()
        {
            if (!_acceptInput || !TryConsumeButtonPress()) return;

            if (IsTutorialRunning) return;

            audioManager?.PlayClick();
            _attemptCount++;
            _acceptInput = false;
            SetClockInputEnabled(false);
            _timerRunning = false;
            SetSubmitButtons(false);
            SetHintButtonInteractable(false);

            ClockLearningAnswerState state = gameMode == ClockLearningMode.SingleClockSetTime
                ? EvaluateSingleClock()
                : EvaluateTimeDifference();

            HandleAnswerState(state);
        }

        public void ResetCurrentQuestion()
        {
            if (IsTutorialRunning) return;
            if (!_acceptInput || !TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            ResetClockPositions(true);
            if (retryPenalty > 0)
            {
                _score = Mathf.Max(0, _score - retryPenalty);
                RefreshTopBar();
            }
        }

        public void OpenPausePanel()
        {
            if (IsTutorialRunning) return;
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            _timerWasRunningBeforePause = _timerRunning;
            _timerRunning = false;
            SetClockInputEnabled(false);
            SetHintButtonInteractable(false);
            if (pauseBackgroundMusicDuringPauseMenu) audioManager?.PauseBackgroundMusic();
            Time.timeScale = 0f;
            ShowGroup(pausePanelGroup, true, false);
        }

        public void ResumeGame()
        {
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            Time.timeScale = 1f;
            _timerRunning = _timerWasRunningBeforePause && useTimer && _acceptInput;
            SetClockInputEnabled(_acceptInput);
            SetHintButtonInteractable(_acceptInput);
            if (pauseBackgroundMusicDuringPauseMenu) audioManager?.ResumeBackgroundMusic();
            ShowGroup(pausePanelGroup, false, false);
        }

        public void OpenHowToPlay()
        {
            if (IsTutorialRunning) return;
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            _timerWasRunningBeforeHowTo = _timerRunning;
            _timerRunning = false;
            SetClockInputEnabled(false);
            SetHintButtonInteractable(false);
            if (pauseBackgroundMusicDuringHowTo) audioManager?.PauseBackgroundMusic();
            OpenModeHowTo(gameMode, false);
        }

        public void CloseHowToPlay()
        {
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            bool shouldStartGame = _howToIsPreGame;
            _howToIsPreGame = false;
            ShowGroup(howToPlayPanelGroup, false, false);

            if (pauseBackgroundMusicDuringHowTo) audioManager?.ResumeBackgroundMusic();

            if (shouldStartGame)
            {
                StartGame();
            }
            else
            {
                _timerRunning = _timerWasRunningBeforeHowTo && useTimer && _acceptInput;
                SetClockInputEnabled(_acceptInput);
                SetHintButtonInteractable(_acceptInput);
            }
        }

        public void ShowPreviousHowToPage()
        {
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            _howToPageIndex = Mathf.Max(0, _howToPageIndex - 1);
            RefreshHowToPage();
        }

        public void ShowNextHowToPage()
        {
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            int pageCount = GetHowToSprites(_howToMode).Count;
            _howToPageIndex = Mathf.Min(Mathf.Max(0, pageCount - 1), _howToPageIndex + 1);
            RefreshHowToPage();
        }

        public void PressHome()
        {
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            Time.timeScale = 1f;
            _acceptInput = false;
            _timerRunning = false;
            if (tutorialController != null) tutorialController.HideInstant();
            SetClockInputEnabled(false);
            SetHintButtonInteractable(false);
            StopHintAttention();
            if (modeMenuPanelGroup != null)
            {
                ShowMainMenu();
            }
            onHomePressed?.Invoke();
        }

        public void ExitGameUnfinished()
        {
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            ExitToHome(unfinishedMessage);
        }

        public void ExitGameUnfinishedNoDebounce()
        {
            ExitToHome(unfinishedMessage);
        }

        private void OpenModeHowTo(ClockLearningMode mode, bool preGame)
        {
            _howToMode = mode;
            _howToIsPreGame = preGame;
            if (preGame)
            {
                _acceptInput = false;
                _timerRunning = false;
            }
            SetClockInputEnabled(false);
            SetHintButtonInteractable(false);
            _howToPageIndex = 0;
            RefreshHowToPage();
            SetButtonLabel(closeHowToPlayButton, preGame ? "Start Game" : "Close");
            ShowGroup(howToPlayPanelGroup, true, false);
        }

        private void RefreshHowToPage()
        {
            List<Sprite> sprites = GetHowToSprites(_howToMode);
            bool hasImages = sprites.Count > 0;

            if (howToPlayImage != null)
            {
                howToPlayImage.enabled = hasImages;
                howToPlayImage.sprite = hasImages ? sprites[Mathf.Clamp(_howToPageIndex, 0, sprites.Count - 1)] : null;
                howToPlayImage.preserveAspect = true;
            }

            if (howToPlayText != null)
            {
                howToPlayText.gameObject.SetActive(!hasImages);
                howToPlayText.text = _howToMode == ClockLearningMode.SingleClockSetTime
                    ? "Drag the short hour hand and the long minute hand.\nSet the clock to the shown time.\nPress Start, then Submit when your clock is ready."
                    : "Set both clocks so the time difference matches the target.\nUse AM and PM for each clock.\nPress Start, then Submit when ready.";
            }

            int pageCount = Mathf.Max(1, sprites.Count);
            _howToPageIndex = Mathf.Clamp(_howToPageIndex, 0, pageCount - 1);

            if (howToPageCounterText != null)
            {
                howToPageCounterText.text = hasImages ? $"{_howToPageIndex + 1}/{pageCount}" : "";
            }

            if (howToPreviousButton != null)
            {
                howToPreviousButton.gameObject.SetActive(hasImages && pageCount > 1);
                howToPreviousButton.interactable = _howToPageIndex > 0;
            }

            if (howToNextButton != null)
            {
                howToNextButton.gameObject.SetActive(hasImages && pageCount > 1);
                howToNextButton.interactable = _howToPageIndex < pageCount - 1;
            }
        }

        private List<Sprite> GetHowToSprites(ClockLearningMode mode)
        {
            List<Sprite> sprites = mode == ClockLearningMode.SingleClockSetTime ? singleModeHowToImages : doubleModeHowToImages;
            return sprites ?? new List<Sprite>();
        }

        private void RefreshModeMenuAvailability()
        {
            if (!singleModeAvailable && !doubleModeAvailable)
            {
                singleModeAvailable = true;
                Debug.LogWarning("Clock Learning Game: at least one mode must be available. Single mode was enabled automatically.");
            }

            if (modeMenuTitleText != null) modeMenuTitleText.text = "Clock Game";
            if (singleModeButton != null) singleModeButton.gameObject.SetActive(singleModeAvailable);
            if (doubleModeButton != null) doubleModeButton.gameObject.SetActive(doubleModeAvailable);

            if (modeMenuPanelGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)modeMenuPanelGroup.transform);
            }
        }

        private void LoadCurrentQuestion()
        {
            bool isSingle = gameMode == ClockLearningMode.SingleClockSetTime;
            if (singleModeRoot != null) singleModeRoot.SetActive(isSingle);
            if (doubleModeRoot != null) doubleModeRoot.SetActive(!isSingle);

            if (titleText != null) titleText.text = isSingle ? "Clock Game" : "Time Difference";

            if (isSingle) LoadSingleClockQuestion();
            else LoadTimeDifferenceQuestion();

            ResetClockPositions(false);
            _hintUsedThisQuestion = false;
            _hintAnimating = false;
            StopHintAttention();
            _timerRemaining = questionTimeSeconds;
            _timerRunning = useTimer;
            _acceptInput = true;
            SetClockInputEnabled(true);
            SetSubmitButtons(true);
            SetHintButtonInteractable(true);
            RefreshTopBar();
        }

        private void LoadSingleClockQuestion()
        {
            SingleClockQuestion question = GetCurrentSingleQuestion();
            if (singlePromptText != null) singlePromptText.text = question.prompt;
            if (singleTargetText != null) singleTargetText.text = question.DisplayTime;
            if (singleLegendText != null) singleLegendText.text = string.IsNullOrWhiteSpace(question.hint)
                ? "Hour Hand = short hand\nMinute Hand = long hand"
                : question.hint;
        }

        private void LoadTimeDifferenceQuestion()
        {
            TimeDifferenceQuestion question = GetCurrentDifferenceQuestion();
            if (differencePromptText != null) differencePromptText.text = question.prompt;
            if (differenceTargetText != null) differenceTargetText.text = question.DisplayDifference;
            if (differenceChipText != null) differenceChipText.text = $"Difference Target: {question.DisplayDifference}";
            UpdateAmPmLabels();
        }

        private ClockLearningAnswerState EvaluateSingleClock()
        {
            if (singleClock == null) return ClockLearningAnswerState.Wrong;

            int target = GetCurrentSingleQuestion().TargetMinutes12;
            int answer = singleClock.TotalMinutes12;
            int diff = CircularDifference(answer, target, 720);

            if (diff <= ExactToleranceMinutes) return ClockLearningAnswerState.Correct;
            if (diff <= CloseToleranceMinutes) return ClockLearningAnswerState.Close;
            return ClockLearningAnswerState.Wrong;
        }

        private ClockLearningAnswerState EvaluateTimeDifference()
        {
            if (doubleClockA == null || doubleClockB == null) return ClockLearningAnswerState.Wrong;

            bool aPm = clockAPmToggle != null && clockAPmToggle.isOn;
            bool bPm = clockBPmToggle != null && clockBPmToggle.isOn;
            int a = doubleClockA.GetTotalMinutes24(aPm);
            int b = doubleClockB.GetTotalMinutes24(bPm);
            int answerDifference = Mathf.Abs(a - b);
            int targetDifference = GetCurrentDifferenceQuestion().TargetDifferenceMinutes;
            int diff = Mathf.Abs(answerDifference - targetDifference);

            if (diff <= ExactToleranceMinutes) return ClockLearningAnswerState.Correct;
            if (diff <= CloseToleranceMinutes) return ClockLearningAnswerState.Close;
            return ClockLearningAnswerState.Wrong;
        }

        private void HandleAnswerState(ClockLearningAnswerState state)
        {
            switch (state)
            {
                case ClockLearningAnswerState.Correct:
                    _correctCount++;
                    int bonus = addTimerBonus && useTimer && !_hintUsedThisQuestion ? Mathf.CeilToInt(_timerRemaining) : 0;
                    if (!_hintUsedThisQuestion)
                    {
                        _score += correctScore + bonus;
                    }
                    audioManager?.PlayCorrect();
                    ShowFeedback(_hintUsedThisQuestion ? "Correct! Hint was used, so no points for this question." : "Great work!", true, AdvanceToNextQuestion);
                    break;

                case ClockLearningAnswerState.Close:
                    _mistakeCount++;
                    if (!_hintUsedThisQuestion)
                    {
                        _score += closeScore;
                    }
                    audioManager?.PlayClose();
                    ShowFeedback(_hintUsedThisQuestion ? "Almost there! Hint was used, so no points for this question." : "Nearly there! Check the hands and try again.", false, EnableRetry);
                    if (!_hintUsedThisQuestion) PulseHintButtonForAttention();
                    break;

                default:
                    _mistakeCount++;
                    audioManager?.PlayWrong();
                    ShowFeedback(_hintUsedThisQuestion ? "Try again! Hint was used, so no points for this question." : "Try again! Use Hint if you need help.", false, EnableRetry);
                    if (!_hintUsedThisQuestion) PulseHintButtonForAttention();
                    break;
            }

            RefreshTopBar();
        }

        public void UseHint()
        {
            if (!enableHintButton || IsTutorialRunning || _hintAnimating || !_acceptInput) return;
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayHint();
            StopHintAttention();
            _hintUsedThisQuestion = true;
            _hintUsedCount++;
            _hintAnimating = true;
            _acceptInput = false;
            _timerRunning = false;
            SetClockInputEnabled(false);
            SetSubmitButtons(false);
            SetResetButtons(false);
            SetHintButtonInteractable(false);

            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
            }

            _hintRoutine = StartCoroutine(RunHintRoutine());
        }

        private IEnumerator RunHintRoutine()
        {
            if (gameMode == ClockLearningMode.SingleClockSetTime)
            {
                SingleClockQuestion question = GetCurrentSingleQuestion();
                if (singleClock != null)
                {
                    Tween hourTween = singleClock.AnimateHandToTimeForHint(ClockLearningHandType.Hour, question.hour, question.minute, hintHourHandMoveDuration, false);
                    if (hourTween != null) yield return hourTween.WaitForCompletion();
                    yield return ShowTimedHintPrompt(hintHourPlacedPrompt);

                    if (hintBetweenHandsDelay > 0f) yield return new WaitForSeconds(hintBetweenHandsDelay);

                    Tween minuteTween = singleClock.AnimateHandToTimeForHint(ClockLearningHandType.Minute, question.hour, question.minute, hintMinuteHandMoveDuration, true);
                    if (minuteTween != null) yield return minuteTween.WaitForCompletion();
                    yield return ShowTimedHintPrompt(hintMinutePlacedPrompt);
                }

                yield return ShowHintLearningPrompt(singleHintLearningPrompt);
            }
            else
            {
                TimeDifferenceQuestion question = GetCurrentDifferenceQuestion();
                int startMinutes24 = NormalizeMinutes24(doubleHintClockAStartMinutes24);
                int endMinutes24 = NormalizeMinutes24(startMinutes24 + question.TargetDifferenceMinutes);

                SetClockAmPm(clockAPmToggle, startMinutes24 >= 720);
                SetClockAmPm(clockBPmToggle, endMinutes24 >= 720);
                UpdateAmPmLabels();

                if (doubleClockA != null)
                {
                    Tween clockAHour = doubleClockA.AnimateHandToTimeForHint(ClockLearningHandType.Hour, Hour12FromMinutes24(startMinutes24), startMinutes24 % 60, hintHourHandMoveDuration, false);
                    if (clockAHour != null) yield return clockAHour.WaitForCompletion();
                    yield return ShowTimedHintPrompt("Clock A: short hand is in place.");

                    if (hintBetweenHandsDelay > 0f) yield return new WaitForSeconds(hintBetweenHandsDelay);

                    Tween clockAMinute = doubleClockA.AnimateHandToTimeForHint(ClockLearningHandType.Minute, Hour12FromMinutes24(startMinutes24), startMinutes24 % 60, hintMinuteHandMoveDuration, true);
                    if (clockAMinute != null) yield return clockAMinute.WaitForCompletion();
                    yield return ShowTimedHintPrompt("Clock A: long hand is in place.");
                }

                if (hintBetweenClocksDelay > 0f) yield return new WaitForSeconds(hintBetweenClocksDelay);

                if (doubleClockB != null)
                {
                    Tween clockBHour = doubleClockB.AnimateHandToTimeForHint(ClockLearningHandType.Hour, Hour12FromMinutes24(endMinutes24), endMinutes24 % 60, hintHourHandMoveDuration, false);
                    if (clockBHour != null) yield return clockBHour.WaitForCompletion();
                    yield return ShowTimedHintPrompt("Clock B: short hand is in place.");

                    if (hintBetweenHandsDelay > 0f) yield return new WaitForSeconds(hintBetweenHandsDelay);

                    Tween clockBMinute = doubleClockB.AnimateHandToTimeForHint(ClockLearningHandType.Minute, Hour12FromMinutes24(endMinutes24), endMinutes24 % 60, hintMinuteHandMoveDuration, true);
                    if (clockBMinute != null) yield return clockBMinute.WaitForCompletion();
                    yield return ShowTimedHintPrompt("Clock B: long hand is in place.");
                }

                yield return ShowHintLearningPrompt(doubleHintLearningPrompt);
            }

            _hintAnimating = false;
            _acceptInput = true;
            SetClockInputEnabled(true);
            SetSubmitButtons(true);
            SetResetButtons(true);
            SetHintButtonInteractable(false);
            _timerRunning = useTimer;
            RefreshTopBar();
            _hintRoutine = null;
        }

        private IEnumerator ShowTimedHintPrompt(string message)
        {
            bool finished = false;
            ShowFeedback(string.IsNullOrWhiteSpace(message) ? "Good! The hand is in place." : message, false, () => finished = true, hintStepPromptDuration);
            yield return new WaitUntil(() => finished);
        }

        private IEnumerator ShowHintLearningPrompt(string message)
        {
            bool finished = false;
            ShowFeedback(string.IsNullOrWhiteSpace(message) ? "All set! Press Submit." : message, false, () => finished = true, 1.25f);
            yield return new WaitUntil(() => finished);
        }

        private void PulseHintButtonForAttention()
        {
            if (!pulseHintButtonOnWrongAnswer || hintButton == null || !enableHintButton || _hintUsedThisQuestion || _hintAnimating) return;

            audioManager?.PlayHintAttention();
            StopHintAttention();

            Transform hintTransform = hintButton.transform;
            _hintAttentionSequence = DOTween.Sequence().SetUpdate(true);
            _hintAttentionSequence
                .Append(hintTransform.DOPunchScale(Vector3.one * hintAttentionPunchScale, hintAttentionDuration, 8, 0.65f))
                .AppendInterval(0.08f)
                .Append(hintTransform.DOPunchScale(Vector3.one * (hintAttentionPunchScale * 0.75f), hintAttentionDuration, 8, 0.65f))
                .OnComplete(() =>
                {
                    if (hintTransform != null) hintTransform.localScale = Vector3.one;
                });
        }

        private void StopHintAttention()
        {
            _hintAttentionSequence?.Kill();
            _hintAttentionSequence = null;
            if (hintButton != null) hintButton.transform.localScale = Vector3.one;
        }

        private static void SetClockAmPm(Toggle toggle, bool isPm)
        {
            if (toggle != null) toggle.isOn = isPm;
        }

        private static int NormalizeMinutes24(int minutes)
        {
            minutes %= 1440;
            if (minutes < 0) minutes += 1440;
            return minutes;
        }

        private static int Hour12FromMinutes24(int minutes24)
        {
            int hour24 = NormalizeMinutes24(minutes24) / 60;
            int hour12 = hour24 % 12;
            return hour12 == 0 ? 12 : hour12;
        }

        private void EnableRetry()
        {
            _acceptInput = true;
            SetClockInputEnabled(true);
            SetSubmitButtons(true);
            SetHintButtonInteractable(true);
            _timerRunning = useTimer;
        }

        private void AdvanceToNextQuestion()
        {
            _questionIndex++;
            if (_questionIndex >= questionCount)
            {
                ShowResultPanel();
                return;
            }

            LoadCurrentQuestion();
        }

        private void ShowResultPanel()
        {
            _acceptInput = false;
            _timerRunning = false;
            SetClockInputEnabled(false);
            SetSubmitButtons(false);
            EnsureResultButtonLabels();

            if (resultTitleText != null) resultTitleText.text = "Well done!";
            if (resultScoreText != null)
            {
                string hintLine = _hintUsedCount > 0 ? $"\nHints Used: {_hintUsedCount}" : string.Empty;
                resultScoreText.text = $"Final Score: {_score}\nCorrect: {_correctCount}/{questionCount}\nMistakes: {_mistakeCount}{hintLine}";
            }
            ShowGroup(resultPanelGroup, true, false);
        }

        public void ContinueToBloomPostGame()
        {
            if (!TryConsumeButtonPress()) return;

            audioManager?.PlayClick();
            Time.timeScale = 1f;

            if (_bloomPostGameShown) return;
            _bloomPostGameShown = true;

            if (resultPanelGroup != null)
            {
                ShowGroup(resultPanelGroup, false, false);
            }

            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                if (stopBackgroundMusicWhenRewardScreenOpens)
                {
                    audioManager?.StopBackgroundMusic();
                }

                RewardManager.Instance.ShowPostGame(BuildBloomSkillEntries(), BuildBloomEvaluationData());
            }
            else
            {
                if (modeMenuPanelGroup != null)
                {
                    ShowMainMenu();
                }

                onHomePressed?.Invoke();
            }
        }

        public void OnRewardScreenOpen()
        {
            if (stopBackgroundMusicWhenRewardScreenOpens)
            {
                audioManager?.StopBackgroundMusic();
            }
        }

        public void OnPlayAgain()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnHome()
        {
            ExitToHome(finishedMessage);
        }

        private void ExitToHome(string statusMessage)
        {
            Time.timeScale = 1f;
            _acceptInput = false;
            _timerRunning = false;
            if (tutorialController != null) tutorialController.HideInstant();
            SetClockInputEnabled(false);
            SetHintButtonInteractable(false);
            StopHintAttention();
            audioManager?.StopBackgroundMusic();
            NotifyHostGameStatus(statusMessage);

            if (string.IsNullOrWhiteSpace(homeSceneName))
            {
                Debug.LogWarning("Clock Learning Game: Home Scene Name is empty.");
                return;
            }

            SceneManager.LoadScene(homeSceneName);
        }

        private void NotifyHostGameStatus(string statusMessage)
        {
            if (string.IsNullOrWhiteSpace(statusMessage)) return;

            if (sendAndroidMediatorMessage)
            {
                TrySendAndroidMediatorMessage(statusMessage);
            }

            if (sendGameLoaderJsEvent)
            {
                TrySendGameLoaderJsEvent(statusMessage, hostGameName);
            }
        }

        private static void TrySendAndroidMediatorMessage(string statusMessage)
        {
            object mediator = GetSingletonInstance("UnityAndroidMediator");
            if (mediator == null) return;

            MethodInfo method = mediator.GetType().GetMethod("PassDataToAndroid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return;

            try
            {
                method.Invoke(mediator, new object[] { statusMessage });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Clock Learning Game: UnityAndroidMediator message failed. {ex.Message}");
            }
        }

        private static void TrySendGameLoaderJsEvent(string statusMessage, string gameName)
        {
            object loader = GetSingletonInstance("GameLoader");
            if (loader == null) return;

            MethodInfo method = loader.GetType().GetMethod("SendEventToJS", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return;

            try
            {
                method.Invoke(loader, new object[] { statusMessage, string.IsNullOrWhiteSpace(gameName) ? "Clock Game" : gameName });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Clock Learning Game: GameLoader SendEventToJS failed. {ex.Message}");
            }
        }

        private static object GetSingletonInstance(string typeName)
        {
            Type type = null;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(typeName);
                if (type != null) break;
            }

            if (type == null) return null;

            PropertyInfo property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null) return property.GetValue(null, null);

            FieldInfo field = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(null) : null;
        }

        private GameEvaluationData BuildBloomEvaluationData()
        {
            float timeTaken = Mathf.Max(0f, Time.time - _roundStartTime);
            float maxTime = expectedMaxTimeSeconds > 0f ? expectedMaxTimeSeconds : Mathf.Max(1f, questionCount * questionTimeSeconds);
            float timeScore = Mathf.Clamp01(1f - (timeTaken / maxTime));
            int unassistedCorrectCount = Mathf.Max(0, _correctCount - _hintUsedCount);
            float completionAccuracy = questionCount > 0 ? Mathf.Clamp01((float)unassistedCorrectCount / questionCount) : 0f;
            float attemptAccuracy = _attemptCount > 0 ? Mathf.Clamp01((float)unassistedCorrectCount / _attemptCount) : completionAccuracy;
            float accuracyScore = Mathf.Clamp01((completionAccuracy * 0.75f) + (attemptAccuracy * 0.25f));

            return new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = _mistakeCount,
                timeTaken = timeTaken
            };
        }

        private List<SkillEntry> BuildBloomSkillEntries()
        {
            List<SkillEntry> skills = new List<SkillEntry>();

            if (bloomSkills != null)
            {
                for (int i = 0; i < bloomSkills.Count; i++)
                {
                    ClockLearningBloomSkillConfig config = bloomSkills[i];
                    if (config == null) continue;
                    skills.Add(new SkillEntry(config.skillType, Mathf.Max(1f, config.maxScore), config.timeWeight, config.accuracyWeight));
                }
            }

            if (skills.Count == 0)
            {
                skills.Add(new SkillEntry(BloomSkillType.Remember, 50f));
                skills.Add(new SkillEntry(BloomSkillType.Understand, 75f));
                skills.Add(new SkillEntry(BloomSkillType.Apply, 100f, 0.3f, 0.7f));
            }

            return skills;
        }

        private void EnsureResultButtonLabels()
        {
            SetButtonLabel(resultRestartButton, "Restart");
            SetButtonLabel(resultHomeButton, "Continue");
        }

        private void BeginTutorialInputLock()
        {
            _acceptInput = false;
            _timerRunning = false;
            SetSubmitButtons(false);
            SetResetButtons(false);
            SetHintButtonInteractable(false);
            SetClockInputEnabled(false);
        }

        private void OnTutorialComplete()
        {
            _acceptInput = true;
            LoadCurrentQuestion();
            _roundStartTime = Time.time;
        }

        private void ResetClockPositions(bool animate)
        {
            if (gameMode == ClockLearningMode.SingleClockSetTime)
            {
                singleClock?.SetRandomTime(animate);
                return;
            }

            doubleClockA?.SetRandomTime(animate);
            doubleClockB?.SetRandomTime(animate);
            if (clockAPmToggle != null) clockAPmToggle.isOn = false;
            if (clockBPmToggle != null) clockBPmToggle.isOn = true;
            UpdateAmPmLabels();
        }

        private void UpdateTimer()
        {
            if (!useTimer || !_timerRunning || !_acceptInput) return;

            _timerRemaining -= Time.deltaTime;
            if (_timerRemaining <= 0f)
            {
                _timerRemaining = 0f;
                _timerRunning = false;
                _acceptInput = false;
                _mistakeCount++;
                _attemptCount++;
                SetClockInputEnabled(false);
                SetSubmitButtons(false);
                audioManager?.PlayWrong();
                if (!_hintUsedThisQuestion) PulseHintButtonForAttention();
                ShowFeedback("Time's up! Try again.", false, () =>
                {
                    ResetClockPositions(true);
                    _timerRemaining = questionTimeSeconds;
                    EnableRetry();
                    RefreshTopBar();
                });
            }

            RefreshTimerUI();
        }

        private void RefreshTopBar()
        {
            if (questionCounterText != null) questionCounterText.text = $"Question {Mathf.Min(_questionIndex + 1, questionCount)}/{questionCount}";
            if (scoreText != null) scoreText.text = $"Score {_score}";
            RefreshTimerUI();
        }

        private void RefreshTimerUI()
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(useTimer);
                timerText.text = useTimer ? $"Time {Mathf.CeilToInt(_timerRemaining)}" : string.Empty;
            }

            if (timerFillImage != null)
            {
                timerFillImage.gameObject.SetActive(useTimer);
                timerFillImage.fillAmount = useTimer && questionTimeSeconds > 0f ? _timerRemaining / questionTimeSeconds : 0f;
            }
        }

        private void ShowFeedback(string message, bool advance, Action onComplete, float customHoldTime = -1f)
        {
            if (feedbackPanelGroup == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (feedbackText != null) feedbackText.text = message;

            _feedbackSequence?.Kill();
            GameObject panelObject = feedbackPanelGroup.gameObject;
            panelObject.SetActive(true);
            feedbackPanelGroup.alpha = 0f;
            feedbackPanelGroup.interactable = false;
            feedbackPanelGroup.blocksRaycasts = true;
            panelObject.transform.localScale = Vector3.one * 0.92f;

            float holdTime = customHoldTime >= 0f ? customHoldTime : (advance ? 0.7f : 1.0f);
            _feedbackSequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(feedbackPanelGroup.DOFade(1f, 0.16f))
                .Join(panelObject.transform.DOScale(1f, 0.16f).SetEase(Ease.OutBack))
                .AppendInterval(holdTime)
                .Append(feedbackPanelGroup.DOFade(0f, 0.14f))
                .OnComplete(() =>
                {
                    panelObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }

        private void ShowGroup(CanvasGroup group, bool visible, bool instant)
        {
            if (group == null) return;

            group.gameObject.SetActive(true);
            group.interactable = visible;
            group.blocksRaycasts = visible;
            group.DOKill();
            group.transform.DOKill();

            if (instant)
            {
                group.alpha = visible ? 1f : 0f;
                if (!visible) group.gameObject.SetActive(false);
                return;
            }

            group.DOFade(visible ? 1f : 0f, 0.18f)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (!visible) group.gameObject.SetActive(false);
                });
        }

        private void HideAllPanelsInstant()
        {
            ShowGroup(feedbackPanelGroup, false, true);
            ShowGroup(pausePanelGroup, false, true);
            ShowGroup(howToPlayPanelGroup, false, true);
            ShowGroup(resultPanelGroup, false, true);
            if (tutorialController != null) tutorialController.HideInstant();
        }

        private void ConfigureClocksForDifficulty()
        {
            ConfigureClock(singleClock, singleClockHandMode);
            ConfigureClock(doubleClockA, doubleClockHandMode);
            ConfigureClock(doubleClockB, doubleClockHandMode);
        }

        private void ConfigureClock(ClockLearningClockView clock, ClockLearningHandRelationMode relationMode)
        {
            if (clock == null) return;
            clock.SetMinuteSnapInterval(MinuteSnapInterval);
            clock.SetHandRelationMode(relationMode);
            clock.SetDrivenHandSmoothing(smoothDrivenHands, drivenHandSmoothDuration);
            clock.SetCarryHourWhenMinuteCrosses12(carryHourWhenMinuteCrosses12);

            if (applySameClockVisualStyleToAllClocks)
            {
                clock.SetVisualStyle(
                    sharedNumberInsetFromClockEdge,
                    sharedTickInsetFromClockEdge,
                    sharedExtraMarkInsetPixels,
                    sharedHourHandWidth,
                    sharedHourHandHeight,
                    sharedMinuteHandWidth,
                    sharedMinuteHandHeight);
            }
            else
            {
                clock.ForceVisualRefresh();
            }
        }

        public void ApplyConfiguredFonts()
        {
            ApplyPrimaryFont(modeMenuTitleText);
            ApplyPrimaryFont(titleText);
            ApplyPrimaryFont(singleTargetText);
            ApplyPrimaryFont(differenceTargetText);
            ApplyPrimaryFont(feedbackText);
            ApplyPrimaryFont(resultTitleText);

            ApplySecondaryFont(questionCounterText);
            ApplySecondaryFont(scoreText);
            ApplySecondaryFont(timerText);
            ApplySecondaryFont(singlePromptText);
            ApplySecondaryFont(singleLegendText);
            ApplySecondaryFont(differencePromptText);
            ApplySecondaryFont(differenceChipText);
            ApplySecondaryFont(clockAAmPmLabel);
            ApplySecondaryFont(clockBAmPmLabel);
            ApplySecondaryFont(howToPlayText);
            ApplySecondaryFont(howToPageCounterText);
            ApplySecondaryFont(resultScoreText);
            if (tutorialController != null) tutorialController.ApplyFont(secondaryFont);

            ApplyButtonFont(singleModeButton, primaryFont);
            ApplyButtonFont(doubleModeButton, primaryFont);
            ApplyButtonFont(menuHomeButton, secondaryFont);
            ApplyButtonFont(homeButton, secondaryFont);
            ApplyButtonFont(pauseButton, secondaryFont);
            ApplyButtonFont(hintButton, secondaryFont);
            ApplyButtonFont(singleSubmitButton, primaryFont);
            ApplyButtonFont(singleResetButton, secondaryFont);
            ApplyButtonFont(doubleSubmitButton, primaryFont);
            ApplyButtonFont(doubleResetButton, secondaryFont);
            ApplyButtonFont(resumeButton, secondaryFont);
            ApplyButtonFont(pauseRestartButton, secondaryFont);
            ApplyButtonFont(pauseHowToPlayButton, secondaryFont);
            ApplyButtonFont(pauseHomeButton, secondaryFont);
            ApplyButtonFont(howToPreviousButton, secondaryFont);
            ApplyButtonFont(howToNextButton, secondaryFont);
            ApplyButtonFont(closeHowToPlayButton, primaryFont);
            ApplyButtonFont(resultRestartButton, secondaryFont);
            ApplyButtonFont(resultHomeButton, primaryFont);

            if (extraPrimaryTexts != null)
            {
                for (int i = 0; i < extraPrimaryTexts.Count; i++) ApplyPrimaryFont(extraPrimaryTexts[i]);
            }

            if (extraSecondaryTexts != null)
            {
                for (int i = 0; i < extraSecondaryTexts.Count; i++) ApplySecondaryFont(extraSecondaryTexts[i]);
            }
        }

        private void ApplyPrimaryFont(TextMeshProUGUI text)
        {
            ApplyFont(text, primaryFont);
        }

        private void ApplySecondaryFont(TextMeshProUGUI text)
        {
            ApplyFont(text, secondaryFont != null ? secondaryFont : primaryFont);
        }

        private static void ApplyButtonFont(Button button, TMP_FontAsset font)
        {
            if (button == null || font == null) return;
            TextMeshProUGUI[] labels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].font = font;
            }
        }

        private static void ApplyFont(TextMeshProUGUI text, TMP_FontAsset font)
        {
            if (text == null || font == null) return;
            text.font = font;
        }

        private bool TryConsumeButtonPress()
        {
            if (!Application.isPlaying) return true;

            if (buttonDebounceSeconds <= 0f) return true;

            float now = Time.unscaledTime;
            if (now - _lastButtonPressRealtime < buttonDebounceSeconds)
            {
                return false;
            }

            _lastButtonPressRealtime = now;
            return true;
        }

        public void RestartGameFromButton()
        {
            if (!TryConsumeButtonPress()) return;
            audioManager?.PlayClick();
            StartGame();
        }

        private void SetClockInputEnabled(bool enabled)
        {
            if (!lockClockInputWhenPanelsOpen) enabled = _acceptInput;

            bool singleActive = enabled && gameMode == ClockLearningMode.SingleClockSetTime;
            bool doubleActive = enabled && gameMode == ClockLearningMode.DoubleClockTimeDifference;

            if (singleClock != null) singleClock.SetDraggable(singleActive);
            if (doubleClockA != null) doubleClockA.SetDraggable(doubleActive);
            if (doubleClockB != null) doubleClockB.SetDraggable(doubleActive);
        }

        private void SetSubmitButtons(bool interactable)
        {
            if (singleSubmitButton != null) singleSubmitButton.interactable = interactable;
            if (doubleSubmitButton != null) doubleSubmitButton.interactable = interactable;
        }

        private void SetHintButtonInteractable(bool interactable)
        {
            if (hintButton == null) return;
            hintButton.gameObject.SetActive(enableHintButton);
            hintButton.interactable = enableHintButton && interactable && !_hintUsedThisQuestion && !_hintAnimating;
        }

        private void SetResetButtons(bool interactable)
        {
            if (singleResetButton != null) singleResetButton.interactable = interactable;
            if (doubleResetButton != null) doubleResetButton.interactable = interactable;
        }

        private void UpdateAmPmLabels()
        {
            if (clockAAmPmLabel != null) clockAAmPmLabel.text = clockAPmToggle != null && clockAPmToggle.isOn ? "PM" : "AM";
            if (clockBAmPmLabel != null) clockBAmPmLabel.text = clockBPmToggle != null && clockBPmToggle.isOn ? "PM" : "AM";
        }

        private SingleClockQuestion GetCurrentSingleQuestion()
        {
            List<SingleClockQuestion> source = GetActiveSingleQuestions();
            if (source.Count == 0)
            {
                EnsureManualFallbackQuestions();
                source = singleClockQuestions;
            }

            int index = GetCurrentQuestionSourceIndex(source.Count);
            return source[index];
        }

        private TimeDifferenceQuestion GetCurrentDifferenceQuestion()
        {
            List<TimeDifferenceQuestion> source = GetActiveDifferenceQuestions();
            if (source.Count == 0)
            {
                EnsureManualFallbackQuestions();
                source = differenceQuestions;
            }

            int index = GetCurrentQuestionSourceIndex(source.Count);
            return source[index];
        }

        private List<SingleClockQuestion> GetActiveSingleQuestions()
        {
            return generateQuestionsAtRuntime ? _runtimeSingleQuestions : singleClockQuestions;
        }

        private List<TimeDifferenceQuestion> GetActiveDifferenceQuestions()
        {
            return generateQuestionsAtRuntime ? _runtimeDifferenceQuestions : differenceQuestions;
        }

        private int GetCurrentQuestionSourceIndex(int sourceCount)
        {
            if (sourceCount <= 0) return 0;
            if (_questionOrder.Count == 0) return _questionIndex % sourceCount;
            return _questionOrder[Mathf.Clamp(_questionIndex, 0, _questionOrder.Count - 1)] % sourceCount;
        }

        private void BuildQuestionOrder()
        {
            _questionOrder.Clear();
            int sourceCount = gameMode == ClockLearningMode.SingleClockSetTime ? GetActiveSingleQuestions().Count : GetActiveDifferenceQuestions().Count;
            sourceCount = Mathf.Max(1, sourceCount);

            for (int i = 0; i < questionCount; i++)
            {
                _questionOrder.Add(i % sourceCount);
            }

            if (!shuffleQuestions) return;

            System.Random rng = CreateRoundRandom();
            Shuffle(_questionOrder, rng);
        }

        private void BuildRuntimeQuestionsIfNeeded()
        {
            _runtimeSingleQuestions.Clear();
            _runtimeDifferenceQuestions.Clear();
            if (!generateQuestionsAtRuntime) return;

            ClockLearningQuestionGenerationProfile profile = GetActiveGenerationProfile();
            System.Random rng = CreateRoundRandom();

            if (gameMode == ClockLearningMode.SingleClockSetTime)
            {
                GenerateSingleClockQuestions(profile, rng);
            }
            else
            {
                GenerateDifferenceQuestions(profile, rng);
            }
        }

        private ClockLearningQuestionGenerationProfile GetActiveGenerationProfile()
        {
            switch (difficulty)
            {
                case ClockLearningDifficulty.Easy:
                    return easyGeneration ?? ClockLearningQuestionGenerationProfile.Easy();
                case ClockLearningDifficulty.Normal:
                    return normalGeneration ?? ClockLearningQuestionGenerationProfile.Normal();
                default:
                    return hardGeneration ?? ClockLearningQuestionGenerationProfile.Hard();
            }
        }

        private void GenerateSingleClockQuestions(ClockLearningQuestionGenerationProfile profile, System.Random rng)
        {
            List<TimeCandidate> candidates = new List<TimeCandidate>();
            int minHour = Mathf.Clamp(Mathf.Min(profile.singleMinHour, profile.singleMaxHour), 1, 12);
            int maxHour = Mathf.Clamp(Mathf.Max(profile.singleMinHour, profile.singleMaxHour), 1, 12);
            int minMinute = Mathf.Clamp(Mathf.Min(profile.singleMinMinute, profile.singleMaxMinute), 0, 59);
            int maxMinute = Mathf.Clamp(Mathf.Max(profile.singleMinMinute, profile.singleMaxMinute), 0, 59);
            int step = Mathf.Clamp(profile.singleMinuteStep, 1, 30);

            for (int hour = minHour; hour <= maxHour; hour++)
            {
                for (int minute = minMinute; minute <= maxMinute; minute += step)
                {
                    candidates.Add(new TimeCandidate(hour, minute));
                }
            }

            if (candidates.Count == 0)
            {
                candidates.Add(new TimeCandidate(3, 0));
            }

            Shuffle(candidates, rng);
            if (candidates.Count < questionCount)
            {
                Debug.LogWarning($"Clock Learning Game: only {candidates.Count} unique single-clock questions are possible with current {difficulty} generation settings. Increase hour/minute range or reduce question count to avoid repeats.");
            }

            for (int i = 0; i < questionCount; i++)
            {
                TimeCandidate candidate = candidates[i % candidates.Count];
                bool isPm = profile.randomizeSingleAmPm ? rng.Next(0, 2) == 1 : profile.defaultSingleIsPm;
                _runtimeSingleQuestions.Add(new SingleClockQuestion
                {
                    prompt = string.IsNullOrWhiteSpace(profile.singlePrompt) ? "Set the clock to" : profile.singlePrompt,
                    hour = candidate.Hour,
                    minute = candidate.Minute,
                    showAmPm = profile.showAmPmForSingle,
                    isPm = isPm,
                    displayText = BuildTimeDisplayText(candidate.Hour, candidate.Minute, profile, isPm, rng),
                    hint = profile.singleHint
                });
            }
        }

        private void GenerateDifferenceQuestions(ClockLearningQuestionGenerationProfile profile, System.Random rng)
        {
            List<int> candidates = new List<int>();
            int min = Mathf.Clamp(Mathf.Min(profile.differenceMinMinutes, profile.differenceMaxMinutes), 0, 1439);
            int max = Mathf.Clamp(Mathf.Max(profile.differenceMinMinutes, profile.differenceMaxMinutes), 0, 1439);
            int step = Mathf.Clamp(profile.differenceStepMinutes, 1, 60);

            for (int diff = min; diff <= max; diff += step)
            {
                if (profile.avoidZeroDifference && diff == 0) continue;
                candidates.Add(diff);
            }

            if (candidates.Count == 0)
            {
                candidates.Add(60);
            }

            Shuffle(candidates, rng);
            if (candidates.Count < questionCount)
            {
                Debug.LogWarning($"Clock Learning Game: only {candidates.Count} unique time-difference questions are possible with current {difficulty} generation settings. Increase difference range or reduce question count to avoid repeats.");
            }

            for (int i = 0; i < questionCount; i++)
            {
                int diff = candidates[i % candidates.Count];
                _runtimeDifferenceQuestions.Add(new TimeDifferenceQuestion
                {
                    prompt = "Make a time difference of",
                    targetHours = diff / 60,
                    targetMinutes = diff % 60,
                    hint = "Set both clocks so the difference matches the target. Use AM and PM for each clock."
                });
            }
        }

        private string BuildTimeDisplayText(int hour, int minute, ClockLearningQuestionGenerationProfile profile, bool isPm, System.Random rng)
        {
            bool usePhrase = profile.timeTextMode == ClockLearningTimeTextMode.TimePhrase;
            if (profile.timeTextMode == ClockLearningTimeTextMode.Mixed)
            {
                usePhrase = rng.NextDouble() < profile.phraseChance;
            }

            string text = usePhrase && CanBuildTimePhrase(minute)
                ? BuildTimePhrase(hour, minute)
                : $"{hour}:{minute:00}";

            if (profile.showAmPmForSingle)
            {
                text += isPm ? " PM" : " AM";
            }

            return text;
        }

        private static bool CanBuildTimePhrase(int minute)
        {
            return minute == 0 || minute == 5 || minute == 10 || minute == 15 || minute == 20 || minute == 25 || minute == 30 || minute == 35 || minute == 40 || minute == 45 || minute == 50 || minute == 55;
        }

        private static string BuildTimePhrase(int hour, int minute)
        {
            if (minute == 0) return $"{HourWord(hour)} o'clock";
            if (minute == 15) return $"quarter past {HourWord(hour)}";
            if (minute == 30) return $"half past {HourWord(hour)}";
            if (minute == 45) return $"quarter to {HourWord(NextHour(hour))}";

            if (minute < 30)
            {
                return $"{MinuteWord(minute)} past {HourWord(hour)}";
            }

            int minutesToNextHour = 60 - minute;
            return $"{MinuteWord(minutesToNextHour)} to {HourWord(NextHour(hour))}";
        }

        private static string MinuteWord(int minute)
        {
            switch (minute)
            {
                case 5: return "five";
                case 10: return "ten";
                case 20: return "twenty";
                case 25: return "twenty-five";
                default: return minute.ToString();
            }
        }

        private static string HourWord(int hour)
        {
            switch (hour)
            {
                case 1: return "one";
                case 2: return "two";
                case 3: return "three";
                case 4: return "four";
                case 5: return "five";
                case 6: return "six";
                case 7: return "seven";
                case 8: return "eight";
                case 9: return "nine";
                case 10: return "ten";
                case 11: return "eleven";
                default: return "twelve";
            }
        }

        private static int NextHour(int hour)
        {
            return hour >= 12 ? 1 : hour + 1;
        }

        private System.Random CreateRoundRandom()
        {
            int seed;
            if (randomSeed == 0)
            {
                seed = unchecked(Environment.TickCount ^ UnityEngine.Random.Range(0, int.MaxValue) ^ (_roundSeedOffset * 397));
            }
            else
            {
                seed = unchecked(randomSeed + (_roundSeedOffset * 397));
            }

            _roundSeedOffset++;
            return new System.Random(seed);
        }

        private void EnsureManualFallbackQuestions()
        {
            if (singleClockQuestions.Count == 0)
            {
                singleClockQuestions.Add(new SingleClockQuestion { hour = 4, minute = 0, showAmPm = false });
                singleClockQuestions.Add(new SingleClockQuestion { hour = 6, minute = 30, showAmPm = false, displayText = "half past six" });
                singleClockQuestions.Add(new SingleClockQuestion { hour = 2, minute = 15, showAmPm = false, displayText = "quarter past two" });
                singleClockQuestions.Add(new SingleClockQuestion { hour = 7, minute = 45, showAmPm = false, displayText = "quarter to eight" });
                singleClockQuestions.Add(new SingleClockQuestion { hour = 9, minute = 20, showAmPm = false, displayText = "twenty past nine" });
            }

            if (differenceQuestions.Count == 0)
            {
                differenceQuestions.Add(new TimeDifferenceQuestion { targetHours = 1, targetMinutes = 0 });
                differenceQuestions.Add(new TimeDifferenceQuestion { targetHours = 0, targetMinutes = 30 });
                differenceQuestions.Add(new TimeDifferenceQuestion { targetHours = 2, targetMinutes = 30 });
                differenceQuestions.Add(new TimeDifferenceQuestion { targetHours = 3, targetMinutes = 15 });
                differenceQuestions.Add(new TimeDifferenceQuestion { targetHours = 0, targetMinutes = 45 });
            }
        }

        private void AddListeners()
        {
            if (singleModeButton != null) singleModeButton.onClick.AddListener(SelectSingleMode);
            if (doubleModeButton != null) doubleModeButton.onClick.AddListener(SelectDoubleMode);
            if (menuHomeButton != null) menuHomeButton.onClick.AddListener(ExitGameUnfinished);
            if (singleSubmitButton != null) singleSubmitButton.onClick.AddListener(SubmitAnswer);
            if (singleResetButton != null) singleResetButton.onClick.AddListener(ResetCurrentQuestion);
            if (doubleSubmitButton != null) doubleSubmitButton.onClick.AddListener(SubmitAnswer);
            if (doubleResetButton != null) doubleResetButton.onClick.AddListener(ResetCurrentQuestion);
            if (pauseButton != null) pauseButton.onClick.AddListener(OpenPausePanel);
            if (hintButton != null) hintButton.onClick.AddListener(UseHint);
            if (homeButton != null) homeButton.onClick.AddListener(PressHome);
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (pauseRestartButton != null) pauseRestartButton.onClick.AddListener(RestartGameFromButton);
            if (pauseHowToPlayButton != null) pauseHowToPlayButton.onClick.AddListener(OpenHowToPlay);
            if (pauseHomeButton != null) pauseHomeButton.onClick.AddListener(ExitGameUnfinished);
            if (howToPreviousButton != null) howToPreviousButton.onClick.AddListener(ShowPreviousHowToPage);
            if (howToNextButton != null) howToNextButton.onClick.AddListener(ShowNextHowToPage);
            if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);
            if (resultRestartButton != null) resultRestartButton.onClick.AddListener(RestartGameFromButton);
            if (resultHomeButton != null) resultHomeButton.onClick.AddListener(ContinueToBloomPostGame);
            if (_clockAPmListener == null) _clockAPmListener = _ => UpdateAmPmLabels();
            if (_clockBPmListener == null) _clockBPmListener = _ => UpdateAmPmLabels();
            if (clockAPmToggle != null) clockAPmToggle.onValueChanged.AddListener(_clockAPmListener);
            if (clockBPmToggle != null) clockBPmToggle.onValueChanged.AddListener(_clockBPmListener);
        }

        private void RemoveListeners()
        {
            if (singleModeButton != null) singleModeButton.onClick.RemoveListener(SelectSingleMode);
            if (doubleModeButton != null) doubleModeButton.onClick.RemoveListener(SelectDoubleMode);
            if (menuHomeButton != null) menuHomeButton.onClick.RemoveListener(ExitGameUnfinished);
            if (singleSubmitButton != null) singleSubmitButton.onClick.RemoveListener(SubmitAnswer);
            if (singleResetButton != null) singleResetButton.onClick.RemoveListener(ResetCurrentQuestion);
            if (doubleSubmitButton != null) doubleSubmitButton.onClick.RemoveListener(SubmitAnswer);
            if (doubleResetButton != null) doubleResetButton.onClick.RemoveListener(ResetCurrentQuestion);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(OpenPausePanel);
            if (hintButton != null) hintButton.onClick.RemoveListener(UseHint);
            if (homeButton != null) homeButton.onClick.RemoveListener(PressHome);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(ResumeGame);
            if (pauseRestartButton != null) pauseRestartButton.onClick.RemoveListener(RestartGameFromButton);
            if (pauseHowToPlayButton != null) pauseHowToPlayButton.onClick.RemoveListener(OpenHowToPlay);
            if (pauseHomeButton != null) pauseHomeButton.onClick.RemoveListener(ExitGameUnfinished);
            if (howToPreviousButton != null) howToPreviousButton.onClick.RemoveListener(ShowPreviousHowToPage);
            if (howToNextButton != null) howToNextButton.onClick.RemoveListener(ShowNextHowToPage);
            if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.RemoveListener(CloseHowToPlay);
            if (resultRestartButton != null) resultRestartButton.onClick.RemoveListener(RestartGameFromButton);
            if (resultHomeButton != null) resultHomeButton.onClick.RemoveListener(ContinueToBloomPostGame);
            if (clockAPmToggle != null && _clockAPmListener != null) clockAPmToggle.onValueChanged.RemoveListener(_clockAPmListener);
            if (clockBPmToggle != null && _clockBPmListener != null) clockBPmToggle.onValueChanged.RemoveListener(_clockBPmListener);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            if (button == null) return;
            TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = label;
        }

        private static void Shuffle<T>(IList<T> list, System.Random rng)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int swapIndex = rng.Next(i, list.Count);
                T temp = list[i];
                list[i] = list[swapIndex];
                list[swapIndex] = temp;
            }
        }

        private static int CircularDifference(int a, int b, int cycle)
        {
            int diff = Mathf.Abs(a - b) % cycle;
            return Mathf.Min(diff, cycle - diff);
        }

        private struct TimeCandidate
        {
            public int Hour;
            public int Minute;

            public TimeCandidate(int hour, int minute)
            {
                Hour = hour;
                Minute = minute;
            }
        }
    }
}
