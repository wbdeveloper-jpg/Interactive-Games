using System.Collections.Generic;
using UnityEngine;

namespace GuessWhoIAm
{
    [System.Serializable]
    public class GuessWhoQuestionData
    {
        [Header("Identity")]
        public string questionId;
        public string answer;

        [Header("Clues")]
        [TextArea(2, 4)] public string clue1;
        [TextArea(2, 4)] public string clue2;
        [TextArea(2, 4)] public string clue3;

        [Header("Options")]
        public List<string> manualWrongOptions = new List<string>();

        [Header("Optional Audio")]
        public AudioClip correctAudio;
        public AudioClip wrongAudio;
        public AudioClip revealAudio;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(answer)
                && !string.IsNullOrWhiteSpace(clue1)
                && !string.IsNullOrWhiteSpace(clue2)
                && !string.IsNullOrWhiteSpace(clue3);
        }

        public string GetClue(int zeroBasedIndex)
        {
            switch (zeroBasedIndex)
            {
                case 0: return clue1;
                case 1: return clue2;
                case 2: return clue3;
                default: return string.Empty;
            }
        }
    }
}
