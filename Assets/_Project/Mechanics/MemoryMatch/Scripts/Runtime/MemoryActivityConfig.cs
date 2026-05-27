using System.Collections.Generic;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    [CreateAssetMenu(
        fileName = "MemoryActivityConfig",
        menuName = "NG Education/Memory Match/Activity Config")]
    public sealed class MemoryActivityConfig : ScriptableObject
    {
        [Header("Activity Identity")]
        [SerializeField] private string activityId = string.Empty;
        [SerializeField] private string activityTitle = "Memory Match";
        [TextArea(1, 3)]
        [SerializeField] private string instructionText = "Match the correct cards.";

        [Header("Learning Context")]
        [SerializeField] private int classLevel = 4;
        [SerializeField] private string subject = string.Empty;
        [SerializeField] private string chapterName = string.Empty;

        [Header("Pause Overlay Content")]
        [SerializeField] private string pauseTitle = "Paused";

        [TextArea(2, 5)]
        [SerializeField] private string pauseBodyText = string.Empty;

        [Tooltip("If pause body is empty, the game can show one random learning line from playable pair popup text.")]
        [SerializeField] private bool useRandomLearningTextForPauseBody = true;

        [Header("Phase 5 Optional Configs")]
        [SerializeField] private MemoryThemeConfig themeConfig;
        [SerializeField] private MemoryDifficultyConfig difficultyConfig;

        [Header("Board Layout - Fallback If No Difficulty Config")]
        [Min(1)]
        [SerializeField] private int gridColumns = 4;

        [Min(1)]
        [SerializeField] private int gridRows = 3;

        [Tooltip("Width / Height. 1 = square, 0.75 = portrait/taller, 1.15 = wider.")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float cardAspectRatio = 0.9f;

        [Tooltip("0 means use as many valid pairs as the grid can fit.")]
        [Min(0)]
        [SerializeField] private int maxPairsToUse = 0;

        [Header("Pair Selection")]
        [Tooltip("If true and the grid cannot fit every valid pair, a random subset is selected each activity start instead of always taking the first pairs.")]
        [SerializeField] private bool randomizePairSelection = true;

        [Header("Pairs")]
        [SerializeField] private List<MemoryPairDefinition> pairs = new List<MemoryPairDefinition>();

        public string ActivityId => activityId;
        public string ActivityTitle => activityTitle;
        public string InstructionText => instructionText;
        public int ClassLevel => classLevel;
        public string Subject => subject;
        public string ChapterName => chapterName;

        public MemoryThemeConfig ThemeConfig => themeConfig;
        public MemoryDifficultyConfig DifficultyConfig => difficultyConfig;

        public string PauseTitle => string.IsNullOrWhiteSpace(pauseTitle) ? "Paused" : pauseTitle;
        public string PauseBodyText => pauseBodyText;
        public bool UseRandomLearningTextForPauseBody => useRandomLearningTextForPauseBody;

        public int GridColumns => Mathf.Max(1, gridColumns);
        public int GridRows => Mathf.Max(1, gridRows);
        public float CardAspectRatio => Mathf.Clamp(cardAspectRatio, 0.5f, 1.5f);
        public IReadOnlyList<MemoryPairDefinition> Pairs => pairs;
        public bool RandomizePairSelection => randomizePairSelection;

        public int GetEffectiveGridColumns()
        {
            return difficultyConfig != null ? difficultyConfig.GridColumns : GridColumns;
        }

        public int GetEffectiveGridRows()
        {
            return difficultyConfig != null ? difficultyConfig.GridRows : GridRows;
        }

        public float GetEffectiveCardAspectRatio()
        {
            return difficultyConfig != null ? difficultyConfig.CardAspectRatio : CardAspectRatio;
        }

        public int GetEffectiveGridSlotCapacity()
        {
            return GetEffectiveGridColumns() * GetEffectiveGridRows();
        }

        public int GridSlotCapacity => GetEffectiveGridSlotCapacity();
        public int MaxPairsAllowedByGrid => Mathf.FloorToInt(GridSlotCapacity / 2f);

        public List<MemoryPairDefinition> GetPlayablePairsForCurrentGrid()
        {
            return GetPlayablePairs(GridSlotCapacity);
        }

        public List<MemoryPairDefinition> GetPlayablePairs(int gridSlotCapacity)
        {
            int maxPairsByCapacity = Mathf.FloorToInt(Mathf.Max(0, gridSlotCapacity) / 2f);
            int configuredLimit = maxPairsToUse <= 0 ? int.MaxValue : maxPairsToUse;
            int finalLimit = Mathf.Min(maxPairsByCapacity, configuredLimit);

            List<MemoryPairDefinition> validPairs = new List<MemoryPairDefinition>();

            if (finalLimit <= 0 || pairs == null)
            {
                return validPairs;
            }

            for (int i = 0; i < pairs.Count; i++)
            {
                MemoryPairDefinition pair = pairs[i];

                if (pair == null || !pair.IsValid())
                {
                    continue;
                }

                validPairs.Add(pair);
            }

            if (validPairs.Count <= finalLimit)
            {
                return validPairs;
            }

            if (randomizePairSelection)
            {
                Shuffle(validPairs);
            }

            return validPairs.GetRange(0, finalLimit);
        }

        private static void Shuffle<T>(IList<T> list)
        {
            if (list == null || list.Count <= 1)
            {
                return;
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }

        public string GetPauseBodyText(IReadOnlyList<MemoryPairDefinition> playablePairs)
        {
            if (!string.IsNullOrWhiteSpace(pauseBodyText))
            {
                return pauseBodyText;
            }

            if (!useRandomLearningTextForPauseBody || playablePairs == null || playablePairs.Count == 0)
            {
                return "Take a short break. Tap Resume when you are ready.";
            }

            List<string> availableLines = new List<string>();

            for (int i = 0; i < playablePairs.Count; i++)
            {
                MemoryPairDefinition pair = playablePairs[i];

                if (pair != null && !string.IsNullOrWhiteSpace(pair.LearningText))
                {
                    availableLines.Add(pair.LearningText);
                }
            }

            if (availableLines.Count == 0)
            {
                return "Take a short break. Tap Resume when you are ready.";
            }

            int index = Random.Range(0, availableLines.Count);
            return availableLines[index];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            gridColumns = Mathf.Max(1, gridColumns);
            gridRows = Mathf.Max(1, gridRows);
            cardAspectRatio = Mathf.Clamp(cardAspectRatio, 0.5f, 1.5f);
            maxPairsToUse = Mathf.Max(0, maxPairsToUse);

            MemoryContentValidationUtility.ValidateActivity(this);
        }
#endif
    }
}
