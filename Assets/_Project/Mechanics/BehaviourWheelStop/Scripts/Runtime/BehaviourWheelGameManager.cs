using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;

namespace BehaviourWheelStop
{
    public enum BehaviourWheelHowToPlayDisplayMode
    {
        FirstTimeAutomatically,
        EveryGameStartAutomatically,
        ManualButtonOnly
    }

    public class BehaviourWheelGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("References")]
        public BehaviourWheelQuestionBank questionBank;
        public BehaviourWheelSpinner spinner;
        public BehaviourWheelUI ui;
        public BehaviourWheelPausePanel pausePanel;
        public BehaviourWheelResultPanel resultPanel;
        public BehaviourWheelFontTheme fontTheme;
        public BehaviourWheelAudioController audioController;
        public BehaviourWheelFirstTimeTutorial firstTimeTutorial;

        [Header("Quiz Mode")]
        public BehaviourWheelQuizMode quizMode = BehaviourWheelQuizMode.Behaviour;
        public BehaviourWheelDifficulty difficulty = BehaviourWheelDifficulty.Easy;
        public bool filterByDifficulty = false;

        [Header("Round Settings")]
        public int questionsPerRound = 5;
        public int scorePerCorrect = 10;
        public float loadingDuration = 0.65f;
        [Tooltip("Main feedback wait before next question. Existing scenes may keep their old serialized value, so Minimum Feedback Duration below is also respected.")]
        public float feedbackDuration = 1.8f;
        [Tooltip("Guarantees feedback stays visible long enough even if an older scene still has a smaller serialized Feedback Duration.")]
        public float minimumFeedbackDuration = 1.8f;
        [Header("How To Play Behaviour")]
        public BehaviourWheelHowToPlayDisplayMode howToPlayDisplayMode = BehaviourWheelHowToPlayDisplayMode.FirstTimeAutomatically;
        [Tooltip("Kept only so existing scene serialization is not broken. The dropdown above now controls automatic display.")]
        [HideInInspector] public bool showHowToPlayAtStart = true;

        [Header("Bloom Reward Integration")]
        public bool useBloomReward = true;
        public float expectedMaxTimeForBloom = 120f;
        public string homeSceneName = "Loader Scene";
        [SerializeField] private List<SkillEntry> bloomSkills = new List<SkillEntry>
        {
            new SkillEntry(BloomSkillType.Remember, 80f),
            new SkillEntry(BloomSkillType.Understand, 100f),
            new SkillEntry(BloomSkillType.Apply, 50f)
        };

        [Header("State Debug")]
        [SerializeField] private int currentQuestionIndex;
        [SerializeField] private int score;
        [SerializeField] private int correctCount;
        [SerializeField] private int wrongCount;
        [SerializeField] private float roundStartTime;

        private readonly List<BehaviourWheelQuestionData> roundQuestions = new List<BehaviourWheelQuestionData>();
        private BehaviourWheelQuestionData currentQuestion;
        private bool waitingForNextQuestion;
        private bool isPaused;
        private bool howToPlayOpenedFromPause;
        private bool localResultShown;
        private bool bloomPostShown;
        private bool howToPlayOpenedAtStartup;

        private string HowToPlaySeenKey =>
            $"BehaviourWheelStop.HowToPlay.Seen.{SceneManager.GetActiveScene().name}";

        private void Awake()
        {
            if (ui != null)
            {
                if (ui.stopButton != null)
                    ui.stopButton.onClick.AddListener(StopWheel);

                if (ui.pauseButton != null)
                    ui.pauseButton.onClick.AddListener(OpenPause);

                if (ui.howToPlayPrevButton != null)
                    ui.howToPlayPrevButton.onClick.AddListener(ui.PreviousHowToPlayPage);

                if (ui.howToPlayNextButton != null)
                    ui.howToPlayNextButton.onClick.AddListener(ui.NextHowToPlayPage);

                if (ui.howToPlayStartButton != null)
                    ui.howToPlayStartButton.onClick.AddListener(StartRoundFromHowToPlay);
            }

            if (spinner != null)
                spinner.StoppedOnSlice += OnWheelStopped;

            if (pausePanel != null)
                pausePanel.SetButtons(ResumeGame, ShowHowToPlayFromPause, RestartRound, OnHome);

            if (resultPanel != null)
                resultPanel.SetButtons(RestartRound, ShowBloomPostGame);
        }

        private void Start()
        {
            if (firstTimeTutorial != null)
                firstTimeTutorial.PrepareSavedStateForTesting();

            if (fontTheme != null)
                fontTheme.ApplyFontsToScene();

            if (useBloomReward && RewardManager.Instance != null)
            {
                if (ui != null)
                    ui.HideAllMainPanelsImmediate();

                RewardManager.Instance.ShowPreGame(bloomSkills);
                StartCoroutine(BloomPreGameThenLoadingRoutine());
            }
            else
            {
                StartCoroutine(LoadingRoutine());
            }
        }

        private IEnumerator BloomPreGameThenLoadingRoutine()
        {
            yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
            yield return LoadingRoutine();
        }

        private IEnumerator LoadingRoutine()
        {
            if (audioController != null && audioController.playBgmOnGameStart)
                audioController.PlayBackgroundMusic();

            if (ui != null)
            {
                ui.ShowLoading();
                ui.SetLoadingProgress(0f);
            }

            float timer = 0f;
            while (timer < loadingDuration)
            {
                timer += Time.deltaTime;
                if (ui != null)
                    ui.SetLoadingProgress(loadingDuration <= 0f ? 1f : timer / loadingDuration);
                yield return null;
            }

            if (ui != null)
                ui.SetLoadingProgress(1f);

            if (audioController != null)
                audioController.PlayPanelOpen();

            if (ShouldShowHowToPlayAutomatically() && ui != null)
            {
                howToPlayOpenedAtStartup = true;
                ui.ShowHowToPlay();
            }
            else
                BeginTutorialOrRound();
        }

        public void StartRoundFromHowToPlay()
        {
            if (audioController != null)
                audioController.PlayButtonClick();

            if (howToPlayOpenedFromPause)
            {
                howToPlayOpenedFromPause = false;
                PlayerPrefs.SetInt(HowToPlaySeenKey, 1);
                PlayerPrefs.Save();
                ResumeGame();
                if (ui != null)
                    ui.ShowGameplay();
                return;
            }

            if (howToPlayOpenedAtStartup)
            {
                howToPlayOpenedAtStartup = false;
                PlayerPrefs.SetInt(HowToPlaySeenKey, 1);
                PlayerPrefs.Save();
            }

            BeginTutorialOrRound();
        }

        private bool ShouldShowHowToPlayAutomatically()
        {
            switch (howToPlayDisplayMode)
            {
                case BehaviourWheelHowToPlayDisplayMode.EveryGameStartAutomatically:
                    return true;

                case BehaviourWheelHowToPlayDisplayMode.ManualButtonOnly:
                    return false;

                default:
                    return PlayerPrefs.GetInt(HowToPlaySeenKey, 0) == 0;
            }
        }

        private void BeginTutorialOrRound()
        {
            if (firstTimeTutorial != null && firstTimeTutorial.ShouldPlayTutorial())
            {
                firstTimeTutorial.BeginTutorial(StartRound);
                return;
            }

            StartRound();
        }

        public void StartRound()
        {
            if (firstTimeTutorial != null && firstTimeTutorial.IsRunning)
                return;

            isPaused = false;
            waitingForNextQuestion = false;
            localResultShown = false;
            bloomPostShown = false;
            currentQuestionIndex = 0;
            score = 0;
            correctCount = 0;
            wrongCount = 0;
            roundStartTime = Time.time;

            roundQuestions.Clear();
            if (questionBank != null)
                roundQuestions.AddRange(questionBank.GetRoundQuestions(questionsPerRound, quizMode, difficulty, filterByDifficulty));

            if (ui != null)
            {
                ui.HideFeedback();
                ui.ShowGameplay();
                ui.SetScore(score);
            }

            LoadNextQuestion();
        }

        public void RestartRound()
        {
            if (firstTimeTutorial != null && firstTimeTutorial.IsRunning)
                return;

            if (audioController != null)
                audioController.PlayButtonClick();

            StopAllCoroutines();
            Time.timeScale = 1f;
            if (ui != null)
                ui.HidePause();

            StartRound();
        }

        public void OpenPause()
        {
            if ((firstTimeTutorial != null && firstTimeTutorial.IsRunning) || waitingForNextQuestion || localResultShown)
                return;

            if (audioController != null)
                audioController.PlayPauseOpen();

            isPaused = true;
            Time.timeScale = 0f;
            if (ui != null)
                ui.ShowPause();
        }

        public void ResumeGame()
        {
            if (audioController != null)
                audioController.PlayButtonClick();

            isPaused = false;
            Time.timeScale = 1f;
            if (ui != null)
                ui.HidePause();
        }

        public void ShowHowToPlayFromPause()
        {
            if (audioController != null)
                audioController.PlayButtonClick();

            howToPlayOpenedFromPause = true;
            if (ui != null)
            {
                ui.HidePause();
                ui.ShowHowToPlay();
            }
        }

        public void StopWheel()
        {
            if (isPaused || waitingForNextQuestion || spinner == null || localResultShown)
                return;

            if (firstTimeTutorial != null && firstTimeTutorial.IsRunning)
            {
                if (!firstTimeTutorial.CanAcceptPlayerStop)
                    return;

                if (audioController != null)
                {
                    audioController.PlayButtonClick();
                    audioController.PlayStopWheel();
                }

                if (ui != null)
                {
                    ui.SetStopButtonInteractable(false);
                    ui.PlayStopButtonTapAnimation();
                }

                firstTimeTutorial.HandlePlayerStop();
                return;
            }

            if (audioController != null)
            {
                audioController.PlayButtonClick();
                audioController.PlayStopWheel();
            }

            if (ui != null)
            {
                ui.SetStopButtonInteractable(false);
                ui.PlayStopButtonTapAnimation();
            }

            spinner.StopNow();
        }

        private void LoadNextQuestion()
        {
            if (currentQuestionIndex >= roundQuestions.Count)
            {
                ShowResult();
                return;
            }

            currentQuestion = roundQuestions[currentQuestionIndex];

            if (spinner != null)
            {
                spinner.SetupOptions(currentQuestion.options);
                spinner.StartSpin();
            }

            if (ui != null)
            {
                ui.HideFeedback();
                ui.SetStopButtonInteractable(true);
                ui.SetGameplayTexts(currentQuestionIndex + 1, roundQuestions.Count, currentQuestion.questionText, score);
            }
        }

        private void OnWheelStopped(int sliceIndex, string selectedAnswer)
        {
            if (firstTimeTutorial != null && firstTimeTutorial.IsRunning)
                return;

            if (waitingForNextQuestion || currentQuestion == null || localResultShown)
                return;

            bool correct = string.Equals(selectedAnswer, currentQuestion.correctAnswer, System.StringComparison.OrdinalIgnoreCase);
            if (correct)
            {
                correctCount++;
                score += scorePerCorrect;
                if (audioController != null)
                    audioController.PlayCorrect();
            }
            else
            {
                wrongCount++;
                if (audioController != null)
                    audioController.PlayWrong();
            }

            if (audioController != null)
                audioController.PlayFeedbackPopup();

            if (ui != null)
            {
                ui.SetScore(score);
                ui.ShowFeedback(correct, selectedAnswer, currentQuestion.correctAnswer, currentQuestion.explanation);
            }

            StartCoroutine(FeedbackThenNextRoutine());
        }

        private IEnumerator FeedbackThenNextRoutine()
        {
            waitingForNextQuestion = true;
            float waitTime = Mathf.Max(0f, feedbackDuration, minimumFeedbackDuration);
            yield return new WaitForSeconds(waitTime);
            waitingForNextQuestion = false;
            currentQuestionIndex++;
            LoadNextQuestion();
        }

        private void ShowResult()
        {
            localResultShown = true;

            if (audioController != null)
                audioController.PlayResult();

            if (ui != null)
            {
                ui.HideFeedback();
                ui.ShowResultPanel();
            }

            if (resultPanel != null)
                resultPanel.ShowResult(score, correctCount, wrongCount, roundQuestions.Count);
        }

        public void ShowBloomPostGame()
        {
            if (bloomPostShown)
                return;

            bloomPostShown = true;
            Time.timeScale = 1f;

            if (audioController != null)
                audioController.PlayButtonClick();

            if (!useBloomReward || RewardManager.Instance == null)
            {
                OnHome();
                return;
            }

            if (ui != null)
                ui.HideAllMainPanelsImmediate();

            GameEvaluationData eval = BuildEvaluationData();
            RewardManager.Instance.ShowPostGame(bloomSkills, eval);
        }

        private GameEvaluationData BuildEvaluationData()
        {
            float timeTaken = Mathf.Max(0f, Time.time - roundStartTime);
            float safeExpectedTime = Mathf.Max(1f, expectedMaxTimeForBloom);
            float timeScore = Mathf.Clamp01(1f - (timeTaken / safeExpectedTime));
            float accuracyScore = roundQuestions.Count > 0
                ? Mathf.Clamp01((float)correctCount / roundQuestions.Count)
                : 0f;

            return new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = wrongCount,
                timeTaken = timeTaken
            };
        }

        public void OnRewardScreenOpen()
        {
            if (audioController != null)
                audioController.StopBackgroundMusic();
        }

        public void OnPlayAgain()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnHome()
        {
            Time.timeScale = 1f;
            if (audioController != null)
                audioController.StopBackgroundMusic();

            if (RewardManager.Instance != null)
                RewardManager.Instance.HideAll();

            if (UnityAndroidMediator.Instance != null)
                UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

            //if (GameLoader.Instance != null)
            //    GameLoader.Instance.SendEventToJS("Game Done", "Behaviour Wheel");

            SceneManager.LoadScene(homeSceneName);
        }

        [ContextMenu("Reset How To Play Seen State For This Scene")]
        public void ResetHowToPlaySeenStateForThisScene()
        {
            PlayerPrefs.DeleteKey(HowToPlaySeenKey);
            PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            if (spinner != null)
                spinner.StoppedOnSlice -= OnWheelStopped;
        }
    }
}
