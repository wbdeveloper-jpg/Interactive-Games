using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class WordFillUIAnimator : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private TMP_Text centerFeedbackText;
    [SerializeField] private CanvasGroup centerFeedbackCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float centerFeedbackDuration = 0.75f;
    [SerializeField] private float letterSpawnDelay = 0.035f;
    [SerializeField] private float hintAttentionInterval = 2.5f;

    private Coroutine hintAttentionRoutine;
    private Coroutine timerWarningRoutine;

    private void Awake()
    {
        if (centerFeedbackText != null)
            centerFeedbackText.gameObject.SetActive(false);

        if (centerFeedbackCanvasGroup != null)
            centerFeedbackCanvasGroup.alpha = 0f;
    }

    public void PlayCenterFeedback(string message)
    {
        if (centerFeedbackText == null)
            return;

        RectTransform rect = centerFeedbackText.transform as RectTransform;
        Vector2 startPosition = rect != null ? rect.anchoredPosition : Vector2.zero;

        centerFeedbackText.gameObject.SetActive(true);
        centerFeedbackText.text = message;

        if (rect != null)
        {
            rect.DOKill();
            rect.localScale = Vector3.zero;
            rect.anchoredPosition = startPosition;
        }

        if (centerFeedbackCanvasGroup != null)
        {
            centerFeedbackCanvasGroup.DOKill();
            centerFeedbackCanvasGroup.alpha = 1f;
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        if (rect != null)
        {
            sequence.Join(rect.DOScale(1.15f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true));
            sequence.Append(rect.DOScale(1f, 0.12f).SetEase(Ease.OutQuad).SetUpdate(true));
            sequence.AppendInterval(Mathf.Max(0.05f, centerFeedbackDuration - 0.35f));
            sequence.Join(rect.DOAnchorPos(startPosition + new Vector2(0f, 70f), 0.28f).SetEase(Ease.OutQuad).SetUpdate(true));
        }

        if (centerFeedbackCanvasGroup != null)
            sequence.Join(centerFeedbackCanvasGroup.DOFade(0f, 0.28f).SetUpdate(true));

        sequence.OnComplete(() =>
        {
            if (rect != null)
            {
                rect.anchoredPosition = startPosition;
                rect.localScale = Vector3.one;
            }

            centerFeedbackText.gameObject.SetActive(false);
        });
    }

    public void AnimateLetterSpawn(RectTransform letterRect, int index)
    {
        if (letterRect == null)
            return;

        letterRect.DOKill();
        letterRect.localScale = Vector3.zero;
        letterRect.DOScale(1f, 0.22f)
            .SetDelay(index * letterSpawnDelay)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    public void PlayLetterTap(RectTransform letterRect)
    {
        if (letterRect == null)
            return;

        letterRect.DOKill();
        letterRect.localScale = Vector3.one;
        letterRect.DOPunchScale(Vector3.one * 0.15f, 0.18f, 6, 0.8f).SetUpdate(true);
    }

    public void PlayWrongShake(RectTransform target)
    {
        if (target == null)
            return;

        target.DOKill();
        target.DOShakeAnchorPos(0.35f, new Vector2(18f, 0f), 16, 90f, false, true).SetUpdate(true);
    }

    public void PlayCorrectWordPulse(RectTransform target)
    {
        if (target == null)
            return;

        target.DOKill();
        target.localScale = Vector3.one;
        target.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.8f).SetUpdate(true);
    }

    public void PlayPanelOpen(GameObject panel)
    {
        if (panel == null)
            return;

        panel.SetActive(true);

        RectTransform rect = panel.transform as RectTransform;

        if (rect == null)
            return;

        rect.DOKill();
        rect.localScale = Vector3.zero;
        rect.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    public void PlayHintReveal(CanvasGroup hintCanvasGroup)
    {
        if (hintCanvasGroup == null)
            return;

        hintCanvasGroup.DOKill();
        hintCanvasGroup.alpha = 0f;
        hintCanvasGroup.DOFade(1f, 0.25f).SetUpdate(true);
    }

    public void StartHintAttention(RectTransform hintButtonRect)
    {
        StopHintAttention(hintButtonRect);

        if (hintButtonRect != null)
            hintAttentionRoutine = StartCoroutine(HintAttentionRoutine(hintButtonRect));
    }

    public void StopHintAttention(RectTransform hintButtonRect)
    {
        if (hintAttentionRoutine != null)
        {
            StopCoroutine(hintAttentionRoutine);
            hintAttentionRoutine = null;
        }

        if (hintButtonRect != null)
        {
            hintButtonRect.DOKill();
            hintButtonRect.localScale = Vector3.one;
        }
    }

    private IEnumerator HintAttentionRoutine(RectTransform rect)
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(hintAttentionInterval);

            rect.DOKill();
            rect.localScale = Vector3.one;
            Tween tween = rect.DOPunchScale(Vector3.one * 0.12f, 0.35f, 6, 0.8f).SetUpdate(true);

            yield return tween.WaitForCompletion();
        }
    }

    public void StartTimerWarning(RectTransform timerRect)
    {
        if (timerWarningRoutine != null || timerRect == null)
            return;

        timerWarningRoutine = StartCoroutine(TimerWarningRoutine(timerRect));
    }

    public void StopTimerWarning(RectTransform timerRect)
    {
        if (timerWarningRoutine != null)
        {
            StopCoroutine(timerWarningRoutine);
            timerWarningRoutine = null;
        }

        if (timerRect != null)
        {
            timerRect.DOKill();
            timerRect.localScale = Vector3.one;
        }
    }

    private IEnumerator TimerWarningRoutine(RectTransform rect)
    {
        while (true)
        {
            rect.DOKill();
            rect.localScale = Vector3.one;

            yield return rect.DOScale(1.12f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true).WaitForCompletion();
            yield return rect.DOScale(1f, 0.2f).SetEase(Ease.InQuad).SetUpdate(true).WaitForCompletion();
            yield return new WaitForSecondsRealtime(0.3f);
        }
    }

    public IEnumerator PlayNarrationHighlight(TMP_Text lineText, Color normalColor, Color narrationColor, float duration)
    {
        if (lineText == null)
            yield break;

        duration = Mathf.Max(0.1f, duration);

        RectTransform rect = lineText.rectTransform;
        rect.DOKill();

        lineText.color = narrationColor;
        rect.localScale = Vector3.one;

        Tween pulseTween = rect.DOScale(1.045f, 0.35f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        pulseTween.Kill();

        rect.DOScale(1f, 0.12f).SetUpdate(true);
        lineText.color = normalColor;
    }
}
