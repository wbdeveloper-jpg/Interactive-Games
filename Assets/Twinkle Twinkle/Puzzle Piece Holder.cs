using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzlePieceHolder : MonoBehaviour, IDropHandler
{

    public int id;
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Dropped");
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            PuzzlePiece draggableItem = dropped.GetComponent<PuzzlePiece>();
            draggableItem.parentAfterDrag = transform;
        }
        else
        {
            GameObject dropped = eventData.pointerDrag;
            PuzzlePiece draggableItem = dropped.GetComponent<PuzzlePiece>();

            GameObject current = transform.GetChild(0).gameObject;
            PuzzlePiece currentDraggable = current.GetComponent<PuzzlePiece>();

            currentDraggable.transform.SetParent(draggableItem.parentAfterDrag);
            PuzzlePiece.SetStretchWithMargins(currentDraggable.GetComponent<RectTransform>());
            draggableItem.parentAfterDrag = transform;
            FindObjectOfType<PuzzleManager>().CheckForIndividualPiece(currentDraggable);

        }

    }
}