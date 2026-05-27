using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryTimerUIView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject timerRoot;

        [Header("UI")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Image timerBackgroundImage;
        [SerializeField] private Image clockIconImage;

        [Header("Responsive Layout")]
        [Tooltip("If enabled, the script will position icon/text depending on icon visibility.")]
        [SerializeField] private bool applyResponsiveLayout = true;

        [Tooltip("Recommended ON. You design the icon/text layout for 'icon + text' in the prefab, and script only changes layout for text-only mode.")]
        [SerializeField] private bool preserveDesignedLayoutWhenClockVisible = true;

        [SerializeField] private RectTransform timerTextRect;
        [SerializeField] private RectTransform clockIconRect;

        [Tooltip("Padding inside the timer area when only timer text is visible.")]
        [SerializeField, Min(0f)] private float textOnlyHorizontalPadding = 10f;

        [SerializeField, Min(0f)] private float textOnlyVerticalPadding = 4f;

        [Header("Fallback Auto Layout - Used Only If Preserve Designed Layout Is Off")]
        [SerializeField, Min(0f)] private float horizontalPadding = 10f;
        [SerializeField, Min(0f)] private float verticalPadding = 4f;
        [SerializeField, Min(8f)] private float clockIconSize = 42f;
        [SerializeField, Min(0f)] private float iconTextSpacing = 8f;

        [Tooltip("Optional. If > 0 and auto-size is enabled on TMP, this max size is used when icon is hidden.")]
        [SerializeField, Min(0f)] private float textOnlyAutoSizeMax = 46f;

        [Header("Warning Animation")]
        [SerializeField] private RectTransform warningPulseTarget;
        [SerializeField, Min(1f)] private float warningPulseScale = 1.08f;
        [SerializeField, Min(0.05f)] private float warningPulseDuration = 0.35f;

        [Header("Warning Audio")]
        [Tooltip("Recommended: add this AudioSource on TimerGroup/TimerWarningAudio.")]
        [SerializeField] private AudioSource tickingAudioSource;

        private Color normalTextColor = Color.white;
        private Color warningTextColor = Color.red;
        private bool showTimerText = true;
        private bool showTimerBackground = true;
        private bool showClockIcon = true;
        private bool pulseOnWarning = true;
        private bool playTickingOnWarning = true;
        private Tween warningPulseTween;
        private Vector3 originalPulseScale = Vector3.one;

        private bool cachedTextAutoSize;
        private float cachedTextFontSizeMax;
        private RectSnapshot originalTextRect;
        private RectSnapshot originalClockRect;
        private bool hasOriginalTextRect;
        private bool hasOriginalClockRect;

        private struct RectSnapshot
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 SizeDelta;
            public Vector2 AnchoredPosition;
            public Vector2 OffsetMin;
            public Vector2 OffsetMax;

            public RectSnapshot(RectTransform rect)
            {
                AnchorMin = rect.anchorMin;
                AnchorMax = rect.anchorMax;
                Pivot = rect.pivot;
                SizeDelta = rect.sizeDelta;
                AnchoredPosition = rect.anchoredPosition;
                OffsetMin = rect.offsetMin;
                OffsetMax = rect.offsetMax;
            }

            public void Restore(RectTransform rect)
            {
                rect.anchorMin = AnchorMin;
                rect.anchorMax = AnchorMax;
                rect.pivot = Pivot;
                rect.sizeDelta = SizeDelta;
                rect.anchoredPosition = AnchoredPosition;
                rect.offsetMin = OffsetMin;
                rect.offsetMax = OffsetMax;
            }
        }

        private void Awake()
        {
            if (timerRoot == null)
            {
                timerRoot = gameObject;
            }

            if (timerTextRect == null && timerText != null)
            {
                timerTextRect = timerText.rectTransform;
            }

            if (clockIconRect == null && clockIconImage != null)
            {
                clockIconRect = clockIconImage.rectTransform;
            }

            CaptureDesignedLayout();

            if (warningPulseTarget == null)
            {
                warningPulseTarget = transform as RectTransform;
            }

            if (warningPulseTarget != null)
            {
                originalPulseScale = warningPulseTarget.localScale;
            }

            if (timerText != null)
            {
                cachedTextAutoSize = timerText.enableAutoSizing;
                cachedTextFontSizeMax = timerText.fontSizeMax;
            }

            if (tickingAudioSource != null)
            {
                tickingAudioSource.playOnAwake = false;
                tickingAudioSource.loop = true;
                tickingAudioSource.spatialBlend = 0f;
            }
        }

        private void OnDestroy()
        {
            StopWarningFeedback();
        }

        public void Configure(MemoryDifficultyConfig difficulty)
        {
            if (difficulty == null)
            {
                showTimerText = true;
                showTimerBackground = true;
                showClockIcon = true;
                pulseOnWarning = true;
                playTickingOnWarning = true;
            }
            else
            {
                showTimerText = difficulty.ShowTimerText;
                showTimerBackground = difficulty.ShowTimerBackground;
                showClockIcon = difficulty.ShowClockIcon;
                pulseOnWarning = difficulty.PulseTimerOnWarning;
                playTickingOnWarning = difficulty.PlayTickingSoundOnWarning;
            }

            ApplyVisibility();
            ApplyResponsiveLayout();
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                return;
            }

            normalTextColor = theme.TimerTextColor;
            warningTextColor = theme.TimerWarningTextColor;

            if (timerText != null)
            {
                timerText.color = normalTextColor;

                if (theme.UIFont != null)
                {
                    timerText.font = theme.UIFont;
                }
            }

            if (timerBackgroundImage != null)
            {
                timerBackgroundImage.color = theme.TimerBackgroundColor;

                if (theme.TimerBackgroundSprite != null)
                {
                    timerBackgroundImage.sprite = theme.TimerBackgroundSprite;
                }
            }

            if (clockIconImage != null)
            {
                clockIconImage.color = theme.ClockIconColor;

                if (theme.ClockIconSprite != null)
                {
                    clockIconImage.sprite = theme.ClockIconSprite;
                }
            }

            if (tickingAudioSource != null)
            {
                tickingAudioSource.clip = theme.TimerWarningTickingLoop;
                tickingAudioSource.loop = true;
                tickingAudioSource.playOnAwake = false;
                tickingAudioSource.spatialBlend = 0f;
            }
        }

        public void SetTimerVisible(bool visible)
        {
            if (timerRoot != null)
            {
                timerRoot.SetActive(visible);
            }
        }

        public void UpdateTime(float remainingSeconds)
        {
            if (timerText == null)
            {
                return;
            }

            timerText.text = FormatTime(remainingSeconds);
        }

        public void SetWarningState(bool isWarning)
        {
            if (timerText != null)
            {
                timerText.color = isWarning ? warningTextColor : normalTextColor;
            }

            if (isWarning)
            {
                StartWarningFeedback();
            }
            else
            {
                StopWarningFeedback();
            }
        }

        public void StopAllFeedback()
        {
            StopWarningFeedback();
        }

        private void CaptureDesignedLayout()
        {
            if (timerTextRect != null)
            {
                originalTextRect = new RectSnapshot(timerTextRect);
                hasOriginalTextRect = true;
            }

            if (clockIconRect != null)
            {
                originalClockRect = new RectSnapshot(clockIconRect);
                hasOriginalClockRect = true;
            }
        }

        private void ApplyVisibility()
        {
            if (timerText != null)
            {
                timerText.gameObject.SetActive(showTimerText);
            }

            if (timerBackgroundImage != null)
            {
                timerBackgroundImage.gameObject.SetActive(showTimerBackground);
            }

            if (clockIconImage != null)
            {
                clockIconImage.gameObject.SetActive(showClockIcon);
            }
        }

        private void ApplyResponsiveLayout()
        {
            if (!applyResponsiveLayout || timerTextRect == null)
            {
                return;
            }

            bool iconVisible = showClockIcon && clockIconRect != null && clockIconImage != null;

            if (timerText != null)
            {
                timerText.alignment = TextAlignmentOptions.Center;
                timerText.enableAutoSizing = cachedTextAutoSize;
            }

            if (iconVisible)
            {
                if (preserveDesignedLayoutWhenClockVisible)
                {
                    if (hasOriginalClockRect)
                    {
                        originalClockRect.Restore(clockIconRect);
                    }

                    if (hasOriginalTextRect)
                    {
                        originalTextRect.Restore(timerTextRect);
                    }

                    if (timerText != null && cachedTextAutoSize)
                    {
                        timerText.fontSizeMax = cachedTextFontSizeMax;
                    }

                    return;
                }

                clockIconRect.anchorMin = new Vector2(0f, 0.5f);
                clockIconRect.anchorMax = new Vector2(0f, 0.5f);
                clockIconRect.pivot = new Vector2(0f, 0.5f);
                clockIconRect.sizeDelta = new Vector2(clockIconSize, clockIconSize);
                clockIconRect.anchoredPosition = new Vector2(horizontalPadding, 0f);

                float textLeft = horizontalPadding + clockIconSize + iconTextSpacing;
                Stretch(timerTextRect, textLeft, horizontalPadding, verticalPadding, verticalPadding);

                if (timerText != null && cachedTextAutoSize)
                {
                    timerText.fontSizeMax = cachedTextFontSizeMax;
                }

                return;
            }

            // Text-only mode: make text use full timer area.
            Stretch(timerTextRect, textOnlyHorizontalPadding, textOnlyHorizontalPadding, textOnlyVerticalPadding, textOnlyVerticalPadding);

            if (timerText != null && cachedTextAutoSize && textOnlyAutoSizeMax > 0f)
            {
                timerText.fontSizeMax = Mathf.Max(cachedTextFontSizeMax, textOnlyAutoSizeMax);
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

        private void StartWarningFeedback()
        {
            if (pulseOnWarning && warningPulseTarget != null && warningPulseTween == null)
            {
                warningPulseTween = warningPulseTarget
                    .DOScale(originalPulseScale * warningPulseScale, warningPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }

            if (playTickingOnWarning &&
                tickingAudioSource != null &&
                tickingAudioSource.clip != null &&
                !tickingAudioSource.isPlaying)
            {
                tickingAudioSource.Play();
            }
        }

        private void StopWarningFeedback()
        {
            if (warningPulseTween != null && warningPulseTween.IsActive())
            {
                warningPulseTween.Kill();
                warningPulseTween = null;
            }

            if (warningPulseTarget != null)
            {
                warningPulseTarget.localScale = originalPulseScale;
            }

            if (tickingAudioSource != null && tickingAudioSource.isPlaying)
            {
                tickingAudioSource.Stop();
            }

            if (timerText != null)
            {
                timerText.color = normalTextColor;
            }
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            int minutes = totalSeconds / 60;
            int secondsPart = totalSeconds % 60;
            return $"{minutes:00}:{secondsPart:00}";
        }
    }
}
