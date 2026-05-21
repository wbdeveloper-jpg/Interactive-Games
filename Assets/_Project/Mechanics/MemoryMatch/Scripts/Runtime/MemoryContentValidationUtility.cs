using System.Collections.Generic;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public static class MemoryContentValidationUtility
    {
        private const int RecommendedMaxCardTextLength = 24;

        public static void ValidateActivity(MemoryActivityConfig config)
        {
            if (config == null || config.Pairs == null)
            {
                return;
            }

            HashSet<string> usedPairIds = new HashSet<string>();

            for (int i = 0; i < config.Pairs.Count; i++)
            {
                MemoryPairDefinition pair = config.Pairs[i];

                if (pair == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(pair.PairId))
                {
                    Debug.LogWarning(
                        $"Memory activity '{config.name}' has a pair with empty Pair Id at index {i}.",
                        config);
                    continue;
                }

                if (!usedPairIds.Add(pair.PairId))
                {
                    Debug.LogWarning(
                        $"Memory activity '{config.name}' has duplicate Pair Id '{pair.PairId}'.",
                        config);
                }

                WarnIfLong(config, pair.PairId, "Card A", pair.CardAText);
                WarnIfLong(config, pair.PairId, "Card B", pair.CardBText);

                if (string.IsNullOrWhiteSpace(pair.LearningText))
                {
                    Debug.LogWarning(
                        $"Memory activity '{config.name}' pair '{pair.PairId}' has no learning popup text. Phase 4 can still run, but learning value is weak.",
                        config);
                }
            }

            int capacity = config.GridSlotCapacity;
            int playablePairs = config.GetPlayablePairs(capacity).Count;

            if (capacity < 2)
            {
                Debug.LogWarning(
                    $"Memory activity '{config.name}' grid has only {capacity} slot. Memory Match needs at least 2 slots.",
                    config);
            }
            else if (playablePairs < config.MaxPairsAllowedByGrid)
            {
                Debug.LogWarning(
                    $"Memory activity '{config.name}' grid can fit {config.MaxPairsAllowedByGrid} pairs, but only {playablePairs} valid pairs are available.",
                    config);
            }
        }

        private static void WarnIfLong(MemoryActivityConfig config, string pairId, string cardName, string text)
        {
            if (!string.IsNullOrWhiteSpace(text) && text.Length > RecommendedMaxCardTextLength)
            {
                Debug.LogWarning(
                    $"Memory activity '{config.name}' pair '{pairId}' {cardName} text is long ({text.Length} chars). Keep cards short; use popup for explanation.",
                    config);
            }
        }
    }
}
