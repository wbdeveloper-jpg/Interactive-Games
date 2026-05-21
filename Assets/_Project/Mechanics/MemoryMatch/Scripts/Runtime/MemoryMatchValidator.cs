namespace NGEducation.MemoryMatch
{
    public static class MemoryMatchValidator
    {
        public static bool IsCorrectMatch(MemoryCardView firstCard, MemoryCardView secondCard)
        {
            if (firstCard == null || secondCard == null)
            {
                return false;
            }

            if (ReferenceEquals(firstCard, secondCard))
            {
                return false;
            }

            if (firstCard.Data == null || secondCard.Data == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(firstCard.Data.PairId) ||
                string.IsNullOrWhiteSpace(secondCard.Data.PairId))
            {
                return false;
            }

            if (firstCard.Data.CardId == secondCard.Data.CardId)
            {
                return false;
            }

            return firstCard.Data.PairId == secondCard.Data.PairId;
        }
    }
}
