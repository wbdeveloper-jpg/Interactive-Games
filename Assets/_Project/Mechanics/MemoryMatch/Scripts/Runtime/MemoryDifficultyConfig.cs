using UnityEngine;

namespace NGEducation.MemoryMatch
{
    [CreateAssetMenu(
        fileName = "MemoryDifficultyConfig",
        menuName = "NG Education/Memory Match/Difficulty Config")]
    public sealed class MemoryDifficultyConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string difficultyId = "class4_normal";
        [SerializeField] private string displayName = "Class 4 Normal";
        [SerializeField] private int classLevel = 4;

        [Header("Board Layout")]
        [Min(1)]
        [SerializeField] private int gridColumns = 4;

        [Min(1)]
        [SerializeField] private int gridRows = 3;

        [Tooltip("Width / Height. 1 = square, 0.75 = portrait/taller, 1.15 = wider.")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float cardAspectRatio = 0.9f;

        [Header("Match Timing")]
        [SerializeField, Min(0f)] private float matchCheckDelay = 0.25f;
        [SerializeField, Min(0f)] private float wrongFlipBackDelay = 0.65f;

        [Header("Learning Popup Timing")]
        [SerializeField] private bool enablePopupAutoContinue = true;
        [SerializeField, Min(0f)] private float delayAfterNarrationBeforeAutoContinue = 1.25f;
        [SerializeField, Min(0f)] private float noAudioAutoContinueDelay = 2.5f;

        [Header("Countdown Timer")]
        [SerializeField] private bool timerEnabled = true;

        [Tooltip("Total countdown time in seconds.")]
        [SerializeField, Min(5f)] private float countdownSeconds = 120f;

        [Tooltip("0.15 means warning starts when 15% time remains.")]
        [SerializeField, Range(0.01f, 0.75f)] private float warningRemainingPercent = 0.15f;

        [Tooltip("If true, timer pauses while learning popup is open.")]
        [SerializeField] private bool pauseTimerDuringLearningPopup = true;

        [Header("Timer UI Visibility")]
        [SerializeField] private bool showTimerText = true;
        [SerializeField] private bool showTimerBackground = true;
        [SerializeField] private bool showClockIcon = true;

        [Header("Timer Warning Feedback")]
        [SerializeField] private bool pulseTimerOnWarning = true;
        [SerializeField] private bool playTickingSoundOnWarning = true;



        [Header("Hints")]
        [SerializeField] private bool hintsEnabled = true;

        [Tooltip("Maximum hints available for the activity.")]
        [SerializeField, Min(0)] private int maxHints = 3;

        [Tooltip("How long a hinted pair stays revealed/highlighted.")]
        [SerializeField, Min(0.25f)] private float hintRevealDuration = 1.5f;

        [Tooltip("If true, timer pauses while the hint reveal is active.")]
        [SerializeField] private bool pauseTimerDuringHintReveal = true;

        [Header("Hint UI Visibility")]
        [SerializeField] private bool showHintButton = true;
        [SerializeField] private bool showHintsRemainingText = true;
        [SerializeField] private bool showHintBackground = true;
        [SerializeField] private bool showHintIcon = true;

        public string DifficultyId => difficultyId;
        public string DisplayName => displayName;
        public int ClassLevel => classLevel;

        public int GridColumns => Mathf.Max(1, gridColumns);
        public int GridRows => Mathf.Max(1, gridRows);
        public float CardAspectRatio => Mathf.Clamp(cardAspectRatio, 0.5f, 1.5f);

        public float MatchCheckDelay => Mathf.Max(0f, matchCheckDelay);
        public float WrongFlipBackDelay => Mathf.Max(0f, wrongFlipBackDelay);

        public bool EnablePopupAutoContinue => enablePopupAutoContinue;
        public float DelayAfterNarrationBeforeAutoContinue => Mathf.Max(0f, delayAfterNarrationBeforeAutoContinue);
        public float NoAudioAutoContinueDelay => Mathf.Max(0f, noAudioAutoContinueDelay);

        public bool TimerEnabled => timerEnabled;
        public float CountdownSeconds => Mathf.Max(5f, countdownSeconds);
        public float WarningRemainingPercent => Mathf.Clamp(warningRemainingPercent, 0.01f, 0.75f);
        public bool PauseTimerDuringLearningPopup => pauseTimerDuringLearningPopup;

        public bool ShowTimerText => showTimerText;
        public bool ShowTimerBackground => showTimerBackground;
        public bool ShowClockIcon => showClockIcon;

        public bool PulseTimerOnWarning => pulseTimerOnWarning;
        public bool PlayTickingSoundOnWarning => playTickingSoundOnWarning;

        public bool HintsEnabled => hintsEnabled;
        public int MaxHints => Mathf.Max(0, maxHints);
        public float HintRevealDuration => Mathf.Max(0.25f, hintRevealDuration);
        public bool PauseTimerDuringHintReveal => pauseTimerDuringHintReveal;

        public bool ShowHintButton => showHintButton;
        public bool ShowHintsRemainingText => showHintsRemainingText;
        public bool ShowHintBackground => showHintBackground;
        public bool ShowHintIcon => showHintIcon;


#if UNITY_EDITOR
        private void OnValidate()
        {
            gridColumns = Mathf.Max(1, gridColumns);
            gridRows = Mathf.Max(1, gridRows);
            cardAspectRatio = Mathf.Clamp(cardAspectRatio, 0.5f, 1.5f);
            matchCheckDelay = Mathf.Max(0f, matchCheckDelay);
            wrongFlipBackDelay = Mathf.Max(0f, wrongFlipBackDelay);
            delayAfterNarrationBeforeAutoContinue = Mathf.Max(0f, delayAfterNarrationBeforeAutoContinue);
            noAudioAutoContinueDelay = Mathf.Max(0f, noAudioAutoContinueDelay);
            maxHints = Mathf.Max(0, maxHints);
            hintRevealDuration = Mathf.Max(0.25f, hintRevealDuration);
            countdownSeconds = Mathf.Max(5f, countdownSeconds);
            warningRemainingPercent = Mathf.Clamp(warningRemainingPercent, 0.01f, 0.75f);
        }
#endif
    }
}
