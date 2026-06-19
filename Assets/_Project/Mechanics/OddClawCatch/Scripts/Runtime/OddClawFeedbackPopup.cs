using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class OddClawFeedbackPopup : MonoBehaviour
{
    [Header("References")]
    public RectTransform root;
    public CanvasGroup canvasGroup;
    public TMP_Text messageText;

    [Header("Animation")]
    public float showDuration = 0.18f;
    public float holdDuration = 0.55f;
    public float hideDuration = 0.18f;
    public float punchScale = 0.15f;

    public float TotalDuration => Mathf.Max(0.01f, showDuration) + 0.2f + Mathf.Max(0f, holdDuration) + Mathf.Max(0.01f, hideDuration);

    private void Reset()
    {
        root = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        messageText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Awake()
    {
        if (root == null)
            root = transform as RectTransform;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        HideImmediate();
    }

    public void ApplyFont(TMP_FontAsset font)
    {
        if (messageText != null && font != null)
            messageText.font = font;
    }

    public void Show(string message, Color color)
    {
        if (canvasGroup == null || root == null || messageText == null)
            return;

        DOTween.Kill(root);
        DOTween.Kill(canvasGroup);

        messageText.text = message;
        messageText.color = color;
        root.localScale = Vector3.one * 0.85f;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);

        Sequence sequence = DOTween.Sequence();
        sequence.Join(canvasGroup.DOFade(1f, showDuration));
        sequence.Join(root.DOScale(Vector3.one, showDuration).SetEase(Ease.OutBack));
        sequence.Append(root.DOPunchScale(Vector3.one * punchScale, 0.2f, 8, 0.8f));
        sequence.AppendInterval(holdDuration);
        sequence.Append(canvasGroup.DOFade(0f, hideDuration));
        sequence.OnComplete(HideImmediate);
    }


    public IEnumerator ShowAndWait(string message, Color color)
    {
        Show(message, color);
        yield return new WaitForSeconds(TotalDuration);
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }
}
