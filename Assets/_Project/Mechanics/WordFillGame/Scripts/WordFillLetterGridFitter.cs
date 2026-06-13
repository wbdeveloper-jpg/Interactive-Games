using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum WordFillLetterDifficulty
{
    Easy,
    Medium,
    Hard
}

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(GridLayoutGroup))]
public class WordFillLetterGridFitter : MonoBehaviour
{
    [Header("Cell Limits")]
    [SerializeField] private float maxCellWidth = 92f;
    [SerializeField] private float maxCellHeight = 80f;
    [SerializeField] private float minCellWidth = 42f;
    [SerializeField] private float minCellHeight = 38f;

    [Header("Spacing")]
    [SerializeField] private Vector2 easySpacing = new Vector2(16f, 14f);
    [SerializeField] private Vector2 mediumSpacing = new Vector2(12f, 12f);
    [SerializeField] private Vector2 hardSpacing = new Vector2(8f, 8f);

    [Header("Columns")]
    [SerializeField] private int easyColumns = 8;
    [SerializeField] private int mediumColumns = 7;
    [SerializeField] private int hardColumns = 6;
    [SerializeField] private bool randomizeColumnCount = true;

    [Header("Visual Randomness")]
    [SerializeField] private float mediumRotation = 3f;
    [SerializeField] private float hardRotation = 7f;
    [SerializeField] private float hardScaleVariation = 0.06f;

    private RectTransform rectTransform;
    private GridLayoutGroup grid;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        grid = GetComponent<GridLayoutGroup>();
    }

    public void FitGrid(int itemCount, WordFillLetterDifficulty difficulty)
    {
        if (!gameObject.activeInHierarchy)
            return;

        StartCoroutine(FitNextFrame(itemCount, difficulty));
    }

    private IEnumerator FitNextFrame(int itemCount, WordFillLetterDifficulty difficulty)
    {
        yield return null;

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        itemCount = Mathf.Max(1, itemCount);

        Vector2 spacing = GetSpacing(difficulty);
        int columns = GetColumns(itemCount, difficulty);
        int rows = Mathf.CeilToInt(itemCount / (float)columns);

        Rect rect = rectTransform.rect;

        float availableWidth = Mathf.Max(1f, rect.width - grid.padding.left - grid.padding.right - spacing.x * Mathf.Max(0, columns - 1));
        float availableHeight = Mathf.Max(1f, rect.height - grid.padding.top - grid.padding.bottom - spacing.y * Mathf.Max(0, rows - 1));

        float cellWidth = Mathf.Clamp(availableWidth / columns, minCellWidth, maxCellWidth);
        float cellHeight = Mathf.Clamp(availableHeight / rows, minCellHeight, maxCellHeight);

        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.cellSize = new Vector2(cellWidth, cellHeight);
        grid.childAlignment = TextAnchor.MiddleCenter;

        ApplyDifficultyLook(difficulty);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private Vector2 GetSpacing(WordFillLetterDifficulty difficulty)
    {
        if (difficulty == WordFillLetterDifficulty.Hard)
            return hardSpacing;

        if (difficulty == WordFillLetterDifficulty.Medium)
            return mediumSpacing;

        return easySpacing;
    }

    private int GetColumns(int itemCount, WordFillLetterDifficulty difficulty)
    {
        int baseColumns = easyColumns;

        if (difficulty == WordFillLetterDifficulty.Medium)
            baseColumns = mediumColumns;
        else if (difficulty == WordFillLetterDifficulty.Hard)
            baseColumns = hardColumns;

        baseColumns = Mathf.Clamp(baseColumns, 1, itemCount);

        if (!randomizeColumnCount || difficulty == WordFillLetterDifficulty.Easy)
            return baseColumns;

        int min = Mathf.Max(2, baseColumns - 1);
        int max = Mathf.Min(itemCount, baseColumns + 1);

        return Random.Range(min, max + 1);
    }

    private void ApplyDifficultyLook(WordFillLetterDifficulty difficulty)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;

            if (child == null)
                continue;

            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;

            if (difficulty == WordFillLetterDifficulty.Medium)
            {
                child.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-mediumRotation, mediumRotation));
            }
            else if (difficulty == WordFillLetterDifficulty.Hard)
            {
                float scale = 1f + Random.Range(-hardScaleVariation, hardScaleVariation);
                child.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-hardRotation, hardRotation));
                child.localScale = Vector3.one * scale;
            }
        }
    }
}