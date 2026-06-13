using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DictationGame
{
    public sealed class DictationHintSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI hintDisplayText;
        [SerializeField] private Button hintButton;
        [SerializeField] private TextMeshProUGUI hintButtonLabel;

        [Header("Hint Costs")]
        [Min(0)] [SerializeField] private int hint1Cost = 5;
        [Min(0)] [SerializeField] private int hint2Cost = 10;
        [Min(0)] [SerializeField] private int hint3Cost = 15;

        [Header("Text")]
        [SerializeField] private string emptyHintText = "Use hints only if you need help.";
        [SerializeField] private string noMoreHintsText = "No More Hints";

        public event Action<int> OnHintUsed;

        public int CurrentTier => currentTier;
        public int MaxTier => 3;
        public bool HasMoreHints => currentTier < MaxTier;

        private string[] words = Array.Empty<string>();
        private int currentTier;
        private string lastTier3Word = string.Empty;

        private void Awake()
        {
            BindButton();
        }

        public void LoadRound(DictationRoundData data)
        {
            string answer = data != null ? data.AnswerSentence : string.Empty;
            words = SplitWords(answer);
            currentTier = 0;
            lastTier3Word = string.Empty;
            RefreshHintDisplay();
            RefreshButton();
        }

        public void OnHintButtonPressed()
        {
            if (!HasMoreHints || words.Length == 0) return;

            currentTier++;
            int cost = GetCostForTier(currentTier);
            if (currentTier == 3)
                lastTier3Word = BuildTier3();

            OnHintUsed?.Invoke(cost);
            RefreshHintDisplay();
            RefreshButton();
        }

        public void SetInteractable(bool interactable)
        {
            if (hintButton == null) return;
            hintButton.interactable = interactable && HasMoreHints && words.Length > 0;
        }

        private void BindButton()
        {
            if (hintButton == null) return;
            hintButton.onClick.RemoveListener(OnHintButtonPressed);
            hintButton.onClick.AddListener(OnHintButtonPressed);
        }

        private void RefreshHintDisplay()
        {
            if (hintDisplayText == null) return;

            if (currentTier <= 0)
            {
                hintDisplayText.text = emptyHintText;
                return;
            }

            StringBuilder builder = new StringBuilder(128);
            if (currentTier >= 1) builder.AppendLine("<b>Hint 1:</b> " + BuildTier1());
            if (currentTier >= 2) builder.AppendLine("<b>Hint 2:</b> " + BuildTier2());
            if (currentTier >= 3) builder.AppendLine("<b>Hint 3:</b> " + lastTier3Word);
            hintDisplayText.text = builder.ToString().TrimEnd();
        }

        private void RefreshButton()
        {
            if (hintButton == null || hintButtonLabel == null) return;

            if (!HasMoreHints || words.Length == 0)
            {
                hintButtonLabel.text = noMoreHintsText;
                hintButton.interactable = false;
                return;
            }

            int nextTier = currentTier + 1;
            int cost = GetCostForTier(nextTier);
            hintButtonLabel.text = $"Use Hint {nextTier} (-{cost} pts)";
            hintButton.interactable = true;
        }

        private int GetCostForTier(int tier)
        {
            return tier switch
            {
                1 => hint1Cost,
                2 => hint2Cost,
                3 => hint3Cost,
                _ => 0
            };
        }

        private string BuildTier1()
        {
            StringBuilder builder = new StringBuilder(64);
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (string.IsNullOrEmpty(word)) continue;
                builder.Append(word[0]);
                builder.Append(new string('_', Mathf.Max(0, word.Length - 1)));
                if (i < words.Length - 1) builder.Append("  ");
            }
            return builder.ToString();
        }

        private string BuildTier2()
        {
            StringBuilder builder = new StringBuilder(64);
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (string.IsNullOrEmpty(word)) continue;
                builder.Append(new string('_', word.Length));
                builder.Append($"({word.Length})");
                if (i < words.Length - 1) builder.Append("  ");
            }
            return builder.ToString();
        }

        private string BuildTier3()
        {
            if (words.Length == 0) return string.Empty;
            if (words.Length == 1) return $"The word is: \"{words[0]}\"";
            if (words.Length == 2) return $"Word 2 is: \"{words[1]}\"";

            int index = UnityEngine.Random.Range(1, words.Length - 1);
            return $"Word {index + 1} is: \"{words[index]}\"";
        }

        private static string[] SplitWords(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence)) return Array.Empty<string>();
            return sentence.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
