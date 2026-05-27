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

        [Header("Optional Common Font Targets")]
        [Tooltip("Texts that should use the theme heading font.")]
        [SerializeField] private TMP_Text[] headingFontTexts;

        [Tooltip("Texts that should use the theme body font.")]
        [SerializeField] private TMP_Text[] bodyFontTexts;

        [Tooltip("Texts that should use the theme UI font.")]
        [SerializeField] private TMP_Text[] uiFontTexts;

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
            ApplyCommonFontTargets(theme);
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

                if (theme.BodyFont != null)
                {
                    instructionText.font = theme.BodyFont;
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

                if (theme.BodyFont != null)
                {
                    popupBodyText.font = theme.BodyFont;
                }
            }
        }

        private void ApplyCommonFontTargets(MemoryThemeConfig theme)
        {
            ApplyFontArray(headingFontTexts, theme.HeaderFont);
            ApplyFontArray(bodyFontTexts, theme.BodyFont);
            ApplyFontArray(uiFontTexts, theme.UIFont);
        }

        private static void ApplyFontArray(TMP_Text[] texts, TMP_FontAsset font)
        {
            if (texts == null || font == null)
            {
                return;
            }

            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].font = font;
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
