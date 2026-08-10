using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OddClawItemView : MonoBehaviour
{
    [Header("References")]
    public RectTransform root;
    public RectTransform catchZone;
    public TMP_Text answerText;
    public Image answerImage;
    public Image backgroundImage;
    public CanvasGroup canvasGroup;

    [Header("Grab Attach Offset")]
    [Tooltip("Extra local offset applied after the object is attached to the claw socket. Use this per template for text/image/object-specific alignment.")]
    public Vector2 grabbedLocalOffset = Vector2.zero;
    [Tooltip("Extra local rotation applied after the object is attached to the claw socket.")]
    public Vector3 grabbedLocalRotation = Vector3.zero;
    [Tooltip("Extra local scale multiplier applied after the object is attached to the claw socket.")]
    public float grabbedLocalScale = 1f;

    [Header("State Colors")]
    public Color normalColor = Color.white;
    public Color correctColor = new Color(0.35f, 1f, 0.45f, 1f);
    public Color wrongColor = new Color(1f, 0.35f, 0.35f, 1f);
    public Color caughtColor = new Color(1f, 0.92f, 0.35f, 1f);

    [Header("Evaluation Animation")]
    [Tooltip("Small item-only punch after correct/wrong is evaluated. This does not scale the claw.")]
    public float evaluationPunchScale = 0.08f;
    public float evaluationPunchDuration = 0.18f;

    public int Index { get; private set; }
    public bool IsCorrect { get; private set; }
    public bool IsCaught { get; private set; }
    public Vector2 GrabbedLocalOffset => grabbedLocalOffset;
    public Vector3 GrabbedLocalRotation => grabbedLocalRotation;
    public float GrabbedLocalScale => Mathf.Max(0.01f, grabbedLocalScale);
    public RectTransform RectTransform => root != null ? root : transform as RectTransform;

    private Transform _originalParent;
    private Vector3 _originalScale;

    private void Reset()
    {
        root = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (root == null)
            root = transform as RectTransform;

        if (catchZone == null)
            catchZone = root;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        _originalParent = transform.parent;
        _originalScale = transform.localScale;
    }

    public void Setup(OddClawAnswerOption option, OddClawAnswerDisplayMode displayMode, int index, int correctIndex, TMP_FontAsset primaryFont, TMP_FontAsset secondaryFont)
    {
        Index = index;
        IsCorrect = index == correctIndex;
        IsCaught = false;

        bool hasSprite = option != null && option.sprite != null;
        bool hasText = option != null && !string.IsNullOrWhiteSpace(option.text);
        bool usesImages = displayMode == OddClawAnswerDisplayMode.Sprite
            || displayMode == OddClawAnswerDisplayMode.SpriteWithOptionalText;
        bool showText = displayMode == OddClawAnswerDisplayMode.Text
            || !hasSprite
            || (displayMode == OddClawAnswerDisplayMode.SpriteWithOptionalText && hasText);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        if (answerText != null)
        {
            answerText.font = primaryFont != null ? primaryFont : answerText.font;
            answerText.gameObject.SetActive(showText);
            answerText.text = option != null ? option.text : string.Empty;
        }

        if (answerImage != null)
        {
            answerImage.gameObject.SetActive(usesImages && hasSprite);
            answerImage.sprite = option != null ? option.sprite : null;
            answerImage.preserveAspect = true;
        }

        transform.localScale = _originalScale;
    }

    public bool OverlapsScreenCircle(Vector2 screenCenter, float radiusPixels, Camera uiCamera)
    {
        RectTransform target = catchZone != null ? catchZone : root;
        if (target == null || !gameObject.activeInHierarchy)
            return false;

        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < worldCorners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCorners[i]);
            minX = Mathf.Min(minX, screen.x);
            maxX = Mathf.Max(maxX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxY = Mathf.Max(maxY, screen.y);
        }

        float closestX = Mathf.Clamp(screenCenter.x, minX, maxX);
        float closestY = Mathf.Clamp(screenCenter.y, minY, maxY);
        float dx = screenCenter.x - closestX;
        float dy = screenCenter.y - closestY;

        return (dx * dx + dy * dy) <= radiusPixels * radiusPixels;
    }

    public void MarkCaught(Transform clawTip)
    {
        MarkCaught(clawTip, Vector2.zero, Vector3.zero, 1f);
    }

    public void MarkCaught(Transform attachParent, Vector2 localOffset, Vector3 localRotation, float localScale)
    {
        PrepareCaughtState();

        RectTransform rect = RectTransform;
        if (rect != null)
        {
            rect.SetParent(attachParent, false);
            rect.anchoredPosition = localOffset;
            rect.localEulerAngles = localRotation;
            rect.localScale = Vector3.one * Mathf.Max(0.01f, localScale);
        }
        else
        {
            transform.SetParent(attachParent, false);
            transform.localPosition = localOffset;
            transform.localEulerAngles = localRotation;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, localScale);
        }
    }

    public void MarkCaughtAnimated(
        Transform attachParent,
        Vector2 localOffset,
        Vector3 localRotation,
        float localScale,
        float duration)
    {
        if (attachParent == null)
            return;

        PrepareCaughtState();
        float safeDuration = Mathf.Max(0f, duration);
        RectTransform rect = RectTransform;

        if (rect != null)
        {
            rect.DOKill();
            rect.SetParent(attachParent, true);

            if (safeDuration <= 0f)
            {
                rect.anchoredPosition = localOffset;
                rect.localEulerAngles = localRotation;
                rect.localScale = Vector3.one * Mathf.Max(0.01f, localScale);
                return;
            }

            rect.DOAnchorPos(localOffset, safeDuration).SetEase(Ease.OutCubic);
            rect.DOLocalRotate(localRotation, safeDuration).SetEase(Ease.OutCubic);
            rect.DOScale(Vector3.one * Mathf.Max(0.01f, localScale), safeDuration)
                .SetEase(Ease.OutBack);
            return;
        }

        transform.DOKill();
        transform.SetParent(attachParent, true);
        if (safeDuration <= 0f)
        {
            transform.localPosition = localOffset;
            transform.localEulerAngles = localRotation;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, localScale);
            return;
        }

        transform.DOLocalMove(localOffset, safeDuration).SetEase(Ease.OutCubic);
        transform.DOLocalRotate(localRotation, safeDuration).SetEase(Ease.OutCubic);
        transform.DOScale(Vector3.one * Mathf.Max(0.01f, localScale), safeDuration)
            .SetEase(Ease.OutBack);
    }

    private void PrepareCaughtState()
    {
        IsCaught = true;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (backgroundImage != null)
            backgroundImage.color = caughtColor;
    }

    public void SetFeedback(bool correct)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = correct ? correctColor : wrongColor;
    }

    public void PlayCaughtPop(float popScale, float duration)
    {
        Transform target = root != null ? root : transform;
        if (target == null)
            return;

        target.DOKill();
        float punchAmount = Mathf.Max(0f, popScale - 1f);
        target.DOPunchScale(Vector3.one * punchAmount, Mathf.Max(0.01f, duration), 6, 0.75f);
    }


    public IEnumerator PlayEvaluationFeedbackAndFade(bool correct, float holdBeforeFade, float fadeDuration)
    {
        SetFeedback(correct);

        Transform target = root != null ? root : transform;
        if (target != null)
        {
            target.DOKill();
            float safePunch = Mathf.Max(0f, evaluationPunchScale);
            if (safePunch > 0f)
                target.DOPunchScale(Vector3.one * safePunch, Mathf.Max(0.01f, evaluationPunchDuration), 7, 0.8f);
        }

        if (holdBeforeFade > 0f)
            yield return new WaitForSeconds(holdBeforeFade);

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            yield return canvasGroup.DOFade(0f, Mathf.Max(0.01f, fadeDuration)).WaitForCompletion();
        }
        else if (fadeDuration > 0f)
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        gameObject.SetActive(false);
    }

    public void RestoreParent()
    {
        if (_originalParent != null)
            transform.SetParent(_originalParent, true);
    }
}
