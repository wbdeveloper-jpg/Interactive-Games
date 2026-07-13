using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace NarayanaGames.SpellBotRescue
{
    public enum SpellBotKeyType
    {
        Letter,
        Backspace,
        Clear,
        Fixed
    }

    [RequireComponent(typeof(Button))]
    public class SpellBotKeyboardKey : MonoBehaviour
    {
        [Header("Key Setup")]
        public SpellBotKeyType keyType = SpellBotKeyType.Letter;
        public string letterValue = "A";

        [Header("References")]
        public Button button;
        public Image keyBackground;
        public TextMeshProUGUI label;

        [Header("Press Feel")]
        public float pressScale = 0.92f;
        public float pressDuration = 0.08f;

        private SpellBotRescueManager manager;
        private Vector3 originalScale;

        private void Reset()
        {
            button = GetComponent<Button>();
            keyBackground = GetComponent<Image>();
            label = GetComponentInChildren<TextMeshProUGUI>();
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            originalScale = transform.localScale;
            button.onClick.AddListener(HandlePress);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandlePress);
            }
        }

        public void Initialize(SpellBotRescueManager owner)
        {
            manager = owner;
            RefreshLabel();
        }

        public void RefreshLabel()
        {
            if (label == null)
            {
                return;
            }

            switch (keyType)
            {
                case SpellBotKeyType.Letter:
                    label.text = string.IsNullOrWhiteSpace(letterValue) ? "A" : letterValue.ToUpperInvariant();
                    break;
                case SpellBotKeyType.Backspace:
                    label.text = "BACK";
                    break;
                case SpellBotKeyType.Clear:
                    label.text = "CLEAR";
                    break;
                case SpellBotKeyType.Fixed:
                    label.text = "FIXED";
                    break;
            }
        }

        private void HandlePress()
        {
            transform.DOKill();
            transform.localScale = originalScale;
            transform.DOScale(originalScale * pressScale, pressDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);

            if (manager == null)
            {
                manager = FindObjectOfType<SpellBotRescueManager>();
            }

            if (manager == null)
            {
                Debug.LogWarning("SpellBotKeyboardKey could not find SpellBotRescueManager.", this);
                return;
            }

            switch (keyType)
            {
                case SpellBotKeyType.Letter:
                    manager.OnKeyboardLetter(letterValue);
                    break;
                case SpellBotKeyType.Backspace:
                    manager.OnKeyboardBackspace();
                    break;
                case SpellBotKeyType.Clear:
                    manager.OnKeyboardClear();
                    break;
                case SpellBotKeyType.Fixed:
                    manager.OnKeyboardFixed();
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (keyBackground == null)
            {
                keyBackground = GetComponent<Image>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TextMeshProUGUI>();
            }

            RefreshLabel();
        }
#endif
    }
}
