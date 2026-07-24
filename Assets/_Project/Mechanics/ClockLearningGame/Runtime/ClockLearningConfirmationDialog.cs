using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ClockLearningGame
{
    [DisallowMultipleComponent]
    public sealed class ClockLearningConfirmationDialog : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup dialogGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private TextMeshProUGUI cancelButtonText;

        [Header("Default Text")]
        [SerializeField] private string defaultTitle = "Are you sure?";
        [SerializeField] private string defaultConfirmText = "Yes";
        [SerializeField] private string defaultCancelText = "No";

        private Action _onConfirm;
        private Action _onCancel;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            HideInstant();
        }

        private void OnEnable()
        {
            RegisterButtons(true);
        }

        private void OnDisable()
        {
            RegisterButtons(false);
        }

        public void Show(string message, Action onConfirm, Action onCancel = null, string title = null, string confirmText = null, string cancelText = null)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _isOpen = true;

            if (titleText != null) titleText.text = string.IsNullOrWhiteSpace(title) ? defaultTitle : title;
            if (messageText != null) messageText.text = message ?? string.Empty;
            if (confirmButtonText != null) confirmButtonText.text = string.IsNullOrWhiteSpace(confirmText) ? defaultConfirmText : confirmText;
            if (cancelButtonText != null) cancelButtonText.text = string.IsNullOrWhiteSpace(cancelText) ? defaultCancelText : cancelText;

            if (dialogGroup != null)
            {
                dialogGroup.gameObject.SetActive(true);
                dialogGroup.alpha = 1f;
                dialogGroup.interactable = true;
                dialogGroup.blocksRaycasts = true;
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            _isOpen = false;
            _onConfirm = null;
            _onCancel = null;
            HideInstant();
        }

        public void HideInstant()
        {
            if (dialogGroup != null)
            {
                dialogGroup.alpha = 0f;
                dialogGroup.interactable = false;
                dialogGroup.blocksRaycasts = false;
                dialogGroup.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void RegisterButtons(bool register)
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
                if (register) confirmButton.onClick.AddListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
                if (register) cancelButton.onClick.AddListener(Cancel);
            }
        }

        private void Confirm()
        {
            Action callback = _onConfirm;
            Hide();
            callback?.Invoke();
        }

        private void Cancel()
        {
            Action callback = _onCancel;
            Hide();
            callback?.Invoke();
        }
    }
}
