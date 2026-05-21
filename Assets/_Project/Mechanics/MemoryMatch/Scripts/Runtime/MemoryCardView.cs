using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryCardView : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private Button button;
        [SerializeField] private GameObject frontRoot;
        [SerializeField] private GameObject backRoot;

        [Header("Animation")]
        [SerializeField] private MemoryCardAnimator cardAnimator;
        [SerializeField] private bool useAnimatedFlip = true;

        [Header("Front Content")]
        [SerializeField] private TMP_Text displayText;
        [SerializeField] private Image displayImage;

        [Header("Content Layout - Optional")]
        [Tooltip("Optional. If empty, Display Text RectTransform is used.")]
        [SerializeField] private RectTransform displayTextRect;

        [Tooltip("Optional. If empty, Display Image RectTransform is used.")]
        [SerializeField] private RectTransform displayImageRect;

        [SerializeField] private bool applyContentLayoutOnInitialize = true;

        [Tooltip("Padding used when card contains only text.")]
        [SerializeField, Min(0f)] private float textOnlyPadding = 10f;

        [Tooltip("Padding used when card contains only image.")]
        [SerializeField, Min(0f)] private float imageOnlyPadding = 10f;

        [Tooltip("Padding used when card contains both image and text.")]
        [SerializeField, Min(0f)] private float mixedContentPadding = 10f;

        [Tooltip("Gap between image and text when both are active.")]
        [SerializeField, Min(0f)] private float mixedContentGap = 8f;

        [Tooltip("How much vertical space the image gets when both image and text are active.")]
        [SerializeField, Range(0.45f, 0.8f)] private float mixedImageHeightRatio = 0.62f;

        [Header("Theme Images - Optional")]
        [SerializeField] private Image frontBackgroundImage;
        [SerializeField] private Image backBackgroundImage;

        [Header("State Visuals")]
        [SerializeField] private GameObject selectedVisual;
        [SerializeField] private GameObject matchedVisual;
        [SerializeField] private GameObject hintVisual;
        [SerializeField] private Image hintVisualImage;

        private Action<MemoryCardView> onClicked;
        private bool inputEnabled = true;

        public MemoryCardRuntimeData Data { get; private set; }
        public bool IsFaceUp { get; private set; }
        public bool IsMatched { get; private set; }
        public bool IsHinted { get; private set; }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (cardAnimator == null)
            {
                cardAnimator = GetComponent<MemoryCardAnimator>();
            }

            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }

            CacheOptionalContentRects();
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Initialize(MemoryCardRuntimeData data, Action<MemoryCardView> clickCallback)
        {
            Data = data;
            onClicked = clickCallback;
            IsMatched = false;
            IsHinted = false;
            inputEnabled = true;

            ApplyContent();

            if (applyContentLayoutOnInitialize)
            {
                ApplyContentLayout();
            }

            SetSelected(false);
            SetHinted(false);
            SetMatched(false);
            ShowBackImmediate();
            RefreshInteractable();
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                return;
            }

            if (frontBackgroundImage != null)
            {
                frontBackgroundImage.color = theme.CardFrontColor;

                if (theme.CardFrontSprite != null)
                {
                    frontBackgroundImage.sprite = theme.CardFrontSprite;
                }
            }

            if (backBackgroundImage != null)
            {
                backBackgroundImage.color = theme.CardBackColor;

                if (theme.CardBackSprite != null)
                {
                    backBackgroundImage.sprite = theme.CardBackSprite;
                }
            }

            if (displayText != null)
            {
                displayText.color = theme.CardTextColor;

                if (theme.CardFont != null)
                {
                    displayText.font = theme.CardFont;
                }
            }

            if (hintVisualImage != null)
            {
                hintVisualImage.color = theme.HintVisualColor;
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            RefreshInteractable();
        }

        public void FlipUp()
        {
            if (IsMatched || IsFaceUp)
            {
                return;
            }

            IsFaceUp = true;
            SetFaceState(true, useAnimatedFlip);
        }

        public void FlipDown()
        {
            if (IsMatched || !IsFaceUp)
            {
                return;
            }

            IsFaceUp = false;
            SetFaceState(false, useAnimatedFlip);
            SetSelected(false);
        }

        public void ShowBackImmediate()
        {
            IsFaceUp = false;
            SetFaceState(false, false);
        }

        public void PlayCorrectFeedback()
        {
            if (cardAnimator != null)
            {
                cardAnimator.PlayCorrectPulse();
            }
        }

        public void PlayWrongFeedback()
        {
            if (cardAnimator != null)
            {
                cardAnimator.PlayWrongShake();
            }
        }

        public void PlayHintFeedback()
        {
            if (cardAnimator != null)
            {
                cardAnimator.PlayHintPulse();
            }
        }

        public void StopHintFeedback()
        {
            if (cardAnimator != null)
            {
                cardAnimator.StopHintPulse();
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedVisual != null)
            {
                selectedVisual.SetActive(selected);
            }
        }

        public void SetHinted(bool hinted)
        {
            IsHinted = hinted;

            if (hintVisual != null)
            {
                hintVisual.SetActive(hinted);
            }
        }

        public void SetMatched(bool matched)
        {
            IsMatched = matched;

            if (matchedVisual != null)
            {
                matchedVisual.SetActive(matched);
            }

            if (matched)
            {
                IsFaceUp = true;
                SetFaceState(true, false);
                SetSelected(false);
                SetHinted(false);
                StopHintFeedback();
            }

            RefreshInteractable();
        }

        private void ApplyContent()
        {
            if (Data == null)
            {
                return;
            }

            if (displayText != null)
            {
                bool shouldShowText =
                    Data.ContentType == MemoryCardContentType.Text ||
                    Data.ContentType == MemoryCardContentType.ImageWithLabel;

                displayText.gameObject.SetActive(shouldShowText);
                displayText.text = Data.DisplayText;

                if (shouldShowText)
                {
                    displayText.alignment = TextAlignmentOptions.Center;
                    displayText.enableWordWrapping = true;
                    displayText.overflowMode = TextOverflowModes.Ellipsis;
                }
            }

            if (displayImage != null)
            {
                bool shouldShowImage =
                    (Data.ContentType == MemoryCardContentType.Image ||
                     Data.ContentType == MemoryCardContentType.ImageWithLabel) &&
                    Data.DisplaySprite != null;

                displayImage.gameObject.SetActive(shouldShowImage);
                displayImage.sprite = Data.DisplaySprite;

                if (shouldShowImage)
                {
                    displayImage.preserveAspect = true;
                }
            }
        }

        private void ApplyContentLayout()
        {
            if (Data == null)
            {
                return;
            }

            CacheOptionalContentRects();

            switch (Data.ContentType)
            {
                case MemoryCardContentType.Text:
                    ApplyTextOnlyLayout();
                    break;

                case MemoryCardContentType.Image:
                    ApplyImageOnlyLayout();
                    break;

                case MemoryCardContentType.ImageWithLabel:
                    ApplyImageWithLabelLayout();
                    break;
            }
        }

        private void ApplyTextOnlyLayout()
        {
            if (displayTextRect != null)
            {
                Stretch(displayTextRect, textOnlyPadding, textOnlyPadding, textOnlyPadding, textOnlyPadding);
            }

            if (displayImageRect != null)
            {
                displayImageRect.gameObject.SetActive(false);
            }
        }

        private void ApplyImageOnlyLayout()
        {
            if (displayImageRect != null)
            {
                displayImageRect.gameObject.SetActive(displayImage != null && displayImage.sprite != null);
                Stretch(displayImageRect, imageOnlyPadding, imageOnlyPadding, imageOnlyPadding, imageOnlyPadding);
            }

            if (displayTextRect != null)
            {
                displayTextRect.gameObject.SetActive(false);
            }
        }

        private void ApplyImageWithLabelLayout()
        {
            if (displayImageRect == null || displayTextRect == null)
            {
                return;
            }

            bool hasImage = displayImage != null && displayImage.sprite != null;
            bool hasText = displayText != null && !string.IsNullOrWhiteSpace(displayText.text);

            displayImageRect.gameObject.SetActive(hasImage);
            displayTextRect.gameObject.SetActive(hasText);

            if (hasImage && hasText)
            {
                float imageStartY = Mathf.Clamp01(1f - mixedImageHeightRatio);

                displayImageRect.anchorMin = new Vector2(0f, imageStartY);
                displayImageRect.anchorMax = new Vector2(1f, 1f);
                displayImageRect.offsetMin = new Vector2(mixedContentPadding, mixedContentGap * 0.5f);
                displayImageRect.offsetMax = new Vector2(-mixedContentPadding, -mixedContentPadding);

                displayTextRect.anchorMin = Vector2.zero;
                displayTextRect.anchorMax = new Vector2(1f, imageStartY);
                displayTextRect.offsetMin = new Vector2(mixedContentPadding, mixedContentPadding);
                displayTextRect.offsetMax = new Vector2(-mixedContentPadding, -mixedContentGap * 0.5f);
            }
            else if (hasImage)
            {
                Stretch(displayImageRect, imageOnlyPadding, imageOnlyPadding, imageOnlyPadding, imageOnlyPadding);
            }
            else if (hasText)
            {
                Stretch(displayTextRect, textOnlyPadding, textOnlyPadding, textOnlyPadding, textOnlyPadding);
            }
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void CacheOptionalContentRects()
        {
            if (displayTextRect == null && displayText != null)
            {
                displayTextRect = displayText.rectTransform;
            }

            if (displayImageRect == null && displayImage != null)
            {
                displayImageRect = displayImage.rectTransform;
            }
        }

        private void SetFaceState(bool showFront, bool animate)
        {
            if (animate && cardAnimator != null)
            {
                cardAnimator.FlipTo(frontRoot, backRoot, showFront);
                return;
            }

            if (cardAnimator != null)
            {
                cardAnimator.SetFaceImmediate(frontRoot, backRoot, showFront);
                return;
            }

            if (frontRoot != null)
            {
                frontRoot.SetActive(showFront);
            }

            if (backRoot != null)
            {
                backRoot.SetActive(!showFront);
            }
        }

        private void HandleClicked()
        {
            if (!inputEnabled || IsMatched)
            {
                return;
            }

            onClicked?.Invoke(this);
        }

        private void RefreshInteractable()
        {
            if (button != null)
            {
                button.interactable = inputEnabled && !IsMatched;
            }
        }
    }
}
