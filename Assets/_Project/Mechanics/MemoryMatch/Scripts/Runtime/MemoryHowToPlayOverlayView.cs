using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryHowToPlayOverlayView : MonoBehaviour
    {
        [Header("Entry Button")]
        [SerializeField] private Button howToPlayButton;

        [Header("Entry Button Theme - Optional")]
        [SerializeField] private Image howToPlayButtonBackgroundImage;
        [SerializeField] private Image howToPlayButtonIconImage;

        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private bool showOnActivityStart = false;

        [Header("Panel")]
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private Image overlayBackgroundImage;
        [SerializeField] private Image panelBackgroundImage;

        [Header("Guide Image Steps")]
        [SerializeField] private Image guideImage;
        [SerializeField] private Sprite[] guideSprites;
        [SerializeField] private TMP_Text stepCounterText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button previousButton;

        [Tooltip("If false, body text hides whenever guide images exist.")]
        [SerializeField] private bool showBodyTextWithGuideImages = false;

        [Header("Text Fallback")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startButton;

        [Header("Default Copy")]
        [SerializeField] private string title = "How to Play";
        [TextArea(5, 10)]
        [SerializeField] private string body =
            "1. Tap a card to flip it.\n" +
            "2. Tap another card to find its match.\n" +
            "3. Correct matches stay open.\n" +
            "4. Wrong matches flip back.\n" +
            "5. Use hints only when you need help.\n" +
            "6. Match all pairs before time runs out.";

        [Header("Animation")]
        [SerializeField, Min(0.05f)] private float openDuration = 0.22f;
        [SerializeField, Min(0.05f)] private float closeDuration = 0.16f;
        [SerializeField] private Vector3 hiddenScale = new Vector3(0.92f, 0.92f, 0.92f);

        private Action onShowRequested;
        private Action onClosed;
        private MemorySfxAudioManager sfxAudioManager;
        private Tween activeTween;
        private Vector3 shownScale = Vector3.one;
        private int currentGuideIndex;

        public bool ShowOnActivityStart => showOnActivityStart;

        private void Awake()
        {
            if (root == null) root = gameObject;

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (panelTransform == null) panelTransform = transform as RectTransform;
            if (panelTransform != null) shownScale = panelTransform.localScale;

            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(HandleHowToPlayButtonClicked);
            if (closeButton != null) closeButton.onClick.AddListener(HandleCloseClicked);
            if (startButton != null) startButton.onClick.AddListener(HandleCloseClicked);
            if (nextButton != null) nextButton.onClick.AddListener(HandleNextClicked);
            if (previousButton != null) previousButton.onClick.AddListener(HandlePreviousClicked);

            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;

            UpdateGuideUI();

            if (hideOnAwake) HideImmediate();
        }

        private void OnDestroy()
        {
            if (howToPlayButton != null) howToPlayButton.onClick.RemoveListener(HandleHowToPlayButtonClicked);
            if (closeButton != null) closeButton.onClick.RemoveListener(HandleCloseClicked);
            if (startButton != null) startButton.onClick.RemoveListener(HandleCloseClicked);
            if (nextButton != null) nextButton.onClick.RemoveListener(HandleNextClicked);
            if (previousButton != null) previousButton.onClick.RemoveListener(HandlePreviousClicked);
            KillTween();
        }

        public void Initialize(Action showRequestedCallback, Action closedCallback)
        {
            onShowRequested = showRequestedCallback;
            onClosed = closedCallback;
        }

        public void SetSfxAudioManager(MemorySfxAudioManager manager)
        {
            sfxAudioManager = manager;
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                return;
            }

            ApplyImage(howToPlayButtonBackgroundImage, theme.HowToPlayButtonBackgroundSprite, theme.HowToPlayButtonBackgroundColor);
            ApplyImage(howToPlayButtonIconImage, theme.HowToPlayButtonIconSprite, theme.HowToPlayButtonIconColor);

            if (titleText != null && theme.HeaderFont != null)
            {
                titleText.font = theme.HeaderFont;
            }

            if (bodyText != null && theme.BodyFont != null)
            {
                bodyText.font = theme.BodyFont;
            }

            if (stepCounterText != null && theme.UIFont != null)
            {
                stepCounterText.font = theme.UIFont;
            }
        }

        public void SetButtonVisible(bool visible)
        {
            if (howToPlayButton != null) howToPlayButton.gameObject.SetActive(visible);
        }

        public void Show()
        {
            currentGuideIndex = 0;
            UpdateGuideUI();

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            PlayOpenAnimation();
        }

        public void Hide()
        {
            KillTween();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
            }

            Sequence sequence = DOTween.Sequence();
            if (canvasGroup != null) sequence.Join(canvasGroup.DOFade(0f, closeDuration));
            if (panelTransform != null) sequence.Join(panelTransform.DOScale(hiddenScale, closeDuration).SetEase(Ease.InSine));

            sequence.OnComplete(() =>
            {
                HideImmediate();
                onClosed?.Invoke();
            });

            activeTween = sequence;
        }

        public void HideImmediate()
        {
            KillTween();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panelTransform != null) panelTransform.localScale = shownScale;
            if (root != null) root.SetActive(false);
        }

        private void UpdateGuideUI()
        {
            bool hasGuideImages = guideSprites != null && guideSprites.Length > 0;

            if (guideImage != null)
            {
                guideImage.gameObject.SetActive(hasGuideImages);

                if (hasGuideImages)
                {
                    currentGuideIndex = Mathf.Clamp(currentGuideIndex, 0, guideSprites.Length - 1);
                    guideImage.sprite = guideSprites[currentGuideIndex];
                    guideImage.preserveAspect = true;
                }
            }

            if (bodyText != null)
            {
                bodyText.gameObject.SetActive(!hasGuideImages || showBodyTextWithGuideImages);
            }

            if (stepCounterText != null)
            {
                stepCounterText.gameObject.SetActive(hasGuideImages);

                if (hasGuideImages)
                {
                    stepCounterText.text = $"{currentGuideIndex + 1}/{guideSprites.Length}";
                }
            }

            bool isLastGuide = !hasGuideImages || currentGuideIndex >= guideSprites.Length - 1;

            if (previousButton != null)
            {
                previousButton.gameObject.SetActive(hasGuideImages && guideSprites.Length > 1);
                previousButton.interactable = currentGuideIndex > 0;
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(hasGuideImages && guideSprites.Length > 1 && !isLastGuide);
            }

            if (startButton != null)
            {
                startButton.gameObject.SetActive(!hasGuideImages || isLastGuide);
            }
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

        private void PlayOpenAnimation()
        {
            KillTween();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (panelTransform != null) panelTransform.localScale = hiddenScale;

            Sequence sequence = DOTween.Sequence();
            if (canvasGroup != null) sequence.Join(canvasGroup.DOFade(1f, openDuration));
            if (panelTransform != null) sequence.Join(panelTransform.DOScale(shownScale, openDuration).SetEase(Ease.OutBack));
            activeTween = sequence;
        }

        private void HandleHowToPlayButtonClicked()
        {
            sfxAudioManager?.PlayButtonClick();
            onShowRequested?.Invoke();
        }

        private void HandleCloseClicked()
        {
            sfxAudioManager?.PlayButtonClick();
            Hide();
        }

        private void HandleNextClicked()
        {
            sfxAudioManager?.PlayButtonClick();
            bool hasGuideImages = guideSprites != null && guideSprites.Length > 0;

            if (!hasGuideImages)
            {
                Hide();
                return;
            }

            if (currentGuideIndex < guideSprites.Length - 1)
            {
                currentGuideIndex++;
                UpdateGuideUI();
                return;
            }

            Hide();
        }

        private void HandlePreviousClicked()
        {
            if (guideSprites == null || guideSprites.Length <= 0) return;

            currentGuideIndex = Mathf.Max(0, currentGuideIndex - 1);
            UpdateGuideUI();
        }

        private void KillTween()
        {
            if (activeTween != null && activeTween.IsActive()) activeTween.Kill();
            activeTween = null;
        }
    }
}
