using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class OddClawTutorialPracticeAnswer
{
    public string text;
    public Sprite sprite;
    public bool isCorrect;
    public Vector2 anchoredPosition;

    public OddClawTutorialPracticeAnswer(string textValue, bool correct, Vector2 position)
    {
        text = textValue;
        isCorrect = correct;
        anchoredPosition = position;
    }
}

public class OddClawFirstTimeTutorialController : MonoBehaviour
{
    private enum TutorialStage
    {
        None,
        ReadQuestion,
        InspectAnswers,
        WatchClaw,
        Demonstration,
        Practice,
        Success,
        Finishing
    }

    [Header("First-Time Behaviour")]
    public bool enableFirstTimeTutorial = true;
    [Tooltip("Plays the tutorial on every scene launch while enabled. Intended only for testing.")]
    public bool forcePlayForTesting;
    [Tooltip("When enabled, confirming Skip also prevents the tutorial from opening next time.")]
    public bool saveCompletionWhenSkipped = true;
    [Tooltip("The active scene name is appended automatically.")]
    public string tutorialSaveKeyPrefix = "OddClawCatch_InteractiveTutorialCompleted";

    [Header("Core References")]
    public OddClawCatchManager gameManager;
    public Canvas rootCanvas;
    public RectTransform tutorialRoot;
    public CanvasGroup tutorialCanvasGroup;
    public RectTransform handPointer;
    public Image handPointerImage;
    public RectTransform focusHighlight;
    public CanvasGroup focusCanvasGroup;
    public RectTransform instructionRoot;
    public CanvasGroup instructionCanvasGroup;
    public TMP_Text instructionText;

    [Header("Skip Confirmation")]
    public Button skipButton;
    public GameObject skipConfirmationPanel;
    public TMP_Text skipConfirmationText;
    public Button skipConfirmButton;
    public Button skipCancelButton;
    [TextArea(2, 4)] public string skipConfirmationMessage = "Skip this practice tutorial?";

    [Header("Editable Practice Question")]
    [TextArea(2, 4)] public string practiceQuestion = "Catch the EVEN number.";
    public OddClawAnswerDisplayMode practiceDisplayMode = OddClawAnswerDisplayMode.Text;
    [Tooltip("When enabled, sprite-based tutorial practice uses the Image Magnet Head. Text practice always keeps the normal claw.")]
    public bool useMagnetForImagePractice = true;
    public OddClawItemView practiceTextTemplateOverride;
    public OddClawItemView practiceImageTemplateOverride;
    public OddClawItemView practiceImageTextTemplateOverride;
    public List<OddClawTutorialPracticeAnswer> practiceAnswers = new List<OddClawTutorialPracticeAnswer>
    {
        new OddClawTutorialPracticeAnswer("3", false, new Vector2(-315f, 0f)),
        new OddClawTutorialPracticeAnswer("7", false, new Vector2(-105f, 0f)),
        new OddClawTutorialPracticeAnswer("4", true, new Vector2(105f, 0f)),
        new OddClawTutorialPracticeAnswer("9", false, new Vector2(315f, 0f))
    };
    [Range(0.5f, 1.5f)] public float practiceItemScale = 1f;
    public float practiceReachPadding = 70f;
    public float practiceRotationSpeed = 48f;
    public bool showAimGuideDuringPractice = true;

    [Header("Editable Instructions")]
    [TextArea(2, 4)] public string readQuestionInstruction =
        "Read the question and find the correct answer.\nClick anywhere to continue.";
    [TextArea(2, 4)] public string inspectAnswersInstruction =
        "Look at all the answers. Which one is correct?\nClick anywhere to continue.";
    [TextArea(2, 4)] public string watchClawInstruction =
        "Watch the claw. Wait until it points at the correct answer.\nClick anywhere to continue.";
    [TextArea(2, 4)] public string demonstrationInstruction =
        "Tap when the claw points at the correct answer.\nWatch closely.";
    [TextArea(2, 4)] public string practiceInstruction =
        "Your turn! Wait, aim, and tap to catch the correct answer.";
    [TextArea(2, 4)] public string missInstruction =
        "Almost! Wait until the claw points at the answer, then try again.";
    [TextArea(2, 4)] public string wrongInstruction =
        "That is not the correct answer. Try again!";
    [TextArea(2, 4)] public string successInstruction =
        "Great catch! You are ready to play.\nClick anywhere to start.";

    [Header("Pointer Placement")]
    public Vector2 questionPointerOffset = new Vector2(0f, -95f);
    [Tooltip("Placed below and to the right of each answer so the hand does not cover its text or appear to indicate the previous box.")]
    public Vector2 answersPointerOffset = new Vector2(55f, -95f);
    [Tooltip("Placed on the opposite side of the claw from the instruction panel.")]
    public Vector2 clawPointerOffset = new Vector2(120f, -10f);
    public RectTransform demonstrationTapTarget;
    public Vector2 demonstrationTapPosition = new Vector2(-360f, 120f);
    public Vector2 demonstrationTapPointerOffset = Vector2.zero;
    public Vector2 successPointerPosition = new Vector2(0f, -220f);
    public float pointerScreenPadding = 55f;
    [Tooltip("Automatically moves the pointer away if its visual rectangle would cover the instruction board.")]
    public bool keepPointerClearOfInstruction = true;
    public float pointerInstructionClearance = 24f;

    [Header("Instruction Placement")]
    public Vector2 questionInstructionOffset = new Vector2(0f, -205f);
    public Vector2 answersInstructionOffset = new Vector2(0f, 205f);
    public Vector2 clawInstructionOffset = new Vector2(-330f, -40f);
    [Tooltip("Fallback position used when automatic demonstration placement is disabled or a practice target cannot be found.")]
    public Vector2 demonstrationInstructionPosition = new Vector2(-300f, -300f);
    [Tooltip("Places the demonstration instruction at the screen side opposite the correct answer, keeping it clear of the animated claw path.")]
    public bool autoPlaceDemonstrationInstructionAwayFromClaw = true;
    public Vector2 practiceInstructionPosition = new Vector2(0f, 350f);
    public Vector2 successInstructionPosition = new Vector2(0f, 80f);
    public float instructionScreenPadding = 35f;
    public Vector2 highlightPadding = new Vector2(30f, 22f);

    [HideInInspector] public int positioningRevision;

