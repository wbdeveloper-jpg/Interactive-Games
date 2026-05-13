using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ConstellationRevealController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Constellation")]
    [SerializeField] private Transform constellationParent;

    [Header("Win Message")]
    [FormerlySerializedAs("titleText")]
    [SerializeField] private TextMeshProUGUI winMessageText;

    [SerializeField] private string successMessage = "You Won!";
    [SerializeField] private float messageStartYOffset = 420f;
    [SerializeField] private float messageFallDuration = 0.45f;
    [SerializeField] private float messageHoldBeforeConstellation = 0.35f;
    [SerializeField] private float messageFadeOutDuration = 0.25f;
    [SerializeField] private Ease messageFallEase = Ease.OutBack;

    [Header("Continue")]
    [SerializeField] private GameObject continuePanel;
    [SerializeField] private TextMeshProUGUI continueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private float delayBeforeContinue = 0.25f;
    [SerializeField] private float continueFadeDuration = 0.25f;

    private GameObject spawnedConstellation;
    private ConstellationAnimator activeAnimator;

    private Action onContinueCallback;
    private Sequence revealSequence;
    private Coroutine enableContinueCoroutine;

    private bool canContinue;

    private Vector2 cachedMessageFinalPosition;
    private bool hasCachedMessagePosition;

    private void Awake()
    {
        CacheMessageFinalPosition();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinuePressed);
            continueButton.onClick.AddListener(OnContinuePressed);
        }

        Clear();
    }

    private void OnDestroy()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinuePressed);

        CleanupRuntimeState();
    }

    /// <summary>
    /// Main public method expected by GameFlowController.
    /// Flow:
    /// 1. Show win message falling from top.
    /// 2. Hold briefly.
    /// 3. Fade out win message completely.
    /// 4. Spawn and play constellation animation.
    /// 5. Enable continue after constellation finishes.
    /// </summary>
    public void Play(ZodiacPuzzleData zodiacData, Action onContinue)
    {
        if (zodiacData == null)
        {
            Debug.LogError("[ConstellationRevealController] Cannot play reveal. ZodiacPuzzleData is missing.");
            return;
        }

        CleanupRuntimeState();

        onContinueCallback = onContinue;
        canContinue = false;

        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        SetupContinueHidden();
        SetupWinMessage();

        revealSequence = DOTween.Sequence();
        revealSequence.SetUpdate(true);

        AppendWinMessageFallHoldAndFade();

        // Important:
        // The constellation is spawned ONLY after the message has fully faded out.
        // This prevents the completed constellation from flashing for one frame.
        revealSequence.AppendCallback(() =>
        {
            if (winMessageText != null)
            {
                winMessageText.alpha = 0f;
                winMessageText.gameObject.SetActive(false);
            }

            SpawnAndPlayConstellation(zodiacData);
        });
    }

    /// <summary>
    /// Optional overload. Safe if any script calls Play without a callback.
    /// </summary>
    public void Play(ZodiacPuzzleData zodiacData)
    {
        Play(zodiacData, null);
    }

    /// <summary>
    /// Main clear method expected by GameFlowController.
    /// </summary>
    public void Clear()
    {
        CleanupRuntimeState();

        canContinue = false;
        onContinueCallback = null;

        SetupContinueHidden();
        ResetWinMessageHidden();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// Button event for continue button / full-screen transparent button.
    /// </summary>
    public void OnContinuePressed()
    {
        if (!canContinue)
            return;

        canContinue = false;

        if (continueButton != null)
            continueButton.interactable = false;

        onContinueCallback?.Invoke();
    }

    // Compatibility aliases. GameFlowController should normally call Play and Clear.
    public void PlayReveal(ZodiacPuzzleData zodiacData, Action onContinue)
    {
        Play(zodiacData, onContinue);
    }

    public void ShowReveal(ZodiacPuzzleData zodiacData, Action onContinue)
    {
        Play(zodiacData, onContinue);
    }

    public void Show(ZodiacPuzzleData zodiacData, Action onContinue)
    {
        Play(zodiacData, onContinue);
    }

    public void HideImmediate()
    {
        Clear();
    }

    private void CacheMessageFinalPosition()
    {
        if (hasCachedMessagePosition || winMessageText == null)
            return;

        cachedMessageFinalPosition = winMessageText.rectTransform.anchoredPosition;
        hasCachedMessagePosition = true;
    }

    private void SetupWinMessage()
    {
        if (winMessageText == null)
            return;

        CacheMessageFinalPosition();

        winMessageText.gameObject.SetActive(true);
        winMessageText.text = string.IsNullOrWhiteSpace(successMessage) ? "You Won!" : successMessage;
        winMessageText.alpha = 0f;

        RectTransform messageRect = winMessageText.rectTransform;
        messageRect.localScale = Vector3.one;

        Vector2 finalPosition = hasCachedMessagePosition
            ? cachedMessageFinalPosition
            : messageRect.anchoredPosition;

        messageRect.anchoredPosition = finalPosition + Vector2.up * messageStartYOffset;
    }

    private void AppendWinMessageFallHoldAndFade()
    {
        if (winMessageText == null)
            return;

        RectTransform messageRect = winMessageText.rectTransform;

        Vector2 finalPosition = hasCachedMessagePosition
            ? cachedMessageFinalPosition
            : messageRect.anchoredPosition;

        // Fall from top and fade in.
        revealSequence.Append(
            messageRect.DOAnchorPos(finalPosition, messageFallDuration)
                .SetEase(messageFallEase)
                .SetUpdate(true)
        );

        revealSequence.Join(
            winMessageText.DOFade(1f, messageFallDuration * 0.75f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
        );

        // Force exact final state.
        revealSequence.AppendCallback(() =>
        {
            if (winMessageText == null)
                return;

            winMessageText.alpha = 1f;
            winMessageText.rectTransform.anchoredPosition = finalPosition;
        });

        // Hold message for impact.
        if (messageHoldBeforeConstellation > 0f)
            revealSequence.AppendInterval(messageHoldBeforeConstellation);

        // Fade out completely before constellation appears.
        revealSequence.Append(
            winMessageText.DOFade(0f, messageFadeOutDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
        );

        // Hard guarantee: message is hidden before constellation spawn callback runs.
        revealSequence.AppendCallback(() =>
        {
            if (winMessageText == null)
                return;

            winMessageText.alpha = 0f;
            winMessageText.gameObject.SetActive(false);
        });
    }

    private void SetupContinueHidden()
    {
        if (continuePanel != null)
            continuePanel.SetActive(false);

        if (continueText != null)
        {
            continueText.DOKill();
            continueText.rectTransform.DOKill();

            continueText.gameObject.SetActive(false);
            continueText.alpha = 0f;
            continueText.rectTransform.localScale = Vector3.one;
        }

        if (continueButton != null)
            continueButton.interactable = false;
    }

    private void SpawnAndPlayConstellation(ZodiacPuzzleData zodiacData)
    {
        ClearOldConstellation();

        if (zodiacData.constellationPrefab == null)
        {
            Debug.LogWarning("[ConstellationRevealController] No constellation prefab assigned for " + zodiacData.sign + ".");
            EnableContinue();
            return;
        }

        Transform parent = constellationParent != null ? constellationParent : transform;

        spawnedConstellation = Instantiate(zodiacData.constellationPrefab, parent, false);

        // Hide instantly in the same frame it is created.
        // This prevents any completed/default visual state from being rendered.
        spawnedConstellation.SetActive(false);

        activeAnimator = spawnedConstellation.GetComponent<ConstellationAnimator>();

        if (activeAnimator == null)
            activeAnimator = spawnedConstellation.GetComponentInChildren<ConstellationAnimator>(true);

        if (activeAnimator == null)
        {
            Debug.LogWarning("[ConstellationRevealController] Spawned constellation has no ConstellationAnimator.");
            spawnedConstellation.SetActive(true);
            EnableContinue();
            return;
        }

        activeAnimator.onComplete.RemoveListener(EnableContinue);
        activeAnimator.onComplete.AddListener(EnableContinue);

        // Stop any possible play-on-enable / old sequence before starting cleanly.
        activeAnimator.Stop();

        spawnedConstellation.SetActive(true);

        // If the animator has an OnEnable auto-play, this immediately kills it
        // and starts the controlled reveal animation.
        activeAnimator.Stop();
        activeAnimator.Play();
    }

    private void EnableContinue()
    {
        if (enableContinueCoroutine != null)
            StopCoroutine(enableContinueCoroutine);

        enableContinueCoroutine = StartCoroutine(EnableContinueRoutine());
    }

    private IEnumerator EnableContinueRoutine()
    {
        yield return new WaitForSecondsRealtime(delayBeforeContinue);

        if (continuePanel != null)
            continuePanel.SetActive(true);

        if (continueText != null)
        {
            continueText.DOKill();
            continueText.rectTransform.DOKill();

            continueText.gameObject.SetActive(true);
            continueText.alpha = 0f;
            continueText.rectTransform.localScale = Vector3.one;

            continueText.DOFade(1f, continueFadeDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        if (continueButton != null)
            continueButton.interactable = true;

        canContinue = true;
        enableContinueCoroutine = null;
    }

    private void ResetWinMessageHidden()
    {
        if (winMessageText == null)
            return;

        CacheMessageFinalPosition();

        winMessageText.DOKill();
        winMessageText.rectTransform.DOKill();

        winMessageText.text = string.IsNullOrWhiteSpace(successMessage) ? "You Won!" : successMessage;
        winMessageText.alpha = 0f;
        winMessageText.rectTransform.localScale = Vector3.one;

        if (hasCachedMessagePosition)
            winMessageText.rectTransform.anchoredPosition = cachedMessageFinalPosition;

        winMessageText.gameObject.SetActive(false);
    }

    private void ClearOldConstellation()
    {
        if (activeAnimator != null)
        {
            activeAnimator.onComplete.RemoveListener(EnableContinue);
            activeAnimator.Stop();
            activeAnimator = null;
        }

        if (spawnedConstellation != null)
        {
            Destroy(spawnedConstellation);
            spawnedConstellation = null;
        }
    }

    private void CleanupRuntimeState()
    {
        if (enableContinueCoroutine != null)
        {
            StopCoroutine(enableContinueCoroutine);
            enableContinueCoroutine = null;
        }

        if (revealSequence != null)
        {
            revealSequence.Kill();
            revealSequence = null;
        }

        if (winMessageText != null)
        {
            winMessageText.DOKill();
            winMessageText.rectTransform.DOKill();
        }

        if (continueText != null)
        {
            continueText.DOKill();
            continueText.rectTransform.DOKill();
        }

        ClearOldConstellation();
    }
}