using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class FractionPortionResponsiveLayout : MonoBehaviour
{
    [Header("Main Responsive Areas")]
    public RectTransform topBarRoot;
    public RectTransform questionRoot;
    public RectTransform mainContentRoot;
    public RectTransform portionSectionRoot;
    public RectTransform basketSectionRoot;
    public RectTransform instructionRoot;
    public RectTransform bottomBarRoot;

    [Header("Layout Weights")]
    public LayoutElement portionLayoutElement;
    public LayoutElement basketLayoutElement;
    public GridLayoutGroup basketGrid;
    public RectTransform basketCardsRoot;

    [Header("Pizza Board Scaling")]
    public RectTransform pizzaBoardRoot;
    public RectTransform pizzaTrayVisualRoot;
    public RectTransform pizzaEdgeVisualRoot;
    public RectTransform portionDropZoneRoot;
    [Range(0.7f, 0.99f)] public float pizzaBoardFillPercent = 0.94f;
    [Range(0.75f, 1f)] public float edgeToTrayPercent = 0.9f;
    [Range(0.7f, 1f)] public float dropZoneToEdgePercent = 0.88f;
    public float pizzaBoardInnerPadding = 12f;

    [Header("Tuning")]
    [Range(0.45f, 0.82f)] public float portionWidthWeight = 0.72f;
    [Range(0.18f, 0.55f)] public float basketWidthWeight = 0.28f;
    public float minBasketCardHeight = 92f;
    public float maxBasketCardHeight = 124f;
    public float minBasketCardWidth = 220f;
    public float maxBasketCardWidth = 360f;

    private RectTransform selfRect;
    private Vector2 lastSize;

    private void Awake()
    {
        selfRect = GetComponent<RectTransform>();
        Apply();
    }

    private void OnEnable()
    {
        selfRect = GetComponent<RectTransform>();
        Apply();
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }


    private void LateUpdate()
    {
        ApplyPizzaBoardSizing();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
            Apply();
    }
#endif

    public void Apply()
    {
        if (selfRect == null)
            selfRect = GetComponent<RectTransform>();

        if (selfRect == null)
            return;

        Vector2 size = selfRect.rect.size;
        if (size.x <= 1f || size.y <= 1f)
            return;

        if ((size - lastSize).sqrMagnitude < 0.25f && Application.isPlaying)
        {
            ApplyPizzaBoardSizing();
            return;
        }

        lastSize = size;

        if (portionLayoutElement != null)
        {
            portionLayoutElement.flexibleWidth = Mathf.Max(0.1f, portionWidthWeight);
            portionLayoutElement.flexibleHeight = 1f;
        }

        if (basketLayoutElement != null)
        {
            basketLayoutElement.flexibleWidth = Mathf.Max(0.1f, basketWidthWeight);
            basketLayoutElement.flexibleHeight = 1f;
        }

        if (basketGrid != null && basketCardsRoot != null)
        {
            FractionPortionBasketGridAutoSizer autoSizer = basketGrid.GetComponent<FractionPortionBasketGridAutoSizer>();
            if (autoSizer != null)
            {
                autoSizer.minCardWidth = Mathf.Max(300f, autoSizer.minCardWidth);
                autoSizer.Apply();
            }
            else
            {
                float width = basketCardsRoot.parent is RectTransform parentRect && parentRect.rect.width > 1f
                    ? parentRect.rect.width
                    : basketCardsRoot.rect.width;

                if (width <= 1f)
                    width = Mathf.Lerp(minBasketCardWidth, maxBasketCardWidth, basketWidthWeight);

                float availableWidth = Mathf.Max(80f, width - basketGrid.padding.left - basketGrid.padding.right);
                float cardWidth = availableWidth >= 300f ? availableWidth : Mathf.Max(120f, availableWidth);
                float cardHeight = Mathf.Clamp(size.y * 0.078f, minBasketCardHeight, maxBasketCardHeight);
                basketGrid.cellSize = new Vector2(cardWidth, cardHeight);
            }
        }

        ApplyPizzaBoardSizing();
    }

    public void ApplyPizzaBoardSizing()
    {
        RectTransform board = pizzaBoardRoot;
        if (board == null && portionDropZoneRoot != null)
            board = portionDropZoneRoot.parent as RectTransform;

        if (board == null)
            return;

        float availableWidth = Mathf.Max(1f, board.rect.width - pizzaBoardInnerPadding * 2f);
        float availableHeight = Mathf.Max(1f, board.rect.height - pizzaBoardInnerPadding * 2f);
        float traySide = Mathf.Min(availableWidth, availableHeight) * pizzaBoardFillPercent;
        traySide = Mathf.Max(64f, traySide);
        float edgeSide = Mathf.Max(56f, traySide * edgeToTrayPercent);
        float dropSide = Mathf.Max(52f, edgeSide * dropZoneToEdgePercent);

        if (pizzaTrayVisualRoot != null)
            CenterFillSquare(pizzaTrayVisualRoot, traySide);

        if (pizzaEdgeVisualRoot != null)
            CenterFillSquare(pizzaEdgeVisualRoot, edgeSide);

        if (portionDropZoneRoot != null)
            CenterFillSquare(portionDropZoneRoot, dropSide);
    }

    private static void CenterFillSquare(RectTransform rect, float side)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(side, side);
    }
}
