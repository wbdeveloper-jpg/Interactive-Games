using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine.SceneManagement;
using RewardSystem;
using UnityEngine;
using UnityEngine.UI;

namespace ImageChoiceRevealGame
{
    public class ImageChoiceRevealGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("Game Text")]
        [SerializeField] private string gameHeading = "Guess The Object";
        [TextArea(1, 3)] [SerializeField] private string gameInstruction = "Look carefully and choose the correct object.";
        [SerializeField] private TMP_Text loadingHeadingText;
        [SerializeField] private TMP_Text gameInstructionText;

        [Header("Project Fonts")]
        [Tooltip("Assign once. Used for main title / important headings.")]
        [SerializeField] private TMP_FontAsset primaryFont;

        [Tooltip("Assign once. Used for all normal UI text under Font Apply Root.")]
        [SerializeField] private TMP_FontAsset secondaryFont;

        [SerializeField] private Transform fontApplyRoot;
        [SerializeField] private TMP_Text[] primaryFontTexts;
        [SerializeField] private bool applyFontsOnAwake = true;

        [Header("Question Data")]
        [SerializeField] private List<ImageChoiceRevealQuestionData> questions = new List<ImageChoiceRevealQuestionData>();

        [Header("Game Settings")]
        [SerializeField] private ImageChoiceRevealMode revealMode = ImageChoiceRevealMode.Shadow;
        [SerializeField] private ImageChoiceHintMode hintMode = ImageChoiceHintMode.AutoByRevealMode;
        [SerializeField, Min(2)] private int optionsPerQuestion = 4;
        [SerializeField] private ImageChoiceOptionDisplayMode optionDisplayMode = ImageChoiceOptionDisplayMode.UsePerOptionSetting;
        [SerializeField, Min(0)] private int totalQuestions = 0;
        [SerializeField] private bool randomizeQuestions = true;
        [SerializeField] private bool randomizeOptions = true;
        [SerializeField] private bool allowQuestionRepeats = false;
        [SerializeField] private bool playOnStart = true;

        [Header("Loading")]
        [SerializeField] private ImageChoiceLoadingSettings loadingSettings = new ImageChoiceLoadingSettings();
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private CanvasGroup loadingCanvasGroup;
        [SerializeField] private Slider loadingSlider;
        [SerializeField] private TMP_Text loadingText;

        [Header("How To Play Flow")]
        [Tooltip("If true, How To Play opens after loading and before the first question timer starts.")]
        [SerializeField] private bool showHowToPlayBeforeGameplay = true;

        [Header("Bloom Reward System")]
        [Tooltip("Uses RewardManager.Instance from the external Bloom Reward System.")]
        [SerializeField] private bool useBloomRewardSystem = true;

        [Tooltip("Scene loaded by the Bloom reward system Home callback.")]
        [SerializeField] private string homeSceneName = "Loader Scene";

        [Tooltip("Used for timeScore when timer is disabled. If timer is enabled, Game Duration Seconds is used.")]
        [SerializeField, Min(1f)] private float expectedMaxTimeForReward = 120f;

        [Tooltip("Default: first two Bloom skills with equal value.")]
        [SerializeField] private List<SkillEntry> bloomSkills = new List<SkillEntry>
        {
            new SkillEntry(BloomSkillType.Remember, 100f),
            new SkillEntry(BloomSkillType.Understand, 100f)
        };

        [Header("Gameplay Root")]
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private CanvasGroup gameplayCanvasGroup;

        [Header("Score / Timer")]
        [SerializeField] private ImageChoiceScoreSettings scoreSettings = new ImageChoiceScoreSettings();
        [SerializeField] private ImageChoiceTimerSettings timerSettings = new ImageChoiceTimerSettings();

        [Header("Hint Score Cost")]
        [Tooltip("If ON, every hint subtracts Hint Cost Points from score.")]
        [SerializeField] private bool hintCostsScore = false;

        [SerializeField, Min(0)] private int hintCostPoints = 5;

        [Header("Reveal / Hint")]
        [SerializeField] private ImageChoiceRevealSettings revealSettings = new ImageChoiceRevealSettings();
        [SerializeField, Min(0)] private int maxHintsPerQuestion = 2;

        [Header("Flow")]
        [SerializeField, Range(0.2f, 3f)] private float autoNextDelay = 1.05f;

        [Header("Animation")]
        [SerializeField] private ImageChoiceAnimationSettings animationSettings = new ImageChoiceAnimationSettings();

        [Header("Main UI")]
        [SerializeField] private Image questionImage;
        [SerializeField] private RectTransform optionsParent;
        [Tooltip("Inactive scene object used as runtime option template. Customize this in scene, not as prefab.")]
        [SerializeField] private ImageChoiceRevealOptionButton optionButtonTemplate;

        [Header("Texts")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text questionCounterText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private TMP_Text scorePopupText;

        [Header("Score Popup Colors")]
        [SerializeField] private Color correctScorePopupColor = new Color(0.1f, 0.55f, 0.18f, 1f);
        [SerializeField] private Color wrongScorePopupColor = new Color(0.85f, 0.12f, 0.12f, 1f);
        [SerializeField] private Color hintCostPopupColor = new Color(0.95f, 0.55f, 0.05f, 1f);

        [Header("Panels")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultScoreText;
        [SerializeField] private TMP_Text resultCorrectText;
        [SerializeField] private TMP_Text resultWrongText;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private TMP_Text howToPlayText;

        [Header("Buttons")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button closeHowToPlayButton;

        [Header("Audio")]
        [SerializeField] private ImageChoiceAudioSettings audioSettings = new ImageChoiceAudioSettings();
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource questionAudioSource;
        [SerializeField] private AudioSource musicSource;

        private readonly List<ImageChoiceRevealOptionButton> optionPool = new List<ImageChoiceRevealOptionButton>();
        private readonly List<int> questionOrder = new List<int>();
        private int currentTurnIndex, plannedQuestionCount, score, hintsUsedThisQuestion;
        private int totalHintCount, correctAnswerCount, wrongAnswerCount;
        private bool gameRunning, isPaused, hasAnsweredCurrentQuestion, waitingForPreGameHowToPlay, howToPlayPausedGameplay;
        private float remainingTime, currentShadowRevealAmount, currentZoomScale;
        private float gameplayStartTime;
        private Coroutine autoNextRoutine, loadingDotsRoutine;
        private CanvasGroup questionCanvasGroup;
        private RectTransform questionRectTransform;
        private Sequence questionSequence, scorePopupSequence, panelSequence, loadingSequence, gameplaySequence;

        private bool UseAnimations => animationSettings != null && animationSettings.useAnimations;

        private ImageChoiceRevealQuestionData CurrentQuestion
        {
            get
            {
                if (currentTurnIndex < 0 || currentTurnIndex >= questionOrder.Count) return null;
                int questionIndex = questionOrder[currentTurnIndex];
                return questionIndex >= 0 && questionIndex < questions.Count ? questions[questionIndex] : null;
            }
        }

        private void Awake()
        {
            CacheQuestionComponents();
            RegisterButtonEvents();
            HideAllPanelsInstant();
            HideScorePopupInstant();
            ApplyProjectFonts();
            ApplyGameText();
            if (optionButtonTemplate != null) optionButtonTemplate.gameObject.SetActive(false);
        }

        private void Start()
        {
            SetupMusic();
            if (playOnStart) StartGame();
        }

        private void Update()
        {
            if (!gameRunning || isPaused || howToPlayPausedGameplay || waitingForPreGameHowToPlay || !timerSettings.useTimer) return;
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                UpdateTimerUI();
                EndGame();
                return;
            }
            UpdateTimerUI();
        }

        private void OnDestroy()
        {
            UnregisterButtonEvents();
            KillTweens();
        }

        public void StartGame()
        {
            if (!ValidateRequiredReferences()) return;

            StopRunningCoroutines();
            KillTweens();
            BuildQuestionOrder();

            if (questionOrder.Count == 0)
            {
                Debug.LogWarning("[ImageChoiceReveal] No valid questions found. Add sprites to the Questions list.");
                return;
            }

            ApplyGameText();
            plannedQuestionCount = questionOrder.Count;
            currentTurnIndex = 0;
            score = 0;
            totalHintCount = 0;
            correctAnswerCount = 0;
            wrongAnswerCount = 0;
            gameplayStartTime = 0f;
            remainingTime = timerSettings.gameDurationSeconds;
            gameRunning = false;
            isPaused = false;
            hasAnsweredCurrentQuestion = false;
            waitingForPreGameHowToPlay = false;
            howToPlayPausedGameplay = false;

            HideAllPanelsInstant();
            HideScorePopupInstant();
            ClearRuntimeOptions();
            UpdateScoreUI();
            UpdateTimerUI();

            StartCoroutine(PreGameRewardThenContinueRoutine());
        }

        private IEnumerator PreGameRewardThenContinueRoutine()
        {
            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPreGame(bloomSkills);
                yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
            }

            ContinueAfterBloomPreGame();
        }

        private void ContinueAfterBloomPreGame()
        {
            if (loadingSettings.showLoadingPanel && loadingPanel != null) StartCoroutine(LoadingThenBeginRoutine());
            else BeginGameplayNow();
        }

        public void RestartGame() { PlaySfx(audioSettings.clickSfx); StartGame(); }

        public void ContinueToBloomReward()
        {
            PlaySfx(audioSettings.clickSfx);

            if (!useBloomRewardSystem)
            {
                Debug.LogWarning("[ImageChoiceReveal] Bloom Reward System is disabled on the manager.");
                return;
            }

            if (RewardManager.Instance == null)
            {
                Debug.LogWarning("[ImageChoiceReveal] RewardManager.Instance is missing. Make sure RewardManager prefab exists once in LoadingScene.");
                return;
            }

            RewardManager.Instance.ShowPostGame(bloomSkills, BuildGameEvaluationData());
        }

        public void OnRewardScreenOpen()
        {
            if (musicSource != null)
                musicSource.Stop();

            if (questionAudioSource != null)
                questionAudioSource.Stop();
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

            if (!string.IsNullOrWhiteSpace(homeSceneName))
                SceneManager.LoadScene(homeSceneName);
        }

        public void PauseGame()
        {
            if (!gameRunning || waitingForPreGameHowToPlay || howToPlayPausedGameplay) return;
            PlaySfx(audioSettings.clickSfx);
            isPaused = true;
            ShowPanel(pausePanel);
            UpdateHintButtonState();
        }

        public void ResumeGame()
        {
            PlaySfx(audioSettings.clickSfx);
            isPaused = false;
            HidePanel(pausePanel);
            UpdateHintButtonState();
        }

        public void OpenHowToPlay()
        {
            PlaySfx(audioSettings.clickSfx);

            if (gameRunning && !isPaused)
                howToPlayPausedGameplay = true;

            if (howToPlayText != null)
                howToPlayText.text = string.IsNullOrWhiteSpace(gameInstruction) ? "Look carefully and choose the correct option." : gameInstruction;

            ShowPanel(howToPlayPanel);
            UpdateHintButtonState();
        }

        public void CloseHowToPlay()
        {
            PlaySfx(audioSettings.clickSfx);
            HidePanel(howToPlayPanel);

            if (waitingForPreGameHowToPlay)
            {
                waitingForPreGameHowToPlay = false;
                BeginActualGameplayAfterIntro();
                return;
            }

            howToPlayPausedGameplay = false;
            UpdateHintButtonState();
        }

        public void UseHint()
        {
            if (!gameRunning || hasAnsweredCurrentQuestion || isPaused || howToPlayPausedGameplay || waitingForPreGameHowToPlay || hintsUsedThisQuestion >= maxHintsPerQuestion) return;
            hintsUsedThisQuestion++;
            totalHintCount++;
            ApplyHintPenalty();
            PlaySfx(audioSettings.hintSfx);

            if (hintMode == ImageChoiceHintMode.AutoByRevealMode && GetActiveRevealMode() == ImageChoiceRevealMode.ZoomedShadow) ApplyZoomedShadowHint();
            else
            {
                switch (ResolveHintMode())
                {
                    case ImageChoiceHintMode.ShadowReveal: ApplyShadowHint(); break;
                    case ImageChoiceHintMode.ZoomOut: ApplyZoomHint(); break;
                    case ImageChoiceHintMode.ReduceOptionsToTwo: ReduceOptionsToTwo(); break;
                }
            }

            UpdateHintButtonState();
        }

        private IEnumerator LoadingThenBeginRoutine()
        {
            if (gameplayPanel != null) gameplayPanel.SetActive(false);
            if (gameplayCanvasGroup != null) gameplayCanvasGroup.alpha = 0f;
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (loadingCanvasGroup != null) loadingCanvasGroup.alpha = 1f;

            bool useSlider = loadingSettings.loadingStyle == ImageChoiceLoadingStyle.Slider || loadingSettings.loadingStyle == ImageChoiceLoadingStyle.SliderAndDots;
            bool useDots = loadingSettings.loadingStyle == ImageChoiceLoadingStyle.BlinkingDots || loadingSettings.loadingStyle == ImageChoiceLoadingStyle.SliderAndDots;

            if (loadingSlider != null)
            {
                loadingSlider.gameObject.SetActive(useSlider);
                loadingSlider.value = 0f;
                if (useSlider) loadingSlider.DOValue(1f, loadingSettings.loadingDuration).SetEase(Ease.OutCubic).SetLink(loadingSlider.gameObject);
            }

            if (loadingText != null)
            {
                loadingText.gameObject.SetActive(true);
                loadingText.text = useDots ? "Loading" : "Loading...";
            }

            if (useDots && loadingText != null) loadingDotsRoutine = StartCoroutine(LoadingDotsRoutine());
            yield return new WaitForSeconds(loadingSettings.loadingDuration);

            if (loadingDotsRoutine != null) { StopCoroutine(loadingDotsRoutine); loadingDotsRoutine = null; }

            if (loadingCanvasGroup != null && UseAnimations)
            {
                loadingSequence = DOTween.Sequence().SetLink(loadingPanel);
                loadingSequence.Append(loadingCanvasGroup.DOFade(0f, animationSettings.panelFadeDuration));
                yield return loadingSequence.WaitForCompletion();
            }

            if (loadingPanel != null) loadingPanel.SetActive(false);
            BeginGameplayNow();
        }

        private IEnumerator LoadingDotsRoutine()
        {
            int dots = 0;
            while (true)
            {
                dots = (dots + 1) % 4;
                loadingText.text = "Loading" + new string('.', dots);
                yield return new WaitForSeconds(0.22f);
            }
        }

        private void BeginGameplayNow()
        {
            if (gameplayPanel != null) gameplayPanel.SetActive(true);
            if (gameplayCanvasGroup != null)
            {
                gameplayCanvasGroup.alpha = 0f;
                if (UseAnimations) gameplaySequence = DOTween.Sequence().SetLink(gameplayPanel).Append(gameplayCanvasGroup.DOFade(1f, animationSettings.panelFadeDuration));
                else gameplayCanvasGroup.alpha = 1f;
            }

            if (showHowToPlayBeforeGameplay && howToPlayPanel != null)
            {
                gameRunning = false;
                isPaused = false;
                waitingForPreGameHowToPlay = true;
                howToPlayPausedGameplay = false;
                OpenHowToPlay();
                return;
            }

            BeginActualGameplayAfterIntro();
        }

        private void BeginActualGameplayAfterIntro()
        {
            gameRunning = true;
            isPaused = false;
            waitingForPreGameHowToPlay = false;
            howToPlayPausedGameplay = false;

            remainingTime = timerSettings.gameDurationSeconds;
            gameplayStartTime = Time.time;
            UpdateTimerUI();
            LoadCurrentQuestion();
        }

        private void CacheQuestionComponents()
        {
            if (questionImage == null) return;
            questionRectTransform = questionImage.rectTransform;
            questionCanvasGroup = questionImage.GetComponent<CanvasGroup>();
            if (questionCanvasGroup == null) questionCanvasGroup = questionImage.gameObject.AddComponent<CanvasGroup>();
        }

        private void RegisterButtonEvents()
        {
            if (hintButton != null) hintButton.onClick.AddListener(UseHint);
            if (pauseButton != null) pauseButton.onClick.AddListener(PauseGame);
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueToBloomReward);
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(OpenHowToPlay);
            if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);
        }

        private void UnregisterButtonEvents()
        {
            if (hintButton != null) hintButton.onClick.RemoveListener(UseHint);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(PauseGame);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(ResumeGame);
            if (restartButton != null) restartButton.onClick.RemoveListener(RestartGame);
            if (continueButton != null) continueButton.onClick.RemoveListener(ContinueToBloomReward);
            if (howToPlayButton != null) howToPlayButton.onClick.RemoveListener(OpenHowToPlay);
            if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.RemoveListener(CloseHowToPlay);
        }

        private bool ValidateRequiredReferences()
        {
            bool valid = true;
            if (questionImage == null) { Debug.LogError("[ImageChoiceReveal] Question Image reference is missing."); valid = false; }
            if (optionsParent == null) { Debug.LogError("[ImageChoiceReveal] Options Parent reference is missing."); valid = false; }
            if (optionButtonTemplate == null) { Debug.LogError("[ImageChoiceReveal] Option Button Template missing. Use inactive scene object OptionButtonTemplate."); valid = false; }
            return valid;
        }

        private void ApplyProjectFonts()
        {
            if (!applyFontsOnAwake) return;

            if (secondaryFont != null && fontApplyRoot != null)
            {
                TMP_Text[] allTexts = fontApplyRoot.GetComponentsInChildren<TMP_Text>(true);
                for (int i = 0; i < allTexts.Length; i++)
                {
                    if (allTexts[i] != null)
                        allTexts[i].font = secondaryFont;
                }
            }

            if (primaryFont != null && primaryFontTexts != null)
            {
                for (int i = 0; i < primaryFontTexts.Length; i++)
                {
                    if (primaryFontTexts[i] != null)
                        primaryFontTexts[i].font = primaryFont;
                }
            }
        }

        private void ApplyGameText()
        {
            if (loadingHeadingText != null) loadingHeadingText.text = gameHeading;
            if (gameInstructionText != null) gameInstructionText.text = gameInstruction;
        }

        private void SetupMusic()
        {
            if (musicSource == null || audioSettings == null) return;
            musicSource.loop = audioSettings.loopBackgroundMusic;
            musicSource.volume = audioSettings.backgroundVolume;
            musicSource.clip = audioSettings.backgroundMusic;
            if (audioSettings.playBackgroundMusic && audioSettings.backgroundMusic != null && !musicSource.isPlaying) musicSource.Play();
        }

        private void BuildQuestionOrder()
        {
            questionOrder.Clear();
            List<int> validIndices = new List<int>();
            for (int i = 0; i < questions.Count; i++) if (questions[i] != null && questions[i].IsValid()) validIndices.Add(i);
            if (validIndices.Count == 0) return;
            if (randomizeQuestions) Shuffle(validIndices);

            int requestedCount = totalQuestions <= 0 ? validIndices.Count : totalQuestions;
            int finalCount = allowQuestionRepeats ? requestedCount : Mathf.Min(requestedCount, validIndices.Count);
            for (int i = 0; i < finalCount; i++) questionOrder.Add(i < validIndices.Count ? validIndices[i] : validIndices[UnityEngine.Random.Range(0, validIndices.Count)]);
        }

        private void LoadCurrentQuestion()
        {
            ImageChoiceRevealQuestionData question = CurrentQuestion;
            if (question == null) { EndGame(); return; }

            hasAnsweredCurrentQuestion = false;
            hintsUsedThisQuestion = 0;
            HideScorePopupInstant();
            SetQuestionImage(question);
            BuildOptions(question);
            ApplyInitialRevealState();
            UpdateQuestionCounterUI();
            UpdateHintButtonState();
            if (feedbackText != null) feedbackText.text = "";
            PlayQuestionAudio(question);
            PlayQuestionEnterAnimation();
        }

        private void SetQuestionImage(ImageChoiceRevealQuestionData question)
        {
            CacheQuestionComponents();
            Sprite sprite = question.GetQuestionSprite();
            if (questionImage == null) return;
            questionImage.sprite = sprite;
            questionImage.enabled = sprite != null;
            questionImage.preserveAspect = true;
            questionImage.color = Color.white;
            ApplyQuestionInstruction(question);
            if (questionRectTransform != null) questionRectTransform.localScale = Vector3.one;
            if (questionCanvasGroup != null) questionCanvasGroup.alpha = 1f;
        }

        private void ApplyQuestionInstruction(ImageChoiceRevealQuestionData question)
        {
            if (gameInstructionText == null) return;

            if (question != null && !string.IsNullOrWhiteSpace(question.instructionOverride))
                gameInstructionText.text = question.instructionOverride;
            else
                gameInstructionText.text = gameInstruction;
        }

        private void BuildOptions(ImageChoiceRevealQuestionData question)
        {
            ImageChoiceRevealOptionData correctOption = question.GetCorrectOptionData();
            List<OptionPayload> payloads = new List<OptionPayload> { new OptionPayload(correctOption, true) };

            List<ImageChoiceRevealOptionData> distractors = BuildDistractorPool(question, correctOption);
            int usableDistractorCount = Mathf.Min(Mathf.Max(1, optionsPerQuestion - 1), distractors.Count);

            for (int i = 0; i < usableDistractorCount; i++)
                payloads.Add(new OptionPayload(distractors[i], false));

            if (randomizeOptions)
                Shuffle(payloads);

            EnsureOptionPool(payloads.Count);

            for (int i = 0; i < optionPool.Count; i++)
            {
                if (i < payloads.Count)
                {
                    optionPool[i].Configure(payloads[i].optionData, payloads[i].isCorrect, optionDisplayMode, OnOptionClicked);
                    optionPool[i].PlayAppear(i * animationSettings.optionStaggerDelay, animationSettings.optionEnterDuration, UseAnimations);
                }
                else
                {
                    optionPool[i].gameObject.SetActive(false);
                }
            }
        }

        private List<ImageChoiceRevealOptionData> BuildDistractorPool(
    ImageChoiceRevealQuestionData question,
    ImageChoiceRevealOptionData correctOption)
        {
            List<ImageChoiceRevealOptionData> distractors =
                new List<ImageChoiceRevealOptionData>();

            // Number of wrong options required for this question.
            int requiredDistractorCount = Mathf.Max(0, optionsPerQuestion - 1);

            // First, add the distractors manually assigned to this question.
            List<ImageChoiceRevealOptionData> questionDistractors =
                question.GetDistractorOptionData();

            for (int i = 0; i < questionDistractors.Count; i++)
            {
                AddUniqueOption(
                    distractors,
                    questionDistractors[i],
                    correctOption
                );
            }

            // Fallback only when this question does not have enough distractors.
            if (distractors.Count < requiredDistractorCount)
            {
                for (int i = 0; i < questions.Count; i++)
                {
                    ImageChoiceRevealQuestionData otherQuestion = questions[i];

                    if (otherQuestion == null ||
                        otherQuestion == question ||
                        !otherQuestion.IsValid())
                    {
                        continue;
                    }

                    AddUniqueOption(
                        distractors,
                        otherQuestion.GetCorrectOptionData(),
                        correctOption
                    );

                    // Stop immediately when the missing slots are filled.
                    if (distractors.Count >= requiredDistractorCount)
                        break;
                }
            }

            Shuffle(distractors);
            return distractors;
        }

        private static void AddUniqueOption(List<ImageChoiceRevealOptionData> list, ImageChoiceRevealOptionData candidate, ImageChoiceRevealOptionData exclude)
        {
            if (candidate == null || !candidate.IsValid())
                return;

            if (AreSameOption(candidate, exclude))
                return;

            for (int i = 0; i < list.Count; i++)
                if (AreSameOption(list[i], candidate)) return;

            list.Add(candidate);
        }

        private static bool AreSameOption(ImageChoiceRevealOptionData a, ImageChoiceRevealOptionData b)
        {
            if (a == null || b == null)
                return false;

            if (a.optionSprite != null && b.optionSprite != null && a.optionSprite == b.optionSprite)
                return true;

            if (!string.IsNullOrWhiteSpace(a.optionText) &&
                !string.IsNullOrWhiteSpace(b.optionText) &&
                string.Equals(a.optionText.Trim(), b.optionText.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private void EnsureOptionPool(int requiredCount)
        {
            if (optionButtonTemplate != null) optionButtonTemplate.gameObject.SetActive(false);
            while (optionPool.Count < requiredCount)
            {
                ImageChoiceRevealOptionButton created = Instantiate(optionButtonTemplate, optionsParent);
                created.name = "OptionButton_Runtime_" + optionPool.Count;
                created.gameObject.SetActive(false);
                optionPool.Add(created);
            }
        }

        private void ClearRuntimeOptions()
        {
            for (int i = 0; i < optionPool.Count; i++) if (optionPool[i] != null) Destroy(optionPool[i].gameObject);
            optionPool.Clear();
            if (optionButtonTemplate != null) optionButtonTemplate.gameObject.SetActive(false);
        }

        private ImageChoiceRevealMode GetActiveRevealMode()
        {
            ImageChoiceRevealQuestionData question = CurrentQuestion;
            if (question == null) return revealMode;

            switch (question.revealOverride)
            {
                case ImageChoiceQuestionRevealOverride.Normal: return ImageChoiceRevealMode.Normal;
                case ImageChoiceQuestionRevealOverride.Shadow: return ImageChoiceRevealMode.Shadow;
                case ImageChoiceQuestionRevealOverride.Zoomed: return ImageChoiceRevealMode.Zoomed;
                case ImageChoiceQuestionRevealOverride.ZoomedShadow: return ImageChoiceRevealMode.ZoomedShadow;
                default: return revealMode;
            }
        }

        private void ApplyInitialRevealState()
        {
            if (questionImage == null) return;
            currentShadowRevealAmount = Mathf.Clamp01(revealSettings.shadowStartRevealAmount);
            currentZoomScale = Mathf.Max(1f, revealSettings.zoomStartScale);

            ImageChoiceRevealMode activeRevealMode = GetActiveRevealMode();

            switch (activeRevealMode)
            {
                case ImageChoiceRevealMode.Normal:
                    questionImage.color = Color.white;
                    questionImage.rectTransform.localScale = Vector3.one;
                    break;
                case ImageChoiceRevealMode.Shadow:
                    ApplyShadowRevealAmount(currentShadowRevealAmount);
                    questionImage.rectTransform.localScale = Vector3.one;
                    break;
                case ImageChoiceRevealMode.Zoomed:
                    questionImage.color = Color.white;
                    questionImage.rectTransform.localScale = Vector3.one * currentZoomScale;
                    break;
                case ImageChoiceRevealMode.ZoomedShadow:
                    ApplyShadowRevealAmount(currentShadowRevealAmount);
                    questionImage.rectTransform.localScale = Vector3.one * currentZoomScale;
                    break;
            }
        }

        private void PlayQuestionEnterAnimation()
        {
            if (!UseAnimations || questionCanvasGroup == null || questionRectTransform == null) return;
            KillQuestionTween();
            Vector3 targetScale = questionRectTransform.localScale;
            questionCanvasGroup.alpha = 0f;
            questionRectTransform.localScale = targetScale * 0.9f;
            questionSequence = DOTween.Sequence().SetLink(questionImage.gameObject);
            questionSequence.Join(questionCanvasGroup.DOFade(1f, animationSettings.questionEnterDuration));
            questionSequence.Join(questionRectTransform.DOScale(targetScale, animationSettings.questionEnterDuration).SetEase(Ease.OutBack));
        }

        private void OnOptionClicked(ImageChoiceRevealOptionButton selectedOption)
        {
            if (!gameRunning || hasAnsweredCurrentQuestion || selectedOption == null || isPaused) return;
            hasAnsweredCurrentQuestion = true;
            PlaySfx(audioSettings.clickSfx);
            SetAllOptionsInteractable(false);

            if (selectedOption.IsCorrect)
            {
                correctAnswerCount++;
                selectedOption.PlayCorrectFeedback(animationSettings.feedbackDuration, UseAnimations);
                AddScore(scoreSettings.correctScore);
                ShowScorePopup("+" + scoreSettings.correctScore, correctScorePopupColor);
                SetFeedback("Correct!");
                PlaySfx(audioSettings.correctSfx);
            }
            else
            {
                wrongAnswerCount++;
                selectedOption.PlayWrongFeedback(animationSettings.feedbackDuration, UseAnimations);
                ShowCorrectOption();
                ApplyWrongPenalty();
                ShowScorePopup("-" + scoreSettings.wrongPenalty, wrongScorePopupColor);
                SetFeedback("Wrong!");
                PlaySfx(audioSettings.wrongSfx);
            }

            UpdateHintButtonState();
            StartAutoNextAfterDelay();
        }

        private void StartAutoNextAfterDelay()
        {
            if (autoNextRoutine != null) StopCoroutine(autoNextRoutine);
            autoNextRoutine = StartCoroutine(AutoNextRoutine());
        }

        private IEnumerator AutoNextRoutine()
        {
            yield return new WaitForSeconds(Mathf.Max(0.2f, autoNextDelay));
            currentTurnIndex++;
            if (currentTurnIndex >= plannedQuestionCount) EndGame(); else LoadCurrentQuestion();
            autoNextRoutine = null;
        }

        private void ShowCorrectOption()
        {
            for (int i = 0; i < optionPool.Count; i++)
            {
                if (optionPool[i].gameObject.activeSelf && optionPool[i].IsCorrect)
                {
                    optionPool[i].PlayCorrectFeedback(animationSettings.feedbackDuration, UseAnimations);
                    return;
                }
            }
        }

        private void SetAllOptionsInteractable(bool interactable)
        {
            for (int i = 0; i < optionPool.Count; i++) if (optionPool[i].gameObject.activeSelf) optionPool[i].SetInteractable(interactable);
        }

        private void AddScore(int amount) { score += Mathf.Max(0, amount); UpdateScoreUI(); }
        private void ApplyWrongPenalty() { score = Mathf.Max(0, score - Mathf.Max(0, scoreSettings.wrongPenalty)); UpdateScoreUI(); }

        private void ApplyHintPenalty()
        {
            if (!hintCostsScore) return;

            int penalty = Mathf.Max(0, hintCostPoints);
            if (penalty <= 0) return;

            score = Mathf.Max(0, score - penalty);
            ShowScorePopup("-" + penalty, hintCostPopupColor);
            UpdateScoreUI();
        }

        private ImageChoiceHintMode ResolveHintMode()
        {
            if (hintMode != ImageChoiceHintMode.AutoByRevealMode) return hintMode;

            ImageChoiceRevealMode activeRevealMode = GetActiveRevealMode();
            if (activeRevealMode == ImageChoiceRevealMode.Shadow) return ImageChoiceHintMode.ShadowReveal;
            if (activeRevealMode == ImageChoiceRevealMode.Zoomed) return ImageChoiceHintMode.ZoomOut;
            return ImageChoiceHintMode.ReduceOptionsToTwo;
        }

        private void ApplyShadowHint()
        {
            currentShadowRevealAmount = Mathf.Clamp01(currentShadowRevealAmount + revealSettings.shadowHintRevealStep);
            Color targetColor = Color.Lerp(Color.black, Color.white, currentShadowRevealAmount);
            if (UseAnimations) questionImage.DOColor(targetColor, animationSettings.questionHintDuration).SetEase(Ease.OutCubic).SetLink(questionImage.gameObject);
            else questionImage.color = targetColor;
        }

        private void ApplyZoomHint()
        {
            if (questionImage == null) return;
            RectTransform rect = questionImage.rectTransform;
            currentZoomScale = Mathf.Max(1f, currentZoomScale - revealSettings.zoomHintStep);
            if (UseAnimations) rect.DOScale(Vector3.one * currentZoomScale, animationSettings.questionHintDuration).SetEase(Ease.OutCubic).SetLink(questionImage.gameObject);
            else rect.localScale = Vector3.one * currentZoomScale;
        }

        private void ApplyZoomedShadowHint()
        {
            if (questionImage == null) return;
            RectTransform rect = questionImage.rectTransform;
            currentShadowRevealAmount = Mathf.Clamp01(currentShadowRevealAmount + revealSettings.shadowHintRevealStep);
            currentZoomScale = Mathf.Max(1f, currentZoomScale - revealSettings.zoomHintStep);
            Color targetColor = Color.Lerp(Color.black, Color.white, currentShadowRevealAmount);

            if (UseAnimations)
            {
                Sequence sequence = DOTween.Sequence().SetLink(questionImage.gameObject);
                sequence.Join(questionImage.DOColor(targetColor, animationSettings.questionHintDuration).SetEase(Ease.OutCubic));
                sequence.Join(rect.DOScale(Vector3.one * currentZoomScale, animationSettings.questionHintDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                questionImage.color = targetColor;
                rect.localScale = Vector3.one * currentZoomScale;
            }
        }

        private void ApplyShadowRevealAmount(float revealAmount)
        {
            if (questionImage != null) questionImage.color = Color.Lerp(Color.black, Color.white, Mathf.Clamp01(revealAmount));
        }

        private void ReduceOptionsToTwo()
        {
            List<ImageChoiceRevealOptionButton> activeWrongOptions = new List<ImageChoiceRevealOptionButton>();
            bool correctStillActive = false;
            for (int i = 0; i < optionPool.Count; i++)
            {
                ImageChoiceRevealOptionButton option = optionPool[i];
                if (!option.gameObject.activeSelf) continue;
                if (option.IsCorrect) correctStillActive = true; else activeWrongOptions.Add(option);
            }

            if (!correctStillActive || activeWrongOptions.Count <= 1) return;
            Shuffle(activeWrongOptions);
            for (int i = 1; i < activeWrongOptions.Count; i++) activeWrongOptions[i].PlayHideByHint(animationSettings.optionRemoveDuration, UseAnimations);
        }

        private void UpdateHintButtonState()
        {
            if (hintButton != null) hintButton.interactable = gameRunning && !isPaused && !howToPlayPausedGameplay && !waitingForPreGameHowToPlay && !hasAnsweredCurrentQuestion && hintsUsedThisQuestion < maxHintsPerQuestion;
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null) feedbackText.text = message;
        }

        private void ShowScorePopup(string message)
        {
            ShowScorePopup(message, scorePopupText != null ? scorePopupText.color : Color.white);
        }

        private void ShowScorePopup(string message, Color popupColor)
        {
            if (scorePopupText == null) return;

            KillScorePopupTween();

            scorePopupText.gameObject.SetActive(true);
            scorePopupText.text = message;
            scorePopupText.color = popupColor;

            RectTransform rect = scorePopupText.rectTransform;
            CanvasGroup group = scorePopupText.GetComponent<CanvasGroup>();
            if (group == null) group = scorePopupText.gameObject.AddComponent<CanvasGroup>();

            Vector2 startPos = rect.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, 55f);

            if (!UseAnimations)
            {
                group.alpha = 1f;
                return;
            }

            group.alpha = 0f;
            rect.localScale = Vector3.one * 0.82f;

            scorePopupSequence = DOTween.Sequence().SetLink(scorePopupText.gameObject);
            scorePopupSequence.Join(group.DOFade(1f, animationSettings.scorePopupDuration * 0.2f));
            scorePopupSequence.Join(rect.DOScale(1f, animationSettings.scorePopupDuration * 0.32f).SetEase(Ease.OutBack));
            scorePopupSequence.Join(rect.DOAnchorPos(endPos, animationSettings.scorePopupDuration).SetEase(Ease.OutCubic));
            scorePopupSequence.Append(group.DOFade(0f, animationSettings.scorePopupDuration * 0.22f));
            scorePopupSequence.OnComplete(() =>
            {
                rect.anchoredPosition = startPos;
                rect.localScale = Vector3.one;
                HideScorePopupInstant();
            });
        }

        private void HideScorePopupInstant()
        {
            if (scorePopupText == null) return;
            scorePopupText.gameObject.SetActive(false);
            CanvasGroup group = scorePopupText.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 0f;
        }

        private void EndGame()
        {
            if (!gameRunning) return;
            gameRunning = false;
            isPaused = false;
            SetAllOptionsInteractable(false);
            HidePanel(pausePanel);
            if (resultTitleText != null) resultTitleText.text = "Game Complete!";
            if (resultScoreText != null) resultScoreText.text = "Score: " + score;
            if (resultCorrectText != null) resultCorrectText.text = "Correct: " + correctAnswerCount;
            if (resultWrongText != null) resultWrongText.text = "Wrong: " + wrongAnswerCount;
            ShowPanel(resultPanel);
            PlaySfx(audioSettings.gameCompleteSfx);
            UpdateHintButtonState();
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel == null) return;
            panel.SetActive(true);
            CanvasGroup group = GetOrAddCanvasGroup(panel);
            RectTransform rect = panel.transform as RectTransform;
            if (!UseAnimations) { group.alpha = 1f; return; }
            KillPanelTween();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;
            if (rect != null) rect.localScale = Vector3.one * 0.96f;
            panelSequence = DOTween.Sequence().SetLink(panel);
            panelSequence.Join(group.DOFade(1f, animationSettings.panelFadeDuration));
            if (rect != null) panelSequence.Join(rect.DOScale(Vector3.one, animationSettings.panelFadeDuration).SetEase(Ease.OutBack));
        }

        private void HidePanel(GameObject panel)
        {
            if (panel != null) panel.SetActive(false);
        }

        private void HideAllPanelsInstant()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        }

        private CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.AddComponent<CanvasGroup>();
        }

        private void UpdateScoreUI() { if (scoreText != null) scoreText.text = "Score: " + score; }

        private void UpdateTimerUI()
        {
            if (timerText == null) return;
            if (!timerSettings.useTimer) { timerText.text = ""; return; }
            int seconds = Mathf.CeilToInt(remainingTime);
            timerText.text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        }

        private void UpdateQuestionCounterUI()
        {
            if (questionCounterText != null) questionCounterText.text = (currentTurnIndex + 1) + " / " + plannedQuestionCount;
        }

        private void PlayQuestionAudio(ImageChoiceRevealQuestionData question)
        {
            if (questionAudioSource == null || question == null || question.questionAudio == null) return;
            questionAudioSource.Stop();
            questionAudioSource.clip = question.questionAudio;
            questionAudioSource.Play();
        }

        private void PlaySfx(AudioClip clip)
        {
            if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
        }

        private GameEvaluationData BuildGameEvaluationData()
        {
            float timeTaken = gameplayStartTime > 0f ? Time.time - gameplayStartTime : 0f;
            float expectedTime = timerSettings.useTimer ? timerSettings.gameDurationSeconds : expectedMaxTimeForReward;
            expectedTime = Mathf.Max(1f, expectedTime);

            float timeScore = Mathf.Clamp01(1f - (timeTaken / expectedTime));
            float accuracyScore = plannedQuestionCount > 0 ? Mathf.Clamp01((float)correctAnswerCount / plannedQuestionCount) : 0f;

            return new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = wrongAnswerCount,
                timeTaken = timeTaken
            };
        }

        private void StopRunningCoroutines()
        {
            if (autoNextRoutine != null) { StopCoroutine(autoNextRoutine); autoNextRoutine = null; }
            if (loadingDotsRoutine != null) { StopCoroutine(loadingDotsRoutine); loadingDotsRoutine = null; }
        }

        private void KillTweens()
        {
            KillQuestionTween();
            KillScorePopupTween();
            KillPanelTween();
            if (loadingSequence != null && loadingSequence.IsActive()) loadingSequence.Kill();
            if (gameplaySequence != null && gameplaySequence.IsActive()) gameplaySequence.Kill();
            loadingSequence = null;
            gameplaySequence = null;
        }

        private void KillQuestionTween()
        {
            if (questionSequence != null && questionSequence.IsActive()) questionSequence.Kill();
            questionSequence = null;
        }

        private void KillScorePopupTween()
        {
            if (scorePopupSequence != null && scorePopupSequence.IsActive()) scorePopupSequence.Kill();
            scorePopupSequence = null;
        }

        private void KillPanelTween()
        {
            if (panelSequence != null && panelSequence.IsActive()) panelSequence.Kill();
            panelSequence = null;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        private readonly struct OptionPayload
        {
            public readonly ImageChoiceRevealOptionData optionData;
            public readonly bool isCorrect;

            public OptionPayload(ImageChoiceRevealOptionData optionData, bool isCorrect)
            {
                this.optionData = optionData;
                this.isCorrect = isCorrect;
            }
        }

    }
}
