/*
 * ============================================================
 * BloomCardPre.cs  —  MonoBehaviour on BloomCardPre Prefab
 * ============================================================
 * PURPOSE:
 *   Controls a single Bloom skill card shown in the Pre-Game panel.
 *   Displays the skill's icon and name, then plays a pop-in animation.
 *
 * PREFAB HIERARCHY:
 *   BloomCardPre (this script)
 *     ├── Icon        (UnityEngine.UI.Image)
 *     └── SkillName   (TMPro.TextMeshProUGUI)
 *
 * SETUP:
 *   • Assign Icon and SkillName references in inspector.
 *   • Prefab starts at scale (0,0,0) — animation scales it to (1,1,1).
 *   • Call Populate() then PlayAppearAnimation() from PreGamePanel.
 *
 * ANIMATION:
 *   Simple LeanTween-free coroutine scale punch.
 *   No external tween library required — uses AnimationCurve from inspector.
 * ============================================================
 */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RewardSystem
{
    public class BloomCardPre : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image            iconImage;
        [SerializeField] private TextMeshProUGUI  skillNameText;

        [Header("Animation")]
        [SerializeField] private AnimationCurve   appearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float            appearDuration = 0.4f;

        // ── Public API ──────────────────────────────────────────

        /// <summary>Fill the card with skill data. Call before PlayAppearAnimation.</summary>
        public void Populate(BloomSkillData data)
        {
            iconImage.sprite    = data.icon;
            skillNameText.text  = data.skillName;
            // Start invisible/small — animation will reveal
            transform.localScale = Vector3.zero;
        }

        /// <summary>
        /// Animate this card popping into view.
        /// Pass a delay (seconds) to stagger multiple cards.
        /// </summary>
        public void PlayAppearAnimation(float delay = 0f)
        {
            StartCoroutine(AppearRoutine(delay));
        }

        // ── Private ─────────────────────────────────────────────

        private IEnumerator AppearRoutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            while (elapsed < appearDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / appearDuration);
                float scale = appearCurve.Evaluate(t);
                transform.localScale = Vector3.one * scale;
                yield return null;
            }

            transform.localScale = Vector3.one; // ensure exact final value
        }
    }
}
