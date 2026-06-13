using UnityEngine;
using TMPro;
using DG.Tweening;

public class FractionPortionFeedbackPopup : MonoBehaviour
{
    public TMP_Text popupText;
    public CanvasGroup canvasGroup;
    public RectTransform popupRoot;

    [Header("Animation Timing - Inspector Adjustable")]
    [Min(0.01f)] public float fadeInDuration = 0.18f;
    [Min(0.01f)] public float scaleInDuration = 0.22f;
    [Min(0f)] public float holdDuration = 1.25f;
    [Min(0.01f)] public float moveUpDuration = 0.45f;
    [Min(0.01f)] public float fadeOutDuration = 0.35f;
    public float moveUpDistance = 42f;

    private Vector2 startPosition;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (popupRoot != null)
            startPosition = popupRoot.anchoredPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void Show(string message, Color color)
    {
        if (popupText != null)
        {
            popupText.text = message;
            popupText.color = color;
        }

        if (popupRoot != null)
        {
            popupRoot.DOKill();
            popupRoot.anchoredPosition = startPosition;
            popupRoot.localScale = Vector3.one * 0.92f;
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
        }

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(false);

        if (canvasGroup != null)
            sequence.Join(canvasGroup.DOFade(1f, fadeInDuration));

        if (popupRoot != null)
        {
            sequence.Join(popupRoot.DOScale(1f, scaleInDuration).SetEase(Ease.OutBack));
            sequence.AppendInterval(holdDuration);
            sequence.Append(popupRoot.DOAnchorPos(startPosition + Vector2.up * moveUpDistance, moveUpDuration).SetEase(Ease.OutSine));
        }
        else
        {
            sequence.AppendInterval(holdDuration + moveUpDuration);
        }

        if (canvasGroup != null)
            sequence.Join(canvasGroup.DOFade(0f, fadeOutDuration));
    }
}
