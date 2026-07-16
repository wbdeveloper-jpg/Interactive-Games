using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using RewardSystem;

namespace GuessWhoIAm
{
    public class GuessWhoIAmOptionGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Serializable]
        public class GuessWhoClueCardUI
        {
            public RectTransform root;
            public Button button;
            public Image background;
            public CanvasGroup canvasGroup;
            public LayoutElement layoutElement;
            public TMP_Text badgeText;
            public Image badgeBackground;
            public Outline outline;
            public TMP_Text titleText;
            public TMP_Text clueText;
            public TMP_Text valueChipText;
            public Image valueChipBackground;
            public Image selectedPointer;
            public Image lockIconBackground;
            public TMP_Text lockIconText;
        }

        [Serializable]
        public class GuessWhoOptionButtonUI
        {
            public RectTransform root;
            public Button button;
            public Image background;
            public Image letterBadgeBackground;
            public TMP_Text letterBadgeText;
            public TMP_Text labelText;
            [HideInInspector] public string runtimeAnswer;
        }

        [Header("Database")]
        [SerializeField] private GuessWhoQuestionDatabase questionDatabase;
        [SerializeField] [Min(1)] private int roundQuestionCount = 10;
        [SerializeField] [Range(3, 4)] private int optionCount = 4;
        [SerializeField] private bool startRoundOnPlay = true;

        [Header("Startup Flow")]
        [SerializeField] private bool useBloomRewardSystem = true;
        [SerializeField] private bool showLoadingPanelBeforeHowTo = true;
        [SerializeField] [Min(0f)] private float loadingPanelSeconds = 1.2f;
        [SerializeField] private bool showHowToBeforeFirstQuestion = true;
        [SerializeField] private string homeSceneName = "Loader Scene";

        [Header("Bloom Evaluation")]
        [SerializeField] private BloomSkillType primaryBloomSkill = BloomSkillType.Remember;
        [SerializeField] [Min(1f)] private float primaryBloomMaxScore = 100f;
        [SerializeField] private BloomSkillType secondaryBloomSkill = BloomSkillType.Understand;
        [SerializeField] [Min(0f)] private float secondaryBloomMaxScore = 50f;
        [SerializeField] private bool useCustomBloomWeights = false;
        [SerializeField] private float bloomTimeWeight = -1f;
        [SerializeField] private float bloomAccuracyWeight = -1f;
        [SerializeField] [Min(1f)] private float expectedMaxTime = 120f;

        [Header("Scoring")]
        [SerializeField] private int clue1AnswerPoints = 10;
        [SerializeField] private int clue2AnswerPoints = 7;
        [SerializeField] private int clue3AnswerPoints = 5;
        [SerializeField] private int startingCoins = 0;

        [Header("Timing")]
        [SerializeField] private float autoNextSeconds = 4f;

        [Header("HUD")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text questionProgressText;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private List<Image> progressStepMarkers = new List<Image>();

        [Header("Clues")]
        [SerializeField] private List<GuessWhoClueCardUI> clueCards = new List<GuessWhoClueCardUI>();
        [SerializeField] private string lockedClueText = "Reveal next clue to unlock";

        [Header("Options")]
        [SerializeField] private List<GuessWhoOptionButtonUI> optionButtons = new List<GuessWhoOptionButtonUI>();

        [Header("Mascot Guide")]
        [SerializeField] private CanvasGroup guideBubbleCanvasGroup;
        [SerializeField] private TMP_Text guideMessageText;
        [SerializeField] private Button revealButton;
        [SerializeField] private TMP_Text revealButtonMainText;
        [SerializeField] private TMP_Text revealButtonSubText;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonText;
        [SerializeField] private Slider nextButtonProgressSlider;

        [Header("Utility Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button closeHowToButton;
        [SerializeField] private Button resultRestartButton;
        [SerializeField] private Button resultContinueButton;

        [Header("Panels")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultScoreText;
        [SerializeField] private TMP_Text resultMessageText;

        [Header("Loading Panel UI")]
        [SerializeField] private TMP_Text loadingTitleText;
        [SerializeField] private Slider loadingProgressSlider;
        [SerializeField] private TMP_Text loadingStatusText;
        [SerializeField] private string loadingTitle = "Guess Who I Am";
        [SerializeField] private string loadingTextFormat = "Loading...";

        [Header("How To Play Image Guide")]
        [SerializeField] private Image howToGuideImage;
        [SerializeField] private TMP_Text howToBackupText;
        [SerializeField] private TMP_Text howToPageText;
        [SerializeField] private Button howToPreviousButton;
        [SerializeField] private Button howToNextButton;
        [SerializeField] private Button howToStartButton;
        [SerializeField] private List<Sprite> howToGuideSprites = new List<Sprite>();
        [SerializeField] [TextArea(2, 5)] private string howToBackupMessage = "Read clue 1 and choose the correct answer. Reveal more clues only when needed. Fewer clues means more points.";
        [SerializeField] private string howToStartButtonText = "Start";
        [SerializeField] private string howToContinueButtonText = "Continue";

        [Header("Audio")]
        [SerializeField] private GuessWhoIAmAudioManager audioManager;

        [Header("Guide Messages")]
        [SerializeField] private string startGuideMessage = "Pick one. Reveal only if needed.";
        [SerializeField] private string revealGuideMessage = "Reveal more clues if you're not sure.";
        [SerializeField] private string correctGuideMessage = "Correct! Tap Next or wait.";
        [SerializeField] private string wrongGuideMessage = "Answer revealed. Check the clues.";

        [Header("Colors")]
        [SerializeField] private Color clueActiveColor = new Color32(255, 252, 245, 255);
        [SerializeField] private Color clueUnlockedColor = new Color32(232, 224, 214, 205);
        [SerializeField] private Color clueLockedColor = new Color32(71, 55, 105, 170);
        [SerializeField] private Color clueActiveTextColor = new Color32(23, 18, 35, 255);
        [SerializeField] private Color clueLockedTextColor = new Color32(205, 195, 225, 255);
        [SerializeField] private Color clueChipActiveColor = new Color32(255, 188, 46, 255);
        [SerializeField] private Color clueChipLockedColor = new Color32(154, 137, 181, 220);
        [SerializeField] private Color clueUnlockedDimTextColor = new Color32(78, 66, 92, 225);
        [SerializeField] private Color clueChipUnlockedDimColor = new Color32(255, 203, 82, 185);
        [SerializeField] private Color optionNormalColor = new Color32(54, 41, 86, 225);
        [SerializeField] private Color optionCorrectColor = new Color32(55, 185, 112, 240);
        [SerializeField] private Color optionWrongColor = new Color32(230, 82, 98, 240);
        [SerializeField] private Color optionDisabledColor = new Color32(55, 43, 86, 135);
        [SerializeField] private Color progressActiveColor = new Color32(255, 188, 46, 255);
        [SerializeField] private Color progressInactiveColor = new Color32(113, 105, 143, 255);

        [Header("Animation")]
        [SerializeField] private float tweenDuration = 0.2f;
        [SerializeField] private float selectedClueScale = 1.04f;
        [SerializeField] private float lockedClueScale = 0.96f;
        [SerializeField] private float optionSpawnDelay = 0.04f;

        [Header("Points Popup Optional Prefab")]
        [Tooltip("Optional TMP_Text prefab. Assign your own +10/+7/+5 popup text prefab here. Leave empty to disable.")]
        [SerializeField] private TMP_Text pointsPopupTextPrefab;
        [Tooltip("Optional parent for popup instances. If empty, this manager transform is used.")]
        [SerializeField] private RectTransform pointsPopupParent;
        [SerializeField] private Vector2 pointsPopupOffset = new Vector2(0f, 70f);
        [SerializeField] [Min(0.1f)] private float pointsPopupDuration = 0.75f;
        [SerializeField] private float pointsPopupMoveY = 80f;
        [SerializeField] private float pointsPopupStartScale = 0.75f;
        [SerializeField] private float pointsPopupEndScale = 1.15f;

        [Header("Locked Clue Tap UX")]
        [SerializeField] private bool allowLockedClueTapReveal = true;
        [SerializeField] private bool showGuideMessageOnBlockedLockedClue = false;
        [SerializeField] private string blockedLockedClueGuideMessage = "Unlock the previous clue first.";
        [SerializeField] private float lockedClueHintPunchScale = 0.1f;
        [SerializeField] private float lockedClueHintShakeStrength = 14f;

        [Header("Clue Reveal Animation")]
        [SerializeField] private bool useClueRevealSpinAnimation = true;
        [SerializeField] [Min(0.1f)] private float clueRevealSpinDuration = 0.48f;
        [SerializeField] [Min(0f)] private float clueRevealPopScale = 0.1f;
        [SerializeField] [Min(1)] private int clueRevealSpinTurns = 1;

        private readonly List<GuessWhoQuestionData> roundQuestions = new List<GuessWhoQuestionData>();
        private readonly List<string> currentOptionValues = new List<string>();
        private readonly List<SkillEntry> bloomSkills = new List<SkillEntry>();
        private readonly List<Sprite> runtimeHowToSlides = new List<Sprite>();

        private GuessWhoQuestionData currentQuestion;
        private int currentQuestionIndex = -1;
        private int revealedClueCount = 1;
        private int selectedClueIndex = 0;
        private int score;
        private int coins;
        private int correctCount;
        private int mistakeCount;
        private int totalQuestionCount;
        private float gameStartTime;
        private bool isAnswered;
        private bool listenersRegistered;
        private bool startupFlowRunning;
        private bool hasStartedGameplay;
        private int howToPageIndex;
        private Coroutine autoNextCoroutine;
        private Tween nextButtonSliderTween;

        private void Awake()
        {
            RegisterButtonListeners();

            if (audioManager == null)
                audioManager = GetComponent<GuessWhoIAmAudioManager>();
        }

        private void Start()
        {
            PrepareInitialPanelState();

            if (startRoundOnPlay)
                StartCoroutine(StartupFlowRoutine());
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
            KillAutoNext();
        }

        public void StartNewRound()
        {
            Time.timeScale = 1f;
            hasStartedGameplay = true;
            score = 0;
            coins = startingCoins;
            correctCount = 0;
            mistakeCount = 0;
            currentQuestionIndex = -1;
            gameStartTime = Time.time;
            roundQuestions.Clear();

            HidePanel(loadingPanel);
            HidePanel(howToPlayPanel);
            HidePanel(pausePanel);
            HidePanel(resultPanel);

            audioManager?.PlayBackgroundMusic();

            List<GuessWhoQuestionData> validQuestions = questionDatabase != null
                ? questionDatabase.GetValidQuestions()
                : new List<GuessWhoQuestionData>();

            Shuffle(validQuestions);

            int count = Mathf.Min(roundQuestionCount, validQuestions.Count);
            for (int i = 0; i < count; i++)
                roundQuestions.Add(validQuestions[i]);

            totalQuestionCount = roundQuestions.Count;

            if (roundQuestions.Count == 0)
            {
                SetGuideMessage("No valid questions found. Assign a database.");
                UpdateHud();
                SetNextButtonVisible(false);
                return;
            }

            GoToNextQuestion();
        }

        public void GoToNextQuestion()
        {
            KillAutoNext();

            currentQuestionIndex++;
            if (currentQuestionIndex >= roundQuestions.Count)
            {
                ShowResultPanel();
                return;
            }

            currentQuestion = roundQuestions[currentQuestionIndex];
            revealedClueCount = 1;
            selectedClueIndex = 0;
            isAnswered = false;

            BuildOptionsForCurrentQuestion();
            UpdateHud();
            UpdateClueCards(true);
            UpdateOptionButtons(true);
            UpdateRevealButton();
            SetNextButtonVisible(false);
            SetGuideMessage(startGuideMessage);
            AnimatePanelIn();
        }

        public void RevealNextClue()
        {
            if (isAnswered || currentQuestion == null || revealedClueCount >= 3)
                return;

            int newlyRevealedIndex = revealedClueCount;

            audioManager?.PlayReveal(currentQuestion);
            revealedClueCount = Mathf.Clamp(revealedClueCount + 1, 1, 3);
            selectedClueIndex = newlyRevealedIndex;

            UpdateClueCards(!useClueRevealSpinAnimation);
            UpdateRevealButton();
            SetGuideMessage(revealGuideMessage);

            if (useClueRevealSpinAnimation)
                AnimateClueRevealSpin(newlyRevealedIndex);

            PulseRevealButton();
        }

        public void SelectClueCard(int index)
        {
            if (index < 0 || index >= 3)
                return;

            bool isUnlocked = index < revealedClueCount;

            if (isUnlocked)
            {
                // Already-open clues are read-only. Do not switch the active card,
                // because the active card represents the current answer points.
                return;
            }

            if (isAnswered || currentQuestion == null)
                return;

            // Tapping the next locked clue works like the Reveal Next Clue button.
            // Example: Clue 1 open -> tap Clue 2 -> reveals Clue 2.
            if (allowLockedClueTapReveal && index == revealedClueCount)
            {
                RevealNextClue();
                return;
            }

            // Tapping Clue 3 before Clue 2 gives only a visual hint.
            // No popup or new UI object needed.
            AnimateLockedClueTapHint(index);

            if (showGuideMessageOnBlockedLockedClue)
                SetGuideMessage(blockedLockedClueGuideMessage);
        }

        public void PressOption(int index)
        {
            if (isAnswered || currentQuestion == null || index < 0 || index >= optionButtons.Count)
                return;

            GuessWhoOptionButtonUI option = optionButtons[index];
            if (option == null || string.IsNullOrWhiteSpace(option.runtimeAnswer))
                return;

            bool isCorrect = string.Equals(option.runtimeAnswer.Trim(), currentQuestion.answer.Trim(), StringComparison.OrdinalIgnoreCase);
            isAnswered = true;

            DisableAllOptions();

            if (isCorrect)
            {
                correctCount++;
                int earnedPoints = GetCurrentAnswerPoints();
                score += earnedPoints;
                MarkOption(index, optionCorrectColor);
                PunchOption(option);
                ShowPointsPopup(earnedPoints, option.root);
                audioManager?.PlayCorrect(currentQuestion);
                SetGuideMessage(correctGuideMessage);
            }
            else
            {
                mistakeCount++;
                MarkOption(index, optionWrongColor);
                ShakeOption(option);
                RevealAllCluesAfterWrong();
                HighlightCorrectOption();
                audioManager?.PlayWrong(currentQuestion);
                SetGuideMessage($"Answer revealed: {currentQuestion.answer}");
            }

            UpdateHud();
            UpdateRevealButton();
            SetNextButtonVisible(true);
            StartAutoNext();
        }

        public void ShowHowToPlayPanel()
        {
            audioManager?.PlayButtonClick();
            SetupHowToPanel(true);
            ShowPanel(howToPlayPanel, true);
        }

        public void HideHowToPlayPanel()
        {
            audioManager?.PlayButtonClick();
            HidePanel(howToPlayPanel);
        }

        public void ShowNextHowToSlide()
        {
            audioManager?.PlayButtonClick();
            BuildRuntimeHowToSlides();
            if (runtimeHowToSlides.Count <= 0)
                return;

            howToPageIndex = Mathf.Clamp(howToPageIndex + 1, 0, runtimeHowToSlides.Count - 1);
            UpdateHowToPanelView();
        }

        public void ShowPreviousHowToSlide()
        {
            audioManager?.PlayButtonClick();
            BuildRuntimeHowToSlides();
            if (runtimeHowToSlides.Count <= 0)
                return;

            howToPageIndex = Mathf.Clamp(howToPageIndex - 1, 0, runtimeHowToSlides.Count - 1);
            UpdateHowToPanelView();
        }

        public void CompleteHowToPlayPanel()
        {
            audioManager?.PlayButtonClick();
            HidePanel(howToPlayPanel);
        }

        public void PauseGame()
        {
            audioManager?.PlayButtonClick();
            Time.timeScale = 0f;
            ShowPanel(pausePanel, true);
        }

        public void ResumeGame()
        {
            audioManager?.PlayButtonClick();
            Time.timeScale = 1f;
            HidePanel(pausePanel);
        }

        public void ContinueToBloomRewards()
        {
            audioManager?.PlayButtonClick();
            HidePanel(resultPanel);
            ShowBloomPostGame();
        }

        public void OnPlayAgain()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

        public void OnRewardScreenOpen()
        {
            audioManager?.StopBackgroundMusic();
        }

        private IEnumerator StartupFlowRoutine()
        {
            if (startupFlowRunning)
                yield break;

            startupFlowRunning = true;
            PrepareInitialPanelState();
            BuildBloomSkillList();

            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPreGame(bloomSkills);
                yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
            }

            if (showLoadingPanelBeforeHowTo && loadingPanel != null)
            {
                ShowPanel(loadingPanel, false);
                yield return PlayLoadingPanelRoutine();
                HidePanel(loadingPanel);
            }

            if (showHowToBeforeFirstQuestion && howToPlayPanel != null)
            {
                SetupHowToPanel(true);
                ShowPanel(howToPlayPanel, true);
                yield return new WaitUntil(() => howToPlayPanel == null || !howToPlayPanel.activeSelf);
            }

            StartNewRound();
            startupFlowRunning = false;
        }


        private IEnumerator PlayLoadingPanelRoutine()
        {
            if (loadingTitleText != null)
                loadingTitleText.text = loadingTitle;

            float duration = Mathf.Max(0.05f, loadingPanelSeconds);
            float elapsed = 0f;
            UpdateLoadingProgress(0f);

            while (elapsed < duration)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                UpdateLoadingProgress(progress);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            UpdateLoadingProgress(1f);
        }

        private void UpdateLoadingProgress(float normalizedProgress)
        {
            float progress = Mathf.Clamp01(normalizedProgress);

            if (loadingProgressSlider != null)
                loadingProgressSlider.value = progress;

            if (loadingStatusText != null)
            {
                if (!string.IsNullOrEmpty(loadingTextFormat) && loadingTextFormat.Contains("{0}"))
                    loadingStatusText.text = string.Format(loadingTextFormat, Mathf.RoundToInt(progress * 100f));
                else
                    loadingStatusText.text = string.IsNullOrEmpty(loadingTextFormat) ? "Loading..." : loadingTextFormat;
            }
        }

        private void SetupHowToPanel(bool resetPage)
        {
            BuildRuntimeHowToSlides();

            if (resetPage)
                howToPageIndex = 0;

            UpdateHowToPanelView();
        }

        private void BuildRuntimeHowToSlides()
        {
            runtimeHowToSlides.Clear();

            if (howToGuideSprites == null)
                return;

            for (int i = 0; i < howToGuideSprites.Count; i++)
            {
                if (howToGuideSprites[i] != null)
                    runtimeHowToSlides.Add(howToGuideSprites[i]);
            }
        }

        private void UpdateHowToPanelView()
        {
            BuildRuntimeHowToSlides();
            bool hasImages = runtimeHowToSlides.Count > 0;

            if (hasImages)
                howToPageIndex = Mathf.Clamp(howToPageIndex, 0, runtimeHowToSlides.Count - 1);
            else
                howToPageIndex = 0;

            bool isFinalImage = hasImages && howToPageIndex >= runtimeHowToSlides.Count - 1;

            if (howToGuideImage != null)
            {
                howToGuideImage.gameObject.SetActive(hasImages);
                howToGuideImage.preserveAspect = true;
                howToGuideImage.sprite = hasImages ? runtimeHowToSlides[howToPageIndex] : null;
            }

            if (howToBackupText != null)
            {
                howToBackupText.gameObject.SetActive(!hasImages);
                howToBackupText.text = howToBackupMessage;
            }

            if (howToPageText != null)
            {
                howToPageText.gameObject.SetActive(hasImages && runtimeHowToSlides.Count > 1);
                howToPageText.text = hasImages ? $"{howToPageIndex + 1} / {runtimeHowToSlides.Count}" : string.Empty;
            }

            if (howToPreviousButton != null)
            {
                howToPreviousButton.gameObject.SetActive(hasImages && runtimeHowToSlides.Count > 1);
                howToPreviousButton.interactable = howToPageIndex > 0;
            }

            if (howToNextButton != null)
            {
                howToNextButton.gameObject.SetActive(hasImages && runtimeHowToSlides.Count > 1 && !isFinalImage);
                howToNextButton.interactable = hasImages && howToPageIndex < runtimeHowToSlides.Count - 1;
            }

            if (howToStartButton != null)
            {
                howToStartButton.gameObject.SetActive(!hasImages || isFinalImage || runtimeHowToSlides.Count <= 1);
                SetButtonText(howToStartButton, hasImages ? howToStartButtonText : howToContinueButtonText);
            }
        }

        private void SetButtonText(Button button, string text)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = text;
        }

        private void PrepareInitialPanelState()
        {
            HidePanel(loadingPanel);
            HidePanel(howToPlayPanel);
            HidePanel(pausePanel);
            HidePanel(resultPanel);
            SetNextButtonVisible(false);
            UpdateLoadingProgress(0f);
            SetupHowToPanel(true);
        }

        private void RegisterButtonListeners()
        {
            if (listenersRegistered)
                return;

            listenersRegistered = true;

            if (revealButton != null)
                revealButton.onClick.AddListener(RevealNextClue);

            if (nextButton != null)
                nextButton.onClick.AddListener(GoToNextQuestion);

            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseGame);

            if (helpButton != null)
                helpButton.onClick.AddListener(ShowHowToPlayPanel);

            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (closeHowToButton != null && closeHowToButton != howToStartButton)
                closeHowToButton.onClick.AddListener(HideHowToPlayPanel);

            if (howToPreviousButton != null)
                howToPreviousButton.onClick.AddListener(ShowPreviousHowToSlide);

            if (howToNextButton != null)
                howToNextButton.onClick.AddListener(ShowNextHowToSlide);

            if (howToStartButton != null)
                howToStartButton.onClick.AddListener(CompleteHowToPlayPanel);

            if (restartButton != null)
                restartButton.onClick.AddListener(StartNewRound);

            if (resultRestartButton != null)
                resultRestartButton.onClick.AddListener(StartNewRound);

            if (resultContinueButton != null)
                resultContinueButton.onClick.AddListener(ContinueToBloomRewards);

            for (int i = 0; i < clueCards.Count; i++)
            {
                int cachedIndex = i;
                if (clueCards[i] != null && clueCards[i].button != null)
                    clueCards[i].button.onClick.AddListener(() => SelectClueCard(cachedIndex));
            }

            for (int i = 0; i < optionButtons.Count; i++)
            {
                int cachedIndex = i;
                if (optionButtons[i] != null && optionButtons[i].button != null)
                    optionButtons[i].button.onClick.AddListener(() => PressOption(cachedIndex));
            }
        }

        private void BuildOptionsForCurrentQuestion()
        {
            currentOptionValues.Clear();

            if (currentQuestion == null)
                return;

            AddUniqueOption(currentQuestion.answer);

            if (currentQuestion.manualWrongOptions != null)
            {
                List<string> manualWrongs = new List<string>(currentQuestion.manualWrongOptions);
                Shuffle(manualWrongs);
                for (int i = 0; i < manualWrongs.Count && currentOptionValues.Count < optionCount; i++)
                    AddUniqueOption(manualWrongs[i]);
            }

            if (questionDatabase != null && currentOptionValues.Count < optionCount)
            {
                List<string> fallbackAnswers = questionDatabase.GetAnswersExcept(currentQuestion.answer);
                Shuffle(fallbackAnswers);
                for (int i = 0; i < fallbackAnswers.Count && currentOptionValues.Count < optionCount; i++)
                    AddUniqueOption(fallbackAnswers[i]);
            }

            while (currentOptionValues.Count < optionCount)
                AddUniqueOption($"Option {currentOptionValues.Count + 1}");

            Shuffle(currentOptionValues);
        }

        private void AddUniqueOption(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            string clean = value.Trim();
            for (int i = 0; i < currentOptionValues.Count; i++)
            {
                if (string.Equals(currentOptionValues[i], clean, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            currentOptionValues.Add(clean);
        }

        private void UpdateHud()
        {
            if (scoreText != null)
                scoreText.text = score.ToString();

            if (coinText != null)
                coinText.text = coins.ToString();

            int total = Mathf.Max(1, roundQuestions.Count > 0 ? roundQuestions.Count : roundQuestionCount);
            int displayIndex = Mathf.Clamp(currentQuestionIndex + 1, 0, total);

            if (questionProgressText != null)
                questionProgressText.text = $"Question {displayIndex} / {total}";

            if (progressFillImage != null)
                progressFillImage.fillAmount = total <= 0 ? 0f : displayIndex / (float)total;

            for (int i = 0; i < progressStepMarkers.Count; i++)
            {
                if (progressStepMarkers[i] == null)
                    continue;

                bool shouldShow = i < total;
                progressStepMarkers[i].gameObject.SetActive(shouldShow);
                if (shouldShow)
                    progressStepMarkers[i].color = i < displayIndex ? progressActiveColor : progressInactiveColor;
            }
        }

        private void UpdateClueCards(bool animate)
        {
            for (int i = 0; i < clueCards.Count; i++)
            {
                GuessWhoClueCardUI card = clueCards[i];
                if (card == null)
                    continue;

                bool exists = i < 3;
                bool unlocked = i < revealedClueCount;
                bool selected = i == selectedClueIndex && unlocked;

                if (card.root != null)
                    card.root.gameObject.SetActive(exists);

                if (!exists)
                    continue;

                if (card.badgeText != null)
                    card.badgeText.text = (i + 1).ToString();

                if (card.badgeBackground != null)
                    card.badgeBackground.color = selected ? clueChipActiveColor : (unlocked ? clueChipUnlockedDimColor : clueChipLockedColor);

                if (card.outline != null)
                    card.outline.effectColor = selected ? clueChipActiveColor : (unlocked ? new Color32(255, 220, 120, 90) : new Color32(160, 142, 196, 120));

                if (card.titleText != null)
                    card.titleText.text = $"CLUE {i + 1}";

                if (card.clueText != null)
                    card.clueText.text = unlocked && currentQuestion != null ? currentQuestion.GetClue(i) : lockedClueText;

                if (card.valueChipText != null)
                    card.valueChipText.text = $"+{GetPointsForClueIndex(i)}";

                Color textColor = selected ? clueActiveTextColor : (unlocked ? clueUnlockedDimTextColor : clueLockedTextColor);

                if (card.titleText != null)
                    card.titleText.color = textColor;

                if (card.clueText != null)
                    card.clueText.color = textColor;

                if (card.valueChipText != null)
                    card.valueChipText.color = clueActiveTextColor;

                if (card.valueChipBackground != null)
                    card.valueChipBackground.color = selected ? clueChipActiveColor : (unlocked ? clueChipUnlockedDimColor : clueChipLockedColor);

                if (card.selectedPointer != null)
                    card.selectedPointer.gameObject.SetActive(selected);

                if (card.lockIconBackground != null)
                    card.lockIconBackground.gameObject.SetActive(!unlocked);

                if (card.lockIconText != null)
                    card.lockIconText.gameObject.SetActive(false);

                if (card.background != null)
                    card.background.color = selected ? clueActiveColor : (unlocked ? clueUnlockedColor : clueLockedColor);

                if (card.canvasGroup != null)
                {
                    card.canvasGroup.alpha = 1f;
                    // Only locked clue cards are clickable. Already-open cards are read-only,
                    // so tapping Clue 1 after opening Clue 3 cannot wrongly imply +10 points.
                    card.canvasGroup.interactable = true;
                    card.canvasGroup.blocksRaycasts = !unlocked && !isAnswered;
                }

                if (card.button != null)
                    card.button.interactable = true;

                AnimateClueCard(card, selected, unlocked, animate);
            }
        }

        private void UpdateOptionButtons(bool animate)
        {
            int visibleCount = Mathf.Clamp(optionCount, 3, Mathf.Min(4, optionButtons.Count));

            for (int i = 0; i < optionButtons.Count; i++)
            {
                GuessWhoOptionButtonUI option = optionButtons[i];
                if (option == null)
                    continue;

                bool active = i < visibleCount;
                if (option.root != null)
                    option.root.gameObject.SetActive(active);

                if (!active)
                    continue;

                string value = i < currentOptionValues.Count ? currentOptionValues[i] : string.Empty;
                option.runtimeAnswer = value;

                if (option.labelText != null)
                    option.labelText.text = value;

                if (option.letterBadgeText != null)
                    option.letterBadgeText.text = ((char)('A' + i)).ToString();

                if (option.background != null)
                    option.background.color = optionNormalColor;

                if (option.button != null)
                    option.button.interactable = true;

                if (animate)
                    AnimateOptionSpawn(option, i);
            }
        }

        private void UpdateRevealButton()
        {
            bool canReveal = !isAnswered && currentQuestion != null && revealedClueCount < 3;

            if (revealButton != null)
                revealButton.interactable = canReveal;

            if (revealButtonMainText != null)
                revealButtonMainText.text = canReveal ? "Reveal Next Clue" : "No More Clues";

            if (revealButtonSubText != null)
            {
                if (canReveal)
                {
                    int nextPoints = revealedClueCount == 1 ? clue2AnswerPoints : clue3AnswerPoints;
                    revealButtonSubText.text = $"Your answer points will be {nextPoints}";
                }
                else
                {
                    revealButtonSubText.text = isAnswered ? "Answer locked" : "Use your best guess";
                }
            }
        }

        private void SetNextButtonVisible(bool visible)
        {
            if (nextButton != null)
                nextButton.gameObject.SetActive(visible);

            if (nextButtonProgressSlider != null)
                nextButtonProgressSlider.value = 0f;

            if (nextButtonText != null)
                nextButtonText.text = visible ? $"Next {Mathf.CeilToInt(autoNextSeconds)}s" : "Next";
        }

        private void DisableAllOptions()
        {
            for (int i = 0; i < optionButtons.Count; i++)
            {
                GuessWhoOptionButtonUI option = optionButtons[i];
                if (option == null)
                    continue;

                if (option.button != null)
                    option.button.interactable = false;

                if (option.background != null)
                    option.background.color = optionDisabledColor;
            }
        }

        private void MarkOption(int index, Color color)
        {
            if (index < 0 || index >= optionButtons.Count || optionButtons[index] == null)
                return;

            if (optionButtons[index].background != null)
                optionButtons[index].background.color = color;
        }

        private void HighlightCorrectOption()
        {
            if (currentQuestion == null)
                return;

            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (optionButtons[i] == null)
                    continue;

                if (string.Equals(optionButtons[i].runtimeAnswer, currentQuestion.answer, StringComparison.OrdinalIgnoreCase))
                    MarkOption(i, optionCorrectColor);
            }
        }

        private void RevealAllCluesAfterWrong()
        {
            revealedClueCount = 3;
            selectedClueIndex = 2;
            UpdateClueCards(true);
        }

        private int GetCurrentAnswerPoints()
        {
            if (revealedClueCount <= 1)
                return clue1AnswerPoints;

            if (revealedClueCount == 2)
                return clue2AnswerPoints;

            return clue3AnswerPoints;
        }

        private int GetPointsForClueIndex(int clueIndex)
        {
            if (clueIndex <= 0)
                return clue1AnswerPoints;

            if (clueIndex == 1)
                return clue2AnswerPoints;

            return clue3AnswerPoints;
        }

        private void ShowPointsPopup(int points, RectTransform sourceRect)
        {
            if (pointsPopupTextPrefab == null || points <= 0)
                return;

            RectTransform parent = pointsPopupParent;
            if (parent == null && sourceRect != null)
            {
                Canvas canvas = sourceRect.GetComponentInParent<Canvas>();
                if (canvas != null)
                    parent = canvas.transform as RectTransform;
            }
            if (parent == null)
                parent = transform as RectTransform;

            Transform popupParentTransform = parent != null ? parent : transform;
            TMP_Text popup = Instantiate(pointsPopupTextPrefab, popupParentTransform);
            popup.gameObject.SetActive(true);
            popup.text = $"+{points}";
            popup.alpha = 1f;

            RectTransform popupRect = popup.transform as RectTransform;
            if (popupRect != null)
            {
                popupRect.localRotation = Quaternion.identity;
                popupRect.localScale = Vector3.one * Mathf.Max(0.01f, pointsPopupStartScale);
                popupRect.anchoredPosition = GetPopupAnchoredPosition(sourceRect, parent) + pointsPopupOffset;

                Sequence sequence = DOTween.Sequence();
                sequence.Join(popupRect.DOAnchorPosY(popupRect.anchoredPosition.y + pointsPopupMoveY, pointsPopupDuration)
                    .SetEase(Ease.OutCubic));
                sequence.Join(popupRect.DOScale(pointsPopupEndScale, pointsPopupDuration * 0.45f)
                    .SetEase(Ease.OutBack));
                sequence.Append(popup.DOFade(0f, pointsPopupDuration * 0.35f)
                    .SetEase(Ease.InQuad));
                sequence.OnComplete(() =>
                {
                    if (popup != null)
                        Destroy(popup.gameObject);
                });
            }
            else
            {
                popup.transform.localScale = Vector3.one;
                popup.DOFade(0f, pointsPopupDuration).OnComplete(() =>
                {
                    if (popup != null)
                        Destroy(popup.gameObject);
                });
            }
        }

        private Vector2 GetPopupAnchoredPosition(RectTransform sourceRect, RectTransform parent)
        {
            if (sourceRect == null)
                return Vector2.zero;

            if (parent == null)
                return sourceRect.anchoredPosition;

            Canvas canvas = parent.GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = canvas.worldCamera;

            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, sourceRect.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, camera, out Vector2 localPoint))
                return localPoint;

            return sourceRect.anchoredPosition;
        }

        private void SetGuideMessage(string message)
        {
            if (guideMessageText == null)
                return;

            guideMessageText.DOKill();
            if (guideBubbleCanvasGroup != null)
                guideBubbleCanvasGroup.DOKill();

            if (guideBubbleCanvasGroup != null)
            {
                guideBubbleCanvasGroup.alpha = 0f;
                guideMessageText.text = message;
                guideBubbleCanvasGroup.DOFade(1f, tweenDuration).SetUpdate(true);
            }
            else
            {
                guideMessageText.text = message;
                guideMessageText.alpha = 0f;
                guideMessageText.DOFade(1f, tweenDuration).SetUpdate(true);
            }
        }

        private void StartAutoNext()
        {
            KillAutoNext();

            if (nextButton == null || !nextButton.gameObject.activeInHierarchy)
                return;

            if (autoNextSeconds <= 0.1f)
            {
                GoToNextQuestion();
                return;
            }

            autoNextCoroutine = StartCoroutine(AutoNextRoutine());

            if (nextButtonProgressSlider != null)
            {
                nextButtonProgressSlider.value = 0f;
                nextButtonSliderTween = nextButtonProgressSlider.DOValue(1f, autoNextSeconds).SetEase(Ease.Linear);
            }
        }

        private IEnumerator AutoNextRoutine()
        {
            float remaining = autoNextSeconds;

            while (remaining > 0f)
            {
                if (nextButtonText != null)
                    nextButtonText.text = $"Next {Mathf.CeilToInt(remaining)}s";

                remaining -= Time.deltaTime;
                yield return null;
            }

            GoToNextQuestion();
        }

        private void KillAutoNext()
        {
            if (autoNextCoroutine != null)
            {
                StopCoroutine(autoNextCoroutine);
                autoNextCoroutine = null;
            }

            if (nextButtonSliderTween != null && nextButtonSliderTween.IsActive())
                nextButtonSliderTween.Kill();

            if (nextButtonProgressSlider != null)
                nextButtonProgressSlider.value = 0f;
        }

        private void ShowResultPanel()
        {
            KillAutoNext();
            SetNextButtonVisible(false);
            UpdateHud();
            audioManager?.PlayResult();

            if (resultTitleText != null)
                resultTitleText.text = "Round Complete";

            if (resultScoreText != null)
                resultScoreText.text = $"Score: {score}";

            if (resultMessageText != null)
                resultMessageText.text = $"Correct: {correctCount} / {Mathf.Max(1, totalQuestionCount)}   Mistakes: {mistakeCount}";

            ShowPanel(resultPanel, true);
        }

        private void ShowBloomPostGame()
        {
            BuildBloomSkillList();
            audioManager?.StopBackgroundMusic();

            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                float timeTaken = Mathf.Max(0f, Time.time - gameStartTime);
                float timeScore = Mathf.Clamp01(1f - (timeTaken / Mathf.Max(1f, expectedMaxTime)));
                float accuracyScore = totalQuestionCount > 0 ? Mathf.Clamp01((float)correctCount / totalQuestionCount) : 0f;

                GameEvaluationData eval = new GameEvaluationData
                {
                    timeScore = timeScore,
                    accuracyScore = accuracyScore,
                    mistakeCount = mistakeCount,
                    timeTaken = timeTaken
                };

                RewardManager.Instance.ShowPostGame(bloomSkills, eval);
            }
            else
            {
                SetGuideMessage("Reward system is not available in this scene.");
                ShowPanel(resultPanel, true);
            }
        }

        private void BuildBloomSkillList()
        {
            bloomSkills.Clear();
            AddBloomSkill(primaryBloomSkill, primaryBloomMaxScore);

            if (secondaryBloomMaxScore > 0.01f)
                AddBloomSkill(secondaryBloomSkill, secondaryBloomMaxScore);
        }

        private void AddBloomSkill(BloomSkillType type, float maxScore)
        {
            if (maxScore <= 0f)
                return;

            if (useCustomBloomWeights)
                bloomSkills.Add(new SkillEntry(type, maxScore, timeWeight: bloomTimeWeight, accuracyWeight: bloomAccuracyWeight));
            else
                bloomSkills.Add(new SkillEntry(type, maxScore));
        }

        private void ShowPanel(GameObject panel, bool pop)
        {
            if (panel == null)
                return;

            panel.SetActive(true);

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.AddComponent<CanvasGroup>();

            group.DOKill();
            group.alpha = 0f;
            group.DOFade(1f, tweenDuration).SetUpdate(true);

            if (pop && panel.transform.childCount > 0)
            {
                RectTransform rect = panel.transform.GetChild(0) as RectTransform;
                if (rect != null)
                {
                    rect.DOKill();
                    rect.localScale = Vector3.one * 0.9f;
                    rect.DOScale(1f, tweenDuration).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }
        }

        private void HidePanel(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        private void AnimatePanelIn()
        {
            for (int i = 0; i < clueCards.Count; i++)
            {
                if (clueCards[i] != null && clueCards[i].root != null)
                    clueCards[i].root.DOComplete();
            }
        }

        private void AnimateClueCard(GuessWhoClueCardUI card, bool selected, bool unlocked, bool animate)
        {
            if (card == null || card.root == null)
                return;

            float targetScale = selected ? selectedClueScale : (unlocked ? 1f : lockedClueScale);
            card.root.DOKill();

            if (animate)
                card.root.DOScale(targetScale, tweenDuration).SetEase(Ease.OutBack);
            else
                card.root.localScale = Vector3.one * targetScale;
        }

        private void AnimateOptionSpawn(GuessWhoOptionButtonUI option, int index)
        {
            if (option == null || option.root == null)
                return;

            option.root.DOKill();
            option.root.localScale = Vector3.one * 0.92f;
            option.root.DOScale(1f, tweenDuration)
                .SetDelay(index * optionSpawnDelay)
                .SetEase(Ease.OutBack);
        }

        private void PunchOption(GuessWhoOptionButtonUI option)
        {
            if (option == null || option.root == null)
                return;

            option.root.DOKill();
            option.root.DOPunchScale(Vector3.one * 0.12f, 0.28f, 8, 0.7f);
        }

        private void ShakeOption(GuessWhoOptionButtonUI option)
        {
            if (option == null || option.root == null)
                return;

            option.root.DOKill();
            option.root.DOShakeAnchorPos(0.28f, 18f, 12, 90f, false, true);
        }

        private void AnimateLockedClueTapHint(int attemptedIndex)
        {
            int nextRequiredIndex = Mathf.Clamp(revealedClueCount, 0, 2);

            if (attemptedIndex >= 0 && attemptedIndex < clueCards.Count)
            {
                GuessWhoClueCardUI attemptedCard = clueCards[attemptedIndex];
                if (attemptedCard != null && attemptedCard.root != null)
                {
                    attemptedCard.root.DOKill();
                    attemptedCard.root.DOShakeAnchorPos(0.25f, lockedClueHintShakeStrength, 10, 90f, false, true);
                }
            }

            if (nextRequiredIndex >= 0 && nextRequiredIndex < clueCards.Count)
            {
                GuessWhoClueCardUI requiredCard = clueCards[nextRequiredIndex];
                if (requiredCard != null && requiredCard.root != null)
                {
                    requiredCard.root.DOKill();
                    requiredCard.root.DOPunchScale(Vector3.one * lockedClueHintPunchScale, 0.35f, 8, 0.8f);
                }
            }

            PulseRevealButton();
        }

        private void AnimateClueRevealSpin(int clueIndex)
        {
            if (clueIndex < 0 || clueIndex >= clueCards.Count)
                return;

            GuessWhoClueCardUI card = clueCards[clueIndex];
            if (card == null || card.root == null)
                return;

            RectTransform root = card.root;
            float targetScale = selectedClueScale;
            float spinAngle = 360f * Mathf.Max(1, clueRevealSpinTurns);

            root.DOKill();
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one * Mathf.Max(0.01f, lockedClueScale);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(root.DOLocalRotate(new Vector3(0f, spinAngle, 0f), clueRevealSpinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic));
            sequence.Join(root.DOScale(targetScale, clueRevealSpinDuration)
                .SetEase(Ease.OutBack));

            if (clueRevealPopScale > 0f)
                sequence.Append(root.DOPunchScale(Vector3.one * clueRevealPopScale, 0.2f, 6, 0.75f));

            sequence.OnComplete(() =>
            {
                if (root == null)
                    return;

                root.localRotation = Quaternion.identity;
                root.localScale = Vector3.one * targetScale;
            });
        }

        private void PulseRevealButton()
        {
            if (revealButton == null)
                return;

            revealButton.transform.DOKill();
            revealButton.transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 8, 0.8f);
        }

        private void Shuffle<T>(List<T> list)
        {
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}
