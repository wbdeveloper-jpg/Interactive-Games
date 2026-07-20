using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class LoadingDotsAnimator : MonoBehaviour
{
    [Header("Dot Sprite")]
    [SerializeField] private Sprite dotSprite;
    [SerializeField]
    private Color[] dotColors =
    {
        new Color(0.93f, 0.38f, 0.42f, 1f),
        new Color(0.96f, 0.68f, 0.24f, 1f),
        new Color(0.93f, 0.45f, 0.65f, 1f)
    };

    [Header("Layout")]
    [Min(1f)][SerializeField] private float dotSize = 32f;
    [Min(0f)][SerializeField] private float spacing = 18f;

    [Header("Animation")]
    [Min(0f)][SerializeField] private float jumpHeight = 14f;
    [Min(0.05f)][SerializeField] private float jumpDuration = 0.28f;
    [Min(0f)][SerializeField] private float delayBetweenDots = 0.12f;
    [Min(0f)][SerializeField] private float pauseBetweenLoops = 0.18f;
    [Range(0.5f, 1.5f)][SerializeField] private float peakScale = 1.12f;
    [SerializeField] private Ease jumpEase = Ease.OutQuad;
    [SerializeField] private Ease fallEase = Ease.InQuad;
    [Tooltip("Keeps the loader moving when Time.timeScale is zero during scene loading.")]
    [SerializeField] private bool useUnscaledTime = true;

    private const int DotCount = 3;
    private readonly RectTransform[] dots = new RectTransform[DotCount];
    private Sequence animationSequence;

    private void Awake()
    {
        BuildDots();
    }

    private void OnEnable()
    {
        if (dots[0] == null)
            BuildDots();

        Play();
    }

    private void OnDisable()
    {
        StopAndReset();
    }

    private void OnDestroy()
    {
        animationSequence?.Kill();
    }

    private void OnValidate()
    {
        dotSize = Mathf.Max(1f, dotSize);
        spacing = Mathf.Max(0f, spacing);
        jumpHeight = Mathf.Max(0f, jumpHeight);
        jumpDuration = Mathf.Max(0.05f, jumpDuration);
        delayBetweenDots = Mathf.Max(0f, delayBetweenDots);
        pauseBetweenLoops = Mathf.Max(0f, pauseBetweenLoops);

        if (Application.isPlaying && dots[0] != null)
            ApplyDotAppearanceAndLayout();
    }

    public void Play()
    {
        StopAndReset();

        float singleJumpLength = jumpDuration * 2f;
        float loopLength = singleJumpLength + (delayBetweenDots * (DotCount - 1)) + pauseBetweenLoops;

        animationSequence = DOTween.Sequence()
            .SetUpdate(useUnscaledTime)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        for (int i = 0; i < DotCount; i++)
        {
            RectTransform dot = dots[i];
            float startTime = i * delayBetweenDots;

            animationSequence.Insert(
                startTime,
                dot.DOAnchorPosY(jumpHeight, jumpDuration).SetEase(jumpEase));

            animationSequence.Insert(
                startTime + jumpDuration,
                dot.DOAnchorPosY(0f, jumpDuration).SetEase(fallEase));

            animationSequence.Insert(
                startTime,
                dot.DOScale(peakScale, jumpDuration).SetEase(jumpEase));

            animationSequence.Insert(
                startTime + jumpDuration,
                dot.DOScale(1f, jumpDuration).SetEase(fallEase));
        }

        animationSequence.AppendInterval(Mathf.Max(0f, loopLength - animationSequence.Duration()));
        animationSequence.SetLoops(-1, LoopType.Restart);
    }

    public void StopAndReset()
    {
        animationSequence?.Kill();
        animationSequence = null;

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null)
                continue;

            dots[i].DOKill();
            dots[i].anchoredPosition = new Vector2(dots[i].anchoredPosition.x, 0f);
            dots[i].localScale = Vector3.one;
        }
    }

    private void BuildDots()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("LoadingDot_"))
                Destroy(child.gameObject);
        }

        for (int i = 0; i < DotCount; i++)
        {
            var dotObject = new GameObject(
                $"LoadingDot_{i + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            RectTransform dot = dotObject.GetComponent<RectTransform>();
            dot.SetParent(transform, false);
            dot.anchorMin = new Vector2(0.5f, 0.5f);
            dot.anchorMax = new Vector2(0.5f, 0.5f);
            dot.pivot = new Vector2(0.5f, 0.5f);

            Image image = dotObject.GetComponent<Image>();
            image.raycastTarget = false;

            dots[i] = dot;
        }

        ApplyDotAppearanceAndLayout();
    }

    private void ApplyDotAppearanceAndLayout()
    {
        float totalWidth = (dotSize * DotCount) + (spacing * (DotCount - 1));
        float firstX = -totalWidth * 0.5f + dotSize * 0.5f;

        for (int i = 0; i < DotCount; i++)
        {
            if (dots[i] == null)
                continue;

            dots[i].sizeDelta = Vector2.one * dotSize;
            dots[i].anchoredPosition = new Vector2(firstX + i * (dotSize + spacing), 0f);

            Image image = dots[i].GetComponent<Image>();
            image.sprite = dotSprite;
            image.color = GetDotColor(i);
            image.preserveAspect = true;
        }
    }

    private Color GetDotColor(int index)
    {
        if (dotColors == null || dotColors.Length == 0)
            return Color.white;

        return dotColors[Mathf.Min(index, dotColors.Length - 1)];
    }
}