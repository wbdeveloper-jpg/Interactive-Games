using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class OddClawCatchResult
{
    public bool caughtSomething;
    public bool missed;
    public bool caughtCorrect;
    public int caughtIndex = -1;
    public OddClawItemView caughtItem;
}

public class OddClawController : MonoBehaviour
{
    [Header("Claw References")]
    public RectTransform clawPivot;
    public RectTransform clawArm;
    public RectTransform clawHead;
    public Image clawHeadImage;
    public RectTransform easyAimGuideLine;

    [Header("Designer Layout Source")]
    [Tooltip("Recommended ON. The placed RectTransform values in the scene are treated as the idle pose. Runtime retracts back to your placed arm height and grabber position, not to hardcoded values.")]
    public bool useSceneRectTransformValues = true;
    [Tooltip("Legacy compatibility. When scene values are used, the grabber Y helps define the visual reach. It no longer forces the arm/head to a hidden hardcoded value.")]
    public bool useGrabberYAsIdleLength = true;
    public bool overrideArmIdleSize = false;
    public Vector2 armIdleSizeOverride = new Vector2(22f, 215f);
    public bool overrideHeadIdlePosition = false;
    public Vector2 headIdleAnchoredPositionOverride = new Vector2(0f, -215f);
    public bool animateArmSizeDuringExtension = true;
    public bool animateHeadPositionDuringExtension = true;

    [Header("Grabbed Object Attach")]
    [Tooltip("Optional socket under the claw head. Move this in the scene to visually place where caught objects should hang.")]
    public RectTransform grabSocket;
    public Vector2 globalGrabbedItemOffset = Vector2.zero;
    public Vector3 globalGrabbedItemRotation = Vector3.zero;
    public float globalGrabbedItemScale = 1f;
    public bool usePerItemGrabOffset = true;

    [Header("Claw Sprites")]
    [Tooltip("Sprite used while rotating, extending, missing, and after retracting.")]
    public Sprite normalClawSprite;
    [Tooltip("Sprite used when an item is actually grabbed. Replace this with your closed/grabbing claw art.")]
    public Sprite grabbingClawSprite;
    public bool useSpriteSwap = true;

    [Header("Rotation")]
    public float minRotationAngle = -55f;
    public float maxRotationAngle = 55f;
    public float rotationSpeed = 70f;
    public float speedIncreasePerWave = 4f;
    public float maxRotationSpeed = 160f;

    [Header("Extend And Catch")]
    [Tooltip("Extra extension from the designer idle pose. The manager can temporarily increase this at runtime so edge objects are reachable.")]
    public float extensionLength = 620f;
    public float extensionDuration = 0.65f;
    public float retractDuration = 0.6f;
    [Tooltip("Catch radius in screen pixels. Only overlapping items can be caught. No nearest-object fallback is used.")]
    public float catchRadius = 58f;
    public bool easyModeAimGuide = true;

    [Header("Catch Feel")]
    [Tooltip("Small pause after the claw tip reaches an item, before the claw closes.")]
    public float holdBeforeGrabDelay = 0.18f;
    [Tooltip("Claw close/sprite swap duration.")]
    public float clawCloseDuration = 0.14f;
    [Tooltip("Small satisfying pop applied to the caught item after attaching.")]
    public float caughtItemPopScale = 1.08f;
    public float caughtItemPopDuration = 0.12f;
    [Tooltip("Small pause after the item is grabbed, before retracting.")]
    public float holdAfterGrabDelay = 0.22f;
    [Tooltip("Small pause after retracting before the manager evaluates correct/wrong/miss.")]
    public float evaluateAfterRetractDelay = 0.16f;

    [Header("Animation")]
    public float clawOpenScale = 1.08f;
    [Tooltip("Compatibility field. Runtime now closes back to the cached base scale instead of shrinking the claw while holding an object.")]
    public float clawCloseScale = 1f;
    public float readyPunchScale = 0.12f;
    public float feedbackDuration = 0.22f;

    [Header("Optional Audio")]
    public OddClawAudioManager audioManager;

