using DG.Tweening;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public sealed class MemorySfxAudioManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private MemoryAudioConfig audioConfig;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource gameplaySource;
        [SerializeField] private AudioSource scoreSource;
        [SerializeField] private AudioSource timerSource;
        [SerializeField] private AudioSource backgroundSource;

        private Tween backgroundVolumeTween;
        private bool backgroundDucked;
        private bool backgroundPausedByGamePause;

        private void Awake()
        {
            uiSource = EnsureSource(uiSource, false);
            gameplaySource = EnsureSource(gameplaySource, false);
            scoreSource = EnsureSource(scoreSource, false);
            timerSource = EnsureSource(timerSource, true);
            backgroundSource = EnsureSource(backgroundSource, true);

            ApplySourceDefaults();
        }

        private void OnDestroy()
        {
            KillBackgroundTween();
        }

        public void Configure(MemoryAudioConfig config)
        {
            audioConfig = config;
            StopTimerTickingLoop();
            StopBackgroundLoop();
            ApplySourceDefaults();

            if (backgroundSource != null && audioConfig != null)
            {
                backgroundSource.clip = audioConfig.BackgroundLoop;
                backgroundSource.loop = true;
                backgroundSource.volume = GetBackgroundVolume(false);
            }

            if (timerSource != null && audioConfig != null)
            {
                timerSource.clip = audioConfig.TimerTickingLoop;
                timerSource.loop = true;
                timerSource.volume = GetTimerVolume();
            }
        }

        public void PlayButtonClick() => PlayOneShot(uiSource, audioConfig != null ? audioConfig.ButtonClick : null, GetUiVolume());
        public void PlayPopupOpen() => PlayOneShot(uiSource, audioConfig != null ? audioConfig.PopupOpen : null, GetUiVolume());
        public void PlayActivityStart() => PlayOneShot(uiSource, audioConfig != null ? audioConfig.ActivityStart : null, GetUiVolume());
        public void PlayPause() => PlayOneShot(uiSource, audioConfig != null ? audioConfig.Pause : null, GetUiVolume());
        public void PlayResume() => PlayOneShot(uiSource, audioConfig != null ? audioConfig.Resume : null, GetUiVolume());

        public void PlayCardFlip() => PlayOneShot(gameplaySource, audioConfig != null ? audioConfig.CardFlip : null, GetGameplayVolume());
        public void PlayCorrectMatch() => PlayOneShot(gameplaySource, audioConfig != null ? audioConfig.CorrectMatch : null, GetGameplayVolume());
        public void PlayWrongMatch() => PlayOneShot(gameplaySource, audioConfig != null ? audioConfig.WrongMatch : null, GetGameplayVolume());
        public void PlayHintUsed() => PlayOneShot(gameplaySource, audioConfig != null ? audioConfig.HintUsed : null, GetGameplayVolume());

        public void PlayScorePositive() => PlayOneShot(scoreSource, audioConfig != null ? audioConfig.ScorePositive : null, GetScoreVolume());
        public void PlayScoreNegative() => PlayOneShot(scoreSource, audioConfig != null ? audioConfig.ScoreNegative : null, GetScoreVolume());

        public void PlayWarningStart() => PlayOneShot(timerSource, audioConfig != null ? audioConfig.WarningStart : null, GetTimerVolume());
        public void PlayTimeUp() => PlayOneShot(timerSource, audioConfig != null ? audioConfig.TimeUp : null, GetTimerVolume());

        public void PlaySummarySuccess() => PlayOneShot(uiSource, audioConfig != null ? audioConfig.SummarySuccess : null, GetUiVolume());
        public void PlaySummaryTimeUp() => PlayOneShot(uiSource, audioConfig != null ? audioConfig.SummaryTimeUp : null, GetUiVolume());

        public void StartTimerTickingLoop()
        {
            if (audioConfig == null || audioConfig.TimerTickingLoop == null || timerSource == null)
            {
                return;
            }

            timerSource.clip = audioConfig.TimerTickingLoop;
            timerSource.volume = GetTimerVolume();
            timerSource.loop = true;

            if (!timerSource.isPlaying)
            {
                timerSource.Play();
            }
        }

        public void StopTimerTickingLoop()
        {
            if (timerSource != null && timerSource.isPlaying)
            {
                timerSource.Stop();
            }
        }

        public void StartBackgroundLoop()
        {
            if (audioConfig == null || audioConfig.BackgroundLoop == null || backgroundSource == null)
            {
                return;
            }

            backgroundPausedByGamePause = false;
            backgroundSource.clip = audioConfig.BackgroundLoop;
            backgroundSource.loop = true;
            backgroundSource.volume = GetBackgroundVolume(backgroundDucked);

            if (!backgroundSource.isPlaying)
            {
                backgroundSource.Play();
            }
        }

        public void StopBackgroundLoop()
        {
            backgroundPausedByGamePause = false;
            backgroundDucked = false;
            KillBackgroundTween();

            if (backgroundSource != null && backgroundSource.isPlaying)
            {
                backgroundSource.Stop();
            }
        }

        public void PauseBackgroundLoop()
        {
            if (backgroundSource == null || !backgroundSource.isPlaying)
            {
                return;
            }

            backgroundSource.Pause();
            backgroundPausedByGamePause = true;
        }

        public void ResumeBackgroundLoop()
        {
            if (backgroundSource == null || !backgroundPausedByGamePause)
            {
                return;
            }

            backgroundSource.UnPause();
            backgroundPausedByGamePause = false;
        }

        public void SetBackgroundDucked(bool ducked)
        {
            backgroundDucked = ducked;

            if (backgroundSource == null || audioConfig == null || backgroundSource.clip == null)
            {
                return;
            }

            float targetVolume = GetBackgroundVolume(ducked);
            float duration = audioConfig.BackgroundFadeDuration;

            KillBackgroundTween();

            if (duration <= 0f)
            {
                backgroundSource.volume = targetVolume;
                return;
            }

            backgroundVolumeTween = backgroundSource.DOFade(targetVolume, duration).SetUpdate(true);
        }

        private void ApplySourceDefaults()
        {
            PrepareSource(uiSource, false);
            PrepareSource(gameplaySource, false);
            PrepareSource(scoreSource, false);
            PrepareSource(timerSource, true);
            PrepareSource(backgroundSource, true);
        }

        private AudioSource EnsureSource(AudioSource source, bool loop)
        {
            if (source != null)
            {
                return source;
            }

            AudioSource created = gameObject.AddComponent<AudioSource>();
            created.playOnAwake = false;
            created.loop = loop;
            created.spatialBlend = 0f;
            return created;
        }

        private static void PrepareSource(AudioSource source, bool loop)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
        }

        private static void PlayOneShot(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null || volume <= 0f)
            {
                return;
            }

            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private float GetUiVolume() => audioConfig == null ? 0f : audioConfig.MasterVolume * audioConfig.UiVolume;
        private float GetGameplayVolume() => audioConfig == null ? 0f : audioConfig.MasterVolume * audioConfig.GameplayVolume;
        private float GetScoreVolume() => audioConfig == null ? 0f : audioConfig.MasterVolume * audioConfig.ScoreVolume;
        private float GetTimerVolume() => audioConfig == null ? 0f : audioConfig.MasterVolume * audioConfig.TimerVolume;
        private float GetBackgroundVolume(bool ducked) => audioConfig == null ? 0f : audioConfig.MasterVolume * (ducked ? audioConfig.DuckedBackgroundVolume : audioConfig.BackgroundVolume);

        private void KillBackgroundTween()
        {
            if (backgroundVolumeTween != null && backgroundVolumeTween.IsActive())
            {
                backgroundVolumeTween.Kill();
            }

            backgroundVolumeTween = null;
        }
    }
}
