/*
 * ============================================================
 * BloomCardPost.cs  —  MonoBehaviour on BloomCardPost Prefab
 * ============================================================
 * PURPOSE:
 *   Post-game result card. Minimalistic design — no medal sprites.
 *   Skill color drives: frame, icon, skill name, stars (outer + inner),
 *   score background, remark text. Remark background = skillColor at low alpha.
 *
 * PREFAB HIERARCHY:
 *   BloomCardPost  (this script + CanvasGroup)
 *     ├── Frame              (Image — gets skillColor)
 *     ├── Icon               (Image — gets skillColor as tint)
 *     ├── SkillName          (TextMeshProUGUI — gets skillColor)
 *     ├── ScoreBG            (Image — gets skillColor)
 *     │     └── ScoreText    (TextMeshProUGUI)
 *     ├── StarsContainer
 *     │     ├── StarOuter1   (Image — gets skillColor dimmed)
 *     │     │     └── StarInner1  (Image — gets skillColor full)
 *     │     ├── StarOuter2   (same)
 *     │     └── StarOuter3   (same)
 *     ├── RemarkBG           (Image — gets skillColor at low alpha)
 *     │     └── RemarkText   (TextMeshProUGUI — gets skillColor)
 *     └── [ParticleSystem]   (optional — assign in inspector)
 *
 * ANIMATION (DOTween — premium minimalist):
 *   Card: fade in + subtle scale from 0.92 → 1.0 (feels like lifting off page)
 *   Stars: each star inner draws in with a clean fade + scale, no bounce
 *   Remark: fades in last after stars, feels like a conclusion
 *
 * REMARK LOGIC (based on normalizedScore):
 *   >= goldThreshold   → "Excellent Work" / "Outstanding" / "Perfect Score"
 *   >= silverThreshold → "Well Done" / "Good Job" / "Solid Effort"
 *   bronze             → "Keep Going" / "Nice Try" / "Good Start"
 *   Randomly picks from the pool for each tier — variety prevents repetition.
 * ============================================================
 */

