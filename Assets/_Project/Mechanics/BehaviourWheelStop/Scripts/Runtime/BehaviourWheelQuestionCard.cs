using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    public enum BehaviourWheelQuestionCardDisplayMode
    {
        Disabled,
        AtQuestionStartOnly,
        AtQuestionStartAndOnTap
    }

    /// <summary>
    /// Presents the current question without owning gameplay state. The game manager
    /// pauses/resumes the wheel and excludes visible time from round evaluation.
    /// </summary>
    public class BehaviourWheelQuestionCard : MonoBehaviour
    {
        [Header("Display Behaviour")]
        [Tooltip("Disabled keeps the existing game flow unchanged. The recommended mode also lets the player tap the gameplay question to read it again.")]
        public BehaviourWheelQuestionCardDisplayMode displayMode =
            BehaviourWheelQuestionCardDisplayMode.AtQuestionStartAndOnTap;
        [Min(0f)] public float questionStartDuration = 5f;
        [Min(0f)] public float reopenedDuration = 5f;
        [Range(0f, 1f)] public float transitionDuration = 0.22f;

        [Header("UI References")]
        public CanvasGroup overlayCanvasGroup;
        public RectTransform questionCardPanel;
        public TMP_Text questionCardText;
        public Button continueButton;
        public TMP_Text continueButtonText;
        [Tooltip("CanvasGroup on the normal gameplay question card. It fades out while this expanded card is visible.")]
        public CanvasGroup gameplayQuestionCanvasGroup;
        [Tooltip("An invisible Button on the existing gameplay question card. It does not change the card's appearance.")]
        public Button gameplayQuestionButton;

        [Header("Countdown Button")]
        [Tooltip("When enabled, tapping the countdown button closes the card early. The automatic countdown still works.")]
        public bool allowEarlyContinue = true;
        public string countdownTextFormat = "Continuing in... {0}";
        public string continuingText = "Continuing...";

        [Header("Animation")]
        [Range(0.75f, 1f)] public float hiddenScale = 0.94f;

        public event Action GameplayQuestionRequested;
        public event Action<bool> VisibilityChanged;

        public bool IsVisible { get; private set; }
        public bool AllowsGameplayQuestionTap =>
            displayMode == BehaviourWheelQuestionCardDisplayMode.AtQuestionStartAndOnTap;

        private Coroutine presentationRoutine;
        private Action closedCallback;
        private bool closeRequested;

        private void Awake()
        {
            if (gameplayQuestionButton != null)
                gameplayQuestionButton.onClick.AddListener(HandleGameplayQuestionClicked);

            if (continueButton != null)
                continueButton.onClick.AddListener(HandleContinueClicked);

            ApplyHiddenVisualState();
        }

        public bool ShowAtQuestionStart(string question, Action onClosed)
        {
            if (displayMode == BehaviourWheelQuestionCardDisplayMode.Disabled)
                return false;

            return Show(question, questionStartDuration, onClosed);
        }

        public bool ShowFromGameplayQuestion(string question, Action onClosed)
        {
            if (!AllowsGameplayQuestionTap)
                return false;

            return Show(question, reopenedDuration, onClosed);
        }

        public void HideImmediate(bool invokeClosedCallback)
        {
            if (presentationRoutine != null)
            {
                StopCoroutine(presentationRoutine);
                presentationRoutine = null;
            }

            bool wasVisible = IsVisible;
            IsVisible = false;
            closeRequested = false;
            ApplyHiddenVisualState();

            Action callback = closedCallback;
            closedCallback = null;

            if (wasVisible)
                VisibilityChanged?.Invoke(false);

            if (invokeClosedCallback)
                callback?.Invoke();
        }

        private bool Show(string question, float visibleDuration, Action onClosed)
        {
            if (IsVisible || overlayCanvasGroup == null || questionCardPanel == null || questionCardText == null)
                return false;

            if (presentationRoutine != null)
                StopCoroutine(presentationRoutine);

            questionCardText.text = question ?? string.Empty;
            closedCallback = onClosed;
            IsVisible = true;
            closeRequested = false;

            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;
            questionCardPanel.localScale = Vector3.one * Mathf.Clamp(hiddenScale, 0.75f, 1f);
            if (gameplayQuestionCanvasGroup != null)
            {
                gameplayQuestionCanvasGroup.alpha = 1f;
                gameplayQuestionCanvasGroup.interactable = false;
                gameplayQuestionCanvasGroup.blocksRaycasts = false;
            }

            if (continueButton != null)
                continueButton.interactable = allowEarlyContinue;

            UpdateCountdownText(Mathf.Max(0f, visibleDuration));
            transform.SetAsLastSibling();

            VisibilityChanged?.Invoke(true);
            presentationRoutine = StartCoroutine(PresentationRoutine(Mathf.Max(0f, visibleDuration)));
            return true;
        }

        private IEnumerator PresentationRoutine(float visibleDuration)
        {
            yield return AnimateVisuals(0f, 1f, hiddenScale, 1f, 1f, 0f);

            float timer = 0f;
            while (timer < visibleDuration && !closeRequested)
            {
                UpdateCountdownText(visibleDuration - timer);
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!closeRequested)
                UpdateCountdownText(0f);

            yield return AnimateVisuals(1f, 0f, 1f, hiddenScale, 0f, 1f);
            presentationRoutine = null;
            CompletePresentation();
        }

        private IEnumerator AnimateVisuals(float fromAlpha, float toAlpha, float fromScale, float toScale,
            float gameplayFromAlpha, float gameplayToAlpha)
        {
            float duration = Mathf.Max(0f, transitionDuration);
            if (duration <= 0.001f)
            {
                overlayCanvasGroup.alpha = toAlpha;
                questionCardPanel.localScale = Vector3.one * toScale;
                if (gameplayQuestionCanvasGroup != null)
                    gameplayQuestionCanvasGroup.alpha = gameplayToAlpha;
                yield break;
            }

            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float eased = t * t * (3f - 2f * t);
                overlayCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                questionCardPanel.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
                if (gameplayQuestionCanvasGroup != null)
                    gameplayQuestionCanvasGroup.alpha = Mathf.Lerp(gameplayFromAlpha, gameplayToAlpha, eased);
                yield return null;
            }

            overlayCanvasGroup.alpha = toAlpha;
            questionCardPanel.localScale = Vector3.one * toScale;
            if (gameplayQuestionCanvasGroup != null)
                gameplayQuestionCanvasGroup.alpha = gameplayToAlpha;
        }

        private void CompletePresentation()
        {
            bool wasVisible = IsVisible;
            IsVisible = false;
            closeRequested = false;
            ApplyHiddenVisualState();

            Action callback = closedCallback;
            closedCallback = null;

            if (wasVisible)
                VisibilityChanged?.Invoke(false);

            callback?.Invoke();
        }

        private void ApplyHiddenVisualState()
        {
            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.interactable = false;
                overlayCanvasGroup.blocksRaycasts = false;
            }

            if (questionCardPanel != null)
                questionCardPanel.localScale = Vector3.one;

            if (gameplayQuestionCanvasGroup != null)
            {
                gameplayQuestionCanvasGroup.alpha = 1f;
                gameplayQuestionCanvasGroup.interactable = true;
                gameplayQuestionCanvasGroup.blocksRaycasts = true;
            }
        }

        private void UpdateCountdownText(float remainingTime)
        {
            if (continueButtonText == null)
                return;

            int seconds = Mathf.Max(0, Mathf.CeilToInt(remainingTime));
            if (seconds <= 0)
            {
                continueButtonText.text = string.IsNullOrWhiteSpace(continuingText)
                    ? "Continuing..."
                    : continuingText;
                return;
            }

            string format = string.IsNullOrWhiteSpace(countdownTextFormat)
                ? "Continuing in... {0}"
                : countdownTextFormat;
            try
            {
                continueButtonText.text = string.Format(format, seconds);
            }
            catch (FormatException)
            {
                continueButtonText.text = $"Continuing in... {seconds}";
            }
        }

        private void HandleContinueClicked()
        {
            if (!IsVisible || !allowEarlyContinue)
                return;

            closeRequested = true;
            if (continueButtonText != null)
                continueButtonText.text = string.IsNullOrWhiteSpace(continuingText)
                    ? "Continuing..."
                    : continuingText;
            if (continueButton != null)
                continueButton.interactable = false;
        }

        private void HandleGameplayQuestionClicked()
        {
            if (AllowsGameplayQuestionTap && !IsVisible)
                GameplayQuestionRequested?.Invoke();
        }

        private void OnDestroy()
        {
            if (gameplayQuestionButton != null)
                gameplayQuestionButton.onClick.RemoveListener(HandleGameplayQuestionClicked);

            if (continueButton != null)
                continueButton.onClick.RemoveListener(HandleContinueClicked);
        }
    }
}
