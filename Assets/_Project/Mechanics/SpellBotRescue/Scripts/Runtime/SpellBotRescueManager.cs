using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using RewardSystem;

namespace NarayanaGames.SpellBotRescue
{
    public enum SpellBotDifficultyFilter
    {
        All,
        Grade3,
        Grade4,
        Grade5
    }

    public enum SpellBotGameState
    {
        Boot,
        Home,
        HowToPlay,
        RoundIntro,
        Editing,
        Validating,
        CorrectFeedback,
        WrongFeedback,
        Result,
        Paused
    }

    public enum SpellBotHowToPlayMode
    {
        FirstTimeAutomatically,
        EveryGameStartAutomatically,
        ManualButtonOnly
    }

    public class SpellBotRescueManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
    {
        [Header("Data")]
        public SpellBotWordDatabase wordDatabase;
        public SpellBotDifficultyFilter difficultyFilter = SpellBotDifficultyFilter.All;
        [Min(1)] public int roundsPerSession = 10;

        [Header("Score Rewards")]
        [FormerlySerializedAs("baseGearsReward")] public int baseScoreReward = 25;
        public int streakNeededForOverdrive = 3;
        public bool useOverdrive = true;
        public int overdriveMultiplier = 2;

        [Header("Progress UI")]
        public Slider progressSlider;
        public TextMeshProUGUI streakLabelText;
        public string streakLabelPrefix = "Streak";

        [Header("Bloom Reward Integration")]
        public bool useBloomRewardSystem = true;
        [Tooltip("Expected full session duration in seconds, used to normalize Bloom timeScore.")]
        public float expectedMaxTime = 120f;
        public string bloomHomeSceneName = "Loader Scene";
        public bool countShowAnswerAsBloomMistake = true;
        public bool stopAudioWhenBloomPostOpens = true;

        private readonly List<SkillEntry> bloomSkills = new List<SkillEntry>
        {
            new SkillEntry(BloomSkillType.Remember, 100f, timeWeight: 0.25f, accuracyWeight: 0.75f),
            new SkillEntry(BloomSkillType.Understand, 70f, timeWeight: 0.30f, accuracyWeight: 0.70f),
            new SkillEntry(BloomSkillType.Apply, 60f, timeWeight: 0.35f, accuracyWeight: 0.65f)
        };

        [Header("Timing")]
        public float correctDelay = 1.5f;
        public float wrongResetDelay = 0.55f;
        public float cursorBlinkInterval = 0.45f;

        [Header("UI Text")]
        public TextMeshProUGUI roundText;
        [FormerlySerializedAs("gearsText")] public TextMeshProUGUI scoreText;
        public TextMeshProUGUI wordText;
        public TextMeshProUGUI hintText;
        public TextMeshProUGUI resultTitleText;
        public TextMeshProUGUI resultBodyText;

        [Header("Fonts")]
        public TMP_FontAsset primaryFont;
        public TMP_FontAsset secondaryFont;
        public Transform fontApplyRoot;
        public bool applyFontsOnAwake = true;

        [Header("Unity InputField Caret")]
        public TMP_InputField wordInputField;
        public bool useUnityInputFieldCaret = true;
        public bool preventMobileAndTabletKeyboard = true;
        public bool blockNativeInputFieldTyping = true;
        public bool keepWordInputFocused = true;
        public bool forceVisibleInputFieldCaret = true;
        [Min(1)] public int inputFieldCaretWidth = 4;
        public Color inputFieldCaretColor = new Color(0.10f, 0.15f, 0.23f, 1f);
        public Color inputFieldSelectionColor = new Color(0.28f, 0.62f, 0.95f, 0.32f);

        [Header("UI Panels")]
        public GameObject homePagePanel;
        public GameObject howToPlayPanel;
        public GameObject pausePanel;
        public GameObject resultPanel;
        public GameObject overdriveLabelRoot;

        [Header("Hint / Answer UI")]
        public Button hintButton;
        public Button showAnswerButton;
        public string hintPrefix = "Hint: ";
        public string showAnswerPrefix = "Correct spelling: ";
        public float answerRevealDelay = 1.25f;
        public bool allowShowAnswerAfterHint = true;

        [Header("Streak UI")]
        public Image[] streakStars;
        public Sprite starSprite;
        public Color streakFilledColor = new Color(1f, 0.78f, 0.16f, 1f);
        public Color streakEmptyColor = new Color(0.78f, 0.78f, 0.78f, 1f);

        [Header("Word Colors")]
        public Color editingWordColor = new Color(0.10f, 0.14f, 0.20f, 1f);
        public Color correctWordColor = new Color(0.07f, 0.62f, 0.31f, 1f);
        public Color wrongWordColor = new Color(0.85f, 0.20f, 0.18f, 1f);

        [Header("Legacy Visual Caret Fallback")]
        public bool useVisualCaret = false;
        public RectTransform visualCaret;
        [Min(1f)] public float caretWidth = 4f;
        public Color caretColor = new Color(0.10f, 0.15f, 0.23f, 1f);

        [Header("Hardware Keyboard Input")]
        public bool enableHardwareKeyboardInput = true;
        public bool arrowKeysMoveCaret = true;
        public bool homeEndKeysMoveCaret = true;
        public bool allowDeleteForward = true;
        public bool enterSubmitsFixed = true;
        public bool escapeClearsWord = false;

        [Header("Scene References")]
        public SpellBotKeyboardView keyboardView;
        public SpellBotUIFeedback feedback;
        public SpellBotRobotView robotView;
        public SpellBotFirstTimeTutorialController firstTimeTutorial;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip keyPressClip;
        public AudioClip correctClip;
        public AudioClip wrongClip;

        [Header("Start Behaviour")]
        public bool showHomePageOnStart = true;
        public SpellBotHowToPlayMode howToPlayMode = SpellBotHowToPlayMode.FirstTimeAutomatically;
        [Tooltip("Separate, scene-based key used only by the How to Play panel.")]
        public string howToPlayPlayerPrefsPrefix = "SpellBotRescue.HowToPlay.Viewed";

        [Header("Legacy Start Behaviour (kept for existing Inspector data)")]
        public bool showHowToPlayOnStart = false;
        [Tooltip("When true, Home Start opens How To Play first. The HTP Start button then begins the real gameplay.")]
        public bool showHowToPlayBeforeFirstRound = true;
        public bool resetHowToPlayToFirstPageOnOpen = true;
        public bool autoStartIfNoHomeOrHowToPlayPanel = true;

        [Header("Future Continue Hook")]
        public UnityEvent onContinuePressed;

        public SpellBotGameState CurrentState { get; private set; } = SpellBotGameState.Boot;

