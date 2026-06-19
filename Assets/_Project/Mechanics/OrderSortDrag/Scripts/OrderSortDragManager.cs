using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using RewardSystem;

public enum OrderSortRule
{
    AlphabeticalAZ,
    AlphabeticalZA,
    ShortToLong,
    LongToShort,
    NumberSmallToLarge,
    NumberLargeToSmall,
    ManualOrder
}

public enum OrderSortComparisonMode
{
    ExactText,
    NumericValue
}

public enum OrderSortBankPlacementMode
{
    GridLayoutGroup,
    OrganicRandom
}

public enum OrderSortContentMode
{
    TextOnly,
    ImageOnly
}

[Serializable]
public class OrderSortItemData
{
    [Tooltip("Hidden answer/check value. Text mode displays this. Image mode uses this only for checking/sorting.")]
    public string value;

    [Tooltip("Used only in ImageOnly mode.")]
    public Sprite image;
}

[Serializable]
public class OrderSortQuestion
{
    [TextArea]
    public string questionText = "Arrange the items in the correct order.";

    public OrderSortRule sortRule = OrderSortRule.AlphabeticalAZ;

    [Tooltip("ExactText checks the exact card value/order. NumericValue accepts cards with the same numeric value in the same slot, useful for math expressions like 9-4 and 20/4.")]
    public OrderSortComparisonMode comparisonMode = OrderSortComparisonMode.ExactText;

    [Tooltip("Full item pool for this question. Manager can randomly choose a smaller count from this list.")]
    public List<OrderSortItemData> items = new List<OrderSortItemData>();

    [HideInInspector]
    public List<string> manualCorrectOrder = new List<string>(); // Legacy field kept only so older scenes do not lose serialized data. ManualOrder now uses Items order as the answer key.

    public AudioClip questionAudio;
}

