namespace NGEducation.MemoryMatch
{
    public readonly struct MemoryActivitySummaryResult
    {
        public readonly string ActivityId;
        public readonly bool Completed;
        public readonly bool TimeUp;

        public readonly int TotalPairs;
        public readonly int MatchedPairs;
        public readonly int PairAttempts;
        public readonly int WrongAttempts;
        public readonly int CardClicks;
        public readonly int HintsUsed;
        public readonly int ActivityPoints;

        public readonly float TotalTimeSeconds;
        public readonly float TimeRemainingSeconds;
        public readonly float TimeUsedSeconds;
        public readonly float AccuracyPercent;

        public MemoryActivitySummaryResult(
            string activityId,
            bool completed,
            bool timeUp,
            int totalPairs,
            int matchedPairs,
            int pairAttempts,
            int wrongAttempts,
            int cardClicks,
            int hintsUsed,
            int activityPoints,
            float totalTimeSeconds,
            float timeRemainingSeconds,
            float accuracyPercent)
        {
            ActivityId = activityId;
            Completed = completed;
            TimeUp = timeUp;
            TotalPairs = totalPairs;
            MatchedPairs = matchedPairs;
            PairAttempts = pairAttempts;
            WrongAttempts = wrongAttempts;
            CardClicks = cardClicks;
            HintsUsed = hintsUsed;
            ActivityPoints = activityPoints;
            TotalTimeSeconds = totalTimeSeconds;
            TimeRemainingSeconds = timeRemainingSeconds;
            TimeUsedSeconds = totalTimeSeconds > 0f
                ? totalTimeSeconds - timeRemainingSeconds
                : 0f;
            AccuracyPercent = accuracyPercent;
        }
    }
}
