using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using RewardSystem;

namespace DictationGame
{
    public sealed class DictationGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        private enum GameState
        {
            Boot,
            HowToPlay,
            RoundActive,
            Result,
            Paused,
            SessionComplete
        }

        private enum EvaluationResult
        {
            Perfect,
            CloseEnough,
            Wrong
        }

        private struct RoundReview
        {
            public string Title;
            public string CorrectAnswer;
            public string PlayerAnswer;
            public bool Success;
            public bool CloseEnough;
            public int Score;
            public int AttemptsUsed;
            public int HintsUsed;
            public int ReplaysUsed;
        }

        [Header("Question Set")]
        [SerializeField] private DictationQuestionSet questionSet;

        [Header("Sub Systems")]
        [SerializeField] private DictationAudioManager audioManager;
        [SerializeField] private DictationHintSystem hintSystem;
        [SerializeField] private DictationKeyboard keyboard;

        [Header("Top Bar")]
        [SerializeField] private TextMeshProUGUI roundTitleText;
        [SerializeField] private TextMeshProUGUI roundProgressText;
        [SerializeField] private TextMeshProUGUI difficultyBadgeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button pauseButton;

        [Header("Difficulty Colors")]
        [SerializeField] private Color easyColor = new Color(0.66f, 0.84f, 0.73f);
        [SerializeField] private Color mediumColor = new Color(0.96f, 0.86f, 0.63f);
        [SerializeField] private Color hardColor = new Color(0.90f, 0.65f, 0.62f);

        [Header("Answer UI")]
        [SerializeField] private TMP_InputField answerInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI inlineFeedbackText;

        [Header("How To Play Panel")]
        [SerializeField] private bool showHowToPlayOnStart = true;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private TextMeshProUGUI howToPlayBodyText;
        [SerializeField] private Button gotItButton;

