using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzlePiece : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Transform parentAfterDrag;
    public int id;
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("Drag Started");
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        GetComponent<Image>().raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("Dragging");
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Drag End");
        transform.SetParent(parentAfterDrag);
        SetStretchWithMargins(GetComponent<RectTransform>());
        GetComponent<Image>().raycastTarget = true;
        FindObjectOfType<PuzzleManager>().CheckForIndividualPiece(this.GetComponent<PuzzlePiece>());
        FindObjectOfType<PuzzleManager>().CheckSolved();
    }


    public static void SetStretchWithMargins(RectTransform rt, float margin = 0f)
    {
        if (rt == null) return;

        // Set anchor preset to Stretch in both directions
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);

        // Set offsets (Left, Right, Top, Bottom)
        rt.offsetMin = new Vector2(margin, margin);      // left, bottom
        rt.offsetMax = new Vector2(-margin, -margin);    // right, top
    }



}
