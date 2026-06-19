using DG.Tweening;
using UnityEngine;

namespace OddSuckMechanic
{
    public enum OddSuckPullVisualStyle
    {
        SuckingBeam = 0,
        RopePull = 1
    }

    /// <summary>
    /// Visual-only controller for the pull effect.
    /// Gameplay still stays inside OddSuckManager.
    /// Use SuckingBeam for UFO/vacuum/portal themes.
    /// Use RopePull for hot-air-balloon/helicopter/crane/hook themes.
    /// </summary>
    public class OddSuckPullVisualController : MonoBehaviour
    {
        [Header("Beam Visual - Sucking Mode")]
        [SerializeField] private RectTransform beamTransform;
        [SerializeField] private CanvasGroup beamCanvasGroup;
        [SerializeField] private OddSuckUiParticleEmitter beamParticleEmitter;

        [Header("Rope Visual - Rope Pull Mode")]
        [Tooltip("Assign a thin UI Image RectTransform. Pivot should be top-center: X 0.5, Y 1.")]
        [SerializeField] private RectTransform ropeTransform;
        [SerializeField] private CanvasGroup ropeCanvasGroup;
        [Tooltip("If no rope is assigned, Rope Pull can reuse the old beam object as a rope. Good for quick copied-scene testing; for production use a real thin rope image.")]
        [SerializeField] private bool useBeamObjectAsRopeIfNoRopeAssigned = true;

        [Header("Rope Size Control")]
        [Tooltip("Recommended ON. The rope uses the RectTransform height you set in the scene as its full visible length, then animates from/to that size.")]
        [SerializeField] private bool useDesignedRopeHeight = true;
        [Tooltip("Recommended ON. The rope keeps the RectTransform width you set in the scene.")]
        [SerializeField] private bool useDesignedRopeWidth = true;
        [Tooltip("Used only when Use Designed Rope Width is OFF, or as fallback when width is missing.")]
        [SerializeField, Min(2f)] private float ropeWidth = 10f;
        [Tooltip("Fallback minimum height if the assigned rope RectTransform has a very small height.")]
        [SerializeField, Min(10f)] private float minimumVisibleRopeHeight = 160f;
        [Tooltip("Height when the rope is fully retracted. Usually 0.")]
        [SerializeField, Min(0f)] private float retractedRopeHeight = 0f;
        [Tooltip("Recommended ON for balloon/helicopter UI. Rope stays where you placed it under the collector and only height changes.")]
        [SerializeField] private bool keepDesignedLocalPlacement = true;

        [Header("Rope Timing")]
        [Tooltip("ON = rope theme uses its own timing instead of the UFO beam/item pull timing. Recommended ON for balloon/helicopter/crane themes.")]
        [SerializeField] private bool useIndependentRopeTiming = true;
        [Tooltip("How long the rope takes to drop/extend before the item starts moving.")]
        [SerializeField, Min(0.01f)] private float ropeDropDuration = 0.3f;
        [Tooltip("How long the item takes to travel upward in Rope Pull mode. Higher value = slower rope pulling.")]
        [SerializeField, Min(0.05f)] private float ropeItemPullDuration = 0.9f;
        [Tooltip("How long the rope fades/hides after the pull is finished.")]
        [SerializeField, Min(0.01f)] private float ropeHideDuration = 0.15f;

        [Header("Advanced Rope Options")]
        [SerializeField] private bool rotateRopeTowardTarget;
        [SerializeField] private bool hideBeamWhenUsingRope = true;
        [SerializeField] private Vector2 collectorAnchorOffset;
        [SerializeField] private Vector2 targetAttachOffset;
        [SerializeField, Min(0.01f)] private float ropeFadeDuration = 0.12f;

        private Tween visualTween;
        private Tween ropeFollowTween;
        private Tween beamTween;

        private bool ropeDesignCached;
        private Vector2 designedRopeSize;
        private Vector3 designedRopeLocalPosition;
        private Quaternion designedRopeLocalRotation;
        private Vector3 designedRopeLocalScale;

        public OddSuckPullVisualStyle CurrentStyle { get; private set; }

