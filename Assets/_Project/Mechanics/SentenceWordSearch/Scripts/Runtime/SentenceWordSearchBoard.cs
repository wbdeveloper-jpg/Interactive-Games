using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SentenceWordSearchBoard : MonoBehaviour
{
    [Header("References")]
    public RectTransform gridParent;
    public GridLayoutGroup gridLayout;
    public SentenceWordSearchCell cellPrefab;

    [Header("Board Size")]
    [Min(2)] public int rows = 8;
    [Min(2)] public int columns = 8;
    public int gridPadding = 10;
    public Vector2 gridSpacing = new Vector2(8f, 8f);

    [Header("Letter Fill")]
    public string fillerAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public SentenceWordSearchDifficulty difficulty = SentenceWordSearchDifficulty.Medium;

    [Header("Hint")]
    public float hintPulseDuration = 2.2f;

    private readonly Dictionary<string, SentenceWordSearchPlacedWord> placedWords = new Dictionary<string, SentenceWordSearchPlacedWord>();
    private readonly List<SentenceWordSearchCell> previewPath = new List<SentenceWordSearchCell>();

    private char[,] letters;
    private SentenceWordSearchCell[,] cells;

    public void BuildBoard(List<SentenceWordSearchQuestion> questions, TMP_FontAsset cellFont)
    {
        if (gridParent == null)
            gridParent = transform as RectTransform;

        if (gridLayout == null && gridParent != null)
            gridLayout = gridParent.GetComponent<GridLayoutGroup>();

        if (gridLayout == null && gridParent != null)
            gridLayout = gridParent.gameObject.AddComponent<GridLayoutGroup>();

        if (cellPrefab == null)
        {
            Debug.LogError("SentenceWordSearchBoard is missing Cell Prefab.");
            return;
        }

        rows = Mathf.Max(2, rows);
        columns = Mathf.Max(2, columns);

        ClearBoard();
        ConfigureGridLayout();

        letters = new char[rows, columns];
        placedWords.Clear();

        TryPlaceAllWords(questions);
        FillEmptyCells();
        SpawnCells(cellFont);
    }

    public void ConfigureGridLayout()
    {
        if (gridParent == null || gridLayout == null)
            return;

        Canvas.ForceUpdateCanvases();

        Rect rect = gridParent.rect;
        float parentWidth = rect.width > 1f ? rect.width : 760f;
        float parentHeight = rect.height > 1f ? rect.height : 760f;

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        gridLayout.spacing = gridSpacing;
        gridLayout.padding = new RectOffset(gridPadding, gridPadding, gridPadding, gridPadding);

        float availableWidth = parentWidth - gridPadding * 2f - gridSpacing.x * Mathf.Max(0, columns - 1);
        float availableHeight = parentHeight - gridPadding * 2f - gridSpacing.y * Mathf.Max(0, rows - 1);

        float cellWidth = availableWidth / columns;
        float cellHeight = availableHeight / rows;
        float finalSize = Mathf.Floor(Mathf.Max(12f, Mathf.Min(cellWidth, cellHeight)));

        gridLayout.cellSize = new Vector2(finalSize, finalSize);
    }

    public SentenceWordSearchCell FindCellAtScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        if (cells == null)
            return null;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                SentenceWordSearchCell cell = cells[r, c];

                if (cell == null)
                    continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(cell.RectTransform, screenPosition, eventCamera))
                    return cell;
            }
        }

        return null;
    }

    public List<SentenceWordSearchCell> GetStraightPath(SentenceWordSearchCell from, SentenceWordSearchCell to)
    {
        if (from == null || to == null || cells == null)
            return null;

        int rowDelta = to.Row - from.Row;
        int colDelta = to.Column - from.Column;

        bool horizontal = rowDelta == 0;
        bool vertical = colDelta == 0;
        bool diagonal = Mathf.Abs(rowDelta) == Mathf.Abs(colDelta);

        if (!horizontal && !vertical && !diagonal)
            return null;

        int rowStep = rowDelta == 0 ? 0 : rowDelta / Mathf.Abs(rowDelta);
        int colStep = colDelta == 0 ? 0 : colDelta / Mathf.Abs(colDelta);

        int length = Mathf.Max(Mathf.Abs(rowDelta), Mathf.Abs(colDelta)) + 1;

        List<SentenceWordSearchCell> path = new List<SentenceWordSearchCell>();

        for (int i = 0; i < length; i++)
        {
            int row = from.Row + rowStep * i;
            int col = from.Column + colStep * i;

            if (!IsInside(row, col))
                return null;

            path.Add(cells[row, col]);
        }

        return path;
    }

    public void SetPreviewPath(List<SentenceWordSearchCell> path)
    {
        ClearPreview();

        if (path == null)
            return;

        previewPath.AddRange(path);

        for (int i = 0; i < previewPath.Count; i++)
        {
            if (previewPath[i] != null)
                previewPath[i].SetPreview(true);
        }
    }

    public void ClearPreview()
    {
        for (int i = 0; i < previewPath.Count; i++)
        {
            if (previewPath[i] != null)
                previewPath[i].SetPreview(false);
        }

        previewPath.Clear();
    }

    public string GetWordFromPath(List<SentenceWordSearchCell> path)
    {
        if (path == null || path.Count == 0)
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(path.Count);

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
                builder.Append(path[i].Letter);
        }

        return builder.ToString();
    }

    public Vector2 GetPathCenterScreenPosition(List<SentenceWordSearchCell> path, Camera eventCamera)
    {
        if (path == null || path.Count == 0)
            return Vector2.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] == null)
                continue;

            sum += path[i].RectTransform.position;
            count++;
        }

        if (count == 0)
            return Vector2.zero;

        Vector3 world = sum / count;
        return RectTransformUtility.WorldToScreenPoint(eventCamera, world);
    }

    public void MarkWordSolved(string cleanWord)
    {
        cleanWord = SentenceWordSearchManager.CleanWordStatic(cleanWord);

        if (!placedWords.TryGetValue(cleanWord, out SentenceWordSearchPlacedWord placed))
            return;

        for (int i = 0; i < placed.cells.Count; i++)
        {
            Vector2Int pos = placed.cells[i];

            if (IsInside(pos.x, pos.y) && cells[pos.x, pos.y] != null)
                cells[pos.x, pos.y].SetSolved(true);
        }
    }

    public void FlashWrongPath(List<SentenceWordSearchCell> path, float duration)
    {
        if (path == null)
            return;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
                path[i].FlashWrong(duration);
        }
    }

    public void PulseHintForWord(string cleanWord)
    {
        StopAllHintPulses();

        cleanWord = SentenceWordSearchManager.CleanWordStatic(cleanWord);

        if (!placedWords.TryGetValue(cleanWord, out SentenceWordSearchPlacedWord placed))
            return;

        if (placed.cells.Count == 0)
            return;

        Vector2Int first = placed.cells[0];
        Vector2Int last = placed.cells[placed.cells.Count - 1];

        if (IsInside(first.x, first.y) && cells[first.x, first.y] != null)
            cells[first.x, first.y].PulseHint(hintPulseDuration);

        if (last != first && IsInside(last.x, last.y) && cells[last.x, last.y] != null)
            cells[last.x, last.y].PulseHint(hintPulseDuration);
    }

    public void StopAllHintPulses()
    {
        if (cells == null)
            return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (cells[r, c] != null)
                    cells[r, c].StopHintPulse();
            }
        }
    }

    private void TryPlaceAllWords(List<SentenceWordSearchQuestion> questions)
    {
        if (questions == null)
            return;

        for (int i = 0; i < questions.Count; i++)
        {
            string word = SentenceWordSearchManager.CleanWordStatic(questions[i].answer);

            if (string.IsNullOrEmpty(word))
                continue;

            if (word.Length > Mathf.Max(rows, columns))
            {
                Debug.LogWarning($"Word '{word}' is longer than the board side. Increase rows/columns.");
                continue;
            }

            bool placed = TryPlaceWord(word);

            if (!placed)
                Debug.LogWarning($"Could not place word '{word}'. Increase grid size or reduce question count.");
        }
    }

    private bool TryPlaceWord(string word)
    {
        Vector2Int[] directions = GetDirections();

        for (int attempt = 0; attempt < 350; attempt++)
        {
            Vector2Int dir = directions[Random.Range(0, directions.Length)];
            int startRow = Random.Range(0, rows);
            int startCol = Random.Range(0, columns);

            if (!CanPlaceWord(word, startRow, startCol, dir))
                continue;

            PlaceWord(word, startRow, startCol, dir);
            return true;
        }

        return false;
    }

    private bool CanPlaceWord(string word, int startRow, int startCol, Vector2Int dir)
    {
        int endRow = startRow + dir.x * (word.Length - 1);
        int endCol = startCol + dir.y * (word.Length - 1);

        if (!IsInside(endRow, endCol))
            return false;

        for (int i = 0; i < word.Length; i++)
        {
            int row = startRow + dir.x * i;
            int col = startCol + dir.y * i;

            char existing = letters[row, col];

            if (existing != '\0' && existing != word[i])
                return false;
        }

        return true;
    }

    private void PlaceWord(string word, int startRow, int startCol, Vector2Int dir)
    {
        SentenceWordSearchPlacedWord placed = new SentenceWordSearchPlacedWord
        {
            word = word,
            start = new Vector2Int(startRow, startCol),
            direction = dir
        };

        for (int i = 0; i < word.Length; i++)
        {
            int row = startRow + dir.x * i;
            int col = startCol + dir.y * i;

            letters[row, col] = word[i];
            placed.cells.Add(new Vector2Int(row, col));
        }

        placedWords[word] = placed;
    }

    private Vector2Int[] GetDirections()
    {
        switch (difficulty)
        {
            case SentenceWordSearchDifficulty.Easy:
                return new[] { new Vector2Int(0, 1), new Vector2Int(1, 0) };

            case SentenceWordSearchDifficulty.Hard:
                return new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(0, -1),
                    new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(1, 1), new Vector2Int(1, -1),
                    new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                };

            default:
                return new[]
                {
                    new Vector2Int(0, 1), new Vector2Int(1, 0),
                    new Vector2Int(1, 1), new Vector2Int(1, -1)
                };
        }
    }

    private void FillEmptyCells()
    {
        if (string.IsNullOrEmpty(fillerAlphabet))
            fillerAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (letters[r, c] == '\0')
                    letters[r, c] = fillerAlphabet[Random.Range(0, fillerAlphabet.Length)];
            }
        }
    }

    private void SpawnCells(TMP_FontAsset cellFont)
    {
        cells = new SentenceWordSearchCell[rows, columns];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                SentenceWordSearchCell cell = Instantiate(cellPrefab, gridParent);
                cell.gameObject.SetActive(true);
                cell.name = $"Cell_{r}_{c}_{letters[r, c]}";
                cell.Setup(r, c, letters[r, c], cellFont);
                cells[r, c] = cell;
            }
        }
    }

    private void ClearBoard()
    {
        ClearPreview();

        if (gridParent == null)
            return;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = gridParent.GetChild(i);

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        cells = null;
    }

    private bool IsInside(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < columns;
    }
}
