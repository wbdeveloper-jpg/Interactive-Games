using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OddSuckMechanic
{
    public class OddSuckUfoAutoMover : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform ufoRoot;
        [SerializeField] private RectTransform ufoVisualTransform;
        [SerializeField] private RectTransform moveBounds;

        [Header("UFO Sprite Animation")]
        [SerializeField] private Image ufoSpriteImage;
        [SerializeField] private List<Sprite> movingSprites = new List<Sprite>();
        [SerializeField, Min(1f)] private float movingFramesPerSecond = 10f;
        [SerializeField] private bool animateSpritesOnlyWhileMoving = true;

        [Header("Speed Feedback")]
        [SerializeField, Min(0)] private int speedUpShakeStrength = 1;

        [Header("Horizontal Patrol")]
        [SerializeField, Min(10f)] private float baseHorizontalSpeed = 260f;
        [SerializeField, Min(0f)] private float sidePadding = 110f;
        [SerializeField] private bool startMovingRight = true;

        [Header("Direction Flip")]
        [SerializeField] private bool flipVisualToDirection = true;
        [SerializeField, Min(0.01f)] private float flipDuration = 0.16f;
        [SerializeField] private Ease flipEase = Ease.OutBack;

        [Header("Realistic Vertical Drift")]
        [SerializeField, Min(0f)] private float verticalDriftRange = 42f;
        [SerializeField, Min(0.1f)] private float verticalSmoothTime = 0.45f;
        [SerializeField, Min(0.3f)] private float minVerticalChangeDelay = 0.8f;
        [SerializeField, Min(0.3f)] private float maxVerticalChangeDelay = 1.8f;

        [Header("Small Hover Bob")]
        [SerializeField, Min(0f)] private float hoverBobAmplitude = 10f;
        [SerializeField, Min(0.1f)] private float hoverBobFrequency = 1.6f;

        private float baseY;
        private float direction;
        private float targetVerticalOffset;
        private float currentVerticalOffset;
        private float verticalVelocity;
        private float verticalChangeTimer;
        private float speedMultiplier = 1f;
        private bool movementEnabled = true;
        private Tween flipTween;
        private Tween exitTween;
        private int movingFrameIndex;
        private float movingFrameTimer;

        public RectTransform UfoRoot => ufoRoot != null ? ufoRoot : transform as RectTransform;
        public float CurrentSpeed => baseHorizontalSpeed * speedMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public float Direction => direction;

        private void Reset()
        {
            ufoRoot = transform as RectTransform;
            ufoVisualTransform = transform as RectTransform;
            moveBounds = transform.parent as RectTransform;
        }

        private void Awake()
        {
            if (ufoRoot == null)
            {
                ufoRoot = transform as RectTransform;
            }

            if (moveBounds == null && ufoRoot != null)
            {
                moveBounds = ufoRoot.parent as RectTransform;
            }

            baseY = ufoRoot != null ? ufoRoot.anchoredPosition.y : 0f;
            direction = startMovingRight ? 1f : -1f;
            ApplyDirectionFlipInstant();
            PickNextVerticalTarget();
        }

        private void OnDestroy()
        {
            flipTween?.Kill();
            exitTween?.Kill();
        }

        private void Update()
        {
            if (!movementEnabled || ufoRoot == null || moveBounds == null)
            {
                return;
            }

            Vector2 position = ufoRoot.anchoredPosition;
            position.x += direction * CurrentSpeed * Time.deltaTime;

            float minX = moveBounds.rect.xMin + sidePadding;
            float maxX = moveBounds.rect.xMax - sidePadding;

            if (position.x >= maxX)
            {
                position.x = maxX;
                SetDirection(-1f, true);
            }
            else if (position.x <= minX)
            {
                position.x = minX;
                SetDirection(1f, true);
            }

            verticalChangeTimer -= Time.deltaTime;
            if (verticalChangeTimer <= 0f)
            {
                PickNextVerticalTarget();
            }

            currentVerticalOffset = Mathf.SmoothDamp(currentVerticalOffset, targetVerticalOffset, ref verticalVelocity, verticalSmoothTime);
            float bob = Mathf.Sin(Time.time * hoverBobFrequency * Mathf.PI * 2f) * hoverBobAmplitude;
            position.y = baseY + currentVerticalOffset + bob;

            ufoRoot.anchoredPosition = position;
            UpdateMovingSpriteAnimation();
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!enabled && animateSpritesOnlyWhileMoving)
            {
                movingFrameTimer = 0f;
                movingFrameIndex = 0;
                if (ufoSpriteImage != null && movingSprites != null && movingSprites.Count > 0 && movingSprites[0] != null)
                {
                    ufoSpriteImage.sprite = movingSprites[0];
                }
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void ResetToCenter()
        {
            if (ufoRoot == null || moveBounds == null)
            {
                return;
            }

            exitTween?.Kill();
            SetDirection(startMovingRight ? 1f : -1f, false);
            currentVerticalOffset = 0f;
            targetVerticalOffset = 0f;
            verticalVelocity = 0f;
            PickNextVerticalTarget();
            ufoRoot.anchoredPosition = new Vector2(0f, baseY);
            ApplyDirectionFlipInstant();
            if (ufoVisualTransform != null)
            {
                ufoVisualTransform.anchoredPosition = Vector2.zero;
                ufoVisualTransform.localRotation = Quaternion.identity;
            }

        }

        public void PlayCorrectUfoAnimation(RectTransform visualRoot, ImageFlashTarget flashTarget)
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.DOKill();
            Vector3 normalScale = GetDirectedScale(visualRoot, 1f);
            Vector3 punchScale = GetDirectedScale(visualRoot, 1.16f);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(visualRoot.DOScale(punchScale, 0.1f).SetEase(Ease.OutBack));
            sequence.Join(visualRoot.DOPunchAnchorPos(Vector2.up * 34f, 0.34f, 8, 0.8f));
            sequence.Join(visualRoot.DOPunchRotation(Vector3.forward * 8f, 0.34f, 8, 0.8f));
            sequence.Append(visualRoot.DOScale(normalScale, 0.16f).SetEase(Ease.OutQuad));
            sequence.SetLink(visualRoot.gameObject);

            flashTarget?.Flash(new Color(0.45f, 1f, 0.7f, 1f), 0.16f);
        }

        public void PlayWrongUfoAnimation(RectTransform visualRoot, ImageFlashTarget flashTarget)
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.DOKill();
            Vector3 normalScale = GetDirectedScale(visualRoot, 1f);
            Vector3 squashScale = GetDirectedScale(visualRoot, 0.94f);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(visualRoot.DOPunchAnchorPos(new Vector2(42f, -20f), 0.34f, 12, 0.95f));
            sequence.Join(visualRoot.DOPunchRotation(Vector3.forward * 13f, 0.34f, 11, 0.85f));
            sequence.Append(visualRoot.DOScale(squashScale, 0.08f));
            sequence.Append(visualRoot.DOScale(normalScale, 0.12f).SetEase(Ease.OutBack));
            sequence.SetLink(visualRoot.gameObject);

            flashTarget?.Flash(new Color(1f, 0.38f, 0.3f, 1f), 0.16f);
        }

        public void PlaySpeedUpAnimation(RectTransform visualRoot)
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.DOKill();
            float sign = visualRoot.localScale.x < 0f ? -1f : 1f;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(visualRoot.DOScale(new Vector3(sign * 1.12f, 0.92f, 1f), 0.08f).SetEase(Ease.OutQuad));
            sequence.Append(visualRoot.DOScale(new Vector3(sign, 1f, 1f), 0.16f).SetEase(Ease.OutBack));
            sequence.SetLink(visualRoot.gameObject);
        }

        public void PlayEntryFromTop(RectTransform visualRoot, float duration, Action onComplete)
        {
            if (ufoRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            movementEnabled = false;
            exitTween?.Kill();
            ufoRoot.DOKill();
            visualRoot?.DOKill();

            float startY = moveBounds != null ? moveBounds.rect.yMax + 260f : baseY + 900f;
            Vector2 targetPosition = new Vector2(0f, baseY);
            ufoRoot.anchoredPosition = new Vector2(0f, startY);

            if (visualRoot != null)
            {
                visualRoot.localScale = GetDirectedScale(visualRoot, 0.74f);
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, -6f);
            }

            Sequence sequence = DOTween.Sequence();
            sequence.Append(ufoRoot.DOAnchorPos(targetPosition, Mathf.Max(0.1f, duration)).SetEase(Ease.OutCubic));
            if (visualRoot != null)
            {
                sequence.Join(visualRoot.DOScale(GetDirectedScale(visualRoot, 1f), duration).SetEase(Ease.OutBack));
                sequence.Join(visualRoot.DORotate(Vector3.zero, duration * 0.85f).SetEase(Ease.OutQuad));
            }

            sequence.OnComplete(() =>
            {
                currentVerticalOffset = 0f;
                targetVerticalOffset = 0f;
                verticalVelocity = 0f;
                onComplete?.Invoke();
            });

            exitTween = sequence.SetLink(ufoRoot.gameObject);
        }

        public void PlayExitToSpace(RectTransform visualRoot, float duration, Action onComplete)
        {
            if (ufoRoot == null)
            {
                onComplete?.Invoke();
                return;
            }

            movementEnabled = false;
            exitTween?.Kill();
            ufoRoot.DOKill();
            visualRoot?.DOKill();

            float topY = moveBounds != null ? moveBounds.rect.yMax + 260f : ufoRoot.anchoredPosition.y + 900f;
            float exitX = ufoRoot.anchoredPosition.x;

            if (moveBounds != null)
            {
                int exitStyle = UnityEngine.Random.Range(0, 3);
                if (exitStyle == 1)
                {
                    exitX = moveBounds.rect.xMin - sidePadding;
                }
                else if (exitStyle == 2)
                {
                    exitX = moveBounds.rect.xMax + sidePadding;
                }
            }

            if (!Mathf.Approximately(exitX, ufoRoot.anchoredPosition.x))
            {
                SetDirection(exitX > ufoRoot.anchoredPosition.x ? 1f : -1f, true);
            }

            Vector2 exitPosition = new Vector2(exitX, topY);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(ufoRoot.DOAnchorPos(exitPosition, duration).SetEase(Ease.InBack));
            if (visualRoot != null)
            {
                sequence.Join(visualRoot.DOPunchRotation(Vector3.forward * 16f, 0.45f, 10, 0.7f));
                sequence.Join(visualRoot.DOScale(GetDirectedScale(visualRoot, 0.72f), duration).SetEase(Ease.InBack));
            }

            sequence.OnComplete(() => onComplete?.Invoke());
            exitTween = sequence.SetLink(ufoRoot.gameObject);
        }

        private void SetDirection(float newDirection, bool animated)
        {
            newDirection = newDirection >= 0f ? 1f : -1f;
            if (Mathf.Approximately(direction, newDirection))
            {
                return;
            }

            direction = newDirection;

            if (animated)
            {
                AnimateDirectionFlip();
            }
            else
            {
                ApplyDirectionFlipInstant();
            }
        }

        private void ApplyDirectionFlipInstant()
        {
            if (!flipVisualToDirection || ufoVisualTransform == null)
            {
                return;
            }

            ufoVisualTransform.localScale = new Vector3(direction >= 0f ? 1f : -1f, 1f, 1f);
        }

        private void AnimateDirectionFlip()
        {
            if (!flipVisualToDirection || ufoVisualTransform == null)
            {
                return;
            }

            flipTween?.Kill();
            float targetX = direction >= 0f ? 1f : -1f;
            Vector3 targetScale = new Vector3(targetX, 1f, 1f);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(ufoVisualTransform.DOScale(new Vector3(0.08f * Mathf.Sign(ufoVisualTransform.localScale.x), 1.08f, 1f), flipDuration * 0.45f).SetEase(Ease.InQuad));
            sequence.Append(ufoVisualTransform.DOScale(targetScale, flipDuration * 0.55f).SetEase(flipEase));
            sequence.Join(ufoVisualTransform.DOPunchRotation(Vector3.forward * 5f * -targetX, 0.22f, 6, 0.65f));
            flipTween = sequence.SetLink(ufoVisualTransform.gameObject);
        }

        private static Vector3 GetDirectedScale(RectTransform visualRoot, float scale)
        {
            float sign = visualRoot != null && visualRoot.localScale.x < 0f ? -1f : 1f;
            return new Vector3(sign * scale, scale, 1f);
        }

        private void UpdateMovingSpriteAnimation()
        {
            if (ufoSpriteImage == null || movingSprites == null || movingSprites.Count == 0)
            {
                return;
            }

            if (animateSpritesOnlyWhileMoving && !movementEnabled)
            {
                return;
            }

            movingFrameTimer += Time.deltaTime;
            float frameTime = 1f / Mathf.Max(1f, movingFramesPerSecond);
            if (movingFrameTimer < frameTime)
            {
                return;
            }

            movingFrameTimer = 0f;
            movingFrameIndex = (movingFrameIndex + 1) % movingSprites.Count;
            Sprite nextSprite = movingSprites[movingFrameIndex];
            if (nextSprite != null)
            {
                ufoSpriteImage.sprite = nextSprite;
            }
        }

        private void PickNextVerticalTarget()
        {
            targetVerticalOffset = verticalDriftRange <= 0f
                ? 0f
                : UnityEngine.Random.Range(-verticalDriftRange, verticalDriftRange);

            float safeMaxDelay = Mathf.Max(minVerticalChangeDelay, maxVerticalChangeDelay);
            verticalChangeTimer = UnityEngine.Random.Range(minVerticalChangeDelay, safeMaxDelay);
        }
    }

    [System.Serializable]
    public class ImageFlashTarget
    {
        [SerializeField] private UnityEngine.UI.Image image;

        private Color originalColor;
        private bool hasOriginalColor;

        public ImageFlashTarget(UnityEngine.UI.Image targetImage)
        {
            image = targetImage;
        }

        public void CacheOriginalColor()
        {
            if (image == null)
            {
                return;
            }

            originalColor = image.color;
            hasOriginalColor = true;
        }

        public void Flash(Color flashColor, float duration)
        {
            if (image == null)
            {
                return;
            }

            if (!hasOriginalColor)
            {
                CacheOriginalColor();
            }

            image.DOKill();
            image.DOColor(flashColor, duration)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => image.color = originalColor)
                .SetLink(image.gameObject);
        }
    }
}
