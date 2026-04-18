using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressScale = 0.9f;
    public float duration = 0.1f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(originalScale * pressScale, duration).SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(originalScale, duration).SetEase(Ease.OutBack);
    }
}