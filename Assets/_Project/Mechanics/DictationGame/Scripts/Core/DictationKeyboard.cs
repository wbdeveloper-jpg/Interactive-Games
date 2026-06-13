using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace DictationGame
{
    public sealed class DictationKeyboard : MonoBehaviour
    {
        private enum PhysicalKeyboardMode
        {
            Disabled,
            DesktopAndWebGL,
            Always
        }

        [Header("Target")]
        [SerializeField] private TMP_InputField targetInputField;

        [Header("Templates - Scene Objects")]
        [Tooltip("Default letter key template. Keep this disabled in the scene.")]
        [SerializeField] private GameObject keyTemplate;
        [Tooltip("Optional separate template for the Space key. Falls back to Key Template.")]
        [SerializeField] private GameObject spaceKeyTemplate;
        [Tooltip("Optional separate template for Backspace. Falls back to Key Template.")]
        [SerializeField] private GameObject backspaceKeyTemplate;

        [Header("Rows")]
        [SerializeField] private Transform row1Container;
        [SerializeField] private Transform row2Container;
        [SerializeField] private Transform row3Container;
        [SerializeField] private Transform row4Container;

        [Header("Special Key Layout")]
        [SerializeField] private float backspacePreferredWidth = 120f;
        [SerializeField] private float spacePreferredWidth = 620f;
        [SerializeField] private float spaceMinWidth = 360f;
        [SerializeField] private bool forceSpecialPreferredWidths = true;

        [Header("Input Rules")]
        [SerializeField] private int maxCharacters = 120;
        [SerializeField] private bool useLowercaseInput = true;
        [SerializeField] private bool trimLeadingSpace = true;

        [Header("Physical Keyboard / Caret")]
        [Tooltip("DesktopAndWebGL lets users type from a real keyboard on PC/Mac/WebGL. Mobile stays custom-keyboard only.")]
        [SerializeField] private PhysicalKeyboardMode physicalKeyboardMode = PhysicalKeyboardMode.DesktopAndWebGL;
        [Tooltip("Keeps mobile from opening the native keyboard. Users can still tap/click inside the field to move the caret.")]
        [SerializeField] private bool keepMobileReadOnly = true;
        [SerializeField] private bool keepCaretVisible = true;
        [SerializeField] private Color caretColor = new Color(0.72f, 0.50f, 0.61f, 1f);
        [SerializeField] private Color selectionColor = new Color(0.93f, 0.74f, 0.84f, 0.45f);

        [Header("Animation")]
        [SerializeField] private float punchScale = 0.16f;
        [SerializeField] private float punchDuration = 0.11f;

        private static readonly string[] Row1 = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
        private static readonly string[] Row2 = { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
        private static readonly string[] Row3 = { "Z", "X", "C", "V", "B", "N", "M" };

        private readonly List<Button> spawnedButtons = new List<Button>(40);
        private readonly List<GameObject> spawnedKeys = new List<GameObject>(40);
        private bool built;

        private void Awake()
        {
            PrepareInputField();
        }

        private void Start()
        {
            BuildKeyboard();
        }

        public void BuildKeyboard()
        {
            if (built) return;

            if (!ValidateSetup())
                return;

            ClearSpawnedKeys();
            SpawnLetterRow(Row1, row1Container);
            SpawnLetterRow(Row2, row2Container);
            SpawnLetterRow(Row3, row3Container);
            SpawnSpecialKey(row3Container, BackspaceTemplate, "BACK", HandleBackspace, backspacePreferredWidth, 90f);
            SpawnSpecialKey(row4Container, SpaceTemplate, "SPACE", () => HandleKey(" "), spacePreferredWidth, spaceMinWidth);

            keyTemplate.SetActive(false);
            if (spaceKeyTemplate != null) spaceKeyTemplate.SetActive(false);
            if (backspaceKeyTemplate != null) backspaceKeyTemplate.SetActive(false);

            built = true;
        }

        public void SetInteractable(bool interactable)
        {
            for (int i = 0; i < spawnedButtons.Count; i++)
            {
                if (spawnedButtons[i] != null)
                    spawnedButtons[i].interactable = interactable;
            }

            if (targetInputField != null)
                targetInputField.interactable = interactable;
        }

        public void ClearInput()
        {
            if (targetInputField == null) return;
            targetInputField.SetTextWithoutNotify(string.Empty);
            SetCaretPosition(0);
        }

        private GameObject SpaceTemplate => spaceKeyTemplate != null ? spaceKeyTemplate : keyTemplate;
        private GameObject BackspaceTemplate => backspaceKeyTemplate != null ? backspaceKeyTemplate : keyTemplate;

        private void PrepareInputField()
        {
            if (targetInputField == null) return;

            bool allowPhysicalKeyboard = ShouldAllowPhysicalKeyboard();
            targetInputField.readOnly = !allowPhysicalKeyboard || (keepMobileReadOnly && IsMobileRuntime());
            targetInputField.shouldHideMobileInput = true;
            targetInputField.keyboardType = TouchScreenKeyboardType.Default;
            targetInputField.characterLimit = Mathf.Max(1, maxCharacters);
            targetInputField.caretBlinkRate = keepCaretVisible ? 0.85f : 0f;
            targetInputField.customCaretColor = true;
            targetInputField.caretColor = caretColor;
            targetInputField.selectionColor = selectionColor;
            targetInputField.interactable = true;
        }

        private bool ShouldAllowPhysicalKeyboard()
        {
            switch (physicalKeyboardMode)
            {
                case PhysicalKeyboardMode.Always:
                    return true;
                case PhysicalKeyboardMode.DesktopAndWebGL:
                    return IsDesktopOrWebGLRuntime();
                default:
                    return false;
            }
        }

        private static bool IsDesktopOrWebGLRuntime()
        {
#if UNITY_WEBGL
            return true;
#else
            RuntimePlatform platform = Application.platform;
            return platform == RuntimePlatform.WindowsPlayer ||
                   platform == RuntimePlatform.WindowsEditor ||
                   platform == RuntimePlatform.OSXPlayer ||
                   platform == RuntimePlatform.OSXEditor ||
                   platform == RuntimePlatform.LinuxPlayer ||
                   platform == RuntimePlatform.LinuxEditor;
#endif
        }

        private static bool IsMobileRuntime()
        {
#if UNITY_IOS || UNITY_ANDROID
            return true;
#else
            RuntimePlatform platform = Application.platform;
            return platform == RuntimePlatform.Android ||
                   platform == RuntimePlatform.IPhonePlayer;
#endif
        }

        private bool ValidateSetup()
        {
            bool ok = true;
            if (targetInputField == null)
            {
                Debug.LogError("[DictationKeyboard] Target Input Field is missing.", this);
                ok = false;
            }
            if (keyTemplate == null)
            {
                Debug.LogError("[DictationKeyboard] Key Template is missing.", this);
                ok = false;
            }
            if (row1Container == null || row2Container == null || row3Container == null || row4Container == null)
            {
                Debug.LogError("[DictationKeyboard] One or more row containers are missing.", this);
                ok = false;
            }
            return ok;
        }

        private void ClearSpawnedKeys()
        {
            for (int i = spawnedKeys.Count - 1; i >= 0; i--)
            {
                if (spawnedKeys[i] != null)
                    Destroy(spawnedKeys[i]);
            }

            spawnedKeys.Clear();
            spawnedButtons.Clear();
        }

        private void SpawnLetterRow(string[] keys, Transform container)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                string visible = keys[i];
                string input = useLowercaseInput ? visible.ToLowerInvariant() : visible;
                SpawnKey(container, keyTemplate, visible, () => HandleKey(input), -1f, -1f);
            }
        }

        private void SpawnSpecialKey(Transform container, GameObject template, string label, System.Action onPress, float preferredWidth, float minWidth)
        {
            SpawnKey(container, template, label, onPress, preferredWidth, minWidth);
        }

        private void SpawnKey(Transform container, GameObject template, string label, System.Action onPress, float preferredWidth, float minWidth)
        {
            if (container == null || template == null) return;

            GameObject key = Instantiate(template, container);
            key.name = $"Key_{label}";
            key.SetActive(true);
            spawnedKeys.Add(key);

            TextMeshProUGUI text = key.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) text.text = label;

            if (forceSpecialPreferredWidths && preferredWidth > 0f)
            {
                LayoutElement layoutElement = key.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = key.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = preferredWidth;
                layoutElement.minWidth = Mathf.Max(0f, minWidth);
                layoutElement.flexibleWidth = 0f;
            }

            Button button = key.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogWarning($"[DictationKeyboard] Template '{template.name}' has no Button component.", template);
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                onPress?.Invoke();
                AnimateKey(key.transform);
            });
            spawnedButtons.Add(button);
        }

        private void HandleKey(string character)
        {
            if (targetInputField == null || string.IsNullOrEmpty(character)) return;

            string current = targetInputField.text ?? string.Empty;
            GetSelectionRange(current.Length, out int start, out int end);

            if (trimLeadingSpace && character == " " && start == 0 && end == 0 && current.Length == 0)
                return;

            int selectedLength = Mathf.Max(0, end - start);
            if (current.Length - selectedLength + character.Length > maxCharacters)
                return;

            string next = current.Remove(start, selectedLength).Insert(start, character);
            targetInputField.SetTextWithoutNotify(next);
            SetCaretPosition(start + character.Length);
            targetInputField.ActivateInputField();
        }

        private void HandleBackspace()
        {
            if (targetInputField == null) return;

            string current = targetInputField.text ?? string.Empty;
            if (string.IsNullOrEmpty(current)) return;

            GetSelectionRange(current.Length, out int start, out int end);

            if (end > start)
            {
                string next = current.Remove(start, end - start);
                targetInputField.SetTextWithoutNotify(next);
                SetCaretPosition(start);
                targetInputField.ActivateInputField();
                return;
            }

            if (start <= 0) return;

            string afterBackspace = current.Remove(start - 1, 1);
            targetInputField.SetTextWithoutNotify(afterBackspace);
            SetCaretPosition(start - 1);
            targetInputField.ActivateInputField();
        }

        private void GetSelectionRange(int textLength, out int start, out int end)
        {
            int caret = Mathf.Clamp(targetInputField.caretPosition, 0, textLength);
            int anchor = Mathf.Clamp(targetInputField.selectionAnchorPosition, 0, textLength);
            int focus = Mathf.Clamp(targetInputField.selectionFocusPosition, 0, textLength);

            // If there is an active selection, use it. Otherwise edit at the caret.
            if (anchor != focus)
            {
                start = Mathf.Min(anchor, focus);
                end = Mathf.Max(anchor, focus);
            }
            else
            {
                start = caret;
                end = caret;
            }

            // When the field was never focused, TMP can report 0 even after text exists.
            // In that case, custom keyboard input should behave naturally by editing at the end.
            if (!targetInputField.isFocused && start == 0 && end == 0 && textLength > 0)
            {
                start = textLength;
                end = textLength;
            }
        }

        private void SetCaretPosition(int position)
        {
            if (targetInputField == null) return;

            int clamped = Mathf.Clamp(position, 0, targetInputField.text != null ? targetInputField.text.Length : 0);
            targetInputField.caretPosition = clamped;
            targetInputField.selectionAnchorPosition = clamped;
            targetInputField.selectionFocusPosition = clamped;
        }

        private void AnimateKey(Transform keyTransform)
        {
            if (keyTransform == null) return;
            keyTransform.DOKill();
            keyTransform.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f).SetUpdate(true);
        }
    }
}
