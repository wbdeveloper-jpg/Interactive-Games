using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmotionAudioMapper : MonoBehaviour
{
    public static EmotionAudioMapper Instance { get; private set; }

    [System.Serializable]
    public class IntensityAudio
    {
        [Tooltip("Use only 0.2, 0.4, 0.6, 0.8, or 1.0")]
        public float intensity = 0.2f;

        [Tooltip("Example: Not at all / Just a little / More or less / Quite a bit / Very")]
        public AudioClip clip;
    }

    [System.Serializable]
    public class EmotionAudio
    {
        [Tooltip("Must match Draggable.label. Example: Happy, Sad, Angry")]
        public string label;

        [Tooltip("Example: happy / sad / angry")]
        public AudioClip clip;
    }

    [Header("Audio Mapping")]
    [SerializeField] private List<IntensityAudio> intensityAudios = new List<IntensityAudio>();
    [SerializeField] private List<EmotionAudio> emotionAudios = new List<EmotionAudio>();

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float gapBetweenClips = 0.12f;
    [SerializeField] private bool useUnscaledTime = false;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayEmotionAudio(float intensity, string emotionLabel)
    {
        AudioClip intensityClip = GetIntensityClip(intensity);
        AudioClip emotionClip = GetEmotionClip(emotionLabel);

        if (intensityClip == null)
        {
            Debug.LogWarning("EmotionAudioMapper: Missing intensity audio for " + Draggable.NormalizeIntensity(intensity), this);
            return;
        }

        if (emotionClip == null)
        {
            Debug.LogWarning("EmotionAudioMapper: Missing emotion audio for label '" + emotionLabel + "'", this);
            return;
        }

        StopCurrentAudioSequence();
        playRoutine = StartCoroutine(PlaySequence(intensityClip, emotionClip));
    }

    public void StopCurrentAudioSequence()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    private IEnumerator PlaySequence(AudioClip intensityClip, AudioClip emotionClip)
    {
        PlayClip(intensityClip);
        yield return Wait(intensityClip.length + gapBetweenClips);

        PlayClip(emotionClip);
        yield return Wait(emotionClip.length);

        playRoutine = null;
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
            return;

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("EmotionAudioMapper: AudioManager.Instance is missing.", this);
            return;
        }

        AudioManager.Instance.PlaySFXClip(clip);
    }

    private IEnumerator Wait(float seconds)
    {
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
        else
            yield return new WaitForSeconds(Mathf.Max(0f, seconds));
    }

    private AudioClip GetIntensityClip(float intensity)
    {
        float normalized = Draggable.NormalizeIntensity(intensity);

        for (int i = 0; i < intensityAudios.Count; i++)
        {
            IntensityAudio entry = intensityAudios[i];
            if (entry == null || entry.clip == null)
                continue;

            if (Mathf.Approximately(Draggable.NormalizeIntensity(entry.intensity), normalized))
                return entry.clip;
        }

        return null;
    }

    private AudioClip GetEmotionClip(string emotionLabel)
    {
        if (string.IsNullOrWhiteSpace(emotionLabel))
            return null;

        for (int i = 0; i < emotionAudios.Count; i++)
        {
            EmotionAudio entry = emotionAudios[i];
            if (entry == null || entry.clip == null)
                continue;

            if (Draggable.LabelsMatch(entry.label, emotionLabel))
                return entry.clip;
        }

        return null;
    }

    private void OnValidate()
    {
        gapBetweenClips = Mathf.Max(0f, gapBetweenClips);
    }
}