    public bool IsBusy { get; private set; }
    public bool IsRotating { get; private set; }
    public float CurrentRotationSpeed { get; private set; }
    public float BaseRotationSpeed => Mathf.Max(0.01f, rotationSpeed);
    public float SpeedMultiplier => CurrentRotationSpeed / BaseRotationSpeed;
    public float CurrentReachLength => _baseReachLength + extensionLength;

    private Vector2 _baseArmSize = new Vector2(22f, 215f);
    private Vector2 _baseHeadAnchoredPosition = new Vector2(0f, -215f);
    private float _baseReachLength = 215f;
    private float _currentReachLength = 215f;
    private float _headExtensionSignY = -1f;
    private float _currentAngle;
    private int _rotateDirection = 1;
    private Vector3 _headBaseScale = Vector3.one;
    private Vector2 _pivotBaseAnchoredPosition;
    private Coroutine _catchRoutine;

    private void Awake()
    {
        if (clawHeadImage == null && clawHead != null)
            clawHeadImage = clawHead.GetComponent<Image>();

        CacheBasePose();
        CurrentRotationSpeed = rotationSpeed;
        ShowNormalClawSprite();
    }

    private void OnEnable()
    {
        IsRotating = true;
        SetEasyGuideVisible(easyModeAimGuide);
    }

    private void Update()
    {
        if (!IsBusy && IsRotating)
            RotateIdle();

        UpdateAimGuide();
    }

    public void CacheBasePose()
    {
        if (overrideArmIdleSize)
            _baseArmSize = armIdleSizeOverride;
        else if (useSceneRectTransformValues && clawArm != null)
            _baseArmSize = clawArm.sizeDelta;

        if (overrideHeadIdlePosition)
            _baseHeadAnchoredPosition = headIdleAnchoredPositionOverride;
        else if (useSceneRectTransformValues && clawHead != null)
            _baseHeadAnchoredPosition = clawHead.anchoredPosition;

        if (clawHead != null)
            _headBaseScale = clawHead.localScale;

        float reachFromHead = Mathf.Abs(_baseHeadAnchoredPosition.y);
        float reachFromArm = Mathf.Abs(_baseArmSize.y);

        if (useGrabberYAsIdleLength && reachFromHead > 10f)
            _baseReachLength = reachFromHead;
        else if (reachFromArm > 10f)
            _baseReachLength = reachFromArm;
        else
            _baseReachLength = Mathf.Max(10f, reachFromHead);

        _headExtensionSignY = _baseHeadAnchoredPosition.y < 0f ? -1f : 1f;
        _currentReachLength = _baseReachLength;

        if (clawPivot != null)
        {
            _pivotBaseAnchoredPosition = clawPivot.anchoredPosition;
            _currentAngle = Mathf.Clamp(NormalizeAngle(clawPivot.localEulerAngles.z), minRotationAngle, maxRotationAngle);
            ApplyRotation();
        }

        ApplyReach(_baseReachLength);
    }

    public void SetWaveDifficulty(int wave)
    {
        int safeWave = Mathf.Max(1, wave);
        CurrentRotationSpeed = Mathf.Min(maxRotationSpeed, rotationSpeed + ((safeWave - 1) * speedIncreasePerWave));
    }

    public void EnsureExtensionLength(float requiredTotalLengthFromPivot)
    {
        float requiredExtension = Mathf.Max(0f, requiredTotalLengthFromPivot - _baseReachLength);
        if (requiredExtension > extensionLength)
            extensionLength = requiredExtension;

        UpdateAimGuide();
    }

    public void SetEasyGuideEnabled(bool enabled)
    {
        easyModeAimGuide = enabled;
        SetEasyGuideVisible(IsRotating && !IsBusy && easyModeAimGuide);
    }

    public void SetInputEnabled(bool enabled)
    {
        IsRotating = enabled;
        SetEasyGuideVisible(enabled && easyModeAimGuide);
    }

    public void ResetClawImmediate()
    {
        if (_catchRoutine != null)
            StopCoroutine(_catchRoutine);

        IsBusy = false;
        IsRotating = true;
        DOTween.Kill(clawPivot);
        DOTween.Kill(clawHead);
        DOTween.Kill(clawArm);

        if (clawPivot != null)
            clawPivot.anchoredPosition = _pivotBaseAnchoredPosition;

        if (clawHead != null)
            clawHead.localScale = _headBaseScale;

        ApplyReach(_baseReachLength);
        ShowNormalClawSprite();
    }

