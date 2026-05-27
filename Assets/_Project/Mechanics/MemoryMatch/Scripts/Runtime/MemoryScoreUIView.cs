using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryScoreUIView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject scoreRoot;

        [Header("Score UI")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image scoreBackgroundImage;

        [Header("Score Delta Popup")]
        [SerializeField] private TMP_Text scoreDeltaText;
        [SerializeField] private CanvasGroup scoreDeltaCanvasGroup;
        [SerializeField] private RectTransform scoreDeltaRect;
        [SerializeField, Min(0.1f)] private float deltaAnimationDuration = 0.75f;
        [SerializeField, Min(0f)] private float deltaMoveY = 36f;

        [Header("Correct Score Particles")]
        [SerializeField] private ParticleSystem correctScoreParticle;

        private Color scoreTextColor = Color.white;
        private Color positiveDeltaColor = Color.green;
        private Color negativeDeltaColor = Color.red;

        private bool showScoreBackground = true;
        private bool showScoreDeltaPopup = true;
        private bool playCorrectScoreParticle = true;

        private Tween deltaTween;
        private Vector2 deltaOriginalPosition;

        private void Awake()
        {
            if (scoreRoot == null)
            {
                scoreRoot = gameObject;
            }

            if (scoreDeltaRect == null && scoreDeltaText != null)
            {
                scoreDeltaRect = scoreDeltaText.rectTransform;
            }

            if (scoreDeltaCanvasGroup == null && scoreDeltaText != null)
            {
                scoreDeltaCanvasGroup = scoreDeltaText.GetComponent<CanvasGroup>();
            }

            if (scoreDeltaCanvasGroup == null && scoreDeltaText != null)
            {
                scoreDeltaCanvasGroup = scoreDeltaText.gameObject.AddComponent<CanvasGroup>();
            }

            if (scoreDeltaRect != null)
            {
                deltaOriginalPosition = scoreDeltaRect.anchoredPosition;
            }

            HideDeltaImmediate();
        }

        private void OnDestroy()
        {
            KillDeltaTween();
        }

        public void Configure(MemoryDifficultyConfig difficulty)
        {
            bool scoringEnabled = difficulty != null && difficulty.ScoringEnabled && difficulty.ShowScoreUI;

            showScoreBackground = difficulty == null || difficulty.ShowScoreBackground;
            showScoreDeltaPopup = difficulty == null || difficulty.ShowScoreDeltaPopup;
            playCorrectScoreParticle = difficulty == null || difficulty.PlayCorrectScoreParticle;

            SetVisible(scoringEnabled);

            if (scoreBackgroundImage != null)
            {
                scoreBackgroundImage.gameObject.SetActive(showScoreBackground);
            }

            if (scoreDeltaText != null)
            {
                scoreDeltaText.gameObject.SetActive(showScoreDeltaPopup);
            }
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                return;
            }

            scoreTextColor = theme.ScoreTextColor;
            positiveDeltaColor = theme.ScorePositiveDeltaColor;
            negativeDeltaColor = theme.ScoreNegativeDeltaColor;

            if (scoreText != null)
            {
                scoreText.color = scoreTextColor;

                if (theme.UIFont != null)
                {
                    scoreText.font = theme.UIFont;
                }
            }

            if (scoreDeltaText != null && theme.UIFont != null)
            {
                scoreDeltaText.font = theme.UIFont;
            }

            if (scoreBackgroundImage != null)
            {
                scoreBackgroundImage.color = theme.ScoreBackgroundColor;

                if (theme.ScoreBackgroundSprite != null)
                {
                    scoreBackgroundImage.sprite = theme.ScoreBackgroundSprite;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (scoreRoot != null)
            {
                scoreRoot.SetActive(visible);
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
            }
        }

        public void ShowScoreDelta(int delta)
        {
            if (!showScoreDeltaPopup || scoreDeltaText == null || scoreDeltaCanvasGroup == null || scoreDeltaRect == null || delta == 0)
            {
                return;
            }

            KillDeltaTween();

            scoreDeltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
            scoreDeltaText.color = delta > 0 ? positiveDeltaColor : negativeDeltaColor;
            scoreDeltaText.gameObject.SetActive(true);

            scoreDeltaRect.anchoredPosition = deltaOriginalPosition;
            scoreDeltaCanvasGroup.alpha = 1f;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(scoreDeltaRect.DOAnchorPosY(deltaOriginalPosition.y + deltaMoveY, deltaAnimationDuration).SetEase(Ease.OutCubic));
            sequence.Join(scoreDeltaCanvasGroup.DOFade(0f, deltaAnimationDuration).SetEase(Ease.InSine));
            sequence.OnComplete(HideDeltaImmediate);

            deltaTween = sequence;
        }

        public void PlayCorrectParticle()
        {
            if (!playCorrectScoreParticle || correctScoreParticle == null)
            {
                return;
            }

            correctScoreParticle.Play();
        }

        private void HideDeltaImmediate()
        {
            if (scoreDeltaCanvasGroup != null)
            {
                scoreDeltaCanvasGroup.alpha = 0f;
            }

            if (scoreDeltaRect != null)
            {
                scoreDeltaRect.anchoredPosition = deltaOriginalPosition;
            }

            if (scoreDeltaText != null)
            {
                scoreDeltaText.gameObject.SetActive(false);
            }
        }

        private void KillDeltaTween()
        {
            if (deltaTween != null && deltaTween.IsActive())
            {
                deltaTween.Kill();
            }

            deltaTween = null;
        }
    }
}
