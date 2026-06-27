using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordShuffleDragSwap
{
    [Serializable]
    public class WordShuffleQuestionEntry
    {
        [TextArea(2, 4)]
        [Tooltip("Question shown to the player. Example: Which animal says meow?")]
        public string Question;

        [Tooltip("Correct answer to build with drag-swap tiles. Letters and digits only are used.")]
        public string Answer;

        [Tooltip("Optional short hint shown with the question if required.")]
        public string Hint;

        [Tooltip("Optional image for the question.")]
        public Sprite Picture;

        [Tooltip("Optional audio/voice-over for the question.")]
        public AudioClip VoiceOver;

        public string CleanAnswer(bool upperCase = true)
        {
            if (string.IsNullOrWhiteSpace(Answer))
                return string.Empty;

            string cleaned = new string(Answer.Trim().Where(char.IsLetterOrDigit).ToArray());
            return upperCase ? cleaned.ToUpperInvariant() : cleaned;
        }
    }

    [CreateAssetMenu(menuName = "Word Shuffle Drag Swap/Question Database", fileName = "WordShuffleQuestionDatabase")]
    public class WordShuffleQuestionDatabase : ScriptableObject
    {
        [SerializeField] private List<WordShuffleQuestionEntry> questions = new List<WordShuffleQuestionEntry>();

        public IReadOnlyList<WordShuffleQuestionEntry> Questions => questions;

        public List<WordShuffleQuestionEntry> GetValidEntries(int minAnswerLength, int maxAnswerLength)
        {
            minAnswerLength = Mathf.Max(1, minAnswerLength);
            maxAnswerLength = Mathf.Max(minAnswerLength, maxAnswerLength);

            return questions
                .Where(entry => entry != null)
                .Where(entry =>
                {
                    string cleanAnswer = entry.CleanAnswer();
                    return !string.IsNullOrWhiteSpace(entry.Question) &&
                           cleanAnswer.Length >= minAnswerLength &&
                           cleanAnswer.Length <= maxAnswerLength;
                })
                .GroupBy(entry => entry.Question.Trim() + "|" + entry.CleanAnswer())
                .Select(group => group.First())
                .ToList();
        }

#if UNITY_EDITOR
        public void EditorSetQuestions(IEnumerable<WordShuffleQuestionEntry> defaultQuestions)
        {
            questions.Clear();

            foreach (WordShuffleQuestionEntry item in defaultQuestions)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Question) || string.IsNullOrWhiteSpace(item.Answer))
                    continue;

                string cleanAnswer = item.CleanAnswer();
                if (string.IsNullOrEmpty(cleanAnswer))
                    continue;

                questions.Add(new WordShuffleQuestionEntry
                {
                    Question = item.Question.Trim(),
                    Answer = cleanAnswer,
                    Hint = item.Hint ?? string.Empty,
                    Picture = item.Picture,
                    VoiceOver = item.VoiceOver
                });
            }
        }
#endif
    }
}
