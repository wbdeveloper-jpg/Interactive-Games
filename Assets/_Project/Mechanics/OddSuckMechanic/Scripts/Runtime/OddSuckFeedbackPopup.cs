using DG.Tweening;
using TMPro;
using UnityEngine;

namespace OddSuckMechanic
{
    public class OddSuckFeedbackPopup : MonoBehaviour
    {
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField, Min(0.1f)] private float showDuration = 1.35f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.22f;
        [SerializeField, Min(0f)] private float floatUpDistance = 40f;

        private Tween activeTween;
        private Vector2 startPosition;

        private void Reset()
        {
            popupRoot = transform as RectTransform;
            canvasGroup = GetComponent<CanvasGroup>();
            messageText = GetComponentInChildren<TMP_Text>();
        }

        private void Awake()
        {
            if (popupRoot == null)
            {
                popupRoot = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            startPosition = popupRoot != null ? popupRoot.anchoredPosition : Vector2.zero;
            HideInstant();
        }

        private void OnDestroy()
        {
            activeTween?.Kill();
        }

        public void Show(string message, Color color)
        {
            if (popupRoot == null || canvasGroup == null || messageText == null)
            {
                return;
            }

            activeTween?.Kill();
            gameObject.SetActive(true);
            popupRoot.anchoredPosition = startPosition;
            popupRoot.localScale = Vector3.one * 0.85f;
            canvasGroup.alpha = 0f;
            messageText.text = message;
            messageText.color = color;

            activeTween = DOTween.Sequence()
                .Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 1f, fadeDuration))
                .Join(popupRoot.DOScale(Vector3.one, fadeDuration).SetEase(Ease.OutBack))
                .AppendInterval(showDuration)
                .Join(popupRoot.DOAnchorPos(startPosition + Vector2.up * floatUpDistance, fadeDuration).SetEase(Ease.OutQuad))
                .Join(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 0f, fadeDuration))
                .OnComplete(HideInstant)
                .SetLink(gameObject);
        }

        private void HideInstant()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (popupRoot != null)
            {
                popupRoot.anchoredPosition = startPosition;
            }

            gameObject.SetActive(false);
        }
    }
}
