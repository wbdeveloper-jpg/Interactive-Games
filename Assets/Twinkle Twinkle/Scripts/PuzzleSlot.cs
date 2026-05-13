using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzleSlot : MonoBehaviour, IDropHandler
{
    public int id;
    [Tooltip("Optional child transform where the piece should be parented. If empty, this slot transform is used.")]
    public RectTransform pieceRoot;

    public PuzzlePieceView CurrentPiece { get; internal set; }
    public Transform PieceRootTransform { get { return pieceRoot != null ? pieceRoot : transform; } }

    private PuzzleBoardController board;

    public void Initialize(PuzzleBoardController owner, int slotId)
    {
        board = owner;
        id = slotId;
        CurrentPiece = null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (board == null || eventData.pointerDrag == null) return;

        PuzzlePieceView piece = eventData.pointerDrag.GetComponent<PuzzlePieceView>();
        if (piece == null) return;

        board.TryDropPiece(piece, this);
    }
}
