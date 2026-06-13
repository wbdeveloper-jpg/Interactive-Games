using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkyFallScreenFlowController : MonoBehaviour
{
    [Header("Gameplay")]
    public SkyFallGameManager gameManager;
    public bool forceTimescaleResetOnStart = true;

    [Header("Loading Screen")]
    public bool showLoadingScreen = true;
    public float loadingDuration = 0.9f;
    public string gameTitle = "SkyFall";
    public TMP_Text loadingGameTitleText;
    public Slider loadingSlider;
    public SkyFallUiPanelAnimator loadingPanel;

    [Header("How To Play")]
    public bool showHowToPlayBeforeFirstGame = true;
    public SkyFallUiPanelAnimator howToPlayPanel;
    public SkyFallImageGuidePanel imageGuidePanel;
    public Button howToPlayStartButton;
    public TMP_Text howToPlayStartButtonText;
    public string firstStartButtonLabel = "START";
    public string continueButtonLabel = "CONTINUE";

    [Header("Pause")]
    public Button pauseButton;
    public SkyFallUiPanelAnimator pausePanel;
    public Button resumeButton;
    public Button pauseHowToPlayButton;
    public Button restartFromPauseButton;
    public bool hidePauseButtonUntilGameStarts = true;

    private bool gameStarted;
    private bool paused;
    private Coroutine bootRoutine;
    private bool startGameRoutineRunning;

    private void Awake()
    {
        AutoFindReferences();
        BindButtons();
        SubscribeToGameManager();
        PrepareInitialState();
    }

    private void Start()
    {
        if (forceTimescaleResetOnStart)
            Time.timeScale = 1f;

        if (bootRoutine != null)
            StopCoroutine(bootRoutine);

        bootRoutine = StartCoroutine(BootFlowRoutine());
    }

    private void OnDestroy()
    {
        UnsubscribeFromGameManager();
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    public void StartGameFromHowToPlay()
    {
        if (startGameRoutineRunning)
            return;

        if (!gameStarted)
        {
            StartCoroutine(StartGameplayAfterHowToPlayRoutine());
            return;
        }

        ResumeGameFromOverlay();
    }

    private IEnumerator StartGameplayAfterHowToPlayRoutine()
    {
        startGameRoutineRunning = true;

        gameStarted = true;
        paused = false;
        Time.timeScale = 1f;

        if (gameManager != null)
            gameManager.SetGameplayInputEnabled(false);

        if (howToPlayPanel != null)
            howToPlayPanel.Hide();

        if (pausePanel != null)
            pausePanel.HideImmediate();

        SetPauseButtonVisible(false);

        // Prevent the START button pointer/touch from leaking into gameplay.
        // This avoids the first accidental wrong catch / -1 issue when gameplay opens.
        yield return null;

        while (Input.GetMouseButton(0) || Input.touchCount > 0)
            yield return null;

        yield return new WaitForSecondsRealtime(0.08f);

        if (gameManager != null)
            gameManager.BeginGame();

        SetPauseButtonVisible(true);
        startGameRoutineRunning = false;
    }

    public void PauseGame()
    {
        if (!gameStarted || paused)
            return;

        paused = true;
        Time.timeScale = 0f;
        SetPauseButtonVisible(false);

        if (pausePanel != null)
            pausePanel.Show();
    }

    public void ResumeGameFromOverlay()
    {
        if (!gameStarted)
            return;

        paused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.Hide();

        if (howToPlayPanel != null)
            howToPlayPanel.Hide();

        SetPauseButtonVisible(true);
    }

    public void OpenHowToPlayFromPause()
    {
        if (imageGuidePanel != null)
            imageGuidePanel.ShowFirstPage();

        if (howToPlayStartButtonText != null)
            howToPlayStartButtonText.text = continueButtonLabel;

        if (pausePanel != null)
            pausePanel.Hide();

        if (howToPlayPanel != null)
            howToPlayPanel.Show();
    }

    public void RestartFromPause()
    {
        paused = false;
        gameStarted = true;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.Hide();

        if (howToPlayPanel != null)
            howToPlayPanel.HideImmediate();

        SetPauseButtonVisible(true);

        if (gameManager != null)
            gameManager.BeginGame();
    }

    private IEnumerator BootFlowRoutine()
    {
        if (loadingGameTitleText != null)
            loadingGameTitleText.text = gameTitle;

        if (howToPlayStartButtonText != null)
            howToPlayStartButtonText.text = firstStartButtonLabel;

        SetPauseButtonVisible(!hidePauseButtonUntilGameStarts);

        if (gameManager != null)
        {
            gameManager.SetGameplayInputEnabled(false);
            yield return gameManager.RunBloomPreGameFlow();
        }

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        if (gameManager != null)
            gameManager.SetGameplayInputEnabled(false);

        if (showLoadingScreen && loadingPanel != null)
        {
            loadingPanel.Show();

            float timer = 0f;
            float duration = Mathf.Max(0.01f, loadingDuration);

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;

                if (loadingSlider != null)
                    loadingSlider.value = Mathf.Clamp01(timer / duration);

                yield return null;
            }

            if (loadingSlider != null)
                loadingSlider.value = 1f;

            loadingPanel.Hide();

            yield return new WaitForSecondsRealtime(0.08f);
        }

        if (showHowToPlayBeforeFirstGame && howToPlayPanel != null)
        {
            if (imageGuidePanel != null)
                imageGuidePanel.ShowFirstPage();

            howToPlayPanel.Show();
        }
        else
        {
            StartGameFromHowToPlay();
        }
    }

    private void PrepareInitialState()
    {
        if (loadingPanel != null)
            loadingPanel.HideImmediate();

        if (howToPlayPanel != null)
            howToPlayPanel.HideImmediate();

        if (pausePanel != null)
            pausePanel.HideImmediate();

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        if (gameManager != null)
            gameManager.SetGameplayInputEnabled(false);
    }

    private void BindButtons()
    {
        if (howToPlayStartButton != null)
        {
            howToPlayStartButton.onClick.RemoveListener(StartGameFromHowToPlay);
            howToPlayStartButton.onClick.AddListener(StartGameFromHowToPlay);
        }

        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(PauseGame);
            pauseButton.onClick.AddListener(PauseGame);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGameFromOverlay);
            resumeButton.onClick.AddListener(ResumeGameFromOverlay);
        }

        if (pauseHowToPlayButton != null)
        {
            pauseHowToPlayButton.onClick.RemoveListener(OpenHowToPlayFromPause);
            pauseHowToPlayButton.onClick.AddListener(OpenHowToPlayFromPause);
        }

        if (restartFromPauseButton != null)
        {
            restartFromPauseButton.onClick.RemoveListener(RestartFromPause);
            restartFromPauseButton.onClick.AddListener(RestartFromPause);
        }
    }

    private void SubscribeToGameManager()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStarted -= HandleGameStarted;
        gameManager.OnGameEnded -= HandleGameEnded;
        gameManager.OnGameStarted += HandleGameStarted;
        gameManager.OnGameEnded += HandleGameEnded;
    }

    private void UnsubscribeFromGameManager()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStarted -= HandleGameStarted;
        gameManager.OnGameEnded -= HandleGameEnded;
    }

    private void HandleGameStarted()
    {
        gameStarted = true;
        paused = false;

        if (gameManager != null)
            gameManager.SetGameplayInputEnabled(true);

        SetPauseButtonVisible(true);
    }

    private void HandleGameEnded()
    {
        paused = false;
        gameStarted = false;
        Time.timeScale = 1f;

        if (gameManager != null)
            gameManager.SetGameplayInputEnabled(false);

        SetPauseButtonVisible(false);

        if (pausePanel != null)
            pausePanel.HideImmediate();

        if (howToPlayPanel != null)
            howToPlayPanel.HideImmediate();
    }

    private void SetPauseButtonVisible(bool visible)
    {
        if (pauseButton != null)
            pauseButton.gameObject.SetActive(visible);
    }

    private void AutoFindReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<SkyFallGameManager>();
    }
}
