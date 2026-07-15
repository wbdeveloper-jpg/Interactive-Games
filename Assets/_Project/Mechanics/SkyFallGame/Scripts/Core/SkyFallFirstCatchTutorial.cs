using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkyFallFirstCatchTutorial : MonoBehaviour
{
    [Header("Core References")]
    public SkyFallGameManager gameManager;
    public RectTransform basket;
    public RectTransform overlayRoot;
    public RectTransform questionTarget;
    public Image handImage;
    public TMP_Text instructionText;

    [Header("Tutorial Message")]
    public bool showInstructionText = true;
    public string readQuestionMessage = "Read the question. Tap anywhere to continue.";
    public string instructionMessage = "Hold and drag the basket left and right!";
    public string catchInstructionMessage = "Catch the correct answer!";
    public string successMessage = "Great! Now catch the correct answers!";
    public Vector2 instructionOffset = new Vector2(0f, 155f);

    [Header("Read Question Step")]
    public Vector2 questionHandOffset = new Vector2(0f, -78f);
    public Vector2 readInstructionOffset = new Vector2(0f, -165f);
    public float questionHandRotationZ = 0f;
    public float questionHandBounceDistance = 10f;
    public float questionHandBounceSpeed = 2.2f;
    public float continueInputGuardTime = 0.10f;

    [Header("Wide Drag Demonstration")]
    public float showDelay = 0.25f;
    [Range(0.3f, 1f)] public float horizontalTravelNormalized = 0.82f;
    public Vector2 handOffset = new Vector2(0f, -8f);
    public float handPressDistance = 12f;
    [Range(0.05f, 1f)] public float ghostBasketAlpha = 0.32f;
    [Range(0.5f, 1f)] public float handPressedScale = 0.84f;
    public float tapDuration = 0.22f;
    public float oneWayDragDuration = 0.72f;
    public float loopPause = 0.25f;
    public float fadeDuration = 0.18f;

    [Header("Instruction Breathing")]
    public bool animateInstructionBreathing = true;
    [Range(0f, 0.25f)] public float instructionBreathAmount = 0.08f;
    public float instructionBreathCycle = 1.25f;

    [Header("Practice Drop")]
    [Tooltip("Leave blank to generate a correct item from the assigned content provider.")]
    public string dummyDisplayTextOverride = "";
    [Min(1)] public int correctDropGenerateAttempts = 40;
    public float dummyFallDuration = 3.8f;
    public float dummySidePadding = 90f;
    public float dummyTopPadding = 15f;
    public float dummyCatchHeightOffset = 42f;
    public float minimumDummyFallDistance = 300f;
    public float dummyHoverAmplitude = 6f;
    public float dummyHoverSpeed = 2.2f;
    public float minimumPlayerDragDistance = 45f;
    public float successMessageDuration = 0.55f;

    [Header("First-Time Rule")]
    public bool rememberCompletion = true;
    public bool includeSceneNameInPlayerPrefsKey = true;
    public string playerPrefsKey = "SkyFall.FirstCatchTutorial.Completed";

    private CanvasGroup overlayCanvasGroup;
    private RectTransform handRect;
    private RectTransform instructionRect;
    private RectTransform ghostBasket;
    private SkyFallFallingItem dummyItem;
    private Coroutine tutorialRoutine;

    private bool completedThisSession;
    private bool tutorialActive;
    private bool playerMovedBasket;
    private bool dummyReachedHoverPoint;
    private float accumulatedBasketMovement;
    private float lastBasketX;
    private float dummyHoverY;
    private float dummyFallSpeed;
    private Vector2 guideCenter;

    private void Awake()
    {
        CacheReferences();
        SetVisualsHiddenImmediate();
    }

    private void OnEnable()
    {
        CacheReferences();
        Subscribe();
    }

    private void Start()
    {
        if (gameManager != null && gameManager.IsRunning)
            TryShowTutorial();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopAndCleanup(true);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void HandleGameStarted()
    {
        TryShowTutorial();
    }

    private void HandleGameEnded()
    {
        StopAndCleanup(false);
    }

    public void TryShowTutorial()
    {
        CacheReferences();

        if (gameManager == null || basket == null || overlayRoot == null)
            return;

        if (completedThisSession || IsCompletionRemembered())
        {
            StopAndCleanup(true);
            return;
        }

        StopAndCleanup(false);

        tutorialActive = true;
        playerMovedBasket = false;
        dummyReachedHoverPoint = false;
        accumulatedBasketMovement = 0f;
        lastBasketX = basket.anchoredPosition.x;

        // The real game is running, but its timer and real drops are held.
        // The carrier may continue moving as a visual-only background effect.
        // Basket input remains enabled so the child can complete the practice catch.
        gameManager.SetTutorialGameplayHold(true);

        tutorialRoutine = StartCoroutine(TutorialRoutine());
    }

    [ContextMenu("Reset Tutorial Completion")]
    public void ResetTutorialCompletion()
    {
        completedThisSession = false;
        PlayerPrefs.DeleteKey(GetResolvedPlayerPrefsKey());
        PlayerPrefs.Save();
    }

    public void HideTutorialImmediate()
    {
        StopAndCleanup(true);
    }

    private IEnumerator TutorialRoutine()
    {
        yield return WaitForActiveGameplaySeconds(Mathf.Max(0f, showDelay));

        if (!CanContinueTutorial())
        {
            StopAndCleanup(true);
            yield break;
        }

        guideCenter = GetBasketPositionInOverlay();
        overlayCanvasGroup.alpha = 0f;

        yield return RunReadQuestionStep();

        if (!CanContinueTutorial())
        {
            StopAndCleanup(true);
            yield break;
        }

        yield return RunBasketDragStep();

        if (!CanContinueTutorial() || !playerMovedBasket)
        {
            StopAndCleanup(true);
            yield break;
        }

        yield return RunPracticeCatchStep();

        if (completedThisSession)
        {
            tutorialRoutine = null;
            yield break;
        }

        StopAndCleanup(true);
    }

    private IEnumerator RunReadQuestionStep()
    {
        Vector2 questionPosition = GetQuestionPositionInOverlay();

        if (instructionText != null)
        {
            instructionText.text = readQuestionMessage;
            instructionText.gameObject.SetActive(showInstructionText);
        }

        if (instructionRect != null)
            instructionRect.anchoredPosition = questionPosition + readInstructionOffset;

        if (handImage != null)
        {
            // This slot is intentionally blank until the user assigns a hand sprite.
            handImage.enabled = handImage.sprite != null;
            handImage.gameObject.SetActive(true);
        }

        yield return WaitForPointerRelease();
        yield return WaitForActiveGameplaySeconds(Mathf.Max(0f, continueInputGuardTime));

        float stepTime = 0f;
        float visibleAlpha = 0f;

        while (CanContinueTutorial())
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            float dt = Time.unscaledDeltaTime;
            stepTime += dt;
            visibleAlpha = Mathf.MoveTowards(visibleAlpha, 1f, dt / Mathf.Max(0.01f, fadeDuration));
            overlayCanvasGroup.alpha = visibleAlpha;

            UpdateReadQuestionHand(questionPosition, stepTime);
            UpdateInstructionBreathing();

            if (WasPointerPressedThisFrame())
                yield break;

            yield return null;
        }
    }

    private IEnumerator RunBasketDragStep()
    {
        CreateGhostBasket();

        if (ghostBasket == null)
        {
            Debug.LogWarning("SkyFall first-catch tutorial could not create the basket guide. Normal gameplay will continue.");
            yield break;
        }

        guideCenter = GetBasketPositionInOverlay();
        playerMovedBasket = false;
        accumulatedBasketMovement = 0f;
        lastBasketX = basket.anchoredPosition.x;

        if (instructionText != null)
        {
            instructionText.text = instructionMessage;
            instructionText.gameObject.SetActive(showInstructionText);
        }

        if (instructionRect != null)
            instructionRect.anchoredPosition = guideCenter + instructionOffset;

        if (handImage != null)
        {
            handImage.enabled = handImage.sprite != null;
            handImage.gameObject.SetActive(true);
        }

        float guideTime = 0f;

        while (CanContinueTutorial() && !playerMovedBasket)
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            float dt = Time.unscaledDeltaTime;
            guideTime += dt;

            TrackPlayerBasketMovement();
            UpdateGuideAnimation(guideTime);
            UpdateInstructionBreathing();
            yield return null;
        }

        DestroyGhostBasket();

        if (handImage != null)
            handImage.gameObject.SetActive(false);
    }

    private IEnumerator RunPracticeCatchStep()
    {
        if (instructionText != null)
        {
            instructionText.text = catchInstructionMessage;
            instructionText.gameObject.SetActive(showInstructionText);
        }

        if (instructionRect != null)
            instructionRect.anchoredPosition = guideCenter + instructionOffset;

        CreateDummyItem();

        if (dummyItem == null)
        {
            Debug.LogWarning("SkyFall first-catch tutorial could not create the practice answer. Normal gameplay will continue.");
            yield break;
        }

        while (CanContinueTutorial())
        {
            if (Time.timeScale <= 0f)
            {
                yield return null;
                continue;
            }

            UpdateDummyItem(Time.unscaledDeltaTime);
            UpdateInstructionBreathing();

            if (dummyItem != null && IsOverlapping(dummyItem.CatchRect, basket))
            {
                yield return CompleteTutorialRoutine();
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator CompleteTutorialRoutine()
    {
        tutorialActive = false;
        completedThisSession = true;

        if (rememberCompletion)
        {
            PlayerPrefs.SetInt(GetResolvedPlayerPrefsKey(), 1);
            PlayerPrefs.Save();
        }

        DestroyGhostBasket();

        if (handImage != null)
            handImage.gameObject.SetActive(false);

        if (instructionText != null)
        {
            instructionText.text = successMessage;
            instructionText.gameObject.SetActive(showInstructionText);
        }

        if (gameManager != null && gameManager.sfxSource != null && gameManager.correctClip != null)
            gameManager.sfxSource.PlayOneShot(gameManager.correctClip);

        if (dummyItem != null)
        {
            SkyFallFallingItem caughtDummy = dummyItem;

            yield return caughtDummy.AnimateCorrectAbsorb(
                basket,
                gameManager != null ? gameManager.correctItemBasketOffset : new Vector2(0f, 18f),
                gameManager != null ? gameManager.correctItemAbsorbDuration : 0.28f,
                gameManager != null ? gameManager.correctItemAbsorbEndScale : 0.18f
            );

            if (caughtDummy != null)
                Destroy(caughtDummy.gameObject);

            dummyItem = null;
        }

        float successTimer = 0f;
        while (successTimer < successMessageDuration)
        {
            if (Time.timeScale > 0f)
            {
                successTimer += Time.unscaledDeltaTime;
                UpdateInstructionBreathing();
            }

            yield return null;
        }

        float startAlpha = overlayCanvasGroup != null ? overlayCanvasGroup.alpha : 1f;
        float fadeTimer = 0f;

        while (fadeTimer < fadeDuration)
        {
            if (Time.timeScale > 0f)
            {
                fadeTimer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeTimer / Mathf.Max(0.01f, fadeDuration));

                if (overlayCanvasGroup != null)
                    overlayCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                UpdateInstructionBreathing();
            }

            yield return null;
        }

        SetVisualsHiddenImmediate();

        if (gameManager != null)
            gameManager.SetTutorialGameplayHold(false);
    }

    private void TrackPlayerBasketMovement()
    {
        if (basket == null || playerMovedBasket)
            return;

        float currentX = basket.anchoredPosition.x;
        accumulatedBasketMovement += Mathf.Abs(currentX - lastBasketX);
        lastBasketX = currentX;

        if (accumulatedBasketMovement >= Mathf.Max(1f, minimumPlayerDragDistance))
            playerMovedBasket = true;
    }

    private void UpdateDummyItem(float deltaTime)
    {
        if (dummyItem == null)
            return;

        RectTransform dummyRect = dummyItem.RectTransform;

        if (!dummyReachedHoverPoint)
        {
            dummyItem.Tick(
                dummyFallSpeed,
                deltaTime,
                -100000f,
                Mathf.Max(0.1f, dummyFallDuration)
            );

            if (dummyRect.anchoredPosition.y <= dummyHoverY)
            {
                Vector2 position = dummyRect.anchoredPosition;
                position.y = dummyHoverY;
                dummyRect.anchoredPosition = position;
                dummyReachedHoverPoint = true;
            }

            return;
        }

        Vector2 hoverPosition = dummyRect.anchoredPosition;
        hoverPosition.y = dummyHoverY + Mathf.Sin(Time.unscaledTime * dummyHoverSpeed) * dummyHoverAmplitude;
        dummyRect.anchoredPosition = hoverPosition;
    }

    private void UpdateGuideAnimation(float time)
    {
        if (ghostBasket == null)
            return;

        Vector2 left;
        Vector2 right;
        GetWideGuidePoints(guideCenter, out left, out right);

        float press = Mathf.Max(0.01f, tapDuration);
        float oneWay = Mathf.Max(0.01f, oneWayDragDuration);
        float pause = Mathf.Max(0f, loopPause);
        float total = press + oneWay + oneWay * 2f + oneWay + press + pause;
        float phase = Mathf.Repeat(time, Mathf.Max(0.01f, total));

        Vector2 position = guideCenter;
        float pressed01 = 0f;

        if (phase < press)
        {
            pressed01 = Mathf.SmoothStep(0f, 1f, phase / press);
        }
        else if (phase < press + oneWay)
        {
            float t = Mathf.SmoothStep(0f, 1f, (phase - press) / oneWay);
            position = Vector2.Lerp(guideCenter, left, t);
            pressed01 = 1f;
        }
        else if (phase < press + oneWay * 3f)
        {
            float t = Mathf.SmoothStep(0f, 1f, (phase - press - oneWay) / (oneWay * 2f));
            position = Vector2.Lerp(left, right, t);
            pressed01 = 1f;
        }
        else if (phase < press + oneWay * 4f)
        {
            float t = Mathf.SmoothStep(0f, 1f, (phase - press - oneWay * 3f) / oneWay);
            position = Vector2.Lerp(right, guideCenter, t);
            pressed01 = 1f;
        }
        else if (phase < press * 2f + oneWay * 4f)
        {
            float t = (phase - press - oneWay * 4f) / press;
            pressed01 = 1f - Mathf.SmoothStep(0f, 1f, t);
        }

        SetGhostAndHandPosition(position, pressed01);
    }

    private void SetGhostAndHandPosition(Vector2 position, float pressed01)
    {
        if (ghostBasket != null)
            ghostBasket.anchoredPosition = position;

        if (handRect != null)
        {
            handRect.localRotation = Quaternion.identity;
            handRect.anchoredPosition = position + handOffset + Vector2.down * handPressDistance * pressed01;
            handRect.localScale = Vector3.one * Mathf.Lerp(1f, handPressedScale, pressed01);
        }
    }

    private void UpdateReadQuestionHand(Vector2 questionPosition, float time)
    {
        if (handRect == null)
            return;

        float bounce01 = Mathf.Sin(time * Mathf.PI * 2f * questionHandBounceSpeed) * 0.5f + 0.5f;
        handRect.anchoredPosition = questionPosition + questionHandOffset + Vector2.up * questionHandBounceDistance * bounce01;
        handRect.localRotation = Quaternion.Euler(0f, 0f, questionHandRotationZ);
        handRect.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.05f, bounce01);
    }

    private void UpdateInstructionBreathing()
    {
        if (instructionRect == null)
            return;

        if (!animateInstructionBreathing)
        {
            instructionRect.localScale = Vector3.one;
            return;
        }

        float cycle = Mathf.Max(0.1f, instructionBreathCycle);
        float wave01 = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / cycle) * 0.5f + 0.5f;
        float scale = 1f + wave01 * instructionBreathAmount;
        instructionRect.localScale = Vector3.one * scale;
    }

    private void CreateDummyItem()
    {
        DestroyDummyItem();

        if (gameManager == null || gameManager.itemPrefab == null || gameManager.itemParent == null)
            return;

        SkyFallDropData data = GenerateCorrectPracticeDrop();
        dummyItem = Instantiate(gameManager.itemPrefab, gameManager.itemParent);
        dummyItem.gameObject.name = "FirstCatchTutorialDummyItem";
        dummyItem.gameObject.SetActive(true);
        dummyItem.Setup(data, gameManager.trailFxLayer);

        RectTransform parent = gameManager.itemParent;
        Vector2 basketPosition = ConvertWorldToLocal(parent, basket.position);
        float halfItemWidth = dummyItem.RectTransform.rect.width * 0.5f;
        float leftX = parent.rect.xMin + halfItemWidth + dummySidePadding;
        float rightX = parent.rect.xMax - halfItemWidth - dummySidePadding;
        float parentCenterX = (parent.rect.xMin + parent.rect.xMax) * 0.5f;

        // Spawn on the side farthest from the real basket so a genuine drag is required.
        float spawnX = basketPosition.x <= parentCenterX ? rightX : leftX;
        float spawnY = parent.rect.yMax - dummyItem.RectTransform.rect.height * 0.5f - Mathf.Max(0f, dummyTopPadding);

        float maxSafeCatchOffset = Mathf.Max(
            0f,
            (basket.rect.height + dummyItem.CatchRect.rect.height) * 0.5f - 10f
        );

        dummyHoverY = basketPosition.y + Mathf.Min(Mathf.Max(0f, dummyCatchHeightOffset), maxSafeCatchOffset);
        spawnY = Mathf.Max(spawnY, dummyHoverY + minimumDummyFallDistance);
        dummyFallSpeed = Mathf.Abs(spawnY - dummyHoverY) / Mathf.Max(0.1f, dummyFallDuration);
        dummyItem.RectTransform.anchoredPosition = new Vector2(spawnX, spawnY);
    }

    private SkyFallDropData GenerateCorrectPracticeDrop()
    {
        if (!string.IsNullOrEmpty(dummyDisplayTextOverride))
        {
            return new SkyFallDropData
            {
                displayText = dummyDisplayTextOverride,
                isCorrect = true
            };
        }

        if (gameManager != null && gameManager.contentProvider != null)
        {
            SkyFallDropContext context = new SkyFallDropContext
            {
                score = 0,
                correctCaught = 0,
                wrongCaught = 0,
                missedCorrect = 0,
                elapsedTime = 0f,
                progress01 = 0f
            };

            int attempts = Mathf.Max(1, correctDropGenerateAttempts);
            for (int i = 0; i < attempts; i++)
            {
                SkyFallDropData candidate = gameManager.contentProvider.GenerateDrop(context);
                if (candidate != null && candidate.isCorrect)
                    return candidate;
            }
        }

        return new SkyFallDropData
        {
            displayText = "✓",
            isCorrect = true
        };
    }

    private void CreateGhostBasket()
    {
        DestroyGhostBasket();

        if (basket == null || overlayRoot == null)
            return;

        GameObject ghostObject = Instantiate(basket.gameObject, overlayRoot);
        ghostObject.name = "TutorialBasketGhost";
        ghostObject.SetActive(true);

        ghostBasket = ghostObject.transform as RectTransform;
        ghostBasket.anchorMin = new Vector2(0.5f, 0.5f);
        ghostBasket.anchorMax = new Vector2(0.5f, 0.5f);
        ghostBasket.pivot = basket.pivot;
        ghostBasket.sizeDelta = basket.rect.size;
        ghostBasket.localRotation = Quaternion.identity;
        ghostBasket.localScale = Vector3.one;
        ghostBasket.anchoredPosition = GetBasketPositionInOverlay();
        ghostBasket.SetAsFirstSibling();

        MonoBehaviour[] behaviours = ghostObject.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null || behaviour is Graphic || behaviour is Mask ||
                behaviour is RectMask2D || behaviour is LayoutGroup ||
                behaviour is ContentSizeFitter || behaviour is AspectRatioFitter)
            {
                continue;
            }

            behaviour.enabled = false;
        }

        Animator[] animators = ghostObject.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
            animators[i].enabled = false;

        Graphic[] graphics = ghostObject.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;

        CanvasGroup ghostGroup = ghostObject.GetComponent<CanvasGroup>();
        if (ghostGroup == null)
            ghostGroup = ghostObject.AddComponent<CanvasGroup>();

        ghostGroup.alpha = ghostBasketAlpha;
        ghostGroup.interactable = false;
        ghostGroup.blocksRaycasts = false;
        ghostGroup.ignoreParentGroups = false;
    }

    private void GetWideGuidePoints(Vector2 center, out Vector2 left, out Vector2 right)
    {
        float halfBasketWidth = basket != null ? basket.rect.width * 0.5f : 60f;
        float leftBound = overlayRoot.rect.xMin + halfBasketWidth + 20f;
        float rightBound = overlayRoot.rect.xMax - halfBasketWidth - 20f;
        float amount = Mathf.Clamp01(horizontalTravelNormalized);

        left = new Vector2(Mathf.Lerp(center.x, leftBound, amount), center.y);
        right = new Vector2(Mathf.Lerp(center.x, rightBound, amount), center.y);
    }

    private Vector2 GetBasketPositionInOverlay()
    {
        return basket != null && overlayRoot != null
            ? ConvertWorldToLocal(overlayRoot, basket.position)
            : Vector2.zero;
    }

    private Vector2 GetQuestionPositionInOverlay()
    {
        if (questionTarget != null && overlayRoot != null)
            return ConvertWorldToLocal(overlayRoot, questionTarget.position);

        return overlayRoot != null
            ? new Vector2(0f, overlayRoot.rect.yMax - 55f)
            : Vector2.zero;
    }

    private Vector2 ConvertWorldToLocal(RectTransform targetParent, Vector3 worldPosition)
    {
        if (targetParent == null)
            return Vector2.zero;

        Canvas canvas = targetParent.GetComponentInParent<Canvas>();
        Camera cameraToUse = null;

        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cameraToUse = canvas.worldCamera;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(cameraToUse, worldPosition);
        Vector2 localPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetParent,
            screenPosition,
            cameraToUse,
            out localPosition))
        {
            return localPosition;
        }

        return Vector2.zero;
    }

    private bool IsOverlapping(RectTransform a, RectTransform b)
    {
        if (a == null || b == null)
            return false;

        Vector3[] aCorners = new Vector3[4];
        Vector3[] bCorners = new Vector3[4];
        a.GetWorldCorners(aCorners);
        b.GetWorldCorners(bCorners);

        Rect aRect = new Rect(
            aCorners[0].x,
            aCorners[0].y,
            aCorners[2].x - aCorners[0].x,
            aCorners[2].y - aCorners[0].y
        );

        Rect bRect = new Rect(
            bCorners[0].x,
            bCorners[0].y,
            bCorners[2].x - bCorners[0].x,
            bCorners[2].y - bCorners[0].y
        );

        return aRect.Overlaps(bRect);
    }

    private IEnumerator WaitForActiveGameplaySeconds(float duration)
    {
        float timer = 0f;

        while (timer < duration && CanContinueTutorial())
        {
            if (Time.timeScale > 0f)
                timer += Time.unscaledDeltaTime;

            yield return null;
        }
    }

    private IEnumerator WaitForPointerRelease()
    {
        while (CanContinueTutorial() && IsPointerHeld())
            yield return null;
    }

    private bool IsPointerHeld()
    {
        return Input.GetMouseButton(0) || Input.touchCount > 0;
    }

    private bool WasPointerPressedThisFrame()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    private bool CanContinueTutorial()
    {
        return tutorialActive &&
               !completedThisSession &&
               gameManager != null &&
               gameManager.IsRunning;
    }

    private bool IsCompletionRemembered()
    {
        return rememberCompletion && PlayerPrefs.GetInt(GetResolvedPlayerPrefsKey(), 0) == 1;
    }

    private string GetResolvedPlayerPrefsKey()
    {
        string key = string.IsNullOrEmpty(playerPrefsKey)
            ? "SkyFall.FirstCatchTutorial.Completed"
            : playerPrefsKey;

        if (includeSceneNameInPlayerPrefsKey)
            key += "." + SceneManager.GetActiveScene().name;

        return key;
    }

    private void StopAndCleanup(bool releaseGameplay)
    {
        tutorialActive = false;

        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        SetVisualsHiddenImmediate();
        DestroyGhostBasket();
        DestroyDummyItem();

        if (releaseGameplay && gameManager != null)
            gameManager.SetTutorialGameplayHold(false);
    }

    private void SetVisualsHiddenImmediate()
    {
        CacheReferences();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.interactable = false;
            overlayCanvasGroup.blocksRaycasts = false;
        }

        if (handImage != null)
        {
            handImage.gameObject.SetActive(false);
            handImage.rectTransform.localRotation = Quaternion.identity;
            handImage.rectTransform.localScale = Vector3.one;
        }

        if (instructionText != null)
        {
            instructionText.gameObject.SetActive(false);
            instructionText.rectTransform.localScale = Vector3.one;
        }
    }

    private void DestroyGhostBasket()
    {
        if (ghostBasket == null)
            return;

        if (Application.isPlaying)
            Destroy(ghostBasket.gameObject);
        else
            DestroyImmediate(ghostBasket.gameObject);

        ghostBasket = null;
    }

    private void DestroyDummyItem()
    {
        if (dummyItem == null)
            return;

        dummyItem.StopTrail();

        if (Application.isPlaying)
            Destroy(dummyItem.gameObject);
        else
            DestroyImmediate(dummyItem.gameObject);

        dummyItem = null;
    }

    private void CacheReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<SkyFallGameManager>(true);

        if (basket == null && gameManager != null)
            basket = gameManager.basket;

        if (questionTarget == null && gameManager != null && gameManager.questionText != null)
            questionTarget = gameManager.questionText.rectTransform;

        if (overlayRoot == null)
            overlayRoot = transform as RectTransform;

        if (overlayCanvasGroup == null)
            overlayCanvasGroup = GetComponent<CanvasGroup>();

        if (overlayCanvasGroup == null)
            overlayCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        overlayCanvasGroup.interactable = false;
        overlayCanvasGroup.blocksRaycasts = false;

        if (handImage != null)
            handRect = handImage.rectTransform;

        if (instructionText != null)
            instructionRect = instructionText.rectTransform;
    }

    private void Subscribe()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStarted -= HandleGameStarted;
        gameManager.OnGameEnded -= HandleGameEnded;
        gameManager.OnGameStarted += HandleGameStarted;
        gameManager.OnGameEnded += HandleGameEnded;
    }

    private void Unsubscribe()
    {
        if (gameManager == null)
            return;

        gameManager.OnGameStarted -= HandleGameStarted;
        gameManager.OnGameEnded -= HandleGameEnded;
    }
}
