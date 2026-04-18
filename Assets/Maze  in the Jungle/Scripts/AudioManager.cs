using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Background Music Clips")]
    public AudioClip[] bgmClips;

    [Header("SFX Clips")]
    public AudioClip[] sfxClips;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // 🎵 BGM FUNCTIONS
    // =========================

    public void PlayBGM(int index, bool loop = true)
    {
        if (index < 0 || index >= bgmClips.Length)
        {
            Debug.LogWarning("Invalid BGM index");
            return;
        }

        bgmSource.clip = bgmClips[index];
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PauseBGM()
    {
        bgmSource.Pause();
    }

    public void ResumeBGM()
    {
        bgmSource.UnPause();
    }

    // =========================
    // 🔊 SFX FUNCTIONS
    // =========================

    // ✅ Normal SFX (always plays at full volume = 1)
    public void PlaySFX(int index)
    {
        if (index < 0 || index >= sfxClips.Length)
        {
            Debug.LogWarning("Invalid SFX index");
            return;
        }

        // Stop previous SFX (NO overlap)
        if (sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }

        // Reset volume to constant
        sfxSource.volume = 1f;

        sfxSource.clip = sfxClips[index];
        sfxSource.Play();
    }


    // ✅ Custom Volume SFX (you control volume)
    public void PlaySFXWithVolume(int index, float volume)
    {
        if (index < 0 || index >= sfxClips.Length)
        {
            Debug.LogWarning("Invalid SFX index");
            return;
        }

        // Clamp volume (safety)
        volume = Mathf.Clamp01(volume);

        // Stop previous SFX (NO overlap)
        if (sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }

        // Apply custom volume
        sfxSource.volume = volume;

        sfxSource.clip = sfxClips[index];
        sfxSource.Play();
    }

    // =========================
    // 🔊 VOLUME CONTROL
    // =========================

    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}