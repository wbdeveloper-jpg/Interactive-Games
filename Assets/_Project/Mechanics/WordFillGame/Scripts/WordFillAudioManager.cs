using UnityEngine;

public class WordFillAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource narrationSource;
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip buttonTapClip;
    [SerializeField] private AudioClip letterTapClip;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;
    [SerializeField] private AudioClip hintOpenClip;
    [SerializeField] private AudioClip timerTickClip;
    [SerializeField] private AudioClip timeUpClip;
    [SerializeField] private AudioClip gameCompleteClip;
    [SerializeField] private AudioClip panelOpenClip;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField] private bool playMusicOnRoundStart = true;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float narrationVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.35f;

    private void Awake()
    {
        if (sfxSource == null)
            sfxSource = CreateAudioSource("SFX Source", false);

        if (narrationSource == null)
            narrationSource = CreateAudioSource("Narration Source", false);

        if (musicSource == null)
            musicSource = CreateAudioSource("Music Source", true);

        ApplyVolumes();
    }

    private void OnValidate()
    {
        ApplyVolumes();
    }

    private AudioSource CreateAudioSource(string objectName, bool loop)
    {
        GameObject sourceObject = new GameObject(objectName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;

        return source;
    }

    private void ApplyVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (narrationSource != null)
            narrationSource.volume = narrationVolume;

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void PlayRoundMusicIfAllowed()
    {
        if (!playMusicOnRoundStart)
            return;

        PlayBackgroundMusic();
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusicClip == null || musicSource == null)
            return;

        if (musicSource.clip != backgroundMusicClip)
            musicSource.clip = backgroundMusicClip;

        musicSource.loop = true;
        musicSource.volume = musicVolume;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void PauseBackgroundMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeBackgroundMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }

    public void StopBackgroundMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void PlayButtonTap() => PlaySfx(buttonTapClip);
    public void PlayLetterTap() => PlaySfx(letterTapClip);
    public void PlayCorrect() => PlaySfx(correctClip);
    public void PlayWrong() => PlaySfx(wrongClip);
    public void PlayHintOpen() => PlaySfx(hintOpenClip);
    public void PlayTimerTick() => PlaySfx(timerTickClip);
    public void PlayTimeUp() => PlaySfx(timeUpClip);
    public void PlayGameComplete() => PlaySfx(gameCompleteClip);
    public void PlayPanelOpen() => PlaySfx(panelOpenClip);

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public float PlayNarration(AudioClip clip)
    {
        if (clip == null || narrationSource == null)
            return 0f;

        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.volume = narrationVolume;
        narrationSource.Play();

        return clip.length;
    }

    public void PauseNarration()
    {
        if (narrationSource != null && narrationSource.isPlaying)
            narrationSource.Pause();
    }

    public void ResumeNarration()
    {
        if (narrationSource != null)
            narrationSource.UnPause();
    }

    public void StopNarration()
    {
        if (narrationSource != null)
            narrationSource.Stop();
    }
}
