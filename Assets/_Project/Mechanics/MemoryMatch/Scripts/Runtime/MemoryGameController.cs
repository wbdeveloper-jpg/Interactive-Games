using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

            currentActivityConfig = config;
            currentDifficultyConfig = config.DifficultyConfig;
            firstSelectedCard = null;
            secondSelectedCard = null;
            matchedPairs = 0;
            wrongAttempts = 0;
            hintsUsed = 0;
            maxHints = currentDifficultyConfig != null ? currentDifficultyConfig.MaxHints : 0;
            inputLocked = false;
            waitingForPopupContinue = false;
            activityCompleted = false;
            timerExpired = false;
            gamePaused = false;
            hintActive = false;
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
            SetInputLocked(false);

            if (countdownTimer != null && countdownTimer.TimerEnabled)
            {
                countdownTimer.StartTimer();
            }

            UpdateDebugStatus("Activity started.");
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
            secondSelectedCard.FlipUp();
            secondSelectedCard.SetSelected(true);
            SetInputLocked(true);
            RefreshHintUIInteractivity();

            evaluateSelectionRoutine = StartCoroutine(EvaluateSelectedCardsRoutine());
        }

        private IEnumerator EvaluateSelectedCardsRoutine()
        {
            yield return new WaitForSeconds(matchCheckDelay);

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

            if (correctFeedbackBeforePopupDelay > 0f)
            {
                yield return new WaitForSeconds(correctFeedbackBeforePopupDelay);
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

            HintPair pair = FindHintPair();

            if (!pair.IsValid)
            {
                RefreshHintUIInteractivity();
                return;
            }

            hintRevealRoutine = StartCoroutine(HintRevealRoutine(pair.First, pair.Second));
        }

        private bool CanUseHint()
        {
            if (!CanUseHintWithoutPairSearch())
            {
                return false;
            }

            return FindHintPair().IsValid;
        }

        private IEnumerator HintRevealRoutine(MemoryCardView first, MemoryCardView second)
        {
            hintActive = true;
            hintsUsed++;
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

        private void PauseGame()
        {
            if (gamePaused || activityCompleted || timerExpired)
            {
                return;
            }

            gamePaused = true;
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
            hintActive = false;

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

            if (!waitingForPopupContinue && !timerExpired && !activityCompleted && !hintActive)
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
        }

        private void HandleTimerExpired()
        {
            if (activityCompleted || timerExpired)
            {
                return;
            }

            timerExpired = true;
            SetInputLocked(true);

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
            Debug.Log($"Memory Match time up. Activity: {currentActivityConfig?.ActivityId}, Matched: {matchedPairs}/{totalPairs}, Wrong Attempts: {wrongAttempts}, Hints Used: {hintsUsed}", this);
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
            Debug.Log(
                $"Memory Match complete. Activity: {currentActivityConfig?.ActivityId}, Pairs: {matchedPairs}/{totalPairs}, Wrong Attempts: {wrongAttempts}, Hints Used: {hintsUsed}, Time Remaining: {countdownTimer?.RemainingSeconds}",
                this);
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

            if (hintActive || inputLocked || gamePaused || timerExpired || activityCompleted || waitingForPopupContinue)
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

            debugStatusText.text =
                $"{state}\n" +
                $"Activity: {activityId}\n" +
                $"Matched: {matchedPairs}/{totalPairs}\n" +
                $"Wrong Attempts: {wrongAttempts}" +
                timeLine;
        }
    }
}
