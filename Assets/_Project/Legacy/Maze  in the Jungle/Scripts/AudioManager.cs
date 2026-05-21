using UnityEngine;

/// <summary>
/// AudioManager
/// -----------------------------------------------------------------------------
/// A centralized audio control system for managing Background Music (BGM) and
/// Sound Effects (SFX) across the game.
///
/// Supports:
/// - Playing BGM by array index
/// - Playing BGM directly using an AudioClip
/// - Playing SFX by array index
/// - Playing SFX directly using an AudioClip
/// - Playing overlapping SFX using PlayOneShot
/// - Pause, resume, stop, and volume control
/// - Optional persistence across scene loads
///
/// Recommended Usage:
/// - Attach this script to a GameObject named "AudioManager".
/// - Assign one AudioSource for BGM.
/// - Assign one AudioSource for SFX.
/// - Add common BGM clips to bgmClips.
/// - Add common SFX clips to sfxClips.
/// - Enable "Persist Across Scenes" if audio should continue between scenes.
///
/// Example Usage:
///     AudioManager.Instance.PlayBGM(0);
///     AudioManager.Instance.PlayBGMClip(menuMusic);
///     AudioManager.Instance.PlaySFX(1);
///     AudioManager.Instance.PlaySFXClipOneShot(jumpSound);
/// -----------------------------------------------------------------------------
//// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [Tooltip("AudioSource used for background music playback.")]
    public AudioSource bgmSource;

    [Tooltip("AudioSource used for sound effect playback.")]
    public AudioSource sfxSource;

    [Header("Background Music Clips")]
    [Tooltip("List of background music clips. Can be played by index.")]
    public AudioClip[] bgmClips;

    [Header("SFX Clips")]
    [Tooltip("List of sound effect clips. Can be played by index.")]
    public AudioClip[] sfxClips;

    [Header("Lifetime")]
    [SerializeField]
    [Tooltip("If enabled, this AudioManager will not be destroyed when loading a new scene.")]
    private bool persistAcrossScenes = false;

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

    // -------------------------------------------------------------------------
    // BGM METHODS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays background music from the bgmClips array using an index.
    /// </summary>
    public void PlayBGM(int index, bool loop = true)
    {
        if (!IsValidClipIndex(bgmClips, index, "BGM") || bgmSource == null)
        {
            return;
        }

        PlayBGMClip(bgmClips[index], loop);
    }

    /// <summary>
    /// Plays background music directly using an AudioClip reference.
    ///
    /// Use this when another script already has a direct AudioClip variable.
    /// Example:
    ///     AudioManager.Instance.PlayBGMClip(levelMusic);
    /// </summary>
    public void PlayBGMClip(AudioClip clip, bool loop = true)
    {
        if (clip == null || bgmSource == null)
        {
            Debug.LogWarning("Invalid BGM clip or missing BGM AudioSource.", this);
            return;
        }

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    /// <summary>
    /// Pauses the currently playing background music.
    /// </summary>
    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }

    /// <summary>
    /// Resumes paused background music.
    /// </summary>
    public void ResumeBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.UnPause();
        }
    }

    // -------------------------------------------------------------------------
    // SFX METHODS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plays a sound effect from the sfxClips array using an index.
    /// Stops the currently playing SFX before playing the new one.
    /// </summary>
    public void PlaySFX(int index)
    {
        PlaySFXInternal(index, 1f, true);
    }

    /// <summary>
    /// Plays a sound effect from the sfxClips array using an index and custom volume.
    /// Stops the currently playing SFX before playing the new one.
    /// </summary>
    public void PlaySFXWithVolume(int index, float volume)
    {
        PlaySFXInternal(index, Mathf.Clamp01(volume), true);
    }

    /// <summary>
    /// Plays a sound effect directly using an AudioClip reference.
    /// Stops the currently playing SFX before playing the new one.
    ///
    /// Use this when only one SFX should play at a time.
    /// </summary>
    public void PlaySFXClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
        {
            Debug.LogWarning("Invalid SFX clip or missing SFX AudioSource.", this);
            return;
        }

        if (sfxSource.isPlaying)
        {
            sfxSource.Stop();
        }

        sfxSource.volume = Mathf.Clamp01(volume);
        sfxSource.clip = clip;
        sfxSource.Play();
    }

    /// <summary>
    /// Plays a sound effect from the sfxClips array using PlayOneShot.
    /// Allows multiple SFX sounds to overlap.
    ///
    /// Best for:
    /// - Button clicks
    /// - Jump sounds
    /// - Coin collection
    /// - Hit effects
    /// - Explosions
    /// </summary>
    public void PlaySFXOneShot(int index, float volume = 1f)
    {
        if (!IsValidClipIndex(sfxClips, index, "SFX") || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(sfxClips[index], Mathf.Clamp01(volume));
    }

    /// <summary>
    /// Plays a sound effect directly using an AudioClip with PlayOneShot.
    /// Allows multiple SFX sounds to overlap.
    ///
    /// Recommended for most gameplay sound effects.
    ///
    /// Example:
    ///     AudioManager.Instance.PlaySFXClipOneShot(jumpSound);
    /// </summary>
    public void PlaySFXClipOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
        {
            Debug.LogWarning("Invalid SFX clip or missing SFX AudioSource.", this);
            return;
        }

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    /// <summary>
    /// Internal method for playing indexed SFX.
    /// </summary>
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

        sfxSource.volume = Mathf.Clamp01(volume);
        sfxSource.clip = sfxClips[index];
        sfxSource.Play();
    }

    /// <summary>
    /// Stops the currently playing background music.
    /// </summary>
    public void StopSFX()
    {
        if (sfxSource != null)
        {
            sfxSource.Stop();
        }
    }

    // -------------------------------------------------------------------------
    // VOLUME METHODS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets background music volume.
    /// Value is clamped between 0 and 1.
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        if (bgmSource != null)
        {
            bgmSource.volume = Mathf.Clamp01(volume);
        }
    }

    /// <summary>
    /// Sets sound effect volume.
    /// Value is clamped between 0 and 1.
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        if (sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(volume);
        }
    }

    // -------------------------------------------------------------------------
    // VALIDATION METHODS
    // -------------------------------------------------------------------------

    /// <summary>
    /// Checks whether a valid SFX clip exists at the given index.
    /// </summary>
    public bool HasSFXClip(int index)
    {
        return IsValidClipIndex(sfxClips, index, "SFX", false);
    }

    /// <summary>
    /// Checks whether a valid BGM clip exists at the given index.
    /// </summary>
    public bool HasBGMClip(int index)
    {
        return IsValidClipIndex(bgmClips, index, "BGM", false);
    }

    /// <summary>
    /// Validates whether the given AudioClip array index is safe to use.
    /// Prevents null reference and out-of-range errors.
    /// </summary>
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