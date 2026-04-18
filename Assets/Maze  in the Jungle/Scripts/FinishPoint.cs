using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;

public class FinishPoint : MonoBehaviour
{
    public GameObject controllerPanel;
    public EndPanelController endPanel;

    private bool triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null && !triggered)
        {
            triggered = true;

            player.StopMovement();
            player.enabled = false;

            controllerPanel.SetActive(false);

            StartCoroutine(EndFlow());
        }
    }

    IEnumerator EndFlow()
    {
        AudioManager.Instance.StopBGM();

        yield return endPanel.PlayOutro();

        //android return
        UnityAndroidMediator.Instance.PassDataToAndroid("Game Completed in 43 secs"); 

        // web gl return
        GameLoader.Instance.SendEventToJS("Game Completed in 43 sec", "Maze in the Jungle");
        SceneManager.LoadScene(0);
    }
}