using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryGameController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private MemoryBoardController boardController;
        [SerializeField] private MemoryGridLayoutFitter gridLayoutFitter;
        [SerializeField] private MemoryLearningPopupView learningPopupView;
        [SerializeField] private MemoryAudioNarrationManager audioNarrationManager;
        [SerializeField] private MemoryThemeApplier themeApplier;
        [SerializeField] private MemoryCountdownTimer countdownTimer;
        [SerializeField] private MemoryTimerUIView timerUIView;
        [SerializeField] private MemoryPauseController pauseController;
        [SerializeField] private MemoryHintUIView hintUIView;
        [SerializeField] private MemoryScoreUIView scoreUIView;
        [SerializeField] private MemorySfxAudioManager sfxAudioManager;
        [SerializeField] private MemorySummaryOverlayView summaryOverlayView;
        [SerializeField] private MemoryHowToPlayOverlayView howToPlayOverlayView;

        [Header("Config")]
        [SerializeField] private MemoryActivityConfig activityConfig;
        [SerializeField] private bool startOnPlay = true;
        [SerializeField] private bool applyGridFromConfig = true;
        [SerializeField] private bool applyCardAspectRatioFromConfig = true;
        [SerializeField] private bool applyThemeFromConfig = true;
        [SerializeField] private bool applyDifficultyTimingFromConfig = true;

        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text debugStatusText;

        [Header("Phase 3 Match Timing - Fallback If No Difficulty Config")]
        [SerializeField, Min(0f)] private float matchCheckDelay = 0.25f;
        [SerializeField, Min(0f)] private float wrongFlipBackDelay = 0.65f;

        [Header("Phase 6 Animation Timing")]
        [Tooltip("Small delay after correct pulse before the learning popup opens.")]
        [SerializeField, Min(0f)] private float correctFeedbackBeforePopupDelay = 0.25f;

        [Header("Phase 8.5 Score Feedback Timing")]
        [Tooltip("Delay after correct scoring feedback before the learning popup opens. Use this so +score feedback finishes before the educational popup appears.")]
        [SerializeField, Min(0f)] private float scoreFeedbackBeforePopupDelay = 0.75f;

        [Header("Phase 9 Summary Timing")]
        [Tooltip("Small delay before the summarization overlay appears.")]
        [SerializeField, Min(0f)] private float summaryOverlayDelay = 0.35f;

        [Header("Phase 4 Learning Popup")]
        [SerializeField] private bool showLearningPopupOnCorrectMatch = true;
        [SerializeField] private bool stopNarrationWhenPopupContinues = true;

        [Header("Phase 4.1 Popup Auto Continue - Fallback If No Difficulty Config")]
        [SerializeField] private bool enablePopupAutoContinue = true;
        [SerializeField, Min(0f)] private float delayAfterNarrationBeforeAutoContinue = 1.25f;
        [SerializeField, Min(0f)] private float noAudioAutoContinueDelay = 2.5f;

        [Header("Phase 7 Pause Rules")]
        [SerializeField] private bool pauseNarrationOnGamePause = true;
        [SerializeField] private bool disableLearningPopupButtonsDuringPause = true;

        private readonly Dictionary<string, MemoryPairDefinition> pairLookup =
            new Dictionary<string, MemoryPairDefinition>();

        private List<MemoryCardView> activeCards = new List<MemoryCardView>();
        private List<MemoryPairDefinition> currentPlayablePairs = new List<MemoryPairDefinition>();

        private MemoryActivityConfig currentActivityConfig;
        private MemoryDifficultyConfig currentDifficultyConfig;
        private MemoryCardView firstSelectedCard;
        private MemoryCardView secondSelectedCard;
        private Coroutine evaluateSelectionRoutine;
        private Coroutine hintRevealRoutine;
        private Coroutine summaryOverlayRoutine;

        private bool inputLocked;
        private bool waitingForPopupContinue;
        private bool activityCompleted;
        private bool timerExpired;
        private bool gamePaused;
        private bool hintActive;

        private int totalPairs;
        private int matchedPairs;
        private int wrongAttempts;
        private int hintsUsed;
        private int maxHints;
        private int currentScore;
        private int cardClicks;
        private int pairAttempts;
        private bool waitingForHowToPlay;
        private bool howToOpenedFromGameplay;

        private void Awake()
        {
            if (pauseController != null)
            {
                pauseController.Initialize(PauseGame, ResumeGame);
            }

            if (hintUIView != null)
            {
                hintUIView.Initialize(TryUseHint);
            }

            if (howToPlayOverlayView != null)
            {
                howToPlayOverlayView.Initialize(HandleHowToPlayRequested, HandleHowToPlayClosed);
            }
        }

        private void OnEnable()
        {
            SubscribeTimerEvents();
        }

        private void OnDisable()
        {
            UnsubscribeTimerEvents();
        }

        private void Start()
        {
            if (startOnPlay && activityConfig != null)
            {
                StartActivity(activityConfig);
            }
        }

        public void StartActivity(MemoryActivityConfig config)
        {
            if (config == null)
            {
                Debug.LogError("Cannot start Memory Match. Activity Config is null.", this);
                return;
            }

            if (boardController == null)
            {
                Debug.LogError("Cannot start Memory Match. Board Controller is missing.", this);
                return;
            }

            if (evaluateSelectionRoutine != null)
            {
                StopCoroutine(evaluateSelectionRoutine);
                evaluateSelectionRoutine = null;
            }

            if (hintRevealRoutine != null)
            {
                StopCoroutine(hintRevealRoutine);
                hintRevealRoutine = null;
            }

            if (summaryOverlayRoutine != null)
            {
                StopCoroutine(summaryOverlayRoutine);
                summaryOverlayRoutine = null;
            }

            currentActivityConfig = config;
            currentDifficultyConfig = config.DifficultyConfig;
            firstSelectedCard = null;
            secondSelectedCard = null;
            matchedPairs = 0;
            wrongAttempts = 0;
            hintsUsed = 0;
            maxHints = currentDifficultyConfig != null ? currentDifficultyConfig.MaxHints : 0;
            currentScore = currentDifficultyConfig != null && currentDifficultyConfig.ScoringEnabled
                ? currentDifficultyConfig.StartingScore
                : 0;
            cardClicks = 0;
            pairAttempts = 0;

            inputLocked = false;
            waitingForPopupContinue = false;
            activityCompleted = false;
            timerExpired = false;
            gamePaused = false;
            hintActive = false;
            waitingForHowToPlay = false;
            howToOpenedFromGameplay = false;
            pairLookup.Clear();

            ApplyConfigToScene(config);

            int gridCapacity = config.GetEffectiveGridSlotCapacity();
            currentPlayablePairs = config.GetPlayablePairs(gridCapacity);

            if (currentPlayablePairs.Count <= 0)
            {
                Debug.LogError(
                    $"Activity '{config.name}' does not have enough cards to start Memory Match.",
                    config);
                UpdateDebugStatus("No playable pairs found.");
                return;
            }

            totalPairs = currentPlayablePairs.Count;

            List<MemoryCardRuntimeData> runtimeCards = new List<MemoryCardRuntimeData>();

            for (int i = 0; i < currentPlayablePairs.Count; i++)
            {
                MemoryPairDefinition pair = currentPlayablePairs[i];

                if (pair == null || !pair.IsValid())
                {
                    continue;
                }

                pairLookup[pair.PairId] = pair;
                runtimeCards.Add(pair.CreateCardA());
                runtimeCards.Add(pair.CreateCardB());
            }

            if (runtimeCards.Count < 2)
            {
                Debug.LogError(
                    $"Activity '{config.name}' produced fewer than 2 runtime cards.",
                    config);
                UpdateDebugStatus("Not enough runtime cards.");
                return;
            }

            UpdateHeader(config);
            activeCards = boardController.BuildBoard(runtimeCards, HandleCardClicked, config.ThemeConfig);
            ConfigureTimer(config);
            ConfigurePause(config);
            ConfigureHints(config);
            ConfigureScore(config);
            ConfigureAudio(config);
            ConfigureSummaryOverlay(config);
            ConfigureHowToPlay(config);

            if (howToPlayOverlayView != null && howToPlayOverlayView.ShowOnActivityStart)
            {
                waitingForHowToPlay = true;
                howToOpenedFromGameplay = false;
                SetInputLocked(true);
                RefreshHintUIInteractivity();
                sfxAudioManager?.PlayPopupOpen();
                howToPlayOverlayView.Show();
                UpdateDebugStatus("How to play.");
            }
            else
            {
                StartGameplayAfterIntro();
            }
        }

        private void ApplyConfigToScene(MemoryActivityConfig config)
        {
            MemoryDifficultyConfig difficulty = config.DifficultyConfig;

            if (gridLayoutFitter != null)
            {
                if (applyGridFromConfig)
                {
                    gridLayoutFitter.SetGrid(config.GetEffectiveGridColumns(), config.GetEffectiveGridRows());
                }

                if (applyCardAspectRatioFromConfig)
                {
                    gridLayoutFitter.SetCardAspectRatio(config.GetEffectiveCardAspectRatio());
                }
            }

            if (applyThemeFromConfig)
            {
                if (themeApplier != null)
                {
                    themeApplier.ApplyTheme(config.ThemeConfig);
                }

                if (learningPopupView != null)
                {
                    learningPopupView.ApplyTheme(config.ThemeConfig);
                }
            }

            if (applyDifficultyTimingFromConfig && difficulty != null)
            {
                matchCheckDelay = difficulty.MatchCheckDelay;
                wrongFlipBackDelay = difficulty.WrongFlipBackDelay;
                enablePopupAutoContinue = difficulty.EnablePopupAutoContinue;
                delayAfterNarrationBeforeAutoContinue = difficulty.DelayAfterNarrationBeforeAutoContinue;
                noAudioAutoContinueDelay = difficulty.NoAudioAutoContinueDelay;
            }
        }

        private void ConfigureTimer(MemoryActivityConfig config)
        {
            MemoryDifficultyConfig difficulty = config.DifficultyConfig;
            bool enabled = difficulty != null && difficulty.TimerEnabled;

            if (timerUIView != null)
            {
                timerUIView.ApplyTheme(config.ThemeConfig);
                timerUIView.Configure(difficulty);
                timerUIView.SetTimerVisible(enabled);
                timerUIView.StopAllFeedback();
            }

            if (countdownTimer != null)
            {
                float duration = difficulty != null ? difficulty.CountdownSeconds : 120f;
                float warning = difficulty != null ? difficulty.WarningRemainingPercent : 0.15f;
                countdownTimer.Configure(enabled, duration, warning);
            }
        }

        private void ConfigurePause(MemoryActivityConfig config)
        {
            if (pauseController == null)
            {
                return;
            }

            pauseController.ApplyTheme(config.ThemeConfig);
            pauseController.SetPauseButtonVisible(true);
            pauseController.SetPauseButtonInteractable(true);
            pauseController.HideImmediate();
        }

        private void ConfigureHints(MemoryActivityConfig config)
        {
            MemoryDifficultyConfig difficulty = config.DifficultyConfig;
            maxHints = difficulty != null ? difficulty.MaxHints : 0;

            if (hintUIView == null)
            {
                return;
            }

            hintUIView.ApplyTheme(config.ThemeConfig);
            hintUIView.Configure(difficulty);
            hintUIView.UpdateHintsRemaining(hintsUsed, maxHints);
            RefreshHintUIInteractivity();
        }

        private void ConfigureScore(MemoryActivityConfig config)
        {
            if (scoreUIView == null)
            {
                return;
            }

            scoreUIView.ApplyTheme(config.ThemeConfig);
            scoreUIView.Configure(config.DifficultyConfig);
            scoreUIView.SetScore(currentScore);
        }

        private void ConfigureAudio(MemoryActivityConfig config)
        {
            MemoryAudioConfig audioConfig = config != null && config.ThemeConfig != null
                ? config.ThemeConfig.AudioConfig
                : null;

            if (sfxAudioManager != null)
            {
                sfxAudioManager.Configure(audioConfig);
            }

            if (learningPopupView != null)
            {
                learningPopupView.SetSfxAudioManager(sfxAudioManager);
            }

            if (howToPlayOverlayView != null)
            {
                howToPlayOverlayView.SetSfxAudioManager(sfxAudioManager);
            }
        }

        private void ConfigureSummaryOverlay(MemoryActivityConfig config)
        {
            if (summaryOverlayView == null)
            {
                return;
            }

            summaryOverlayView.ApplyTheme(config.ThemeConfig);
            summaryOverlayView.HideImmediate();
        }

        private void ConfigureHowToPlay(MemoryActivityConfig config)
        {
            if (howToPlayOverlayView == null)
            {
                return;
            }

            howToPlayOverlayView.SetSfxAudioManager(sfxAudioManager);
            howToPlayOverlayView.ApplyTheme(config.ThemeConfig);
            howToPlayOverlayView.HideImmediate();
            howToPlayOverlayView.SetButtonVisible(true);
        }

        private void StartGameplayAfterIntro()
        {
            waitingForHowToPlay = false;
            howToOpenedFromGameplay = false;

            SetInputLocked(false);
            RefreshHintUIInteractivity();

            sfxAudioManager?.PlayActivityStart();
            sfxAudioManager?.StartBackgroundLoop();

            if (countdownTimer != null && countdownTimer.TimerEnabled)
            {
                countdownTimer.StartTimer();
            }

            UpdateDebugStatus("Activity started.");
        }

        private void UpdateHeader(MemoryActivityConfig config)
        {
            if (titleText != null)
            {
                titleText.text = config.ActivityTitle;
            }

            if (instructionText != null)
            {
                instructionText.text = config.InstructionText;
            }
        }

        private void HandleCardClicked(MemoryCardView clickedCard)
        {
            if (inputLocked || hintActive || gamePaused || timerExpired || activityCompleted || clickedCard == null || clickedCard.IsMatched)
            {
                return;
            }

            if (clickedCard == firstSelectedCard)
            {
                return;
            }

            cardClicks++;
            sfxAudioManager?.PlayCardFlip();

            if (firstSelectedCard == null)
            {
                firstSelectedCard = clickedCard;
                firstSelectedCard.FlipUp();
                firstSelectedCard.SetSelected(true);
                RefreshHintUIInteractivity();
                UpdateDebugStatus("First card selected.");
                return;
            }

            secondSelectedCard = clickedCard;
            pairAttempts++;
            secondSelectedCard.FlipUp();
            secondSelectedCard.SetSelected(true);
            SetInputLocked(true);
            RefreshHintUIInteractivity();

            evaluateSelectionRoutine = StartCoroutine(EvaluateSelectedCardsRoutine());
        }

        private IEnumerator EvaluateSelectedCardsRoutine()
        {
            yield return new WaitForSeconds(matchCheckDelay);

            yield return WaitWhilePausedForFlow();

            if (ShouldStopFlow())
            {
                evaluateSelectionRoutine = null;
                yield break;
            }

            bool isCorrect = MemoryMatchValidator.IsCorrectMatch(firstSelectedCard, secondSelectedCard);

            if (isCorrect)
            {
                yield return HandleCorrectMatchRoutine();
            }
            else
            {
                yield return HandleWrongMatchRoutine();
            }

            evaluateSelectionRoutine = null;
        }

        private IEnumerator HandleCorrectMatchRoutine()
        {
            MemoryCardView first = firstSelectedCard;
            MemoryCardView second = secondSelectedCard;

            ClearCurrentSelectionReferences();

            if (first != null)
            {
                first.SetMatched(true);
                first.PlayCorrectFeedback();
            }

            if (second != null)
            {
                second.SetMatched(true);
                second.PlayCorrectFeedback();
            }

            matchedPairs++;
            sfxAudioManager?.PlayCorrectMatch();
            AddScore(currentDifficultyConfig != null ? currentDifficultyConfig.ScorePerCorrectMatch : 0, true);

            float feedbackDelay = Mathf.Max(correctFeedbackBeforePopupDelay, scoreFeedbackBeforePopupDelay);

            if (feedbackDelay > 0f)
            {
                yield return new WaitForSeconds(feedbackDelay);
            }

            yield return WaitWhilePausedForFlow();

            if (ShouldStopFlow())
            {
                yield break;
            }

            MemoryPairDefinition pair = null;
            if (first != null && first.Data != null)
            {
                pairLookup.TryGetValue(first.Data.PairId, out pair);
            }

            UpdateDebugStatus("Correct match.");

            if (showLearningPopupOnCorrectMatch && learningPopupView != null && pair != null)
            {
                waitingForPopupContinue = true;
                RefreshHintUIInteractivity();

                bool pauseTimerForPopup =
                    currentDifficultyConfig == null || currentDifficultyConfig.PauseTimerDuringLearningPopup;

                if (pauseTimerForPopup)
                {
                    PauseTimerOnly();
                }

                float narrationDuration = 0f;
                if (audioNarrationManager != null)
                {
                    narrationDuration = audioNarrationManager.Play(pair.NarrationAudio);
                }
                else if (pair.NarrationAudio != null)
                {
                    narrationDuration = pair.NarrationAudio.length;
                }

                sfxAudioManager?.PlayPopupOpen();
                sfxAudioManager?.SetBackgroundDucked(true);

                learningPopupView.Show(
                    pair,
                    ReplayCurrentNarration,
                    ContinueFromLearningPopup,
                    enablePopupAutoContinue,
                    narrationDuration,
                    delayAfterNarrationBeforeAutoContinue,
                    noAudioAutoContinueDelay);

                while (waitingForPopupContinue)
                {
                    yield return null;
                }

                if (pauseTimerForPopup && !gamePaused && !activityCompleted && !timerExpired)
                {
                    ResumeTimerOnly();
                }
            }

            if (matchedPairs >= totalPairs)
            {
                CompleteActivity();
            }
            else
            {
                SetInputLocked(false);
                RefreshHintUIInteractivity();
                UpdateDebugStatus("Continue matching.");
            }
        }

        private IEnumerator HandleWrongMatchRoutine()
        {
            wrongAttempts++;
            sfxAudioManager?.PlayWrongMatch();
            AddScore(-(currentDifficultyConfig != null ? currentDifficultyConfig.WrongMatchPenalty : 0), false);
            UpdateDebugStatus("Wrong match.");

            if (firstSelectedCard != null)
            {
                firstSelectedCard.PlayWrongFeedback();
            }

            if (secondSelectedCard != null)
            {
                secondSelectedCard.PlayWrongFeedback();
            }

            yield return new WaitForSeconds(wrongFlipBackDelay);

            yield return WaitWhilePausedForFlow();

            if (ShouldStopFlow())
            {
                yield break;
            }

            if (firstSelectedCard != null)
            {
                firstSelectedCard.FlipDown();
            }

            if (secondSelectedCard != null)
            {
                secondSelectedCard.FlipDown();
            }

            ClearCurrentSelectionReferences();

            if (!gamePaused && !timerExpired && !activityCompleted)
            {
                SetInputLocked(false);
            }

            RefreshHintUIInteractivity();
            UpdateDebugStatus("Try again.");
        }

        private void TryUseHint()
        {
            if (!CanUseHint())
            {
                return;
            }

            HintPair hintPair = FindHintPair();

            if (!hintPair.IsValid)
            {
                RefreshHintUIInteractivity();
                return;
            }

            sfxAudioManager?.PlayButtonClick();
            hintRevealRoutine = StartCoroutine(HintRevealRoutine(hintPair.First, hintPair.Second));
        }

        private bool CanUseHint()
        {
            if (currentDifficultyConfig == null || !currentDifficultyConfig.HintsEnabled)
            {
                return false;
            }

            if (hintsUsed >= maxHints || maxHints <= 0)
            {
                return false;
            }

            if (hintActive || inputLocked || gamePaused || timerExpired || activityCompleted || waitingForPopupContinue || waitingForHowToPlay)
            {
                return false;
            }

            if (firstSelectedCard != null || secondSelectedCard != null)
            {
                return false;
            }

            return FindHintPair().IsValid;
        }

        private IEnumerator HintRevealRoutine(MemoryCardView first, MemoryCardView second)
        {
            hintActive = true;
            hintsUsed++;
            sfxAudioManager?.PlayHintUsed();
            AddScore(-(currentDifficultyConfig != null ? currentDifficultyConfig.HintPenalty : 0), false);
            RefreshHintUIInteractivity();
            SetInputLocked(true);

            bool pauseTimerForHint =
                currentDifficultyConfig != null && currentDifficultyConfig.PauseTimerDuringHintReveal;

            if (pauseTimerForHint)
            {
                PauseTimerOnly();
            }

            first.FlipUp();
            second.FlipUp();

            first.SetHinted(true);
            second.SetHinted(true);

            first.PlayHintFeedback();
            second.PlayHintFeedback();

            float duration = currentDifficultyConfig != null ? currentDifficultyConfig.HintRevealDuration : 1.5f;
            yield return new WaitForSeconds(duration);

            first.StopHintFeedback();
            second.StopHintFeedback();

            first.SetHinted(false);
            second.SetHinted(false);

            if (!first.IsMatched)
            {
                first.FlipDown();
            }

            if (!second.IsMatched)
            {
                second.FlipDown();
            }

            if (pauseTimerForHint && !gamePaused && !activityCompleted && !timerExpired)
            {
                ResumeTimerOnly();
            }

            hintActive = false;
            hintRevealRoutine = null;

            if (!gamePaused && !timerExpired && !activityCompleted && !waitingForPopupContinue)
            {
                SetInputLocked(false);
            }

            RefreshHintUIInteractivity();
            UpdateDebugStatus("Hint used.");
        }

        private HintPair FindHintPair()
        {
            if (activeCards == null || activeCards.Count <= 0)
            {
                return default;
            }

            Dictionary<string, MemoryCardView> firstByPairId = new Dictionary<string, MemoryCardView>();

            for (int i = 0; i < activeCards.Count; i++)
            {
                MemoryCardView card = activeCards[i];

                if (card == null || card.IsMatched || card.Data == null || string.IsNullOrWhiteSpace(card.Data.PairId))
                {
                    continue;
                }

                if (firstByPairId.TryGetValue(card.Data.PairId, out MemoryCardView firstCard))
                {
                    if (firstCard != null && firstCard != card && !firstCard.IsMatched)
                    {
                        return new HintPair(firstCard, card);
                    }
                }
                else
                {
                    firstByPairId.Add(card.Data.PairId, card);
                }
            }

            return default;
        }

        private readonly struct HintPair
        {
            public readonly MemoryCardView First;
            public readonly MemoryCardView Second;
            public bool IsValid => First != null && Second != null;

            public HintPair(MemoryCardView first, MemoryCardView second)
            {
                First = first;
                Second = second;
            }
        }

        private void AddScore(int delta, bool playPositiveParticle)
        {
            if (currentDifficultyConfig == null || !currentDifficultyConfig.ScoringEnabled || delta == 0)
            {
                return;
            }

            currentScore += delta;

            if (currentDifficultyConfig.ClampScoreAtZero)
            {
                currentScore = Mathf.Max(0, currentScore);
            }

            if (delta > 0)
            {
                sfxAudioManager?.PlayScorePositive();
            }
            else if (delta < 0)
            {
                sfxAudioManager?.PlayScoreNegative();
            }

            if (scoreUIView != null)
            {
                scoreUIView.SetScore(currentScore);
                scoreUIView.ShowScoreDelta(delta);

                if (delta > 0 && playPositiveParticle)
                {
                    scoreUIView.PlayCorrectParticle();
                }
            }
        }

        private IEnumerator WaitWhilePausedForFlow()
        {
            while (gamePaused || waitingForHowToPlay)
            {
                yield return null;
            }
        }

        private bool ShouldStopFlow()
        {
            return activityCompleted || timerExpired;
        }

        private void PauseGame()
        {
            if (gamePaused || activityCompleted || timerExpired)
            {
                return;
            }

            gamePaused = true;
            sfxAudioManager?.PlayButtonClick();
            sfxAudioManager?.PlayPause();
            sfxAudioManager?.PauseBackgroundLoop();
            SetInputLocked(true);
            PauseTimerOnly();

            if (pauseNarrationOnGamePause && audioNarrationManager != null)
            {
                audioNarrationManager.PauseForGamePause();
            }

            if (learningPopupView != null)
            {
                learningPopupView.SetAutoContinuePaused(true);

                if (disableLearningPopupButtonsDuringPause)
                {
                    learningPopupView.SetInteractionEnabled(false);
                }
            }

            if (pauseController != null && currentActivityConfig != null)
            {
                string body = currentActivityConfig.GetPauseBodyText(currentPlayablePairs);
                pauseController.ShowOverlay(currentActivityConfig.PauseTitle, body);
                pauseController.SetPauseButtonInteractable(false);
            }

            RefreshHintUIInteractivity();
            UpdateDebugStatus("Paused.");
        }

        private void ResumeGame()
        {
            if (!gamePaused)
            {
                return;
            }

            gamePaused = false;
            sfxAudioManager?.PlayButtonClick();
            sfxAudioManager?.PlayResume();
            sfxAudioManager?.ResumeBackgroundLoop();

            if (pauseController != null)
            {
                pauseController.HideOverlay();
                pauseController.SetPauseButtonInteractable(true);
            }

            if (pauseNarrationOnGamePause && audioNarrationManager != null)
            {
                audioNarrationManager.ResumeFromGamePause();
            }

            if (learningPopupView != null)
            {
                learningPopupView.SetAutoContinuePaused(false);
                learningPopupView.SetInteractionEnabled(true);
            }

            bool shouldKeepTimerPaused =
                waitingForPopupContinue &&
                (currentDifficultyConfig == null || currentDifficultyConfig.PauseTimerDuringLearningPopup);

            if (!shouldKeepTimerPaused)
            {
                ResumeTimerOnly();
            }

            if (!waitingForPopupContinue &&
                !timerExpired &&
                !activityCompleted &&
                !hintActive &&
                !waitingForHowToPlay &&
                evaluateSelectionRoutine == null)
            {
                SetInputLocked(false);
            }

            RefreshHintUIInteractivity();
            UpdateDebugStatus("Resumed.");
        }

        private void PauseTimerOnly()
        {
            if (countdownTimer != null)
            {
                countdownTimer.PauseTimer();
            }
        }

        private void ResumeTimerOnly()
        {
            if (countdownTimer != null)
            {
                countdownTimer.ResumeTimer();
            }
        }

        private void ReplayCurrentNarration()
        {
            sfxAudioManager?.PlayButtonClick();

            if (audioNarrationManager != null)
            {
                audioNarrationManager.ReplayCurrent();
            }
        }

        private void ContinueFromLearningPopup()
        {
            if (stopNarrationWhenPopupContinues && audioNarrationManager != null)
            {
                audioNarrationManager.Stop();
            }

            if (learningPopupView != null)
            {
                learningPopupView.Hide();
            }

            sfxAudioManager?.SetBackgroundDucked(false);
            waitingForPopupContinue = false;
            RefreshHintUIInteractivity();
        }

        private void HandleTimerChanged(float remainingSeconds, float normalizedRemaining)
        {
            if (timerUIView != null)
            {
                timerUIView.UpdateTime(remainingSeconds);
            }
        }

        private void HandleTimerWarningStateChanged(bool isWarning)
        {
            if (timerUIView != null)
            {
                timerUIView.SetWarningState(isWarning);
            }

            if (isWarning)
            {
                sfxAudioManager?.PlayWarningStart();

                if (currentDifficultyConfig == null || currentDifficultyConfig.PlayTickingSoundOnWarning)
                {
                    sfxAudioManager?.StartTimerTickingLoop();
                }
            }
            else
            {
                sfxAudioManager?.StopTimerTickingLoop();
            }
        }

        private void HandleTimerExpired()
        {
            if (activityCompleted || timerExpired)
            {
                return;
            }

            timerExpired = true;
            sfxAudioManager?.StopTimerTickingLoop();
            sfxAudioManager?.PlayTimeUp();
            sfxAudioManager?.StopBackgroundLoop();
            SetInputLocked(true);
            StopTemporaryStates();

            if (timerUIView != null)
            {
                timerUIView.StopAllFeedback();
            }

            if (pauseController != null)
            {
                pauseController.SetPauseButtonInteractable(false);
            }

            RefreshHintUIInteractivity();
            UpdateDebugStatus("Time up.");

            ShowSummaryOverlay(BuildSummaryResult(false, true), summaryOverlayDelay);

            Debug.Log($"Memory Match time up. Activity: {currentActivityConfig?.ActivityId}, Matched: {matchedPairs}/{totalPairs}, Attempts: {pairAttempts}, Clicks: {cardClicks}, Wrong Attempts: {wrongAttempts}, Hints Used: {hintsUsed}, Score: {currentScore}", this);
        }

        private void StopTemporaryStates()
        {
            if (hintRevealRoutine != null)
            {
                StopCoroutine(hintRevealRoutine);
                hintRevealRoutine = null;
            }

            hintActive = false;

            if (learningPopupView != null && waitingForPopupContinue)
            {
                learningPopupView.Hide();
                waitingForPopupContinue = false;
            }

            if (audioNarrationManager != null)
            {
                audioNarrationManager.Stop();
            }

            if (activeCards != null)
            {
                for (int i = 0; i < activeCards.Count; i++)
                {
                    MemoryCardView card = activeCards[i];

                    if (card == null)
                    {
                        continue;
                    }

                    card.SetHinted(false);
                    card.StopHintFeedback();

                    if (!card.IsMatched && card.IsFaceUp)
                    {
                        card.FlipDown();
                    }
                }
            }
        }

        private void ClearCurrentSelectionReferences()
        {
            if (firstSelectedCard != null)
            {
                firstSelectedCard.SetSelected(false);
            }

            if (secondSelectedCard != null)
            {
                secondSelectedCard.SetSelected(false);
            }

            firstSelectedCard = null;
            secondSelectedCard = null;
        }

        private void SetInputLocked(bool locked)
        {
            inputLocked = locked;

            if (boardController != null)
            {
                boardController.SetCardsInputEnabled(!locked);
            }
        }

        private void CompleteActivity()
        {
            activityCompleted = true;
            sfxAudioManager?.StopTimerTickingLoop();
            SetInputLocked(true);

            if (countdownTimer != null)
            {
                countdownTimer.StopTimer();
            }

            if (timerUIView != null)
            {
                timerUIView.StopAllFeedback();
            }

            if (pauseController != null)
            {
                pauseController.SetPauseButtonInteractable(false);
            }

            RefreshHintUIInteractivity();
            UpdateDebugStatus("Activity complete.");

            ShowSummaryOverlay(BuildSummaryResult(true, false), summaryOverlayDelay);

            Debug.Log(
                $"Memory Match complete. Activity: {currentActivityConfig?.ActivityId}, Pairs: {matchedPairs}/{totalPairs}, Attempts: {pairAttempts}, Clicks: {cardClicks}, Wrong Attempts: {wrongAttempts}, Hints Used: {hintsUsed}, Score: {currentScore}, Time Remaining: {countdownTimer?.RemainingSeconds}",
                this);
        }

        private MemoryActivitySummaryResult BuildSummaryResult(bool completed, bool timeUp)
        {
            float totalTime = countdownTimer != null && countdownTimer.TimerEnabled
                ? countdownTimer.TotalSeconds
                : 0f;

            float timeRemaining = countdownTimer != null && countdownTimer.TimerEnabled
                ? countdownTimer.RemainingSeconds
                : 0f;

            float accuracy = pairAttempts <= 0
                ? 0f
                : (float)matchedPairs / pairAttempts * 100f;

            return new MemoryActivitySummaryResult(
                currentActivityConfig != null ? currentActivityConfig.ActivityId : string.Empty,
                completed,
                timeUp,
                totalPairs,
                matchedPairs,
                pairAttempts,
                wrongAttempts,
                cardClicks,
                hintsUsed,
                currentScore,
                totalTime,
                timeRemaining,
                accuracy);
        }

        private void ShowSummaryOverlay(MemoryActivitySummaryResult result, float delay)
        {
            if (summaryOverlayRoutine != null)
            {
                StopCoroutine(summaryOverlayRoutine);
            }

            summaryOverlayRoutine = StartCoroutine(ShowSummaryOverlayRoutine(result, delay));
        }

        private IEnumerator ShowSummaryOverlayRoutine(MemoryActivitySummaryResult result, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            yield return WaitWhilePausedForFlow();

            sfxAudioManager?.StopBackgroundLoop();

            if (result.Completed)
            {
                sfxAudioManager?.PlaySummarySuccess();
            }
            else if (result.TimeUp)
            {
                sfxAudioManager?.PlaySummaryTimeUp();
            }

            if (summaryOverlayView != null)
            {
                summaryOverlayView.Show(result, HandleSummaryContinue, HandleSummaryRetry);
            }

            summaryOverlayRoutine = null;
        }

        private void HandleSummaryContinue()
        {
            sfxAudioManager?.PlayButtonClick();
            Debug.Log("Memory Match summary continue clicked. Phase 10 should forward this result to the global Bloom reward module.", this);
            SceneManager.LoadScene("Loader Scene");
        }

        private void HandleSummaryRetry()
        {
            sfxAudioManager?.PlayButtonClick();

            if (currentActivityConfig != null)
            {
                StartActivity(currentActivityConfig);
            }
        }

        private void HandleHowToPlayRequested()
        {
            if (activityCompleted ||
                timerExpired ||
                waitingForPopupContinue ||
                hintActive ||
                inputLocked ||
                evaluateSelectionRoutine != null)
            {
                return;
            }

            waitingForHowToPlay = true;
            howToOpenedFromGameplay = true;

            sfxAudioManager?.PlayPopupOpen();
            sfxAudioManager?.PauseBackgroundLoop();

            SetInputLocked(true);
            PauseTimerOnly();
            RefreshHintUIInteractivity();

            if (howToPlayOverlayView != null)
            {
                howToPlayOverlayView.Show();
            }

            UpdateDebugStatus("How to play.");
        }

        private void HandleHowToPlayClosed()
        {
            if (!waitingForHowToPlay)
            {
                return;
            }

            bool wasOpenedFromGameplay = howToOpenedFromGameplay;

            waitingForHowToPlay = false;
            howToOpenedFromGameplay = false;

            if (activityCompleted || timerExpired)
            {
                return;
            }

            if (wasOpenedFromGameplay)
            {
                sfxAudioManager?.ResumeBackgroundLoop();
                ResumeTimerOnly();
                SetInputLocked(false);
                RefreshHintUIInteractivity();
                UpdateDebugStatus("Resumed.");
            }
            else
            {
                StartGameplayAfterIntro();
            }
        }

        private void RefreshHintUIInteractivity()
        {
            if (hintUIView == null)
            {
                return;
            }

            hintUIView.UpdateHintsRemaining(hintsUsed, maxHints);
            hintUIView.SetInteractable(CanUseHintWithoutPairSearch());
        }

        private bool CanUseHintWithoutPairSearch()
        {
            if (currentDifficultyConfig == null || !currentDifficultyConfig.HintsEnabled)
            {
                return false;
            }

            if (hintsUsed >= maxHints || maxHints <= 0)
            {
                return false;
            }

            if (hintActive || inputLocked || gamePaused || timerExpired || activityCompleted || waitingForPopupContinue || waitingForHowToPlay)
            {
                return false;
            }

            if (firstSelectedCard != null || secondSelectedCard != null)
            {
                return false;
            }

            return true;
        }

        private void SubscribeTimerEvents()
        {
            if (countdownTimer == null)
            {
                return;
            }

            countdownTimer.TimeChanged += HandleTimerChanged;
            countdownTimer.WarningStateChanged += HandleTimerWarningStateChanged;
            countdownTimer.TimeExpired += HandleTimerExpired;
        }

        private void UnsubscribeTimerEvents()
        {
            if (countdownTimer == null)
            {
                return;
            }

            countdownTimer.TimeChanged -= HandleTimerChanged;
            countdownTimer.WarningStateChanged -= HandleTimerWarningStateChanged;
            countdownTimer.TimeExpired -= HandleTimerExpired;
        }

        private void UpdateDebugStatus(string state)
        {
            if (debugStatusText == null)
            {
                return;
            }

            string activityId = currentActivityConfig != null
                ? currentActivityConfig.ActivityId
                : "No Activity";

            string timeLine = countdownTimer != null && countdownTimer.TimerEnabled
                ? $"\nTime: {Mathf.CeilToInt(countdownTimer.RemainingSeconds)}s"
                : string.Empty;

            string hintLine = currentDifficultyConfig != null && currentDifficultyConfig.HintsEnabled
                ? $"\nHints: {hintsUsed}/{maxHints}"
                : string.Empty;

            string scoreLine = currentDifficultyConfig != null && currentDifficultyConfig.ScoringEnabled
                ? $"\nScore: {currentScore}"
                : string.Empty;

            string metricsLine =
                $"\nAttempts: {pairAttempts}" +
                $"\nClicks: {cardClicks}";

            debugStatusText.text =
                $"{state}\n" +
                $"Activity: {activityId}\n" +
                $"Matched: {matchedPairs}/{totalPairs}\n" +
                $"Wrong Attempts: {wrongAttempts}" +
                metricsLine +
                hintLine +
                scoreLine +
                timeLine;
        }

        public void GoHome()
        {
            SceneManager.LoadScene("Loader Scene");
        }
    }
}
