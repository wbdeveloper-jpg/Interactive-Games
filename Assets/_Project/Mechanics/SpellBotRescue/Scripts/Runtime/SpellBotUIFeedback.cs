using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using DG.Tweening;

namespace NarayanaGames.SpellBotRescue
{
    public class SpellBotUIFeedback : MonoBehaviour
    {
        [Header("Main UI References")]
        public RectTransform monitorRoot;
        public RectTransform fixedButtonRoot;
        public RectTransform hintPopupRoot;
        public CanvasGroup hintCanvasGroup;
        [FormerlySerializedAs("gearsTextTransform")] public Transform scoreTextTransform;
        public Image overdriveGlow;
        public Image monitorGlow;

        [Header("Premium Timing")]
        public float roundIntroDuration = 0.35f;
        public float correctPopDuration = 0.38f;
        public float wrongShakeDuration = 0.35f;
        public float hintFadeDuration = 0.22f;
        public float panelOpenDuration = 0.28f;

        [Header("Motion Strength")]
        public float wrongShakeStrength = 22f;
        public float hintSlideDistance = 24f;
        public float monitorPopScale = 0.035f;
        public float wordPunchScale = 0.13f;

        [Header("Glow Colors")]
        public Color correctGlowColor = new Color(0.16f, 0.95f, 0.48f, 0.32f);
        public Color overdriveGlowColor = new Color(1f, 0.78f, 0.18f, 0.55f);

        private Vector2 hintOriginalPosition;
        private Vector3 monitorOriginalScale;

        private void Awake()
        {
            if (hintPopupRoot != null)
            {
                hintOriginalPosition = hintPopupRoot.anchoredPosition;
            }

            if (monitorRoot != null)
            {
                monitorOriginalScale = monitorRoot.localScale;
            }

            HideHintInstant();
            HideMonitorGlowInstant();
        }

        public void PlayRoundIntro(TextMeshProUGUI wordText)
        {
            if (wordText != null)
            {
                Transform target = wordText.transform;
                target.DOKill();
                target.localScale = Vector3.one * 0.94f;
                target.DOScale(Vector3.one, roundIntroDuration).SetEase(Ease.OutBack);
            }

            if (monitorRoot != null)
            {
                monitorRoot.DOKill();
                monitorRoot.localScale = monitorOriginalScale * 0.985f;
                monitorRoot.DOScale(monitorOriginalScale, roundIntroDuration).SetEase(Ease.OutCubic);
            }
        }

        public void PlayWrongShake()
        {
            if (monitorRoot != null)
            {
                monitorRoot.DOKill();
                monitorRoot.DOShakeAnchorPos(wrongShakeDuration, wrongShakeStrength, 16, 90f)
                    .SetUpdate(true);
            }

            if (fixedButtonRoot != null)
            {
                fixedButtonRoot.DOKill();
                fixedButtonRoot.DOShakeAnchorPos(0.24f, 10f, 12, 90f)
                    .SetUpdate(true);
            }
        }

        public void PlayDisabledFixedShake()
        {
            if (fixedButtonRoot != null)
            {
                fixedButtonRoot.DOKill();
                fixedButtonRoot.DOShakeAnchorPos(0.22f, 14f, 12, 90f)
                    .SetUpdate(true);
            }
        }

        public void PlayCorrectWord(TextMeshProUGUI wordText)
        {
            if (wordText == null)
            {
                return;
            }

            Transform target = wordText.transform;
            target.DOKill();
            target.localScale = Vector3.one;
            target.DOPunchScale(Vector3.one * wordPunchScale, correctPopDuration, 8, 0.82f);
        }

        public void PlayCorrectMonitorGlow()
        {
            if (monitorRoot != null)
            {
                monitorRoot.DOKill();
                monitorRoot.localScale = monitorOriginalScale;
                monitorRoot.DOPunchScale(Vector3.one * monitorPopScale, 0.34f, 7, 0.8f);
            }

            if (monitorGlow == null)
            {
                return;
            }

            monitorGlow.gameObject.SetActive(true);
            monitorGlow.DOKill();
            monitorGlow.color = new Color(correctGlowColor.r, correctGlowColor.g, correctGlowColor.b, 0f);
            monitorGlow.DOFade(correctGlowColor.a, 0.12f)
                .OnComplete(() => monitorGlow.DOFade(0f, 0.42f).OnComplete(HideMonitorGlowInstant));
        }

