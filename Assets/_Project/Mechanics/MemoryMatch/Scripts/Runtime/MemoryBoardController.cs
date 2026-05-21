using System;
using System.Collections.Generic;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryBoardController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cardParent;
        [SerializeField] private MemoryCardView cardPrefab;

        [Header("Shuffle")]
        [SerializeField] private bool shuffleCards = true;
        [Tooltip("0 uses a random seed. Any other value gives deterministic shuffle for testing.")]
        [SerializeField] private int randomSeed = 0;

        private readonly List<MemoryCardView> spawnedCards = new List<MemoryCardView>();

        public IReadOnlyList<MemoryCardView> SpawnedCards => spawnedCards;

        public List<MemoryCardView> BuildBoard(
            List<MemoryCardRuntimeData> cards,
            Action<MemoryCardView> onCardClicked,
            MemoryThemeConfig themeConfig = null)
        {
            ClearBoard();

            if (cardParent == null)
            {
                Debug.LogError("MemoryBoardController is missing Card Parent.", this);
                return spawnedCards;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("MemoryBoardController is missing Card Prefab.", this);
                return spawnedCards;
            }

            List<MemoryCardRuntimeData> runtimeCards = new List<MemoryCardRuntimeData>(cards);

            if (shuffleCards)
            {
                Shuffle(runtimeCards);
            }

            for (int i = 0; i < runtimeCards.Count; i++)
            {
                MemoryCardView cardView = Instantiate(cardPrefab, cardParent);
                cardView.Initialize(runtimeCards[i], onCardClicked);
                cardView.ApplyTheme(themeConfig);
                spawnedCards.Add(cardView);
            }

            return spawnedCards;
        }

        public void SetCardsInputEnabled(bool enabled)
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null)
                {
                    spawnedCards[i].SetInputEnabled(enabled);
                }
            }
        }

        public void ClearBoard()
        {
            for (int i = spawnedCards.Count - 1; i >= 0; i--)
            {
                if (spawnedCards[i] != null)
                {
                    Destroy(spawnedCards[i].gameObject);
                }
            }

            spawnedCards.Clear();

            if (cardParent == null)
            {
                return;
            }

            for (int i = cardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(cardParent.GetChild(i).gameObject);
            }
        }

        private void Shuffle<T>(IList<T> list)
        {
            System.Random random = randomSeed == 0
                ? new System.Random()
                : new System.Random(randomSeed);

            for (int i = list.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
