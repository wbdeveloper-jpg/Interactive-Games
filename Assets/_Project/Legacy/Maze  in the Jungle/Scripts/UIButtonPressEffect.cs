using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float pressScale = 0.9f;
    public float duration = 0.1f;

    private Vector3 originalScale;
    private bool initialized;

    private void Awake()
    {
        CacheOriginalScale();
    }

    private void OnEnable()
    {
        CacheOriginalScale();
        transform.localScale = originalScale;
    }

    private void CacheOriginalScale()
    {
        if (initialized)
        {
            return;
        }

        originalScale = transform.localScale;
        initialized = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        AnimateToScale(originalScale * pressScale, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        AnimateToScale(originalScale, Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        AnimateToScale(originalScale, Ease.OutBack);
    }

    private void AnimateToScale(Vector3 targetScale, Ease ease)
    {
        transform.DOKill(false);
        transform.DOScale(targetScale, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        transform.DOKill(false);
        if (initialized)
        {
            transform.localScale = originalScale;
        }
    }

    private void OnValidate()
    {
        pressScale = Mathf.Clamp(pressScale, 0.01f, 1.5f);
        duration = Mathf.Max(0f, duration);
    }
}
