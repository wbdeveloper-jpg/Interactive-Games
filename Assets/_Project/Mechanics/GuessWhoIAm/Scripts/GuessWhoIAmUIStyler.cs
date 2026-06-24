using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuessWhoIAm
{
    [ExecuteAlways]
    public class GuessWhoIAmUIStyler : MonoBehaviour
    {
        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset primaryFont;
        [SerializeField] private TMP_FontAsset secondaryFont;

        [Header("Primary Font Texts")]
        [SerializeField] private List<TMP_Text> primaryTexts = new List<TMP_Text>();

        [Header("Secondary Font Texts")]
        [SerializeField] private List<TMP_Text> secondaryTexts = new List<TMP_Text>();

        [Header("Text Defaults")]
        [SerializeField] private bool applyOnValidate;
        [SerializeField] private bool autoClassifyByObjectName = true;

        [ContextMenu("Collect Texts From Children")]
        public void CollectTextsFromChildren()
        {
            primaryTexts.Clear();
            secondaryTexts.Clear();

            TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < allTexts.Length; i++)
            {
                TMP_Text text = allTexts[i];
                if (text == null)
                    continue;

                if (ShouldUseSecondary(text))
                    secondaryTexts.Add(text);
                else
                    primaryTexts.Add(text);
            }
        }

        [ContextMenu("Apply Fonts")]
        public void ApplyFonts()
        {
            ApplyFontList(primaryTexts, primaryFont);
            ApplyFontList(secondaryTexts, secondaryFont != null ? secondaryFont : primaryFont);
        }

        private void OnValidate()
        {
            if (applyOnValidate)
                ApplyFonts();
        }

        private void ApplyFontList(List<TMP_Text> texts, TMP_FontAsset font)
        {
            if (texts == null || font == null)
                return;

            for (int i = texts.Count - 1; i >= 0; i--)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    texts.RemoveAt(i);
                    continue;
                }

                text.font = font;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.SetDirty(text);
#endif
            }
        }

        private bool ShouldUseSecondary(TMP_Text text)
        {
            if (!autoClassifyByObjectName || text == null)
                return false;

            string lowerName = text.gameObject.name.ToLowerInvariant();
            return lowerName.Contains("badge")
                || lowerName.Contains("chip")
                || lowerName.Contains("progress")
                || lowerName.Contains("helper")
                || lowerName.Contains("score")
                || lowerName.Contains("coin")
                || lowerName.Contains("small")
                || lowerName.Contains("sub")
                || lowerName.Contains("timer");
        }
    }
}
