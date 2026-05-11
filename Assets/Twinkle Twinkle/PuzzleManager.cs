// PuzzleManager.cs (updated with extra UI flows)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PuzzleManager : MonoBehaviour
{
    [Header("Holder container (optional)")]
    [Tooltip("If set, holders will be gathered from this transform's direct children.")]
    public Transform holdersParent;

    [Header("Manual holders (optional)")]
    public PuzzlePieceHolder[] Holders;

    [Header("Timing / animation")]
    public float delayBeforeShuffle = 2f;     // show main image for 2 seconds
    public float moveDuration = 0.6f;
    public float stagger = 0.05f;
    public Ease moveEase = Ease.OutCubic;

    [Header("Countdown Timer")]
    [Tooltip("TextMeshProUGUI to display countdown (MM:SS). Optional.")]
    public TMP_Text timerText;
    [Tooltip("Total seconds allowed for player to solve after shuffle completes.")]
    public float timeLimitSeconds = 60f;

    [Header("Extra Ui Element")]
    public GameObject extraUiPanel;
    public TextMeshProUGUI extraMessage;      // main message used for intro and fail/success (pop + fall)
    public GameObject specialStar;            // shown on success
    [Tooltip("Second text shown after specialStar on success; fades in and breathes")]
    public GameObject continuePanel;
    public TextMeshProUGUI finalContinueText;

    [Header("Intro settings")]
    public int introCountdownStart = 5;
    public float introPopDuration = 0.25f;
    public float introFadeDuration = 0.18f;

    [Header("Success/Fail UI timings")]
    public float successFallDuration = 0.25f;
    public float successPauseBeforeSpecialStar = 0.3f;
    public float specialStarPopDuration = 0.35f;
    public float timeBeforeFinalText = 5f;
    public float finalTextFadeDuration = 0.4f;

    // internals
    private int pendingMoveCompletions = 0;
    private Coroutine countdownCoroutine = null;
    private bool timerRunning = false;
    private float timeRemaining = 0f;
    private float timerStartRealtime = 0f;

    // UI effect state
    private bool pulse30Triggered = false;
    private bool breathingStarted = false;
    private Tween breathingScaleTween = null;
    private Tween breathingColorTween = null;
    private Tween pulseTween = null;

    // Guard so GameOver is only shown once
    private bool gameOverTriggered = false;

    // sequence refs so we can kill them
    private Sequence introSequence = null;
    private Sequence successSequence = null;
    private Sequence failSequence = null;

    void OnEnable()
    {
        AutoFillIfNeeded();

        // Start the intro flow (extra UI -> countdown -> shuffle)
        StartCoroutine(IntroSequenceCoroutine());
    }

    void AutoFillIfNeeded()
    {
        if ((Holders == null || Holders.Length == 0) && holdersParent != null)
        {
            var list = new List<PuzzlePieceHolder>();
            foreach (Transform child in holdersParent)
            {
                var h = child.GetComponent<PuzzlePieceHolder>();
                if (h != null) list.Add(h);
            }
            Holders = list.ToArray();
        }

        if (Holders == null || Holders.Length == 0)
        {
            Debug.LogWarning("PuzzleManager: No holders found. Assign holdersParent or fill Holders array.");
        }
    }

    IEnumerator IntroSequenceCoroutine()
    {
        // Ensure panel exists; if not, just wait and shuffle after the configured delay
        if (extraUiPanel == null || extraMessage == null)
        {
            yield return new WaitForSeconds(delayBeforeShuffle);
            ShufflePiecesToHoldersAnimated();
            yield break;
        }

        // Show the panel and animate message pop-in
        extraUiPanel.SetActive(true);
        SetupMessagePopState();

        // Pop-in animation (scale small -> 1, fade-in)
        PlayMessagePopIn(extraMessage, introPopDuration, introFadeDuration);

        // Countdown text (introCountdownStart .. 0)
        for (int i = introCountdownStart; i >= 0; i--)
        {
            extraMessage.text = $"Shuffling in {i}";
            yield return new WaitForSeconds(1f);
        }

        // Fade out the message, then immediately shuffle and hide panel
        yield return StartCoroutine(FadeOutTMP(extraMessage, 0.25f));
        extraUiPanel.SetActive(false);

        // Start shuffle immediately
        ShufflePiecesToHoldersAnimated();
    }

    // --- Shuffle logic (unchanged except we removed the earlier automatic StartCoroutine call) ---
    IEnumerator ShuffleAfterDelayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShufflePiecesToHoldersAnimated();
    }

    /// <summary>
    /// Public call to shuffle now (animated).
    /// </summary>
    public void ShuffleNow()
    {
        ShufflePiecesToHoldersAnimated();
    }

    void ShufflePiecesToHoldersAnimated()
    {
        if (Holders == null || Holders.Length == 0)
        {
            Debug.LogError("PuzzleManager: No holders to shuffle.");
            return;
        }

        // Collect current pieces (first child of each holder, if any)
        List<Transform> pieces = new List<Transform>();

        for (int i = 0; i < Holders.Length; i++)
        {
            var h = Holders[i];
            if (h == null) continue;

            if (h.transform.childCount > 0)
            {
                Transform pieceT = h.transform.GetChild(0);
                if (pieceT != null)
                {
                    pieces.Add(pieceT);
                }
            }
        }

        if (pieces.Count == 0)
        {
            Debug.LogWarning("PuzzleManager: No pieces found inside holders to shuffle.");
            return;
        }

        AudioManager.Instance.PlaySFX(5);

        // Shuffle holder indices (so pieces can move into empty holders as well)
        int holderCount = Holders.Length;
        List<int> holderIndices = new List<int>(holderCount);
        for (int i = 0; i < holderCount; i++) holderIndices.Add(i);

        for (int i = holderIndices.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = holderIndices[i];
            holderIndices[i] = holderIndices[j];
            holderIndices[j] = tmp;
        }

        // Reset any running timer (if shuffle is requested again)
        StopTimerIfRunning();

        // Prepare completion counter
        pendingMoveCompletions = pieces.Count;

        // Determine reparent root for animation (prefer holdersParent root, fallback to manager root)
        Transform animationRoot = (holdersParent != null) ? holdersParent.root : transform.root;

        // Schedule tweens
        for (int i = 0; i < pieces.Count; i++)
        {
            Transform pieceT = pieces[i];
            int targetHolderIndex = holderIndices[i];
            PuzzlePieceHolder targetHolder = Holders[targetHolderIndex];
            if (targetHolder == null)
            {
                // reduce pending if there's no valid target (shouldn't normally happen)
                pendingMoveCompletions--;
                continue;
            }

            // Kill any existing tweens on piece
            pieceT.DOKill();

            // Temporarily reparent to top root so animation isn't influenced by holder hierarchy.
            // Use worldPositionStays = true to preserve world position immediately after reparent.
            pieceT.SetParent(animationRoot, worldPositionStays: true);

            Vector3 targetWorldPos = targetHolder.transform.position;

            // capture local variables for closure
            Transform capturedPiece = pieceT;
            PuzzlePieceHolder capturedTarget = targetHolder;

            capturedPiece.DOMove(targetWorldPos, moveDuration)
                         .SetDelay(i * stagger)
                         .SetEase(moveEase)
                         .OnComplete(() =>
                         {
                             // Reparent to the target holder and snap local transform
                             capturedPiece.SetParent(capturedTarget.transform, worldPositionStays: false);
                             capturedPiece.localPosition = Vector3.zero;
                             capturedPiece.localRotation = Quaternion.identity;
                             capturedPiece.localScale = Vector3.one;

                             // making the lay out active
                             Image layout = capturedPiece.GetChild(0)?.GetComponent<Image>();
                             if (layout != null)
                             {
                                 LayerFadeIn(layout, 0.1f);
                             }

                             // Ensure UI pieces fit: call your helper to set anchors/stretch
                             var rt = capturedPiece.GetComponent<RectTransform>();
                             if (rt != null)
                             {
                                 PuzzlePiece.SetStretchWithMargins(rt, 0f);
                             }

                             // when each piece completes, decrement counter and when last completes, start timer
                             pendingMoveCompletions--;
                             if (pendingMoveCompletions <= 0)
                             {
                                 // all moves done -> start countdown for player solving
                                 StartCountdown();
                             }
                         });
        }

        Debug.Log("PuzzleManager: Shuffle started (animated).");
    }

    // --- Timer logic ---
    private void StartCountdown()
    {
        AudioManager.Instance.PlayBGM(0);
        // Reset UI effect flags
        pulse30Triggered = false;
        StopBreathing(); // ensure breathing not active from previous run

        // Reset values
        timeRemaining = timeLimitSeconds;
        timerRunning = true;
        timerStartRealtime = Time.realtimeSinceStartup;

        // Start or restart coroutine
        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        UpdateTimerDisplay(timeRemaining);
        while (timeRemaining > 0f && !gameOverTriggered)
        {
            yield return null;
            // Use realtime so it's unaffected by timeScale
            timeRemaining = Mathf.Max(0f, timeLimitSeconds - (Time.realtimeSinceStartup - timerStartRealtime));
            UpdateTimerDisplay(timeRemaining);

            // check thresholds for UI effects:
            float elapsed = timeLimitSeconds - timeRemaining;

            // Trigger one-time pulse at 30s elapsed
            if (!pulse30Triggered && elapsed >= 30f)
            {
                pulse30Triggered = true;
                Trigger30SecondPulse();
            }

            // Start continuous breathing when <= 15s remaining
            if (!breathingStarted && timeRemaining <= 15f)
            {
                StartBreathing();
            }

            // If at any point puzzle is solved while timer running, break early to handle solved result here
            if (IsSolvedInternal())
            {
                // stop countdown loop; we'll handle success sequence below
                StopTimerIfRunning();
                StartCoroutine(HandleSolvedSequenceCoroutine());
                yield break;
            }
        }

        timerRunning = false;
        countdownCoroutine = null;

        // If GameOver already triggered elsewhere, skip
        if (gameOverTriggered)
        {
            yield break;
        }

        // time up — check solved; if not solved, show fail GameOver UI sequence
        if (!IsSolvedInternal())
        {
            StartCoroutine(HandleFailSequenceCoroutine());
            Debug.Log("time up - not solved (starting fail UI)");
        }
        else
        {
            // solved exactly as time hit zero -> handle as solved
            StartCoroutine(HandleSolvedSequenceCoroutine());
        }

        // stop any breathing/pulse effects now
        StopBreathing();
        KillPulseTween();
    }

    private void StopTimerIfRunning()
    {
        if (timerRunning)
        {
            timerRunning = false;
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }
        }

        UpdateTimerDisplay(0f);
        timerStartRealtime = 0f;

        // reset any visual effects
        StopBreathing();
        KillPulseTween();
    }

    private void UpdateTimerDisplay(float seconds)
    {
        if (timerText != null)
        {
            int s = Mathf.CeilToInt(seconds);
            if (s < 0) s = 0;
            int mins = s / 60;
            int secs = s % 60;
            timerText.text = string.Format("{0:00}:{1:00}", mins, secs);
        }
    }

    // --- 30s pulse effect (one-shot) ---
    private void Trigger30SecondPulse()
    {
        if (timerText == null) return;

        KillPulseTween(); // ensure we don't clash with existing pulse


        // Pulse: quick scale up and color to red, then back.
        Vector3 originalScale = timerText.rectTransform.localScale;
        Color originalColor = timerText.color;

        Sequence seq = DOTween.Sequence();
        seq.Append(timerText.rectTransform.DOScale(originalScale * 1.25f, 0.18f).SetEase(Ease.OutBack));
        seq.Join(timerText.DOColor(Color.red, 0.18f));
        seq.Append(timerText.rectTransform.DOScale(originalScale, 0.22f).SetEase(Ease.InBack));
        seq.Join(timerText.DOColor(originalColor, 0.22f));
        seq.OnComplete(() =>
        {
            // Reset (redundant, but safe)
            timerText.rectTransform.localScale = originalScale;
            timerText.color = originalColor;
        });

        pulseTween = seq;
        Debug.Log("30s passed — pulse triggered");
    }

    private void KillPulseTween()
    {
        if (pulseTween != null)
        {
            pulseTween.Kill();
            pulseTween = null;
        }
    }

    // --- Breathing effect (continuous for last 15s) ---
    private void StartBreathing()
    {
        if (timerText == null) return;
        if (breathingStarted) return;

        breathingStarted = true;

        // Ensure any one-shot pulse is stopped to avoid visual clash
        KillPulseTween();

        // Breathing: gentle scale tween + color tween loop
        RectTransform rt = timerText.rectTransform;
        Vector3 baseScale = rt.localScale;
        float scaleFactor = 1.08f;
        float halfDuration = 0.7f; // time to go up or down

        breathingScaleTween = rt.DOScale(baseScale * scaleFactor, halfDuration)
                                .SetEase(Ease.InOutSine)
                                .SetLoops(-1, LoopType.Yoyo)
                                .OnStepComplete(() =>
                                {
                                    AudioManager.Instance.PlaySFX(6);
                                });

        Color originalColor = timerText.color;
        Color targetColor = Color.red;
        breathingColorTween = timerText.DOColor(targetColor, halfDuration)
                                        .SetEase(Ease.InOutSine)
                                        .SetLoops(-1, LoopType.Yoyo);

        Debug.Log("Breathing started — last 15s");
    }

    private void StopBreathing()
    {
        if (!breathingStarted) return;

        breathingStarted = false;

        if (breathingScaleTween != null)
        {
            breathingScaleTween.Kill();
            breathingScaleTween = null;
        }

        if (breathingColorTween != null)
        {
            breathingColorTween.Kill();
            breathingColorTween = null;
        }

        // Reset UI to default
        if (timerText != null)
        {
            timerText.rectTransform.localScale = Vector3.one;
            timerText.color = Color.white;
        }

        Debug.Log("Breathing stopped");
    }

    // --- Checking / Results ---
    /// <summary>
    /// Checks if puzzle is solved: each holder must have a PuzzlePiece child whose id matches holder.id.
    /// Logs "done" if solved, otherwise "continue".
    /// If solved and timer was running (or had run), it triggers the timed result handling.
    /// </summary>
    public void CheckSolved()
    {
        bool solved = IsSolvedInternal();
        if (solved)
        {
            Debug.Log("done " + timeRemaining);

            // stop breathing/pulse once solved
            StopBreathing();
            KillPulseTween();

            // Start success UI sequence (this replaces immediate GameOver)
            if (!gameOverTriggered)
            {
                StopTimerIfRunning();
                StartCoroutine(HandleSolvedSequenceCoroutine());
            }
        }
        else
        {
            Debug.Log("continue");
        }
    }

    // internal check w/o logging
    private bool IsSolvedInternal()
    {
        if (Holders == null || Holders.Length == 0) return false;

        foreach (var holder in Holders)
        {
            if (holder == null) return false;
            if (holder.transform.childCount == 0) return false;

            var placedPiece = holder.transform.GetChild(0).GetComponent<PuzzlePiece>();
            if (placedPiece == null) return false;
            if (placedPiece.id != holder.id) return false;
        }

        return true;
    }

    /// <summary>
    /// The success UI sequence coroutine: show falling message, special star, final text (breathing) and wait for click.
    /// </summary>
    private IEnumerator HandleSolvedSequenceCoroutine()
    {
        AudioManager.Instance.StopBGM();
        // Prevent re-entry
        if (gameOverTriggered) yield break;

        // Calculate elapsed
        float elapsed = 0f;
        elapsed = timeLimitSeconds - timeRemaining;
        // Determine short message based on elapsed (reuse your result tiers)
        string resultMsg = "Done";
        if (elapsed > 0f)
        {
            if (elapsed <= 30f) resultMsg = "Excellent!";
            else if (elapsed <= 60f) resultMsg = "Good!";
            else resultMsg = "Done!";
        }

        // Activate extra panel and show fall-from-top animation with extraMessage
        if (extraUiPanel != null && extraMessage != null)
        {
            extraUiPanel.SetActive(true);
            extraMessage.text = resultMsg;
            // place message off-top (y + 200) then fall in quickly
            RectTransform rt = extraMessage.rectTransform;
            Vector3 origPos = rt.anchoredPosition;
            rt.anchoredPosition = origPos + Vector3.up * 400f;
            Canvas.ForceUpdateCanvases();

            // fall fast
            AudioManager.Instance.PlaySFX(2);
            rt.DOAnchorPos(origPos, successFallDuration).SetEase(Ease.OutExpo);
            extraMessage.DOFade(1f, successFallDuration * 0.7f).SetEase(Ease.OutCubic);

            yield return new WaitForSeconds(successFallDuration + successPauseBeforeSpecialStar);

            // hide message
            yield return StartCoroutine(FadeOutTMP(extraMessage, 0.25f));
        }

        // Show special star with pop animation
        if (specialStar != null)
        {
            specialStar.SetActive(true);
            specialStar.transform.localScale = Vector3.zero;
            specialStar.transform.DOScale(Vector3.one, specialStarPopDuration).SetEase(Ease.OutBack);
        }

        // Wait before final text
        yield return new WaitForSeconds(timeBeforeFinalText);

        // Show finalContinueText with fade in + breathing loop
        if (finalContinueText != null && extraUiPanel != null)
        {
            continuePanel.SetActive(true);
            finalContinueText.gameObject.SetActive(true);
            Color fc = finalContinueText.color;
            fc.a = 0f;
            finalContinueText.color = fc;
            finalContinueText.DOFade(1f, finalTextFadeDuration).SetEase(Ease.OutCubic);

            // breathing: scale loop
            RectTransform frt = finalContinueText.rectTransform;
            breathingScaleTween?.Kill();
            breathingScaleTween = frt.DOScale(1.08f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

            // make sure click on panel advances
            // (User requested clicking anywhere on panel)
        }

        // Keep extraUiPanel active until user clicks (or until a safety timeout)
        // Wait until OnExtraPanelClicked calls TriggerGameOver (it sets gameOverTriggered)
        float safetyTimeout = 30f; // fallback in case user doesn't click
        float timer = 0f;
        while (!gameOverTriggered && timer < safetyTimeout)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!gameOverTriggered)
        {
            // fallback: trigger game over
            TriggerGameOver(true, timeLimitSeconds - timeRemaining, ZodiacSign.Aries);
        }
    }

    /// <summary>
    /// The fail UI sequence coroutine (Time Up): show falling "Time's up!" message then go to GameOver.
    /// </summary>
    private IEnumerator HandleFailSequenceCoroutine()
    {
        AudioManager.Instance.StopBGM();

        if (gameOverTriggered) yield break;

        if (extraUiPanel != null && extraMessage != null)
        {
            extraUiPanel.SetActive(true);
            extraMessage.text = "Time's up!";
            RectTransform rt = extraMessage.rectTransform;
            Vector3 origPos = rt.anchoredPosition;
            rt.anchoredPosition = origPos + Vector3.up * 400f;
            Canvas.ForceUpdateCanvases();

            // fall in

            AudioManager.Instance.PlaySFX(3);
            rt.DOAnchorPos(origPos, successFallDuration).SetEase(Ease.OutExpo);
            extraMessage.DOFade(1f, successFallDuration * 0.7f).SetEase(Ease.OutCubic);

            // stay visible for a moment
            yield return new WaitForSeconds(1.2f);

            // fade out
            yield return StartCoroutine(FadeOutTMP(extraMessage, 0.35f));
        }

        // then trigger game over (fail)
        TriggerGameOver(false, timeLimitSeconds, ZodiacSign.Aries);
    }

    /// <summary>
    /// Call this from your UI (e.g., add a Button on extraUiPanel that calls it) to continue from final screen.
    /// Because you asked "click anywhere on extraUiPanel", wire the panel's Button or EventTrigger to call this.
    /// </summary>
    public void OnExtraPanelClicked()
    {
        if (gameOverTriggered) return;

        Debug.Log("Extra Panel Clicked");

        // stop breathing effect for finalContinueText
        breathingScaleTween?.Kill();
        breathingScaleTween = null;

        // mark as handled and show final GameOver (success)
        //gameOverTriggered = true;

        TriggerGameOver(true, timeLimitSeconds - timeRemaining, ZodiacSign.Aries);
    }

    // --- helper fade functions for TMP and Image ---
    private IEnumerator FadeOutTMP(TextMeshProUGUI t, float duration)
    {
        if (t == null) yield break;
        t.DOKill();
        Color c = t.color;
        float start = c.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float tval = Mathf.Clamp01(elapsed / duration);
            c.a = Mathf.Lerp(start, 0f, tval);
            t.color = c;
            yield return null;
        }
        c.a = 0f;
        t.color = c;
    }

    private void SetupMessagePopState()
    {
        if (extraMessage == null) return;
        // prepare small scale and zero alpha
        extraMessage.rectTransform.localScale = Vector3.one * 0.6f;
        Color c = extraMessage.color; c.a = 0f; extraMessage.color = c;
        // ensure finalContinueText and specialStar are hidden
        if (finalContinueText != null) finalContinueText.gameObject.SetActive(false);
        if (specialStar != null) specialStar.SetActive(false);
    }

    private void PlayMessagePopIn(TextMeshProUGUI t, float popDur, float fadeDur)
    {
        if (t == null) return;
        t.DOKill();
        RectTransform rt = t.rectTransform;
        rt.localScale = Vector3.one * 0.6f;
        t.color = new Color(t.color.r, t.color.g, t.color.b, 0f);
        rt.DOScale(1f, popDur).SetEase(Ease.OutBack);
        t.DOFade(1f, fadeDur).SetEase(Ease.OutCubic);
    }

    private IEnumerator FadeOutImage(Image img, float dur)
    {
        if (img == null) yield break;
        img.DOKill();
        float start = img.color.a;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            Color c = img.color; c.a = Mathf.Lerp(start, 0f, t); img.color = c;
            yield return null;
        }
        Color cc = img.color; cc.a = 0f; img.color = cc;
        img.gameObject.SetActive(false);
    }

    
    // --- existing GameOver/solve logic preserved ---
    private void HandleSolvedResultImmediate()
    {
        // This method kept for backward compatibility but no longer used as main path
        if (gameOverTriggered) return;
        if (timerRunning) StopTimerIfRunning();

        float elapsed = 0f;
        if (timerStartRealtime > 0f) elapsed = Time.realtimeSinceStartup - timerStartRealtime;

        string resultMsg = "done";
        if (elapsed > 0f)
        {
            if (elapsed <= 30f) resultMsg = "done — excellent";
            else if (elapsed <= 60f) resultMsg = "done — good";
            else resultMsg = "done";
        }

        TriggerGameOver(true, timeLimitSeconds - timeRemaining, ZodiacSign.Aries);
    }

    private void HandleSolvedResult()
    {
        // kept for compatibility if called directly elsewhere
        StartCoroutine(HandleSolvedSequenceCoroutine());
    }

    /// <summary>
    /// Centralized GameOver trigger — sets guard flag and calls the GameOver UI.
    /// This prevents multiple ShowGameOver calls from different parts of the script.
    /// </summary>
    /// <param name="completed">whether the player completed the puzzle</param>
    /// <param name="timeTakenSeconds">time taken (seconds)</param>
    /// <param name="zodiac">zodiac sign to display</param>
    private void TriggerGameOver(bool completed, float timeTakenSeconds, ZodiacSign zodiac)
    {
        if (gameOverTriggered) return; // already handled

        Debug.Log("Reached Here");

        gameOverTriggered = true;

        // Ensure timer is stopped and coroutines killed (so time-based calls stop)
        StopTimerIfRunning();

        // Call GameOver UI safely
        var go = FindObjectOfType<GameOver>();
        if (go != null)
        {
            go.ShowGameOver(completed, timeTakenSeconds, zodiac);
        }
        else
        {
            Debug.LogWarning("PuzzleManager: GameOver manager not found in scene.");
        }
    }

    private bool ISRightParent(PuzzlePiece puzzle)
    {
        PuzzlePieceHolder parent = puzzle.transform.parent.GetComponent<PuzzlePieceHolder>();
        if (parent != null)
        {
            if (parent.id == puzzle.id)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    public void CheckForIndividualPiece(PuzzlePiece puzzle)
    {
        if (ISRightParent(puzzle))
        {
            Debug.Log("Right Postion");
            LayerFadeOut(puzzle.transform.GetChild(0).GetComponent<Image>());
        }
        else
        {
            Debug.Log("Wrong Postion");
            LayerFadeIn(puzzle.transform.GetChild(0).GetComponent<Image>(), 0.1f);
        }
    }

    private void LayerFadeIn(Image layout, float fadeVal)
    {
        if (layout == null) return;

        layout.DOKill();                 // stop old tweens
        layout.gameObject.SetActive(true);

        // Start from 0 alpha
        Color c = layout.color;
        c.a = 0f;
        layout.color = c;

        // Fade smoothly to fadeVal
        layout.DOFade(fadeVal, 0.5f)
              .SetEase(Ease.OutCubic);
    }


    private void LayerFadeOut(Image layout)
    {
        if (layout == null) return;

        layout.DOKill();

        // Always ensure starting alpha is what the user currently sees
        // No forced alpha reset here

        layout.gameObject.SetActive(true);

        layout.DOFade(0f, 0.5f)
              .SetEase(Ease.OutCubic)
              .OnComplete(() =>
              {
                  layout.gameObject.SetActive(false);
              });
    }
}
