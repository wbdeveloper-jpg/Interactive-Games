using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ConstellationAnimator : MonoBehaviour
{
    [Header("Stars")]
    public RectTransform[] stars;

    [Header("Sticks / Lines")]
    public RectTransform[] sticks;

    [Header("Star Animation")]
    public float initialDelay = 0.05f;
    public float starScaleDuration = 0.28f;
    public float starFadeDuration = 0.18f;
    public float starStagger = 0.08f;
    public float starStartScale = 0f;
    public float starEndScale = 1f;
    public Ease starEase = Ease.OutBack;

    [Header("Stick Animation")]
    public float stickDelayAfterStar = 0.03f;
    public float stickDrawDuration = 0.22f;
    public Ease stickEase = Ease.OutCubic;

    [Header("Behaviour")]
    [Tooltip("Keep false for this project. RevealController manually plays this animation.")]
    public bool playOnEnable = false;

    [Header("Events")]
    public UnityEvent onComplete;

    private Sequence sequence;
    private Vector3[] cachedStarScales;
    private Vector3[] cachedStickScales;
    private bool cached;

    private Action runtimeCompleteCallback;

    private void Awake()
    {
        CacheOriginalValues();
        ResetVisuals();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDestroy()
    {
        Stop();
    }

    public void Play()
    {
        Play(null);
    }

    public void Play(Action onCompleteCallback)
    {
        runtimeCompleteCallback = onCompleteCallback;

        CacheOriginalValues();
        Stop();

        ResetVisuals();

        sequence = DOTween.Sequence();
        sequence.AppendInterval(initialDelay);

        int starCount = stars != null ? stars.Length : 0;

        for (int i = 0; i < starCount; i++)
        {
            int index = i;

            RectTransform star = stars[index];
            if (star == null)
                continue;

            Graphic starGraphic = star.GetComponent<Graphic>();
            CanvasGroup starCanvasGroup = star.GetComponent<CanvasGroup>();

            sequence.AppendCallback(() =>
            {
                if (star != null)
                    star.gameObject.SetActive(true);
            });

            sequence.Append(
                star.DOScale(GetStarTargetScale(index), starScaleDuration)
                    .SetEase(starEase)
            );

            if (starCanvasGroup != null)
            {
                sequence.Join(starCanvasGroup.DOFade(1f, starFadeDuration));
            }
            else if (starGraphic != null)
            {
                sequence.Join(starGraphic.DOFade(1f, starFadeDuration));
            }

            // Draw the stick after this star, if a matching stick exists.
            if (sticks != null && index < sticks.Length)
            {
                RectTransform stick = sticks[index];

                if (stick != null)
                {
                    sequence.AppendInterval(stickDelayAfterStar);

                    sequence.AppendCallback(() =>
                    {
                        if (stick != null)
                            stick.gameObject.SetActive(true);
                    });

                    AppendStickTween(sequence, stick, index);
                }
            }

            sequence.AppendInterval(starStagger);
        }

        sequence.OnComplete(() =>
        {
            sequence = null;
            runtimeCompleteCallback?.Invoke();
            runtimeCompleteCallback = null;
            onComplete?.Invoke();
        });
    }

    public void Stop()
    {
        if (sequence != null)
        {
            sequence.Kill();
            sequence = null;
        }

        if (stars != null)
        {
            foreach (RectTransform star in stars)
            {
                if (star != null)
                {
                    star.DOKill();

                    Graphic graphic = star.GetComponent<Graphic>();
                    if (graphic != null)
                        graphic.DOKill();

                    CanvasGroup canvasGroup = star.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                        canvasGroup.DOKill();
                }
            }
        }

        if (sticks != null)
        {
            foreach (RectTransform stick in sticks)
            {
                if (stick != null)
                {
                    stick.DOKill();

                    Graphic graphic = stick.GetComponent<Graphic>();
                    if (graphic != null)
                        graphic.DOKill();

                    Image image = stick.GetComponent<Image>();
                    if (image != null)
                        image.DOKill();
                }
            }
        }
    }

    public void ResetVisuals()
    {
        CacheOriginalValues();

        if (stars != null)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                RectTransform star = stars[i];
                if (star == null)
                    continue;

                star.DOKill();
                star.gameObject.SetActive(true);
                star.localScale = Vector3.one * starStartScale;

                CanvasGroup canvasGroup = star.GetComponent<CanvasGroup>();
                Graphic graphic = star.GetComponent<Graphic>();

                if (canvasGroup != null)
                {
                    canvasGroup.DOKill();
                    canvasGroup.alpha = 0f;
                }
                else if (graphic != null)
                {
                    graphic.DOKill();
                    Color color = graphic.color;
                    color.a = 0f;
                    graphic.color = color;
                }
            }
        }

        if (sticks != null)
        {
            for (int i = 0; i < sticks.Length; i++)
            {
                RectTransform stick = sticks[i];
                if (stick == null)
                    continue;

                stick.DOKill();
                stick.gameObject.SetActive(true);

                Image image = stick.GetComponent<Image>();

                if (image != null && image.type == Image.Type.Filled)
                {
                    image.DOKill();
                    image.fillAmount = 0f;

                    Color color = image.color;
                    color.a = 1f;
                    image.color = color;
                }
                else
                {
                    Vector3 targetScale = GetStickTargetScale(i);

                    // Important:
                    // We reset only X to zero, but preserve Y/Z from the original stick scale.
                    // If we accidentally cache after this point, stick animation becomes invisible.
                    stick.localScale = new Vector3(0f, targetScale.y, targetScale.z);

                    Graphic graphic = stick.GetComponent<Graphic>();
                    if (graphic != null)
                    {
                        graphic.DOKill();
                        Color color = graphic.color;
                        color.a = 1f;
                        graphic.color = color;
                    }
                }
            }
        }
    }

    private void AppendStickTween(Sequence targetSequence, RectTransform stick, int index)
    {
        if (targetSequence == null || stick == null)
            return;

        Image image = stick.GetComponent<Image>();

        if (image != null && image.type == Image.Type.Filled)
        {
            image.fillAmount = 0f;
            targetSequence.Append(
                image.DOFillAmount(1f, stickDrawDuration)
                    .SetEase(stickEase)
            );
        }
        else
        {
            Vector3 targetScale = GetStickTargetScale(index);

            stick.localScale = new Vector3(0f, targetScale.y, targetScale.z);

            targetSequence.Append(
                stick.DOScaleX(targetScale.x, stickDrawDuration)
                    .SetEase(stickEase)
            );
        }
    }

    private void CacheOriginalValues()
    {
        if (cached)
            return;

        int starCount = stars != null ? stars.Length : 0;
        int stickCount = sticks != null ? sticks.Length : 0;

        cachedStarScales = new Vector3[starCount];
        cachedStickScales = new Vector3[stickCount];

        for (int i = 0; i < starCount; i++)
        {
            cachedStarScales[i] = stars[i] != null ? stars[i].localScale : Vector3.one;
        }

        for (int i = 0; i < stickCount; i++)
        {
            cachedStickScales[i] = sticks[i] != null ? sticks[i].localScale : Vector3.one;
        }

        cached = true;
    }

    private Vector3 GetStarTargetScale(int index)
    {
        if (starEndScale > 0f)
            return Vector3.one * starEndScale;

        if (cachedStarScales != null && index >= 0 && index < cachedStarScales.Length)
            return cachedStarScales[index];

        return Vector3.one;
    }

    private Vector3 GetStickTargetScale(int index)
    {
        if (cachedStickScales != null && index >= 0 && index < cachedStickScales.Length)
            return cachedStickScales[index];

        return Vector3.one;
    }
}