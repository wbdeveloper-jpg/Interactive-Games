/*
 * ============================================================
 * ScoreCalculator.cs  —  Static Utility (no MonoBehaviour)
 * ============================================================
 * PURPOSE:
 *   Pure calculation logic. Takes raw GameEvaluationData and
 *   a SkillEntry, returns a fully populated SkillResult.
 *   Completely decoupled from UI — easy to unit test or swap.
 *
 * FORMULA:
 *   combinedPerformance = (timeScore * timeWeight) + (accuracyScore * accuracyWeight)
 *   finalScore          = combinedPerformance * skill.maxScore
 *   normalizedScore     = finalScore / skill.maxScore  →  0.0 to 1.0
 *
 * WEIGHTS & THRESHOLDS:
 *   All tunable from RewardManager inspector — passed in as params.
 *   Default weights: time = 0.4, accuracy = 0.6
 *   Default thresholds: bronze = 0, silver = 0.4, gold = 0.7
 *
 * STAR LOGIC:
 *   normalizedScore >= 0.75  →  3 stars
 *   normalizedScore >= 0.45  →  2 stars
 *   else                     →  1 star
 * ============================================================
 */

using UnityEngine;

namespace RewardSystem
{
    public static class ScoreCalculator
    {
        /// <summary>
        /// Calculates the full SkillResult for one skill given evaluation data.
        /// </summary>
        /// <param name="skill">The skill entry with its maxScore weighting.</param>
        /// <param name="eval">Raw evaluation data from the game scene.</param>
        /// <param name="timeWeight">How much time performance contributes (0-1).</param>
        /// <param name="accuracyWeight">How much accuracy contributes (0-1).</param>
        /// <param name="silverThreshold">Normalized score needed for silver (0-1).</param>
        /// <param name="goldThreshold">Normalized score needed for gold (0-1).</param>
        public static SkillResult Calculate(
            SkillEntry         skill,
            GameEvaluationData eval,
            float              timeWeight,
            float              accuracyWeight,
            float              silverThreshold,
            float              goldThreshold)
        {
            // Clamp inputs to valid range
            float tScore = Mathf.Clamp01(eval.timeScore);
            float aScore = Mathf.Clamp01(eval.accuracyScore);

            // Normalize weights so they always sum to 1 even if inspector values don't
            float totalWeight = timeWeight + accuracyWeight;
            if (totalWeight <= 0f) totalWeight = 1f; // safety guard
            float tW = timeWeight     / totalWeight;
            float aW = accuracyWeight / totalWeight;

            // Combined 0-1 performance score
            float combined = (tScore * tW) + (aScore * aW);

            // Final score in the skill's own range
            float finalScore = combined * skill.maxScore;

            // Normalized always 0-1 regardless of maxScore
            float normalized = skill.maxScore > 0f ? finalScore / skill.maxScore : 0f;

            return new SkillResult
            {
                skillType       = skill.skillType,
                finalScore      = finalScore,
                normalizedScore = normalized,
                starCount       = GetStarCount(normalized),
                medal           = GetMedal(normalized, silverThreshold, goldThreshold)
            };
        }

        // --- Private helpers ---

        private static int GetStarCount(float normalized)
        {
            if (normalized >= 0.75f) return 3;
            if (normalized >= 0.45f) return 2;
            return 1;
        }

        private static MedalTier GetMedal(float normalized, float silver, float gold)
        {
            if (normalized >= gold)   return MedalTier.Gold;
            if (normalized >= silver) return MedalTier.Silver;
            return MedalTier.Bronze;
        }
    }
}
