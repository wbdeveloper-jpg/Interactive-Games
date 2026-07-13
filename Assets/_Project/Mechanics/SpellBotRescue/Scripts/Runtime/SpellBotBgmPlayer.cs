using DG.Tweening;
using UnityEngine;

namespace NarayanaGames.SpellBotRescue
{
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class SpellBotBgmPlayer : MonoBehaviour
{
    public static SpellBotBgmPlayer Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float targetVolume = 0.45f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool keepPlayingAcrossSceneLoads = false;

    [Header("Fade")]
    [SerializeField] private bool useFadeIn = true;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.8f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;

    private AudioSource _audioSource;
    private Tween _volumeTween;
    private float _cachedVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (keepPlayingAcrossSceneLoads)
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
        _audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();

        if (keepPlayingAcrossSceneLoads)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayMusic();
        }
    }

    private void OnDestroy()
    {
        _volumeTween?.Kill();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void ConfigureAudioSource()
    {
        if (_audioSource == null)
        {
            return;
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = loop;
        _audioSource.spatialBlend = 0f;
        _audioSource.clip = backgroundMusic;
        _audioSource.volume = targetVolume;
        _cachedVolume = targetVolume;
    }

    public void PlayMusic()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
        }

        if (backgroundMusic != null && _audioSource.clip != backgroundMusic)
        {
            _audioSource.clip = backgroundMusic;
        }

        if (_audioSource.clip == null)
        {
            Debug.LogWarning("SpellBotBgmPlayer: No background music clip assigned.", this);
            return;
        }

        _volumeTween?.Kill();
        _audioSource.loop = loop;

        if (useFadeIn && fadeInDuration > 0f)
        {
            _audioSource.volume = 0f;
            _audioSource.Play();
            _volumeTween = _audioSource.DOFade(targetVolume, fadeInDuration).SetUpdate(true);
        }
        else
        {
            _audioSource.volume = targetVolume;
            _audioSource.Play();
        }

        _cachedVolume = targetVolume;
    }

    public void StopMusic()
    {
        StopMusicWithFade();
    }

    public void StopMusicWithFade()
    {
        if (_audioSource == null || !_audioSource.isPlaying)
        {
            return;
        }

        _volumeTween?.Kill();

        if (fadeOutDuration <= 0f)
        {
            StopMusicInstant();
            return;
        }

        _volumeTween = _audioSource
            .DOFade(0f, fadeOutDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (_audioSource == null)
                {
                    return;
                }

                _audioSource.Stop();
                _audioSource.volume = targetVolume;
            });
    }

    public void StopMusicInstant()
    {
        if (_audioSource == null)
        {
            return;
        }

        _volumeTween?.Kill();
        _audioSource.Stop();
        _audioSource.volume = targetVolume;
    }

    public void PauseMusic()
    {
        if (_audioSource == null)
        {
            return;
        }

        _cachedVolume = _audioSource.volume;
        _audioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (_audioSource == null || _audioSource.clip == null)
        {
            return;
        }

        _audioSource.volume = Mathf.Clamp01(_cachedVolume <= 0f ? targetVolume : _cachedVolume);
        _audioSource.UnPause();
    }

    public void SetVolume(float volume)
    {
        targetVolume = Mathf.Clamp01(volume);
        _cachedVolume = targetVolume;

        if (_audioSource != null)
        {
            _audioSource.volume = targetVolume;
        }
    }

    public void Mute()
    {
        if (_audioSource != null)
        {
            _audioSource.mute = true;
        }
    }

    public void Unmute()
    {
        if (_audioSource != null)
        {
            _audioSource.mute = false;
        }
    }
}
}
