using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    private Vector3 originalPosition;
    private Tween activeTween;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    public void Shake(float duration = 0.3f, float strength = 0.2f)
    {
        duration = Mathf.Max(0f, duration);
        strength = Mathf.Max(0f, strength);

        if (duration <= 0f || strength <= 0f)
        {
            return;
        }

        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill(false);
            transform.position = originalPosition;
        }

        originalPosition = transform.position;

        activeTween = transform.DOShakePosition(duration, strength, 10, 90f)
            .SetLink(gameObject)
            .OnComplete(() => transform.position = originalPosition);
    }

    private void OnDisable()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill(false);
        }

        transform.position = originalPosition;
    }
}
