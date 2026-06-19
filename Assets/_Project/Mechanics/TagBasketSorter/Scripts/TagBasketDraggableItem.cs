using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TagBasketSorter
{
    public enum TagBasketItemVisualMode
    {
        ImageOnly,
        ImageAndLabel
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TagBasketDraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Item Data")]
        [Tooltip("Must match a basket acceptedTag. Strings are intentional so each client scene can use different categories without code changes.")]
        public string itemTag = "Common Noun";
        public string itemId;

        [Header("Visual Mode")]
        public TagBasketItemVisualMode visualMode = TagBasketItemVisualMode.ImageAndLabel;
        public Image iconImage;
        public TMP_Text labelText;
        public Color labelColor = Color.red;

        [Header("Drag Behaviour")]
        public bool snapToBasketCenter = true;
        public bool disableAfterCorrectDrop = true;

        public bool IsPlacedCorrectly { get; private set; }
        public bool IsDragging { get; private set; }
        public TagBasketLevelPanel OwnerLevel { get; private set; }

        private TagBasketSortGameManager gameManager;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Transform startParent;
        private int startSiblingIndex;
        private Vector2 startAnchoredPosition;
        private Quaternion startLocalRotation = Quaternion.identity;
        private Vector2 designerAnchoredPosition;
        private Quaternion designerLocalRotation = Quaternion.identity;
        private bool designerPoseCaptured;
        private bool canDrag = true;

        public bool CanReceiveHint => !IsPlacedCorrectly && gameObject.activeInHierarchy;

        private void Awake()
        {
            EnsureReferences();
            CaptureDesignerPoseIfNeeded();
            ApplyVisualMode();
        }

        private void Reset()
        {
            EnsureReferences();
            CaptureDesignerPoseIfNeeded();
            ApplyVisualMode();
        }

        private void OnValidate()
        {
            EnsureReferences();
            ApplyVisualMode();
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
            CaptureDesignerPoseIfNeeded();
            ApplyVisualMode();

            gameManager = manager;
            OwnerLevel = ownerLevel;

            CacheStartTransform();
            ResetItem(false);
        }

        public void RestoreDesignerStartPose()
        {
            EnsureReferences();
            CaptureDesignerPoseIfNeeded();

            if (rectTransform == null)
                return;

            rectTransform.anchoredPosition = designerAnchoredPosition;
            rectTransform.localRotation = designerLocalRotation;
            CacheStartTransform();
        }

        public void CacheStartTransform()
        {
            EnsureReferences();

            if (rectTransform == null)
            {
                Debug.LogError($"TagBasketDraggableItem on {name} requires a RectTransform. Put draggable objects under a Canvas/UI panel.", this);
                return;
            }

            startParent = transform.parent;
            startSiblingIndex = transform.GetSiblingIndex();
            startAnchoredPosition = rectTransform.anchoredPosition;
            startLocalRotation = rectTransform.localRotation;
        }

        public void ResetItem(bool animate)
        {
            EnsureReferences();
            ApplyVisualMode();

            if (rectTransform == null)
                return;

            if (startParent == null)
                CacheStartTransform();

            IsPlacedCorrectly = false;
            IsDragging = false;
            canDrag = true;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
                canvasGroup.alpha = 1f;
            }

            if (startParent != null)
            {
                transform.SetParent(startParent, true);
                transform.SetSiblingIndex(startSiblingIndex);
            }

            rectTransform.DOKill();
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = startLocalRotation;

            if (animate)
                MoveToLocalPosition(startAnchoredPosition, gameManager != null ? gameManager.snapBackDuration : 0.15f, true);
            else
                rectTransform.anchoredPosition = startAnchoredPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EnsureReferences();

            if (!canDrag || IsPlacedCorrectly || gameManager == null || !gameManager.CanDragItems || rectTransform == null)
                return;

            IsDragging = true;

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;

            startSiblingIndex = transform.GetSiblingIndex();
            rectTransform.DOKill();
            rectTransform.localRotation = Quaternion.identity;

            if (gameManager.dragLayer != null)
                transform.SetParent(gameManager.dragLayer, true);

            transform.SetAsLastSibling();
            gameManager.OnItemDragStarted(this);
            SetDraggedPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging || gameManager == null)
                return;

            SetDraggedPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging)
                return;

            IsDragging = false;

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            if (!IsPlacedCorrectly)
            {
                if (gameManager != null)
                    gameManager.OnItemReleasedOutsideBasket(this);
                else
                    ReturnToStart(true);
            }
        }

        public void MarkPlacedCorrectly(TagBasketDropZone dropZone)
        {
            EnsureReferences();

            if (dropZone == null || rectTransform == null)
                return;

            IsPlacedCorrectly = true;
            IsDragging = false;
            canDrag = !disableAfterCorrectDrop;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = !disableAfterCorrectDrop;
                canvasGroup.interactable = !disableAfterCorrectDrop;
            }

            RectTransform targetParent = dropZone.GetPlacementRoot();
            if (targetParent != null)
                transform.SetParent(targetParent, true);

            transform.SetAsLastSibling();

            Vector2 targetPosition = snapToBasketCenter ? dropZone.ReserveNextPlacementPosition() : rectTransform.anchoredPosition;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, dropZone.GetRandomPlacedRotation());
            rectTransform.DOKill();
            rectTransform.DOLocalRotateQuaternion(targetRotation, gameManager != null ? gameManager.correctDropSnapDuration : 0.15f)
                .SetEase(Ease.OutQuad);

            if (snapToBasketCenter)
                MoveToLocalPosition(targetPosition, gameManager != null ? gameManager.correctDropSnapDuration : 0.15f, true);
        }

        public void ReturnToStart(bool animate = true)
        {
            EnsureReferences();

            if (rectTransform == null)
                return;

            IsDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (startParent == null)
                CacheStartTransform();

            if (startParent != null)
            {
                transform.SetParent(startParent, true);
                transform.SetSiblingIndex(startSiblingIndex);
            }

            rectTransform.DOKill();
            rectTransform.localRotation = startLocalRotation;
            MoveToLocalPosition(startAnchoredPosition, animate && gameManager != null ? gameManager.snapBackDuration : 0f, true);
        }

        public void PlayHintPulse(float scaleAmount, float duration, int loopCount = 4)
        {
            EnsureReferences();
            if (rectTransform == null)
                return;

            int safeLoopCount = Mathf.Clamp(loopCount, 2, 8);
            rectTransform.DOKill();
            rectTransform.localScale = Vector3.one;
            rectTransform.DOScale(Vector3.one * Mathf.Max(1f, scaleAmount), duration)
                .SetEase(Ease.OutBack)
                .SetLoops(safeLoopCount, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    if (rectTransform != null)
                        rectTransform.localScale = Vector3.one;
                });
        }

        public string GetDisplayName()
        {
            if (labelText != null && !string.IsNullOrWhiteSpace(labelText.text))
                return labelText.text;

            if (!string.IsNullOrWhiteSpace(itemId))
                return itemId;

            return name;
        }

        private void SetDraggedPosition(PointerEventData eventData)
        {
            if (eventData == null || rectTransform == null || gameManager == null || gameManager.rootCanvas == null)
                return;

            RectTransform canvasRect = gameManager.rootCanvas.transform as RectTransform;
            Camera uiCamera = gameManager.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : gameManager.rootCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, uiCamera, out Vector2 localPoint))
                rectTransform.anchoredPosition = localPoint;
        }

        private void MoveToLocalPosition(Vector2 targetPosition, float duration, bool useEase)
        {
            if (rectTransform == null)
                return;

            rectTransform.DOKill();
            if (duration <= 0f)
            {
                rectTransform.anchoredPosition = targetPosition;
                return;
            }

            Tween tween = rectTransform.DOAnchorPos(targetPosition, duration);
            if (useEase)
                tween.SetEase(Ease.OutQuad);
        }

        private void ApplyVisualMode()
        {
            if (labelText != null)
            {
                bool showLabel = visualMode == TagBasketItemVisualMode.ImageAndLabel;
                labelText.gameObject.SetActive(showLabel);
                labelText.color = labelColor;
            }

            if (iconImage != null)
                iconImage.raycastTarget = true;
        }

        private void CaptureDesignerPoseIfNeeded()
        {
            if (designerPoseCaptured || rectTransform == null)
                return;

            designerAnchoredPosition = rectTransform.anchoredPosition;
            designerLocalRotation = rectTransform.localRotation;
            designerPoseCaptured = true;
        }

        private void EnsureReferences()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (iconImage == null)
                iconImage = GetComponent<Image>();

            if (labelText == null)
                labelText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
