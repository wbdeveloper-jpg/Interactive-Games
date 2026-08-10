using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MeasurementMix
{
    public class MeasurementConversionController : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text categoryLabel;
        public List<Button> optionButtons = new List<Button>();
        public List<TMP_Text> optionLabels = new List<TMP_Text>();
        public List<Image> optionBackgrounds = new List<Image>();

        [Header("Colours")]
        public Color normalColour = new Color(0.93f, 0.96f, 1f, 1f);
        public Color selectedColour = new Color(0.36f, 0.62f, 1f, 1f);
        public Color hintColour = new Color(1f, 0.78f, 0.2f, 1f);

        public bool InteractionsEnabled { get; private set; }
        public int SelectedIndex { get; private set; } = -1;

        private MeasurementQuestion activeQuestion;

        private void Awake()
        {
            for (int index = 0; index < optionButtons.Count; index++)
            {
                int capturedIndex = index;
                if (optionButtons[index] != null)
                    optionButtons[index].onClick.AddListener(
                        () => SelectOption(capturedIndex));
            }
        }

        public void PrepareQuestion(MeasurementQuestion question)
        {
            activeQuestion = question;
            SelectedIndex = -1;
            InteractionsEnabled = true;

            if (categoryLabel != null)
            {
                categoryLabel.text = question.domain == MeasurementDomain.Mass
                    ? "MASS CONVERSION"
                    : "LIQUID CONVERSION";
            }

            for (int index = 0; index < optionButtons.Count; index++)
            {
                bool visible = index < question.options.Count;
                if (optionButtons[index] != null)
                {
                    optionButtons[index].gameObject.SetActive(visible);
                    optionButtons[index].interactable = visible;
                }

                if (index < optionLabels.Count && optionLabels[index] != null)
                    optionLabels[index].text = visible
                        ? question.options[index].text
                        : string.Empty;

                SetOptionColour(index, normalColour);
            }
        }

        public bool HasSelection()
        {
            return SelectedIndex >= 0;
        }

        public bool IsCorrect()
        {
            return activeQuestion != null &&
                SelectedIndex >= 0 &&
                SelectedIndex < activeQuestion.options.Count &&
                activeQuestion.options[SelectedIndex].isCorrect;
        }

        public void SetInteraction(bool enabled)
        {
            InteractionsEnabled = enabled;
            for (int index = 0; index < optionButtons.Count; index++)
            {
                if (optionButtons[index] != null &&
                    optionButtons[index].gameObject.activeSelf)
                    optionButtons[index].interactable = enabled;
            }
        }

        public void ShowCorrectOptionHint()
        {
            if (activeQuestion == null)
                return;

            for (int index = 0; index < activeQuestion.options.Count; index++)
            {
                if (!activeQuestion.options[index].isCorrect)
                    continue;

                SetOptionColour(index, hintColour);
                if (index < optionButtons.Count && optionButtons[index] != null)
                {
                    RectTransform rect =
                        optionButtons[index].transform as RectTransform;
                    if (rect != null)
                    {
                        rect.DOKill();
                        rect.localScale = Vector3.one;
                        rect.DOPunchScale(Vector3.one * 0.12f, 0.7f, 6, 0.7f)
                            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                    }
                }
                break;
            }
        }

        private void SelectOption(int index)
        {
            if (!InteractionsEnabled ||
                activeQuestion == null ||
                index < 0 ||
                index >= activeQuestion.options.Count)
                return;

            SelectedIndex = index;
            for (int optionIndex = 0;
                 optionIndex < activeQuestion.options.Count;
                 optionIndex++)
            {
                SetOptionColour(
                    optionIndex,
                    optionIndex == SelectedIndex ? selectedColour : normalColour);
            }

            if (index < optionButtons.Count && optionButtons[index] != null)
            {
                RectTransform rect =
                    optionButtons[index].transform as RectTransform;
                if (rect != null)
                {
                    rect.DOKill();
                    rect.localScale = Vector3.one;
                    rect.DOPunchScale(Vector3.one * 0.06f, 0.25f, 4, 0.7f)
                        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                }
            }
        }

        private void SetOptionColour(int index, Color colour)
        {
            if (index < 0 || index >= optionBackgrounds.Count)
                return;

            Image background = optionBackgrounds[index];
            if (background == null)
                return;

            background.DOKill();
            background.DOColor(colour, 0.16f)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
