using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Timer")]
    public float timeElapsed = 0f;
    [SerializeField] private bool startTimerOnStart = true;

    [Header("Score")]
    public int basePoint;

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Transform prefabParent;

    private int lastAnnouncedTime = 0;
    private int userPoint;
    private bool timerRunning;

    [Header("Time Announcement")]
    public bool showTimeAnnouncements = true;
    public int announcementIntervalSeconds = 30;
    public string timeAnnouncementFormat = "{0} sec passed";
    public Color timeAnnouncementColor = Color.white;
    public Vector2 timeAnnouncementAnchoredPosition = new Vector2(0f, -260f);
    public bool useAdaptiveLowerAnnouncementPosition = true;

    [Range(0.1f, 0.45f)]
    public float announcementLowerScreenPercent = 0.28f;

    private void Start()
    {
        ResetPoints();
        timerRunning = startTimerOnStart;
    }

    private void Update()
    {
        if (!timerRunning)
        {
            return;
        }

        timeElapsed += Time.deltaTime;
        AnnounceElapsedTime();
    }

    private void AnnounceElapsedTime()
    {
        int seconds = Mathf.FloorToInt(timeElapsed);
        if (seconds % 30 == 0 && seconds != 0 && seconds != lastAnnouncedTime)
        {
            if (
                showTimeAnnouncements &&
                announcementIntervalSeconds > 0 &&
                seconds % announcementIntervalSeconds == 0 &&
                seconds != 0 &&
                seconds != lastAnnouncedTime
)
            {
                lastAnnouncedTime = seconds;
                ShowTimeAnnouncement(seconds);
            }
        }
    }

    public void ResetGameState(bool startTimer = true)
    {
        timeElapsed = 0f;
        lastAnnouncedTime = 0;
        ResetPoints();
        timerRunning = startTimer;
    }

    public void StartTimer()
    {
        timerRunning = true;
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public void PauseTimer()
    {
        StopTimer();
    }

    public void ResumeTimer()
    {
        StartTimer();
    }

    public void ReduceTime(float amount)
    {
        timeElapsed = Mathf.Max(0f, timeElapsed - Mathf.Max(0f, amount));
    }

    public void ReducePoint(int amount)
    {
        amount = Mathf.Max(0, amount);
        userPoint = Mathf.Max(0, userPoint - amount);
    }

    public void IncreasePoint(int amount)
    {
        amount = Mathf.Max(0, amount);
        userPoint = Mathf.Min(basePoint, userPoint + amount);
    }

    public int GetFinalPoint()
    {
        return userPoint;
    }

    public float GetFinalTime()
    {
        return timeElapsed;
    }

    public float GetAccuracy01()
    {
        if (basePoint <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)userPoint / basePoint);
    }

    private void ResetPoints()
    {
        basePoint = Mathf.Max(0, basePoint);
        userPoint = basePoint;
    }

    private void ShowTimeAnnouncement(int seconds)
    {
        string message = string.Format(timeAnnouncementFormat, seconds);

        Debug.Log(message);

        if (floatingTextPrefab == null || prefabParent == null)
            return;

        GameObject textObj = Instantiate(floatingTextPrefab, prefabParent);

        SetFloatingTextPosition(textObj);

        FloatingText floatingText = textObj.GetComponent<FloatingText>();

        if (floatingText == null)
        {
            Destroy(textObj);
            return;
        }

        floatingText.Show(message, timeAnnouncementColor);
    }

    private void SetFloatingTextPosition(GameObject textObj)
    {
        if (textObj == null)
            return;

        RectTransform textRect = textObj.transform as RectTransform;
        RectTransform parentRect = prefabParent as RectTransform;

        if (textRect == null)
            return;

        Vector2 finalPosition = timeAnnouncementAnchoredPosition;

        if (
            useAdaptiveLowerAnnouncementPosition &&
            parentRect != null &&
            parentRect.rect.height > 1f
        )
        {
            finalPosition = new Vector2(
                timeAnnouncementAnchoredPosition.x,
                -parentRect.rect.height * announcementLowerScreenPercent
            );
        }

        textRect.anchoredPosition = finalPosition;
    }

    
}
