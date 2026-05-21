using System;
using DG.Tweening;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryPopupAnimator : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Open")]
        [SerializeField, Min(0.05f)] private float openDuration = 0.22f;
        [SerializeField] private Vector3 hiddenScale = new Vector3(0.92f, 0.92f, 0.92f);
        [SerializeField] private Ease openEase = Ease.OutBack;

        [Header("Close")]
        [SerializeField, Min(0.05f)] private float closeDuration = 0.16f;
        [SerializeField] private Ease closeEase = Ease.InSine;

        [Header("Update")]
        [SerializeField] private bool useUnscaledTime = false;

        private Sequence activeSequence;
        private Vector3 shownScale = Vector3.one;

        private void Awake()
        {
            CacheTargets();

            if (panelTransform != null)
            {
                shownScale = panelTransform.localScale;
            }
        }

        private void OnDestroy()
        {
            KillActiveSequence();
        }

        public void Open()
        {
            CacheTargets();
            KillActiveSequence();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = hiddenScale;
            }

            activeSequence = DOTween.Sequence();
            activeSequence.SetUpdate(useUnscaledTime);

            if (canvasGroup != null)
            {
                activeSequence.Join(canvasGroup.DOFade(1f, openDuration));
            }

            if (panelTransform != null)
            {
                activeSequence.Join(panelTransform.DOScale(shownScale, openDuration).SetEase(openEase));
            }
        }

        public void Close(Action onComplete)
        {
            CacheTargets();
            KillActiveSequence();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = true;
            }

            activeSequence = DOTween.Sequence();
            activeSequence.SetUpdate(useUnscaledTime);

            if (canvasGroup != null)
            {
                activeSequence.Join(canvasGroup.DOFade(0f, closeDuration));
            }

            if (panelTransform != null)
            {
                activeSequence.Join(panelTransform.DOScale(hiddenScale, closeDuration).SetEase(closeEase));
            }

            activeSequence.OnComplete(() =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = false;
                }

                onComplete?.Invoke();
            });
        }

        public void HideImmediate()
        {
            CacheTargets();
            KillActiveSequence();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panelTransform != null)
            {
                panelTransform.localScale = shownScale;
            }
        }

        private void CacheTargets()
        {
            if (panelTransform == null)
            {
                panelTransform = transform as RectTransform;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void KillActiveSequence()
        {
            if (activeSequence != null && activeSequence.IsActive())
            {
                activeSequence.Kill();
            }

            activeSequence = null;
        }
    }
}
