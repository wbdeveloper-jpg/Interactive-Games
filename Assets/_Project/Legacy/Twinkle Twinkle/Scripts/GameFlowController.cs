using DG.Tweening;
using RewardSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the high-level game flow.
/// 
/// Production rules:
/// - This object must live on an always-active GameObject.
/// - Do not place this inside SelectionScreen, PuzzleScreen, RevealScreen, or GameOverScreen.
/// - RewardManager integration belongs here, not inside puzzle pieces or constellation scripts.
/// </summary>
public class GameFlowController : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Screens / Panels")]
    public GameObject zodiacSelectionScreen;
    public GameObject puzzleScreen;
    public GameObject constellationRevealScreen;
    public GameObject gameOverScreen;

    [Header("Controllers")]
    public ZodiacSelectionUI zodiacSelectionUI;
    public PuzzleBoardController puzzleBoardController;
    public ConstellationRevealController constellationRevealController;
    public GameOverScreen gameOverController;

    [Header("Startup")]
    [Tooltip("When true, game waits for RewardManager pre-game, then opens birthday selection.")]
    public bool showSelectionOnStart = true;

    [Header("Selection Audio")]
    public bool playSelectionAudio = true;
    [Tooltip("BGM played when selection screen opens. It continues through intro/shuffle until PuzzleBoardController starts gameplay BGM.")]
    public int selectionBgmId = 0;

    [Header("Reward Module")]
    [Tooltip("Turn OFF only for local testing without RewardManager in scene.")]
    [SerializeField] private bool useRewardModule = true;

    [Tooltip("Reward skills passed into RewardManager pre-game and post-game screens.")]
    [SerializeField]
    private List<SkillEntry> rewardSkills = new()
    {
        new SkillEntry(BloomSkillType.Understand, 100f, timeWeight: 0.7f, accuracyWeight: 0.3f),
        new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: 0.5f, accuracyWeight: 0.5f)
    };

    [Header("Overlay Behaviour")]
    [Tooltip("Keep puzzleScreen active while the constellation reveal is showing. Keep this ON if reveal is under puzzleScreen.")]
    public bool keepPuzzleScreenActiveDuringReveal = true;

    [Tooltip("Keep puzzleScreen active behind GameOver/reward. Turn this ON if you want the solved puzzle visible behind final overlay.")]
    public bool keepPuzzleScreenActiveDuringGameOver = true;

    private ZodiacPuzzleData currentData;

    private bool gameStarted;
    private bool eventsHooked;
    private bool bootFlowStarted;
    private bool selectionBgmRequested;

    private bool lastPuzzleCompleted;
    private float lastPuzzleTimeTaken;
    private float lastPuzzleMaxTime;

    private GameEvaluationData rewardEvaluationData = new();

    private void Awake()
    {
        HookEvents();
    }

    private void OnEnable()
    {
        HookEvents();
    }

    private void Start()
    {
        if (!showSelectionOnStart)
            return;

        if (bootFlowStarted)
            return;

        bootFlowStarted = true;
        StartCoroutine(BootGameFlowRoutine());
    }

    private void OnDestroy()
    {
        UnhookEvents();
    }

    private IEnumerator BootGameFlowRoutine()
    {
        HideAllScreens();

        if (useRewardModule && RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPreGame(rewardSkills);

            yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
        }
        else
        {
            Debug.LogWarning("[GameFlowController] RewardManager missing or disabled. Opening selection screen directly.");
        }

        ReturnToMainMenu();
    }

    private void HookEvents()
    {
        if (eventsHooked)
            return;

        if (zodiacSelectionUI != null)
        {
            zodiacSelectionUI.StartRequested += StartGame;
        }

        if (puzzleBoardController != null)
        {
            puzzleBoardController.PuzzleSolved += HandlePuzzleSolved;
            puzzleBoardController.PuzzleFailed += HandlePuzzleFailed;
        }

        if (gameOverController != null)
        {
            gameOverController.PlayAgainRequested += RestartCurrentPuzzle;
            gameOverController.MainMenuRequested += ReturnToMainMenu;
        }

        eventsHooked = true;
    }

    private void UnhookEvents()
    {
        if (!eventsHooked)
            return;

        if (zodiacSelectionUI != null)
        {
            zodiacSelectionUI.StartRequested -= StartGame;
        }

        if (puzzleBoardController != null)
        {
            puzzleBoardController.PuzzleSolved -= HandlePuzzleSolved;
            puzzleBoardController.PuzzleFailed -= HandlePuzzleFailed;
        }

        if (gameOverController != null)
        {
            gameOverController.PlayAgainRequested -= RestartCurrentPuzzle;
            gameOverController.MainMenuRequested -= ReturnToMainMenu;
        }

        eventsHooked = false;
    }

    public void StartGame(ZodiacPuzzleData data)
    {
        if (data == null)
        {
            Debug.LogError("[GameFlowController] Cannot start. ZodiacPuzzleData is null.");
            return;
        }

        currentData = data;
        gameStarted = true;

        // Do not stop selection BGM here. It should continue until gameplay BGM starts after the post-shuffle click prompt.
        selectionBgmRequested = false;

        ResetLastResultData();

        ShowPuzzleState();

        if (puzzleBoardController != null)
        {
            puzzleBoardController.BeginPuzzle(data);
        }
        else
        {
            Debug.LogError("[GameFlowController] PuzzleBoardController is not assigned.");
        }
    }

    private void HandlePuzzleSolved(float timeTakenSeconds)
    {
        if (!gameStarted || currentData == null)
            return;

        StorePuzzleResult(
            completed: true,
            timeTakenSeconds: timeTakenSeconds
        );

        if (constellationRevealController != null && currentData.constellationPrefab != null)
        {
            ShowRevealState();

            // Continue button inside ConstellationRevealController will call this callback.
            constellationRevealController.Play(currentData, ShowRewardPostGame);
        }
        else
        {
            // Fallback: if no reveal exists, go directly to reward post-game.
            ShowRewardPostGame();
        }
    }

    private void HandlePuzzleFailed(float timeTakenSeconds)
    {
        if (!gameStarted || currentData == null)
            return;

        StorePuzzleResult(
            completed: false,
            timeTakenSeconds: timeTakenSeconds
        );

        // Fail has no constellation reveal in this flow.
        ShowRewardPostGame();
    }

    private void StorePuzzleResult(bool completed, float timeTakenSeconds)
    {
        lastPuzzleCompleted = completed;
        lastPuzzleTimeTaken = Mathf.Max(0f, timeTakenSeconds);
        lastPuzzleMaxTime = currentData != null
            ? Mathf.Max(1f, currentData.timeLimitSeconds)
            : Mathf.Max(1f, timeTakenSeconds);
    }

    private void ShowRewardPostGame()
    {
        ShowFinalOverlayState();

        rewardEvaluationData = BuildRewardEvaluationData();

        if (useRewardModule && RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPostGame(rewardSkills, rewardEvaluationData);
            return;
        }

        Debug.LogWarning("[GameFlowController] RewardManager missing or disabled. Showing fallback GameOverScreen.");

        ShowFallbackGameOver();
    }

    private GameEvaluationData BuildRewardEvaluationData()
    {
        float safeMaxTime = Mathf.Max(1f, lastPuzzleMaxTime);
        float safeTimeTaken = Mathf.Clamp(lastPuzzleTimeTaken, 0f, safeMaxTime);

        float accuracyScore = lastPuzzleCompleted ? 1f : 0f;

        float timeScore = lastPuzzleCompleted
            ? Mathf.Clamp01(1f - (safeTimeTaken / safeMaxTime))
            : 0f;

        GameEvaluationData data = new GameEvaluationData
        {
            timeTaken = safeTimeTaken,
            mistakeCount = lastPuzzleCompleted ? 0 : 1,
            accuracyScore = accuracyScore,
            timeScore = timeScore
        };

        Debug.Log(
            "[GameFlowController] Reward Evaluation Data -> " +
            $"Completed: {lastPuzzleCompleted}, " +
            $"TimeTaken: {safeTimeTaken}, " +
            $"MaxTime: {safeMaxTime}, " +
            $"AccuracyScore: {accuracyScore}, " +
            $"TimeScore: {timeScore}"
        );

        return data;
    }

    private void ShowFallbackGameOver()
    {
        ShowGameOverState();

        if (gameOverController != null)
        {
            gameOverController.Show(
                currentData,
                lastPuzzleCompleted,
                lastPuzzleTimeTaken,
                lastPuzzleMaxTime
            );
        }
        else
        {
            Debug.LogError("[GameFlowController] Fallback GameOverScreen is not assigned.");
        }
    }

    public void RestartCurrentPuzzle()
    {
        if (currentData == null)
        {
            ReturnToMainMenu();
            return;
        }

        StartGame(currentData);
    }

    public void ReturnToMainMenu()
    {
        gameStarted = false;
        currentData = null;

        ResetLastResultData();

        if (puzzleBoardController != null)
        {
            puzzleBoardController.StopPuzzleAndClear();
        }

        if (constellationRevealController != null)
        {
            constellationRevealController.Clear();
        }

        if (gameOverController != null)
        {
            gameOverController.Hide();
        }

        ShowSelectionState();
    }

    private void ResetLastResultData()
    {
        lastPuzzleCompleted = false;
        lastPuzzleTimeTaken = 0f;
        lastPuzzleMaxTime = currentData != null
            ? Mathf.Max(1f, currentData.timeLimitSeconds)
            : 1f;

        rewardEvaluationData = new GameEvaluationData();
    }

    private void HideAllScreens()
    {
        SetActiveSafe(zodiacSelectionScreen, false);
        SetActiveSafe(puzzleScreen, false);
        SetActiveSafe(constellationRevealScreen, false);
        SetActiveSafe(gameOverScreen, false);
    }

    private void ShowSelectionState()
    {
        SetActiveSafe(zodiacSelectionScreen, true);
        SetActiveSafe(puzzleScreen, false);
        SetActiveSafe(constellationRevealScreen, false);
        SetActiveSafe(gameOverScreen, false);

        PlaySelectionBgmIfNeeded();
    }

    private void ShowPuzzleState()
    {
        SetActiveSafe(zodiacSelectionScreen, false);
        SetActiveSafe(puzzleScreen, true);
        SetActiveSafe(constellationRevealScreen, false);
        SetActiveSafe(gameOverScreen, false);
    }

    private void ShowRevealState()
    {
        SetActiveSafe(zodiacSelectionScreen, false);
        SetActiveSafe(puzzleScreen, keepPuzzleScreenActiveDuringReveal);
        SetActiveSafe(constellationRevealScreen, true);
        SetActiveSafe(gameOverScreen, false);
    }

    private void ShowFinalOverlayState()
    {
        SetActiveSafe(zodiacSelectionScreen, false);
        SetActiveSafe(puzzleScreen, keepPuzzleScreenActiveDuringGameOver);
        SetActiveSafe(constellationRevealScreen, false);

        // RewardManager has its own post-game UI.
        // Keep old gameOverScreen hidden unless fallback is needed.
        SetActiveSafe(gameOverScreen, false);
    }

    private void ShowGameOverState()
    {
        SetActiveSafe(zodiacSelectionScreen, false);
        SetActiveSafe(puzzleScreen, keepPuzzleScreenActiveDuringGameOver);
        SetActiveSafe(constellationRevealScreen, false);
        SetActiveSafe(gameOverScreen, true);
    }

    private void PlaySelectionBgmIfNeeded()
    {
        if (!playSelectionAudio || selectionBgmId < 0) return;
        if (selectionBgmRequested) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(selectionBgmId);
            selectionBgmRequested = true;
        }
    }

    private static void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
        {
            obj.SetActive(active);
        }
    }
    public void MainMenu()
    {
        DOTween.KillAll(false);
        SceneManager.LoadScene("Loader Scene");
    }
    public void OnPlayAgain()
    {
        ReturnToMainMenu();
    }

    public void OnHome()
    {
        MainMenu();
    }

    public void OnRewardScreenOpen()
    {
        selectionBgmRequested = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }
    }
}