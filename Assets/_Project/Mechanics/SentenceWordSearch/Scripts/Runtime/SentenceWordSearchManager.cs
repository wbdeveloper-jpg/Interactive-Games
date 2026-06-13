using System.Collections;
using System.Collections.Generic;
using RewardSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class SentenceWordSearchManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Questions")]
    public List<SentenceWordSearchQuestion> questionBank = new List<SentenceWordSearchQuestion>();
    [Min(1)] public int questionCount = 5;
    public bool randomizeQuestions = true;

    [Header("Gameplay")]
    public bool autoStart = true;
    public bool useTimer = true;
    public float gameTime = 120f;
    public int correctScore = 10;
    public int wrongPenalty = 1;
    public bool allowReverseSelection = true;
    public float wrongFlashDuration = 0.35f;

    [Header("Bloom Reward System")]
    [Tooltip("Keep enabled for final build. If RewardManager.Instance is missing during direct scene testing, the game will safely skip Bloom screens.")]
    public bool useBloomRewardSystem = true;
    [Tooltip("Used for Bloom timeScore. If 0 or less, Game Time is used.")]
    public float expectedMaxTime = 120f;

    [Header("Board Settings - Edit Here Or On Board")]
    [Tooltip("If enabled, these Manager board values are copied to SentenceWordSearchBoard when the game starts. Keep enabled for fast production editing from one Inspector.")]
    public bool useManagerBoardSettings = true;
    [Min(2)] public int rows = 8;
    [Min(2)] public int columns = 8;
    public int gridPadding = 10;
    public Vector2 gridSpacing = new Vector2(8f, 8f);
    public SentenceWordSearchDifficulty difficulty = SentenceWordSearchDifficulty.Medium;
    public string fillerAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    [Header("References")]
    public SentenceWordSearchBoard board;
    public SentenceWordSearchInputController inputController;
    public SentenceWordSearchUI ui;
    public SentenceWordSearchAudio audioController;

    public bool CanAcceptInput
    {
        get
        {
            return gameRunning
                   && gameplayStarted
                   && !inputLocked
                   && !paused
                   && (ui == null || !ui.IsGameplayBlockingPanelOpen);
        }
    }

    private readonly List<SentenceWordSearchQuestion> activeQuestions = new List<SentenceWordSearchQuestion>();

    private readonly List<SkillEntry> _skills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Remember, 100f),
        new SkillEntry(BloomSkillType.Understand, 100f),
    };

    private int currentQuestionIndex;
    private int score;
    private int correctCount;
    private int wrongCount;
    private float remainingTime;
    private float _startTime;
    private bool gameRunning;
    private bool gameplayStarted;
    private bool inputLocked;
    private bool paused;
    private bool waitingForHowToPlayClose;
    private bool postGameOpened;
    private GameEvaluationData lastEvaluationData;

    private SentenceWordSearchQuestion CurrentQuestion
    {
        get
        {
            if (currentQuestionIndex < 0 || currentQuestionIndex >= activeQuestions.Count)
                return null;

            return activeQuestions[currentQuestionIndex];
        }
    }

    private void Awake()
    {
        if (board == null)
            board = FindObjectOfType<SentenceWordSearchBoard>();

        if (inputController == null)
            inputController = FindObjectOfType<SentenceWordSearchInputController>();

        if (ui == null)
            ui = FindObjectOfType<SentenceWordSearchUI>();

        if (audioController == null)
            audioController = FindObjectOfType<SentenceWordSearchAudio>();

        if (inputController != null)
        {
            inputController.manager = this;
            inputController.board = board;
        }

        PullBoardSettingsIntoManagerIfNeeded();
        HookButtons();
        BuildDefaultQuestionsIfEmpty();
    }

    private void Start()
    {
        if (autoStart)
            StartGame();
    }

    private void Update()
    {
        if (!gameRunning || !gameplayStarted || paused || inputLocked)
            return;

        if (useTimer)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                FinishGame(false);
            }
        }

        if (ui != null)
            ui.UpdateTimer(remainingTime, useTimer);
    }

    public void StartGame()
    {
        StopAllCoroutines();
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        BuildDefaultQuestionsIfEmpty();
        SelectActiveQuestions();

        if (activeQuestions.Count == 0)
        {
            Debug.LogError("SentenceWordSearchManager has no valid questions.");
            yield break;
        }

        currentQuestionIndex = 0;
        score = 0;
        correctCount = 0;
        wrongCount = 0;
        remainingTime = Mathf.Max(1f, gameTime);
        gameRunning = true;
        gameplayStarted = false;
        inputLocked = true;
        paused = false;
        waitingForHowToPlayClose = false;
        postGameOpened = false;

        if (inputController != null)
            inputController.SetInputEnabled(false);

        if (audioController != null)
        {
            audioController.StopAllGameAudio();
        }

        if (ui != null)
        {
            ui.HideResult();
            ui.HidePause();
            ui.HideHowToPlay();
            ui.UpdateScore(score);
            ui.UpdateTimer(remainingTime, useTimer);
            ui.ApplyFonts();
        }

        ApplyManagerBoardSettingsToBoard();

        if (board != null)
            board.BuildBoard(activeQuestions, ui != null ? ui.primaryFont : null);

        LoadCurrentQuestion();

        yield return ShowBloomPreGameRoutine();
        yield return ShowHowToPlayBeforeGameplayRoutine();

        BeginGameplayAfterPreScreens();
    }

    private IEnumerator ShowBloomPreGameRoutine()
    {
        if (!useBloomRewardSystem || RewardManager.Instance == null)
            yield break;

        RewardManager.Instance.ShowPreGame(_skills);
        yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
    }

    private IEnumerator ShowHowToPlayBeforeGameplayRoutine()
    {
        if (ui == null || ui.howToPlayPanel == null)
            yield break;

        waitingForHowToPlayClose = true;
        ui.ShowHowToPlay();

        while (waitingForHowToPlayClose && ui != null && ui.IsHowToPlayOpen)
            yield return null;

        waitingForHowToPlayClose = false;
    }

    private void BeginGameplayAfterPreScreens()
    {
        if (!gameRunning)
            return;

        inputLocked = false;
        gameplayStarted = true;
        _startTime = Time.time;

        if (inputController != null)
            inputController.SetInputEnabled(true);

        if (audioController != null)
            audioController.PlayBgMusic();
    }

    public void SubmitSelectedWord(string selectedWord, List<SentenceWordSearchCell> selectedPath)
    {
        if (!CanAcceptInput)
            return;

        SentenceWordSearchQuestion question = CurrentQuestion;

        if (question == null)
            return;

        string selectedClean = CleanWordStatic(selectedWord);
        string answerClean = CleanWordStatic(question.answer);

        bool correct = selectedClean == answerClean;

        if (!correct && allowReverseSelection)
            correct = Reverse(selectedClean) == answerClean;

        if (correct)
            StartCoroutine(CorrectRoutine(question, selectedPath));
        else
            StartCoroutine(WrongRoutine(selectedPath));
    }

    public void UseHint()
    {
        if (!CanAcceptInput || CurrentQuestion == null)
            return;

        if (board != null)
            board.PulseHintForWord(CurrentQuestion.answer);

        if (audioController != null)
            audioController.PlaySfx(audioController.hintClip);
    }

    public void PauseGame()
    {
        if (!gameRunning || !gameplayStarted)
            return;

        paused = true;

        if (inputController != null)
            inputController.SetInputEnabled(false);

        if (ui != null)
            ui.ShowPause();
    }

    public void ResumeGame()
    {
        paused = false;

        if (ui != null)
            ui.HidePause();

        if (inputController != null)
            inputController.SetInputEnabled(CanAcceptInput);
    }

    public void ShowHowToPlay()
    {
        if (ui == null)
            return;

        if (inputController != null)
            inputController.SetInputEnabled(false);

        ui.ShowHowToPlay();
    }

    public void HideHowToPlay()
    {
        OnHowToPlayClosePressed();
    }

    public void OnHowToPlayClosePressed()
    {
        if (ui != null)
            ui.HideHowToPlay();

        if (waitingForHowToPlayClose)
        {
            waitingForHowToPlayClose = false;
            return;
        }

        if (inputController != null)
            inputController.SetInputEnabled(CanAcceptInput);
    }

    private IEnumerator CorrectRoutine(SentenceWordSearchQuestion question, List<SentenceWordSearchCell> selectedPath)
    {
        inputLocked = true;

        string answer = CleanWordStatic(question.answer);

        Vector2 popupPosition = Vector2.zero;
        Camera eventCamera = inputController != null ? inputController.EventCamera : null;

        if (board != null)
            popupPosition = board.GetPathCenterScreenPosition(selectedPath, eventCamera);

        score += correctScore;
        correctCount++;

        if (ui != null)
        {
            ui.UpdateScore(score);
            ui.ShowScorePopup($"+{correctScore}", popupPosition, eventCamera, true);
        }

        if (audioController != null)
            audioController.PlaySfx(audioController.scorePopupClip != null ? audioController.scorePopupClip : audioController.correctClip);

        yield return new WaitForSeconds(0.28f);

        if (board != null)
        {
            board.ClearPreview();
            board.MarkWordSolved(answer);
        }

        if (ui != null)
            yield return ui.AnimateWordToSentence(answer, popupPosition, eventCamera);

        if (audioController != null)
            audioController.PlaySfx(audioController.correctClip);

        float fallbackReadDuration = audioController != null ? audioController.PlayNarration(question.narrationClip) : 1.25f;

        if (ui != null)
            ui.StartSentenceReadPulse();

        if (audioController != null && question.narrationClip != null)
        {
            while (audioController.IsNarrationPlaying)
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(fallbackReadDuration);
        }

        if (ui != null)
            ui.StopSentenceReadPulse();

        currentQuestionIndex++;

        if (currentQuestionIndex >= activeQuestions.Count)
        {
            FinishGame(true);
            yield break;
        }

        inputLocked = false;
        LoadCurrentQuestion();
    }

    private IEnumerator WrongRoutine(List<SentenceWordSearchCell> selectedPath)
    {
        inputLocked = true;
        wrongCount++;

        Vector2 popupPosition = Vector2.zero;
        Camera eventCamera = inputController != null ? inputController.EventCamera : null;

        if (board != null)
            popupPosition = board.GetPathCenterScreenPosition(selectedPath, eventCamera);

        if (wrongPenalty > 0)
            score = Mathf.Max(0, score - wrongPenalty);

        if (ui != null)
        {
            ui.UpdateScore(score);
            ui.ShowScorePopup($"-{wrongPenalty}", popupPosition, eventCamera, false);
        }

        if (audioController != null)
            audioController.PlaySfx(audioController.wrongClip);

        if (board != null)
            board.FlashWrongPath(selectedPath, wrongFlashDuration);

        yield return new WaitForSeconds(wrongFlashDuration);

        if (board != null)
            board.ClearPreview();

        inputLocked = false;
    }

    private void LoadCurrentQuestion()
    {
        SentenceWordSearchQuestion question = CurrentQuestion;

        if (question == null)
        {
            FinishGame(true);
            return;
        }

        if (board != null)
        {
            board.ClearPreview();
            board.StopAllHintPulses();
        }

        if (ui != null)
            ui.ShowQuestion(question, currentQuestionIndex + 1, activeQuestions.Count);
    }

    private void FinishGame(bool completed)
    {
        if (!gameRunning)
            return;

        gameRunning = false;
        gameplayStarted = false;
        inputLocked = true;
        paused = false;

        if (inputController != null)
            inputController.SetInputEnabled(false);

        if (audioController != null)
        {
            audioController.StopNarration();
            audioController.PlaySfx(audioController.completeClip);
        }

        lastEvaluationData = BuildEvaluationData();

        if (ui != null)
        {
            ui.StopSentenceReadPulse();
            ui.ShowResult(score, completed);
        }
    }

    private GameEvaluationData BuildEvaluationData()
    {
        int totalQuestions = activeQuestions.Count;
        float timeTaken = Mathf.Max(0f, Time.time - _startTime);
        float maxTime = expectedMaxTime > 0f ? expectedMaxTime : gameTime;
        maxTime = Mathf.Max(1f, maxTime);

        float timeScore = Mathf.Clamp01(1f - (timeTaken / maxTime));
        float accuracyScore = totalQuestions > 0 ? (float)correctCount / totalQuestions : 0f;

        return new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = Mathf.Clamp01(accuracyScore),
            mistakeCount = wrongCount,
            timeTaken = timeTaken
        };
    }

    public void ShowPostGameReward()
    {
        if (postGameOpened)
            return;

        postGameOpened = true;

        if (ui != null)
            ui.HideResult();

        if (audioController != null)
            audioController.StopAllGameAudio();

        if (useBloomRewardSystem && RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPostGame(_skills, lastEvaluationData);
        }
        else
        {
            Debug.LogWarning("Bloom RewardManager.Instance not found. Post-game reward screen skipped for direct scene testing.");
        }
    }

    public void OnPlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHome()
    {
        SceneManager.LoadScene("Loader Scene");
    }

    public void OnRewardScreenOpen()
    {
        if (audioController != null)
            audioController.StopAllGameAudio();
    }

    private void PullBoardSettingsIntoManagerIfNeeded()
    {
        if (board == null)
            return;

        rows = Mathf.Max(2, board.rows);
        columns = Mathf.Max(2, board.columns);
        gridPadding = board.gridPadding;
        gridSpacing = board.gridSpacing;
        difficulty = board.difficulty;
        fillerAlphabet = board.fillerAlphabet;
    }

    private void ApplyManagerBoardSettingsToBoard()
    {
        if (!useManagerBoardSettings || board == null)
            return;

        rows = Mathf.Max(2, rows);
        columns = Mathf.Max(2, columns);

        board.rows = rows;
        board.columns = columns;
        board.gridPadding = gridPadding;
        board.gridSpacing = gridSpacing;
        board.difficulty = difficulty;
        board.fillerAlphabet = fillerAlphabet;
    }

    private void OnValidate()
    {
        questionCount = Mathf.Max(1, questionCount);
        rows = Mathf.Max(2, rows);
        columns = Mathf.Max(2, columns);
        gridPadding = Mathf.Max(0, gridPadding);
        wrongPenalty = Mathf.Max(0, wrongPenalty);
        correctScore = Mathf.Max(0, correctScore);

        if (expectedMaxTime <= 0f)
            expectedMaxTime = gameTime;
    }

    private void SelectActiveQuestions()
    {
        activeQuestions.Clear();

        List<SentenceWordSearchQuestion> valid = new List<SentenceWordSearchQuestion>();

        for (int i = 0; i < questionBank.Count; i++)
        {
            if (questionBank[i] == null)
                continue;

            if (string.IsNullOrWhiteSpace(questionBank[i].answer))
                continue;

            valid.Add(questionBank[i]);
        }

        if (randomizeQuestions)
            Shuffle(valid);

        int count = Mathf.Clamp(questionCount, 1, valid.Count);

        for (int i = 0; i < count; i++)
            activeQuestions.Add(valid[i]);
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void HookButtons()
    {
        if (ui == null)
            return;

        if (ui.pauseButton != null)
        {
            ui.pauseButton.onClick.RemoveListener(PauseGame);
            ui.pauseButton.onClick.AddListener(PauseGame);
        }

        if (ui.resumeButton != null)
        {
            ui.resumeButton.onClick.RemoveListener(ResumeGame);
            ui.resumeButton.onClick.AddListener(ResumeGame);
        }

        if (ui.howToPlayButton != null)
        {
            ui.howToPlayButton.onClick.RemoveListener(ShowHowToPlay);
            ui.howToPlayButton.onClick.AddListener(ShowHowToPlay);
        }

        if (ui.closeHowToPlayButton != null)
        {
            ui.closeHowToPlayButton.onClick.RemoveListener(OnHowToPlayClosePressed);
            ui.closeHowToPlayButton.onClick.AddListener(OnHowToPlayClosePressed);
        }

        if (ui.hintButton != null)
        {
            ui.hintButton.onClick.RemoveListener(UseHint);
            ui.hintButton.onClick.AddListener(UseHint);
        }

        if (ui.restartButton != null)
        {
            ui.restartButton.onClick.RemoveListener(StartGame);
            ui.restartButton.onClick.AddListener(StartGame);
        }

        if (ui.resultRestartButton != null)
        {
            ui.resultRestartButton.onClick.RemoveListener(StartGame);
            ui.resultRestartButton.onClick.AddListener(StartGame);
        }

        if (ui.resultContinueButton != null)
        {
            ui.resultContinueButton.onClick.RemoveListener(ShowPostGameReward);
            ui.resultContinueButton.onClick.AddListener(ShowPostGameReward);
        }
    }

    private void BuildDefaultQuestionsIfEmpty()
    {
        if (questionBank.Count > 0)
            return;

        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The wind is _________.", answer = "STRONG" });
        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The sun is _________.", answer = "BRIGHT" });
        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "Ice feels _________.", answer = "COLD" });
        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A baby cat is called a _________.", answer = "KITTEN" });
        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "We drink _________.", answer = "WATER" });
        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A bird can _________.", answer = "FLY" });
        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "Grass is usually _________.", answer = "GREEN" });
        questionBank.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "We see with our _________.", answer = "EYES" });
    }

    public static string CleanWordStatic(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];

            if (char.IsLetterOrDigit(ch))
                builder.Append(char.ToUpperInvariant(ch));
        }

        return builder.ToString();
    }

    private string Reverse(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        char[] chars = value.ToCharArray();
        System.Array.Reverse(chars);
        return new string(chars);
    }
}
