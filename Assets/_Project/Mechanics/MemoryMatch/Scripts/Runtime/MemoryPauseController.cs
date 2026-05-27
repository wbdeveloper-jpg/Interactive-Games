using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryPauseController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;

        [Header("Pause Button Theme - Optional")]
        [SerializeField] private Image pauseButtonBackgroundImage;
        [SerializeField] private Image pauseButtonIconImage;
        [SerializeField] private TMP_Text resumeButtonText;

        [Header("Overlay")]
        [Tooltip("This should be a full-screen object under Canvas/SafeAreaRoot.")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private RectTransform pausePanelTransform;

        [SerializeField] private bool forceOverlayRootFullScreen = true;

        [Header("Overlay Images")]
        [SerializeField] private Image overlayBackgroundImage;
        [SerializeField] private Image pausePanelImage;

        [Header("Overlay Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        [Header("Animation")]
        [SerializeField, Min(0.05f)] private float openDuration = 0.2f;
        [SerializeField, Min(0.05f)] private float closeDuration = 0.15f;
        [SerializeField] private Vector3 hiddenPanelScale = new Vector3(0.92f, 0.92f, 0.92f);

        private Action onPauseRequested;
        private Action onResumeRequested;
        private Tween activeTween;
        private Vector3 shownPanelScale = Vector3.one;

        private void Awake()
        {
            if (overlayRoot == null)
            {
                overlayRoot = gameObject;
            }

            if (forceOverlayRootFullScreen)
            {
                StretchOverlayRoot();
            }

            if (overlayCanvasGroup == null && overlayRoot != null)
            {
                overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            }

            if (overlayCanvasGroup == null && overlayRoot != null)
            {
                overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            }

            if (pausePanelTransform != null)
            {
                shownPanelScale = pausePanelTransform.localScale;
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(HandlePauseClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(HandleResumeClicked);
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(HandlePauseClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(HandleResumeClicked);
            }

            KillActiveTween();
        }

        public void Initialize(Action pauseRequestedCallback, Action resumeRequestedCallback)
        {
            onPauseRequested = pauseRequestedCallback;
            onResumeRequested = resumeRequestedCallback;
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                return;
            }

            ApplyImage(pauseButtonBackgroundImage, theme.PauseButtonBackgroundSprite, theme.PauseButtonBackgroundColor);
            ApplyImage(pauseButtonIconImage, theme.PauseButtonIconSprite, theme.PauseButtonIconColor);
            ApplyImage(overlayBackgroundImage, theme.PauseOverlayBackgroundSprite, theme.PauseOverlayBackgroundColor);
            ApplyImage(pausePanelImage, theme.PausePanelSprite, theme.PausePanelColor);

            if (titleText != null)
            {
                titleText.color = theme.PauseTitleColor;

                if (theme.HeaderFont != null)
                {
                    titleText.font = theme.HeaderFont;
                }
            }

            if (bodyText != null)
            {
                bodyText.color = theme.PauseBodyColor;

                if (theme.BodyFont != null)
                {
                    bodyText.font = theme.BodyFont;
                }
            }

            if (resumeButtonText != null && theme.UIFont != null)
            {
                resumeButtonText.font = theme.UIFont;
            }
        }

        public void SetPauseButtonVisible(bool visible)
        {
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(visible);
            }
        }

        public void SetPauseButtonInteractable(bool interactable)
        {
            if (pauseButton != null)
            {
                pauseButton.interactable = interactable;
            }
        }

        public void ShowOverlay(string title, string body)
        {
            if (overlayRoot != null)
            {
                if (forceOverlayRootFullScreen)
                {
                    StretchOverlayRoot();
                }

                overlayRoot.SetActive(true);
                overlayRoot.transform.SetAsLastSibling();
            }

            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(title) ? "Paused" : title;
            }

            if (bodyText != null)
            {
                bodyText.text = string.IsNullOrWhiteSpace(body)
                    ? "Take a short break. Tap Resume when you are ready."
                    : body;
            }

            KillActiveTween();

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.interactable = true;
                overlayCanvasGroup.blocksRaycasts = true;
            }

            if (pausePanelTransform != null)
            {
                pausePanelTransform.localScale = hiddenPanelScale;
            }

            Sequence sequence = DOTween.Sequence();

            if (overlayCanvasGroup != null)
            {
                sequence.Join(overlayCanvasGroup.DOFade(1f, openDuration));
            }

            if (pausePanelTransform != null)
            {
                sequence.Join(pausePanelTransform.DOScale(shownPanelScale, openDuration).SetEase(Ease.OutBack));
            }

            activeTween = sequence;
        }

        public void HideOverlay()
        {
            KillActiveTween();

            Sequence sequence = DOTween.Sequence();

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.interactable = false;
                overlayCanvasGroup.blocksRaycasts = true;
                sequence.Join(overlayCanvasGroup.DOFade(0f, closeDuration));
            }

            if (pausePanelTransform != null)
            {
                sequence.Join(pausePanelTransform.DOScale(hiddenPanelScale, closeDuration).SetEase(Ease.InSine));
            }

            sequence.OnComplete(HideImmediate);
            activeTween = sequence;
        }

        public void HideImmediate()
        {
            KillActiveTween();

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.interactable = false;
                overlayCanvasGroup.blocksRaycasts = false;
            }

            if (pausePanelTransform != null)
            {
                pausePanelTransform.localScale = shownPanelScale;
            }

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }
        }

        private void HandlePauseClicked()
        {
            onPauseRequested?.Invoke();
        }

        private void HandleResumeClicked()
        {
            onResumeRequested?.Invoke();
        }

        private void StretchOverlayRoot()
        {
            if (overlayRoot == null)
            {
                return;
            }

            RectTransform rect = overlayRoot.transform as RectTransform;

            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void KillActiveTween()
        {
            if (activeTween != null && activeTween.IsActive())
            {
                activeTween.Kill();
            }

            activeTween = null;
        }

        private static void ApplyImage(Image image, Sprite sprite, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.color = color;

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.enabled = sprite != null || color.a > 0f;
        }
    }
}
