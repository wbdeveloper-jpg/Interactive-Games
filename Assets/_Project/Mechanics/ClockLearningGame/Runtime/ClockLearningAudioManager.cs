using UnityEngine;
using DG.Tweening;

namespace ClockLearningGame
{
    [DisallowMultipleComponent]
    public sealed class ClockLearningAudioManager : MonoBehaviour
    {
        [Header("SFX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip closeClip;
        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip hintClip;
        [SerializeField] private AudioClip hintAttentionClip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Header("Background Music")]
        [SerializeField] private AudioSource backgroundMusicSource;
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.45f;
        [SerializeField] private bool loopBackgroundMusic = true;
        [SerializeField] private bool playBackgroundMusicOnStart = false;
        [SerializeField] private bool fadeBackgroundMusic = true;
        [SerializeField, Range(0.05f, 2f)] private float backgroundMusicFadeDuration = 0.35f;

        private Tween _backgroundMusicTween;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.loop = false;
            audioSource.playOnAwake = false;

            if (backgroundMusicSource == null)
            {
                AudioSource[] sources = GetComponents<AudioSource>();
                if (sources.Length > 1) backgroundMusicSource = sources[1];
            }

            if (backgroundMusicSource == null)
            {
                backgroundMusicSource = gameObject.AddComponent<AudioSource>();
            }

            backgroundMusicSource.loop = loopBackgroundMusic;
            backgroundMusicSource.playOnAwake = false;
            backgroundMusicSource.volume = backgroundMusicVolume;
        }

        private void Start()
        {
            if (playBackgroundMusicOnStart)
            {
                PlayBackgroundMusic();
            }
        }

        private void OnDestroy()
        {
            _backgroundMusicTween?.Kill();
        }

        public void PlayClick() => Play(clickClip);
        public void PlayCorrect() => Play(correctClip);
        public void PlayClose() => Play(closeClip);
        public void PlayWrong() => Play(wrongClip);
        public void PlayHint() => Play(hintClip != null ? hintClip : clickClip);
        public void PlayHintAttention() => Play(hintAttentionClip != null ? hintAttentionClip : wrongClip);

        public void PlayBackgroundMusic()
        {
            PlayBackgroundMusic(fadeBackgroundMusic ? backgroundMusicFadeDuration : 0f);
        }

        public void PlayBackgroundMusic(float fadeDuration)
        {
            if (backgroundMusicClip == null || backgroundMusicSource == null) return;

            _backgroundMusicTween?.Kill();

            if (backgroundMusicSource.clip != backgroundMusicClip)
            {
                backgroundMusicSource.clip = backgroundMusicClip;
            }

            backgroundMusicSource.loop = loopBackgroundMusic;

            if (!backgroundMusicSource.isPlaying)
            {
                if (fadeDuration > 0f) backgroundMusicSource.volume = 0f;
                backgroundMusicSource.Play();
            }

            if (fadeDuration > 0f)
            {
                _backgroundMusicTween = backgroundMusicSource.DOFade(backgroundMusicVolume, fadeDuration).SetUpdate(true);
            }
            else
            {
                backgroundMusicSource.volume = backgroundMusicVolume;
            }
        }

        public void StopBackgroundMusic()
        {
            StopBackgroundMusic(fadeBackgroundMusic ? backgroundMusicFadeDuration : 0f);
        }

        public void StopBackgroundMusic(float fadeDuration)
        {
            if (backgroundMusicSource == null) return;

            _backgroundMusicTween?.Kill();

            if (fadeDuration > 0f && backgroundMusicSource.isPlaying)
            {
                _backgroundMusicTween = backgroundMusicSource.DOFade(0f, fadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (backgroundMusicSource != null) backgroundMusicSource.Stop();
                    });
                return;
            }

            backgroundMusicSource.Stop();
        }

        public void PauseBackgroundMusic()
        {
            PauseBackgroundMusic(fadeBackgroundMusic ? backgroundMusicFadeDuration : 0f);
        }

        public void PauseBackgroundMusic(float fadeDuration)
        {
            if (backgroundMusicSource == null || !backgroundMusicSource.isPlaying) return;

            _backgroundMusicTween?.Kill();

            if (fadeDuration > 0f)
            {
                _backgroundMusicTween = backgroundMusicSource.DOFade(0f, fadeDuration)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (backgroundMusicSource != null) backgroundMusicSource.Pause();
                    });
                return;
            }

            backgroundMusicSource.Pause();
        }

        public void ResumeBackgroundMusic()
        {
            if (backgroundMusicSource == null || backgroundMusicClip == null) return;

            _backgroundMusicTween?.Kill();

            if (backgroundMusicSource.clip != backgroundMusicClip)
            {
                backgroundMusicSource.clip = backgroundMusicClip;
            }

            backgroundMusicSource.loop = loopBackgroundMusic;
            backgroundMusicSource.UnPause();

            if (!backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }

            if (fadeBackgroundMusic)
            {
                backgroundMusicSource.volume = Mathf.Min(backgroundMusicSource.volume, backgroundMusicVolume);
                _backgroundMusicTween = backgroundMusicSource.DOFade(backgroundMusicVolume, backgroundMusicFadeDuration).SetUpdate(true);
            }
            else
            {
                backgroundMusicSource.volume = backgroundMusicVolume;
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }

            audioSource.PlayOneShot(clip, volume);
        }
    }
}
