using DG.Tweening;
using RewardSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishPoint : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    public GameObject controllerPanel;
    public EndPanelController endPanel;
    public SimpleLoader simpleloader;
    public GameManager gameManager;
    public float avgTime;

    private bool triggered = false;
    private float timeTaken;
    private GameEvaluationData _evaluationData = new GameEvaluationData();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
        {
            return;
        }

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            return;
        }

        triggered = true;

        player.StopMovement();
        player.enabled = false;

        if (controllerPanel != null)
        {
            controllerPanel.SetActive(false);
        }

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (gameManager != null)
        {
            gameManager.StopTimer();
        }

        BuildEvaluationData();
        StartCoroutine(EndFlow());
    }

    private void BuildEvaluationData()
    {
        if (gameManager == null)
        {
            Debug.LogWarning("FinishPoint could not find GameManager. Evaluation data will use defaults.", this);
            return;
        }

        timeTaken = gameManager.GetFinalTime();
        int finalPoint = gameManager.GetFinalPoint();
        int basePoint = Mathf.Max(1, gameManager.basePoint);

        _evaluationData.timeTaken = timeTaken;
        _evaluationData.timeScore = CalculateTimeScore(timeTaken, avgTime);
        _evaluationData.accuracyScore = Mathf.Clamp01((float)finalPoint / basePoint);
        _evaluationData.mistakeCount = Mathf.Max(0, gameManager.basePoint - finalPoint);

        Debug.Log(
            "User time score: " + _evaluationData.timeScore +
            ", accuracy score: " + _evaluationData.accuracyScore +
            ", final score: " + finalPoint + " / " + gameManager.basePoint,
            this
        );
    }

    private float CalculateTimeScore(float actualTime, float targetAverageTime)
    {
        if (actualTime <= 0f)
        {
            return 1f;
        }

        if (targetAverageTime <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(targetAverageTime / actualTime);
    }

    private IEnumerator EndFlow()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        if (endPanel != null)
        {
            yield return endPanel.PlayOutro();
        }

        if (RewardManager.Instance != null && simpleloader != null)
        {
            RewardManager.Instance.ShowPostGame(simpleloader._skills, _evaluationData);
        }

        string completionMessage = "Game Completed in " + Mathf.RoundToInt(timeTaken) + " secs";

        if (UnityAndroidMediator.Instance != null)
        {
            UnityAndroidMediator.Instance.PassDataToAndroid(completionMessage);
        }

        if (GameLoader.Instance != null)
        {
            GameLoader.Instance.SendEventToJS(completionMessage, "Maze in the Jungle");
        }
    }

    public void OnRewardScreenOpen()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
    }

    public void OnPlayAgain()
    {
        Debug.Log("Play Again", this);
        LoadScene();
    }

    public void OnHome()
    {
        Debug.Log("Main Menu", this);
        MainMenu();

        if (UnityAndroidMediator.Instance != null)
        {
            UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");
        }

        if (GameLoader.Instance != null)
        {
            GameLoader.Instance.SendEventToJS("Game Done", "Maze in the Jungle");
        }
    }

    public void LoadScene()
    {
        DOTween.KillAll(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        DOTween.KillAll(false);
        if (RewardManager.Instance != null)
            RewardManager.Instance.HideAll();

        if (UnityAndroidMediator.Instance != null)
            UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

        //if (GameLoader.Instance != null)
        //    GameLoader.Instance.SendEventToJS("Game Done", "Maze in the Jungle");

        SceneManager.LoadScene("Loader Scene");
    }

    private void OnValidate()
    {
        avgTime = Mathf.Max(0f, avgTime);
    }
}
