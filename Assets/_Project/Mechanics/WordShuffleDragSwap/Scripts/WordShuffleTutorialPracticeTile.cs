using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WordShuffleDragSwap
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class WordShuffleTutorialPracticeTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private WordShuffleFirstTimeTutorialController owner;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI letterText;
        private Vector2 dragOffset;

        public string Letter { get; private set; }
        public int CurrentIndex { get; private set; }
        public bool IsLocked { get; private set; }
        public RectTransform RectTransform => rectTransform;

        public void Initialize(WordShuffleFirstTimeTutorialController tutorialOwner, string letter, int index)
        {
            CacheReferences();
            owner = tutorialOwner;
            Letter = letter;
            CurrentIndex = index;
            IsLocked = false;

            if (letterText != null)
                letterText.text = letter;

            SetRaycastState(true);
        }

        public void SetIndex(int index)
        {
            CurrentIndex = index;
        }

        public void SetLocked(bool locked, Color lockedColor)
        {
            CacheReferences();
            IsLocked = locked;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = !locked;
                canvasGroup.blocksRaycasts = !locked;
                canvasGroup.alpha = locked ? 0.96f : 1f;
            }

            UnityEngine.UI.Image image = GetComponent<UnityEngine.UI.Image>();
            if (image != null && locked)
                image.color = lockedColor;
        }

        public void SetRaycastState(bool canRaycast)
        {
            CacheReferences();

            if (canvasGroup == null)
                return;

            canvasGroup.interactable = canRaycast && !IsLocked;
            canvasGroup.blocksRaycasts = canRaycast && !IsLocked;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner == null || !owner.CanDragPracticeTile(this) || IsLocked)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                owner.PracticeTileLayer,
                eventData.position,
                owner.UICamera,
                out Vector2 localPointerPosition);

            dragOffset = rectTransform.anchoredPosition - localPointerPosition;
            SetRaycastState(false);
            transform.SetAsLastSibling();
            owner.NotifyPracticeDragStarted(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner == null || !owner.CanDragPracticeTile(this) || IsLocked)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                owner.PracticeTileLayer,
                eventData.position,
                owner.UICamera,
                out Vector2 localPointerPosition);

            rectTransform.anchoredPosition = localPointerPosition + dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner == null)
                return;

            SetRaycastState(true);
            owner.NotifyPracticeTileDropped(this, eventData.position);
        }

        private void CacheReferences()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (letterText == null)
                letterText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
