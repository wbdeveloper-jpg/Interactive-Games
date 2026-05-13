using UnityEngine;

/// <summary>
/// Owns the high-level game flow.
/// Important hierarchy rule:
/// - This object should live on an always-active object under the Canvas, not inside Selection/Puzzle/Reveal/GameOver panels.
/// - The constellation reveal screen is allowed to be a child of the puzzle screen.
/// - For that reason this script does NOT use "only one screen active" anymore.
/// </summary>
public class GameFlowController : MonoBehaviour
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
    [Tooltip("When true, Start() forces the birthday selection page to be visible.")]
    public bool showSelectionOnStart = true;

    [Header("Overlay Behaviour")]
    [Tooltip("Keep puzzleScreen active while the constellation reveal is showing. Keep this ON if reveal is under puzzleScreen.")]
    public bool keepPuzzleScreenActiveDuringReveal = true;

    [Tooltip("Keep puzzleScreen active behind GameOver. Turn this ON if GameOver is also under puzzleScreen or you want the solved puzzle visible behind the result.")]
    public bool keepPuzzleScreenActiveDuringGameOver = true;

    private ZodiacPuzzleData currentData;
    private bool gameStarted;
    private bool eventsHooked;

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
        if (showSelectionOnStart)
        {
            ReturnToMainMenu();
        }
    }

    private void OnDestroy()
    {
        UnhookEvents();
    }

    private void HookEvents()
    {
        if (eventsHooked) return;

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
        if (!eventsHooked) return;

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
            Debug.LogError("GameFlowController: Cannot start. ZodiacPuzzleData is null.");
            return;
        }

        currentData = data;
        gameStarted = true;

        ShowPuzzleState();

        if (puzzleBoardController != null)
        {
            puzzleBoardController.BeginPuzzle(data);
        }
        else
        {
            Debug.LogError("GameFlowController: PuzzleBoardController is not assigned.");
        }
    }

    private void HandlePuzzleSolved(float timeTakenSeconds)
    {
        if (!gameStarted || currentData == null) return;

        if (constellationRevealController != null && currentData.constellationPrefab != null)
        {
            ShowRevealState();
            constellationRevealController.Play(currentData, () => ShowGameOver(true, timeTakenSeconds));
        }
        else
        {
            ShowGameOver(true, timeTakenSeconds);
        }
    }

    private void HandlePuzzleFailed(float timeTakenSeconds)
    {
        if (!gameStarted || currentData == null) return;
        ShowGameOver(false, timeTakenSeconds);
    }

    private void ShowGameOver(bool completed, float timeTakenSeconds)
    {
        ShowGameOverState();

        if (gameOverController != null)
        {
            float maxTime = currentData != null ? currentData.timeLimitSeconds : timeTakenSeconds;
            gameOverController.Show(currentData, completed, timeTakenSeconds, maxTime);
        }
        else
        {
            Debug.LogError("GameFlowController: GameOverScreen is not assigned.");
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

    private void ShowSelectionState()
    {
        SetActiveSafe(zodiacSelectionScreen, true);
        SetActiveSafe(puzzleScreen, false);
        SetActiveSafe(constellationRevealScreen, false);
        SetActiveSafe(gameOverScreen, false);
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

    private void ShowGameOverState()
    {
        SetActiveSafe(zodiacSelectionScreen, false);
        SetActiveSafe(puzzleScreen, keepPuzzleScreenActiveDuringGameOver);
        SetActiveSafe(constellationRevealScreen, false);
        SetActiveSafe(gameOverScreen, true);
    }

    private static void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null && obj.activeSelf != active)
        {
            obj.SetActive(active);
        }
    }
}
