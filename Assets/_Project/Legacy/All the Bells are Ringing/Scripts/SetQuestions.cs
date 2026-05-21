using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SetQuestions : MonoBehaviour
{
    [Header("Question Data")]
    public List<Draggable> AllDraggable;
    public Transform placeHolderParent;
    public TextMeshProUGUI instructionText;

    [Header("Spawn Settings")]
    [Min(0f)] public float spawnScaleDuration = 0.35f;
    [Min(0f)] public float delayBetweenSpawns = 0.12f;
    public int spawnSfxIndex = 1;

    [Header("Gameplay Audio")]
    [SerializeField] private bool playBgmOnEnable = true;
    [SerializeField] private int gameplayBgmIndex = 0;

    [Header("Emotion Lock")]
    [Tooltip("If true, one emotion is selected for the full game turn.")]
    [SerializeField] private bool lockOneEmotionPerGame = true;

    [Tooltip("If true, target emotion should have all required intensities: 0.2, 0.4, 0.6, 0.8, 1.0.")]
    [SerializeField] private bool preferEmotionWithCompleteIntensitySet = true;

    [Tooltip("Runtime selected target emotion. Visible for debugging.")]
    [SerializeField] private string selectedTargetEmotionLabel;

    [Header("Instruction Text")]
    [SerializeField] private bool updateInstructionWithTargetEmotion = true;

    [SerializeField]
    private string lockedEmotionInstructionFormat = "Drag the {0} emoji that feels the same.";

    [SerializeField]
    private string defaultInstructionText = "Drag the emoji that feels the same.";

    [Header("Mood Repeat Prevention")]
    [Tooltip("Prevents recently selected moods from being selected again, when alternatives exist.")]
    [SerializeField] private bool avoidRecentMoods = true;

    [Tooltip("How many previous moods should be avoided. Recommended: 1 or 2.")]
    [Min(1)]
    [SerializeField] private int recentMoodMemoryCount = 2;

    [SerializeField] private string recentMoodPrefsKey = "EmotionGame_RecentMoods";

    public string SelectedTargetEmotionLabel => selectedTargetEmotionLabel;

    public string CurrentInstructionText { get; private set; }

    public bool IsEmotionLocked =>
        lockOneEmotionPerGame && !string.IsNullOrWhiteSpace(selectedTargetEmotionLabel);

    private static readonly float[] RequiredIntensities = { 0.2f, 0.4f, 0.6f, 0.8f, 1f };

    private readonly List<Transform> placeholders = new List<Transform>();
    private Coroutine fillRoutine;

    private void OnEnable()
    {
        if (fillRoutine != null)
            StopCoroutine(fillRoutine);

        ChooseTargetEmotion();
        ApplyInstructionText();

        if (playBgmOnEnable && AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(gameplayBgmIndex);

        fillRoutine = StartCoroutine(FillAllPlaceholdersSequential());

        FadeInstructionText();
    }

    private IEnumerator FillAllPlaceholdersSequential()
    {
        if (!HasValidSetup())
        {
            fillRoutine = null;
            yield break;
        }

        CacheAndShufflePlaceholders();

        int index = 0;

        // First, guarantee correct-answer candidates:
        // selected emotion + each required intensity.
        for (int i = 0; i < RequiredIntensities.Length && index < placeholders.Count; i++)
        {
            float requiredIntensity = RequiredIntensities[i];

            Draggable prefab = IsEmotionLocked
                ? GetRandomDraggableByEmotionAndIntensity(selectedTargetEmotionLabel, requiredIntensity)
                : GetRandomDraggableByIntensity(requiredIntensity);

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"SetQuestions: Missing prefab for emotion '{selectedTargetEmotionLabel}' and intensity {requiredIntensity}.",
                    this
                );
                continue;
            }

            yield return SpawnWithAnimation(prefab, placeholders[index]);
            index++;
        }

        // Then fill extra slots with fodder.
        // Prefer different emotions as fodder so the selected emotion is clearer.
        while (index < placeholders.Count)
        {
            Draggable prefab = IsEmotionLocked
                ? GetRandomFodderDraggable()
                : GetRandomDraggable();

            if (prefab == null)
                break;

            yield return SpawnWithAnimation(prefab, placeholders[index]);
            index++;
        }

        fillRoutine = null;
    }

    private IEnumerator SpawnWithAnimation(Draggable prefab, Transform placeholder)
    {
        if (prefab == null || placeholder == null)
            yield break;

        ClearPlaceholder(placeholder);

        Draggable instance = Instantiate(prefab, placeholder);
        Transform instanceTransform = instance.transform;

        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instanceTransform.localScale = Vector3.zero;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(spawnSfxIndex);

        instanceTransform
            .DOScale(1f, spawnScaleDuration)
            .SetEase(Ease.OutBack, 1.5f)
            .SetLink(instance.gameObject);

        if (delayBetweenSpawns > 0f)
            yield return new WaitForSeconds(delayBetweenSpawns);
    }

    private void ChooseTargetEmotion()
    {
        selectedTargetEmotionLabel = string.Empty;

        if (!lockOneEmotionPerGame)
            return;

        if (AllDraggable == null || AllDraggable.Count == 0)
            return;

        List<string> allLabels = GetUniqueEmotionLabels();

        if (allLabels.Count == 0)
        {
            Debug.LogWarning("SetQuestions: No valid emotion labels found in AllDraggable.", this);
            return;
        }

        List<string> selectableLabels = new List<string>();

        if (preferEmotionWithCompleteIntensitySet)
        {
            for (int i = 0; i < allLabels.Count; i++)
            {
                if (HasCompleteIntensitySet(allLabels[i]))
                    selectableLabels.Add(allLabels[i]);
            }
        }

        // If no emotion has a full set, fallback to all available labels
        // instead of breaking the game.
        if (selectableLabels.Count == 0)
            selectableLabels.AddRange(allLabels);

        RemoveRecentMoodsFromCandidates(selectableLabels);

        selectedTargetEmotionLabel = selectableLabels[Random.Range(0, selectableLabels.Count)];

        SaveRecentMood(selectedTargetEmotionLabel);

        Debug.Log("SetQuestions: Target emotion selected = " + selectedTargetEmotionLabel, this);
    }

    private List<string> GetUniqueEmotionLabels()
    {
        List<string> labels = new List<string>();

        if (AllDraggable == null)
            return labels;

        for (int i = 0; i < AllDraggable.Count; i++)
        {
            Draggable item = AllDraggable[i];

            if (item == null)
                continue;

            string label = NormalizeLabel(item.label);

            if (string.IsNullOrEmpty(label))
                continue;

            bool alreadyAdded = false;

            for (int j = 0; j < labels.Count; j++)
            {
                if (LabelsMatch(labels[j], label))
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
                labels.Add(label);
        }

        return labels;
    }

    private bool HasCompleteIntensitySet(string emotionLabel)
    {
        for (int i = 0; i < RequiredIntensities.Length; i++)
        {
            if (GetRandomDraggableByEmotionAndIntensity(emotionLabel, RequiredIntensities[i]) == null)
                return false;
        }

        return true;
    }

    private Draggable GetRandomDraggableByEmotionAndIntensity(string emotionLabel, float intensity)
    {
        List<Draggable> candidates = new List<Draggable>();

        for (int i = 0; i < AllDraggable.Count; i++)
        {
            Draggable item = AllDraggable[i];

            if (item == null)
                continue;

            bool emotionMatches = LabelsMatch(item.label, emotionLabel);
            bool intensityMatches = Mathf.Approximately(
                NormalizeIntensity(item.intensity),
                NormalizeIntensity(intensity)
            );

            if (emotionMatches && intensityMatches)
                candidates.Add(item);
        }

        return candidates.Count == 0
            ? null
            : candidates[Random.Range(0, candidates.Count)];
    }

    private Draggable GetRandomDraggableByIntensity(float intensity)
    {
        List<Draggable> candidates = new List<Draggable>();

        for (int i = 0; i < AllDraggable.Count; i++)
        {
            Draggable item = AllDraggable[i];

            if (item == null)
                continue;

            if (Mathf.Approximately(NormalizeIntensity(item.intensity), NormalizeIntensity(intensity)))
                candidates.Add(item);
        }

        return candidates.Count == 0
            ? null
            : candidates[Random.Range(0, candidates.Count)];
    }

    private Draggable GetRandomFodderDraggable()
    {
        List<Draggable> candidates = new List<Draggable>();

        // Prefer fodder from a different emotion.
        for (int i = 0; i < AllDraggable.Count; i++)
        {
            Draggable item = AllDraggable[i];

            if (item == null)
                continue;

            if (!LabelsMatch(item.label, selectedTargetEmotionLabel))
                candidates.Add(item);
        }

        if (candidates.Count > 0)
            return candidates[Random.Range(0, candidates.Count)];

        // Fallback: if only one emotion exists, still fill placeholders.
        return GetRandomDraggable();
    }

    private Draggable GetRandomDraggable()
    {
        if (AllDraggable == null || AllDraggable.Count == 0)
            return null;

        for (int attempts = 0; attempts < AllDraggable.Count; attempts++)
        {
            Draggable item = AllDraggable[Random.Range(0, AllDraggable.Count)];

            if (item != null)
                return item;
        }

        return null;
    }

    private void ApplyInstructionText()
    {
        CurrentInstructionText = BuildInstructionText();

        if (instructionText != null)
            instructionText.text = CurrentInstructionText;
    }

    private string BuildInstructionText()
    {
        if (!updateInstructionWithTargetEmotion)
            return defaultInstructionText;

        if (string.IsNullOrWhiteSpace(selectedTargetEmotionLabel))
            return defaultInstructionText;

        string displayName = ToDisplayName(selectedTargetEmotionLabel);
        return string.Format(lockedEmotionInstructionFormat, displayName);
    }

    private void FadeInstructionText()
    {
        if (instructionText == null)
            return;

        instructionText.DOKill(false);
        instructionText.alpha = 0f;

        instructionText
            .DOFade(1f, 1f)
            .SetEase(Ease.InOutSine)
            .SetLink(instructionText.gameObject);
    }

    private void CacheAndShufflePlaceholders()
    {
        placeholders.Clear();

        if (placeHolderParent == null)
            return;

        for (int i = 0; i < placeHolderParent.childCount; i++)
            placeholders.Add(placeHolderParent.GetChild(i));

        Shuffle(placeholders);
    }

    private bool HasValidSetup()
    {
        if (placeHolderParent == null)
        {
            Debug.LogWarning("SetQuestions: placeHolderParent is not assigned.", this);
            return false;
        }

        if (AllDraggable == null || AllDraggable.Count == 0)
        {
            Debug.LogWarning("SetQuestions: AllDraggable is empty.", this);
            return false;
        }

        if (placeHolderParent.childCount == 0)
        {
            Debug.LogWarning("SetQuestions: placeHolderParent has no child placeholders.", this);
            return false;
        }

        if (lockOneEmotionPerGame && string.IsNullOrWhiteSpace(selectedTargetEmotionLabel))
        {
            Debug.LogWarning("SetQuestions: target emotion could not be selected. Check Draggable labels.", this);
            return false;
        }

        return true;
    }

    private List<string> GetRecentMoods()
    {
        string saved = PlayerPrefs.GetString(recentMoodPrefsKey, string.Empty);
        List<string> moods = new List<string>();

        if (string.IsNullOrWhiteSpace(saved))
            return moods;

        string[] parts = saved.Split('|');

        for (int i = 0; i < parts.Length; i++)
        {
            string mood = NormalizeLabel(parts[i]);

            if (!string.IsNullOrEmpty(mood))
                moods.Add(mood);
        }

        return moods;
    }

    private void SaveRecentMood(string mood)
    {
        mood = NormalizeLabel(mood);

        if (string.IsNullOrEmpty(mood))
            return;

        List<string> moods = GetRecentMoods();

        moods.RemoveAll(existing => LabelsMatch(existing, mood));
        moods.Insert(0, mood);

        int maxCount = Mathf.Max(1, recentMoodMemoryCount);

        while (moods.Count > maxCount)
            moods.RemoveAt(moods.Count - 1);

        PlayerPrefs.SetString(recentMoodPrefsKey, string.Join("|", moods));
        PlayerPrefs.Save();
    }

    private void RemoveRecentMoodsFromCandidates(List<string> candidates)
    {
        if (!avoidRecentMoods)
            return;

        if (candidates == null || candidates.Count <= 1)
            return;

        List<string> recentMoods = GetRecentMoods();

        if (recentMoods.Count == 0)
            return;

        List<string> filtered = new List<string>();

        for (int i = 0; i < candidates.Count; i++)
        {
            bool isRecent = false;

            for (int j = 0; j < recentMoods.Count; j++)
            {
                if (LabelsMatch(candidates[i], recentMoods[j]))
                {
                    isRecent = true;
                    break;
                }
            }

            if (!isRecent)
                filtered.Add(candidates[i]);
        }

        // Important:
        // If filtering removes everything, keep original candidates.
        // This prevents soft-lock when only 1 or 2 moods exist.
        if (filtered.Count == 0)
            return;

        candidates.Clear();
        candidates.AddRange(filtered);
    }

    public void ResetMoodHistory()
    {
        PlayerPrefs.DeleteKey(recentMoodPrefsKey);
        PlayerPrefs.Save();

        Debug.Log("SetQuestions: mood history reset.", this);
    }

    private static void ClearPlaceholder(Transform placeholder)
    {
        if (placeholder == null)
            return;

        for (int i = placeholder.childCount - 1; i >= 0; i--)
        {
            Transform child = placeholder.GetChild(i);
            child.DOKill(false);
            Destroy(child.gameObject);
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        if (list == null)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static bool LabelsMatch(string a, string b)
    {
        return string.Equals(
            NormalizeLabel(a),
            NormalizeLabel(b),
            System.StringComparison.OrdinalIgnoreCase
        );
    }

    private static string NormalizeLabel(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
    }

    private static string ToDisplayName(string value)
    {
        value = NormalizeLabel(value);

        if (string.IsNullOrEmpty(value))
            return string.Empty;

        value = value.Replace("_", " ").Replace("-", " ");

        if (value.Length == 1)
            return value.ToUpper();

        return char.ToUpper(value[0]) + value.Substring(1);
    }

    private static float NormalizeIntensity(float value)
    {
        int step = Mathf.RoundToInt(Mathf.Clamp01(value) / 0.2f);
        step = Mathf.Clamp(step, 1, 5);
        return step * 0.2f;
    }

    private void OnDisable()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (instructionText != null)
            instructionText.DOKill(false);
    }

    private void OnValidate()
    {
        spawnScaleDuration = Mathf.Max(0f, spawnScaleDuration);
        delayBetweenSpawns = Mathf.Max(0f, delayBetweenSpawns);
        recentMoodMemoryCount = Mathf.Max(1, recentMoodMemoryCount);

        if (string.IsNullOrWhiteSpace(lockedEmotionInstructionFormat))
            lockedEmotionInstructionFormat = "Drag the {0} emoji that feels the same.";

        if (string.IsNullOrWhiteSpace(defaultInstructionText))
            defaultInstructionText = "Drag the emoji that feels the same.";

        if (string.IsNullOrWhiteSpace(recentMoodPrefsKey))
            recentMoodPrefsKey = "EmotionGame_RecentMoods";
    }
}