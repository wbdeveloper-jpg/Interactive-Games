using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MeasurementMix
{
    public class MeasurementHintController : MonoBehaviour
    {
        public Button hintButton;
        public RectTransform hintButtonRect;
        public TMP_Text hintButtonLabel;
        public MeasurementGameManager gameManager;

        [Header("Animation")]
        [Min(0.2f)] public float pulseDuration = 0.55f;
        [Range(1, 4)] public int pulseCount = 2;

        private Tween encouragementTween;

        private void Awake()
        {
            if (hintButton != null)
                hintButton.onClick.AddListener(HandleHintClicked);
        }

        private void OnDisable()
        {
            encouragementTween?.Kill();
        }

        public void ResetForRound()
        {
            encouragementTween?.Kill();
            if (hintButtonRect != null)
            {
                hintButtonRect.DOKill();
                hintButtonRect.localScale = Vector3.one;
            }

            if (hintButton != null)
                hintButton.interactable = true;
            if (hintButtonLabel != null)
                hintButtonLabel.text = "HINT";
        }

        public void EncourageHint()
        {
            if (hintButtonRect == null)
                return;

            encouragementTween?.Kill();
            hintButtonRect.DOKill();
            hintButtonRect.localScale = Vector3.one;

            Sequence sequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            for (int index = 0; index < Mathf.Max(1, pulseCount); index++)
            {
                sequence.Append(hintButtonRect.DOScale(1.13f, pulseDuration * 0.45f)
                    .SetEase(Ease.OutQuad));
                sequence.Append(hintButtonRect.DOScale(1f, pulseDuration * 0.55f)
                    .SetEase(Ease.InOutQuad));
            }

            encouragementTween = sequence;
        }

        public void MarkUsed()
        {
            encouragementTween?.Kill();
            if (hintButtonRect != null)
            {
                hintButtonRect.DOKill();
                hintButtonRect.localScale = Vector3.one;
            }
            if (hintButtonLabel != null)
                hintButtonLabel.text = "HINT USED";
        }

        private void HandleHintClicked()
        {
            gameManager?.UseHint();
        }
    }
}
