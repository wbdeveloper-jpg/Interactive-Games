using UnityEngine;

public class SentenceWordSearchAudio : MonoBehaviour
{
    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource narrationSource;

    [Header("Clips")]
    public AudioClip bgmClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip completeClip;

    [Header("Volumes")]
    [Range(0f, 1f)] public float bgmVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float narrationVolume = 1f;

    private void Awake()
    {
        EnsureSources();
    }

    public void PlayBgm()
    {
        EnsureSources();

        if (bgmSource == null || bgmClip == null)
            return;

        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlayCorrect()
    {
        PlaySfx(correctClip);
    }

    public void PlayWrong()
    {
        PlaySfx(wrongClip);
    }

    public void PlayComplete()
    {
        PlaySfx(completeClip);
    }

    public float PlayNarrationAndGetDuration(AudioClip clip)
    {
        EnsureSources();

        if (narrationSource == null || clip == null)
            return 0f;

        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.volume = narrationVolume;
        narrationSource.loop = false;
        narrationSource.Play();

        return clip.length;
    }

    public bool IsNarrationPlaying()
    {
        return narrationSource != null && narrationSource.isPlaying;
    }

    private void PlaySfx(AudioClip clip)
    {
        EnsureSources();

        if (sfxSource != null && clip != null)
        {
            sfxSource.volume = sfxVolume;
            sfxSource.PlayOneShot(clip);
        }
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
        {
            GameObject obj = new GameObject("SentenceWordSearch_BGM_Source");
            obj.transform.SetParent(transform, false);
            bgmSource = obj.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject obj = new GameObject("SentenceWordSearch_SFX_Source");
            obj.transform.SetParent(transform, false);
            sfxSource = obj.AddComponent<AudioSource>();
        }

        if (narrationSource == null)
        {
            GameObject obj = new GameObject("SentenceWordSearch_Narration_Source");
            obj.transform.SetParent(transform, false);
            narrationSource = obj.AddComponent<AudioSource>();
        }
    }
}
