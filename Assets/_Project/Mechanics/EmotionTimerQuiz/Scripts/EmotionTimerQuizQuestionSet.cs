using System.Collections.Generic;
using UnityEngine;

namespace EmotionTimerQuiz
{
    [CreateAssetMenu(fileName = "EmotionTimerQuizQuestionSet", menuName = "Emotion Timer Quiz/Question Set")]
    public class EmotionTimerQuizQuestionSet : ScriptableObject
    {
        public List<SituationQuestion> questions = new List<SituationQuestion>();
    }
}
