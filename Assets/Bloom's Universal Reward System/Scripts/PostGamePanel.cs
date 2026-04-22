/*
 * ============================================================
 * PostGamePanel.cs  —  MonoBehaviour on Panel 2 GameObject
 * ============================================================
 * PURPOSE:
 *   Manages the Post-Game panel lifecycle:
 *     1. Receives skill results from RewardManager
 *     2. Instantiates BloomCardPost prefabs and reveals them one by one
 *     3. Wires Play Again and Home buttons to the active scene's callbacks
 *        via IGameSceneCallbacks interface
 *
 * HIERARCHY:
 *   Panel_PostGame (this script + CanvasGroup)
 *     ├── Heading           (TextMeshProUGUI)
 *     ├── CardHolder        (Layout Group — cards spawn here)
 *     ├── BtnPlayAgain      (Button)
 *     ├── BtnHome           (Button)
 *     └── BtnInfo (eye)     (Button — opens Info Panel)
 *
 * SETUP:
 *   • Assign all references in inspector.
 *   • Medal sprites assigned here and passed to each card.
 *   • RewardManager calls Show(results) to activate.
 * ============================================================
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RewardSystem
{
    public class PostGamePanel : MonoBehaviour
    {
        [Header("Card Prefab & Holder")]
        [SerializeField] private BloomCardPost cardPrefab;
        [SerializeField] private Transform     cardHolder;

        [Header("Medal Sprites")]
        [SerializeField] private Sprite bronzeSprite;
        [SerializeField] private Sprite silverSprite;
        [SerializeField] private Sprite goldSprite;

        [Header("Buttons")]
        [SerializeField] private Button btnPlayAgain;
        [SerializeField] private Button btnHome;
        [SerializeField] private Button btnInfo;

        [Header("Animation")]
        [SerializeField] private float cardRevealStagger = 0.8f; // seconds between each card reveal

        [Header("Fade In")]
        [SerializeField] private float fadeInDuration = 0.5f;

        private CanvasGroup            _canvasGroup;
        private List<BloomCardPost>    _spawnedCards = new();

        // ── Unity ───────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ── Public API ──────────────────────────────────────────

        /// <summary>
        /// Show post-game results. Called by RewardManager after game over.
        /// </summary>
        public void Show(
            List<SkillResult>   results,
            List<BloomSkillData> allSkillData,
            System.Action        onInfoClicked)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;

            // Wire buttons to current scene's callbacks (looked up fresh each time)
            btnPlayAgain.onClick.RemoveAllListeners();
            btnHome.onClick.RemoveAllListeners();
            btnInfo.onClick.RemoveAllListeners();

            btnPlayAgain.onClick.AddListener(HandlePlayAgain);
            btnHome.onClick.AddListener(HandleHome);
            btnInfo.onClick.AddListener(() => onInfoClicked?.Invoke());

            // Clear previous cards
            ClearCards();

            // Spawn cards
            for (int i = 0; i < results.Count; i++)
            {
                BloomSkillData data = allSkillData.Find(d => d.skillType == results[i].skillType);
                if (data == null) continue;

                BloomCardPost card = Instantiate(cardPrefab, cardHolder);
                card.Populate(results[i], data, bronzeSprite, silverSprite, goldSprite);
                _spawnedCards.Add(card);
            }

            StartCoroutine(RevealSequence());
        }

        // ── Private ─────────────────────────────────────────────

        private IEnumerator RevealSequence()
        {
            // Fade panel in first
            yield return StartCoroutine(FadeIn());

            // Then reveal cards one by one with stagger
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                _spawnedCards[i].PlayRevealAnimation(delay: i * cardRevealStagger);
            }
        }

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private void HandlePlayAgain()
        {
            // Find the scene's callback implementer and call it
            IGameSceneCallbacks callbacks = FindAnyObjectByType<MonoBehaviour>() is MonoBehaviour mb
                ? mb.GetComponentInParent<IGameSceneCallbacks>() ?? FindCallbacksInScene()
                : null;

            callbacks?.OnPlayAgain();
            Hide();
        }

        private void HandleHome()
        {
            IGameSceneCallbacks callbacks = FindCallbacksInScene();
            callbacks?.OnHome();
            Hide();
        }

        /// <summary>Searches all MonoBehaviours in scene for IGameSceneCallbacks.</summary>
        private IGameSceneCallbacks FindCallbacksInScene()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IGameSceneCallbacks cb) return cb;
            }
            Debug.LogWarning("[RewardSystem] No IGameSceneCallbacks found in scene.");
            return null;
        }

        private void Hide()
        {
            ClearCards();
            gameObject.SetActive(false);
        }

        private void ClearCards()
        {
            foreach (var card in _spawnedCards)
            {
                if (card != null) Destroy(card.gameObject);
            }
            _spawnedCards.Clear();
        }
    }

    // Extension to keep BloomCardPost.Populate signature clean
    public static class BloomCardPostExtensions
    {
        public static void Populate(
            this BloomCardPost card,
            SkillResult        result,
            BloomSkillData     data,
            Sprite             bronze,
            Sprite             silver,
            Sprite             gold)
        {
            card.Populate(data, result, bronze, silver, gold);
        }
    }
}