        private readonly List<SpellBotWordEntry> activePlaylist = new List<SpellBotWordEntry>();
        private SpellBotWordEntry currentEntry;
        private string originalIncorrectWord = string.Empty;
        private string currentText = string.Empty;
        private int cursorIndex;
        private int currentRoundIndex;
        private int score;
        private int streak;
        private int correctRounds;
        private int wrongAttempts;
        private bool overdriveActive;
        private bool hasUserEdited;
        private bool inputLocked;
        private bool sessionStarted;
        private bool hintUsedThisRound;
        private bool answerShownThisRound;
        private int hintUses;
        private int answerReveals;
        private float sessionStartTime;
        private bool bloomPostGameShown;
        private bool cursorVisible = true;
        private float cursorTimer;
        private SpellBotGameState stateBeforePause;
        private SpellBotGameState stateBeforeHowToPlay;
        private Coroutine validationRoutine;
        private bool suppressInputFieldEvent;
        private int lastCaretVisibilityRefreshFrame = -1;
        private bool openingSequenceInProgress;
        private bool howToPlayOpenedForStartSequence;
        private bool tutorialInputActive;

        private void Awake()
        {
            ConfigureWordInputField();
            ApplyConfiguredFonts();

            if (keyboardView != null)
            {
                keyboardView.Initialize(this);
            }

            SetPanel(homePagePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            ResetHowToPlayPagerIfNeeded();

            if (feedback != null)
            {
                feedback.HideHintInstant();
                feedback.SetOverdriveGlow(false);
            }

            ApplyOverdriveRobotVisual(false, false);
            PrepareVisualCaret();
            UpdateTopUI();
            UpdateHintAnswerUI();
        }

        private void Start()
        {
            if (useBloomRewardSystem && RewardManager.Instance != null)
            {
                RewardManager.Instance.ShowPreGame(bloomSkills);
                StartCoroutine(BloomPreGameThenLocalFlow());
                return;
            }

            StartLocalOpeningFlow();
        }

        private IEnumerator BloomPreGameThenLocalFlow()
        {
            yield return new WaitUntil(() => RewardManager.Instance == null || RewardManager.Instance.IsPreGameComplete);
            StartLocalOpeningFlow();
        }

        private void StartLocalOpeningFlow()
        {
            if (showHomePageOnStart && homePagePanel != null)
            {
                OpenHomePage();
                return;
            }

            if (autoStartIfNoHomeOrHowToPlayPanel)
            {
                BeginOpeningSequence();
            }
        }

        private void OnDestroy()
        {
            if (wordInputField != null)
            {
                wordInputField.onValueChanged.RemoveListener(HandleInputFieldValueChanged);
                wordInputField.onSubmit.RemoveListener(HandleInputFieldSubmit);
            }
        }

        private void Update()
        {
            if (CurrentState != SpellBotGameState.Editing || inputLocked)
            {
                return;
            }

            HandleHardwareKeyboardInput();

            if (UseUnityInputFieldCaret())
            {
                if (keepWordInputFocused && NeedsInputFieldFocusRepair())
                {
                    RefocusWordInputField();
                }

                return;
            }

            cursorTimer += Time.deltaTime;
            if (cursorTimer >= cursorBlinkInterval)
            {
                cursorTimer = 0f;
                cursorVisible = !cursorVisible;
                RefreshWordDisplay(true);
            }
        }

        public void OpenHomePage()
        {
            Time.timeScale = 1f;
            CurrentState = SpellBotGameState.Home;
            SetInputLocked(true);
            SetPanel(homePagePanel, true);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);

            if (feedback != null)
            {
                feedback.PlayPanelOpen(homePagePanel);
            }
        }

        public void StartGameFromHome()
        {
            BeginOpeningSequence();
        }

        public void OpenHowToPlayPanel()
        {
            stateBeforeHowToPlay = CurrentState;
            ResetHowToPlayPagerIfNeeded();
            SetInputLocked(true);
            SetPanel(homePagePanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);
            SetPanel(howToPlayPanel, true);

            if (feedback != null)
            {
                feedback.PlayPanelOpen(howToPlayPanel);
            }

            CurrentState = SpellBotGameState.HowToPlay;
        }

        private void ResetHowToPlayPagerIfNeeded()
        {
            if (!resetHowToPlayToFirstPageOnOpen || howToPlayPanel == null)
            {
                return;
            }

            SpellBotHowToPlayImagePager pager = howToPlayPanel.GetComponent<SpellBotHowToPlayImagePager>();
            if (pager != null)
            {
                pager.ShowPage(0, false);
            }
        }

        public void CloseHowToPlayPanel()
        {
            MarkHowToPlayViewedForCurrentScene();
            SetPanel(howToPlayPanel, false);

            if (howToPlayOpenedForStartSequence)
            {
                howToPlayOpenedForStartSequence = false;
                ContinueOpeningSequence();
                return;
            }

            if (!sessionStarted)
            {
                OpenHomePage();
                return;
            }

            CurrentState = stateBeforeHowToPlay == SpellBotGameState.Paused ? SpellBotGameState.Paused : stateBeforeHowToPlay;
            SetInputLocked(CurrentState != SpellBotGameState.Editing);

            if (CurrentState == SpellBotGameState.Paused)
            {
                SetPanel(pausePanel, true);
            }

            if (CurrentState == SpellBotGameState.Editing)
            {
                RefocusWordInputField();
            }
        }

        public void StartGameFromHowToPlay()
        {
            MarkHowToPlayViewedForCurrentScene();
            SetPanel(howToPlayPanel, false);

            if (sessionStarted)
            {
                CurrentState = stateBeforeHowToPlay == SpellBotGameState.Paused ? SpellBotGameState.Paused : stateBeforeHowToPlay;
                SetInputLocked(CurrentState != SpellBotGameState.Editing);

                if (CurrentState == SpellBotGameState.Paused)
                {
                    SetPanel(pausePanel, true);
                }
                else if (CurrentState == SpellBotGameState.Editing)
                {
                    RefocusWordInputField();
                }

                return;
            }

            howToPlayOpenedForStartSequence = false;
            openingSequenceInProgress = true;
            ContinueOpeningSequence();
        }

        public void BeginOpeningSequence()
        {
            if (openingSequenceInProgress || tutorialInputActive)
            {
                return;
            }

            openingSequenceInProgress = true;
            Time.timeScale = 1f;
            SetPanel(homePagePanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);

            if (ShouldShowHowToPlayAutomatically() && howToPlayPanel != null)
            {
                howToPlayOpenedForStartSequence = true;
                OpenHowToPlayPanel();
                return;
            }

            ContinueOpeningSequence();
        }

        private void ContinueOpeningSequence()
        {
            SetPanel(howToPlayPanel, false);

            if (firstTimeTutorial != null && firstTimeTutorial.ShouldPlayForCurrentScene())
            {
                firstTimeTutorial.BeginTutorial(this, HandleFirstTimeTutorialCompleted);
                return;
            }

            HandleFirstTimeTutorialCompleted();
        }

        private void HandleFirstTimeTutorialCompleted()
        {
            openingSequenceInProgress = false;
            howToPlayOpenedForStartSequence = false;
            BeginGame();
        }

        private bool ShouldShowHowToPlayAutomatically()
        {
            switch (howToPlayMode)
            {
                case SpellBotHowToPlayMode.EveryGameStartAutomatically:
                    return true;
                case SpellBotHowToPlayMode.FirstTimeAutomatically:
                    return PlayerPrefs.GetInt(GetHowToPlayPlayerPrefsKey(), 0) == 0;
                default:
                    return false;
            }
        }

