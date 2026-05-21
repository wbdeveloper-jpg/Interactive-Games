using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryThemeApplier : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image backgroundImage;

        [Header("Header Backgrounds - Optional")]
        [SerializeField] private Image titleBackgroundImage;
        [SerializeField] private Image instructionBackgroundImage;

        [Header("Header Text")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;

        [Header("Popup - Optional")]
        [SerializeField] private Image popupPanelImage;
        [SerializeField] private TMP_Text popupTitleText;
        [SerializeField] private TMP_Text popupBodyText;

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                return;
            }

            ApplyBackground(theme);
            ApplyHeaderBackgrounds(theme);
            ApplyHeaderText(theme);
            ApplyPopup(theme);
        }

        private void ApplyBackground(MemoryThemeConfig theme)
        {
            if (backgroundImage == null)
            {
                return;
            }

            backgroundImage.color = theme.BackgroundColor;

            if (theme.BackgroundSprite != null)
            {
                backgroundImage.sprite = theme.BackgroundSprite;
                backgroundImage.enabled = true;
            }
        }

        private void ApplyHeaderBackgrounds(MemoryThemeConfig theme)
        {
            ApplyOptionalImage(
                titleBackgroundImage,
                theme.TitleBackgroundSprite,
                theme.TitleBackgroundColor);

            ApplyOptionalImage(
                instructionBackgroundImage,
                theme.InstructionBackgroundSprite,
                theme.InstructionBackgroundColor);
        }

        private void ApplyHeaderText(MemoryThemeConfig theme)
        {
            if (titleText != null)
            {
                titleText.color = theme.TitleTextColor;

                if (theme.HeaderFont != null)
                {
                    titleText.font = theme.HeaderFont;
                }
            }

            if (instructionText != null)
            {
                instructionText.color = theme.InstructionTextColor;

                if (theme.HeaderFont != null)
                {
                    instructionText.font = theme.HeaderFont;
                }
            }
        }

        private void ApplyPopup(MemoryThemeConfig theme)
        {
            ApplyOptionalImage(
                popupPanelImage,
                theme.PopupPanelSprite,
                theme.PopupPanelColor);

            if (popupTitleText != null)
            {
                popupTitleText.color = theme.PopupTitleColor;

                if (theme.HeaderFont != null)
                {
                    popupTitleText.font = theme.HeaderFont;
                }
            }

            if (popupBodyText != null)
            {
                popupBodyText.color = theme.PopupBodyColor;

                if (theme.HeaderFont != null)
                {
                    popupBodyText.font = theme.HeaderFont;
                }
            }
        }

        private static void ApplyOptionalImage(Image image, Sprite sprite, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.color = color;

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            image.enabled = sprite != null || color.a > 0f;
        }
    }
}
