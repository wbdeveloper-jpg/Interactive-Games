using UnityEngine;

namespace EmotionTimerQuiz
{
    public class EmotionTimerQuizAudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        public AudioSource sfxSource;
        public AudioSource musicSource;

        [Header("SFX")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip timeoutClip;
        public AudioClip buttonClip;
        public AudioClip nextRoundClip;

        [Header("Music")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        public bool playMusicOnStart = false;

        private void Awake()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            musicSource.loop = true;
            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
        }

        private void Start()
        {
            if (playMusicOnStart)
            {
                PlayMusic();
            }
        }

        public void PlayCorrect()
        {
            PlayOneShot(correctClip);
        }

        public void PlayWrong()
        {
            PlayOneShot(wrongClip);
        }

        public void PlayTimeout()
        {
            PlayOneShot(timeoutClip);
        }

        public void PlayButton()
        {
            PlayOneShot(buttonClip);
        }

        public void PlayNextRound()
        {
            PlayOneShot(nextRoundClip != null ? nextRoundClip : buttonClip);
        }

        public void PlayMusic()
        {
            if (musicSource == null || backgroundMusic == null)
            {
                return;
            }

            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (sfxSource == null || clip == null)
            {
                return;
            }

            sfxSource.volume = sfxVolume;
            sfxSource.PlayOneShot(clip);
        }
    }
}
