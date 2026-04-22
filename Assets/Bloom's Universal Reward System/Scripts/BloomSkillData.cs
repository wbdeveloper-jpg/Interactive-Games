/*
 * ============================================================
 * BloomSkillData.cs  —  ScriptableObject
 * ============================================================
 * PURPOSE:
 *   Stores all static data for a single Bloom's Taxonomy skill.
 *   Create one asset per skill (6 total) via the Unity Editor:
 *   Right-click in Project → Create → RewardSystem → BloomSkillData
 *
 * SETUP:
 *   Fill in each field in the Inspector:
 *     • skillType   — pick the enum value matching this skill
 *     • skillName   — display name e.g. "Remember"
 *     • icon        — the Sprite shown on all Bloom cards
 *     • definition  — one-liner definition shown in Info Panel
 *     • details     — longer description shown in Info Panel
 *
 * USAGE:
 *   Referenced by RewardManager's inspector list.
 *   At runtime, manager looks up skills by BloomSkillType enum.
 * ============================================================
 */

using UnityEngine;

namespace RewardSystem
{
    // The 6 levels of Bloom's Taxonomy
    public enum BloomSkillType
    {
        Remember,
        Understand,
        Apply,
        Analyze,
        Evaluate,
        Create
    }

    [CreateAssetMenu(fileName = "BloomSkillData", menuName = "RewardSystem/BloomSkillData")]
    public class BloomSkillData : ScriptableObject
    {
        [Header("Identity")]
        public BloomSkillType skillType;
        public string skillName;

        [Header("Visuals")]
        public Sprite icon;

        [Header("Info Panel Content")]
        [TextArea(2, 4)]
        public string definition;

        [TextArea(4, 8)]
        public string details;
    }
}
