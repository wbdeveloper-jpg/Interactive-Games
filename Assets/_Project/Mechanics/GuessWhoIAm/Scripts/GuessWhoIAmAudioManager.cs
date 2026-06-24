using UnityEngine;
using DG.Tweening;

namespace GuessWhoIAm
{
    public class GuessWhoIAmAudioManager : MonoBehaviour
    {
        [Header("SFX Source")]
        [SerializeField] private AudioSource sfxSource;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

        [Header("Background Music")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip backgroundMusicClip;
        [Range(0f, 1f)] [SerializeField] private float backgroundMusicVolume = 0.45f;
        [SerializeField] private bool loopBackgroundMusic = true;
        [SerializeField] private bool playBgmOnGameStart = true;
        [SerializeField] private float bgmFadeSeconds = 0.25f;

        [Header("Fallback SFX")]
        [SerializeField] private AudioClip defaultCorrectClip;
        [SerializeField] private AudioClip defaultWrongClip;
        [SerializeField] private AudioClip defaultRevealClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip resultClip;

        private Tween bgmFadeTween;

        private void Reset()
        {
            EnsureSfxSource();
            EnsureBgmSource();
        }

        private void Awake()
        {
            EnsureSfxSource();
            EnsureBgmSource();
        }

        public void PlayBackgroundMusic()
        {
            if (!playBgmOnGameStart || backgroundMusicClip == null)
                return;

            EnsureBgmSource();
            bgmFadeTween?.Kill();

            bgmSource.clip = backgroundMusicClip;
            bgmSource.loop = loopBackgroundMusic;
            bgmSource.playOnAwake = false;

            if (!bgmSource.isPlaying)
                bgmSource.Play();

            bgmSource.volume = 0f;
            bgmFadeTween = bgmSource.DOFade(backgroundMusicVolume, bgmFadeSeconds).SetUpdate(true);
        }

        public void StopBackgroundMusic()
        {
            if (bgmSource == null)
                return;

            bgmFadeTween?.Kill();

            if (!bgmSource.isPlaying)
            {
                bgmSource.Stop();
                return;
            }

            bgmFadeTween = bgmSource.DOFade(0f, bgmFadeSeconds).SetUpdate(true).OnComplete(() =>
            {
                if (bgmSource != null)
                    bgmSource.Stop();
            });
        }

        public void PlayCorrect(GuessWhoQuestionData question)
        {
            PlayOneShot(question != null && question.correctAudio != null ? question.correctAudio : defaultCorrectClip);
        }

        public void PlayWrong(GuessWhoQuestionData question)
        {
            PlayOneShot(question != null && question.wrongAudio != null ? question.wrongAudio : defaultWrongClip);
        }

        public void PlayReveal(GuessWhoQuestionData question)
        {
            PlayOneShot(question != null && question.revealAudio != null ? question.revealAudio : defaultRevealClip);
        }

        public void PlayButtonClick()
        {
            PlayOneShot(buttonClickClip);
        }

        public void PlayResult()
        {
            PlayOneShot(resultClip);
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null)
                return;

            EnsureSfxSource();
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private void EnsureSfxSource()
        {
            if (sfxSource != null)
                return;

            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();

            sfxSource.playOnAwake = false;
        }

        private void EnsureBgmSource()
        {
            if (bgmSource != null)
                return;

            GameObject bgmGo = new GameObject("GuessWhoIAm_BGMSource");
            bgmGo.transform.SetParent(transform, false);
            bgmSource = bgmGo.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = loopBackgroundMusic;
            bgmSource.volume = backgroundMusicVolume;
        }
    }
}
