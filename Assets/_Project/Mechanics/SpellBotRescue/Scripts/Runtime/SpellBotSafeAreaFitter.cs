using UnityEngine;

namespace NarayanaGames.SpellBotRescue
{
    [ExecuteAlways]
    public class SpellBotSafeAreaFitter : MonoBehaviour
    {
        public bool applySafeArea = true;
        public bool updateEveryFrameInEditor = true;

        private RectTransform rectTransform;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        private void OnEnable()
        {
            rectTransform = transform as RectTransform;
            Apply();
        }

        private void Update()
        {
            if (!Application.isPlaying && !updateEveryFrameInEditor)
            {
                return;
            }

            if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (!applySafeArea || rectTransform == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            lastSafeArea = safeArea;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            if (Screen.width > 0 && Screen.height > 0)
            {
                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;
            }

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
