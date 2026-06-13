using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkyFallImageGuidePanel : MonoBehaviour
{
    [Header("Guide Content")]
    public List<Sprite> guideImages = new List<Sprite>();
    public bool resetToFirstPageOnShow = true;
    public bool loopPages = false;

    [Header("UI References")]
    public Image guideImage;
    public TMP_Text pageCounterText;
    public Button previousButton;
    public Button nextButton;
    public GameObject emptyGuideMessageRoot;

    private int currentIndex;

    private void Awake()
    {
        BindButtons();
    }

    private void OnEnable()
    {
        if (resetToFirstPageOnShow)
            currentIndex = 0;

        Refresh();
    }

    public void BindButtons()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(PreviousPage);
            previousButton.onClick.AddListener(PreviousPage);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NextPage);
            nextButton.onClick.AddListener(NextPage);
        }
    }

    public void NextPage()
    {
        if (guideImages == null || guideImages.Count <= 0)
            return;

        if (currentIndex >= guideImages.Count - 1)
        {
            if (loopPages)
                currentIndex = 0;
        }
        else
        {
            currentIndex++;
        }

        Refresh();
    }

    public void PreviousPage()
    {
        if (guideImages == null || guideImages.Count <= 0)
            return;

        if (currentIndex <= 0)
        {
            if (loopPages)
                currentIndex = guideImages.Count - 1;
        }
        else
        {
            currentIndex--;
        }

        Refresh();
    }

    public void ShowFirstPage()
    {
        currentIndex = 0;
        Refresh();
    }

    private void Refresh()
    {
        int count = guideImages != null ? guideImages.Count : 0;
        bool hasImages = count > 0;

        if (guideImage != null)
        {
            guideImage.enabled = hasImages;

            if (hasImages)
            {
                currentIndex = Mathf.Clamp(currentIndex, 0, count - 1);
                guideImage.sprite = guideImages[currentIndex];
                guideImage.preserveAspect = true;
            }
            else
            {
                guideImage.sprite = null;
            }
        }

        if (emptyGuideMessageRoot != null)
            emptyGuideMessageRoot.SetActive(!hasImages);

        if (pageCounterText != null)
            pageCounterText.text = hasImages ? (currentIndex + 1) + " / " + count : "0 / 0";

        if (previousButton != null)
            previousButton.interactable = hasImages && (loopPages || currentIndex > 0);

        if (nextButton != null)
            nextButton.interactable = hasImages && (loopPages || currentIndex < count - 1);
    }
}
