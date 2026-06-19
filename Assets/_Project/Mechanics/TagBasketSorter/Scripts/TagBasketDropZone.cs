using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TagBasketSorter
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TagBasketDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Basket Data")]
        [Tooltip("Draggable itemTag must match this value.")]
        public string acceptedTag = "Common Noun";
        public string basketId;

        [Header("Visual References")]
        public Image basketImage;
        public Image basketFrontOverlay;
        public TMP_Text titleText;
        [Tooltip("Optional background/badge image behind the basket title text.")]
        public Image titleBackgroundImage;
        public RectTransform placementRoot;

        [Header("Placement Layout")]
        [Min(1)] public int maxItemsPerRow = 3;
        public Vector2 placementCellSize = new Vector2(72f, 72f);
        public Vector2 placementSpacing = new Vector2(8f, 8f);
        public Vector2 placementStartOffset = Vector2.zero;
        public Vector2 placedItemPositionJitter = new Vector2(8f, 6f);
        [Range(0f, 20f)] public float placedItemRotationRange = 7f;

        [Header("Feedback")]
        public bool highlightOnHover = true;
        public float hoverScale = 1.04f;
        public float hoverTweenDuration = 0.08f;

        public TagBasketLevelPanel OwnerLevel { get; private set; }

        private TagBasketSortGameManager gameManager;
        private RectTransform rectTransform;
        private Vector3 originalScale = Vector3.one;
        private int placedItemCount;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            EnsureReferences();
        }

        private void OnDisable()
        {
            if (rectTransform != null)
                rectTransform.DOKill();
        }

        private void OnDestroy()
        {
            if (rectTransform != null)
                rectTransform.DOKill();
        }

        public void Setup(TagBasketSortGameManager manager, TagBasketLevelPanel ownerLevel)
        {
            EnsureReferences();

            gameManager = manager;
            OwnerLevel = ownerLevel;

            if (titleText != null && string.IsNullOrWhiteSpace(titleText.text))
                titleText.text = acceptedTag;

            if (basketFrontOverlay != null)
                basketFrontOverlay.raycastTarget = false;

            EnsureBasketLayering();
        }

        public RectTransform GetPlacementRoot()
        {
            EnsureReferences();
            return placementRoot != null ? placementRoot : rectTransform;
        }

        public Vector2 ReserveNextPlacementPosition()
        {
            int safeMaxPerRow = Mathf.Max(1, maxItemsPerRow);
            int index = placedItemCount++;
            int row = index / safeMaxPerRow;
            int column = index % safeMaxPerRow;
            float rowWidth = (safeMaxPerRow - 1) * (placementCellSize.x + placementSpacing.x);
            float x = placementStartOffset.x + column * (placementCellSize.x + placementSpacing.x) - rowWidth * 0.5f;
            float y = placementStartOffset.y - row * (placementCellSize.y + placementSpacing.y);

            if (OwnerLevel == null || OwnerLevel.useBasketOrganicPlacement)
            {
                x += UnityEngine.Random.Range(-Mathf.Abs(placedItemPositionJitter.x), Mathf.Abs(placedItemPositionJitter.x));
                y += UnityEngine.Random.Range(-Mathf.Abs(placedItemPositionJitter.y), Mathf.Abs(placedItemPositionJitter.y));
            }

            return new Vector2(x, y);
        }

        public float GetRandomPlacedRotation()
        {
            if (OwnerLevel != null && !OwnerLevel.useBasketOrganicPlacement)
                return 0f;

            return UnityEngine.Random.Range(-Mathf.Abs(placedItemRotationRange), Mathf.Abs(placedItemRotationRange));
        }

        public void ClearRuntimePlacedItems()
        {
            placedItemCount = 0;
            EnsureBasketLayering();
        }

        public bool Accepts(TagBasketDraggableItem item)
        {
            if (item == null)
                return false;

            return MatchesTag(item.itemTag, acceptedTag);
        }

        public bool AcceptsTag(string tagValue)
        {
            return MatchesTag(tagValue, acceptedTag);
        }

        public void PlayHintPulse(float scaleAmount, float duration, int loopCount = 4)
        {
            EnsureReferences();
            if (rectTransform == null)
                return;

            int safeLoopCount = Mathf.Clamp(loopCount, 2, 8);
            rectTransform.DOKill();
            rectTransform.localScale = originalScale;
            rectTransform.DOScale(originalScale * Mathf.Max(1f, scaleAmount), duration)
                .SetEase(Ease.OutBack)
                .SetLoops(safeLoopCount, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    if (rectTransform != null)
                        rectTransform.localScale = originalScale;
                });
        }

        public void OnDrop(PointerEventData eventData)
        {
            ResetHoverScale();

            if (gameManager == null || eventData == null || eventData.pointerDrag == null)
                return;

            TagBasketDraggableItem item = eventData.pointerDrag.GetComponent<TagBasketDraggableItem>();
            if (item == null)
                item = eventData.pointerDrag.GetComponentInParent<TagBasketDraggableItem>();

            if (item == null)
                return;

            gameManager.TryDropItem(item, this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            EnsureReferences();

            if (!highlightOnHover || gameManager == null || !gameManager.CanDragItems || rectTransform == null)
                return;

            if (eventData != null && eventData.pointerDrag != null && eventData.pointerDrag.GetComponentInParent<TagBasketDraggableItem>() != null)
            {
                rectTransform.DOKill();
                rectTransform.DOScale(originalScale * hoverScale, hoverTweenDuration).SetEase(Ease.OutQuad);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResetHoverScale();
        }

        private void ResetHoverScale()
        {
            EnsureReferences();
            if (rectTransform == null)
                return;

            rectTransform.DOKill();
            rectTransform.DOScale(originalScale, hoverTweenDuration).SetEase(Ease.OutQuad);
        }

        private void EnsureReferences()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (rectTransform != null)
                originalScale = Vector3.one;

            if (basketImage == null)
                basketImage = GetComponent<Image>();

            if (placementRoot == null)
            {
                Transform placedRoot = transform.Find("PlacedItemsRoot");
                if (placedRoot == null)
                    placedRoot = transform.Find("PlacementRoot");

                placementRoot = placedRoot as RectTransform;
            }

            if (basketFrontOverlay == null)
            {
                Transform front = transform.Find("BasketFrontOverlay");
                if (front != null)
                    basketFrontOverlay = front.GetComponent<Image>();
            }

            if (titleBackgroundImage == null)
            {
                Transform titleBg = transform.Find("BasketTitleBackground");
                if (titleBg == null)
                    titleBg = transform.Find("BasketTitleBadge");

                if (titleBg != null)
                    titleBackgroundImage = titleBg.GetComponent<Image>();
            }

            if (titleText == null)
            {
                Transform title = transform.Find("BasketTitle");
                if (title != null)
                    titleText = title.GetComponent<TMP_Text>();

                if (titleText == null && titleBackgroundImage != null)
                    titleText = titleBackgroundImage.GetComponentInChildren<TMP_Text>(true);

                if (titleText == null)
                    titleText = GetComponentInChildren<TMP_Text>(true);
            }

            if (titleBackgroundImage == null && titleText != null)
            {
                Transform topTitleContainer = GetTopLevelChildUnderThis(titleText.transform);
                if (topTitleContainer != null && topTitleContainer != titleText.transform)
                    titleBackgroundImage = topTitleContainer.GetComponent<Image>();
            }

            if (placementRoot == null)
                placementRoot = rectTransform;
        }

        private void EnsureBasketLayering()
        {
            if (rectTransform == null)
                return;

            if (placementRoot != null && placementRoot != rectTransform && placementRoot.parent == transform)
                placementRoot.SetAsLastSibling();

            if (basketFrontOverlay != null)
            {
                Transform frontTopLevel = GetTopLevelChildUnderThis(basketFrontOverlay.transform);
                if (frontTopLevel != null)
                    frontTopLevel.SetAsLastSibling();
            }

            Transform titleTopLevel = null;
            if (titleBackgroundImage != null)
                titleTopLevel = GetTopLevelChildUnderThis(titleBackgroundImage.transform);

            if (titleTopLevel == null && titleText != null)
                titleTopLevel = GetTopLevelChildUnderThis(titleText.transform);

            if (titleTopLevel != null)
                titleTopLevel.SetAsLastSibling();
        }

        private Transform GetTopLevelChildUnderThis(Transform child)
        {
            if (child == null)
                return null;

            Transform current = child;
            while (current.parent != null && current.parent != transform)
                current = current.parent;

            return current.parent == transform ? current : null;
        }

        private static bool MatchesTag(string left, string right)
        {
            return string.Equals(Normalize(left), Normalize(right), System.StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
