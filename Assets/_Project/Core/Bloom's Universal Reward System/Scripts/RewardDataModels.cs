/*
 * ============================================================
 * RewardDataModels.cs  —  Plain C# Data Classes (no MonoBehaviour)
 * ============================================================
 * PURPOSE:
 *   Defines all data structures passed between game scenes
 *   and the RewardManager. These are lightweight plain classes
 *   with zero Unity overhead — safe to create anywhere, anytime.
 *
 * HOW GAME SCENES USE THIS:
 *
 *   // 1. Define which skills this game trains, with their max scores
 *   List<SkillEntry> skills = new List<SkillEntry>
 *   {
 *       new SkillEntry(BloomSkillType.Remember,   maxScore: 100),
 *       new SkillEntry(BloomSkillType.Understand, maxScore: 50),
 *   };
 *
 *   // 2. Show pre-game panel
 *   RewardManager.Instance.ShowPreGame(skills);
 *
 *   // 3. On game over, build evaluation data and show post-game
 *   GameEvaluationData eval = new GameEvaluationData
 *   {
 *       timeScore     = 0.8f,      // 0-1, pre-calculated by game
 *       accuracyScore = 0.6f,      // 0-1, e.g. 60% correct
 *       mistakeCount  = 3,
 *       timeTaken     = 45.2f      // raw seconds, for display only
 *   };
 *   RewardManager.Instance.ShowPostGame(skills, eval);
 *
 * ============================================================
 */

using System.Collections.Generic;

namespace RewardSystem
{
    /// <summary>
    /// Represents one Bloom skill a game scene trains,
    /// along with the maximum score achievable for that skill in this game.
    /// </summary>
    public class SkillEntry
    {
        /// <summary>Which Bloom skill this entry represents.</summary>
        public BloomSkillType skillType;

        /// <summary>
        /// The maximum raw score this skill can earn in this specific game.
        /// Example: Remember = 100, Understand = 50 means Remember is twice as important.
        /// </summary>
        public float maxScore;

        /// <summary>
        /// How much TIME performance influences this skill's score (0-1).
        /// Set to -1 to fall back to RewardManager global default.
        /// Example: Remember cares less about speed → 0.2
        /// </summary>
        public float timeWeight = -1f;

        /// <summary>
        /// How much ACCURACY influences this skill's score (0-1).
        /// Set to -1 to fall back to RewardManager global default.
        /// Example: Remember cares more about correctness → 0.8
        /// </summary>
        public float accuracyWeight = -1f;

        /// <summary>Basic constructor — uses RewardManager global weights.</summary>
        public SkillEntry(BloomSkillType type, float maxScore)
        {
            this.skillType = type;
            this.maxScore = maxScore;
        }

        /// <summary>
        /// Full constructor with per-skill metric weights.
        /// Weights auto-normalize so they don't need to sum to 1.
        /// </summary>
        public SkillEntry(BloomSkillType type, float maxScore, float timeWeight, float accuracyWeight)
        {
            this.skillType = type;
            this.maxScore = maxScore;
            this.timeWeight = timeWeight;
            this.accuracyWeight = accuracyWeight;
        }
    }

    /// <summary>
    /// Evaluation metrics collected by the game scene at game-over time.
    /// The game scene pre-calculates normalized 0-1 floats so the
    /// RewardManager stays generic across all game types.
    /// </summary>
    public class GameEvaluationData
    {
        /// <summary>
        /// Normalized time performance. 1.0 = perfect speed, 0.0 = very slow.
        /// Game scene calculates this based on its own expected time range.
        /// </summary>
        public float timeScore = 1f;

        /// <summary>
        /// Normalized accuracy. 1.0 = 100% correct, 0.0 = nothing correct.
        /// Example: 60% correct → 0.6f
        /// </summary>
        public float accuracyScore = 1f;

        /// <summary>Raw mistake count — used for display purposes only.</summary>
        public int mistakeCount = 0;

        /// <summary>Raw time in seconds — used for display purposes only.</summary>
        public float timeTaken = 0f;
    }

    /// <summary>
    /// Internal result calculated by RewardManager for one skill.
    /// Passed to post-game UI cards.
    /// </summary>
    public class SkillResult
    {
        public BloomSkillType skillType;
        public float finalScore;      // 0 - maxScore range
        public float normalizedScore; // 0.0 - 1.0 for medal/star logic
        public float maxScore;        // stored here so UI card can display e.g. "80/100"
        public int starCount;       // 1, 2, or 3
        public MedalTier medal;
    }

    public enum MedalTier { Bronze, Silver, Gold }
}