using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class PuzzlePieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Runtime Identity")]
    public int id;

    [Header("UI")]
    public Image pieceImage;
    [Tooltip("Optional overlay shown when the piece is in the wrong slot.")]
    public Image wrongPositionOverlay;

    public PuzzleSlot CurrentSlot { get; internal set; }

    private PuzzleBoardController board;
    private CanvasGroup canvasGroup;
    private PuzzleSlot slotBeforeDrag;
    private bool droppedDuringThisDrag;
    private RectTransform rectTransform;
    private Canvas rootCanvas;

    public void Initialize(PuzzleBoardController owner, int pieceId, Sprite sprite)
    {
        board = owner;
        id = pieceId;
        rectTransform = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();

        if (pieceImage == null) pieceImage = GetComponent<Image>();
        if (pieceImage != null) pieceImage.sprite = sprite;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        SetWrongOverlayVisible(false, 0f, true);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (board == null || board.IsInputLocked) return;

        slotBeforeDrag = CurrentSlot;
        droppedDuringThisDrag = false;

        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.95f;

        Transform parent = board.DragRoot;
        transform.SetParent(parent, true);
        transform.SetAsLastSibling();

        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (board == null || board.IsInputLocked) return;
        MoveToPointer(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }

        if (board == null || board.IsInputLocked) return;

        if (!droppedDuringThisDrag)
        {
            board.PlacePieceInSlot(this, slotBeforeDrag, true);
        }

        board.ValidatePiece(this);
        board.CheckSolved();
    }

    public void MarkDropped()
    {
        droppedDuringThisDrag = true;
    }

    public void StretchToParent(float margin = 0f)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(margin, margin);
        rectTransform.offsetMax = new Vector2(-margin, -margin);
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localPosition = Vector3.zero;
    }

    public void SetWrongOverlayVisible(bool visible, float alpha, bool instant)
    {
        if (wrongPositionOverlay == null) return;

        wrongPositionOverlay.gameObject.SetActive(true);
        Color color = wrongPositionOverlay.color;

        if (instant)
        {
            color.a = visible ? alpha : 0f;
            wrongPositionOverlay.color = color;
            wrongPositionOverlay.gameObject.SetActive(visible);
        }
        else
        {
            wrongPositionOverlay.DOKill();
            wrongPositionOverlay.DOFade(visible ? alpha : 0f, 0.25f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                if (!visible && wrongPositionOverlay != null)
                {
                    wrongPositionOverlay.gameObject.SetActive(false);
                }
            });
        }
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        RectTransform dragPlane = null;
        if (board != null && board.DragRoot != null)
        {
            dragPlane = board.DragRoot as RectTransform;
        }

        if (dragPlane == null)
        {
            dragPlane = transform.parent as RectTransform;
        }

        Camera eventCamera = eventData.pressEventCamera;
        if (eventCamera == null)
        {
            Canvas canvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera;
            }
        }

        if (dragPlane != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(dragPlane, eventData.position, eventCamera, out Vector3 worldPosition))
        {
            rectTransform.position = worldPosition;
        }
        else
        {
            // Fallback works for Screen Space Overlay.
            rectTransform.position = eventData.position;
        }
    }
}
