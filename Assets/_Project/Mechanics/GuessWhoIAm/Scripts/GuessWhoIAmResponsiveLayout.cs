using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuessWhoIAm
{
    [ExecuteAlways]
    public class GuessWhoIAmResponsiveLayout : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform canvasRect;

        [Header("Key Layout Elements")]
        [SerializeField] private LayoutElement topBarLayout;
        [SerializeField] private LayoutElement rightMascotPanelLayout;
        [SerializeField] private LayoutElement clueRowLayout;
        [SerializeField] private RectTransform optionsGridRect;
        [SerializeField] private LayoutElement optionsGridLayout;
        [SerializeField] private GridLayoutGroup optionsGrid;
        [SerializeField] private RectTransform clueRowRect;
        [SerializeField] private HorizontalLayoutGroup clueRowGroup;
        [SerializeField] private List<LayoutElement> clueCardLayoutElements = new List<LayoutElement>();

        [Header("Reference Sizes")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField] private float minTopBarHeight = 65f;
        [SerializeField] private float maxTopBarHeight = 75f;
        [SerializeField] private float rightPanelMinWidth = 420f;
        [SerializeField] private float rightPanelPreferredWidth = 560f;
        [SerializeField] private float rightPanelMaxWidth = 650f;
        [SerializeField] private float longPhoneAspect = 2.05f;
        [SerializeField] private float tabletAspect = 1.45f;

        [Header("Spacing")]
        [SerializeField] private float bodyHorizontalPadding = 28f;
        [SerializeField] private float mainColumnSpacing = 28f;
        [SerializeField] private float optionSpacing = 24f;
        [SerializeField] private float clueSpacing = 34f;
        [SerializeField] private float clueRowHorizontalPadding = 40f;
        [SerializeField] private float clueRowVerticalPadding = 56f;
        [SerializeField] private float maxTabletOptionWidth = 520f;
        [SerializeField] private float minOptionButtonHeight = 118f;
        [SerializeField] private float maxOptionButtonHeight = 150f;

        private Vector2 lastSize;

        private void Reset()
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasRect = canvas.transform as RectTransform;
        }

        private void OnEnable()
        {
            ApplyResponsiveLayout();
        }

        private void OnValidate()
        {
            ApplyResponsiveLayout();
        }

        private void Update()
        {
            if (!Application.isPlaying)
                ApplyResponsiveLayout();
            else if (GetCurrentCanvasSize() != lastSize)
                ApplyResponsiveLayout();
        }

        [ContextMenu("Apply Responsive Layout")]
        public void ApplyResponsiveLayout()
        {
            Vector2 size = GetCurrentCanvasSize();
            if (size.x <= 1f || size.y <= 1f)
                return;

            lastSize = size;

            float width = size.x;
            float height = size.y;
            float aspect = width / Mathf.Max(1f, height);
            float heightScale = height / referenceResolution.y;
            float widthScale = width / referenceResolution.x;
            float balancedScale = Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.85f, 1.25f);

            float topHeight = Mathf.Clamp(height * 0.069f, minTopBarHeight, maxTopBarHeight);
            if (topBarLayout != null)
            {
                topBarLayout.minHeight = minTopBarHeight;
                topBarLayout.preferredHeight = topHeight;
                topBarLayout.flexibleHeight = 0f;
            }

            float rightWidthPercent = aspect > longPhoneAspect ? 0.23f : (aspect < tabletAspect ? 0.25f : 0.27f);
            float rightWidth = Mathf.Clamp(width * rightWidthPercent, rightPanelMinWidth, rightPanelMaxWidth);
            rightWidth = Mathf.Max(rightWidth, rightPanelPreferredWidth * Mathf.Clamp(widthScale, 0.9f, 1.05f));
            rightWidth = Mathf.Clamp(rightWidth, rightPanelMinWidth, rightPanelMaxWidth);

            if (rightMascotPanelLayout != null)
            {
                rightMascotPanelLayout.minWidth = rightPanelMinWidth;
                rightMascotPanelLayout.preferredWidth = rightWidth;
                rightMascotPanelLayout.flexibleWidth = 0f;
            }

            float bodyHeight = Mathf.Max(400f, height - topHeight - 40f);
            float estimatedLeftWidth = width - rightWidth - bodyHorizontalPadding * 2f - mainColumnSpacing;
            float clueHeightRatio = aspect > longPhoneAspect ? 0.55f : (aspect < tabletAspect ? 0.58f : 0.57f);
            float maxClueRowHeight = Mathf.Clamp(bodyHeight * clueHeightRatio, 360f * balancedScale, 560f * balancedScale);
            float spacing = optionSpacing * balancedScale;
            float clueGap = clueSpacing * balancedScale;
            float availableCardWidth = Mathf.Max(360f, estimatedLeftWidth - clueRowHorizontalPadding * balancedScale - clueGap * 2f);
            float cardSize = Mathf.Floor(Mathf.Min(maxClueRowHeight - clueRowVerticalPadding * balancedScale, availableCardWidth / 3f));
            cardSize = Mathf.Clamp(cardSize, 300f * balancedScale, 500f * balancedScale);
            float clueRowHeight = cardSize + clueRowVerticalPadding * balancedScale;

            if (clueRowLayout != null)
            {
                clueRowLayout.minHeight = clueRowHeight * 0.96f;
                clueRowLayout.preferredHeight = clueRowHeight;
                clueRowLayout.flexibleHeight = 0f;
            }

            if (clueRowGroup != null)
            {
                clueRowGroup.spacing = clueGap;
                clueRowGroup.childAlignment = TextAnchor.MiddleCenter;
                clueRowGroup.childForceExpandWidth = false;
                clueRowGroup.childForceExpandHeight = false;
            }

            for (int i = 0; i < clueCardLayoutElements.Count; i++)
            {
                if (clueCardLayoutElements[i] == null)
                    continue;

                clueCardLayoutElements[i].minWidth = cardSize * 0.96f;
                clueCardLayoutElements[i].preferredWidth = cardSize;
                clueCardLayoutElements[i].minHeight = cardSize * 0.96f;
                clueCardLayoutElements[i].preferredHeight = cardSize;
                clueCardLayoutElements[i].flexibleWidth = 0f;
                clueCardLayoutElements[i].flexibleHeight = 0f;
            }

            if (optionsGrid != null)
            {
                float availableOptionWidth = optionsGridRect != null && optionsGridRect.rect.width > 50f ? optionsGridRect.rect.width : estimatedLeftWidth;
                float availableOptionHeight = optionsGridRect != null && optionsGridRect.rect.height > 50f
                    ? optionsGridRect.rect.height
                    : bodyHeight - clueRowHeight - 30f;

                float cellWidth = Mathf.Max(300f, (availableOptionWidth - spacing) * 0.5f);
                float cellHeight = Mathf.Clamp((availableOptionHeight - spacing) * 0.5f, minOptionButtonHeight * balancedScale, maxOptionButtonHeight * balancedScale);

                if (aspect > longPhoneAspect)
                    cellHeight = Mathf.Clamp(cellHeight, minOptionButtonHeight * balancedScale, maxOptionButtonHeight * balancedScale);

                if (aspect < tabletAspect)
                {
                    cellWidth = Mathf.Min(cellWidth, maxTabletOptionWidth);
                    cellHeight = Mathf.Clamp(cellHeight, minOptionButtonHeight * balancedScale, maxOptionButtonHeight * balancedScale);
                }

                optionsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                optionsGrid.constraintCount = 2;
                optionsGrid.spacing = new Vector2(spacing, spacing);
                optionsGrid.childAlignment = TextAnchor.MiddleCenter;
                optionsGrid.cellSize = new Vector2(cellWidth, cellHeight);

                if (optionsGridLayout != null)
                {
                    float gridHeight = cellHeight * 2f + spacing;
                    optionsGridLayout.minHeight = gridHeight;
                    optionsGridLayout.preferredHeight = gridHeight;
                    optionsGridLayout.flexibleHeight = 0f;
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
        }

        private Vector2 GetCurrentCanvasSize()
        {
            if (canvasRect == null)
            {
                if (canvas == null)
                    canvas = GetComponentInParent<Canvas>();

                if (canvas != null)
                    canvasRect = canvas.transform as RectTransform;
            }

            if (canvasRect != null)
            {
                Rect rect = canvasRect.rect;
                if (rect.width > 1f && rect.height > 1f)
                    return new Vector2(rect.width, rect.height);
            }

            return referenceResolution;
        }
    }
}
