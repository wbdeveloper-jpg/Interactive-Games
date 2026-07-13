using UnityEngine;

namespace BehaviourWheelStop
{
    public class BehaviourWheelAudioController : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float bgmVolume = 0.45f;
        public bool playBgmOnGameStart = true;
        public bool loopBgm = true;

        [Header("SFX Clips")]
        public AudioClip buttonClickClip;
        public AudioClip stopWheelClip;
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip feedbackPopupClip;
        public AudioClip resultClip;
        public AudioClip pauseOpenClip;
        public AudioClip panelOpenClip;

        [Header("SFX Volume")]
        [Range(0f, 1f)] public float sfxVolume = 0.85f;

        private void Awake()
        {
            EnsureSources();
        }

        public void PlayBackgroundMusic()
        {
            EnsureSources();
            if (bgmSource == null || backgroundMusic == null)
                return;

            bgmSource.clip = backgroundMusic;
            bgmSource.volume = bgmVolume;
            bgmSource.loop = loopBgm;
            if (!bgmSource.isPlaying)
                bgmSource.Play();
        }

        public void StopBackgroundMusic()
        {
            if (bgmSource != null && bgmSource.isPlaying)
                bgmSource.Stop();
        }

        public void PlayButtonClick() => PlayOneShot(buttonClickClip);
        public void PlayStopWheel() => PlayOneShot(stopWheelClip);
        public void PlayCorrect() => PlayOneShot(correctClip);
        public void PlayWrong() => PlayOneShot(wrongClip);
        public void PlayFeedbackPopup() => PlayOneShot(feedbackPopupClip);
        public void PlayResult() => PlayOneShot(resultClip);
        public void PlayPauseOpen() => PlayOneShot(pauseOpenClip);
        public void PlayPanelOpen() => PlayOneShot(panelOpenClip);

        private void PlayOneShot(AudioClip clip)
        {
            EnsureSources();
            if (sfxSource == null || clip == null)
                return;

            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private void EnsureSources()
        {
            if (bgmSource == null)
            {
                GameObject bgmObject = new GameObject("BGM_AudioSource");
                bgmObject.transform.SetParent(transform, false);
                bgmSource = bgmObject.AddComponent<AudioSource>();
                bgmSource.playOnAwake = false;
                bgmSource.loop = loopBgm;
            }

            if (sfxSource == null)
            {
                GameObject sfxObject = new GameObject("SFX_AudioSource");
                sfxObject.transform.SetParent(transform, false);
                sfxSource = sfxObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }
        }
    }
}
