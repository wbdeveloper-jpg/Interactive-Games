using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum SkyFallTrailEmissionMode
{
    Always,
    WhileMoving,
    Manual
}

[DisallowMultipleComponent]
public class SkyFallUiTrailEmitter : MonoBehaviour
{
    [Header("References")]
    public RectTransform source;
    public RectTransform emissionSpace;

    [Header("Emission")]
    public SkyFallTrailEmissionMode emissionMode = SkyFallTrailEmissionMode.Always;
    public bool playOnEnable = true;
    public bool useUnscaledTime = false;
    public float emissionRate = 18f;
    public float movementThreshold = 35f;
    public int maxParticles = 50;

    [Header("Particle Shape")]
    public float lifeTime = 0.45f;
    public float startSize = 16f;
    public float endSize = 2f;
    public Vector2 randomSpawnOffset = new Vector2(8f, 8f);
    public Vector2 driftMin = new Vector2(-12f, -8f);
    public Vector2 driftMax = new Vector2(12f, 18f);

    [Header("Color")]
    public Color startColor = new Color(1f, 1f, 1f, 0.75f);
    public Color endColor = new Color(1f, 1f, 1f, 0f);

    private sealed class TrailParticle
    {
        public RectTransform rect;
        public Image image;
        public float age;
        public float lifeTime;
        public Vector2 startPosition;
        public Vector2 velocity;
        public float startSize;
        public float endSize;
        public Color startColor;
        public Color endColor;
    }

    private readonly List<TrailParticle> particles = new List<TrailParticle>();
    private Vector3 lastWorldPosition;
    private bool hasLastWorldPosition;
    private bool isPlaying;
    private float emissionAccumulator;
    private static Sprite dotSprite;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        hasLastWorldPosition = false;
        emissionAccumulator = 0f;
        isPlaying = playOnEnable;
    }

    private void OnDisable()
    {
        ClearParticles();
    }

    private void Update()
    {
        CacheReferences();

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f)
            return;

        UpdateParticles(dt);

        if (!isPlaying || source == null || emissionSpace == null)
            return;

        float movementSpeed = GetMovementSpeed(dt);

        bool shouldEmit =
            emissionMode == SkyFallTrailEmissionMode.Always ||
            (emissionMode == SkyFallTrailEmissionMode.WhileMoving && movementSpeed >= movementThreshold);

        if (!shouldEmit)
            return;

        emissionAccumulator += emissionRate * dt;

        while (emissionAccumulator >= 1f)
        {
            emissionAccumulator -= 1f;
            SpawnParticle();
        }
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Stop(bool clearExistingParticles)
    {
        isPlaying = false;

        if (clearExistingParticles)
            ClearParticles();
    }

    public void SetEmissionSpace(RectTransform newEmissionSpace)
    {
        emissionSpace = newEmissionSpace;
    }

    public void SetSource(RectTransform newSource)
    {
        source = newSource;
        hasLastWorldPosition = false;
    }

    private void CacheReferences()
    {
        if (source == null)
            source = transform as RectTransform;

        if (emissionSpace == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                emissionSpace = canvas.transform as RectTransform;
        }
    }

    private float GetMovementSpeed(float dt)
    {
        if (source == null)
            return 0f;

        Vector3 current = source.position;

        if (!hasLastWorldPosition)
        {
            lastWorldPosition = current;
            hasLastWorldPosition = true;
            return 0f;
        }

        float speed = Vector3.Distance(lastWorldPosition, current) / Mathf.Max(dt, 0.0001f);
        lastWorldPosition = current;
        return speed;
    }

    private void SpawnParticle()
    {
        if (source == null || emissionSpace == null)
            return;

        if (particles.Count >= maxParticles)
        {
            DestroyParticle(particles[0]);
            particles.RemoveAt(0);
        }

        Vector2 localPosition;
        if (!TryGetSourceLocalPosition(out localPosition))
            return;

        localPosition += new Vector2(
            Random.Range(-randomSpawnOffset.x, randomSpawnOffset.x),
            Random.Range(-randomSpawnOffset.y, randomSpawnOffset.y)
        );

        GameObject particleObject = new GameObject("UiTrailParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        particleObject.transform.SetParent(emissionSpace, false);

        RectTransform particleRect = particleObject.GetComponent<RectTransform>();
        particleRect.anchorMin = new Vector2(0.5f, 0.5f);
        particleRect.anchorMax = new Vector2(0.5f, 0.5f);
        particleRect.pivot = new Vector2(0.5f, 0.5f);
        particleRect.anchoredPosition = localPosition;
        particleRect.sizeDelta = Vector2.one * startSize;

        Image image = particleObject.GetComponent<Image>();
        image.sprite = GetDotSprite();
        image.color = startColor;
        image.raycastTarget = false;
        image.preserveAspect = true;

        particles.Add(new TrailParticle
        {
            rect = particleRect,
            image = image,
            age = 0f,
            lifeTime = Mathf.Max(0.01f, lifeTime),
            startPosition = localPosition,
            velocity = new Vector2(Random.Range(driftMin.x, driftMax.x), Random.Range(driftMin.y, driftMax.y)),
            startSize = startSize,
            endSize = endSize,
            startColor = startColor,
            endColor = endColor
        });
    }

    private bool TryGetSourceLocalPosition(out Vector2 localPosition)
    {
        localPosition = Vector2.zero;

        Canvas canvas = emissionSpace.GetComponentInParent<Canvas>();
        Camera cameraToUse = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cameraToUse = canvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cameraToUse, source.position);

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            emissionSpace,
            screenPoint,
            cameraToUse,
            out localPosition
        );
    }

    private void UpdateParticles(float dt)
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            TrailParticle particle = particles[i];

            if (particle == null || particle.rect == null || particle.image == null)
            {
                particles.RemoveAt(i);
                continue;
            }

            particle.age += dt;
            float t = Mathf.Clamp01(particle.age / particle.lifeTime);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            particle.rect.anchoredPosition = particle.startPosition + particle.velocity * eased;
            particle.rect.sizeDelta = Vector2.one * Mathf.Lerp(particle.startSize, particle.endSize, eased);
            particle.image.color = Color.Lerp(particle.startColor, particle.endColor, eased);

            if (t >= 1f)
            {
                DestroyParticle(particle);
                particles.RemoveAt(i);
            }
        }
    }

    private void ClearParticles()
    {
        for (int i = particles.Count - 1; i >= 0; i--)
            DestroyParticle(particles[i]);

        particles.Clear();
    }

    private void DestroyParticle(TrailParticle particle)
    {
        if (particle == null || particle.rect == null)
            return;

        if (Application.isPlaying)
            Destroy(particle.rect.gameObject);
        else
            DestroyImmediate(particle.rect.gameObject);
    }

    private static Sprite GetDotSprite()
    {
        if (dotSprite != null)
            return dotSprite;

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "SkyFall_UiTrailDot_Runtime";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - distance / radius);
                alpha = Mathf.SmoothStep(0f, 1f, alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        dotSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        dotSprite.name = "SkyFall_UiTrailDotSprite_Runtime";
        return dotSprite;
    }
}
