using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SentenceWordSearchBoard : MonoBehaviour
{
    [Header("Grid References")]
    public RectTransform gridParent;
    public GridLayoutGroup gridLayout;
    public SentenceWordSearchCell cellPrefab;

    [Header("Grid Settings")]
    public int rows = 8;
    public int columns = 8;
    public int padding = 8;
    public Vector2 spacing = new Vector2(6f, 6f);
    public bool autoResizeCellsToParent = true;
    public string fillerAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public SentenceWordSearchCell[,] Cells { get; private set; }

    private char[,] letters;
    private readonly Dictionary<string, List<Vector2Int>> placedWordPositions = new Dictionary<string, List<Vector2Int>>();

    public void BuildFixedBoard(List<SentenceWordSearchQuestion> questions, int questionCount, SentenceWordSearchDifficulty difficulty)
    {
        if (gridParent == null || gridLayout == null || cellPrefab == null)
        {
            Debug.LogError("SentenceWordSearchBoard is missing gridParent, gridLayout, or cellPrefab.");
            return;
        }

        rows = Mathf.Max(2, rows);
        columns = Mathf.Max(2, columns);
        EnsureBoardCanFitLongestWord(questions, questionCount);

        placedWordPositions.Clear();
        letters = new char[rows, columns];
        ClearExistingCells();

        int count = Mathf.Clamp(questionCount, 1, questions.Count);
        for (int i = 0; i < count; i++)
        {
            string answer = SentenceWordSearchUtility.CleanWord(questions[i].answer);
            if (string.IsNullOrEmpty(answer))
                continue;

            bool placed = TryPlaceWord(answer, difficulty);
            if (!placed)
                Debug.LogWarning($"Could not place word '{answer}'. Increase rows/columns or reduce difficulty restrictions.");
        }

        FillEmptyCells();
        CreateCells();
        ResizeCellsToParent();
    }

    public void ResizeCellsToParent()
    {
        if (!autoResizeCellsToParent || gridParent == null || gridLayout == null)
            return;

        Canvas.ForceUpdateCanvases();

        Rect rect = gridParent.rect;
        float parentWidth = Mathf.Max(100f, rect.width);
        float parentHeight = Mathf.Max(100f, rect.height);

        float availableWidth = parentWidth - padding * 2f - spacing.x * (columns - 1);
        float availableHeight = parentHeight - padding * 2f - spacing.y * (rows - 1);
        float cellSize = Mathf.Floor(Mathf.Min(availableWidth / columns, availableHeight / rows));
        cellSize = Mathf.Max(24f, cellSize);

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        gridLayout.padding = new RectOffset(padding, padding, padding, padding);
        gridLayout.spacing = spacing;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
    }

    public SentenceWordSearchCell FindCellAtScreenPosition(Vector2 screenPosition)
    {
        if (Cells == null)
            return null;

        Camera cameraToUse = null;
        Canvas canvas = gridParent != null ? gridParent.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cameraToUse = canvas.worldCamera;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                SentenceWordSearchCell cell = Cells[r, c];
                if (cell == null || cell.rectTransform == null)
                    continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(cell.rectTransform, screenPosition, cameraToUse))
                    return cell;
            }
        }

        return null;
    }

    public List<SentenceWordSearchCell> GetStraightCellPath(SentenceWordSearchCell from, SentenceWordSearchCell to)
    {
        if (from == null || to == null || Cells == null)
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

        List<SentenceWordSearchCell> path = new List<SentenceWordSearchCell>(length);

        for (int i = 0; i < length; i++)
        {
            int row = from.Row + rowStep * i;
            int col = from.Column + colStep * i;

            if (!IsInside(row, col))
                return null;

            path.Add(Cells[row, col]);
        }

        return path;
    }

    public string GetWordFromCells(List<SentenceWordSearchCell> path)
    {
        if (path == null || path.Count == 0)
            return string.Empty;

        System.Text.StringBuilder builder = new System.Text.StringBuilder(path.Count);
        for (int i = 0; i < path.Count; i++)
            builder.Append(path[i].Letter);

        return builder.ToString();
    }

    public bool TryGetPlacedWordPath(string answer, out List<SentenceWordSearchCell> path)
    {
        path = null;
        string cleanAnswer = SentenceWordSearchUtility.CleanWord(answer);

        if (Cells == null || string.IsNullOrEmpty(cleanAnswer))
            return false;

        if (!placedWordPositions.TryGetValue(cleanAnswer, out List<Vector2Int> positions))
            return false;

        path = new List<SentenceWordSearchCell>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            Vector2Int pos = positions[i];
            if (!IsInside(pos.x, pos.y))
                return false;

            path.Add(Cells[pos.x, pos.y]);
        }

        return true;
    }

    public void ShowHintForWord(string answer)
    {
        ClearAllHints();

        if (!TryGetPlacedWordPath(answer, out List<SentenceWordSearchCell> path) || path == null || path.Count == 0)
            return;

        path[0]?.SetHint(true);

        if (path.Count > 1)
            path[path.Count - 1]?.SetHint(true);
    }

    public void ClearAllHints()
    {
        if (Cells == null)
            return;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (Cells[r, c] != null)
                    Cells[r, c].ClearHint();
            }
        }
    }

    public void ClearPreview(List<SentenceWordSearchCell> path)
    {
        if (path == null)
            return;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
                path[i].ClearTransient();
        }
    }

    public void MarkPreview(List<SentenceWordSearchCell> path)
    {
        if (path == null)
            return;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
                path[i].SetPreview(true);
        }
    }

    public void MarkSolved(List<SentenceWordSearchCell> path)
    {
        if (path == null)
            return;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
            {
                path[i].ClearTransient();
                path[i].ClearHint();
                path[i].SetSolved(true);
            }
        }
    }

    public void MarkWrong(List<SentenceWordSearchCell> path, bool active)
    {
        if (path == null)
            return;

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
                path[i].SetWrong(active);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        if (gameObject.activeInHierarchy)
            ResizeCellsToParent();
    }

    private void EnsureBoardCanFitLongestWord(List<SentenceWordSearchQuestion> questions, int questionCount)
    {
        int longest = 0;
        int count = Mathf.Clamp(questionCount, 1, questions.Count);

        for (int i = 0; i < count; i++)
        {
            string answer = SentenceWordSearchUtility.CleanWord(questions[i].answer);
            longest = Mathf.Max(longest, answer.Length);
        }

        int maxDimension = Mathf.Max(rows, columns);
        if (longest > maxDimension)
            columns = longest;
    }

    private bool TryPlaceWord(string word, SentenceWordSearchDifficulty difficulty)
    {
        List<Vector2Int> directions = GetDirections(difficulty);

        for (int attempt = 0; attempt < 1200; attempt++)
        {
            if (attempt % directions.Count == 0)
                ShuffleDirections(directions);

            Vector2Int direction = directions[attempt % directions.Count];
            int startRow = Random.Range(0, rows);
            int startCol = Random.Range(0, columns);

            if (!CanPlaceWord(word, startRow, startCol, direction))
                continue;

            List<Vector2Int> path = new List<Vector2Int>(word.Length);
            for (int i = 0; i < word.Length; i++)
            {
                int row = startRow + direction.x * i;
                int col = startCol + direction.y * i;
                letters[row, col] = word[i];
                path.Add(new Vector2Int(row, col));
            }

            placedWordPositions[word] = path;
            return true;
        }

        return false;
    }

    private bool CanPlaceWord(string word, int startRow, int startCol, Vector2Int direction)
    {
        for (int i = 0; i < word.Length; i++)
        {
            int row = startRow + direction.x * i;
            int col = startCol + direction.y * i;

            if (!IsInside(row, col))
                return false;

            char existing = letters[row, col];
            if (existing != '\0' && existing != word[i])
                return false;
        }

        return true;
    }

    private List<Vector2Int> GetDirections(SentenceWordSearchDifficulty difficulty)
    {
        List<Vector2Int> dirs = new List<Vector2Int>
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0)
        };

        if (difficulty == SentenceWordSearchDifficulty.Medium || difficulty == SentenceWordSearchDifficulty.Hard)
        {
            dirs.Add(new Vector2Int(1, 1));
            dirs.Add(new Vector2Int(1, -1));
        }

        if (difficulty == SentenceWordSearchDifficulty.Hard)
        {
            dirs.Add(new Vector2Int(0, -1));
            dirs.Add(new Vector2Int(-1, 0));
            dirs.Add(new Vector2Int(-1, -1));
            dirs.Add(new Vector2Int(-1, 1));
        }

        ShuffleDirections(dirs);
        return dirs;
    }

    private void ShuffleDirections(List<Vector2Int> dirs)
    {
        for (int i = 0; i < dirs.Count; i++)
        {
            int randomIndex = Random.Range(i, dirs.Count);
            Vector2Int temp = dirs[i];
            dirs[i] = dirs[randomIndex];
            dirs[randomIndex] = temp;
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

    private void CreateCells()
    {
        Cells = new SentenceWordSearchCell[rows, columns];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                SentenceWordSearchCell cell = Instantiate(cellPrefab, gridParent);
                cell.gameObject.name = $"Cell_{r}_{c}_{letters[r, c]}";
                cell.gameObject.SetActive(true);
                cell.Setup(r, c, letters[r, c]);
                Cells[r, c] = cell;
            }
        }
    }

    private void ClearExistingCells()
    {
        if (gridParent == null)
            return;

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Transform child = gridParent.GetChild(i);
            if (child.GetComponent<SentenceWordSearchCell>() == null)
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private bool IsInside(int row, int col)
    {
        return row >= 0 && row < rows && col >= 0 && col < columns;
    }
}