using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RewardSystem
{
    public class BloomCardPost : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image frame;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI skillNameText;
        [SerializeField] private Image scoreBG;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Stars — Outer (always visible, dimmed color)")]
        [SerializeField] private Image starOuter1;
        [SerializeField] private Image starOuter2;
        [SerializeField] private Image starOuter3;

        [Header("Stars — Inner (active stars, full color)")]
        [SerializeField] private Image starInner1;
        [SerializeField] private Image starInner2;
        [SerializeField] private Image starInner3;

        [Header("Remark")]
        [SerializeField] private Image remarkBG;
        [SerializeField] private TextMeshProUGUI remarkText;

        [Header("Particle Effect")]
        [Tooltip("Optional looped particle. Active for Silver and Gold only.")]
        [SerializeField] private ParticleSystem rewardParticle;

        [Header("Animation Timing")]
        [SerializeField] private float cardFadeDuration = 0.4f;
        [SerializeField] private float starInterval = 0.12f;  // gap between each star
        [SerializeField] private float starFadeDuration = 0.25f;
        [SerializeField] private float remarkDelay = 0.15f;  // after last star
        [SerializeField] private float remarkDuration = 0.35f;

        [Header("Color Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float outerStarAlpha = 0.25f;   // dimmed outer star alpha
        [Range(0f, 1f)]
        [SerializeField] private float remarkBGAlpha = 0.12f;   // very low alpha remark bg

        // Remark text pools per tier
        private static readonly string[] GoldRemarks = { "Outstanding!", "Perfect Score", "Excellent Work", "Mastered It" };
        private static readonly string[] SilverRemarks = { "Well Done", "Good Job", "Solid Effort", "Almost There" };
        private static readonly string[] BronzeRemarks = { "Keep Going", "Good Start", "Nice Try", "Keep Practicing" };

        private CanvasGroup _cg;
        private int _starCount;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        }

        // ── Public API ──────────────────────────────────────────

        /// <summary>Populate card with result data and apply skill color theming.</summary>
        public void Populate(
            BloomSkillData data,
            SkillResult result,
            Sprite bronzeSprite,  // kept for signature compatibility, unused in new design
            Sprite silverSprite,
            Sprite goldSprite)
        {
            Color c = data.skillColor;

            // Base content
            iconImage.sprite = data.icon;
            skillNameText.text = data.skillName;
            scoreText.text = $"{Mathf.RoundToInt(result.finalScore)}/{Mathf.RoundToInt(result.maxScore)}";

            // Apply skill color to all themed elements
            if (frame != null) frame.color = c;
            iconImage.color = c;
            skillNameText.color = c;
            if (scoreBG != null) scoreBG.color = c;

            // Outer stars — dimmed version of skill color
            Color outerColor = new Color(c.r, c.g, c.b, outerStarAlpha);
            if (starOuter1 != null) starOuter1.color = outerColor;
            if (starOuter2 != null) starOuter2.color = outerColor;
            if (starOuter3 != null) starOuter3.color = outerColor;

            // Inner stars — full skill color, start invisible
            Color innerColor = new Color(c.r, c.g, c.b, 0f);
            if (starInner1 != null) { starInner1.color = innerColor; starInner1.transform.localScale = Vector3.one * 0.5f; }
            if (starInner2 != null) { starInner2.color = innerColor; starInner2.transform.localScale = Vector3.one * 0.5f; }
            if (starInner3 != null) { starInner3.color = innerColor; starInner3.transform.localScale = Vector3.one * 0.5f; }

            // Remark
            string remark = GetRemark(result.medal);
            if (remarkText != null) { remarkText.text = remark; remarkText.color = c; }
            if (remarkBG != null) remarkBG.color = new Color(c.r, c.g, c.b, 0f); // starts transparent

            // Particle system kept in prefab but not used in this UI style
            // Disabled unconditionally — never shown regardless of medal
            if (rewardParticle != null)
                rewardParticle.gameObject.SetActive(false);

            _starCount = result.starCount;
            _cg.alpha = 0f;
            transform.localScale = Vector3.one * 0.92f; // slight scale start for lift effect
        }

        /// <summary>Play full reveal animation. Pass delay to stagger cards.</summary>
        public void PlayRevealAnimation(float delay = 0f)
        {
            StartCoroutine(RevealRoutine(delay));
        }

        // ── Private ─────────────────────────────────────────────

        private IEnumerator RevealRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            // Step 1 — Card lifts in: fade + subtle scale to 1.0
            _cg.DOFade(1f, cardFadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            yield return transform
                .DOScale(Vector3.one, cardFadeDuration)
                .SetEase(Ease.OutQuart)
                .SetUpdate(true)
                .WaitForCompletion();

            // Step 2 — Stars draw in one by one: fade + scale from 0.5 to 1.0
            Image[] inners = { starInner1, starInner2, starInner3 };
            for (int i = 0; i < _starCount; i++)
            {
                if (inners[i] == null) continue;
                Image star = inners[i];
                Color targetColor = star.color;
                targetColor.a = 1f;

                star.DOColor(targetColor, starFadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                yield return star.transform
                    .DOScale(Vector3.one, starFadeDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .WaitForCompletion();

                if (i < _starCount - 1)
                    yield return new WaitForSeconds(starInterval);
            }

            // Step 3 — Remark fades in after stars settle
            yield return new WaitForSeconds(remarkDelay);

            if (remarkBG != null)
            {
                Color c = remarkBG.color;
                remarkBG.DOColor(new Color(c.r, c.g, c.b, remarkBGAlpha), remarkDuration)
                    .SetEase(Ease.OutQuad).SetUpdate(true);
            }

            // Particle intentionally not played — kept in prefab for potential future use
        }

        private void OnDestroy()
        {
            if (rewardParticle != null && rewardParticle.isPlaying)
                rewardParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static string GetRemark(MedalTier medal)
        {
            string[] pool = medal switch
            {
                MedalTier.Gold => GoldRemarks,
                MedalTier.Silver => SilverRemarks,
                _ => BronzeRemarks,
            };
            return pool[Random.Range(0, pool.Length)];
        }
    }
}