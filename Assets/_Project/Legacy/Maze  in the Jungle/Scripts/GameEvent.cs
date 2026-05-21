using UnityEngine;

public class GameEvent : MonoBehaviour
{
    [SerializeField] private int requiredTouchCount = 5;
    [SerializeField] private float eventCooldownSeconds = 0.012f;

    private bool alreadyEventSend;
    private int playerClickCount;
    private float lastEventTime = -999f;

    public void GameEventCatcher()
    {
        if (Input.touchCount > 0 || Input.GetMouseButtonDown(0))
        {
            RegisterPlayerTouch();
        }
    }

    public void RegisterPlayerTouch()
    {
        playerClickCount++;
        if (playerClickCount >= requiredTouchCount)
        {
            SendEvent();
        }
    }

    public bool SendEvent()
    {
        if (Time.unscaledTime - lastEventTime < eventCooldownSeconds)
        {
            return false;
        }

        if (alreadyEventSend)
        {
            return false;
        }

        alreadyEventSend = true;
        lastEventTime = Time.unscaledTime;
        Debug.Log("Event is being sent to the panel", this);
        return true;
    }

    public void ResetEventState()
    {
        alreadyEventSend = false;
        playerClickCount = 0;
    }

    private void OnValidate()
    {
        requiredTouchCount = Mathf.Max(1, requiredTouchCount);
        eventCooldownSeconds = Mathf.Max(0f, eventCooldownSeconds);
    }
}
