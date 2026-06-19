using UnityEngine;

public class OddClawAudioManager : MonoBehaviour
{
    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip bgm;
    public AudioClip buttonClick;
    public AudioClip correct;
    public AudioClip wrong;
    public AudioClip miss;
    public AudioClip clawExtend;
    public AudioClip clawGrab;
    public AudioClip clawRetract;
    public AudioClip gameOver;

    [Header("Settings")]
    [Range(0f, 1f)] public float bgmVolume = 0.45f;
    [Range(0f, 1f)] public float sfxVolume = 0.85f;
    public bool playBgmOnGameplayStart = true;

    private void Reset()
    {
        EnsureSources();
    }

    private void Awake()
    {
        EnsureSources();
    }

    public void PlayBgm()
    {
        if (!playBgmOnGameplayStart || bgmSource == null || bgm == null)
            return;

        bgmSource.clip = bgm;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlayButtonClick() => PlayOneShot(buttonClick);
    public void PlayCorrect() => PlayOneShot(correct);
    public void PlayWrong() => PlayOneShot(wrong);
    public void PlayMiss() => PlayOneShot(miss);
    public void PlayClawExtend() => PlayOneShot(clawExtend);
    public void PlayClawGrab() => PlayOneShot(clawGrab);
    public void PlayClawRetract() => PlayOneShot(clawRetract);
    public void PlayGameOver() => PlayOneShot(gameOver);

    private void PlayOneShot(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void EnsureSources()
    {
        if (bgmSource == null)
        {
            GameObject bgmObject = new GameObject("BGM Source");
            bgmObject.transform.SetParent(transform, false);
            bgmSource = bgmObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            GameObject sfxObject = new GameObject("SFX Source");
            sfxObject.transform.SetParent(transform, false);
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }
}