    [Header("Smooth Animation")]
    public float rootFadeDuration = 0.25f;
    public float stageTransitionDelay = 0.18f;
    public float instructionFadeDuration = 0.2f;
    public float pointerMoveDuration = 0.42f;
    public float pointerTapDuration = 0.28f;
    public float answerRevealDelay = 0.22f;
    public float instructionBreathScale = 1.025f;
    public float instructionBreathDuration = 0.85f;
    public float ghostFadeDuration = 0.2f;
    public float ghostAimDuration = 0.65f;
    public float ghostExtendDuration = 0.75f;
    public float ghostHoldDuration = 0.35f;
    [Range(0.05f, 1f)] public float ghostTransparency = 0.42f;
    public float inactivityReplayDelay = 15f;

    private readonly List<OddClawItemView> _practiceItems = new List<OddClawItemView>();
    private RectTransform _canvasRect;
    private TutorialStage _stage;
    private Action _completionCallback;
    private Coroutine _flowRoutine;
    private bool _running;
    private bool _acceptAdvanceTap;
    private bool _practiceAwaitingResult;
    private bool _confirmationOpen;
    private float _inactiveTime;
    private string _originalQuestionText;
    private string _originalHeaderText;
    private float _originalRotationSpeed;
    private float _originalExtensionLength;
    private bool _originalAimGuide;
    private bool _originalImageMagnetMode;
    private RectTransform _ghostRoot;
    private RectTransform _ghostArm;
    private RectTransform _ghostHead;
    private CanvasGroup _ghostCanvasGroup;
    private RectTransform _ghostItem;
    private CanvasGroup _ghostItemCanvasGroup;
    private Vector2 _ghostBaseArmSize;
    private Vector2 _ghostBaseHeadPosition;
    private float _ghostBaseReach;
    private float _ghostHeadDirection = -1f;

    public bool IsRunning => _running;

    private void Awake()
    {
        ResolveReferences();
        HookButtons();
        HideImmediate();
    }

    private void OnDestroy()
    {
        UnhookButtons();
        KillTutorialTweens();
    }

    private void Update()
    {
        if (!_running
            || _confirmationOpen
            || _flowRoutine != null
            || _stage == TutorialStage.Demonstration
            || _stage == TutorialStage.Finishing)
            return;

        if (_stage == TutorialStage.Practice && !_practiceAwaitingResult)
        {
            _inactiveTime += Time.unscaledDeltaTime;
            if (_inactiveTime >= Mathf.Max(2f, inactivityReplayDelay))
            {
                _inactiveTime = 0f;
                StartFlow(ReplayDemonstrationThenPractice());
                return;
            }
        }

        if (!WasTutorialTapPressed())
            return;

        _inactiveTime = 0f;

        if (_stage == TutorialStage.ReadQuestion && _acceptAdvanceTap)
        {
            StartFlow(ShowAnswersStage());
        }
        else if (_stage == TutorialStage.InspectAnswers && _acceptAdvanceTap)
        {
            StartFlow(ShowWatchClawStage());
        }
        else if (_stage == TutorialStage.WatchClaw && _acceptAdvanceTap)
        {
            StartFlow(PlayDemonstrationThenPractice());
        }
        else if (_stage == TutorialStage.Practice && !_practiceAwaitingResult)
        {
            TryPracticeCatch();
        }
        else if (_stage == TutorialStage.Success && _acceptAdvanceTap)
        {
            StartFlow(FinishTutorial(true));
        }
    }

    public bool ShouldPlayTutorial()
    {
        if (!enableFirstTimeTutorial)
            return false;

        if (forcePlayForTesting)
            return true;

        return PlayerPrefs.GetInt(GetSceneSaveKey(), 0) == 0;
    }

    public void BeginTutorial(OddClawCatchManager manager, Action completionCallback)
    {
        if (_running)
            return;

        gameManager = manager != null ? manager : gameManager;
        _completionCallback = completionCallback;
        ResolveReferences();
        HookButtons();

        if (!CanRunTutorial())
        {
            Debug.LogWarning("Odd Claw tutorial is missing a required reference. Real gameplay will start without the tutorial.");
            Action callback = _completionCallback;
            _completionCallback = null;
            callback?.Invoke();
            return;
        }

        gameManager.EnterTutorialHold();
        CacheOriginalState();
        ApplyTutorialHeadMode();
        PrepareTutorialVisuals();

        if (!BuildPracticeItems())
        {
            Debug.LogWarning("Odd Claw tutorial could not build its editable practice answers. Real gameplay will start.");
            CleanupTutorialObjects();
            RestoreOriginalState();
            Action callback = _completionCallback;
            _completionCallback = null;
            callback?.Invoke();
            return;
        }

        _running = true;
        StartFlow(RunOpeningFlow());
    }

    [ContextMenu("Reset Tutorial Save For This Scene")]
    public void ResetTutorialSaveForThisScene()
    {
        PlayerPrefs.DeleteKey(GetSceneSaveKey());
        PlayerPrefs.Save();
        Debug.Log("Reset Odd Claw interactive tutorial save for scene: " + SceneManager.GetActiveScene().name);
    }

    public string GetSceneSaveKey()
    {
        return tutorialSaveKeyPrefix + "_" + SceneManager.GetActiveScene().name;
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<OddClawCatchManager>();

        if (rootCanvas == null && gameManager != null)
            rootCanvas = gameManager.rootCanvas;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas != null)
            _canvasRect = rootCanvas.transform as RectTransform;

        if (tutorialRoot == null)
            tutorialRoot = transform as RectTransform;

        if (tutorialCanvasGroup == null)
            tutorialCanvasGroup = GetComponent<CanvasGroup>();

