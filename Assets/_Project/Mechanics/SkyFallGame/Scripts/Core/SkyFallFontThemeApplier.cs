using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteAlways]
public class SkyFallFontThemeApplier : MonoBehaviour
{
    [Header("Fonts")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;

    [Header("Text Groups")]
    public List<TMP_Text> primaryTexts = new List<TMP_Text>();
    public List<TMP_Text> secondaryTexts = new List<TMP_Text>();

    [Header("Behavior")]
    public bool applyOnStart = true;
    public bool autoApplyInEditor = true;

    private void Start()
    {
        if (applyOnStart)
            ApplyFonts();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && autoApplyInEditor)
            ApplyFonts();
    }
#endif

    [ContextMenu("Apply Fonts")]
    public void ApplyFonts()
    {
        ApplyFontList(primaryTexts, primaryFont);
        ApplyFontList(secondaryTexts, secondaryFont != null ? secondaryFont : primaryFont);
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
        }
    }
}
