using System.Collections;
using UnityEngine;

public class IdleAnimationRandomizer : MonoBehaviour
{
    [Header("Animator Parameters")]
    [SerializeField] private string talkingBool = "Talking";
    [SerializeField] private string blinkTrigger = "Blink";
    [SerializeField] private string splTrigger = "SPL";

    [Header("Idle Timing")]
    [Tooltip("Random wait time before playing idle animation")]
    [SerializeField] private Vector2 idleDelayRange = new Vector2(3f, 7f);

    [Header("Probability")]
    [Tooltip("Chance (in %) to play Blink. Remaining goes to SPL.")]
    [Range(0, 100)]
    [SerializeField] private int blinkChance = 80;

    private Animator animator;
    private Coroutine idleRoutine;

    void Awake()
    {
        // Always get the animator belonging to THIS level character
        animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        if (animator == null)
        {
            Debug.LogWarning($"{name} : IdleAnimationRandomizer could not find Animator.");
            return;
        }

        idleRoutine = StartCoroutine(IdleLoop());
    }

    void OnDisable()
    {
        if (idleRoutine != null)
            StopCoroutine(idleRoutine);
    }

    private IEnumerator IdleLoop()
    {
        while (true)
        {
            float wait = Random.Range(idleDelayRange.x, idleDelayRange.y);
            yield return new WaitForSeconds(wait);

            // ? Do not play idle animations while talking
            if (animator.GetBool(talkingBool))
                continue;

            int roll = Random.Range(0, 100);

            if (roll < blinkChance)
            {
                animator.SetTrigger(blinkTrigger);
            }
            else
            {
                animator.SetTrigger(splTrigger);
            }
        }
    }
}
