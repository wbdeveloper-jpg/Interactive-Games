using UnityEngine;
using TMPro;
using DG.Tweening;

[DisallowMultipleComponent]
public class FloatingText : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI text;

    [Header("Animation")]
    public float popDuration = 0.25f;
    public float floatDuration = 1.15f;
    public float fadeDuration = 0.8f;
    public float riseDistance = 120f;

    private bool _destroyRequested;

    public void Show(string message, Color color)
    {
        Show(message, color, null);
    }

    public void Show(string message, Color color, Vector2? anchoredPosition)
    {
        if (text == null)
        {
            Debug.LogWarning($"{nameof(FloatingText)} on {name} has no TextMeshProUGUI reference.");
            SafeDestroy();
            return;
        }

        KillTweens();

        _destroyRequested = false;

        if (anchoredPosition.HasValue)
            SetAnchoredPosition(anchoredPosition.Value);

        CanvasGroup canvasGroup = GetOrCreateCanvasGroup();
        canvasGroup.alpha = 1f;

        text.text = message;
        text.color = color;
        text.alpha = 1f;

        transform.localScale = Vector3.zero;

        transform
            .DOScale(1f, popDuration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject);

        MoveUp();

        canvasGroup
            .DOFade(0f, fadeDuration)
            .SetDelay(Mathf.Max(0.05f, floatDuration - fadeDuration))
            .SetEase(Ease.OutQuad)
            .OnComplete(SafeDestroy)
            .SetLink(gameObject);
    }

    private void SetAnchoredPosition(Vector2 anchoredPosition)
    {
        RectTransform rectTransform = transform as RectTransform;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition;
        }
        else
        {
            transform.localPosition = new Vector3(
                anchoredPosition.x,
                anchoredPosition.y,
                transform.localPosition.z
            );
        }
    }

    private void MoveUp()
    {
        RectTransform rectTransform = transform as RectTransform;

        if (rectTransform != null)
        {
            rectTransform
                .DOAnchorPosY(rectTransform.anchoredPosition.y + riseDistance, floatDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
        else
        {
            transform
                .DOLocalMoveY(transform.localPosition.y + riseDistance, floatDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }
    }

    private CanvasGroup GetOrCreateCanvasGroup()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private void SafeDestroy()
    {
        if (_destroyRequested)
            return;

        _destroyRequested = true;
        Destroy(gameObject);
    }

    private void KillTweens()
    {
        transform.DOKill();

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
            rectTransform.DOKill();

        if (text != null)
            text.DOKill();

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.DOKill();
    }

    private void OnDisable()
    {
        KillTweens();
    }
}