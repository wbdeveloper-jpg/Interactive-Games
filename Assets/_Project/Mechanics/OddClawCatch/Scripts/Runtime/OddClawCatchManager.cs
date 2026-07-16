using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum OddClawAimMode
{
    EasyWithGuideLine,
    NormalNoGuideLine
}

public class OddClawCatchManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Fonts")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;
    public bool applyFontsOnAwake = true;

    [Header("Question Generator")]
    public OddClawQuestionGeneratorBase questionGenerator;
    [Range(2, 6)] public int answerOptionCount = 4;

    [Header("Game Mode")]
    public OddClawAimMode aimMode = OddClawAimMode.EasyWithGuideLine;

    [Header("Core References")]
    public Canvas rootCanvas;
    public OddClawController clawController;
    public OddClawAudioManager audioManager;
    public OddClawFeedbackPopup feedbackPopup;
    public TMP_Text questionText;

    [Header("Top Bar")]
    public TMP_Text scoreText;
    public TMP_Text questionHeaderText;
    public TMP_Text healthLabel;
    public Slider healthSlider;
    public TMP_Text waveText;
    public TMP_Text timerLabel;
    public Slider timerSlider;
    public TMP_Text speedMultiplierText;
    public Button pauseButton;

    [Header("Ground Item Area")]
    public RectTransform itemContainer;
    public OddClawItemView textItemTemplate;
    public OddClawItemView imageItemTemplate;
    public float itemSpacing = 30f;
    [Tooltip("Keeps remaining objects from snapping into the empty gap after one object gets caught.")]
    public bool lockItemPositionsAfterSpawn = true;
    [Tooltip("Adds small random offsets/rotation so the bottom objects feel less like a rigid menu.")]
    public bool organicItemPlacement = true;
    public float organicHorizontalJitter = 10f;
    public float organicVerticalJitter = 12f;
    public float organicRotationJitter = 3f;
    [Tooltip("Extra reach padding added after calculating the furthest answer object from the claw pivot.")]
    public float dynamicReachPadding = 90f;

    [Header("Overlay Panels")]
    public GameObject loadingPanel;
    public GameObject howToPlayPanel;
    public GameObject pausePanel;
    public GameObject resultPanel;

    [Header("Loading Screen")]
    public string gameTitle = "ODD CLAW CATCH";
    public TMP_Text loadingTitleText;
    public Slider loadingSlider;
    public float localLoadingDuration = 0.75f;

    [Header("First Pick Hint")]
    public bool showFirstPickHint = true;
    public bool showFirstPickHintUntilFirstCorrect = true;
    public string firstPickHintMessage = "Click anywhere to pick an object";
    public CanvasGroup firstPickHintOverlay;
    public TMP_Text firstPickHintText;
    public float firstPickHintBreathScale = 1.06f;
    public float firstPickHintBreathDuration = 0.8f;

    [Header("How To Play")]
    public List<Sprite> howToPlayGuideImages = new List<Sprite>();
    public Image howToPlayGuideImage;
    public TMP_Text howToPlayFallbackText;
    public TMP_Text howToPlayStepCounterText;
    public Button howToPlayPrevButton;
    public Button howToPlayNextButton;
    public Button howToPlayStartButton;
    [TextArea(4, 8)] public string howToPlayFallbackInstructions =
        "Wait for the claw to aim at the correct answer.\nTap anywhere to extend the claw.\nOnly overlapping items can be caught.\nCorrect catches increase score. Wrong catches and timeouts reduce health. Misses only play feedback and let you try again.";

    [Header("Pause Panel Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button homeButton;

    [Header("Result Panel")]
    public TMP_Text resultTitleText;
    public TMP_Text resultBodyText;
    public Button resultContinueButton;
    public Button resultPlayAgainButton;
    public Button resultHomeButton;

    [Header("Gameplay Rules")]
    public int maxHealth = 3;
    public int wrongHealthPenalty = 1;
    public int missHealthPenalty = 1;
    [Tooltip("Keep this false for arcade timing feel: a miss only plays miss feedback and lets the player retry the same question.")]
    public bool penalizeMiss = false;
    [Tooltip("When false, misses do not reduce Bloom accuracy. Correct/wrong catches and timeouts still count as attempts.")]
    public bool countMissAsAttempt = false;
    [Tooltip("Small final settle delay after the full correct evaluation popup + item fade has completed.")]
    public float correctDelay = 0.15f;
    [Tooltip("Small final settle delay after the full wrong evaluation popup + item fade has completed.")]
    public float nextWaveDelay = 0.15f;

    [Header("Evaluation Flow")]
    [Tooltip("Recommended ON. Keeps the claw locked until popup, item fade, and result timing are completely finished.")]
    public bool lockClawDuringEvaluation = true;
    [Tooltip("How long the evaluated object stays visible after turning green/red before fading out.")]
    public float evaluationItemHoldBeforeFade = 0.42f;
    [Tooltip("Fade duration for the caught object after correct/wrong evaluation.")]
    public float evaluationItemFadeDuration = 0.28f;
    [Tooltip("When enabled, next wave/retry waits for the popup to fade away fully.")]
    public bool waitForPopupBeforeContinuing = true;

    [Header("Wave Timer")]
    public float startingWaveTime = 35f;
    public float timerDecreasePerWave = 1f;
    public float minimumWaveTime = 15f;

    [Header("Bloom Evaluation")]
    public float expectedMaxTime = 180f;

    private readonly List<OddClawItemView> _spawnedItems = new List<OddClawItemView>();
    private List<SkillEntry> _skills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: 0.3f, accuracyWeight: 0.7f),
        new SkillEntry(BloomSkillType.Analyze, 75f, timeWeight: 0.5f, accuracyWeight: 0.5f),
    };

    private OddClawQuestionData _currentQuestion;
    private int _currentHealth;
    private int _wave;
    private int _score;
    private int _correctCount;
    private int _totalAttempts;
    private int _mistakeCount;
    private float _currentWaveDuration;
    private float _remainingWaveTime;
    private float _sessionStartTime;
    private int _howToPlayIndex;
    private bool _gameplayActive;
    private bool _gameOver;
    private bool _isPaused;
    private bool _localResultShown;
    private bool _hasCaughtFirstCorrect;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (audioManager == null)
            audioManager = FindObjectOfType<OddClawAudioManager>();

        if (clawController != null && clawController.audioManager == null)
            clawController.audioManager = audioManager;

        ApplyAimModeToClaw();

        _currentHealth = maxHealth;
        HookButtons();

        if (applyFontsOnAwake)
            ApplyFontsToAllTexts();

        HideAllPanels();
        HideFirstPickHint(true);
        SetTemplatesHidden();
        UpdateTopBar();
    }

    private IEnumerator Start()
    {
        yield return StartCoroutine(BootFlow());
    }

    private void Update()
    {
        if (!_gameplayActive || _gameOver || _isPaused)
            return;

        _remainingWaveTime -= Time.deltaTime;
        UpdateTopBar();

        if (_remainingWaveTime <= 0f)
        {
            StartCoroutine(HandleTimeout());
            return;
        }

        if (WasTapPressed())
        {
            HideFirstPickHint(false);

            if (clawController != null && !clawController.IsBusy)
            {
                _gameplayActive = false;
                clawController.TryCatch(_spawnedItems, rootCanvas, OnClawCatchComplete);
            }
        }
    }

    private IEnumerator BootFlow()
    {
        _gameplayActive = false;

        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPreGame(_skills);
            while (!RewardManager.Instance.IsPreGameComplete)
                yield return null;
        }
        else
        {
            Debug.LogWarning("OddClawCatchManager could not find RewardManager.Instance. Gameplay will continue for editor testing, but Bloom must exist in the LoadingScene for production.");
        }

        ShowOnlyPanel(loadingPanel);
        UpdateLoadingView(0f);

        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, localLoadingDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            UpdateLoadingView(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        UpdateLoadingView(1f);
        yield return new WaitForSeconds(0.1f);

        ShowHowToPlayPanel();
    }

    private void UpdateLoadingView(float progress)
    {
        if (loadingTitleText != null)
            loadingTitleText.text = gameTitle;

        if (loadingSlider != null)
            loadingSlider.value = Mathf.Clamp01(progress);
    }

    private void HookButtons()
    {
        AddButtonListener(pauseButton, PauseGame);
        AddButtonListener(howToPlayPrevButton, PreviousHowToPlayStep);
        AddButtonListener(howToPlayNextButton, NextHowToPlayStep);
        AddButtonListener(howToPlayStartButton, StartFromHowToPlay);
        AddButtonListener(resumeButton, ResumeGame);
        AddButtonListener(restartButton, OnPlayAgain);
        AddButtonListener(homeButton, OnHome);
        AddButtonListener(resultContinueButton, ContinueToBloomPostGame);
        AddButtonListener(resultPlayAgainButton, OnPlayAgain);
        AddButtonListener(resultHomeButton, OnHome);
    }

    private void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(() =>
        {
            if (audioManager != null)
                audioManager.PlayButtonClick();
            action.Invoke();
        });
    }

    private void ApplyAimModeToClaw()
    {
        if (clawController == null)
            return;

        clawController.SetEasyGuideEnabled(aimMode == OddClawAimMode.EasyWithGuideLine);
    }

    private void ShowHowToPlayPanel()
    {
        HideFirstPickHint(true);
        _howToPlayIndex = 0;
        UpdateHowToPlayPanel();
        ShowOnlyPanel(howToPlayPanel);
    }

    private void PreviousHowToPlayStep()
    {
        if (howToPlayGuideImages == null || howToPlayGuideImages.Count == 0)
            return;

        _howToPlayIndex = Mathf.Max(0, _howToPlayIndex - 1);
        UpdateHowToPlayPanel();
    }

    private void NextHowToPlayStep()
    {
        if (howToPlayGuideImages == null || howToPlayGuideImages.Count == 0)
            return;

        _howToPlayIndex = Mathf.Min(howToPlayGuideImages.Count - 1, _howToPlayIndex + 1);
        UpdateHowToPlayPanel();
    }

    private void UpdateHowToPlayPanel()
    {
        bool hasImages = howToPlayGuideImages != null && howToPlayGuideImages.Count > 0;

        if (howToPlayGuideImage != null)
        {
            howToPlayGuideImage.gameObject.SetActive(hasImages);
            if (hasImages)
                howToPlayGuideImage.sprite = howToPlayGuideImages[Mathf.Clamp(_howToPlayIndex, 0, howToPlayGuideImages.Count - 1)];
        }

        if (howToPlayFallbackText != null)
        {
            howToPlayFallbackText.gameObject.SetActive(!hasImages);
            howToPlayFallbackText.text = howToPlayFallbackInstructions;
        }

        if (howToPlayStepCounterText != null)
        {
            howToPlayStepCounterText.text = hasImages
                ? (_howToPlayIndex + 1) + " / " + howToPlayGuideImages.Count
                : "Guide";
        }

        if (howToPlayPrevButton != null)
            howToPlayPrevButton.interactable = hasImages && _howToPlayIndex > 0;

        if (howToPlayNextButton != null)
            howToPlayNextButton.interactable = hasImages && _howToPlayIndex < howToPlayGuideImages.Count - 1;
    }

    private void StartFromHowToPlay()
    {
        HideAllPanels();
        StartCoroutine(StartGameplayAfterReadyAnimation());
    }

    private IEnumerator StartGameplayAfterReadyAnimation()
    {
        if (clawController != null)
        {
            ApplyAimModeToClaw();
            clawController.SetInputEnabled(false);
            clawController.PlayReadyAnimation();
        }

        yield return new WaitForSeconds(0.45f);

        _sessionStartTime = Time.time;
        _currentHealth = maxHealth;
        _wave = 0;
        _score = 0;
        _correctCount = 0;
        _totalAttempts = 0;
        _mistakeCount = 0;
        _gameOver = false;
        _localResultShown = false;
        _hasCaughtFirstCorrect = false;

        if (audioManager != null)
            audioManager.PlayBgm();

        StartNextWave();
    }

    private void StartNextWave()
    {
        if (_gameOver)
            return;

        ClearItems();
        EnsureGenerator();

        _wave++;
        _currentWaveDuration = Mathf.Max(minimumWaveTime, startingWaveTime - ((_wave - 1) * timerDecreasePerWave));
        _remainingWaveTime = _currentWaveDuration;

        if (clawController != null)
        {
            ApplyAimModeToClaw();
            clawController.SetWaveDifficulty(_wave);
            clawController.ResetClawImmediate();
            clawController.SetInputEnabled(true);
        }

        _currentQuestion = questionGenerator.GenerateQuestion(_wave, answerOptionCount);
        string error = string.Empty;
        if (_currentQuestion == null || !_currentQuestion.IsValid(2, out error))
        {
            Debug.LogWarning("Odd Claw question generator returned invalid data: " + error);
            _currentQuestion = BuildEmergencyQuestion();
        }

        SpawnAnswerItems(_currentQuestion);
        _gameplayActive = true;
        UpdateTopBar();
        RefreshFirstPickHintForWave();
    }

    private void EnsureGenerator()
    {
        if (questionGenerator != null)
            return;

        questionGenerator = ScriptableObject.CreateInstance<OddClawMathQuestionGenerator>();
        Debug.LogWarning("No OddClawQuestionGeneratorBase assigned. Created runtime math generator for testing.");
    }

    private OddClawQuestionData BuildEmergencyQuestion()
    {
        List<OddClawAnswerOption> options = new List<OddClawAnswerOption>
        {
            new OddClawAnswerOption("1"),
            new OddClawAnswerOption("2"),
            new OddClawAnswerOption("3"),
            new OddClawAnswerOption("4")
        };

        return new OddClawQuestionData
        {
            questionText = "Catch 1",
            answerOptions = options,
            correctAnswerIndex = 0,
            displayMode = OddClawAnswerDisplayMode.Text
        };
    }

    private void SpawnAnswerItems(OddClawQuestionData question)
    {
        if (questionText != null)
            questionText.text = question.questionText;

        if (questionHeaderText != null)
            questionHeaderText.text = question.questionText;

        if (itemContainer == null)
            return;

        HorizontalLayoutGroup layoutGroup = itemContainer.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.spacing = itemSpacing;
            layoutGroup.enabled = !lockItemPositionsAfterSpawn;
        }

        for (int i = 0; i < question.answerOptions.Count; i++)
        {
            OddClawItemView template = question.displayMode == OddClawAnswerDisplayMode.Sprite && imageItemTemplate != null
                ? imageItemTemplate
                : textItemTemplate;

            if (template == null)
            {
                Debug.LogError("OddClawCatchManager has no item template assigned.");
                return;
            }

            OddClawItemView view = Instantiate(template, itemContainer);
            view.gameObject.name = "AnswerItem_" + (i + 1);
            view.gameObject.SetActive(true);
            view.Setup(question.answerOptions[i], question.displayMode, i, question.correctAnswerIndex, primaryFont, secondaryFont);
            _spawnedItems.Add(view);
        }

        if (lockItemPositionsAfterSpawn)
            PlaceItemsManuallyAndLock();
        else
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemContainer);

        EnsureClawCanReachEdgeItems();
    }

    private void PlaceItemsManuallyAndLock()
    {
        int count = _spawnedItems.Count;
        if (count == 0 || itemContainer == null)
            return;

        float containerWidth = itemContainer.rect.width;
        if (containerWidth <= 10f && rootCanvas != null)
        {
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (canvasRect != null)
                containerWidth = canvasRect.rect.width;
        }

        if (containerWidth <= 10f)
            containerWidth = 1080f;

        float itemWidth = 150f;
        RectTransform firstRect = _spawnedItems[0] != null ? _spawnedItems[0].root : null;
        if (firstRect != null)
            itemWidth = Mathf.Max(80f, firstRect.sizeDelta.x);

        float availableWidth = Mathf.Max(240f, containerWidth - 90f);
        float maxSpacing = count > 1 ? Mathf.Max(6f, (availableWidth - (count * itemWidth)) / (count - 1)) : 0f;
        float spacing = count > 1 ? Mathf.Min(itemSpacing, maxSpacing) : 0f;
        float totalWidth = (count * itemWidth) + ((count - 1) * spacing);
        float startX = -totalWidth * 0.5f + itemWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            OddClawItemView item = _spawnedItems[i];
            if (item == null)
                continue;

            RectTransform rect = item.root != null ? item.root : item.transform as RectTransform;
            if (rect == null)
                continue;

            LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement != null)
                layoutElement.ignoreLayout = true;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            float x = startX + (i * (itemWidth + spacing));
            float y = 0f;
            float zRotation = 0f;

            if (organicItemPlacement)
            {
                x += UnityEngine.Random.Range(-organicHorizontalJitter, organicHorizontalJitter);
                y += UnityEngine.Random.Range(-organicVerticalJitter, organicVerticalJitter);
                zRotation = UnityEngine.Random.Range(-organicRotationJitter, organicRotationJitter);
            }

            rect.anchoredPosition = new Vector2(x, y);
            rect.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }
    }


    private void EnsureClawCanReachEdgeItems()
    {
        if (clawController == null || clawController.clawPivot == null || _spawnedItems.Count == 0)
            return;

        float maxDistance = 0f;
        Transform pivotTransform = clawController.clawPivot;

        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            OddClawItemView item = _spawnedItems[i];
            if (item == null)
                continue;

            RectTransform zone = item.catchZone != null ? item.catchZone : item.root;
            if (zone == null)
                continue;

            Vector3[] corners = new Vector3[4];
            zone.GetWorldCorners(corners);
            for (int c = 0; c < corners.Length; c++)
            {
                Vector3 localToPivot = pivotTransform.InverseTransformPoint(corners[c]);
                maxDistance = Mathf.Max(maxDistance, new Vector2(localToPivot.x, localToPivot.y).magnitude);
            }
        }

        if (maxDistance > 0f)
            clawController.EnsureExtensionLength(maxDistance + dynamicReachPadding);
    }

    private void OnClawCatchComplete(OddClawCatchResult result)
    {
        StartCoroutine(ResolveCatchResult(result));
    }

    private IEnumerator ResolveCatchResult(OddClawCatchResult result)
    {
        if (lockClawDuringEvaluation && clawController != null)
            clawController.SetInputEnabled(false);

        bool caughtSomething = result != null && result.caughtSomething;

        if (caughtSomething || countMissAsAttempt)
            _totalAttempts++;

        if (caughtSomething)
        {
            bool isCorrect = result.caughtCorrect;
            string message = isCorrect ? "Correct!" : "Wrong!";
            Color messageColor = isCorrect
                ? new Color(0.1f, 0.85f, 0.2f, 1f)
                : new Color(1f, 0.18f, 0.18f, 1f);

            if (isCorrect)
            {
                _score++;
                _correctCount++;
                _hasCaughtFirstCorrect = true;

                if (audioManager != null)
                    audioManager.PlayCorrect();
                if (clawController != null)
                    clawController.PlaySuccessFeedback();
            }
            else
            {
                _mistakeCount++;
                ApplyDamage(wrongHealthPenalty);

                if (audioManager != null)
                    audioManager.PlayWrong();
                if (clawController != null)
                    clawController.PlayWrongFeedback();
            }

            UpdateTopBar();

            float popupDuration = 0f;
            if (feedbackPopup != null)
            {
                feedbackPopup.Show(message, messageColor);
                popupDuration = feedbackPopup.TotalDuration;
            }

            float itemSequenceDuration = 0f;
            if (result.caughtItem != null)
            {
                itemSequenceDuration = Mathf.Max(0f, evaluationItemHoldBeforeFade) + Mathf.Max(0.01f, evaluationItemFadeDuration);
                yield return result.caughtItem.PlayEvaluationFeedbackAndFade(isCorrect, evaluationItemHoldBeforeFade, evaluationItemFadeDuration);
            }

            if (waitForPopupBeforeContinuing && popupDuration > itemSequenceDuration)
                yield return new WaitForSeconds(popupDuration - itemSequenceDuration);

            float finalDelay = isCorrect ? correctDelay : nextWaveDelay;
            if (finalDelay > 0f)
                yield return new WaitForSeconds(finalDelay);
        }
        else
        {
            // Miss is a retry state: same question, same objects, no wave/score/health/accuracy change by default.
            if (penalizeMiss)
            {
                _mistakeCount++;
                ApplyDamage(missHealthPenalty);
            }

            float popupDuration = 0f;
            if (feedbackPopup != null)
            {
                feedbackPopup.Show("Miss!", new Color(1f, 0.7f, 0.1f, 1f));
                popupDuration = feedbackPopup.TotalDuration;
            }

            if (audioManager != null)
                audioManager.PlayMiss();
            if (clawController != null)
                clawController.PlayMissFeedback();

            if (waitForPopupBeforeContinuing && popupDuration > 0f)
                yield return new WaitForSeconds(popupDuration);
            else
                yield return new WaitForSeconds(0.28f);

            UpdateTopBar();

            if (_currentHealth <= 0)
            {
                GameOver();
                yield break;
            }

            _gameplayActive = true;
            if (clawController != null)
                clawController.SetInputEnabled(true);
            RefreshFirstPickHintForWave();
            yield break;
        }

        UpdateTopBar();

        if (_currentHealth <= 0)
            GameOver();
        else
            StartNextWave();
    }

    private IEnumerator HandleTimeout()
    {
        if (!_gameplayActive || _gameOver)
            yield break;

        _gameplayActive = false;
        HideFirstPickHint(false);
        if (clawController != null)
            clawController.SetInputEnabled(false);

        _totalAttempts++;
        _mistakeCount++;
        ApplyDamage(missHealthPenalty);

        float popupDuration = 0f;
        if (feedbackPopup != null)
        {
            feedbackPopup.Show("Time Up!", new Color(1f, 0.55f, 0.1f, 1f));
            popupDuration = feedbackPopup.TotalDuration;
        }
        if (audioManager != null)
            audioManager.PlayMiss();
        if (clawController != null)
            clawController.PlayMissFeedback();

        if (waitForPopupBeforeContinuing && popupDuration > 0f)
            yield return new WaitForSeconds(popupDuration);

        if (nextWaveDelay > 0f)
            yield return new WaitForSeconds(nextWaveDelay);

        if (_currentHealth <= 0)
            GameOver();
        else
            StartNextWave();
    }

    private void ApplyDamage(int amount)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - Mathf.Max(0, amount));
        UpdateTopBar();
    }

    private void UpdateTopBar()
    {
        if (scoreText != null)
            scoreText.text = "Score " + _score;

        if (healthLabel != null)
            healthLabel.text = "HP " + _currentHealth + "/" + maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = Mathf.Max(1, maxHealth);
            healthSlider.value = _currentHealth;
        }

        if (waveText != null)
            waveText.text = "Wave " + Mathf.Max(1, _wave);

        if (timerLabel != null)
            timerLabel.text = "Time " + Mathf.CeilToInt(Mathf.Max(0f, _remainingWaveTime)) + "s";

        if (timerSlider != null)
        {
            timerSlider.maxValue = Mathf.Max(1f, _currentWaveDuration);
            timerSlider.value = Mathf.Clamp(_remainingWaveTime, 0f, _currentWaveDuration);
        }

        if (speedMultiplierText != null)
        {
            float multiplier = clawController != null ? clawController.SpeedMultiplier : 1f;
            speedMultiplierText.text = FormatSpeedMultiplier(multiplier);
        }
    }

    private string FormatSpeedMultiplier(float multiplier)
    {
        float roundedToTenth = Mathf.Round(multiplier * 10f) / 10f;
        float roundedWhole = Mathf.Round(roundedToTenth);

        if (Mathf.Abs(roundedToTenth - roundedWhole) < 0.01f)
            return roundedWhole.ToString("0") + "X";

        return roundedToTenth.ToString("0.0") + "X";
    }

    private void PauseGame()
    {
        if (_gameOver || !_gameplayActive)
            return;

        _isPaused = true;
        Time.timeScale = 0f;
        HideFirstPickHint(true);
        ShowOnlyPanel(pausePanel);
        if (clawController != null)
            clawController.SetInputEnabled(false);
    }

    private void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        HideAllPanels();
        if (clawController != null)
            clawController.SetInputEnabled(true);
        RefreshFirstPickHintForWave();
    }

    private void GameOver()
    {
        if (_localResultShown)
            return;

        _gameOver = true;
        _gameplayActive = false;
        _localResultShown = true;
        Time.timeScale = 1f;
        HideFirstPickHint(true);

        if (clawController != null)
            clawController.SetInputEnabled(false);

        if (audioManager != null)
            audioManager.PlayGameOver();

        float timeTaken = Mathf.Max(0f, Time.time - _sessionStartTime);
        float accuracy = _totalAttempts > 0 ? Mathf.Clamp01((float)_correctCount / _totalAttempts) : 0f;

        if (resultTitleText != null)
            resultTitleText.text = "Game Over";

        if (resultBodyText != null)
        {
            resultBodyText.text =
                "Score: " + _score + "\n" +
                "Waves Played: " + Mathf.Max(0, _wave) + "\n" +
                "Accuracy: " + Mathf.RoundToInt(accuracy * 100f) + "%\n" +
                "Mistakes: " + _mistakeCount + "\n" +
                "Time: " + Mathf.RoundToInt(timeTaken) + "s";
        }

        ShowOnlyPanel(resultPanel);
    }

    private void ContinueToBloomPostGame()
    {
        float timeTaken = Mathf.Max(0f, Time.time - _sessionStartTime);
        float timeScore = Mathf.Clamp01(1f - (timeTaken / Mathf.Max(1f, expectedMaxTime)));
        float accuracyScore = _totalAttempts > 0
            ? Mathf.Clamp01((float)_correctCount / _totalAttempts)
            : 0f;

        GameEvaluationData eval = new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = accuracyScore,
            mistakeCount = _mistakeCount,
            timeTaken = timeTaken
        };

        HideAllPanels();
        HideFirstPickHint(true);
        OnRewardScreenOpen();

        if (RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPostGame(_skills, eval);
        }
        else
        {
            Debug.LogWarning("RewardManager.Instance not found. Cannot show Bloom post-game screen.");
        }
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

        SceneManager.LoadScene("Loader Scene");
    }

    public void OnRewardScreenOpen()
    {
        if (audioManager != null)
            audioManager.StopBgm();
    }

    private bool WasTapPressed()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            return true;
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Began)
                    continue;

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    continue;

                return true;
            }
        }

        return false;
    }

    private void RefreshFirstPickHintForWave()
    {
        if (!showFirstPickHint || firstPickHintOverlay == null || !_gameplayActive || _gameOver || _isPaused)
        {
            HideFirstPickHint(false);
            return;
        }

        bool shouldShow = showFirstPickHintUntilFirstCorrect ? !_hasCaughtFirstCorrect : _totalAttempts == 0;
        if (shouldShow)
            ShowFirstPickHint();
        else
            HideFirstPickHint(false);
    }

    private void ShowFirstPickHint()
    {
        if (firstPickHintOverlay == null)
            return;

        if (firstPickHintText != null)
            firstPickHintText.text = firstPickHintMessage;

        firstPickHintOverlay.gameObject.SetActive(true);
        firstPickHintOverlay.blocksRaycasts = false;
        firstPickHintOverlay.interactable = false;
        firstPickHintOverlay.DOKill();
        firstPickHintOverlay.transform.DOKill();
        firstPickHintOverlay.alpha = 1f;
        firstPickHintOverlay.transform.localScale = Vector3.one;
        firstPickHintOverlay.transform.DOScale(Vector3.one * firstPickHintBreathScale, firstPickHintBreathDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private void HideFirstPickHint(bool immediate)
    {
        if (firstPickHintOverlay == null)
            return;

        firstPickHintOverlay.DOKill();
        firstPickHintOverlay.transform.DOKill();

        if (immediate)
        {
            firstPickHintOverlay.alpha = 0f;
            firstPickHintOverlay.gameObject.SetActive(false);
            firstPickHintOverlay.transform.localScale = Vector3.one;
            return;
        }

        firstPickHintOverlay.DOFade(0f, 0.12f).OnComplete(() =>
        {
            if (firstPickHintOverlay != null)
            {
                firstPickHintOverlay.gameObject.SetActive(false);
                firstPickHintOverlay.transform.localScale = Vector3.one;
            }
        });
    }

    public void ApplyFontsToAllTexts()
    {
        TMP_Text[] texts = rootCanvas != null
            ? rootCanvas.GetComponentsInChildren<TMP_Text>(true)
            : GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            texts[i].font = primaryFont != null ? primaryFont : texts[i].font;
        }

        if (questionText != null && secondaryFont != null)
            questionText.font = secondaryFont;

        if (feedbackPopup != null)
            feedbackPopup.ApplyFont(secondaryFont != null ? secondaryFont : primaryFont);
    }

    private void ShowOnlyPanel(GameObject panel)
    {
        HideAllPanels();
        HideFirstPickHint(true);
        if (panel != null)
            panel.SetActive(true);
    }

    private void HideAllPanels()
    {
        SetPanelActive(loadingPanel, false);
        SetPanelActive(howToPlayPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(resultPanel, false);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private void SetTemplatesHidden()
    {
        if (textItemTemplate != null)
            textItemTemplate.gameObject.SetActive(false);
        if (imageItemTemplate != null)
            imageItemTemplate.gameObject.SetActive(false);
    }

    private void ClearItems()
    {
        for (int i = _spawnedItems.Count - 1; i >= 0; i--)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }
}
