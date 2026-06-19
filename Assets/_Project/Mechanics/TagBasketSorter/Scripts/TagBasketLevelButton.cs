using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TagBasketSorter
{
    [DisallowMultipleComponent]
    public sealed class TagBasketLevelButton : MonoBehaviour
    {
        public Button button;
        public TMP_Text levelText;
        public GameObject lockOverlay;
        public TMP_Text lockText;

        private int levelIndex;
        private TagBasketSortGameManager gameManager;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Reset()
        {
            EnsureReferences();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                EnsureReferences();
        }

        public void Setup(TagBasketSortGameManager manager, int index, string title, bool unlocked)
        {
            EnsureReferences();

            gameManager = manager;
            levelIndex = index;

            if (levelText != null)
                levelText.text = string.IsNullOrWhiteSpace(title) ? $"Level {index + 1}" : title;

            if (lockOverlay != null)
                lockOverlay.SetActive(!unlocked);

            if (lockText != null)
                lockText.text = unlocked ? string.Empty : "LOCKED";

            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveListener(OpenLevel);
                button.onClick.AddListener(OpenLevel);
            }
        }

        private void OpenLevel()
        {
            if (gameManager != null)
                gameManager.OpenLevelFromLanding(levelIndex);
        }

        private void EnsureReferences()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (levelText == null)
                levelText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
