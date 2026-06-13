using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]
public class FractionPortionWedgeGraphic : MaskableGraphic, ICanvasRaycastFilter
{
    [Range(3, 96)] public int segments = 36;
    [SerializeField] private float startAngle;
    [SerializeField] private float endAngle = 90f;
    [Range(0f, 0.15f)] public float visualGapPercent = 0.01f;


    protected override void Awake()
    {
        EnsureCanvasRendererExists();
        base.Awake();
    }

    protected override void OnEnable()
    {
        EnsureCanvasRendererExists();
        base.OnEnable();
    }

    private void EnsureCanvasRendererExists()
    {
        if (GetComponent<CanvasRenderer>() == null)
            gameObject.AddComponent<CanvasRenderer>();
    }

    public void SetAngles(float start, float end)
    {
        startAngle = start;
        endAngle = end;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float gapRadius = Mathf.Clamp01(visualGapPercent) * radius;
        radius = Mathf.Max(1f, radius - gapRadius);

        float sweep = GetPositiveSweep();
        int steps = Mathf.Max(3, Mathf.CeilToInt(segments * (sweep / 360f)));
        List<UIVertex> vertices = new List<UIVertex>();

        UIVertex centerVertex = UIVertex.simpleVert;
        centerVertex.color = color;
        centerVertex.position = center;
        vertices.Add(centerVertex);

        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            float angle = (startAngle + sweep * t) * Mathf.Deg2Rad;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = point;
            vertices.Add(vertex);
        }

        for (int i = 0; i < vertices.Count; i++)
            vh.AddVert(vertices[i]);

        for (int i = 1; i < vertices.Count - 1; i++)
            vh.AddTriangle(0, i, i + 1);
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
            return false;

        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        Vector2 delta = localPoint - center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;

        if (delta.sqrMagnitude > radius * radius)
            return false;

        if (delta.sqrMagnitude < 9f)
            return true;

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        return IsAngleInside(angle);
    }

    private bool IsAngleInside(float angle)
    {
        float start = NormalizeAngle(startAngle);
        float sweep = GetPositiveSweep();
        float relative = NormalizeAngle(angle - start);
        return relative >= 0f && relative <= sweep;
    }

    private float GetPositiveSweep()
    {
        float sweep = Mathf.DeltaAngle(startAngle, endAngle);
        if (sweep <= 0f)
            sweep += 360f;
        return sweep;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f)
            angle += 360f;
        return angle;
    }
}
