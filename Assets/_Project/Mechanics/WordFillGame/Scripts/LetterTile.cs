using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LetterTile : MonoBehaviour
{
    [SerializeField] private TMP_Text letterText;

    private Button button;
    private Action<string, LetterTile> onClick;

    public RectTransform RectTransform { get; private set; }

    private void Awake()
    {
        RectTransform = transform as RectTransform;
        button = GetComponent<Button>();

        if (letterText == null)
            letterText = GetComponentInChildren<TMP_Text>(true);
    }

    public void Setup(char character, Action<string, LetterTile> clickCallback, TMP_FontAsset bodyFont)
    {
        string letter = char.ToUpperInvariant(character).ToString();
        onClick = clickCallback;

        if (letterText != null)
        {
            letterText.text = letter;

            if (bodyFont != null)
                letterText.font = bodyFont;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(letter, this));

        SetInteractable(true);
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
            button.interactable = value;
    }
}
