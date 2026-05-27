using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordFillHowToPlayPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private Image instructionImage;
    [SerializeField] private TMP_Text pageText;

    [Header("Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button continueButton;

    [Header("Content")]
    [SerializeField] private string panelTitle = "How To Play";
    [SerializeField] private List<WordFillHowToPlayStep> steps = new List<WordFillHowToPlayStep>();

    private int currentIndex;
    private Action onContinue;

    private void Awake()
    {
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousStep);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextStep);

        if (continueButton != null)
            continueButton.onClick.AddListener(Continue);

        CloseInstant();
    }

    public void ApplyFonts(TMP_FontAsset headingFont, TMP_FontAsset bodyFont)
    {
        if (headingFont != null && titleText != null)
            titleText.font = headingFont;

        if (bodyFont != null)
        {
            if (instructionText != null)
                instructionText.font = bodyFont;

            if (pageText != null)
                pageText.font = bodyFont;
        }
    }

    public void Open(Action continueCallback)
    {
        onContinue = continueCallback;
        currentIndex = 0;

        if (titleText != null)
            titleText.text = panelTitle;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    public void CloseInstant()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void PreviousStep()
    {
        if (currentIndex <= 0)
            return;

        currentIndex--;
        Refresh();
    }

    private void NextStep()
    {
        if (steps == null || steps.Count == 0)
            return;

        if (currentIndex >= steps.Count - 1)
            return;

        currentIndex++;
        Refresh();
    }

    private void Continue()
    {
        CloseInstant();
        onContinue?.Invoke();
        onContinue = null;
    }

    private void Refresh()
    {
        int count = steps != null ? steps.Count : 0;

        if (count == 0)
        {
            if (instructionText != null)
                instructionText.text = "Fill in the missing letters to complete the word.";

            if (instructionImage != null)
            {
                instructionImage.sprite = null;
                instructionImage.enabled = false;
            }

            if (pageText != null)
                pageText.text = "1 / 1";

            SetButtonStates(false, false);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, count - 1);
        WordFillHowToPlayStep step = steps[currentIndex];

        if (instructionText != null)
            instructionText.text = step.instructionText;

        if (instructionImage != null)
        {
            instructionImage.sprite = step.instructionImage;
            instructionImage.enabled = step.instructionImage != null;
            instructionImage.preserveAspect = true;
        }

        if (pageText != null)
            pageText.text = (currentIndex + 1) + " / " + count;

        SetButtonStates(currentIndex > 0, currentIndex < count - 1);
    }

    private void SetButtonStates(bool canGoPrevious, bool canGoNext)
    {
        if (previousButton != null)
            previousButton.interactable = canGoPrevious;

        if (nextButton != null)
            nextButton.interactable = canGoNext;
    }
}
