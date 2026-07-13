using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace NarayanaGames.SpellBotRescue
{
    public class SpellBotRobotView : MonoBehaviour
    {
        [Header("Robot Image")]
        public Image robotImage;
        [Tooltip("Normal robot sprite. If empty, the sprite already assigned on the Image is used.")]
        public Sprite idleSprite;
        [Tooltip("Only needed if you want a different robot during Overdrive.")]
        public Sprite overdriveSprite;

        [Header("Optional Emotion Sprites")]
        [Tooltip("OFF by default. Keep it off if you only want Normal + Overdrive robot sprites.")]
        public bool useEmotionSprites = false;
        public Sprite happySprite;
        public Sprite sadSprite;

        [Header("Overdrive Sprite Mode")]
        public bool useOverdriveSprite = true;
        public bool animateOverdriveEnter = true;
        public float overdriveEnterPunchScale = 0.10f;
        public float overdriveEnterDuration = 0.26f;

        [Header("Premium Animation")]
        public float happyJumpHeight = 18f;
        public float happyPunchScale = 0.12f;
        public float happyDuration = 0.38f;
        public float sadShakeStrength = 12f;
        public float sadTiltAngle = 6f;

        [Header("Safety")]
        [Tooltip("Prevents accidental DOTween/editor scale 0 from making the robot disappear.")]
        public bool neverAllowZeroScale = true;
        [Tooltip("Used only if the current scene scale was already saved as 0,0,0.")]
        public Vector3 fallbackVisibleScale = Vector3.one;

        private Vector3 originalScale;
        private Quaternion originalRotation;
        private Vector2 originalAnchoredPosition;
        private RectTransform rectTransform;
        private Sprite cachedSceneIdleSprite;
        private bool overdriveActive;
        private bool cachedTransform;

        private void Awake()
        {
            if (robotImage == null)
            {
                robotImage = GetComponent<Image>();
            }

            rectTransform = transform as RectTransform;

            if (robotImage != null && robotImage.sprite != null)
            {
                cachedSceneIdleSprite = robotImage.sprite;
            }

            CacheBaseTransformFromCurrent();
            RestoreBaseTransform();
            ApplyCurrentBaseSprite();
        }

        private void OnEnable()
        {
            EnsureVisibleScale();
            ApplyCurrentBaseSprite();
        }

        [ContextMenu("SpellBot/Refresh Base Transform From Current")]
        public void RefreshBaseTransformFromCurrent()
        {
            CacheBaseTransformFromCurrent(true);
        }

        public void SetOverdriveActive(bool active)
        {
            SetOverdriveActive(active, true);
        }

        public void SetOverdriveActive(bool active, bool animate)
        {
            overdriveActive = active && useOverdriveSprite && overdriveSprite != null;

            RestoreBaseTransform();
            ApplyCurrentBaseSprite();

            if (overdriveActive && animate && animateOverdriveEnter)
            {
                transform.DOKill();
                EnsureVisibleScale();
                transform.DOPunchScale(Vector3.one * overdriveEnterPunchScale, overdriveEnterDuration, 7, 0.8f);
            }
        }

        public void SetIdle()
        {
            RestoreBaseTransform();
            ApplyCurrentBaseSprite();
        }

        public void PlayHappy()
        {
            if (useEmotionSprites && robotImage != null && happySprite != null)
            {
                robotImage.sprite = happySprite;
            }

            RestoreBaseTransform();

            Sequence sequence = DOTween.Sequence();

            if (rectTransform != null)
            {
                rectTransform.DOKill();
                sequence.Join(rectTransform.DOAnchorPosY(originalAnchoredPosition.y + happyJumpHeight, happyDuration * 0.45f).SetEase(Ease.OutCubic));
                sequence.Append(rectTransform.DOAnchorPosY(originalAnchoredPosition.y, happyDuration * 0.55f).SetEase(Ease.OutBack));
            }

            sequence.Join(transform.DOPunchScale(Vector3.one * happyPunchScale, happyDuration, 7, 0.8f));
            sequence.Join(transform.DORotate(new Vector3(0f, 0f, -4f), happyDuration * 0.45f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            sequence.OnComplete(SetIdle);
        }

        public void PlaySad()
        {
            if (useEmotionSprites && robotImage != null && sadSprite != null)
            {
                robotImage.sprite = sadSprite;
            }

            RestoreBaseTransform();

            if (rectTransform != null)
            {
                rectTransform.DOKill();
                Sequence sequence = DOTween.Sequence();
                sequence.Join(rectTransform.DOShakeAnchorPos(0.26f, sadShakeStrength, 10, 90f));
                sequence.Join(transform.DORotate(new Vector3(0f, 0f, sadTiltAngle), 0.11f).SetLoops(2, LoopType.Yoyo));
                sequence.OnComplete(SetIdle);
            }
            else
            {
                Invoke(nameof(SetIdle), 0.3f);
            }
        }

        private void ApplyCurrentBaseSprite()
        {
            if (robotImage == null)
            {
                return;
            }

            if (overdriveActive && overdriveSprite != null)
            {
                robotImage.sprite = overdriveSprite;
                return;
            }

            if (idleSprite != null)
            {
                robotImage.sprite = idleSprite;
                return;
            }

            if (cachedSceneIdleSprite != null)
            {
                robotImage.sprite = cachedSceneIdleSprite;
            }
        }

        private void RestoreBaseTransform()
        {
            if (!cachedTransform)
            {
                CacheBaseTransformFromCurrent();
            }

            transform.DOKill();

            if (neverAllowZeroScale && IsZeroScale(originalScale))
            {
                originalScale = IsZeroScale(fallbackVisibleScale) ? Vector3.one : fallbackVisibleScale;
            }

            transform.localScale = originalScale;
            transform.localRotation = originalRotation;

            if (rectTransform != null)
            {
                rectTransform.DOKill();
                rectTransform.anchoredPosition = originalAnchoredPosition;
            }

            EnsureVisibleScale();
        }

        private void CacheBaseTransformFromCurrent(bool force = false)
        {
            if (cachedTransform && !force)
            {
                return;
            }

            Vector3 currentScale = transform.localScale;
            if (neverAllowZeroScale && IsZeroScale(currentScale))
            {
                currentScale = IsZeroScale(fallbackVisibleScale) ? Vector3.one : fallbackVisibleScale;
            }

            originalScale = currentScale;
            originalRotation = transform.localRotation;

            if (rectTransform != null)
            {
                originalAnchoredPosition = rectTransform.anchoredPosition;
            }

            cachedTransform = true;
        }

        private void EnsureVisibleScale()
        {
            if (!neverAllowZeroScale)
            {
                return;
            }

            if (!IsZeroScale(transform.localScale))
            {
                return;
            }

            transform.localScale = IsZeroScale(originalScale) ? fallbackVisibleScale : originalScale;

            if (IsZeroScale(transform.localScale))
            {
                transform.localScale = Vector3.one;
            }
        }

        private static bool IsZeroScale(Vector3 scale)
        {
            return Mathf.Abs(scale.x) < 0.001f || Mathf.Abs(scale.y) < 0.001f || Mathf.Abs(scale.z) < 0.001f;
        }
    }
}
