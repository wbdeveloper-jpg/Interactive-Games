using DG.Tweening;
using RewardSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeroInMaking : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Character Levels")]
    [Tooltip("Index 0 = Level 1")]
    [SerializeField] private List<GameObject> heroLevelCharacters;
    [SerializeField] private float entryDelay;
    private Animator currentHeroAnimator;

    [Header("Animator Parameters")]
    [SerializeField] private string talkingBool = "Talking";
    [SerializeField] private string correctTrigger = "SPL";
    [SerializeField] private string sadTrigger = "Sad";
    [SerializeField] private string lowestLevelIdleStateName = "Level 0 Idle";

    [Header("Hero Level UI")]
    [SerializeField] private TMP_Text heroLevelText;

    [Header("Dialogue UI")]
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Dialogue Audio")]
    [SerializeField] private AudioSource narrationSource;
    [SerializeField] private float fallbackTypewriterSpeed = 0.03f;

    [Header("Dialogue Timing")]
    [SerializeField] private float dialogueLineGap = 0.7f;

    [Header("Intro / Outro Dialogues")]
    public List<DialogueData> introDialogues;
    public List<DialogueData> outroDialoguesMax;
    public List<DialogueData> outroDialoguesMid;
    public List<DialogueData> outroDialoguesMin;

    [Header("Question Board")]
    [SerializeField] private CanvasGroup questionBoardGroup;
    [SerializeField] private RectTransform questionBoard;
    [SerializeField] private TMP_Text questionText;

    [Header("Replay Audio Button")]
    [SerializeField] private CanvasGroup replayAudioGroup;
    [SerializeField] private CanvasGroup instruction;

    [Header("Answer Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Particles")]
    [SerializeField] private ParticleSystem correctParticle;
    [SerializeField] private ParticleSystem wrongParticle;
    [SerializeField] private ParticleSystem cardBgParticle;

    [Header("Timings")]
    [SerializeField] private float boardEntryDuration = 0.5f;
    [SerializeField] private float boardRotateDuration = 0.6f;
    [SerializeField] private float buttonPopDelay = 0.1f;
    [SerializeField] private float afterAnswerDelay = 1.5f;

    [Header("Game Over UI")]
    [SerializeField] private CanvasGroup gameOverGroup;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private float gameOverFadeDuration = 0.4f;

    [Header("Question Bank")]
    [Tooltip("Add all available questions for this game here. At runtime, the game randomly selects from this list.")]
    [SerializeField] private List<QuestionData> questionBank = new();

    [Tooltip("How many random questions should be selected from the question bank.")]
    [SerializeField] private int randomQuestionCount = 5;

    [Tooltip("If false, the same question will not be selected twice in one game.")]
    [SerializeField] private bool allowDuplicateQuestions = false;

    [Header("Runtime Questions")]
    [Tooltip("Runtime selected questions. This is filled automatically from Question Bank when the game starts.")]
    [SerializeField] private List<QuestionData> questions = new();

    [Header("Question Common Ending Audio")]
    [Tooltip("Common audio played after every question's unique audio. The common ending text should already be included inside each Question text.")]
    [SerializeField] private AudioClip questionCommonEndingAudio;

    [Tooltip("Small pause between the unique question audio and the common ending audio.")]
    [SerializeField] private float questionToCommonEndingGap = 0.4f;

    [Header("Answer Feedback Dialogues")]
    [SerializeField] private List<DialogueData> correctFeedbackDialogues;
    [SerializeField] private List<DialogueData> wrongFeedbackDialogues;
    [SerializeField] private float feedbackFadeDuration = 0.25f;

    private const int DefaultMaxEvaluationLevel = 6;

    private int heroLevel = 1;
    private int currentQuestionIndex = 0;
    private bool inputLocked = false;
    private bool questionResolved = false;
    private bool gameFlowStarted = false;
    private Coroutine replayAudioCoroutine;
    private float timeTaken;
    private int correctAnswerCount = 0;
    private int mistakeCount = 0;

    public int avgTime;

    public List<SkillEntry> _skills = new()
    {
        new SkillEntry(BloomSkillType.Evaluate,   100f, timeWeight: 0.2f, accuracyWeight: 0.8f),
        new SkillEntry(BloomSkillType.Understand,  50f, timeWeight: 0.6f, accuracyWeight: 0.4f),
    };

    private GameEvaluationData _evaluationData = new();

    private void Start()
    {
        Application.targetFrameRate = 60;

        ResetRuntimeState();
        SelectRandomQuestionsFromInspectorBank();
        ResetUI();
        StopAllParticles();
        StopNarration();
        UpdateHeroLevelUI();
        SetupButtonCallbacks();

        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPreGame(_skills);
        }
        else
        {
            Debug.LogWarning("RewardManager.Instance is missing. Continuing without pre-game reward screen.");
        }

        if (!gameFlowStarted)
        {
            gameFlowStarted = true;
            StartCoroutine(MainFlow());
        }
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    #region Main Flow

    private IEnumerator MainFlow()
    {
        if (RewardManager.Instance != null)
        {
            yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
        }

        ActivateHeroLevel(heroLevel);

        if (entryDelay > 0f)
        {
            yield return new WaitForSeconds(entryDelay);
        }

        yield return PlayDialogueSequence(introDialogues, true);

        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.StartTimer();
        }
        else
        {
            Debug.LogWarning("GameTimer.Instance is missing. Time score will be calculated as 0.");
        }

        yield return ShowQuestionBoard();
        yield return QuestionLoop();
        yield return HideQuestionBoard();

        if (heroLevel <= 1)
        {
            Debug.Log("I failed to powerup any means, Hero Level is -> " + heroLevel);
            yield return PlayDialogueSequence(outroDialoguesMin, false);
        }
        else if (heroLevel > 1 && heroLevel <= 5)
        {
            Debug.Log("I am really very powerful right now, Hero Level is -> " + heroLevel);
            yield return PlayDialogueSequence(outroDialoguesMid, false);
        }
        else
        {
            Debug.Log("I reached my maximum potential. Hero Level is -> " + heroLevel);
            yield return PlayDialogueSequence(outroDialoguesMax, false);
        }

        HideDialogueText();
        ShowGameOverPanel();
    }

    #endregion

    #region Character Handling

    private void ActivateHeroLevel(int level)
    {
        if (heroLevelCharacters == null || heroLevelCharacters.Count == 0)
        {
            currentHeroAnimator = null;
            Debug.LogWarning("No hero level characters assigned.");
            return;
        }

        foreach (GameObject hero in heroLevelCharacters)
        {
            if (hero != null)
            {
                hero.SetActive(false);
            }
        }

        int index = Mathf.Clamp(level - 1, 0, heroLevelCharacters.Count - 1);
        GameObject selectedHero = heroLevelCharacters[index];

        if (selectedHero == null)
        {
            currentHeroAnimator = null;
            Debug.LogWarning($"Hero character at index {index} is missing.");
            return;
        }

        selectedHero.SetActive(true);
        currentHeroAnimator = selectedHero.GetComponentInChildren<Animator>();
    }

    private int GetMaxHeroLevel()
    {
        if (heroLevelCharacters != null && heroLevelCharacters.Count > 0)
        {
            return heroLevelCharacters.Count;
        }

        return DefaultMaxEvaluationLevel;
    }

    #endregion

    #region Dialogue

    private IEnumerator PlayDialogueSequence(List<DialogueData> dialogues, bool hideAfter)
    {
        if (dialogues == null || dialogues.Count == 0)
        {
            yield break;
        }

        if (dialogueGroup == null || dialogueText == null)
        {
            Debug.LogWarning("Dialogue UI references are missing.");
            yield break;
        }

        dialogueGroup.gameObject.SetActive(true);
        dialogueGroup.alpha = 1f;
        dialogueText.gameObject.SetActive(true);

        foreach (DialogueData line in dialogues)
        {
            if (line == null || string.IsNullOrEmpty(line.text))
            {
                continue;
            }

            yield return PlayTextWithAudio(dialogueText, line);

            if (dialogueLineGap > 0f)
            {
                yield return new WaitForSeconds(dialogueLineGap);
            }
        }

        if (hideAfter)
        {
            yield return FadeOutDialogue(0.3f);
        }
    }

    private IEnumerator FadeOutDialogue(float duration)
    {
        if (dialogueGroup == null)
        {
            yield break;
        }

        dialogueGroup.DOKill();
        dialogueGroup.DOFade(0f, duration);
        yield return new WaitForSeconds(duration);
        dialogueGroup.gameObject.SetActive(false);
    }

    private IEnumerator PlayTextWithAudio(TMP_Text textUI, DialogueData data)
    {
        if (textUI == null || data == null)
        {
            yield break;
        }

        string lineText = data.text ?? string.Empty;
        textUI.text = string.Empty;
        SetTalking(true);

        float duration = Mathf.Max(0.01f, fallbackTypewriterSpeed * lineText.Length);

        if (data.audio != null && narrationSource != null)
        {
            narrationSource.Stop();
            narrationSource.clip = data.audio;
            narrationSource.Play();
            duration = Mathf.Max(0.01f, data.audio.length);
        }

        float delay = lineText.Length > 0 ? duration / lineText.Length : 0f;

        foreach (char c in lineText)
        {
            textUI.text += c;

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        if (data.audio != null && narrationSource != null)
        {
            yield return new WaitUntil(() => narrationSource == null || !narrationSource.isPlaying);
        }

        SetTalking(false);
    }

    private IEnumerator PlayQuestionTextWithAudio(TMP_Text textUI, QuestionData data)
    {
        if (textUI == null || data == null)
        {
            yield break;
        }

        string fullQuestionText = data.question ?? string.Empty;
        textUI.text = string.Empty;
        SetTalking(true);

        float questionAudioDuration = data.questionAudio != null ? data.questionAudio.length : 0f;
        float commonAudioDuration = questionCommonEndingAudio != null ? questionCommonEndingAudio.length : 0f;
        float gapDuration = questionCommonEndingAudio != null ? Mathf.Max(0f, questionToCommonEndingGap) : 0f;

        float totalAudioDuration = questionAudioDuration + gapDuration + commonAudioDuration;
        float fallbackDuration = Mathf.Max(0.01f, fallbackTypewriterSpeed * fullQuestionText.Length);
        float typewriterDuration = totalAudioDuration > 0f ? totalAudioDuration : fallbackDuration;

        Coroutine audioSequenceCoroutine = null;
        if (narrationSource != null && (data.questionAudio != null || questionCommonEndingAudio != null))
        {
            audioSequenceCoroutine = StartCoroutine(PlayQuestionAudioSequence(data.questionAudio));
        }

        yield return TypeTextOverDuration(textUI, fullQuestionText, typewriterDuration);

        if (audioSequenceCoroutine != null)
        {
            yield return audioSequenceCoroutine;
        }

        SetTalking(false);
    }

    private IEnumerator TypeTextOverDuration(TMP_Text textUI, string text, float duration)
    {
        if (textUI == null)
        {
            yield break;
        }

        string safeText = text ?? string.Empty;
        textUI.text = string.Empty;

        if (safeText.Length == 0)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            yield break;
        }

        float delay = Mathf.Max(0.001f, duration / safeText.Length);

        foreach (char c in safeText)
        {
            textUI.text += c;
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator PlayQuestionAudioSequence(AudioClip questionAudio)
    {
        if (narrationSource == null)
        {
            yield break;
        }

        if (questionAudio != null)
        {
            narrationSource.Stop();
            narrationSource.clip = questionAudio;
            narrationSource.Play();
            yield return new WaitUntil(() => narrationSource == null || !narrationSource.isPlaying);
        }

        if (questionCommonEndingAudio != null)
        {
            float gap = Mathf.Max(0f, questionToCommonEndingGap);
            if (gap > 0f)
            {
                yield return new WaitForSeconds(gap);
            }

            narrationSource.Stop();
            narrationSource.clip = questionCommonEndingAudio;
            narrationSource.Play();
            yield return new WaitUntil(() => narrationSource == null || !narrationSource.isPlaying);
        }
    }

    private void SetTalking(bool value)
    {
        if (currentHeroAnimator != null && !string.IsNullOrEmpty(talkingBool))
        {
            currentHeroAnimator.SetBool(talkingBool, value);
        }
    }

    private void HideDialogueText()
    {
        if (dialogueText != null)
        {
            dialogueText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Questions

    private IEnumerator ShowQuestionBoard()
    {
        if (questionBoardGroup == null || questionBoard == null)
        {
            Debug.LogWarning("Question board references are missing.");
            yield break;
        }

        questionBoardGroup.gameObject.SetActive(true);
        questionBoardGroup.alpha = 0f;
        questionBoard.localScale = Vector3.one * 0.95f;
        questionBoard.localRotation = Quaternion.identity;

        questionBoardGroup.DOKill();
        questionBoard.DOKill();

        questionBoardGroup.DOFade(1f, boardEntryDuration);
        questionBoard.DOScale(1f, boardEntryDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(boardEntryDuration);
    }

    private IEnumerator HideQuestionBoard()
    {
        if (questionBoardGroup == null || questionBoard == null)
        {
            yield break;
        }

        questionBoardGroup.DOKill();
        questionBoard.DOKill();

        questionBoardGroup.DOFade(0f, boardRotateDuration);
        questionBoard.DOScale(0.95f, boardRotateDuration);

        yield return new WaitForSeconds(boardRotateDuration);
        questionBoardGroup.gameObject.SetActive(false);
    }

    private void SelectRandomQuestionsFromInspectorBank()
    {
        if (questionBank == null || questionBank.Count == 0)
        {
            Debug.LogWarning("Question Bank is empty. Falling back to Runtime Questions list if it has any manually assigned questions.");
            return;
        }

        int requiredQuestionCount = Mathf.Max(1, randomQuestionCount);
        List<QuestionData> sourceQuestions = new List<QuestionData>(questionBank);
        List<QuestionData> selectedQuestions = new List<QuestionData>();

        if (allowDuplicateQuestions)
        {
            for (int i = 0; i < requiredQuestionCount; i++)
            {
                selectedQuestions.Add(CloneQuestionData(sourceQuestions[Random.Range(0, sourceQuestions.Count)]));
            }
        }
        else
        {
            ShuffleList(sourceQuestions);
            int count = Mathf.Min(requiredQuestionCount, sourceQuestions.Count);

            for (int i = 0; i < count; i++)
            {
                selectedQuestions.Add(CloneQuestionData(sourceQuestions[i]));
            }
        }

        questions = selectedQuestions;
        LogSelectedQuestionAudioStatus();
        Debug.Log($"Selected {questions.Count} random questions from Inspector Question Bank.");
    }

    private QuestionData CloneQuestionData(QuestionData source)
    {
        if (source == null)
        {
            return new QuestionData();
        }

        return new QuestionData
        {
            question = source.question,
            questionAudio = source.questionAudio,
            correctAnswerIndex = source.correctAnswerIndex
        };
    }

    private void LogSelectedQuestionAudioStatus()
    {
        if (questions == null)
        {
            return;
        }

        for (int i = 0; i < questions.Count; i++)
        {
            QuestionData question = questions[i];
            string audioName = question != null && question.questionAudio != null ? question.questionAudio.name : "MISSING_AUDIO";
            Debug.Log($"Selected Question {i + 1}: audio = {audioName}");
        }
    }

    private IEnumerator QuestionLoop()
    {
        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("No questions assigned.");
            yield break;
        }

        while (currentQuestionIndex < questions.Count)
        {
            QuestionData currentQuestion = questions[currentQuestionIndex];
            yield return ShowQuestion(currentQuestion);
            currentQuestionIndex++;
        }
    }

    private IEnumerator ShowQuestion(QuestionData data)
    {
        inputLocked = true;
        questionResolved = false;
        SetButtonsInteractable(false);

        if (replayAudioGroup != null)
        {
            replayAudioGroup.DOKill();
            replayAudioGroup.gameObject.SetActive(false);
        }

        if (instruction != null)
        {
            instruction.DOKill();
            instruction.gameObject.SetActive(false);
        }

        if (questionText != null)
        {
            questionText.text = string.Empty;
        }

        if (data == null)
        {
            Debug.LogWarning($"Question at index {currentQuestionIndex} is missing.");
            questionResolved = true;
            yield break;
        }

        yield return PlayQuestionTextWithAudio(questionText, data);

        yield return ShowButtons();

        bool hasQuestionAudio = data.questionAudio != null || questionCommonEndingAudio != null;

        if (replayAudioGroup != null)
        {
            replayAudioGroup.alpha = 0f;
            replayAudioGroup.gameObject.SetActive(hasQuestionAudio);

            if (hasQuestionAudio)
            {
                replayAudioGroup.DOFade(1f, 0.25f);
            }
        }

        if (instruction != null)
        {
            instruction.alpha = 0f;
            instruction.gameObject.SetActive(hasQuestionAudio);

            if (hasQuestionAudio)
            {
                instruction.DOFade(1f, 0.25f);
            }
        }

        inputLocked = false;
        SetButtonsInteractable(true);
        yield return new WaitUntil(() => questionResolved);
    }

    private IEnumerator ShowButtons()
    {
        if (yesButton == null || noButton == null)
        {
            Debug.LogWarning("Answer buttons are missing.");
            yield break;
        }

        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);

        yesButton.transform.DOKill();
        noButton.transform.DOKill();

        yesButton.transform.localScale = Vector3.zero;
        noButton.transform.localScale = Vector3.zero;

        yesButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        if (buttonPopDelay > 0f)
        {
            yield return new WaitForSeconds(buttonPopDelay);
        }

        noButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    public void OnAnswer(int index)
    {
        if (inputLocked)
        {
            return;
        }

        if (questions == null || currentQuestionIndex < 0 || currentQuestionIndex >= questions.Count)
        {
            Debug.LogWarning("Answer received, but current question index is invalid.");
            return;
        }

        StopReplayQuestionAudio();

        inputLocked = true;
        SetButtonsInteractable(false);
        StartCoroutine(OnAnswerSelected(index));
    }

    private IEnumerator OnAnswerSelected(int index)
    {
        if (replayAudioGroup != null)
        {
            replayAudioGroup.DOKill();
            replayAudioGroup.DOFade(0f, 0.2f).OnComplete(() =>
            {
                if (replayAudioGroup != null)
                {
                    replayAudioGroup.gameObject.SetActive(false);
                }
            });
        }

        if (instruction != null)
        {
            instruction.DOKill();
            instruction.DOFade(0f, 0.2f).OnComplete(() =>
            {
                if (instruction != null)
                {
                    instruction.gameObject.SetActive(false);
                }
            });
        }

        QuestionData currentQuestion = questions[currentQuestionIndex];
        bool correct = currentQuestion != null && index == currentQuestion.correctAnswerIndex;

        if (correct)
        {
            correctAnswerCount++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(0);
            }

            correctParticle?.Play();
            cardBgParticle?.Play();
            heroLevel = Mathf.Min(heroLevel + 1, GetMaxHeroLevel());
        }
        else
        {
            mistakeCount++;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(1);
            }

            wrongParticle?.Play();
            heroLevel = Mathf.Max(1, heroLevel - 1);
        }

        ActivateHeroLevel(heroLevel);

        if (heroLevel <= 1 && currentHeroAnimator != null && !string.IsNullOrEmpty(lowestLevelIdleStateName))
        {
            currentHeroAnimator.Play(lowestLevelIdleStateName, 0, 0f);
        }

        yield return ShowAnswerFeedback(correct);

        yield return new WaitForSeconds(1.5f);

        if (currentHeroAnimator != null)
        {
            currentHeroAnimator.SetTrigger(correct ? correctTrigger : sadTrigger);
        }

        UpdateHeroLevelUI();
        StartCoroutine(ResolveQuestionAfterDelay());
    }

    private IEnumerator ResolveQuestionAfterDelay()
    {
        yield return new WaitForSeconds(afterAnswerDelay);
        yield return NextQuestionTransition();
        questionResolved = true;
    }

    private IEnumerator NextQuestionTransition()
    {
        if (questionBoardGroup == null || questionBoard == null)
        {
            HideButtonsImmediate();
            yield break;
        }

        questionBoardGroup.DOKill();
        questionBoard.DOKill();

        questionBoardGroup.DOFade(0.3f, boardRotateDuration / 2f);

        questionBoard
            .DORotate(new Vector3(0f, 90f, 0f), boardRotateDuration / 2f)
            .SetEase(Ease.InSine);

        yield return new WaitForSeconds(boardRotateDuration / 2f);

        questionBoard.localRotation = Quaternion.Euler(0f, -90f, 0f);

        questionBoardGroup.DOFade(1f, boardRotateDuration / 2f);

        questionBoard
            .DORotate(Vector3.zero, boardRotateDuration / 2f)
            .SetEase(Ease.OutSine);

        if (yesButton != null)
        {
            yesButton.transform.DOKill();
            yesButton.transform.DOScale(0f, 0.2f);
        }

        if (noButton != null)
        {
            noButton.transform.DOKill();
            noButton.transform.DOScale(0f, 0.2f);
        }

        yield return new WaitForSeconds(0.2f);
        HideButtonsImmediate();
    }

    private IEnumerator ShowAnswerFeedback(bool correct)
    {
        List<DialogueData> pool = correct ? correctFeedbackDialogues : wrongFeedbackDialogues;

        if (pool == null || pool.Count == 0)
        {
            yield break;
        }

        if (dialogueGroup == null || dialogueText == null)
        {
            yield break;
        }

        DialogueData selectedDialogue = pool[Random.Range(0, pool.Count)];

        if (selectedDialogue == null || string.IsNullOrEmpty(selectedDialogue.text))
        {
            yield break;
        }

        dialogueGroup.gameObject.SetActive(true);
        dialogueGroup.alpha = 0f;

        dialogueText.gameObject.SetActive(true);
        dialogueText.text = string.Empty;

        dialogueGroup.DOKill();
        dialogueGroup.DOFade(1f, feedbackFadeDuration);

        yield return new WaitForSeconds(feedbackFadeDuration);

        yield return PlayTextWithAudio(dialogueText, selectedDialogue);

        yield return new WaitForSeconds(1.5f);

        dialogueGroup.DOFade(0f, feedbackFadeDuration);
        yield return new WaitForSeconds(feedbackFadeDuration);

        dialogueGroup.gameObject.SetActive(false);
    }

    #endregion

    #region Game Over

    private void ShowGameOverPanel()
    {
        if (GameTimer.Instance != null)
        {
            timeTaken = GameTimer.Instance.StopTimer();
        }
        else
        {
            timeTaken = 0f;
        }

        _evaluationData.timeTaken = timeTaken;
        _evaluationData.mistakeCount = mistakeCount;
        _evaluationData.accuracyScore = GetAccuracyScore();
        _evaluationData.timeScore = GameTimer.CalculateTimeScore(timeTaken, avgTime);

        Debug.Log("User's Score " + timeTaken + "/" + avgTime);
        Debug.Log("User's Accuracy Score is - " + _evaluationData.accuracyScore);
        Debug.Log("User's Time Score is - " + _evaluationData.timeScore);

        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPostGame(_skills, _evaluationData);
        }
        else
        {
            Debug.LogWarning("RewardManager.Instance is missing. Showing fallback game over panel.");
            ShowFallbackGameOverPanel();
        }
    }

    private float GetAccuracyScore()
    {
        if (questions == null || questions.Count == 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)correctAnswerCount / questions.Count);
    }

    private void ShowFallbackGameOverPanel()
    {
        if (gameOverGroup == null)
        {
            return;
        }

        gameOverGroup.gameObject.SetActive(true);
        gameOverGroup.alpha = 0f;
        gameOverGroup.DOKill();
        gameOverGroup.DOFade(1f, gameOverFadeDuration);
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Loader Scene");
    }

    #endregion

    #region Utility

    private void ShuffleList<T>(List<T> list)
    {
        if (list == null || list.Count <= 1)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void ResetRuntimeState()
    {
        heroLevel = 1;
        currentQuestionIndex = 0;
        inputLocked = false;
        questionResolved = false;
        timeTaken = 0f;
        correctAnswerCount = 0;
        mistakeCount = 0;
        _evaluationData = new GameEvaluationData();
    }

    private void UpdateHeroLevelUI()
    {
        if (heroLevelText != null)
        {
            heroLevelText.text = $"Hero Level: {heroLevel}";
        }
    }

    private void ResetUI()
    {
        SetCanvasGroupActive(dialogueGroup, false, 0f);
        SetCanvasGroupActive(questionBoardGroup, false, 0f);
        SetCanvasGroupActive(replayAudioGroup, false, 0f);
        SetCanvasGroupActive(instruction, false, 0f);
        SetCanvasGroupActive(gameOverGroup, false, 0f);

        HideButtonsImmediate();
    }

    private void SetCanvasGroupActive(CanvasGroup group, bool active, float alpha)
    {
        if (group == null)
        {
            return;
        }

        group.DOKill();
        group.alpha = alpha;
        group.gameObject.SetActive(active);
    }

    private void StopNarration()
    {
        StopReplayQuestionAudio();
    }

    private void StopAllParticles()
    {
        correctParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        wrongParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        cardBgParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void PlayCurrentQuestionAudio()
    {
        if (inputLocked || questionResolved)
        {
            return;
        }

        if (questions == null || currentQuestionIndex >= questions.Count)
        {
            return;
        }

        QuestionData currentQuestion = questions[currentQuestionIndex];
        if (currentQuestion == null || narrationSource == null)
        {
            return;
        }

        StopReplayQuestionAudio();

        replayAudioCoroutine = StartCoroutine(ReplayCurrentQuestionAudioSequence(currentQuestion));
    }

    private IEnumerator ReplayCurrentQuestionAudioSequence(QuestionData currentQuestion)
    {
        if (currentQuestion == null)
        {
            replayAudioCoroutine = null;
            yield break;
        }

        SetTalking(true);
        yield return PlayQuestionAudioSequence(currentQuestion.questionAudio);
        SetTalking(false);

        replayAudioCoroutine = null;
    }

    private IEnumerator StopTalkingAfterAudio(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetTalking(false);
    }

    private void StopReplayQuestionAudio()
    {
        if (replayAudioCoroutine != null)
        {
            StopCoroutine(replayAudioCoroutine);
            replayAudioCoroutine = null;
        }

        if (narrationSource != null)
        {
            narrationSource.Stop();
        }

        SetTalking(false);
    }

    private void SetupButtonCallbacks()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgain);
            playAgainButton.onClick.AddListener(OnPlayAgain);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnHome);
            mainMenuButton.onClick.AddListener(OnHome);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (yesButton != null)
        {
            yesButton.interactable = interactable;
        }

        if (noButton != null)
        {
            noButton.interactable = interactable;
        }
    }

    private void HideButtonsImmediate()
    {
        if (yesButton != null)
        {
            yesButton.transform.DOKill();
            yesButton.transform.localScale = Vector3.zero;
            yesButton.interactable = false;
            yesButton.gameObject.SetActive(false);
        }

        if (noButton != null)
        {
            noButton.transform.DOKill();
            noButton.transform.localScale = Vector3.zero;
            noButton.interactable = false;
            noButton.gameObject.SetActive(false);
        }
    }

    private void KillTweens()
    {
        dialogueGroup?.DOKill();
        questionBoardGroup?.DOKill();
        replayAudioGroup?.DOKill();
        instruction?.DOKill();
        gameOverGroup?.DOKill();

        if (questionBoard != null)
        {
            questionBoard.DOKill();
        }

        if (yesButton != null)
        {
            yesButton.transform.DOKill();
        }

        if (noButton != null)
        {
            noButton.transform.DOKill();
        }
    }

    public void OnPlayAgain()
    {
        Debug.Log("Play Again");
        LoadScene();
    }

    public void OnHome()
    {
        Debug.Log("Main Menu");
        MainMenu();
        UnityAndroidMediator.Instance?.PassDataToAndroid("Game Done");
        GameLoader.Instance?.SendEventToJS("Game Done", "Being Honest");
    }

    public void OnRewardScreenOpen()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
    }

    #endregion
}

[System.Serializable]
public class DialogueData
{
    [TextArea] public string text;
    public AudioClip audio;
}

[System.Serializable]
public class QuestionData
{
    [TextArea] public string question;
    public AudioClip questionAudio;
    public int correctAnswerIndex;
}
