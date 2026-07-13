using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TreasureQuestAudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("SFX Clips")]
    public AudioClip clickClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip unlockClip;
    public AudioClip lockedClip;

    [Header("Music")]
    public AudioClip backgroundMusic;
    public bool playMusicOnStart = true;
    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private void Reset()
    {
        sfxSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (musicSource == null)
        {
            GameObject musicObject = new GameObject("TreasureQuest_MusicSource");
            musicObject.transform.SetParent(transform);
            musicSource = musicObject.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.volume = musicVolume;
    }

    private void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
            PlayMusic(backgroundMusic);
    }

    public void PlayClick() => PlayOneShot(clickClip);
    public void PlayCorrect() => PlayOneShot(correctClip);
    public void PlayWrong() => PlayOneShot(wrongClip);
    public void PlayUnlock() => PlayOneShot(unlockClip);
    public void PlayLocked() => PlayOneShot(lockedClip);

    public void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
    }
}
