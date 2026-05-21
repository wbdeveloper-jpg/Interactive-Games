/*
 * ============================================================
 * BloomCardPre.cs  —  MonoBehaviour on BloomCardPre Prefab
 * ============================================================
 * PURPOSE:
 *   Pre-game skill card. Minimalistic Google-style design.
 *   Skill color drives: card frame/background, icon tint, name text color.
 *
 * PREFAB HIERARCHY:
 *   BloomCardPre  (this script + CanvasGroup)
 *     ├── Frame         (Image — colored outline or bg, gets skillColor)
 *     ├── Icon          (Image — gets skillColor as tint)
 *     └── SkillName     (TextMeshProUGUI — gets skillColor)
 *
 * ANIMATION (DOTween — premium minimalist):
 *   Cards slide up from slightly below + fade in, staggered per card.
 *   Clean, subtle, Google Material-style entrance.
 *
 * SETUP:
 *   Assign Frame, Icon, SkillName in inspector.
 *   PreGamePanel calls Populate() then PlayAppearAnimation(delay).
 * ============================================================
 */

using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RewardSystem
{
    public class BloomCardPre : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image frame;
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI skillNameText;

        [Header("Animation")]
        [SerializeField] private float appearDuration = 0.45f;
        [SerializeField] private float slideDistance = 30f;   // pixels to slide up from

        private CanvasGroup _cg;
        private RectTransform _rt;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            _rt = GetComponent<RectTransform>();
        }

        // ── Public API ──────────────────────────────────────────

        /// <summary>Fill card data and apply skill color to all elements.</summary>
        public void Populate(BloomSkillData data)
        {
            skillNameText.text = data.skillName;
            iconImage.sprite = data.icon;

            // Apply skill color to all themed elements
            Color c = data.skillColor;
            if (frame != null) frame.color = c;
            iconImage.color = c;
            skillNameText.color = c;

            // Start invisible + slightly scaled down
            // DO NOT touch anchoredPosition — HorizontalLayoutGroup owns it
            // Modifying anchoredPosition on layout children causes them to snap to corner
            _cg.alpha = 0f;
            transform.localScale = Vector3.one * 0.85f;
        }

        /// <summary>
        /// Fade in + subtle scale up. Layout group position is never touched.
        /// Pass delay to stagger multiple cards.
        /// </summary>
        public void PlayAppearAnimation(float delay = 0f)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);
            seq.SetDelay(delay);

            // Fade in and scale to full size simultaneously — layout group stays in control
            seq.Join(_cg.DOFade(1f, appearDuration).SetEase(Ease.OutQuad));
            seq.Join(transform.DOScale(Vector3.one, appearDuration).SetEase(Ease.OutBack));
        }
    }
}