public class OrderSortDragManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Question Data")]
    public List<OrderSortQuestion> questions = new List<OrderSortQuestion>();
    public bool shuffleQuestions;
    public bool shuffleCards = true;

    [Tooltip("0 means use all questions.")]
    public int questionLimit;

    [Tooltip("0 means use all items from the question. Otherwise randomly choose this many items.")]
    public int objectsPerQuestion = 5;

    [Tooltip("If enabled, selected objects are picked randomly from the question item pool.")]
    public bool randomizeObjectsPerQuestion = true;

    [Header("Gameplay")]
    public bool allowSwap = true;

    [Tooltip("If false, placed cards cannot be dropped back into the basket/source container.")]
    public bool allowReturnToBasket;

    public bool autoStart;

    [Header("Title")]
    public string gameTitle = "Order Sort";
    public TMP_Text gameTitleText;

    [Header("Timer")]
    [Tooltip("Recommended. Time = secondsPerObject * selected object count.")]
    public bool usePerObjectTimer = true;

    public float secondsPerObject = 6f;
    public float minimumQuestionTime = 12f;
    public float maximumQuestionTime = 90f;

    [Header("Scoring")]
    public int scorePerCorrectPosition = 10;
    public int penaltyPerWrongPosition = -20;
    public int scorePerEmptySlot = 0;
    public bool allowNegativeScore;

    [Header("Answer Comparison")]
    [Tooltip("Used only when a question uses Comparison Mode = NumericValue. Allows equivalent math expressions like 9-4 and 20/4 to be accepted in either equal-value position.")]
    public float numericComparisonTolerance = 0.001f;

    [Header("Scoring Animation")]
    public bool autoShowResultAfterFinalQuestion = true;
    public float betweenSlotCheckDelay = 0.35f;
    public float checkingFlashDuration = 0.6f;
    public float resultColorLerpDuration = 0.45f;
    public float resultHoldDuration = 0.6f;
    public float resultFadeOutDuration = 0.25f;

    [Tooltip("Legacy option. If true, wrong slots keep the wrong color overlay visible after checking.")]
    public bool keepWrongOverlayVisible = true;

    [Tooltip("Recommended. If enabled, every slot result overlay stays visible after checking: correct, wrong, and empty/skipped.")]
    public bool keepAllResultOverlaysVisible = true;

    [Tooltip("Updates score text immediately after each slot result popup, instead of only after all slots are checked.")]
    public bool updateScoreDuringSlotEvaluation = true;

    [Header("Score Popup Placement")]
    [Tooltip("Recommended. Shows +10/-20/0 in the middle of the screen instead of over the card/slot, so result overlays do not hide it.")]
    public bool spawnScorePopupAtCenter = true;

    [Tooltip("Optional anchor. If empty, popup appears at the center of Drag Layer.")]
    public RectTransform scorePopupCenterAnchor;

    public Vector2 scorePopupCenterOffset = new Vector2(0f, 30f);
    public float scorePopupStackSpacing = 42f;

    [Header("Text Card Pastel Colors")]
    public bool useRandomTextCardColors = true;
    public List<Color> textCardPastelColors = new List<Color>
    {
        new Color(1f, 0.86f, 0.71f, 1f),
        new Color(0.86f, 0.94f, 1f, 1f),
        new Color(0.89f, 1f, 0.86f, 1f),
        new Color(1f, 0.90f, 0.96f, 1f),
        new Color(0.95f, 0.90f, 1f, 1f),
    };

    [Header("Feedback Overlay Colors")]
    public Color checkingOverlayColor = new Color(1f, 0.55f, 0.05f, 0.72f);
    public Color correctOverlayColor = new Color(0.1f, 1f, 0.25f, 0.68f);
    public Color wrongOverlayColor = new Color(1f, 0.12f, 0.1f, 0.72f);
    public Color emptyOverlayColor = new Color(0.65f, 0.65f, 0.65f, 0.58f);

    [Header("Content Mode")]
    [Tooltip("Strict mode: TextOnly uses rectangular word cards. ImageOnly uses square object cards. No mixed cards.")]
    public OrderSortContentMode contentMode = OrderSortContentMode.TextOnly;

    [Header("Card And Slot Size")]
    public Vector2 textCardSize = new Vector2(190f, 74f);
    public Vector2 textSlotSize = new Vector2(190f, 90f);
    public Vector2 imageCardSize = new Vector2(116f, 116f);
    public Vector2 imageSlotSize = new Vector2(128f, 128f);

    [Header("Responsive Slots")]
    [Tooltip("Slots resize inside their parent layout. Less objects = larger slots, more objects = smaller slots.")]
    public bool useResponsiveSlots = true;
    public Vector2 minTextSlotSize = new Vector2(90f, 70f);
    public Vector2 minImageSlotSize = new Vector2(82f, 82f);

    [Header("Bank Placement")]
    public OrderSortBankPlacementMode bankPlacementMode = OrderSortBankPlacementMode.OrganicRandom;
    public Vector2 organicBankPadding = new Vector2(80f, 55f);
    public float organicRotationRange = 7f;
    public Vector2 organicScaleRange = new Vector2(0.95f, 1.05f);
    public bool avoidOverlapInOrganicBank = true;
    public int maxOrganicPlacementAttempts = 80;

    [Header("Theme Fonts")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;

    [Header("How To Play")]
    public bool showHowToPlayBeforeGame = true;

    [TextArea]
    public string howToPlayMessage = "Drag cards into correct order. You can swap cards already placed in slots.";

    public List<Sprite> howToPlayImages = new List<Sprite>();
    public Image howToPlayImageView;
    public TMP_Text howToPlayText;
    public TMP_Text howToPlayCounterText;
    public Button howToPrevButton;
    public Button howToNextButton;

    [Header("Scene Layout Roots")]
    public RectTransform bankParent;
    public RectTransform slotsParent;
    public RectTransform dragLayer;

    [Header("Scene Templates - Not Prefabs")]
    public RectTransform cardSceneTemplate;
    public RectTransform slotSceneTemplate;
    public TMP_Text scorePopupSceneTemplate;

    [Header("UI Text")]
    public TMP_Text questionText;
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text progressText;
    public TMP_Text feedbackText;
    public TMP_Text resultText;

    [Header("Buttons")]
    public Button checkButton;
    public Button nextButton;
    public Button continueButton;
    public Button pauseButton;
    public Button resumeButton;
    public Button restartButton;
    public Button howToPlayOpenButton;
    public Button howToPrimaryButton;
    public TMP_Text howToPrimaryButtonText;

    [Header("Panels")]
    public GameObject howToPlayPanel;
    public GameObject pausePanel;
    public GameObject resultPanel;

    [Header("SFX Audio")]
    public AudioSource sfxSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;
    public AudioClip dropSfx;
    public AudioClip timeoutSfx;
    public AudioClip clickSfx;

    [Header("Background Music")]
    public AudioSource bgmSource;
    public AudioClip backgroundMusicClip;
    public bool playBgmOnGameStart = true;
    public bool loopBackgroundMusic = true;
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.4f;
    public bool pauseBgmWithGamePause = true;
    public bool stopBgmOnRewardScreen = true;

    [Header("Bloom Reward Integration")]
    public bool useBloomRewardSystem = true;
    public string homeSceneName = "Loader Scene";

    private List<SkillEntry> _skills = new List<SkillEntry>
    {
        new SkillEntry(BloomSkillType.Remember, 100f),
        new SkillEntry(BloomSkillType.Understand, 100f),
    };

    private readonly List<OrderSortDropSlot> activeSlots = new List<OrderSortDropSlot>();
    private readonly List<OrderSortDragItem> activeItems = new List<OrderSortDragItem>();
    private readonly List<OrderSortQuestion> runtimeQuestions = new List<OrderSortQuestion>();
    private readonly List<Rect> usedOrganicBankRects = new List<Rect>();

    private List<OrderSortItemData> currentItems = new List<OrderSortItemData>();
    private List<string> currentCorrectOrder = new List<string>();
    private int currentQuestionIndex;
    private int score;
    private float currentTimer;
    private bool gameRunning;
    private bool questionLocked;
    private int howToIndex;
    private float inputUnlockTime;
    private float gameStartTime;
    private float expectedMaxTime;
    private int evaluatedSlotCount;
    private int correctSlotCount;
    private int currentScorePopupIndex;
    private int wrongSlotCount;
    private bool postRewardShown;

    public bool IsGameInputEnabled => CanAcceptGameplayInput();

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        ValidateRequiredReferences();
        HideSceneTemplates();
        HookButtons();
        ApplyThemeFonts();
        UpdateTitleUI();
        SetupHowToPlayUI();

        SetPanel(resultPanel, false, false);
        SetPanel(pausePanel, false, false);
        SetPanel(howToPlayPanel, false, false);
        BlockGameplayInputBriefly(0.2f);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (checkButton != null)
            checkButton.gameObject.SetActive(false);

        UpdateIdleUI();
        BlockGameplayInputBriefly(0.25f);
        StartCoroutine(BeginGameFlowRoutine());
    }

    private void Update()
    {
        if (!gameRunning || questionLocked || currentTimer <= 0f || IsBlockingPanelOpen())
            return;

        currentTimer -= Time.deltaTime;
        UpdateTimerUI();

        if (currentTimer <= 0f)
        {
            currentTimer = 0f;
            EvaluateAnswer(true);
        }
    }

    private void HideSceneTemplates()
    {
        if (cardSceneTemplate != null)
            cardSceneTemplate.gameObject.SetActive(false);

        if (slotSceneTemplate != null)
            slotSceneTemplate.gameObject.SetActive(false);

        if (scorePopupSceneTemplate != null)
            scorePopupSceneTemplate.gameObject.SetActive(false);
    }

    private void HookButtons()
    {
        if (checkButton != null)
            checkButton.onClick.AddListener(CheckAnswer);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextQuestion);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        if (howToPlayOpenButton != null)
            howToPlayOpenButton.onClick.AddListener(OpenHowToPlayDuringGame);

        if (howToPrimaryButton != null)
            howToPrimaryButton.onClick.AddListener(OnHowToPrimaryButtonPressed);

        TryAutoFindContinueButton();
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToBloomReward);

        if (howToPrevButton != null)
            howToPrevButton.onClick.AddListener(ShowPreviousHowToImage);

        if (howToNextButton != null)
            howToNextButton.onClick.AddListener(ShowNextHowToImage);
    }

    private IEnumerator BeginGameFlowRoutine()
    {
        if (useBloomRewardSystem && RewardManager.Instance != null)
        {
            RewardManager.Instance.ShowPreGame(_skills);
            yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);
        }

        BlockGameplayInputBriefly(0.25f);

        if (showHowToPlayBeforeGame && howToPlayPanel != null)
        {
            RefreshHowToPrimaryButtonLabel(false);
            SetPanel(howToPlayPanel, true, true);
        }
        else if (autoStart)
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        PlayClick();
        Time.timeScale = 1f;

        score = 0;
        currentQuestionIndex = 0;
        expectedMaxTime = 0f;
        evaluatedSlotCount = 0;
        correctSlotCount = 0;
        wrongSlotCount = 0;
        postRewardShown = false;
        gameStartTime = Time.time;
        gameRunning = true;
        questionLocked = false;

        SetPanel(resultPanel, false, false);
        SetPanel(pausePanel, false, false);
        SetPanel(howToPlayPanel, false, false);
        BlockGameplayInputBriefly(0.2f);

        StartBackgroundMusic();

        PrepareQuestionList();
        LoadQuestion();
    }

    public void RestartGame()
    {
        StartGame();
    }

    public void PauseGame()
    {
        if (!gameRunning || IsPanelActive(resultPanel) || IsPanelActive(howToPlayPanel))
            return;

        PlayClick();
        PauseBackgroundMusic();
        Time.timeScale = 0f;
        SetPanel(pausePanel, true, true);
    }

    public void ResumeGame()
    {
        PlayClick();
        Time.timeScale = 1f;
        SetPanel(pausePanel, false, true);
        ResumeBackgroundMusic();
    }

    private void PrepareQuestionList()
    {
        runtimeQuestions.Clear();
        runtimeQuestions.AddRange(questions.Where(q => q != null && q.items != null && q.items.Count > 0));

        if (shuffleQuestions)
            Shuffle(runtimeQuestions);

        if (questionLimit > 0 && questionLimit < runtimeQuestions.Count)
            runtimeQuestions.RemoveRange(questionLimit, runtimeQuestions.Count - questionLimit);
    }

    private void LoadQuestion()
    {
        ClearCurrentQuestion();

        if (runtimeQuestions.Count == 0)
        {
            Debug.LogWarning("OrderSortDragManager has no valid questions.");
            UpdateIdleUI();
            return;
        }

        //RefreshBankGridCellSize();
        RefreshBankLayoutMode();
        ConfigureSlotsParentForResponsiveSlots();
        Canvas.ForceUpdateCanvases();

        if (bankParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(bankParent);

        questionLocked = false;
        usedOrganicBankRects.Clear();

        OrderSortQuestion question = runtimeQuestions[currentQuestionIndex];
        currentItems = SelectItemsForQuestion(question);
        currentCorrectOrder = BuildCorrectOrder(question, currentItems);

        if (questionText != null)
            questionText.text = question.questionText;

        if (feedbackText != null)
            feedbackText.text = string.Empty;

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (checkButton != null)
            checkButton.gameObject.SetActive(true);

        currentTimer = CalculateQuestionTime(currentItems.Count);
        expectedMaxTime += Mathf.Max(1f, currentTimer);
        if (timerText != null)
            timerText.gameObject.SetActive(currentTimer > 0f);

        UpdateScoreUI();
        UpdateTimerUI();
        UpdateProgressUI();

        CreateSlots(currentItems.Count);
        CreateCards(currentItems);
        AnimateQuestionIntro();

        if (question.questionAudio != null)
            PlaySfx(question.questionAudio);
    }

    private List<OrderSortItemData> SelectItemsForQuestion(OrderSortQuestion question)
    {
        List<OrderSortItemData> pool = new List<OrderSortItemData>(question.items.Where(x => x != null && !string.IsNullOrWhiteSpace(x.value)));

        if (pool.Count == 0)
            return pool;

        int targetCount = objectsPerQuestion <= 0 ? pool.Count : Mathf.Clamp(objectsPerQuestion, 1, pool.Count);

        if (randomizeObjectsPerQuestion)
            Shuffle(pool);

        if (targetCount < pool.Count)
            pool.RemoveRange(targetCount, pool.Count - targetCount);

        return pool;
    }

    private float CalculateQuestionTime(int itemCount)
    {
        if (!usePerObjectTimer)
            return Mathf.Clamp(secondsPerObject, minimumQuestionTime, maximumQuestionTime);

        float time = Mathf.Max(1, itemCount) * Mathf.Max(1f, secondsPerObject);
        return Mathf.Clamp(time, minimumQuestionTime, maximumQuestionTime);
    }

    private void RefreshBankGridCellSize()
    {
        if (bankParent == null)
            return;

        GridLayoutGroup grid = bankParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
            grid.cellSize = GetActiveCardSize();
    }

    private void RefreshBankLayoutMode()
    {
        if (bankParent == null)
            return;

        GridLayoutGroup grid = bankParent.GetComponent<GridLayoutGroup>();

        if (bankPlacementMode == OrderSortBankPlacementMode.GridLayoutGroup)
        {
            if (grid == null)
                grid = bankParent.gameObject.AddComponent<GridLayoutGroup>();

            grid.enabled = true;
            grid.cellSize = GetActiveCardSize();
            grid.spacing = new Vector2(14f, 14f);
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.childAlignment = TextAnchor.MiddleCenter;
            return;
        }

        if (grid != null)
            grid.enabled = false;
    }
    private void CreateSlots(int count)
    {
        for (int i = 0; i < count; i++)
        {
            RectTransform slotObj = Instantiate(slotSceneTemplate, slotsParent);
            slotObj.name = "Order Slot " + (i + 1);
            slotObj.gameObject.SetActive(true);
            ApplySlotSize(slotObj);

            OrderSortDropSlot slot = slotObj.GetComponent<OrderSortDropSlot>();
            slot.Init(this, i + 1);
            activeSlots.Add(slot);
        }
    }

    private void CreateCards(List<OrderSortItemData> items)
    {
        List<OrderSortItemData> displayItems = new List<OrderSortItemData>(items);

        if (shuffleCards)
            Shuffle(displayItems);

        for (int i = 0; i < displayItems.Count; i++)
        {
            OrderSortItemData itemData = displayItems[i];
            RectTransform cardObj = Instantiate(cardSceneTemplate, bankParent);
            cardObj.name = contentMode == OrderSortContentMode.ImageOnly ? "Image Object - " + itemData.value : "Text Card - " + itemData.value;
            cardObj.gameObject.SetActive(true);
            ApplyCardSize(cardObj);

            OrderSortDragItem item = cardObj.GetComponent<OrderSortDragItem>();
            item.Init(this, itemData, contentMode);
            ApplyTextCardColor(item, i);
            activeItems.Add(item);

            ApplyInitialBankPlacement(item, i);
            AnimateCardSpawn(cardObj, i);
        }
    }

    private void ApplyTextCardColor(OrderSortDragItem item, int displayIndex)
    {
        if (item == null || contentMode != OrderSortContentMode.TextOnly || !useRandomTextCardColors)
            return;

        if (textCardPastelColors == null || textCardPastelColors.Count == 0)
            return;

        Color color = textCardPastelColors[Mathf.Abs(displayIndex) % textCardPastelColors.Count];
        item.ApplyCardColor(color);
    }

    private Vector2 GetActiveCardSize()
    {
        return contentMode == OrderSortContentMode.ImageOnly ? imageCardSize : textCardSize;
    }

    private Vector2 GetActiveSlotSize()
    {
        return contentMode == OrderSortContentMode.ImageOnly ? imageSlotSize : textSlotSize;
    }

    private void ApplyCardSize(RectTransform rect)
    {
        Vector2 size = GetActiveCardSize();
        rect.sizeDelta = size;

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();

        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
    }

    private void ApplySlotSize(RectTransform rect)
    {
        Vector2 size = GetActiveSlotSize();
        rect.sizeDelta = size;

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
            layout = rect.gameObject.AddComponent<LayoutElement>();

        if (useResponsiveSlots)
        {
            Vector2 minSize = GetActiveMinimumSlotSize();
            layout.minWidth = minSize.x;
            layout.minHeight = minSize.y;
            layout.preferredWidth = minSize.x;
            layout.preferredHeight = minSize.y;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 1f;
        }
        else
        {
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }
    }

    private Vector2 GetActiveMinimumSlotSize()
    {
        return contentMode == OrderSortContentMode.ImageOnly ? minImageSlotSize : minTextSlotSize;
    }

    private void ConfigureSlotsParentForResponsiveSlots()
    {
        if (!useResponsiveSlots || slotsParent == null)
            return;

        HorizontalLayoutGroup horizontal = slotsParent.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
        {
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;
        }

        VerticalLayoutGroup vertical = slotsParent.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
        {
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = true;
        }
    }

    private void ApplyInitialBankPlacement(OrderSortDragItem item, int index)
    {
        if (item == null || bankParent == null)
            return;

        RectTransform itemRect = item.GetComponent<RectTransform>();
        item.transform.SetParent(bankParent, false);

        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        ApplyCardSize(itemRect);

        if (bankPlacementMode == OrderSortBankPlacementMode.OrganicRandom)
        {
            float scaleValue = UnityEngine.Random.Range(organicScaleRange.x, organicScaleRange.y);
            scaleValue = Mathf.Max(0.1f, scaleValue);

            Vector2 position = GetOrganicBankPosition(GetActiveCardSize() * scaleValue, index);
            float rotationZ = UnityEngine.Random.Range(-organicRotationRange, organicRotationRange);
            Vector3 scale = new Vector3(scaleValue, scaleValue, 1f);

            itemRect.anchoredPosition = position;
            itemRect.localEulerAngles = new Vector3(0f, 0f, rotationZ);
            itemRect.localScale = scale;

            item.StoreBankVisual(position, rotationZ, scale);
        }
        else
        {
            itemRect.localEulerAngles = Vector3.zero;
            itemRect.localScale = Vector3.one;
            item.StoreBankVisual(itemRect.anchoredPosition, 0f, Vector3.one);
        }
    }

    private Vector2 GetOrganicBankPosition(Vector2 itemSize, int index)
    {
        Rect bankRect = bankParent != null ? bankParent.rect : new Rect(0f, 0f, 800f, 300f);

        float halfWidth = itemSize.x * 0.5f;
        float halfHeight = itemSize.y * 0.5f;

        float minX = -bankRect.width * 0.5f + organicBankPadding.x + halfWidth;
        float maxX = bankRect.width * 0.5f - organicBankPadding.x - halfWidth;
        float minY = -bankRect.height * 0.5f + organicBankPadding.y + halfHeight;
        float maxY = bankRect.height * 0.5f - organicBankPadding.y - halfHeight;

        if (minX > maxX)
        {
            minX = -bankRect.width * 0.35f;
            maxX = bankRect.width * 0.35f;
        }

        if (minY > maxY)
        {
            minY = -bankRect.height * 0.25f;
            maxY = bankRect.height * 0.25f;
        }

        Vector2 bestPosition = Vector2.zero;
        int attempts = Mathf.Max(1, maxOrganicPlacementAttempts);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector2 candidate = new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY));
            Rect candidateRect = CreateCenteredRect(candidate, itemSize);

            if (!avoidOverlapInOrganicBank || !DoesOverlapUsedRects(candidateRect))
            {
                usedOrganicBankRects.Add(candidateRect);
                return candidate;
            }

            bestPosition = candidate;
        }

        float angle = index * 137.5f * Mathf.Deg2Rad;
        float radius = 18f + index * 12f;
        Vector2 fallback = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
        fallback.x = Mathf.Clamp(fallback.x, minX, maxX);
        fallback.y = Mathf.Clamp(fallback.y, minY, maxY);
        usedOrganicBankRects.Add(CreateCenteredRect(fallback, itemSize));
        return fallback == Vector2.zero ? bestPosition : fallback;
    }

    private Rect CreateCenteredRect(Vector2 center, Vector2 size)
    {
        return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
    }

    private bool DoesOverlapUsedRects(Rect candidate)
    {
        for (int i = 0; i < usedOrganicBankRects.Count; i++)
        {
            if (candidate.Overlaps(usedOrganicBankRects[i]))
                return true;
        }

        return false;
    }

    private List<string> BuildCorrectOrder(OrderSortQuestion question, List<OrderSortItemData> selectedItems)
    {
        List<string> values = selectedItems.Select(x => x.value).ToList();

        switch (question.sortRule)
        {
            case OrderSortRule.AlphabeticalAZ:
                return values.OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();

            case OrderSortRule.AlphabeticalZA:
                return values.OrderByDescending(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();

            case OrderSortRule.ShortToLong:
                return values.OrderBy(x => x.Length).ThenBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();

            case OrderSortRule.LongToShort:
                return values.OrderByDescending(x => x.Length).ThenBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();

            case OrderSortRule.NumberSmallToLarge:
                return values.OrderBy(ParseNumberSafe).ToList();

            case OrderSortRule.NumberLargeToSmall:
                return values.OrderByDescending(ParseNumberSafe).ToList();

            case OrderSortRule.ManualOrder:
                return BuildManualOrderFromItemList(question, selectedItems);

            default:
                return values;
        }
    }

    private List<string> BuildManualOrderFromItemList(OrderSortQuestion question, List<OrderSortItemData> selectedItems)
    {
        // ManualOrder now uses the Items list itself as the correct answer key.
        // The displayed cards can still be shuffled by shuffleCards/CreateCards.
        // If a random subset is selected, preserve the original Items order and filter to only those selected cards.
        List<string> remainingSelectedValues = selectedItems
            .Where(x => x != null && !string.IsNullOrWhiteSpace(x.value))
            .Select(x => Normalize(x.value))
            .ToList();

        List<string> orderedValues = new List<string>();

        foreach (OrderSortItemData item in question.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.value))
                continue;

            string normalized = Normalize(item.value);
            int selectedIndex = remainingSelectedValues.FindIndex(x => x == normalized);

            if (selectedIndex < 0)
                continue;

            orderedValues.Add(item.value);
            remainingSelectedValues.RemoveAt(selectedIndex);
        }

        return orderedValues;
    }

    private float ParseNumberSafe(string input)
    {
        if (TryEvaluateNumericValue(input, out float result))
            return result;

        Debug.LogWarning("Could not parse number/expression: " + input);
        return 0f;
    }

    private bool TryEvaluateNumericValue(string input, out float result)
    {
        result = 0f;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string expression = NormalizeMathExpression(input);

        if (TryParseMathOperand(expression, out result))
            return true;

        int operatorIndex = FindMainMathOperator(expression);
        if (operatorIndex <= 0 || operatorIndex >= expression.Length - 1)
            return false;

        char op = expression[operatorIndex];
        string leftText = expression.Substring(0, operatorIndex);
        string rightText = expression.Substring(operatorIndex + 1);

        if (!TryParseMathOperand(leftText, out float left))
            return false;

        if (!TryParseMathOperand(rightText, out float right))
            return false;

        switch (op)
        {
            case '+':
                result = left + right;
                return true;

            case '-':
                result = left - right;
                return true;

            case '*':
                result = left * right;
                return true;

            case '/':
                if (Mathf.Abs(right) <= Mathf.Epsilon)
                    return false;

                result = left / right;
                return true;

            default:
                return false;
        }
    }

    private string NormalizeMathExpression(string input)
    {
        return input
            .Trim()
            .Replace(" ", string.Empty)
            .Replace("×", "*")
            .Replace("x", "*")
            .Replace("X", "*")
            .Replace("÷", "/")
            .Replace("−", "-")
            .Replace("–", "-");
    }

    private int FindMainMathOperator(string expression)
    {
        for (int i = 1; i < expression.Length; i++)
        {
            char c = expression[i];
            if (c == '+' || c == '-' || c == '*' || c == '/')
                return i;
        }

        return -1;
    }

    private bool TryParseMathOperand(string text, out float value)
    {
        value = 0f;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();

        if (text.StartsWith("(", StringComparison.Ordinal) && text.EndsWith(")", StringComparison.Ordinal) && text.Length > 2)
            text = text.Substring(1, text.Length - 2);

        if (text.StartsWith("√", StringComparison.Ordinal))
        {
            string inner = text.Substring(1);
            if (inner.StartsWith("(", StringComparison.Ordinal) && inner.EndsWith(")", StringComparison.Ordinal) && inner.Length > 2)
                inner = inner.Substring(1, inner.Length - 2);

            if (!TryParseMathOperand(inner, out float innerValue))
                return false;

            if (innerValue < 0f)
                return false;

            value = Mathf.Sqrt(innerValue);
            return true;
        }

        return float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    public void PrepareItemForDrag(OrderSortDragItem item)
    {
        if (item == null || dragLayer == null)
            return;

        item.StorePreviousSlot();

        if (item.CurrentSlot != null)
            item.CurrentSlot.ClearItemOnly();

        item.MarkSlot(null);
        item.MarkPlacedThisDrag(false);

        RectTransform itemRect = item.GetComponent<RectTransform>();
        Vector2 currentSize = itemRect.rect.size;

        item.transform.SetParent(dragLayer, true);
        item.transform.SetAsLastSibling();
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.sizeDelta = currentSize;
        itemRect.localEulerAngles = Vector3.zero;
        itemRect.localScale = Vector3.one;
        itemRect.DOKill();
        itemRect.DOScale(1.06f, 0.12f).SetEase(Ease.OutBack);
    }

    public void DropItemOnSlot(OrderSortDragItem item, OrderSortDropSlot targetSlot)
    {
        if (!IsGameInputEnabled || item == null || targetSlot == null)
            return;

        PlaySfx(dropSfx);

        OrderSortDropSlot previousSlot = item.PreviousSlot;
        OrderSortDragItem existingItem = targetSlot.PlacedItem;

        if (existingItem != null && existingItem != item)
        {
            if (!allowSwap)
            {
                ShowFeedback("Swapping is disabled.");
                ReturnItemToPreviousPlace(item);
                item.MarkPlacedThisDrag(true);
                return;
            }

            targetSlot.ClearItemOnly();

            if (previousSlot != null && previousSlot.PlacedItem == null)
                PlaceItemInSlot(existingItem, previousSlot, true);
            else
                SendItemToBank(existingItem, true);
        }

        PlaceItemInSlot(item, targetSlot, true);
        item.MarkPlacedThisDrag(true);
    }

    public void DropItemOnBank(OrderSortDragItem item)
    {
        if (!IsGameInputEnabled || item == null)
            return;

        if (item.PreviousSlot != null && !allowReturnToBasket)
        {
            ShowFeedback("You can't return it to the basket.");
            ReturnItemToPreviousPlace(item);
            item.MarkPlacedThisDrag(true);
            return;
        }

        SendItemToBank(item, true);
        item.MarkPlacedThisDrag(true);
    }

    public void ReturnItemToPreviousPlace(OrderSortDragItem item)
    {
        if (item == null)
            return;

        if (item.PreviousSlot != null && item.PreviousSlot.PlacedItem == null)
            PlaceItemInSlot(item, item.PreviousSlot, true);
        else
            SendItemToBank(item, true);
    }

    private void PlaceItemInSlot(OrderSortDragItem item, OrderSortDropSlot slot, bool animate)
    {
        if (item == null || slot == null)
            return;

        if (item.CurrentSlot != null)
            item.CurrentSlot.ClearItemOnly();

        slot.SetItem(item);
        item.MarkSlot(slot);

        RectTransform itemRect = item.GetComponent<RectTransform>();
        item.transform.SetParent(slot.ItemHolder, false);

        itemRect.anchorMin = Vector2.zero;
        itemRect.anchorMax = Vector2.one;
        itemRect.offsetMin = Vector2.zero;
        itemRect.offsetMax = Vector2.zero;
        itemRect.localEulerAngles = Vector3.zero;
        itemRect.localScale = Vector3.one;

        slot.BringOverlayToFront();

        if (animate)
        {
            itemRect.DOKill();
            itemRect.localScale = Vector3.one * 0.92f;
            itemRect.DOScale(1f, 0.18f).SetEase(Ease.OutBack);
        }
    }

    private void SendItemToBank(OrderSortDragItem item, bool animate)
    {
        if (item == null || bankParent == null)
            return;

        if (item.CurrentSlot != null)
            item.CurrentSlot.ClearItemOnly();

        item.MarkSlot(null);

        RectTransform itemRect = item.GetComponent<RectTransform>();
        item.transform.SetParent(bankParent, false);
        item.transform.SetAsLastSibling();
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        ApplyCardSize(itemRect);

        if (bankPlacementMode == OrderSortBankPlacementMode.OrganicRandom)
            item.RestoreBankVisual();
        else
        {
            itemRect.localEulerAngles = Vector3.zero;
            itemRect.localScale = Vector3.one;
        }

        if (animate)
        {
            itemRect.DOKill();
            itemRect.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 0.7f);
        }
    }

    public void CheckAnswer()
    {
        if (!CanAcceptGameplayInput())
            return;

        PlayClick();
        EvaluateAnswer(false);
    }

    private void EvaluateAnswer(bool fromTimeout)
    {
        if (questionLocked)
            return;

        questionLocked = true;
        currentScorePopupIndex = 0;

        if (checkButton != null)
            checkButton.gameObject.SetActive(false);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (fromTimeout)
        {
            ShowFeedback("Time up! Checking placed items...");
            PlaySfx(timeoutSfx);
        }
        else
        {
            ShowFeedback("Checking...");
        }

        Sequence sequence = DOTween.Sequence();
        int delayedTotalScoreDelta = 0;

        for (int i = 0; i < activeSlots.Count; i++)
        {
            int slotIndex = i;
            OrderSortDropSlot slot = activeSlots[slotIndex];
            SlotResult result = GetSlotResult(slot, slotIndex);
            delayedTotalScoreDelta += result.scoreDelta;

            sequence.AppendInterval(Mathf.Max(0.01f, betweenSlotCheckDelay));

            if (slot != null)
            {
                bool keepOverlayVisible = keepAllResultOverlaysVisible || (keepWrongOverlayVisible && result.isWrong);

                Tween checkTween = slot.PlayCheckingThenResult(
                    checkingOverlayColor,
                    result.color,
                    Mathf.Max(0.05f, checkingFlashDuration),
                    Mathf.Max(0.05f, resultColorLerpDuration),
                    Mathf.Max(0.05f, resultHoldDuration),
                    Mathf.Max(0.05f, resultFadeOutDuration),
                    keepOverlayVisible);

                if (checkTween != null)
                    sequence.Append(checkTween);
            }

            sequence.AppendCallback(() =>
            {
                RegisterSlotResult(result);

                if (updateScoreDuringSlotEvaluation)
                {
                    ApplyScoreDelta(result.scoreDelta);
                }

                if (slot != null)
                    SpawnScorePopup(slot.transform.position, result.scoreText, result.color);

                if (result.scoreDelta > 0)
                    PlaySfx(correctSfx);
                else if (result.scoreDelta < 0)
                    PlaySfx(wrongSfx);
            });
        }

        bool isFinalQuestion = currentQuestionIndex >= runtimeQuestions.Count - 1;

        sequence.AppendInterval(0.25f);
        sequence.AppendCallback(() =>
        {
            if (!updateScoreDuringSlotEvaluation)
                ApplyScoreDelta(delayedTotalScoreDelta);

            bool allCorrect = IsCurrentAnswerPerfect();
            ShowFeedback(allCorrect ? "Perfect!" : "Review the highlighted slots.");

            if (!isFinalQuestion || !autoShowResultAfterFinalQuestion)
            {
                if (nextButton != null)
                    nextButton.gameObject.SetActive(true);
            }
        });

        if (isFinalQuestion && autoShowResultAfterFinalQuestion)
        {
            sequence.AppendInterval(0.45f);
            sequence.AppendCallback(ShowResult);
        }
    }

    private SlotResult GetSlotResult(OrderSortDropSlot slot, int index)
    {
        if (slot == null || slot.PlacedItem == null)
            return new SlotResult(scorePerEmptySlot, scorePerEmptySlot == 0 ? "0" : scorePerEmptySlot.ToString("+0;-0;0"), emptyOverlayColor, false, false, true);

        string placedValue = slot.PlacedItem.Value;
        string correctValue = index >= 0 && index < currentCorrectOrder.Count ? currentCorrectOrder[index] : string.Empty;
        OrderSortQuestion question = GetCurrentQuestion();

        if (IsAnswerValueAccepted(placedValue, correctValue, question))
            return new SlotResult(scorePerCorrectPosition, scorePerCorrectPosition.ToString("+0;-0;0"), correctOverlayColor, true, false, false);

        return new SlotResult(penaltyPerWrongPosition, penaltyPerWrongPosition.ToString("+0;-0;0"), wrongOverlayColor, false, true, false);
    }

    private OrderSortQuestion GetCurrentQuestion()
    {
        if (currentQuestionIndex < 0 || currentQuestionIndex >= runtimeQuestions.Count)
            return null;

        return runtimeQuestions[currentQuestionIndex];
    }

    private bool IsAnswerValueAccepted(string placedValue, string correctValue, OrderSortQuestion question)
    {
        if (Normalize(placedValue) == Normalize(correctValue))
            return true;

        if (question == null || question.comparisonMode != OrderSortComparisonMode.NumericValue)
            return false;

        if (!TryEvaluateNumericValue(placedValue, out float placedNumber))
            return false;

        if (!TryEvaluateNumericValue(correctValue, out float correctNumber))
            return false;

        return Mathf.Abs(placedNumber - correctNumber) <= Mathf.Max(0.00001f, numericComparisonTolerance);
    }

    private struct SlotResult
    {
        public int scoreDelta;
        public string scoreText;
        public Color color;
        public bool isCorrect;
        public bool isWrong;
        public bool isEmpty;

        public SlotResult(int scoreDelta, string scoreText, Color color, bool isCorrect, bool isWrong, bool isEmpty)
        {
            this.scoreDelta = scoreDelta;
            this.scoreText = scoreText;
            this.color = color;
            this.isCorrect = isCorrect;
            this.isWrong = isWrong;
            this.isEmpty = isEmpty;
        }
    }

    private void RegisterSlotResult(SlotResult result)
    {
        evaluatedSlotCount++;

        if (result.isCorrect)
            correctSlotCount++;
        else if (result.isWrong)
            wrongSlotCount++;
    }

    private void ApplyScoreDelta(int delta)
    {
        score += delta;
        if (!allowNegativeScore)
            score = Mathf.Max(0, score);

        UpdateScoreUI();
    }

    public void NextQuestion()
    {
        PlayClick();
        currentQuestionIndex++;

        if (currentQuestionIndex >= runtimeQuestions.Count)
        {
            ShowResult();
            return;
        }

        LoadQuestion();
    }

    private void ShowResult()
    {
        gameRunning = false;
        questionLocked = true;
        currentScorePopupIndex = 0;

        if (checkButton != null)
            checkButton.gameObject.SetActive(false);

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        ClearCurrentQuestion();
        SetPanel(resultPanel, true, true);

        if (resultText != null)
            resultText.text = "Game Complete!\nScore: " + score;

        TryAutoFindContinueButton();
        if (continueButton != null)
            continueButton.gameObject.SetActive(useBloomRewardSystem);

        BlockGameplayInputBriefly(0.25f);
    }

    private void SpawnScorePopup(Vector3 worldPosition, string text, Color color)
    {
        if (scorePopupSceneTemplate == null || dragLayer == null)
            return;

        TMP_Text popup = Instantiate(scorePopupSceneTemplate, dragLayer);
        popup.gameObject.SetActive(true);
        popup.text = text;
        popup.color = color;

        RectTransform rect = popup.GetComponent<RectTransform>();
        CanvasGroup group = popup.GetComponent<CanvasGroup>();
        if (group == null)
            group = popup.gameObject.AddComponent<CanvasGroup>();

        Vector2 startAnchoredPosition;

        if (spawnScorePopupAtCenter)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Vector2 anchorPosition = Vector2.zero;
            if (scorePopupCenterAnchor != null)
                anchorPosition = scorePopupCenterAnchor.anchoredPosition;

            float stackedOffset = currentScorePopupIndex * scorePopupStackSpacing;
            currentScorePopupIndex++;
            startAnchoredPosition = anchorPosition + scorePopupCenterOffset + new Vector2(0f, stackedOffset);
            rect.anchoredPosition = startAnchoredPosition;
        }
        else
        {
            popup.transform.position = worldPosition;
            startAnchoredPosition = rect.anchoredPosition;
        }

        rect.localScale = Vector3.one * 0.75f;
        group.alpha = 1f;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(rect.DOScale(1.35f, 0.2f).SetEase(Ease.OutBack));
        sequence.Join(rect.DOAnchorPos(startAnchoredPosition + new Vector2(0f, 58f), 0.8f).SetEase(Ease.OutQuad));
        sequence.Join(group.DOFade(0f, 0.75f).SetDelay(0.25f));
        sequence.OnComplete(() =>
        {
            if (popup != null)
                Destroy(popup.gameObject);
        });
    }

    private void AnimateQuestionIntro()
    {
        if (questionText == null)
            return;

        RectTransform rect = questionText.GetComponent<RectTransform>();
        rect.DOKill();
        rect.localScale = Vector3.one * 0.96f;
        rect.DOScale(1f, 0.22f).SetEase(Ease.OutBack);
    }

    private void AnimateCardSpawn(RectTransform cardRect, int index)
    {
        if (cardRect == null)
            return;

        Vector3 targetScale = cardRect.localScale;
        cardRect.localScale = Vector3.zero;
        cardRect.DOScale(targetScale, 0.25f).SetEase(Ease.OutBack).SetDelay(index * 0.025f);
    }


    private bool IsCurrentAnswerPerfect()
    {
        if (activeSlots.Count == 0 || currentCorrectOrder.Count != activeSlots.Count)
            return false;

        for (int i = 0; i < activeSlots.Count; i++)
        {
            if (activeSlots[i].PlacedItem == null)
                return false;

            if (Normalize(activeSlots[i].PlacedItem.Value) != Normalize(currentCorrectOrder[i]))
                return false;
        }

        return true;
    }

    private bool AreAllSlotsFilled()
    {
        return activeSlots.All(slot => slot.PlacedItem != null);
    }

    private string Normalize(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private void ClearCurrentQuestion()
    {
        foreach (OrderSortDragItem item in activeItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        foreach (OrderSortDropSlot slot in activeSlots)
        {
            if (slot != null)
                Destroy(slot.gameObject);
        }

        activeItems.Clear();
        activeSlots.Clear();
        usedOrganicBankRects.Clear();
    }

    private void UpdateTitleUI()
    {
        if (gameTitleText != null)
            gameTitleText.text = gameTitle;
    }

    private void UpdateIdleUI()
    {
        UpdateScoreUI();

        if (timerText != null)
            timerText.text = "Time: -";

        if (progressText != null)
        {
            progressText.text = string.Empty;
            progressText.gameObject.SetActive(false);
            if (progressText.transform.parent != null)
                progressText.transform.parent.gameObject.SetActive(false);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = currentTimer > 0f ? "Time: " + Mathf.CeilToInt(currentTimer) : "Time: 0";
    }

    private void UpdateProgressUI()
    {
        if (progressText == null)
            return;

        int total = runtimeQuestions != null ? runtimeQuestions.Count : 0;
        bool showProgress = total > 1;

        if (progressText.transform.parent != null)
            progressText.transform.parent.gameObject.SetActive(showProgress);

        progressText.gameObject.SetActive(showProgress);
        progressText.text = showProgress ? "Question " + (currentQuestionIndex + 1) + "/" + total : string.Empty;
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText == null)
            return;

        feedbackText.text = message;
        RectTransform rect = feedbackText.GetComponent<RectTransform>();
        rect.DOKill();
        rect.localScale = Vector3.one * 0.92f;
        rect.DOScale(1f, 0.18f).SetEase(Ease.OutBack);
    }


    public void OpenHowToPlayDuringGame()
    {
        if (howToPlayPanel == null || IsPanelActive(resultPanel) || IsPanelActive(pausePanel))
            return;

        PlayClick();
        RefreshHowToPrimaryButtonLabel(gameRunning);

        if (gameRunning)
            Time.timeScale = 0f;

        SetPanel(howToPlayPanel, true, true);
    }

    public void OnHowToPrimaryButtonPressed()
    {
        PlayClick();

        if (gameRunning)
        {
            Time.timeScale = 1f;
            SetPanel(howToPlayPanel, false, true);
            BlockGameplayInputBriefly(0.2f);
            return;
        }

        StartGame();
    }

    private void RefreshHowToPrimaryButtonLabel(bool closeMode)
    {
        if (howToPrimaryButtonText != null)
            howToPrimaryButtonText.text = closeMode ? "Close" : "Start";
    }

    private void SetupHowToPlayUI()
    {
        if (howToPlayText != null)
            howToPlayText.text = howToPlayMessage;

        RefreshHowToPrimaryButtonLabel(false);
        howToIndex = 0;
        RefreshHowToImage();
    }

    public void ShowNextHowToImage()
    {
        if (howToPlayImages == null || howToPlayImages.Count == 0)
            return;

        PlayClick();
        howToIndex = (howToIndex + 1) % howToPlayImages.Count;
        RefreshHowToImage();
    }

    public void ShowPreviousHowToImage()
    {
        if (howToPlayImages == null || howToPlayImages.Count == 0)
            return;

        PlayClick();
        howToIndex--;
        if (howToIndex < 0)
            howToIndex = howToPlayImages.Count - 1;

        RefreshHowToImage();
    }

    private void RefreshHowToImage()
    {
        bool hasImages = howToPlayImages != null && howToPlayImages.Count > 0;

        if (howToPlayImageView != null)
        {
            howToPlayImageView.gameObject.SetActive(hasImages);
            if (hasImages)
            {
                howToPlayImageView.sprite = howToPlayImages[Mathf.Clamp(howToIndex, 0, howToPlayImages.Count - 1)];
                howToPlayImageView.preserveAspect = true;
            }
        }

        if (howToPlayCounterText != null)
            howToPlayCounterText.text = hasImages ? (howToIndex + 1) + "/" + howToPlayImages.Count : "No guide image";

        if (howToPrevButton != null)
            howToPrevButton.gameObject.SetActive(hasImages && howToPlayImages.Count > 1);

        if (howToNextButton != null)
            howToNextButton.gameObject.SetActive(hasImages && howToPlayImages.Count > 1);
    }

    public void ApplyThemeFonts()
    {
        if (primaryFont == null && secondaryFont == null)
            return;

        TMP_Text[] texts = FindObjectsOfType<TMP_Text>(true);
        TMP_FontAsset fallbackFont = primaryFont != null ? primaryFont : secondaryFont;

        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            bool usePrimary = text.fontSize >= 30f || text.name.Contains("Question") || text.name.Contains("Title") || text.name.Contains("Result") || text.name.Contains("Feedback");
            text.font = usePrimary ? fallbackFont : (secondaryFont != null ? secondaryFont : fallbackFont);
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    private void PlayClick()
    {
        PlaySfx(clickSfx);
    }

    private void StartBackgroundMusic()
    {
        if (!playBgmOnGameStart || bgmSource == null || backgroundMusicClip == null)
            return;

        bgmSource.clip = backgroundMusicClip;
        bgmSource.loop = loopBackgroundMusic;
        bgmSource.volume = backgroundMusicVolume;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    private void PauseBackgroundMusic()
    {
        if (!pauseBgmWithGamePause || bgmSource == null || !bgmSource.isPlaying)
            return;

        bgmSource.Pause();
    }

    private void ResumeBackgroundMusic()
    {
        if (!pauseBgmWithGamePause || bgmSource == null || backgroundMusicClip == null || !gameRunning)
            return;

        if (bgmSource.clip == null)
            bgmSource.clip = backgroundMusicClip;

        bgmSource.loop = loopBackgroundMusic;
        bgmSource.volume = backgroundMusicVolume;
        bgmSource.UnPause();

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    private void StopBackgroundMusicForReward()
    {
        if (!stopBgmOnRewardScreen || bgmSource == null)
            return;

        bgmSource.Stop();
    }

    private void SetPanel(GameObject panel, bool state, bool animate)
    {
        if (panel == null)
            return;

        if (state)
        {
            panel.transform.SetAsLastSibling();
            Canvas panelCanvas = panel.GetComponent<Canvas>();
            if (panelCanvas == null)
                panelCanvas = panel.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 1000;

            if (panel.GetComponent<GraphicRaycaster>() == null)
                panel.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
            group = panel.AddComponent<CanvasGroup>();

        panel.SetActive(state);

        group.interactable = state;
        group.blocksRaycasts = state;

        if (!animate)
        {
            group.alpha = state ? 1f : 0f;
            panel.transform.localScale = Vector3.one;
            if (state)
                BlockGameplayInputBriefly(0.15f);
            return;
        }

        group.DOKill();
        panel.transform.DOKill();

        if (state)
        {
            BlockGameplayInputBriefly(0.15f);
            group.alpha = 0f;
            panel.transform.localScale = Vector3.one * 0.96f;
            group.DOFade(1f, 0.18f).SetUpdate(true);
            panel.transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
        }
        else
        {
            group.DOFade(0f, 0.12f).SetUpdate(true).OnComplete(() =>
            {
                if (panel != null)
                    panel.SetActive(false);
            });
        }
    }

    private bool CanAcceptGameplayInput()
    {
        return gameRunning
            && !questionLocked
            && Time.timeScale > 0f
            && Time.unscaledTime >= inputUnlockTime
            && !IsBlockingPanelOpen();
    }

    private bool IsBlockingPanelOpen()
    {
        return IsPanelActive(howToPlayPanel) || IsPanelActive(pausePanel) || IsPanelActive(resultPanel);
    }

    private bool IsPanelActive(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    private void BlockGameplayInputBriefly(float duration)
    {
        inputUnlockTime = Mathf.Max(inputUnlockTime, Time.unscaledTime + Mathf.Max(0f, duration));
    }

    private void TryAutoFindContinueButton()
    {
        if (continueButton != null || resultPanel == null)
            return;

        Button[] buttons = resultPanel.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.name.ToLowerInvariant().Contains("continue"))
            {
                continueButton = button;
                return;
            }
        }
    }

    public void ContinueToBloomReward()
    {
        if (postRewardShown)
            return;

        postRewardShown = true;
        PlayClick();
        SetPanel(resultPanel, false, true);
        StopBackgroundMusicForReward();

        if (!useBloomRewardSystem || RewardManager.Instance == null)
            return;

        float timeTaken = Mathf.Max(0f, Time.time - gameStartTime);
        float expectedMax = Mathf.Max(1f, expectedMaxTime);
        float timeScore = Mathf.Clamp01(1f - (timeTaken / expectedMax));
        float accuracyScore = evaluatedSlotCount > 0 ? (float)correctSlotCount / evaluatedSlotCount : 0f;

        GameEvaluationData eval = new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = Mathf.Clamp01(accuracyScore),
            mistakeCount = wrongSlotCount,
            timeTaken = timeTaken
        };

        RewardManager.Instance.ShowPostGame(_skills, eval);
    }

    public void OnPlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHome()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(homeSceneName);
    }

    public void OnRewardScreenOpen()
    {
        StopBackgroundMusicForReward();

        if (sfxSource != null)
            sfxSource.Stop();
    }

    private void ValidateRequiredReferences()
    {
        if (bankParent == null || slotsParent == null || dragLayer == null || cardSceneTemplate == null || slotSceneTemplate == null)
            Debug.LogWarning("OrderSortDragManager has missing scene references. Use Tools > Mini Games > Order Sort Drag to create a scene.");
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
