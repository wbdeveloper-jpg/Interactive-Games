using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OddSuckMechanic
{
    public enum OddSuckItemTemplateSide
    {
        Center,
        Left,
        Right,
        ImageMode
    }

    [RequireComponent(typeof(RectTransform))]
    public class OddSuckItemView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image highlightImage;
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Sprite/Image Mode Template")]
        [Tooltip("In sprite-only/image mode this controls the item background/template alpha. Keep 0 for icon-only, set 1 if you manually assign a visible image card sprite on the image template.")]
        [Range(0f, 1f)]
        [SerializeField] private float spriteModeBackgroundAlpha = 0f;
        [SerializeField] private bool hideTextInSpriteMode = true;

        private Tween spawnTween;
        private Tween selectedTween;
        private bool cachedBackgroundColor;
        private Color originalBackgroundColor;
        private Sprite originalBackgroundSprite;

        public bool IsOdd { get; private set; }
        public RectTransform RectTransform => rectTransform != null ? rectTransform : transform as RectTransform;

        private void Reset()
        {
            rectTransform = transform as RectTransform;
            backgroundImage = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            CacheBackgroundState();
            DisableRaycastTargets();
        }

        private void OnDestroy()
        {
            spawnTween?.Kill();
            selectedTween?.Kill();
        }

        public void Setup(OddSuckItemData data, OddSuckItemDisplayMode displayMode, bool forceShowSpriteLabels)
        {
            Setup(data, displayMode, forceShowSpriteLabels, OddSuckItemTemplateSide.Center);
        }

        public void Setup(OddSuckItemData data, OddSuckItemDisplayMode displayMode, bool forceShowSpriteLabels, OddSuckItemTemplateSide templateSide)
        {
            DisableRaycastTargets();

            if (data == null)
            {
                IsOdd = false;
                SetText(string.Empty, false);
                SetIcon(null, false);
                ApplyBackgroundForMode(displayMode == OddSuckItemDisplayMode.Sprite, templateSide);
                return;
            }

            IsOdd = data.isOdd;
            transform.localScale = Vector3.one;
            MarkSelected(false);

            bool spriteMode = displayMode == OddSuckItemDisplayMode.Sprite;
            bool showIcon = spriteMode && data.icon != null;
            bool showText = !spriteMode || forceShowSpriteLabels || !hideTextInSpriteMode;

            SetIcon(data.icon, showIcon);
            SetText(data.displayText, showText && !string.IsNullOrWhiteSpace(data.displayText));
            ApplyBackgroundForMode(spriteMode, templateSide);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public void PlaySpawn(float delay)
        {
            PlaySpawn(delay, false, 0f, 0.28f);
        }

        public void PlaySpawn(float delay, bool useFallAnimation, float fallFromYOffset, float duration)
        {
            spawnTween?.Kill();

            RectTransform rect = RectTransform;
            Vector2 finalPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
            float safeDuration = Mathf.Max(0.1f, duration);

            transform.localScale = useFallAnimation ? Vector3.one : Vector3.zero;

            if (rect != null && useFallAnimation)
            {
                rect.anchoredPosition = finalPosition + Vector2.up * Mathf.Max(0f, fallFromYOffset);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            Sequence sequence = DOTween.Sequence().SetDelay(delay);

            if (useFallAnimation && rect != null)
            {
                sequence.Append(rect.DOAnchorPos(finalPosition, safeDuration).SetEase(Ease.OutBack));
                sequence.Join(transform.DOPunchScale(Vector3.one * 0.08f, safeDuration, 5, 0.55f));
            }
            else
            {
                sequence.Append(transform.DOScale(Vector3.one, safeDuration).SetEase(Ease.OutBack));
            }

            if (canvasGroup != null)
            {
                sequence.Join(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 1f, Mathf.Min(0.22f, safeDuration)));
            }

            spawnTween = sequence.SetLink(gameObject);
        }

        public void MarkSelected(bool selected)
        {
            if (highlightImage != null)
            {
                highlightImage.gameObject.SetActive(selected);
            }

            selectedTween?.Kill();
            if (selected)
            {
                selectedTween = transform.DOPunchScale(Vector3.one * 0.12f, 0.2f, 8, 0.8f).SetLink(gameObject);
            }
        }

        private void ApplyBackgroundForMode(bool spriteMode, OddSuckItemTemplateSide templateSide)
        {
            if (backgroundImage == null)
            {
                return;
            }

            CacheBackgroundState();
            backgroundImage.enabled = true;
            backgroundImage.sprite = originalBackgroundSprite;

            Color color = originalBackgroundColor;
            color.a = spriteMode ? spriteModeBackgroundAlpha : originalBackgroundColor.a;
            backgroundImage.color = color;
        }

        private void CacheBackgroundState()
        {
            if (cachedBackgroundColor || backgroundImage == null)
            {
                return;
            }

            originalBackgroundColor = backgroundImage.color;
            originalBackgroundSprite = backgroundImage.sprite;
            cachedBackgroundColor = true;
        }

        private void SetText(string value, bool visible)
        {
            if (labelText == null)
            {
                return;
            }

            labelText.text = value;
            labelText.gameObject.SetActive(visible);
        }

        private void SetIcon(Sprite icon, bool visible)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = icon;
            iconImage.enabled = visible;
            iconImage.gameObject.SetActive(visible);
            iconImage.preserveAspect = true;
        }

        private void DisableRaycastTargets()
        {
            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = false;
            }

            if (iconImage != null)
            {
                iconImage.raycastTarget = false;
            }

            if (highlightImage != null)
            {
                highlightImage.raycastTarget = false;
            }

            if (labelText != null)
            {
                labelText.raycastTarget = false;
            }
        }
    }
}