        private string GetHowToPlayPlayerPrefsKey()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return howToPlayPlayerPrefsPrefix + "." + sceneName;
        }

        private void MarkHowToPlayViewedForCurrentScene()
        {
            PlayerPrefs.SetInt(GetHowToPlayPlayerPrefsKey(), 1);
            PlayerPrefs.Save();
        }

        [ContextMenu("SpellBot/Reset How To Play For Current Scene")]
        public void ResetHowToPlayForCurrentScene()
        {
            PlayerPrefs.DeleteKey(GetHowToPlayPlayerPrefsKey());
            PlayerPrefs.Save();
        }

        public void EnterFirstTimeTutorialMode(SpellBotFirstTimeTutorialController tutorial)
        {
            firstTimeTutorial = tutorial;
            tutorialInputActive = true;
            Time.timeScale = 1f;
            CurrentState = SpellBotGameState.Editing;
            inputLocked = false;
            currentEntry = null;

            SetPanel(homePagePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);

            if (feedback != null)
            {
                feedback.HideHintInstant();
            }

            if (keyboardView != null)
            {
                keyboardView.SetInputLocked(false);
                keyboardView.SetFixedReady(false);
            }

            SetInputFieldInteractable(true);
            UpdateHintAnswerUI();
        }

        public void ExitFirstTimeTutorialMode(SpellBotFirstTimeTutorialController tutorial)
        {
            if (firstTimeTutorial != tutorial)
            {
                return;
            }

            tutorialInputActive = false;
            CurrentState = SpellBotGameState.Boot;
            SetInputLocked(true);

            if (keyboardView != null)
            {
                keyboardView.SetFixedReady(false);
            }
        }

        public void SetTutorialWordDisplay(string text, int caretPosition, bool showCaret)
        {
            currentText = SanitizeWord(text);
            cursorIndex = Mathf.Clamp(caretPosition, 0, currentText.Length);
            cursorVisible = true;
            cursorTimer = 0f;
            SetWordTextColor(editingWordColor);
            PushCurrentTextToUI(showCaret);
        }

        public bool TryGetTutorialCaretWorldPosition(int targetIndex, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (wordText == null)
            {
                return false;
            }

            int previousIndex = cursorIndex;
            cursorIndex = Mathf.Clamp(targetIndex, 0, currentText != null ? currentText.Length : 0);
            wordText.ForceMeshUpdate();

            bool found = TryGetCaretLocalPosition(out Vector2 localPosition, out _);
            cursorIndex = previousIndex;

            if (!found)
            {
                return false;
            }

            worldPosition = wordText.rectTransform.TransformPoint(new Vector3(localPosition.x, localPosition.y, 0f));
            return true;
        }

        public void PlayTutorialKeyPressSound()
        {
            PlayClip(keyPressClip, 0.65f);
        }

        public void ContinueFromResult()
        {
            ShowBloomPostGameIfAvailable();

            if (onContinuePressed != null)
            {
                onContinuePressed.Invoke();
            }
        }

        public void BeginGame()
        {
            Time.timeScale = 1f;
            tutorialInputActive = false;
            openingSequenceInProgress = false;
            howToPlayOpenedForStartSequence = false;
            SetPanel(homePagePanel, false);
            SetPanel(howToPlayPanel, false);
            SetPanel(pausePanel, false);
            SetPanel(resultPanel, false);

            sessionStarted = true;
            score = 0;
            streak = 0;
            correctRounds = 0;
            wrongAttempts = 0;
            hintUses = 0;
            answerReveals = 0;
            overdriveActive = false;
            bloomPostGameShown = false;
            currentRoundIndex = 0;
            sessionStartTime = Time.time;

            ApplyOverdriveRobotVisual(false, false);

            if (feedback != null)
            {
                feedback.HideHintInstant();
                feedback.SetOverdriveGlow(false);
                feedback.PlayPanelClose(howToPlayPanel);
            }

            if (!BuildSessionPlaylist())
            {
                Debug.LogError("Spell-Bot Rescue could not start. Check Word Database entries.", this);
                return;
            }

            StartRound(currentRoundIndex);
        }

        public void RestartGame()
        {
            if (validationRoutine != null)
            {
                StopCoroutine(validationRoutine);
                validationRoutine = null;
            }

            sessionStarted = false;
            openingSequenceInProgress = false;
            howToPlayOpenedForStartSequence = false;
            BeginOpeningSequence();
        }

        public void PauseGame()
        {
            if (tutorialInputActive)
            {
                return;
            }

            if (CurrentState == SpellBotGameState.Paused || CurrentState == SpellBotGameState.Result)
            {
                return;
            }

            stateBeforePause = CurrentState;
            CurrentState = SpellBotGameState.Paused;
            Time.timeScale = 0f;
            SetInputLocked(true);
            SetPanel(pausePanel, true);

            if (feedback != null)
            {
                feedback.PlayPanelOpen(pausePanel);
            }
        }

        public void ResumeGame()
        {
            if (CurrentState != SpellBotGameState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            CurrentState = stateBeforePause;
            SetPanel(pausePanel, false);
            SetInputLocked(CurrentState != SpellBotGameState.Editing);
            UpdateHintAnswerUI();

            if (CurrentState == SpellBotGameState.Editing)
            {
                RefocusWordInputField();
            }
        }

        public void MoveCaretFromScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            if (wordText == null)
            {
                return;
            }

            int requestedIndex = CalculateCaretIndexFromScreenPoint(screenPosition, eventCamera);

            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleCaretInput(requestedIndex);
                return;
            }

            if (!CanEdit())
            {
                return;
            }

