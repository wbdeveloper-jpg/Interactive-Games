using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using RewardSystem;

// UPDATED BUILD: Per-operation difficulty, rare mixed-number subtraction,
// Score wording, and direct pauseProfitText compatibility for editor scripts.
public class FractionPortionFillManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    public enum QuestionMode
    {
        DirectFractionOnly,
        FractionAdditionOnly,
        FractionSubtractionOnly,
        MixedRuntime
    }

    public enum OperationType
    {
        Direct,
        Addition,
        Subtraction
    }

    public enum StandardQuestionDifficulty
    {
        Normal,
        Easy
    }

    public enum SubtractionQuestionDifficulty
    {
        Normal,
        Easy,
        Hard
    }

    [Serializable]
    public class PortionItemData
    {
        [Tooltip("Stable id. Keep lowercase and unique. Example: capsicum")]
        public string id = "item";
        public string displayName = "Item";
        public Color color = Color.white;
        public Sprite icon;
        public bool canAppearInQuestions = true;

        [Header("Distractor Stock")]
        [Min(0)] public int distractorStockMin = 1;
        [Min(0)] public int distractorStockMax = 4;
    }

    [Serializable]
    public class MixedModeSettings
    {
        public bool includeDirect = true;
        public bool includeAddition = true;
        public bool includeSubtraction = true;
        [Min(0)] public int directWeight = 4;
        [Min(0)] public int additionWeight = 3;
        [Min(0)] public int subtractionWeight = 3;
    }

    public class FractionTerm
    {
        public int numerator;
        public int denominator;
        public bool displayAsMixedNumber;

        public FractionTerm(int numerator, int denominator, bool simplify = true, bool displayAsMixedNumber = false)
        {
            this.numerator = numerator;
            this.denominator = Mathf.Max(1, denominator);
            this.displayAsMixedNumber = displayAsMixedNumber;
            if (simplify)
                Simplify();
        }

        public bool ShouldDisplayAsMixedNumber => displayAsMixedNumber
            && denominator > 1
            && Mathf.Abs(numerator) > denominator
            && numerator % denominator != 0;

        public int WholeNumber => numerator / denominator;
        public int RemainderNumerator => Mathf.Abs(numerator % denominator);

        public void Simplify()
        {
            int gcd = GreatestCommonDivisor(Mathf.Abs(numerator), Mathf.Abs(denominator));
            if (gcd <= 0)
                return;

            numerator /= gcd;
            denominator /= gcd;
        }

        public string GetText()
        {
            if (denominator == 1)
                return numerator.ToString();

            if (ShouldDisplayAsMixedNumber)
                return WholeNumber + " " + RemainderNumerator + "/" + denominator;

            return numerator + "/" + denominator;
        }
    }

    public class RuntimeRequest
    {
        public string itemId;
        public string itemName;
        public OperationType operationType;
        public int requiredUnits;
        public readonly List<FractionTerm> terms = new List<FractionTerm>();

        public string GetQuestionText()
        {
            if (terms.Count == 0)
                return "Cover " + requiredUnits + (requiredUnits == 1 ? " slice" : " slices") + " of the pizza with " + itemName;

            if (operationType == OperationType.Addition && terms.Count >= 2)
                return "Cover " + terms[0].GetText() + " + " + terms[1].GetText() + " of the pizza with " + itemName;

            if (operationType == OperationType.Subtraction && terms.Count >= 2)
                return "Cover " + terms[0].GetText() + " - " + terms[1].GetText() + " of the pizza with " + itemName;

            return "Cover " + terms[0].GetText() + " of the pizza with " + itemName;
        }
    }

    public class RuntimeQuestion
    {
        public int portionCount;
        public readonly List<RuntimeRequest> requests = new List<RuntimeRequest>();
        public readonly Dictionary<string, int> initialStockByItemId = new Dictionary<string, int>();
        public bool isImpossibleAtStart;
        public string signature;
    }

    [Header("Item Library")]
    public List<PortionItemData> items = new List<PortionItemData>();

    [Header("Runtime Question Generation")]
    public QuestionMode questionMode = QuestionMode.MixedRuntime;
    [Header("Difficulty By Operation")]
    [Tooltip("Easy keeps the denominator equal to the pizza slice count. Normal preserves the original simplified direct-fraction behaviour.")]
    public StandardQuestionDifficulty directDifficulty = StandardQuestionDifficulty.Normal;
    [Tooltip("Easy keeps both denominators equal to the pizza slice count. Normal preserves the original addition behaviour.")]
    public StandardQuestionDifficulty additionDifficulty = StandardQuestionDifficulty.Normal;
    [Tooltip("Hard preserves Normal behaviour but can rarely generate a mixed-number subtraction whose answer still fits one pizza.")]
    public SubtractionQuestionDifficulty subtractionDifficulty = SubtractionQuestionDifficulty.Normal;
    [Tooltip("Chance that a Hard subtraction request uses mixed numbers. 0.05 means 5%.")]
    [Range(0f, 1f)] public float hardMixedNumberChance = 0.05f;
    [Header("Question Quantity And Fractions")]
    [Min(10)] public int rounds = 10;
    [Range(2, 12)] public int minPortionCount = 4;
    [Range(2, 12)] public int maxPortionCount = 12;
    [Range(1, 4)] public int minRequestsPerQuestion = 1;
    [Range(1, 4)] public int maxRequestsPerQuestion = 2;
    [Tooltip("Default ON because 5/7/9/11 slices are visually harder for young players. Turn off if you want every count from min to max.")]
    public bool useCommonPortionCountsOnly = true;
    public bool avoidWholePizzaFractions = true;
    [Tooltip("Keeps addition/subtraction terms on the original pizza denominator, e.g. 5/10 - 3/10 instead of 1/2 - 3/10. This is clearer for grade-school slice math.")]
    public bool keepOperationFractionsOnPizzaDenominator = true;
    public MixedModeSettings mixedMode = new MixedModeSettings();
    [Min(10)] public int maxGenerationAttemptsPerQuestion = 300;

    [Header("Stock / Impossible Order")]
    public bool allowImpossibleOrders = true;
    [Range(0f, 1f)] public float impossibleOrderChance = 0.2f;
    [Min(0)] public int solvableExtraStockMin = 0;
    [Min(0)] public int solvableExtraStockMax = 2;
    [Tooltip("False means removed placed items are discarded, as requested.")]
    public bool returnClearedItemsToBasket = false;
    public bool showCannotCompleteButton = true;

    [Header("Scoring")]
    public int perfectOrderReward = 100;
    public int wrongOrderPenalty = 50;
    public int hintCost = 20;
    public bool hintOncePerQuestion = true;

    [Header("Game Flow")]
    public bool playOnStart = true;
    public bool useTimer = true;
    [Min(5f)] public float questionTime = 45f;
    public bool autoNextQuestion = true;
    [Min(0.1f)] public float nextDelay = 1f;
    public bool allowReplaceExistingDropZone = true;

    [Header("Startup Flow")]
    public bool showLoadingBeforeGame = true;
    [Min(0.05f)] public float loadingDuration = 1.2f;
    public bool showHowToBeforeFirstOrder = true;

    [Header("Bloom Reward Integration")]
    [Tooltip("RewardManager prefab must live once in the LoadingScene, as described in the Bloom integration guide.")]
    public bool useBloomRewardSystem = true;
    [Tooltip("Show Bloom skill preview before this game's own loading page.")]
    public bool showBloomPreGameBeforeLocalLoading = true;
    [Tooltip("Scene loaded when Bloom post-game Home is pressed.")]
    public string bloomHomeSceneName = "Loader Scene";
    [Tooltip("0 means auto: rounds * questionTime. Used for normalized Bloom timeScore.")]
    [Min(0f)] public float bloomExpectedMaxTimeSeconds = 0f;
    [Range(0f, 1f)] public float bloomApplyTimeWeight = 0.25f;
    [Range(0f, 1f)] public float bloomApplyAccuracyWeight = 0.75f;
    [Range(0f, 1f)] public float bloomAnalyzeTimeWeight = 0.35f;
    [Range(0f, 1f)] public float bloomAnalyzeAccuracyWeight = 0.65f;

    [Header("Typography - Assign In Inspector")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;

    [Header("Layout Roots")]
    public GameObject gameplayRoot;
    public GameObject overlayRoot;
    public GameObject loadingPanel;
    public Slider loadingSlider;
    public TMP_Text loadingTitleText;
    public TMP_Text loadingProgressText;

    [Header("Scene References - Templates Stay In Scene")]
    public Canvas rootCanvas;
    public RectTransform portionRoot;
    public RectTransform basketRoot;
    public RectTransform dragLayer;
    public FractionPortionDropZone dropZoneTemplate;
    public FractionPortionBasketCard basketCardTemplate;
    public FractionPortionDragVisual dragVisualTemplate;

    [Header("Text UI")]
    public TMP_Text questionText;
    public FractionPortionQuestionRenderer questionRenderer;
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text feedbackText;
    public TMP_Text progressText;
    public TMP_Text portionCountText;

    [Header("Buttons / Panels")]
    public Button cannotCompleteButton;
    public Button hintButton;
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button resultContinueButton;
    public GameObject howToPlayPanel;
    public FractionPortionHowToGuidePanel howToGuidePanel;
    public GameObject pausePanel;
    public TMP_Text pauseProfitText;

    [Header("Order Detail Overlay")]
    public Button orderDetailsButton;
    public GameObject orderDetailsPanel;
    public CanvasGroup orderDetailsCanvasGroup;
    public TMP_Text orderDetailsTitleText;
    public TMP_Text orderDetailsBodyText;
    public TMP_Text orderDetailsMascotText;
    [Tooltip("Image used in the Order Details overlay mascot art area. Assign Mascot Art Placeholder Root Image here for existing scenes.")]
    public Image orderDetailsMascotImage;
    [Tooltip("Optional text placeholder shown when no customer/chef sprite is assigned.")]
    public TMP_Text orderDetailsMascotPlaceholderText;
    public Button orderDetailsContinueButton;
    public bool showOrderDetailsBeforeEachOrder = true;
    [Min(0f)] public float orderDetailsIntroAutoCloseSeconds = 30f;
    [Min(0f)] public float orderDetailsReviewAutoCloseSeconds = 15f;
    [Min(0f)] public float orderDetailsAnimationDuration = 0.34f;
    public string customerMascotLabel = "Customer";
    public string chefMascotLabel = "Chef";
    [Header("Order Detail Mascot Sprites")]
    [Tooltip("Primary customer image used by the Order Details overlay.")]
    public Sprite customerMascotSprite;
    [Tooltip("Optional second customer image. A customer sprite is selected randomly once for each new order.")]
    public Sprite customerMascotSprite2;
    [Tooltip("When both customer sprites are assigned, prevent the same customer appearing in two orders back-to-back.")]
    public bool avoidRepeatingCustomerMascot = true;
    public Sprite chefMascotSprite;
    public bool hideMascotPlaceholderTextWhenSpriteAssigned = true;

    [Header("Order Detail Fraction Text")]
    [Range(70, 150)] public int orderFractionSizePercent = 105;
    [Tooltip("Small spacing placed on both sides of the fraction slash in the order/hint overlay. Use empty string for no gap, thin space for small gap.")]
    public string orderFractionSlashSideSpace = " ";
    public string orderFractionSlash = "⁄";
    [Range(0f, 0.5f)] public float orderFractionNumeratorOffsetEm = 0.18f;
    [Range(0f, 0.5f)] public float orderFractionDenominatorOffsetEm = 0.16f;

    [Header("Feedback")]
    public FractionPortionFeedbackPopup feedbackPopup;
    public Color neutralFeedbackColor = Color.white;
    public Color correctFeedbackColor = Color.green;
    public Color wrongFeedbackColor = new Color(1f, 0.2f, 0.2f);

    [Header("Result Feedback Animation")]
    [Tooltip("Target that pulses before the reward/wrong popup appears. Usually Pizza Board Background Root.")]
    public RectTransform pizzaFeedbackTarget;
    public CanvasGroup pizzaFeedbackOverlayGroup;
    public Image pizzaFeedbackOverlayImage;
    public TMP_Text pizzaFeedbackOverlayText;
    public bool useSequentialResultFeedback = true;
    [Range(1.01f, 1.25f)] public float pizzaFeedbackScaleAmount = 1.06f;
    [Min(0.05f)] public float pizzaFeedbackPulseDuration = 0.22f;
    [Min(0f)] public float pizzaFeedbackHoldDuration = 0.32f;
    [Min(0f)] public float popupAfterPizzaDelay = 0.12f;
    [Min(0f)] public float wrongFeedbackShakeDistance = 16f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioSource musicSource;
    public AudioClip backgroundMusicClip;
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.28f;
    public bool playBackgroundMusic = true;
    public bool loopBackgroundMusic = true;
    public AudioClip uiClickClip;
    public AudioClip dragStartClip;
    public AudioClip dropClip;
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip timeUpClip;
    public AudioClip hintClip;
    public AudioClip completeClip;

    [Header("Portion Visuals")]
    public Color evenPortionColor = new Color(1f, 0.72f, 0.32f, 0.72f);
    public Color oddPortionColor = new Color(1f, 0.60f, 0.24f, 0.72f);
    [Range(0f, 0.04f)] public float portionCutGapPercent = 0.012f;
    public bool showPortionNumbers = true;

    [Header("Placed Topping Visuals")]
    [Tooltip("When ON, a dropped topping lightly tints the whole pizza wedge/slice using the topping color. Optional support layer; the main client-facing visual is the scattered topping copies below.")]
    public bool fillEntireSliceWithDroppedTopping = false;
    [Range(0f, 1f)] public float filledSliceToppingAlpha = 0.45f;
    [Tooltip("When ON, dropping one basket item spawns multiple visual copies inside that single pizza slice, making the whole slice look topped.")]
    public bool scatterToppingCopiesOnPlacedSlice = true;
    [Tooltip("Fallback copy count used only when dynamic copy count is OFF.")]
    [Range(1, 24)] public int toppingCopiesPerPlacedSlice = 7;
    [Tooltip("Automatically reduces topping copies as pizza has more/smaller slices. 4 slices = many copies, 12 slices = fewer copies.")]
    public bool useDynamicToppingCopyCount = true;
    [Range(4, 30)] public int toppingCopiesAtFourSlices = 20;
    [Range(3, 18)] public int toppingCopiesAtTwelveSlices = 6;
    [Min(8f)] public float toppingCopyMinSize = 28f;
    [Min(8f)] public float toppingCopyMaxSize = 44f;
    [Range(0.02f, 0.55f)] public float toppingScatterInnerRadiusPercent = 0.16f;
    [Range(0.2f, 0.96f)] public float toppingScatterOuterRadiusPercent = 0.88f;
    [Range(0f, 0.30f)] public float toppingScatterAnglePaddingPercent = 0.06f;
    [Min(0f)] public float toppingScatterMinDistance = 30f;
    [Range(1, 40)] public int toppingScatterPlacementAttempts = 22;
    public bool randomizeToppingCopyRotation = true;
    [Tooltip("Optional single center icon. Usually OFF when scatter copies are enabled.")]
    public bool showPlacedItemIconOnPizza = false;
    public bool showPlacedItemLabelOnPizza = false;
    public bool showDragVisualName = false;

    private readonly List<RuntimeQuestion> generatedQuestions = new List<RuntimeQuestion>();
    private readonly List<FractionPortionDropZone> activeDropZones = new List<FractionPortionDropZone>();
    private readonly Dictionary<string, FractionPortionBasketCard> activeBasketCards = new Dictionary<string, FractionPortionBasketCard>();

    private RuntimeQuestion currentQuestion;
    private int currentQuestionIndex;
    public int CurrentPortionCount => currentQuestion != null ? currentQuestion.portionCount : Mathf.Clamp(minPortionCount, 4, 12);

    public int GetToppingCopyCountForCurrentPortion()
    {
        if (!useDynamicToppingCopyCount)
            return Mathf.Max(1, toppingCopiesPerPlacedSlice);

        int portions = Mathf.Clamp(CurrentPortionCount, 4, 12);
        float t = Mathf.InverseLerp(4f, 12f, portions);
        int count = Mathf.RoundToInt(Mathf.Lerp(toppingCopiesAtFourSlices, toppingCopiesAtTwelveSlices, t));
        int min = Mathf.Min(toppingCopiesAtFourSlices, toppingCopiesAtTwelveSlices);
        int max = Mathf.Max(toppingCopiesAtFourSlices, toppingCopiesAtTwelveSlices);
        return Mathf.Clamp(count, Mathf.Max(1, min), Mathf.Max(1, max));
    }

    private int score;
    private int successfulOrders;
    private int wrongOrders;
    private int correctCannotServeOrders;
    private int hintsUsed;
    private float bloomActiveGameplayTime;
    private bool bloomPostGameShown;
    private float timer;
    private bool questionActive;
    private bool isPaused;
    private bool hintUsedThisQuestion;
    private bool waitingForMandatoryHowTo;
    private bool resultSequenceRunning;
    private bool orderDetailsOpen;
    private bool orderDetailsPausesTimer;
    private Sprite selectedCustomerMascotSprite;
    private Sprite lastCustomerMascotSprite;
    private bool orderDetailsIntroWaiting;
    private Coroutine orderDetailsAutoCloseRoutine;
    private FractionPortionDragVisual activeDragVisual;

    public RectTransform DragLayer => dragLayer;

    private void Awake()
    {
        ResolveSceneReferences();
        PrepareAudioSources();
        EnsureDefaultItemsIfEmpty();
        EnsureButtonListeners();
        EnsureTemplatesExist();
        ApplyConfiguredFonts();
    }

    private void Start()
    {
        if (playOnStart)
            StartGameFlow();
    }

    private void Update()
    {
        if (!questionActive || isPaused || orderDetailsPausesTimer)
            return;

        bloomActiveGameplayTime += Time.deltaTime;

        if (!useTimer)
            return;

        timer -= Time.deltaTime;
        if (timer < 0f)
            timer = 0f;

        UpdateTimerUI();

        if (timer <= 0f)
        {
            if (resultSequenceRunning)
                return;

            questionActive = false;
            ApplyWrongOrderPenalty();
            StartCoroutine(ResultFeedbackSequence(false, "Time up! -" + wrongOrderPenalty, wrongFeedbackColor, timeUpClip, true));
        }
    }

    public void StartGameFlow()
    {
        StopAllCoroutines();
        StartCoroutine(StartGameFlowRoutine());
    }

    private IEnumerator StartGameFlowRoutine()
    {
        ResolveSceneReferences();
        PrepareAudioSources();
        EnsureDefaultItemsIfEmpty();
        EnsureButtonListeners();
        EnsureTemplatesExist();
        ApplyConfiguredFonts();

        if (useBloomRewardSystem && showBloomPreGameBeforeLocalLoading)
            yield return ShowBloomPreGameRoutine();

        PlayBackgroundMusic();

        questionActive = false;
        isPaused = false;
        waitingForMandatoryHowTo = false;

        HidePanel(resultPanel);
        HidePanel(pausePanel);
        HidePanel(howToPlayPanel);
        HidePanel(orderDetailsPanel);

        if (gameplayRoot != null)
            gameplayRoot.SetActive(false);

        if (loadingPanel != null && showLoadingBeforeGame)
        {
            loadingPanel.SetActive(true);
            if (loadingSlider != null)
                loadingSlider.value = 0f;
            if (loadingProgressText != null)
                loadingProgressText.text = "Loading 0%";

            float duration = Mathf.Max(0.05f, loadingDuration);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                if (loadingSlider != null)
                    loadingSlider.value = progress;
                if (loadingProgressText != null)
                    loadingProgressText.text = "Loading " + Mathf.RoundToInt(progress * 100f) + "%";
                yield return null;
            }

            if (loadingSlider != null)
                loadingSlider.value = 1f;
            if (loadingProgressText != null)
                loadingProgressText.text = "Loading 100%";
            yield return new WaitForSecondsRealtime(0.15f);
            loadingPanel.SetActive(false);
        }
        else
        {
            HidePanel(loadingPanel);
        }

        if (gameplayRoot != null)
            gameplayRoot.SetActive(true);

        if (showHowToBeforeFirstOrder && howToPlayPanel != null)
        {
            waitingForMandatoryHowTo = true;
            if (howToGuidePanel != null)
                howToGuidePanel.ResetGuide();
            howToPlayPanel.SetActive(true);
            howToPlayPanel.transform.SetAsLastSibling();
            yield break;
        }

        StartGame();
    }

    public void StartGame()
    {
        StopAllCoroutines();
        ResolveSceneReferences();
        PrepareAudioSources();
        PlayBackgroundMusic();
        EnsureDefaultItemsIfEmpty();
        EnsureButtonListeners();
        EnsureTemplatesExist();
        ApplyConfiguredFonts();

        if (gameplayRoot != null)
            gameplayRoot.SetActive(true);
        HidePanel(loadingPanel);

        score = 0;
        successfulOrders = 0;
        wrongOrders = 0;
        correctCannotServeOrders = 0;
        hintsUsed = 0;
        bloomActiveGameplayTime = 0f;
        bloomPostGameShown = false;
        currentQuestionIndex = 0;
        isPaused = false;
        HidePanel(resultPanel);
        HidePanel(pausePanel);
        HidePanel(howToPlayPanel);
        HidePanel(orderDetailsPanel);

        GenerateSessionQuestions();
        LoadCurrentQuestion();
    }

    public void RestartGame()
    {
        PlayClip(uiClickClip);
        StartGameFlow();
    }

    public void TogglePause()
    {
        PlayClip(uiClickClip);
        isPaused = !isPaused;
        if (pauseProfitText != null)
            pauseProfitText.text = "Current Score: " + score;
        if (pausePanel != null)
        {
            pausePanel.SetActive(isPaused);
            if (isPaused)
                pausePanel.transform.SetAsLastSibling();
        }
    }

    public void ToggleHowToPlay()
    {
        if (howToPlayPanel == null)
            return;

        if (waitingForMandatoryHowTo && howToPlayPanel.activeSelf)
        {
            CloseHowToPlay();
            return;
        }

        bool shouldShow = !howToPlayPanel.activeSelf;
        if (shouldShow && howToGuidePanel != null)
            howToGuidePanel.ResetGuide();
        howToPlayPanel.SetActive(shouldShow);
        if (shouldShow)
            howToPlayPanel.transform.SetAsLastSibling();
    }

    public void OpenHowToPlay()
    {
        PlayClip(uiClickClip);
        if (howToPlayPanel != null)
        {
            if (howToGuidePanel != null)
                howToGuidePanel.ResetGuide();
            howToPlayPanel.SetActive(true);
            howToPlayPanel.transform.SetAsLastSibling();
        }
    }

    public void CloseHowToPlay()
    {
        PlayClip(uiClickClip);
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        if (waitingForMandatoryHowTo)
        {
            waitingForMandatoryHowTo = false;
            StartGame();
        }
    }

    public bool CanDragItems()
    {
        return questionActive && !isPaused && !orderDetailsOpen;
    }

    public void BeginBasketDrag(FractionPortionBasketCard sourceCard, UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (sourceCard == null || !CanDragItems())
            return;

        EnsureTemplatesExist();

        if (dragVisualTemplate == null || dragLayer == null)
        {
            ShowFeedback("Drag visual template missing.", wrongFeedbackColor);
            return;
        }

        if (activeDragVisual != null)
            Destroy(activeDragVisual.gameObject);

        activeDragVisual = Instantiate(dragVisualTemplate, dragLayer);
        activeDragVisual.gameObject.SetActive(true);
        activeDragVisual.Setup(sourceCard.ItemData, showDragVisualName);
        activeDragVisual.transform.SetAsLastSibling();
        activeDragVisual.FollowPointer(rootCanvas, eventData);
        PlayClip(dragStartClip);
    }

    public void UpdateBasketDrag(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (activeDragVisual != null)
            activeDragVisual.FollowPointer(rootCanvas, eventData);
    }

    public void EndBasketDrag()
    {
        if (activeDragVisual != null)
        {
            Destroy(activeDragVisual.gameObject);
            activeDragVisual = null;
        }
    }

    public bool TryPlaceFromBasket(FractionPortionDropZone zone, FractionPortionBasketCard sourceCard)
    {
        if (!questionActive || zone == null || sourceCard == null)
            return false;

        if (!IsRequestedItem(sourceCard.ItemId))
        {
            ShowFeedback("This item is not needed.", wrongFeedbackColor);
            PlayClip(wrongClip);
            return false;
        }

        if (!allowReplaceExistingDropZone && zone.IsOccupied)
        {
            ShowFeedback("This portion is already filled.", wrongFeedbackColor);
            PlayClip(wrongClip);
            return false;
        }

        zone.SetItem(sourceCard.ItemData);
        sourceCard.MarkCurrentDragConsumed();
        PlayClip(dropClip);
        CheckAnswer();
        return true;
    }

    public void ClearDropZone(FractionPortionDropZone zone)
    {
        if (!questionActive || zone == null || !zone.IsOccupied)
            return;

        string removedId = zone.AssignedItemId;
        zone.ClearZone();

        if (returnClearedItemsToBasket && activeBasketCards.TryGetValue(removedId, out FractionPortionBasketCard card))
            card.AddStock(1);

        ShowFeedback(returnClearedItemsToBasket ? "Item returned." : "Item removed.", neutralFeedbackColor);
    }

    public void OnCannotCompletePressed()
    {
        if (!questionActive || currentQuestion == null || resultSequenceRunning)
            return;

        PlayClip(uiClickClip);
        questionActive = false;

        if (currentQuestion.isImpossibleAtStart)
        {
            correctCannotServeOrders++;
            UpdateScoreUI();
            StartCoroutine(ResultFeedbackSequence(true, "Correct! This order cannot be served. 0 reward", correctFeedbackColor, correctClip, true));
        }
        else
        {
            ApplyWrongOrderPenalty();
            StartCoroutine(ResultFeedbackSequence(false, "This order can be served. -" + wrongOrderPenalty, wrongFeedbackColor, wrongClip, true));
        }
    }

    private void GenerateSessionQuestions()
    {
        generatedQuestions.Clear();
        HashSet<string> usedSignatures = new HashSet<string>();

        int targetRounds = Mathf.Max(10, rounds);
        int safety = targetRounds * Mathf.Max(50, maxGenerationAttemptsPerQuestion);

        while (generatedQuestions.Count < targetRounds && safety > 0)
        {
            safety--;
            RuntimeQuestion question = GenerateQuestionCandidate();
            if (question == null || string.IsNullOrEmpty(question.signature))
                continue;

            if (usedSignatures.Add(question.signature))
                generatedQuestions.Add(question);
        }

        while (generatedQuestions.Count < targetRounds)
        {
            RuntimeQuestion fallback = GenerateQuestionCandidate();
            if (fallback == null)
                break;

            fallback.signature += "_fallback_" + generatedQuestions.Count;
            generatedQuestions.Add(fallback);
        }
    }

    private RuntimeQuestion GenerateQuestionCandidate()
    {
        List<PortionItemData> questionItems = GetQuestionItemPool();
        if (questionItems.Count == 0)
            return null;

        int portionCount = PickPortionCount();
        int requestMin = Mathf.Max(1, minRequestsPerQuestion);
        int requestMax = Mathf.Max(requestMin, maxRequestsPerQuestion);
        int requestCount = UnityEngine.Random.Range(requestMin, requestMax + 1);
        requestCount = Mathf.Clamp(requestCount, 1, Mathf.Min(questionItems.Count, 4));

        Shuffle(questionItems);

        RuntimeQuestion question = new RuntimeQuestion();
        question.portionCount = portionCount;

        int remainingSlices = portionCount;
        for (int i = 0; i < requestCount; i++)
        {
            int remainingRequestsAfterThis = requestCount - i - 1;
            int maxUnitsForThisRequest = remainingSlices - remainingRequestsAfterThis;
            if (avoidWholePizzaFractions)
                maxUnitsForThisRequest = Mathf.Min(maxUnitsForThisRequest, portionCount - 1);

            if (maxUnitsForThisRequest <= 0)
                break;

            OperationType operation = PickValidOperation(maxUnitsForThisRequest, portionCount);
            int minUnits = operation == OperationType.Addition ? 2 : 1;
            if (maxUnitsForThisRequest < minUnits)
                operation = OperationType.Direct;

            minUnits = operation == OperationType.Addition ? 2 : 1;
            if (maxUnitsForThisRequest < minUnits)
                continue;

            int targetUnits = UnityEngine.Random.Range(minUnits, maxUnitsForThisRequest + 1);
            RuntimeRequest request = GenerateRequest(questionItems[i], portionCount, operation, targetUnits);
            if (request == null || request.requiredUnits <= 0)
                continue;

            question.requests.Add(request);
            remainingSlices -= request.requiredUnits;
        }

        if (question.requests.Count == 0)
            return null;

        if (GetTotalRequiredUnits(question) > question.portionCount)
            return null;

        BuildInitialStock(question);
        question.isImpossibleAtStart = IsQuestionImpossible(question);
        question.signature = BuildSignature(question);
        return question;
    }

    private int PickPortionCount()
    {
        int minPieces = Mathf.Clamp(minPortionCount, 2, 12);
        int maxPieces = Mathf.Clamp(maxPortionCount, minPieces, 12);

        if (!useCommonPortionCountsOnly)
            return UnityEngine.Random.Range(minPieces, maxPieces + 1);

        int[] commonCounts = { 2, 3, 4, 6, 8, 10, 12 };
        List<int> valid = new List<int>();
        for (int i = 0; i < commonCounts.Length; i++)
        {
            if (commonCounts[i] >= minPieces && commonCounts[i] <= maxPieces)
                valid.Add(commonCounts[i]);
        }

        if (valid.Count == 0)
            return UnityEngine.Random.Range(minPieces, maxPieces + 1);

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    private OperationType PickValidOperation(int maxUnitsForThisRequest, int portionCount)
    {
        List<OperationType> possible = new List<OperationType>();
        possible.Add(OperationType.Direct);

        if (maxUnitsForThisRequest >= 2)
            possible.Add(OperationType.Addition);

        if (portionCount >= 3 && maxUnitsForThisRequest >= 1)
            possible.Add(OperationType.Subtraction);

        for (int guard = 0; guard < 10; guard++)
        {
            OperationType picked = PickOperation();
            if (possible.Contains(picked))
                return picked;
        }

        return possible[UnityEngine.Random.Range(0, possible.Count)];
    }

    private RuntimeRequest GenerateRequest(PortionItemData item, int portionCount, OperationType operationType, int targetUnits)
    {
        if (item == null)
            return null;

        targetUnits = Mathf.Clamp(targetUnits, 1, Mathf.Max(1, portionCount - 1));

        RuntimeRequest request = new RuntimeRequest
        {
            itemId = item.id,
            itemName = item.displayName,
            operationType = operationType,
            requiredUnits = targetUnits
        };

        if (operationType == OperationType.Direct)
        {
            AddDirectTerm(request, targetUnits, portionCount);
            return request;
        }

        if (operationType == OperationType.Addition)
        {
            if (targetUnits < 2)
            {
                request.operationType = OperationType.Direct;
                AddDirectTerm(request, targetUnits, portionCount);
                return request;
            }

            int first = UnityEngine.Random.Range(1, targetUnits);
            int second = targetUnits - first;
            bool simplifyOperationTerms = additionDifficulty != StandardQuestionDifficulty.Easy
                && !keepOperationFractionsOnPizzaDenominator;
            request.terms.Add(new FractionTerm(first, portionCount, simplifyOperationTerms));
            request.terms.Add(new FractionTerm(second, portionCount, simplifyOperationTerms));
            return request;
        }

        if (operationType == OperationType.Subtraction)
        {
            if (subtractionDifficulty == SubtractionQuestionDifficulty.Hard
                && UnityEngine.Random.value < Mathf.Clamp01(hardMixedNumberChance)
                && TryAddHardMixedNumberSubtraction(request, targetUnits, portionCount))
            {
                return request;
            }

            int maxSecond = (portionCount - 1) - targetUnits;
            if (maxSecond < 1)
            {
                request.operationType = OperationType.Direct;
                AddDirectTerm(request, targetUnits, portionCount);
                return request;
            }

            int second = UnityEngine.Random.Range(1, maxSecond + 1);
            int first = targetUnits + second;
            bool simplifyOperationTerms = subtractionDifficulty != SubtractionQuestionDifficulty.Easy
                && !keepOperationFractionsOnPizzaDenominator;
            request.terms.Add(new FractionTerm(first, portionCount, simplifyOperationTerms));
            request.terms.Add(new FractionTerm(second, portionCount, simplifyOperationTerms));
            return request;
        }

        return null;
    }

    private void AddDirectTerm(RuntimeRequest request, int targetUnits, int portionCount)
    {
        if (request == null)
            return;

        bool simplify = directDifficulty != StandardQuestionDifficulty.Easy;
        request.terms.Add(new FractionTerm(targetUnits, portionCount, simplify));
    }

    private bool TryAddHardMixedNumberSubtraction(RuntimeRequest request, int targetUnits, int portionCount)
    {
        if (request == null || portionCount < 2 || targetUnits <= 0 || targetUnits >= portionCount)
            return false;

        // Prefer two mixed-number operands. If that is not possible (for example, halves),
        // use one mixed number minus a whole number. The result is always targetUnits/portionCount.
        int fractionalOffset = -1;
        for (int guard = 0; guard < 20; guard++)
        {
            int candidate = UnityEngine.Random.Range(1, portionCount);
            if ((candidate + targetUnits) % portionCount != 0)
            {
                fractionalOffset = candidate;
                break;
            }
        }

        if (fractionalOffset < 0)
            fractionalOffset = 0;

        int wholeOffset = UnityEngine.Random.Range(1, 3);
        int secondNumerator = wholeOffset * portionCount + fractionalOffset;
        int firstNumerator = secondNumerator + targetUnits;

        FractionTerm firstTerm = new FractionTerm(firstNumerator, portionCount, true, true);
        FractionTerm secondTerm = new FractionTerm(secondNumerator, portionCount, true, true);

        if (!firstTerm.ShouldDisplayAsMixedNumber && !secondTerm.ShouldDisplayAsMixedNumber)
            return false;

        request.terms.Add(firstTerm);
        request.terms.Add(secondTerm);
        return true;
    }

    private void BuildInitialStock(RuntimeQuestion question)
    {
        question.initialStockByItemId.Clear();

        bool makeImpossible = allowImpossibleOrders && UnityEngine.Random.value < impossibleOrderChance;
        int impossibleRequestIndex = makeImpossible ? UnityEngine.Random.Range(0, question.requests.Count) : -1;
        string impossibleItemId = impossibleRequestIndex >= 0 ? question.requests[impossibleRequestIndex].itemId : string.Empty;

        for (int i = 0; i < items.Count; i++)
        {
            PortionItemData item = items[i];
            if (item == null || string.IsNullOrEmpty(item.id))
                continue;

            int requiredUnits = GetRequiredUnitsForItem(question, item.id);
            int stock;

            if (requiredUnits > 0)
            {
                bool forceImpossible = makeImpossible && item.id == impossibleItemId;
                if (forceImpossible)
                {
                    stock = UnityEngine.Random.Range(0, Mathf.Max(1, requiredUnits));
                }
                else
                {
                    int extraMin = Mathf.Max(0, solvableExtraStockMin);
                    int extraMax = Mathf.Max(extraMin, solvableExtraStockMax);
                    stock = requiredUnits + UnityEngine.Random.Range(extraMin, extraMax + 1);
                }
            }
            else
            {
                int min = Mathf.Max(0, item.distractorStockMin);
                int max = Mathf.Max(min, item.distractorStockMax);
                stock = UnityEngine.Random.Range(min, max + 1);
            }

            question.initialStockByItemId[item.id] = stock;
        }
    }

    private bool IsQuestionImpossible(RuntimeQuestion question)
    {
        if (GetTotalRequiredUnits(question) > question.portionCount)
            return true;

        for (int i = 0; i < question.requests.Count; i++)
        {
            RuntimeRequest request = question.requests[i];
            int stock = question.initialStockByItemId.ContainsKey(request.itemId) ? question.initialStockByItemId[request.itemId] : 0;
            if (stock < request.requiredUnits)
                return true;
        }

        return false;
    }

    private void LoadCurrentQuestion()
    {
        if (generatedQuestions.Count == 0)
        {
            questionActive = false;
            ShowFeedback("No questions generated. Check items and settings.", wrongFeedbackColor);
            return;
        }

        if (currentQuestionIndex >= generatedQuestions.Count)
        {
            ShowResult();
            return;
        }

        currentQuestion = generatedQuestions[currentQuestionIndex];
        selectedCustomerMascotSprite = PickCustomerMascotSpriteForNewOrder();
        timer = questionTime;
        questionActive = !showOrderDetailsBeforeEachOrder;
        isPaused = false;
        resultSequenceRunning = false;
        hintUsedThisQuestion = false;
        orderDetailsOpen = false;
        orderDetailsPausesTimer = false;
        orderDetailsIntroWaiting = false;
        HidePizzaFeedbackOverlay();

        SetCannotCompleteButtonVisible(showCannotCompleteButton);
        SetHintButtonVisible(true);
        SetOrderDetailsButtonVisible(true);
        BuildPortions(currentQuestion.portionCount);
        BuildBasket(currentQuestion);
        ApplyConfiguredFonts();
        UpdateQuestionUI(currentQuestion);
        UpdateScoreUI();
        UpdateTimerUI();
        UpdateProgressUI();
        ShowFeedback("Drag items to the correct portions.", neutralFeedbackColor);

        if (showOrderDetailsBeforeEachOrder)
            OpenOrderDetailsForCurrentOrder(true);
    }

    private void BuildPortions(int portionCount)
    {
        ClearActiveDropZones();
        EnsureTemplatesExist();

        if (portionRoot == null || dropZoneTemplate == null)
        {
            ShowFeedback("Portion template missing.", wrongFeedbackColor);
            return;
        }

        portionCount = Mathf.Clamp(portionCount, 2, 12);
        float anglePerPortion = 360f / portionCount;
        Vector2 size = portionRoot.rect.size;
        if (size.x <= 1f || size.y <= 1f)
            size = new Vector2(620f, 620f);

        for (int i = 0; i < portionCount; i++)
        {
            FractionPortionDropZone zone = Instantiate(dropZoneTemplate, portionRoot);
            zone.gameObject.name = "Portion Drop Zone " + (i + 1);
            if (zone.GetComponent<CanvasRenderer>() == null)
                zone.gameObject.AddComponent<CanvasRenderer>();
            if (zone.wedgeGraphic == null)
                zone.wedgeGraphic = zone.GetComponent<FractionPortionWedgeGraphic>();
            if (zone.wedgeGraphic != null)
                zone.wedgeGraphic.visualGapPercent = portionCutGapPercent;
            zone.gameObject.SetActive(true);

            RectTransform rect = zone.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            float startAngle = i * anglePerPortion;
            float endAngle = startAngle + anglePerPortion;
            Color color = i % 2 == 0 ? evenPortionColor : oddPortionColor;

            zone.Setup(this, i, startAngle, endAngle, color);
            if (zone.portionNumberText != null)
                zone.portionNumberText.gameObject.SetActive(showPortionNumbers);
            activeDropZones.Add(zone);
        }
    }

    private void BuildBasket(RuntimeQuestion question)
    {
        ClearBasketCards();
        EnsureDefaultItemsIfEmpty();
        EnsureTemplatesExist();

        if (basketRoot == null || basketCardTemplate == null)
        {
            ShowFeedback("Basket template missing.", wrongFeedbackColor);
            return;
        }

        int created = 0;
        for (int i = 0; i < items.Count; i++)
        {
            PortionItemData item = items[i];
            if (item == null || string.IsNullOrEmpty(item.id))
                continue;

            int stock = question.initialStockByItemId.ContainsKey(item.id) ? question.initialStockByItemId[item.id] : 0;
            FractionPortionBasketCard card = Instantiate(basketCardTemplate, basketRoot);
            card.gameObject.name = "Basket Card - " + item.displayName;
            card.gameObject.SetActive(true);
            card.Setup(this, item, stock);
            activeBasketCards[item.id] = card;
            created++;
        }

        if (created == 0)
            ShowFeedback("Basket has no valid items.", wrongFeedbackColor);
    }

    private void CheckAnswer()
    {
        if (currentQuestion == null)
            return;

        for (int i = 0; i < currentQuestion.requests.Count; i++)
        {
            RuntimeRequest request = currentQuestion.requests[i];
            int placed = CountPlaced(request.itemId);

            if (placed > request.requiredUnits)
            {
                ShowFeedback("Too many " + request.itemName + ".", wrongFeedbackColor);
                PlayClip(wrongClip);
                return;
            }
        }

        for (int i = 0; i < currentQuestion.requests.Count; i++)
        {
            RuntimeRequest request = currentQuestion.requests[i];
            int placed = CountPlaced(request.itemId);

            if (placed != request.requiredUnits)
            {
                ShowFeedback("Keep going.", neutralFeedbackColor);
                return;
            }
        }

        questionActive = false;
        successfulOrders++;
        score += Mathf.Max(0, perfectOrderReward);
        UpdateScoreUI();

        if (autoNextQuestion)
            StartCoroutine(ResultFeedbackSequence(true, "Perfect order! +" + perfectOrderReward, correctFeedbackColor, correctClip, true));
        else
            StartCoroutine(ResultFeedbackSequence(true, "Perfect order! +" + perfectOrderReward, correctFeedbackColor, correctClip, false));
    }

    private IEnumerator GoNextAfterDelay()
    {
        yield return new WaitForSeconds(nextDelay);
        currentQuestionIndex++;
        LoadCurrentQuestion();
    }

    private IEnumerator ResultFeedbackSequence(bool isPositive, string popupMessage, Color feedbackColor, AudioClip clip, bool advanceAfter)
    {
        if (resultSequenceRunning)
            yield break;

        resultSequenceRunning = true;
        questionActive = false;
        SetHintButtonVisible(false);
        SetCannotCompleteButtonVisible(false);
        SetOrderDetailsButtonVisible(false);
        CloseOrderDetailsInstant();
        PlayClip(clip);

        if (useSequentialResultFeedback)
            yield return AnimatePizzaFeedback(isPositive, feedbackColor);

        if (popupAfterPizzaDelay > 0f)
            yield return new WaitForSeconds(popupAfterPizzaDelay);

        ShowFeedback(popupMessage, feedbackColor);

        if (!advanceAfter)
        {
            resultSequenceRunning = false;
            yield break;
        }

        yield return new WaitForSeconds(Mathf.Max(0.05f, nextDelay));
        currentQuestionIndex++;
        LoadCurrentQuestion();
    }

    private IEnumerator AnimatePizzaFeedback(bool isPositive, Color feedbackColor)
    {
        RectTransform target = pizzaFeedbackTarget != null ? pizzaFeedbackTarget : portionRoot;
        if (target == null)
            yield break;

        yield return AnimatePizzaFeedbackWithDOTween(target, isPositive, feedbackColor);
    }

    private IEnumerator AnimatePizzaFeedbackWithDOTween(RectTransform target, bool isPositive, Color feedbackColor)
    {
        Vector3 originalScale = target.localScale;
        Vector2 originalPosition = target.anchoredPosition;
        Vector3 pulseScale = originalScale * pizzaFeedbackScaleAmount;
        string overlayText = isPositive ? "ORDER DONE" : "CHECK ORDER";
        SetupPizzaFeedbackOverlay(feedbackColor, overlayText);

        target.DOKill();
        if (pizzaFeedbackOverlayGroup != null)
            pizzaFeedbackOverlayGroup.DOKill();

        Sequence sequence = DOTween.Sequence();
        if (isPositive)
        {
            sequence.Append(target.DOScale(pulseScale, pizzaFeedbackPulseDuration).SetEase(Ease.OutBack));
            sequence.Join(target.DOPunchRotation(new Vector3(0f, 0f, 3.5f), pizzaFeedbackPulseDuration, 4, 0.45f));
        }
        else
        {
            sequence.Append(target.DOScale(pulseScale, pizzaFeedbackPulseDuration).SetEase(Ease.OutQuad));
            sequence.Join(target.DOShakeAnchorPos(pizzaFeedbackPulseDuration + pizzaFeedbackHoldDuration, new Vector2(wrongFeedbackShakeDistance, 0f), 14, 90f, false, true));
        }
        if (pizzaFeedbackOverlayGroup != null)
            sequence.Join(pizzaFeedbackOverlayGroup.DOFade(1f, pizzaFeedbackPulseDuration));
        sequence.AppendInterval(pizzaFeedbackHoldDuration);
        sequence.Append(target.DOScale(originalScale, pizzaFeedbackPulseDuration).SetEase(Ease.InOutSine));
        if (pizzaFeedbackOverlayGroup != null)
            sequence.Join(pizzaFeedbackOverlayGroup.DOFade(0f, pizzaFeedbackPulseDuration));
        yield return sequence.WaitForCompletion();
        target.localScale = originalScale;
        target.anchoredPosition = originalPosition;
        target.localRotation = Quaternion.identity;
        HidePizzaFeedbackOverlay();
    }

    private IEnumerator AnimatePizzaFeedbackCoroutine(RectTransform target, bool isPositive, Color feedbackColor)
    {
        Vector3 originalScale = target.localScale;
        Vector2 originalPosition = target.anchoredPosition;
        Vector3 pulseScale = originalScale * pizzaFeedbackScaleAmount;
        string overlayText = isPositive ? "ORDER DONE" : "CHECK ORDER";
        SetupPizzaFeedbackOverlay(feedbackColor, overlayText);

        float duration = Mathf.Max(0.05f, pizzaFeedbackPulseDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = EaseOutBack(Mathf.Clamp01(t));
            target.localScale = Vector3.LerpUnclamped(originalScale, pulseScale, isPositive ? eased : Smooth01(t));
            if (!isPositive)
                target.anchoredPosition = originalPosition + new Vector2(Mathf.Sin(t * Mathf.PI * 8f) * wrongFeedbackShakeDistance * (1f - t), 0f);
            if (pizzaFeedbackOverlayGroup != null)
                pizzaFeedbackOverlayGroup.alpha = Smooth01(t);
            yield return null;
        }

        target.localScale = pulseScale;
        target.anchoredPosition = originalPosition;
        if (pizzaFeedbackOverlayGroup != null)
            pizzaFeedbackOverlayGroup.alpha = 1f;

        if (pizzaFeedbackHoldDuration > 0f)
            yield return new WaitForSeconds(pizzaFeedbackHoldDuration);

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.localScale = Vector3.Lerp(pulseScale, originalScale, Smooth01(t));
            if (pizzaFeedbackOverlayGroup != null)
                pizzaFeedbackOverlayGroup.alpha = 1f - Smooth01(t);
            yield return null;
        }

        target.localScale = originalScale;
        target.anchoredPosition = originalPosition;
        HidePizzaFeedbackOverlay();
    }

    private void SetupPizzaFeedbackOverlay(Color feedbackColor, string message)
    {
        if (pizzaFeedbackOverlayImage != null)
        {
            Color overlayColor = feedbackColor;
            overlayColor.a = 0.34f;
            pizzaFeedbackOverlayImage.color = overlayColor;
        }

        if (pizzaFeedbackOverlayText != null)
        {
            pizzaFeedbackOverlayText.text = message;
            pizzaFeedbackOverlayText.color = Color.white;
        }

        if (pizzaFeedbackOverlayGroup != null)
        {
            pizzaFeedbackOverlayGroup.gameObject.SetActive(true);
            pizzaFeedbackOverlayGroup.alpha = 0f;
            pizzaFeedbackOverlayGroup.blocksRaycasts = false;
            pizzaFeedbackOverlayGroup.interactable = false;
        }
    }

    private void HidePizzaFeedbackOverlay()
    {
        if (pizzaFeedbackOverlayGroup != null)
        {
            pizzaFeedbackOverlayGroup.alpha = 0f;
            pizzaFeedbackOverlayGroup.blocksRaycasts = false;
            pizzaFeedbackOverlayGroup.interactable = false;
        }
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private static float EaseOutBack(float value)
    {
        value = Mathf.Clamp01(value);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(value - 1f, 3f) + c1 * Mathf.Pow(value - 1f, 2f);
    }

    private void ShowResult()
    {
        questionActive = false;
        SetCannotCompleteButtonVisible(false);
        SetHintButtonVisible(false);
        SetOrderDetailsButtonVisible(false);
        CloseOrderDetailsInstant();
        HidePizzaFeedbackOverlay();
        PlayClip(completeClip);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultContinueButton != null)
            resultContinueButton.gameObject.SetActive(useBloomRewardSystem);

        if (resultText != null)
        {
            resultText.text = "Game Complete!"
                + "\nSuccessful Orders: " + successfulOrders
                + "\nCorrect Can't Serve: " + correctCannotServeOrders
                + "\nWrong Orders: " + wrongOrders + " (-" + (wrongOrders * wrongOrderPenalty) + ")"
                + "\nHints Used: " + hintsUsed + " (-" + (hintsUsed * hintCost) + ")"
                + "\nScore: " + score;
        }
    }

    private OperationType PickOperation()
    {
        if (questionMode == QuestionMode.DirectFractionOnly)
            return OperationType.Direct;
        if (questionMode == QuestionMode.FractionAdditionOnly)
            return OperationType.Addition;
        if (questionMode == QuestionMode.FractionSubtractionOnly)
            return OperationType.Subtraction;

        int directWeight = mixedMode.includeDirect ? Mathf.Max(0, mixedMode.directWeight) : 0;
        int additionWeight = mixedMode.includeAddition ? Mathf.Max(0, mixedMode.additionWeight) : 0;
        int subtractionWeight = mixedMode.includeSubtraction ? Mathf.Max(0, mixedMode.subtractionWeight) : 0;
        int total = directWeight + additionWeight + subtractionWeight;

        if (total <= 0)
            return OperationType.Direct;

        int roll = UnityEngine.Random.Range(0, total);
        if (roll < directWeight)
            return OperationType.Direct;

        roll -= directWeight;
        if (roll < additionWeight)
            return OperationType.Addition;

        return OperationType.Subtraction;
    }

    private List<PortionItemData> GetQuestionItemPool()
    {
        List<PortionItemData> pool = new List<PortionItemData>();
        for (int i = 0; i < items.Count; i++)
        {
            PortionItemData item = items[i];
            if (item != null && item.canAppearInQuestions && !string.IsNullOrEmpty(item.id))
                pool.Add(item);
        }

        return pool;
    }

    private bool IsRequestedItem(string itemId)
    {
        if (currentQuestion == null)
            return false;

        for (int i = 0; i < currentQuestion.requests.Count; i++)
        {
            if (currentQuestion.requests[i].itemId == itemId)
                return true;
        }

        return false;
    }

    private int GetRequiredUnitsForItem(RuntimeQuestion question, string itemId)
    {
        int count = 0;
        for (int i = 0; i < question.requests.Count; i++)
        {
            if (question.requests[i].itemId == itemId)
                count += question.requests[i].requiredUnits;
        }

        return count;
    }

    private int CountPlaced(string itemId)
    {
        int count = 0;
        for (int i = 0; i < activeDropZones.Count; i++)
        {
            if (activeDropZones[i] != null && activeDropZones[i].AssignedItemId == itemId)
                count++;
        }

        return count;
    }

    private int GetTotalRequiredUnits(RuntimeQuestion question)
    {
        int total = 0;
        if (question == null)
            return total;

        for (int i = 0; i < question.requests.Count; i++)
            total += question.requests[i].requiredUnits;

        return total;
    }

    public void ShowHint()
    {
        if ((!questionActive && !orderDetailsIntroWaiting) || currentQuestion == null)
            return;

        if (hintOncePerQuestion && hintUsedThisQuestion)
        {
            ShowFeedback("Hint already used for this order.", neutralFeedbackColor);
            return;
        }

        hintUsedThisQuestion = true;
        hintsUsed++;
        score -= Mathf.Max(0, hintCost);
        UpdateScoreUI();
        PlayClip(hintClip != null ? hintClip : uiClickClip);

        string title = "Chef Hint";
        string body = GetHintText(currentQuestion);
        OpenOrderDetailsPanel(title, body, chefMascotLabel, chefMascotSprite, true, 0f, false);
        ShowFeedback("Hint used. -" + hintCost, neutralFeedbackColor);
    }

    public void OpenOrderDetailsFromButton()
    {
        if (currentQuestion == null)
            return;

        PlayClip(uiClickClip);
        OpenOrderDetailsForCurrentOrder(false);
    }

    private void OpenOrderDetailsForCurrentOrder(bool beforeOrderStarts)
    {
        if (currentQuestion == null)
            return;

        string title = "Order " + (currentQuestionIndex + 1) + "/" + generatedQuestions.Count;
        string body = BuildOrderDetailsText(currentQuestion);
        float autoClose = beforeOrderStarts ? orderDetailsIntroAutoCloseSeconds : orderDetailsReviewAutoCloseSeconds;
        if (selectedCustomerMascotSprite == null)
            selectedCustomerMascotSprite = PickCustomerMascotSpriteForNewOrder();

        OpenOrderDetailsPanel(title, body, customerMascotLabel, selectedCustomerMascotSprite, beforeOrderStarts, autoClose, beforeOrderStarts);
    }

    private Sprite PickCustomerMascotSpriteForNewOrder()
    {
        Sprite picked = PickRandomCustomerMascotSprite();
        lastCustomerMascotSprite = picked;
        return picked;
    }

    private Sprite PickRandomCustomerMascotSprite()
    {
        bool hasFirst = customerMascotSprite != null;
        bool hasSecond = customerMascotSprite2 != null;

        if (hasFirst && hasSecond)
        {
            if (avoidRepeatingCustomerMascot)
            {
                if (lastCustomerMascotSprite == customerMascotSprite)
                    return customerMascotSprite2;

                if (lastCustomerMascotSprite == customerMascotSprite2)
                    return customerMascotSprite;
            }

            return UnityEngine.Random.value < 0.5f ? customerMascotSprite : customerMascotSprite2;
        }

        if (hasFirst)
            return customerMascotSprite;

        if (hasSecond)
            return customerMascotSprite2;

        return null;
    }

    private void OpenOrderDetailsPanel(string title, string body, string mascotLabel, Sprite mascotSprite, bool pauseTimer, float autoCloseSeconds, bool introBeforeOrder)
    {
        if (orderDetailsPanel == null)
            return;

        if (orderDetailsAutoCloseRoutine != null)
        {
            StopCoroutine(orderDetailsAutoCloseRoutine);
            orderDetailsAutoCloseRoutine = null;
        }

        orderDetailsOpen = true;
        orderDetailsPausesTimer = pauseTimer;
        orderDetailsIntroWaiting = introBeforeOrder;
        if (introBeforeOrder)
            questionActive = false;

        if (orderDetailsTitleText != null)
            orderDetailsTitleText.text = title;
        if (orderDetailsBodyText != null)
            orderDetailsBodyText.text = body;
        if (orderDetailsMascotText != null)
            orderDetailsMascotText.text = mascotLabel;

        ApplyOrderDetailsMascotSprite(mascotSprite);

        orderDetailsPanel.SetActive(true);
        orderDetailsPanel.transform.SetAsLastSibling();

        RectTransform panelRect = orderDetailsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.DOKill();
            panelRect.localScale = Vector3.one * 0.94f;
            panelRect.DOScale(Vector3.one, Mathf.Max(0.05f, orderDetailsAnimationDuration)).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (orderDetailsCanvasGroup != null)
        {
            orderDetailsCanvasGroup.DOKill();
            orderDetailsCanvasGroup.alpha = 0f;
            orderDetailsCanvasGroup.blocksRaycasts = true;
            orderDetailsCanvasGroup.interactable = true;
            orderDetailsCanvasGroup.DOFade(1f, Mathf.Max(0.05f, orderDetailsAnimationDuration)).SetUpdate(true);
        }

        if (autoCloseSeconds > 0f)
            orderDetailsAutoCloseRoutine = StartCoroutine(AutoCloseOrderDetailsRoutine(autoCloseSeconds));
    }

    private IEnumerator AutoCloseOrderDetailsRoutine(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        CloseOrderDetailsPanel();
    }

    private void ApplyOrderDetailsMascotSprite(Sprite mascotSprite)
    {
        if (orderDetailsMascotImage == null)
            return;

        orderDetailsMascotImage.sprite = mascotSprite;
        orderDetailsMascotImage.preserveAspect = true;

        if (mascotSprite != null)
        {
            orderDetailsMascotImage.color = Color.white;

            if (orderDetailsMascotPlaceholderText != null)
                orderDetailsMascotPlaceholderText.gameObject.SetActive(!hideMascotPlaceholderTextWhenSpriteAssigned);
        }
        else
        {
            orderDetailsMascotImage.color = new Color(0.28f, 0.48f, 0.36f, 1f);

            if (orderDetailsMascotPlaceholderText != null)
                orderDetailsMascotPlaceholderText.gameObject.SetActive(true);
        }
    }

    public void CloseOrderDetailsPanel()
    {
        if (!orderDetailsOpen && orderDetailsPanel != null && !orderDetailsPanel.activeSelf)
            return;

        if (orderDetailsAutoCloseRoutine != null)
        {
            StopCoroutine(orderDetailsAutoCloseRoutine);
            orderDetailsAutoCloseRoutine = null;
        }

        bool shouldStartOrder = orderDetailsIntroWaiting;
        orderDetailsOpen = false;
        orderDetailsPausesTimer = false;
        orderDetailsIntroWaiting = false;

        if (orderDetailsPanel != null)
            orderDetailsPanel.SetActive(false);

        if (shouldStartOrder)
        {
            timer = questionTime;
            questionActive = true;
            UpdateTimerUI();
            ShowFeedback("Drag items to the correct portions.", neutralFeedbackColor);
        }
    }

    private void CloseOrderDetailsInstant()
    {
        if (orderDetailsAutoCloseRoutine != null)
        {
            StopCoroutine(orderDetailsAutoCloseRoutine);
            orderDetailsAutoCloseRoutine = null;
        }

        orderDetailsOpen = false;
        orderDetailsPausesTimer = false;
        orderDetailsIntroWaiting = false;
        if (orderDetailsPanel != null)
            orderDetailsPanel.SetActive(false);
    }

    private string BuildOrderDetailsText(RuntimeQuestion question)
    {
        if (question == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        builder.Append("Pizza size: ").Append(question.portionCount).Append(" slices");
        builder.Append("\n\n");
        builder.Append("Order:\n");

        for (int i = 0; i < question.requests.Count; i++)
        {
            RuntimeRequest request = question.requests[i];
            builder.Append("• Cover ");

            for (int t = 0; t < request.terms.Count; t++)
            {
                if (t > 0)
                    builder.Append(request.operationType == OperationType.Subtraction ? " − " : " + ");

                builder.Append(FormatFractionRich(request.terms[t]));
            }

            builder.Append(" of the pizza with ").Append(request.itemName);

            if (i < question.requests.Count - 1)
                builder.Append("\n");
        }

        return builder.ToString();
    }

    private string FormatFractionRich(FractionTerm term)
    {
        if (term == null)
            return string.Empty;

        if (term.denominator == 1)
            return term.numerator.ToString();

        string slashSpace = orderFractionSlashSideSpace ?? string.Empty;
        string slash = string.IsNullOrEmpty(orderFractionSlash) ? "/" : orderFractionSlash;
        int sizePercent = Mathf.Clamp(orderFractionSizePercent, 70, 150);
        float numeratorOffset = Mathf.Max(0f, orderFractionNumeratorOffsetEm);
        float denominatorOffset = Mathf.Max(0f, orderFractionDenominatorOffsetEm);
        int displayNumerator = term.ShouldDisplayAsMixedNumber ? term.RemainderNumerator : term.numerator;
        string mixedPrefix = term.ShouldDisplayAsMixedNumber ? term.WholeNumber + " " : string.Empty;

        return mixedPrefix + "<size=" + sizePercent + "%><voffset=" + numeratorOffset.ToString("0.###") + "em>"
            + displayNumerator + "</voffset>"
            + slashSpace + slash + slashSpace
            + "<voffset=-" + denominatorOffset.ToString("0.###") + "em>"
            + term.denominator + "</voffset></size>";
    }

    private string GetHintText(RuntimeQuestion question)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Hint:\n");
        builder.Append("Pizza size: ").Append(question.portionCount).Append(" slices");
        builder.Append("\n\n");

        if (question.isImpossibleAtStart)
        {
            builder.Append("This order cannot be served with current stock.\n");
            for (int i = 0; i < question.requests.Count; i++)
            {
                RuntimeRequest request = question.requests[i];
                int stock = question.initialStockByItemId.ContainsKey(request.itemId) ? question.initialStockByItemId[request.itemId] : 0;
                builder.Append("• ").Append(request.itemName).Append(": needs ")
                    .Append(request.requiredUnits).Append(request.requiredUnits == 1 ? " slice" : " slices")
                    .Append(", stock has ").Append(stock);

                if (i < question.requests.Count - 1)
                    builder.Append("\n");
            }
            return builder.ToString();
        }

        builder.Append("Place these toppings:\n");
        for (int i = 0; i < question.requests.Count; i++)
        {
            RuntimeRequest request = question.requests[i];
            builder.Append("• ").Append(request.itemName).Append(": ")
                .Append(request.requiredUnits).Append(request.requiredUnits == 1 ? " slice" : " slices");

            if (i < question.requests.Count - 1)
                builder.Append("\n");
        }

        return builder.ToString();
    }

    private IEnumerator ShowBloomPreGameRoutine()
    {
        RewardManager rewardManager = RewardManager.Instance;
        if (rewardManager == null)
        {
            Debug.LogWarning("FractionPortionFill: RewardManager.Instance not found. Bloom pre-game skipped. Start from LoadingScene for full Bloom flow.");
            yield break;
        }

        rewardManager.ShowPreGame(GetBloomSkills());
        yield return new WaitUntil(() => rewardManager == null || rewardManager.IsPreGameComplete);
    }

    private List<SkillEntry> GetBloomSkills()
    {
        return new List<SkillEntry>
        {
            new SkillEntry(BloomSkillType.Understand, 100f),
            new SkillEntry(BloomSkillType.Apply, 100f, timeWeight: bloomApplyTimeWeight, accuracyWeight: bloomApplyAccuracyWeight),
            new SkillEntry(BloomSkillType.Analyze, 60f, timeWeight: bloomAnalyzeTimeWeight, accuracyWeight: bloomAnalyzeAccuracyWeight),
        };
    }

    public void ShowBloomPostGameFromResultButton()
    {
        PlayClip(uiClickClip);
        ShowBloomPostGame();
    }

    private void ShowBloomPostGame()
    {
        if (!useBloomRewardSystem || bloomPostGameShown)
            return;

        RewardManager rewardManager = RewardManager.Instance;
        if (rewardManager == null)
        {
            Debug.LogWarning("FractionPortionFill: RewardManager.Instance not found. Bloom post-game skipped. Start from LoadingScene for full Bloom flow.");
            return;
        }

        bloomPostGameShown = true;
        HidePanel(resultPanel);
        CloseOrderDetailsInstant();
        StopGameAudioForBloom();

        GameEvaluationData eval = BuildBloomEvaluationData();
        rewardManager.ShowPostGame(GetBloomSkills(), eval);
    }

    private GameEvaluationData BuildBloomEvaluationData()
    {
        int totalQuestions = generatedQuestions != null && generatedQuestions.Count > 0 ? generatedQuestions.Count : Mathf.Max(1, rounds);
        int correctOrders = successfulOrders + correctCannotServeOrders;
        float accuracyScore = Mathf.Clamp01((float)correctOrders / Mathf.Max(1, totalQuestions));

        float expectedMaxTime = bloomExpectedMaxTimeSeconds > 0f
            ? bloomExpectedMaxTimeSeconds
            : Mathf.Max(1f, totalQuestions * Mathf.Max(1f, questionTime));

        float timeTaken = Mathf.Max(0f, bloomActiveGameplayTime);
        float timeScore = Mathf.Clamp01(1f - (timeTaken / expectedMaxTime));

        return new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = accuracyScore,
            mistakeCount = wrongOrders,
            timeTaken = timeTaken
        };
    }

    private void StopGameAudioForBloom()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();

        if (audioSource != null)
            audioSource.Stop();
    }

    public void OnRewardScreenOpen()
    {
        StopGameAudioForBloom();
    }

    public void OnPlayAgain()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

    public void OnHome()
    {
        string targetScene = string.IsNullOrWhiteSpace(bloomHomeSceneName) ? "Loader Scene" : bloomHomeSceneName;
        if (RewardManager.Instance != null)
            RewardManager.Instance.HideAll();

        if (UnityAndroidMediator.Instance != null)
            UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

        //if (GameLoader.Instance != null)
        //    GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");
        SceneManager.LoadScene(targetScene);
    }

    private void ApplyWrongOrderPenalty()
    {
        wrongOrders++;
        score -= Mathf.Max(0, wrongOrderPenalty);
        UpdateScoreUI();
    }

    private void UpdateQuestionUI(RuntimeQuestion question)
    {
        if (question == null)
            return;

        if (questionRenderer != null)
        {
            questionRenderer.RenderQuestion(question, primaryFont, secondaryFont);
            return;
        }

        if (questionText == null)
            return;

        StringBuilder builder = new StringBuilder();
        builder.Append("Order for ").Append(question.portionCount).Append("-slice pizza:\n");
        for (int i = 0; i < question.requests.Count; i++)
        {
            builder.Append("• ");
            builder.Append(question.requests[i].GetQuestionText());
            if (i < question.requests.Count - 1)
                builder.Append("\n");
        }

        questionText.text = builder.ToString();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = useTimer ? "Time: " + Mathf.CeilToInt(timer) : string.Empty;
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
            progressText.text = "Round: " + (currentQuestionIndex + 1) + "/" + generatedQuestions.Count;

        if (portionCountText != null && currentQuestion != null)
            portionCountText.text = currentQuestion.portionCount + " slices";
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }

        if (feedbackPopup != null)
            feedbackPopup.Show(message, color);
    }

    private void ClearActiveDropZones()
    {
        for (int i = activeDropZones.Count - 1; i >= 0; i--)
        {
            if (activeDropZones[i] != null)
                Destroy(activeDropZones[i].gameObject);
        }

        activeDropZones.Clear();
    }

    private void ClearBasketCards()
    {
        foreach (KeyValuePair<string, FractionPortionBasketCard> pair in activeBasketCards)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        activeBasketCards.Clear();
    }

    private string BuildSignature(RuntimeQuestion question)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(question.portionCount).Append("|");
        builder.Append(question.isImpossibleAtStart ? "I" : "S").Append("|");
        builder.Append("T").Append(GetTotalRequiredUnits(question)).Append("|");
        for (int i = 0; i < question.requests.Count; i++)
        {
            RuntimeRequest request = question.requests[i];
            builder.Append(request.itemId).Append(":").Append(request.operationType).Append(":").Append(request.requiredUnits);
            for (int t = 0; t < request.terms.Count; t++)
                builder.Append(":").Append(request.terms[t].GetText());
            int stock = question.initialStockByItemId.ContainsKey(request.itemId) ? question.initialStockByItemId[request.itemId] : 0;
            builder.Append(":stock").Append(stock).Append("|");
        }

        return builder.ToString();
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PrepareAudioSources()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (musicSource == null)
        {
            Transform existing = transform.Find("Background Music Source");
            if (existing != null)
                musicSource = existing.GetComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("Background Music Source");
            musicGO.transform.SetParent(transform, false);
            musicSource = musicGO.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = loopBackgroundMusic;
        musicSource.volume = backgroundMusicVolume;
    }

    private void PlayBackgroundMusic()
    {
        if (!playBackgroundMusic || musicSource == null || backgroundMusicClip == null)
            return;

        musicSource.clip = backgroundMusicClip;
        musicSource.loop = loopBackgroundMusic;
        musicSource.volume = backgroundMusicVolume;

        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    private void SetCannotCompleteButtonVisible(bool visible)
    {
        if (cannotCompleteButton != null)
            cannotCompleteButton.gameObject.SetActive(visible);
    }

    private void SetHintButtonVisible(bool visible)
    {
        if (hintButton != null)
            hintButton.gameObject.SetActive(visible);
    }

    private void SetOrderDetailsButtonVisible(bool visible)
    {
        if (orderDetailsButton != null)
            orderDetailsButton.gameObject.SetActive(visible);
    }

    public void ApplyConfiguredFonts()
    {
        if (questionRenderer != null)
            questionRenderer.SetFonts(primaryFont, secondaryFont);

        if (rootCanvas == null || (primaryFont == null && secondaryFont == null))
            return;

        TMP_Text[] allTexts = rootCanvas.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text == null)
                continue;

            bool usePrimary = text == scoreText || text == pauseProfitText || IsPrimaryText(text.name);
            TMP_FontAsset chosenFont = usePrimary ? primaryFont : (secondaryFont != null ? secondaryFont : primaryFont);
            if (chosenFont != null)
                text.font = chosenFont;
        }
    }

    private bool IsPrimaryText(string textName)
    {
        if (string.IsNullOrEmpty(textName))
            return false;

        string lower = textName.ToLowerInvariant();
        return lower.Contains("title")
            || lower.Contains("question")
            || lower.Contains("result")
            || lower.Contains("score")
            || lower.Contains("round")
            || lower.Contains("timer")
            || lower.Contains("time")
            || lower.Contains("loading");
    }

    private void EnsureButtonListeners()
    {
        if (cannotCompleteButton != null)
        {
            cannotCompleteButton.onClick.RemoveListener(OnCannotCompletePressed);
            cannotCompleteButton.onClick.AddListener(OnCannotCompletePressed);
            cannotCompleteButton.gameObject.SetActive(showCannotCompleteButton);
        }

        if (hintButton != null)
        {
            hintButton.onClick.RemoveListener(ShowHint);
            hintButton.onClick.AddListener(ShowHint);
            hintButton.gameObject.SetActive(true);
        }

        if (orderDetailsButton != null)
        {
            orderDetailsButton.onClick.RemoveListener(OpenOrderDetailsFromButton);
            orderDetailsButton.onClick.AddListener(OpenOrderDetailsFromButton);
            orderDetailsButton.gameObject.SetActive(true);
        }

        if (orderDetailsContinueButton != null)
        {
            orderDetailsContinueButton.onClick.RemoveListener(CloseOrderDetailsPanel);
            orderDetailsContinueButton.onClick.AddListener(CloseOrderDetailsPanel);
        }

        if (resultContinueButton != null)
        {
            resultContinueButton.onClick.RemoveListener(ShowBloomPostGameFromResultButton);
            resultContinueButton.onClick.AddListener(ShowBloomPostGameFromResultButton);
            resultContinueButton.gameObject.SetActive(useBloomRewardSystem);
        }
    }

    private void ResolveSceneReferences()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
            rootCanvas = FindObjectOfType<Canvas>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (musicSource == null)
        {
            Transform music = transform.Find("Background Music Source");
            if (music != null)
                musicSource = music.GetComponent<AudioSource>();
        }

        if (questionRenderer == null && questionText != null)
            questionRenderer = questionText.GetComponentInParent<FractionPortionQuestionRenderer>();

        if (orderDetailsCanvasGroup == null && orderDetailsPanel != null)
            orderDetailsCanvasGroup = orderDetailsPanel.GetComponent<CanvasGroup>();

        if (howToGuidePanel == null && howToPlayPanel != null)
            howToGuidePanel = howToPlayPanel.GetComponentInChildren<FractionPortionHowToGuidePanel>(true);

        if (resultContinueButton == null && resultPanel != null)
        {
            Button[] resultButtons = resultPanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < resultButtons.Length; i++)
            {
                if (resultButtons[i] != null && resultButtons[i].name.ToLowerInvariant().Contains("continue"))
                {
                    resultContinueButton = resultButtons[i];
                    break;
                }
            }
        }
    }

    private void EnsureDefaultItemsIfEmpty()
    {
        if (items == null)
            items = new List<PortionItemData>();

        bool hasValidItem = false;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && !string.IsNullOrEmpty(items[i].id))
            {
                hasValidItem = true;
                break;
            }
        }

        if (hasValidItem)
            return;

        items.Clear();
        items.Add(new PortionItemData { id = "capsicum", displayName = "Capsicum", color = new Color(0.25f, 0.75f, 0.25f), distractorStockMin = 1, distractorStockMax = 4 });
        items.Add(new PortionItemData { id = "olive", displayName = "Olive", color = new Color(0.08f, 0.08f, 0.08f), distractorStockMin = 1, distractorStockMax = 4 });
        items.Add(new PortionItemData { id = "tomato", displayName = "Tomato", color = new Color(0.9f, 0.12f, 0.1f), distractorStockMin = 1, distractorStockMax = 4 });
        items.Add(new PortionItemData { id = "cheese", displayName = "Cheese", color = new Color(1f, 0.9f, 0.2f), distractorStockMin = 1, distractorStockMax = 4 });
    }

    private void EnsureTemplatesExist()
    {
        if (rootCanvas == null)
            return;

        RectTransform canvasRoot = rootCanvas.GetComponent<RectTransform>();

        if (dragLayer == null)
        {
            GameObject dragGO = new GameObject("Drag Layer - Runtime Auto Created", typeof(RectTransform));
            dragGO.transform.SetParent(canvasRoot, false);
            dragLayer = dragGO.GetComponent<RectTransform>();
            dragLayer.anchorMin = Vector2.zero;
            dragLayer.anchorMax = Vector2.one;
            dragLayer.offsetMin = Vector2.zero;
            dragLayer.offsetMax = Vector2.zero;
            dragLayer.SetAsLastSibling();
        }

        if (dropZoneTemplate != null && basketCardTemplate != null && dragVisualTemplate != null)
            return;

        GameObject templateRoot = new GameObject("Scene Templates - Runtime Auto Created");
        templateRoot.transform.SetParent(canvasRoot, false);
        templateRoot.SetActive(false);

        if (dropZoneTemplate == null)
            dropZoneTemplate = FractionPortionRuntimeTemplateFactory.CreateDropZoneTemplate(templateRoot.transform);

        if (basketCardTemplate == null)
            basketCardTemplate = FractionPortionRuntimeTemplateFactory.CreateBasketCardTemplate(templateRoot.transform);

        if (dragVisualTemplate == null)
            dragVisualTemplate = FractionPortionRuntimeTemplateFactory.CreateDragVisualTemplate(templateRoot.transform);
    }

    private static void HidePanel(GameObject panel)
    {
        if (panel != null)
            panel.SetActive(false);
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        if (a == 0)
            return Mathf.Max(1, b);
        if (b == 0)
            return Mathf.Max(1, a);

        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }

        return Mathf.Abs(a);
    }

    private static void Shuffle<T>(List<T> list)
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

