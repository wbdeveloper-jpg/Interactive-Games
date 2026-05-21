using DG.Tweening;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryCardAnimator : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Recommended: assign a child visual RectTransform, not the GridLayout child root. If empty, this object's RectTransform is used.")]
        [SerializeField] private RectTransform animatedRoot;

        [Header("Flip")]
        [SerializeField, Min(0.05f)] private float flipDuration = 0.28f;
        [SerializeField] private Ease flipEaseIn = Ease.InSine;
        [SerializeField] private Ease flipEaseOut = Ease.OutSine;

        [Header("Correct Feedback")]
        [SerializeField, Min(0.05f)] private float correctPulseDuration = 0.22f;
        [SerializeField, Min(1f)] private float correctPulseScale = 1.08f;
        [SerializeField] private Ease correctPulseEase = Ease.OutBack;

        [Header("Hint Feedback")]
        [SerializeField, Min(0.05f)] private float hintPulseDuration = 0.35f;
        [SerializeField, Min(1f)] private float hintPulseScale = 1.08f;
        [SerializeField] private Ease hintPulseEase = Ease.InOutSine;

        [Header("Wrong Feedback")]
        [SerializeField, Min(0.05f)] private float wrongShakeDuration = 0.28f;
        [SerializeField, Min(1f)] private float wrongShakeStrength = 16f;
        [SerializeField, Min(1)] private int wrongShakeVibrato = 12;

        [Header("Update")]
        [SerializeField] private bool useUnscaledTime = false;

        private Tween activeFlipTween;
        private Tween activeFeedbackTween;

        private Vector3 originalScale;
        private Vector2 lastSafeAnchoredPosition;

        public bool IsAnimating =>
            activeFlipTween != null && activeFlipTween.IsActive() && activeFlipTween.IsPlaying();

        private void Awake()
        {
            CacheTarget();
            CaptureOriginalScale();
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void SetFaceImmediate(GameObject frontRoot, GameObject backRoot, bool showFront)
        {
            KillFlipTween();
            CacheTarget();

            if (animatedRoot != null)
            {
                animatedRoot.localEulerAngles = Vector3.zero;
            }

            SetRoots(frontRoot, backRoot, showFront);
        }

        public void FlipTo(GameObject frontRoot, GameObject backRoot, bool showFront)
        {
            CacheTarget();

            if (animatedRoot == null)
            {
                SetRoots(frontRoot, backRoot, showFront);
                return;
            }

            KillFlipTween();

            float halfDuration = Mathf.Max(0.025f, flipDuration * 0.5f);

            Sequence sequence = DOTween.Sequence();
            sequence.SetUpdate(useUnscaledTime);

            sequence.Append(animatedRoot
                .DOLocalRotate(new Vector3(0f, 90f, 0f), halfDuration, RotateMode.Fast)
                .SetEase(flipEaseIn));

            sequence.AppendCallback(() =>
            {
                SetRoots(frontRoot, backRoot, showFront);
                animatedRoot.localEulerAngles = new Vector3(0f, -90f, 0f);
            });

            sequence.Append(animatedRoot
                .DOLocalRotate(Vector3.zero, halfDuration, RotateMode.Fast)
                .SetEase(flipEaseOut));

            activeFlipTween = sequence;
        }

        public void PlayCorrectPulse()
        {
            CacheTarget();

            if (animatedRoot == null)
            {
                return;
            }

            KillFeedbackTween();

            activeFeedbackTween = animatedRoot
                .DOScale(originalScale * correctPulseScale, correctPulseDuration)
                .SetEase(correctPulseEase)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(useUnscaledTime)
                .OnKill(RestoreScale);
        }

        public void PlayHintPulse()
        {
            CacheTarget();

            if (animatedRoot == null)
            {
                return;
            }

            KillFeedbackTween();

            activeFeedbackTween = animatedRoot
                .DOScale(originalScale * hintPulseScale, hintPulseDuration)
                .SetEase(hintPulseEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(useUnscaledTime);
        }

        public void StopHintPulse()
        {
            KillFeedbackTween();
            RestoreScale();
        }

        public void PlayWrongShake()
        {
            CacheTarget();

            if (animatedRoot == null)
            {
                return;
            }

            KillFeedbackTween();

            /*
             * Important:
             * Do NOT restore to an Awake-time anchoredPosition.
             * In a GridLayoutGroup, the card's final anchoredPosition is assigned later by layout.
             * Capturing the position in Awake can be 0,0, which makes wrong cards jump to the corner.
             *
             * Capture the current layout position immediately before the shake,
             * then restore to that exact runtime position after the shake.
             */
            lastSafeAnchoredPosition = animatedRoot.anchoredPosition;

            activeFeedbackTween = animatedRoot
                .DOPunchAnchorPos(
                    new Vector2(wrongShakeStrength, 0f),
                    wrongShakeDuration,
                    wrongShakeVibrato,
                    0.65f)
                .SetUpdate(useUnscaledTime)
                .OnKill(RestoreAnchoredPosition)
                .OnComplete(RestoreAnchoredPosition);
        }

        public void KillTweens()
        {
            KillFlipTween();
            KillFeedbackTween();
        }

        private void KillFlipTween()
        {
            if (activeFlipTween != null && activeFlipTween.IsActive())
            {
                activeFlipTween.Kill();
            }

            activeFlipTween = null;
        }

        private void KillFeedbackTween()
        {
            if (activeFeedbackTween != null && activeFeedbackTween.IsActive())
            {
                activeFeedbackTween.Kill();
            }

            activeFeedbackTween = null;
        }

        private void CacheTarget()
        {
            if (animatedRoot == null)
            {
                animatedRoot = transform as RectTransform;
            }
        }

        private void CaptureOriginalScale()
        {
            CacheTarget();
            originalScale = animatedRoot != null ? animatedRoot.localScale : Vector3.one;
        }

        private void RestoreScale()
        {
            if (animatedRoot != null)
            {
                animatedRoot.localScale = originalScale;
            }
        }

        private void RestoreAnchoredPosition()
        {
            if (animatedRoot != null)
            {
                animatedRoot.anchoredPosition = lastSafeAnchoredPosition;
            }
        }

        private static void SetRoots(GameObject frontRoot, GameObject backRoot, bool showFront)
        {
            if (frontRoot != null)
            {
                frontRoot.SetActive(showFront);
            }

            if (backRoot != null)
            {
                backRoot.SetActive(!showFront);
            }
        }
    }
}
