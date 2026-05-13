using DG.Tweening;
using RewardSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingPage : MonoBehaviour
{
    [Header("Timing")]
    [Min(0f)] public float duration = 5f;
    [Min(0f)] public float startDelay = 2f;
    public bool useUnscaledTime = false;

    [Header("UI References")]
    public RectTransform barRect;
    public Image barFill;
    public RectTransform smileyRect;
    public Image smileyImage;

    [Header("Smiley Sprites (5 levels)")]
    [Tooltip("Assign exactly 5 sprites for intensities: 0.2, 0.4, 0.6, 0.8, 1.0")]
    public Sprite[] smileyLevelSprites = new Sprite[5];

    [Header("Motion")]
    public bool rotateDuring = true;
    [Min(0f)] public float rotateSpeedDegrees = 360f;

    [Tooltip("Preserved original field name to avoid breaking existing Inspector references. Use it as smiley X offset.")]
    public float specialTheshold;

    [Header("Orientation")]
    public bool forceLandscapeLeft = true;

    [Header("Reward Skills")]
    public List<SkillEntry> _skills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: 0.1f, accuracyWeight: 0.9f),
        new SkillEntry(BloomSkillType.Understand, 50f, timeWeight: 0.5f, accuracyWeight: 0.5f),
    };

    [Header("Events")]
    public UnityEvent onComplete;

    private static readonly float[] Thresholds = { 0.2f, 0.4f, 0.6f, 0.8f, 1f };

    private Sequence loadingSequence;
    private Coroutine loadingRoutine;
    private float currentProgress;
    private float leftX;
    private float rightX;
    private bool warnedBarFillType;

    private void Reset()
    {
        AutoAssignSmileyReferences();
    }

    private void Awake()
    {
        AutoAssignSmileyReferences();
    }

    private void Start()
    {
        if (forceLandscapeLeft && Screen.orientation != ScreenOrientation.LandscapeLeft)
            Screen.orientation = ScreenOrientation.LandscapeLeft;

        if (!ValidateReferences())
            return;

        StartLoading();
    }

    public void StartLoading()
    {
        StopLoading();

        if (RewardManager.Instance != null)
            RewardManager.Instance.ShowPreGame(_skills);

        loadingRoutine = StartCoroutine(AnimateLoadingRoutine());
    }

    public void StopLoading()
    {
        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
            loadingRoutine = null;
        }

        StopActiveTweens(false);
    }

    private IEnumerator AnimateLoadingRoutine()
    {
        currentProgress = 0f;
        CalculateBarPositions();
        ApplyProgress(0f);
        ResetSmileyVisuals();

        if (RewardManager.Instance != null)
            yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);

        if (startDelay > 0f)
            yield return useUnscaledTime ? WaitRealtime(startDelay) : new WaitForSeconds(startDelay);

        loadingSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        Tween progressTween = DOTween.To(
                () => currentProgress,
                ApplyProgress,
                1f,
                Mathf.Max(0.0001f, duration))
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime);

        loadingSequence.Join(progressTween);

        if (rotateDuring && smileyRect != null && duration > 0f)
        {
            float totalRotation = rotateSpeedDegrees * duration;
            loadingSequence.Join(
                smileyRect.DOLocalRotate(new Vector3(0f, 0f, -totalRotation), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetUpdate(useUnscaledTime));
        }

        if (smileyRect != null)
            loadingSequence.Append(smileyRect.DOPunchScale(Vector3.one * 0.12f, 0.2f, 1, 0.5f).SetUpdate(useUnscaledTime));

        loadingSequence.OnComplete(CompleteLoading);
        yield return loadingSequence.WaitForCompletion();
        loadingRoutine = null;
    }

    private static IEnumerator WaitRealtime(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
    }

    private void CompleteLoading()
    {
        ApplyProgress(1f);

        if (smileyRect != null)
        {
            Vector3 local = smileyRect.localPosition;
            local.x = rightX;
            smileyRect.localPosition = local;
            smileyRect.localEulerAngles = Vector3.zero;
            smileyRect.localScale = Vector3.one;
        }

        Debug.Log("LoadingPage: animation complete.", this);
        onComplete?.Invoke();
    }

    private void ApplyProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);

        if (barFill != null)
        {
            if (barFill.type == Image.Type.Filled)
                barFill.fillAmount = currentProgress;
            else if (!warnedBarFillType)
            {
                warnedBarFillType = true;
                Debug.LogWarning("LoadingPage: barFill Image Type should be Filled for fillAmount to work.", barFill);
            }
        }

        if (smileyRect != null)
        {
            float x = Mathf.Lerp(leftX, rightX, currentProgress);
            Vector3 local = smileyRect.localPosition;
            local.x = x;
            smileyRect.localPosition = local;
        }

        UpdateSmileySprite(currentProgress);
    }

    private void ResetSmileyVisuals()
    {
        if (smileyRect == null)
            return;

        Vector3 local = smileyRect.localPosition;
        local.x = leftX;
        smileyRect.localPosition = local;
        smileyRect.localEulerAngles = Vector3.zero;
        smileyRect.localScale = Vector3.one;
    }

    private void CalculateBarPositions()
    {
        if (barRect == null || smileyRect == null)
            return;

        RectTransform smileyParent = smileyRect.parent as RectTransform;
        if (smileyParent == null)
        {
            Debug.LogWarning("LoadingPage: smileyRect parent must be a RectTransform.", this);
            return;
        }

        Vector3 leftWorld = barRect.TransformPoint(new Vector3(barRect.rect.xMin, barRect.rect.center.y, 0f));
        Vector3 rightWorld = barRect.TransformPoint(new Vector3(barRect.rect.xMax, barRect.rect.center.y, 0f));

        leftX = smileyParent.InverseTransformPoint(leftWorld).x + specialTheshold;
        rightX = smileyParent.InverseTransformPoint(rightWorld).x + specialTheshold;
    }

    private void UpdateSmileySprite(float progress)
    {
        if (smileyImage == null || smileyLevelSprites == null || smileyLevelSprites.Length < Thresholds.Length)
            return;

        for (int i = 0; i < Thresholds.Length; i++)
        {
            if (progress <= Thresholds[i])
            {
                Sprite nextSprite = smileyLevelSprites[i];
                if (nextSprite != null && smileyImage.sprite != nextSprite)
                    smileyImage.sprite = nextSprite;
                return;
            }
        }

        Sprite lastSprite = smileyLevelSprites[smileyLevelSprites.Length - 1];
        if (lastSprite != null)
            smileyImage.sprite = lastSprite;
    }

    private void AutoAssignSmileyReferences()
    {
        if (smileyImage == null && smileyRect != null)
            smileyImage = smileyRect.GetComponent<Image>();

        if (smileyRect == null)
        {
            Image childImage = GetComponentInChildren<Image>();
            if (childImage != null)
            {
                smileyImage = childImage;
                smileyRect = childImage.rectTransform;
            }
        }
    }

    private bool ValidateReferences()
    {
        bool valid = true;

        if (barRect == null)
        {
            Debug.LogWarning("LoadingPage: barRect is not assigned. Smiley movement will not work.", this);
            valid = false;
        }

        if (smileyRect == null || smileyImage == null)
        {
            Debug.LogWarning("LoadingPage: smileyRect or smileyImage is not assigned.", this);
            valid = false;
        }

        if (smileyLevelSprites == null || smileyLevelSprites.Length < Thresholds.Length)
            Debug.LogWarning("LoadingPage: assign 5 smiley sprites for intensities 0.2, 0.4, 0.6, 0.8, 1.0.", this);

        return valid;
    }

    private void StopActiveTweens(bool complete)
    {
        if (loadingSequence != null && loadingSequence.IsActive())
            loadingSequence.Kill(complete);

        loadingSequence = null;
        smileyRect?.DOKill(false);
    }

    private void OnDisable()
    {
        StopLoading();
    }

    private void OnDestroy()
    {
        StopLoading();
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0f, duration);
        startDelay = Mathf.Max(0f, startDelay);
        rotateSpeedDegrees = Mathf.Max(0f, rotateSpeedDegrees);
    }
}
