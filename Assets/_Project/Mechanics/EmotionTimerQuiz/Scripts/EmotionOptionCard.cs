using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmotionTimerQuiz
{
    [RequireComponent(typeof(Button))]
    public class EmotionOptionCard : MonoBehaviour
    {
        [Header("UI References")]
        public Button button;
        public Image backgroundImage;
        public Image characterImage;
        public TextMeshProUGUI letterText;
        public TextMeshProUGUI emotionText;
        public TextMeshProUGUI tapText;
        public CanvasGroup canvasGroup;

        [Header("Feedback Overlay Images")]
        [Tooltip("Top overlay Image. Assign your own frame/tick/cross PNGs here from Inspector.")]
        public Image feedbackOverlayImage;
        public Sprite normalOverlaySprite;
        public Sprite correctOverlaySprite;
        public Sprite wrongOverlaySprite;
        public Sprite correctRevealOverlaySprite;
        public Color normalOverlayColor = new Color(1f, 1f, 1f, 0f);
        public Color correctOverlayColor = new Color(0.35f, 0.95f, 0.55f, 0.32f);
        public Color wrongOverlayColor = new Color(1f, 0.35f, 0.35f, 0.30f);
        public Color correctRevealOverlayColor = new Color(0.42f, 0.83f, 1f, 0.28f);

        [Header("Visual Settings")]
        [Range(0.1f, 1f)] public float disabledAlpha = 0.85f;

        private EmotionOptionData optionData;
        private Action<EmotionOptionCard> onSelected;
        private Vector3 originalScale;

        public EmotionOptionData OptionData
        {
            get { return optionData; }
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (feedbackOverlayImage != null)
            {
                feedbackOverlayImage.raycastTarget = false;
            }

            originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;

            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }

            transform.DOKill();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
            }

            if (feedbackOverlayImage != null)
            {
                feedbackOverlayImage.DOKill();
            }
        }

        public void Setup(char letter, EmotionOptionData data, Color cardColor, Action<EmotionOptionCard> selectedCallback)
        {
            optionData = data;
            onSelected = selectedCallback;

            if (letterText != null)
            {
                letterText.text = letter.ToString();
            }

            if (emotionText != null)
            {
                emotionText.text = EmotionTimerQuizUtility.ToDisplayText(data.expression);
            }

            if (tapText != null)
            {
                tapText.text = "Tap to Select";
            }

            if (characterImage != null)
            {
                characterImage.sprite = data.sprite;
                characterImage.enabled = data.sprite != null;
                characterImage.preserveAspect = true;
            }

            // Base color only. Feedback does not recolor the card anymore.
            if (backgroundImage != null)
            {
                backgroundImage.color = cardColor;
            }

            ShowNormal();
            SetInteractable(true, false);
            PlaySpawnAnimation();
        }

        public void SetInteractable(bool interactable)
        {
            SetInteractable(interactable, true);
        }

        public void SetInteractable(bool interactable, bool animate)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                float targetAlpha = interactable ? 1f : disabledAlpha;
                if (animate && gameObject.activeInHierarchy)
                {
                    canvasGroup.DOFade(targetAlpha, 0.15f).SetUpdate(true);
                }
                else
                {
                    canvasGroup.alpha = targetAlpha;
                }
            }
        }

        public void ShowNormal()
        {
            transform.DOKill();
            transform.localScale = originalScale == Vector3.zero ? Vector3.one : originalScale;
            SetOverlay(normalOverlaySprite, normalOverlayColor, false);
        }

        public void ShowCorrect()
        {
            SetOverlay(correctOverlaySprite, correctOverlayColor, true);
            if (tapText != null)
            {
                tapText.text = "Correct!";
            }

            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.14f, 0.34f, 8, 0.8f).SetUpdate(true);
        }

        public void ShowWrong()
        {
            SetOverlay(wrongOverlaySprite, wrongOverlayColor, true);
            if (tapText != null)
            {
                tapText.text = "Try Again";
            }

            transform.DOKill();
            transform.DOShakeAnchorPositionIfRect(0.32f, 18f);
        }

        public void ShowCorrectReveal()
        {
            SetOverlay(correctRevealOverlaySprite != null ? correctRevealOverlaySprite : correctOverlaySprite, correctRevealOverlayColor, true);
            if (tapText != null)
            {
                tapText.text = "Correct Answer";
            }

            transform.DOKill();
            transform.DOPunchScale(Vector3.one * 0.09f, 0.32f, 6, 0.75f).SetUpdate(true);
        }

        private void PlaySpawnAnimation()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            transform.DOKill();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            }

            transform.localScale = originalScale * 0.92f;
            transform.DOScale(originalScale, 0.24f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void HandleClick()
        {
            if (optionData == null)
            {
                return;
            }

            if (onSelected != null)
            {
                onSelected.Invoke(this);
            }
        }

        private void SetOverlay(Sprite sprite, Color color, bool animate)
        {
            if (feedbackOverlayImage == null)
            {
                return;
            }

            feedbackOverlayImage.DOKill();
            feedbackOverlayImage.sprite = sprite;
            feedbackOverlayImage.color = color;
            feedbackOverlayImage.enabled = sprite != null || color.a > 0.001f;
            feedbackOverlayImage.raycastTarget = false;

            if (animate && feedbackOverlayImage.enabled && gameObject.activeInHierarchy)
            {
                Transform overlayTransform = feedbackOverlayImage.transform;
                overlayTransform.DOKill();
                overlayTransform.localScale = Vector3.one * 0.96f;
                overlayTransform.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }
    }

    public static class EmotionOptionCardTweenExtensions
    {
        public static void DOShakeAnchorPositionIfRect(this Transform target, float duration, float strength)
        {
            RectTransform rectTransform = target as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.DOShakeAnchorPos(duration, strength, 16, 90f, false, true).SetUpdate(true);
            }
            else
            {
                target.DOShakePosition(duration, strength, 16, 90f, false, true).SetUpdate(true);
            }
        }
    }
}
