using UnityEngine;

namespace NGEducation.MemoryMatch
{
    [ExecuteAlways]
    public sealed class MemoryRelativeCornerBadgeFitter : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private RectTransform badgeRect;
        [SerializeField] private RectTransform parentRect;

        [Header("Sizing")]
        [Tooltip("Badge size as percentage of parent min(width,height).")]
        [SerializeField, Range(0.08f, 0.5f)] private float sizePercent = 0.22f;

        [SerializeField, Min(8f)] private float minSize = 18f;
        [SerializeField, Min(8f)] private float maxSize = 48f;

        [Header("Corner")]
        [SerializeField] private Vector2 anchor = new Vector2(1f, 0f);
        [SerializeField] private Vector2 pivot = new Vector2(1f, 0f);

        [Tooltip("Inset as percentage of badge size.")]
        [SerializeField, Range(0f, 1f)] private float insetPercent = 0.18f;

        private Vector2 lastParentSize;

        private void Reset()
        {
            badgeRect = transform as RectTransform;
            parentRect = transform.parent as RectTransform;
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (parentRect == null)
            {
                parentRect = transform.parent as RectTransform;
            }

            if (parentRect == null)
            {
                return;
            }

            Vector2 size = parentRect.rect.size;
            if (size != lastParentSize)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (badgeRect == null)
            {
                badgeRect = transform as RectTransform;
            }

            if (parentRect == null)
            {
                parentRect = transform.parent as RectTransform;
            }

            if (badgeRect == null || parentRect == null)
            {
                return;
            }

            lastParentSize = parentRect.rect.size;
            float baseSize = Mathf.Min(Mathf.Abs(lastParentSize.x), Mathf.Abs(lastParentSize.y));
            float badgeSize = Mathf.Clamp(baseSize * sizePercent, minSize, maxSize);
            float inset = badgeSize * insetPercent;

            badgeRect.anchorMin = anchor;
            badgeRect.anchorMax = anchor;
            badgeRect.pivot = pivot;
            badgeRect.sizeDelta = new Vector2(badgeSize, badgeSize);

            float x = anchor.x >= 0.5f ? -inset : inset;
            float y = anchor.y >= 0.5f ? -inset : inset;
            badgeRect.anchoredPosition = new Vector2(x, y);
        }
    }
}
