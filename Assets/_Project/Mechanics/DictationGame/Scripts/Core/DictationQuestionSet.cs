using System.Collections.Generic;
using UnityEngine;

namespace DictationGame
{
    [CreateAssetMenu(fileName = "NewDictationQuestionSet", menuName = "DictationGame/Question Set")]
    public sealed class DictationQuestionSet : ScriptableObject
    {
        [Header("Question Bank")]
        [SerializeField] private DictationRoundData[] allQuestions;

        [Header("Session Settings")]
        [Min(1)] [SerializeField] private int totalRoundsPerSession = 10;

        [Header("Difficulty Distribution")]
        [Min(0)] [SerializeField] private int easyCount = 4;
        [Min(0)] [SerializeField] private int mediumCount = 3;
        [Min(0)] [SerializeField] private int hardCount = 3;

        public IReadOnlyList<DictationRoundData> AllQuestions => allQuestions;
        public int TotalRoundsPerSession => Mathf.Max(1, totalRoundsPerSession);

        public List<DictationRoundData> BuildSessionList()
        {
            List<DictationRoundData> validQuestions = GetValidQuestions();
            if (validQuestions.Count == 0)
            {
                Debug.LogWarning("[DictationGame] QuestionSet has no valid questions with answers.", this);
                return new List<DictationRoundData>();
            }

            int targetCount = Mathf.Min(TotalRoundsPerSession, validQuestions.Count);

            List<DictationRoundData> easyPool = new List<DictationRoundData>();
            List<DictationRoundData> mediumPool = new List<DictationRoundData>();
            List<DictationRoundData> hardPool = new List<DictationRoundData>();

            for (int i = 0; i < validQuestions.Count; i++)
            {
                DictationRoundData question = validQuestions[i];
                switch (question.Difficulty)
                {
                    case DifficultyLevel.Easy:
                        easyPool.Add(question);
                        break;
                    case DifficultyLevel.Medium:
                        mediumPool.Add(question);
                        break;
                    case DifficultyLevel.Hard:
                        hardPool.Add(question);
                        break;
                }
            }

            Shuffle(easyPool);
            Shuffle(mediumPool);
            Shuffle(hardPool);

            List<DictationRoundData> picked = new List<DictationRoundData>(targetCount);
            AddFromPool(picked, easyPool, easyCount, targetCount);
            AddFromPool(picked, mediumPool, mediumCount, targetCount);
            AddFromPool(picked, hardPool, hardCount, targetCount);

            if (picked.Count < targetCount)
            {
                List<DictationRoundData> leftovers = new List<DictationRoundData>(validQuestions.Count);
                for (int i = 0; i < validQuestions.Count; i++)
                {
                    if (!picked.Contains(validQuestions[i]))
                        leftovers.Add(validQuestions[i]);
                }

                Shuffle(leftovers);
                AddFromPool(picked, leftovers, targetCount - picked.Count, targetCount);
            }

            Shuffle(picked);
            return picked;
        }

        private List<DictationRoundData> GetValidQuestions()
        {
            List<DictationRoundData> result = new List<DictationRoundData>();
            if (allQuestions == null) return result;

            for (int i = 0; i < allQuestions.Length; i++)
            {
                DictationRoundData question = allQuestions[i];
                if (question == null) continue;
                if (!question.HasValidAnswer)
                {
                    Debug.LogWarning($"[DictationGame] Skipping question '{question.name}' because Answer Sentence is empty.", question);
                    continue;
                }

                if (!result.Contains(question))
                    result.Add(question);
            }

            return result;
        }

        private static void AddFromPool(List<DictationRoundData> target, List<DictationRoundData> pool, int count, int maxTotal)
        {
            int safeCount = Mathf.Max(0, count);
            for (int i = 0; i < pool.Count && i < safeCount && target.Count < maxTotal; i++)
            {
                if (!target.Contains(pool[i]))
                    target.Add(pool[i]);
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            totalRoundsPerSession = Mathf.Max(1, totalRoundsPerSession);
            easyCount = Mathf.Max(0, easyCount);
            mediumCount = Mathf.Max(0, mediumCount);
            hardCount = Mathf.Max(0, hardCount);
        }
#endif
    }
}
