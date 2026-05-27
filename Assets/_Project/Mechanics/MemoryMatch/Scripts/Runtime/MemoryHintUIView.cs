using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryHintUIView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject hintRoot;

        [Header("Button")]
        [SerializeField] private Button hintButton;

        [Header("UI")]
        [SerializeField] private TMP_Text hintsRemainingText;
        [SerializeField] private Image hintBackgroundImage;
        [SerializeField] private Image hintIconImage;

        private Action onHintRequested;
        private Color enabledTextColor = Color.white;
        private Color disabledTextColor = Color.gray;
        private bool showHintButton = true;
        private bool showHintsRemainingText = true;
        private bool showHintBackground = true;
        private bool showHintIcon = true;

        private void Awake()
        {
            if (hintRoot == null)
            {
                hintRoot = gameObject;
            }

            if (hintButton != null)
            {
                hintButton.onClick.AddListener(HandleHintButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(HandleHintButtonClicked);
            }
        }

        public void Initialize(Action hintRequestedCallback)
        {
            onHintRequested = hintRequestedCallback;
        }

        public void Configure(MemoryDifficultyConfig difficulty)
        {
            bool hintsEnabled = difficulty != null && difficulty.HintsEnabled && difficulty.MaxHints > 0;

            showHintButton = difficulty == null || difficulty.ShowHintButton;
            showHintsRemainingText = difficulty == null || difficulty.ShowHintsRemainingText;
            showHintBackground = difficulty == null || difficulty.ShowHintBackground;
            showHintIcon = difficulty == null || difficulty.ShowHintIcon;

            SetVisible(hintsEnabled);
            ApplyVisibility();
        }

        public void ApplyTheme(MemoryThemeConfig theme)
        {
            if (theme == null)
            {
                return;
            }

            enabledTextColor = theme.HintTextColor;
            disabledTextColor = theme.HintDisabledTextColor;

            if (hintsRemainingText != null)
            {
                hintsRemainingText.color = enabledTextColor;

                if (theme.UIFont != null)
                {
                    hintsRemainingText.font = theme.UIFont;
                }
            }

            if (hintBackgroundImage != null)
            {
                hintBackgroundImage.color = theme.HintBackgroundColor;

                if (theme.HintBackgroundSprite != null)
                {
                    hintBackgroundImage.sprite = theme.HintBackgroundSprite;
                }
            }

            if (hintIconImage != null)
            {
                hintIconImage.color = theme.HintIconColor;

                if (theme.HintIconSprite != null)
                {
                    hintIconImage.sprite = theme.HintIconSprite;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            if (hintRoot != null)
            {
                hintRoot.SetActive(visible);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (hintButton != null)
            {
                hintButton.interactable = interactable;
            }

            if (hintsRemainingText != null)
            {
                hintsRemainingText.color = interactable ? enabledTextColor : disabledTextColor;
            }
        }

        public void UpdateHintsRemaining(int hintsUsed, int maxHints)
        {
            int remaining = Mathf.Max(0, maxHints - hintsUsed);

            if (hintsRemainingText != null)
            {
                hintsRemainingText.text = $"{remaining}/{maxHints}";
            }
        }

        private void ApplyVisibility()
        {
            if (hintButton != null)
            {
                hintButton.gameObject.SetActive(showHintButton);
            }

            if (hintsRemainingText != null)
            {
                hintsRemainingText.gameObject.SetActive(showHintsRemainingText);
            }

            if (hintBackgroundImage != null)
            {
                hintBackgroundImage.gameObject.SetActive(showHintBackground);
            }

            if (hintIconImage != null)
            {
                hintIconImage.gameObject.SetActive(showHintIcon);
            }
        }

        private void HandleHintButtonClicked()
        {
            onHintRequested?.Invoke();
        }
    }
}
