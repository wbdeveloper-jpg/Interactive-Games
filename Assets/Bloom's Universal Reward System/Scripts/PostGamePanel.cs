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
        [SerializeField] private Transform cardHolder;

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

        private CanvasGroup _canvasGroup;
        private List<BloomCardPost> _spawnedCards = new();

        // Stored so btnInfo can be wired once in Awake, not re-added every Show()
        public System.Action OnInfoClicked;

        // Fired when panel hides itself (Play Again or Home pressed)
        // RewardManager uses this to deactivate bgCanvas
        public System.Action OnHidden;

        // ── Unity ───────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Wire once — delegates to whatever RewardManager last assigned
            btnInfo.onClick.AddListener(() => OnInfoClicked?.Invoke());
        }

        // ── Public API ──────────────────────────────────────────

        /// <summary>
        /// Show post-game results. Called by RewardManager after game over.
        /// </summary>
        public void Show(
            List<SkillResult> results,
            List<BloomSkillData> allSkillData)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;

            // Wire play/home buttons fresh each call (scene callbacks change per scene)
            btnPlayAgain.onClick.RemoveAllListeners();
            btnHome.onClick.RemoveAllListeners();

            btnPlayAgain.onClick.AddListener(HandlePlayAgain);
            btnHome.onClick.AddListener(HandleHome);

            // Clear previous cards
            ClearCards();

            // Spawn cards
            for (int i = 0; i < results.Count; i++)
            {
                BloomSkillData data = allSkillData.Find(d => d.skillType == results[i].skillType);
                if (data == null) continue;

                BloomCardPost card = Instantiate(cardPrefab, cardHolder);
                card.Populate(data, results[i], bronzeSprite, silverSprite, goldSprite);
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
            IGameSceneCallbacks callbacks = FindCallbacksInScene();
            // Hide fully BEFORE triggering scene reload — prevents panel surviving mid-transition on Android
            Hide();
            callbacks?.OnPlayAgain();
        }

        private void HandleHome()
        {
            IGameSceneCallbacks callbacks = FindCallbacksInScene();
            // Hide fully BEFORE scene load — Android destroys scene objects immediately on LoadScene
            // If we call OnHome() first, the scene starts unloading while Hide() is still executing
            Hide();
            callbacks?.OnHome();
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
            // Stop all running coroutines (RevealSequence / FadeIn may still be active)
            StopAllCoroutines();
            // Force alpha to 0 explicitly — CanvasGroup may be mid-tween on Android
            _canvasGroup.alpha = 0f;
            ClearCards();
            gameObject.SetActive(false);
            // Notify RewardManager so it can deactivate bgCanvas
            OnHidden?.Invoke();
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

    // Note: BloomCardPostExtensions removed — Populate() is called directly
    // with correct parameter order (data, result, sprites) matching BloomCardPost signature.
}