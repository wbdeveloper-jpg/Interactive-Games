using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace NarayanaGames.SpellBotRescue
{
    public class SpellBotHowToPlayImagePager : MonoBehaviour
    {
        [Header("Image Pages")]
        public Image mainImage;
        public GameObject emptyPlaceholder;
        public Sprite[] pages;
        public bool preserveAspect = true;

        [Header("Navigation Buttons")]
        public Button previousButton;
        public Button nextButton;

        [Header("Animation")]
        public float pageFadeDuration = 0.18f;
        public float pagePunchScale = 0.04f;

        private int currentIndex;

        private void Awake()
        {
            if (previousButton != null)
            {
                previousButton.onClick.AddListener(PreviousPage);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextPage);
            }

            ShowPage(0, false);
        }

        private void OnDestroy()
        {
            if (previousButton != null)
            {
                previousButton.onClick.RemoveListener(PreviousPage);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(NextPage);
            }
        }

        public void NextPage()
        {
            if (pages == null || pages.Length == 0)
            {
                return;
            }

            ShowPage(Mathf.Min(currentIndex + 1, pages.Length - 1), true);
        }

        public void PreviousPage()
        {
            if (pages == null || pages.Length == 0)
            {
                return;
            }

            ShowPage(Mathf.Max(currentIndex - 1, 0), true);
        }

        public void ShowPage(int index, bool animate)
        {
            currentIndex = pages == null || pages.Length == 0 ? 0 : Mathf.Clamp(index, 0, pages.Length - 1);

            if (mainImage != null)
            {
                mainImage.preserveAspect = preserveAspect;
                mainImage.sprite = pages != null && pages.Length > 0 ? pages[currentIndex] : null;
                mainImage.color = mainImage.sprite == null ? new Color(0.86f, 0.90f, 0.96f, 1f) : Color.white;

                if (emptyPlaceholder != null)
                {
                    emptyPlaceholder.SetActive(mainImage.sprite == null);
                }

                if (animate)
                {
                    mainImage.DOKill();
                    mainImage.transform.DOKill();
                    mainImage.DOFade(0f, 0f);
                    mainImage.DOFade(1f, pageFadeDuration).SetUpdate(true);
                    mainImage.transform.localScale = Vector3.one;
                    mainImage.transform.DOPunchScale(Vector3.one * pagePunchScale, pageFadeDuration + 0.08f, 6, 0.82f).SetUpdate(true);
                }
            }

            RefreshButtons();
        }

        private void RefreshButtons()
        {
            int pageCount = pages != null ? pages.Length : 0;

            if (previousButton != null)
            {
                previousButton.interactable = pageCount > 0 && currentIndex > 0;
            }

            if (nextButton != null)
            {
                nextButton.interactable = pageCount > 0 && currentIndex < pageCount - 1;
            }
        }
    }
}
