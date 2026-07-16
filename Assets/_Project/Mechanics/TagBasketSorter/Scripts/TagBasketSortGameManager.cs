using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RewardSystem;

namespace TagBasketSorter
{
    public enum TagBasketWrongDropMode
    {
        SnapBackOnly,
        SnapBackAndPenalty
    }

    [Serializable]
    public sealed class TagBasketBloomSkillConfig
    {
        public BloomSkillType skillType = BloomSkillType.Apply;
        [Min(1f)] public float maxScore = 100f;
        [Tooltip("Use -1 to use RewardManager global default.")]
        public float timeWeight = -1f;
        [Tooltip("Use -1 to use RewardManager global default.")]
        public float accuracyWeight = -1f;
    }

    [DisallowMultipleComponent]
    public sealed class TagBasketSortGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("Core References")]
        public Canvas rootCanvas;
        public RectTransform dragLayer;
        public GameObject landingPage;
        public GameObject gameplayPage;
        public RectTransform levelPanelsRoot;
        public List<TagBasketLevelPanel> levels = new List<TagBasketLevelPanel>();

        [Header("Landing Level Buttons")]
        public RectTransform levelButtonContainer;
        public TagBasketLevelButton levelButtonTemplate;
        public List<TagBasketLevelButton> levelButtons = new List<TagBasketLevelButton>();
        public bool autoCollectLevelsFromChildren = true;
        public bool autoCreateMissingLevelButtons = true;
        public bool autoArrangeLevelButtons = true;
        public bool hideExtraLevelButtons = true;
        [Min(1)] public int levelButtonsPerRow = 3;
        public Vector2 levelButtonSpacing = new Vector2(280f, 190f);

        [Header("Top UI")]
        public TMP_Text scoreText;
        public TMP_Text progressText;

        [Tooltip("Optional legacy text timer. New generated layout uses timerSlider instead.")]
        public TMP_Text timerText;
        public Slider timerSlider;
        public Image timerSliderFillImage;
        public Color timerNormalFillColor = new Color(0.18f, 0.82f, 1f, 1f);
        public Color timerWarningFillColor = new Color(1f, 0.22f, 0.12f, 1f);

        public RectTransform hintContainer;
        public TMP_Text hintCounterText;
        public Button hintButton;
        public Button pauseButton;

        [Header("Panels")]
        public GameObject pausePanel;
        public GameObject resultPanel;
        public GameObject howToPlayPanel;
        public Button continueButton;
        public Button playAgainButton;
        public Button retryButton;
        public Button homeButton;
        public List<Button> homeButtons = new List<Button>();
        public Button resumeButton;
        public Button howToPlayButton;
        public Button closeHowToPlayButton;
        public TMP_Text resultTitleText;
        public TMP_Text resultBodyText;

        [Header("Popups - Text Only")]
        public CanvasGroup feedbackPopup;
        public TMP_Text feedbackText;
        public CanvasGroup scoreDeltaPopup;
        public TMP_Text scoreDeltaText;
        public Color positiveMessageColor = new Color(0.12f, 0.85f, 0.32f, 1f);
        public Color negativeMessageColor = new Color(1f, 0.2f, 0.16f, 1f);
        public Color neutralMessageColor = Color.white;

        [Header("Typography")]
        public TMP_FontAsset primaryFont;
        public TMP_FontAsset secondaryFont;
        public bool applyFontsOnAwake = true;

        [Header("Bloom Reward Integration")]
        public bool useBloomRewardSystem = true;
        public bool showBloomPreGameBeforeLanding = true;
        public string homeSceneName = "Loader Scene";
        public List<TagBasketBloomSkillConfig> bloomSkills = new List<TagBasketBloomSkillConfig>
        {
            new TagBasketBloomSkillConfig { skillType = BloomSkillType.Apply, maxScore = 100f, timeWeight = 0.3f, accuracyWeight = 0.7f },
            new TagBasketBloomSkillConfig { skillType = BloomSkillType.Understand, maxScore = 60f, timeWeight = 0.2f, accuracyWeight = 0.8f }
        };

        [Header("Startup How To Play")]
        public bool showHowToPlayOnStart = true;

        [Header("First Level Tutorial Overlay")]
        public bool showTutorialOnFirstPlayableLevel = true;
        public bool hideTutorialAfterFirstCorrectDrop = true;
        public CanvasGroup tutorialOverlay;
        [Tooltip("Optional. Assign the tutorial card/background RectTransform so the full message card breathes, not only the text.")]
        public RectTransform tutorialBreathTarget;
        public TMP_Text tutorialText;
        [TextArea(2, 3)] public string tutorialMessage = "Drag an object into the matching basket.";
        [Min(0.1f)] public float tutorialBreathDuration = 0.85f;
        [Min(1f)] public float tutorialBreathScale = 1.06f;

        [Header("Hint Settings")]
        public CanvasGroup hintOverlay;
        public TMP_Text hintText;
        [Tooltip("Keep this off for premium no-text hint. When off, Hint only pulses the object and matching basket.")]
        public bool showHintTextOverlay = false;
        [Min(0f)] public float hintOverlayDuration = 1.1f;
        [Min(1f)] public float hintPulseScale = 1.12f;
        [Min(0.05f)] public float hintPulseDuration = 0.45f;
        [Min(2)] public int hintPulseLoopCount = 4;
        public string hintMessageFormat = "Try: {0} → {1}";

        [Header("Gameplay Settings")]
        [Min(1)] public int initiallyUnlockedLevels = 1;
        public bool saveProgressWithPlayerPrefs = true;
        public string progressPrefsKey = "TagBasketSorter_UnlockedLevel";
        public bool resetSessionScoreWhenOpeningFromLanding = true;
        public bool useTimer = true;
        [Min(5f)] public float secondsPerLevel = 60f;
        public int pointsPerCorrectDrop = 10;
        public int wrongDropPenalty = 2;
        public TagBasketWrongDropMode wrongDropMode = TagBasketWrongDropMode.SnapBackAndPenalty;
        public bool autoAdvanceAfterLevelComplete = false;
        [Min(0f)] public float autoAdvanceDelay = 1.2f;

        [Header("Feel Settings")]
        [Min(0f)] public float snapBackDuration = 0.2f;
        [Min(0f)] public float correctDropSnapDuration = 0.12f;
        [Min(0f)] public float popupDuration = 0.75f;
        public bool useUnscaledTimeForUi = true;

        [Header("Audio - Sources")]
        public AudioSource sfxAudioSource;
        public AudioSource bgmAudioSource;

        [Header("Audio - Clips")]
        public AudioClip backgroundMusicClip;
        public AudioClip clickClip;
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip levelCompleteClip;
        public AudioClip gameCompleteClip;
        public AudioClip timeoutClip;
        public AudioClip hintClip;
        public AudioClip clockWarningClip;

        [Header("Audio - Behaviour")]
        public bool playBackgroundMusicDuringGameplay = true;
        public bool stopBackgroundMusicOnRewardScreen = true;
        [Range(0.01f, 0.5f)] public float clockWarningTimePercent = 0.1f;

        public bool CanDragItems => isPlaying && !isPaused && activeLevel != null && !levelFinished;

        private readonly List<TagBasketLevelPanel> playableLevels = new List<TagBasketLevelPanel>();
        private readonly List<TagBasketDraggableItem> hintItemBuffer = new List<TagBasketDraggableItem>();
        private readonly List<SkillEntry> runtimeBloomSkills = new List<SkillEntry>();
        private int currentLevelIndex = -1;
        private int unlockedLevelCount;
        private int currentCorrectCount;
        private int currentWrongCount;
        private int levelScore;
        private int sessionScore;
        private int hintsUsedThisLevel;
        private int lastShownTimerSeconds = -1;
        private float remainingTime;
        private float levelStartRealtime;
        private bool isPlaying;
        private bool isPaused;
        private bool levelFinished;
        private bool hasShownHowToPlayThisSession;
        private bool hasPlayedClockWarning;
        private bool localResultReadyForBloom;
        private TagBasketLevelPanel activeLevel;
        private TagBasketDraggableItem currentHintItem;
        private TagBasketDropZone currentHintZone;
        private Coroutine feedbackRoutine;
        private Coroutine scoreDeltaRoutine;
        private Coroutine hintRoutine;
        private Coroutine autoAdvanceRoutine;
        private Tween tutorialTween;

        private void Awake()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            ResolveMissingRuntimeReferences();

            if (sfxAudioSource == null)
                sfxAudioSource = GetComponent<AudioSource>();

            if (sfxAudioSource == null)
                sfxAudioSource = gameObject.AddComponent<AudioSource>();

            if (bgmAudioSource == null)
            {
                bgmAudioSource = gameObject.AddComponent<AudioSource>();
                bgmAudioSource.playOnAwake = false;
                bgmAudioSource.loop = true;
            }

            if (applyFontsOnAwake)
                ApplyConfiguredFontsToAllTexts();

            BuildLevelCache();
            LoadProgress();
            WireButtons();
            SetupLevels();
            HideAllLocalPages();
        }

        private void Start()
        {
            StartCoroutine(BootFlow());
        }

        private IEnumerator BootFlow()
        {
            BuildRuntimeBloomSkills();

            if (useBloomRewardSystem && showBloomPreGameBeforeLanding && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPreGame(runtimeBloomSkills);
                yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
            }

            ShowLandingPage();
        }

        private void Update()
        {
            if (!isPlaying || isPaused || !useTimer || levelFinished)
                return;

            remainingTime -= Time.deltaTime;

            if (!hasPlayedClockWarning && remainingTime > 0f && remainingTime <= secondsPerLevel * clockWarningTimePercent)
            {
                hasPlayedClockWarning = true;
                PlaySfx(clockWarningClip, 0.95f);
            }

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                UpdateHud(true);
                FinishLevel(false, "Time Up!");
                return;
            }

            UpdateTimerOnly(false);
        }

        public void OpenLevelFromLanding(int levelIndex)
        {
            PlaySfx(clickClip);

            if (resetSessionScoreWhenOpeningFromLanding)
                sessionScore = 0;

            LoadLevel(levelIndex);
        }

        public void LoadLevel(int levelIndex)
        {
            BuildLevelCache();

            if (levelIndex < 0 || levelIndex >= playableLevels.Count)
            {
                Debug.LogWarning($"TagBasketSortGameManager: Level index {levelIndex} is out of range.");
                return;
            }

            if (levelIndex >= unlockedLevelCount)
            {
                ShowFeedback("Complete previous level first", false);
                return;
            }

            currentLevelIndex = levelIndex;
            activeLevel = playableLevels[currentLevelIndex];
            currentCorrectCount = 0;
            currentWrongCount = 0;
            levelScore = 0;
            hintsUsedThisLevel = 0;
            currentHintItem = null;
            currentHintZone = null;
            remainingTime = secondsPerLevel;
            lastShownTimerSeconds = -1;
            hasPlayedClockWarning = false;
            localResultReadyForBloom = false;
            isPaused = false;
            Time.timeScale = 1f;
            isPlaying = true;
            levelFinished = false;
            levelStartRealtime = Time.realtimeSinceStartup;

            StopAutoAdvanceRoutine();
            HideTutorialOverlay(false);
            HideHintOverlay(false);

            SetActiveSafe(landingPage, false);
            SetActiveSafe(gameplayPage, true);
            SetActiveSafe(pausePanel, false);
            SetActiveSafe(resultPanel, false);
            SetActiveSafe(howToPlayPanel, false);

            foreach (TagBasketLevelPanel level in levels)
                SetActiveSafe(level != null ? level.gameObject : null, level == activeLevel);

            activeLevel.Setup(this);
            activeLevel.StartLevel();
            StartBackgroundMusic();
            UpdateHud(true);
            AnimatePanelIn(activeLevel.transform as RectTransform);
            StartFirstLevelTutorialIfNeeded();
        }

        public void TryDropItem(TagBasketDraggableItem item, TagBasketDropZone dropZone)
        {
            if (!CanDragItems || item == null || dropZone == null || activeLevel == null || levelFinished)
                return;

            if (!activeLevel.OwnsItem(item) || !activeLevel.OwnsDropZone(dropZone))
            {
                item.ReturnToStart(true);
                return;
            }

            if (dropZone.Accepts(item))
                HandleCorrectDrop(item, dropZone);
            else
                HandleWrongDrop(item);
        }

        public void OnItemDragStarted(TagBasketDraggableItem item)
        {
            if (item != null)
                PlaySfx(clickClip, 0.55f);
        }

        public void OnItemReleasedOutsideBasket(TagBasketDraggableItem item)
        {
            if (item != null && !item.IsPlacedCorrectly)
                item.ReturnToStart(true);
        }

        public void RetryCurrentLevel()
        {
            ReloadCurrentScene();
        }

        public void ShowLandingPage()
        {
            isPlaying = false;
            isPaused = false;
            Time.timeScale = 1f;
            levelFinished = false;
            activeLevel = null;
            currentLevelIndex = -1;
            currentHintItem = null;
            currentHintZone = null;
            localResultReadyForBloom = false;
            StopAutoAdvanceRoutine();
            HideTutorialOverlay(false);
            HideHintOverlay(false);
            StopBackgroundMusic();

            BuildLevelCache();
            SetActiveSafe(landingPage, true);
            SetActiveSafe(gameplayPage, false);
            SetActiveSafe(pausePanel, false);
            SetActiveSafe(resultPanel, false);

            foreach (TagBasketLevelPanel level in levels)
                SetActiveSafe(level != null ? level.gameObject : null, false);

            RefreshLevelButtons();

            bool shouldShowHowTo = showHowToPlayOnStart && !hasShownHowToPlayThisSession;
            SetActiveSafe(howToPlayPanel, shouldShowHowTo);
            if (shouldShowHowTo)
            {
                hasShownHowToPlayThisSession = true;
                AnimatePanelIn(howToPlayPanel != null ? howToPlayPanel.transform as RectTransform : null);
            }
        }

        public void PauseGame()
        {
            if (!isPlaying || levelFinished)
                return;

            PlaySfx(clickClip);
            isPaused = true;
            Time.timeScale = 0f;
            SetActiveSafe(pausePanel, true);
            AnimatePanelIn(pausePanel != null ? pausePanel.transform as RectTransform : null);
        }

        public void ResumeGame()
        {
            PlaySfx(clickClip);
            isPaused = false;
            Time.timeScale = 1f;
            SetActiveSafe(pausePanel, false);
            SetActiveSafe(howToPlayPanel, false);
        }

        public void OpenHowToPlay()
        {
            PlaySfx(clickClip);
            SetActiveSafe(howToPlayPanel, true);
            AnimatePanelIn(howToPlayPanel != null ? howToPlayPanel.transform as RectTransform : null);
        }

        public void CloseHowToPlay()
        {
            PlaySfx(clickClip);
            SetActiveSafe(howToPlayPanel, false);
        }

        public void ShowHint()
        {
            if (!CanDragItems || activeLevel == null || levelFinished)
                return;

            int maxHints = Mathf.Max(0, activeLevel.maxHintsAllowed);
            if (maxHints <= 0)
            {
                ShowFeedback("Hints disabled for this level", false);
                return;
            }

            if (!IsCurrentHintStillValid())
            {
                currentHintItem = null;
                currentHintZone = null;

                if (hintsUsedThisLevel >= maxHints)
                {
                    ShowFeedback("No hints left", false);
                    return;
                }

                activeLevel.GetUnplacedItems(hintItemBuffer);
                if (hintItemBuffer.Count == 0)
                    return;

                int randomIndex = UnityEngine.Random.Range(0, hintItemBuffer.Count);
                currentHintItem = hintItemBuffer[randomIndex];
                currentHintZone = activeLevel.FindDropZoneForTag(currentHintItem.itemTag);
                hintsUsedThisLevel++;
            }

            if (currentHintItem == null || currentHintZone == null)
            {
                ShowFeedback("Check item and basket tags", false);
                return;
            }

            PlaySfx(hintClip != null ? hintClip : clickClip, 0.8f);
            currentHintItem.PlayHintPulse(hintPulseScale, hintPulseDuration, hintPulseLoopCount);
            currentHintZone.PlayHintPulse(hintPulseScale, hintPulseDuration, hintPulseLoopCount);

            if (showHintTextOverlay)
                ShowHintOverlay(string.Format(hintMessageFormat, currentHintItem.GetDisplayName(), currentHintZone.acceptedTag));
            else
                HideHintOverlay(false);

            UpdateHintButtonState();
        }

        public void ContinueToBloomReward()
        {
            PlaySfx(clickClip);

            if (!localResultReadyForBloom)
                return;

            SetActiveSafe(resultPanel, false);
            StopBackgroundMusic();

            if (!useBloomRewardSystem || RewardManager.Instance == null)
            {
                ShowLandingPage();
                return;
            }

            RewardManager.Instance.ShowPostGame(runtimeBloomSkills, BuildEvaluationData());
        }

        public void OnRewardScreenOpen()
        {
            if (stopBackgroundMusicOnRewardScreen)
                StopBackgroundMusic();
        }

        public void OnPlayAgain()
        {
            ReloadCurrentScene();
        }

        private void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            DOTween.KillAll(false);

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.name))
                SceneManager.LoadScene(activeScene.name);
            else if (activeScene.buildIndex >= 0)
                SceneManager.LoadScene(activeScene.buildIndex);
        }

        public void OnHome()
        {
            Time.timeScale = 1f;

            if (RewardManager.Instance != null)
                RewardManager.Instance.HideAll();

            if (UnityAndroidMediator.Instance != null)
                UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

            //if (GameLoader.Instance != null)
            //    GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");

            SceneManager.LoadScene(homeSceneName);
        }

        [ContextMenu("Reset Saved Progress")]
        public void ResetSavedProgress()
        {
            PlayerPrefs.DeleteKey(progressPrefsKey);
            PlayerPrefs.Save();
            BuildLevelCache();
            unlockedLevelCount = Mathf.Clamp(initiallyUnlockedLevels, 1, Mathf.Max(1, playableLevels.Count));
            RefreshLevelButtons();
        }

        [ContextMenu("Refresh Levels And Buttons")]
        public void RefreshLevelsAndButtonsManual()
        {
            BuildLevelCache();
            SetupLevels();
            RefreshLevelButtons();
        }

        [ContextMenu("Apply Fonts To All TMP Texts")]
        public void ApplyConfiguredFontsToAllTexts()
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                if (text == null)
                    continue;

                bool secondary = text.name.IndexOf("Body", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.name.IndexOf("Instruction", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.name.IndexOf("Hint", StringComparison.OrdinalIgnoreCase) >= 0;

                TMP_FontAsset targetFont = secondary && secondaryFont != null ? secondaryFont : primaryFont;
                if (targetFont != null)
                    text.font = targetFont;
            }
        }

        private void HandleCorrectDrop(TagBasketDraggableItem item, TagBasketDropZone dropZone)
        {
            item.MarkPlacedCorrectly(dropZone);
            currentCorrectCount++;
            levelScore += pointsPerCorrectDrop;

            if (item == currentHintItem)
            {
                currentHintItem = null;
                currentHintZone = null;
                HideHintOverlay(true);
            }

            if (showTutorialOnFirstPlayableLevel && hideTutorialAfterFirstCorrectDrop && currentLevelIndex == 0 && currentCorrectCount >= 1)
                HideTutorialOverlay(true);

            PlaySfx(correctClip);
            ShowFeedback("Correct!", true);
            ShowScoreDelta(pointsPerCorrectDrop);
            UpdateHud(true);

            if (activeLevel != null && currentCorrectCount >= activeLevel.TotalItems)
                FinishLevel(true, "Level Complete!");
        }

        private void HandleWrongDrop(TagBasketDraggableItem item)
        {
            currentWrongCount++;
            int appliedPenalty = 0;
            if (wrongDropMode == TagBasketWrongDropMode.SnapBackAndPenalty)
            {
                int before = levelScore;
                levelScore = Mathf.Max(0, levelScore - wrongDropPenalty);
                appliedPenalty = before - levelScore;
            }

            item.ReturnToStart(true);
            PlaySfx(wrongClip);
            ShowFeedback("Try another basket", false);
            if (appliedPenalty > 0)
                ShowScoreDelta(-appliedPenalty);
            UpdateHud(true);
        }

        private void FinishLevel(bool success, string title)
        {
            if (levelFinished)
                return;

            levelFinished = true;
            isPlaying = false;
            isPaused = false;
            Time.timeScale = 1f;
            HideTutorialOverlay(true);
            HideHintOverlay(true);
            localResultReadyForBloom = true;

            if (success)
            {
                sessionScore += Mathf.Max(0, levelScore);
                UnlockNextLevelIfNeeded();
                bool isLastLevel = currentLevelIndex >= playableLevels.Count - 1;
                PlaySfx(isLastLevel ? gameCompleteClip : levelCompleteClip);

                if (autoAdvanceAfterLevelComplete && !isLastLevel)
                {
                    autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine());
                    ShowFeedback("Next level unlocked!", true);
                    return;
                }

                ShowLocalResultPanel(isLastLevel ? "Game Complete!" : title, BuildResultBody(success));
            }
            else
            {
                PlaySfx(timeoutClip != null ? timeoutClip : wrongClip);
                ShowLocalResultPanel(title, BuildResultBody(success));
            }

            RefreshLevelButtons();
        }

        private string BuildResultBody(bool success)
        {
            string levelName = activeLevel != null ? activeLevel.levelTitle : $"Level {currentLevelIndex + 1}";
            string line1 = success ? $"{levelName} completed." : $"{levelName} not completed.";
            string line2 = $"Correct: {currentCorrectCount}/{(activeLevel != null ? activeLevel.TotalItems : 0)}";
            string line3 = $"Wrong Attempts: {currentWrongCount}";
            string line4 = $"Score: {sessionScore + (success ? 0 : levelScore)}";
            return $"{line1}\n{line2}\n{line3}\n{line4}";
        }

        private void ShowLocalResultPanel(string title, string body)
        {
            SetActiveSafe(resultPanel, true);
            if (resultTitleText != null) resultTitleText.text = title;
            if (resultBodyText != null) resultBodyText.text = body;
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            if (playAgainButton != null) playAgainButton.gameObject.SetActive(true);
            if (retryButton != null) retryButton.gameObject.SetActive(true);
            AnimatePanelIn(resultPanel != null ? resultPanel.transform as RectTransform : null);
        }

        private GameEvaluationData BuildEvaluationData()
        {
            float timeTaken = Mathf.Max(0f, Time.realtimeSinceStartup - levelStartRealtime);
            float expectedMaxTime = Mathf.Max(1f, secondsPerLevel);
            float timeScore = Mathf.Clamp01(1f - (timeTaken / expectedMaxTime));
            int totalItems = activeLevel != null ? Mathf.Max(1, activeLevel.TotalItems) : Mathf.Max(1, currentCorrectCount + currentWrongCount);
            float accuracyScore = Mathf.Clamp01((float)currentCorrectCount / totalItems);

            return new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = currentWrongCount,
                timeTaken = timeTaken
            };
        }

        private void BuildRuntimeBloomSkills()
        {
            runtimeBloomSkills.Clear();

            if (bloomSkills != null)
            {
                foreach (TagBasketBloomSkillConfig config in bloomSkills)
                {
                    if (config == null)
                        continue;

                    runtimeBloomSkills.Add(new SkillEntry(
                        config.skillType,
                        Mathf.Max(1f, config.maxScore),
                        timeWeight: config.timeWeight,
                        accuracyWeight: config.accuracyWeight));
                }
            }

            if (runtimeBloomSkills.Count == 0)
                runtimeBloomSkills.Add(new SkillEntry(BloomSkillType.Apply, 100f));
        }

        private void UnlockNextLevelIfNeeded()
        {
            int requiredUnlocked = Mathf.Clamp(currentLevelIndex + 2, 1, Mathf.Max(1, playableLevels.Count));
            if (requiredUnlocked > unlockedLevelCount)
            {
                unlockedLevelCount = requiredUnlocked;
                SaveProgress();
            }
        }

        private void BuildLevelCache()
        {
            if (levels == null)
                levels = new List<TagBasketLevelPanel>();

            if (autoCollectLevelsFromChildren && levelPanelsRoot != null)
            {
                levels.Clear();
                TagBasketLevelPanel[] foundLevels = levelPanelsRoot.GetComponentsInChildren<TagBasketLevelPanel>(true);
                foreach (TagBasketLevelPanel level in foundLevels)
                {
                    if (level != null && !levels.Contains(level))
                        levels.Add(level);
                }
            }

            playableLevels.Clear();
            foreach (TagBasketLevelPanel level in levels)
            {
                if (level != null && level.isLevelEnabled)
                    playableLevels.Add(level);
            }

            if (playableLevels.Count > 0)
                unlockedLevelCount = Mathf.Clamp(Mathf.Max(1, unlockedLevelCount), 1, playableLevels.Count);
        }

        private void LoadProgress()
        {
            int levelCount = playableLevels.Count;
            int defaultUnlocked = Mathf.Max(1, initiallyUnlockedLevels);
            unlockedLevelCount = saveProgressWithPlayerPrefs
                ? PlayerPrefs.GetInt(progressPrefsKey, defaultUnlocked)
                : defaultUnlocked;

            if (levelCount > 0)
                unlockedLevelCount = Mathf.Clamp(unlockedLevelCount, 1, levelCount);
            else
                unlockedLevelCount = 0;
        }

        private void SaveProgress()
        {
            if (!saveProgressWithPlayerPrefs)
                return;

            PlayerPrefs.SetInt(progressPrefsKey, unlockedLevelCount);
            PlayerPrefs.Save();
        }

        private void SetupLevels()
        {
            BuildLevelCache();

            foreach (TagBasketLevelPanel level in levels)
            {
                if (level != null)
                    level.Setup(this);
            }

            if (playableLevels.Count > 0)
                unlockedLevelCount = Mathf.Clamp(unlockedLevelCount, 1, playableLevels.Count);
        }

        private void RefreshLevelButtons()
        {
            BuildLevelCache();
            EnsureLevelButtonsForPlayableLevels();

            for (int i = 0; i < levelButtons.Count; i++)
            {
                TagBasketLevelButton levelButton = levelButtons[i];
                if (levelButton == null)
                    continue;

                bool shouldShow = i < playableLevels.Count;
                if (hideExtraLevelButtons)
                    levelButton.gameObject.SetActive(shouldShow);

                if (!shouldShow)
                    continue;

                string title = playableLevels[i] != null ? playableLevels[i].levelTitle : $"Level {i + 1}";
                bool unlocked = i < unlockedLevelCount;
                levelButton.Setup(this, i, title, unlocked);
            }

            UpdateHintButtonState();
        }

        private void EnsureLevelButtonsForPlayableLevels()
        {
            if (!autoCreateMissingLevelButtons || levelButtonTemplate == null || levelButtonContainer == null)
                return;

            if (levelButtons == null)
                levelButtons = new List<TagBasketLevelButton>();

            if (!levelButtons.Contains(levelButtonTemplate))
                levelButtons.Insert(0, levelButtonTemplate);

            for (int i = 0; i < levelButtons.Count; i++)
            {
                if (levelButtons[i] == null)
                    levelButtons.RemoveAt(i--);
            }

            while (levelButtons.Count < playableLevels.Count)
            {
                TagBasketLevelButton clone = Instantiate(levelButtonTemplate, levelButtonContainer);
                clone.name = $"LevelButton_{levelButtons.Count + 1}";
                clone.gameObject.SetActive(true);
                levelButtons.Add(clone);
            }

            if (autoArrangeLevelButtons)
                ArrangeLevelButtons();
        }

        private void ArrangeLevelButtons()
        {
            int count = Mathf.Max(1, playableLevels.Count);
            int columns = Mathf.Max(1, levelButtonsPerRow);
            int rows = Mathf.CeilToInt(count / (float)columns);

            for (int i = 0; i < levelButtons.Count; i++)
            {
                if (levelButtons[i] == null)
                    continue;

                RectTransform rect = levelButtons[i].transform as RectTransform;
                if (rect == null)
                    continue;

                int row = i / columns;
                int column = i % columns;
                int itemsInRow = Mathf.Min(columns, count - row * columns);
                float rowWidth = (itemsInRow - 1) * levelButtonSpacing.x;
                float x = column * levelButtonSpacing.x - rowWidth * 0.5f;
                float y = ((rows - 1) * levelButtonSpacing.y * 0.5f) - row * levelButtonSpacing.y;
                rect.anchoredPosition = new Vector2(x, y);
            }
        }

        private void WireButtons()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(ContinueToBloomReward);
                continueButton.onClick.AddListener(ContinueToBloomReward);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(RetryCurrentLevel);
                playAgainButton.onClick.RemoveListener(OnPlayAgain);
                playAgainButton.onClick.AddListener(OnPlayAgain);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(RetryCurrentLevel);
                retryButton.onClick.RemoveListener(OnPlayAgain);
                retryButton.onClick.AddListener(OnPlayAgain);
            }

            if (homeButton != null)
            {
                homeButton.onClick.RemoveListener(ShowLandingPage);
                homeButton.onClick.AddListener(ShowLandingPage);
            }

            if (homeButtons != null)
            {
                foreach (Button button in homeButtons)
                {
                    if (button == null) continue;
                    button.onClick.RemoveListener(ShowLandingPage);
                    button.onClick.AddListener(ShowLandingPage);
                }
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(PauseGame);
                pauseButton.onClick.AddListener(PauseGame);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(ResumeGame);
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (howToPlayButton != null)
            {
                howToPlayButton.onClick.RemoveListener(OpenHowToPlay);
                howToPlayButton.onClick.AddListener(OpenHowToPlay);
            }

            if (closeHowToPlayButton != null)
            {
                closeHowToPlayButton.onClick.RemoveListener(CloseHowToPlay);
                closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);
            }

            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(ShowHint);
                hintButton.onClick.AddListener(ShowHint);
            }
        }

        private void UpdateHud(bool forceTimer)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {sessionScore + levelScore}";

            if (progressText != null)
                progressText.text = activeLevel != null ? $"{currentCorrectCount}/{activeLevel.TotalItems}" : "0/0";

            UpdateTimerOnly(forceTimer);
            UpdateHintButtonState();
        }

        private void UpdateTimerOnly(bool force)
        {
            float normalizedTime = useTimer && secondsPerLevel > 0f
                ? Mathf.Clamp01(remainingTime / secondsPerLevel)
                : 1f;

            if (timerSlider != null)
            {
                if (timerSlider.gameObject.activeSelf != useTimer)
                    timerSlider.gameObject.SetActive(useTimer);

                if (useTimer)
                    timerSlider.value = normalizedTime;
            }

            if (timerSliderFillImage != null)
                timerSliderFillImage.color = normalizedTime <= clockWarningTimePercent ? timerWarningFillColor : timerNormalFillColor;

            if (timerText == null)
                return;

            if (!useTimer)
            {
                if (timerText.text.Length > 0)
                    timerText.text = string.Empty;
                return;
            }

            int seconds = Mathf.CeilToInt(remainingTime);
            if (!force && seconds == lastShownTimerSeconds)
                return;

            lastShownTimerSeconds = seconds;
            int minutesPart = seconds / 60;
            int secondsPart = seconds % 60;
            timerText.text = $"{minutesPart:00}:{secondsPart:00}";
        }

        private void UpdateHintButtonState()
        {
            int maxHints = activeLevel != null ? Mathf.Max(0, activeLevel.maxHintsAllowed) : 0;
            int remainingHints = activeLevel != null ? Mathf.Max(0, maxHints - hintsUsedThisLevel) : 0;

            if (hintCounterText != null)
                hintCounterText.text = $"{remainingHints}/{maxHints}";

            bool canInteract = false;
            if (CanDragItems && activeLevel != null)
            {
                bool canReuseCurrent = IsCurrentHintStillValid();
                bool canUseNewHint = hintsUsedThisLevel < maxHints;
                canInteract = canReuseCurrent || canUseNewHint;
            }

            if (hintButton != null)
                hintButton.interactable = canInteract;
        }

        private bool IsCurrentHintStillValid()
        {
            return currentHintItem != null
                && currentHintZone != null
                && currentHintItem.CanReceiveHint
                && activeLevel != null
                && activeLevel.OwnsItem(currentHintItem)
                && activeLevel.OwnsDropZone(currentHintZone);
        }

        private void ShowFeedback(string message, bool positive)
        {
            if (feedbackPopup == null || feedbackText == null)
                return;

            if (feedbackRoutine != null)
                StopCoroutine(feedbackRoutine);

            feedbackText.text = message;
            feedbackText.color = positive ? positiveMessageColor : negativeMessageColor;
            feedbackRoutine = StartCoroutine(FeedbackRoutine());
        }

        private IEnumerator FeedbackRoutine()
        {
            feedbackPopup.gameObject.SetActive(true);
            feedbackPopup.DOKill();
            feedbackPopup.alpha = 1f;
            RectTransform popupRect = feedbackPopup.transform as RectTransform;

            if (popupRect != null)
            {
                popupRect.DOKill();
                popupRect.localScale = Vector3.one * 0.88f;
                popupRect.DOScale(1f, 0.14f).SetEase(Ease.OutBack).SetUpdate(useUnscaledTimeForUi);
            }

            yield return Wait(popupDuration);

            feedbackPopup.DOKill();
            feedbackPopup.DOFade(0f, 0.15f).SetUpdate(useUnscaledTimeForUi);
            yield return Wait(0.16f);

            feedbackPopup.alpha = 0f;
            feedbackPopup.gameObject.SetActive(false);
        }

        private void ShowScoreDelta(int delta)
        {
            if (scoreDeltaPopup == null || scoreDeltaText == null || delta == 0)
                return;

            if (scoreDeltaRoutine != null)
                StopCoroutine(scoreDeltaRoutine);

            scoreDeltaText.text = delta > 0 ? $"+{delta}" : delta.ToString();
            scoreDeltaText.color = delta > 0 ? positiveMessageColor : negativeMessageColor;
            scoreDeltaRoutine = StartCoroutine(ScoreDeltaRoutine());
        }

        private IEnumerator ScoreDeltaRoutine()
        {
            scoreDeltaPopup.gameObject.SetActive(true);
            scoreDeltaPopup.DOKill();
            scoreDeltaPopup.alpha = 1f;

            RectTransform rect = scoreDeltaPopup.transform as RectTransform;
            Vector2 original = rect != null ? rect.anchoredPosition : Vector2.zero;

            if (rect != null)
            {
                rect.DOKill();
                rect.localScale = Vector3.one * 0.88f;
                rect.anchoredPosition = original;
                rect.DOScale(1.1f, 0.16f).SetEase(Ease.OutBack).SetUpdate(useUnscaledTimeForUi);
                rect.DOAnchorPos(original + new Vector2(0f, 52f), 0.42f).SetEase(Ease.OutQuad).SetUpdate(useUnscaledTimeForUi);
            }

            yield return Wait(0.42f);
            scoreDeltaPopup.DOFade(0f, 0.15f).SetUpdate(useUnscaledTimeForUi);
            yield return Wait(0.16f);

            if (rect != null)
                rect.anchoredPosition = original;

            scoreDeltaPopup.alpha = 0f;
            scoreDeltaPopup.gameObject.SetActive(false);
        }

        private void ShowHintOverlay(string message)
        {
            if (hintOverlay == null || hintText == null)
            {
                ShowFeedback(message, true);
                return;
            }

            if (hintRoutine != null)
                StopCoroutine(hintRoutine);

            hintText.text = message;
            hintRoutine = StartCoroutine(HintRoutine());
        }

        private IEnumerator HintRoutine()
        {
            hintOverlay.gameObject.SetActive(true);
            hintOverlay.DOKill();
            hintOverlay.alpha = 0f;
            hintOverlay.DOFade(1f, 0.12f).SetUpdate(useUnscaledTimeForUi);

            RectTransform hintRect = hintOverlay.transform as RectTransform;
            if (hintRect != null)
            {
                hintRect.DOKill();
                hintRect.localScale = Vector3.one * 0.94f;
                hintRect.DOScale(1f, 0.16f).SetEase(Ease.OutBack).SetUpdate(useUnscaledTimeForUi);
            }

            yield return Wait(hintOverlayDuration);

            hintOverlay.DOKill();
            hintOverlay.DOFade(0f, 0.15f).SetUpdate(useUnscaledTimeForUi);
            yield return Wait(0.16f);
            hintOverlay.alpha = 0f;
            hintOverlay.gameObject.SetActive(false);
        }

        private void HideHintOverlay(bool animate)
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            if (hintOverlay == null)
                return;

            hintOverlay.DOKill();
            if (animate)
                hintOverlay.DOFade(0f, 0.12f).SetUpdate(useUnscaledTimeForUi).OnComplete(() => SetActiveSafe(hintOverlay.gameObject, false));
            else
            {
                hintOverlay.alpha = 0f;
                SetActiveSafe(hintOverlay.gameObject, false);
            }
        }

        private void StartFirstLevelTutorialIfNeeded()
        {
            if (!showTutorialOnFirstPlayableLevel || currentLevelIndex != 0 || tutorialOverlay == null)
            {
                HideTutorialOverlay(false);
                return;
            }

            if (tutorialText != null)
                tutorialText.text = tutorialMessage;

            tutorialOverlay.gameObject.SetActive(true);
            tutorialOverlay.alpha = 1f;
            tutorialOverlay.blocksRaycasts = false;
            tutorialOverlay.interactable = false;

            RectTransform target = tutorialBreathTarget != null
                ? tutorialBreathTarget
                : tutorialOverlay.transform as RectTransform;

            if (target == null && tutorialText != null)
                target = tutorialText.transform as RectTransform;

            if (target == null)
                return;

            target.DOKill();
            target.localScale = Vector3.one;
            tutorialTween = target.DOScale(Vector3.one * tutorialBreathScale, tutorialBreathDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(useUnscaledTimeForUi);
        }

        private void HideTutorialOverlay(bool animate)
        {
            if (tutorialTween != null)
            {
                tutorialTween.Kill();
                tutorialTween = null;
            }

            if (tutorialBreathTarget != null)
            {
                tutorialBreathTarget.DOKill();
                tutorialBreathTarget.localScale = Vector3.one;
            }

            if (tutorialText != null)
            {
                tutorialText.transform.DOKill();
                tutorialText.transform.localScale = Vector3.one;
            }

            if (tutorialOverlay == null)
                return;

            tutorialOverlay.DOKill();
            if (animate)
            {
                tutorialOverlay.DOFade(0f, 0.16f).SetUpdate(useUnscaledTimeForUi).OnComplete(() =>
                {
                    if (tutorialOverlay != null)
                    {
                        tutorialOverlay.alpha = 0f;
                        tutorialOverlay.gameObject.SetActive(false);
                    }
                });
            }
            else
            {
                tutorialOverlay.alpha = 0f;
                tutorialOverlay.gameObject.SetActive(false);
            }
        }

        private void AnimatePanelIn(RectTransform panel)
        {
            if (panel == null)
                return;

            panel.DOKill();
            panel.localScale = Vector3.one * 0.96f;
            panel.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private IEnumerator AutoAdvanceRoutine()
        {
            yield return Wait(autoAdvanceDelay);
            if (currentLevelIndex + 1 < playableLevels.Count)
                LoadLevel(currentLevelIndex + 1);
        }

        private void StopAutoAdvanceRoutine()
        {
            if (autoAdvanceRoutine != null)
            {
                StopCoroutine(autoAdvanceRoutine);
                autoAdvanceRoutine = null;
            }
        }

        private object Wait(float seconds)
        {
            return useUnscaledTimeForUi ? (object)new WaitForSecondsRealtime(seconds) : new WaitForSeconds(seconds);
        }

        private void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxAudioSource != null)
                sfxAudioSource.PlayOneShot(clip, volume);
        }

        private void StartBackgroundMusic()
        {
            if (!playBackgroundMusicDuringGameplay || bgmAudioSource == null || backgroundMusicClip == null)
                return;

            if (bgmAudioSource.clip != backgroundMusicClip)
                bgmAudioSource.clip = backgroundMusicClip;

            bgmAudioSource.loop = true;
            if (!bgmAudioSource.isPlaying)
                bgmAudioSource.Play();
        }

        private void StopBackgroundMusic()
        {
            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
                bgmAudioSource.Stop();
        }


        private void ResolveMissingRuntimeReferences()
        {
            Transform searchRoot = rootCanvas != null ? rootCanvas.transform : transform;

            if (timerSlider == null)
                timerSlider = FindComponentByName<Slider>(searchRoot, "TimerSlider");

            if (timerSliderFillImage == null && timerSlider != null)
            {
                Transform fill = FindChildByName(timerSlider.transform, "Fill");
                if (fill != null)
                    timerSliderFillImage = fill.GetComponent<Image>();
            }

            if (hintContainer == null)
            {
                Transform hintContainerTransform = FindChildByName(searchRoot, "HintContainer");
                hintContainer = hintContainerTransform as RectTransform;
            }

            if (hintCounterText == null)
                hintCounterText = FindComponentByName<TMP_Text>(searchRoot, "HintCounterText");

            if (hintOverlay == null)
                hintOverlay = FindComponentByName<CanvasGroup>(searchRoot, "HintOverlay");

            if (hintText == null)
                hintText = FindComponentByName<TMP_Text>(searchRoot, "HintText");

            if (tutorialBreathTarget == null)
            {
                Transform tutorialCard = FindChildByName(searchRoot, "FirstLevelTutorialOverlay");
                tutorialBreathTarget = tutorialCard as RectTransform;
            }
        }

        private static T FindComponentByName<T>(Transform root, string objectName) where T : Component
        {
            Transform child = FindChildByName(root, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindChildByName(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && string.Equals(children[i].name, objectName, StringComparison.OrdinalIgnoreCase))
                    return children[i];
            }

            return null;
        }

        private void HideAllLocalPages()
        {
            SetActiveSafe(landingPage, false);
            SetActiveSafe(gameplayPage, false);
            SetActiveSafe(pausePanel, false);
            SetActiveSafe(resultPanel, false);
            SetActiveSafe(howToPlayPanel, false);
            if (feedbackPopup != null) SetActiveSafe(feedbackPopup.gameObject, false);
            if (scoreDeltaPopup != null) SetActiveSafe(scoreDeltaPopup.gameObject, false);
            if (hintOverlay != null) SetActiveSafe(hintOverlay.gameObject, false);
            if (tutorialOverlay != null) SetActiveSafe(tutorialOverlay.gameObject, false);
        }

        private static void SetActiveSafe(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private void OnDestroy()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;

            StopAutoAdvanceRoutine();

            if (tutorialTween != null)
                tutorialTween.Kill();

            DOTween.Kill(transform);
        }
    }
}
