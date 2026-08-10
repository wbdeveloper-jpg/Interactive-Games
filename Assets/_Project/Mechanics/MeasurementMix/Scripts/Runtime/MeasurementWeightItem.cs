using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MeasurementMix
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class MeasurementWeightItem : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("Weight")]
        [Min(1)] public int valueInGrams = 100;

        [Header("References")]
        public BalanceScaleController scaleController;
        public RectTransform homeParent;
        public RectTransform dragLayer;
        public TMP_Text valueLabel;
        public Image visual;

        [Header("Hint Style")]
        public Color hintColour = new Color(1f, 0.78f, 0.2f, 1f);

        public bool IsOnPan { get; private set; }

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private Color normalColour;
        private Vector3 normalScale;

        private void Awake()
        {
            CacheReferences();
            RefreshLabel();
        }

        public void Configure(
            int grams,
            BalanceScaleController owner,
            RectTransform home,
            RectTransform dragRoot,
            TMP_Text label,
            Image image)
        {
            valueInGrams = grams;
            scaleController = owner;
            homeParent = home;
            dragLayer = dragRoot;
            valueLabel = label;
            visual = image;
            CacheReferences();
            RefreshLabel();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (scaleController == null || !scaleController.InteractionsEnabled)
                return;

            StopHintAnimation();
            transform.SetParent(dragLayer, true);
            transform.SetAsLastSibling();
            canvasGroup.blocksRaycasts = false;
            rectTransform.localScale = normalScale * 1.08f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (scaleController == null || !scaleController.InteractionsEnabled)
                return;

            float scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition +=
                eventData.delta / Mathf.Max(0.01f, scaleFactor);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            CacheReferences();
            canvasGroup.blocksRaycasts = true;
            rectTransform.localScale = normalScale;

            if (scaleController == null || !scaleController.InteractionsEnabled)
            {
                ReturnHome();
                return;
            }

            scaleController.HandleDrop(
                this,
                eventData.position,
                eventData.pressEventCamera);
        }

        public void PlaceOnPan(RectTransform panContent)
        {
            IsOnPan = true;
            transform.SetParent(panContent, false);
            rectTransform.localScale = normalScale;
        }

        public void ReturnHome()
        {
            IsOnPan = false;
            if (homeParent == null)
                return;

            transform.SetParent(homeParent, false);
            rectTransform.localScale = normalScale;
        }

        public void SetAvailable(bool available)
        {
            if (!available)
                IsOnPan = false;
            gameObject.SetActive(available);
        }

        public void PlayHintAnimation(int pulseCount)
        {
            StopHintAnimation();

            Sequence sequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            int count = Mathf.Max(1, pulseCount);
            for (int index = 0; index < count; index++)
            {
                sequence.Append(rectTransform.DOScale(normalScale * 1.16f, 0.22f)
                    .SetEase(Ease.OutQuad));
                if (visual != null)
                    sequence.Join(visual.DOColor(hintColour, 0.22f));
                sequence.Append(rectTransform.DOScale(normalScale, 0.25f)
                    .SetEase(Ease.InOutQuad));
                if (visual != null)
                    sequence.Join(visual.DOColor(normalColour, 0.25f));
            }
        }

        public void StopHintAnimation()
        {
            if (rectTransform != null)
            {
                rectTransform.DOKill();
                rectTransform.localScale = normalScale;
            }

            if (visual != null)
            {
                visual.DOKill();
                visual.color = normalColour;
            }
        }

        private void CacheReferences()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();
            if (visual == null)
                visual = GetComponent<Image>();

            normalColour = visual != null ? visual.color : Color.white;
            normalScale = Vector3.one;
        }

        private void RefreshLabel()
        {
            if (valueLabel != null)
                valueLabel.text =
                    MeasurementQuestionGenerator.FormatPracticalMass(valueInGrams);
        }
    }
}
