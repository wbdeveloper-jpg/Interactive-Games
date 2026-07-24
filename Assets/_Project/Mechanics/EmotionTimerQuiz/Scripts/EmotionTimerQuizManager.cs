using System.Collections;
using System.Collections.Generic;
using RewardSystem;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EmotionTimerQuiz
{
    public class EmotionTimerQuizManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("Data")]
        public EmotionTimerQuizQuestionSet questionSet;
        public List<CharacterSpriteEntry> assetRegistry = new List<CharacterSpriteEntry>();
        public bool shuffleQuestions = true;
        [Tooltip("0 means use all questions after progressive level limiting. Recommended: 25.")]
        public int questionLimit = 25;
        [Min(1)] public int defaultTimeLimitSeconds = 15;
        public bool useSampleQuestionsIfQuestionSetMissing = true;

        [Header("Progressive Level Question Count")]
        public bool useProgressiveQuestionCount = true;
        [Min(1)] public int firstPlayQuestionCount = 5;
        [Min(1)] public int secondPlayQuestionCount = 10;
        [Min(1)] public int questionIncreasePerCompletedPlay = 5;
        [Min(1)] public int maxProgressiveQuestionCount = 25;
        public string completedPlayPrefsKey = "EmotionTimerQuiz_CompletedPlayCount";

        [Header("Bloom Reward System")]
        public bool useBloomRewardSystem = true;
        public string homeSceneName = "Loader Scene";
        [Tooltip("Fallback expected time used only if question time data is missing.")]
        public float expectedMaxTimeFallbackSeconds = 120f;
        private readonly List<SkillEntry> bloomSkills = new List<SkillEntry>
        {
            new SkillEntry(BloomSkillType.Evaluate, 100f),
            new SkillEntry(BloomSkillType.Understand, 75f)
        };

        [Header("Fonts")]
        public TMP_FontAsset primaryFont;
        public TMP_FontAsset secondaryFont;
        public bool applyFontsOnAwake = true;
        [Tooltip("Texts with these words in their object name use Secondary Font. Everything else uses Primary Font.")]
        public List<string> secondaryFontObjectNameKeywords = new List<string>
        {
            "Body",
            "GuideText",
            "TapText",
            "Feedback",
            "Counter"
        };

        [Header("Startup Loading")]
        public bool showLoadingPanelOnStart = true;
        public string gameTitle = "Emotion Timer Quiz";
        [Min(0.1f)] public float loadingDurationSeconds = 1.5f;

        [Header("Score")]
        public int baseScorePerCorrect = 10;
        public bool useSpeedBonus = true;
        public int speedBonusPerSecond = 1;

        [Header("Auto Continue")]
        public bool autoContinueAfterAnswer = true;
        [Min(1)] public int autoContinueDelaySeconds = 10;

        [Header("UI References")]
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI situationText;
        public TextMeshProUGUI feedbackText;
        public RectTransform situationCardTransform;
        [HideInInspector, FormerlySerializedAs("timerFillImage")] public Image timerFillImage;
        public Slider timerSlider;
        public EmotionOptionCard[] optionCards = new EmotionOptionCard[3];
        public Button nextRoundButton;
        public TextMeshProUGUI nextRoundButtonText;
        public Image nextRoundCountdownFillImage;
        [FormerlySerializedAs("menuButton")] public Button pauseButton;
        public TextMeshProUGUI pauseButtonText;
        public GameObject timeoutBanner;
        public GameObject howToPlayPanel;
        public GameObject pausePanel;
        public GameObject resultPanel;
        public TextMeshProUGUI resultTitleText;
        public TextMeshProUGUI resultScoreText;
        public TextMeshProUGUI resultStatsText;
        public Button resultContinueButton;
        public Button resumeButton;
        public Button restartButton;
        public Button resultRestartButton;
        public Button pauseHowToPlayButton;
        [Tooltip("Optional always-available How To Play button in the gameplay UI.")]
        public Button howToPlayButton;

        [Header("Loading Panel References")]
        public GameObject loadingPanel;
        public TextMeshProUGUI loadingTitleText;
        public Slider loadingSlider;

        [Header("How To Play Guide")]
        public List<Sprite> guideImages = new List<Sprite>();
        [TextArea(2, 5)] public List<string> guideFallbackTexts = new List<string>();
        public Image guideImage;
        public TextMeshProUGUI guideText;
        public TextMeshProUGUI guideCounterText;
        public TextMeshProUGUI guideStartButtonText;
        public Button guidePrevButton;
        public Button guideNextButton;
        public Button guideStartButton;
        [Tooltip("Master switch. Turn this off to prevent How To Play from opening when the game starts. The manual How To Play button will still work.")]
        public bool openHowToPlayAutomaticallyOnGameStart = true;
        public HowToPlayDisplayMode howToPlayDisplayMode = HowToPlayDisplayMode.EveryGameStartAutomatically;
        public string howToPlayViewedPrefsKey = "EmotionTimerQuiz_HowToPlayViewed";
        [HideInInspector] public bool showHowToPlayOnStart = true;
        [HideInInspector] public bool autoStartIfHowToPlayDisabled = true;

        [Header("First-Time Interactive Tutorial")]
        public EmotionFirstTimeTutorialController firstTimeTutorialController;

        [Header("Card Colors")]
        public Color cardAColor = new Color(0.82f, 0.95f, 0.84f, 1f);
        public Color cardBColor = new Color(0.89f, 0.86f, 0.98f, 1f);
        public Color cardCColor = new Color(1f, 0.85f, 0.85f, 1f);
        public Color timerNormalColor = new Color(1f, 0.63f, 0.60f, 1f);
        public Color timerLowColor = new Color(1f, 0.36f, 0.36f, 1f);
        public int lowTimeWarningSeconds = 5;

        [Header("Audio")]
        public EmotionTimerQuizAudioManager audioManager;

        private readonly Dictionary<CharacterType, Dictionary<ExpressionType, Sprite>> spriteLookup = new Dictionary<CharacterType, Dictionary<ExpressionType, Sprite>>();
        private readonly List<SituationQuestion> activeQuestions = new List<SituationQuestion>();
        private EmotionQuizState currentState = EmotionQuizState.None;
        private EmotionQuizState stateBeforeGuide = EmotionQuizState.None;
        private Coroutine timerCoroutine;
        private Coroutine autoContinueCoroutine;
        private Coroutine loadingCoroutine;
        private Coroutine startupFlowCoroutine;
        private int currentQuestionIndex = -1;
        private int score;
        private int correctCount;
        private int wrongCount;
        private int timeoutCount;
        private float totalTimeTaken;
        private bool completedPlayRegistered;
        private float expectedMaxTimeForRun;
        private float remainingTime;
        private int currentQuestionTimeLimit;
        private int guidePageIndex;
        private bool hasGameStarted;

        private void Awake()
        {
            BuildSpriteLookup();
            BindButtons();
            ApplyConfiguredFonts();
        }

        private void Start()
        {
            SetPanel(loadingPanel, false, false);
            SetPanel(timeoutBanner, false, false);
            SetPanel(howToPlayPanel, false, false);
            SetPanel(pausePanel, false, false);
            SetPanel(resultPanel, false, false);
            ResetNextRoundButton();

            if (pauseButtonText != null)
            {
                pauseButtonText.text = "PAUSE";
            }

            if (loadingTitleText != null)
            {
                loadingTitleText.text = gameTitle;
            }

            ApplyConfiguredFonts();
            BeginStartupFlow();
        }

        private void OnDisable()
        {
            StopTimer();
            StopAutoContinue();
            StopLoading();
            StopStartupFlow();
            DOTween.Kill(transform);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ApplyConfiguredFonts();
            }
        }

        [ContextMenu("Apply Fonts To All TextMeshPro")]
        public void ApplyConfiguredFonts()
        {
            if (!applyFontsOnAwake)
            {
                return;
            }

            if (primaryFont == null && secondaryFont == null)
            {
                return;
            }

            Transform searchRoot = transform.root != null ? transform.root : transform;
            TextMeshProUGUI[] texts = searchRoot.GetComponentsInChildren<TextMeshProUGUI>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TextMeshProUGUI tmp = texts[i];
                if (tmp == null)
                {
                    continue;
                }

                TMP_FontAsset chosenFont = ShouldUseSecondaryFont(tmp) ? secondaryFont : primaryFont;
                if (chosenFont != null)
                {
                    tmp.font = chosenFont;
                }
            }
        }

        private bool ShouldUseSecondaryFont(TextMeshProUGUI tmp)
        {
            if (tmp == null || secondaryFont == null)
            {
                return false;
            }

            if (secondaryFontObjectNameKeywords == null)
            {
                return false;
            }

            string objectName = tmp.gameObject.name;
            for (int i = 0; i < secondaryFontObjectNameKeywords.Count; i++)
            {
                string keyword = secondaryFontObjectNameKeywords[i];
                if (!string.IsNullOrWhiteSpace(keyword) && objectName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void BeginStartupFlow()
        {
            StopStartupFlow();
            StopLoading();
            hasGameStarted = false;
            SetOptionCardsInteractable(false);
            startupFlowCoroutine = StartCoroutine(StartupFlowRoutine());
        }

        private IEnumerator StartupFlowRoutine()
        {
            currentState = EmotionQuizState.Loading;
            SetPanel(loadingPanel, false, false);
            SetPanel(howToPlayPanel, false, false);
            SetPanel(pausePanel, false, false);
            SetPanel(resultPanel, false, false);
            SetPanel(timeoutBanner, false, false);

            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPreGame(bloomSkills);
                yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
            }

            if (showLoadingPanelOnStart && loadingPanel != null)
            {
                yield return LoadingRoutine();
            }

            startupFlowCoroutine = null;
            ContinueAfterStartupLoading();
        }

        private IEnumerator LoadingRoutine()
        {
            currentState = EmotionQuizState.Loading;
            SetPanel(howToPlayPanel, false, false);
            SetPanel(pausePanel, false, false);
            SetPanel(resultPanel, false, false);
            SetPanel(timeoutBanner, false, false);
            SetPanel(loadingPanel, true);

            if (loadingTitleText != null)
            {
                loadingTitleText.text = gameTitle;
                loadingTitleText.transform.DOKill();
                loadingTitleText.transform.localScale = Vector3.one;
                loadingTitleText.transform.DOPunchScale(Vector3.one * 0.06f, 0.45f, 4, 0.7f).SetUpdate(true);
            }

            if (loadingSlider != null)
            {
                loadingSlider.minValue = 0f;
                loadingSlider.maxValue = 1f;
                loadingSlider.value = 0f;
            }

            float duration = Mathf.Max(0.1f, loadingDurationSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                if (loadingSlider != null)
                {
                    loadingSlider.value = progress;
                }

                yield return null;
            }

            if (loadingSlider != null)
            {
                loadingSlider.value = 1f;
            }

            loadingCoroutine = null;
        }

        private void StopLoading()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
        }

        private void StopStartupFlow()
        {
            if (startupFlowCoroutine != null)
            {
                StopCoroutine(startupFlowCoroutine);
                startupFlowCoroutine = null;
            }
        }

        private void ContinueAfterStartupLoading()
        {
            SetPanel(loadingPanel, false);

            if (ShouldShowHowToPlayAutomatically() && howToPlayPanel != null)
            {
                ShowHowToPlay(false);
            }
            else
            {
                ContinueToTutorialOrGame();
            }
        }

        private bool ShouldShowHowToPlayAutomatically()
        {
            if (!openHowToPlayAutomaticallyOnGameStart)
            {
                return false;
            }

            switch (howToPlayDisplayMode)
            {
                case HowToPlayDisplayMode.FirstTimeAutomatically:
                    return !HasViewedHowToPlay();

                case HowToPlayDisplayMode.EveryGameStartAutomatically:
                    return true;

                case HowToPlayDisplayMode.ManualButtonOnly:
                default:
                    return false;
            }
        }

        private bool HasViewedHowToPlay()
        {
            return !string.IsNullOrEmpty(howToPlayViewedPrefsKey) && PlayerPrefs.GetInt(howToPlayViewedPrefsKey, 0) == 1;
        }

        private void MarkHowToPlayViewed()
        {
            if (string.IsNullOrEmpty(howToPlayViewedPrefsKey))
            {
                return;
            }

            PlayerPrefs.SetInt(howToPlayViewedPrefsKey, 1);
            PlayerPrefs.Save();
        }

        private void ContinueToTutorialOrGame()
        {
            if (firstTimeTutorialController != null && firstTimeTutorialController.ShouldPlayTutorial())
            {
                firstTimeTutorialController.BeginTutorial();
                return;
            }

            StartGame();
        }

        [ContextMenu("Reset How To Play First-Time Status")]
        public void ResetHowToPlayFirstTimeStatus()
        {
            if (!string.IsNullOrEmpty(howToPlayViewedPrefsKey))
            {
                PlayerPrefs.DeleteKey(howToPlayViewedPrefsKey);
                PlayerPrefs.Save();
            }
        }

        public void StartGame()
        {
            StopTimer();
            StopAutoContinue();
            BuildSpriteLookup();
            BuildActiveQuestionList();

            score = 0;
            correctCount = 0;
            wrongCount = 0;
            timeoutCount = 0;
            totalTimeTaken = 0f;
            completedPlayRegistered = false;
            currentQuestionIndex = -1;
            currentState = EmotionQuizState.None;
            hasGameStarted = true;

            SetPanel(loadingPanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(timeoutBanner, false);

            if (activeQuestions.Count == 0)
            {
                Debug.LogWarning("EmotionTimerQuizManager: No questions available.");
                ShowResult();
                return;
            }

            LoadNextRound();
        }

        public void LoadNextRound()
        {
            StopTimer();
            StopAutoContinue();
            ResetNextRoundButton();

            if (audioManager != null && currentQuestionIndex >= 0)
            {
                audioManager.PlayNextRound();
            }

            currentQuestionIndex++;

            if (currentQuestionIndex >= activeQuestions.Count)
            {
                ShowResult();
                return;
            }

            SituationQuestion question = activeQuestions[currentQuestionIndex];
            currentState = EmotionQuizState.Playing;
            currentQuestionTimeLimit = Mathf.Max(1, question.timeLimitSeconds > 0 ? question.timeLimitSeconds : defaultTimeLimitSeconds);
            remainingTime = currentQuestionTimeLimit;

            SetPanel(timeoutBanner, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            if (situationText != null)
            {
                situationText.text = question.situationText;
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.interactable = false;
            }

            SetupOptionsForQuestion(question);
            UpdateHUD();
            AnimateNewRound();
            StartTimer();
        }

        public void TogglePause()
        {
            if (audioManager != null)
            {
                audioManager.PlayButton();
            }

            if (currentState == EmotionQuizState.Playing)
            {
                PauseGame();
                return;
            }

            if (currentState == EmotionQuizState.Paused)
            {
                ResumeGame();
            }
        }

        public void PauseGame()
        {
            if (currentState != EmotionQuizState.Playing)
            {
                return;
            }

            currentState = EmotionQuizState.Paused;
            StopTimer();
            SetOptionCardsInteractable(false);
            SetPanel(pausePanel, true);
        }

        public void ResumeGame()
        {
            if (currentState != EmotionQuizState.Paused && currentState != EmotionQuizState.ShowingHowToPlay)
            {
                return;
            }

            currentState = EmotionQuizState.Playing;
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetOptionCardsInteractable(true);
            StartTimer();
        }

        public void RestartGame()
        {
            if (audioManager != null)
            {
                audioManager.PlayButton();
            }

            StartGame();
        }

        public void OpenBloomPostGame()
        {
            PlayButtonSound();

            if (!useBloomRewardSystem || RewardManager.Instance == null)
            {
                Debug.LogWarning("EmotionTimerQuizManager: Bloom RewardManager.Instance not found. Make sure RewardManager exists once in LoadingScene and persists with DontDestroyOnLoad.");
                return;
            }

            RewardManager.Instance.ShowPostGame(bloomSkills, BuildBloomEvaluationData());
        }

        private GameEvaluationData BuildBloomEvaluationData()
        {
            int totalQuestions = Mathf.Max(1, activeQuestions.Count);
            float timeScore = Mathf.Clamp01(1f - (totalTimeTaken / Mathf.Max(1f, expectedMaxTimeForRun)));
            float accuracyScore = Mathf.Clamp01((float)correctCount / totalQuestions);

            return new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = wrongCount + timeoutCount,
                timeTaken = totalTimeTaken
            };
        }

        public void OnRewardScreenOpen()
        {
            if (audioManager != null)
            {
                audioManager.StopMusic();
            }
        }

        public void OnPlayAgain()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnHome()
        {
            if (RewardManager.Instance != null)
                RewardManager.Instance.HideAll();

            if (UnityAndroidMediator.Instance != null)
                UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

            //if (GameLoader.Instance != null)
            //    GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");
            SceneManager.LoadScene(homeSceneName);
        }

        public void ShowHowToPlayFromPause()
        {
            ShowHowToPlay(true);
        }

        public void OpenHowToPlayManual()
        {
            ShowHowToPlay(hasGameStarted);
        }

        public void ShowHowToPlay(bool openedFromPause)
        {
            StopTimer();
            StopAutoContinue();
            stateBeforeGuide = openedFromPause ? EmotionQuizState.Paused : currentState;
            currentState = EmotionQuizState.ShowingHowToPlay;
            guidePageIndex = 0;
            SetOptionCardsInteractable(false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(timeoutBanner, false);
            SetPanel(howToPlayPanel, true);
            UpdateGuidePage();
        }

        public void GuideNext()
        {
            int pageCount = GetGuidePageCount();
            guidePageIndex = Mathf.Clamp(guidePageIndex + 1, 0, pageCount - 1);
            UpdateGuidePage();
            PlayButtonSound();
        }

        public void GuidePrevious()
        {
            int pageCount = GetGuidePageCount();
            guidePageIndex = Mathf.Clamp(guidePageIndex - 1, 0, pageCount - 1);
            UpdateGuidePage();
            PlayButtonSound();
        }

        public void GuideStartOrContinue()
        {
            PlayButtonSound();
            MarkHowToPlayViewed();

            if (!hasGameStarted)
            {
                SetPanel(howToPlayPanel, false, false);
                ContinueToTutorialOrGame();
                return;
            }

            SetPanel(howToPlayPanel, false);

            if (stateBeforeGuide == EmotionQuizState.Paused)
            {
                ResumeGame();
                return;
            }

            currentState = EmotionQuizState.Playing;
            SetOptionCardsInteractable(true);
            StartTimer();
        }

        private void ShowResult()
        {
            currentState = EmotionQuizState.Result;
            StopTimer();
            StopAutoContinue();
            RegisterCompletedPlayOnce();
            SetOptionCardsInteractable(false);
            SetPanel(timeoutBanner, false);
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(resultPanel, true);

            int questionsPlayed = Mathf.Max(0, activeQuestions.Count);

            if (resultTitleText != null)
            {
                resultTitleText.text = "Great Work!";
            }

            if (resultScoreText != null)
            {
                resultScoreText.text = "Final Score: " + score;
            }

            if (resultStatsText != null)
            {
                resultStatsText.text =
                    "Questions: " + questionsPlayed +
                    "\nCorrect: " + correctCount +
                    "\nWrong: " + wrongCount +
                    "\nTimed Out: " + timeoutCount +
                    "\nTime Taken: " + FormatSeconds(totalTimeTaken);
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.interactable = false;
            }
        }

        private string FormatSeconds(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
            return minutes > 0 ? minutes + "m " + remainingSeconds + "s" : remainingSeconds + "s";
        }

        private void BindButtons()
        {
            if (nextRoundButton != null)
            {
                nextRoundButton.onClick.RemoveListener(HandleNextRoundClicked);
                nextRoundButton.onClick.AddListener(HandleNextRoundClicked);
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(TogglePause);
                pauseButton.onClick.AddListener(TogglePause);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ResumeGame);
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
                restartButton.onClick.AddListener(RestartGame);
            }

            if (resultRestartButton != null)
            {
                resultRestartButton.onClick.RemoveListener(RestartGame);
                resultRestartButton.onClick.AddListener(RestartGame);
            }

            if (resultContinueButton != null)
            {
                resultContinueButton.onClick.RemoveListener(OpenBloomPostGame);
                resultContinueButton.onClick.AddListener(OpenBloomPostGame);
            }

            if (pauseHowToPlayButton != null)
            {
                pauseHowToPlayButton.onClick.RemoveListener(ShowHowToPlayFromPause);
                pauseHowToPlayButton.onClick.AddListener(ShowHowToPlayFromPause);
            }

            if (howToPlayButton != null)
            {
                howToPlayButton.onClick.RemoveListener(OpenHowToPlayManual);
                howToPlayButton.onClick.AddListener(OpenHowToPlayManual);
            }

            if (guidePrevButton != null)
            {
                guidePrevButton.onClick.RemoveListener(GuidePrevious);
                guidePrevButton.onClick.AddListener(GuidePrevious);
            }

            if (guideNextButton != null)
            {
                guideNextButton.onClick.RemoveListener(GuideNext);
                guideNextButton.onClick.AddListener(GuideNext);
            }

            if (guideStartButton != null)
            {
                guideStartButton.onClick.RemoveListener(GuideStartOrContinue);
                guideStartButton.onClick.AddListener(GuideStartOrContinue);
            }
        }

        private void HandleNextRoundClicked()
        {
            if (currentState != EmotionQuizState.AnswerLocked && currentState != EmotionQuizState.Timeout)
            {
                return;
            }

            LoadNextRound();
        }

        private void BuildSpriteLookup()
        {
            spriteLookup.Clear();

            if (assetRegistry == null || assetRegistry.Count == 0)
            {
                assetRegistry = EmotionTimerQuizUtility.CreateEmptySpriteRegistry();
            }

            for (int i = 0; i < assetRegistry.Count; i++)
            {
                CharacterSpriteEntry characterEntry = assetRegistry[i];

                if (characterEntry == null)
                {
                    continue;
                }

                if (!spriteLookup.ContainsKey(characterEntry.character))
                {
                    spriteLookup.Add(characterEntry.character, new Dictionary<ExpressionType, Sprite>());
                }

                Dictionary<ExpressionType, Sprite> expressionMap = spriteLookup[characterEntry.character];

                for (int e = 0; e < characterEntry.expressionSprites.Count; e++)
                {
                    ExpressionSpriteEntry spriteEntry = characterEntry.expressionSprites[e];

                    if (spriteEntry == null)
                    {
                        continue;
                    }

                    expressionMap[spriteEntry.expression] = spriteEntry.sprite;
                }
            }
        }

        private void BuildActiveQuestionList()
        {
            activeQuestions.Clear();

            if (questionSet != null && questionSet.questions != null)
            {
                for (int i = 0; i < questionSet.questions.Count; i++)
                {
                    SituationQuestion question = questionSet.questions[i];
                    if (question != null && !string.IsNullOrWhiteSpace(question.situationText))
                    {
                        activeQuestions.Add(question);
                    }
                }
            }

            if (activeQuestions.Count == 0 && useSampleQuestionsIfQuestionSetMissing)
            {
                activeQuestions.AddRange(CreateRuntimeSampleQuestions());
            }

            if (shuffleQuestions)
            {
                EmotionTimerQuizUtility.FisherYatesShuffle(activeQuestions);
            }

            int effectiveLimit = GetEffectiveQuestionLimit();
            if (effectiveLimit > 0 && activeQuestions.Count > effectiveLimit)
            {
                activeQuestions.RemoveRange(effectiveLimit, activeQuestions.Count - effectiveLimit);
            }

            expectedMaxTimeForRun = 0f;
            for (int i = 0; i < activeQuestions.Count; i++)
            {
                SituationQuestion question = activeQuestions[i];
                int seconds = question != null && question.timeLimitSeconds > 0 ? question.timeLimitSeconds : defaultTimeLimitSeconds;
                expectedMaxTimeForRun += Mathf.Max(1, seconds);
            }

            if (expectedMaxTimeForRun <= 0f)
            {
                expectedMaxTimeForRun = Mathf.Max(1f, expectedMaxTimeFallbackSeconds);
            }
        }

        private int GetEffectiveQuestionLimit()
        {
            int hardLimit = questionLimit > 0 ? questionLimit : int.MaxValue;

            if (!useProgressiveQuestionCount)
            {
                return hardLimit == int.MaxValue ? 0 : hardLimit;
            }

            int completedPlays = PlayerPrefs.GetInt(completedPlayPrefsKey, 0);
            int progressiveLimit;

            if (completedPlays <= 0)
            {
                progressiveLimit = firstPlayQuestionCount;
            }
            else if (completedPlays == 1)
            {
                progressiveLimit = secondPlayQuestionCount;
            }
            else
            {
                progressiveLimit = secondPlayQuestionCount + ((completedPlays - 1) * questionIncreasePerCompletedPlay);
            }

            progressiveLimit = Mathf.Clamp(progressiveLimit, 1, Mathf.Max(1, maxProgressiveQuestionCount));
            return Mathf.Min(hardLimit, progressiveLimit);
        }

        private void RegisterCompletedPlayOnce()
        {
            if (completedPlayRegistered || string.IsNullOrEmpty(completedPlayPrefsKey))
            {
                return;
            }

            completedPlayRegistered = true;
            int completedPlays = PlayerPrefs.GetInt(completedPlayPrefsKey, 0);
            PlayerPrefs.SetInt(completedPlayPrefsKey, completedPlays + 1);
            PlayerPrefs.Save();
        }

        public bool PrepareTutorialPracticeRound(
            int preferredQuestionIndex,
            System.Action<EmotionOptionCard> selectedCallback,
            out EmotionOptionCard correctCard)
        {
            correctCard = null;
            StopTimer();
            StopAutoContinue();
            BuildSpriteLookup();

            SituationQuestion practiceQuestion = GetTutorialPracticeQuestion(preferredQuestionIndex);
            if (practiceQuestion == null)
            {
                Debug.LogWarning("EmotionTimerQuizManager: No valid question is available for tutorial practice.");
                return false;
            }

            currentState = EmotionQuizState.Tutorial;
            hasGameStarted = false;
            SetPanel(loadingPanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(timeoutBanner, false);

            if (roundText != null)
            {
                roundText.text = "PRACTICE";
            }

            if (scoreText != null)
            {
                scoreText.text = "SCORE: 0";
            }

            if (timerText != null)
            {
                timerText.text = "TAKE YOUR TIME";
            }

            if (timerSlider != null)
            {
                timerSlider.DOKill();
                timerSlider.value = 1f;
            }

            if (timerFillImage != null)
            {
                timerFillImage.DOKill();
                timerFillImage.fillAmount = 1f;
            }

            if (situationText != null)
            {
                situationText.text = practiceQuestion.situationText;
            }

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            if (nextRoundButton != null)
            {
                nextRoundButton.interactable = false;
            }

            ResetNextRoundButton();
            correctCard = SetupOptionsForQuestion(practiceQuestion, selectedCallback);
            SetOptionCardsInteractable(false);
            AnimateNewRound();
            return correctCard != null;
        }

        public void SetTutorialOptionCardsInteractable(bool interactable)
        {
            if (currentState == EmotionQuizState.Tutorial)
            {
                SetOptionCardsInteractable(interactable);
            }
        }

        private SituationQuestion GetTutorialPracticeQuestion(int preferredQuestionIndex)
        {
            if (questionSet != null && questionSet.questions != null && questionSet.questions.Count > 0)
            {
                int startIndex = Mathf.Clamp(preferredQuestionIndex, 0, questionSet.questions.Count - 1);
                for (int offset = 0; offset < questionSet.questions.Count; offset++)
                {
                    SituationQuestion candidate = questionSet.questions[(startIndex + offset) % questionSet.questions.Count];
                    if (candidate != null && !string.IsNullOrWhiteSpace(candidate.situationText))
                    {
                        return candidate;
                    }
                }
            }

            if (useSampleQuestionsIfQuestionSetMissing)
            {
                List<SituationQuestion> samples = CreateRuntimeSampleQuestions();
                if (samples.Count > 0)
                {
                    return samples[Mathf.Clamp(preferredQuestionIndex, 0, samples.Count - 1)];
                }
            }

            return null;
        }

        private void SetupOptionsForQuestion(SituationQuestion question)
        {
            SetupOptionsForQuestion(question, HandleCardSelected);
        }

        private EmotionOptionCard SetupOptionsForQuestion(SituationQuestion question, System.Action<EmotionOptionCard> selectedCallback)
        {
            List<EmotionOptionData> options = GenerateOptions(question);
            Color[] cardColors = { cardAColor, cardBColor, cardCColor };
            char[] letters = { 'A', 'B', 'C' };
            EmotionOptionCard correctCard = null;

            for (int i = 0; i < optionCards.Length; i++)
            {
                if (optionCards[i] == null)
                {
                    continue;
                }

                if (i < options.Count)
                {
                    optionCards[i].gameObject.SetActive(true);
                    optionCards[i].Setup(letters[i], options[i], cardColors[Mathf.Clamp(i, 0, cardColors.Length - 1)], selectedCallback);
                    if (options[i].isCorrect)
                    {
                        correctCard = optionCards[i];
                    }
                }
                else
                {
                    optionCards[i].gameObject.SetActive(false);
                }
            }

            return correctCard;
        }

        private List<EmotionOptionData> GenerateOptions(SituationQuestion question)
        {
            List<ExpressionType> fodderPool = GetExpressionsForCharacter(question.targetCharacter);
            fodderPool.Remove(question.correctExpression);
            EmotionTimerQuizUtility.FisherYatesShuffle(fodderPool);

            List<EmotionOptionData> options = new List<EmotionOptionData>();
            options.Add(new EmotionOptionData(question.correctExpression, GetSprite(question.targetCharacter, question.correctExpression), true));

            int fodderNeeded = 2;
            for (int i = 0; i < fodderPool.Count && fodderNeeded > 0; i++)
            {
                ExpressionType wrongExpression = fodderPool[i];
                options.Add(new EmotionOptionData(wrongExpression, GetSprite(question.targetCharacter, wrongExpression), false));
                fodderNeeded--;
            }

            for (int i = 0; i < EmotionTimerQuizUtility.AllExpressions.Length && fodderNeeded > 0; i++)
            {
                ExpressionType expression = EmotionTimerQuizUtility.AllExpressions[i];
                if (expression == question.correctExpression)
                {
                    continue;
                }

                bool alreadyUsed = false;
                for (int o = 0; o < options.Count; o++)
                {
                    if (options[o].expression == expression)
                    {
                        alreadyUsed = true;
                        break;
                    }
                }

                if (!alreadyUsed)
                {
                    options.Add(new EmotionOptionData(expression, GetSprite(question.targetCharacter, expression), false));
                    fodderNeeded--;
                }
            }

            EmotionTimerQuizUtility.FisherYatesShuffle(options);
            return options;
        }

        private List<ExpressionType> GetExpressionsForCharacter(CharacterType character)
        {
            List<ExpressionType> expressions = new List<ExpressionType>();

            if (spriteLookup.ContainsKey(character))
            {
                foreach (KeyValuePair<ExpressionType, Sprite> pair in spriteLookup[character])
                {
                    if (!expressions.Contains(pair.Key))
                    {
                        expressions.Add(pair.Key);
                    }
                }
            }

            if (expressions.Count < 3)
            {
                expressions.Clear();
                for (int i = 0; i < EmotionTimerQuizUtility.AllExpressions.Length; i++)
                {
                    expressions.Add(EmotionTimerQuizUtility.AllExpressions[i]);
                }
            }

            return expressions;
        }

        private Sprite GetSprite(CharacterType character, ExpressionType expression)
        {
            if (!spriteLookup.ContainsKey(character))
            {
                return null;
            }

            Dictionary<ExpressionType, Sprite> expressionMap = spriteLookup[character];
            if (!expressionMap.ContainsKey(expression))
            {
                return null;
            }

            return expressionMap[expression];
        }

        private void HandleCardSelected(EmotionOptionCard selectedCard)
        {
            if (currentState != EmotionQuizState.Playing || selectedCard == null || selectedCard.OptionData == null)
            {
                return;
            }

            currentState = EmotionQuizState.AnswerLocked;
            totalTimeTaken += Mathf.Clamp(currentQuestionTimeLimit - remainingTime, 0f, currentQuestionTimeLimit);
            StopTimer();
            SetOptionCardsInteractable(false);

            if (selectedCard.OptionData.isCorrect)
            {
                correctCount++;
                int earned = baseScorePerCorrect;
                if (useSpeedBonus)
                {
                    earned += Mathf.FloorToInt(remainingTime) * Mathf.Max(0, speedBonusPerSecond);
                }

                score += earned;
                selectedCard.ShowCorrect();

                if (feedbackText != null)
                {
                    feedbackText.text = "+" + earned + " Correct!";
                    feedbackText.transform.DOKill();
                    feedbackText.transform.DOPunchScale(Vector3.one * 0.08f, 0.28f, 6, 0.8f).SetUpdate(true);
                }

                if (audioManager != null)
                {
                    audioManager.PlayCorrect();
                }
            }
            else
            {
                wrongCount++;
                selectedCard.ShowWrong();
                RevealCorrectCard();

                if (feedbackText != null)
                {
                    feedbackText.text = "Good try! Look at the correct answer.";
                }

                if (audioManager != null)
                {
                    audioManager.PlayWrong();
                }
            }

            UnlockNextRoundWithAutoContinue();
            UpdateHUD();
        }

        private void StartTimer()
        {
            StopTimer();
            UpdateTimerUI();
            timerCoroutine = StartCoroutine(TimerRoutine());
        }

        private void StopTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private IEnumerator TimerRoutine()
        {
            while (remainingTime > 0f && currentState == EmotionQuizState.Playing)
            {
                yield return new WaitForSecondsRealtime(1f);

                if (currentState != EmotionQuizState.Playing)
                {
                    timerCoroutine = null;
                    yield break;
                }

                remainingTime = Mathf.Max(0f, remainingTime - 1f);
                UpdateTimerUI();
            }

            timerCoroutine = null;

            if (currentState == EmotionQuizState.Playing && remainingTime <= 0f)
            {
                HandleTimeout();
            }
        }

        private void HandleTimeout()
        {
            if (currentState != EmotionQuizState.Playing)
            {
                return;
            }

            currentState = EmotionQuizState.Timeout;
            timeoutCount++;
            totalTimeTaken += currentQuestionTimeLimit;
            SetOptionCardsInteractable(false);
            RevealCorrectCard();
            SetPanel(timeoutBanner, true);

            if (feedbackText != null)
            {
                feedbackText.text = "Time's Up!";
            }

            if (audioManager != null)
            {
                audioManager.PlayTimeout();
            }

            UnlockNextRoundWithAutoContinue();
            UpdateHUD();
        }

        private void UnlockNextRoundWithAutoContinue()
        {
            if (nextRoundButton != null)
            {
                nextRoundButton.interactable = true;
            }

            if (autoContinueAfterAnswer)
            {
                StartAutoContinueCountdown();
            }
            else
            {
                UpdateNextRoundButtonText(-1);
            }
        }

        private void StartAutoContinueCountdown()
        {
            StopAutoContinue();
            autoContinueCoroutine = StartCoroutine(AutoContinueRoutine());
        }

        private void StopAutoContinue()
        {
            if (autoContinueCoroutine != null)
            {
                StopCoroutine(autoContinueCoroutine);
                autoContinueCoroutine = null;
            }
        }

        private IEnumerator AutoContinueRoutine()
        {
            int visibleSeconds = Mathf.Max(1, autoContinueDelaySeconds);

            while (visibleSeconds > 0 && (currentState == EmotionQuizState.AnswerLocked || currentState == EmotionQuizState.Timeout))
            {
                UpdateNextRoundButtonText(visibleSeconds);
                yield return new WaitForSecondsRealtime(1f);
                visibleSeconds--;
            }

            autoContinueCoroutine = null;

            if (currentState == EmotionQuizState.AnswerLocked || currentState == EmotionQuizState.Timeout)
            {
                LoadNextRound();
            }
        }

        private void UpdateNextRoundButtonText(int seconds)
        {
            if (nextRoundButtonText != null)
            {
                nextRoundButtonText.text = seconds > 0 ? "NEXT ROUND (" + seconds + "s)" : "NEXT ROUND";
            }

            if (nextRoundCountdownFillImage != null)
            {
                float fill = seconds > 0 ? Mathf.Clamp01((float)seconds / Mathf.Max(1, autoContinueDelaySeconds)) : 0f;
                nextRoundCountdownFillImage.DOKill();
                nextRoundCountdownFillImage.DOFillAmount(fill, 0.15f).SetUpdate(true);
            }
        }

        private void ResetNextRoundButton()
        {
            UpdateNextRoundButtonText(-1);

            if (nextRoundCountdownFillImage != null)
            {
                nextRoundCountdownFillImage.fillAmount = 0f;
            }
        }

        private void RevealCorrectCard()
        {
            for (int i = 0; i < optionCards.Length; i++)
            {
                if (optionCards[i] != null && optionCards[i].OptionData != null && optionCards[i].OptionData.isCorrect)
                {
                    optionCards[i].ShowCorrectReveal();
                }
            }
        }

        private void SetOptionCardsInteractable(bool interactable)
        {
            for (int i = 0; i < optionCards.Length; i++)
            {
                if (optionCards[i] != null)
                {
                    optionCards[i].SetInteractable(interactable);
                }
            }
        }

        private void UpdateHUD()
        {
            if (roundText != null)
            {
                int displayRound = Mathf.Clamp(currentQuestionIndex + 1, 0, activeQuestions.Count);
                roundText.text = "ROUND " + displayRound + " / " + activeQuestions.Count;
            }

            if (scoreText != null)
            {
                scoreText.text = "SCORE: " + score;
            }

            UpdateTimerUI();
        }

        private void UpdateTimerUI()
        {
            int visibleTime = Mathf.CeilToInt(remainingTime);

            if (timerText != null)
            {
                timerText.text = "TIME LEFT: " + visibleTime + "s";

                if (visibleTime <= lowTimeWarningSeconds && currentState == EmotionQuizState.Playing)
                {
                    timerText.transform.DOKill();
                    timerText.transform.DOPunchScale(Vector3.one * 0.08f, 0.18f, 4, 0.7f).SetUpdate(true);
                }
            }

            float fill = currentQuestionTimeLimit <= 0 ? 0f : Mathf.Clamp01(remainingTime / currentQuestionTimeLimit);

            if (timerSlider != null)
            {
                timerSlider.DOKill();
                timerSlider.DOValue(fill, 0.18f).SetUpdate(true);

                Image fillGraphic = timerSlider.fillRect != null ? timerSlider.fillRect.GetComponent<Image>() : null;
                if (fillGraphic != null)
                {
                    fillGraphic.color = visibleTime <= lowTimeWarningSeconds ? timerLowColor : timerNormalColor;
                }
            }

            // Backward compatibility for old scenes still using filled Image.
            if (timerFillImage != null)
            {
                timerFillImage.DOKill();
                timerFillImage.DOFillAmount(fill, 0.18f).SetUpdate(true);
                timerFillImage.color = visibleTime <= lowTimeWarningSeconds ? timerLowColor : timerNormalColor;
            }
        }

        private void AnimateNewRound()
        {
            if (situationCardTransform != null)
            {
                situationCardTransform.DOKill();
                situationCardTransform.localScale = Vector3.one * 0.96f;
                situationCardTransform.DOScale(Vector3.one, 0.24f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void UpdateGuidePage()
        {
            int pageCount = GetGuidePageCount();
            guidePageIndex = Mathf.Clamp(guidePageIndex, 0, pageCount - 1);

            Sprite pageSprite = null;
            if (guideImages != null && guidePageIndex < guideImages.Count)
            {
                pageSprite = guideImages[guidePageIndex];
            }

            string fallback = GetGuideFallbackText(guidePageIndex);

            if (guideImage != null)
            {
                guideImage.sprite = pageSprite;
                guideImage.enabled = pageSprite != null;
                guideImage.preserveAspect = true;
            }

            if (guideText != null)
            {
                if (pageSprite == null)
                {
                    guideText.text = fallback;
                    guideText.gameObject.SetActive(true);
                }
                else
                {
                    guideText.text = string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback;
                    guideText.gameObject.SetActive(!string.IsNullOrWhiteSpace(guideText.text));
                }
            }

            if (guideCounterText != null)
            {
                guideCounterText.text = (guidePageIndex + 1) + " / " + pageCount;
            }

            if (guidePrevButton != null)
            {
                guidePrevButton.interactable = guidePageIndex > 0;
            }

            if (guideNextButton != null)
            {
                guideNextButton.interactable = guidePageIndex < pageCount - 1;
            }

            if (guideStartButtonText != null)
            {
                guideStartButtonText.text = hasGameStarted ? "CONTINUE" : "START";
            }
        }

        private int GetGuidePageCount()
        {
            int imageCount = guideImages == null ? 0 : guideImages.Count;
            int textCount = guideFallbackTexts == null ? 0 : guideFallbackTexts.Count;
            return Mathf.Max(1, Mathf.Max(imageCount, textCount));
        }

        private string GetGuideFallbackText(int index)
        {
            if (guideFallbackTexts != null && index >= 0 && index < guideFallbackTexts.Count && !string.IsNullOrWhiteSpace(guideFallbackTexts[index]))
            {
                return guideFallbackTexts[index];
            }

            if (index == 0)
            {
                return "Read the situation carefully. Tap the emotion card that best matches how the character feels.";
            }

            if (index == 1)
            {
                return "Answer before the timer reaches zero. Faster correct answers can give bonus points.";
            }

            return "After each answer, tap NEXT ROUND or wait for the countdown to continue automatically.";
        }

        private void SetPanel(GameObject panel, bool active)
        {
            SetPanel(panel, active, true);
        }

        private void SetPanel(GameObject panel, bool active, bool animate)
        {
            if (panel == null)
            {
                return;
            }

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            canvasGroup.DOKill();

            Transform animatedTransform = panel.transform.Find("Panel");
            if (animatedTransform == null)
            {
                animatedTransform = panel.transform;
            }

            animatedTransform.DOKill();

            if (active)
            {
                panel.SetActive(true);
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;

                if (animate && gameObject.activeInHierarchy)
                {
                    canvasGroup.alpha = 0f;
                    animatedTransform.localScale = Vector3.one * 0.96f;
                    canvasGroup.DOFade(1f, 0.18f).SetUpdate(true);
                    animatedTransform.DOScale(Vector3.one, 0.24f).SetEase(Ease.OutBack).SetUpdate(true);
                }
                else
                {
                    canvasGroup.alpha = 1f;
                    animatedTransform.localScale = Vector3.one;
                }
            }
            else
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;

                if (animate && gameObject.activeInHierarchy && panel.activeSelf)
                {
                    canvasGroup.DOFade(0f, 0.12f).SetUpdate(true).OnComplete(() =>
                    {
                        if (panel != null)
                        {
                            panel.SetActive(false);
                        }
                    });
                }
                else
                {
                    panel.SetActive(false);
                    canvasGroup.alpha = 0f;
                }
            }
        }

        private void PlayButtonSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayButton();
            }
        }

        private List<SituationQuestion> CreateRuntimeSampleQuestions()
        {
            List<SituationQuestion> samples = new List<SituationQuestion>();

            samples.Add(new SituationQuestion
            {
                id = "Q001",
                situationText = "Rajes sees a massive spider on his bed!",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q002",
                situationText = "Tina gets a new box of crayons from her teacher.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q003",
                situationText = "Raj's paper boat tears before the race starts.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q004",
                situationText = "Tanvi practices hard and speaks clearly on stage.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            samples.Add(new SituationQuestion
            {
                id = "Q005",
                situationText = "Raj finds out his class is going on a picnic tomorrow!",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            samples.Add(new SituationQuestion
            {
                id = "Q006",
                situationText = "Tina drops her ice cream before taking a bite.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q007",
                situationText = "Rajes tells the truth even when it feels difficult.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 14
            });

            samples.Add(new SituationQuestion
            {
                id = "Q008",
                situationText = "Tanvi sees her little brother break her favorite pencil on purpose.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.ANGRY,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q009",
                situationText = "Raj hears thunder loudly while walking home.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 12
            });

            samples.Add(new SituationQuestion
            {
                id = "Q010",
                situationText = "Tina wins the classroom drawing star badge.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            samples.Add(new SituationQuestion
            {
                id = "Q011",
                situationText = "Rajes shares his lunch with a friend who forgot food.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q012",
                situationText = "Tanvi cannot find her school project before class.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 13
            });

            samples.Add(new SituationQuestion
            {
                id = "Q013",
                situationText = "Raj finishes reading a story aloud without mistakes.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            samples.Add(new SituationQuestion
            {
                id = "Q014",
                situationText = "Tina waits quietly while others get a turn first.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q015",
                situationText = "Rajes sees someone push his friend in the line.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.ANGRY,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q016",
                situationText = "Tanvi gets invited to play a new game at recess.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q017",
                situationText = "Raj opens a gift and finds the toy he wanted.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            samples.Add(new SituationQuestion
            {
                id = "Q018",
                situationText = "Tina has to speak in front of the whole class for the first time.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 12
            });

            samples.Add(new SituationQuestion
            {
                id = "Q019",
                situationText = "Rajes solves a hard maths puzzle by himself.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            samples.Add(new SituationQuestion
            {
                id = "Q020",
                situationText = "Tanvi loses a race after trying her best.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.SAD,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q021",
                situationText = "Raj sees his friend take his eraser without asking.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.ANGRY,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q022",
                situationText = "Tina helps a new student find the classroom.",
                targetCharacter = CharacterType.TINA,
                correctExpression = ExpressionType.HAPPY,
                timeLimitSeconds = 15
            });

            samples.Add(new SituationQuestion
            {
                id = "Q023",
                situationText = "Rajes hears that tomorrow is the school fun fair.",
                targetCharacter = CharacterType.RAJES,
                correctExpression = ExpressionType.EXCITED,
                timeLimitSeconds = 10
            });

            samples.Add(new SituationQuestion
            {
                id = "Q024",
                situationText = "Tanvi stands up and answers the teacher clearly.",
                targetCharacter = CharacterType.TANVI,
                correctExpression = ExpressionType.CONFIDENT,
                timeLimitSeconds = 12
            });

            samples.Add(new SituationQuestion
            {
                id = "Q025",
                situationText = "Raj notices a puppy stuck near a busy road.",
                targetCharacter = CharacterType.RAJ,
                correctExpression = ExpressionType.SCARED,
                timeLimitSeconds = 12
            });

            return samples;
        }
    }
}
