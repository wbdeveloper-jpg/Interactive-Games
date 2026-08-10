using UnityEngine;

namespace MeasurementMix
{
    public class MeasurementAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource effectsSource;

        [Header("Music")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float musicVolume = 0.35f;

        [Header("Effects")]
        public AudioClip buttonClip;
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip hintClip;
        public AudioClip weightDropClip;
        public AudioClip waterClip;
        public AudioClip timeoutClip;
        [Range(0f, 1f)] public float effectsVolume = 0.8f;

        private void Awake()
        {
            if (musicSource == null)
                return;

            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
            musicSource.clip = backgroundMusic;

            if (backgroundMusic != null)
                musicSource.Play();
        }

        public void PlayButton() => Play(buttonClip);
        public void PlayCorrect() => Play(correctClip);
        public void PlayWrong() => Play(wrongClip);
        public void PlayHint() => Play(hintClip);
        public void PlayWeightDrop() => Play(weightDropClip);
        public void PlayWater() => Play(waterClip);
        public void PlayTimeout() => Play(timeoutClip);

        private void Play(AudioClip clip)
        {
            if (clip != null && effectsSource != null)
                effectsSource.PlayOneShot(clip, effectsVolume);
        }
    }
}
