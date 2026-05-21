using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryAudioNarrationManager : MonoBehaviour
    {
        [SerializeField] private AudioSource narrationAudioSource;
        [SerializeField] private bool stopCurrentBeforePlay = true;

        private AudioClip currentClip;
        private bool pausedByGamePause;

        public bool HasCurrentClip => currentClip != null;
        public float CurrentClipLength => currentClip != null ? currentClip.length : 0f;
        public bool IsPlaying => narrationAudioSource != null && narrationAudioSource.isPlaying;
        public bool IsPausedByGamePause => pausedByGamePause;

        private void Awake()
        {
            if (narrationAudioSource == null)
            {
                narrationAudioSource = GetComponent<AudioSource>();
            }

            if (narrationAudioSource == null)
            {
                narrationAudioSource = gameObject.AddComponent<AudioSource>();
            }

            narrationAudioSource.playOnAwake = false;
        }

        public float Play(AudioClip clip)
        {
            currentClip = clip;
            pausedByGamePause = false;

            if (clip == null || narrationAudioSource == null)
            {
                return 0f;
            }

            if (stopCurrentBeforePlay)
            {
                narrationAudioSource.Stop();
            }

            narrationAudioSource.clip = clip;
            narrationAudioSource.Play();
            return clip.length;
        }

        public float ReplayCurrent()
        {
            if (currentClip == null)
            {
                return 0f;
            }

            return Play(currentClip);
        }

        public void PauseForGamePause()
        {
            if (narrationAudioSource == null)
            {
                return;
            }

            if (narrationAudioSource.isPlaying)
            {
                narrationAudioSource.Pause();
                pausedByGamePause = true;
            }
        }

        public void ResumeFromGamePause()
        {
            if (narrationAudioSource == null || !pausedByGamePause)
            {
                return;
            }

            narrationAudioSource.UnPause();
            pausedByGamePause = false;
        }

        public void Stop()
        {
            pausedByGamePause = false;

            if (narrationAudioSource != null)
            {
                narrationAudioSource.Stop();
            }
        }
    }
}
