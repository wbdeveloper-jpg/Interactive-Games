using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RewardSystem;

public enum GridAdventureHowToPlayStartupMode
{
    FirstTimeAutomatically,
    EveryGameStartAutomatically,
    ManualButtonOnly
}

public class GridAdventureManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Level")]
    public GridAdventureLevelData levelData;
    public bool startOnAwake = true;

    [Header("Canvas References")]
    public Canvas rootCanvas;
    public GraphicRaycaster graphicRaycaster;
    public RectTransform dragLayer;
    public Transform gridRoot;
    public Transform basketRoot;

    [Header("Item Card Template")]
    [Tooltip("One disabled template card in the basket. Modify this template once and all runtime cards use that look.")]
    public GridAdventureItemCard itemCardTemplate;
    [Tooltip("When true, runtime cards use the display mode set on ItemCardTemplate. This makes ImageOnly templates hide labels reliably.")]
    public bool preferItemTemplateDisplayMode = true;
    public bool useLevelItemDisplayMode = true;
    public GridAdventureItemDisplayMode itemDisplayMode = GridAdventureItemDisplayMode.ImageAndLabel;
    public bool shuffleBasketOrder = true;

    [Header("Basket Visual")]
    [Tooltip("Optional background image under BasketRoot. Assign scene-specific art here without changing code.")]
    public Image basketBackgroundImage;

    [Header("Fonts")]
    [Tooltip("Main display font for title, headers, and buttons.")]
    public TMP_FontAsset primaryFontAsset;
    [Tooltip("Secondary readable font for clues, counters, item labels, and body text.")]
    public TMP_FontAsset secondaryFontAsset;
    public bool applyFontsOnStart = true;
    [Tooltip("If Secondary Font is empty, secondary texts use the Primary Font.")]
    public bool fallbackSecondaryToPrimary = true;

    [Header("Top Bar UI")]
    [Tooltip("Used as static title text. Recommended value: TIME REMAINING")]
    public TextMeshProUGUI levelText;
    [Tooltip("Optional top bar game title. Generated UI uses this to fill the remaining space between timer and controls.")]
    public TextMeshProUGUI topBarGameNameText;
    public TextMeshProUGUI itemCountText;
    public TextMeshProUGUI scoreText;
    [Tooltip("Legacy optional field. Generated UI no longer shows timer seconds text.")]
    public TextMeshProUGUI timerText;
    public Slider timerSlider;
    public Image[] starFillImages;
    public Button pauseButton;
    public Button helpButton;

    [Header("Clue UI")]
    public TextMeshProUGUI clueText;
    public RectTransform clueBanner;
    [Tooltip("Optional inner RectTransform used for clue text bounce. Do not assign the layout-controlled banner root here.")]
    public RectTransform clueMotionRoot;

    [Header("Gameplay Instruction Overlay")]
    public bool showInstructionUntilFirstCorrect = true;
    [TextArea(2, 3)] public string gameplayInstruction = "Tap the glowing cell, then drag the matching image.";
    public GameObject gameplayInstructionOverlayRoot;
    public RectTransform gameplayInstructionMotionRoot;
    public TextMeshProUGUI gameplayInstructionText;

    [Header("Loading Overlay")]
    public bool showLoadingOnStart = true;
    [Min(2f)] public float loadingDuration = 2f;
    public string gameName = "Grid Adventure";
    public GameObject loadingOverlayRoot;
    public RectTransform loadingMainCard;
    public TextMeshProUGUI loadingGameNameText;
    public Slider loadingSlider;

    [Header("Pause Overlay")]
    public GameObject pauseOverlayRoot;
    public RectTransform pauseMainCard;
    public Button resumeButton;
    public Button pauseHowToPlayButton;
    public Button restartButton;

    [Header("Result Overlay")]
    public GameObject resultOverlayRoot;
    public RectTransform resultMainCard;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public Button resultContinueButton;
    public Button resultRestartButton;

    [Header("How To Play Overlay")]
    [Tooltip("Controls only the separate How To Play image panel. The button continues to work in every mode.")]
    public GridAdventureHowToPlayStartupMode howToPlayStartupMode = GridAdventureHowToPlayStartupMode.FirstTimeAutomatically;
    [Tooltip("Base PlayerPrefs key for the How To Play panel. The scene name is appended automatically.")]
    public string howToPlayViewedSaveKey = "GridAdventure.HowToPlay.Viewed";
    [HideInInspector] public bool showHowToPlayOnStart = true;
    public GameObject howToPlayOverlayRoot;
    public RectTransform howToPlayMainCard;
    public Image guideImage;
    public List<Sprite> guideImages = new List<Sprite>();
    public TextMeshProUGUI guideCounterText;
    public Button guidePrevButton;
    public Button guideNextButton;
    public Button guideStartButton;

    [Header("Gameplay Settings")]
    public int scorePerCorrect = 10;
    public bool useTimer = true;
    public float roundTimeSeconds = 90f;
    public bool allowManualCellSelection = true;

    [Header("Bloom Reward System")]
    [Tooltip("RewardManager must already exist in LoadingScene and persist with DontDestroyOnLoad. Do not place RewardManager in this game scene.")]
    public bool useBloomRewardSystem = true;
    public string homeSceneName = "Loader Scene";

    [Header("Feedback")]
    public Color clueFlashColor = new Color(1f, 0.88f, 0.35f, 1f);
    public float correctAdvanceDelay = 0.48f;

    [Header("Audio")]
    public GridAdventureAudioManager audioManager;
    [Tooltip("Optional manager-level override. Leave empty to use the clip assigned on GridAdventureAudioManager.")]
    public AudioClip backgroundMusicClip;
    public bool playBackgroundMusicOnStart = true;
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.45f;

    [Header("First-Time Interactive Tutorial")]
    [Tooltip("Installed automatically by Tools > Grid Adventure > Install or Upgrade First-Time Tutorial.")]
    public GridAdventureFirstTimeTutorialController firstTimeTutorialController;

    public Canvas RootCanvas { get { return rootCanvas; } }
    public RectTransform DragLayer { get { return dragLayer; } }
    public bool CanDragCards { get { return isGameplayActive && !GameplayBlocked; } }
    public bool CanSelectCells { get { return isGameplayActive && !GameplayBlocked; } }
    public bool IsTutorialHeld { get { return isTutorialHold; } }

    private readonly List<SkillEntry> bloomSkills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Remember, 75f, timeWeight: 0.25f, accuracyWeight: 0.75f),
        new SkillEntry(BloomSkillType.Understand, 75f, timeWeight: 0.35f, accuracyWeight: 0.65f),
        new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: 0.4f, accuracyWeight: 0.6f)
    };

    private class RoundItem
    {
        public GridAdventureItemData data;
        public string coordinate;
    }

    private readonly Dictionary<string, GridAdventureCell> cellsByCoordinate = new Dictionary<string, GridAdventureCell>();
    private readonly Dictionary<string, GridAdventureItemCard> cardsByItemId = new Dictionary<string, GridAdventureItemCard>();
    private readonly Dictionary<string, RoundItem> roundItemsByCoordinate = new Dictionary<string, RoundItem>();
    private readonly HashSet<string> completedCoordinates = new HashSet<string>();
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>(16);
    private readonly List<RoundItem> roundItems = new List<RoundItem>();
    private readonly List<GridAdventureItemCard> runtimeCards = new List<GridAdventureItemCard>();

    private GridAdventureCell activeCell;
    private RoundItem activeRoundItem;
    private int completedCount;
    private int currentScore;
    private int currentGuideIndex;
    private int correctCount;
    private int mistakeCount;
    private float timerRemaining;
    private float gameplayStartTime;
    private bool isPauseOpen;
    private bool isGuideOpen;
    private bool guideOpenedFromPause;
    private bool isLoading;
    private bool isBloomPreGameOpen;
    private bool isGameplayActive;
    private bool isGameComplete;
    private bool isTutorialHold;
    private Sequence clueSequence;
    private Sequence loadingSequence;
    private Sequence gameplayInstructionSequence;
    private Coroutine startFlowCoroutine;

    private int RoundTotal
    {
        get { return roundItems.Count; }
    }

    private bool GameplayBlocked
    {
        get { return isBloomPreGameOpen || isPauseOpen || isGuideOpen || isLoading || isGameComplete || isTutorialHold; }
    }

    private void Awake()
    {
        CacheMissingReferences();
        WireButtons();
    }

    private void Start()
    {
        if (startOnAwake)
            StartLevel();
    }

    private void Update()
    {
        if (!isGameplayActive || !useTimer || GameplayBlocked) return;

        timerRemaining -= Time.deltaTime;
        if (timerRemaining <= 0f)
        {
            timerRemaining = 0f;
            UpdateTimerUI();
            ShowResult(false);
            return;
        }

        UpdateTimerUI();
    }

    public void StartLevel()
    {
        CacheMissingReferences();
        WireButtons();

        if (levelData == null)
        {
            Debug.LogWarning("GridAdventureManager needs a GridAdventureLevelData asset.", this);
            return;
        }

        if (applyFontsOnStart)
            ApplySceneFonts();

        if (startFlowCoroutine != null)
        {
            StopCoroutine(startFlowCoroutine);
            startFlowCoroutine = null;
        }

        DOTween.Kill(this);
        KillSequences();
        if (firstTimeTutorialController != null)
            firstTimeTutorialController.StopTutorialWithoutCompleting();
        ClearRuntimeCards();

        isPauseOpen = false;
        isGuideOpen = false;
        guideOpenedFromPause = false;
        isLoading = false;
        isBloomPreGameOpen = false;
        isGameplayActive = false;
        isGameComplete = false;
        isTutorialHold = false;
        completedCount = 0;
        currentScore = 0;
        correctCount = 0;
        mistakeCount = 0;
        currentGuideIndex = 0;
        gameplayStartTime = 0f;
        timerRemaining = Mathf.Max(1f, roundTimeSeconds);
        completedCoordinates.Clear();
        cellsByCoordinate.Clear();
        cardsByItemId.Clear();
        roundItems.Clear();
        roundItemsByCoordinate.Clear();
        activeCell = null;
        activeRoundItem = null;

        SetOverlayInstant(loadingOverlayRoot, false);
        SetOverlayInstant(pauseOverlayRoot, false);
        SetOverlayInstant(resultOverlayRoot, false);
        SetOverlayInstant(howToPlayOverlayRoot, false);
        SetGameplayInstructionVisible(false, true);

        ConfigureBackgroundMusic();
        BuildRoundItems();
        SetupCells();
        SetupCards();
        if (applyFontsOnStart)
            ApplySceneFonts();
        UpdateTopUI(true);
        RefreshGameplayLayout();
        SelectFirstUncompletedCell(false);
        BeginStartOverlayFlow();
    }

    public void SelectCell(GridAdventureCell cell, bool playClick, bool animated = true)
    {
        if (cell == null || GameplayBlocked) return;
        if (!allowManualCellSelection && activeCell != null) return;
        if (cell.IsCompleted || completedCoordinates.Contains(cell.coordinate)) return;

        RoundItem item = GetRoundItemForCoordinate(cell.coordinate);
        if (item == null) return;

        if (playClick) PlayClick();

        if (activeCell != null && activeCell != cell)
            activeCell.SetActiveVisual(false);

        activeCell = cell;
        activeRoundItem = item;

        GridAdventureCell selectedCell = activeCell;
        if (animated)
        {
            selectedCell.PlayNextCellPop(delegate
            {
                if (selectedCell == activeCell)
                    selectedCell.SetActiveVisual(true);
            });
        }
        else
        {
            selectedCell.SetActiveVisual(true);
        }

        UpdateClueBanner(animated);
    }

    public GridAdventureCell GetCellUnderPointer(PointerEventData eventData)
    {
        if (graphicRaycaster == null || eventData == null) return null;

        raycastResults.Clear();
        graphicRaycaster.Raycast(eventData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
            GridAdventureCell cell = raycastResults[i].gameObject.GetComponentInParent<GridAdventureCell>();
            if (cell != null) return cell;
        }

        return null;
    }

    public void ResolveDrop(GridAdventureItemCard card, GridAdventureCell targetCell, RectTransform dragClone)
    {
        if (card == null || activeCell == null || activeRoundItem == null || GameplayBlocked)
        {
            if (card != null) card.PlayWrongReturn(dragClone);
            return;
        }

        bool droppedOnActiveCell = targetCell != null && targetCell == activeCell;
        bool correctItem = activeRoundItem.data != null && string.Equals(card.itemId, activeRoundItem.data.itemId, System.StringComparison.OrdinalIgnoreCase);

        if (!droppedOnActiveCell || !correctItem)
        {
            RegisterMistake();
            card.PlayWrongReturn(dragClone);
            return;
        }

        PlayCorrect(card, activeCell, dragClone);
    }

    public void PlayClick()
    {
        if (audioManager != null) audioManager.PlaySFX("click");
    }

    public void PlayWrong()
    {
        if (audioManager != null) audioManager.PlaySFX("wrong_slide");
    }

    public void UseClue()
    {
        PlayClick();
        if (activeRoundItem == null || activeRoundItem.data == null || GameplayBlocked) return;

        GridAdventureItemCard card;
        if (cardsByItemId.TryGetValue(activeRoundItem.data.itemId, out card) && card != null)
        {
            if (audioManager != null) audioManager.PlaySFX("clue");
            card.PlayClue(clueFlashColor);
        }
    }

    public void TogglePausePanel()
    {
        if (isGameComplete || isGuideOpen || isLoading || isBloomPreGameOpen || isTutorialHold) return;

        if (isPauseOpen) ClosePausePanel();
        else OpenPausePanel();
    }

    public void OpenPausePanel()
    {
        if (pauseOverlayRoot == null || isGameComplete || isGuideOpen || isLoading || isBloomPreGameOpen || isTutorialHold) return;

        isPauseOpen = true;
        if (audioManager != null) audioManager.PlaySFX("pause_open");
        ShowOverlay(pauseOverlayRoot, pauseMainCard, true);
    }

    public void ClosePausePanel()
    {
        if (pauseOverlayRoot == null) return;

        isPauseOpen = false;
        ShowOverlay(pauseOverlayRoot, pauseMainCard, false);
    }

    public void RestartLevel()
    {
        PlayClick();
        StartLevel();
    }

    public void OpenHowToPlayFromPause()
    {
        if (howToPlayOverlayRoot == null || isGameComplete || isLoading || isBloomPreGameOpen || isTutorialHold) return;

        PlayClick();
        guideOpenedFromPause = isPauseOpen;
        OpenHowToPlayPanel(false);
    }

    public void OpenHowToPlayPanel(bool instant = false)
    {
        if (!isPauseOpen)
            guideOpenedFromPause = false;

        if (howToPlayOverlayRoot == null)
        {
            isGuideOpen = false;
            if (!isPauseOpen)
                ContinueAfterHowToPlay();
            return;
        }

        isGuideOpen = true;
        currentGuideIndex = Mathf.Clamp(currentGuideIndex, 0, Mathf.Max(0, guideImages.Count - 1));
        UpdateGuideVisual();

        if (instant) SetOverlayInstant(howToPlayOverlayRoot, true);
        else ShowOverlay(howToPlayOverlayRoot, howToPlayMainCard, true);
    }

    public void CloseHowToPlayPanel()
    {
        bool returnToPause = guideOpenedFromPause && isPauseOpen;
        guideOpenedFromPause = false;
        isGuideOpen = false;
        PlayerPrefs.SetInt(GetHowToPlayViewedKey(), 1);
        PlayerPrefs.Save();
        ShowOverlay(howToPlayOverlayRoot, howToPlayMainCard, false);

        DOVirtual.DelayedCall(0.15f, delegate
        {
            RefreshGameplayLayout();
            if (!returnToPause)
                ContinueAfterHowToPlay();
        }).SetUpdate(true).SetId(this);

        PlayClick();
    }

    public void ShowNextGuideImage()
    {
        PlayClick();
        if (guideImages == null || guideImages.Count == 0) return;

        currentGuideIndex = Mathf.Clamp(currentGuideIndex + 1, 0, guideImages.Count - 1);
        UpdateGuideVisual(true);
    }

    public void ShowPreviousGuideImage()
    {
        PlayClick();
        if (guideImages == null || guideImages.Count == 0) return;

        currentGuideIndex = Mathf.Clamp(currentGuideIndex - 1, 0, guideImages.Count - 1);
        UpdateGuideVisual(true);
    }

    public void ContinueToBloomPostPanel()
    {
        PlayClick();

        if (!isGameComplete)
            ShowResult(false);

        ShowOverlay(resultOverlayRoot, resultMainCard, false);

        if (!useBloomRewardSystem)
        {
            Debug.Log("Bloom Reward System is disabled on GridAdventureManager.", this);
            return;
        }

        RewardManager rewardManager = RewardManager.Instance;
        if (rewardManager == null)
        {
            Debug.LogWarning("Bloom RewardManager.Instance was not found. Keep RewardManager only in LoadingScene with DontDestroyOnLoad.", this);
            return;
        }

        GameEvaluationData eval = BuildBloomEvaluationData();
        rewardManager.ShowPostGame(bloomSkills, eval);
    }

    public void OnPlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHome()
    {
        if (RewardManager.Instance != null)
            RewardManager.Instance.HideAll();

        if (UnityAndroidMediator.Instance != null)
            UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

        //if (GameLoader.Instance != null)
        //    GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");
        SceneManager.LoadScene(homeSceneName);
    }

    public void OnRewardScreenOpen()
    {
        if (audioManager != null)
            audioManager.StopBackgroundMusic();
    }

    [ContextMenu("Apply Grid Adventure Fonts")]
    public void ApplySceneFonts()
    {
        Transform searchRoot = rootCanvas != null ? rootCanvas.transform : transform.root;
        if (searchRoot == null) return;

        TMP_Text[] allTexts = searchRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text == null) continue;

            GridAdventureTextFontRole role = text.GetComponent<GridAdventureTextFontRole>();
            GridAdventureFontRole resolvedRole = role != null ? role.fontRole : GuessFontRole(text);
            TMP_FontAsset targetFont = ResolveFont(resolvedRole);
            if (targetFont == null) continue;

            text.font = targetFont;
        }
    }

    private TMP_FontAsset ResolveFont(GridAdventureFontRole role)
    {
        if (role == GridAdventureFontRole.Primary)
        {
            if (primaryFontAsset != null) return primaryFontAsset;
            return secondaryFontAsset;
        }

        if (secondaryFontAsset != null) return secondaryFontAsset;
        return fallbackSecondaryToPrimary ? primaryFontAsset : null;
    }

    private GridAdventureFontRole GuessFontRole(TMP_Text text)
    {
        string lowerName = text.gameObject.name.ToLowerInvariant();
        if (lowerName.Contains("title") || lowerName.Contains("button") || lowerName.Contains("header") || lowerName.Contains("game name"))
            return GridAdventureFontRole.Primary;

        return GridAdventureFontRole.Secondary;
    }

    private void BeginStartOverlayFlow()
    {
        if (startFlowCoroutine != null)
            StopCoroutine(startFlowCoroutine);

        startFlowCoroutine = StartCoroutine(StartOverlayFlowRoutine());
    }

    private IEnumerator StartOverlayFlowRoutine()
    {
        if (useBloomRewardSystem)
        {
            RewardManager rewardManager = RewardManager.Instance;
            if (rewardManager != null)
            {
                isBloomPreGameOpen = true;
                rewardManager.ShowPreGame(bloomSkills);
                yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
                isBloomPreGameOpen = false;
            }
            else
            {
                Debug.LogWarning("Bloom RewardManager.Instance was not found. Skipping Bloom pre-game preview.", this);
            }
        }

        BeginLocalIntroFlow();
        startFlowCoroutine = null;
    }

    private void BeginLocalIntroFlow()
    {
        if (showLoadingOnStart && loadingOverlayRoot != null)
        {
            OpenLoadingPanel();
            return;
        }

        OpenGuideIfNeeded();
    }

    private void OpenLoadingPanel()
    {
        isLoading = true;
        if (loadingGameNameText != null) loadingGameNameText.text = gameName;
        if (loadingSlider != null) loadingSlider.value = 0f;

        ShowOverlay(loadingOverlayRoot, loadingMainCard, true);

        if (loadingSequence != null && loadingSequence.IsActive())
            loadingSequence.Kill();

        float duration = Mathf.Max(2f, loadingDuration);
        loadingSequence = DOTween.Sequence().SetUpdate(true).SetId(this);
        loadingSequence.Append(DOVirtual.Float(0f, 1f, duration, delegate(float value)
        {
            if (loadingSlider != null) loadingSlider.value = value;
        }).SetEase(Ease.OutQuad));
        loadingSequence.OnComplete(delegate
        {
            isLoading = false;
            ShowOverlay(loadingOverlayRoot, loadingMainCard, false);
            DOVirtual.DelayedCall(0.14f, OpenGuideIfNeeded).SetUpdate(true).SetId(this);
        });
    }

    private void OpenGuideIfNeeded()
    {
        if (ShouldOpenHowToPlayAutomatically())
            OpenHowToPlayPanel(false);
        else
        {
            RefreshGameplayLayout();
            ContinueAfterHowToPlay();
        }
    }

    private void ContinueAfterHowToPlay()
    {
        CacheMissingReferences();

        if (firstTimeTutorialController != null &&
            firstTimeTutorialController.TryStartTutorial(BeginGameplay))
            return;

        SetTutorialHold(false);
        BeginGameplay();
    }

    private void BeginGameplay()
    {
        if (isGameplayActive || isGameComplete || isTutorialHold) return;

        isGameplayActive = true;
        gameplayStartTime = Time.time;
        timerRemaining = Mathf.Max(1f, roundTimeSeconds);
        UpdateTopUI(true);
        ShowGameplayInstructionIfNeeded();
    }

    public void SetTutorialHold(bool held)
    {
        isTutorialHold = held;
    }

    public bool TryGetTutorialPracticeQuestion(
        out GridAdventureItemData question,
        out GridAdventureCell targetCell,
        out GridAdventureItemCard correctCard)
    {
        question = activeRoundItem != null ? activeRoundItem.data : null;
        targetCell = activeCell;
        correctCard = null;

        if (question == null || targetCell == null || string.IsNullOrWhiteSpace(question.itemId))
            return false;

        cardsByItemId.TryGetValue(question.itemId, out correctCard);
        return correctCard != null;
    }

    public void GetRuntimeCardsForTutorial(List<GridAdventureItemCard> destination)
    {
        if (destination == null) return;

        destination.Clear();
        for (int i = 0; i < runtimeCards.Count; i++)
        {
            GridAdventureItemCard card = runtimeCards[i];
            if (card != null && card.gameObject.activeInHierarchy && !card.IsSolved)
                destination.Add(card);
        }
    }

    [ContextMenu("Reset How To Play First-Time Status")]
    public void ResetHowToPlayViewedStatus()
    {
        PlayerPrefs.DeleteKey(GetHowToPlayViewedKey());
        PlayerPrefs.Save();
    }

    private bool ShouldOpenHowToPlayAutomatically()
    {
        switch (howToPlayStartupMode)
        {
            case GridAdventureHowToPlayStartupMode.EveryGameStartAutomatically:
                return true;

            case GridAdventureHowToPlayStartupMode.ManualButtonOnly:
                return false;

            default:
                return PlayerPrefs.GetInt(GetHowToPlayViewedKey(), 0) == 0;
        }
    }

    private string GetHowToPlayViewedKey()
    {
        string baseKey = string.IsNullOrWhiteSpace(howToPlayViewedSaveKey)
            ? "GridAdventure.HowToPlay.Viewed"
            : howToPlayViewedSaveKey.Trim();
        return baseKey + "." + SceneManager.GetActiveScene().name;
    }

    private void PlayCorrect(GridAdventureItemCard card, GridAdventureCell cell, RectTransform dragClone)
    {
        if (audioManager != null) audioManager.PlaySFX("correct_snap");

        completedCoordinates.Add(cell.coordinate);
        completedCount++;
        correctCount++;
        currentScore += scorePerCorrect;

        if (completedCount == 1)
            SetGameplayInstructionVisible(false, false);

        cell.PlayCorrectFlash();
        cell.SetCompletedVisual();
        card.SnapCorrectlyInto(cell.placedItemRoot, dragClone);

        UpdateTopUI(false);
        RefreshBasketResponsiveGrid();

        Sequence afterCorrect = DOTween.Sequence().SetId(this);
        afterCorrect.AppendInterval(correctAdvanceDelay);
        afterCorrect.OnComplete(delegate
        {
            if (completedCount >= RoundTotal)
                ShowResult(true);
            else
                SelectFirstUncompletedCell(true);
        });
    }

    private void RegisterMistake()
    {
        mistakeCount++;
    }

    private void SelectFirstUncompletedCell(bool animated)
    {
        GridAdventureCell next = GetNextUncompletedCell();
        if (next == null)
        {
            ShowResult(true);
            return;
        }

        SelectCell(next, false, animated);
    }

    private GridAdventureCell GetNextUncompletedCell()
    {
        int columns = Mathf.Max(1, levelData.columns);
        int rows = Mathf.Max(1, levelData.rows);

        for (int columnIndex = 0; columnIndex < columns; columnIndex++)
        {
            char columnLetter = (char)('A' + columnIndex);
            for (int rowIndex = 1; rowIndex <= rows; rowIndex++)
            {
                string coordinate = string.Format("{0}{1}", columnLetter, rowIndex);
                if (completedCoordinates.Contains(coordinate)) continue;
                if (GetRoundItemForCoordinate(coordinate) == null) continue;

                GridAdventureCell cell;
                if (cellsByCoordinate.TryGetValue(coordinate, out cell))
                    return cell;
            }
        }

        return null;
    }

    private RoundItem GetRoundItemForCoordinate(string coordinate)
    {
        if (string.IsNullOrEmpty(coordinate)) return null;

        RoundItem item;
        return roundItemsByCoordinate.TryGetValue(coordinate, out item) ? item : null;
    }

    private void UpdateClueBanner(bool animated)
    {
        if (clueText == null || activeCell == null) return;

        string clue = activeRoundItem == null || activeRoundItem.data == null
            ? "No clue set."
            : activeRoundItem.data.clueText;

        string newText = string.Format("CLUE FOR {0}: {1}", activeCell.coordinate, clue);

        clueText.DOKill();
        clueText.rectTransform.DOKill();
        if (clueMotionRoot != null) clueMotionRoot.DOKill();
        if (clueSequence != null && clueSequence.IsActive()) clueSequence.Kill();

        ResetClueMotionVisual();

        if (!animated)
        {
            clueText.alpha = 1f;
            clueText.text = newText;
            return;
        }

        RectTransform motionRoot = clueMotionRoot != null ? clueMotionRoot : clueText.rectTransform;

        clueSequence = DOTween.Sequence().SetId(this);
        clueSequence.Append(clueText.DOFade(0f, 0.12f));
        clueSequence.AppendCallback(delegate { clueText.text = newText; });
        clueSequence.Append(clueText.DOFade(1f, 0.18f));

        if (motionRoot != null)
            clueSequence.Join(motionRoot.DOPunchScale(Vector3.one * 0.035f, 0.22f, 8, 0.7f).SetEase(Ease.OutQuad));
    }

    private void ResetClueMotionVisual()
    {
        if (clueMotionRoot != null)
            clueMotionRoot.localScale = Vector3.one;

        if (clueText != null)
            clueText.rectTransform.localScale = Vector3.one;
    }

    private void RefreshGameplayLayout()
    {
        if (rootCanvas == null) return;

        GridAdventureCanvasResizeRefresher resizeRefresher = rootCanvas.GetComponent<GridAdventureCanvasResizeRefresher>();
        if (resizeRefresher != null)
        {
            resizeRefresher.ForceRefreshNow();
            return;
        }

        Canvas.ForceUpdateCanvases();
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);

        GridAdventureMainScreenLayout[] mainLayouts = rootCanvas.GetComponentsInChildren<GridAdventureMainScreenLayout>(true);
        for (int i = 0; i < mainLayouts.Length; i++)
            mainLayouts[i].ForceRefresh();

        GridAdventureCenterSquareLayout[] centerLayouts = rootCanvas.GetComponentsInChildren<GridAdventureCenterSquareLayout>(true);
        for (int i = 0; i < centerLayouts.Length; i++)
            centerLayouts[i].ForceRefresh();

        RefreshBasketResponsiveGrid();
        Canvas.ForceUpdateCanvases();
    }

    private void ShowGameplayInstructionIfNeeded()
    {
        if (!showInstructionUntilFirstCorrect || completedCount > 0 || gameplayInstructionOverlayRoot == null)
            return;

        if (gameplayInstructionText != null)
            gameplayInstructionText.text = gameplayInstruction;

        SetGameplayInstructionVisible(true, true);

        RectTransform target = gameplayInstructionMotionRoot != null
            ? gameplayInstructionMotionRoot
            : gameplayInstructionOverlayRoot.transform as RectTransform;

        if (target == null) return;

        target.DOKill();
        target.localScale = Vector3.one;

        if (gameplayInstructionSequence != null && gameplayInstructionSequence.IsActive())
            gameplayInstructionSequence.Kill();

        gameplayInstructionSequence = DOTween.Sequence().SetId(this);
        gameplayInstructionSequence.Append(target.DOScale(1.035f, 0.78f).SetEase(Ease.InOutSine));
        gameplayInstructionSequence.Append(target.DOScale(1f, 0.78f).SetEase(Ease.InOutSine));
        gameplayInstructionSequence.SetLoops(-1);
    }

    private void SetGameplayInstructionVisible(bool visible, bool instant)
    {
        if (gameplayInstructionOverlayRoot == null) return;

        if (gameplayInstructionSequence != null && gameplayInstructionSequence.IsActive())
            gameplayInstructionSequence.Kill();
        gameplayInstructionSequence = null;

        RectTransform target = gameplayInstructionMotionRoot != null
            ? gameplayInstructionMotionRoot
            : gameplayInstructionOverlayRoot.transform as RectTransform;

        if (target != null)
        {
            target.DOKill();
            target.localScale = Vector3.one;
        }

        CanvasGroup group = gameplayInstructionOverlayRoot.GetComponent<CanvasGroup>();
        if (group == null)
            group = gameplayInstructionOverlayRoot.AddComponent<CanvasGroup>();

        group.interactable = false;
        group.blocksRaycasts = false;

        if (visible)
        {
            gameplayInstructionOverlayRoot.SetActive(true);
            group.alpha = 1f;
            return;
        }

        if (instant)
        {
            group.alpha = 0f;
            gameplayInstructionOverlayRoot.SetActive(false);
            return;
        }

        group.DOKill();
        group.DOFade(0f, 0.16f).SetEase(Ease.OutQuad).OnComplete(delegate
        {
            if (gameplayInstructionOverlayRoot != null)
                gameplayInstructionOverlayRoot.SetActive(false);
        });
    }

    private void UpdateTopUI(bool instant)
    {
        if (levelText != null) levelText.text = "TIME REMAINING";
        if (topBarGameNameText != null) topBarGameNameText.text = gameName;
        if (itemCountText != null) itemCountText.text = string.Format("{0} / {1}", completedCount, RoundTotal);
        if (scoreText != null) scoreText.text = string.Format("Score: {0}", currentScore);
        UpdateTimerUI();

        float progress = RoundTotal <= 0 ? 0f : (float)completedCount / RoundTotal;
        UpdateStars(progress, instant);

        if (!instant && itemCountText != null)
        {
            itemCountText.rectTransform.DOKill();
            itemCountText.rectTransform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 8, 0.7f);
        }
    }

    private void UpdateStars(float progress, bool instant)
    {
        if (starFillImages == null) return;

        for (int i = 0; i < starFillImages.Length; i++)
        {
            Image fill = starFillImages[i];
            if (fill == null) continue;

            float targetFill = Mathf.Clamp01(progress * starFillImages.Length - i);
            fill.DOKill();
            if (instant) fill.fillAmount = targetFill;
            else fill.DOFillAmount(targetFill, 0.35f).SetEase(Ease.OutQuad);
        }
    }

    private void UpdateTimerUI()
    {
        float safeRoundTime = Mathf.Max(1f, roundTimeSeconds);
        float normalized = Mathf.Clamp01(timerRemaining / safeRoundTime);

        if (timerSlider != null)
            timerSlider.value = useTimer ? normalized : 1f;

        if (timerText != null)
        {
            timerText.text = string.Empty;
            timerText.gameObject.SetActive(false);
        }
    }

    private void ShowResult(bool success)
    {
        isGameComplete = true;
        isGameplayActive = false;
        isPauseOpen = false;
        isGuideOpen = false;
        guideOpenedFromPause = false;
        isLoading = false;
        isBloomPreGameOpen = false;

        if (audioManager != null && success)
            audioManager.PlaySFX("result_win");

        SetGameplayInstructionVisible(false, false);

        if (activeCell != null)
            activeCell.SetActiveVisual(false);

        if (resultTitleText != null)
            resultTitleText.text = success ? "Great Job!" : "Time Up!";

        if (resultScoreText != null)
            resultScoreText.text = string.Format("Score: {0}\nItems: {1} / {2}\nMistakes: {3}", currentScore, completedCount, RoundTotal, mistakeCount);

        ShowOverlay(resultOverlayRoot, resultMainCard, true);
    }

    private GameEvaluationData BuildBloomEvaluationData()
    {
        float expectedMaxTime = Mathf.Max(1f, roundTimeSeconds);
        float timeTaken = gameplayStartTime > 0f ? Mathf.Max(0f, Time.time - gameplayStartTime) : expectedMaxTime;
        float timeScore = Mathf.Clamp01(1f - (timeTaken / expectedMaxTime));
        float accuracyScore = RoundTotal > 0 ? Mathf.Clamp01((float)correctCount / RoundTotal) : 0f;

        return new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = accuracyScore,
            mistakeCount = mistakeCount,
            timeTaken = timeTaken
        };
    }

    private void BuildRoundItems()
    {
        roundItems.Clear();
        roundItemsByCoordinate.Clear();

        if (levelData == null || levelData.items == null) return;

        List<GridAdventureItemData> validItems = new List<GridAdventureItemData>();
        for (int i = 0; i < levelData.items.Count; i++)
        {
            GridAdventureItemData item = levelData.items[i];
            if (item == null) continue;
            if (string.IsNullOrWhiteSpace(item.itemId)) continue;
            validItems.Add(item);
        }

        int gridCapacity = Mathf.Max(1, levelData.columns) * Mathf.Max(1, levelData.rows);
        int roundCount = Mathf.Min(gridCapacity, validItems.Count);
        if (roundCount <= 0) return;

        if (validItems.Count > roundCount && levelData.randomizeWhenMoreThanGridCells)
            ShuffleItems(validItems, levelData.randomSeed);

        List<string> coordinates = BuildCoordinateList(levelData.columns, levelData.rows);
        for (int i = 0; i < roundCount && i < coordinates.Count; i++)
        {
            RoundItem roundItem = new RoundItem();
            roundItem.data = validItems[i];
            roundItem.coordinate = coordinates[i];
            roundItems.Add(roundItem);
            roundItemsByCoordinate[roundItem.coordinate] = roundItem;
        }
    }

    private List<string> BuildCoordinateList(int columnCount, int rowCount)
    {
        List<string> coordinates = new List<string>();
        int safeColumns = Mathf.Max(1, columnCount);
        int safeRows = Mathf.Max(1, rowCount);

        for (int columnIndex = 0; columnIndex < safeColumns; columnIndex++)
        {
            char columnLetter = (char)('A' + columnIndex);
            for (int rowIndex = 1; rowIndex <= safeRows; rowIndex++)
                coordinates.Add(string.Format("{0}{1}", columnLetter, rowIndex));
        }

        return coordinates;
    }

    private void ShuffleItems<T>(List<T> list, int seed)
    {
        if (list == null || list.Count <= 1) return;

        if (seed > 0)
            UnityEngine.Random.InitState(seed);

        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[swapIndex];
            list[swapIndex] = temp;
        }
    }

    private void SetupCells()
    {
        if (gridRoot == null) return;

        GridAdventureCell[] cells = gridRoot.GetComponentsInChildren<GridAdventureCell>(true);
        for (int i = 0; i < cells.Length; i++)
        {
            GridAdventureCell cell = cells[i];
            cell.Init(this);
            cellsByCoordinate[cell.coordinate] = cell;
        }
    }

    private GridAdventureItemDisplayMode ResolveRuntimeItemDisplayMode()
    {
        if (preferItemTemplateDisplayMode && itemCardTemplate != null)
            return itemCardTemplate.displayMode;

        if (useLevelItemDisplayMode && levelData != null)
            return levelData.itemDisplayMode;

        return itemDisplayMode;
    }

    private void SetupCards()
    {
        if (basketRoot == null) return;

        ResolveItemTemplate();
        if (itemCardTemplate == null)
        {
            Debug.LogWarning("GridAdventureManager needs an Item Card Template assigned.", this);
            return;
        }

        itemCardTemplate.MarkAsTemplate(true);
        itemCardTemplate.gameObject.SetActive(false);
        DisableLegacyStaticCards();

        List<RoundItem> basketItems = new List<RoundItem>(roundItems);
        if (shuffleBasketOrder && basketItems.Count > 1)
            ShuffleItems(basketItems, levelData != null ? levelData.randomSeed : 0);

        GridAdventureItemDisplayMode mode = ResolveRuntimeItemDisplayMode();

        for (int i = 0; i < basketItems.Count; i++)
        {
            RoundItem roundItem = basketItems[i];
            if (roundItem == null || roundItem.data == null) continue;

            GridAdventureItemCard card = CreateRuntimeCardFromTemplate(i);
            card.Setup(this, roundItem.data, mode);
            runtimeCards.Add(card);

            if (!string.IsNullOrEmpty(roundItem.data.itemId))
                cardsByItemId[roundItem.data.itemId] = card;
        }

        RefreshBasketResponsiveGrid();
    }

    private void DisableLegacyStaticCards()
    {
        if (basketRoot == null || itemCardTemplate == null) return;

        GridAdventureItemCard[] cards = basketRoot.GetComponentsInChildren<GridAdventureItemCard>(true);
        for (int i = 0; i < cards.Length; i++)
        {
            GridAdventureItemCard card = cards[i];
            if (card == null || card == itemCardTemplate) continue;
            if (runtimeCards.Contains(card)) continue;
            card.gameObject.SetActive(false);
        }
    }

    private GridAdventureItemCard CreateRuntimeCardFromTemplate(int index)
    {
        GameObject cardObject = Instantiate(itemCardTemplate.gameObject, basketRoot, false);
        cardObject.name = "Item Card " + (index + 1);
        cardObject.SetActive(true);

        LayoutElement layoutElement = cardObject.GetComponent<LayoutElement>();
        if (layoutElement != null) layoutElement.ignoreLayout = false;

        GridAdventureItemCard card = cardObject.GetComponent<GridAdventureItemCard>();
        card.MarkAsTemplate(false);
        return card;
    }

    private void ResolveItemTemplate()
    {
        if (itemCardTemplate != null) return;

        if (basketRoot != null)
            itemCardTemplate = basketRoot.GetComponentInChildren<GridAdventureItemCard>(true);

        if (itemCardTemplate == null && rootCanvas != null)
            itemCardTemplate = rootCanvas.GetComponentInChildren<GridAdventureItemCard>(true);
    }

    private void ClearRuntimeCards()
    {
        for (int i = runtimeCards.Count - 1; i >= 0; i--)
        {
            GridAdventureItemCard card = runtimeCards[i];
            if (card == null) continue;

            card.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(card.gameObject);
            else
                DestroyImmediate(card.gameObject);
        }

        runtimeCards.Clear();
        cardsByItemId.Clear();
    }

    private void RefreshBasketResponsiveGrid()
    {
        if (basketRoot == null) return;

        GridAdventureResponsiveGrid responsiveGrid = basketRoot.GetComponent<GridAdventureResponsiveGrid>();
        if (responsiveGrid != null)
            responsiveGrid.Refresh();
    }

    private void WireButtons()
    {
        WireButton(pauseButton, TogglePausePanel);
        WireButton(helpButton, UseClue);
        WireButton(resumeButton, ClosePausePanel);
        WireButton(pauseHowToPlayButton, OpenHowToPlayFromPause);
        WireButton(restartButton, RestartLevel);
        WireButton(resultContinueButton, ContinueToBloomPostPanel);
        WireButton(resultRestartButton, RestartLevel);
        WireButton(guidePrevButton, ShowPreviousGuideImage);
        WireButton(guideNextButton, ShowNextGuideImage);
        WireButton(guideStartButton, CloseHowToPlayPanel);
    }

    private void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void ConfigureBackgroundMusic()
    {
        if (audioManager == null) return;

        AudioClip clipToUse = backgroundMusicClip != null ? backgroundMusicClip : audioManager.backgroundMusicClip;
        audioManager.ConfigureBackgroundMusic(clipToUse, playBackgroundMusicOnStart, backgroundMusicVolume);
    }

    private void UpdateGuideVisual(bool animate = false)
    {
        int count = guideImages == null ? 0 : guideImages.Count;
        currentGuideIndex = Mathf.Clamp(currentGuideIndex, 0, Mathf.Max(0, count - 1));

        if (guideImage != null)
        {
            guideImage.sprite = count > 0 ? guideImages[currentGuideIndex] : null;
            guideImage.preserveAspect = true;
            guideImage.enabled = true;

            if (animate)
            {
                guideImage.rectTransform.DOKill();
                guideImage.canvasRenderer.SetAlpha(0f);
                guideImage.CrossFadeAlpha(1f, 0.18f, true);
                guideImage.rectTransform.localScale = Vector3.one * 0.98f;
                guideImage.rectTransform.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        if (guideCounterText != null)
            guideCounterText.text = count <= 0 ? "Add guide images in Inspector" : string.Format("{0} / {1}", currentGuideIndex + 1, count);

        if (guidePrevButton != null)
            guidePrevButton.interactable = count > 1 && currentGuideIndex > 0;

        if (guideNextButton != null)
            guideNextButton.interactable = count > 1 && currentGuideIndex < count - 1;
    }

    private void ShowOverlay(GameObject root, RectTransform card, bool show)
    {
        if (root == null) return;

        root.SetActive(true);
        if (card != null) card.DOKill();

        if (show)
        {
            if (card != null)
            {
                card.localScale = Vector3.one * 0.9f;
                card.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
            }
            return;
        }

        if (card != null)
        {
            card.DOScale(0.9f, 0.12f).SetEase(Ease.InQuad).SetUpdate(true)
                .OnComplete(delegate { if (root != null) root.SetActive(false); });
        }
        else
        {
            root.SetActive(false);
        }
    }

    private void SetOverlayInstant(GameObject root, bool show)
    {
        if (root == null) return;
        root.SetActive(show);
    }

    private void CacheMissingReferences()
    {
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (graphicRaycaster == null && rootCanvas != null) graphicRaycaster = rootCanvas.GetComponent<GraphicRaycaster>();
        if (audioManager == null) audioManager = GetComponent<GridAdventureAudioManager>();
        if (firstTimeTutorialController == null && rootCanvas != null)
            firstTimeTutorialController = rootCanvas.GetComponentInChildren<GridAdventureFirstTimeTutorialController>(true);

        if (dragLayer == null && rootCanvas != null)
        {
            Transform foundDragLayer = rootCanvas.transform.Find("DragLayer");
            if (foundDragLayer != null) dragLayer = foundDragLayer as RectTransform;
        }

        if (itemCardTemplate == null && basketRoot != null)
            itemCardTemplate = basketRoot.GetComponentInChildren<GridAdventureItemCard>(true);

        if (pauseHowToPlayButton == null && pauseOverlayRoot != null)
        {
            Button[] pauseButtons = pauseOverlayRoot.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < pauseButtons.Length; i++)
            {
                if (pauseButtons[i] != null && pauseButtons[i].name.ToLowerInvariant().Contains("how to play"))
                {
                    pauseHowToPlayButton = pauseButtons[i];
                    break;
                }
            }
        }

        if (gameplayInstructionOverlayRoot == null && rootCanvas != null)
        {
            Transform foundInstruction = rootCanvas.transform.Find("SafeAreaRoot/CenterContent/GameplayInstructionOverlayRoot");
            if (foundInstruction != null)
                gameplayInstructionOverlayRoot = foundInstruction.gameObject;
        }
    }

    private void KillSequences()
    {
        if (clueSequence != null && clueSequence.IsActive()) clueSequence.Kill();
        if (loadingSequence != null && loadingSequence.IsActive()) loadingSequence.Kill();
        if (gameplayInstructionSequence != null && gameplayInstructionSequence.IsActive()) gameplayInstructionSequence.Kill();
        clueSequence = null;
        loadingSequence = null;
        gameplayInstructionSequence = null;
    }
}
