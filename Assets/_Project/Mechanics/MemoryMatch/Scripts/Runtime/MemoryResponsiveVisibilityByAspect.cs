using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryResponsiveVisibilityByAspect : MonoBehaviour
    {
        [Header("Aspect Detection")]
        [Tooltip("If width/height is below this value, the view is considered portrait/mobile-like.")]
        [SerializeField] private float portraitAspectThreshold = 0.9f;

        [Header("Objects To Hide")]
        [SerializeField] private GameObject[] hideInPortrait;
        [SerializeField] private GameObject[] hideInLandscape;

        [Header("Objects To Show")]
        [SerializeField] private GameObject[] showOnlyInPortrait;
        [SerializeField] private GameObject[] showOnlyInLandscape;

        private int lastWidth = -1;
        private int lastHeight = -1;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight)
            {
                return;
            }

            Apply();
        }

        public void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;

            float aspect = Screen.height <= 0 ? 1f : (float)Screen.width / Screen.height;
            bool isPortrait = aspect < portraitAspectThreshold;

            SetObjectsActive(hideInPortrait, !isPortrait);
            SetObjectsActive(hideInLandscape, isPortrait);
            SetObjectsActive(showOnlyInPortrait, isPortrait);
            SetObjectsActive(showOnlyInLandscape, !isPortrait);
        }

        private static void SetObjectsActive(GameObject[] objects, bool active)
        {
            if (objects == null)
            {
                return;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null && objects[i].activeSelf != active)
                {
                    objects[i].SetActive(active);
                }
            }
        }
    }
}
