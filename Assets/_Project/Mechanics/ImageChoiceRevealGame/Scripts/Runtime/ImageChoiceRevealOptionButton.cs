using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ImageChoiceRevealGame
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(CanvasGroup))]
    public class ImageChoiceRevealOptionButton : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private Button button;
        [SerializeField] private Image optionImage;
        [SerializeField] private TMP_Text optionText;
        [SerializeField] private Image feedbackOverlay;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Feedback Colors")]
        [SerializeField] private Color correctColor = new Color(0.15f, 0.8f, 0.25f, 0.58f);
        [SerializeField] private Color wrongColor = new Color(1f, 0.1f, 0.1f, 0.58f);
        [SerializeField] private Color neutralColor = new Color(1f, 1f, 1f, 0f);

        private Action<ImageChoiceRevealOptionButton> clickCallback;
        private RectTransform rectTransform;
        private Sequence activeSequence;
        private Vector3 baseScale = Vector3.one;

        public bool IsCorrect { get; private set; }
        public ImageChoiceRevealOptionData CurrentOptionData { get; private set; }

        private void Reset()
        {
            button = GetComponent<Button>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            if (button == null) button = GetComponent<Button>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            if (rectTransform != null) baseScale = rectTransform.localScale;
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
            KillTween();
        }

        public void Configure(ImageChoiceRevealOptionData optionData, bool isCorrect, ImageChoiceOptionDisplayMode globalDisplayMode, Action<ImageChoiceRevealOptionButton> onClicked)
        {
            CurrentOptionData = optionData;
            IsCorrect = isCorrect;
            clickCallback = onClicked;

            gameObject.SetActive(true);
            KillTween();

            if (rectTransform != null) rectTransform.localScale = baseScale;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            ApplyOptionVisual(optionData, globalDisplayMode);
            SetInteractable(true);
            ClearFeedback();
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        public void ClearFeedback()
        {
            if (feedbackOverlay != null)
            {
                feedbackOverlay.enabled = true;
                feedbackOverlay.color = neutralColor;
            }
        }

        public void ShowCorrectInstant()
        {
            if (feedbackOverlay != null) feedbackOverlay.color = correctColor;
        }

        public void ShowWrongInstant()
        {
            if (feedbackOverlay != null) feedbackOverlay.color = wrongColor;
        }

        public void PlayAppear(float delay, float duration, bool animated)
        {
            KillTween();
            gameObject.SetActive(true);

            if (!animated || canvasGroup == null || rectTransform == null)
            {
                if (canvasGroup != null) canvasGroup.alpha = 1f;
                if (rectTransform != null) rectTransform.localScale = baseScale;
                return;
            }

            canvasGroup.alpha = 0f;
            rectTransform.localScale = baseScale * 0.86f;

            activeSequence = DOTween.Sequence();
            activeSequence.SetLink(gameObject);
            activeSequence.SetDelay(delay);
            activeSequence.Join(canvasGroup.DOFade(1f, duration));
            activeSequence.Join(rectTransform.DOScale(baseScale, duration).SetEase(Ease.OutBack));
        }

        public void PlayHideByHint(float duration, bool animated)
        {
            KillTween();
            SetInteractable(false);

            if (!animated || canvasGroup == null || rectTransform == null)
            {
                gameObject.SetActive(false);
                return;
            }

            activeSequence = DOTween.Sequence();
            activeSequence.SetLink(gameObject);
            activeSequence.Join(canvasGroup.DOFade(0f, duration));
            activeSequence.Join(rectTransform.DOScale(baseScale * 0.78f, duration).SetEase(Ease.InBack));
            activeSequence.OnComplete(() => gameObject.SetActive(false));
        }

        public void PlayCorrectFeedback(float duration, bool animated)
        {
            ShowCorrectInstant();
            if (!animated || rectTransform == null) return;

            KillTween();
            activeSequence = DOTween.Sequence();
            activeSequence.SetLink(gameObject);
            activeSequence.Append(rectTransform.DOScale(baseScale * 1.08f, duration * 0.45f).SetEase(Ease.OutBack));
            activeSequence.Append(rectTransform.DOScale(baseScale, duration * 0.55f).SetEase(Ease.OutCubic));
        }

        public void PlayWrongFeedback(float duration, bool animated)
        {
            ShowWrongInstant();
            if (!animated || rectTransform == null) return;

            KillTween();
            activeSequence = DOTween.Sequence();
            activeSequence.SetLink(gameObject);
            activeSequence.Append(rectTransform.DOShakeAnchorPos(duration, new Vector2(18f, 0f), 18, 75f, false, true));
        }

        private void ApplyOptionVisual(ImageChoiceRevealOptionData optionData, ImageChoiceOptionDisplayMode globalDisplayMode)
        {
            ImageChoiceOptionDisplayType resolvedType = ResolveDisplayType(optionData, globalDisplayMode);

            bool showImage = resolvedType == ImageChoiceOptionDisplayType.Image;
            bool showText = resolvedType == ImageChoiceOptionDisplayType.Text;

            if (optionImage != null)
            {
                optionImage.gameObject.SetActive(showImage);
                optionImage.enabled = showImage && optionData != null && optionData.optionSprite != null;
                optionImage.sprite = optionData != null ? optionData.optionSprite : null;
                optionImage.preserveAspect = true;
                optionImage.color = Color.white;
            }

            if (optionText != null)
            {
                optionText.gameObject.SetActive(showText);
                optionText.text = optionData != null ? optionData.GetTextFallback("") : "";
                optionText.enableAutoSizing = true;
                optionText.enableWordWrapping = true;
            }
        }

        private ImageChoiceOptionDisplayType ResolveDisplayType(ImageChoiceRevealOptionData optionData, ImageChoiceOptionDisplayMode globalDisplayMode)
        {
            if (globalDisplayMode == ImageChoiceOptionDisplayMode.ForceImage) return ImageChoiceOptionDisplayType.Image;
            if (globalDisplayMode == ImageChoiceOptionDisplayMode.ForceText) return ImageChoiceOptionDisplayType.Text;
            if (optionData == null) return ImageChoiceOptionDisplayType.Image;
            return optionData.displayType == ImageChoiceOptionDisplayType.Text ? ImageChoiceOptionDisplayType.Text : ImageChoiceOptionDisplayType.Image;
        }

        private void KillTween()
        {
            if (activeSequence != null && activeSequence.IsActive()) activeSequence.Kill();
            activeSequence = null;
        }

        private void HandleClick()
        {
            clickCallback?.Invoke(this);
        }
    }
}
