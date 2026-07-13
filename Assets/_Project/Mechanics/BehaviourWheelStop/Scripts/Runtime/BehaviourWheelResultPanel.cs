using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    public class BehaviourWheelResultPanel : MonoBehaviour
    {
        [Header("Texts")]
        public TMP_Text titleText;
        public TMP_Text scoreText;
        public TMP_Text correctText;
        public TMP_Text wrongText;
        public TMP_Text starRatingText;

        [Header("Buttons")]
        public Button playAgainButton;
        public Button continueButton;

        public void SetButtons(UnityAction playAgain, UnityAction continueAction)
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveAllListeners();
                playAgainButton.onClick.AddListener(playAgain);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(continueAction);
            }
        }

        public void ShowResult(int score, int correct, int wrong, int total)
        {
            if (titleText != null)
                titleText.text = "Round Complete";

            if (scoreText != null)
                scoreText.text = $"Score: {score}";

            if (correctText != null)
                correctText.text = $"Correct: {correct}";

            if (wrongText != null)
                wrongText.text = $"Wrong: {wrong}";

            int stars = CalculateStars(correct, total);
            if (starRatingText != null)
                starRatingText.text = BuildStars(stars);

            transform.DOKill();
            transform.localScale = Vector3.one * 0.9f;
            transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private static int CalculateStars(int correct, int total)
        {
            if (total <= 0)
                return 0;

            float ratio = (float)correct / total;
            if (ratio >= 0.9f) return 3;
            if (ratio >= 0.6f) return 2;
            if (ratio > 0f) return 1;
            return 0;
        }

        private static string BuildStars(int stars)
        {
            string result = string.Empty;
            for (int i = 0; i < 3; i++)
                result += i < stars ? "★" : "☆";
            return result;
        }
    }
}
