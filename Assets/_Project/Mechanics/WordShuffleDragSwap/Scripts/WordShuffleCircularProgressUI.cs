using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace WordShuffleDragSwap
{
    [AddComponentMenu("UI/Word Shuffle/Circular Progress UI")]
    public class WordShuffleCircularProgressUI : MaskableGraphic
    {
        [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
        [SerializeField, Min(3)] private int segments = 96;
        [SerializeField, Min(1f)] private float thickness = 10f;
        [SerializeField] private float startAngle = 90f;
        [SerializeField] private bool clockwise = true;

        private Tween fillTween;

        public float FillAmount
        {
            get => fillAmount;
            set
            {
                float clamped = Mathf.Clamp01(value);
                if (Mathf.Approximately(fillAmount, clamped))
                    return;

                fillAmount = clamped;
                SetVerticesDirty();
            }
        }

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Max(1f, value);
                SetVerticesDirty();
            }
        }

        public void SetProgress(float value, bool animate = false, float duration = 0.18f)
        {
            float target = Mathf.Clamp01(value);
            fillTween?.Kill();

            if (!animate || !Application.isPlaying || duration <= 0f)
            {
                FillAmount = target;
                return;
            }

            fillTween = DOVirtual.Float(fillAmount, target, duration, current => FillAmount = current)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        protected override void OnDisable()
        {
            fillTween?.Kill();
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (fillAmount <= 0f)
                return;

            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            float innerRadius = Mathf.Max(0f, outerRadius - thickness);

            if (outerRadius <= 0f || innerRadius >= outerRadius)
                return;

            int usedSegments = Mathf.Max(3, Mathf.CeilToInt(segments * fillAmount));
            float totalAngle = 360f * fillAmount * (clockwise ? -1f : 1f);
            float step = totalAngle / usedSegments;

            for (int i = 0; i < usedSegments; i++)
            {
                float angleA = (startAngle + step * i) * Mathf.Deg2Rad;
                float angleB = (startAngle + step * (i + 1)) * Mathf.Deg2Rad;

                Vector2 outerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * outerRadius;
                Vector2 outerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * outerRadius;
                Vector2 innerB = center + new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)) * innerRadius;
                Vector2 innerA = center + new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)) * innerRadius;

                UIVertex vertex = UIVertex.simpleVert;
                vertex.color = color;

                UIVertex[] quad = new UIVertex[4];
                quad[0] = vertex;
                quad[0].position = outerA;
                quad[1] = vertex;
                quad[1].position = outerB;
                quad[2] = vertex;
                quad[2].position = innerB;
                quad[3] = vertex;
                quad[3].position = innerA;

                vh.AddUIVertexQuad(quad);
            }
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            fillAmount = Mathf.Clamp01(fillAmount);
            segments = Mathf.Max(3, segments);
            thickness = Mathf.Max(1f, thickness);
            SetVerticesDirty();
        }
#endif
    }
}
