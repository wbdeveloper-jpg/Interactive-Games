using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SentenceWordSearchInputController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public SentenceWordSearchBoard board;

    public event Action<List<SentenceWordSearchCell>, string> SelectionSubmitted;

    private readonly List<SentenceWordSearchCell> currentPath = new List<SentenceWordSearchCell>();

    private SentenceWordSearchCell startCell;
    private bool inputEnabled = true;
    private bool isDragging;

    private void Awake()
    {
        Image raycastImage = GetComponent<Image>();
        raycastImage.raycastTarget = true;
        raycastImage.color = new Color(1f, 1f, 1f, 0f);
    }

    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;

        if (!value)
            ClearCurrentPreview();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!inputEnabled || board == null)
            return;

        SentenceWordSearchCell cell = board.FindCellAtScreenPosition(eventData.position);
        if (cell == null)
            return;

        board.ClearAllHints();
        isDragging = true;
        startCell = cell;
        UpdatePath(cell);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!inputEnabled || !isDragging || board == null || startCell == null)
            return;

        SentenceWordSearchCell cell = board.FindCellAtScreenPosition(eventData.position);
        if (cell == null)
            return;

        UpdatePath(cell);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;

        if (!inputEnabled || currentPath.Count == 0)
        {
            ClearCurrentPreview();
            return;
        }

        List<SentenceWordSearchCell> submittedPath = new List<SentenceWordSearchCell>(currentPath);
        string selectedWord = board.GetWordFromCells(submittedPath);

        SelectionSubmitted?.Invoke(submittedPath, selectedWord);

        currentPath.Clear();
        startCell = null;
    }

    public void ClearCurrentPreview()
    {
        if (board != null)
            board.ClearPreview(currentPath);

        currentPath.Clear();
        startCell = null;
        isDragging = false;
    }

    private void UpdatePath(SentenceWordSearchCell endCell)
    {
        List<SentenceWordSearchCell> newPath = board.GetStraightCellPath(startCell, endCell);
        if (newPath == null || newPath.Count == 0)
            return;

        board.ClearPreview(currentPath);
        currentPath.Clear();
        currentPath.AddRange(newPath);
        board.MarkPreview(currentPath);
    }
}
