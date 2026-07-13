using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureQuestQuizManager : MonoBehaviour
{
    [Header("Managers")]
    public TreasureQuestGameManager gameManager;
    public TreasureQuestLevelManager levelManager;
    public TreasureQuestUIManager uiManager;
    public TreasureQuestAudioManager audioManager;
    public TreasureQuestQuestionDatabase questionDatabase;

    [Header("Quiz Settings")]
    public string selectedSubject = "English";
    [Min(1)] public int questionsPerGateSession = 5;
    [Tooltip("Default Treasure Quest rule: all 5 must be correct. Turn requireAllCorrectToUnlock off if another game needs partial pass.")]
    public bool requireAllCorrectToUnlock = true;
    [Tooltip("Used only when requireAllCorrectToUnlock is false. 0 means finish-only unlock.")]
    [Min(0)] public int requiredCorrectToUnlock = 5;
    [Min(0)] public int pointsPerCorrectAnswer = 10;
    public bool enableCoins = true;
    public bool shuffleAnswerOptions = true;
    [Range(0.2f, 2f)] public float answerFeedbackDelay = 0.75f;
    [Tooltip("After all answers are correct, keep gameplay visible briefly so the big gate can switch to the open sprite before the result panel appears.")]
    [Range(0f, 2f)] public float openGateBeforeResultDelay = 0.85f;

    private readonly List<TreasureQuestQuestion> sessionQuestions = new List<TreasureQuestQuestion>();
    private int currentGate;
    private int currentQuestionIndex;
    private int currentCorrectAnswerIndex;
    private int correctCount;
    private int sessionCoins;
    private int totalCoins;
    private bool acceptingAnswer;

    public void Initialize(TreasureQuestGameManager game, TreasureQuestLevelManager level, TreasureQuestUIManager ui, TreasureQuestAudioManager audio, TreasureQuestQuestionDatabase database)
    {
        gameManager = game;
        levelManager = level;
        uiManager = ui;
        audioManager = audio;
        questionDatabase = database;

        totalCoins = TreasureQuestSaveManager.LoadCoins();
        uiManager?.SetupAnswerButtons(this);
        uiManager?.UpdateCoinText(totalCoins);
    }

    public void StartGate(int gateNumber)
    {
        currentGate = Mathf.Clamp(gateNumber, 1, 5);
        currentQuestionIndex = 0;
        correctCount = 0;
        sessionCoins = 0;
        acceptingAnswer = false;

        if (questionDatabase == null)
        {
            Debug.LogError("TreasureQuestQuizManager: Question Database missing.");
            return;
        }

        List<TreasureQuestQuestion> pool = questionDatabase.GetQuestions(selectedSubject, currentGate);
        if (pool.Count == 0)
        {
            Debug.LogError("TreasureQuestQuizManager: No questions found for subject " + selectedSubject + " gate " + currentGate + ".");
            return;
        }

        Shuffle(pool);
        sessionQuestions.Clear();

        int amount = Mathf.Min(questionsPerGateSession, pool.Count);
        for (int i = 0; i < amount; i++)
            sessionQuestions.Add(pool[i]);

        uiManager?.SetGameplayHeader(currentGate);
        uiManager?.SetGateStatus(false, sessionQuestions.Count, requireAllCorrectToUnlock);
        uiManager?.UpdateProgress(0, sessionQuestions.Count);
        uiManager?.UpdateCoinText(totalCoins);

        ShowCurrentQuestion();
    }

    public void OnAnswerSelected(int answerIndex, TreasureQuestAnswerButton sourceButton)
    {
        if (!acceptingAnswer) return;

        acceptingAnswer = false;
        uiManager?.SetAllAnswerButtonsInteractable(false);

        bool isCorrect = answerIndex == currentCorrectAnswerIndex;

        if (isCorrect)
        {
            correctCount++;
            if (enableCoins)
            {
                sessionCoins += pointsPerCorrectAnswer;
                totalCoins += pointsPerCorrectAnswer;
                TreasureQuestSaveManager.SaveCoins(totalCoins);
                uiManager?.UpdateCoinText(totalCoins);
            }

            sourceButton?.SetState(TreasureQuestAnswerVisualState.Correct, true);
            audioManager?.PlayCorrect();
        }
        else
        {
            sourceButton?.SetState(TreasureQuestAnswerVisualState.Wrong, true);
            HighlightCorrectAnswer();
            audioManager?.PlayWrong();
        }

        currentQuestionIndex++;
        uiManager?.UpdateProgress(currentQuestionIndex, sessionQuestions.Count);
        StartCoroutine(GoNextAfterDelay());
    }

    private void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= sessionQuestions.Count)
        {
            FinishGateSession();
            return;
        }

        TreasureQuestQuestion question = sessionQuestions[currentQuestionIndex];
        if (question == null) return;

        uiManager?.SetQuestion(question.questionText);

        string[] answers = BuildAnswerOptions(question, out currentCorrectAnswerIndex);
        uiManager?.SetAnswerData(answers);
        uiManager?.SetAllAnswerButtonsInteractable(true);
        acceptingAnswer = true;
    }

    private string[] BuildAnswerOptions(TreasureQuestQuestion question, out int correctIndex)
    {
        var answerList = new List<AnswerRuntimeData>();

        for (int i = 0; i < question.options.Length; i++)
        {
            answerList.Add(new AnswerRuntimeData
            {
                text = question.options[i],
                isCorrect = i == question.correctOptionIndex
            });
        }

        if (shuffleAnswerOptions)
            Shuffle(answerList);

        string[] result = new string[answerList.Count];
        correctIndex = 0;

        for (int i = 0; i < answerList.Count; i++)
        {
            result[i] = answerList[i].text;
            if (answerList[i].isCorrect)
                correctIndex = i;
        }

        return result;
    }

    private void HighlightCorrectAnswer()
    {
        if (uiManager == null || uiManager.answerButtons == null) return;
        if (currentCorrectAnswerIndex < 0 || currentCorrectAnswerIndex >= uiManager.answerButtons.Length) return;

        TreasureQuestAnswerButton correctButton = uiManager.answerButtons[currentCorrectAnswerIndex];
        if (correctButton != null)
            correctButton.SetState(TreasureQuestAnswerVisualState.Correct, true);
    }

    private IEnumerator GoNextAfterDelay()
    {
        yield return new WaitForSeconds(answerFeedbackDelay);
        ShowCurrentQuestion();
    }

    private void FinishGateSession()
    {
        int requiredCorrect = GetRequiredCorrectCount();
        bool passed = requiredCorrect <= 0 || correctCount >= requiredCorrect;

        if (passed)
        {
            levelManager?.CompleteGate(currentGate);
            uiManager?.SetGameplayGate(true);
            uiManager?.SetGateStatus(true, sessionQuestions.Count, requireAllCorrectToUnlock);
        }
        else
        {
            uiManager?.SetGameplayGate(false);
            uiManager?.SetGateStatus(false, sessionQuestions.Count, requireAllCorrectToUnlock);
        }

        bool finalTreasureUnlocked = levelManager != null && levelManager.IsFinalTreasureUnlocked();
        StartCoroutine(ShowResultAfterGameplayGateUpdate(passed, finalTreasureUnlocked));
    }

    private IEnumerator ShowResultAfterGameplayGateUpdate(bool passed, bool finalTreasureUnlocked)
    {
        if (passed && openGateBeforeResultDelay > 0f)
            yield return new WaitForSeconds(openGateBeforeResultDelay);

        gameManager?.OnGateSessionFinished(passed, currentGate, correctCount, sessionQuestions.Count, sessionCoins, finalTreasureUnlocked);
    }

    private int GetRequiredCorrectCount()
    {
        int total = sessionQuestions != null && sessionQuestions.Count > 0 ? sessionQuestions.Count : questionsPerGateSession;

        if (requireAllCorrectToUnlock)
            return total;

        return Mathf.Clamp(requiredCorrectToUnlock, 0, total);
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private struct AnswerRuntimeData
    {
        public string text;
        public bool isCorrect;
    }
}
