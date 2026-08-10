using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OddClawGeneralAnswerEntry
{
    public string text;
    public Sprite sprite;
    public bool isCorrect;
}

[Serializable]
public class OddClawGeneralQuestionEntry
{
    [TextArea(2, 5)] public string questionText;
    public OddClawAnswerDisplayMode displayMode = OddClawAnswerDisplayMode.Text;
    public List<OddClawGeneralAnswerEntry> answers = new List<OddClawGeneralAnswerEntry>();
}

[CreateAssetMenu(
    menuName = "Odd Claw Catch/Question Generators/General Question Generator",
    fileName = "OddClawGeneralQuestionGenerator")]
public class OddClawGeneralQuestionGenerator : OddClawQuestionGeneratorBase
{
    [Header("Limited Run")]
    [Min(1)] public int questionsPerRun = 5;
    public string completionTitle = "All Waves Completed!";
    public bool shuffleAnswerOrder = true;

    [Header("Inspector Question Bank")]
    public List<OddClawGeneralQuestionEntry> questionBank =
        new List<OddClawGeneralQuestionEntry>();

    [NonSerialized] private readonly List<OddClawQuestionData> _selectedQuestions =
        new List<OddClawQuestionData>();
    [NonSerialized] private int _nextQuestionIndex;
    [NonSerialized] private bool _runPrepared;

    public int SelectedQuestionCount => _selectedQuestions.Count;
    public int QuestionsAlreadyProvided => _nextQuestionIndex;
    public bool HasRemainingQuestions =>
        _runPrepared && _nextQuestionIndex < _selectedQuestions.Count;

    public bool PrepareRun(out string error)
    {
        _selectedQuestions.Clear();
        _nextQuestionIndex = 0;
        _runPrepared = false;

        List<OddClawQuestionData> validQuestions = new List<OddClawQuestionData>();
        if (questionBank != null)
        {
            for (int i = 0; i < questionBank.Count; i++)
            {
                if (TryBuildQuestion(questionBank[i], out OddClawQuestionData data, out string validationError))
                {
                    validQuestions.Add(data);
                }
                else
                {
                    Debug.LogWarning(
                        "Odd Claw General Question Generator ignored question "
                        + (i + 1)
                        + ": "
                        + validationError,
                        this);
                }
            }
        }

        if (validQuestions.Count == 0)
        {
            error = "The General Question Generator has no valid questions.";
            return false;
        }

        Shuffle(validQuestions);
        int selectionCount = Mathf.Min(Mathf.Max(1, questionsPerRun), validQuestions.Count);
        for (int i = 0; i < selectionCount; i++)
            _selectedQuestions.Add(validQuestions[i]);

        _runPrepared = true;
        error = string.Empty;
        return true;
    }

    public override OddClawQuestionData GenerateQuestion(int wave, int requestedOptionCount)
    {
        if (!_runPrepared)
        {
            if (!PrepareRun(out string prepareError))
            {
                Debug.LogWarning(prepareError, this);
                return null;
            }
        }

        if (!HasRemainingQuestions)
            return null;

        OddClawQuestionData selected = _selectedQuestions[_nextQuestionIndex];
        _nextQuestionIndex++;
        return selected;
    }

    private bool TryBuildQuestion(
        OddClawGeneralQuestionEntry source,
        out OddClawQuestionData data,
        out string error)
    {
        data = null;

        if (source == null)
        {
            error = "Question entry is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(source.questionText))
        {
            error = "Question text is empty.";
            return false;
        }

        int minimum = Mathf.Max(2, minimumOptions);
        int maximum = Mathf.Max(minimum, maximumOptions);
        if (source.answers == null || source.answers.Count < minimum)
        {
            error = "At least " + minimum + " answers are required.";
            return false;
        }

        if (source.answers.Count > maximum)
        {
            error = "The existing answer layout supports up to " + maximum + " answers.";
            return false;
        }

        List<OddClawAnswerOption> options = new List<OddClawAnswerOption>();
        int correctIndex = -1;
        int correctCount = 0;

        for (int i = 0; i < source.answers.Count; i++)
        {
            OddClawGeneralAnswerEntry answer = source.answers[i];
            if (answer == null)
            {
                error = "Answer " + (i + 1) + " is missing.";
                return false;
            }

            if (source.displayMode == OddClawAnswerDisplayMode.Text
                && string.IsNullOrWhiteSpace(answer.text))
            {
                error = "Text answer " + (i + 1) + " is empty.";
                return false;
            }

            if (IsImageDisplayMode(source.displayMode)
                && answer.sprite == null)
            {
                error = "Image answer " + (i + 1) + " has no sprite.";
                return false;
            }

            if (answer.isCorrect)
            {
                correctCount++;
                correctIndex = i;
            }

            options.Add(new OddClawAnswerOption(
                answer.text,
                answer.sprite,
                string.Empty));
        }

        if (correctCount != 1)
        {
            error = "Exactly one answer must be marked correct.";
            return false;
        }

        if (shuffleAnswerOrder)
            correctIndex = ShuffleOptionsKeepingCorrect(options, correctIndex);

        data = new OddClawQuestionData
        {
            questionText = source.questionText.Trim(),
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = source.displayMode
        };

        error = string.Empty;
        return true;
    }

    private static bool IsImageDisplayMode(OddClawAnswerDisplayMode displayMode)
    {
        return displayMode == OddClawAnswerDisplayMode.Sprite
            || displayMode == OddClawAnswerDisplayMode.SpriteWithOptionalText;
    }

    private static void Shuffle<T>(List<T> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, values.Count);
            T temporary = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = temporary;
        }
    }
}
