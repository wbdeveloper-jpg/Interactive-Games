using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MeasurementMix
{
    public class MeasurementGameManager : MonoBehaviour
    {
        [Header("Core")]
        public MeasurementGameSettings settings;
        public MeasurementQuestionGenerator questionGenerator;
        public MeasurementAudioManager audioManager;
        public MeasurementHintController hintController;

        [Header("Question Controllers")]
        public BalanceScaleController balanceScaleController;
        public LiquidMeasurementController liquidController;
        public MeasurementConversionController conversionController;

        [Header("Gameplay Panels")]
        public GameObject massPanel;
        public GameObject liquidPanel;
        public GameObject conversionPanel;

        [Header("Top UI")]
        public TMP_Text questionText;
        public TMP_Text roundText;
        public TMP_Text timerText;
        public TMP_Text scoreText;
        public TMP_Text feedbackText;
        public Button checkButton;
        public Button pauseButton;

        [Header("How To Play")]
        public GameObject howToPlayPanel;
        public Button howToPlayStartButton;

        [Header("Pause")]
        public GameObject pausePanel;
        public Button resumeButton;
        public Button restartButton;
        public Button pauseHomeButton;

        [Header("Result")]
        public GameObject resultPanel;
        public TMP_Text resultTitleText;
        public TMP_Text resultScoreText;
        public TMP_Text resultDetailText;
        public Button replayButton;
        public Button resultHomeButton;

        [Header("External Callbacks")]
        public UnityEvent onHomeRequested;
        public UnityEvent onGameCompleted;

        [Header("Editable Feedback Text")]
        public string massInstruction =
            "Drag weights onto the right pan, then tap CHECK.";
        public string liquidInstruction =
            "Add or remove water, then tap CHECK.";
        public string conversionInstruction =
            "Choose the equivalent measurement, then tap CHECK.";
        public string wrongMassText =
            "The scale is not balanced yet. Try another combination.";
        public string wrongLiquidText =
            "The volume does not match yet. Add or remove water.";
        public string wrongConversionText =
            "That conversion is not equal. Try again or use Hint.";
        public string correctText =
            "Correct! Take a moment, then the next round begins.";
        public string timeoutText =
            "Time is up. The next question will begin shortly.";

        private readonly List<MeasurementQuestionType> questionOrder =
            new List<MeasurementQuestionType>(5);

        private MeasurementQuestion activeQuestion;
        private Tween transitionTween;
        private int currentRoundIndex;
        private int score;
        private int correctAnswers;
        private float timeRemaining;
        private bool roundActive;
        private bool hintUsedThisRound;
        private bool isPaused;
        private int lastDisplayedSecond = -1;

        private void Start()
        {
            if (settings == null || questionGenerator == null)
            {
                Debug.LogError(
                    "MeasurementGameManager requires MeasurementGameSettings " +
                    "and MeasurementQuestionGenerator references.",
                    this);
                enabled = false;
                return;
            }

            RegisterButtons();
            HideAllTransientPanels();

            if (settings != null && settings.showHowToPlayAtStart &&
                howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(true);
            }
            else
            {
                StartNewGame();
            }
        }

        private void Update()
        {
            if (!roundActive || isPaused)
                return;

            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                RefreshTimer();
                CompleteRound(false, true);
                return;
            }

            RefreshTimer();
        }

        private void OnDisable()
        {
            transitionTween?.Kill();
            if (isPaused)
                Time.timeScale = 1f;
        }

        public void StartNewGame()
        {
            transitionTween?.Kill();
            Time.timeScale = 1f;
            isPaused = false;
            currentRoundIndex = 0;
            score = 0;
            correctAnswers = 0;
            roundActive = false;
            activeQuestion = null;

            questionGenerator?.ResetForNewRun();
            BuildQuestionOrder();
            if (howToPlayPanel != null)
                howToPlayPanel.SetActive(false);
            HideAllTransientPanels();
            RefreshScore();
            BeginNextRound();
        }

        public void StartFromHowToPlay()
        {
            audioManager?.PlayButton();
            if (howToPlayPanel != null)
                howToPlayPanel.SetActive(false);
            StartNewGame();
        }

        public void CheckAnswer()
        {
            if (!roundActive || activeQuestion == null)
                return;

            audioManager?.PlayButton();

            if (activeQuestion.IsConversion &&
                (conversionController == null ||
                 !conversionController.HasSelection()))
            {
                ShowFeedback("Choose an answer first.", false);
                return;
            }

            bool correct;
            switch (activeQuestion.type)
            {
                case MeasurementQuestionType.PracticalMass:
                    correct = balanceScaleController != null &&
                        balanceScaleController.IsCorrect();
                    break;
                case MeasurementQuestionType.PracticalLiquid:
                    correct = liquidController != null &&
                        liquidController.IsCorrect();
                    break;
                default:
                    correct = conversionController != null &&
                        conversionController.IsCorrect();
                    break;
            }

            if (correct)
            {
                CompleteRound(true, false);
                return;
            }

                ShowFeedback(GetWrongMessage(activeQuestion.type), false);
            audioManager?.PlayWrong();
            hintController?.EncourageHint();
            ShakeCheckButton();
        }

        public void UseHint()
        {
            if (!roundActive || activeQuestion == null)
                return;

            audioManager?.PlayHint();

            if (!hintUsedThisRound)
            {
                hintUsedThisRound = true;
                score = Mathf.Max(0, score - settings.hintPenalty);
                RefreshScore();
            }

            switch (activeQuestion.type)
            {
                case MeasurementQuestionType.PracticalMass:
                    balanceScaleController?.ShowHint(
                        activeQuestion.solutionWeightValues,
                        settings.weightHintPulseCount);
                    ShowFeedback("Look at the highlighted weights.", true);
                    break;

                case MeasurementQuestionType.PracticalLiquid:
                    liquidController?.RevealTargetLine(
                        settings.keepLiquidTargetLineVisibleAfterHint,
                        settings.temporaryLiquidHintDuration);
                    ShowFeedback("Match the water level to the highlighted line.", true);
                    break;

                default:
                    conversionController?.ShowCorrectOptionHint();
                    ShowFeedback("Look closely at the highlighted conversion.", true);
                    break;
            }

            hintController?.MarkUsed();
        }

        public void PauseGame()
        {
            if (isPaused || !roundActive)
                return;

            audioManager?.PlayButton();
            isPaused = true;
            Time.timeScale = 0f;
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                AnimateModal(pausePanel);
            }
        }

        public void ResumeGame()
        {
            audioManager?.PlayButton();
            isPaused = false;
            Time.timeScale = 1f;
            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        public void RestartGame()
        {
            audioManager?.PlayButton();
            StartNewGame();
        }

        public void RequestHome()
        {
            audioManager?.PlayButton();
            Time.timeScale = 1f;
            isPaused = false;
            onHomeRequested?.Invoke();
        }

        private void BeginNextRound()
        {
            int total = settings != null
                ? Mathf.Clamp(settings.questionsPerRun, 1, 5)
                : 5;

            if (currentRoundIndex >= total)
            {
                ShowResults();
                return;
            }

            MeasurementQuestionType type = questionOrder[currentRoundIndex];
            MeasurementDifficultyProfile profile = settings.CurrentProfile;
            IReadOnlyList<int> tokens = balanceScaleController != null
                ? balanceScaleController.GetAllTokenValues()
                : null;

            activeQuestion = questionGenerator.Generate(type, profile, tokens);
            currentRoundIndex++;
            timeRemaining = Mathf.Max(10f, profile.secondsPerQuestion);
            lastDisplayedSecond = -1;
            roundActive = true;
            hintUsedThisRound = false;

            SetQuestionPanel(type);
            PrepareController(type, profile);

            if (questionText != null)
                questionText.text = activeQuestion.prompt;
            if (roundText != null)
                roundText.text = "Round " + currentRoundIndex + " / " + total;
            if (feedbackText != null)
                feedbackText.text = GetInstruction(type);
            if (checkButton != null)
                checkButton.interactable = true;
            if (pauseButton != null)
                pauseButton.interactable = true;

            hintController?.ResetForRound();
            RefreshTimer(true);
            AnimateActivePanel();
        }

        private void PrepareController(
            MeasurementQuestionType type,
            MeasurementDifficultyProfile profile)
        {
            balanceScaleController?.SetInteraction(false);
            liquidController?.SetInteraction(false);
            conversionController?.SetInteraction(false);

            switch (type)
            {
                case MeasurementQuestionType.PracticalMass:
                    balanceScaleController?.PrepareQuestion(activeQuestion, profile);
                    break;
                case MeasurementQuestionType.PracticalLiquid:
                    liquidController?.PrepareQuestion(activeQuestion, profile);
                    break;
                default:
                    conversionController?.PrepareQuestion(activeQuestion);
                    break;
            }
        }

        private void CompleteRound(bool correct, bool timedOut)
        {
            if (!roundActive)
                return;

            roundActive = false;
            SetAllInteraction(false);

            if (correct)
            {
                correctAnswers++;
                int maximumBonus = Mathf.Max(0, settings.maximumTimeBonus);
                int timeBonus = Mathf.Clamp(
                    Mathf.CeilToInt(timeRemaining),
                    0,
                    maximumBonus);
                score += settings.pointsPerCorrectAnswer + timeBonus;
                ShowFeedback(correctText, true);
                audioManager?.PlayCorrect();
            }
            else if (timedOut)
            {
                ShowFeedback(timeoutText, false);
                audioManager?.PlayTimeout();
            }

            RefreshScore();
            float delay = correct
                ? settings.correctFeedbackDuration
                : settings.timeoutFeedbackDuration;

            transitionTween?.Kill();
            transitionTween = DOVirtual.DelayedCall(
                    Mathf.Max(1f, delay),
                    BeginNextRound)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void ShowResults()
        {
            roundActive = false;
            SetAllInteraction(false);
            SetQuestionPanel(null);

            int total = Mathf.Clamp(settings.questionsPerRun, 1, 5);
            if (resultTitleText != null)
                resultTitleText.text = "Measurement challenge complete!";
            if (resultScoreText != null)
                resultScoreText.text = "Score: " + score;
            if (resultDetailText != null)
            {
                resultDetailText.text =
                    "Correct answers: " + correctAnswers + " / " + total;
            }

            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                AnimateModal(resultPanel);
            }

            onGameCompleted?.Invoke();
        }

        private void BuildQuestionOrder()
        {
            questionOrder.Clear();
            int total = Mathf.Clamp(settings.questionsPerRun, 1, 5);

            int massCount = total == 1
                ? (Random.value < settings.massQuestionChance ? 1 : 0)
                : Mathf.Clamp(
                    Mathf.RoundToInt(total * settings.massQuestionChance),
                    1,
                    total - 1);

            MeasurementDifficultyProfile profile = settings.CurrentProfile;
            for (int index = 0; index < total; index++)
            {
                bool isMass = index < massCount;
                bool conversion = Random.value < profile.conversionQuestionChance;
                questionOrder.Add(GetQuestionType(isMass, conversion));
            }

            if (total >= 2 &&
                profile.conversionQuestionChance > 0f &&
                !ContainsConversion(questionOrder))
            {
                MeasurementDomain domain = GetDomain(questionOrder[total - 1]);
                questionOrder[total - 1] = GetQuestionType(
                    domain == MeasurementDomain.Mass,
                    true);
            }

            if (total >= 2 &&
                profile.conversionQuestionChance < 1f &&
                !ContainsPractical(questionOrder))
            {
                MeasurementDomain domain = GetDomain(questionOrder[0]);
                questionOrder[0] = GetQuestionType(
                    domain == MeasurementDomain.Mass,
                    false);
            }

            Shuffle(questionOrder);
            PreventThreeSameDomains();
        }

        private void PreventThreeSameDomains()
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                if (!HasThreeSameDomains())
                    return;
                Shuffle(questionOrder);
            }
        }

        private bool HasThreeSameDomains()
        {
            for (int index = 2; index < questionOrder.Count; index++)
            {
                MeasurementDomain domain = GetDomain(questionOrder[index]);
                if (domain == GetDomain(questionOrder[index - 1]) &&
                    domain == GetDomain(questionOrder[index - 2]))
                    return true;
            }
            return false;
        }

        private void SetQuestionPanel(MeasurementQuestionType? type)
        {
            bool mass = type == MeasurementQuestionType.PracticalMass;
            bool liquid = type == MeasurementQuestionType.PracticalLiquid;
            bool conversion = type == MeasurementQuestionType.MassConversion ||
                type == MeasurementQuestionType.LiquidConversion;

            if (massPanel != null)
                massPanel.SetActive(mass);
            if (liquidPanel != null)
                liquidPanel.SetActive(liquid);
            if (conversionPanel != null)
                conversionPanel.SetActive(conversion);
        }

        private void AnimateActivePanel()
        {
            GameObject panel = activeQuestion.type ==
                MeasurementQuestionType.PracticalMass
                ? massPanel
                : activeQuestion.type == MeasurementQuestionType.PracticalLiquid
                    ? liquidPanel
                    : conversionPanel;

            if (panel == null)
                return;

            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.AddComponent<CanvasGroup>();

            group.DOKill();
            group.alpha = 0f;
            group.DOFade(1f, settings.panelFadeDuration)
                .SetEase(Ease.OutQuad)
                .SetLink(panel, LinkBehaviour.KillOnDestroy);
        }

        private void AnimateModal(GameObject panel)
        {
            RectTransform rect = panel.transform as RectTransform;
            if (rect == null)
                return;

            rect.DOKill();
            rect.localScale = Vector3.one * 0.86f;
            rect.DOScale(1f, 0.35f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetLink(panel, LinkBehaviour.KillOnDestroy);
        }

        private void ShowFeedback(string message, bool positive)
        {
            if (feedbackText == null)
                return;

            feedbackText.text = message;
            feedbackText.DOKill();
            feedbackText.alpha = 0.55f;
            feedbackText.DOFade(1f, 0.2f)
                .SetLink(feedbackText.gameObject, LinkBehaviour.KillOnDestroy);
            feedbackText.transform.DOKill();
            feedbackText.transform.localScale = Vector3.one;
            feedbackText.transform.DOPunchScale(
                    Vector3.one * (positive ? 0.045f : 0.025f),
                    0.35f,
                    4,
                    0.6f)
                .SetLink(feedbackText.gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void ShakeCheckButton()
        {
            if (checkButton == null)
                return;

            checkButton.transform.DOKill();
            checkButton.transform.DOShakeScale(0.3f, 0.1f, 7, 45f)
                .SetLink(checkButton.gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void SetAllInteraction(bool enabled)
        {
            balanceScaleController?.SetInteraction(enabled);
            liquidController?.SetInteraction(enabled);
            conversionController?.SetInteraction(enabled);
            if (checkButton != null)
                checkButton.interactable = enabled;
            if (pauseButton != null)
                pauseButton.interactable = enabled;
        }

        private void RefreshTimer(bool force = false)
        {
            int displayedSecond = Mathf.CeilToInt(timeRemaining);
            if (!force && displayedSecond == lastDisplayedSecond)
                return;

            lastDisplayedSecond = displayedSecond;
            if (timerText != null)
                timerText.text = "Time: " + displayedSecond + "s";
        }

        private void RefreshScore()
        {
            if (scoreText != null)
                scoreText.text = "Score: " + score;
        }

        private void RegisterButtons()
        {
            if (checkButton != null)
                checkButton.onClick.AddListener(CheckAnswer);
            if (pauseButton != null)
                pauseButton.onClick.AddListener(PauseGame);
            if (howToPlayStartButton != null)
                howToPlayStartButton.onClick.AddListener(StartFromHowToPlay);
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            if (pauseHomeButton != null)
                pauseHomeButton.onClick.AddListener(RequestHome);
            if (replayButton != null)
                replayButton.onClick.AddListener(StartNewGame);
            if (resultHomeButton != null)
                resultHomeButton.onClick.AddListener(RequestHome);
        }

        private void HideAllTransientPanels()
        {
            SetQuestionPanel(null);
            if (pausePanel != null)
                pausePanel.SetActive(false);
            if (resultPanel != null)
                resultPanel.SetActive(false);
            if (howToPlayPanel != null && !settings.showHowToPlayAtStart)
                howToPlayPanel.SetActive(false);
        }

        private string GetInstruction(MeasurementQuestionType type)
        {
            switch (type)
            {
                case MeasurementQuestionType.PracticalMass:
                    return massInstruction;
                case MeasurementQuestionType.PracticalLiquid:
                    return liquidInstruction;
                default:
                    return conversionInstruction;
            }
        }

        private string GetWrongMessage(MeasurementQuestionType type)
        {
            switch (type)
            {
                case MeasurementQuestionType.PracticalMass:
                    return wrongMassText;
                case MeasurementQuestionType.PracticalLiquid:
                    return wrongLiquidText;
                default:
                    return wrongConversionText;
            }
        }

        private static MeasurementQuestionType GetQuestionType(
            bool mass,
            bool conversion)
        {
            if (mass)
            {
                return conversion
                    ? MeasurementQuestionType.MassConversion
                    : MeasurementQuestionType.PracticalMass;
            }

            return conversion
                ? MeasurementQuestionType.LiquidConversion
                : MeasurementQuestionType.PracticalLiquid;
        }

        private static MeasurementDomain GetDomain(MeasurementQuestionType type)
        {
            return type == MeasurementQuestionType.PracticalMass ||
                type == MeasurementQuestionType.MassConversion
                ? MeasurementDomain.Mass
                : MeasurementDomain.Liquid;
        }

        private static bool ContainsConversion(
            IList<MeasurementQuestionType> order)
        {
            for (int index = 0; index < order.Count; index++)
            {
                if (order[index] == MeasurementQuestionType.MassConversion ||
                    order[index] == MeasurementQuestionType.LiquidConversion)
                    return true;
            }
            return false;
        }

        private static bool ContainsPractical(
            IList<MeasurementQuestionType> order)
        {
            for (int index = 0; index < order.Count; index++)
            {
                if (order[index] == MeasurementQuestionType.PracticalMass ||
                    order[index] == MeasurementQuestionType.PracticalLiquid)
                    return true;
            }
            return false;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int index = list.Count - 1; index > 0; index--)
            {
                int swapIndex = Random.Range(0, index + 1);
                T temporary = list[index];
                list[index] = list[swapIndex];
                list[swapIndex] = temporary;
            }
        }
    }
}
