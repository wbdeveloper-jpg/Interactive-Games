using UnityEngine;

public class GridAdventureAudioManager : MonoBehaviour
{
    [Header("Sources")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;
    public AudioSource musicSource;

    [Header("Background Music")]
    public AudioClip backgroundMusicClip;
    public bool playMusicOnStart = true;
    public bool loopBackgroundMusic = true;
    [Range(0f, 1f)] public float musicVolume = 0.45f;

    [Header("SFX Clips")]
    public AudioClip clickClip;
    public AudioClip correctSnapClip;
    public AudioClip wrongSlideClip;
    public AudioClip clueClip;
    public AudioClip resultWinClip;
    public AudioClip pauseOpenClip;

    [Header("Mixer-Free Volumes")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float voiceVolume = 1f;

    [Header("State")]
    public bool isMuted;

    private void Awake()
    {
        EnsureSources();
        ApplyAudioState();
    }

    private void Start()
    {
        if (playMusicOnStart)
            PlayBackgroundMusic();
    }

    public void ConfigureBackgroundMusic(AudioClip clip, bool playOnStart, float volume)
    {
        backgroundMusicClip = clip;
        playMusicOnStart = playOnStart;
        musicVolume = Mathf.Clamp01(volume);
        ApplyAudioState();

        if (playMusicOnStart)
            PlayBackgroundMusic();
    }

    public void PlayBackgroundMusic()
    {
        EnsureSources();

        if (backgroundMusicClip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        if (musicSource.clip != backgroundMusicClip)
            musicSource.clip = backgroundMusicClip;

        musicSource.loop = loopBackgroundMusic;
        musicSource.volume = musicVolume;
        musicSource.mute = isMuted;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopBackgroundMusic()
    {
        EnsureSources();
        musicSource.Stop();
    }

    public void PlaySFX(string id)
    {
        if (isMuted) return;
        EnsureSources();

        AudioClip clip = GetSfxClip(id);
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayVoiceover(AudioClip clip)
    {
        if (isMuted || clip == null) return;
        EnsureSources();

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = voiceVolume;
        voiceSource.Play();
    }

    public void SetMuted(bool muted)
    {
        isMuted = muted;
        ApplyAudioState();
    }

    private AudioClip GetSfxClip(string id)
    {
        switch (id)
        {
            case "click": return clickClip;
            case "correct_snap": return correctSnapClip;
            case "wrong_slide": return wrongSlideClip;
            case "clue": return clueClip;
            case "result_win": return resultWinClip;
            case "pause_open": return pauseOpenClip;
            default: return null;
        }
    }

    private void ApplyAudioState()
    {
        EnsureSources();
        sfxSource.mute = isMuted;
        voiceSource.mute = isMuted;
        musicSource.mute = isMuted;
        sfxSource.volume = sfxVolume;
        voiceSource.volume = voiceVolume;
        musicSource.volume = musicVolume;
        musicSource.loop = loopBackgroundMusic;
    }

    private void EnsureSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
    }
}
