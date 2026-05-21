using UnityEngine;
using UnityEngine.UI;

namespace NGEducation.MemoryMatch
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class MemoryGridLayoutFitter : MonoBehaviour
    {
        [Header("Grid")]
        [Min(1)]
        [SerializeField] private int columns = 4;

        [Min(1)]
        [SerializeField] private int rows = 3;

        [Header("Card Shape")]
        [Tooltip("Width / Height. 1 = square, 0.75 = portrait/taller, 1.15 = wider.")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float cardAspectRatio = 0.9f;

        [Header("Spacing")]
        [SerializeField] private Vector2 spacing = new Vector2(16f, 16f);

        [Header("Padding")]
        [SerializeField, Min(0)] private int paddingLeft = 16;
        [SerializeField, Min(0)] private int paddingRight = 16;
        [SerializeField, Min(0)] private int paddingTop = 16;
        [SerializeField, Min(0)] private int paddingBottom = 16;

        private RectTransform rectTransform;
        private GridLayoutGroup gridLayoutGroup;

        public int Columns => Mathf.Max(1, columns);
        public int Rows => Mathf.Max(1, rows);
        public float CardAspectRatio => Mathf.Clamp(cardAspectRatio, 0.5f, 1.5f);

        private void Awake()
        {
            CacheComponents();
        }

        private void Start()
        {
            ApplyLayout();
        }

        private void OnEnable()
        {
            ApplyLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyLayout();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);
            cardAspectRatio = Mathf.Clamp(cardAspectRatio, 0.5f, 1.5f);
            ApplyLayout();
        }
#endif

        public void SetGrid(int newColumns, int newRows)
        {
            columns = Mathf.Max(1, newColumns);
            rows = Mathf.Max(1, newRows);
            ApplyLayout();
        }

        public void SetCardAspectRatio(float newAspectRatio)
        {
            cardAspectRatio = Mathf.Clamp(newAspectRatio, 0.5f, 1.5f);
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            CacheComponents();

            if (rectTransform == null || gridLayoutGroup == null)
            {
                return;
            }

            Rect rect = rectTransform.rect;

            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            RectOffset runtimePadding = new RectOffset(
                Mathf.Max(0, paddingLeft),
                Mathf.Max(0, paddingRight),
                Mathf.Max(0, paddingTop),
                Mathf.Max(0, paddingBottom));

            gridLayoutGroup.padding = runtimePadding;
            gridLayoutGroup.spacing = spacing;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = Columns;
            gridLayoutGroup.childAlignment = TextAnchor.MiddleCenter;

            float availableWidth = rect.width - runtimePadding.left - runtimePadding.right - spacing.x * (Columns - 1);
            float availableHeight = rect.height - runtimePadding.top - runtimePadding.bottom - spacing.y * (Rows - 1);

            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                return;
            }

            float maxCellWidth = availableWidth / Columns;
            float maxCellHeight = availableHeight / Rows;

            float widthFromHeight = maxCellHeight * CardAspectRatio;
            float finalWidth = Mathf.Min(maxCellWidth, widthFromHeight);
            float finalHeight = finalWidth / CardAspectRatio;

            if (finalHeight > maxCellHeight)
            {
                finalHeight = maxCellHeight;
                finalWidth = finalHeight * CardAspectRatio;
            }

            gridLayoutGroup.cellSize = new Vector2(
                Mathf.Max(1f, finalWidth),
                Mathf.Max(1f, finalHeight));
        }

        private void CacheComponents()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (gridLayoutGroup == null)
            {
                gridLayoutGroup = GetComponent<GridLayoutGroup>();
            }
        }
    }
}
