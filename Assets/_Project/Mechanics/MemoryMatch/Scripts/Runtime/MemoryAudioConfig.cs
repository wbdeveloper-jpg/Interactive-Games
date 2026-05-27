using UnityEngine;

namespace NGEducation.MemoryMatch
{
    [CreateAssetMenu(
        fileName = "MemoryAudioConfig",
        menuName = "NG Education/Memory Match/Audio Config")]
    public sealed class MemoryAudioConfig : ScriptableObject
    {
        [Header("Master Volumes")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float gameplayVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float scoreVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float timerVolume = 0.75f;
        [SerializeField, Range(0f, 1f)] private float backgroundVolume = 0.22f;
        [SerializeField, Range(0f, 1f)] private float duckedBackgroundVolume = 0.08f;
        [SerializeField, Min(0f)] private float backgroundFadeDuration = 0.25f;

        [Header("Background")]
        [SerializeField] private AudioClip backgroundLoop;

        [Header("UI")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip popupOpen;
        [SerializeField] private AudioClip activityStart;
        [SerializeField] private AudioClip pause;
        [SerializeField] private AudioClip resume;

        [Header("Card Gameplay")]
        [SerializeField] private AudioClip cardFlip;
        [SerializeField] private AudioClip correctMatch;
        [SerializeField] private AudioClip wrongMatch;
        [SerializeField] private AudioClip hintUsed;

        [Header("Score Feedback")]
        [SerializeField] private AudioClip scorePositive;
        [SerializeField] private AudioClip scoreNegative;

        [Header("Timer")]
        [SerializeField] private AudioClip warningStart;
        [SerializeField] private AudioClip timerTickingLoop;
        [SerializeField] private AudioClip timeUp;

        [Header("Summary")]
        [SerializeField] private AudioClip summarySuccess;
        [SerializeField] private AudioClip summaryTimeUp;

        public float MasterVolume => masterVolume;
        public float UiVolume => uiVolume;
        public float GameplayVolume => gameplayVolume;
        public float ScoreVolume => scoreVolume;
        public float TimerVolume => timerVolume;
        public float BackgroundVolume => backgroundVolume;
        public float DuckedBackgroundVolume => duckedBackgroundVolume;
        public float BackgroundFadeDuration => Mathf.Max(0f, backgroundFadeDuration);

        public AudioClip BackgroundLoop => backgroundLoop;

        public AudioClip ButtonClick => buttonClick;
        public AudioClip PopupOpen => popupOpen;
        public AudioClip ActivityStart => activityStart;
        public AudioClip Pause => pause;
        public AudioClip Resume => resume;

        public AudioClip CardFlip => cardFlip;
        public AudioClip CorrectMatch => correctMatch;
        public AudioClip WrongMatch => wrongMatch;
        public AudioClip HintUsed => hintUsed;

        public AudioClip ScorePositive => scorePositive;
        public AudioClip ScoreNegative => scoreNegative;

        public AudioClip WarningStart => warningStart;
        public AudioClip TimerTickingLoop => timerTickingLoop;
        public AudioClip TimeUp => timeUp;

        public AudioClip SummarySuccess => summarySuccess;
        public AudioClip SummaryTimeUp => summaryTimeUp;
    }
}
