using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a pointer after the player is idle, moves it from pointA to pointB, and hides it after user input.
/// Keep this file named TouchHintFinal.cs so Unity can find the MonoBehaviour class.
/// </summary>
public class TouchHintFinal : MonoBehaviour
{
    [Header("Prefabs & Points")]
    public GameObject pointerPrefab;
    public Transform pointA;
    public Transform pointB;

    [Header("Timing")]
    [Min(0f)] public float idleTimeBeforeHint = 2f;
    [Min(0f)] public float grabHold = 0.35f;
    [Min(0f)] public float moveDuration = 1.0f;
    [Min(0f)] public float releaseHold = 0.5f;
    [Min(0f)] public float pauseBetweenCycles = 0.4f;

    [Header("Options")]
    public bool pingPong = false;
    public bool isUI = false;
    public bool showOnlyOnce = true;

    [Tooltip("Legacy field kept for existing inspector setup. Gameplay BGM is not stopped by this script.")]
    public bool stopBgmOnUserTouch = false;

    [Header("Timer")]
    [SerializeField] private bool startGameTimerOnFirstInteraction = true;

    [Header("Hand Hint Instruction Narration")]
    [SerializeField] private bool playInstructionNarrationWithHandHint = true;
    [SerializeField] private SetQuestions setQuestions;
    [SerializeField] private InstructionNarrationPlayer instructionNarrationPlayer;

    [Header("Pointer Offset")]
    public float offsetX = 0f;
    public float offsetY = 0f;

    private const float QuickFade = 0.18f;

    private float idleTimer;
    private GameObject instance;
    private Sequence sequence;
    private bool hasShown;
    private bool pendingStop;
    private bool timerStarted;

    private void Awake()
    {
        ResolveOptionalReferences();
    }

