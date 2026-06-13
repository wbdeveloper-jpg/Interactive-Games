using UnityEngine;

namespace DictationGame
{
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard
    }

    [CreateAssetMenu(fileName = "NewDictationQuestion", menuName = "DictationGame/Question")]
    public sealed class DictationRoundData : ScriptableObject
    {
        [Header("Question Info")]
        [SerializeField] private string roundTitle = "Question";
        [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Easy;

        [Header("Audio")]
        [SerializeField] private AudioClip audioClip;

        [Header("Answer")]
        [TextArea(2, 4)]
        [SerializeField] private string answerSentence = "";

        public string RoundTitle => string.IsNullOrWhiteSpace(roundTitle) ? name : roundTitle.Trim();
        public DifficultyLevel Difficulty => difficulty;
        public AudioClip AudioClip => audioClip;
        public string AnswerSentence => answerSentence == null ? string.Empty : answerSentence.Trim();
        public bool HasValidAnswer => !string.IsNullOrWhiteSpace(AnswerSentence);
        public bool HasAudio => audioClip != null;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(roundTitle))
                roundTitle = name;
        }
#endif
    }
}
