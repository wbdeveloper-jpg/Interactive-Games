using UnityEngine;
using DG.Tweening;

public class CameraShake : MonoBehaviour
{
    public void Shake(float duration = 0.3f, float strength = 0.2f)
    {
        transform.DOShakePosition(duration, strength, 10, 90);
    }
}