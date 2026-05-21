using System;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    [Serializable]
    public sealed class MemoryCardRuntimeData
    {
        public string PairId;
        public string CardId;
        public MemoryCardContentType ContentType;
        public string DisplayText;
        public Sprite DisplaySprite;
        public string AccessibilityLabel;

        public MemoryCardRuntimeData(
            string pairId,
            string cardId,
            MemoryCardContentType contentType,
            string displayText,
            Sprite displaySprite,
            string accessibilityLabel)
        {
            PairId = pairId;
            CardId = cardId;
            ContentType = contentType;
            DisplayText = displayText;
            DisplaySprite = displaySprite;
            AccessibilityLabel = accessibilityLabel;
        }
    }
}
