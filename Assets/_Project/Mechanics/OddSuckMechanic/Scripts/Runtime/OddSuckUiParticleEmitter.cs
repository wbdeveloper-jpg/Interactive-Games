using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OddSuckMechanic
{
    /// <summary>
    /// Lightweight Canvas/UI particle emitter for the UFO tractor beam.
    /// This does not act as a UFO movement trail. It only emits while the beam/light is open.
    /// </summary>
    public class OddSuckUiParticleEmitter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform particleRoot;
        [SerializeField] private RectTransform beamTarget;

        [Header("Beam Emission")]
        [SerializeField] private bool playOnEnable = false;
        [SerializeField, Min(1)] private int poolSize = 32;
        [SerializeField, Min(1f)] private float particlesPerSecond = 22f;
        [SerializeField, Min(0.05f)] private float particleLifetime = 0.52f;
        [SerializeField, Range(0.05f, 1f)] private float startWidthFactor = 0.8f;
        [SerializeField, Range(0.05f, 1f)] private float endWidthFactor = 0.22f;
        [SerializeField, Range(0f, 1f)] private float upwardPullStrength = 0.9f;
        [SerializeField] private Vector2 sizeRange = new Vector2(8f, 18f);

        [Header("Look")]
        [SerializeField] private List<Color> particleColors = new List<Color>
        {
            new Color(0.55f, 1f, 1f, 0.86f),
            new Color(0.9f, 0.85f, 1f, 0.8f),
            new Color(1f, 0.96f, 0.45f, 0.74f)
        };

        private readonly List<Image> pool = new List<Image>();
        private Sprite runtimeParticleSprite;
        private float emitTimer;
        private bool emitting;
        private readonly Vector3[] beamWorldCorners = new Vector3[4];

        private RectTransform Root
        {
            get
            {
                if (particleRoot == null)
                {
                    particleRoot = transform as RectTransform;
                }

                return particleRoot;
            }
        }

        private void Awake()
        {
            if (particleRoot == null)
            {
                particleRoot = transform as RectTransform;
            }

            runtimeParticleSprite = CreateSoftCircleSprite();
            BuildPool();
            SetEmitting(playOnEnable);
        }

        private void OnDisable()
        {
            StopAllParticles();
        }

        private void Update()
        {
            if (!emitting || beamTarget == null || Root == null || !beamTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            float interval = 1f / Mathf.Max(1f, particlesPerSecond);
            emitTimer += Time.deltaTime;

            int safety = 0;
            while (emitTimer >= interval && safety < 4)
            {
                emitTimer -= interval;
                safety++;
                EmitParticle();
            }
        }

        public void SetFollowTarget(RectTransform target)
        {
            beamTarget = target;
        }

        public void SetBeamTarget(RectTransform target)
        {
            beamTarget = target;
        }

        public void SetDirection(float newDirection)
        {
            // Kept for backwards compatibility with older scenes. Beam particles do not need direction.
        }

        public void SetEmitting(bool active)
        {
            emitting = active;
            emitTimer = 0f;
        }

        public void Burst(int count)
        {
            if (beamTarget == null)
            {
                return;
            }

            int safeCount = Mathf.Clamp(count, 1, poolSize);
            for (int i = 0; i < safeCount; i++)
            {
                EmitParticle();
            }
        }

        public void StopAllParticles()
        {
            emitting = false;
            emitTimer = 0f;

            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] == null)
                {
                    continue;
                }

                pool[i].DOKill();
                pool[i].rectTransform.DOKill();
                pool[i].gameObject.SetActive(false);
            }
        }

        private void BuildPool()
        {
            if (Root == null)
            {
                return;
            }

            pool.Clear();
            int safePoolSize = Mathf.Max(1, poolSize);
            for (int i = 0; i < safePoolSize; i++)
            {
                GameObject particleGo = new GameObject("BeamEnergyParticle_" + i, typeof(RectTransform), typeof(Image));
                particleGo.transform.SetParent(Root, false);

                Image image = particleGo.GetComponent<Image>();
                image.sprite = runtimeParticleSprite;
                image.raycastTarget = false;
                image.color = Color.clear;

                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = Vector2.one * sizeRange.y;
                rect.localScale = Vector3.one;
                particleGo.SetActive(false);

                pool.Add(image);
            }
        }

        private Image GetFreeParticle()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeSelf)
                {
                    return pool[i];
                }
            }

            return pool.Count > 0 ? pool[0] : null;
        }

        private void EmitParticle()
        {
            Image particle = GetFreeParticle();
            if (particle == null || beamTarget == null || Root == null)
            {
                return;
            }

            if (!TryGetBeamLocalData(out Vector2 bottomCenter, out Vector2 topCenter, out float bottomWidth, out float topWidth))
            {
                return;
            }

            RectTransform rect = particle.rectTransform;
            particle.DOKill();
            rect.DOKill();

            float size = UnityEngine.Random.Range(sizeRange.x, sizeRange.y);
            Color color = particleColors != null && particleColors.Count > 0
                ? particleColors[UnityEngine.Random.Range(0, particleColors.Count)]
                : new Color(0.55f, 1f, 1f, 0.82f);

            float startWidth = Mathf.Max(4f, bottomWidth * startWidthFactor);
            float endWidth = Mathf.Max(2f, topWidth * endWidthFactor);
            float startX = bottomCenter.x + UnityEngine.Random.Range(-startWidth, startWidth) * 0.5f;
            float startY = Mathf.Lerp(bottomCenter.y, topCenter.y, UnityEngine.Random.Range(0.02f, 0.28f));
            Vector2 startPosition = new Vector2(startX, startY);

            float endX = Mathf.Lerp(startX, topCenter.x + UnityEngine.Random.Range(-endWidth, endWidth) * 0.5f, upwardPullStrength);
            float endY = Mathf.Lerp(bottomCenter.y, topCenter.y, UnityEngine.Random.Range(0.78f, 1.08f));
            Vector2 endPosition = new Vector2(endX, endY);

            rect.anchoredPosition = startPosition;
            rect.sizeDelta = new Vector2(size, size);
            rect.localScale = Vector3.one * UnityEngine.Random.Range(0.85f, 1.2f);
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            particle.color = color;
            particle.gameObject.SetActive(true);

            float life = UnityEngine.Random.Range(particleLifetime * 0.75f, particleLifetime * 1.15f);

            Sequence sequence = DOTween.Sequence();
            sequence.Join(rect.DOAnchorPos(endPosition, life).SetEase(Ease.OutCubic));
            sequence.Join(rect.DOScale(UnityEngine.Random.Range(0.08f, 0.22f), life).SetEase(Ease.InQuad));
            sequence.Join(particle.DOFade(0f, life).SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                if (particle != null)
                {
                    particle.gameObject.SetActive(false);
                }
            });
            sequence.SetLink(particle.gameObject);
        }

        private bool TryGetBeamLocalData(out Vector2 bottomCenter, out Vector2 topCenter, out float bottomWidth, out float topWidth)
        {
            bottomCenter = Vector2.zero;
            topCenter = Vector2.zero;
            bottomWidth = 0f;
            topWidth = 0f;

            if (beamTarget == null || Root == null)
            {
                return false;
            }

            beamTarget.GetWorldCorners(beamWorldCorners);

            Vector2 bottomLeft = Root.InverseTransformPoint(beamWorldCorners[0]);
            Vector2 topLeft = Root.InverseTransformPoint(beamWorldCorners[1]);
            Vector2 topRight = Root.InverseTransformPoint(beamWorldCorners[2]);
            Vector2 bottomRight = Root.InverseTransformPoint(beamWorldCorners[3]);

            bottomCenter = (bottomLeft + bottomRight) * 0.5f;
            topCenter = (topLeft + topRight) * 0.5f;
            bottomWidth = Vector2.Distance(bottomLeft, bottomRight);
            topWidth = Vector2.Distance(topLeft, topRight);

            return bottomWidth > 0.1f && topWidth > 0.1f;
        }

        private static Sprite CreateSoftCircleSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "OddSuck_Runtime_BeamParticle";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(1f - distance / radius);
                    alpha = alpha * alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
