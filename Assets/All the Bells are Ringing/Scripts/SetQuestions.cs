using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SetQuestions : MonoBehaviour
{
    public List<Draggable> AllDraggable;
    public Transform placeHolderParent;
    public TextMeshProUGUI instructionText;

    [Header("Spawn Settings")]
    [Min(0f)] public float spawnScaleDuration = 0.35f;
    [Min(0f)] public float delayBetweenSpawns = 0.12f;
    public int spawnSfxIndex = 1;

    private static readonly float[] RequiredIntensities = { 0.2f, 0.4f, 0.6f, 0.8f, 1f };
    private readonly List<Transform> placeholders = new List<Transform>();
    private Coroutine fillRoutine;

    private void OnEnable()
    {
        if (fillRoutine != null)
            StopCoroutine(fillRoutine);
        AudioManager.Instance.PlayBGM(0);
        fillRoutine = StartCoroutine(FillAllPlaceholdersSequential());

        if (instructionText != null)
        {
            instructionText.DOKill(false);
            instructionText.alpha = 0f;
            instructionText.DOFade(1f, 1f).SetEase(Ease.InOutSine).SetLink(instructionText.gameObject);
        }
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
        for (int i = 0; i < RequiredIntensities.Length && index < placeholders.Count; i++)
        {
            Draggable prefab = GetRandomDraggableByIntensity(RequiredIntensities[i]);
            if (prefab == null)
                continue;

            yield return SpawnWithAnimation(prefab, placeholders[index]);
            index++;
        }

        while (index < placeholders.Count)
        {
            Draggable prefab = GetRandomDraggable();
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

        return true;
    }

    private void CacheAndShufflePlaceholders()
    {
        placeholders.Clear();
        for (int i = 0; i < placeHolderParent.childCount; i++)
            placeholders.Add(placeHolderParent.GetChild(i));

        Shuffle(placeholders);
    }

    private Draggable GetRandomDraggableByIntensity(float intensity)
    {
        List<Draggable> candidates = new List<Draggable>();
        for (int i = 0; i < AllDraggable.Count; i++)
        {
            Draggable item = AllDraggable[i];
            if (item != null && Mathf.Approximately(item.intensity, intensity))
                candidates.Add(item);
        }

        return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
    }

    private Draggable GetRandomDraggable()
    {
        for (int attempts = 0; attempts < AllDraggable.Count; attempts++)
        {
            Draggable item = AllDraggable[Random.Range(0, AllDraggable.Count)];
            if (item != null)
                return item;
        }

        return null;
    }

    private static void ClearPlaceholder(Transform placeholder)
    {
        for (int i = placeholder.childCount - 1; i >= 0; i--)
        {
            Transform child = placeholder.GetChild(i);
            child.DOKill(false);
            Destroy(child.gameObject);
        }
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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
}
