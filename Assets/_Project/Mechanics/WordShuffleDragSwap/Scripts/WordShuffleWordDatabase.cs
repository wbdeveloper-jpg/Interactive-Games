using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WordShuffleDragSwap
{
    [Serializable]
    public class WordShuffleWordEntry
    {
        [Tooltip("The correct word. Use simple letters only for best results.")]
        public string Word;

        [Tooltip("Optional short hint shown during the round.")]
        public string Hint;

        [Tooltip("Optional image for themed rounds.")]
        public Sprite Picture;

        [Tooltip("Optional voice/audio for this word.")]
        public AudioClip VoiceOver;

        public string CleanWord(bool upperCase = true)
        {
            if (string.IsNullOrWhiteSpace(Word))
                return string.Empty;

            string cleaned = new string(Word.Trim().Where(char.IsLetter).ToArray());
            return upperCase ? cleaned.ToUpperInvariant() : cleaned;
        }
    }

    [CreateAssetMenu(menuName = "Word Shuffle Drag Swap/Word Database", fileName = "WordShuffleWordDatabase")]
    public class WordShuffleWordDatabase : ScriptableObject
    {
        [SerializeField] private List<WordShuffleWordEntry> words = new List<WordShuffleWordEntry>();

        public IReadOnlyList<WordShuffleWordEntry> Words => words;

        public List<WordShuffleWordEntry> GetValidEntries(int minLength, int maxLength)
        {
            minLength = Mathf.Max(1, minLength);
            maxLength = Mathf.Max(minLength, maxLength);

            return words
                .Where(entry => entry != null)
                .Where(entry =>
                {
                    string cleanWord = entry.CleanWord();
                    return cleanWord.Length >= minLength && cleanWord.Length <= maxLength;
                })
                .GroupBy(entry => entry.CleanWord())
                .Select(group => group.First())
                .ToList();
        }

#if UNITY_EDITOR
        public void EditorSetWords(IEnumerable<string> defaultWords)
        {
            words.Clear();

            foreach (string word in defaultWords)
            {
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                string cleanWord = new string(word.Trim().Where(char.IsLetter).ToArray()).ToLowerInvariant();
                if (string.IsNullOrEmpty(cleanWord))
                    continue;

                if (words.Any(entry => string.Equals(entry.Word, cleanWord, StringComparison.OrdinalIgnoreCase)))
                    continue;

                words.Add(new WordShuffleWordEntry
                {
                    Word = cleanWord,
                    Hint = string.Empty,
                    Picture = null,
                    VoiceOver = null
                });
            }
        }
#endif
    }
}
