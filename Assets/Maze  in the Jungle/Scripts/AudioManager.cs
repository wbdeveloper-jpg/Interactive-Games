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

    [Header("Lifetime")]
    [SerializeField] private bool persistAcrossScenes = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayBGM(int index, bool loop = true)
    {
        if (!IsValidClipIndex(bgmClips, index, "BGM") || bgmSource == null)
        {
            return;
        }

        if (bgmSource.clip == bgmClips[index] && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            return;
        }

        bgmSource.clip = bgmClips[index];
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }

    public void ResumeBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.UnPause();
        }
    }

    public void PlaySFX(int index)
    {
        PlaySFXInternal(index, 1f, true);
    }

    public void PlaySFXWithVolume(int index, float volume)
    {
        PlaySFXInternal(index, Mathf.Clamp01(volume), true);
    }

    public void PlaySFXOneShot(int index, float volume = 1f)
    {
        if (!IsValidClipIndex(sfxClips, index, "SFX") || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(sfxClips[index], Mathf.Clamp01(volume));
    }

    private void PlaySFXInternal(int index, float volume, bool stopCurrent)
    {
        if (!IsValidClipIndex(sfxClips, index, "SFX") || sfxSource == null)
        {
            return;
        }

        if (stopCurrent && sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }

        sfxSource.volume = volume;
        sfxSource.clip = sfxClips[index];
        sfxSource.Play();
    }

    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }
    }

    public bool HasSFXClip(int index)
    {
        return IsValidClipIndex(sfxClips, index, "SFX", false);
    }

    public bool HasBGMClip(int index)
    {
        return IsValidClipIndex(bgmClips, index, "BGM", false);
    }

    private bool IsValidClipIndex(AudioClip[] clips, int index, string label, bool logWarning = true)
    {
        bool valid = clips != null && index >= 0 && index < clips.Length && clips[index] != null;
        if (!valid && logWarning)
        {
            Debug.LogWarning("Invalid " + label + " index: " + index, this);
        }

        return valid;
    }
}
