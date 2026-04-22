/*
 * ============================================================
 * PreGamePanel.cs  —  MonoBehaviour on Panel 1 GameObject
 * ============================================================
 * PURPOSE:
 *   Manages the Pre-Game panel lifecycle:
 *     1. Receives skill list from RewardManager
 *     2. Instantiates BloomCardPre prefabs, populates and animates them
 *     3. Starts a countdown coroutine to auto-fade the panel
 *     4. Pauses countdown when Info Panel opens, resumes on close
 *     5. Fades out and deactivates itself when countdown completes
 *
 * HIERARCHY:
 *   Panel_PreGame (this script + CanvasGroup)
 *     ├── Heading           (TextMeshProUGUI)
 *     ├── CardHolder        (Layout Group — cards spawn here)
 *     └── BtnInfo (eye)     (Button — opens Info Panel)
 *
 * SETUP:
 *   • Attach CanvasGroup to this GameObject for fade support.
 *   • Assign cardPrefab, cardHolder, infoButton in inspector.
 *   • RewardManager calls Show(skills) to activate this panel.
 * ============================================================
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RewardSystem
{
    public class PreGamePanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BloomCardPre cardPrefab;
        [SerializeField] private Transform    cardHolder;
        [SerializeField] private Button       infoButton;

        [Header("Timing")]
        [SerializeField] private float autoHideDelay  = 5.5f; // seconds before fade starts
        [SerializeField] private float fadeDuration   = 0.8f;
        [SerializeField] private float cardStagger    = 0.15f; // delay between each card appearing

        // Cached components
        private CanvasGroup       _canvasGroup;
        private List<BloomCardPre> _spawnedCards = new();

        // Countdown state
        private bool  _infoPanelOpen  = false;
        private float _countdownElapsed = 0f;
        private bool  _countdownRunning = false;

        // Callbacks
        public System.Action OnPanelComplete;
        public System.Action OnInfoClicked;   // set by RewardManager to open InfoPanel

        // ── Unity ───────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ── Public API ──────────────────────────────────────────

        /// <summary>
        /// Activate and populate the pre-game panel with the given skills.
        /// Called by RewardManager.
        /// </summary>
        public void Show(List<SkillEntry> skills, List<BloomSkillData> allSkillData)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _countdownElapsed  = 0f;
            _infoPanelOpen     = false;

            // Wire eye button fresh — avoids stale listener stacking across scene reloads
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(() => OnInfoClicked?.Invoke());

            // Clear any previously spawned cards
            ClearCards();

            // Spawn and populate cards
            for (int i = 0; i < skills.Count; i++)
            {
                BloomSkillData data = allSkillData.Find(d => d.skillType == skills[i].skillType);
                if (data == null) continue;

                BloomCardPre card = Instantiate(cardPrefab, cardHolder);
                card.Populate(data);
                card.PlayAppearAnimation(delay: i * cardStagger);
                _spawnedCards.Add(card);
            }

            // Start countdown after all cards have had time to animate
            float totalAnimTime = skills.Count * cardStagger + 0.5f;
            StartCoroutine(CountdownRoutine(totalAnimTime));
        }

        /// <summary>
        /// Called by InfoPanel when it opens — pauses the countdown.
        /// </summary>
        public void PauseCountdown()
        {
            _infoPanelOpen = true;
        }

        /// <summary>
        /// Called by InfoPanel when it closes — resumes the countdown.
        /// </summary>
        public void ResumeCountdown()
        {
            _infoPanelOpen = false;
        }

        // ── Private ─────────────────────────────────────────────

        private IEnumerator CountdownRoutine(float initialDelay)
        {
            // Wait for cards to finish animating first
            yield return new WaitForSeconds(initialDelay);

            _countdownRunning = true;

            // Count elapsed time, pausing when info panel is open
            while (_countdownElapsed < autoHideDelay)
            {
                if (!_infoPanelOpen)
                    _countdownElapsed += Time.deltaTime;

                yield return null;
            }

            _countdownRunning = false;

            // Fade out
            yield return StartCoroutine(FadeOut());

            // Notify manager and deactivate
            OnPanelComplete?.Invoke();
            gameObject.SetActive(false);
            ClearCards();
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
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
}