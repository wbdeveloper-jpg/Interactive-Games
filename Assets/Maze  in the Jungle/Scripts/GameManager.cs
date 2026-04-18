using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float timeElapsed = 0f;

    private int lastAnnouncedTime = 0;
    public GameObject floatingTextPrefab;
    public Transform prefabParent;
    void Update()
    {
        timeElapsed += Time.deltaTime;

        int seconds = Mathf.FloorToInt(timeElapsed);

        // Every 30 sec announce
        if (seconds % 30 == 0 && seconds != 0 && seconds != lastAnnouncedTime)
        {
            lastAnnouncedTime = seconds;
            Debug.Log(seconds + " seconds passed");

            //  Later: play sound / show text
        }
    }

    public void ReduceTime(float amount)
    {
        timeElapsed -= amount;
        if (timeElapsed < 0) timeElapsed = 0;
    }

    public float GetFinalTime()
    {
        return timeElapsed;
    }
}