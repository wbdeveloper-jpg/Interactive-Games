using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SkyFallUiPanelAnimator : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform cardRoot;

    [Header("Animation")]
    public bool useAnimation = true;
    public float showDuration = 0.18f;
    public float hideDuration = 0.14f;
    public float hiddenScale = 0.88f;
    public float visibleScale = 1f;

    private Coroutine animationRoutine;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        cardRoot = transform as RectTransform;
    }

    private void Awake()
    {
        Cache();
    }

    public void Show()
    {
        Cache();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        gameObject.SetActive(true);

        if (!useAnimation || !gameObject.activeInHierarchy)
        {
            ShowImmediate();
            return;
        }

        animationRoutine = StartCoroutine(AnimatePanel(true));
    }

    public void Hide()
    {
        Cache();

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (!gameObject.activeInHierarchy)
        {
            HideImmediate();
            return;
        }

        if (!useAnimation)
        {
            HideImmediate();
            return;
        }

        animationRoutine = StartCoroutine(AnimatePanel(false));
    }

    public void ShowImmediate()
    {
        Cache();
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (cardRoot != null)
            cardRoot.localScale = Vector3.one * visibleScale;
    }

    public void HideImmediate()
    {
        Cache();

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (cardRoot != null)
            cardRoot.localScale = Vector3.one * hiddenScale;

        gameObject.SetActive(false);
    }

    private IEnumerator AnimatePanel(bool show)
    {
        float duration = Mathf.Max(0.01f, show ? showDuration : hideDuration);

        if (show)
            gameObject.SetActive(true);

        float startAlpha = canvasGroup != null ? canvasGroup.alpha : (show ? 0f : 1f);
        float endAlpha = show ? 1f : 0f;

        float startScale = cardRoot != null ? cardRoot.localScale.x : (show ? hiddenScale : visibleScale);
        float endScale = show ? visibleScale : hiddenScale;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = show ? EaseOutBackSoft(t) : EaseInQuad(t);

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);

            if (cardRoot != null)
                cardRoot.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = endAlpha;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }

        if (cardRoot != null)
            cardRoot.localScale = Vector3.one * endScale;

        if (!show)
            gameObject.SetActive(false);

        animationRoutine = null;
    }

    private void Cache()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (cardRoot == null)
            cardRoot = transform as RectTransform;
    }

    private static float EaseOutBackSoft(float t)
    {
        float c1 = 1.15f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseInQuad(float t)
    {
        return t * t;
    }
}