    public void PlayReadyAnimation()
    {
        if (clawPivot == null)
            return;

        clawPivot.DOKill();
        clawPivot.localScale = Vector3.one;
        clawPivot.DOPunchScale(Vector3.one * readyPunchScale, 0.35f, 8, 0.65f);
    }

    public void TryCatch(List<OddClawItemView> items, Canvas canvas, Action<OddClawCatchResult> completed)
    {
        if (IsBusy)
            return;

        _catchRoutine = StartCoroutine(CatchRoutine(items, canvas, completed));
    }

    public void PlaySuccessFeedback()
    {
        if (clawPivot != null)
            clawPivot.DOPunchScale(Vector3.one * 0.14f, feedbackDuration, 8, 0.8f);
    }

    public void PlayWrongFeedback()
    {
        if (clawPivot != null)
            clawPivot.DOShakeAnchorPos(feedbackDuration, 16f, 16, 90f, false, true);
    }

    public void PlayMissFeedback()
    {
        if (clawPivot != null)
            clawPivot.DOShakeAnchorPos(feedbackDuration * 0.8f, 9f, 12, 90f, false, true);
    }

    private IEnumerator CatchRoutine(List<OddClawItemView> items, Canvas canvas, Action<OddClawCatchResult> completed)
    {
        IsBusy = true;
        IsRotating = false;
        SetEasyGuideVisible(false);
        ShowNormalClawSprite();

        OddClawCatchResult result = new OddClawCatchResult();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

        if (clawHead != null)
        {
            clawHead.DOKill();
            clawHead.DOScale(_headBaseScale * clawOpenScale, 0.08f).SetEase(Ease.OutBack);
        }

        if (audioManager != null)
            audioManager.PlayClawExtend();

        float startReach = _baseReachLength;
        float targetReach = _baseReachLength + extensionLength;
        float elapsed = 0f;

        while (elapsed < extensionDuration)
        {
            elapsed += Time.deltaTime;
            float t = extensionDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / extensionDuration);
            float eased = DOVirtual.EasedValue(0f, 1f, t, Ease.OutQuad);
            ApplyReach(Mathf.Lerp(startReach, targetReach, eased));

            if (!result.caughtSomething)
                TryFindOverlap(items, uiCamera, result);

            if (result.caughtSomething)
                break;

            yield return null;
        }

        if (!result.caughtSomething)
            ApplyReach(targetReach);

        if (result.caughtSomething && result.caughtItem != null)
        {
            if (holdBeforeGrabDelay > 0f)
                yield return new WaitForSeconds(holdBeforeGrabDelay);

            ShowGrabbingClawSprite();
            AttachCaughtItem(result.caughtItem);

            if (clawHead != null)
                clawHead.DOScale(_headBaseScale, clawCloseDuration).SetEase(Ease.OutBack);

            result.caughtItem.PlayCaughtPop(caughtItemPopScale, caughtItemPopDuration);

            if (audioManager != null)
                audioManager.PlayClawGrab();

            float closeWait = Mathf.Max(clawCloseDuration, caughtItemPopDuration);
            if (closeWait > 0f)
                yield return new WaitForSeconds(closeWait);

            if (holdAfterGrabDelay > 0f)
                yield return new WaitForSeconds(holdAfterGrabDelay);
        }

        if (audioManager != null)
            audioManager.PlayClawRetract();

