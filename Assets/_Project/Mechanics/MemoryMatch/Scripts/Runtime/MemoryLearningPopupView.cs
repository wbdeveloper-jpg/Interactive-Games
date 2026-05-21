using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryLearningPopupView : MonoBehaviour
    {
        [Header("Root")]
        [Tooltip("Recommended: this root should be full-screen modal overlay. The 500x400 object should be an inner panel, not the root.")]
        [SerializeField] private GameObject root;
        [SerializeField] private bool hideOnAwake = true;

        [Header("Modal Interaction")]
        [SerializeField] private CanvasGroup rootCanvasGroup;

        [Header("Animation")]
        [SerializeField] private MemoryPopupAnimator popupAnimator;
        [SerializeField] private bool useAnimatedPopup = true;

        [Header("Content")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;

        [Tooltip("This is the optional content/teacher/mascot/pair illustration, not the panel background.")]
        [SerializeField] private Image learningImage;

        [Header("Buttons")]
        [SerializeField] private Button replayAudioButton;
        [SerializeField] private Button continueButton;

        [Header("Optional Auto Continue UI")]
        [Tooltip("Optional. Assign a small text like 'Continuing in 2...' if you want visible countdown.")]
        [SerializeField] private TMP_Text autoContinueCountdownText;

        [Tooltip("Optional. Assign a Slider if you want visual countdown progress.")]
        [SerializeField] private Slider autoContinueProgress;

        private Action onReplayAudio;
        private Action onContinue;

        private bool autoContinueEnabled;
        private bool autoContinuePaused;
        private float autoContinueDuration;
        private Coroutine autoContinueRoutine;
        private bool continueAlreadyRequested;
        private bool interactionEnabled = true;
        private bool currentPairHasAudio;

        private Sprite defaultIllustrationSprite;
        private Color defaultIllustrationColor = Color.white;
        private bool useDefaultIllustrationWhenPairImageMissing = true;

        public bool IsVisible => root != null && root.activeSelf;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = root.GetComponent<CanvasGroup>();
            }

            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = root.AddComponent<CanvasGroup>();
            }

            if (popupAnimator == null)
            {
                popupAnimator = GetComponent<MemoryPopupAnimator>();
            }

            if (hideOnAwake)
            {
                HideImmediate();
            }
        }

        private void OnDestroy()
        {
            StopAutoContinueRoutine();
            ClearButtonListeners();
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                defaultIllustrationSprite = null;
                defaultIllustrationColor = Color.white;
                useDefaultIllustrationWhenPairImageMissing = true;
                return;
            }

            defaultIllustrationSprite = theme.PopupDefaultIllustrationSprite;
            defaultIllustrationColor = theme.PopupDefaultIllustrationColor;
            useDefaultIllustrationWhenPairImageMissing = theme.UseDefaultIllustrationWhenPairImageMissing;
        }

        public void Show(
            MemoryPairDefinition pair,
            Action replayAudioCallback,
            Action continueCallback,
            bool enableAutoContinue = false,
            float narrationDuration = 0f,
            float delayAfterNarration = 1.25f,
            float noAudioAutoContinueDelay = 2.5f)
        {
            continueAlreadyRequested = false;
            autoContinuePaused = false;
            interactionEnabled = true;
            onReplayAudio = replayAudioCallback;
            onContinue = continueCallback;

            autoContinueEnabled = enableAutoContinue;
            autoContinueDuration = CalculateAutoContinueDuration(
                narrationDuration,
                delayAfterNarration,
                noAudioAutoContinueDelay);

            if (root != null)
            {
                root.SetActive(true);
                root.transform.SetAsLastSibling();
            }

            ApplyTextContent(pair);
            ApplyImageContent(pair);
            SetupButtons(pair);
            ApplyInteractionState();

            if (useAnimatedPopup && popupAnimator != null)
            {
                popupAnimator.Open();
            }

            RestartAutoContinueTimer();
        }

        public void Hide()
        {
            StopAutoContinueRoutine();

            if (autoContinueCountdownText != null)
            {
                autoContinueCountdownText.gameObject.SetActive(false);
            }

            if (autoContinueProgress != null)
            {
                autoContinueProgress.gameObject.SetActive(false);
                autoContinueProgress.value = 0f;
            }

            if (root == null)
            {
                return;
            }

            if (useAnimatedPopup && popupAnimator != null && root.activeInHierarchy)
            {
                popupAnimator.Close(() => root.SetActive(false));
            }
            else
            {
                root.SetActive(false);
            }
        }

        public void SetAutoContinuePaused(bool paused)
        {
            autoContinuePaused = paused;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
            ApplyInteractionState();
        }

        private void HideImmediate()
        {
            StopAutoContinueRoutine();

            if (popupAnimator != null)
            {
                popupAnimator.HideImmediate();
            }

            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void ApplyTextContent(MemoryPairDefinition pair)
        {
            if (titleText != null)
            {
                string title = pair != null && !string.IsNullOrWhiteSpace(pair.LearningTitle)
                    ? pair.LearningTitle
                    : "Correct!";

                titleText.text = title;
            }

            if (bodyText != null)
            {
                string body = pair != null ? pair.LearningText : string.Empty;
                bodyText.text = string.IsNullOrWhiteSpace(body)
                    ? "Good job! You found the correct match."
                    : body;
            }
        }

        private void ApplyImageContent(MemoryPairDefinition pair)
        {
            if (learningImage == null)
            {
                return;
            }

            Sprite imageToShow = null;
            Color imageColor = Color.white;

            if (pair != null && pair.LearningImage != null)
            {
                imageToShow = pair.LearningImage;
                imageColor = Color.white;
            }
            else if (useDefaultIllustrationWhenPairImageMissing && defaultIllustrationSprite != null)
            {
                imageToShow = defaultIllustrationSprite;
                imageColor = defaultIllustrationColor;
            }

            bool hasImage = imageToShow != null;
            learningImage.gameObject.SetActive(hasImage);

            if (hasImage)
            {
                learningImage.sprite = imageToShow;
                learningImage.color = imageColor;
                learningImage.preserveAspect = true;
            }
        }

        private void SetupButtons(MemoryPairDefinition pair)
        {
            ClearButtonListeners();

            currentPairHasAudio = pair != null && pair.NarrationAudio != null;

            if (replayAudioButton != null)
            {
                replayAudioButton.gameObject.SetActive(currentPairHasAudio);
                replayAudioButton.onClick.AddListener(HandleReplayAudioClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinueClicked);
            }
        }

        private void ApplyInteractionState()
        {
            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.interactable = interactionEnabled;
                rootCanvasGroup.blocksRaycasts = true;
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.interactable = interactionEnabled && currentPairHasAudio;
            }

            if (continueButton != null)
            {
                continueButton.interactable = interactionEnabled;
            }
        }

        private void HandleReplayAudioClicked()
        {
            if (!interactionEnabled || continueAlreadyRequested)
            {
                return;
            }

            onReplayAudio?.Invoke();
            RestartAutoContinueTimer();
        }

        private void HandleContinueClicked()
        {
            if (!interactionEnabled)
            {
                return;
            }

            RequestContinue();
        }

        private void RequestContinue()
        {
            if (continueAlreadyRequested)
            {
                return;
            }

            continueAlreadyRequested = true;
            StopAutoContinueRoutine();
            onContinue?.Invoke();
        }

        private void RestartAutoContinueTimer()
        {
            StopAutoContinueRoutine();

            if (!autoContinueEnabled || autoContinueDuration <= 0f)
            {
                SetAutoContinueUIVisible(false);
                return;
            }

            autoContinueRoutine = StartCoroutine(AutoContinueRoutine(autoContinueDuration));
        }

        private IEnumerator AutoContinueRoutine(float duration)
        {
            SetAutoContinueUIVisible(true);

            float remaining = duration;

            while (remaining > 0f)
            {
                if (autoContinuePaused)
                {
                    yield return null;
                    continue;
                }

                remaining -= Time.unscaledDeltaTime;
                float clampedRemaining = Mathf.Max(0f, remaining);
                UpdateAutoContinueUI(clampedRemaining, duration);
                yield return null;
            }

            RequestContinue();
        }

        private void StopAutoContinueRoutine()
        {
            if (autoContinueRoutine != null)
            {
                StopCoroutine(autoContinueRoutine);
                autoContinueRoutine = null;
            }
        }

        private void SetAutoContinueUIVisible(bool visible)
        {
            if (autoContinueCountdownText != null)
            {
                autoContinueCountdownText.gameObject.SetActive(visible);
            }

            if (autoContinueProgress != null)
            {
                autoContinueProgress.gameObject.SetActive(visible);
            }
        }

        private void UpdateAutoContinueUI(float remaining, float duration)
        {
            if (autoContinueCountdownText != null)
            {
                int seconds = Mathf.CeilToInt(remaining);
                autoContinueCountdownText.text = $"Continuing in {seconds}...";
            }

            if (autoContinueProgress != null)
            {
                float progress = duration <= 0f ? 1f : 1f - Mathf.Clamp01(remaining / duration);
                autoContinueProgress.value = progress;
            }
        }

        private static float CalculateAutoContinueDuration(
            float narrationDuration,
            float delayAfterNarration,
            float noAudioAutoContinueDelay)
        {
            if (narrationDuration > 0f)
            {
                return Mathf.Max(0f, narrationDuration) + Mathf.Max(0f, delayAfterNarration);
            }

            return Mathf.Max(0f, noAudioAutoContinueDelay);
        }

        private void ClearButtonListeners()
        {
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveListener(HandleReplayAudioClicked);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinueClicked);
            }
        }
    }
}