        [Header("Pause Panel")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button quitButton;

        [Header("Result Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultDetailText;
        [SerializeField] private TextMeshProUGUI correctAnswerText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button continueButton;

        [Header("Session Summary Panel")]
        [SerializeField] private GameObject sessionSummaryPanel;
        [SerializeField] private TextMeshProUGUI summaryTitleText;
        [SerializeField] private TextMeshProUGUI summaryScoreText;
        [SerializeField] private TextMeshProUGUI summaryBreakdownText;
        [SerializeField] private Button replaySessionButton;
        [SerializeField] private Button summaryQuitButton;

        [Header("Effects")]
        [SerializeField] private ParticleSystem correctParticles;
        [SerializeField] private CanvasGroup sceneCanvasGroup;
        [SerializeField] private bool fadeInOnStart = true;
        [SerializeField] private float fadeInDuration = 0.25f;

        [Header("Gameplay Rules")]
        [Tooltip("OFF = scored challenge flow. ON = practice flow where player can retry the same round before continuing.")]
        [SerializeField] private bool allowRetryCurrentRound = false;
        [SerializeField] private bool requireAudioBeforeSubmit = true;

        [Header("Bloom Reward Integration")]
        [SerializeField] private bool useBloomRewardSystem = true;
        [Tooltip("Used by Bloom timeScore. Example: 120 seconds means faster completion scores higher.")]
        [Min(1f)] [SerializeField] private float expectedMaxSessionTime = 120f;
        [SerializeField] private string homeSceneName = "Loader Scene";

        [Header("Scoring")]
        [Min(0)] [SerializeField] private int baseScore = 100;
        [Min(0)] [SerializeField] private int wrongAttemptCost = 10;
        [Min(1)] [SerializeField] private int maxAttempts = 3;
        [Min(0)] [SerializeField] private int closeEnoughPenalty = 5;
        [Min(0)] [SerializeField] private int levenshteinTolerance = 2;

        public static event Action<int> OnRoundComplete;
        public static event Action<int> OnSessionComplete;
        public static event Action OnQuitRequested;

        private readonly List<SkillEntry> bloomSkills = new List<SkillEntry>
        {
            new SkillEntry(BloomSkillType.Remember, 100f),
            new SkillEntry(BloomSkillType.Understand, 75f),
            new SkillEntry(BloomSkillType.Apply, 50f)
        };

        private readonly List<DictationRoundData> sessionRounds = new List<DictationRoundData>();
        private readonly List<RoundReview> roundReviews = new List<RoundReview>();

        private GameState state = GameState.Boot;
        private GameState stateBeforePause = GameState.RoundActive;
        private int currentIndex;
        private int currentScore;
        private int totalSessionScore;
        private int attemptCount;
        private int hintsUsedThisRound;
        private int replaysUsedThisRound;
        private int correctRoundCount;
        private int wrongRoundCount;
        private int totalMistakeCount;
        private float sessionStartTime;
        private bool bloomPostGameShown;
        private bool roundEnded;
        private bool currentRoundCommitted;
        private RoundReview pendingReview;

        private void Awake()
        {
            BindButtons();
            HideAllPanels();
            SetInlineFeedback(string.Empty, false);
        }

        private void OnEnable()
        {
            if (audioManager != null) audioManager.OnReplayUsed += HandleReplayUsed;
            if (hintSystem != null) hintSystem.OnHintUsed += HandleHintUsed;
        }

        private void OnDisable()
        {
            if (audioManager != null) audioManager.OnReplayUsed -= HandleReplayUsed;
            if (hintSystem != null) hintSystem.OnHintUsed -= HandleHintUsed;
        }

        private void Start()
        {
            PlaySceneFadeIn();
            StartCoroutine(BootFlow());
        }

        private IEnumerator BootFlow()
        {
            state = GameState.Boot;
            HideAllPanels();
            SetGameplayInteractable(false);
            SetInlineFeedback(string.Empty, false);

            if (!HasMinimumSetup())
            {
                ShowHowToPlayWithMessage("Setup missing. Assign Question Set and check all generated references on DictationGameManager.");
                yield break;
            }

            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPreGame(bloomSkills);
                yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
            }

            BootGame();
        }

        public void BootGame()
        {
            state = GameState.Boot;
            HideAllPanels();
            SetGameplayInteractable(false);
            SetInlineFeedback(string.Empty, false);

            if (!HasMinimumSetup())
            {
                ShowHowToPlayWithMessage("Setup missing. Assign Question Set and check all generated references on DictationGameManager.");
                return;
            }

            if (showHowToPlayOnStart)
                ShowHowToPlayWithMessage(BuildDefaultHowToPlayText());
            else
                StartNewSession();
        }

        public void OnGotItPressed()
        {
            StartNewSession();
        }

        public void StartNewSession()
        {
            if (questionSet == null)
            {
                ShowHowToPlayWithMessage("No Question Set assigned. Select DictationGameManager and assign your Question Set.");
                return;
            }

            List<DictationRoundData> builtSession = questionSet.BuildSessionList();
            if (builtSession.Count == 0)
            {
                ShowHowToPlayWithMessage("Question Set has no playable questions. Add questions with answer text first.");
                return;
            }

            sessionRounds.Clear();
            sessionRounds.AddRange(builtSession);
            roundReviews.Clear();

            currentIndex = 0;
            totalSessionScore = 0;
            correctRoundCount = 0;
            wrongRoundCount = 0;
            totalMistakeCount = 0;
            sessionStartTime = Time.time;
            bloomPostGameShown = false;
            currentRoundCommitted = false;

            HideAllPanels();
            LoadRound(currentIndex);
        }

        public void OnSubmitPressed()
        {
            if (state != GameState.RoundActive || roundEnded) return;

            if (requireAudioBeforeSubmit && audioManager != null && audioManager.HasAudioClip && !audioManager.HasAudioPlayed)
            {
                SetInlineFeedback("Listen to the audio first.", true);
                return;
            }

            string playerAnswer = answerInputField != null ? answerInputField.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(playerAnswer))
            {
                SetInlineFeedback("Type your answer first.", true);
                ShakeInputField();
                return;
            }

            EvaluateAnswer(playerAnswer);
        }

        public void OnPlayAgainPressed()
        {
            if (!allowRetryCurrentRound || state != GameState.Result) return;
            if (currentIndex < 0 || currentIndex >= sessionRounds.Count) return;

            currentRoundCommitted = false;
            LoadRound(currentIndex);
        }

        public void OnContinuePressed()
        {
            if (state != GameState.Result) return;

            CommitCurrentRoundIfNeeded();

            bool isLastRound = currentIndex >= sessionRounds.Count - 1;
            if (isLastRound)
                CompleteSession(true);
            else
                LoadRound(++currentIndex);
        }

        public void OnPausePressed()
        {
            if (state != GameState.RoundActive) return;

            stateBeforePause = state;
            state = GameState.Paused;
            audioManager?.PauseAudio();
            SetGameplayInteractable(false);
            ShowPanel(pausePanel, true);
        }

        public void OnResumePressed()
        {
            if (state != GameState.Paused) return;

            ShowPanel(pausePanel, false);
            state = stateBeforePause;
            SetGameplayInteractable(state == GameState.RoundActive && !roundEnded);
            audioManager?.ResumeAudio();
        }

        public void OnQuitPressed()
        {
            audioManager?.StopAudio();
            OnQuitRequested?.Invoke();

            // In standalone demo mode, show whatever progress exists without firing session-complete.
            CompleteSession(false);
        }

        public void OnReplaySessionPressed()
        {
            StartNewSession();
        }

        public void OnSummaryQuitPressed()
        {
            ShowBloomPostGame();
        }

        private void LoadRound(int index)
        {
            if (index < 0 || index >= sessionRounds.Count) return;

            DictationRoundData round = sessionRounds[index];
            state = GameState.RoundActive;
            roundEnded = false;
            currentRoundCommitted = false;
            attemptCount = 0;
            hintsUsedThisRound = 0;
            replaysUsedThisRound = 0;
            currentScore = baseScore;
            pendingReview = default;

            HideAllPanels();
            SetInlineFeedback(string.Empty, false);
            RefreshTopBar(round, index);
            RefreshScoreUI();

            if (answerInputField != null) answerInputField.SetTextWithoutNotify(string.Empty);
            keyboard?.ClearInput();
            keyboard?.SetInteractable(true);
            if (submitButton != null) submitButton.interactable = true;
            if (pauseButton != null) pauseButton.interactable = true;

            audioManager?.LoadRound(round);
            hintSystem?.LoadRound(round);
            hintSystem?.SetInteractable(true);

            audioManager?.TryAutoPlayCurrentRound();
        }

        private void EvaluateAnswer(string playerAnswer)
        {
            DictationRoundData round = sessionRounds[currentIndex];
            EvaluationResult result = CheckAnswer(playerAnswer, round.AnswerSentence);

            if (result == EvaluationResult.Perfect)
            {
                EndRound(true, false, playerAnswer);
                return;
            }

            if (result == EvaluationResult.CloseEnough)
            {
                DeductPoints(closeEnoughPenalty);
                EndRound(true, true, playerAnswer);
                return;
            }

            attemptCount++;
            DeductPoints(wrongAttemptCost);

            if (attemptCount >= maxAttempts)
            {
                EndRound(false, false, playerAnswer);
                return;
            }

            int remaining = maxAttempts - attemptCount;
            SetInlineFeedback($"Not quite. {remaining} attempt(s) left (-{wrongAttemptCost} pts)", true);
            ShakeInputField();
            audioManager?.PlaySfx_Wrong();
        }

        private void EndRound(bool success, bool closeEnough, string playerAnswer)
        {
            roundEnded = true;
            state = GameState.Result;
            keyboard?.SetInteractable(false);
            hintSystem?.SetInteractable(false);
            if (submitButton != null) submitButton.interactable = false;
            if (pauseButton != null) pauseButton.interactable = false;
            audioManager?.StopAudio();

            DictationRoundData round = sessionRounds[currentIndex];
            int finalScore = success ? Mathf.Max(0, currentScore) : 0;

            pendingReview = new RoundReview
            {
                Title = round.RoundTitle,
                CorrectAnswer = round.AnswerSentence,
                PlayerAnswer = playerAnswer,
                Success = success,
                CloseEnough = closeEnough,
                Score = finalScore,
                AttemptsUsed = attemptCount + (success ? 1 : 0),
                HintsUsed = hintsUsedThisRound,
                ReplaysUsed = replaysUsedThisRound
            };

            if (success) audioManager?.PlaySfx_Correct();
            else audioManager?.PlaySfx_Wrong();

            ShowResultPanel(success, closeEnough, finalScore);
        }

        private void ShowResultPanel(bool success, bool closeEnough, int finalScore)
        {
            ShowPanel(resultPanel, true);

            if (resultTitleText != null)
            {
                if (!success)
                {
                    resultTitleText.text = "Try Again Next Time";
                    resultTitleText.color = hardColor;
                }
                else if (closeEnough)
                {
                    resultTitleText.text = "Close Enough";
                    resultTitleText.color = mediumColor;
                }
                else
                {
                    resultTitleText.text = "Perfect";
                    resultTitleText.color = easyColor;
                }
            }

            if (resultDetailText != null)
            {
                resultDetailText.text = !success
                    ? "Score this round: 0"
                    : closeEnough
                        ? $"Score: {finalScore} (small typo penalty applied)"
                        : $"Score: {finalScore}";
            }

            if (correctAnswerText != null)
            {
                bool showAnswer = !success || closeEnough;
                correctAnswerText.gameObject.SetActive(showAnswer);
                correctAnswerText.text = showAnswer ? $"Answer: {sessionRounds[currentIndex].AnswerSentence}" : string.Empty;
            }

            if (playAgainButton != null)
                playAgainButton.gameObject.SetActive(allowRetryCurrentRound);

            if (continueButton != null)
            {
                TextMeshProUGUI label = continueButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = currentIndex >= sessionRounds.Count - 1 ? "Finish" : "Continue";
            }

            if (success)
                correctParticles?.Play();

            AnimatePanel(resultPanel);
        }

        private void CommitCurrentRoundIfNeeded()
        {
            if (currentRoundCommitted) return;

            currentRoundCommitted = true;
            roundReviews.Add(pendingReview);
            totalSessionScore += pendingReview.Score;

            if (pendingReview.Success) correctRoundCount++;
            else wrongRoundCount++;

            int wrongAttemptsForRound = pendingReview.Success
                ? Mathf.Max(0, pendingReview.AttemptsUsed - 1)
                : Mathf.Max(1, pendingReview.AttemptsUsed);
            totalMistakeCount += wrongAttemptsForRound;

            OnRoundComplete?.Invoke(pendingReview.Score);
        }

        private void CompleteSession(bool notifyCompletion)
        {
            audioManager?.StopAudio();
            HideAllPanels();
            SetGameplayInteractable(false);
            state = GameState.SessionComplete;

            ShowSummaryPanel();

            if (notifyCompletion)
                OnSessionComplete?.Invoke(totalSessionScore);
        }

        private void ShowSummaryPanel()
        {
            ShowPanel(sessionSummaryPanel, true);

            if (summaryTitleText != null)
                summaryTitleText.text = "Session Complete";

            if (summaryScoreText != null)
                summaryScoreText.text = $"Total Score: {totalSessionScore}";

            if (summaryBreakdownText != null)
                summaryBreakdownText.text = BuildSummaryBreakdown();

            AnimatePanel(sessionSummaryPanel);
        }

        private string BuildSummaryBreakdown()
        {
            int completed = roundReviews.Count;
            if (completed == 0)
                return "No completed rounds yet.";

            return $"Questions: {completed}\nCorrect: {correctRoundCount}\nWrong: {wrongRoundCount}";
        }

        private void ShowBloomPostGame()
        {
            if (bloomPostGameShown) return;
            bloomPostGameShown = true;

            audioManager?.StopAudio();
            SetGameplayInteractable(false);

            float timeTaken = Mathf.Max(0f, Time.time - sessionStartTime);
            int totalQuestions = Mathf.Max(1, sessionRounds.Count);
            float accuracyScore = Mathf.Clamp01((float)correctRoundCount / totalQuestions);
            float timeScore = Mathf.Clamp01(1f - (timeTaken / Mathf.Max(1f, expectedMaxSessionTime)));

            GameEvaluationData eval = new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = totalMistakeCount,
                timeTaken = timeTaken
            };

            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPostGame(bloomSkills, eval);
            }
            else
            {
                OnQuitRequested?.Invoke();
                Debug.Log("[DictationGame] Bloom RewardManager is missing or disabled. Summary Quit fallback invoked.");
            }
        }

        public void OnRewardScreenOpen()
        {
            audioManager?.StopAudio();
        }

        public void OnPlayAgain()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnHome()
        {
            SceneManager.LoadScene(homeSceneName);
        }

        private void HandleReplayUsed(int cost)
        {
            replaysUsedThisRound++;
            DeductPoints(cost);
        }

        private void HandleHintUsed(int cost)
        {
            hintsUsedThisRound++;
            DeductPoints(cost);
            audioManager?.PlaySfx_HintUsed();
        }

        private void DeductPoints(int amount)
        {
            if (amount <= 0) return;
            currentScore = Mathf.Max(0, currentScore - amount);
            RefreshScoreUI();

            if (scoreText != null)
            {
                scoreText.transform.DOKill();
                scoreText.transform.DOPunchScale(Vector3.one * 0.18f, 0.18f, 1, 0.5f).SetUpdate(true);
            }
        }

        private void RefreshTopBar(DictationRoundData round, int index)
        {
            if (roundTitleText != null) roundTitleText.text = round.RoundTitle;
            if (roundProgressText != null) roundProgressText.text = $"Q {index + 1} / {sessionRounds.Count}";
            if (difficultyBadgeText != null)
            {
                difficultyBadgeText.text = round.Difficulty.ToString().ToUpperInvariant();
                difficultyBadgeText.color = GetDifficultyColor(round.Difficulty);
            }
        }

        private Color GetDifficultyColor(DifficultyLevel difficulty)
        {
            return difficulty switch
            {
                DifficultyLevel.Easy => easyColor,
                DifficultyLevel.Medium => mediumColor,
                DifficultyLevel.Hard => hardColor,
                _ => Color.white
            };
        }

        private void RefreshScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"Score: {Mathf.Max(0, currentScore)}";
        }

        private EvaluationResult CheckAnswer(string player, string correct)
        {
            string normalizedPlayer = Normalize(player);
            string normalizedCorrect = Normalize(correct);

            if (normalizedPlayer == normalizedCorrect)
                return EvaluationResult.Perfect;

            int distance = LevenshteinDistance(normalizedPlayer, normalizedCorrect);
            return distance <= levenshteinTolerance ? EvaluationResult.CloseEnough : EvaluationResult.Wrong;
        }

        private static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            StringBuilder builder = new StringBuilder(text.Length);
            string lower = text.ToLowerInvariant().Trim();
            for (int i = 0; i < lower.Length; i++)
            {
                char c = lower[i];
                if (char.IsLetterOrDigit(c) || c == '\'' || c == ' ')
                    builder.Append(c);
            }

            return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            int[,] dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    dp[i, j] = Mathf.Min(
                        dp[i - 1, j] + 1,
                        Mathf.Min(dp[i, j - 1] + 1, dp[i - 1, j - 1] + cost));
                }
            }

            return dp[a.Length, b.Length];
        }

        private void ShowHowToPlayWithMessage(string message)
        {
            state = GameState.HowToPlay;
            HideAllPanels();
            SetGameplayInteractable(false);
            if (howToPlayBodyText != null) howToPlayBodyText.text = message;
            ShowPanel(howToPlayPanel, true);
            AnimatePanel(howToPlayPanel);
        }

        private string BuildDefaultHowToPlayText()
        {
            string playMode = audioManager != null && audioManager.AutoPlayOnRoundStart
                ? "Each question plays automatically. Listen carefully, then type what you heard."
                : "Press Play Audio, listen carefully, then type what you heard.";

            string retryMode = allowRetryCurrentRound
                ? "Practice Mode is ON: you can replay the same round before continuing."
                : "Challenge Mode is ON: answer each round and continue forward.";

            return playMode + "\n\n" +
                   "Use the custom keyboard only. You get limited replays and optional hints, but they reduce your score.\n\n" +
                   retryMode;
        }

        private void BindButtons()
        {
            Bind(gotItButton, OnGotItPressed);
            Bind(submitButton, OnSubmitPressed);
            Bind(pauseButton, OnPausePressed);
            Bind(resumeButton, OnResumePressed);
            Bind(quitButton, OnQuitPressed);
            Bind(playAgainButton, OnPlayAgainPressed);
            Bind(continueButton, OnContinuePressed);
            Bind(replaySessionButton, OnReplaySessionPressed);
            Bind(summaryQuitButton, OnSummaryQuitPressed);
        }

        private static void Bind(Button button, UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void HideAllPanels()
        {
            ShowPanel(howToPlayPanel, false);
            ShowPanel(pausePanel, false);
            ShowPanel(resultPanel, false);
            ShowPanel(sessionSummaryPanel, false);
        }

        private static void ShowPanel(GameObject panel, bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }

        private void AnimatePanel(GameObject panel)
        {
            if (panel == null) return;
            panel.transform.DOKill();
            panel.transform.localScale = Vector3.one * 0.96f;
            panel.transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void SetGameplayInteractable(bool interactable)
        {
            keyboard?.SetInteractable(interactable);
            if (submitButton != null) submitButton.interactable = interactable;
            if (pauseButton != null) pauseButton.interactable = interactable;
            hintSystem?.SetInteractable(interactable);
        }

        private void SetInlineFeedback(string message, bool visible)
        {
            if (inlineFeedbackText == null) return;
            inlineFeedbackText.text = message;
            inlineFeedbackText.gameObject.SetActive(visible);
        }

        private void ShakeInputField()
        {
            if (answerInputField == null) return;
            answerInputField.transform.DOKill();
            answerInputField.transform.DOShakePosition(0.25f, new Vector3(8f, 0f, 0f), 18, 90f).SetUpdate(true);
        }

        private bool HasMinimumSetup()
        {
            bool ok = true;
            ok &= questionSet != null;
            ok &= audioManager != null;
            ok &= hintSystem != null;
            ok &= keyboard != null;
            ok &= answerInputField != null;
            ok &= submitButton != null;
            ok &= resultPanel != null;
            ok &= sessionSummaryPanel != null;

            if (!ok)
                Debug.LogError("[DictationGame] Missing required references. Recreate scene with Tools > Dictation Game > Create Full Scene, then assign QuestionSet.", this);

            return ok;
        }

        private void PlaySceneFadeIn()
        {
            if (!fadeInOnStart || sceneCanvasGroup == null) return;
            sceneCanvasGroup.alpha = 0f;
            sceneCanvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
        }
    }
}
