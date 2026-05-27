using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SentenceWordSearchManager : MonoBehaviour
{
    [Header("Question Bank")]
    public List<SentenceWordSearchQuestion> questions = new List<SentenceWordSearchQuestion>();
    public int maxQuestions = 5;
    public bool randomizeQuestions = true;

    [Header("Mechanic Settings")]
    public SentenceWordSearchDifficulty difficulty = SentenceWordSearchDifficulty.Hard;
    public bool allowReverseSelection = true;
    public bool autoStart = true;

    [Header("Gameplay")]
    public bool useTimer = true;
    public float gameTime = 120f;
    public int scorePerCorrectAnswer = 10;
    public int wrongPenalty = 1;
    public float scorePopupDelay = 0.12f;
    public float nextQuestionDelay = 0.25f;
    public float wrongFlashTime = 0.35f;
    public float textOnlyReadDuration = 1.15f;

    [Header("References")]
    public SentenceWordSearchBoard board;
    public SentenceWordSearchInputController inputController;
    public SentenceWordSearchUI ui;
    public SentenceWordSearchAudio audioController;

    [Header("Buttons")]
    public Button restartButton;
    public Button resultRestartButton;
    public Button howToPlayButton;
    public Button closeHowToPlayButton;
    public Button pauseButton;
    public Button resumeButton;
    public Button hintButton;

    private readonly List<SentenceWordSearchQuestion> activeQuestions = new List<SentenceWordSearchQuestion>();

    private int currentQuestionIndex;
    private int totalQuestions;
    private int score;
    private float remainingTime;
    private bool gameRunning;
    private bool paused;
    private bool busy;

    private void Awake()
    {
        if (inputController != null)
            inputController.SelectionSubmitted += OnSelectionSubmitted;

        if (restartButton != null)
            restartButton.onClick.AddListener(StartGame);

        if (resultRestartButton != null)
            resultRestartButton.onClick.AddListener(StartGame);

        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(() => ui?.ShowHowToPlay(true));

        if (closeHowToPlayButton != null)
            closeHowToPlayButton.onClick.AddListener(() => ui?.ShowHowToPlay(false));

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (hintButton != null)
            hintButton.onClick.AddListener(ShowHintForCurrentAnswer);
    }

    private void Start()
    {
        BuildDefaultQuestionsIfEmpty();

        if (autoStart)
            StartGame();
    }

    private void Update()
    {
        if (!gameRunning || paused || busy)
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

        ui?.SetTimer(remainingTime, useTimer);
    }

    private void OnDestroy()
    {
        if (inputController != null)
            inputController.SelectionSubmitted -= OnSelectionSubmitted;
    }

    public void StartGame()
    {
        StopAllCoroutines();
        BuildDefaultQuestionsIfEmpty();

        BuildActiveQuestionSet();

        if (activeQuestions.Count == 0)
        {
            Debug.LogError("SentenceWordSearchManager has no valid questions.");
            return;
        }

        totalQuestions = activeQuestions.Count;
        currentQuestionIndex = 0;
        score = 0;
        remainingTime = Mathf.Max(1f, gameTime);
        gameRunning = true;
        paused = false;
        busy = false;

        ui?.HideResult();
        ui?.ShowPause(false);
        ui?.SetScore(score);
        ui?.SetTimer(remainingTime, useTimer);

        board?.BuildFixedBoard(activeQuestions, totalQuestions, difficulty);
        audioController?.PlayBgm();
        inputController?.SetInputEnabled(true);

        LoadCurrentQuestion();
    }

    public void PauseGame()
    {
        if (!gameRunning || busy)
            return;

        paused = true;
        inputController?.SetInputEnabled(false);
        ui?.ShowPause(true);
    }

    public void ResumeGame()
    {
        if (!gameRunning)
            return;

        paused = false;
        inputController?.SetInputEnabled(true);
        ui?.ShowPause(false);
    }

    public void ShowHintForCurrentAnswer()
    {
        if (!gameRunning || paused || busy || board == null || currentQuestionIndex >= activeQuestions.Count)
            return;

        string answer = activeQuestions[currentQuestionIndex].answer;
        board.ShowHintForWord(answer);
    }

    private void LoadCurrentQuestion()
    {
        if (currentQuestionIndex >= totalQuestions)
        {
            FinishGame(true);
            return;
        }

        SentenceWordSearchQuestion question = activeQuestions[currentQuestionIndex];
        ui?.SetQuestion(question, currentQuestionIndex, totalQuestions);
        ui?.SetScore(score);
        ui?.SetTimer(remainingTime, useTimer);
    }

    private void OnSelectionSubmitted(List<SentenceWordSearchCell> path, string selectedWord)
    {
        if (!gameRunning || paused || busy || path == null || path.Count == 0)
            return;

        string target = SentenceWordSearchUtility.CleanWord(activeQuestions[currentQuestionIndex].answer);
        string cleanSelected = SentenceWordSearchUtility.CleanWord(selectedWord);

        bool correct = cleanSelected == target;
        if (!correct && allowReverseSelection)
            correct = SentenceWordSearchUtility.Reverse(cleanSelected) == target;

        if (correct)
            StartCoroutine(CorrectRoutine(path));
        else
            StartCoroutine(WrongRoutine(path));
    }

    private IEnumerator CorrectRoutine(List<SentenceWordSearchCell> path)
    {
        busy = true;
        inputController?.SetInputEnabled(false);
        board?.ClearAllHints();

        SentenceWordSearchQuestion question = activeQuestions[currentQuestionIndex];
        string answer = SentenceWordSearchUtility.CleanWord(question.answer);
        Vector3 startPosition = GetPathCenter(path);

        board?.MarkSolved(path);
        audioController?.PlayCorrect();

        if (ui != null)
            yield return ui.PlayScorePopup($"+{scorePerCorrectAnswer}", startPosition, true);

        score += scorePerCorrectAnswer;
        ui?.SetScore(score);

        if (scorePopupDelay > 0f)
            yield return new WaitForSeconds(scorePopupDelay);

        if (ui != null)
            yield return ui.PlayWordToSentenceAnimation(question.sentenceWithBlank, answer, startPosition);

        float narrationDuration = audioController != null ? audioController.PlayNarrationAndGetDuration(question.narrationAudio) : 0f;
        float readDuration = narrationDuration > 0.01f ? narrationDuration : textOnlyReadDuration;

        if (ui != null)
            yield return ui.PlaySentenceReadingPulse(readDuration);
        else
            yield return new WaitForSeconds(readDuration);

        yield return new WaitForSeconds(nextQuestionDelay);

        currentQuestionIndex++;
        busy = false;

        if (currentQuestionIndex >= totalQuestions)
        {
            FinishGame(true);
        }
        else
        {
            LoadCurrentQuestion();
            inputController?.SetInputEnabled(true);
        }
    }

    private IEnumerator WrongRoutine(List<SentenceWordSearchCell> path)
    {
        busy = true;
        inputController?.SetInputEnabled(false);
        board?.ClearAllHints();

        Vector3 startPosition = GetPathCenter(path);
        board?.MarkWrong(path, true);
        audioController?.PlayWrong();

        if (wrongPenalty > 0)
        {
            score = Mathf.Max(0, score - wrongPenalty);
            ui?.SetScore(score);
        }

        if (ui != null)
            StartCoroutine(ui.PlayScorePopup($"-{Mathf.Max(1, wrongPenalty)}", startPosition, false));

        yield return new WaitForSeconds(wrongFlashTime);

        board?.MarkWrong(path, false);
        board?.ClearPreview(path);

        busy = false;
        inputController?.SetInputEnabled(true);
    }

    private void FinishGame(bool completed)
    {
        gameRunning = false;
        busy = true;
        board?.ClearAllHints();
        inputController?.SetInputEnabled(false);
        ui?.ShowResult(completed, score);
        audioController?.PlayComplete();
    }

    private void BuildActiveQuestionSet()
    {
        activeQuestions.Clear();

        List<SentenceWordSearchQuestion> pool = new List<SentenceWordSearchQuestion>();
        for (int i = 0; i < questions.Count; i++)
        {
            if (questions[i] != null && !string.IsNullOrEmpty(SentenceWordSearchUtility.CleanWord(questions[i].answer)))
                pool.Add(questions[i]);
        }

        if (randomizeQuestions)
            ShuffleQuestions(pool);

        int count = Mathf.Clamp(maxQuestions, 1, pool.Count);
        for (int i = 0; i < count; i++)
            activeQuestions.Add(pool[i]);
    }

    private void ShuffleQuestions(List<SentenceWordSearchQuestion> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            SentenceWordSearchQuestion temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private Vector3 GetPathCenter(List<SentenceWordSearchCell> path)
    {
        if (path == null || path.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] == null)
                continue;

            sum += path[i].GetWorldCenter();
            count++;
        }

        return count > 0 ? sum / count : Vector3.zero;
    }

    private void BuildDefaultQuestionsIfEmpty()
    {
        if (questions.Count > 0)
            return;

        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The wind is _________.", answer = "STRONG" });
        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The sun is _________.", answer = "BRIGHT" });
        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "Ice feels _________.", answer = "COLD" });
        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A baby cat is called a _________.", answer = "KITTEN" });
        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "We drink _________.", answer = "WATER" });
        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "The grass is _________.", answer = "GREEN" });
        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A bird can _________.", answer = "FLY" });
        questions.Add(new SentenceWordSearchQuestion { sentenceWithBlank = "A fish can _________.", answer = "SWIM" });
    }
}
