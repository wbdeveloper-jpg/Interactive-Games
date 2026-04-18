using System.Collections;
using UnityEngine;

public class EyeBlinkController : MonoBehaviour
{
    [SerializeField] private Animator eyeAnimator;
    [SerializeField] private string blinkTriggerName = "Blink";

    [Header("Blink Timing")]
    [SerializeField] private float minBlinkDelay = 3f;
    [SerializeField] private float maxBlinkDelay = 7f;

    void Start()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minBlinkDelay, maxBlinkDelay);
            yield return new WaitForSeconds(waitTime);

            if (eyeAnimator != null)
                eyeAnimator.SetTrigger(blinkTriggerName);
        }
    }
}
