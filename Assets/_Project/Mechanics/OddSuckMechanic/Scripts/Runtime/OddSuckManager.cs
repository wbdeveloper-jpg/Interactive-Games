using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RewardSystem;

namespace OddSuckMechanic
{
    public enum OddSuckHowToDisplayMode
    {
        FirstTimeAutomatically,
        EveryGameStartAutomatically,
        ManualButtonOnly
    }

    public class OddSuckManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("Core References")]
        [SerializeField] private OddSuckUfoAutoMover ufoMover;
        [SerializeField] private OddSuckAudioManager audioManager;
        [SerializeField] private OddSuckFeedbackPopup feedbackPopup;
        [SerializeField] private RectTransform ufoMoveTransform;
        [SerializeField] private RectTransform ufoVisualTransform;
        [SerializeField] private Image ufoBodyImage;
        [SerializeField] private RectTransform beamTransform;
        [SerializeField] private CanvasGroup beamCanvasGroup;
        [SerializeField] private OddSuckUiParticleEmitter beamParticleEmitter;
        [Header("Pull Visual Style")]
        [Tooltip("Sucking Beam keeps the current UFO behavior. Rope Pull is for balloon/helicopter/crane themes.")]
        [SerializeField] private OddSuckPullVisualStyle pullVisualStyle = OddSuckPullVisualStyle.SuckingBeam;
        [Tooltip("Optional. Existing UFO scenes can leave this empty. Assign it for Rope Pull or cleaner future theme switching.")]
        [SerializeField] private OddSuckPullVisualController pullVisualController;
        [SerializeField] private RectTransform itemParent;
        [Tooltip("Fallback item template for old scenes. V5.3.1 prefers the dedicated text/image templates below.")]
        [SerializeField] private OddSuckItemView itemTemplate;
        [Header("Item Templates")]
        [Tooltip("Used by text/math mode for left-side slots.")]
        [SerializeField] private OddSuckItemView leftTextItemTemplate;
        [Tooltip("Used by text/math mode for middle/front slots.")]
        [SerializeField] private OddSuckItemView centerTextItemTemplate;
        [Tooltip("Used by text/math mode for right-side slots.")]
        [SerializeField] private OddSuckItemView rightTextItemTemplate;
        [Tooltip("Used by sprite/image-only mode. Keep this visually clean, usually icon-only.")]
        [SerializeField] private OddSuckItemView imageItemTemplate;
        [SerializeField] private Button pullInputButton;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset primaryFont;
        [SerializeField] private TMP_FontAsset secondaryFont;
        [SerializeField] private bool applyPrimaryFontToAllTexts = true;
        [SerializeField] private List<TMP_Text> secondaryFontTargets = new List<TMP_Text>();

        [Header("HUD References")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider healthDamageSlider;
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image healthDamageFillImage;
        [SerializeField] private RectTransform healthBarRoot;
        [SerializeField] private Slider timerSlider;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private RectTransform timerBarRoot;
        [SerializeField] private TMP_Text startPromptText;
        [SerializeField] private CanvasGroup startPromptCanvasGroup;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultScoreText;
        [SerializeField] private Button resultContinueButton;

        [Header("Boot Flow")]
        [SerializeField] private string gameDisplayName = "Odd Suck";
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TMP_Text loadingGameNameText;
        [SerializeField] private Slider loadingSlider;
        [SerializeField, Min(0.1f)] private float loadingPanelDuration = 1.25f;
        [SerializeField] private bool showLoadingPanel = true;
        [SerializeField] private bool playUfoEntryOnGameStart = true;
        [SerializeField, Min(0.1f)] private float ufoEntryDuration = 1.25f;

        [Header("How To Play Images")]
        [SerializeField] private TMP_Text howToText;
        [SerializeField] private Image howToImage;
        [Tooltip("Legacy single button label. If Previous/Next/Start buttons are not assigned, this button can still work as NEXT then START/BACK.")]
        [SerializeField] private TMP_Text howToButtonLabelText;
        [Header("Optional How To Navigation Buttons")]
        [SerializeField] private Button howToPreviousButton;
        [SerializeField] private Button howToNextButton;
        [SerializeField] private Button howToStartButton;
        [SerializeField] private TMP_Text howToStepCounterText;
        [SerializeField] private bool useHowToImagesWhenAvailable = true;
        [SerializeField] private List<Sprite> howToStepImages = new List<Sprite>();

        [Header("Bloom Reward System")]
        [SerializeField] private bool useBloomRewardSystem = true;
        [SerializeField] private string homeSceneName = "Loader Scene";
        [SerializeField, Min(1f)] private float expectedMaxTimeForReward = 120f;
        [SerializeField] private List<OddSuckBloomSkillConfig> bloomSkillConfigs = new List<OddSuckBloomSkillConfig>
        {
            new OddSuckBloomSkillConfig(BloomSkillType.Understand, 100f, 0.35f, 0.65f),
            new OddSuckBloomSkillConfig(BloomSkillType.Analyze, 75f, 0.45f, 0.55f)
        };

        [Header("Question Generators")]
        [SerializeField] private OddSuckPlayMode playMode = OddSuckPlayMode.MixedRandom;
        [SerializeField] private List<OddSuckQuestionGeneratorBase> questionGenerators = new List<OddSuckQuestionGeneratorBase>();
        [SerializeField] private bool autoFindGeneratorsOnStart = true;
        [SerializeField] private bool showSpriteLabels = false;

        [Header("Endless Health Gameplay")]
        [SerializeField, Min(1)] private int startingHealth = 3;
        [SerializeField, Min(1)] private int pointsPerCorrect = 10;
        [SerializeField, Min(1)] private int wrongAnswerHealthLoss = 1;
        [SerializeField] private bool noAlignedTapCostsHealth = false;
        [SerializeField, Min(1)] private int noAlignedHealthLoss = 1;
        [SerializeField] private bool autoStartOnPlay = true;

        [Header("How To Play Behaviour")]
        [SerializeField] private OddSuckHowToDisplayMode howToDisplayMode = OddSuckHowToDisplayMode.FirstTimeAutomatically;

        [Header("First-Time Interactive Tutorial")]
        [SerializeField] private OddSuckFirstTimeTutorialController firstTimeTutorial;

        [Header("Wave Timer")]
        [SerializeField] private bool useWaveTimer = true;
        [SerializeField, Min(1f)] private float startingWaveTime = 30f;
        [SerializeField, Min(0f)] private float waveTimeDecreasePerWave = 0.75f;
        [SerializeField, Min(1f)] private float minimumWaveTime = 15f;
        [SerializeField, Min(1)] private int timeoutHealthLoss = 1;
        [SerializeField] private string timeoutMessage = "Time Up!";
        [SerializeField, Min(0f)] private float timerLowWarningSeconds = 5f;

        [Header("Difficulty")]
        [SerializeField] private OddSuckDifficultyMode difficultyMode = OddSuckDifficultyMode.Normal;
        [SerializeField, Min(0.01f)] private float speedIncreasePerCorrect = 0.06f;
        [SerializeField, Min(1f)] private float maxSpeedMultiplier = 2.8f;
        [SerializeField, Min(1)] private int speedIncreaseEveryCorrectAnswers = 1;
        [SerializeField] private string firstPullPrompt = "Click anywhere to start pulling object";
        [SerializeField] private bool hidePromptAfterFirstPull = true;

        [Header("Rules")]
        [Tooltip("Old fallback tolerance used when Beam Catch Zone is disabled or beam reference is missing.")]
        [SerializeField, Min(1f)] private float alignmentTolerance = 90f;
        [Tooltip("Recommended ON. Pulls an item only when its center is inside the UFO beam catch zone. This prevents empty-space taps from grabbing the nearest object.")]
        [SerializeField] private bool useBeamCatchZone = true;
        [Tooltip("Width of the invisible catch area under the UFO beam in UI units. Lower value = stricter pull. Try 55 to 75 for stricter gameplay.")]
        [SerializeField, Min(1f)] private float beamCatchZoneWidth = 70f;

        [Header("Spawn Layout")]
        [SerializeField, Min(0f)] private float itemXPadding = 80f;
        [Tooltip("Keeps current behavior at 1. Increase slightly, like 1.1 to 1.35, to spread items farther apart without rebuilding the scene.")]
        [SerializeField, Range(0.5f, 2f)] private float itemSpacingMultiplier = 1f;
        [Tooltip("Optional minimum visual gap between item templates. Leave 0 to use automatic spacing only. If the area is too small, the script safely caps spacing to available width.")]
        [SerializeField, Min(0f)] private float minimumItemGap = 0f;
        [SerializeField, Min(0f)] private float randomItemJitterX = 30f;
        [SerializeField, Min(0f)] private float randomItemJitterY = 28f;

        [Header("Animation")]
        [SerializeField] private bool useItemFallSpawnAnimation = true;
        [SerializeField, Min(0f)] private float itemFallFromYOffset = 420f;
        [SerializeField, Min(0.1f)] private float itemFallSpawnDuration = 0.38f;
        [SerializeField, Min(0.05f)] private float beamGrowDuration = 0.14f;
        [SerializeField, Min(0.1f)] private float itemSuckDuration = 0.48f;
        [SerializeField, Min(0.1f)] private float nextWaveDelay = 0.95f;
        [SerializeField, Min(0.1f)] private float healthDamageDelay = 0.18f;
        [SerializeField, Min(0.1f)] private float healthDamageBarDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float gameOverUfoExitDuration = 1.15f;
        [SerializeField] private Ease itemSuckEase = Ease.InBack;
        [SerializeField] private bool pauseUfoDuringSuck = true;
        [SerializeField] private bool playUfoExitOnGameOver = true;

        private readonly List<OddSuckItemView> activeItems = new List<OddSuckItemView>();
        private readonly List<OddSuckQuestionGeneratorBase> usableGenerators = new List<OddSuckQuestionGeneratorBase>();
        private readonly List<SkillEntry> bloomSkills = new List<SkillEntry>();

        private OddSuckGeneratedQuestion currentQuestion;
        private ImageFlashTarget ufoFlashTarget;
        private Tween activeTween;
        private Tween beamTween;
        private Tween delayTween;
        private Tween promptTween;
        private Tween healthTween;
        private Tween timerWarnTween;
        private Tween resultTween;
        private int waveIndex;
        private int correctAnswers;
        private int mistakeCount;
        private int totalAttempts;
        private int score;
        private int health;
        private float currentSpeedMultiplier = 1f;
        private float currentWaveTime;
        private float waveTimeRemaining;
        private bool gameRunning;
        private bool waveLocked;
        private bool paused;
        private bool firstPullDone;
        private bool howToOpenedFromPause;
        private bool lowTimerWarningPlaying;
        private bool bloomPostGameShown;
        private bool tutorialHoldActive;
        private int howToImageIndex;
        private float gameplayStartTime;

        public OddSuckPlayMode PlayMode => playMode;
        public OddSuckPullVisualStyle PullVisualStyle => pullVisualStyle;
        public bool IsTutorialHoldingGameplay => tutorialHoldActive;

        private void Awake()
        {
            ApplyFonts();

            HideTemplate(itemTemplate);
            HideTemplate(leftTextItemTemplate);
            HideTemplate(centerTextItemTemplate);
            HideTemplate(rightTextItemTemplate);
            HideTemplate(imageItemTemplate);

            if (pullInputButton != null)
            {
                pullInputButton.onClick.RemoveListener(HandlePullInput);
                pullInputButton.onClick.AddListener(HandlePullInput);
            }

            if (resultContinueButton != null)
            {
                resultContinueButton.onClick.RemoveListener(ContinueFromResult);
                resultContinueButton.onClick.AddListener(ContinueFromResult);
            }

            if (ufoBodyImage != null)
            {
                ufoFlashTarget = new ImageFlashTarget(ufoBodyImage);
                ufoFlashTarget.CacheOriginalColor();
            }

            ConfigureBeamParticles();
            ConfigurePullVisualController();
            SetPullGuideVisible(false, true);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(loadingPanel, false);
            SetStartPromptVisible(false, true);
            SetHealthBarInstant(1f);
            SetTimerBarInstant(1f);
            UpdateHud();
        }

        private void Start()
        {
            if (autoFindGeneratorsOnStart)
            {
                RefreshGeneratorsFromChildren();
            }

            StartCoroutine(BootFlow());
        }

        private IEnumerator BootFlow()
        {
            gameRunning = false;
            paused = true;
            waveLocked = true;
            SetInputEnabled(false);
            ufoMover?.SetMovementEnabled(false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(loadingPanel, false);
            SetStartPromptVisible(false, true);

            if (TryStartBloomPreGame())
            {
                yield return new WaitUntil(IsBloomPreGameComplete);
            }

            if (showLoadingPanel)
            {
                yield return PlayLoadingPanelRoutine();
            }

            if (ShouldShowHowToAutomatically())
            {
                ShowHowToIntro();
                yield break;
            }

            if (autoStartOnPlay)
            {
                ContinueStartupAfterHowTo();
            }
        }

        private void Update()
        {
            if (tutorialHoldActive || !gameRunning || paused || waveLocked || !useWaveTimer)
            {
                return;
            }

            waveTimeRemaining -= Time.deltaTime;
            UpdateTimerHud(false);

            if (timerLowWarningSeconds > 0f && waveTimeRemaining <= timerLowWarningSeconds && !lowTimerWarningPlaying)
            {
                PlayTimerWarning();
            }

            if (waveTimeRemaining <= 0f)
            {
                HandleWaveTimeout();
            }
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public void StartGame()
        {
            tutorialHoldActive = false;
            KillTweens();
            ClearActiveItems();

            waveIndex = 0;
            correctAnswers = 0;
            mistakeCount = 0;
            totalAttempts = 0;
            score = 0;
            health = startingHealth;
            currentSpeedMultiplier = 1f;
            currentWaveTime = startingWaveTime;
            waveTimeRemaining = startingWaveTime;
            paused = true;
            gameRunning = false;
            waveLocked = true;
            firstPullDone = false;
            howToOpenedFromPause = false;
            lowTimerWarningPlaying = false;
            bloomPostGameShown = false;
            gameplayStartTime = Time.time;

            SetPanel(loadingPanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetInputEnabled(false);
            SetHealthBarInstant(1f);
            SetTimerBarInstant(1f);
            SetPullGuideVisible(false, true);
            SetStartPromptVisible(false, true);
            ufoMover?.SetSpeedMultiplier(currentSpeedMultiplier);
            ufoMover?.ResetToCenter();
            ufoMover?.SetMovementEnabled(false);
            UpdateHud();

            if (playUfoEntryOnGameStart && ufoMover != null)
            {
                ufoMover.PlayEntryFromTop(ufoVisualTransform, ufoEntryDuration, BeginActiveGameplay);
            }
            else
            {
                BeginActiveGameplay();
            }
        }

        private void BeginActiveGameplay()
        {
            gameplayStartTime = Time.time;
            audioManager?.PlayMusic();
            paused = false;
            gameRunning = true;
            waveLocked = false;
            SetInputEnabled(true);
            ufoMover?.SetMovementEnabled(true);
            LoadNextWave();
            UpdateStartPromptState();
            UpdateHud();
        }

        public void RestartGame()
        {
            if (tutorialHoldActive)
            {
                return;
            }

            audioManager?.PlayButton();
            StartGame();
        }

        public void ContinueFromResult()
        {
            audioManager?.PlayButton();
            ShowBloomPostGameFromResult();
        }

        public void PauseGame()
        {
            if (!gameRunning || waveLocked)
            {
                return;
            }

            audioManager?.PlayButton();
            paused = true;
            SetPanel(pausePanel, true);
            SetPanel(howToPlayPanel, false);
            SetInputEnabled(false);
            ufoMover?.SetMovementEnabled(false);
            SetStartPromptVisible(false, false);
            audioManager?.PauseMusic();
            StopTimerWarning();
        }

        public void ResumeGame()
        {
            if (!gameRunning)
            {
                return;
            }

            audioManager?.PlayButton();
            paused = false;
            howToOpenedFromPause = false;
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);
            SetInputEnabled(true);
            ufoMover?.SetMovementEnabled(true);
            audioManager?.ResumeMusic();
            UpdateStartPromptState();
        }

        public void TogglePause()
        {
            if (paused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        public void ShowHowToIntro()
        {
            if (tutorialHoldActive)
            {
                return;
            }

            paused = true;
            gameRunning = false;
            howToOpenedFromPause = false;
            howToImageIndex = 0;
            RefreshHowToContent();
            SetPanel(loadingPanel, false);
            SetPanel(howToPlayPanel, true);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetInputEnabled(false);
            ufoMover?.SetMovementEnabled(false);
            SetStartPromptVisible(false, true);
        }

        public void ShowHowToFromPause()
        {
            if (tutorialHoldActive)
            {
                return;
            }

            audioManager?.PlayButton();
            paused = true;
            howToOpenedFromPause = true;
            howToImageIndex = 0;
            RefreshHowToContent();
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, true);
            SetInputEnabled(false);
            ufoMover?.SetMovementEnabled(false);
            SetStartPromptVisible(false, false);
            audioManager?.PauseMusic();
            StopTimerWarning();
        }

        public void CloseHowToPanel()
        {
            if (tutorialHoldActive)
            {
                return;
            }

            audioManager?.PlayButton();

            if (TryAdvanceHowToImage())
            {
                return;
            }

            SetPanel(howToPlayPanel, false);
            MarkHowToViewed();

            if (howToOpenedFromPause && gameRunning)
            {
                SetPanel(pausePanel, true);
                paused = true;
                return;
            }

            ContinueStartupAfterHowTo();
        }

        public void ShowHowToManually()
        {
            if (tutorialHoldActive)
            {
                return;
            }

            if (gameRunning)
            {
                ShowHowToFromPause();
                return;
            }

            ShowHowToIntro();
        }

        [ContextMenu("Reset How To Viewed Status")]
        public void ResetHowToViewedStatus()
        {
            PlayerPrefs.DeleteKey(GetHowToPrefsKey());
            PlayerPrefs.Save();
        }

        [ContextMenu("Reset First-Time Tutorial Progress")]
        public void ResetFirstTimeTutorialProgress()
        {
            ResolveFirstTimeTutorial();
            firstTimeTutorial?.ResetSavedCompletion();
        }

        [ContextMenu("Force Play First-Time Tutorial")]
        public void ForcePlayFirstTimeTutorial()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("OddSuckManager: Force Play Tutorial is available while the game is running.", this);
                return;
            }

            ResolveFirstTimeTutorial();
            if (firstTimeTutorial == null)
            {
                Debug.LogWarning("OddSuckManager: No first-time tutorial controller is assigned.", this);
                return;
            }

            BeginTutorial(true);
        }

        private void ContinueStartupAfterHowTo()
        {
            ResolveFirstTimeTutorial();

            if (firstTimeTutorial != null && firstTimeTutorial.ShouldPlayAutomatically())
            {
                BeginTutorial(false);
                return;
            }

            StartGame();
        }

        private void BeginTutorial(bool forcePlay)
        {
            if (firstTimeTutorial == null)
            {
                StartGame();
                return;
            }

            KillTweens();
            ClearActiveItems();
            tutorialHoldActive = true;
            gameRunning = false;
            paused = true;
            waveLocked = true;
            SetPanel(loadingPanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetInputEnabled(false);
            SetStartPromptVisible(false, true);
            SetPullGuideVisible(false, true);
            StopTimerWarning();
            ufoMover?.SetMovementEnabled(false);

            firstTimeTutorial.BeginTutorial(this, HandleInteractiveTutorialCompleted, forcePlay);
        }

        private void HandleInteractiveTutorialCompleted()
        {
            tutorialHoldActive = false;
            StartGame();
        }

        private void ResolveFirstTimeTutorial()
        {
            if (firstTimeTutorial == null)
            {
                firstTimeTutorial = GetComponentInChildren<OddSuckFirstTimeTutorialController>(true);
            }

            if (firstTimeTutorial == null)
            {
                OddSuckFirstTimeTutorialController[] tutorials = Resources.FindObjectsOfTypeAll<OddSuckFirstTimeTutorialController>();
                for (int i = 0; i < tutorials.Length; i++)
                {
                    if (tutorials[i] != null && tutorials[i].gameObject.scene == gameObject.scene)
                    {
                        firstTimeTutorial = tutorials[i];
                        break;
                    }
                }
            }
        }

        private bool ShouldShowHowToAutomatically()
        {
            if (howToPlayPanel == null)
            {
                return false;
            }

            switch (howToDisplayMode)
            {
                case OddSuckHowToDisplayMode.EveryGameStartAutomatically:
                    return true;
                case OddSuckHowToDisplayMode.ManualButtonOnly:
                    return false;
                default:
                    return PlayerPrefs.GetInt(GetHowToPrefsKey(), 0) == 0;
            }
        }

        private void MarkHowToViewed()
        {
            PlayerPrefs.SetInt(GetHowToPrefsKey(), 1);
            PlayerPrefs.Save();
        }

        private static string GetHowToPrefsKey()
        {
            return $"OddSuck.HowToViewed.{SceneManager.GetActiveScene().name}";
        }

        private IEnumerator PlayLoadingPanelRoutine()
        {
            SetPanel(loadingPanel, true);

            if (loadingGameNameText != null)
            {
                loadingGameNameText.text = string.IsNullOrWhiteSpace(gameDisplayName) ? "Odd Suck" : gameDisplayName;
            }

            if (loadingSlider != null)
            {
                loadingSlider.minValue = 0f;
                loadingSlider.maxValue = 1f;
                loadingSlider.SetValueWithoutNotify(0f);
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, loadingPanelDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                if (loadingSlider != null)
                {
                    loadingSlider.SetValueWithoutNotify(normalized);
                }
                yield return null;
            }

            if (loadingSlider != null)
            {
                loadingSlider.SetValueWithoutNotify(1f);
            }

            yield return new WaitForSecondsRealtime(0.12f);
            SetPanel(loadingPanel, false);
        }

        private bool TryStartBloomPreGame()
        {
            if (!useBloomRewardSystem)
            {
                return false;
            }

            try
            {
                if (RewardManager.Instance == null)
                {
                    Debug.LogWarning("OddSuckManager: RewardManager.Instance not found. Skipping Bloom pre-game flow for direct scene testing.");
                    return false;
                }

                BuildBloomSkills();
                RewardManager.Instance.ShowPreGame(bloomSkills);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"OddSuckManager: Bloom pre-game failed, continuing local flow. {exception.Message}");
                return false;
            }
        }

        private bool IsBloomPreGameComplete()
        {
            if (!useBloomRewardSystem)
            {
                return true;
            }

            try
            {
                return RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete;
            }
            catch
            {
                return true;
            }
        }

        private void BuildBloomSkills()
        {
            bloomSkills.Clear();

            if (bloomSkillConfigs != null)
            {
                for (int i = 0; i < bloomSkillConfigs.Count; i++)
                {
                    OddSuckBloomSkillConfig config = bloomSkillConfigs[i];
                    if (config == null)
                    {
                        continue;
                    }

                    bloomSkills.Add(new SkillEntry(config.skillType, Mathf.Max(1f, config.maxScore), timeWeight: config.timeWeight, accuracyWeight: config.accuracyWeight));
                }
            }

            if (bloomSkills.Count == 0)
            {
                bloomSkills.Add(new SkillEntry(BloomSkillType.Understand, 100f));
                bloomSkills.Add(new SkillEntry(BloomSkillType.Analyze, 75f));
            }
        }

        private void ShowBloomPostGameFromResult()
        {
            if (bloomPostGameShown)
            {
                return;
            }

            bloomPostGameShown = true;
            SetPanel(resultPanel, false);

            if (!useBloomRewardSystem)
            {
                Debug.Log("OddSuckManager: Bloom is disabled. Continue button is ready for future navigation.");
                return;
            }

            try
            {
                if (RewardManager.Instance == null)
                {
                    Debug.LogWarning("OddSuckManager: RewardManager.Instance not found. Cannot open Bloom post-game panel.");
                    SetPanel(resultPanel, true);
                    bloomPostGameShown = false;
                    return;
                }

                BuildBloomSkills();
                float timeTaken = Mathf.Max(0f, Time.time - gameplayStartTime);
                float timeScore = Mathf.Clamp01(1f - (timeTaken / Mathf.Max(1f, expectedMaxTimeForReward)));
                float accuracyScore = totalAttempts > 0 ? Mathf.Clamp01((float)correctAnswers / totalAttempts) : 0f;

                GameEvaluationData evaluationData = new GameEvaluationData
                {
                    timeScore = timeScore,
                    accuracyScore = accuracyScore,
                    mistakeCount = mistakeCount,
                    timeTaken = timeTaken
                };

                RewardManager.Instance.ShowPostGame(bloomSkills, evaluationData);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"OddSuckManager: Bloom post-game failed. {exception.Message}");
                SetPanel(resultPanel, true);
                bloomPostGameShown = false;
            }
        }

        private void RegisterMistakeAttempt()
        {
            totalAttempts++;
            mistakeCount++;
        }

        public void OnRewardScreenOpen()
        {
            audioManager?.StopAllAudio();
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

            SceneManager.LoadScene(string.IsNullOrWhiteSpace(homeSceneName) ? "Loader Scene" : homeSceneName);
        }

        public void ShowPreviousHowToStep()
        {
            audioManager?.PlayButton();

            if (!IsHowToImageModeActive())
            {
                return;
            }

            howToImageIndex = Mathf.Max(0, howToImageIndex - 1);
            RefreshHowToContent();
        }

        public void ShowNextHowToStep()
        {
            audioManager?.PlayButton();

            if (TryAdvanceHowToImage())
            {
                return;
            }

            CloseHowToPanel();
        }

        private bool TryAdvanceHowToImage()
        {
            if (!IsHowToImageModeActive())
            {
                return false;
            }

            int count = GetValidHowToImageCount();
            if (howToImageIndex >= count - 1)
            {
                return false;
            }

            howToImageIndex++;
            RefreshHowToContent();
            return true;
        }

        private void RefreshHowToContent()
        {
            bool imageMode = IsHowToImageModeActive();
            int imageCount = imageMode ? GetValidHowToImageCount() : 0;
            bool hasMultipleImages = imageMode && imageCount > 1;
            bool isFirstImage = !imageMode || howToImageIndex <= 0;
            bool isLastImage = !imageMode || howToImageIndex >= imageCount - 1;

            if (howToImage != null)
            {
                howToImage.gameObject.SetActive(imageMode);
                howToImage.sprite = imageMode ? GetValidHowToImageAt(howToImageIndex) : null;
                howToImage.preserveAspect = true;
            }

            if (howToText != null)
            {
                howToText.gameObject.SetActive(!imageMode);
            }

            if (howToPreviousButton != null)
            {
                howToPreviousButton.gameObject.SetActive(hasMultipleImages);
                howToPreviousButton.interactable = hasMultipleImages && !isFirstImage;
            }

            if (howToNextButton != null)
            {
                howToNextButton.gameObject.SetActive(hasMultipleImages && !isLastImage);
                howToNextButton.interactable = hasMultipleImages && !isLastImage;
            }

            if (howToStartButton != null)
            {
                howToStartButton.gameObject.SetActive(!imageMode || isLastImage);
            }

            if (howToStepCounterText != null)
            {
                howToStepCounterText.gameObject.SetActive(hasMultipleImages);
                howToStepCounterText.text = hasMultipleImages ? $"{howToImageIndex + 1} / {imageCount}" : string.Empty;
            }

            if (howToButtonLabelText != null)
            {
                howToButtonLabelText.text = isLastImage ? (howToOpenedFromPause ? "BACK" : "START") : "NEXT";
            }
        }

        private bool IsHowToImageModeActive()
        {
            return useHowToImagesWhenAvailable && howToImage != null && GetValidHowToImageCount() > 0;
        }

        private int GetValidHowToImageCount()
        {
            if (howToStepImages == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < howToStepImages.Count; i++)
            {
                if (howToStepImages[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private Sprite GetValidHowToImageAt(int validIndex)
        {
            if (howToStepImages == null)
            {
                return null;
            }

            int current = 0;
            for (int i = 0; i < howToStepImages.Count; i++)
            {
                if (howToStepImages[i] == null)
                {
                    continue;
                }

                if (current == validIndex)
                {
                    return howToStepImages[i];
                }

                current++;
            }

            return null;
        }

        public void HandlePullInput()
        {
            if (tutorialHoldActive || !gameRunning || paused || waveLocked)
            {
                return;
            }

            if (!firstPullDone)
            {
                firstPullDone = true;
                if (hidePromptAfterFirstPull)
                {
                    SetStartPromptVisible(false, false);
                }
            }

            OddSuckItemView target = FindNearestAlignedItem(out _);
            if (target == null)
            {
                feedbackPopup?.Show("Wait for UFO to align", Color.white);
                audioManager?.PlayNoTarget();
                ufoMover?.PlayWrongUfoAnimation(ufoVisualTransform, ufoFlashTarget);

                if (noAlignedTapCostsHealth)
                {
                    RegisterMistakeAttempt();
                    ApplyHealthLoss(noAlignedHealthLoss);
                    if (health <= 0)
                    {
                        ScheduleGameOver("Game Over");
                    }
                }

                return;
            }

            StartSuckSequence(target);
        }

        private void LoadNextWave()
        {
            ClearActiveItems();
            waveLocked = false;
            lowTimerWarningPlaying = false;
            StopTimerWarning();

            OddSuckQuestionGeneratorBase generator = SelectGenerator();
            if (generator == null)
            {
                Debug.LogWarning("OddSuckManager has no usable question generator. Add OddSuckMathQuestionGenerator, OddSuckSpriteCategoryQuestionGenerator, or OddSuckEnglishWordQuestionGenerator.");
                FinishGame("No Generator");
                return;
            }

            waveIndex++;
            currentQuestion = generator.Generate(waveIndex);

            if (!IsQuestionUsable(currentQuestion))
            {
                Debug.LogWarning($"Generator '{generator.GeneratorName}' created an unusable question. Check its inspector data.");
                FinishGame("Bad Question Data");
                return;
            }

            currentWaveTime = GetWaveTime(waveIndex);
            waveTimeRemaining = currentWaveTime;

            SetInputEnabled(true);
            ufoMover?.SetSpeedMultiplier(currentSpeedMultiplier);
            ufoMover?.SetMovementEnabled(true);
            SetPullGuideVisible(difficultyMode == OddSuckDifficultyMode.Easy, true);
            SpawnQuestion(currentQuestion);
            UpdateStartPromptState();
            UpdateHud();
            UpdateTimerHud(true);
        }

        private float GetWaveTime(int index)
        {
            float calculated = startingWaveTime - Mathf.Max(0, index - 1) * waveTimeDecreasePerWave;
            return Mathf.Max(minimumWaveTime, calculated);
        }

        private OddSuckQuestionGeneratorBase SelectGenerator()
        {
            usableGenerators.Clear();
            int totalWeight = 0;

            if (autoFindGeneratorsOnStart && questionGenerators.Count == 0)
            {
                RefreshGeneratorsFromChildren();
            }

            for (int i = 0; i < questionGenerators.Count; i++)
            {
                OddSuckQuestionGeneratorBase generator = questionGenerators[i];
                if (generator != null && generator.enabled && generator.CanGenerate() && IsGeneratorAllowedForPlayMode(generator))
                {
                    usableGenerators.Add(generator);
                    totalWeight += generator.SelectionWeight;
                }
            }

            if (usableGenerators.Count == 0)
            {
                return null;
            }

            int roll = UnityEngine.Random.Range(0, Mathf.Max(1, totalWeight));
            int running = 0;
            for (int i = 0; i < usableGenerators.Count; i++)
            {
                running += usableGenerators[i].SelectionWeight;
                if (roll < running)
                {
                    return usableGenerators[i];
                }
            }

            return usableGenerators[usableGenerators.Count - 1];
        }

        private bool IsGeneratorAllowedForPlayMode(OddSuckQuestionGeneratorBase generator)
        {
            if (generator == null)
            {
                return false;
            }

            switch (playMode)
            {
                case OddSuckPlayMode.MathOnly:
                    return generator is OddSuckMathQuestionGenerator;
                case OddSuckPlayMode.SpriteOnly:
                    return generator is OddSuckSpriteCategoryQuestionGenerator;
                case OddSuckPlayMode.EnglishOnly:
                    return generator is OddSuckEnglishWordQuestionGenerator;
                default:
                    return true;
            }
        }

        private void RefreshGeneratorsFromChildren()
        {
            questionGenerators.Clear();
            OddSuckQuestionGeneratorBase[] foundGenerators = GetComponentsInChildren<OddSuckQuestionGeneratorBase>(true);
            for (int i = 0; i < foundGenerators.Length; i++)
            {
                questionGenerators.Add(foundGenerators[i]);
            }
        }

        private static bool IsQuestionUsable(OddSuckGeneratedQuestion question)
        {
            if (question == null || question.items == null || question.items.Count < 2)
            {
                return false;
            }

            bool hasOdd = false;
            bool hasNormal = false;
            for (int i = 0; i < question.items.Count; i++)
            {
                if (question.items[i] == null)
                {
                    continue;
                }

                if (question.items[i].isOdd)
                {
                    hasOdd = true;
                }
                else
                {
                    hasNormal = true;
                }
            }

            return hasOdd && hasNormal;
        }

        private void SpawnQuestion(OddSuckGeneratedQuestion question)
        {
            if (questionText != null)
            {
                questionText.text = string.IsNullOrWhiteSpace(question.questionText) ? "Find the odd one" : question.questionText;
            }

            List<OddSuckItemData> spawnItems = new List<OddSuckItemData>(question.items);
            Shuffle(spawnItems);

            for (int i = 0; i < spawnItems.Count; i++)
            {
                OddSuckItemTemplateSide templateSide = GetTemplateSideForSlot(i, spawnItems.Count, question.displayMode);
                OddSuckItemView template = GetTemplateForSlot(question.displayMode, templateSide);

                if (template == null)
                {
                    Debug.LogError("OddSuckManager has no item template assigned. Assign Image/Text templates or the fallback Item Template.", this);
                    continue;
                }

                OddSuckItemView view = Instantiate(template, itemParent);
                view.gameObject.SetActive(true);
                view.Setup(spawnItems[i], question.displayMode, showSpriteLabels, templateSide);
                view.RectTransform.anchoredPosition = GetSpawnPosition(i, spawnItems.Count, view.RectTransform);
                view.PlaySpawn(i * 0.04f, useItemFallSpawnAnimation, itemFallFromYOffset, itemFallSpawnDuration);
                activeItems.Add(view);
            }
        }

        private OddSuckItemView GetTemplateForSlot(OddSuckItemDisplayMode displayMode, OddSuckItemTemplateSide templateSide)
        {
            if (displayMode == OddSuckItemDisplayMode.Sprite)
            {
                return imageItemTemplate != null ? imageItemTemplate : itemTemplate;
            }

            switch (templateSide)
            {
                case OddSuckItemTemplateSide.Left:
                    return leftTextItemTemplate != null ? leftTextItemTemplate : GetFallbackTextTemplate();
                case OddSuckItemTemplateSide.Right:
                    return rightTextItemTemplate != null ? rightTextItemTemplate : GetFallbackTextTemplate();
                default:
                    return centerTextItemTemplate != null ? centerTextItemTemplate : GetFallbackTextTemplate();
            }
        }

        private OddSuckItemView GetFallbackTextTemplate()
        {
            if (centerTextItemTemplate != null)
            {
                return centerTextItemTemplate;
            }

            return itemTemplate;
        }

        private static void HideTemplate(OddSuckItemView template)
        {
            if (template != null)
            {
                template.gameObject.SetActive(false);
            }
        }

        private OddSuckItemTemplateSide GetTemplateSideForSlot(int index, int total, OddSuckItemDisplayMode displayMode)
        {
            if (displayMode == OddSuckItemDisplayMode.Sprite)
            {
                return OddSuckItemTemplateSide.ImageMode;
            }

            switch (Mathf.Clamp(total, 1, 6))
            {
                case 1:
                    return OddSuckItemTemplateSide.Center;
                case 2:
                    return index == 0 ? OddSuckItemTemplateSide.Left : OddSuckItemTemplateSide.Right;
                case 3:
                    if (index == 0) return OddSuckItemTemplateSide.Left;
                    if (index == 1) return OddSuckItemTemplateSide.Center;
                    return OddSuckItemTemplateSide.Right;
                case 4:
                    if (index == 0) return OddSuckItemTemplateSide.Left;
                    if (index == 3) return OddSuckItemTemplateSide.Right;
                    return OddSuckItemTemplateSide.Center;
                case 5:
                    if (index <= 1) return OddSuckItemTemplateSide.Left;
                    if (index == 2) return OddSuckItemTemplateSide.Center;
                    return OddSuckItemTemplateSide.Right;
                default:
                    if (index <= 1) return OddSuckItemTemplateSide.Left;
                    if (index >= total - 2) return OddSuckItemTemplateSide.Right;
                    return OddSuckItemTemplateSide.Center;
            }
        }

        private Vector2 GetSpawnPosition(int index, int total, RectTransform spawnedItemRect)
        {
            if (itemParent == null || total <= 0)
            {
                return Vector2.zero;
            }

            Rect rect = itemParent.rect;
            float safeMinX = rect.xMin + itemXPadding;
            float safeMaxX = rect.xMax - itemXPadding;
            float availableWidth = Mathf.Max(1f, safeMaxX - safeMinX);
            float centerX = (safeMinX + safeMaxX) * 0.5f;

            float x;
            float finalCenterSpacing = 0f;
            if (total <= 1)
            {
                x = centerX;
            }
            else
            {
                float baseSpacing = availableWidth / (total + 1f);
                float desiredSpacing = baseSpacing * Mathf.Max(0.01f, itemSpacingMultiplier);

                float itemWidth = spawnedItemRect != null ? Mathf.Abs(spawnedItemRect.rect.width) : 0f;
                if (minimumItemGap > 0f && itemWidth > 0f)
                {
                    desiredSpacing = Mathf.Max(desiredSpacing, itemWidth + minimumItemGap);
                }

                float maxSpacingThatFits = availableWidth / Mathf.Max(1f, total - 1f);
                finalCenterSpacing = Mathf.Min(desiredSpacing, maxSpacingThatFits);

                float centeredIndex = index - ((total - 1f) * 0.5f);
                x = centerX + centeredIndex * finalCenterSpacing;
            }

            float y = UnityEngine.Random.Range(-randomItemJitterY, randomItemJitterY);

            float jitterX = randomItemJitterX;
            if (minimumItemGap > 0f && spawnedItemRect != null && total > 1)
            {
                float itemWidth = Mathf.Abs(spawnedItemRect.rect.width);
                float spareGap = Mathf.Max(0f, finalCenterSpacing - itemWidth - minimumItemGap);
                jitterX = Mathf.Min(jitterX, spareGap * 0.5f);
            }

            if (jitterX > 0f)
            {
                x += UnityEngine.Random.Range(-jitterX, jitterX);
            }

            x = Mathf.Clamp(x, safeMinX, safeMaxX);
            return new Vector2(x, y);
        }

        private OddSuckItemView FindNearestAlignedItem(out float bestDistance)
        {
            bestDistance = float.MaxValue;
            OddSuckItemView best = null;

            RectTransform alignmentReference = beamTransform != null ? beamTransform : ufoMoveTransform;
            if (alignmentReference == null)
            {
                return null;
            }

            float referenceX = GetLocalUiX(alignmentReference);
            float allowedDistance = useBeamCatchZone
                ? beamCatchZoneWidth * 0.5f
                : alignmentTolerance;

            for (int i = 0; i < activeItems.Count; i++)
            {
                OddSuckItemView item = activeItems[i];
                if (item == null || !item.gameObject.activeInHierarchy || item.RectTransform == null)
                {
                    continue;
                }

                float itemX = GetLocalUiX(item.RectTransform);
                float distance = Mathf.Abs(referenceX - itemX);

                if (distance <= allowedDistance && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = item;
                }
            }

            return best;
        }

        private float GetLocalUiX(RectTransform source)
        {
            if (source == null)
            {
                return 0f;
            }

            RectTransform referenceParent = itemParent != null ? itemParent : source.parent as RectTransform;
            if (referenceParent == null)
            {
                return source.position.x;
            }

            return referenceParent.InverseTransformPoint(source.position).x;
        }

        private void StartSuckSequence(OddSuckItemView target)
        {
            waveLocked = true;
            SetInputEnabled(false);
            SetStartPromptVisible(false, false);
            StopTimerWarning();

            if (pauseUfoDuringSuck)
            {
                ufoMover?.SetMovementEnabled(false);
            }

            audioManager?.PlaySuck();
            activeTween?.Kill();

            bool correct = target.IsOdd;
            Vector3 targetScale = correct ? Vector3.one * 0.16f : Vector3.one * 0.36f;

            Sequence sequence = DOTween.Sequence();
            sequence.AppendCallback(() =>
            {
                target.MarkSelected(true);

                bool easyGuideAlreadyVisible = difficultyMode == OddSuckDifficultyMode.Easy
                    && beamTransform != null
                    && beamTransform.gameObject.activeSelf;

                PlayPullVisualStart(target, easyGuideAlreadyVisible);
            });
            float pullStartDuration = GetPullStartDuration();
            float itemPullDuration = GetItemPullDuration();

            sequence.AppendInterval(pullStartDuration);
            sequence.AppendCallback(() => PlayPullVisualActive(target));
            sequence.Append(target.transform.DOMove(ufoMoveTransform.position, itemPullDuration).SetEase(itemSuckEase));
            sequence.Join(target.transform.DOScale(targetScale, itemPullDuration).SetEase(Ease.InBack));

            if (!correct)
            {
                sequence.Append(target.transform.DOPunchPosition(Vector3.down * 70f, 0.22f, 7, 0.7f));
            }

            sequence.AppendCallback(() => FinishWave(correct, target));
            activeTween = sequence.SetLink(gameObject);
        }

        private void FinishWave(bool correct, OddSuckItemView selectedItem)
        {
            if (!gameRunning)
            {
                return;
            }

            waveLocked = true;
            SetInputEnabled(false);

            StopPullActiveEffect();

            if (difficultyMode == OddSuckDifficultyMode.Normal || pullVisualStyle == OddSuckPullVisualStyle.RopePull)
            {
                HidePullVisual(false);
            }

            if (selectedItem != null)
            {
                selectedItem.gameObject.SetActive(false);
            }

            if (correct)
            {
                totalAttempts++;
                correctAnswers++;
                score += pointsPerCorrect;
                feedbackPopup?.Show($"+{pointsPerCorrect} Correct!", new Color(0.3f, 1f, 0.45f));
                audioManager?.PlayCorrect();
                ufoMover?.PlayCorrectUfoAnimation(ufoVisualTransform, ufoFlashTarget);
                UpdateSpeedAfterCorrect();
            }
            else
            {
                RegisterMistakeAttempt();
                feedbackPopup?.Show($"Wrong! -{wrongAnswerHealthLoss}", new Color(1f, 0.45f, 0.35f));
                audioManager?.PlayWrong();
                ufoMover?.PlayWrongUfoAnimation(ufoVisualTransform, ufoFlashTarget);
                ApplyHealthLoss(wrongAnswerHealthLoss);
            }

            UpdateHud();

            if (health <= 0)
            {
                ScheduleGameOver("Game Over");
                return;
            }

            delayTween?.Kill();
            delayTween = DOVirtual.DelayedCall(nextWaveDelay, LoadNextWave).SetLink(gameObject);
        }

        private void HandleWaveTimeout()
        {
            if (!gameRunning || waveLocked)
            {
                return;
            }

            waveLocked = true;
            SetInputEnabled(false);
            SetStartPromptVisible(false, false);
            StopTimerWarning();
            HidePullVisual(false);
            RegisterMistakeAttempt();

            feedbackPopup?.Show($"{timeoutMessage} -{timeoutHealthLoss}", new Color(1f, 0.85f, 0.25f));
            audioManager?.PlayWrong();
            ufoMover?.PlayWrongUfoAnimation(ufoVisualTransform, ufoFlashTarget);
            ApplyHealthLoss(timeoutHealthLoss);
            UpdateHud();

            if (health <= 0)
            {
                ScheduleGameOver("Game Over");
                return;
            }

            delayTween?.Kill();
            delayTween = DOVirtual.DelayedCall(nextWaveDelay, LoadNextWave).SetLink(gameObject);
        }

        private void UpdateSpeedAfterCorrect()
        {
            if (speedIncreaseEveryCorrectAnswers <= 0 || correctAnswers % speedIncreaseEveryCorrectAnswers != 0)
            {
                return;
            }

            currentSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, 1f + correctAnswers * speedIncreasePerCorrect);
            ufoMover?.SetSpeedMultiplier(currentSpeedMultiplier);
            ufoMover?.PlaySpeedUpAnimation(ufoVisualTransform);
        }

        private void ApplyHealthLoss(int amount)
        {
            int previousHealth = health;
            health = Mathf.Max(0, health - Mathf.Max(0, amount));
            AnimateHealthLoss(previousHealth, health);
            UpdateHud();
        }

        private void ScheduleGameOver(string title)
        {
            waveLocked = true;
            SetInputEnabled(false);
            ufoMover?.SetMovementEnabled(false);
            HidePullVisual(false);
            StopTimerWarning();

            delayTween?.Kill();
            float delay = Mathf.Max(0f, healthDamageDelay) + Mathf.Max(0f, healthDamageBarDuration) + 0.16f;
            delayTween = DOVirtual.DelayedCall(delay, () => FinishGame(title)).SetLink(gameObject);
        }

        private void FinishGame(string title)
        {
            if (!gameRunning && resultPanel != null && resultPanel.activeSelf)
            {
                return;
            }

            gameRunning = false;
            paused = false;
            waveLocked = false;
            KillTweens();
            ClearActiveItems();
            SetInputEnabled(false);
            ufoMover?.SetMovementEnabled(false);
            SetPullGuideVisible(false, true);
            SetStartPromptVisible(false, true);
            SetPanel(pausePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(resultPanel, false);
            audioManager?.StopMusic();
            audioManager?.PlayGameOver();
            StopTimerWarning();

            if (playUfoExitOnGameOver && ufoMover != null && ufoMoveTransform != null)
            {
                ufoMover.PlayExitToSpace(ufoVisualTransform, gameOverUfoExitDuration, () => ShowResultPanel(title));
            }
            else
            {
                ShowResultPanel(title);
            }

            UpdateHud();
        }

        private void ShowResultPanel(string title)
        {
            SetPanel(resultPanel, true);

            if (resultTitleText != null)
            {
                resultTitleText.text = title;
            }

            if (resultScoreText != null)
            {
                resultScoreText.text = $"Score: {score}\nWaves: {Mathf.Max(0, waveIndex - 1)}\nCorrect: {correctAnswers}";
            }

            if (resultPanel != null)
            {
                resultTween?.Kill();
                resultPanel.transform.localScale = Vector3.one * 0.82f;
                resultTween = resultPanel.transform.DOScale(Vector3.one, 0.32f).SetEase(Ease.OutBack).SetLink(resultPanel);
            }
        }

        private void ClearActiveItems()
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                if (activeItems[i] != null)
                {
                    Destroy(activeItems[i].gameObject);
                }
            }

            activeItems.Clear();
        }

        private void KillTweens()
        {
            activeTween?.Kill();
            beamTween?.Kill();
            delayTween?.Kill();
            promptTween?.Kill();
            healthTween?.Kill();
            timerWarnTween?.Kill();
            resultTween?.Kill();
        }

        private void SetInputEnabled(bool enabled)
        {
            if (pullInputButton != null)
            {
                pullInputButton.interactable = enabled;
            }
        }

        private void UpdateStartPromptState()
        {
            bool shouldShow = gameRunning && !paused && !waveLocked && difficultyMode == OddSuckDifficultyMode.Normal && !firstPullDone;
            SetStartPromptVisible(shouldShow, false);
        }

        private void SetStartPromptVisible(bool visible, bool instant)
        {
            promptTween?.Kill();

            if (startPromptText != null)
            {
                startPromptText.text = firstPullPrompt;
                startPromptText.gameObject.SetActive(visible);
            }

            if (startPromptCanvasGroup == null)
            {
                return;
            }

            startPromptCanvasGroup.gameObject.SetActive(visible);

            if (!visible)
            {
                startPromptCanvasGroup.alpha = 0f;
                return;
            }

            if (instant)
            {
                startPromptCanvasGroup.alpha = 1f;
                return;
            }

            startPromptCanvasGroup.alpha = 0.35f;
            startPromptCanvasGroup.transform.localScale = Vector3.one;
            promptTween = DOTween.Sequence()
                .Append(DOTween.To(() => startPromptCanvasGroup.alpha, value => startPromptCanvasGroup.alpha = value, 1f, 0.65f))
                .Join(startPromptCanvasGroup.transform.DOScale(Vector3.one * 1.04f, 0.65f))
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(startPromptCanvasGroup.gameObject);
        }

        private void ConfigurePullVisualController()
        {
            if (pullVisualController == null)
            {
                pullVisualController = GetComponentInChildren<OddSuckPullVisualController>(true);
            }

            if (pullVisualController == null)
            {
                return;
            }

            pullVisualController.ConfigureFallbacks(beamTransform, beamCanvasGroup, beamParticleEmitter);
        }

        private bool HasPullVisualController()
        {
            return pullVisualController != null && pullVisualController.isActiveAndEnabled;
        }


        private float GetPullStartDuration()
        {
            if (HasPullVisualController())
            {
                return pullVisualController.GetPullStartDuration(pullVisualStyle, beamGrowDuration);
            }

            return beamGrowDuration;
        }

        private float GetItemPullDuration()
        {
            if (HasPullVisualController())
            {
                return pullVisualController.GetItemPullDuration(pullVisualStyle, itemSuckDuration);
            }

            return itemSuckDuration;
        }

        private float GetPullHideDuration()
        {
            if (HasPullVisualController())
            {
                return pullVisualController.GetPullHideDuration(pullVisualStyle, beamGrowDuration);
            }

            return beamGrowDuration;
        }

        private void SetPullGuideVisible(bool visible, bool instant)
        {
            if (HasPullVisualController())
            {
                pullVisualController.SetIdleGuide(pullVisualStyle, visible, instant, GetPullStartDuration());
                return;
            }

            if (pullVisualStyle == OddSuckPullVisualStyle.RopePull)
            {
                SetBeamParticlesVisible(false);
                SetBeamVisible(false, true);
                return;
            }

            SetBeamVisible(visible, instant);
        }

        private void PlayPullVisualStart(OddSuckItemView target, bool easyGuideAlreadyVisible)
        {
            RectTransform targetRect = target != null ? target.RectTransform : null;

            if (HasPullVisualController())
            {
                pullVisualController.PlayPullStart(pullVisualStyle, targetRect, ufoMoveTransform, easyGuideAlreadyVisible, GetPullStartDuration());
                return;
            }

            if (pullVisualStyle == OddSuckPullVisualStyle.RopePull)
            {
                SetBeamParticlesVisible(false);
                SetBeamVisible(false, true);
                return;
            }

            if (!easyGuideAlreadyVisible)
            {
                SetBeamVisible(true, false);
            }

            SetBeamParticlesVisible(true);
        }

        private void PlayPullVisualActive(OddSuckItemView target)
        {
            if (!HasPullVisualController())
            {
                return;
            }

            RectTransform targetRect = target != null ? target.RectTransform : null;
            pullVisualController.PlayPullActive(pullVisualStyle, targetRect, ufoMoveTransform, GetItemPullDuration());
        }

        private void StopPullActiveEffect()
        {
            if (HasPullVisualController())
            {
                pullVisualController.StopActiveEffect(pullVisualStyle);
                return;
            }

            SetBeamParticlesVisible(false);
        }

        private void HidePullVisual(bool instant)
        {
            if (HasPullVisualController())
            {
                pullVisualController.HidePullVisual(pullVisualStyle, instant, GetPullHideDuration());
                return;
            }

            SetBeamParticlesVisible(false);
            SetBeamVisible(false, instant);
        }

        private void ConfigureBeamParticles()
        {
            if (beamParticleEmitter == null || beamTransform == null)
            {
                return;
            }

            beamParticleEmitter.SetBeamTarget(beamTransform);
            beamParticleEmitter.StopAllParticles();
        }

        private void SetBeamVisible(bool visible, bool instant)
        {
            if (beamTransform == null)
            {
                return;
            }

            beamTween?.Kill();

            if (instant)
            {
                beamTransform.gameObject.SetActive(visible);
                beamTransform.localScale = visible ? Vector3.one : new Vector3(1f, 0f, 1f);
                if (beamCanvasGroup != null)
                {
                    beamCanvasGroup.alpha = visible ? 1f : 0f;
                }

                if (!visible)
                {
                    SetBeamParticlesVisible(false);
                }
                return;
            }

            if (visible)
            {
                beamTransform.gameObject.SetActive(true);
                beamTransform.localScale = new Vector3(1f, 0f, 1f);
                if (beamCanvasGroup != null)
                {
                    beamCanvasGroup.alpha = 0f;
                }

                Sequence sequence = DOTween.Sequence();
                sequence.Join(beamTransform.DOScaleY(1f, beamGrowDuration).SetEase(Ease.OutBack));
                if (beamCanvasGroup != null)
                {
                    sequence.Join(DOTween.To(() => beamCanvasGroup.alpha, value => beamCanvasGroup.alpha = value, 1f, beamGrowDuration));
                }
                beamTween = sequence.SetLink(gameObject);
                return;
            }

            SetBeamParticlesVisible(false);

            Sequence hideSequence = DOTween.Sequence();
            hideSequence.Join(beamTransform.DOScaleY(0f, beamGrowDuration).SetEase(Ease.InBack));
            if (beamCanvasGroup != null)
            {
                hideSequence.Join(DOTween.To(() => beamCanvasGroup.alpha, value => beamCanvasGroup.alpha = value, 0f, beamGrowDuration));
            }
            hideSequence.OnComplete(() => beamTransform.gameObject.SetActive(false));
            beamTween = hideSequence.SetLink(gameObject);
        }

        private void SetBeamParticlesVisible(bool visible)
        {
            if (beamParticleEmitter == null)
            {
                return;
            }

            beamParticleEmitter.SetBeamTarget(beamTransform);
            beamParticleEmitter.SetEmitting(visible);
            if (visible)
            {
                beamParticleEmitter.Burst(6);
            }
            else
            {
                beamParticleEmitter.StopAllParticles();
            }
        }

        private void UpdateHud()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }

            if (healthText != null)
            {
                healthText.text = "Health";
            }

            if (waveText != null)
            {
                waveText.text = $"Wave {Mathf.Max(1, waveIndex)}";
            }

            if (speedText != null)
            {
                speedText.text = $"Speed x{currentSpeedMultiplier:0.0}";
            }

            UpdateTimerHud(false);
        }

        private void AnimateHealthLoss(int previousHealth, int newHealth)
        {
            float previousFill = startingHealth <= 0 ? 0f : Mathf.Clamp01(previousHealth / (float)startingHealth);
            float newFill = startingHealth <= 0 ? 0f : Mathf.Clamp01(newHealth / (float)startingHealth);

            if (healthSlider != null)
            {
                healthSlider.SetValueWithoutNotify(newFill);
            }

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = newFill;
            }

            healthTween?.Kill();
            bool hasDamageAnimation = false;
            Sequence healthSequence = DOTween.Sequence();

            if (healthDamageSlider != null)
            {
                healthDamageSlider.SetValueWithoutNotify(previousFill);
                healthSequence.Join(DOTween.To(() => healthDamageSlider.value, value => healthDamageSlider.SetValueWithoutNotify(value), newFill, healthDamageBarDuration).SetEase(Ease.OutQuad).SetDelay(healthDamageDelay));
                hasDamageAnimation = true;
            }

            if (healthDamageFillImage != null)
            {
                healthDamageFillImage.fillAmount = previousFill;
                healthSequence.Join(DOTween.To(() => healthDamageFillImage.fillAmount, value => healthDamageFillImage.fillAmount = value, newFill, healthDamageBarDuration).SetEase(Ease.OutQuad).SetDelay(healthDamageDelay));
                hasDamageAnimation = true;
            }

            if (hasDamageAnimation)
            {
                healthTween = healthSequence.SetLink(gameObject);
            }
            else
            {
                healthSequence.Kill();
            }

            if (healthBarRoot != null)
            {
                healthBarRoot.DOKill();
                healthBarRoot.localScale = Vector3.one;
                healthBarRoot.DOPunchScale(Vector3.one * 0.13f, 0.28f, 8, 0.8f).SetLink(healthBarRoot.gameObject);
            }
        }

        private void SetHealthBarInstant(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            healthTween?.Kill();
            if (healthSlider != null)
            {
                healthSlider.minValue = 0f;
                healthSlider.maxValue = 1f;
                healthSlider.SetValueWithoutNotify(normalized);
            }

            if (healthDamageSlider != null)
            {
                healthDamageSlider.minValue = 0f;
                healthDamageSlider.maxValue = 1f;
                healthDamageSlider.SetValueWithoutNotify(normalized);
            }

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = normalized;
            }

            if (healthDamageFillImage != null)
            {
                healthDamageFillImage.fillAmount = normalized;
            }

            if (healthBarRoot != null)
            {
                healthBarRoot.localScale = Vector3.one;
            }
        }

        private void UpdateTimerHud(bool instant)
        {
            float normalized = currentWaveTime <= 0f ? 0f : Mathf.Clamp01(waveTimeRemaining / currentWaveTime);

            if (timerSlider != null)
            {
                timerSlider.minValue = 0f;
                timerSlider.maxValue = 1f;
                timerSlider.SetValueWithoutNotify(normalized);
            }

            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = normalized;
            }

            if (timerText != null)
            {
                timerText.text = string.Empty;
                timerText.gameObject.SetActive(false);
            }
        }

        private void SetTimerBarInstant(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (timerSlider != null)
            {
                timerSlider.minValue = 0f;
                timerSlider.maxValue = 1f;
                timerSlider.SetValueWithoutNotify(normalized);
            }

            if (timerFillImage != null)
            {
                timerFillImage.fillAmount = normalized;
            }
        }

        private void PlayTimerWarning()
        {
            if (timerBarRoot == null)
            {
                return;
            }

            lowTimerWarningPlaying = true;
            timerWarnTween?.Kill();
            timerBarRoot.localScale = Vector3.one;
            timerWarnTween = timerBarRoot.DOScale(Vector3.one * 1.06f, 0.22f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(timerBarRoot.gameObject);
        }

        private void StopTimerWarning()
        {
            timerWarnTween?.Kill();
            if (timerBarRoot != null)
            {
                timerBarRoot.localScale = Vector3.one;
            }
        }

        private void ApplyFonts()
        {
            if (applyPrimaryFontToAllTexts && primaryFont != null)
            {
                Transform textSearchRoot = transform;
                Canvas parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    textSearchRoot = parentCanvas.transform;
                }

                TMP_Text[] allTexts = textSearchRoot.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < allTexts.Length; i++)
                {
                    allTexts[i].font = primaryFont;
                }
            }

            if (secondaryFont == null || secondaryFontTargets == null)
            {
                return;
            }

            for (int i = 0; i < secondaryFontTargets.Count; i++)
            {
                if (secondaryFontTargets[i] != null)
                {
                    secondaryFontTargets[i].font = secondaryFont;
                }
            }
        }

        private void SetPanel(GameObject panel, bool visible)
        {
            if (panel != null)
            {
                panel.SetActive(visible);
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            if (list == null)
            {
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
    [Serializable]
    public class OddSuckBloomSkillConfig
    {
        public BloomSkillType skillType = BloomSkillType.Understand;
        [Min(1f)] public float maxScore = 100f;
        public float timeWeight = -1f;
        public float accuracyWeight = -1f;

        public OddSuckBloomSkillConfig()
        {
        }

        public OddSuckBloomSkillConfig(BloomSkillType skillType, float maxScore, float timeWeight = -1f, float accuracyWeight = -1f)
        {
            this.skillType = skillType;
            this.maxScore = maxScore;
            this.timeWeight = timeWeight;
            this.accuracyWeight = accuracyWeight;
        }
    }

}