        public void PlayClearPulse(TextMeshProUGUI wordText)
        {
            if (wordText == null)
            {
                return;
            }

            Transform target = wordText.transform;
            target.DOKill();
            target.localScale = Vector3.one;
            target.DOScale(0.96f, 0.08f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }

        public void ShowHint(string hint, TextMeshProUGUI hintText)
        {
            if (hintText != null)
            {
                hintText.text = hint;
            }

            if (hintPopupRoot != null)
            {
                hintPopupRoot.gameObject.SetActive(true);
                hintPopupRoot.DOKill();
                hintPopupRoot.anchoredPosition = hintOriginalPosition - new Vector2(0f, hintSlideDistance);
                hintPopupRoot.DOAnchorPos(hintOriginalPosition, hintFadeDuration).SetEase(Ease.OutCubic);
                hintPopupRoot.localScale = Vector3.one * 0.98f;
                hintPopupRoot.DOScale(Vector3.one, hintFadeDuration).SetEase(Ease.OutBack);
            }

            if (hintCanvasGroup != null)
            {
                hintCanvasGroup.DOKill();
                hintCanvasGroup.alpha = 0f;
                hintCanvasGroup.DOFade(1f, hintFadeDuration);
            }
        }

        public void HideHint()
        {
            if (hintCanvasGroup != null)
            {
                hintCanvasGroup.DOKill();
                hintCanvasGroup.DOFade(0f, 0.12f).OnComplete(() =>
                {
                    if (hintPopupRoot != null)
                    {
                        hintPopupRoot.gameObject.SetActive(false);
                    }
                });
            }
            else if (hintPopupRoot != null)
            {
                hintPopupRoot.gameObject.SetActive(false);
            }
        }

        public void HideHintInstant()
        {
            if (hintCanvasGroup != null)
            {
                hintCanvasGroup.DOKill();
                hintCanvasGroup.alpha = 0f;
            }

            if (hintPopupRoot != null)
            {
                hintPopupRoot.DOKill();
                hintPopupRoot.gameObject.SetActive(false);
                hintPopupRoot.anchoredPosition = hintOriginalPosition;
                hintPopupRoot.localScale = Vector3.one;
            }
        }

        public void PlayShowAnswerAvailable(Transform target)
        {
            if (target == null)
            {
                return;
            }

            target.DOKill();
            target.localScale = Vector3.one;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(target.DOPunchScale(Vector3.one * 0.12f, 0.34f, 8, 0.82f));
            sequence.Join(target.DORotate(new Vector3(0f, 0f, -3f), 0.12f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad));
        }

        public void PopScore()
        {
            if (scoreTextTransform == null)
            {
                return;
            }

            scoreTextTransform.DOKill();
            scoreTextTransform.localScale = Vector3.one;
            scoreTextTransform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 8, 0.8f);
        }

        public void PopGears()
        {
            PopScore();
        }

        public void PopStar(Image star)
        {
            if (star == null)
            {
                return;
            }

            star.transform.DOKill();
            star.transform.localScale = Vector3.one;
            star.transform.DOPunchScale(Vector3.one * 0.24f, 0.28f, 8, 0.75f);
            star.transform.DORotate(new Vector3(0f, 0f, 12f), 0.12f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }

        public void SetOverdriveGlow(bool active)
        {
            // Overdrive glow is intentionally disabled.
            // Overdrive is now communicated by SpellBotRobotView switching to an overdrive sprite.
            if (overdriveGlow == null)
            {
                return;
            }

            overdriveGlow.transform.DOKill();
            overdriveGlow.transform.localScale = Vector3.one;
            overdriveGlow.gameObject.SetActive(false);
        }

        public void PlayPanelOpen(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            RectTransform rect = panel.transform as RectTransform;
            CanvasGroup group = panel.GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = panel.AddComponent<CanvasGroup>();
            }

            panel.SetActive(true);
            group.DOKill();
            group.alpha = 0f;
            group.DOFade(1f, panelOpenDuration).SetUpdate(true);

            if (rect != null)
            {
                rect.DOKill();
                rect.localScale = Vector3.one * 0.94f;
                rect.DOScale(Vector3.one, panelOpenDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        public void PlayPanelClose(GameObject panel)
        {
            if (panel == null || !panel.activeSelf)
            {
                return;
            }

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                panel.SetActive(false);
                return;
            }

            group.DOKill();
            group.DOFade(0f, 0.12f).SetUpdate(true).OnComplete(() => panel.SetActive(false));
        }

        private void HideMonitorGlowInstant()
        {
            if (monitorGlow == null)
            {
                return;
            }

            monitorGlow.DOKill();
            monitorGlow.color = new Color(correctGlowColor.r, correctGlowColor.g, correctGlowColor.b, 0f);
            monitorGlow.gameObject.SetActive(false);
        }
    }
}
