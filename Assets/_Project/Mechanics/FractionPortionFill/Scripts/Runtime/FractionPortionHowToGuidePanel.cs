using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class FractionPortionHowToGuidePanel : MonoBehaviour
{
    [Header("Guide Images - Assign In Inspector")]
    public List<Sprite> guideImages = new List<Sprite>();

    [Header("Scene References")]
    public Image guideImage;
    public TMP_Text stepText;
    public GameObject emptyStateObject;
    public Button previousButton;
    public Button nextButton;
    public Button continueButton;

    [Header("Display")]
    public bool preserveImageAspect = true;
    public Sprite fallbackSprite;
    [Min(0.05f)] public float pageFadeDuration = 0.18f;
    [Min(0.05f)] public float pageScaleDuration = 0.18f;

    private int currentIndex;

    private void Awake()
    {
        WireButtons();
        ShowPage(0, false);
    }

    private void OnEnable()
    {
        WireButtons();
        ShowPage(0, false);
    }

    public void ResetGuide()
    {
        ShowPage(0, false);
    }

    public void ShowPrevious()
    {
        ShowPage(currentIndex - 1, true);
    }

    public void ShowNext()
    {
        ShowPage(currentIndex + 1, true);
    }

    private void WireButtons()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPrevious);
            previousButton.onClick.AddListener(ShowPrevious);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNext);
            nextButton.onClick.AddListener(ShowNext);
        }
    }

    private void ShowPage(int targetIndex, bool animate)
    {
        int count = guideImages != null ? guideImages.Count : 0;
        if (count <= 0)
        {
            currentIndex = 0;
            if (emptyStateObject != null)
                emptyStateObject.SetActive(true);
            if (guideImage != null)
            {
                guideImage.preserveAspect = preserveImageAspect;
                guideImage.sprite = fallbackSprite;
                guideImage.enabled = fallbackSprite != null;
            }

            if (stepText != null)
                stepText.text = "Add guide images in Inspector";

            SetButtonState(previousButton, false);
            SetButtonState(nextButton, false);
            SetButtonState(continueButton, true);
            return;
        }

        currentIndex = Mathf.Clamp(targetIndex, 0, count - 1);

        if (emptyStateObject != null)
            emptyStateObject.SetActive(false);

        if (guideImage != null)
        {
            guideImage.preserveAspect = preserveImageAspect;
            guideImage.sprite = guideImages[currentIndex];
            guideImage.enabled = guideImage.sprite != null;

            if (animate)
            {
                guideImage.DOKill();
                Color color = guideImage.color;
                color.a = 0f;
                guideImage.color = color;
                guideImage.rectTransform.localScale = Vector3.one * 0.975f;
                guideImage.DOFade(1f, pageFadeDuration).SetUpdate(true);
                guideImage.rectTransform.DOScale(Vector3.one, pageScaleDuration).SetEase(Ease.OutSine).SetUpdate(true);
            }
            else
            {
                Color color = guideImage.color;
                color.a = 1f;
                guideImage.color = color;
                guideImage.rectTransform.localScale = Vector3.one;
            }
        }

        if (stepText != null)
            stepText.text = "Step " + (currentIndex + 1) + " / " + count;

        SetButtonState(previousButton, currentIndex > 0);
        SetButtonState(nextButton, currentIndex < count - 1);
        SetButtonState(continueButton, currentIndex == count - 1);
    }

    private static void SetButtonState(Button button, bool enabled)
    {
        if (button == null)
            return;

        button.interactable = enabled;
        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null)
            group = button.gameObject.AddComponent<CanvasGroup>();
        group.alpha = enabled ? 1f : 0.42f;
    }
}
