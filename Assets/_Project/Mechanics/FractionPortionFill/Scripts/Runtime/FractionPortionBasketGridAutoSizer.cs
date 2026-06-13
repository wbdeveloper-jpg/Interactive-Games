using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class FractionPortionBasketGridAutoSizer : MonoBehaviour
{
    [Header("References")]
    public RectTransform viewportRoot;

    [Header("Card Width")]
    [Tooltip("Preferred minimum card width when the basket has enough space.")]
    public float minCardWidth = 300f;

    [Tooltip("Left/right breathing room inside the scroll viewport.")]
    public float sidePadding = 14f;

    [Tooltip("When viewport is narrower than min width, shrink to fit instead of overflowing on small screens.")]
    public bool shrinkBelowMinOnSmallScreens = true;

    [Header("Card Height")]
    public float minCardHeight = 96f;
    public float maxCardHeight = 124f;
    public float heightPercentOfViewport = 0.18f;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;
    private Vector2 lastViewportSize;

    private void Awake()
    {
        Cache();
        Apply();
    }

    private void OnEnable()
    {
        Cache();
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
            Apply();
    }
#endif

    private void LateUpdate()
    {
        Apply();
    }

    public void Apply()
    {
        Cache();

        if (grid == null)
            return;

        RectTransform source = viewportRoot != null ? viewportRoot : transform.parent as RectTransform;
        if (source == null)
            source = rectTransform;

        if (source == null)
            return;

        Vector2 viewportSize = source.rect.size;
        if (viewportSize.x <= 1f || viewportSize.y <= 1f)
            return;

        if ((viewportSize - lastViewportSize).sqrMagnitude < 0.05f && Application.isPlaying)
            return;

        lastViewportSize = viewportSize;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;
        grid.childAlignment = TextAnchor.UpperCenter;

        int padding = Mathf.Max(0, Mathf.RoundToInt(sidePadding));
        grid.padding.left = padding;
        grid.padding.right = padding;

        float availableWidth = Mathf.Max(1f, viewportSize.x - grid.padding.left - grid.padding.right);
        float cardWidth = availableWidth;

        if (availableWidth >= minCardWidth)
            cardWidth = availableWidth;
        else if (!shrinkBelowMinOnSmallScreens)
            cardWidth = minCardWidth;

        float cardHeight = Mathf.Clamp(viewportSize.y * heightPercentOfViewport, minCardHeight, maxCardHeight);
        grid.cellSize = new Vector2(cardWidth, cardHeight);

        if (rectTransform != null)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(0f, rectTransform.offsetMin.y);
            rectTransform.offsetMax = new Vector2(0f, rectTransform.offsetMax.y);
        }
    }

    private void Cache()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();

        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }
}
