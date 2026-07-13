using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    [Serializable]
    public class BehaviourWheelHowToPlayPage
    {
        public string title;
        [TextArea(2, 4)] public string description;
        public Sprite image;
    }

    public class BehaviourWheelUI : MonoBehaviour
    {
        [Header("Panels")]
        public CanvasGroup loadingPanel;
        public CanvasGroup howToPlayPanel;
        public CanvasGroup gameplayPanel;
        public CanvasGroup pausePanel;
        public CanvasGroup resultPanel;
        public CanvasGroup feedbackPanel;

        [Header("Loading")]
        public Slider loadingSlider;
        public TMP_Text loadingTitleText;

        [Header("Top Bar")]
        public Image questionCounterBackgroundImage;
        public TMP_Text questionCounterText;
        public TMP_Text questionText;
        public Image scoreBackgroundImage;
        public TMP_Text scoreText;
        public Button pauseButton;

        [Header("Gameplay")]
        public Button stopButton;
        public TMP_Text instructionText;
        public TMP_Text feedbackText;
        public Image feedbackIcon;

        [Header("Feedback Style")]
        [Tooltip("Assign the inner FeedbackCard Image here. The outer FeedbackPanel overlay color will not be changed.")]
        public Image feedbackBackgroundImage;
        public Color correctFeedbackColor = new Color(0.13f, 0.62f, 0.32f, 1f);
        public Color wrongFeedbackColor = new Color(0.88f, 0.18f, 0.18f, 1f);
        public Color feedbackTextColor = Color.white;
        public bool makeFeedbackTextBold = true;

        [Header("How To Play")]
        public Image howToPlayImage;
        public TMP_Text howToPlayTitleText;
        public TMP_Text howToPlayDescriptionText;
        public TMP_Text howToPlayPageText;
        public Button howToPlayPrevButton;
        public Button howToPlayNextButton;
        public Button howToPlayStartButton;
        public List<BehaviourWheelHowToPlayPage> howToPlayPages = new List<BehaviourWheelHowToPlayPage>();

        [Header("Animation")]
        public float panelFadeDuration = 0.22f;
        public float buttonPunchScale = 0.08f;
        public float buttonPunchDuration = 0.16f;

        private int currentHowToPlayPage;
        private readonly List<CanvasGroup> allPanels = new List<CanvasGroup>();

        private void Awake()
        {
            CachePanels();
            EnsureDefaultHowToPlayPages();
        }


        public void HideAllMainPanelsImmediate()
        {
            CachePanels();
            for (int i = 0; i < allPanels.Count; i++)
                SetPanelVisible(allPanels[i], false, false);

            SetPanelVisible(pausePanel, false, false);
            SetPanelVisible(feedbackPanel, false, false);
        }

        public void SetLoadingProgress(float progress)
        {
            if (loadingSlider != null)
                loadingSlider.value = Mathf.Clamp01(progress);
        }

        public void ShowLoading()
        {
            ShowOnly(loadingPanel, false);
        }

        public void ShowGameplay()
        {
            ShowOnly(gameplayPanel, true);
        }

        public void ShowHowToPlay()
        {
            currentHowToPlayPage = 0;
            RefreshHowToPlayPage();
            ShowOnly(howToPlayPanel, true);
        }

        public void ShowPause()
        {
            SetPanelVisible(pausePanel, true, true);
        }

        public void HidePause()
        {
            SetPanelVisible(pausePanel, false, true);
        }

        public void ShowResultPanel()
        {
            ShowOnly(resultPanel, true);
        }

        public void SetGameplayTexts(int questionIndex, int totalQuestions, string question, int score)
        {
            if (questionCounterText != null)
                questionCounterText.text = $"Q {questionIndex}/{totalQuestions}";

            if (questionText != null)
                questionText.text = question;

            SetScore(score);
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        public void SetStopButtonInteractable(bool interactable)
        {
            if (stopButton != null)
                stopButton.interactable = interactable;
        }

        public void PlayStopButtonTapAnimation()
        {
            if (stopButton == null)
                return;

            Transform target = stopButton.transform;
            target.DOKill();
            target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * buttonPunchScale, buttonPunchDuration, 8, 0.7f).SetUpdate(true);
        }

        public void ShowFeedback(bool correct, string selectedAnswer, string correctAnswer, string explanation)
        {
            if (feedbackPanel == null)
                return;

            string message = correct
                ? $"Correct! {correctAnswer}"
                : $"Wrong! Correct answer: {correctAnswer}";

            if (!string.IsNullOrWhiteSpace(explanation))
                message += $"\n{explanation}";

            ApplyFeedbackStyle(correct);

            if (feedbackText != null)
                feedbackText.text = message;

            SetPanelVisible(feedbackPanel, true, true);

            Transform feedbackTransform = feedbackPanel.transform;
            feedbackTransform.DOKill();
            feedbackTransform.localScale = Vector3.one * 0.92f;
            feedbackTransform.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void HideFeedback()
        {
            SetPanelVisible(feedbackPanel, false, true);
        }

        private void ApplyFeedbackStyle(bool correct)
        {
            Image background = GetFeedbackBackgroundImage();
            if (background != null)
                background.color = correct ? correctFeedbackColor : wrongFeedbackColor;

            if (feedbackText != null)
            {
                feedbackText.color = feedbackTextColor;

                if (makeFeedbackTextBold)
                    feedbackText.fontStyle |= FontStyles.Bold;
                else
                    feedbackText.fontStyle &= ~FontStyles.Bold;

                feedbackText.alignment = TextAlignmentOptions.Center;
            }
        }

        private Image GetFeedbackBackgroundImage()
        {
            if (feedbackBackgroundImage != null)
                return feedbackBackgroundImage;

            if (feedbackPanel == null)
                return null;

            // Important: do NOT use the Image on FeedbackPanel itself.
            // FeedbackPanel is the full-screen dim overlay. We only recolor the inner FeedbackCard.
            Image[] childImages = feedbackPanel.GetComponentsInChildren<Image>(true);

            for (int i = 0; i < childImages.Length; i++)
            {
                Image image = childImages[i];
                if (image == null || image == feedbackIcon)
                    continue;

                if (image.gameObject == feedbackPanel.gameObject)
                    continue;

                string lowerName = image.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("card") || lowerName.Contains("background") || lowerName.Contains("bg"))
                {
                    feedbackBackgroundImage = image;
                    return feedbackBackgroundImage;
                }
            }

            for (int i = 0; i < childImages.Length; i++)
            {
                Image image = childImages[i];
                if (image == null || image == feedbackIcon)
                    continue;

                if (image.gameObject == feedbackPanel.gameObject)
                    continue;

                feedbackBackgroundImage = image;
                return feedbackBackgroundImage;
            }

            return null;
        }

        public void NextHowToPlayPage()
        {
            EnsureDefaultHowToPlayPages();
            currentHowToPlayPage = Mathf.Clamp(currentHowToPlayPage + 1, 0, howToPlayPages.Count - 1);
            RefreshHowToPlayPage();
        }

        public void PreviousHowToPlayPage()
        {
            EnsureDefaultHowToPlayPages();
            currentHowToPlayPage = Mathf.Clamp(currentHowToPlayPage - 1, 0, howToPlayPages.Count - 1);
            RefreshHowToPlayPage();
        }

        public void RefreshHowToPlayPage()
        {
            EnsureDefaultHowToPlayPages();
            if (howToPlayPages.Count == 0)
                return;

            currentHowToPlayPage = Mathf.Clamp(currentHowToPlayPage, 0, howToPlayPages.Count - 1);
            BehaviourWheelHowToPlayPage page = howToPlayPages[currentHowToPlayPage];

            if (howToPlayTitleText != null)
                howToPlayTitleText.text = page.title;

            if (howToPlayDescriptionText != null)
                howToPlayDescriptionText.text = page.description;

            if (howToPlayPageText != null)
                howToPlayPageText.text = $"{currentHowToPlayPage + 1}/{howToPlayPages.Count}";

            if (howToPlayImage != null)
            {
                howToPlayImage.sprite = page.image;
                howToPlayImage.enabled = true;
                howToPlayImage.color = page.image == null ? new Color(1f, 1f, 1f, 0.28f) : Color.white;

                TMP_Text[] placeholderTexts = howToPlayImage.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < placeholderTexts.Length; i++)
                    placeholderTexts[i].gameObject.SetActive(page.image == null);
            }

            if (howToPlayPrevButton != null)
                howToPlayPrevButton.interactable = currentHowToPlayPage > 0;

            if (howToPlayNextButton != null)
                howToPlayNextButton.gameObject.SetActive(currentHowToPlayPage < howToPlayPages.Count - 1);

            if (howToPlayStartButton != null)
                howToPlayStartButton.gameObject.SetActive(currentHowToPlayPage >= howToPlayPages.Count - 1);
        }

        public void EnsureDefaultHowToPlayPages()
        {
            if (howToPlayPages != null && howToPlayPages.Count > 0)
                return;

            howToPlayPages = new List<BehaviourWheelHowToPlayPage>
            {
                new BehaviourWheelHowToPlayPage
                {
                    title = "Read",
                    description = "Read the question carefully.",
                    image = null
                },
                new BehaviourWheelHowToPlayPage
                {
                    title = "Watch",
                    description = "Watch the behaviour wheel spin.",
                    image = null
                },
                new BehaviourWheelHowToPlayPage
                {
                    title = "Stop",
                    description = "Tap STOP when the correct behaviour reaches the pointer.",
                    image = null
                }
            };
        }

        private void ShowOnly(CanvasGroup target, bool animate)
        {
            CachePanels();
            for (int i = 0; i < allPanels.Count; i++)
            {
                CanvasGroup panel = allPanels[i];
                if (panel == null)
                    continue;

                SetPanelVisible(panel, panel == target, animate);
            }
        }

        private void SetPanelVisible(CanvasGroup panel, bool visible, bool animate)
        {
            if (panel == null)
                return;

            panel.DOKill();
            panel.blocksRaycasts = visible;
            panel.interactable = visible;
            panel.gameObject.SetActive(true);

            if (animate)
            {
                panel.DOFade(visible ? 1f : 0f, panelFadeDuration).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() =>
                {
                    if (!visible)
                        panel.gameObject.SetActive(false);
                });
            }
            else
            {
                panel.alpha = visible ? 1f : 0f;
                panel.gameObject.SetActive(visible);
            }
        }

        private void CachePanels()
        {
            allPanels.Clear();
            AddPanel(loadingPanel);
            AddPanel(howToPlayPanel);
            AddPanel(gameplayPanel);
            AddPanel(resultPanel);
        }

        private void AddPanel(CanvasGroup panel)
        {
            if (panel != null && !allPanels.Contains(panel))
                allPanels.Add(panel);
        }
    }
}