        public float GetPullStartDuration(OddSuckPullVisualStyle style, float fallbackDuration)
        {
            if (style == OddSuckPullVisualStyle.RopePull && useIndependentRopeTiming)
            {
                return Mathf.Max(0.01f, ropeDropDuration);
            }

            return Mathf.Max(0.01f, fallbackDuration);
        }

        public float GetItemPullDuration(OddSuckPullVisualStyle style, float fallbackDuration)
        {
            if (style == OddSuckPullVisualStyle.RopePull && useIndependentRopeTiming)
            {
                return Mathf.Max(0.05f, ropeItemPullDuration);
            }

            return Mathf.Max(0.01f, fallbackDuration);
        }

        public float GetPullHideDuration(OddSuckPullVisualStyle style, float fallbackDuration)
        {
            if (style == OddSuckPullVisualStyle.RopePull && useIndependentRopeTiming)
            {
                return Mathf.Max(0.01f, ropeHideDuration);
            }

            return Mathf.Max(0.01f, fallbackDuration);
        }

        public void ConfigureFallbacks(RectTransform fallbackBeam, CanvasGroup fallbackBeamCanvasGroup, OddSuckUiParticleEmitter fallbackBeamParticles)
        {
            if (beamTransform == null)
            {
                beamTransform = fallbackBeam;
            }

            if (beamCanvasGroup == null)
            {
                beamCanvasGroup = fallbackBeamCanvasGroup;
            }

            if (beamParticleEmitter == null)
            {
                beamParticleEmitter = fallbackBeamParticles;
            }

            if (ropeTransform == null && useBeamObjectAsRopeIfNoRopeAssigned && fallbackBeam != null)
            {
                ropeTransform = fallbackBeam;
                ropeCanvasGroup = fallbackBeamCanvasGroup;
                useDesignedRopeHeight = false;
                keepDesignedLocalPlacement = false;
            }

            CacheRopeDesignSettings();

            if (beamParticleEmitter != null && beamTransform != null)
            {
                beamParticleEmitter.SetBeamTarget(beamTransform);
                beamParticleEmitter.StopAllParticles();
            }

            HideRope(true);
        }

        public void SetIdleGuide(OddSuckPullVisualStyle style, bool visible, bool instant, float duration)
        {
            CurrentStyle = style;

            if (style == OddSuckPullVisualStyle.RopePull)
            {
                SetBeamParticles(false);

                if (hideBeamWhenUsingRope)
                {
                    SetBeamVisible(false, true, duration);
                }

                HideRope(true);
                return;
            }

            HideRope(true);
            SetBeamVisible(visible, instant, duration);
        }

        public void PlayPullStart(OddSuckPullVisualStyle style, RectTransform item, RectTransform collector, bool guideAlreadyVisible, float duration)
        {
            CurrentStyle = style;
            visualTween?.Kill();
            ropeFollowTween?.Kill();

            if (style == OddSuckPullVisualStyle.RopePull)
            {
                SetBeamParticles(false);

                if (hideBeamWhenUsingRope)
                {
                    SetBeamVisible(false, true, duration);
                }

                PlayRopeDrop(item, collector, duration);
                return;
            }

            HideRope(true);

            if (!guideAlreadyVisible)
            {
                SetBeamVisible(true, false, duration);
            }

            SetBeamParticles(true);
        }

