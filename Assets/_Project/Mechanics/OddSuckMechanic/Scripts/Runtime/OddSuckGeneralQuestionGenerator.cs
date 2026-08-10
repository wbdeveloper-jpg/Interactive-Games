using System;
using System.Collections.Generic;
using UnityEngine;

namespace OddSuckMechanic
{
    [Serializable]
    public class OddSuckGeneralAnswer
    {
        [Tooltip("Shown when the question uses Text display mode. It can also be used as an optional label in Sprite mode.")]
        public string answerText;
        [Tooltip("Required when the question uses Sprite display mode.")]
        public Sprite answerImage;
        public bool isCorrect;
    }

    [Serializable]
    public class OddSuckGeneralQuestion
    {
        [TextArea(2, 4)]
        public string questionText;
        public OddSuckItemDisplayMode displayMode = OddSuckItemDisplayMode.Text;
        public List<OddSuckGeneralAnswer> answers = new List<OddSuckGeneralAnswer>();
    }

    public class OddSuckGeneralQuestionGenerator : OddSuckQuestionGeneratorBase
    {
        [Header("Limited General Question Run")]
        [Tooltip("The run randomly selects this many valid questions. If fewer are available, every valid question is used.")]
        [SerializeField, Min(1)] private int questionsPerRun = 5;

        [Header("Direct Question Bank")]
        [SerializeField] private List<OddSuckGeneralQuestion> questions = new List<OddSuckGeneralQuestion>();

        private readonly List<int> preparedQuestionIndices = new List<int>();
        private int nextPreparedQuestion;
        private bool runPrepared;

        public int RequestedQuestionsPerRun => Mathf.Max(1, questionsPerRun);
        public int PreparedQuestionCount => preparedQuestionIndices.Count;
        public int QuestionsServed => Mathf.Clamp(nextPreparedQuestion, 0, preparedQuestionIndices.Count);
        public bool IsRunPrepared => runPrepared;
        public bool IsRunComplete => runPrepared && nextPreparedQuestion >= preparedQuestionIndices.Count;

        public override bool CanGenerate()
        {
            return GetValidQuestionCount() > 0;
        }

        public void PrepareRun()
        {
            preparedQuestionIndices.Clear();
            nextPreparedQuestion = 0;

            if (questions == null)
            {
                runPrepared = true;
                return;
            }

            for (int i = 0; i < questions.Count; i++)
            {
                if (IsQuestionValid(questions[i]))
                {
                    preparedQuestionIndices.Add(i);
                }
            }

            Shuffle(preparedQuestionIndices);

            int requestedCount = Mathf.Max(1, questionsPerRun);
            if (preparedQuestionIndices.Count > requestedCount)
            {
                preparedQuestionIndices.RemoveRange(
                    requestedCount,
                    preparedQuestionIndices.Count - requestedCount);
            }

            runPrepared = true;
        }

        public override OddSuckGeneratedQuestion Generate(int waveIndex)
        {
            if (!runPrepared)
            {
                PrepareRun();
            }

            if (IsRunComplete)
            {
                return null;
            }

            int questionIndex = preparedQuestionIndices[nextPreparedQuestion];
            nextPreparedQuestion++;
            return BuildGeneratedQuestion(questions[questionIndex]);
        }

        private OddSuckGeneratedQuestion BuildGeneratedQuestion(OddSuckGeneralQuestion source)
        {
            OddSuckGeneratedQuestion generated = new OddSuckGeneratedQuestion
            {
                questionText = string.IsNullOrWhiteSpace(source.questionText)
                    ? "Choose the correct answer"
                    : source.questionText.Trim(),
                displayMode = source.displayMode
            };

            for (int i = 0; i < source.answers.Count; i++)
            {
                OddSuckGeneralAnswer answer = source.answers[i];
                if (!IsAnswerUsable(answer, source.displayMode))
                {
                    continue;
                }

                generated.items.Add(new OddSuckItemData
                {
                    displayText = answer.answerText == null ? string.Empty : answer.answerText.Trim(),
                    icon = answer.answerImage,
                    isOdd = answer.isCorrect
                });
            }

            return generated;
        }

        private int GetValidQuestionCount()
        {
            if (questions == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                if (IsQuestionValid(questions[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsQuestionValid(OddSuckGeneralQuestion question)
        {
            if (question == null || question.answers == null)
            {
                return false;
            }

            int usableAnswers = 0;
            int correctAnswers = 0;

            for (int i = 0; i < question.answers.Count; i++)
            {
                OddSuckGeneralAnswer answer = question.answers[i];
                if (!IsAnswerUsable(answer, question.displayMode))
                {
                    continue;
                }

                usableAnswers++;
                if (answer.isCorrect)
                {
                    correctAnswers++;
                }
            }

            return usableAnswers >= 2 && correctAnswers == 1;
        }

        private static bool IsAnswerUsable(OddSuckGeneralAnswer answer, OddSuckItemDisplayMode displayMode)
        {
            if (answer == null)
            {
                return false;
            }

            if (displayMode == OddSuckItemDisplayMode.Sprite)
            {
                return answer.answerImage != null;
            }

            return !string.IsNullOrWhiteSpace(answer.answerText);
        }

        private void OnValidate()
        {
            questionsPerRun = Mathf.Max(1, questionsPerRun);
        }
    }
}