        if (handPointer == null && handPointerImage != null)
            handPointer = handPointerImage.rectTransform;
    }

    private bool CanRunTutorial()
    {
        return gameManager != null
            && rootCanvas != null
            && _canvasRect != null
            && tutorialRoot != null
            && tutorialCanvasGroup != null
            && instructionRoot != null
            && instructionCanvasGroup != null
            && instructionText != null
            && gameManager.clawController != null
            && gameManager.itemContainer != null
            && practiceAnswers != null
            && practiceAnswers.Count >= 2;
    }

    private void HookButtons()
    {
        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(RequestSkip);
            skipButton.onClick.AddListener(RequestSkip);
        }

        if (skipConfirmButton != null)
        {
            skipConfirmButton.onClick.RemoveListener(ConfirmSkip);
            skipConfirmButton.onClick.AddListener(ConfirmSkip);
        }

        if (skipCancelButton != null)
        {
            skipCancelButton.onClick.RemoveListener(CancelSkip);
            skipCancelButton.onClick.AddListener(CancelSkip);
        }
    }

    private void UnhookButtons()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(RequestSkip);
        if (skipConfirmButton != null)
            skipConfirmButton.onClick.RemoveListener(ConfirmSkip);
        if (skipCancelButton != null)
            skipCancelButton.onClick.RemoveListener(CancelSkip);
    }

    private void CacheOriginalState()
    {
        _originalQuestionText = gameManager.questionText != null ? gameManager.questionText.text : string.Empty;
        _originalHeaderText = gameManager.questionHeaderText != null ? gameManager.questionHeaderText.text : string.Empty;

        OddClawController claw = gameManager.clawController;
        _originalRotationSpeed = claw.rotationSpeed;
        _originalExtensionLength = claw.extensionLength;
        _originalAimGuide = claw.easyModeAimGuide;
        _originalImageMagnetMode = claw.IsImageMagnetMode;
    }

    private void ApplyTutorialHeadMode()
    {
        if (gameManager == null || gameManager.clawController == null)
            return;

        bool shouldUseMagnet = useMagnetForImagePractice
            && IsImageDisplayMode(practiceDisplayMode);
        gameManager.clawController.SetImageMagnetMode(shouldUseMagnet);
    }

    private void PrepareTutorialVisuals()
    {
        tutorialRoot.gameObject.SetActive(true);
        tutorialRoot.SetAsLastSibling();
        tutorialCanvasGroup.DOKill();
        tutorialCanvasGroup.alpha = 0f;
        tutorialCanvasGroup.blocksRaycasts = true;
        tutorialCanvasGroup.interactable = true;

        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);

        if (skipConfirmationText != null)
            skipConfirmationText.text = skipConfirmationMessage;

        if (handPointerImage != null)
        {
            handPointerImage.raycastTarget = false;
            handPointerImage.enabled = handPointerImage.sprite != null;
        }

        if (handPointer != null)
        {
            handPointer.gameObject.SetActive(false);
            handPointer.localScale = Vector3.one;
        }

        if (focusCanvasGroup != null)
        {
            focusCanvasGroup.alpha = 0f;
            focusCanvasGroup.blocksRaycasts = false;
            focusCanvasGroup.interactable = false;
        }

        instructionCanvasGroup.alpha = 0f;
        instructionCanvasGroup.blocksRaycasts = false;
        instructionCanvasGroup.interactable = false;
        instructionRoot.localScale = Vector3.one;

        if (gameManager.questionText != null)
            gameManager.questionText.text = practiceQuestion;
        if (gameManager.questionHeaderText != null)
            gameManager.questionHeaderText.text = practiceQuestion;
    }

    private bool BuildPracticeItems()
    {
        ClearPracticeItems();

        int correctIndex = GetCorrectAnswerIndex();
        float maxReach = 0f;

        for (int i = 0; i < practiceAnswers.Count; i++)
        {
            OddClawTutorialPracticeAnswer data = practiceAnswers[i];
            if (data == null)
                continue;

            OddClawItemView template = GetPracticeTemplate(data);
            if (template == null)
                return false;

            OddClawItemView item = Instantiate(template, gameManager.itemContainer);
            item.gameObject.name = "TutorialPracticeAnswer_" + (i + 1);
            item.gameObject.SetActive(true);

            OddClawAnswerOption option = new OddClawAnswerOption(data.text, data.sprite, string.Empty);
            item.Setup(option, practiceDisplayMode, i, correctIndex, gameManager.primaryFont, gameManager.secondaryFont);

            RectTransform rect = item.RectTransform;
            if (rect != null)
            {
                LayoutElement layout = rect.GetComponent<LayoutElement>();
                if (layout != null)
                    layout.ignoreLayout = true;

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = data.anchoredPosition;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one * Mathf.Max(0.1f, practiceItemScale);
            }

            if (item.canvasGroup != null)
                item.canvasGroup.alpha = 0f;

            _practiceItems.Add(item);

            if (rect != null && gameManager.clawController.clawPivot != null)
            {
                Vector3 local = gameManager.clawController.clawPivot.InverseTransformPoint(rect.position);
                maxReach = Mathf.Max(maxReach, new Vector2(local.x, local.y).magnitude);
            }
        }

        if (_practiceItems.Count < 2)
            return false;

        gameManager.clawController.EnsureExtensionLength(maxReach + Mathf.Max(0f, practiceReachPadding));
        return true;
    }

    private OddClawItemView GetPracticeTemplate(OddClawTutorialPracticeAnswer data)
    {
        if (IsImageDisplayMode(practiceDisplayMode) && data != null && data.sprite != null)
        {
            if (practiceDisplayMode == OddClawAnswerDisplayMode.SpriteWithOptionalText
                && !string.IsNullOrWhiteSpace(data.text))
            {
                if (practiceImageTextTemplateOverride != null)
                    return practiceImageTextTemplateOverride;
                if (gameManager.imageTextItemTemplate != null)
                    return gameManager.imageTextItemTemplate;
            }

            if (practiceImageTemplateOverride != null)
                return practiceImageTemplateOverride;
            if (gameManager.imageItemTemplate != null)
                return gameManager.imageItemTemplate;
        }

        if (practiceTextTemplateOverride != null)
            return practiceTextTemplateOverride;

        return gameManager.textItemTemplate;
    }

    private static bool IsImageDisplayMode(OddClawAnswerDisplayMode displayMode)
    {
        return displayMode == OddClawAnswerDisplayMode.Sprite
            || displayMode == OddClawAnswerDisplayMode.SpriteWithOptionalText;
    }

    private int GetCorrectAnswerIndex()
    {
        for (int i = 0; i < practiceAnswers.Count; i++)
        {
            if (practiceAnswers[i] != null && practiceAnswers[i].isCorrect)
                return i;
        }

        return 0;
    }

    private IEnumerator RunOpeningFlow()
    {
        _acceptAdvanceTap = false;
        tutorialCanvasGroup.DOFade(1f, Mathf.Max(0.01f, rootFadeDuration));
        yield return new WaitForSeconds(Mathf.Max(0f, stageTransitionDelay));
        yield return ShowReadQuestionStage();
    }

    private IEnumerator ShowReadQuestionStage()
    {
        _stage = TutorialStage.ReadQuestion;
        HideAllPracticeItemsImmediate();
        gameManager.clawController.SetInputEnabled(false);

        RectTransform target = gameManager.questionText != null
            ? gameManager.questionText.rectTransform
            : gameManager.questionHeaderText != null ? gameManager.questionHeaderText.rectTransform : null;

        yield return TransitionInstruction(
            readQuestionInstruction,
            target,
            questionInstructionOffset,
            target,
            questionPointerOffset,
            true);

        _acceptAdvanceTap = true;
    }

    private IEnumerator ShowAnswersStage()
    {
        _acceptAdvanceTap = false;
        _stage = TutorialStage.InspectAnswers;

        yield return TransitionInstruction(
            inspectAnswersInstruction,
            gameManager.itemContainer,
            answersInstructionOffset,
            null,
            Vector2.zero,
            false);

        for (int i = 0; i < _practiceItems.Count; i++)
        {
            OddClawItemView item = _practiceItems[i];
            if (item == null)
                continue;

            item.gameObject.SetActive(true);
            if (item.canvasGroup != null)
            {
                item.canvasGroup.DOKill();
                yield return item.canvasGroup.DOFade(1f, Mathf.Max(0.01f, instructionFadeDuration)).WaitForCompletion();
            }

            if (handPointer != null && handPointerImage != null && handPointerImage.sprite != null)
            {
                ShowPointer(true);
                yield return MovePointerTo(item.RectTransform, answersPointerOffset);
            }

            if (answerRevealDelay > 0f)
                yield return new WaitForSeconds(answerRevealDelay);
        }

        _acceptAdvanceTap = true;
    }

    private IEnumerator ShowWatchClawStage()
    {
        _acceptAdvanceTap = false;
        _stage = TutorialStage.WatchClaw;

        ConfigurePracticeClaw(true);
        RectTransform clawTarget = gameManager.clawController.clawHead != null
            ? gameManager.clawController.clawHead
            : gameManager.clawController.clawPivot;

        yield return TransitionInstruction(
            watchClawInstruction,
            clawTarget,
            clawInstructionOffset,
            clawTarget,
            clawPointerOffset,
            false);

        _acceptAdvanceTap = true;
    }

    private IEnumerator PlayDemonstrationThenPractice()
    {
        _acceptAdvanceTap = false;
        _stage = TutorialStage.Demonstration;
        gameManager.clawController.SetInputEnabled(false);

        yield return TransitionInstructionAtCanvasPosition(
            demonstrationInstruction,
            GetDemonstrationInstructionPosition());
        yield return PlayGhostDemonstration();
        yield return EnterPracticeStage();
    }

    private IEnumerator ReplayDemonstrationThenPractice()
    {
        if (_practiceAwaitingResult || _stage != TutorialStage.Practice)
            yield break;

        _stage = TutorialStage.Demonstration;
        gameManager.clawController.SetInputEnabled(false);
        HidePointerImmediate();
        yield return TransitionInstructionAtCanvasPosition(
            demonstrationInstruction,
            GetDemonstrationInstructionPosition());
        yield return PlayGhostDemonstration();
        yield return EnterPracticeStage();
    }

    private IEnumerator PlayGhostDemonstration()
    {
        OddClawItemView correctItem = GetCorrectPracticeItem();
        if (correctItem == null)
            yield break;

        CreateGhostClaw();
        CreateGhostCaughtItem(correctItem);

        if (_ghostRoot == null || _ghostCanvasGroup == null)
            yield break;

        float targetAngle = CalculateTargetClawAngle(correctItem.RectTransform);
        float targetReach = CalculateTargetReach(correctItem.RectTransform);

        _ghostCanvasGroup.alpha = 0f;
        _ghostRoot.gameObject.SetActive(true);
        _ghostCanvasGroup.DOFade(ghostTransparency, Mathf.Max(0.01f, ghostFadeDuration));

        _ghostRoot.DOKill();
        yield return _ghostRoot
            .DOLocalRotate(new Vector3(0f, 0f, targetAngle), Mathf.Max(0.01f, ghostAimDuration))
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        if (handPointer != null && handPointerImage != null && handPointerImage.sprite != null)
        {
            ShowPointer(true);
            if (demonstrationTapTarget != null)
                yield return MovePointerTo(demonstrationTapTarget, demonstrationTapPointerOffset);
            else
                yield return MovePointerToCanvasPosition(demonstrationTapPosition + demonstrationTapPointerOffset);

            yield return PlayPointerTap();
        }

        float currentReach = _ghostBaseReach;
        Tween extendTween = DOTween.To(
                () => currentReach,
                value =>
                {
                    currentReach = value;
                    ApplyGhostReach(value);
                },
                targetReach,
                Mathf.Max(0.01f, ghostExtendDuration))
            .SetEase(Ease.OutQuad);
        yield return extendTween.WaitForCompletion();

        AttachGhostItemToClaw();
        if (ghostHoldDuration > 0f)
            yield return new WaitForSeconds(ghostHoldDuration);

        Tween retractTween = DOTween.To(
                () => currentReach,
                value =>
                {
                    currentReach = value;
                    ApplyGhostReach(value);
                },
                _ghostBaseReach,
                Mathf.Max(0.01f, ghostExtendDuration * 0.82f))
            .SetEase(Ease.InOutQuad);
        yield return retractTween.WaitForCompletion();

        if (_ghostCanvasGroup != null)
            yield return _ghostCanvasGroup.DOFade(0f, Mathf.Max(0.01f, ghostFadeDuration)).WaitForCompletion();

        DestroyGhostVisuals();
        HidePointerImmediate();
    }

    private IEnumerator EnterPracticeStage()
    {
        _stage = TutorialStage.Practice;
        _acceptAdvanceTap = false;
        _practiceAwaitingResult = false;
        _inactiveTime = 0f;

        gameManager.clawController.ResetClawImmediate();
        ConfigurePracticeClaw(true);
        ShowAllPracticeItems();
        HidePointerImmediate();
        HideFocus();

        yield return TransitionInstructionAtCanvasPosition(practiceInstruction, practiceInstructionPosition);
    }

    private void TryPracticeCatch()
    {
        OddClawController claw = gameManager.clawController;
        if (claw == null || claw.IsBusy)
            return;

        _practiceAwaitingResult = true;
        _acceptAdvanceTap = false;
        HidePointerImmediate();
        HideFocus();

        if (skipButton != null)
            skipButton.interactable = false;

        claw.TryCatch(_practiceItems, rootCanvas, OnPracticeCatchComplete);
    }

    private void OnPracticeCatchComplete(OddClawCatchResult result)
    {
        if (!_running)
            return;

        StartFlow(ResolvePracticeResult(result));
    }

    private IEnumerator ResolvePracticeResult(OddClawCatchResult result)
    {
        if (skipButton != null)
            skipButton.interactable = true;

        if (result == null || !result.caughtSomething)
        {
            _practiceAwaitingResult = false;
            gameManager.clawController.SetInputEnabled(true);
            yield return TransitionInstructionAtCanvasPosition(missInstruction, practiceInstructionPosition);
            _stage = TutorialStage.Practice;
            _inactiveTime = 0f;
            yield break;
        }

        if (!result.caughtCorrect)
        {
            int caughtIndex = result.caughtIndex;
            if (result.caughtItem != null)
            {
                _practiceItems.Remove(result.caughtItem);
                Destroy(result.caughtItem.gameObject);
            }

            yield return new WaitForSeconds(Mathf.Max(0f, stageTransitionDelay));
            RecreatePracticeItem(caughtIndex);
            gameManager.clawController.ResetClawImmediate();
            ConfigurePracticeClaw(true);
            _practiceAwaitingResult = false;
            yield return TransitionInstructionAtCanvasPosition(wrongInstruction, practiceInstructionPosition);
            _stage = TutorialStage.Practice;
            _inactiveTime = 0f;
            yield break;
        }

        if (gameManager.audioManager != null)
            gameManager.audioManager.PlayCorrect();

        if (result.caughtItem != null)
            result.caughtItem.SetFeedback(true);

        yield return new WaitForSeconds(0.45f);

        _practiceAwaitingResult = false;
        gameManager.clawController.SetInputEnabled(false);
        if (result.caughtItem != null)
            Destroy(result.caughtItem.gameObject);
        gameManager.clawController.ResetClawImmediate();
        gameManager.clawController.SetInputEnabled(false);

        yield return ShowSuccessStage();
    }

    private IEnumerator ShowSuccessStage()
    {
        _stage = TutorialStage.Success;
        _acceptAdvanceTap = false;
        ClearPracticeItems();
        HidePointerImmediate();
        HideFocus();

        yield return TransitionInstructionAtCanvasPosition(successInstruction, successInstructionPosition);

        if (handPointer != null && handPointerImage != null && handPointerImage.sprite != null)
        {
            ShowPointer(true);
            yield return MovePointerToCanvasPosition(successPointerPosition);
        }

        _acceptAdvanceTap = true;
    }

    private IEnumerator TransitionInstruction(
        string message,
        RectTransform instructionTarget,
        Vector2 instructionOffset,
        RectTransform pointerTarget,
        Vector2 pointerOffset,
        bool showFocus)
    {
        _acceptAdvanceTap = false;
        yield return FadeInstructionOut();

        instructionText.text = message;
        PositionInstructionNear(instructionTarget, instructionOffset);
        instructionRoot.localScale = Vector3.one;

        if (showFocus && instructionTarget != null)
            ShowFocus(instructionTarget);
        else
            HideFocus();

        yield return FadeInstructionIn();

        if (pointerTarget != null && handPointer != null && handPointerImage != null && handPointerImage.sprite != null)
        {
            ShowPointer(true);
            yield return MovePointerTo(pointerTarget, pointerOffset);
        }
        else
        {
            HidePointerImmediate();
        }

        if (stageTransitionDelay > 0f)
            yield return new WaitForSeconds(stageTransitionDelay);
    }

    private IEnumerator TransitionInstructionAtCanvasPosition(string message, Vector2 canvasPosition)
    {
        _acceptAdvanceTap = false;
        yield return FadeInstructionOut();

        instructionText.text = message;
        instructionRoot.anchoredPosition = ClampRectPosition(
            instructionRoot,
            canvasPosition,
            instructionScreenPadding);
        instructionRoot.localScale = Vector3.one;
        HideFocus();

        yield return FadeInstructionIn();

        if (stageTransitionDelay > 0f)
            yield return new WaitForSeconds(stageTransitionDelay);
    }

    private IEnumerator FadeInstructionOut()
    {
        instructionRoot.DOKill();
        instructionCanvasGroup.DOKill();
        instructionRoot.localScale = Vector3.one;

        if (instructionCanvasGroup.alpha > 0.01f)
            yield return instructionCanvasGroup
                .DOFade(0f, Mathf.Max(0.01f, instructionFadeDuration))
                .WaitForCompletion();
    }

    private IEnumerator FadeInstructionIn()
    {
        instructionCanvasGroup.DOKill();
        instructionCanvasGroup.alpha = 0f;
        yield return instructionCanvasGroup
            .DOFade(1f, Mathf.Max(0.01f, instructionFadeDuration))
            .WaitForCompletion();

        instructionRoot.DOKill();
        instructionRoot.localScale = Vector3.one;
        instructionRoot
            .DOScale(Vector3.one * Mathf.Max(1f, instructionBreathScale), Mathf.Max(0.1f, instructionBreathDuration))
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    private IEnumerator MovePointerTo(RectTransform target, Vector2 offset)
    {
        if (target == null)
            yield break;

        Vector2 localPoint = WorldToCanvasPoint(target.TransformPoint(target.rect.center));
        yield return MovePointerToCanvasPosition(localPoint + offset);
    }

    private IEnumerator MovePointerToCanvasPosition(Vector2 position)
    {
        if (handPointer == null)
            yield break;

        handPointer.DOKill();
        Vector2 clamped = ClampRectPosition(handPointer, position, pointerScreenPadding);
        clamped = KeepPointerAwayFromInstruction(clamped);
        yield return handPointer
            .DOAnchorPos(clamped, Mathf.Max(0.01f, pointerMoveDuration))
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();
    }

    private Vector2 KeepPointerAwayFromInstruction(Vector2 desiredPosition)
    {
        if (!keepPointerClearOfInstruction
            || handPointer == null
            || instructionRoot == null
            || instructionCanvasGroup == null
            || instructionCanvasGroup.alpha <= 0.01f)
        {
            return desiredPosition;
        }

        Rect instructionRect = BuildLocalRect(
            instructionRoot,
            instructionRoot.anchoredPosition,
            Mathf.Max(0f, pointerInstructionClearance));
        Rect pointerRect = BuildLocalRect(handPointer, desiredPosition, 0f);

        if (!instructionRect.Overlaps(pointerRect))
            return desiredPosition;

        float halfPointerWidth = pointerRect.width * 0.5f;
        float halfPointerHeight = pointerRect.height * 0.5f;

        Vector2[] candidates =
        {
            new Vector2(instructionRect.xMin - halfPointerWidth, desiredPosition.y),
            new Vector2(instructionRect.xMax + halfPointerWidth, desiredPosition.y),
            new Vector2(desiredPosition.x, instructionRect.yMin - halfPointerHeight),
            new Vector2(desiredPosition.x, instructionRect.yMax + halfPointerHeight)
        };

        Vector2 best = desiredPosition;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector2 candidate = ClampRectPosition(
                handPointer,
                candidates[i],
                pointerScreenPadding);
            Rect candidateRect = BuildLocalRect(handPointer, candidate, 0f);
            if (instructionRect.Overlaps(candidateRect))
                continue;

            float distance = (candidate - desiredPosition).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private Rect BuildLocalRect(RectTransform rect, Vector2 centre, float extraPadding)
    {
        float width = Mathf.Max(1f, rect.rect.width * Mathf.Abs(rect.localScale.x));
        float height = Mathf.Max(1f, rect.rect.height * Mathf.Abs(rect.localScale.y));
        return new Rect(
            centre.x - width * 0.5f - extraPadding,
            centre.y - height * 0.5f - extraPadding,
            width + extraPadding * 2f,
            height + extraPadding * 2f);
    }

    private IEnumerator PlayPointerTap()
    {
        if (handPointer == null)
            yield break;

        handPointer.DOKill();
        handPointer.localScale = Vector3.one;
        Sequence sequence = DOTween.Sequence();
        sequence.Append(handPointer.DOScale(0.84f, Mathf.Max(0.05f, pointerTapDuration * 0.45f)).SetEase(Ease.InQuad));
        sequence.Append(handPointer.DOScale(1f, Mathf.Max(0.05f, pointerTapDuration * 0.55f)).SetEase(Ease.OutBack));
        yield return sequence.WaitForCompletion();
    }

    private void PositionInstructionNear(RectTransform target, Vector2 offset)
    {
        Vector2 basePoint = target != null
            ? WorldToCanvasPoint(target.TransformPoint(target.rect.center))
            : Vector2.zero;

        instructionRoot.anchoredPosition = ClampRectPosition(
            instructionRoot,
            basePoint + offset,
            instructionScreenPadding);
    }

    private Vector2 GetDemonstrationInstructionPosition()
    {
        if (!autoPlaceDemonstrationInstructionAwayFromClaw
            || _canvasRect == null
            || instructionRoot == null)
        {
            return demonstrationInstructionPosition;
        }

        OddClawItemView correctItem = GetCorrectPracticeItem();
        RectTransform clawPivot = gameManager != null && gameManager.clawController != null
            ? gameManager.clawController.clawPivot
            : null;

        if (correctItem == null)
            return demonstrationInstructionPosition;

        Vector2 targetPosition = WorldToCanvasPoint(
            correctItem.RectTransform.TransformPoint(correctItem.RectTransform.rect.center));
        Vector2 pivotPosition = clawPivot != null
            ? WorldToCanvasPoint(clawPivot.TransformPoint(clawPivot.rect.center))
            : Vector2.zero;

        bool clawMovesRight = targetPosition.x >= pivotPosition.x;
        float halfCanvasWidth = _canvasRect.rect.width * 0.5f;
        float halfInstructionWidth = Mathf.Max(
            1f,
            instructionRoot.rect.width * Mathf.Abs(instructionRoot.localScale.x) * 0.5f);
        float sideX = halfCanvasWidth - halfInstructionWidth - instructionScreenPadding;

        Vector2 desired = new Vector2(
            clawMovesRight ? -sideX : sideX,
            demonstrationInstructionPosition.y);
        return ClampRectPosition(instructionRoot, desired, instructionScreenPadding);
    }

    private Vector2 WorldToCanvasPoint(Vector3 worldPoint)
    {
        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPoint, camera, out Vector2 localPoint);
        return localPoint;
    }

    private Vector2 ClampRectPosition(RectTransform rect, Vector2 desired, float padding)
    {
        if (_canvasRect == null || rect == null)
            return desired;

        float halfCanvasWidth = _canvasRect.rect.width * 0.5f;
        float halfCanvasHeight = _canvasRect.rect.height * 0.5f;
        float halfWidth = Mathf.Max(1f, rect.rect.width * Mathf.Abs(rect.localScale.x) * 0.5f);
        float halfHeight = Mathf.Max(1f, rect.rect.height * Mathf.Abs(rect.localScale.y) * 0.5f);

        float minX = -halfCanvasWidth + halfWidth + padding;
        float maxX = halfCanvasWidth - halfWidth - padding;
        float minY = -halfCanvasHeight + halfHeight + padding;
        float maxY = halfCanvasHeight - halfHeight - padding;

        return new Vector2(
            minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : 0f,
            minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : 0f);
    }

    private void ShowFocus(RectTransform target)
    {
        if (focusHighlight == null || focusCanvasGroup == null || target == null)
            return;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 point = WorldToCanvasPoint(corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        focusHighlight.anchorMin = new Vector2(0.5f, 0.5f);
        focusHighlight.anchorMax = new Vector2(0.5f, 0.5f);
        focusHighlight.pivot = new Vector2(0.5f, 0.5f);
        focusHighlight.anchoredPosition = (min + max) * 0.5f;
        focusHighlight.sizeDelta = (max - min) + highlightPadding * 2f;
        focusHighlight.gameObject.SetActive(true);
        focusCanvasGroup.DOKill();
        focusCanvasGroup.DOFade(1f, Mathf.Max(0.01f, instructionFadeDuration));
    }

    private void HideFocus()
    {
        if (focusCanvasGroup == null)
            return;

        focusCanvasGroup.DOKill();
        focusCanvasGroup.alpha = 0f;
        if (focusHighlight != null)
            focusHighlight.gameObject.SetActive(false);
    }

    private void ShowPointer(bool show)
    {
        if (handPointer == null || handPointerImage == null)
            return;

        bool canShow = show && handPointerImage.sprite != null;
        handPointerImage.enabled = canShow;
        handPointer.gameObject.SetActive(canShow);
        if (canShow)
            handPointer.SetAsLastSibling();
    }

    private void HidePointerImmediate()
    {
        if (handPointer == null)
            return;

        handPointer.DOKill();
        handPointer.localScale = Vector3.one;
        handPointer.gameObject.SetActive(false);
    }

    private void ConfigurePracticeClaw(bool enableInput)
    {
        OddClawController claw = gameManager.clawController;
        claw.rotationSpeed = Mathf.Max(5f, practiceRotationSpeed);
        claw.SetWaveDifficulty(1);
        claw.SetEasyGuideEnabled(showAimGuideDuringPractice);
        claw.SetInputEnabled(enableInput);
    }

    private OddClawItemView GetCorrectPracticeItem()
    {
        for (int i = 0; i < _practiceItems.Count; i++)
        {
            if (_practiceItems[i] != null && _practiceItems[i].IsCorrect)
                return _practiceItems[i];
        }

        return null;
    }

    private void CreateGhostClaw()
    {
        DestroyGhostVisuals();

        OddClawController claw = gameManager.clawController;
        RectTransform sourceRoot = claw.clawPivot;
        if (sourceRoot == null || sourceRoot.parent == null)
            return;

        _ghostRoot = CreateRect("OddClawTutorial_GhostClaw", sourceRoot.parent);
        CopyRectTransform(sourceRoot, _ghostRoot);
        _ghostRoot.localRotation = sourceRoot.localRotation;
        _ghostRoot.SetSiblingIndex(sourceRoot.GetSiblingIndex() + 1);
        _ghostCanvasGroup = _ghostRoot.gameObject.AddComponent<CanvasGroup>();
        _ghostCanvasGroup.blocksRaycasts = false;
        _ghostCanvasGroup.interactable = false;

        if (claw.clawArm != null)
        {
            _ghostArm = CreateImageClone("GhostArm", claw.clawArm, _ghostRoot);
            _ghostBaseArmSize = claw.clawArm.sizeDelta;
        }

        if (claw.clawHead != null)
        {
            _ghostHead = CreateImageClone("GhostHead", claw.clawHead, _ghostRoot);
            _ghostBaseHeadPosition = claw.clawHead.anchoredPosition;
        }

        _ghostBaseReach = Mathf.Max(
            Mathf.Abs(_ghostBaseHeadPosition.y),
            Mathf.Abs(_ghostBaseArmSize.y));
        _ghostHeadDirection = _ghostBaseHeadPosition.y < 0f ? -1f : 1f;
    }

    private RectTransform CreateImageClone(string name, RectTransform source, Transform parent)
    {
        GameObject clone = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = clone.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        CopyRectTransform(source, rect);

        Image sourceImage = source.GetComponent<Image>();
        Image image = clone.GetComponent<Image>();
        image.raycastTarget = false;
        if (sourceImage != null)
        {
            image.sprite = sourceImage.sprite;
            image.type = sourceImage.type;
            image.preserveAspect = sourceImage.preserveAspect;
            image.color = sourceImage.color;
        }

        return rect;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private void CopyRectTransform(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.localScale = source.localScale;
        destination.localRotation = source.localRotation;
    }

    private void CreateGhostCaughtItem(OddClawItemView correctItem)
    {
        if (correctItem == null || correctItem.RectTransform == null)
            return;

        GameObject clone = Instantiate(correctItem.gameObject, correctItem.transform.parent);
        clone.name = "OddClawTutorial_GhostCaughtItem";
        OddClawItemView clonedView = clone.GetComponent<OddClawItemView>();
        if (clonedView != null)
            clonedView.enabled = false;

        _ghostItem = clone.transform as RectTransform;
        _ghostItemCanvasGroup = clone.GetComponent<CanvasGroup>();
        if (_ghostItemCanvasGroup == null)
            _ghostItemCanvasGroup = clone.AddComponent<CanvasGroup>();

        _ghostItemCanvasGroup.alpha = 0f;
        _ghostItemCanvasGroup.blocksRaycasts = false;
        _ghostItemCanvasGroup.interactable = false;
    }

    private void AttachGhostItemToClaw()
    {
        if (_ghostItem == null || _ghostHead == null)
            return;

        _ghostItem.SetParent(_ghostHead, false);
        _ghostItem.anchoredPosition = new Vector2(0f, -42f);
        _ghostItem.localRotation = Quaternion.identity;
        _ghostItem.localScale = Vector3.one * 0.72f;

        if (_ghostItemCanvasGroup != null)
            _ghostItemCanvasGroup.DOFade(ghostTransparency, Mathf.Max(0.01f, ghostFadeDuration));
    }

    private float CalculateTargetClawAngle(RectTransform target)
    {
        OddClawController claw = gameManager.clawController;
        if (claw.clawPivot == null || claw.clawPivot.parent == null || target == null)
            return 0f;

        Vector2 pivotPoint = claw.clawPivot.parent.InverseTransformPoint(claw.clawPivot.position);
        Vector2 targetPoint = claw.clawPivot.parent.InverseTransformPoint(target.position);
        Vector2 direction = (targetPoint - pivotPoint).normalized;
        return Vector2.SignedAngle(Vector2.down, direction);
    }

    private float CalculateTargetReach(RectTransform target)
    {
        if (_ghostRoot == null || target == null)
            return _ghostBaseReach + 300f;

        Vector3 local = _ghostRoot.InverseTransformPoint(target.position);
        return Mathf.Max(_ghostBaseReach, new Vector2(local.x, local.y).magnitude);
    }

    private void ApplyGhostReach(float totalReach)
    {
        float extra = Mathf.Max(0f, totalReach - _ghostBaseReach);

        if (_ghostArm != null)
        {
            Vector2 size = _ghostBaseArmSize;
            size.y = Mathf.Sign(size.y == 0f ? 1f : size.y) * (Mathf.Abs(_ghostBaseArmSize.y) + extra);
            _ghostArm.sizeDelta = size;
        }

        if (_ghostHead != null)
            _ghostHead.anchoredPosition = _ghostBaseHeadPosition + new Vector2(0f, _ghostHeadDirection * extra);
    }

    private void RecreatePracticeItem(int answerIndex)
    {
        if (answerIndex < 0 || answerIndex >= practiceAnswers.Count)
            return;

        OddClawTutorialPracticeAnswer data = practiceAnswers[answerIndex];
        OddClawItemView template = GetPracticeTemplate(data);
        if (template == null)
            return;

        int correctIndex = GetCorrectAnswerIndex();
        OddClawItemView item = Instantiate(template, gameManager.itemContainer);
        item.gameObject.name = "TutorialPracticeAnswer_" + (answerIndex + 1);
        item.gameObject.SetActive(true);
        item.Setup(
            new OddClawAnswerOption(data.text, data.sprite, string.Empty),
            practiceDisplayMode,
            answerIndex,
            correctIndex,
            gameManager.primaryFont,
            gameManager.secondaryFont);

        RectTransform rect = item.RectTransform;
        if (rect != null)
        {
            LayoutElement layout = rect.GetComponent<LayoutElement>();
            if (layout != null)
                layout.ignoreLayout = true;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = data.anchoredPosition;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * Mathf.Max(0.1f, practiceItemScale);
        }

        _practiceItems.Add(item);
    }

    private void ShowAllPracticeItems()
    {
        for (int i = 0; i < _practiceItems.Count; i++)
        {
            OddClawItemView item = _practiceItems[i];
            if (item == null)
                continue;
            item.gameObject.SetActive(true);
            if (item.canvasGroup != null)
                item.canvasGroup.alpha = 1f;
        }
    }

    private void HideAllPracticeItemsImmediate()
    {
        for (int i = 0; i < _practiceItems.Count; i++)
        {
            OddClawItemView item = _practiceItems[i];
            if (item == null)
                continue;
            if (item.canvasGroup != null)
                item.canvasGroup.alpha = 0f;
        }
    }

    private void RequestSkip()
    {
        if (!_running || _confirmationOpen)
            return;

        _confirmationOpen = true;
        _acceptAdvanceTap = false;
        gameManager.clawController.SetInputEnabled(false);
        HidePointerImmediate();

        if (skipConfirmationText != null)
            skipConfirmationText.text = skipConfirmationMessage;

        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(true);
    }

    private void ConfirmSkip()
    {
        if (!_running || !_confirmationOpen)
            return;

        _confirmationOpen = false;
        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);
        StartFlow(FinishTutorial(saveCompletionWhenSkipped));
    }

    private void CancelSkip()
    {
        if (!_running || !_confirmationOpen)
            return;

        _confirmationOpen = false;
        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);

        if (_stage == TutorialStage.Practice)
            gameManager.clawController.SetInputEnabled(true);
        else if (_stage == TutorialStage.WatchClaw)
            ConfigurePracticeClaw(true);

        _acceptAdvanceTap = _stage == TutorialStage.ReadQuestion
            || _stage == TutorialStage.InspectAnswers
            || _stage == TutorialStage.WatchClaw
            || _stage == TutorialStage.Success;
    }

    private IEnumerator FinishTutorial(bool saveCompletion)
    {
        if (!_running)
            yield break;

        _stage = TutorialStage.Finishing;
        _acceptAdvanceTap = false;
        _practiceAwaitingResult = false;

        if (saveCompletion)
        {
            PlayerPrefs.SetInt(GetSceneSaveKey(), 1);
            PlayerPrefs.Save();
        }

        if (tutorialCanvasGroup != null)
            yield return tutorialCanvasGroup
                .DOFade(0f, Mathf.Max(0.01f, rootFadeDuration))
                .WaitForCompletion();

        CleanupTutorialObjects();
        RestoreOriginalState();
        HideImmediate();

        _running = false;
        _stage = TutorialStage.None;
        Action callback = _completionCallback;
        _completionCallback = null;
        callback?.Invoke();
    }

    private void RestoreOriginalState()
    {
        if (gameManager == null)
            return;

        if (gameManager.questionText != null)
            gameManager.questionText.text = _originalQuestionText;
        if (gameManager.questionHeaderText != null)
            gameManager.questionHeaderText.text = _originalHeaderText;

        OddClawController claw = gameManager.clawController;
        if (claw != null)
        {
            claw.rotationSpeed = _originalRotationSpeed;
            claw.extensionLength = _originalExtensionLength;
            claw.SetEasyGuideEnabled(_originalAimGuide);
            claw.SetImageMagnetMode(_originalImageMagnetMode);
            claw.ResetClawImmediate();
            claw.SetInputEnabled(false);
        }
    }

    private void CleanupTutorialObjects()
    {
        KillTutorialTweens();
        DestroyGhostVisuals();
        ClearPracticeItems();
        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);
    }

    private void ClearPracticeItems()
    {
        for (int i = _practiceItems.Count - 1; i >= 0; i--)
        {
            if (_practiceItems[i] != null)
                Destroy(_practiceItems[i].gameObject);
        }
        _practiceItems.Clear();
    }

    private void DestroyGhostVisuals()
    {
        if (_ghostRoot != null)
        {
            _ghostRoot.DOKill();
            Destroy(_ghostRoot.gameObject);
        }

        if (_ghostItem != null && (_ghostRoot == null || !_ghostItem.IsChildOf(_ghostRoot)))
            Destroy(_ghostItem.gameObject);

        _ghostRoot = null;
        _ghostArm = null;
        _ghostHead = null;
        _ghostCanvasGroup = null;
        _ghostItem = null;
        _ghostItemCanvasGroup = null;
    }

    private void StartFlow(IEnumerator routine)
    {
        if (_flowRoutine != null)
            StopCoroutine(_flowRoutine);
        _flowRoutine = StartCoroutine(RunTrackedFlow(routine));
    }

    private IEnumerator RunTrackedFlow(IEnumerator routine)
    {
        yield return routine;
        _flowRoutine = null;
    }

    private bool WasTutorialTapPressed()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase != TouchPhase.Began)
                continue;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                continue;
            return true;
        }

        return false;
    }

    private void KillTutorialTweens()
    {
        if (tutorialCanvasGroup != null)
            tutorialCanvasGroup.DOKill();
        if (instructionCanvasGroup != null)
            instructionCanvasGroup.DOKill();
        if (instructionRoot != null)
            instructionRoot.DOKill();
        if (handPointer != null)
            handPointer.DOKill();
        if (focusCanvasGroup != null)
            focusCanvasGroup.DOKill();
        if (_ghostRoot != null)
            _ghostRoot.DOKill();
        if (_ghostCanvasGroup != null)
            _ghostCanvasGroup.DOKill();
        if (_ghostItemCanvasGroup != null)
            _ghostItemCanvasGroup.DOKill();
    }

    private void HideImmediate()
    {
        if (tutorialCanvasGroup != null)
        {
            tutorialCanvasGroup.DOKill();
            tutorialCanvasGroup.alpha = 0f;
            tutorialCanvasGroup.blocksRaycasts = false;
            tutorialCanvasGroup.interactable = false;
        }

        if (instructionCanvasGroup != null)
            instructionCanvasGroup.alpha = 0f;
        HidePointerImmediate();
        HideFocus();

        if (skipConfirmationPanel != null)
            skipConfirmationPanel.SetActive(false);
        if (tutorialRoot != null)
            tutorialRoot.gameObject.SetActive(false);
    }
}
