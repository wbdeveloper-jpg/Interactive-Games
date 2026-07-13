using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace NarayanaGames.SpellBotRescue
{
    public class SpellBotKeyboardView : MonoBehaviour
    {
        [Header("Keys")]
        public List<SpellBotKeyboardKey> keys = new List<SpellBotKeyboardKey>();
        public SpellBotKeyboardKey fixedKey;

        [Header("Colors")]
        public Color alphabetKeyColor = new Color(0.62f, 0.78f, 0.92f, 1f);
        public Color utilityKeyColor = new Color(0.22f, 0.18f, 0.35f, 1f);
        public Color fixedNeutralColor = new Color(0.55f, 0.57f, 0.60f, 1f);
        public Color fixedReadyColor = new Color(0.10f, 0.70f, 0.38f, 1f);
        public Color darkTextColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        public Color lightTextColor = Color.white;

        private SpellBotRescueManager manager;
        private bool fixedReady;

        public void Initialize(SpellBotRescueManager owner)
        {
            manager = owner;

            if (keys.Count == 0)
            {
                keys.AddRange(GetComponentsInChildren<SpellBotKeyboardKey>(true));
            }

            foreach (SpellBotKeyboardKey key in keys)
            {
                if (key == null)
                {
                    continue;
                }

                key.Initialize(manager);
                ApplyKeyStyle(key);

                if (key.keyType == SpellBotKeyType.Fixed)
                {
                    fixedKey = key;
                }
            }

            SetFixedReady(false);
            SetInputLocked(false);
        }

        public void SetInputLocked(bool locked)
        {
            foreach (SpellBotKeyboardKey key in keys)
            {
                if (key != null && key.button != null)
                {
                    key.button.interactable = !locked;
                }
            }
        }

        public void SetFixedReady(bool ready)
        {
            fixedReady = ready;

            if (fixedKey == null)
            {
                return;
            }

            if (fixedKey.keyBackground != null)
            {
                Color targetColor = fixedReady ? fixedReadyColor : fixedNeutralColor;
                fixedKey.keyBackground.DOKill();
                fixedKey.keyBackground.DOColor(targetColor, 0.16f).SetEase(Ease.OutQuad);

                if (ready)
                {
                    fixedKey.transform.DOKill();
                    fixedKey.transform.localScale = Vector3.one;
                    fixedKey.transform.DOPunchScale(Vector3.one * 0.08f, 0.22f, 6, 0.75f);
                }
            }

            if (fixedKey.label != null)
            {
                fixedKey.label.color = lightTextColor;
            }
        }

        public bool IsFixedReady()
        {
            return fixedReady;
        }

        public void ApplyKeyStyle(SpellBotKeyboardKey key)
        {
            if (key == null)
            {
                return;
            }

            Image image = key.keyBackground;
            TextMeshProUGUI text = key.label;

            if (image == null)
            {
                image = key.GetComponent<Image>();
                key.keyBackground = image;
            }

            if (text == null)
            {
                text = key.GetComponentInChildren<TextMeshProUGUI>();
                key.label = text;
            }

            if (image != null)
            {
                switch (key.keyType)
                {
                    case SpellBotKeyType.Letter:
                        image.color = alphabetKeyColor;
                        break;
                    case SpellBotKeyType.Backspace:
                    case SpellBotKeyType.Clear:
                        image.color = utilityKeyColor;
                        break;
                    case SpellBotKeyType.Fixed:
                        image.color = fixedReady ? fixedReadyColor : fixedNeutralColor;
                        break;
                }
            }

            if (text != null)
            {
                text.color = key.keyType == SpellBotKeyType.Letter ? darkTextColor : lightTextColor;
            }
        }
    }
}
