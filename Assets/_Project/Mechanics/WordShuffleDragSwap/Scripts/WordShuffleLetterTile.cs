using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WordShuffleDragSwap
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class WordShuffleLetterTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [SerializeField] private TextMeshProUGUI letterText;
        [SerializeField] private Image tileImage;
        [SerializeField] private CanvasGroup canvasGroup;

        private WordShuffleDragSwapManager manager;
        private RectTransform rectTransform;
        private Vector2 dragOffset;
        private bool isInitialized;
        private Color normalTileColor = Color.white;

        public string Letter { get; private set; }
        public int CurrentIndex { get; private set; }
        public bool IsLockedByHint { get; private set; }
        public RectTransform RectTransform => rectTransform;

        private void Awake()
        {
            CacheReferences();
        }

        private void Reset()
        {
            CacheReferences();
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        public void Initialize(WordShuffleDragSwapManager owner, string letter, int startIndex)
        {
            CacheReferences();

            manager = owner;
            Letter = letter;
            CurrentIndex = startIndex;
            IsLockedByHint = false;
            isInitialized = true;

            if (letterText != null)
                letterText.text = letter;

            if (tileImage != null)
                normalTileColor = tileImage.color;

            SetRaycastState(true);
            KillTweens();
        }

        public void SetIndex(int index)
        {
            CurrentIndex = index;
        }

        public void ApplyFont(TMP_FontAsset fontAsset)
        {
            CacheReferences();

            if (fontAsset != null && letterText != null)
                letterText.font = fontAsset;
        }

        public void ApplyResponsiveVisualSize(Vector2 size, float textSizeRatio)
        {
            CacheReferences();

            if (rectTransform != null)
                rectTransform.sizeDelta = size;

            if (letterText != null)
            {
                float targetFontSize = Mathf.Max(24f, Mathf.Min(size.x, size.y) * Mathf.Clamp(textSizeRatio, 0.35f, 0.75f));
                letterText.enableAutoSizing = true;
                letterText.fontSize = targetFontSize;
                letterText.fontSizeMax = targetFontSize;
                letterText.fontSizeMin = Mathf.Max(18f, targetFontSize * 0.55f);
                letterText.margin = new Vector4(4f, 2f, 4f, 2f);
            }
        }


        public void SetLockedByHint(bool locked, Color lockedColor, bool useAnimation, float punchScale)
        {
            CacheReferences();

            IsLockedByHint = locked;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = !locked;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = locked ? 0.96f : 1f;
            }

            if (tileImage != null)
            {
                tileImage.DOKill();
                if (useAnimation)
                    tileImage.DOColor(locked ? lockedColor : normalTileColor, 0.18f);
                else
                    tileImage.color = locked ? lockedColor : normalTileColor;
            }

            if (letterText != null)
                letterText.text = Letter;

            if (useAnimation && rectTransform != null && locked)
            {
                rectTransform.DOKill();
                rectTransform.localScale = Vector3.one;
                rectTransform.DOPunchScale(Vector3.one * punchScale, 0.28f, 8, 0.72f);
            }
        }


        public void AnimateTileColor(Color color, float duration)
        {
            CacheReferences();

            if (tileImage == null)
                return;

            tileImage.DOKill();
            tileImage.DOColor(color, Mathf.Max(0.01f, duration));
        }

        public void SetRaycastState(bool canRaycast)
        {
            CacheReferences();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = canRaycast;
                canvasGroup.interactable = canRaycast && !IsLockedByHint;
            }
        }

        public void KillTweens()
        {
            if (rectTransform != null)
                rectTransform.DOKill();

            if (tileImage != null)
                tileImage.DOKill();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInitialized || manager == null || !manager.CanDragTile(this))
                return;

            transform.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isInitialized || manager == null || !manager.CanDragTile(this))
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                manager.TileLayer,
                eventData.position,
                manager.UICamera,
                out Vector2 localPointerPosition);

            dragOffset = rectTransform.anchoredPosition - localPointerPosition;
            SetRaycastState(false);
            transform.SetAsLastSibling();
            manager.NotifyTileDragStarted(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isInitialized || manager == null || !manager.CanDragTile(this))
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                manager.TileLayer,
                eventData.position,
                manager.UICamera,
                out Vector2 localPointerPosition);

            rectTransform.anchoredPosition = localPointerPosition + dragOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isInitialized || manager == null)
                return;

            SetRaycastState(true);
            manager.NotifyTileDropped(this, eventData.position);
        }

        private void CacheReferences()
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (tileImage == null)
                tileImage = GetComponent<Image>();

            if (letterText == null)
                letterText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
}
