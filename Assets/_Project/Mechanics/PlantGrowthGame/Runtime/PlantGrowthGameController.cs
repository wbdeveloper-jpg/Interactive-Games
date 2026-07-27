using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PlantGrowthGame
{
    [DisallowMultipleComponent]
    public sealed class PlantGrowthGameController : MonoBehaviour
    {
        [Header("Stage Artwork")]
        [SerializeField] private Sprite[] stageSprites = new Sprite[7];
        [SerializeField] private int[] correctOptionIndexes = { 0, 1, 2, 0, 1 };

        [Header("Generated UI References")]
        [SerializeField] private Image stageImage;
        [SerializeField] private CanvasGroup stageCanvasGroup;
        [SerializeField] private Button[] optionButtons = new Button[3];
        [SerializeField] private Button primaryActionButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button soundButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private CanvasGroup feedbackCanvasGroup;
        [SerializeField] private Text feedbackText;

        [Header("Timing")]
        [Min(0.05f)]
        [SerializeField] private float transitionDuration = 0.35f;
        [Min(0.1f)]
        [SerializeField] private float correctFeedbackDuration = 0.9f;
        [Min(0.1f)]
        [SerializeField] private float wrongFeedbackDuration = 0.75f;
        [SerializeField] private bool pauseSceneTime = true;

        [Header("Optional Audio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip correctSound;
        [SerializeField] private AudioClip wrongSound;
        [SerializeField] private AudioClip stageChangeSound;
        [SerializeField] private AudioClip completionSound;

        [Header("Callbacks")]
        [SerializeField] private UnityEvent onGameStarted = new UnityEvent();
        [SerializeField] private UnityEvent onGameCompleted = new UnityEvent();
        [SerializeField] private UnityEvent onExitRequested = new UnityEvent();

        private static readonly string[] CorrectMessages =
        {
            "Great! Water helps the seed begin to grow.",
            "Good choice! Warmth helps the seed germinate.",
            "Yes! Sunlight helps the new leaves grow.",
            "Well done! The bee helps pollinate the flowers.",
            "Correct! Sunlight helps the tomatoes ripen."
        };

        private int currentStage;
        private bool isTransitioning;
        private bool isPaused;
        private bool soundEnabled = true;
        private bool hasCompleted;
        private float timeScaleBeforePause = 1f;
        private Sequence feedbackSequence;
        private Sequence pauseSequence;

        public int CurrentStage => currentStage;
        public bool IsPaused => isPaused;
        public bool SoundEnabled => soundEnabled;

        private void Awake()
        {
            BindButtons();
            ConfigureAudio();
            SetPaused(false, false);
            ShowStageImmediate(0);
        }

        private void OnDisable()
        {
            DOTween.Kill(this);

            if (isPaused && pauseSceneTime)
            {
                Time.timeScale = timeScaleBeforePause;
            }
        }

        private void BindButtons()
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int capturedIndex = i;
                if (optionButtons[i] != null)
                {
                    optionButtons[i].onClick.AddListener(
                        () => HandleOptionSelected(capturedIndex));
                }
            }

            AddListener(primaryActionButton, HandlePrimaryAction);
            AddListener(pauseButton, () => SetPaused(true, true));
            AddListener(soundButton, ToggleSound);
            AddListener(resumeButton, () => SetPaused(false, true));
            AddListener(restartButton, RestartGame);
            AddListener(exitButton, HandleExitRequested);
        }

        private static void AddListener(Button button, UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private void ConfigureAudio()
        {
            if (musicSource != null)
            {
                musicSource.loop = true;
                musicSource.playOnAwake = false;

                if (backgroundMusic != null)
                {
                    musicSource.clip = backgroundMusic;
                    musicSource.Play();
                }
            }

            if (sfxSource != null)
            {
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void HandlePrimaryAction()
        {
            if (isTransitioning || isPaused)
            {
                return;
            }

            if (currentStage == 0)
            {
                onGameStarted.Invoke();
                StartCoroutine(ChangeStageRoutine(1));
                return;
            }

            if (currentStage == stageSprites.Length - 1)
            {
                if (hasCompleted)
                {
                    return;
                }

                StartCoroutine(CompleteGameRoutine());
            }
        }

        private void HandleOptionSelected(int optionIndex)
        {
            if (isTransitioning || isPaused || currentStage <= 0 ||
                currentStage >= stageSprites.Length - 1)
            {
                return;
            }

            int answerArrayIndex = currentStage - 1;
            int expectedOption = answerArrayIndex < correctOptionIndexes.Length
                ? correctOptionIndexes[answerArrayIndex]
                : 0;

            if (optionIndex == expectedOption)
            {
                StartCoroutine(CorrectAnswerRoutine(answerArrayIndex));
            }
            else
            {
                ShowTemporaryFeedback(
                    "That does not help the plant. Try another one!",
                    wrongFeedbackDuration,
                    new Color(1f, 0.86f, 0.78f, 0.98f));
                PlaySound(wrongSound);
            }
        }

        private IEnumerator CorrectAnswerRoutine(int answerArrayIndex)
        {
            isTransitioning = true;
            SetGameplayInput(false);
            PlaySound(correctSound);

            if (stageImage != null)
            {
                stageImage.rectTransform.DOKill();
                stageImage.rectTransform
                    .DOPunchScale(Vector3.one * 0.012f, 0.28f, 5, 0.45f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetTarget(this);
            }

            string message = answerArrayIndex < CorrectMessages.Length
                ? CorrectMessages[answerArrayIndex]
                : "Great choice!";
            ShowTemporaryFeedback(
                message,
                correctFeedbackDuration,
                new Color(0.88f, 1f, 0.78f, 0.98f));

            yield return WaitUnscaled(correctFeedbackDuration);
            yield return FadeToStage(currentStage + 1);

            isTransitioning = false;
            SetGameplayInput(true);
        }

        private IEnumerator ChangeStageRoutine(int stageIndex)
        {
            isTransitioning = true;
            SetGameplayInput(false);
            yield return FadeToStage(stageIndex);
            isTransitioning = false;
            SetGameplayInput(true);
        }

        private IEnumerator FadeToStage(int stageIndex)
        {
            float halfDuration = Mathf.Max(0.025f, transitionDuration * 0.5f);

            if (stageCanvasGroup != null)
            {
                Tween fadeOut = stageCanvasGroup
                    .DOFade(0f, halfDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .SetTarget(this);
                yield return fadeOut.WaitForCompletion();
            }

            ShowStageImmediate(stageIndex);
            PlaySound(stageChangeSound);

            if (stageCanvasGroup != null)
            {
                stageCanvasGroup.alpha = 0f;
            }

            if (stageImage != null)
            {
                stageImage.rectTransform.localScale = Vector3.one * 1.012f;
            }

            if (stageCanvasGroup != null)
            {
                Tween fadeIn = stageCanvasGroup
                    .DOFade(1f, halfDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetTarget(this);

                if (stageImage != null)
                {
                    stageImage.rectTransform
                        .DOScale(1f, halfDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true)
                        .SetTarget(this);
                }

                yield return fadeIn.WaitForCompletion();
            }
        }

        private void ShowStageImmediate(int stageIndex)
        {
            if (stageSprites == null || stageSprites.Length == 0)
            {
                Debug.LogError("Plant Growth Game has no stage sprites assigned.", this);
                return;
            }

            currentStage = Mathf.Clamp(stageIndex, 0, stageSprites.Length - 1);

            if (stageImage != null)
            {
                stageImage.sprite = stageSprites[currentStage];
                stageImage.enabled = stageImage.sprite != null;
            }

            if (stageCanvasGroup != null)
            {
                stageCanvasGroup.alpha = 1f;
            }

            if (stageImage != null)
            {
                stageImage.rectTransform.localScale = Vector3.one;
            }

            bool showsChoices = currentStage > 0 &&
                currentStage < stageSprites.Length - 1;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    optionButtons[i].gameObject.SetActive(showsChoices);
                }
            }

            if (primaryActionButton != null)
            {
                primaryActionButton.gameObject.SetActive(
                    currentStage == 0 || currentStage == stageSprites.Length - 1);
            }
        }

        private IEnumerator CompleteGameRoutine()
        {
            isTransitioning = true;
            SetGameplayInput(false);
            PlaySound(completionSound);
            ShowTemporaryFeedback(
                "Wonderful work! Your tomatoes are ready to harvest.",
                correctFeedbackDuration,
                new Color(0.88f, 1f, 0.78f, 0.98f));

            yield return WaitUnscaled(correctFeedbackDuration);
            hasCompleted = true;
            onGameCompleted.Invoke();
            isTransitioning = false;
            SetGameplayInput(true);
        }

        private void ShowTemporaryFeedback(
            string message,
            float duration,
            Color panelColour)
        {
            if (feedbackSequence != null && feedbackSequence.IsActive())
            {
                feedbackSequence.Kill();
            }

            if (feedbackCanvasGroup == null || feedbackText == null)
            {
                return;
            }

            Image panelImage = feedbackCanvasGroup.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = panelColour;
            }

            feedbackText.text = message;
            feedbackCanvasGroup.alpha = 0f;
            feedbackCanvasGroup.gameObject.SetActive(true);
            feedbackCanvasGroup.transform.localScale = Vector3.one * 0.94f;

            feedbackSequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetTarget(this)
                .Append(feedbackCanvasGroup.DOFade(1f, 0.16f).SetEase(Ease.OutQuad))
                .Join(feedbackCanvasGroup.transform
                    .DOScale(1f, 0.2f)
                    .SetEase(Ease.OutBack))
                .AppendInterval(Mathf.Max(0.05f, duration - 0.32f))
                .Append(feedbackCanvasGroup.DOFade(0f, 0.16f).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    if (feedbackCanvasGroup != null)
                    {
                        feedbackCanvasGroup.gameObject.SetActive(false);
                    }
                });
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            feedbackSequence?.Kill();
            pauseSequence?.Kill();
            isTransitioning = false;
            hasCompleted = false;

            if (feedbackCanvasGroup != null)
            {
                feedbackCanvasGroup.alpha = 0f;
                feedbackCanvasGroup.gameObject.SetActive(false);
            }

            SetPaused(false, true);
            ShowStageImmediate(0);
            SetGameplayInput(true);
        }

        public void SetPaused(bool paused)
        {
            SetPaused(paused, true);
        }

        private void SetPaused(bool paused, bool updateTimeScale)
        {
            if (isPaused == paused && updateTimeScale)
            {
                return;
            }

            isPaused = paused;

            if (pauseSceneTime && updateTimeScale)
            {
                if (paused)
                {
                    timeScaleBeforePause = Time.timeScale;
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = timeScaleBeforePause;
                }
            }

            if (pausePanel == null)
            {
                return;
            }

            pauseSequence?.Kill();
            CanvasGroup pauseCanvasGroup = pausePanel.GetComponent<CanvasGroup>();
            Transform pauseCard = pausePanel.transform.Find("Pause Card");

            if (!updateTimeScale)
            {
                pausePanel.SetActive(paused);
                if (pauseCanvasGroup != null)
                {
                    pauseCanvasGroup.alpha = paused ? 1f : 0f;
                }
                return;
            }

            if (paused)
            {
                pausePanel.SetActive(true);
                if (pauseCanvasGroup != null)
                {
                    pauseCanvasGroup.alpha = 0f;
                    pauseCanvasGroup.blocksRaycasts = true;
                }
                if (pauseCard != null)
                {
                    pauseCard.localScale = Vector3.one * 0.9f;
                }

                pauseSequence = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetTarget(this);
                if (pauseCanvasGroup != null)
                {
                    pauseSequence.Append(
                        pauseCanvasGroup.DOFade(1f, 0.18f).SetEase(Ease.OutQuad));
                }
                if (pauseCard != null)
                {
                    pauseSequence.Join(
                        pauseCard.DOScale(1f, 0.24f).SetEase(Ease.OutBack));
                }
            }
            else if (pausePanel.activeSelf)
            {
                pauseSequence = DOTween.Sequence()
                    .SetUpdate(true)
                    .SetTarget(this);
                if (pauseCanvasGroup != null)
                {
                    pauseCanvasGroup.blocksRaycasts = false;
                    pauseSequence.Append(
                        pauseCanvasGroup.DOFade(0f, 0.14f).SetEase(Ease.InQuad));
                }
                if (pauseCard != null)
                {
                    pauseSequence.Join(
                        pauseCard.DOScale(0.94f, 0.14f).SetEase(Ease.InQuad));
                }
                pauseSequence.OnComplete(() =>
                {
                    if (pausePanel != null)
                    {
                        pausePanel.SetActive(false);
                    }
                });
            }
        }

        public void ToggleSound()
        {
            soundEnabled = !soundEnabled;

            if (musicSource != null)
            {
                musicSource.mute = !soundEnabled;
            }

            if (sfxSource != null)
            {
                sfxSource.mute = !soundEnabled;
            }
        }

        private void HandleExitRequested()
        {
            SetPaused(false, true);
            onExitRequested.Invoke();
        }

        private void SetGameplayInput(bool enabled)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (optionButtons[i] != null)
                {
                    optionButtons[i].interactable = enabled;
                }
            }

            if (primaryActionButton != null)
            {
                primaryActionButton.interactable = enabled;
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (soundEnabled && sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
