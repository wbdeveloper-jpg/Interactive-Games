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
        [Tooltip("Must match Draggable label. Example: Happy, Sad, Angry")]
        public string label;

        [Tooltip("Example: happy / sad / angry")]
        public AudioClip clip;
    }

    [Header("Audio Mapping")]
    [SerializeField] private List<IntensityAudio> intensityAudios = new List<IntensityAudio>();
    [SerializeField] private List<EmotionAudio> emotionAudios = new List<EmotionAudio>();

    [Header("Timing")]
    [SerializeField] private float gapBetweenClips = 0.12f;

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

    public void PlayEmotionAudio(float intensity, string emotionLabel)
    {
        AudioClip intensityClip = GetIntensityClip(intensity);
        AudioClip emotionClip = GetEmotionClip(emotionLabel);

        if (intensityClip == null)
        {
            Debug.LogWarning($"EmotionAudioMapper: Missing intensity audio for {intensity}", this);
            return;
        }

        if (emotionClip == null)
        {
            Debug.LogWarning($"EmotionAudioMapper: Missing emotion audio for label '{emotionLabel}'", this);
            return;
        }

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlaySequence(intensityClip, emotionClip));
    }

    private IEnumerator PlaySequence(AudioClip intensityClip, AudioClip emotionClip)
    {
        AudioManager.Instance.PlaySFXClip(intensityClip);

        yield return new WaitForSeconds(intensityClip.length + gapBetweenClips);

        AudioManager.Instance.PlaySFXClip(emotionClip);

        yield return new WaitForSeconds(emotionClip.length);

        playRoutine = null;
    }

    private AudioClip GetIntensityClip(float intensity)
    {
        float normalized = NormalizeIntensity(intensity);

        for (int i = 0; i < intensityAudios.Count; i++)
        {
            if (intensityAudios[i] == null)
                continue;

            if (Mathf.Approximately(NormalizeIntensity(intensityAudios[i].intensity), normalized))
                return intensityAudios[i].clip;
        }

        return null;
    }

    private AudioClip GetEmotionClip(string emotionLabel)
    {
        if (string.IsNullOrWhiteSpace(emotionLabel))
            return null;

        string target = emotionLabel.Trim();

        for (int i = 0; i < emotionAudios.Count; i++)
        {
            if (emotionAudios[i] == null)
                continue;

            if (string.Equals(emotionAudios[i].label?.Trim(), target, System.StringComparison.OrdinalIgnoreCase))
                return emotionAudios[i].clip;
        }

        return null;
    }

    private float NormalizeIntensity(float value)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp01(value) / 0.2f);
        step = Mathf.Clamp(step, 1, 5);
        return step * 0.2f;
    }
}