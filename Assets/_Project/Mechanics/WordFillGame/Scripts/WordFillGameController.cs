using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordFillGameController : MonoBehaviour
{
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset headingFontAsset;
    [SerializeField] private TMP_FontAsset bodyFontAsset;

    [Header("Game Heading")]
    [SerializeField] private string gameHeading = "Affirmation Words";
    [SerializeField] private TMP_Text gameHeadingText;

    [Header("Objective Line")]
    [SerializeField] private string gameObjectiveLine = "Fill in the missing letters to complete the affirmation.";
    [SerializeField] private TMP_Text gameObjectiveText;

    [Header("Main UI")]
    [SerializeField] private Image clueImage;
    [SerializeField] private TMP_Text clueText;
    [SerializeField] private CanvasGroup clueTextCanvasGroup;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text timerText;

    [Header("Top Bar Buttons")]
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private Button pauseButton;

    [Header("Systems")]
    [SerializeField] private WordFillUIAnimator uiAnimator;
    [SerializeField] private WordFillAudioManager audioManager;
    [SerializeField] private WordFillHowToPlayPanel howToPlayPanel;

    [Header("Letters")]
    [SerializeField] private Transform letterButtonParent;
    [SerializeField] private LetterTile letterTilePrefab;

    [Header("Control Buttons")]
    [SerializeField] private Button backspaceButton;
    [SerializeField] private Button clearButton;

    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text pauseTitleText;
    [SerializeField] private Button continueButton;

    [Header("Complete Panel")]
    [SerializeField] private GameObject completePanel;
    [SerializeField] private TMP_Text completeTitleText;
    [SerializeField] private TMP_Text completeBodyText;
    [SerializeField] private Button playAgainButton;

    [Header("Questions")]
    [SerializeField] private List<WordQuestion> questions = new List<WordQuestion>();

    [Header("Round Settings")]
    [Tooltip("How many correct answers are required to complete one round.")]
    [SerializeField] private int questionsPerRound = 5;

    [Tooltip("Max time for one round in seconds.")]
    [SerializeField] private float maxTimeSeconds = 60f;

    [Tooltip("Use random question order. Recommended ON.")]
    [SerializeField] private bool randomQuestionOrder = true;

    [Tooltip("Show How To Play panel every time a new round starts.")]
    [SerializeField] private bool showHowToPlayOnRoundStart = true;

    [Tooltip("Timer starts pulsing and ticking below this value.")]
    [SerializeField] private float timerWarningSeconds = 10f;

    [Tooltip("Score reduced when hint is used once in a question.")]
    [SerializeField] private int hintPenaltyPoints = 5;

    [SerializeField] private float nextQuestionDelay = 0.45f;
    [SerializeField] private float fallbackNarrationDuration = 1.2f;
    [SerializeField] private bool autoStartOnPlay = true;

    [Header("Colors")]
    [SerializeField] private Color normalWordColor = Color.black;
    [SerializeField] private Color narrationWordColor = new Color(0.05f, 0.35f, 0.9f);

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private int currentQuestionIndex = -1;
    private int score;
    private int correctAnswers;
    private int wrongAttempts;
    private int hintsUsed;
    private int totalHintPenalty;
    private int lastTickSecond = -1;

    private float remainingTime;

    private bool timerRunning;
    private bool inputLocked;
    private bool roundEnded;
    private bool paused;
    private bool isInCorrectSequence;
    private bool hintUsedForCurrentQuestion;
    private bool timerWarningStarted;
    private bool howToPlayOpen;

    private bool timerRunningBeforePause;
    private bool inputLockedBeforePause;
    private bool timerRunningBeforeHowToPlay;
    private bool inputLockedBeforeHowToPlay;
    private bool pausedBeforeHowToPlay;

    private WordQuestion currentQuestion;
    private Coroutine activeFlowRoutine;

    private readonly List<string> typedLetters = new List<string>();
    private readonly List<LetterTile> usedTiles = new List<LetterTile>();
    private readonly List<int> usedQuestionIndexes = new List<int>();

    private void Start()
    {
        AutoFindSystemsIfMissing();
        HookButtons();
        ApplyFonts();
        UpdateStaticTexts();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (completePanel != null)
            completePanel.SetActive(false);

        if (autoStartOnPlay)
            StartRound();
    }

    private void Update()
    {
        if (!timerRunning || roundEnded || paused || howToPlayOpen)
            return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateTimerText();
            EndRound(false);
            return;
        }

        UpdateTimerText();
        HandleTimerWarning();
    }

    private void AutoFindSystemsIfMissing()
    {
        if (uiAnimator == null)
            uiAnimator = FindObjectOfType<WordFillUIAnimator>();

        if (audioManager == null)
            audioManager = FindObjectOfType<WordFillAudioManager>();

        if (howToPlayPanel == null)
            howToPlayPanel = FindObjectOfType<WordFillHowToPlayPanel>();
    }

    private void HookButtons()
    {
        if (backspaceButton != null)
            backspaceButton.onClick.AddListener(Backspace);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearAnswer);

        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(OpenHowToPlayFromTopBar);

        if (hintButton != null)
            hintButton.onClick.AddListener(ShowHint);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(StartRound);
    }

    private void ApplyFonts()
    {
        if (headingFontAsset != null)
        {
            ApplyFont(gameHeadingText, headingFontAsset);
            ApplyFont(timerText, headingFontAsset);
            ApplyFont(completeTitleText, headingFontAsset);
            ApplyFont(pauseTitleText, headingFontAsset);
        }

        if (bodyFontAsset != null)
        {
            ApplyFont(gameObjectiveText, bodyFontAsset);
            ApplyFont(clueText, bodyFontAsset);
            ApplyFont(wordText, bodyFontAsset);
            ApplyFont(scoreText, bodyFontAsset);
            ApplyFont(feedbackText, bodyFontAsset);
            ApplyFont(completeBodyText, bodyFontAsset);
        }

        if (howToPlayPanel != null)
            howToPlayPanel.ApplyFonts(headingFontAsset, bodyFontAsset);
    }

    private void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text != null && font != null)
            text.font = font;
    }

    private void UpdateStaticTexts()
    {
        if (gameHeadingText != null)
            gameHeadingText.text = gameHeading;

        if (gameObjectiveText != null)
            gameObjectiveText.text = gameObjectiveLine;
    }

    public void StartRound()
    {
        if (questions == null || questions.Count == 0)
        {
            Debug.LogError("WordFillGameController: No questions added.");
            return;
        }

        if (activeFlowRoutine != null)
            StopCoroutine(activeFlowRoutine);

        questionsPerRound = Mathf.Max(1, questionsPerRound);
        maxTimeSeconds = Mathf.Max(1f, maxTimeSeconds);
        hintPenaltyPoints = Mathf.Max(0, hintPenaltyPoints);

        score = 0;
        correctAnswers = 0;
        wrongAttempts = 0;
        hintsUsed = 0;
        totalHintPenalty = 0;

        remainingTime = maxTimeSeconds;
        lastTickSecond = -1;
        timerWarningStarted = false;

        timerRunning = false;
        inputLocked = true;
        roundEnded = false;
        paused = false;
        howToPlayOpen = false;
        isInCorrectSequence = false;

        usedQuestionIndexes.Clear();
        typedLetters.Clear();
        usedTiles.Clear();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (completePanel != null)
            completePanel.SetActive(false);

        if (audioManager != null)
            audioManager.StopNarration();

        if (uiAnimator != null && timerText != null)
            uiAnimator.StopTimerWarning(timerText.rectTransform);

        UpdateStaticTexts();
        UpdateScoreText();
        UpdateTimerText();
        SetFeedback(string.Empty);

        if (showHowToPlayOnRoundStart && howToPlayPanel != null)
            OpenHowToPlayForRoundStart();
        else
            BeginRoundGameplay();
    }

    private void OpenHowToPlayForRoundStart()
    {
        howToPlayOpen = true;
        inputLocked = true;
        timerRunning = false;

        if (audioManager != null)
        {
            audioManager.PlayPanelOpen();
            audioManager.PlayButtonTap();
        }

        howToPlayPanel.Open(() =>
        {
            howToPlayOpen = false;
            BeginRoundGameplay();
        });
    }

    private void BeginRoundGameplay()
    {
        paused = false;
        inputLocked = false;
        timerRunning = true;

        if (audioManager != null)
            audioManager.PlayRoundMusicIfAllowed();

        LoadNextQuestion();
    }

    private void OpenHowToPlayFromTopBar()
    {
        if (howToPlayPanel == null || howToPlayOpen || isInCorrectSequence)
            return;

        if (audioManager != null)
        {
            audioManager.PlayButtonTap();
            audioManager.PlayPanelOpen();
        }

        howToPlayOpen = true;
        timerRunningBeforeHowToPlay = timerRunning;
        inputLockedBeforeHowToPlay = inputLocked;
        pausedBeforeHowToPlay = paused;

        timerRunning = false;
        inputLocked = true;
        paused = true;

        if (audioManager != null)
            audioManager.PauseBackgroundMusic();

        howToPlayPanel.Open(() =>
        {
            howToPlayOpen = false;
            paused = pausedBeforeHowToPlay;
            timerRunning = timerRunningBeforeHowToPlay && !roundEnded;
            inputLocked = inputLockedBeforeHowToPlay;

            if (audioManager != null && !roundEnded)
                audioManager.ResumeBackgroundMusic();
        });
    }

    private void LoadQuestion(int index)
    {
        if (index < 0 || index >= questions.Count)
        {
            Debug.LogError("WordFillGameController: Question index out of range.");
            return;
        }

        currentQuestion = questions[index];

        if (currentQuestion == null)
        {
            Debug.LogError("WordFillGameController: Question is null.");
            return;
        }

        string cleanAnswer = currentQuestion.GetCleanAnswer();

        if (string.IsNullOrEmpty(cleanAnswer) || cleanAnswer.Length < 2)
        {
            Debug.LogError("WordFillGameController: Answer word must contain at least 2 letters.");
            return;
        }

        inputLocked = false;
        isInCorrectSequence = false;
        hintUsedForCurrentQuestion = false;
        typedLetters.Clear();
        usedTiles.Clear();

        if (pauseButton != null)
            pauseButton.interactable = true;

        if (clueImage != null)
        {
            clueImage.sprite = currentQuestion.questionSprite;
            clueImage.enabled = currentQuestion.questionSprite != null;
        }

        if (clueText != null)
        {
            clueText.text = currentQuestion.clueText;
            clueText.gameObject.SetActive(false);
        }

        if (clueTextCanvasGroup != null)
            clueTextCanvasGroup.alpha = 0f;

        if (hintButton != null)
        {
            hintButton.gameObject.SetActive(true);
            hintButton.interactable = true;

            if (uiAnimator != null)
                uiAnimator.StartHintAttention(hintButton.transform as RectTransform);
        }

        SetFeedback(string.Empty);
        UpdateWordText();
        CreateLetterButtons();
    }

    private void ShowHint()
    {
        if (roundEnded || paused || howToPlayOpen || isInCorrectSequence || hintUsedForCurrentQuestion)
            return;

        hintUsedForCurrentQuestion = true;
        hintsUsed++;
        totalHintPenalty += hintPenaltyPoints;
        score -= hintPenaltyPoints;
        UpdateScoreText();

        if (audioManager != null)
        {
            audioManager.PlayButtonTap();
            audioManager.PlayHintOpen();
        }

        if (uiAnimator != null)
            uiAnimator.PlayCenterFeedback("-" + hintPenaltyPoints + "\nHint Used");

        if (hintButton != null)
        {
            hintButton.interactable = false;

            if (uiAnimator != null)
                uiAnimator.StopHintAttention(hintButton.transform as RectTransform);
        }

        if (clueText != null)
            clueText.gameObject.SetActive(true);

        if (clueTextCanvasGroup != null && uiAnimator != null)
            uiAnimator.PlayHintReveal(clueTextCanvasGroup);
        else if (clueTextCanvasGroup != null)
            clueTextCanvasGroup.alpha = 1f;
    }

    private void CreateLetterButtons()
    {
        if (letterButtonParent == null || letterTilePrefab == null)
        {
            Debug.LogError("WordFillGameController: Letter parent or letter tile prefab missing.");
            return;
        }

        for (int i = letterButtonParent.childCount - 1; i >= 0; i--)
            Destroy(letterButtonParent.GetChild(i).gameObject);

        List<char> letters = new List<char>();

        string cleanAnswer = currentQuestion.GetCleanAnswer();
        string missingPart = cleanAnswer.Substring(1);

        for (int i = 0; i < missingPart.Length; i++)
            letters.Add(missingPart[i]);

        int extraCount = Mathf.Max(0, currentQuestion.extraLetters);

        for (int i = 0; i < extraCount; i++)
        {
            char randomLetter = Alphabet[Random.Range(0, Alphabet.Length)];
            letters.Add(char.ToLowerInvariant(randomLetter));
        }

        Shuffle(letters);

        for (int i = 0; i < letters.Count; i++)
        {
            LetterTile tile = Instantiate(letterTilePrefab, letterButtonParent);
            tile.Setup(letters[i], OnLetterClicked, bodyFontAsset);

            if (uiAnimator != null)
                uiAnimator.AnimateLetterSpawn(tile.RectTransform, i);
        }
    }

    private void OnLetterClicked(string letter, LetterTile tile)
    {
        if (inputLocked || roundEnded || paused || howToPlayOpen || isInCorrectSequence || currentQuestion == null)
            return;

        if (audioManager != null)
            audioManager.PlayLetterTap();

        if (uiAnimator != null && tile != null)
            uiAnimator.PlayLetterTap(tile.RectTransform);

        typedLetters.Add(letter.ToLowerInvariant());
        usedTiles.Add(tile);
        tile.SetInteractable(false);

        UpdateWordText();

        int requiredLetterCount = currentQuestion.GetCleanAnswer().Length - 1;

        if (typedLetters.Count >= requiredLetterCount)
            CheckAnswer();
    }

    private void CheckAnswer()
    {
        inputLocked = true;

        string cleanAnswer = currentQuestion.GetCleanAnswer();
        string playerAnswer = cleanAnswer[0].ToString();

        for (int i = 0; i < typedLetters.Count; i++)
            playerAnswer += typedLetters[i];

        if (playerAnswer == cleanAnswer)
        {
            if (activeFlowRoutine != null)
                StopCoroutine(activeFlowRoutine);

            activeFlowRoutine = StartCoroutine(CorrectAnswerSequence());
        }
        else
        {
            wrongAttempts++;

            if (audioManager != null)
                audioManager.PlayWrong();

            SetFeedback("Try again!");

            if (uiAnimator != null)
            {
                uiAnimator.PlayCenterFeedback("Try Again!");

                if (wordText != null)
                    uiAnimator.PlayWrongShake(wordText.rectTransform);
            }

            activeFlowRoutine = StartCoroutine(WrongAnswerRoutine());
        }
    }

    private IEnumerator CorrectAnswerSequence()
    {
        isInCorrectSequence = true;
        inputLocked = true;
        timerRunning = false;

        if (pauseButton != null)
            pauseButton.interactable = false;

        if (hintButton != null && uiAnimator != null)
            uiAnimator.StopHintAttention(hintButton.transform as RectTransform);

        int earnedPoints = Mathf.Max(0, currentQuestion.points);
        score += earnedPoints;
        correctAnswers++;

        UpdateScoreText();
        SetFeedback("Correct!");

        if (audioManager != null)
            audioManager.PlayCorrect();

        if (uiAnimator != null)
        {
            uiAnimator.PlayCenterFeedback("+" + earnedPoints + "\nCorrect!");

            if (wordText != null)
                uiAnimator.PlayCorrectWordPulse(wordText.rectTransform);
        }

        yield return new WaitForSecondsRealtime(0.45f);

        if (wordText != null)
        {
            wordText.text = currentQuestion.GetCompletedLine();
            wordText.color = narrationWordColor;
        }

        float narrationDuration = 0f;

        if (audioManager != null)
            narrationDuration = audioManager.PlayNarration(currentQuestion.completedLineNarration);

        if (narrationDuration <= 0f)
            narrationDuration = fallbackNarrationDuration;

        if (uiAnimator != null && wordText != null)
            yield return uiAnimator.PlayNarrationHighlight(wordText, normalWordColor, narrationWordColor, narrationDuration);
        else
            yield return new WaitForSecondsRealtime(narrationDuration);

        if (roundEnded)
            yield break;

        if (correctAnswers >= questionsPerRound)
        {
            EndRound(true);
        }
        else
        {
            yield return new WaitForSecondsRealtime(nextQuestionDelay);
            timerRunning = true;
            LoadNextQuestion();
        }
    }

    private IEnumerator WrongAnswerRoutine()
    {
        yield return new WaitForSecondsRealtime(0.65f);

        if (roundEnded)
            yield break;

        ClearAnswer();
        SetFeedback(string.Empty);
        inputLocked = false;
    }

    public void LoadNextQuestion()
    {
        if (questions == null || questions.Count == 0 || roundEnded)
            return;

        int nextIndex = randomQuestionOrder ? GetRandomQuestionIndex() : GetSequentialQuestionIndex();

        currentQuestionIndex = nextIndex;
        LoadQuestion(currentQuestionIndex);
    }

    private int GetRandomQuestionIndex()
    {
        if (usedQuestionIndexes.Count >= questions.Count)
            usedQuestionIndexes.Clear();

        int nextIndex = Random.Range(0, questions.Count);

        int safety = 0;
        while (usedQuestionIndexes.Contains(nextIndex) && safety < 50)
        {
            nextIndex = Random.Range(0, questions.Count);
            safety++;
        }

        usedQuestionIndexes.Add(nextIndex);
        return nextIndex;
    }

    private int GetSequentialQuestionIndex()
    {
        int nextIndex = currentQuestionIndex + 1;

        if (nextIndex >= questions.Count)
            nextIndex = 0;

        return nextIndex;
    }

    private void EndRound(bool completed)
    {
        if (roundEnded)
            return;

        roundEnded = true;
        timerRunning = false;
        inputLocked = true;
        isInCorrectSequence = false;

        SetAllLettersInteractable(false);

        if (hintButton != null && uiAnimator != null)
            uiAnimator.StopHintAttention(hintButton.transform as RectTransform);

        if (timerText != null && uiAnimator != null)
            uiAnimator.StopTimerWarning(timerText.rectTransform);

        if (audioManager != null)
        {
            audioManager.StopNarration();

            if (completed)
                audioManager.PlayGameComplete();
            else
                audioManager.PlayTimeUp();

            audioManager.PlayPanelOpen();
        }

        if (completeTitleText != null)
            completeTitleText.text = completed ? "Game Complete!" : "Time Up!";

        if (completeBodyText != null)
        {
            int target = Mathf.Max(1, questionsPerRound);
            int timeUsed = Mathf.RoundToInt(maxTimeSeconds - remainingTime);
            int timeLimit = Mathf.RoundToInt(maxTimeSeconds);

            completeBodyText.text =
                "Correct Answers: " + correctAnswers + " / " + target +
                "\nWrong Attempts: " + wrongAttempts +
                "\nHints Used: " + hintsUsed +
                "\nHint Penalty: -" + totalHintPenalty +
                "\nFinal Score: " + score +
                "\nTime Used: " + timeUsed + " / " + timeLimit + " sec";
        }

        if (completePanel != null)
        {
            if (uiAnimator != null)
                uiAnimator.PlayPanelOpen(completePanel);
            else
                completePanel.SetActive(true);
        }
    }

    private void PauseGame()
    {
        if (paused || roundEnded || howToPlayOpen || isInCorrectSequence)
            return;

        paused = true;
        timerRunningBeforePause = timerRunning;
        inputLockedBeforePause = inputLocked;

        timerRunning = false;
        inputLocked = true;

        if (audioManager != null)
        {
            audioManager.PlayButtonTap();
            audioManager.PauseNarration();
            audioManager.PauseBackgroundMusic();
        }

        if (pausePanel != null)
        {
            if (uiAnimator != null)
                uiAnimator.PlayPanelOpen(pausePanel);
            else
                pausePanel.SetActive(true);
        }
    }

    private void ContinueGame()
    {
        if (!paused)
            return;

        paused = false;
        timerRunning = timerRunningBeforePause;
        inputLocked = inputLockedBeforePause;

        if (audioManager != null)
        {
            audioManager.PlayButtonTap();
            audioManager.ResumeNarration();
            audioManager.ResumeBackgroundMusic();
        }

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void Backspace()
    {
        if (inputLocked || roundEnded || paused || howToPlayOpen || isInCorrectSequence || typedLetters.Count == 0)
            return;

        if (audioManager != null)
            audioManager.PlayButtonTap();

        int lastIndex = typedLetters.Count - 1;

        typedLetters.RemoveAt(lastIndex);

        LetterTile lastTile = usedTiles[lastIndex];
        usedTiles.RemoveAt(lastIndex);

        if (lastTile != null)
            lastTile.SetInteractable(true);

        UpdateWordText();
    }

    public void ClearAnswer()
    {
        if (roundEnded || paused || howToPlayOpen || isInCorrectSequence || currentQuestion == null)
            return;

        typedLetters.Clear();

        for (int i = 0; i < usedTiles.Count; i++)
        {
            if (usedTiles[i] != null)
                usedTiles[i].SetInteractable(true);
        }

        usedTiles.Clear();
        UpdateWordText();
    }

    private void UpdateWordText()
    {
        if (wordText == null || currentQuestion == null)
            return;

        string answer = currentQuestion.GetCleanAnswer();

        if (string.IsNullOrEmpty(answer))
            return;

        string display = "I am " + answer[0];

        int missingCount = answer.Length - 1;

        for (int i = 0; i < missingCount; i++)
        {
            display += " ";

            if (i < typedLetters.Count)
                display += typedLetters[i];
            else
                display += "_";
        }

        wordText.text = display;
        wordText.color = normalWordColor;
    }

    private void HandleTimerWarning()
    {
        if (remainingTime > timerWarningSeconds)
            return;

        if (!timerWarningStarted)
        {
            timerWarningStarted = true;

            if (uiAnimator != null && timerText != null)
                uiAnimator.StartTimerWarning(timerText.rectTransform);
        }

        int currentSecond = Mathf.CeilToInt(remainingTime);

        if (currentSecond != lastTickSecond)
        {
            lastTickSecond = currentSecond;

            if (audioManager != null)
                audioManager.PlayTimerTick();
        }
    }

    private void SetAllLettersInteractable(bool value)
    {
        if (letterButtonParent == null)
            return;

        for (int i = 0; i < letterButtonParent.childCount; i++)
        {
            LetterTile tile = letterButtonParent.GetChild(i).GetComponent<LetterTile>();

            if (tile != null)
                tile.SetInteractable(value);
        }
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int seconds = Mathf.CeilToInt(remainingTime);
        seconds = Mathf.Max(0, seconds);

        int minutesPart = seconds / 60;
        int secondsPart = seconds % 60;

        timerText.text = minutesPart.ToString("00") + ":" + secondsPart.ToString("00");
    }

    private void Shuffle(List<char> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            char temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
