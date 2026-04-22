/*
 * ============================================================
 * BloomCardPost.cs  —  MonoBehaviour on BloomCardPost Prefab
 * ============================================================
 * PURPOSE:
 *   Controls a single Bloom skill result card in the Post-Game panel.
 *   Shows icon, skill name, score text, medal background, and star rating.
 *   Animates in with a reveal sequence: card pop → medal → stars one by one.
 *
 * PREFAB HIERARCHY:
 *   BloomCardPost (this script)
 *     ├── Background          (Image)  ← swapped to bronze/silver/gold sprite
 *     ├── Icon                (Image)
 *     ├── SkillName           (TextMeshProUGUI)
 *     ├── ScoreText           (TextMeshProUGUI)  e.g. "80/100"
 *     └── StarsContainer
 *           ├── Star1
 *           │     ├── StarOuter   (Image — always visible, dimmed)
 *           │     └── StarInner   (Image — glowing, shown based on score)
 *           ├── Star2  (same structure)
 *           └── Star3  (same structure)
 *
 * SETUP:
 *   Assign all references in inspector. Medal sprites assigned on RewardManager.
 *   Call Populate() then PlayRevealAnimation() from PostGamePanel.
 * ============================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RewardSystem
{
    public class BloomCardPost : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image           background;
        [SerializeField] private Image           iconImage;
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Stars — Inner (glowing active stars)")]
        [SerializeField] private GameObject starInner1;
        [SerializeField] private GameObject starInner2;
        [SerializeField] private GameObject starInner3;

        [Header("Animation")]
        [SerializeField] private AnimationCurve appearCurve     = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float          cardDuration     = 0.45f;
        [SerializeField] private float          starDelay        = 0.15f; // delay between each star pop
        [SerializeField] private float          starPopDuration  = 0.25f;

        // ── Public API ──────────────────────────────────────────

        /// <summary>
        /// Populate the card with computed result data and medal sprites.
        /// Call before PlayRevealAnimation.
        /// </summary>
        public void Populate(
            BloomSkillData data,
            SkillResult    result,
            Sprite         bronzeSprite,
            Sprite         silverSprite,
            Sprite         goldSprite)
        {
            // Base data
            iconImage.sprite   = data.icon;
            skillNameText.text = data.skillName;
            scoreText.text     = $"{Mathf.RoundToInt(result.finalScore)}/{Mathf.RoundToInt(data == null ? 100 : result.finalScore / result.normalizedScore)}";

            // Medal background
            background.sprite = result.medal switch
            {
                MedalTier.Gold   => goldSprite,
                MedalTier.Silver => silverSprite,
                _                => bronzeSprite,
            };

            // Hide all inner stars — reveal during animation
            starInner1.SetActive(false);
            starInner2.SetActive(false);
            starInner3.SetActive(false);

            // Store star count for reveal routine
            _starCount = result.starCount;

            // Start invisible
            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Play the full reveal: card scales in, then stars pop one by one.
        /// Pass delay to stagger multiple cards appearing sequentially.
        /// </summary>
        public void PlayRevealAnimation(float delay = 0f)
        {
            StartCoroutine(RevealRoutine(delay));
        }

        // ── Private ─────────────────────────────────────────────

        private int _starCount;

        private IEnumerator RevealRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            // Step 1 — Card scales in
            yield return StartCoroutine(ScaleIn(transform, cardDuration));

            // Step 2 — Pop stars one by one up to starCount
            GameObject[] inners = { starInner1, starInner2, starInner3 };
            for (int i = 0; i < _starCount; i++)
            {
                yield return new WaitForSeconds(starDelay);
                yield return StartCoroutine(PopStar(inners[i]));
            }
        }

        private IEnumerator ScaleIn(Transform t, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = appearCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                t.localScale = Vector3.one * scale;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private IEnumerator PopStar(GameObject star)
        {
            star.SetActive(true);
            star.transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < starPopDuration)
            {
                elapsed += Time.deltaTime;
                float scale = appearCurve.Evaluate(Mathf.Clamp01(elapsed / starPopDuration));
                star.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            star.transform.localScale = Vector3.one;
        }
    }
}
