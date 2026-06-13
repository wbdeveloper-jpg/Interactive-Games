using TMPro;
using UnityEngine;

public class WordFillFontApplier : MonoBehaviour
{
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset primaryFont;
    [SerializeField] private TMP_FontAsset secondaryFont;

    [Header("Root")]
    [SerializeField] private Transform textRoot;

    [Header("Auto Apply")]
    [SerializeField] private bool applyOnAwake = true;

    private void Awake()
    {
        if (applyOnAwake)
            ApplyFonts();
    }

    public void SetFonts(TMP_FontAsset primary, TMP_FontAsset secondary)
    {
        primaryFont = primary;
        secondaryFont = secondary;
        ApplyFonts();
    }

    [ContextMenu("Apply Fonts")]
    public void ApplyFonts()
    {
        Transform root = textRoot != null ? textRoot : transform;
        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            TMP_FontAsset selectedFont = ShouldUsePrimary(text) ? primaryFont : secondaryFont;

            if (selectedFont != null)
                text.font = selectedFont;
        }
    }

    private bool ShouldUsePrimary(TMP_Text text)
    {
        string n = text.gameObject.name.ToLowerInvariant();

        return n.Contains("title")
            || n.Contains("heading")
            || n.Contains("timer")
            || n.Contains("score")
            || n.Contains("complete")
            || n.Contains("pause")
            || n.Contains("loading")
            || n.Contains("feedback")
            || n.Contains("instruction");
    }
}
