using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkyFallFallingItem : MonoBehaviour
{
    [Header("Core References")]
    public RectTransform rectTransform;
    public RectTransform catchHitBox;
    public RectTransform visualRoot;
    public RectTransform outerCard;
    public RectTransform innerCard;
    public Image outerCardImage;
    public Image innerCardImage;
    public Image iconImage;
    public TMP_Text labelText;
    public CanvasGroup canvasGroup;
    public SkyFallUiTrailEmitter trailEmitter;

    [Header("Adaptive Layout")]
    public bool autoResizeCard = true;
    public Vector2 smallTileSize = new Vector2(120f, 120f);
    public Vector2 mediumTileSize = new Vector2(170f, 120f);
    public Vector2 wideCardSize = new Vector2(340f, 120f);
    public Vector2 twoLineCardSize = new Vector2(380f, 150f);
    public Vector2 catchHitBoxSize = new Vector2(130f, 120f);
    public bool allowTwoLineEquation = true;
    public bool useTextAutoSize = true;
    public float minFontSize = 28f;
    public float maxFontSize = 50f;

    [Header("Drop Scale Animation")]
    public bool enableScaleGrowOnDrop = true;
    public float spawnStartScale = 0.52f;
    public float fullScale = 1f;
    [Range(0.05f, 1f)] public float growProgressPortion = 0.38f;
    public AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public SkyFallDropData Data { get; private set; }

    public RectTransform RectTransform
    {
        get
        {
            Cache();
            return rectTransform;
        }
    }

    public RectTransform CatchRect
    {
        get
        {
            Cache();
            return catchHitBox != null ? catchHitBox : rectTransform;
        }
    }

    private float fallProgress;
    private bool isResolving;

    private void Awake()
    {
        Cache();
    }

    public void Setup(SkyFallDropData data, RectTransform trailEmissionSpace)
    {
        Cache();

        Data = data;
        isResolving = false;
        fallProgress = 0f;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;

        if (visualRoot != null)
            visualRoot.localScale = Vector3.one * (enableScaleGrowOnDrop ? spawnStartScale : fullScale);

        if (iconImage != null)
        {
            iconImage.raycastTarget = false;

            if (data.sprite != null)
            {
                iconImage.sprite = data.sprite;
                iconImage.enabled = true;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        if (labelText != null)
        {
            labelText.raycastTarget = false;
            labelText.text = FormatTextForLayout(data.displayText);
            labelText.gameObject.SetActive(!string.IsNullOrEmpty(data.displayText));
            labelText.enableAutoSizing = useTextAutoSize;
            labelText.fontSizeMin = minFontSize;
            labelText.fontSizeMax = maxFontSize;
        }

        ApplyAdaptiveLayout(data.displayText);

        if (trailEmitter != null)
        {
            trailEmitter.SetSource(visualRoot != null ? visualRoot : rectTransform);
            trailEmitter.SetEmissionSpace(trailEmissionSpace);
            trailEmitter.Play();
        }
    }

    public bool Tick(float fallSpeed, float deltaTime, float bottomLimit, float estimatedReachTime)
    {
        if (isResolving)
            return false;

        Vector2 position = rectTransform.anchoredPosition;
        position.y -= fallSpeed * deltaTime;
        rectTransform.anchoredPosition = position;

        if (estimatedReachTime > 0.01f)
            fallProgress += deltaTime / estimatedReachTime;

        UpdateScaleGrow();

        return position.y < bottomLimit;
    }

    public void StopTrail()
    {
        if (trailEmitter != null)
            trailEmitter.Stop(false);
    }

    public IEnumerator AnimateCorrectAbsorb(RectTransform basket, Vector2 basketOffset, float duration, float endScale)
    {
        isResolving = true;
        StopTrail();

        if (basket == null || rectTransform == null)
            yield break;

        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = ConvertWorldToParentAnchoredPosition(basket, basketOffset);

        float startScale = visualRoot != null ? visualRoot.localScale.x : rectTransform.localScale.x;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        duration = Mathf.Max(0.01f, duration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float moveEase = EaseInBackSoft(t);
            float fadeEase = Mathf.SmoothStep(0f, 1f, t);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, moveEase);

            float scale = Mathf.Lerp(startScale, endScale, fadeEase);
            SetVisualScale(scale);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, fadeEase);

            yield return null;
        }
    }

    public IEnumerator AnimateWrongReject(float duration, float moveUp, float endScale)
    {
        isResolving = true;
        StopTrail();

        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, moveUp);

        float startScale = visualRoot != null ? visualRoot.localScale.x : rectTransform.localScale.x;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;

        duration = Mathf.Max(0.01f, duration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = EaseOutQuad(t);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            SetVisualScale(Mathf.Lerp(startScale, endScale, eased));

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);

            yield return null;
        }
    }

    private void Cache()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private string FormatTextForLayout(string rawText)
    {
        if (string.IsNullOrEmpty(rawText))
            return string.Empty;

        if (!allowTwoLineEquation || rawText.Length < 12)
            return rawText;

        string[] parts = rawText.Split(' ');

        if (parts.Length < 5)
            return rawText;

        int splitIndex = Mathf.Clamp(parts.Length / 2, 2, parts.Length - 2);
        string first = string.Empty;
        string second = string.Empty;

        for (int i = 0; i < parts.Length; i++)
        {
            if (i < splitIndex)
                first += (first.Length > 0 ? " " : "") + parts[i];
            else
                second += (second.Length > 0 ? " " : "") + parts[i];
        }

        return first + "\n" + second;
    }

    private void ApplyAdaptiveLayout(string rawText)
    {
        if (!autoResizeCard)
            return;

        string text = rawText ?? string.Empty;
        bool hasOperation = text.Contains("+") || text.Contains("-") || text.Contains("×") || text.Contains("÷") || text.Contains("*") || text.Contains("/");
        bool longText = text.Length >= 12;
        bool multiline = labelText != null && labelText.text.Contains("\n");

        Vector2 size;

        if (multiline || (allowTwoLineEquation && longText && hasOperation))
            size = twoLineCardSize;
        else if (hasOperation)
            size = wideCardSize;
        else if (text.Length >= 3)
            size = mediumTileSize;
        else
            size = smallTileSize;

        if (rectTransform != null)
            rectTransform.sizeDelta = size;

        if (outerCard != null)
            outerCard.sizeDelta = size;

        if (innerCard != null)
            innerCard.sizeDelta = new Vector2(Mathf.Max(10f, size.x - 18f), Mathf.Max(10f, size.y - 18f));

        if (catchHitBox != null)
            catchHitBox.sizeDelta = catchHitBoxSize;
    }

    private void UpdateScaleGrow()
    {
        if (!enableScaleGrowOnDrop || visualRoot == null)
            return;

        float portion = Mathf.Max(0.01f, growProgressPortion);
        float t = Mathf.Clamp01(fallProgress / portion);
        float eased = growCurve != null ? growCurve.Evaluate(t) : t;
        SetVisualScale(Mathf.Lerp(spawnStartScale, fullScale, eased));
    }

    private void SetVisualScale(float scale)
    {
        if (visualRoot != null)
            visualRoot.localScale = Vector3.one * scale;
        else if (rectTransform != null)
            rectTransform.localScale = Vector3.one * scale;
    }

    private Vector2 ConvertWorldToParentAnchoredPosition(RectTransform target, Vector2 offset)
    {
        if (rectTransform == null || rectTransform.parent == null)
            return Vector2.zero;

        RectTransform parent = rectTransform.parent as RectTransform;
        Vector3 world = target.TransformPoint(offset);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, world);
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out localPoint);
        return localPoint;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseInBackSoft(float t)
    {
        float c1 = 0.85f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}
