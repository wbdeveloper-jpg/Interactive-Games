using System;
using System.Collections.Generic;
using UnityEngine;

namespace NarayanaGames.SpellBotRescue
{
    [Serializable]
    public class SpellBotWordEntry
    {
        [Tooltip("Wrong spelling shown to the child.")]
        public string incorrectWord;

        [Tooltip("Correct answer used for validation.")]
        public string correctWord;

        [TextArea(2, 4)]
        [Tooltip("Hint shown after a wrong attempt.")]
        public string hintText;

        [Range(3, 5)]
        [Tooltip("3, 4, or 5. Use this to filter by class/grade level.")]
        public int difficultyTier = 3;
    }

    [CreateAssetMenu(fileName = "SpellBotWordDatabase", menuName = "Spell Bot Rescue/Word Database")]
    public class SpellBotWordDatabase : ScriptableObject
    {
        [Tooltip("Master spelling database. Add at least 50 entries for production.")]
        public List<SpellBotWordEntry> entries = new List<SpellBotWordEntry>();
    }
}
