using UnityEngine;

namespace BehaviourWheelStop
{
    public class BehaviourWheelResponsiveLayout : MonoBehaviour
    {
        [Header("Wheel Sizing")]
        public RectTransform availableCenterArea;
        public RectTransform wheelRoot;
        [Range(0.45f, 0.95f)] public float wheelAreaFill = 0.82f;
        public float minWheelSize = 360f;
        public float maxWheelSize = 620f;

        [Header("Optional Rebuild")]
        public BehaviourWheelSpinner spinner;

        private float lastWidth = -1f;
        private float lastHeight = -1f;

        private void LateUpdate()
        {
            ApplyLayoutIfNeeded();
        }

        public void ApplyLayoutIfNeeded()
        {
            if (availableCenterArea == null || wheelRoot == null)
                return;

            Rect rect = availableCenterArea.rect;
            float width = Mathf.Abs(rect.width);
            float height = Mathf.Abs(rect.height);

            if (width <= 0.01f || height <= 0.01f)
                return;

            if (Mathf.Approximately(width, lastWidth) && Mathf.Approximately(height, lastHeight))
                return;

            lastWidth = width;
            lastHeight = height;

            float size = Mathf.Min(width, height) * wheelAreaFill;
            size = Mathf.Clamp(size, minWheelSize, maxWheelSize);

            wheelRoot.anchorMin = new Vector2(0.5f, 0.5f);
            wheelRoot.anchorMax = new Vector2(0.5f, 0.5f);
            wheelRoot.pivot = new Vector2(0.5f, 0.5f);
            wheelRoot.sizeDelta = new Vector2(size, size);

            if (spinner != null)
                spinner.RebuildSliceContentLayout(true);
        }
    }
}
