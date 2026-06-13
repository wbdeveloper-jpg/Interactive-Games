using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImageChoiceRevealGame
{
    [Serializable]
    public class ImageChoiceRevealOptionData
    {
        public ImageChoiceOptionDisplayType displayType = ImageChoiceOptionDisplayType.Image;
        public Sprite optionSprite;
        public string optionText;
        public AudioClip optionAudio;

        public bool HasImage() { return optionSprite != null; }
        public bool HasText() { return !string.IsNullOrWhiteSpace(optionText); }
        public bool IsValid() { return HasImage() || HasText(); }
        public string GetTextFallback(string fallback) { return !string.IsNullOrWhiteSpace(optionText) ? optionText : fallback; }
    }

    [Serializable]
    public class ImageChoiceRevealQuestionData
    {
        [Header("For Inspector Organization Only")]
        public string questionName = "New Question";

        [Header("Question Visual")]
        [Tooltip("Main image shown in the question area. If empty, Correct Option Sprite is used.")]
        public Sprite questionSprite;

        [Tooltip("Optional per-question reveal override. Default uses manager reveal mode.")]
        public ImageChoiceQuestionRevealOverride revealOverride = ImageChoiceQuestionRevealOverride.UseManagerDefault;

        [Tooltip("Optional per-question instruction. Empty means use manager's common instruction.")]
        [TextArea(1, 3)]
        public string instructionOverride;

        [Header("Flexible Answer Options")]
        public ImageChoiceRevealOptionData correctOption = new ImageChoiceRevealOptionData();

        [Tooltip("Wrong answer options. Each can be Image OR Text.")]
        public List<ImageChoiceRevealOptionData> distractorOptions = new List<ImageChoiceRevealOptionData>();

        [Header("Legacy Image Option Fallback")]
        [Tooltip("Old field. Still supported for older scenes. New scenes should use Correct Option above.")]
        public Sprite correctOptionSprite;

        [Tooltip("Old field. Still supported for older scenes. New scenes should use Distractor Options above.")]
        public List<Sprite> distractorSprites = new List<Sprite>();

        [Header("Optional Audio")]
        public AudioClip questionAudio;

        public Sprite GetQuestionSprite()
        {
            if (questionSprite != null) return questionSprite;
            if (correctOption != null && correctOption.optionSprite != null) return correctOption.optionSprite;
            return correctOptionSprite;
        }

        public ImageChoiceRevealOptionData GetCorrectOptionData()
        {
            if (correctOption != null && correctOption.IsValid()) return correctOption;

            return new ImageChoiceRevealOptionData
            {
                displayType = ImageChoiceOptionDisplayType.Image,
                optionSprite = correctOptionSprite != null ? correctOptionSprite : questionSprite,
                optionText = !string.IsNullOrWhiteSpace(questionName) ? questionName : "Correct"
            };
        }

        public List<ImageChoiceRevealOptionData> GetDistractorOptionData()
        {
            List<ImageChoiceRevealOptionData> options = new List<ImageChoiceRevealOptionData>();

            if (distractorOptions != null)
            {
                for (int i = 0; i < distractorOptions.Count; i++)
                    if (distractorOptions[i] != null && distractorOptions[i].IsValid()) options.Add(distractorOptions[i]);
            }

            if (options.Count == 0 && distractorSprites != null)
            {
                for (int i = 0; i < distractorSprites.Count; i++)
                {
                    if (distractorSprites[i] == null) continue;
                    options.Add(new ImageChoiceRevealOptionData
                    {
                        displayType = ImageChoiceOptionDisplayType.Image,
                        optionSprite = distractorSprites[i],
                        optionText = distractorSprites[i].name
                    });
                }
            }

            return options;
        }

        public Sprite GetCorrectOptionSprite()
        {
            ImageChoiceRevealOptionData data = GetCorrectOptionData();
            if (data != null && data.optionSprite != null) return data.optionSprite;
            return correctOptionSprite != null ? correctOptionSprite : questionSprite;
        }

        public bool IsValid()
        {
            return GetQuestionSprite() != null && GetCorrectOptionData() != null && GetCorrectOptionData().IsValid();
        }
    }
}
