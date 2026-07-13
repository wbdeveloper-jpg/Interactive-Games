using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class BehaviourWheelWheelGraphic : MaskableGraphic
    {
        [Header("Wheel Shape")]
        [SerializeField, Min(6)] private int arcSegmentsPerSlice = 18;
        [SerializeField] private float startAngleOffset = 0f;
        [SerializeField, Range(3, 6)] private int activeSliceCount = 6;

        [Header("Slice Colours")]
        [SerializeField] private List<Color> sliceColors = new List<Color>
        {
            new Color(0.95f, 0.45f, 0.45f, 1f),
            new Color(0.98f, 0.72f, 0.35f, 1f),
            new Color(0.95f, 0.90f, 0.38f, 1f),
            new Color(0.38f, 0.78f, 0.52f, 1f),
            new Color(0.35f, 0.67f, 0.95f, 1f),
            new Color(0.65f, 0.50f, 0.90f, 1f)
        };

        [Header("Lines")]
        [SerializeField] private Color separatorColor = Color.white;
        [SerializeField, Min(0f)] private float separatorWidth = 4f;
        [SerializeField] private bool drawSeparators = true;

        [Header("Outer Border")]
        [SerializeField] private bool drawOuterBorder = true;
        [SerializeField] private Color outerBorderColor = Color.white;
        [SerializeField, Min(0f)] private float outerBorderThickness = 8f;
        [SerializeField, Min(24)] private int outerBorderSegments = 96;

        [Header("Center Cap")]
        [SerializeField] private bool drawCenterCap = true;
        [SerializeField] private Color centerCapColor = Color.white;
        [SerializeField, Range(0.02f, 0.25f)] private float centerCapRadiusMultiplier = 0.105f;
        [SerializeField, Min(12)] private int centerCapSegments = 36;

        public int SliceCount => Mathf.Clamp(activeSliceCount, 3, 6);
        public float SliceAngle => 360f / SliceCount;
        public float StartAngleOffset => startAngleOffset;

        public void SetActiveSliceCount(int count)
        {
            int safeCount = Mathf.Clamp(count, 3, 6);
            if (activeSliceCount == safeCount)
                return;

            activeSliceCount = safeCount;
            SetVerticesDirty();
        }

        public Color GetSliceColor(int index)
        {
            if (sliceColors == null || sliceColors.Count == 0)
                return color;

            int safeIndex = Mathf.Abs(index) % sliceColors.Count;
            return sliceColors[safeIndex];
        }

        public void SetSliceColor(int index, Color newColor)
        {
            if (sliceColors == null)
                sliceColors = new List<Color>();

            while (sliceColors.Count <= index)
                sliceColors.Add(Color.white);

            sliceColors[index] = newColor;
            SetVerticesDirty();
        }

        public float GetWheelRadius()
        {
            Rect rect = rectTransform.rect;
            return Mathf.Min(Mathf.Abs(rect.width), Mathf.Abs(rect.height)) * 0.5f;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(Mathf.Abs(rect.width), Mathf.Abs(rect.height)) * 0.5f;
            if (radius <= 0.01f)
                return;

            Vector2 center = rect.center;
            DrawSlices(vh, center, radius);

            if (drawSeparators && separatorWidth > 0.01f)
                DrawSeparators(vh, center, radius);

            if (drawOuterBorder && outerBorderThickness > 0.01f)
                DrawRing(vh, center, radius, Mathf.Max(0f, radius - outerBorderThickness), outerBorderColor, outerBorderSegments);

            if (drawCenterCap)
                DrawCircle(vh, center, radius * centerCapRadiusMultiplier, centerCapColor, centerCapSegments);
        }

        private void DrawSlices(VertexHelper vh, Vector2 center, float radius)
        {
            for (int sliceIndex = 0; sliceIndex < SliceCount; sliceIndex++)
            {
                float startAngle = startAngleOffset + sliceIndex * SliceAngle;
                float endAngle = startAngle + SliceAngle;
                Color sliceColor = GetSliceColor(sliceIndex) * color;

                int centerIndex = vh.currentVertCount;
                AddVertex(vh, center, sliceColor);

                for (int s = 0; s <= arcSegmentsPerSlice; s++)
                {
                    float t = (float)s / arcSegmentsPerSlice;
                    float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                    Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    AddVertex(vh, point, sliceColor);
                }

                for (int s = 0; s < arcSegmentsPerSlice; s++)
                    vh.AddTriangle(centerIndex, centerIndex + s + 1, centerIndex + s + 2);
            }
        }

        private void DrawSeparators(VertexHelper vh, Vector2 center, float radius)
        {
            for (int i = 0; i < SliceCount; i++)
            {
                float angle = (startAngleOffset + i * SliceAngle) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (separatorWidth * 0.5f);

                Vector2 inner = center;
                Vector2 outer = center + direction * radius;

                AddQuad(vh, inner - perpendicular, inner + perpendicular, outer + perpendicular, outer - perpendicular, separatorColor);
            }
        }

        private void DrawRing(VertexHelper vh, Vector2 center, float outerRadius, float innerRadius, Color ringColor, int segments)
        {
            innerRadius = Mathf.Clamp(innerRadius, 0f, outerRadius);
            int safeSegments = Mathf.Max(12, segments);

            for (int i = 0; i < safeSegments; i++)
            {
                float a0 = (float)i / safeSegments * Mathf.PI * 2f;
                float a1 = (float)(i + 1) / safeSegments * Mathf.PI * 2f;

                Vector2 outer0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * outerRadius;
                Vector2 outer1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * outerRadius;
                Vector2 inner1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * innerRadius;
                Vector2 inner0 = center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * innerRadius;

                AddQuad(vh, inner0, inner1, outer1, outer0, ringColor);
            }
        }

        private void DrawCircle(VertexHelper vh, Vector2 center, float radius, Color circleColor, int segments)
        {
            if (radius <= 0.01f)
                return;

            int safeSegments = Mathf.Max(12, segments);
            int startIndex = vh.currentVertCount;
            AddVertex(vh, center, circleColor);

            for (int i = 0; i <= safeSegments; i++)
            {
                float angle = (float)i / safeSegments * Mathf.PI * 2f;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddVertex(vh, point, circleColor);
            }

            for (int i = 0; i < safeSegments; i++)
                vh.AddTriangle(startIndex, startIndex + i + 1, startIndex + i + 2);
        }

        private static void AddQuad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color quadColor)
        {
            int index = vh.currentVertCount;
            AddVertex(vh, p0, quadColor);
            AddVertex(vh, p1, quadColor);
            AddVertex(vh, p2, quadColor);
            AddVertex(vh, p3, quadColor);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddVertex(VertexHelper vh, Vector2 position, Color vertexColor)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vh.AddVert(vertex);
        }
    }
}