    private void Update()
    {
        if (IsUserTouching())
        {
            HandleUserTouch();
            return;
        }

        if (showOnlyOnce && hasShown)
            return;

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleTimeBeforeHint)
            ShowHint();
    }

    private void HandleUserTouch()
    {
        idleTimer = 0f;

        if (instance != null)
        {
            pendingStop = true;
            return;
        }

        if (showOnlyOnce)
            hasShown = true;

        StartGameTimerOnce();
    }

    private bool IsUserTouching()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    return true;
            }

            return false;
        }

        return Input.GetMouseButton(0);
    }

    private void ShowHint()
    {
        if (!ValidateReferences() || instance != null)
            return;

        hasShown = true;
        pendingStop = false;

        instance = Instantiate(pointerPrefab, transform);
        instance.SetActive(true);
        SetPointerPosition(instance, pointA.position);
        SetInstanceAlpha(instance, 1f);

        CreateAndPlaySequence();
        PlayHandHintNarration();
    }

    private void CreateAndPlaySequence()
    {
        KillSequenceOnly();

        sequence = DOTween.Sequence()
            .SetAutoKill(false)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);

        sequence.AppendInterval(grabHold);
        AppendMoveToB(sequence);
        sequence.AppendInterval(releaseHold);

        if (pingPong)
            AppendInvisibleReturnToA(sequence);
        else
            sequence.AppendCallback(() => SetPointerPosition(instance, pointA.position));

        sequence.AppendInterval(pauseBetweenCycles);
        sequence.AppendCallback(CompleteCycleOrContinue);
        sequence.OnKill(() => sequence = null);
        sequence.SetLoops(-1, LoopType.Restart);
        sequence.Play();
    }

    private void AppendMoveToB(Sequence seq)
    {
        if (instance == null)
            return;

        if (isUI)
        {
            RectTransform rt = instance.GetComponent<RectTransform>();
            RectTransform parent = rt != null ? rt.parent as RectTransform : null;
            if (rt == null || parent == null)
                return;

            Camera cam = GetCanvasCamera(parent);
            Vector2 anchoredA = WorldToLocalAnchored(parent, pointA.position, cam) + new Vector2(offsetX, offsetY);
            Vector2 anchoredB = WorldToLocalAnchored(parent, pointB.position, cam) + new Vector2(offsetX, offsetY);

            rt.anchoredPosition = anchoredA;
            seq.Append(rt.DOAnchorPos(anchoredB, moveDuration).SetEase(Ease.OutQuad));
            return;
        }

        instance.transform.position = pointA.position + GetOffset();
        seq.Append(instance.transform.DOMove(pointB.position + GetOffset(), moveDuration).SetEase(Ease.OutQuad));
    }

    private void AppendInvisibleReturnToA(Sequence seq)
    {
        if (instance == null)
            return;

        if (isUI)
        {
            CanvasGroup canvasGroup = EnsureCanvasGroup(instance);
            seq.Append(canvasGroup.DOFade(0f, QuickFade));
            seq.AppendCallback(() => SetPointerPosition(instance, pointA.position));
            seq.Append(canvasGroup.DOFade(1f, QuickFade));
            return;
        }

        SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            seq.Append(spriteRenderer.DOFade(0f, QuickFade));
            seq.AppendCallback(() => SetPointerPosition(instance, pointA.position));
            seq.Append(spriteRenderer.DOFade(1f, QuickFade));
            return;
        }

        seq.AppendCallback(() => { if (instance != null) instance.SetActive(false); });
        seq.AppendCallback(() => SetPointerPosition(instance, pointA.position));
        seq.AppendCallback(() => { if (instance != null) instance.SetActive(true); });
    }

    private void CompleteCycleOrContinue()
    {
        if (!pendingStop)
            return;

        pendingStop = false;
        HideHintImmediate(startTimer: true);
    }

    private void HideHintImmediate(bool startTimer)
    {
        StopHandHintNarration();
        KillSequenceOnly();

        if (startTimer)
            StartGameTimerOnce();

        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }
    }

    private void StartGameTimerOnce()
    {
        if (!startGameTimerOnFirstInteraction || timerStarted)
            return;

        timerStarted = true;

        if (GameTimer.Instance != null)
            GameTimer.Instance.StartTimer();
    }

    private void PlayHandHintNarration()
    {
        if (!playInstructionNarrationWithHandHint)
            return;

        ResolveOptionalReferences();
        if (instructionNarrationPlayer == null)
            return;

        string emotionLabel = setQuestions != null ? setQuestions.SelectedTargetEmotionLabel : string.Empty;
        instructionNarrationPlayer.PlayInstruction(emotionLabel);
    }

    private void StopHandHintNarration()
    {
        if (instructionNarrationPlayer != null)
            instructionNarrationPlayer.StopNarration();
    }

    private void KillSequenceOnly()
    {
        if (sequence != null && sequence.IsActive())
            sequence.Kill(false);

        sequence = null;
    }

    private void SetPointerPosition(GameObject go, Vector3 worldPos)
    {
        if (go == null)
            return;

        if (isUI)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            RectTransform parent = rt != null ? rt.parent as RectTransform : null;
            if (rt == null || parent == null)
                return;

            Camera cam = GetCanvasCamera(parent);
            rt.anchoredPosition = WorldToLocalAnchored(parent, worldPos, cam) + new Vector2(offsetX, offsetY);
            return;
        }

        go.transform.position = worldPos + GetOffset();
    }

    private Vector3 GetOffset()
    {
        return new Vector3(offsetX, offsetY, 0f);
    }

    private CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        CanvasGroup canvasGroup = go.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : go.AddComponent<CanvasGroup>();
    }

    private void SetInstanceAlpha(GameObject go, float alpha)
    {
        if (go == null)
            return;

        if (isUI)
        {
            EnsureCanvasGroup(go).alpha = alpha;
            return;
        }

        SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
        else
        {
            go.SetActive(alpha > 0.5f);
        }
    }

    private Camera GetCanvasCamera(RectTransform parent)
    {
        Canvas canvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private Vector2 WorldToLocalAnchored(RectTransform parent, Vector3 worldPos, Camera cam)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out local);
        return local;
    }

    private bool ValidateReferences()
    {
        if (pointerPrefab != null && pointA != null && pointB != null)
            return true;

        Debug.LogWarning("TouchHintFinal: pointerPrefab, pointA, or pointB is missing.", this);
        return false;
    }

    private void ResolveOptionalReferences()
    {
        if (setQuestions == null)
            setQuestions = FindObjectOfType<SetQuestions>();

        if (instructionNarrationPlayer == null)
            instructionNarrationPlayer = FindObjectOfType<InstructionNarrationPlayer>();
    }

    private void OnDisable()
    {
        HideHintImmediate(startTimer: false);
    }

    private void OnValidate()
    {
        idleTimeBeforeHint = Mathf.Max(0f, idleTimeBeforeHint);
        grabHold = Mathf.Max(0f, grabHold);
        moveDuration = Mathf.Max(0f, moveDuration);
        releaseHold = Mathf.Max(0f, releaseHold);
        pauseBetweenCycles = Mathf.Max(0f, pauseBetweenCycles);
    }
}
