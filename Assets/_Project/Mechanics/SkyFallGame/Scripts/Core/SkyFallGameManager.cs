using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;
using UnityEngine.UI;

public enum SkyFallGameOverMode
{
    TimeLimited,
    LifeLimited
}

public enum SkyFallDropSpawnMode
{
    SingleActiveItem,
    ContinuousInterval
}

public enum SkyFallCarrierDirectionVisualMode
{
    None,
    FlipScaleX,
    RotateZ
}

public class SkyFallGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    public event Action OnGameStarted;
    public event Action OnGameEnded;

    [Header("Bloom Reward System")]
    [Tooltip("Expected max play time used for normalized Bloom time score. If <= 0, Time Limit Seconds is used.")]
    public float expectedMaxTime = 60f;

    private List<SkillEntry> _skills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Remember, 100f),
        new SkillEntry(BloomSkillType.Understand, 100f),
    };

    private float _startTime;
    private bool _preGameComplete;

    [Header("Content")]
    public SkyFallContentProviderBase contentProvider;

    [Header("Core References")]
    public RectTransform playArea;
    public RectTransform carrier;
    public RectTransform carrierDirectionVisual;
    public RectTransform basket;
    public SkyFallBasketDrag basketDrag;
    public RectTransform itemParent;
    public RectTransform trailFxLayer;
    public SkyFallFallingItem itemPrefab;

    [Header("HUD")]
    public TMP_Text questionText;
    public TMP_Text scoreText;
    public GameObject timerGroup;
    public TMP_Text timerText;
    public GameObject livesGroup;
    public Transform livesIconParent;
    public Image lifeIconPrefab;

    [Header("Feedback")]
    public TMP_Text feedbackText;
    public RectTransform feedbackCard;

    [Header("Result UI")]
    public GameObject resultPanel;
    public SkyFallUiPanelAnimator resultPanelAnimator;
    public TMP_Text resultTitleText;
    public TMP_Text resultScoreText;
    public Button restartButton;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioClip backgroundMusic;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip gameOverClip;
    public AudioClip dropClip;
    public bool playBackgroundMusicOnSceneStart = true;
    [Range(0f, 1f)] public float musicVolume = 0.45f;

    [Header("Game Over Mode")]
    public SkyFallGameOverMode gameOverMode = SkyFallGameOverMode.TimeLimited;
    public float timeLimitSeconds = 60f;
    public int startingLives = 3;
    public bool loseLifeOnWrongCatch = true;
    public bool loseLifeOnMissedCorrect = false;

    [Header("Score Rules")]
    public int correctScore = 10;
    public int wrongPenalty = -5;
    public int missCorrectPenalty = 0;

    [Header("Drop Spawn")]
    public SkyFallDropSpawnMode dropSpawnMode = SkyFallDropSpawnMode.SingleActiveItem;
    public float singleDropRespawnDelay = 0.16f;
    public float startSpawnInterval = 1.2f;
    public float endSpawnInterval = 0.5f;

    [Header("Responsive Falling Timing")]
    public float easiestReachTime = 3.2f;
    public float hardestReachTime = 1.35f;
    public float minimumFallDistance = 350f;
    public float basketCatchZoneYOffset = 20f;
    public AnimationCurve reachTimeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Carrier Movement")]
    public float carrierSpeed = 360f;
    public float carrierTopMinOffset = 105f;
    public float carrierTopMaxOffset = 210f;

    [Header("Carrier Direction Visual")]
    public SkyFallCarrierDirectionVisualMode carrierDirectionVisualMode = SkyFallCarrierDirectionVisualMode.FlipScaleX;
    public float leftToRightRotationZ = 0f;
    public float rightToLeftRotationZ = 180f;

    [Header("Caught Item Animation")]
    public bool animateCorrectItemIntoBasket = true;
    public float correctItemAbsorbDuration = 0.28f;
    public float correctItemAbsorbEndScale = 0.18f;
    public Vector2 correctItemBasketOffset = new Vector2(0f, 18f);
    [Range(0f, 1f)] public float basketPunchAtAbsorbProgress = 0.55f;

    public bool animateWrongCaughtItemReject = true;
    public float wrongItemRejectDuration = 0.18f;
    public float wrongItemRejectMoveUp = 48f;
    public float wrongItemRejectEndScale = 0.55f;

    [Header("Basket Animation")]
    public bool animateBasket = true;
    public float basketPunchScale = 1.12f;
    public float basketPunchDuration = 0.16f;
    public float basketShakeAngle = 7f;
    public float basketShakeDuration = 0.20f;

    [Header("HUD Animation")]
    public bool animateHud = true;
    public float scorePulseScale = 1.12f;
    public float scorePulseDuration = 0.14f;
    public float lowTimeWarningSeconds = 10f;
    public bool pulseTimerWhenLow = true;

    [Header("Carrier Animation")]
    public bool carrierSoftBob = true;
    public float carrierBobAmplitude = 8f;
    public float carrierBobSpeed = 2.2f;

    [Header("Debug")]
    public bool autoStart = false;

    private readonly List<SkyFallFallingItem> activeItems = new List<SkyFallFallingItem>();
    private readonly List<Image> lifeIcons = new List<Image>();

    private int score;
    private int correctCaught;
    private int wrongCaught;
    private int missedCorrect;
    private int livesLeft;
    private float timeLeft;
    private float elapsedTime;
    private float spawnTimer;
    private bool isRunning;
    private bool isResolvingItem;
    private bool gameplaySuspendedForTutorial;
    private int carrierDirection = 1;
    private float carrierBaseY;
    private bool hasCarrierBaseY;
    private Coroutine feedbackRoutine;
    private Coroutine basketRoutine;
    private Coroutine scorePulseRoutine;
    private Coroutine timerPulseRoutine;

    public bool IsRunning
    {
        get { return isRunning; }
    }

    public bool IsGameplaySuspendedForTutorial
    {
        get { return gameplaySuspendedForTutorial; }
    }

    public bool IsBloomPreGameComplete
    {
        get { return _preGameComplete; }
    }

    public IEnumerator RunBloomPreGameFlow()
    {
        _preGameComplete = false;
        SetGameplayInputEnabled(false);

        if (RewardManager.Instance == null)
        {
            Debug.LogWarning("SkyFallGameManager: RewardManager.Instance not found. Continuing without Bloom pre-game panel.");
            _preGameComplete = true;
            yield break;
        }

        RewardManager.Instance.ShowPreGame(_skills);
        yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);

        _preGameComplete = true;
    }

    public void ContinueToBloomReward()
    {
        SetGameplayInputEnabled(false);

        if (resultPanelAnimator != null)
            resultPanelAnimator.Hide();
        else if (resultPanel != null)
            resultPanel.SetActive(false);

        ShowBloomPostGame();
    }

    public void OnPlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHome()
    {
        SceneManager.LoadScene("Loader Scene");
    }

    public void OnRewardScreenOpen()
    {
        if (musicSource != null)
            musicSource.Stop();

        if (sfxSource != null)
            sfxSource.Stop();
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        CacheBasketDrag();

        if (basketDrag != null)
            basketDrag.enabled = enabled;
    }

    public void SetTutorialGameplayHold(bool hold)
    {
        if (gameplaySuspendedForTutorial == hold)
            return;

        gameplaySuspendedForTutorial = hold;

        if (!hold && isRunning)
        {
            _startTime = Time.time;
            spawnTimer = 0f;
            StartCarrierTrip();
        }
    }

    private void CacheBasketDrag()
    {
        if (basketDrag != null)
            return;

        if (playArea != null)
            basketDrag = playArea.GetComponent<SkyFallBasketDrag>();

        if (basketDrag == null)
            basketDrag = FindObjectOfType<SkyFallBasketDrag>(true);
    }

    private void ShowBloomPostGame()
    {
        if (RewardManager.Instance == null)
        {
            Debug.LogWarning("SkyFallGameManager: RewardManager.Instance not found. Cannot show Bloom post-game reward.");
            return;
        }

        float timeTaken = Mathf.Max(0f, Time.time - _startTime);
        float expected = expectedMaxTime > 0f ? expectedMaxTime : Mathf.Max(1f, timeLimitSeconds);
        float timeScore = Mathf.Clamp01(1f - (timeTaken / expected));

        int totalQuestions = correctCaught + wrongCaught + missedCorrect;
        float accuracyScore = totalQuestions > 0 ? (float)correctCaught / totalQuestions : 0f;

        GameEvaluationData eval = new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = Mathf.Clamp01(accuracyScore),
            mistakeCount = wrongCaught + missedCorrect,
            timeTaken = timeTaken
        };

        RewardManager.Instance.ShowPostGame(_skills, eval);
    }


    private void Awake()
    {
        if (contentProvider == null)
            contentProvider = GetComponent<SkyFallContentProviderBase>();

        CacheBasketDrag();
        SetGameplayInputEnabled(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(BeginGame);
    }

    private void Start()
    {
        PrepareLivesUI();
        ApplyModeUI();
        UpdateQuestionText();
        UpdateHUD();
        HideResultImmediate();

        if (playBackgroundMusicOnSceneStart)
            PlayBackgroundMusic();

        if (autoStart)
            BeginGame();
    }

    private void Update()
    {
        if (!isRunning)
        {
            ApplyCarrierBob();
            return;
        }

        if (gameplaySuspendedForTutorial)
        {
            // Keep the scene visually alive during the practice catch.
            // Real timer updates and spawning remain suspended below this return.
            MoveCarrier(Time.deltaTime);
            UpdateHUD();
            return;
        }

        float deltaTime = Time.deltaTime;
        elapsedTime += deltaTime;

        if (gameOverMode == SkyFallGameOverMode.TimeLimited)
        {
            timeLeft -= deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                EndGame();
                return;
            }
        }

        MoveCarrier(deltaTime);
        HandleSpawning(deltaTime);
        UpdateFallingItems(deltaTime);
        UpdateHUD();
    }

    public void BeginGame()
    {
        if (contentProvider == null)
        {
            Debug.LogWarning("SkyFallGameManager: No content provider assigned.");
            return;
        }

        ClearActiveItems();

        score = 0;
        correctCaught = 0;
        wrongCaught = 0;
        missedCorrect = 0;
        livesLeft = Mathf.Max(1, startingLives);
        timeLeft = Mathf.Max(1f, timeLimitSeconds);
        elapsedTime = 0f;
        spawnTimer = 0f;
        isRunning = true;
        isResolvingItem = false;
        gameplaySuspendedForTutorial = false;
        _startTime = Time.time;

        SetGameplayInputEnabled(true);
        contentProvider.OnGameStarted();

        ApplyModeUI();
        PrepareLivesUI();
        HideResultImmediate();
        UpdateQuestionText();
        StartCarrierTrip();
        UpdateHUD();

        OnGameStarted?.Invoke();
    }

    private void HandleSpawning(float deltaTime)
    {
        if (isResolvingItem)
            return;

        if (dropSpawnMode == SkyFallDropSpawnMode.SingleActiveItem)
        {
            if (activeItems.Count == 0)
            {
                spawnTimer -= deltaTime;
                if (spawnTimer <= 0f)
                    SpawnFallingItem();
            }

            return;
        }

        spawnTimer -= deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnFallingItem();
            spawnTimer = GetCurrentSpawnInterval();
        }
    }

    private void SpawnFallingItem()
    {
        if (contentProvider == null || itemPrefab == null || itemParent == null || carrier == null)
            return;

        SkyFallDropData data = contentProvider.GenerateDrop(GetContext());

        SkyFallFallingItem item = Instantiate(itemPrefab, itemParent);
        item.gameObject.SetActive(true);
        item.Setup(data, trailFxLayer);

        Vector2 spawnPosition = carrier.anchoredPosition;
        spawnPosition.y -= 62f;
        item.RectTransform.anchoredPosition = spawnPosition;

        activeItems.Add(item);
        PlayClip(dropClip);

        if (dropSpawnMode == SkyFallDropSpawnMode.SingleActiveItem)
            spawnTimer = singleDropRespawnDelay;
    }

    private void UpdateFallingItems(float deltaTime)
    {
        if (playArea == null || basket == null)
            return;

        float bottomLimit = playArea.rect.yMin - 140f;

        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            SkyFallFallingItem item = activeItems[i];

            if (item == null)
            {
                activeItems.RemoveAt(i);
                continue;
            }

            float reachTime = GetCurrentReachTime();
            float fallSpeed = CalculateFallSpeed(item.RectTransform.anchoredPosition.y, reachTime);

            bool wentOut = item.Tick(fallSpeed, deltaTime, bottomLimit, reachTime);

            if (IsOverlapping(item.CatchRect, basket))
            {
                activeItems.RemoveAt(i);
                StartCoroutine(ResolveCaughtItem(item));
                if (dropSpawnMode == SkyFallDropSpawnMode.SingleActiveItem)
                    return;
                continue;
            }

            if (wentOut)
            {
                activeItems.RemoveAt(i);
                ResolveMissedItem(item);
                if (dropSpawnMode == SkyFallDropSpawnMode.SingleActiveItem)
                    return;
            }
        }
    }

    private IEnumerator ResolveCaughtItem(SkyFallFallingItem item)
    {
        if (item == null)
            yield break;

        isResolvingItem = true;

        bool wasCorrect = item.Data.isCorrect;
        bool shouldEndAfterResolve = ProcessCatch(item.Data);

        if (wasCorrect && animateCorrectItemIntoBasket && basket != null)
        {
            StartCoroutine(DelayedBasketPunch(correctItemAbsorbDuration * basketPunchAtAbsorbProgress));
            yield return item.AnimateCorrectAbsorb(basket, correctItemBasketOffset, correctItemAbsorbDuration, correctItemAbsorbEndScale);
        }
        else if (!wasCorrect && animateWrongCaughtItemReject)
        {
            PlayBasketWrongAnimation();
            yield return item.AnimateWrongReject(wrongItemRejectDuration, wrongItemRejectMoveUp, wrongItemRejectEndScale);
        }
        else
        {
            if (wasCorrect)
                PlayBasketCorrectAnimation();
            else
                PlayBasketWrongAnimation();
        }

        if (item != null)
            Destroy(item.gameObject);

        isResolvingItem = false;
        spawnTimer = singleDropRespawnDelay;

        if (shouldEndAfterResolve)
            EndGame();
    }

    private bool ProcessCatch(SkyFallDropData data)
    {
        bool shouldEnd = false;

        if (data.isCorrect)
        {
            score += correctScore;
            correctCaught++;
            contentProvider.OnCorrectCatch(data);
            PlayClip(correctClip);
            ShowFeedback("+" + correctScore);
            PulseScore();

            if (data.audioClip != null)
                PlayClip(data.audioClip);
        }
        else
        {
            score += wrongPenalty;
            wrongCaught++;
            contentProvider.OnWrongCatch(data);
            PlayClip(wrongClip);
            ShowFeedback(wrongPenalty.ToString());
            PulseScore();

            if (gameOverMode == SkyFallGameOverMode.LifeLimited && loseLifeOnWrongCatch)
            {
                LoseLife();
                shouldEnd = livesLeft <= 0;
            }
        }

        UpdateQuestionText();
        UpdateHUD();

        return shouldEnd;
    }

    private void ResolveMissedItem(SkyFallFallingItem item)
    {
        if (item == null)
            return;

        bool shouldEnd = false;

        if (item.Data.isCorrect)
        {
            missedCorrect++;
            contentProvider.OnCorrectMissed(item.Data);

            if (missCorrectPenalty != 0)
            {
                score += missCorrectPenalty;
                ShowFeedback(missCorrectPenalty.ToString());
                PulseScore();
            }

            if (gameOverMode == SkyFallGameOverMode.LifeLimited && loseLifeOnMissedCorrect)
            {
                LoseLife();
                shouldEnd = livesLeft <= 0;
            }
        }

        Destroy(item.gameObject);
        spawnTimer = singleDropRespawnDelay;

        if (shouldEnd)
            EndGame();
    }

    private void LoseLife()
    {
        livesLeft = Mathf.Max(0, livesLeft - 1);
        RefreshLifeIcons();

        if (animateHud && livesLeft >= 0 && livesLeft < lifeIcons.Count && lifeIcons[livesLeft] != null)
            StartCoroutine(PulseRect(lifeIcons[livesLeft].rectTransform, 0.82f, 0.16f));
    }

    private void MoveCarrier(float deltaTime)
    {
        if (carrier == null || playArea == null)
            return;

        if (!hasCarrierBaseY)
        {
            carrierBaseY = carrier.anchoredPosition.y;
            hasCarrierBaseY = true;
        }

        Vector2 position = carrier.anchoredPosition;
        position.x += carrierDirection * carrierSpeed * deltaTime;

        // Important:
        // Keep movement Y locked to the route base Y.
        // Bob animation must never rewrite the route base Y, otherwise Y drifts out of screen.
        position.y = carrierBaseY;

        float halfWidth = Mathf.Max(0f, playArea.rect.width * 0.5f - carrier.rect.width * 0.5f);

        if (position.x > halfWidth || position.x < -halfWidth)
        {
            StartCarrierTrip();
            return;
        }

        carrier.anchoredPosition = position;
        ApplyCarrierBob();
    }

    private void StartCarrierTrip()
    {
        if (carrier == null || playArea == null)
            return;

        bool leftToRight = UnityEngine.Random.value > 0.5f;
        carrierDirection = leftToRight ? 1 : -1;

        float halfWidth = Mathf.Max(0f, playArea.rect.width * 0.5f - carrier.rect.width * 0.5f);
        float startX = leftToRight ? -halfWidth : halfWidth;

        float topY = playArea.rect.yMax;
        float minY = topY - carrierTopMaxOffset;
        float maxY = topY - carrierTopMinOffset;
        float y = UnityEngine.Random.Range(minY, maxY);

        carrierBaseY = y;
        hasCarrierBaseY = true;
        carrier.anchoredPosition = new Vector2(startX, y);

        ApplyCarrierDirectionVisual(leftToRight);
    }

    private void ApplyCarrierDirectionVisual(bool leftToRight)
    {
        if (carrierDirectionVisual == null)
            return;

        if (carrierDirectionVisualMode == SkyFallCarrierDirectionVisualMode.FlipScaleX)
        {
            Vector3 scale = carrierDirectionVisual.localScale;
            scale.x = leftToRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            carrierDirectionVisual.localScale = scale;
            return;
        }

        if (carrierDirectionVisualMode == SkyFallCarrierDirectionVisualMode.RotateZ)
        {
            float z = leftToRight ? leftToRightRotationZ : rightToLeftRotationZ;
            carrierDirectionVisual.localEulerAngles = new Vector3(0f, 0f, z);
        }
    }

    private void ApplyCarrierBob()
    {
        if (!carrierSoftBob || carrier == null || !hasCarrierBaseY)
            return;

        Vector2 position = carrier.anchoredPosition;

        // Visual bob only around locked base Y. Do not feed bobbed Y back into carrierBaseY.
        position.y = carrierBaseY + Mathf.Sin(Time.time * carrierBobSpeed) * carrierBobAmplitude;

        carrier.anchoredPosition = position;
    }

    private float CalculateFallSpeed(float spawnY, float reachTime)
    {
        if (basket == null)
            return minimumFallDistance / Mathf.Max(0.1f, reachTime);

        float targetY = basket.anchoredPosition.y + basketCatchZoneYOffset;
        float distance = Mathf.Abs(spawnY - targetY);
        distance = Mathf.Max(minimumFallDistance, distance);

        return distance / Mathf.Max(0.1f, reachTime);
    }

    private float GetCurrentReachTime()
    {
        float t = reachTimeCurve != null ? reachTimeCurve.Evaluate(GetProgress01()) : GetProgress01();
        return Mathf.Lerp(easiestReachTime, hardestReachTime, t);
    }

    private float GetCurrentSpawnInterval()
    {
        return Mathf.Lerp(startSpawnInterval, endSpawnInterval, GetProgress01());
    }

    private float GetProgress01()
    {
        if (gameOverMode == SkyFallGameOverMode.TimeLimited)
            return Mathf.Clamp01(1f - timeLeft / Mathf.Max(1f, timeLimitSeconds));

        return Mathf.Clamp01(elapsedTime / 90f);
    }

    private SkyFallDropContext GetContext()
    {
        return new SkyFallDropContext
        {
            score = score,
            correctCaught = correctCaught,
            wrongCaught = wrongCaught,
            missedCorrect = missedCorrect,
            elapsedTime = elapsedTime,
            progress01 = GetProgress01()
        };
    }

    private void UpdateQuestionText()
    {
        if (questionText != null && contentProvider != null)
            questionText.text = contentProvider.GetPromptText(GetContext());
    }

    private void ApplyModeUI()
    {
        if (timerGroup != null)
            timerGroup.SetActive(gameOverMode == SkyFallGameOverMode.TimeLimited);

        if (livesGroup != null)
            livesGroup.SetActive(gameOverMode == SkyFallGameOverMode.LifeLimited);
    }

    private void UpdateHUD()
    {
        if (scoreText != null)
            scoreText.text = score.ToString();

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();

        if (pulseTimerWhenLow &&
            gameOverMode == SkyFallGameOverMode.TimeLimited &&
            timeLeft <= lowTimeWarningSeconds &&
            isRunning &&
            timerPulseRoutine == null &&
            timerText != null)
        {
            timerPulseRoutine = StartCoroutine(TimerPulseRoutine());
        }
    }

    private void PrepareLivesUI()
    {
        if (livesIconParent == null || lifeIconPrefab == null)
            return;

        for (int i = livesIconParent.childCount - 1; i >= 0; i--)
        {
            Transform child = livesIconParent.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        lifeIcons.Clear();

        int count = Mathf.Max(1, startingLives);

        for (int i = 0; i < count; i++)
        {
            Image icon = Instantiate(lifeIconPrefab, livesIconParent);
            icon.gameObject.SetActive(true);
            icon.name = "LifeIcon_" + (i + 1).ToString("00");
            lifeIcons.Add(icon);
        }

        RefreshLifeIcons();
    }

    private void RefreshLifeIcons()
    {
        for (int i = 0; i < lifeIcons.Count; i++)
        {
            if (lifeIcons[i] == null)
                continue;

            bool active = i < livesLeft;
            lifeIcons[i].color = active ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }

    private void EndGame()
    {
        if (!isRunning && resultPanel != null && resultPanel.activeSelf)
            return;

        isRunning = false;
        isResolvingItem = false;
        gameplaySuspendedForTutorial = false;
        SetGameplayInputEnabled(false);
        ClearActiveItems();
        PlayClip(gameOverClip);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = "Game Over";

        if (resultScoreText != null)
        {
            resultScoreText.text =
                "Score: " + score +
                "\nCorrect: " + correctCaught +
                "\nWrong: " + wrongCaught +
                "\nTime Played: " + Mathf.CeilToInt(elapsedTime) + "s";
        }

        if (resultPanelAnimator != null)
            resultPanelAnimator.Show();

        OnGameEnded?.Invoke();
    }

    private void HideResultImmediate()
    {
        if (resultPanelAnimator != null)
            resultPanelAnimator.HideImmediate();
        else if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void PlayBackgroundMusic()
    {
        if (musicSource == null || backgroundMusic == null)
            return;

        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void PlayClip(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    private bool IsOverlapping(RectTransform a, RectTransform b)
    {
        if (a == null || b == null)
            return false;

        Vector3[] aCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];

        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);

        Rect aRect = new Rect(aCorners[0].x, aCorners[0].y, aCorners[2].x - aCorners[0].x, aCorners[2].y - aCorners[0].y);
        Rect bRect = new Rect(bCorners[0].x, bCorners[0].y, bCorners[2].x - bCorners[0].x, bCorners[2].y - bCorners[0].y);

        return aRect.Overlaps(bRect);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(FeedbackRoutine(message));
    }

    private IEnumerator FeedbackRoutine(string message)
    {
        feedbackText.gameObject.SetActive(true);

        if (feedbackCard != null)
            feedbackCard.gameObject.SetActive(true);

        feedbackText.text = message;

        RectTransform target = feedbackCard != null ? feedbackCard : feedbackText.rectTransform;
        Vector2 startPos = target.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(0f, 50f);

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null)
            group = target.gameObject.AddComponent<CanvasGroup>();

        group.alpha = 1f;
        target.localScale = Vector3.one * 0.88f;

        float duration = 0.55f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            target.anchoredPosition = Vector2.Lerp(startPos, endPos, eased);
            target.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, eased);
            group.alpha = Mathf.Lerp(1f, 0f, Mathf.SmoothStep(0.35f, 1f, t));

            yield return null;
        }

        target.anchoredPosition = startPos;

        if (feedbackCard != null)
            feedbackCard.gameObject.SetActive(false);

        feedbackText.gameObject.SetActive(false);
    }

    private void PlayBasketCorrectAnimation()
    {
        if (!animateBasket || basket == null)
            return;

        if (basketRoutine != null)
            StopCoroutine(basketRoutine);

        basketRoutine = StartCoroutine(PunchScale(basket, basketPunchScale, basketPunchDuration));
    }

    private void PlayBasketWrongAnimation()
    {
        if (!animateBasket || basket == null)
            return;

        if (basketRoutine != null)
            StopCoroutine(basketRoutine);

        basketRoutine = StartCoroutine(ShakeRotation(basket, basketShakeAngle, basketShakeDuration));
    }

    private IEnumerator DelayedBasketPunch(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayBasketCorrectAnimation();
    }

    private void PulseScore()
    {
        if (!animateHud || scoreText == null)
            return;

        if (scorePulseRoutine != null)
            StopCoroutine(scorePulseRoutine);

        scorePulseRoutine = StartCoroutine(PulseRect(scoreText.rectTransform, scorePulseScale, scorePulseDuration));
    }

    private IEnumerator PulseRect(RectTransform rect, float scale, float duration)
    {
        if (rect == null)
            yield break;

        rect.localScale = Vector3.one;

        float half = duration * 0.5f;
        float timer = 0f;

        while (timer < half)
        {
            timer += Time.deltaTime;
            rect.localScale = Vector3.one * Mathf.Lerp(1f, scale, Mathf.Clamp01(timer / half));
            yield return null;
        }

        timer = 0f;

        while (timer < half)
        {
            timer += Time.deltaTime;
            rect.localScale = Vector3.one * Mathf.Lerp(scale, 1f, Mathf.Clamp01(timer / half));
            yield return null;
        }

        rect.localScale = Vector3.one;
    }

    private IEnumerator PunchScale(RectTransform rect, float scale, float duration)
    {
        yield return PulseRect(rect, scale, duration);
    }

    private IEnumerator ShakeRotation(RectTransform rect, float angle, float duration)
    {
        if (rect == null)
            yield break;

        Quaternion original = Quaternion.identity;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float wave = Mathf.Sin(t * Mathf.PI * 4f);
            rect.localRotation = Quaternion.Euler(0f, 0f, wave * angle * (1f - t));
            yield return null;
        }

        rect.localRotation = original;
    }

    private IEnumerator TimerPulseRoutine()
    {
        while (isRunning &&
               gameOverMode == SkyFallGameOverMode.TimeLimited &&
               timeLeft <= lowTimeWarningSeconds &&
               timeLeft > 0f &&
               timerText != null)
        {
            yield return PulseRect(timerText.rectTransform, 1.10f, 0.22f);
            yield return new WaitForSeconds(0.28f);
        }

        timerPulseRoutine = null;
    }

    private void ClearActiveItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i] != null)
                Destroy(activeItems[i].gameObject);
        }

        activeItems.Clear();
    }
}