        float retractStart = _currentReachLength;
        elapsed = 0f;
        while (elapsed < retractDuration)
        {
            elapsed += Time.deltaTime;
            float t = retractDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / retractDuration);
            float eased = DOVirtual.EasedValue(0f, 1f, t, Ease.InOutQuad);
            ApplyReach(Mathf.Lerp(retractStart, _baseReachLength, eased));
            yield return null;
        }

        ApplyReach(_baseReachLength);

        if (clawHead != null)
            clawHead.DOScale(_headBaseScale, 0.1f).SetEase(Ease.OutBack);

        if (evaluateAfterRetractDelay > 0f)
            yield return new WaitForSeconds(evaluateAfterRetractDelay);

        ShowNormalClawSprite();
        result.missed = !result.caughtSomething;
        IsBusy = false;

        // Evaluation is manager-owned. Do not resume rotation/input here, otherwise
        // the claw starts moving while the Correct/Wrong/Miss popup and item fade are still playing.
        IsRotating = false;
        SetEasyGuideVisible(false);
        completed?.Invoke(result);
    }

    private void AttachCaughtItem(OddClawItemView item)
    {
        if (item == null)
            return;

        Transform attachParent = grabSocket != null ? grabSocket : clawHead;
        if (attachParent == null)
            return;

        Vector2 finalOffset = globalGrabbedItemOffset;
        Vector3 finalRotation = globalGrabbedItemRotation;
        float finalScale = Mathf.Max(0.01f, globalGrabbedItemScale);

        if (usePerItemGrabOffset)
        {
            finalOffset += item.GrabbedLocalOffset;
            finalRotation += item.GrabbedLocalRotation;
            finalScale *= item.GrabbedLocalScale;
        }

        item.MarkCaught(attachParent, finalOffset, finalRotation, finalScale);
    }

    private void TryFindOverlap(List<OddClawItemView> items, Camera uiCamera, OddClawCatchResult result)
    {
        if (items == null || clawHead == null)
            return;

        Vector2 tipScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, clawHead.position);

        for (int i = 0; i < items.Count; i++)
        {
            OddClawItemView item = items[i];
            if (item == null || item.IsCaught)
                continue;

            if (!item.OverlapsScreenCircle(tipScreen, catchRadius, uiCamera))
                continue;

            result.caughtSomething = true;
            result.caughtItem = item;
            result.caughtIndex = item.Index;
            result.caughtCorrect = item.IsCorrect;
            return;
        }
    }

    private void RotateIdle()
    {
        if (clawPivot == null)
            return;

        _currentAngle += _rotateDirection * CurrentRotationSpeed * Time.deltaTime;

        if (_currentAngle >= maxRotationAngle)
        {
            _currentAngle = maxRotationAngle;
            _rotateDirection = -1;
        }
        else if (_currentAngle <= minRotationAngle)
        {
            _currentAngle = minRotationAngle;
            _rotateDirection = 1;
        }

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (clawPivot != null)
            clawPivot.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
    }

    private void ApplyReach(float totalReachLength)
    {
        _currentReachLength = Mathf.Max(10f, totalReachLength);
        float extraExtension = Mathf.Max(0f, _currentReachLength - _baseReachLength);

        if (clawArm != null && animateArmSizeDuringExtension)
        {
            Vector2 size = _baseArmSize;
            size.y = Mathf.Sign(size.y == 0f ? 1f : size.y) * (Mathf.Abs(_baseArmSize.y) + extraExtension);
            clawArm.sizeDelta = size;
        }

        if (clawHead != null && animateHeadPositionDuringExtension)
            clawHead.anchoredPosition = _baseHeadAnchoredPosition + new Vector2(0f, _headExtensionSignY * extraExtension);
    }

    private void UpdateAimGuide()
    {
        if (easyAimGuideLine == null)
            return;

        if (!easyAimGuideLine.gameObject.activeSelf)
            return;

        Vector2 size = easyAimGuideLine.sizeDelta;
        size.y = _baseReachLength + extensionLength;
        easyAimGuideLine.sizeDelta = size;
        easyAimGuideLine.anchoredPosition = new Vector2(easyAimGuideLine.anchoredPosition.x, -size.y * 0.5f);
    }

    private void SetEasyGuideVisible(bool visible)
    {
        if (easyAimGuideLine != null)
            easyAimGuideLine.gameObject.SetActive(visible && easyModeAimGuide);
    }

    private void ShowNormalClawSprite()
    {
        if (!useSpriteSwap || clawHeadImage == null || normalClawSprite == null)
            return;

        clawHeadImage.sprite = normalClawSprite;
        clawHeadImage.preserveAspect = true;
    }

    private void ShowGrabbingClawSprite()
    {
        if (!useSpriteSwap || clawHeadImage == null || grabbingClawSprite == null)
            return;

        clawHeadImage.sprite = grabbingClawSprite;
        clawHeadImage.preserveAspect = true;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;
        while (angle < -180f)
            angle += 360f;
        return angle;
    }
}
