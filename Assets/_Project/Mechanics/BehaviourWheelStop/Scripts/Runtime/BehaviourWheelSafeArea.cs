using UnityEngine;

namespace BehaviourWheelStop
{
    public class BehaviourWheelSafeArea : MonoBehaviour
    {
        public RectTransform target;
        public bool applyOnMobileOnly = false;

        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            if (target == null)
                target = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
                ApplySafeArea();
        }

        public void ApplySafeArea()
        {
            if (!Application.isPlaying)
                return;

            if (target == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            if (applyOnMobileOnly && !Application.isMobilePlatform)
                return;

            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            target.anchorMin = anchorMin;
            target.anchorMax = anchorMax;
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }
    }
}
