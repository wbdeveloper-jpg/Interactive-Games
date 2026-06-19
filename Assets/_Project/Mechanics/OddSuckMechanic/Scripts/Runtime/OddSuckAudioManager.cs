using UnityEngine;

namespace OddSuckMechanic
{
    public class OddSuckAudioManager : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Background Music")]
        [SerializeField] private AudioClip backgroundMusicClip;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;
        [SerializeField] private bool loopBackgroundMusic = true;
        [SerializeField] private bool playMusicOnGameplayStart = true;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip suckClip;
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip noTargetClip;
        [SerializeField] private AudioClip buttonClip;
        [SerializeField] private AudioClip gameOverClip;

        [Header("Settings")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private bool musicPausedByGame;

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

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume;

            musicSource.playOnAwake = false;
            musicSource.loop = loopBackgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.clip = backgroundMusicClip;
        }

        public void PlayMusic()
        {
            if (!playMusicOnGameplayStart || musicSource == null || backgroundMusicClip == null)
            {
                return;
            }

            musicSource.clip = backgroundMusicClip;
            musicSource.loop = loopBackgroundMusic;
            musicSource.volume = musicVolume;
            musicPausedByGame = false;

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void PauseMusic()
        {
            if (musicSource == null || !musicSource.isPlaying)
            {
                return;
            }

            musicPausedByGame = true;
            musicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (musicSource == null || !musicPausedByGame)
            {
                return;
            }

            musicPausedByGame = false;
            musicSource.UnPause();
        }

        public void StopMusic()
        {
            musicPausedByGame = false;
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void PlaySuck() => PlayOneShot(suckClip);
        public void PlayCorrect() => PlayOneShot(correctClip);
        public void PlayWrong() => PlayOneShot(wrongClip);
        public void PlayNoTarget() => PlayOneShot(noTargetClip);
        public void PlayButton() => PlayOneShot(buttonClip);
        public void PlayGameOver() => PlayOneShot(gameOverClip);

        public void StopAllAudio()
        {
            if (sfxSource != null)
            {
                sfxSource.Stop();
            }

            StopMusic();
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.volume = sfxVolume;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
