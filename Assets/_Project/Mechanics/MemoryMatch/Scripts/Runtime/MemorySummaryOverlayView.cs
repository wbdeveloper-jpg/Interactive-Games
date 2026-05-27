using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemorySummaryOverlayView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject root;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private bool hideOnAwake = true;

        [Header("Panel")]
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private Image overlayBackgroundImage;
        [SerializeField] private Image panelBackgroundImage;

        [Header("Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text metricsText;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;

        [Header("Copy")]
        [TextArea(1, 3)]
        [SerializeField] private string successBody = "Great work! Here is your activity summary.";
        [TextArea(1, 3)]
        [SerializeField] private string timeUpBody = "Time is up. Here is your activity summary.";

        [Header("Compact Metrics Layout")]
        [SerializeField] private bool useCompactTwoColumnText = true;
        [SerializeField] private string separator = "   |   ";

        [Header("Metrics Visibility")]
        [SerializeField] private bool showActivityPoints = false;
        [SerializeField] private bool showAccuracy = true;
        [SerializeField] private bool showClicks = true;
        [SerializeField] private bool showTime = true;

        [Header("Animation")]
        [SerializeField, Min(0.05f)] private float openDuration = 0.22f;
        [SerializeField, Min(0.05f)] private float closeDuration = 0.16f;
        [SerializeField] private Vector3 hiddenScale = new Vector3(0.92f, 0.92f, 0.92f);

        private Action onContinue;
        private Action onRetry;
        private Tween activeTween;
        private Vector3 shownScale = Vector3.one;

        private void Awake()
        {
            if (root == null) root = gameObject;

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (panelTransform == null) panelTransform = transform as RectTransform;
            if (panelTransform != null) shownScale = panelTransform.localScale;

            if (continueButton != null) continueButton.onClick.AddListener(HandleContinueClicked);
            if (retryButton != null) retryButton.onClick.AddListener(HandleRetryClicked);

            if (hideOnAwake) HideImmediate();
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinueClicked);
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetryClicked);
            KillTween();
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null) return;

            ApplyImage(overlayBackgroundImage, theme.SummaryOverlayBackgroundSprite, theme.SummaryOverlayBackgroundColor);
            ApplyImage(panelBackgroundImage, theme.SummaryPanelSprite, theme.SummaryPanelColor);

            if (titleText != null)
            {
                titleText.color = theme.SummaryTitleColor;
                if (theme.HeaderFont != null) titleText.font = theme.HeaderFont;
            }

            if (bodyText != null)
            {
                bodyText.color = theme.SummaryBodyColor;
                if (theme.BodyFont != null) bodyText.font = theme.BodyFont;
            }

            if (metricsText != null)
            {
                metricsText.color = theme.SummaryMetricsColor;
                if (theme.UIFont != null) metricsText.font = theme.UIFont;
            }
        }

        public void Show(MemoryActivitySummaryResult result, Action continueCallback, Action retryCallback)
        {
            onContinue = continueCallback;
            onRetry = retryCallback;

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            if (titleText != null) titleText.text = result.TimeUp ? "Time Up" : "Completed";
            if (bodyText != null) bodyText.text = result.TimeUp ? timeUpBody : successBody;
            if (metricsText != null) metricsText.text = BuildMetricsText(result);

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
            sequence.OnComplete(HideImmediate);
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

        private string BuildMetricsText(MemoryActivitySummaryResult result)
        {
            if (!useCompactTwoColumnText)
            {
                return BuildTallMetricsText(result);
            }

            string text =
                $"Pairs: {result.MatchedPairs}/{result.TotalPairs}{separator}Attempts: {result.PairAttempts}\n" +
                $"Wrong: {result.WrongAttempts}{separator}Hints: {result.HintsUsed}";

            if (showClicks)
            {
                text += $"{separator}Clicks: {result.CardClicks}";
            }

            if (showAccuracy)
            {
                text += $"\nAccuracy: {result.AccuracyPercent:0}%";
            }

            if (showTime && result.TotalTimeSeconds > 0f)
            {
                text += $"{separator}Time: {FormatTime(result.TimeUsedSeconds)}";

                if (!result.TimeUp)
                {
                    text += $" left {FormatTime(result.TimeRemainingSeconds)}";
                }
            }

            if (showActivityPoints)
            {
                text += $"\nActivity Points: {result.ActivityPoints}";
            }

            return text;
        }

        private string BuildTallMetricsText(MemoryActivitySummaryResult result)
        {
            string text =
                $"Pairs Matched: {result.MatchedPairs}/{result.TotalPairs}\n" +
                $"Pair Attempts: {result.PairAttempts}\n" +
                $"Wrong Attempts: {result.WrongAttempts}\n" +
                $"Hints Used: {result.HintsUsed}";

            if (showClicks) text += $"\nCard Clicks: {result.CardClicks}";
            if (showAccuracy) text += $"\nAccuracy: {result.AccuracyPercent:0}%";

            if (showTime && result.TotalTimeSeconds > 0f)
            {
                text += $"\nTime Used: {FormatTime(result.TimeUsedSeconds)}\nTime Left: {FormatTime(result.TimeRemainingSeconds)}";
            }

            if (showActivityPoints) text += $"\nActivity Points: {result.ActivityPoints}";
            return text;
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

        private void HandleContinueClicked() => onContinue?.Invoke();
        private void HandleRetryClicked() => onRetry?.Invoke();

        private void KillTween()
        {
            if (activeTween != null && activeTween.IsActive()) activeTween.Kill();
            activeTween = null;
        }

        private static void ApplyImage(Image image, Sprite sprite, Color color)
        {
            if (image == null) return;
            image.color = color;
            if (sprite != null) image.sprite = sprite;
            image.enabled = sprite != null || color.a > 0f;
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }
    }
}
