using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SentenceWordSearchInputController : MonoBehaviour
{
    [Header("References")]
    public SentenceWordSearchManager manager;
    public SentenceWordSearchBoard board;
    public Canvas targetCanvas;

    [Header("Input")]
    public bool inputEnabled = true;

    private SentenceWordSearchCell startCell;
    private SentenceWordSearchCell currentCell;
    private List<SentenceWordSearchCell> currentPath = new List<SentenceWordSearchCell>();
    private bool dragging;

    public Camera EventCamera
    {
        get
        {
            if (targetCanvas == null)
                return null;

            return targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
        }
    }

    private void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<SentenceWordSearchManager>();

        if (board == null)
            board = FindObjectOfType<SentenceWordSearchBoard>();

        if (targetCanvas == null)
            targetCanvas = FindObjectOfType<Canvas>();
    }

    private void Update()
    {
        if (!inputEnabled || manager == null || board == null || !manager.CanAcceptInput)
        {
            if (dragging)
                CancelDrag();

            return;
        }

        HandleMouseInput();
        HandleTouchInput();
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
            CancelDrag();
    }

    private void HandleMouseInput()
    {
        if (Input.touchCount > 0)
            return;

        if (Input.GetMouseButtonDown(0))
            BeginDrag(Input.mousePosition);

        if (Input.GetMouseButton(0) && dragging)
            UpdateDrag(Input.mousePosition);

        if (Input.GetMouseButtonUp(0) && dragging)
            EndDrag();
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount <= 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
            BeginDrag(touch.position);
        else if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && dragging)
            UpdateDrag(touch.position);
        else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && dragging)
            EndDrag();
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        SentenceWordSearchCell cell = board.FindCellAtScreenPosition(screenPosition, EventCamera);

        if (cell == null)
            return;

        dragging = true;
        startCell = cell;
        currentCell = cell;

        currentPath = new List<SentenceWordSearchCell> { cell };
        board.SetPreviewPath(currentPath);
    }

    private void UpdateDrag(Vector2 screenPosition)
    {
        if (!dragging || startCell == null)
            return;

        SentenceWordSearchCell hoverCell = board.FindCellAtScreenPosition(screenPosition, EventCamera);

        if (hoverCell == null || hoverCell == currentCell)
            return;

        List<SentenceWordSearchCell> path = board.GetStraightPath(startCell, hoverCell);

        if (path == null || path.Count == 0)
            return;

        currentCell = hoverCell;
        currentPath = path;
        board.SetPreviewPath(currentPath);
    }

    private void EndDrag()
    {
        dragging = false;

        if (currentPath == null || currentPath.Count == 0)
        {
            CancelDrag();
            return;
        }

        string selectedWord = board.GetWordFromPath(currentPath);
        List<SentenceWordSearchCell> submittedPath = new List<SentenceWordSearchCell>(currentPath);

        manager.SubmitSelectedWord(selectedWord, submittedPath);
    }

    private void CancelDrag()
    {
        dragging = false;
        startCell = null;
        currentCell = null;
        currentPath.Clear();

        if (board != null)
            board.ClearPreview();
    }
}
