using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class StarAnimation : MonoBehaviour
{
    [Header("Assign stars in order")]
    public RectTransform[] stars;

    [Header("Assign sticks (matching order)")]
    public RectTransform[] sticks;

    [Header("Animation Settings")]
    public float initialDelay = 0.05f;
    public float starScaleDuration = 0.3f;
    public float starFadeDuration = 0.2f;
    public float starStagger = 0.12f;

    public float stickDrawDuration = 0.25f;
    public float stickDelayAfterStar = 0.05f;

    public Ease starScaleEase = Ease.OutBack;
    public Ease stickEase = Ease.Linear;

    [Header("Star scale settings")]
    public float startScale = 0f;     // YOU set scale = 0 in editor already
    public float endScale = 1f;

    [Header("Events")]
    public UnityEvent onComplete;

    private Sequence seq;

    void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        Stop();

        seq = DOTween.Sequence();

        seq.AppendInterval(initialDelay);

        for (int i = 0; i < stars.Length; i++)
        {
            RectTransform star = stars[i];
            if (star == null) continue;

            Image starImg = star.GetComponent<Image>();
            CanvasGroup cg = star.GetComponent<CanvasGroup>();

            // FORCE starting values (from editor or here safely)
            star.localScale = Vector3.one * startScale;

            if (cg != null)
            {
                cg.alpha = 0f;
            }
            else if (starImg != null)
            {
                Color c = starImg.color;
                c.a = 0f;
                starImg.color = c;
            }

            // --- STAR SOUND (play right before the star animation) ---
            seq.AppendCallback(() =>
            {
                // play the star pop SFX (use your AudioManager)
                AudioManager.Instance.PlaySFX(1);
            });

            // STAR ANIMATION
            seq.Append(star.DOScale(endScale, starScaleDuration).SetEase(starScaleEase));

            if (cg != null)
                seq.Join(cg.DOFade(1f, starFadeDuration));
            else if (starImg != null)
                seq.Join(starImg.DOFade(1f, starFadeDuration));

            seq.AppendInterval(starStagger);

            // STICK ANIMATION
            if (i < sticks.Length)
            {
                RectTransform stick = sticks[i];
                if (stick != null)
                {
                    Image stickImg = stick.GetComponent<Image>();

                    seq.AppendInterval(stickDelayAfterStar);

                    // --- SOFT WHOOSH SOUND (play right before the stick draw begins) ---
                    seq.AppendCallback(() =>
                    {
                        AudioManager.Instance.PlaySFX(0);
                    });

                    if (stickImg != null && stickImg.type == Image.Type.Filled)
                    {
                        stickImg.fillAmount = 0f;
                        seq.Append(stickImg.DOFillAmount(1f, stickDrawDuration).SetEase(stickEase));
                    }
                    else
                    {
                        Vector3 original = stick.localScale;
                        stick.localScale = new Vector3(0f, original.y, original.z);
                        seq.Append(stick.DOScaleX(original.x, stickDrawDuration).SetEase(stickEase));
                    }
                }
            }
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void Stop()
    {
        if (seq != null)
        {
            seq.Kill();
            seq = null;
        }
    }
}