public static class FractionPortionRuntimeTemplateFactory
{
    public static FractionPortionDropZone CreateDropZoneTemplate(Transform parent)
    {
        GameObject go = new GameObject("Drop Zone Template - Scene Only", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(620f, 620f);

        FractionPortionWedgeGraphic graphic = go.AddComponent<FractionPortionWedgeGraphic>();
        graphic.raycastTarget = true;

        FractionPortionDropZone zone = go.AddComponent<FractionPortionDropZone>();
        zone.wedgeGraphic = graphic;

        Image icon = CreateImage("Placed Item Icon", go.transform, new Vector2(0f, 0f), new Vector2(70f, 70f), Color.white, false);
        TMP_Text label = CreateText("Placed Item Label", go.transform, new Vector2(0f, -55f), new Vector2(190f, 44f), 22, TextAlignmentOptions.Center, Color.black);
        label.raycastTarget = false;
        label.gameObject.SetActive(false);
        TMP_Text number = CreateText("Portion Number", go.transform, new Vector2(0f, 0f), new Vector2(70f, 40f), 22, TextAlignmentOptions.Center, new Color(0.28f, 0.16f, 0.05f));
        number.raycastTarget = false;

        zone.itemIcon = icon;
        zone.itemLabel = label;
        zone.portionNumberText = number;
        go.SetActive(false);
        return zone;
    }

    public static FractionPortionBasketCard CreateBasketCardTemplate(Transform parent)
    {
        GameObject go = new GameObject("Basket Card Template - Scene Only", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 108f);

        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;
        bg.raycastTarget = true;

        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 108f;
        layout.preferredHeight = 108f;
        layout.flexibleHeight = 0f;

        go.AddComponent<CanvasGroup>();

        FractionPortionBasketCard card = go.AddComponent<FractionPortionBasketCard>();
        card.iconImage = CreateImage("Color Icon", go.transform, new Vector2(0f, 0f), new Vector2(58f, 58f), Color.white, false);
        card.nameText = CreateText("Name", go.transform, Vector2.zero, new Vector2(215f, 46f), 28, TextAlignmentOptions.MidlineLeft, Color.black);
        card.countText = CreateText("Count", go.transform, Vector2.zero, new Vector2(70f, 34f), 24, TextAlignmentOptions.Right, Color.black);

        RectTransform iconRect = card.iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(48f, 0f);

        RectTransform nameRect = card.nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 0.5f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.offsetMin = new Vector2(92f, -22f);
        nameRect.offsetMax = new Vector2(-82f, 22f);
        card.nameText.enableAutoSizing = true;
        card.nameText.fontSizeMin = 18f;
        card.nameText.fontSizeMax = 28f;
        card.nameText.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform countRect = card.countText.rectTransform;
        countRect.anchorMin = new Vector2(1f, 0.5f);
        countRect.anchorMax = new Vector2(1f, 0.5f);
        countRect.pivot = new Vector2(1f, 0.5f);
        countRect.anchoredPosition = new Vector2(-18f, -24f);
        countRect.sizeDelta = new Vector2(70f, 34f);

        go.SetActive(false);
        return card;
    }

    public static FractionPortionDragVisual CreateDragVisualTemplate(Transform parent)
    {
        GameObject go = new GameObject("Drag Visual Template - Scene Only", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 96f);

        Image bg = go.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.95f);
        bg.raycastTarget = false;
        go.AddComponent<CanvasGroup>();

        FractionPortionDragVisual visual = go.AddComponent<FractionPortionDragVisual>();
        visual.backgroundImage = bg;
        visual.iconImage = CreateImage("Color Icon", go.transform, Vector2.zero, new Vector2(70f, 70f), Color.white, false);
        visual.nameText = CreateText("Name", go.transform, new Vector2(62f, 0f), new Vector2(180f, 58f), 28, TextAlignmentOptions.MidlineLeft, Color.black);
        visual.nameText.raycastTarget = false;
        visual.nameText.gameObject.SetActive(false);
        visual.showName = false;
        go.SetActive(false);
        return visual;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool raycastTarget)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = name;
        return text;
    }
}