        public void PlayPullActive(OddSuckPullVisualStyle style, RectTransform item, RectTransform collector, float duration)
        {
            ropeFollowTween?.Kill();

            if (style != OddSuckPullVisualStyle.RopePull || ropeTransform == null || item == null || collector == null)
            {
                return;
            }

            if (useDesignedRopeHeight)
            {
                float startHeight = GetDesignedRopeHeight();
                float endHeight = retractedRopeHeight;

                ropeFollowTween = DOVirtual.Float(startHeight, endHeight, Mathf.Max(0.01f, duration), SetDesignedRopeHeight)
                    .SetEase(Ease.InQuad)
                    .SetLink(gameObject);
                return;
            }

            ropeFollowTween = DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, duration), _ =>
            {
                UpdateRopeBetween(GetCollectorAnchor(collector), GetTargetAttachPoint(item), 1f);
            })
            .SetEase(Ease.Linear)
            .SetLink(gameObject);
        }

        public void StopActiveEffect(OddSuckPullVisualStyle style)
        {
            SetBeamParticles(false);
        }

        public void HidePullVisual(OddSuckPullVisualStyle style, bool instant, float duration)
        {
            StopActiveEffect(style);

            if (style == OddSuckPullVisualStyle.RopePull)
            {
                HideRope(instant);
                return;
            }

            SetBeamVisible(false, instant, duration);
        }

        public void HideAll(bool instant, float duration)
        {
            SetBeamParticles(false);
            SetBeamVisible(false, instant, duration);
            HideRope(true);
        }

        private void PlayRopeDrop(RectTransform item, RectTransform collector, float duration)
        {
            if (ropeTransform == null || item == null || collector == null)
            {
                return;
            }

            CacheRopeDesignSettings();

            ropeTransform.gameObject.SetActive(true);
            if (ropeCanvasGroup != null)
            {
                ropeCanvasGroup.alpha = 1f;
            }

            if (useDesignedRopeHeight)
            {
                RestoreRopeDesignedPlacement();
                SetDesignedRopeHeight(retractedRopeHeight);

                float targetHeight = GetDesignedRopeHeight();
                visualTween = DOVirtual.Float(retractedRopeHeight, targetHeight, Mathf.Max(0.01f, duration), SetDesignedRopeHeight)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
                return;
            }

            Vector3 start = GetCollectorAnchor(collector);
            Vector3 end = GetTargetAttachPoint(item);
            UpdateRopeBetween(start, end, 0f);

            visualTween = DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, duration), progress =>
            {
                UpdateRopeBetween(start, end, progress);
            })
            .SetEase(Ease.OutQuad)
            .SetLink(gameObject);
        }

        private Vector3 GetCollectorAnchor(RectTransform collector)
        {
            return collector.TransformPoint(collectorAnchorOffset);
        }

        private Vector3 GetTargetAttachPoint(RectTransform item)
        {
            return item.TransformPoint(targetAttachOffset);
        }

        private void UpdateRopeBetween(Vector3 collectorPosition, Vector3 itemPosition, float progress)
        {
            if (ropeTransform == null)
            {
                return;
            }

            progress = Mathf.Clamp01(progress);
            Vector3 visibleEnd = Vector3.Lerp(collectorPosition, itemPosition, progress);
            Vector3 middle = (collectorPosition + visibleEnd) * 0.5f;
            float length = Vector3.Distance(collectorPosition, visibleEnd);
            length = Mathf.Max(length, progress > 0.01f ? minimumVisibleRopeHeight : retractedRopeHeight);

            ropeTransform.position = middle;
            ropeTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetDesignedRopeWidth());
            ropeTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, length));

            if (rotateRopeTowardTarget && length > 0.01f)
            {
                Vector3 direction = visibleEnd - collectorPosition;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                ropeTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                RestoreRopeDesignedRotationOnly();
            }
        }

        private void CacheRopeDesignSettings()
        {
            if (ropeTransform == null || ropeDesignCached)
            {
                return;
            }

            designedRopeSize = ropeTransform.rect.size;
            if (designedRopeSize.x <= 0.01f)
            {
                designedRopeSize.x = ropeTransform.sizeDelta.x;
            }

            if (designedRopeSize.y <= 0.01f)
            {
                designedRopeSize.y = ropeTransform.sizeDelta.y;
            }

            designedRopeLocalPosition = ropeTransform.localPosition;
            designedRopeLocalRotation = ropeTransform.localRotation;
            designedRopeLocalScale = ropeTransform.localScale;
            ropeDesignCached = true;
        }

        private float GetDesignedRopeHeight()
        {
            CacheRopeDesignSettings();

            float height = designedRopeSize.y;
            if (height <= 0.01f && ropeTransform != null)
            {
                height = ropeTransform.rect.height;
            }

            if (height <= 0.01f && ropeTransform != null)
            {
                height = ropeTransform.sizeDelta.y;
            }

            return Mathf.Max(minimumVisibleRopeHeight, height);
        }

        private float GetDesignedRopeWidth()
        {
            CacheRopeDesignSettings();

            if (!useDesignedRopeWidth)
            {
                return Mathf.Max(2f, ropeWidth);
            }

            float width = designedRopeSize.x;
            if (width <= 0.01f && ropeTransform != null)
            {
                width = ropeTransform.rect.width;
            }

            if (width <= 0.01f && ropeTransform != null)
            {
                width = ropeTransform.sizeDelta.x;
            }

            return Mathf.Max(2f, width);
        }

        private void SetDesignedRopeHeight(float height)
        {
            if (ropeTransform == null)
            {
                return;
            }

            RestoreRopeDesignedPlacement();
            ropeTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, GetDesignedRopeWidth());
            ropeTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(0f, height));
        }

        private void RestoreRopeDesignedPlacement()
        {
            if (ropeTransform == null || !keepDesignedLocalPlacement)
            {
                return;
            }

            CacheRopeDesignSettings();
            ropeTransform.localPosition = designedRopeLocalPosition;
            ropeTransform.localRotation = designedRopeLocalRotation;
            ropeTransform.localScale = designedRopeLocalScale;
        }

        private void RestoreRopeDesignedRotationOnly()
        {
            if (ropeTransform == null || !keepDesignedLocalPlacement)
            {
                return;
            }

            CacheRopeDesignSettings();
            ropeTransform.localRotation = designedRopeLocalRotation;
            ropeTransform.localScale = designedRopeLocalScale;
        }

        private void HideRope(bool instant)
        {
            ropeFollowTween?.Kill();
            visualTween?.Kill();

            if (ropeTransform == null)
            {
                return;
            }

            if (instant || ropeCanvasGroup == null)
            {
                if (ropeCanvasGroup != null)
                {
                    ropeCanvasGroup.alpha = 0f;
                }

                SetDesignedRopeHeight(GetDesignedRopeHeight());
                ropeTransform.gameObject.SetActive(false);
                return;
            }

            float hideDuration = useIndependentRopeTiming ? ropeHideDuration : ropeFadeDuration;
            visualTween = DOTween.To(() => ropeCanvasGroup.alpha, value => ropeCanvasGroup.alpha = value, 0f, Mathf.Max(0.01f, hideDuration))
                .OnComplete(() =>
                {
                    SetDesignedRopeHeight(GetDesignedRopeHeight());
                    ropeTransform.gameObject.SetActive(false);
                })
                .SetLink(gameObject);
        }

        private void SetBeamVisible(bool visible, bool instant, float duration)
        {
            if (beamTransform == null)
            {
                return;
            }

            beamTween?.Kill();

            if (instant)
            {
                beamTransform.gameObject.SetActive(visible);
                beamTransform.localScale = visible ? Vector3.one : new Vector3(1f, 0f, 1f);
                if (beamCanvasGroup != null)
                {
                    beamCanvasGroup.alpha = visible ? 1f : 0f;
                }
                return;
            }

            if (visible)
            {
                beamTransform.gameObject.SetActive(true);
                beamTransform.localScale = new Vector3(1f, 0f, 1f);
                if (beamCanvasGroup != null)
                {
                    beamCanvasGroup.alpha = 0f;
                }

                Sequence showSequence = DOTween.Sequence();
                showSequence.Join(beamTransform.DOScaleY(1f, duration).SetEase(Ease.OutBack));
                if (beamCanvasGroup != null)
                {
                    showSequence.Join(DOTween.To(() => beamCanvasGroup.alpha, value => beamCanvasGroup.alpha = value, 1f, duration));
                }

                beamTween = showSequence.SetLink(gameObject);
                return;
            }

            Sequence hideSequence = DOTween.Sequence();
            hideSequence.Join(beamTransform.DOScaleY(0f, duration).SetEase(Ease.InBack));
            if (beamCanvasGroup != null)
            {
                hideSequence.Join(DOTween.To(() => beamCanvasGroup.alpha, value => beamCanvasGroup.alpha = value, 0f, duration));
            }

            hideSequence.OnComplete(() => beamTransform.gameObject.SetActive(false));
            beamTween = hideSequence.SetLink(gameObject);
        }

        private void SetBeamParticles(bool visible)
        {
            if (beamParticleEmitter == null)
            {
                return;
            }

            beamParticleEmitter.SetBeamTarget(beamTransform);
            beamParticleEmitter.SetEmitting(visible);

            if (visible)
            {
                beamParticleEmitter.Burst(6);
            }
            else
            {
                beamParticleEmitter.StopAllParticles();
            }
        }
    }
}
