using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace NGEducation.MemoryMatch
{
    [Serializable]
    public sealed class MemoryPairDefinition
    {
        [Header("Identity")]
        [FormerlySerializedAs("id")]
        [FormerlySerializedAs("pairID")]
        [SerializeField] private string pairId = string.Empty;

        [Header("Card A")]
        [SerializeField] private MemoryCardContentType cardAContentType = MemoryCardContentType.Text;

        [FormerlySerializedAs("cardA")]
        [FormerlySerializedAs("firstCardText")]
        [FormerlySerializedAs("leftCardText")]
        [SerializeField] private string cardAText = string.Empty;

        [FormerlySerializedAs("cardASprite")]
        [FormerlySerializedAs("firstCardSprite")]
        [SerializeField] private Sprite cardAImage;

        [Header("Card B")]
        [SerializeField] private MemoryCardContentType cardBContentType = MemoryCardContentType.Text;

        [FormerlySerializedAs("cardB")]
        [FormerlySerializedAs("secondCardText")]
        [FormerlySerializedAs("rightCardText")]
        [SerializeField] private string cardBText = string.Empty;

        [FormerlySerializedAs("cardBSprite")]
        [FormerlySerializedAs("secondCardSprite")]
        [SerializeField] private Sprite cardBImage;

        [Header("Learning Popup - Phase 4")]
        [FormerlySerializedAs("popupTitle")]
        [SerializeField] private string learningTitle = "Correct!";

        [FormerlySerializedAs("popupText")]
        [FormerlySerializedAs("learningExplanation")]
        [FormerlySerializedAs("learningPopupText")]
        [TextArea(2, 5)]
        [SerializeField] private string learningText = string.Empty;

        [SerializeField] private Sprite learningImage;
        [SerializeField] private AudioClip narrationAudio;

        public string PairId => pairId;
        public MemoryCardContentType CardAContentType => cardAContentType;
        public string CardAText => cardAText;
        public Sprite CardAImage => cardAImage;
        public MemoryCardContentType CardBContentType => cardBContentType;
        public string CardBText => cardBText;
        public Sprite CardBImage => cardBImage;
        public string LearningTitle => learningTitle;
        public string LearningText => learningText;
        public Sprite LearningImage => learningImage;
        public AudioClip NarrationAudio => narrationAudio;

        public bool HasLearningContent =>
            !string.IsNullOrWhiteSpace(learningTitle) ||
            !string.IsNullOrWhiteSpace(learningText) ||
            learningImage != null ||
            narrationAudio != null;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(pairId))
            {
                return false;
            }

            return HasCardContent(cardAContentType, cardAText, cardAImage)
                   && HasCardContent(cardBContentType, cardBText, cardBImage);
        }

        public MemoryCardRuntimeData CreateCardA()
        {
            return new MemoryCardRuntimeData(
                pairId,
                $"{pairId}_A",
                cardAContentType,
                cardAText,
                cardAImage,
                string.IsNullOrWhiteSpace(cardAText) ? pairId : cardAText);
        }

        public MemoryCardRuntimeData CreateCardB()
        {
            return new MemoryCardRuntimeData(
                pairId,
                $"{pairId}_B",
                cardBContentType,
                cardBText,
                cardBImage,
                string.IsNullOrWhiteSpace(cardBText) ? pairId : cardBText);
        }

        private static bool HasCardContent(MemoryCardContentType type, string text, Sprite image)
        {
            switch (type)
            {
                case MemoryCardContentType.Text:
                    return !string.IsNullOrWhiteSpace(text);

                case MemoryCardContentType.Image:
                    return image != null;

                case MemoryCardContentType.ImageWithLabel:
                    return image != null || !string.IsNullOrWhiteSpace(text);

                default:
                    return false;
            }
        }
    }
}