            cursorIndex = requestedIndex;
            cursorVisible = true;
            cursorTimer = 0f;
            RefreshWordDisplay(true);
        }

        public void OnHintButtonClicked()
        {
            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleHintInput();
                return;
            }

            if (!CanEdit() || currentEntry == null)
            {
                return;
            }

            RevealHintForCurrentRound(true);
        }

        public void OnShowAnswerButtonClicked()
        {
            if (tutorialInputActive)
            {
                return;
            }

            if (!CanEdit() || currentEntry == null || !hintUsedThisRound || !allowShowAnswerAfterHint || answerShownThisRound)
            {
                return;
            }

            answerShownThisRound = true;
            answerReveals++;
            streak = 0;
            overdriveActive = false;
            ApplyOverdriveRobotVisual(false, true);

            if (feedback != null)
            {
                feedback.ShowHint(showAnswerPrefix + currentEntry.correctWord, hintText);
                feedback.PlayShowAnswerAvailable(hintText != null ? hintText.transform : null);
                feedback.SetOverdriveGlow(false);
            }
            else if (hintText != null)
            {
                hintText.text = showAnswerPrefix + currentEntry.correctWord;
            }

            UpdateTopUI();
            UpdateHintAnswerUI();
            RefocusWordInputField();
        }

        public void OnKeyboardLetter(string letter)
        {
            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleLetterInput(letter);
                return;
            }

            if (!CanEdit() || string.IsNullOrWhiteSpace(letter))
            {
                return;
            }

            char cleanChar = char.ToLowerInvariant(letter[0]);
            if (!IsAllowedLetter(cleanChar))
            {
                return;
            }

            PlayClip(keyPressClip, 0.65f);

            cursorIndex = Mathf.Clamp(cursorIndex, 0, currentText.Length);
            currentText = currentText.Insert(cursorIndex, cleanChar.ToString());
            cursorIndex++;
            MarkEditedAndRefresh();
        }

        public void OnKeyboardBackspace()
        {
            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleBackspaceInput();
                return;
            }

            if (!CanEdit() || cursorIndex <= 0 || currentText.Length == 0)
            {
                return;
            }

            PlayClip(keyPressClip, 0.65f);
            cursorIndex = Mathf.Clamp(cursorIndex, 0, currentText.Length);
            currentText = currentText.Remove(cursorIndex - 1, 1);
            cursorIndex--;
            MarkEditedAndRefresh();
        }

        public void OnKeyboardDeleteForward()
        {
            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleDeleteForwardInput();
                return;
            }

            if (!CanEdit() || cursorIndex < 0 || cursorIndex >= currentText.Length)
            {
                return;
            }

            PlayClip(keyPressClip, 0.65f);
            cursorIndex = Mathf.Clamp(cursorIndex, 0, currentText.Length);
            currentText = currentText.Remove(cursorIndex, 1);
            MarkEditedAndRefresh();
        }

        public void MoveCaretLeft()
        {
            MoveCaretToIndex(cursorIndex - 1);
        }

        public void MoveCaretRight()
        {
            MoveCaretToIndex(cursorIndex + 1);
        }

        public void MoveCaretToStart()
        {
            MoveCaretToIndex(0);
        }

        public void MoveCaretToEnd()
        {
            MoveCaretToIndex(currentText != null ? currentText.Length : 0);
        }

        public void MoveCaretToIndex(int targetIndex)
        {
            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleCaretInput(targetIndex);
                return;
            }

            if (!CanEdit())
            {
                return;
            }

            int maxLength = currentText != null ? currentText.Length : 0;
            cursorIndex = Mathf.Clamp(targetIndex, 0, maxLength);
            cursorVisible = true;
            cursorTimer = 0f;
            RefreshWordDisplay(true);
        }

        public void OnKeyboardClear()
        {
            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleClearInput();
                return;
            }

            if (!CanEdit())
            {
                return;
            }

            PlayClip(keyPressClip, 0.65f);
            ResetCurrentTextToOriginal(false);

            if (feedback != null)
            {
                if (!hintUsedThisRound)
                {
                    feedback.HideHint();
                }

                feedback.PlayClearPulse(wordText);
            }
        }

        public void OnKeyboardFixed()
        {
            if (tutorialInputActive && firstTimeTutorial != null)
            {
                firstTimeTutorial.HandleFixedInput();
                return;
            }

            if (inputLocked || CurrentState != SpellBotGameState.Editing)
            {
                return;
            }

            if (!hasUserEdited)
            {
                if (feedback != null)
                {
                    feedback.PlayDisabledFixedShake();
                }
                return;
            }

            if (validationRoutine != null)
            {
                StopCoroutine(validationRoutine);
            }

            validationRoutine = StartCoroutine(ValidateAnswerRoutine());
        }

        private void HandleHardwareKeyboardInput()
        {
            if (!enableHardwareKeyboardInput || !CanEdit())
            {
                return;
            }

            if (arrowKeysMoveCaret)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    MoveCaretLeft();
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    MoveCaretRight();
                }
            }

            if (homeEndKeysMoveCaret)
            {
                if (Input.GetKeyDown(KeyCode.Home))
                {
                    MoveCaretToStart();
                }

                if (Input.GetKeyDown(KeyCode.End))
                {
                    MoveCaretToEnd();
                }
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                OnKeyboardBackspace();
            }

            if (allowDeleteForward && Input.GetKeyDown(KeyCode.Delete))
            {
                OnKeyboardDeleteForward();
            }

            if (enterSubmitsFixed && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                OnKeyboardFixed();
                return;
            }

            if (escapeClearsWord && Input.GetKeyDown(KeyCode.Escape))
            {
                OnKeyboardClear();
                return;
            }

            string typedCharacters = Input.inputString;
            if (string.IsNullOrEmpty(typedCharacters))
            {
                return;
            }

            for (int i = 0; i < typedCharacters.Length; i++)
            {
                char typedChar = typedCharacters[i];

                if (typedChar == '\b' || typedChar == '\n' || typedChar == '\r')
                {
                    continue;
                }

                typedChar = char.ToLowerInvariant(typedChar);
                if (!IsAllowedLetter(typedChar))
                {
                    continue;
                }

                OnKeyboardLetter(typedChar.ToString());
            }
        }

        private bool BuildSessionPlaylist()
        {
            activePlaylist.Clear();

            if (wordDatabase == null || wordDatabase.entries == null || wordDatabase.entries.Count == 0)
            {
                return false;
            }

            List<SpellBotWordEntry> pool = new List<SpellBotWordEntry>();
            foreach (SpellBotWordEntry entry in wordDatabase.entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.incorrectWord) || string.IsNullOrWhiteSpace(entry.correctWord))
                {
                    continue;
                }

                if (!PassesDifficultyFilter(entry))
                {
                    continue;
                }

                pool.Add(entry);
            }

            if (pool.Count == 0)
            {
                return false;
            }

            Shuffle(pool);

            int targetCount = Mathf.Min(roundsPerSession, pool.Count);
            if (targetCount < roundsPerSession)
            {
                Debug.LogWarning($"Spell-Bot Rescue found only {targetCount} usable words. Add more entries for a full {roundsPerSession}-round session.", this);
            }

            for (int i = 0; i < targetCount; i++)
            {
                activePlaylist.Add(pool[i]);
            }

            return activePlaylist.Count > 0;
        }

        private bool PassesDifficultyFilter(SpellBotWordEntry entry)
        {
            switch (difficultyFilter)
            {
                case SpellBotDifficultyFilter.Grade3:
                    return entry.difficultyTier == 3;
                case SpellBotDifficultyFilter.Grade4:
                    return entry.difficultyTier == 4;
                case SpellBotDifficultyFilter.Grade5:
                    return entry.difficultyTier == 5;
                default:
                    return true;
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        private void StartRound(int index)
        {
            if (index < 0 || index >= activePlaylist.Count)
            {
                ShowResult();
                return;
            }

            CurrentState = SpellBotGameState.RoundIntro;
            currentEntry = activePlaylist[index];
            hintUsedThisRound = false;
            answerShownThisRound = false;
            originalIncorrectWord = SanitizeWord(currentEntry.incorrectWord);
            ResetCurrentTextToOriginal(false);
            UpdateHintAnswerUI();

            if (robotView != null)
            {
                robotView.SetIdle();
            }

            if (feedback != null)
            {
                feedback.HideHintInstant();
                feedback.PlayRoundIntro(wordText);
            }

            CurrentState = SpellBotGameState.Editing;
            SetInputLocked(false);
            UpdateTopUI();
            RefreshWordDisplay(true);
            RefocusWordInputField();
        }

        private IEnumerator ValidateAnswerRoutine()
        {
            CurrentState = SpellBotGameState.Validating;
            SetInputLocked(true);
            RefreshWordDisplay(false);

            bool correct = Normalize(currentText) == Normalize(currentEntry.correctWord);

            if (correct)
            {
                yield return HandleCorrectRoutine();
            }
            else
            {
                yield return HandleWrongRoutine();
            }

            validationRoutine = null;
        }

        private IEnumerator ShowAnswerRoutine()
        {
            // Kept only for backward compatibility with older saved UnityEvents.
            // Current design reveals the answer in the hint area and lets the child type it manually for 0 score.
            OnShowAnswerButtonClicked();
            yield break;
        }

        private IEnumerator HandleCorrectRoutine()
        {
            CurrentState = SpellBotGameState.CorrectFeedback;
            correctRounds++;
            bool wasOverdriveActive = overdriveActive;

            int reward = 0;
            if (answerShownThisRound)
            {
                // Student used Show Answer. They can still learn and complete the round, but score stays 0.
                streak = 0;
                overdriveActive = false;
            }
            else
            {
                streak++;
                reward = overdriveActive ? baseScoreReward * Mathf.Max(1, overdriveMultiplier) : baseScoreReward;
                score += reward;

                if (useOverdrive && streak >= streakNeededForOverdrive)
                {
                    streak = streakNeededForOverdrive;
                    overdriveActive = true;
                }
            }

            SetInputFieldInteractable(false);
            SetVisualCaretActive(false);

            currentText = SanitizeWord(currentEntry.correctWord);
            cursorIndex = currentText.Length;
            PushCurrentTextToUI(false);
            SetWordTextColor(correctWordColor);

            PlayClip(correctClip, 1f);

            bool justEnteredOverdrive = !wasOverdriveActive && overdriveActive;
            ApplyOverdriveRobotVisual(overdriveActive, justEnteredOverdrive);

            if (robotView != null)
            {
                robotView.PlayHappy();
            }

            UpdateTopUI();

            if (feedback != null)
            {
                feedback.PlayCorrectWord(wordText);
                feedback.PlayCorrectMonitorGlow();

                if (reward > 0)
                {
                    feedback.PopScore();
                }

                feedback.SetOverdriveGlow(false);

                if (streakStars != null && streak > 0 && streak <= streakStars.Length)
                {
                    feedback.PopStar(streakStars[streak - 1]);
                }
            }

            yield return new WaitForSeconds(correctDelay);

            currentRoundIndex++;

            if (currentRoundIndex >= activePlaylist.Count)
            {
                ShowResult();
            }
            else
            {
                StartRound(currentRoundIndex);
            }
        }

        private IEnumerator HandleWrongRoutine()
        {
            CurrentState = SpellBotGameState.WrongFeedback;
            wrongAttempts++;
            streak = 0;
            overdriveActive = false;
            ApplyOverdriveRobotVisual(false, true);

            SetWordTextColor(wrongWordColor);
            PlayClip(wrongClip, 1f);

            if (robotView != null)
            {
                robotView.PlaySad();
            }

            if (feedback != null)
            {
                feedback.PlayWrongShake();
            }

            RevealHintForCurrentRound(false);

            if (feedback != null)
            {
                feedback.SetOverdriveGlow(false);
            }

            UpdateTopUI();
            yield return new WaitForSeconds(wrongResetDelay);

            ResetCurrentTextToOriginal(false);
            CurrentState = SpellBotGameState.Editing;
            SetInputLocked(false);
            RefocusWordInputField();
        }

        private void ShowResult()
        {
            CurrentState = SpellBotGameState.Result;
            SetInputLocked(true);
            UpdateTopUI();
            UpdateHintAnswerUI();
            SetPanel(resultPanel, true);

            if (resultTitleText != null)
            {
                resultTitleText.text = "Rescue Complete!";
            }

            if (resultBodyText != null)
            {
                resultBodyText.text = $"Correct Rounds: {correctRounds}/{activePlaylist.Count}\nWrong Tries: {wrongAttempts}\nHints Used: {hintUses}\nAnswers Revealed: {answerReveals}\nScore: {score}";
            }

            if (feedback != null)
            {
                feedback.PlayPanelOpen(resultPanel);
            }
        }

        private void ShowBloomPostGameIfAvailable()
        {
            if (bloomPostGameShown)
            {
                return;
            }

            bloomPostGameShown = true;

            if (!useBloomRewardSystem || RewardManager.Instance == null)
            {
                return;
            }

            float timeTaken = Mathf.Max(0f, Time.time - sessionStartTime);
            float safeExpectedMaxTime = Mathf.Max(1f, expectedMaxTime);
            float timeScore = Mathf.Clamp01(1f - (timeTaken / safeExpectedMaxTime));
            int totalQuestions = activePlaylist.Count > 0 ? activePlaylist.Count : roundsPerSession;
            float accuracyScore = totalQuestions > 0 ? Mathf.Clamp01((float)correctRounds / totalQuestions) : 0f;
            int bloomMistakes = wrongAttempts + (countShowAnswerAsBloomMistake ? answerReveals : 0);

            GameEvaluationData eval = new GameEvaluationData
            {
                timeScore = timeScore,
                accuracyScore = accuracyScore,
                mistakeCount = bloomMistakes,
                timeTaken = timeTaken
            };

            RewardManager.Instance.ShowPostGame(bloomSkills, eval);
        }

        public void OnRewardScreenOpen()
        {
            if (!stopAudioWhenBloomPostOpens)
            {
                return;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            if (SpellBotBgmPlayer.Instance != null)
            {
                SpellBotBgmPlayer.Instance.StopMusicWithFade();
            }
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

            SceneManager.LoadScene(bloomHomeSceneName);
        }

        private void ResetCurrentTextToOriginal(bool keepEditedState)
        {
            currentText = originalIncorrectWord;
            cursorIndex = currentText.Length;
            hasUserEdited = keepEditedState;
            cursorVisible = true;
            cursorTimer = 0f;

            if (keyboardView != null)
            {
                keyboardView.SetFixedReady(hasUserEdited);
            }

            UpdateHintAnswerUI();
            RefreshWordDisplay(true);
        }

        private void MarkEditedAndRefresh()
        {
            hasUserEdited = true;
            cursorVisible = true;
            cursorTimer = 0f;

            if (keyboardView != null)
            {
                keyboardView.SetFixedReady(true);
            }

            RefreshWordDisplay(true);
        }

        private void RefreshWordDisplay(bool showCursor)
        {
            SetWordTextColor(editingWordColor);
            cursorIndex = Mathf.Clamp(cursorIndex, 0, currentText != null ? currentText.Length : 0);

            if (UseUnityInputFieldCaret())
            {
                PushCurrentTextToUI(showCursor && CurrentState == SpellBotGameState.Editing && !inputLocked);
                return;
            }

            RefreshLegacyWordDisplay(showCursor);
        }

        private void ConfigureWordInputField()
        {
            if (wordInputField == null && wordText != null)
            {
                wordInputField = wordText.GetComponentInParent<TMP_InputField>();
            }

            if (wordInputField == null)
            {
                return;
            }

            if (wordText == null)
            {
                wordText = wordInputField.textComponent as TextMeshProUGUI;
            }

            wordInputField.lineType = TMP_InputField.LineType.SingleLine;
            wordInputField.contentType = TMP_InputField.ContentType.Custom;
            wordInputField.inputType = TMP_InputField.InputType.Standard;
            wordInputField.characterValidation = TMP_InputField.CharacterValidation.None;
            wordInputField.keyboardType = TouchScreenKeyboardType.Default;
            wordInputField.richText = false;
            wordInputField.caretWidth = Mathf.Max(1, inputFieldCaretWidth);
            wordInputField.customCaretColor = true;
            inputFieldCaretColor.a = 1f;
            wordInputField.caretColor = inputFieldCaretColor;
            wordInputField.selectionColor = inputFieldSelectionColor;

            // IMPORTANT: TMP_InputField often hides its caret when readOnly is true.
            // We keep it writable for caret rendering, then block native characters manually.
            wordInputField.readOnly = false;

            if (cursorBlinkInterval > 0.01f)
            {
                wordInputField.caretBlinkRate = 1f / cursorBlinkInterval;
            }

            if (preventMobileAndTabletKeyboard)
            {
                wordInputField.shouldHideMobileInput = true;
                TouchScreenKeyboard.hideInput = true;
                TrySetInputFieldBoolProperty(wordInputField, "shouldHideSoftKeyboard", true);
            }

            wordInputField.onValueChanged.RemoveListener(HandleInputFieldValueChanged);
            wordInputField.onSubmit.RemoveListener(HandleInputFieldSubmit);
            wordInputField.onValueChanged.AddListener(HandleInputFieldValueChanged);
            wordInputField.onSubmit.AddListener(HandleInputFieldSubmit);
            wordInputField.onValidateInput = ValidateInputFieldCharacter;
        }

        private void HandleInputFieldValueChanged(string value)
        {
            if (suppressInputFieldEvent || !CanEdit())
            {
                return;
            }

            if (tutorialInputActive)
            {
                PushCurrentTextToUI(true);
                return;
            }

            if (blockNativeInputFieldTyping)
            {
                if (value != currentText)
                {
                    PushCurrentTextToUI(true);
                }

                return;
            }

            string sanitized = SanitizeInputLettersOnly(value);
            if (sanitized != value)
            {
                suppressInputFieldEvent = true;
                wordInputField.SetTextWithoutNotify(sanitized);
                suppressInputFieldEvent = false;
            }

            currentText = sanitized;
            cursorIndex = Mathf.Clamp(wordInputField.stringPosition, 0, currentText.Length);
            hasUserEdited = currentText != originalIncorrectWord;

            if (keyboardView != null)
            {
                keyboardView.SetFixedReady(hasUserEdited);
            }
        }

        private void HandleInputFieldSubmit(string value)
        {
            if (enterSubmitsFixed)
            {
                OnKeyboardFixed();
            }
        }

        private char ValidateInputFieldCharacter(string text, int charIndex, char addedChar)
        {
            if (blockNativeInputFieldTyping)
            {
                return '\0';
            }

            char lowerChar = char.ToLowerInvariant(addedChar);
            return IsAllowedLetter(lowerChar) ? lowerChar : '\0';
        }

        private bool UseUnityInputFieldCaret()
        {
            return useUnityInputFieldCaret && wordInputField != null && wordText != null;
        }

        private void PushCurrentTextToUI(bool showCaret)
        {
            if (wordInputField == null)
            {
                if (wordText != null)
                {
                    wordText.richText = false;
                    wordText.text = currentText ?? string.Empty;
                }
                return;
            }

            ApplyInputFieldCaretStyle();
            suppressInputFieldEvent = true;
            wordInputField.SetTextWithoutNotify(currentText ?? string.Empty);
            suppressInputFieldEvent = false;
            wordInputField.ForceLabelUpdate();
            SetInputFieldCaret(cursorIndex);

            if (showCaret)
            {
                RefocusWordInputField();
            }
            else
            {
                wordInputField.DeactivateInputField();
            }
        }

        private void SetInputFieldCaret(int targetIndex)
        {
            if (wordInputField == null)
            {
                return;
            }

            ApplyInputFieldCaretStyle();

            int length = currentText != null ? currentText.Length : 0;
            cursorIndex = Mathf.Clamp(targetIndex, 0, length);
            wordInputField.caretPosition = cursorIndex;
            wordInputField.stringPosition = cursorIndex;
            wordInputField.selectionAnchorPosition = cursorIndex;
            wordInputField.selectionFocusPosition = cursorIndex;
            wordInputField.ForceLabelUpdate();
        }

        private bool NeedsInputFieldFocusRepair()
        {
            if (!UseUnityInputFieldCaret() || wordInputField == null)
            {
                return false;
            }

            if (!wordInputField.isFocused)
            {
                return true;
            }

            return EventSystem.current != null && EventSystem.current.currentSelectedGameObject != wordInputField.gameObject;
        }

        private void RefocusWordInputField()
        {
            if (!UseUnityInputFieldCaret() || !CanEdit() || !keepWordInputFocused)
            {
                return;
            }

            SetInputFieldInteractable(true);
            ApplyMobileKeyboardSuppression();
            ApplyInputFieldCaretStyle();

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != wordInputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(wordInputField.gameObject);
            }

            if (!wordInputField.isFocused)
            {
                wordInputField.ActivateInputField();
            }

            SetInputFieldCaret(cursorIndex);
            ApplyMobileKeyboardSuppression();
            RequestCaretVisibilityRefresh();
        }

        private void ApplyInputFieldCaretStyle()
        {
            if (wordInputField == null)
            {
                return;
            }

            inputFieldCaretColor.a = 1f;
            wordInputField.readOnly = false;
            wordInputField.customCaretColor = true;
            wordInputField.caretColor = inputFieldCaretColor;
            wordInputField.caretWidth = Mathf.Max(1, inputFieldCaretWidth);
            wordInputField.selectionColor = inputFieldSelectionColor;
        }

        private void RequestCaretVisibilityRefresh()
        {
            if (!forceVisibleInputFieldCaret || !Application.isPlaying || !UseUnityInputFieldCaret())
            {
                return;
            }

            if (lastCaretVisibilityRefreshFrame == Time.frameCount)
            {
                return;
            }

            lastCaretVisibilityRefreshFrame = Time.frameCount;
            StartCoroutine(ForceCaretVisibleAtEndOfFrame());
        }

        private IEnumerator ForceCaretVisibleAtEndOfFrame()
        {
            yield return null;

            if (!UseUnityInputFieldCaret() || !CanEdit())
            {
                yield break;
            }

            ApplyMobileKeyboardSuppression();
            ApplyInputFieldCaretStyle();

            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != wordInputField.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(wordInputField.gameObject);
            }

            if (!wordInputField.isFocused)
            {
                wordInputField.ActivateInputField();
            }

            SetInputFieldCaret(cursorIndex);
            wordInputField.ForceLabelUpdate();
            ApplyMobileKeyboardSuppression();
        }

        private void SetInputFieldInteractable(bool interactable)
        {
            if (wordInputField != null)
            {
                wordInputField.interactable = interactable;
            }
        }

        private void ApplyMobileKeyboardSuppression()
        {
            if (!preventMobileAndTabletKeyboard || wordInputField == null)
            {
                return;
            }

            wordInputField.shouldHideMobileInput = true;
            TouchScreenKeyboard.hideInput = true;
            TrySetInputFieldBoolProperty(wordInputField, "shouldHideSoftKeyboard", true);
        }

        private static void TrySetInputFieldBoolProperty(TMP_InputField inputField, string propertyName, bool value)
        {
            if (inputField == null)
            {
                return;
            }

            PropertyInfo propertyInfo = typeof(TMP_InputField).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo != null && propertyInfo.CanWrite && propertyInfo.PropertyType == typeof(bool))
            {
                propertyInfo.SetValue(inputField, value, null);
            }
        }

        private int CalculateCaretIndexFromScreenPoint(Vector2 screenPosition, Camera eventCamera)
        {
            string plainText = currentText ?? string.Empty;
            if (plainText.Length == 0 || wordText == null)
            {
                return 0;
            }

            wordText.richText = false;
            wordText.text = plainText;
            wordText.ForceMeshUpdate();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(wordText.rectTransform, screenPosition, eventCamera, out Vector2 localPoint))
            {
                return Mathf.Clamp(cursorIndex, 0, plainText.Length);
            }

            TMP_TextInfo textInfo = wordText.textInfo;
            int firstVisibleIndex = -1;
            int lastVisibleIndex = -1;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                {
                    continue;
                }

                if (firstVisibleIndex < 0)
                {
                    firstVisibleIndex = i;
                }

                lastVisibleIndex = i;
            }

            if (firstVisibleIndex < 0 || lastVisibleIndex < 0)
            {
                return Mathf.Clamp(cursorIndex, 0, plainText.Length);
            }

            TMP_CharacterInfo firstChar = textInfo.characterInfo[firstVisibleIndex];
            TMP_CharacterInfo lastChar = textInfo.characterInfo[lastVisibleIndex];
            float pointerX = localPoint.x;

            if (pointerX <= firstChar.bottomLeft.x)
            {
                return 0;
            }

            if (pointerX >= lastChar.topRight.x)
            {
                return plainText.Length;
            }

            for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible)
                {
                    continue;
                }

                float middleX = (charInfo.bottomLeft.x + charInfo.topRight.x) * 0.5f;
                int sourceIndex = Mathf.Clamp(charInfo.index, 0, plainText.Length);

                if (pointerX < middleX)
                {
                    return sourceIndex;
                }

                if (pointerX <= charInfo.topRight.x)
                {
                    return Mathf.Clamp(sourceIndex + 1, 0, plainText.Length);
                }
            }

            return plainText.Length;
        }

        private void RefreshLegacyWordDisplay(bool showCursor)
        {
            if (wordText == null)
            {
                return;
            }

            string visibleText = currentText ?? string.Empty;
            cursorIndex = Mathf.Clamp(cursorIndex, 0, visibleText.Length);

            if (UseExternalVisualCaret())
            {
                wordText.richText = false;
                wordText.text = visibleText;
                wordText.ForceMeshUpdate();
                UpdateVisualCaret(showCursor && cursorVisible && CurrentState == SpellBotGameState.Editing && !inputLocked);
                return;
            }

            SetVisualCaretActive(false);
            wordText.richText = true;

            if (showCursor && CurrentState == SpellBotGameState.Editing)
            {
                string cursor = cursorVisible ? "<color=#1B263B>|</color>" : "<color=#1B263B00>|</color>";
                visibleText = visibleText.Insert(cursorIndex, cursor);
            }

            wordText.text = visibleText;
        }

        private void PrepareVisualCaret()
        {
            if (visualCaret == null)
            {
                return;
            }

            visualCaret.pivot = new Vector2(0.5f, 0.5f);
            visualCaret.anchorMin = new Vector2(0.5f, 0.5f);
            visualCaret.anchorMax = new Vector2(0.5f, 0.5f);
            visualCaret.SetAsLastSibling();

            Graphic caretGraphic = visualCaret.GetComponent<Graphic>();
            if (caretGraphic != null)
            {
                caretGraphic.color = caretColor;
                caretGraphic.raycastTarget = false;
            }

            SetVisualCaretActive(false);
        }

        private bool UseExternalVisualCaret()
        {
            return !UseUnityInputFieldCaret() && useVisualCaret && visualCaret != null;
        }

        private void SetVisualCaretActive(bool active)
        {
            if (visualCaret != null && visualCaret.gameObject.activeSelf != active)
            {
                visualCaret.gameObject.SetActive(active);
            }
        }

        private void UpdateVisualCaret(bool show)
        {
            if (!UseExternalVisualCaret())
            {
                return;
            }

            if (!show)
            {
                SetVisualCaretActive(false);
                return;
            }

            if (!TryGetCaretLocalPosition(out Vector2 textLocalPoint, out float caretHeight))
            {
                SetVisualCaretActive(false);
                return;
            }

            RectTransform parentRect = visualCaret.parent as RectTransform;
            if (parentRect == null)
            {
                SetVisualCaretActive(false);
                return;
            }

            Vector3 worldPoint = wordText.rectTransform.TransformPoint(new Vector3(textLocalPoint.x, textLocalPoint.y, 0f));
            Vector3 parentLocalPoint = parentRect.InverseTransformPoint(worldPoint);

            visualCaret.anchoredPosition = new Vector2(parentLocalPoint.x, parentLocalPoint.y);
            visualCaret.sizeDelta = new Vector2(caretWidth, caretHeight);
            visualCaret.SetAsLastSibling();
            SetVisualCaretActive(true);
        }

        private bool TryGetCaretLocalPosition(out Vector2 localPoint, out float caretHeight)
        {
            localPoint = Vector2.zero;
            caretHeight = Mathf.Max(36f, wordText.fontSize * 1.15f);

            string plainText = currentText ?? string.Empty;
            wordText.ForceMeshUpdate();

            TMP_TextInfo textInfo = wordText.textInfo;
            if (plainText.Length == 0 || textInfo.characterCount == 0)
            {
                return true;
            }

            int firstVisibleIndex = -1;
            int lastVisibleIndex = -1;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                {
                    continue;
                }

                if (firstVisibleIndex < 0)
                {
                    firstVisibleIndex = i;
                }

                lastVisibleIndex = i;
            }

            if (firstVisibleIndex < 0 || lastVisibleIndex < 0)
            {
                return true;
            }

            TMP_CharacterInfo firstChar = textInfo.characterInfo[firstVisibleIndex];
            TMP_CharacterInfo lastChar = textInfo.characterInfo[lastVisibleIndex];
            float xPosition;

            if (cursorIndex <= 0)
            {
                xPosition = firstChar.bottomLeft.x;
            }
            else if (cursorIndex >= plainText.Length)
            {
                xPosition = lastChar.topRight.x;
            }
            else
            {
                xPosition = lastChar.topRight.x;

                for (int i = firstVisibleIndex; i <= lastVisibleIndex; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible)
                    {
                        continue;
                    }

                    if (charInfo.index >= cursorIndex)
                    {
                        xPosition = charInfo.bottomLeft.x;
                        break;
                    }
                }
            }

            caretHeight = Mathf.Max(36f, Mathf.Abs(firstChar.topRight.y - firstChar.bottomLeft.y) * 1.08f);
            float yPosition = (firstChar.bottomLeft.y + firstChar.topRight.y) * 0.5f;
            localPoint = new Vector2(xPosition, yPosition);
            return true;
        }

        private void SetWordTextColor(Color color)
        {
            if (wordText != null)
            {
                wordText.color = color;
            }

            if (wordInputField != null && wordInputField.textComponent != null)
            {
                wordInputField.textComponent.color = color;
            }
        }

        private bool CanEdit()
        {
            return !inputLocked && CurrentState == SpellBotGameState.Editing;
        }

        private void SetInputLocked(bool locked)
        {
            inputLocked = locked;

            if (locked)
            {
                SetVisualCaretActive(false);
                if (wordInputField != null)
                {
                    wordInputField.DeactivateInputField();
                    SetInputFieldInteractable(false);
                }
            }
            else
            {
                SetInputFieldInteractable(true);
            }

            if (keyboardView != null)
            {
                keyboardView.SetInputLocked(locked);
            }

            UpdateHintAnswerUI();
        }

        private void RevealHintForCurrentRound(bool userClickedHintButton)
        {
            if (currentEntry == null)
            {
                return;
            }

            if (!hintUsedThisRound)
            {
                hintUses++;
            }

            hintUsedThisRound = true;
            UpdateHintAnswerUI();

            string message;
            if (answerShownThisRound)
            {
                message = showAnswerPrefix + currentEntry.correctWord;
            }
            else
            {
                string text = string.IsNullOrWhiteSpace(currentEntry.hintText) ? "Look carefully at the spelling." : currentEntry.hintText;
                message = hintPrefix + text;
            }

            if (feedback != null)
            {
                feedback.ShowHint(message, hintText);

                if (!answerShownThisRound)
                {
                    feedback.PlayShowAnswerAvailable(showAnswerButton != null ? showAnswerButton.transform : null);
                }
            }
            else if (hintText != null)
            {
                hintText.text = message;
            }
        }

        private void UpdateHintAnswerUI()
        {
            bool canUseHint = CanEdit() && currentEntry != null;

            if (hintButton != null)
            {
                hintButton.interactable = canUseHint;
            }

            if (showAnswerButton != null)
            {
                bool showAnswer = allowShowAnswerAfterHint && hintUsedThisRound && !answerShownThisRound && currentEntry != null && CurrentState != SpellBotGameState.Result;
                if (showAnswerButton.gameObject.activeSelf != showAnswer)
                {
                    showAnswerButton.gameObject.SetActive(showAnswer);
                }

                showAnswerButton.interactable = showAnswer && CanEdit();
            }
        }

        private void ApplyConfiguredFonts()
        {
            if (!applyFontsOnAwake || (primaryFont == null && secondaryFont == null))
            {
                return;
            }

            Transform root = fontApplyRoot != null ? fontApplyRoot : transform.root;
            if (root == null)
            {
                return;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null)
                {
                    continue;
                }

                bool useSecondary = ShouldUseSecondaryFont(text);
                TMP_FontAsset targetFont = useSecondary ? secondaryFont : primaryFont;

                if (targetFont == null)
                {
                    targetFont = useSecondary ? primaryFont : secondaryFont;
                }

                if (targetFont != null)
                {
                    text.font = targetFont;
                }
            }
        }

        private bool ShouldUseSecondaryFont(TMP_Text text)
        {
            string lowerName = text.name.ToLowerInvariant();
            return lowerName.Contains("word") ||
                   lowerName.Contains("hint") ||
                   lowerName.Contains("body") ||
                   lowerName.Contains("key") ||
                   lowerName.Contains("keyboard");
        }

        private void UpdateTopUI()
        {
            if (roundText != null)
            {
                int displayRound = Mathf.Clamp(currentRoundIndex + 1, 1, Mathf.Max(1, activePlaylist.Count));
                int totalRounds = activePlaylist.Count > 0 ? activePlaylist.Count : roundsPerSession;
                roundText.text = $"Round {displayRound} of {totalRounds}";
            }

            if (progressSlider != null)
            {
                int totalRoundsForSlider = activePlaylist.Count > 0 ? activePlaylist.Count : Mathf.Max(1, roundsPerSession);
                progressSlider.minValue = 0f;
                progressSlider.maxValue = totalRoundsForSlider;
                progressSlider.wholeNumbers = false;
                float completedRounds = CurrentState == SpellBotGameState.Result ? totalRoundsForSlider : Mathf.Clamp(currentRoundIndex, 0, totalRoundsForSlider);
                progressSlider.value = completedRounds;
            }

            if (streakLabelText != null)
            {
                streakLabelText.text = $"{streakLabelPrefix}: {streak}/{Mathf.Max(1, streakNeededForOverdrive)}";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }

            if (streakStars != null)
            {
                for (int i = 0; i < streakStars.Length; i++)
                {
                    if (streakStars[i] != null)
                    {
                        if (starSprite != null)
                        {
                            streakStars[i].sprite = starSprite;
                            streakStars[i].preserveAspect = true;
                        }

                        streakStars[i].color = i < streak ? streakFilledColor : streakEmptyColor;
                    }
                }
            }

            // Overdrive is now represented by the robot sprite, not a UI glow/label.
            SetPanel(overdriveLabelRoot, false);
        }

        private void ApplyOverdriveRobotVisual(bool active, bool animate)
        {
            if (robotView != null)
            {
                robotView.SetOverdriveActive(active, animate);
            }

            // Keep old assigned glow objects disabled without requiring scene/layout rework.
            if (feedback != null)
            {
                feedback.SetOverdriveGlow(false);
            }
        }

        private void SetPanel(GameObject panel, bool show)
        {
            if (panel != null)
            {
                panel.SetActive(show);
            }
        }

        private string SanitizeWord(string word)
        {
            return string.IsNullOrWhiteSpace(word) ? string.Empty : SanitizeInputLettersOnly(word.Trim().ToLowerInvariant());
        }

        private string Normalize(string word)
        {
            return string.IsNullOrWhiteSpace(word) ? string.Empty : SanitizeInputLettersOnly(word.Trim().ToLowerInvariant());
        }

        private string SanitizeInputLettersOnly(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int count = 0;

            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                if (IsAllowedLetter(c))
                {
                    buffer[count] = c;
                    count++;
                }
            }

            return new string(buffer, 0, count);
        }

        private bool IsAllowedLetter(char typedChar)
        {
            return typedChar >= 'a' && typedChar <= 'z';
        }

        private void PlayClip(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
