using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DictationGame
{
    /// <summary>
    /// Assign two TMP font assets once and apply them across the generated UI.
    /// Put this on the root canvas. Headings use Primary, utility/body/button text uses Secondary.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DictationThemeFonts : MonoBehaviour
    {
        [Header("Theme Fonts")]
        [SerializeField] private TMP_FontAsset primaryFont;
        [SerializeField] private TMP_FontAsset secondaryFont;

        [Header("Scope")]
        [SerializeField] private Transform root;
        [SerializeField] private bool includeInactive = true;
        [SerializeField] private bool applyOnAwake = true;

        [Header("Rules")]
        [SerializeField] private bool useSecondaryForButtonLabels = true;
        [SerializeField] private bool useSecondaryForBodyAndUtilityText = true;
        [SerializeField] private bool usePrimaryForDifficultyBadge = false;

        public TMP_FontAsset PrimaryFont => primaryFont;
        public TMP_FontAsset SecondaryFont => secondaryFont;

        private void Reset()
        {
            if (root == null) root = transform;
        }

        private void Awake()
        {
            if (root == null) root = transform;
            if (applyOnAwake) ApplyThemeFonts();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (root == null) root = transform;
        }
#endif

        [ContextMenu("Apply Theme Fonts")]
        public void ApplyThemeFonts()
        {
            if (root == null) root = transform;
            if (primaryFont == null && secondaryFont == null) return;

            TMP_Text[] allTexts = root.GetComponentsInChildren<TMP_Text>(includeInactive);
            for (int i = 0; i < allTexts.Length; i++)
            {
                TMP_Text text = allTexts[i];
                if (text == null) continue;

                FontRole role = ResolveRole(text);
                TMP_FontAsset target = role == FontRole.Primary
                    ? (primaryFont != null ? primaryFont : secondaryFont)
                    : (secondaryFont != null ? secondaryFont : primaryFont);

                if (target != null)
                    text.font = target;
            }
        }

        private FontRole ResolveRole(TMP_Text text)
        {
            string n = text.name.ToLowerInvariant();
            string parentName = text.transform.parent != null ? text.transform.parent.name.ToLowerInvariant() : string.Empty;

            if (n.Contains("roundtitle") || n.Contains("resulttitle") || n.Contains("summarytitle") ||
                (n == "title") || parentName.Contains("card") && n == "title")
                return FontRole.Primary;

            if (n.Contains("difficulty") && usePrimaryForDifficultyBadge)
                return FontRole.Primary;

            if (text.GetComponentInParent<Button>(true) != null)
                return useSecondaryForButtonLabels ? FontRole.Secondary : FontRole.Primary;

            if (n.Contains("body") || n.Contains("detail") || n.Contains("score") || n.Contains("progress") ||
                n.Contains("feedback") || n.Contains("hint") || n.Contains("placeholder") || n == "text" ||
                n.Contains("listening") || n.Contains("correctanswer") || n.Contains("breakdown"))
                return useSecondaryForBodyAndUtilityText ? FontRole.Secondary : FontRole.Primary;

            return FontRole.Primary;
        }

        private enum FontRole
        {
            Primary,
            Secondary
        }
    }
}
