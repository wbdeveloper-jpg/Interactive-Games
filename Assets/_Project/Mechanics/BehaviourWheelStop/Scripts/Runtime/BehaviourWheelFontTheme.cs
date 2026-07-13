using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BehaviourWheelStop
{
    public class BehaviourWheelFontTheme : MonoBehaviour
    {
        [Header("Fonts")]
        public TMP_FontAsset primaryFont;
        public TMP_FontAsset secondaryFont;

        [Header("Manual Groups Optional")]
        public List<TMP_Text> primaryTexts = new List<TMP_Text>();
        public List<TMP_Text> secondaryTexts = new List<TMP_Text>();

        [Header("Auto Apply")]
        public bool applyOnStart = true;
        public bool autoFindMissingTexts = true;
        public bool primaryForButtonsAndTitles = true;

        private void Start()
        {
            if (applyOnStart)
                ApplyFontsToScene();
        }

        public void ApplyFontsToScene()
        {
            if (primaryFont == null && secondaryFont == null)
                return;

            if (autoFindMissingTexts)
                AutoCollectTexts();

            for (int i = 0; i < primaryTexts.Count; i++)
            {
                if (primaryTexts[i] != null && primaryFont != null)
                    primaryTexts[i].font = primaryFont;
            }

            TMP_FontAsset bodyFont = secondaryFont != null ? secondaryFont : primaryFont;
            for (int i = 0; i < secondaryTexts.Count; i++)
            {
                if (secondaryTexts[i] != null && bodyFont != null)
                    secondaryTexts[i].font = bodyFont;
            }
        }

        public void AutoCollectTexts()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            HashSet<TMP_Text> primarySet = new HashSet<TMP_Text>(primaryTexts);
            HashSet<TMP_Text> secondarySet = new HashSet<TMP_Text>(secondaryTexts);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || primarySet.Contains(text) || secondarySet.Contains(text))
                    continue;

                if (ShouldUsePrimary(text))
                {
                    primaryTexts.Add(text);
                    primarySet.Add(text);
                }
                else
                {
                    secondaryTexts.Add(text);
                    secondarySet.Add(text);
                }
            }
        }

        private bool ShouldUsePrimary(TMP_Text text)
        {
            if (!primaryForButtonsAndTitles)
                return false;

            string n = text.gameObject.name.ToLowerInvariant();
            return n.Contains("title") || n.Contains("button") || n.Contains("btn") || n.Contains("score") ||
                   n.Contains("counter") || n.Contains("label") || n.Contains("question");
        }
    }
}
