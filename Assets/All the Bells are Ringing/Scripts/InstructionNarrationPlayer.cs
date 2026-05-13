using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstructionNarrationPlayer : MonoBehaviour
{
    [System.Serializable]
    public class EmotionNarrationClip
    {
        public string emotionLabel;
        public AudioClip clip;
    }

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Instruction Audio Parts")]
    [Tooltip("Example audio: 'Drag the'")]
    [SerializeField] private AudioClip prefixClip;

    [Tooltip("Example audio: 'emoji that feels the same'")]
    [SerializeField] private AudioClip suffixClip;

    [Header("Emotion Clips")]
    [SerializeField] private List<EmotionNarrationClip> emotionClips = new List<EmotionNarrationClip>();

    [Header("Timing")]
    [Min(0f)] [SerializeField] private float gapBetweenClips = 0.08f;
    [SerializeField] private bool useUnscaledTime = false;

    private Coroutine narrationRoutine;

    private void Awake()
    {
        EnsureAudioSource();
    }

    public void PlayInstruction(string emotionLabel)
    {
        StopNarration();
        EnsureAudioSource();

        AudioClip emotionClip = GetEmotionClip(emotionLabel);
        narrationRoutine = StartCoroutine(PlayInstructionRoutine(prefixClip, emotionClip, suffixClip));
    }

    public void StopNarration()
    {
        if (narrationRoutine != null)
        {
            StopCoroutine(narrationRoutine);
            narrationRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private IEnumerator PlayInstructionRoutine(AudioClip prefix, AudioClip emotion, AudioClip suffix)
    {
        yield return PlayClip(prefix);
        yield return PlayClip(emotion);
        yield return PlayClip(suffix);

        narrationRoutine = null;
    }

    private IEnumerator PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            yield break;

        audioSource.clip = clip;
        audioSource.Play();

        float wait = Mathf.Max(0f, clip.length + gapBetweenClips);
        if (useUnscaledTime)
            yield return new WaitForSecondsRealtime(wait);
        else
            yield return new WaitForSeconds(wait);
    }

    private AudioClip GetEmotionClip(string emotionLabel)
    {
        if (string.IsNullOrWhiteSpace(emotionLabel))
            return null;

        for (int i = 0; i < emotionClips.Count; i++)
        {
            EmotionNarrationClip entry = emotionClips[i];
            if (entry == null || entry.clip == null)
                continue;

            if (Draggable.LabelsMatch(entry.emotionLabel, emotionLabel))
                return entry.clip;
        }

        Debug.LogWarning("InstructionNarrationPlayer: No narration emotion clip found for " + emotionLabel, this);
        return null;
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void OnDisable()
    {
        StopNarration();
    }

    private void OnValidate()
    {
        gapBetweenClips = Mathf.Max(0f, gapBetweenClips);
    }
}
