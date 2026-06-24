using System.Collections.Generic;
using UnityEngine;

namespace GuessWhoIAm
{
    [CreateAssetMenu(menuName = "Guess Who I Am/Question Database", fileName = "GuessWhoQuestionDatabase")]
    public class GuessWhoQuestionDatabase : ScriptableObject
    {
        public List<GuessWhoQuestionData> questions = new List<GuessWhoQuestionData>();

        public List<GuessWhoQuestionData> GetValidQuestions()
        {
            List<GuessWhoQuestionData> valid = new List<GuessWhoQuestionData>();

            for (int i = 0; i < questions.Count; i++)
            {
                if (questions[i] != null && questions[i].IsValid())
                    valid.Add(questions[i]);
            }

            return valid;
        }

        public List<string> GetAnswersExcept(string answer)
        {
            List<string> answers = new List<string>();

            for (int i = 0; i < questions.Count; i++)
            {
                GuessWhoQuestionData question = questions[i];
                if (question == null || string.IsNullOrWhiteSpace(question.answer))
                    continue;

                if (!string.Equals(question.answer.Trim(), answer.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    answers.Add(question.answer.Trim());
            }

            return answers;
        }
    }
}
