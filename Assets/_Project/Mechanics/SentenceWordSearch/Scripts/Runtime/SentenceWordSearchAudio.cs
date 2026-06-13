using UnityEngine;

[DisallowMultipleComponent]
public class SentenceWordSearchAudio : MonoBehaviour
{
    [Header("Sources")]
    public AudioSource bgMusicSource;
    public AudioSource sfxSource;
    public AudioSource narrationSource;

    [Header("Music")]
    public AudioClip bgMusicClip;
    [Range(0f, 1f)] public float bgMusicVolume = 0.35f;
    public bool playMusicOnStart = true;
    [Tooltip("Recommended for Bloom flow. Music starts only after PreGame and How To Play are complete.")]
    public bool deferMusicUntilGameplayStarts = true;

    [Header("SFX")]
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip scorePopupClip;
    public AudioClip hintClip;
    public AudioClip completeClip;

    [Header("Narration")]
    public float noNarrationReadDuration = 1.35f;

    public bool IsNarrationPlaying => narrationSource != null && narrationSource.isPlaying;

    private void Awake()
    {
        EnsureSources();
    }

    private void Start()
    {
        if (playMusicOnStart && !deferMusicUntilGameplayStarts)
            PlayBgMusic();
    }

    public void EnsureSources()
    {
        if (bgMusicSource == null)
        {
            bgMusicSource = gameObject.AddComponent<AudioSource>();
            bgMusicSource.loop = true;
            bgMusicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (narrationSource == null)
        {
            narrationSource = gameObject.AddComponent<AudioSource>();
            narrationSource.loop = false;
            narrationSource.playOnAwake = false;
        }
    }

    public void PlayBgMusic()
    {
        EnsureSources();

        if (!playMusicOnStart || bgMusicClip == null)
            return;

        bgMusicSource.clip = bgMusicClip;
        bgMusicSource.volume = bgMusicVolume;
        bgMusicSource.loop = true;
        bgMusicSource.Play();
    }

    public void StopBgMusic()
    {
        if (bgMusicSource != null)
            bgMusicSource.Stop();
    }

    public void PlaySfx(AudioClip clip)
    {
        EnsureSources();

        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public float PlayNarration(AudioClip clip)
    {
        EnsureSources();

        if (clip == null)
            return noNarrationReadDuration;

        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.Play();

        return Mathf.Max(0.1f, clip.length);
    }

    public void StopNarration()
    {
        if (narrationSource != null)
            narrationSource.Stop();
    }

    public void StopSfx()
    {
        if (sfxSource != null)
            sfxSource.Stop();
    }

    public void StopAllGameAudio()
    {
        StopBgMusic();
        StopNarration();
        StopSfx();
    }
}
