using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RewardSystem;

public class WordFillGameController : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset primaryFont;
    [SerializeField] private TMP_FontAsset secondaryFont;
    [SerializeField] private WordFillFontApplier fontApplier;

    [Header("Game Text")]
    [SerializeField] private string gameInstructionLine = "Fill in the missing letters to complete the affirmation.";
    [SerializeField] private TMP_Text gameInstructionText;

    [Header("Main UI")]
    [SerializeField] private Image clueImage;
    [SerializeField] private TMP_Text clueText;
    [SerializeField] private CanvasGroup clueTextCanvasGroup;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text timerText;

    [Header("Bottom Corner Buttons")]
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Button pauseButton;

    [Header("Top Bar Button")]
    [SerializeField] private Button hintButton;

    [Header("Systems")]
    [SerializeField] private WordFillUIAnimator uiAnimator;
    [SerializeField] private WordFillAudioManager audioManager;
    [SerializeField] private WordFillHowToPlayPanel howToPlayPanel;
    [SerializeField] private WordFillLoadingPanel loadingPanel;

    [Header("Letters")]
    [Tooltip("Scene template object, not prefab asset. Keep it inactive in the scene.")]
    [SerializeField] private LetterTile letterTileTemplate;
    [SerializeField] private Transform letterButtonParent;
    [SerializeField] private WordFillLetterGridFitter letterGridFitter;
    [SerializeField] private WordFillLetterDifficulty letterGridDifficulty = WordFillLetterDifficulty.Easy;

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
    [SerializeField] private Button completeContinueButton;

    [Header("Questions")]
    [SerializeField] private List<WordQuestion> questions = new List<WordQuestion>();

    [Header("Round Settings")]
    [SerializeField] private int questionsPerRound = 5;
    [SerializeField] private float maxTimeSeconds = 60f;
    [SerializeField] private bool randomQuestionOrder = true;
    [SerializeField] private bool showLoadingPanelOnRoundStart = true;
    [SerializeField] private bool showHowToPlayOnRoundStart = true;
    [SerializeField] private float timerWarningSeconds = 10f;
    [SerializeField] private int hintPenaltyPoints = 5;
    [SerializeField] private float nextQuestionDelay = 0.45f;
    [SerializeField] private float fallbackNarrationDuration = 1.2f;
    [SerializeField] private bool autoStartOnPlay = true;

    [Header("Bloom Reward")]
    [SerializeField] private float expectedMaxTime = 60f;
    [SerializeField] private bool showBloomPreGame = true;
    [SerializeField] private bool showBloomPostGameFromCompleteContinue = true;

    [Header("Colors")]
    [SerializeField] private Color normalWordColor = Color.black;
    [SerializeField] private Color narrationWordColor = new Color(0.05f, 0.35f, 0.9f);

    private readonly List<SkillEntry> _skills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Remember, 100f),
        new SkillEntry(BloomSkillType.Understand, 100f),
    };

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private int currentQuestionIndex = -1;
    private int score;
    private int correctAnswers;
    private int wrongAttempts;
    private int hintsUsed;
    private int totalHintPenalty;
    private int lastTickSecond = -1;

    private float remainingTime;
    private float _startTime;
    private float _finalTimeTaken;
    private GameEvaluationData _finalEvaluationData;
    private bool _hasFinalEvaluation;

    private bool timerRunning;
    private bool inputLocked;
    private bool roundEnded;
    private bool paused;
    private bool isInCorrectSequence;
    private bool hintUsedForCurrentQuestion;
    private bool timerWarningStarted;
    private bool howToPlayOpen;
    private bool loadingOpen;
    private bool bloomPreGameOpen;
    private bool bloomPostGameOpen;

    private bool timerRunningBeforePause;
    private bool inputLockedBeforePause;
    private bool timerRunningBeforeHowToPlay;
    private bool inputLockedBeforeHowToPlay;
    private bool pausedBeforeHowToPlay;

    private WordQuestion currentQuestion;
    private string currentQuestionSentence = string.Empty;
    private Coroutine activeFlowRoutine;
    private Coroutine bloomPreGameRoutine;

    private readonly List<string> typedLetters = new List<string>();
    private readonly List<LetterTile> usedTiles = new List<LetterTile>();
    private readonly List<int> usedQuestionIndexes = new List<int>();
    private readonly List<WordFillAnswerMatch> currentAnswerMatches = new List<WordFillAnswerMatch>();

    private void Start()
    {
        AutoFindSystemsIfMissing();
        HookButtons();
        ApplyFonts();
        UpdateStaticTexts();

        expectedMaxTime = Mathf.Max(1f, expectedMaxTime <= 0f ? maxTimeSeconds : expectedMaxTime);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (completePanel != null)
            completePanel.SetActive(false);

        if (letterTileTemplate != null)
            letterTileTemplate.gameObject.SetActive(false);

        if (autoStartOnPlay)
            StartRound();
    }

    private void Update()
    {
        if (!timerRunning || roundEnded || paused || howToPlayOpen || loadingOpen || bloomPreGameOpen || bloomPostGameOpen)
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

        if (loadingPanel == null)
            loadingPanel = FindObjectOfType<WordFillLoadingPanel>();

        if (fontApplier == null)
            fontApplier = FindObjectOfType<WordFillFontApplier>();

        if (letterGridFitter == null && letterButtonParent != null)
        {
            letterGridFitter = letterButtonParent.GetComponent<WordFillLetterGridFitter>();

            if (letterGridFitter == null &&
                letterButtonParent.GetComponent<GridLayoutGroup>() != null)
            {
                letterGridFitter =
                    letterButtonParent.gameObject.AddComponent<WordFillLetterGridFitter>();
            }
        }
    }

    private void HookButtons()
    {
        if (backspaceButton != null)
            backspaceButton.onClick.AddListener(Backspace);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearAnswer);

        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(OpenHowToPlayFromCornerButton);

        if (hintButton != null)
            hintButton.onClick.AddListener(ShowHint);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueGame);

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnCustomCompletePlayAgainClicked);

        if (completeContinueButton != null)
            completeContinueButton.onClick.AddListener(OnCompleteContinueClicked);
    }

    private void ApplyFonts()
    {
        if (fontApplier != null)
            fontApplier.SetFonts(primaryFont, secondaryFont);
    }

    private void UpdateStaticTexts()
    {
        if (gameInstructionText != null)
            gameInstructionText.text = gameInstructionLine;
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

        if (bloomPreGameRoutine != null)
            StopCoroutine(bloomPreGameRoutine);

        questionsPerRound = Mathf.Max(1, questionsPerRound);
        maxTimeSeconds = Mathf.Max(1f, maxTimeSeconds);
        expectedMaxTime = Mathf.Max(1f, expectedMaxTime <= 0f ? maxTimeSeconds : expectedMaxTime);
        hintPenaltyPoints = Mathf.Max(0, hintPenaltyPoints);

        score = 0;
        correctAnswers = 0;
        wrongAttempts = 0;
        hintsUsed = 0;
        totalHintPenalty = 0;

        remainingTime = maxTimeSeconds;
        lastTickSecond = -1;
        timerWarningStarted = false;

        _startTime = 0f;
        _finalTimeTaken = 0f;
        _hasFinalEvaluation = false;

        timerRunning = false;
        inputLocked = true;
        roundEnded = false;
        paused = false;
        howToPlayOpen = false;
        loadingOpen = false;
        bloomPreGameOpen = false;
        bloomPostGameOpen = false;
        isInCorrectSequence = false;

        usedQuestionIndexes.Clear();
        typedLetters.Clear();
        usedTiles.Clear();
        currentAnswerMatches.Clear();
        currentQuestionSentence = string.Empty;

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

        StartBloomPreGameThenLoading();
    }

    private void OpenLoadingPanel()
    {
        loadingOpen = true;
        inputLocked = true;
        timerRunning = false;

        if (audioManager != null)
            audioManager.PlayPanelOpen();

        loadingPanel.Open(() =>
        {
            loadingOpen = false;
            AfterLoadingPanel();
        });
    }

    private void AfterLoadingPanel()
    {
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

    private void StartBloomPreGameThenLoading()
    {
        if (bloomPreGameRoutine != null)
            StopCoroutine(bloomPreGameRoutine);

        bloomPreGameRoutine = StartCoroutine(BloomPreGameThenLoadingRoutine());
    }

    private IEnumerator BloomPreGameThenLoadingRoutine()
    {
        bloomPreGameOpen = true;
        inputLocked = true;
        timerRunning = false;

        if (showBloomPreGame && RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPreGame(_skills);
            yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
        }

        bloomPreGameOpen = false;

        if (showLoadingPanelOnRoundStart && loadingPanel != null)
            OpenLoadingPanel();
        else
            AfterLoadingPanel();
    }

    private void BeginRoundGameplay()
    {
        _startTime = Time.time;

        paused = false;
        inputLocked = false;
        timerRunning = true;

        if (audioManager != null)
            audioManager.PlayRoundMusicIfAllowed();

        LoadNextQuestion();
    }

    private void OpenHowToPlayFromCornerButton()
    {
        if (howToPlayPanel == null || howToPlayOpen || loadingOpen || bloomPreGameOpen || bloomPostGameOpen || isInCorrectSequence)
            return;

        if (audioManager != null)
        {
            audioManager.PlayButtonTap();
            audioManager.PlayPanelOpen();
            audioManager.PauseBackgroundMusic();
        }

        howToPlayOpen = true;
        timerRunningBeforeHowToPlay = timerRunning;
        inputLockedBeforeHowToPlay = inputLocked;
        pausedBeforeHowToPlay = paused;

        timerRunning = false;
        inputLocked = true;
        paused = true;

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

        List<string> answerWords = currentQuestion.GetAnswerWords();

        if (answerWords.Count == 0)
        {
            Debug.LogError("WordFillGameController: Add at least one Answer Word.");
            return;
        }

        for (int i = 0; i < answerWords.Count; i++)
        {
            if (answerWords[i].Length < 2)
            {
                Debug.LogError(
                    "WordFillGameController: Every Answer Word must contain at least 2 letters.");
                return;
            }
        }

        string matchedSentence;
        List<WordFillAnswerMatch> matches;

        if (!currentQuestion.TryGetAnswerMatches(out matchedSentence, out matches))
        {
            Debug.LogError(
                "WordFillGameController: Completed Line Text must contain every Answer Word " +
                "as an exact standalone word. Words may appear anywhere in the sentence.");
            return;
        }

        currentQuestionSentence = matchedSentence;
        currentAnswerMatches.Clear();
        currentAnswerMatches.AddRange(matches);

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
        if (roundEnded || paused || howToPlayOpen || loadingOpen || bloomPreGameOpen || bloomPostGameOpen || isInCorrectSequence || hintUsedForCurrentQuestion)
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
        if (letterButtonParent == null || letterTileTemplate == null)
        {
            Debug.LogError("WordFillGameController: Letter parent or letter tile scene template missing.");
            return;
        }

        for (int i = letterButtonParent.childCount - 1; i >= 0; i--)
            Destroy(letterButtonParent.GetChild(i).gameObject);

        List<char> letters = new List<char>();

        for (int matchIndex = 0; matchIndex < currentAnswerMatches.Count; matchIndex++)
        {
            string sentenceWord = currentAnswerMatches[matchIndex].SentenceWord;

            for (int characterIndex = 1; characterIndex < sentenceWord.Length; characterIndex++)
                letters.Add(char.ToLowerInvariant(sentenceWord[characterIndex]));
        }

        int extraCount = Mathf.Max(0, currentQuestion.extraLetters);

        for (int i = 0; i < extraCount; i++)
        {
            char randomLetter = Alphabet[Random.Range(0, Alphabet.Length)];
            letters.Add(char.ToLowerInvariant(randomLetter));
        }

        Shuffle(letters);

        for (int i = 0; i < letters.Count; i++)
        {
            LetterTile tile = Instantiate(letterTileTemplate, letterButtonParent);
            tile.gameObject.SetActive(true);
            tile.Setup(letters[i], OnLetterClicked, secondaryFont);

            if (uiAnimator != null)
                uiAnimator.AnimateLetterSpawn(tile.RectTransform, i);
        }

        if (letterGridFitter != null)
            letterGridFitter.FitGrid(letters.Count, letterGridDifficulty);
    }

    private void OnLetterClicked(string letter, LetterTile tile)
    {
        if (inputLocked || roundEnded || paused || howToPlayOpen || loadingOpen || bloomPreGameOpen || bloomPostGameOpen || isInCorrectSequence || currentQuestion == null)
            return;

        if (audioManager != null)
            audioManager.PlayLetterTap();

        if (uiAnimator != null && tile != null)
            uiAnimator.PlayLetterTap(tile.RectTransform);

        typedLetters.Add(letter.ToLowerInvariant());
        usedTiles.Add(tile);
        tile.SetInteractable(false);

        UpdateWordText();

        int requiredLetterCount = GetRequiredLetterCount();

        if (typedLetters.Count >= requiredLetterCount)
            CheckAnswer();
    }

    private void CheckAnswer()
    {
        inputLocked = true;

        if (DoesTypedAnswerMatch())
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

        BuildFinalEvaluationData();
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
            int timeUsed = Mathf.RoundToInt(_finalTimeTaken);
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

    private void BuildFinalEvaluationData()
    {
        _finalTimeTaken = _startTime > 0f ? Time.time - _startTime : maxTimeSeconds;

        int totalQuestions = Mathf.Max(1, questionsPerRound);
        float timeScore = Mathf.Clamp01(1f - (_finalTimeTaken / Mathf.Max(1f, expectedMaxTime)));
        float accuracyScore = totalQuestions > 0 ? (float)correctAnswers / totalQuestions : 0f;

        _finalEvaluationData = new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = Mathf.Clamp01(accuracyScore),
            mistakeCount = wrongAttempts,
            timeTaken = _finalTimeTaken
        };

        _hasFinalEvaluation = true;
    }

    private void OnCompleteContinueClicked()
    {
        if (audioManager != null)
            audioManager.PlayButtonTap();

        if (completePanel != null)
            completePanel.SetActive(false);

        if (!showBloomPostGameFromCompleteContinue)
            return;

        ShowBloomPostGame();
    }

    private void OnCustomCompletePlayAgainClicked()
    {
        if (showBloomPostGameFromCompleteContinue)
            OnCompleteContinueClicked();
        else
            StartRound();
    }

    private void ShowBloomPostGame()
    {
        if (!_hasFinalEvaluation)
            BuildFinalEvaluationData();

        bloomPostGameOpen = true;
        inputLocked = true;
        timerRunning = false;

        OnRewardScreenOpen();

        if (RewardManager.Instance != null)
            RewardManager.Instance.ShowPostGame(_skills, _finalEvaluationData);
        else
            Debug.LogWarning("RewardManager.Instance not found. Make sure RewardManager exists in LoadingScene and uses DontDestroyOnLoad.");
    }

    private void PauseGame()
    {
        if (paused || roundEnded || howToPlayOpen || loadingOpen || bloomPreGameOpen || bloomPostGameOpen || isInCorrectSequence)
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
        if (inputLocked || roundEnded || paused || howToPlayOpen || loadingOpen || bloomPreGameOpen || bloomPostGameOpen || isInCorrectSequence || typedLetters.Count == 0)
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
        if (roundEnded || paused || howToPlayOpen || loadingOpen || bloomPreGameOpen || bloomPostGameOpen || isInCorrectSequence || currentQuestion == null)
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

        if (string.IsNullOrEmpty(currentQuestionSentence) || currentAnswerMatches.Count == 0)
            return;

        StringBuilder sentenceBuilder = new StringBuilder(
            currentQuestionSentence.Length + GetRequiredLetterCount());
        int sentenceReadIndex = 0;
        int typedLetterIndex = 0;

        for (int i = 0; i < currentAnswerMatches.Count; i++)
        {
            WordFillAnswerMatch match = currentAnswerMatches[i];

            sentenceBuilder.Append(
                currentQuestionSentence,
                sentenceReadIndex,
                match.StartIndex - sentenceReadIndex);
            sentenceBuilder.Append(
                BuildAnswerProgress(match.SentenceWord, ref typedLetterIndex));

            sentenceReadIndex = match.EndIndex;
        }

        sentenceBuilder.Append(
            currentQuestionSentence,
            sentenceReadIndex,
            currentQuestionSentence.Length - sentenceReadIndex);

        wordText.text = sentenceBuilder.ToString();
        wordText.color = normalWordColor;
    }

    private string BuildAnswerProgress(
        string answerAsWritten,
        ref int typedLetterIndex)
    {
        if (string.IsNullOrEmpty(answerAsWritten))
            return string.Empty;

        StringBuilder progressBuilder = new StringBuilder(answerAsWritten.Length * 2);
        progressBuilder.Append(answerAsWritten[0]);

        int missingCount = answerAsWritten.Length - 1;

        for (int i = 0; i < missingCount; i++)
        {
            progressBuilder.Append(' ');

            if (typedLetterIndex < typedLetters.Count)
            {
                char typedCharacter = typedLetters[typedLetterIndex][0];
                char sentenceCharacter = answerAsWritten[i + 1];

                progressBuilder.Append(
                    char.IsUpper(sentenceCharacter)
                        ? char.ToUpperInvariant(typedCharacter)
                        : char.ToLowerInvariant(typedCharacter));
            }
            else
            {
                progressBuilder.Append('_');
            }

            typedLetterIndex++;
        }

        return progressBuilder.ToString();
    }

    private int GetRequiredLetterCount()
    {
        int requiredLetterCount = 0;

        for (int i = 0; i < currentAnswerMatches.Count; i++)
            requiredLetterCount += Mathf.Max(0, currentAnswerMatches[i].SentenceWord.Length - 1);

        return requiredLetterCount;
    }

    private bool DoesTypedAnswerMatch()
    {
        int requiredLetterCount = GetRequiredLetterCount();

        if (typedLetters.Count != requiredLetterCount)
            return false;

        int typedLetterIndex = 0;

        for (int matchIndex = 0; matchIndex < currentAnswerMatches.Count; matchIndex++)
        {
            string sentenceWord = currentAnswerMatches[matchIndex].SentenceWord;

            for (int characterIndex = 1; characterIndex < sentenceWord.Length; characterIndex++)
            {
                char expectedCharacter = char.ToLowerInvariant(sentenceWord[characterIndex]);
                char typedCharacter = char.ToLowerInvariant(typedLetters[typedLetterIndex][0]);

                if (typedCharacter != expectedCharacter)
                    return false;

                typedLetterIndex++;
            }
        }

        return true;
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
        SceneManager.LoadScene("Loader Scene");
    }

    public void OnRewardScreenOpen()
    {
        if (audioManager != null)
        {
            audioManager.StopNarration();
            audioManager.StopBackgroundMusic();
        }
    }
}
