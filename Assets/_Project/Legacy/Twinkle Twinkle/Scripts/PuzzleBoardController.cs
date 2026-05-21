using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PuzzleBoardController : MonoBehaviour
{
    [Header("Board Setup")]
    public Transform slotsParent;
    public PuzzleSlot[] slots;
    public PuzzlePieceView piecePrefab;
    [Tooltip("Where dragged pieces are temporarily parented. Usually the top Canvas transform.")]
    public Transform dragRoot;
    public bool autoAssignSlotIdsByOrder = true;

    [Header("Intro / Preview")]
    public GameObject introPanel;
    public TextMeshProUGUI introMessageText;
    public Image fullPuzzlePreviewImage;
    public float fullImagePreviewSeconds = 1.2f;
    public int shuffleCountdownStart = 3;

    [Header("Post-Shuffle Start Prompt")]
    [Tooltip("When true, player must click the prompt panel after shuffle before input unlocks and timer starts.")]
    public bool useStartPromptAfterShuffle = true;
    public GameObject startPromptPanel;
    public TextMeshProUGUI startPromptText;
    [Tooltip("Optional. If empty, the script will try to use Button on startPromptPanel.")]
    public Button startPromptButton;
    public string startPromptMessage = "Click to start rearranging";
    public float startPromptBreathScale = 1.08f;
    public float startPromptBreathDuration = 0.7f;

    [Header("Shuffle Animation")]
    public float moveDuration = 0.45f;
    public float moveStagger = 0.04f;
    public Ease moveEase = Ease.OutCubic;

    [Header("Timer")]
    public TMP_Text timerText;
    public Color normalTimerColor = Color.white;
    public Color dangerTimerColor = Color.red;
    public float dangerTimeRemaining = 15f;

    [Header("Piece Feedback")]
    [Range(0f, 1f)] public float wrongOverlayAlpha = 0.12f;

    [Header("Audio")]
    public bool playAudio = true;

    [Tooltip("Played when the shuffle animation starts. Original project used SFX 5.")]
    public int shuffleSfxId = 5;

    [Tooltip("After shuffle finishes, call AudioManager StopSFX/StopSTX if available to stop the shuffle loop/sound.")]
    public bool stopShuffleSfxWhenShuffleEnds = true;

    [Tooltip("Keep this OFF if selection BGM should continue during puzzle gameplay.")]
    public bool switchBgmWhenGameplayStarts = false;

    [Tooltip("Only used when Switch Bgm When Gameplay Starts is ON. Original project used BGM 0.")]
    public int gameplayBgmId = 0;

    [Tooltip("Played while the danger timer pulse loops. Original project used SFX 6.")]
    public int dangerTimerPulseSfxId = 6;

    [Tooltip("Played when the timer reaches zero. Original project used SFX 3.")]
    public int failSfxId = 3;

    public event Action<float> PuzzleSolved;
    public event Action<float> PuzzleFailed;

    public bool IsInputLocked { get; private set; } = true;
    public Transform DragRoot { get { return dragRoot != null ? dragRoot : transform.root; } }

    private readonly List<PuzzlePieceView> pieces = new List<PuzzlePieceView>();

    private ZodiacPuzzleData currentData;
    private Coroutine gameCoroutine;

    private bool puzzleResolved;
    private bool timerRunning;
    private bool gameplayBgmStartedByThisController;

    private float playStartRealtime;
    private float timeLimit;

    private Tween timerPulseTween;
    private Tween startPromptBreathTween;

    private bool startPromptClicked;

    private void Awake()
    {
        CacheSlots();
        InitializeSlots();
        HookStartPromptButton();
    }

    private void OnDestroy()
    {
        UnhookStartPromptButton();
    }

    private void OnDisable()
    {
        StopPuzzleAndClear();
    }

    public void BeginPuzzle(ZodiacPuzzleData data)
    {
        if (data == null)
        {
            Debug.LogError("PuzzleBoardController: Cannot begin puzzle. Data is null.");
            return;
        }

        StopPuzzleAndClear();
        CacheSlots();
        InitializeSlots();

        currentData = data;
        timeLimit = Mathf.Max(1f, data.timeLimitSeconds);
        puzzleResolved = false;
        IsInputLocked = true;

        if (!ValidatePuzzleData(data))
        {
            return;
        }

        BuildSolvedPuzzle(data);
        gameCoroutine = StartCoroutine(BeginPuzzleRoutine(data));
    }

    public void StopPuzzleAndClear()
    {
        if (gameCoroutine != null)
        {
            StopCoroutine(gameCoroutine);
            gameCoroutine = null;
        }

        DOTween.Kill(this);

        timerPulseTween?.Kill();
        timerPulseTween = null;

        StopGameplayBgmIfStartedByThisController();

        timerRunning = false;
        gameplayBgmStartedByThisController = false;
        puzzleResolved = false;
        IsInputLocked = true;

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i] != null)
            {
                pieces[i].transform.DOKill();
                Destroy(pieces[i].gameObject);
            }
        }

        pieces.Clear();

        if (slots != null)
        {
            foreach (PuzzleSlot slot in slots)
            {
                if (slot != null)
                    slot.CurrentPiece = null;
            }
        }

        if (introPanel != null)
            introPanel.SetActive(false);

        if (fullPuzzlePreviewImage != null)
            fullPuzzlePreviewImage.enabled = false;

        HideStartPrompt();
        UpdateTimerDisplay(0f);
        ResetTimerVisuals();
    }

    public void TryDropPiece(PuzzlePieceView draggedPiece, PuzzleSlot targetSlot)
    {
        if (IsInputLocked || draggedPiece == null || targetSlot == null)
            return;

        PuzzleSlot sourceSlot = draggedPiece.CurrentSlot;
        PuzzlePieceView existingPiece = targetSlot.CurrentPiece;

        if (existingPiece == draggedPiece)
        {
            PlacePieceInSlot(draggedPiece, targetSlot, true);
            draggedPiece.MarkDropped();
            return;
        }

        if (existingPiece != null && sourceSlot != null)
        {
            PlacePieceInSlot(existingPiece, sourceSlot, true);
            ValidatePiece(existingPiece);
        }

        PlacePieceInSlot(draggedPiece, targetSlot, true);
        draggedPiece.MarkDropped();
        ValidatePiece(draggedPiece);
        CheckSolved();
    }

    public void PlacePieceInSlot(PuzzlePieceView piece, PuzzleSlot slot, bool instant)
    {
        if (piece == null || slot == null)
            return;

        if (piece.CurrentSlot != null && piece.CurrentSlot.CurrentPiece == piece)
        {
            piece.CurrentSlot.CurrentPiece = null;
        }

        if (slot.CurrentPiece != null && slot.CurrentPiece != piece)
        {
            slot.CurrentPiece.CurrentSlot = null;
        }

        piece.CurrentSlot = slot;
        slot.CurrentPiece = piece;

        piece.transform.SetParent(slot.PieceRootTransform, false);
        piece.StretchToParent();
    }

    public void ValidatePiece(PuzzlePieceView piece)
    {
        if (piece == null || piece.CurrentSlot == null)
            return;

        bool isCorrect = piece.id == piece.CurrentSlot.id;
        piece.SetWrongOverlayVisible(!isCorrect, wrongOverlayAlpha, false);
    }

    public void CheckSolved()
    {
        if (IsInputLocked || puzzleResolved)
            return;

        if (!IsSolved())
            return;

        puzzleResolved = true;
        IsInputLocked = true;

        float timeTaken = GetElapsedTime();

        StopTimerOnly();
        StopGameplayBgmIfStartedByThisController();

        PuzzleSolved?.Invoke(timeTaken);
    }

    private IEnumerator BeginPuzzleRoutine(ZodiacPuzzleData data)
    {
        if (introPanel != null)
            introPanel.SetActive(true);

        if (fullPuzzlePreviewImage != null)
        {
            fullPuzzlePreviewImage.sprite = data.fullPuzzleSprite != null ? data.fullPuzzleSprite : data.resultSprite;
            fullPuzzlePreviewImage.enabled = fullPuzzlePreviewImage.sprite != null;
        }

        if (introMessageText != null)
        {
            introMessageText.text = "You got " + data.DisplayName + " Puzzle";
        }

        yield return new WaitForSeconds(fullImagePreviewSeconds);

        if (introMessageText != null)
        {
            introMessageText.text = "Carefully look the image!";
        }

        yield return new WaitForSeconds(fullImagePreviewSeconds);

        if (introMessageText != null)
        {
            introMessageText.text = "You have to rearrange it.";
        }

        yield return new WaitForSeconds(fullImagePreviewSeconds);

        for (int i = shuffleCountdownStart; i > 0; i--)
        {
            if (introMessageText != null)
                introMessageText.text = "Shuffling in " + i;

            yield return new WaitForSeconds(1f);
        }

        if (introMessageText != null)
            introMessageText.text = "Go!";

        yield return new WaitForSeconds(0.25f);

        if (introPanel != null)
            introPanel.SetActive(false);

        if (fullPuzzlePreviewImage != null)
            fullPuzzlePreviewImage.enabled = false;

        yield return ShufflePiecesRoutine();

        StopShuffleSfxIfAvailable();

        RefreshAllPieceFeedback();

        yield return ShowStartPromptAndWaitForClick();

        StartTimer();
        IsInputLocked = false;
    }

    private IEnumerator ShowStartPromptAndWaitForClick()
    {
        if (!useStartPromptAfterShuffle)
            yield break;

        if (startPromptPanel == null)
            yield break;

        startPromptClicked = false;
        startPromptPanel.SetActive(true);

        if (startPromptText != null)
        {
            startPromptText.gameObject.SetActive(true);
            startPromptText.text = string.IsNullOrWhiteSpace(startPromptMessage)
                ? "Click to start rearranging"
                : startPromptMessage;

            startPromptText.rectTransform.localScale = Vector3.one;
            startPromptText.alpha = 1f;

            startPromptBreathTween?.Kill();
            startPromptBreathTween = startPromptText.rectTransform
                .DOScale(Vector3.one * Mathf.Max(1f, startPromptBreathScale), Mathf.Max(0.05f, startPromptBreathDuration))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId(this);
        }

        Button button = ResolveStartPromptButton();

        if (button != null)
        {
            button.interactable = true;
        }
        else
        {
            Debug.LogWarning("PuzzleBoardController: startPromptPanel is assigned but no Button was found/assigned. Continuing automatically to avoid a soft-lock.");
            yield return new WaitForSeconds(0.25f);
            HideStartPrompt();
            yield break;
        }

        while (!startPromptClicked && !puzzleResolved)
        {
            yield return null;
        }

        HideStartPrompt();
    }

    private void HandleStartPromptClicked()
    {
        startPromptClicked = true;
    }

    private void HideStartPrompt()
    {
        startPromptBreathTween?.Kill();
        startPromptBreathTween = null;

        if (startPromptText != null)
        {
            startPromptText.rectTransform.DOKill();
            startPromptText.rectTransform.localScale = Vector3.one;
        }

        Button button = ResolveStartPromptButton();

        if (button != null)
            button.interactable = false;

        if (startPromptPanel != null)
            startPromptPanel.SetActive(false);
    }

    private void HookStartPromptButton()
    {
        Button button = ResolveStartPromptButton();

        if (button == null)
            return;

        button.onClick.RemoveListener(HandleStartPromptClicked);
        button.onClick.AddListener(HandleStartPromptClicked);
    }

    private void UnhookStartPromptButton()
    {
        Button button = ResolveStartPromptButton();

        if (button == null)
            return;

        button.onClick.RemoveListener(HandleStartPromptClicked);
    }

    private Button ResolveStartPromptButton()
    {
        if (startPromptButton != null)
            return startPromptButton;

        if (startPromptPanel != null)
            startPromptButton = startPromptPanel.GetComponent<Button>();

        return startPromptButton;
    }

    private void CacheSlots()
    {
        if ((slots == null || slots.Length == 0) && slotsParent != null)
        {
            slots = slotsParent.GetComponentsInChildren<PuzzleSlot>(true);
        }
    }

    private void InitializeSlots()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            int id = autoAssignSlotIdsByOrder ? i : slots[i].id;
            slots[i].Initialize(this, id);
        }
    }

    private bool ValidatePuzzleData(ZodiacPuzzleData data)
    {
        if (slots == null || slots.Length == 0)
        {
            Debug.LogError("PuzzleBoardController: No slots found. Assign slotsParent or slots array.");
            return false;
        }

        if (piecePrefab == null)
        {
            Debug.LogError("PuzzleBoardController: Assign piecePrefab.");
            return false;
        }

        if (data.pieceSprites == null || data.pieceSprites.Length != slots.Length)
        {
            Debug.LogError("PuzzleBoardController: " + data.DisplayName + " has " +
                           (data.pieceSprites == null ? 0 : data.pieceSprites.Length) +
                           " piece sprites, but board has " + slots.Length + " slots.");
            return false;
        }

        return true;
    }

    private void BuildSolvedPuzzle(ZodiacPuzzleData data)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            PuzzlePieceView piece = Instantiate(piecePrefab, slots[i].PieceRootTransform);
            piece.name = "Piece_" + i + "_" + data.DisplayName;
            piece.Initialize(this, i, data.pieceSprites[i]);

            pieces.Add(piece);

            PlacePieceInSlot(piece, slots[i], true);
            piece.SetWrongOverlayVisible(false, 0f, true);
        }
    }

    private IEnumerator ShufflePiecesRoutine()
    {
        if (pieces.Count <= 1)
            yield break;

        PlaySfx(shuffleSfxId);

        List<int> targetIndices = new List<int>();

        for (int i = 0; i < slots.Length; i++)
            targetIndices.Add(i);

        Shuffle(targetIndices);

        if (IsSameOrder(targetIndices) && targetIndices.Count > 1)
        {
            int temp = targetIndices[0];
            targetIndices[0] = targetIndices[1];
            targetIndices[1] = temp;
        }

        int completed = 0;
        int total = pieces.Count;
        Transform animationRoot = DragRoot;

        for (int i = 0; i < pieces.Count; i++)
        {
            PuzzlePieceView piece = pieces[i];
            PuzzleSlot targetSlot = slots[targetIndices[i]];

            if (piece == null || targetSlot == null)
            {
                completed++;
                continue;
            }

            if (piece.CurrentSlot != null && piece.CurrentSlot.CurrentPiece == piece)
            {
                piece.CurrentSlot.CurrentPiece = null;
            }

            piece.transform.SetParent(animationRoot, true);
            piece.transform.SetAsLastSibling();
            piece.transform.DOKill();

            PuzzlePieceView capturedPiece = piece;
            PuzzleSlot capturedSlot = targetSlot;

            capturedPiece.transform.DOMove(capturedSlot.transform.position, moveDuration)
                .SetDelay(i * moveStagger)
                .SetEase(moveEase)
                .SetId(this)
                .OnComplete(() =>
                {
                    PlacePieceInSlot(capturedPiece, capturedSlot, true);
                    completed++;
                });
        }

        while (completed < total)
        {
            yield return null;
        }
    }

    private void RefreshAllPieceFeedback()
    {
        foreach (PuzzlePieceView piece in pieces)
        {
            ValidatePiece(piece);
        }
    }

    private bool IsSolved()
    {
        if (slots == null || slots.Length == 0)
            return false;

        foreach (PuzzleSlot slot in slots)
        {
            if (slot == null || slot.CurrentPiece == null)
                return false;

            if (slot.CurrentPiece.id != slot.id)
                return false;
        }

        return true;
    }

    private void StartTimer()
    {
        timerRunning = true;
        playStartRealtime = Time.realtimeSinceStartup;

        UpdateTimerDisplay(timeLimit);
        ResetTimerVisuals();

        TryStartGameplayBgm();
    }

    private void Update()
    {
        if (!timerRunning || puzzleResolved)
            return;

        float elapsed = GetElapsedTime();
        float remaining = Mathf.Max(0f, timeLimit - elapsed);

        UpdateTimerDisplay(remaining);

        if (remaining <= dangerTimeRemaining)
        {
            StartDangerTimerVisuals();
        }

        if (remaining <= 0f)
        {
            puzzleResolved = true;
            IsInputLocked = true;

            StopTimerOnly();
            StopGameplayBgmIfStartedByThisController();

            PlaySfx(failSfxId);

            PuzzleFailed?.Invoke(timeLimit);
        }
    }

    private float GetElapsedTime()
    {
        if (playStartRealtime <= 0f)
            return 0f;

        return Time.realtimeSinceStartup - playStartRealtime;
    }

    private void StopTimerOnly()
    {
        timerRunning = false;

        timerPulseTween?.Kill();
        timerPulseTween = null;

        ResetTimerVisuals();
    }

    private void UpdateTimerDisplay(float seconds)
    {
        if (timerText == null)
            return;

        int value = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = value / 60;
        int secs = value % 60;

        timerText.text = string.Format("{0:00}:{1:00}", minutes, secs);
    }

    private void StartDangerTimerVisuals()
    {
        if (timerText == null || timerPulseTween != null)
            return;

        timerPulseTween = DOTween.Sequence()
            .Append(timerText.rectTransform.DOScale(Vector3.one * 1.08f, 0.5f).SetEase(Ease.InOutSine))
            .Join(timerText.DOColor(dangerTimerColor, 0.5f))
            .SetLoops(-1, LoopType.Yoyo)
            .OnStepComplete(() => PlaySfx(dangerTimerPulseSfxId));
    }

    private void ResetTimerVisuals()
    {
        if (timerText == null)
            return;

        timerText.rectTransform.localScale = Vector3.one;
        timerText.color = normalTimerColor;
    }

    private void StopShuffleSfxIfAvailable()
    {
        if (!playAudio || !stopShuffleSfxWhenShuffleEnds)
            return;

        if (AudioManager.Instance == null)
            return;

        if (TryInvokeAudioMethod("StopSFX"))
            return;

        if (TryInvokeAudioMethod("StopSTX"))
            return;

        TryInvokeAudioMethod("StopSFX", shuffleSfxId);
    }

    private bool TryInvokeAudioMethod(string methodName)
    {
        object audio = AudioManager.Instance;
        System.Reflection.MethodInfo method = audio.GetType().GetMethod(methodName, Type.EmptyTypes);

        if (method == null)
            return false;

        method.Invoke(audio, null);
        return true;
    }

    private bool TryInvokeAudioMethod(string methodName, int intArgument)
    {
        object audio = AudioManager.Instance;
        System.Reflection.MethodInfo method = audio.GetType().GetMethod(methodName, new[] { typeof(int) });

        if (method == null)
            return false;

        method.Invoke(audio, new object[] { intArgument });
        return true;
    }

    private void PlaySfx(int sfxId)
    {
        if (!playAudio || sfxId < 0)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfxId);
        }
    }

    private void TryStartGameplayBgm()
    {
        gameplayBgmStartedByThisController = false;

        if (!playAudio)
            return;

        if (!switchBgmWhenGameplayStarts)
            return;

        if (gameplayBgmId < 0)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayBGM(gameplayBgmId);
        gameplayBgmStartedByThisController = true;
    }

    private void StopGameplayBgmIfStartedByThisController()
    {
        if (!gameplayBgmStartedByThisController)
            return;

        if (!playAudio)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        gameplayBgmStartedByThisController = false;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static bool IsSameOrder(IList<int> indices)
    {
        for (int i = 0; i < indices.Count; i++)
        {
            if (indices[i] != i)
                return false;
        }

        return true;
    }
}