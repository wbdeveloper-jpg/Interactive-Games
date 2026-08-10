using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum OddClawImagePreviewCloseMode
{
    ManualClose,
    AutoCloseAfterSeconds
}

public class OddClawImageAnswerPreviewTarget : MonoBehaviour, IPointerClickHandler
{
    private OddClawImageModeFeatureController _owner;
    private Sprite _sprite;

    public void Initialize(OddClawImageModeFeatureController owner, Sprite sprite)
    {
        _owner = owner;
        _sprite = sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_owner == null || _sprite == null)
            return;

        eventData.Use();
        _owner.TryOpenPreview(_sprite);
    }
}

public class OddClawImageModeFeatureController : MonoBehaviour
{
    [Header("Core References")]
    public OddClawCatchManager gameManager;
    public OddClawController clawController;
    public Canvas rootCanvas;

    [Header("Image-Only Behaviour")]
    public bool enableImagePreview = true;
    public bool useMagnetForImageQuestions = true;

    [Header("Enlarged Image Preview")]
    public GameObject previewRoot;
    public CanvasGroup previewCanvasGroup;
    public RectTransform previewCard;
    public Image enlargedImage;
    public Button previewCloseButton;
    public OddClawImagePreviewCloseMode closeMode = OddClawImagePreviewCloseMode.ManualClose;
    [Min(1f)] public float autoCloseSeconds = 15f;
    [Min(0.01f)] public float previewOpenDuration = 0.2f;
    [Min(0.01f)] public float previewCloseDuration = 0.16f;
    public Vector3 previewHiddenScale = new Vector3(0.9f, 0.9f, 1f);

    [Header("First Image Preview Hint")]
    public bool enableFirstImageHint = true;
    [Tooltip("The hint is saved only after an enlarged image has actually opened.")]
    public string imageHintSaveKeyPrefix = "OddClawCatch_ImagePreviewLearned";
    [Min(0f)] public float imageHintDelay = 3f;
    [TextArea(2, 4)] public string imageHintMessage = "Tap any picture to see it bigger.";
    public CanvasGroup imageHintCanvasGroup;
    public TMP_Text imageHintText;
    public RectTransform imageHintPointer;
    public Image imageHintPointerImage;
    public Vector2 imageHintPointerOffset = new Vector2(75f, 70f);
    [Min(0.01f)] public float imageHintFadeDuration = 0.2f;
    [Min(1f)] public float imagePulseScale = 1.045f;
    [Min(0.1f)] public float imagePulseDuration = 0.7f;
    [Tooltip("Testing only. Shows the contextual hint even after its scene-specific save exists.")]
    public bool forceImageHintForTesting = false;

    private readonly List<Image> _waveImages = new List<Image>();
    private readonly Dictionary<Transform, Vector3> _waveImageBaseScales =
        new Dictionary<Transform, Vector3>();
    private readonly List<Tween> _imagePulseTweens = new List<Tween>();

    private int _waveToken;
    private bool _currentWaveUsesImages;
    private bool _previewOpen;
    private bool _ownsGameplayHold;
    private bool _warnedMissingPreviewReferences;
    private Coroutine _hintRoutine;
    private Coroutine _autoCloseRoutine;
    private Coroutine _releaseRoutine;

    private void Awake()
    {
        ResolveMissingCoreReferences();
        HookCloseButton();
        HidePreviewImmediate();
        HideHintImmediate();
    }

    private void OnDestroy()
    {
        if (previewCloseButton != null)
            previewCloseButton.onClick.RemoveListener(ClosePreview);

        StopOwnedCoroutines();
        StopImagePulses();
        if (clawController != null)
            clawController.SetImageMagnetMode(false);
    }

    public void ConfigureForWave(
        OddClawQuestionData question,
        List<OddClawItemView> spawnedItems)
    {
        EndWave(true);
        ResolveMissingCoreReferences();
        _waveToken++;

        _currentWaveUsesImages =
            question != null
            && IsImageDisplayMode(question.displayMode)
            && HasAtLeastOneSprite(question);

        if (clawController != null)
            clawController.SetImageMagnetMode(
                _currentWaveUsesImages && useMagnetForImageQuestions);

        if (!_currentWaveUsesImages || spawnedItems == null || !enableImagePreview)
            return;

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            OddClawItemView item = spawnedItems[i];
            if (item == null || item.answerImage == null || item.answerImage.sprite == null)
                continue;

            OddClawImageAnswerPreviewTarget target =
                item.GetComponent<OddClawImageAnswerPreviewTarget>();
            if (target == null)
                target = item.gameObject.AddComponent<OddClawImageAnswerPreviewTarget>();
            target.Initialize(this, item.answerImage.sprite);

            item.answerImage.raycastTarget = true;
            if (item.backgroundImage != null)
                item.backgroundImage.raycastTarget = true;
            if (item.canvasGroup != null)
            {
                item.canvasGroup.blocksRaycasts = true;
                item.canvasGroup.interactable = true;
            }

            _waveImages.Add(item.answerImage);
            _waveImageBaseScales[item.answerImage.transform] = item.answerImage.transform.localScale;
        }

        if (enableImagePreview && ShouldTeachImagePreview())
            _hintRoutine = StartCoroutine(ShowHintAfterDelay(_waveToken));
    }

    public void EndWave()
    {
        EndWave(true);
    }

    public void TryOpenPreview(Sprite sprite)
    {
        if (!enableImagePreview
            || !_currentWaveUsesImages
            || sprite == null
            || _previewOpen)
            return;

        if (!HasPreviewReferences())
        {
            if (!_warnedMissingPreviewReferences)
            {
                _warnedMissingPreviewReferences = true;
                Debug.LogWarning(
                    "Odd Claw image preview is enabled, but its Preview Root, Canvas Group, " +
                    "Card or Enlarged Image reference is missing. Run the additive image-mode installer.",
                    this);
            }
            return;
        }

        if (!AcquireGameplayHold())
            return;

        StopHintRoutine();
        HideHintImmediate();
        MarkImagePreviewLearned();

        enlargedImage.sprite = sprite;
        enlargedImage.preserveAspect = true;
        enlargedImage.gameObject.SetActive(true);

        _previewOpen = true;
        previewRoot.SetActive(true);
        previewCanvasGroup.DOKill();
        previewCard.DOKill();
        previewCanvasGroup.alpha = 0f;
        previewCanvasGroup.blocksRaycasts = true;
        previewCanvasGroup.interactable = true;
        previewCard.localScale = previewHiddenScale;

        previewCanvasGroup
            .DOFade(1f, Mathf.Max(0.01f, previewOpenDuration))
            .SetUpdate(true);
        previewCard
            .DOScale(Vector3.one, Mathf.Max(0.01f, previewOpenDuration))
            .SetEase(Ease.OutBack)
            .SetUpdate(true);

        if (closeMode == OddClawImagePreviewCloseMode.AutoCloseAfterSeconds)
            _autoCloseRoutine = StartCoroutine(AutoClosePreview());
    }

    public void ClosePreview()
    {
        if (!_previewOpen)
            return;

        StopAutoCloseRoutine();
        _previewOpen = false;
        previewCanvasGroup.blocksRaycasts = false;
        previewCanvasGroup.interactable = false;
        previewCanvasGroup.DOKill();
        previewCard.DOKill();

        float duration = Mathf.Max(0.01f, previewCloseDuration);
        previewCard
            .DOScale(previewHiddenScale, duration)
            .SetEase(Ease.InCubic)
            .SetUpdate(true);
        previewCanvasGroup
            .DOFade(0f, duration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (previewRoot != null)
                    previewRoot.SetActive(false);
                if (enlargedImage != null)
                    enlargedImage.sprite = null;

                if (_releaseRoutine != null)
                    StopCoroutine(_releaseRoutine);
                _releaseRoutine = StartCoroutine(ReleaseGameplayHoldNextFrame());
            });
    }

    [ContextMenu("Reset Image Preview Hint For This Scene")]
    public void ResetImagePreviewHintForThisScene()
    {
        PlayerPrefs.DeleteKey(GetImageHintSaveKey());
        PlayerPrefs.Save();
    }

    public bool HasLearnedImagePreview()
    {
        return PlayerPrefs.GetInt(GetImageHintSaveKey(), 0) == 1;
    }

    private IEnumerator ShowHintAfterDelay(int token)
    {
        float elapsed = 0f;
        while (elapsed < imageHintDelay)
        {
            if (token != _waveToken || !_currentWaveUsesImages)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        while (token == _waveToken && _currentWaveUsesImages && !_previewOpen)
        {
            if (AcquireGameplayHold())
            {
                ShowImageHint();
                yield break;
            }

            yield return null;
        }
    }

    private void ShowImageHint()
    {
        if (imageHintCanvasGroup == null)
        {
            ReleaseGameplayHold();
            return;
        }

        if (imageHintText != null)
            imageHintText.text = imageHintMessage;

        imageHintCanvasGroup.gameObject.SetActive(true);
        imageHintCanvasGroup.DOKill();
        imageHintCanvasGroup.alpha = 0f;
        imageHintCanvasGroup.blocksRaycasts = false;
        imageHintCanvasGroup.interactable = false;
        imageHintCanvasGroup
            .DOFade(1f, Mathf.Max(0.01f, imageHintFadeDuration))
            .SetUpdate(true);

        PositionHintPointer();
        StartImagePulses();
    }

    private void PositionHintPointer()
    {
        if (imageHintPointer == null || _waveImages.Count == 0 || _waveImages[0] == null)
            return;

        bool hasVisibleSprite = imageHintPointerImage != null
            && imageHintPointerImage.sprite != null;
        imageHintPointer.gameObject.SetActive(hasVisibleSprite);
        if (!hasVisibleSprite || rootCanvas == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        RectTransform targetRect = _waveImages[0].rectTransform;
        if (canvasRect == null || targetRect == null)
            return;

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, targetRect.position);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                targetScreen,
                uiCamera,
                out Vector2 canvasLocal))
            return;

        Vector2 desired = canvasLocal + imageHintPointerOffset;
        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 halfPointer = imageHintPointer.rect.size * 0.5f;
        desired.x = Mathf.Clamp(
            desired.x,
            -halfCanvas.x + halfPointer.x,
            halfCanvas.x - halfPointer.x);
        desired.y = Mathf.Clamp(
            desired.y,
            -halfCanvas.y + halfPointer.y,
            halfCanvas.y - halfPointer.y);
        imageHintPointer.anchoredPosition = desired;
    }

    private void StartImagePulses()
    {
        StopImagePulses();
        for (int i = 0; i < _waveImages.Count; i++)
        {
            Image image = _waveImages[i];
            if (image == null)
                continue;

            Transform target = image.transform;
            Vector3 baseScale = _waveImageBaseScales.TryGetValue(target, out Vector3 stored)
                ? stored
                : target.localScale;
            target.localScale = baseScale;
            Tween pulse = target
                .DOScale(baseScale * Mathf.Max(1f, imagePulseScale), Mathf.Max(0.1f, imagePulseDuration))
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
            _imagePulseTweens.Add(pulse);
        }
    }

    private void StopImagePulses()
    {
        for (int i = 0; i < _imagePulseTweens.Count; i++)
        {
            if (_imagePulseTweens[i] != null && _imagePulseTweens[i].IsActive())
                _imagePulseTweens[i].Kill();
        }
        _imagePulseTweens.Clear();

        foreach (KeyValuePair<Transform, Vector3> pair in _waveImageBaseScales)
        {
            if (pair.Key == null)
                continue;
            pair.Key.localScale = pair.Value;
        }
    }

    private void HideHintImmediate()
    {
        StopImagePulses();

        if (imageHintCanvasGroup != null)
        {
            imageHintCanvasGroup.DOKill();
            imageHintCanvasGroup.alpha = 0f;
            imageHintCanvasGroup.blocksRaycasts = false;
            imageHintCanvasGroup.interactable = false;
            imageHintCanvasGroup.gameObject.SetActive(false);
        }

        if (imageHintPointer != null)
            imageHintPointer.gameObject.SetActive(false);
    }

    private void HidePreviewImmediate()
    {
        _previewOpen = false;
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.DOKill();
            previewCanvasGroup.alpha = 0f;
            previewCanvasGroup.blocksRaycasts = false;
            previewCanvasGroup.interactable = false;
        }
        if (previewCard != null)
        {
            previewCard.DOKill();
            previewCard.localScale = Vector3.one;
        }
        if (enlargedImage != null)
            enlargedImage.sprite = null;
        if (previewRoot != null)
            previewRoot.SetActive(false);
    }

    private IEnumerator AutoClosePreview()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(1f, autoCloseSeconds);
        while (_previewOpen && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _autoCloseRoutine = null;
        if (_previewOpen)
            ClosePreview();
    }

    private IEnumerator ReleaseGameplayHoldNextFrame()
    {
        yield return null;
        _releaseRoutine = null;
        ReleaseGameplayHold();
    }

    private bool AcquireGameplayHold()
    {
        if (_ownsGameplayHold)
            return true;
        if (gameManager == null || !gameManager.BeginImageFeatureHold())
            return false;

        _ownsGameplayHold = true;
        return true;
    }

    private void ReleaseGameplayHold()
    {
        if (!_ownsGameplayHold)
            return;

        _ownsGameplayHold = false;
        if (gameManager != null)
            gameManager.EndImageFeatureHold();
    }

    private void EndWave(bool releaseHold)
    {
        _waveToken++;
        _currentWaveUsesImages = false;
        StopOwnedCoroutines();
        HideHintImmediate();
        HidePreviewImmediate();
        _waveImages.Clear();
        _waveImageBaseScales.Clear();

        if (clawController != null)
            clawController.SetImageMagnetMode(false);
        if (releaseHold)
            ReleaseGameplayHold();
    }

    private void StopOwnedCoroutines()
    {
        StopHintRoutine();
        StopAutoCloseRoutine();
        if (_releaseRoutine != null)
        {
            StopCoroutine(_releaseRoutine);
            _releaseRoutine = null;
        }
    }

    private void StopHintRoutine()
    {
        if (_hintRoutine == null)
            return;
        StopCoroutine(_hintRoutine);
        _hintRoutine = null;
    }

    private void StopAutoCloseRoutine()
    {
        if (_autoCloseRoutine == null)
            return;
        StopCoroutine(_autoCloseRoutine);
        _autoCloseRoutine = null;
    }

    private bool ShouldTeachImagePreview()
    {
        return forceImageHintForTesting || !HasLearnedImagePreview();
    }

    private void MarkImagePreviewLearned()
    {
        PlayerPrefs.SetInt(GetImageHintSaveKey(), 1);
        PlayerPrefs.Save();
    }

    private string GetImageHintSaveKey()
    {
        return imageHintSaveKeyPrefix + "_" + SceneManager.GetActiveScene().name;
    }

    private bool HasPreviewReferences()
    {
        return previewRoot != null
            && previewCanvasGroup != null
            && previewCard != null
            && enlargedImage != null;
    }

    private bool HasAtLeastOneSprite(OddClawQuestionData question)
    {
        if (question.answerOptions == null)
            return false;
        for (int i = 0; i < question.answerOptions.Count; i++)
        {
            if (question.answerOptions[i] != null && question.answerOptions[i].sprite != null)
                return true;
        }
        return false;
    }

    private static bool IsImageDisplayMode(OddClawAnswerDisplayMode displayMode)
    {
        return displayMode == OddClawAnswerDisplayMode.Sprite
            || displayMode == OddClawAnswerDisplayMode.SpriteWithOptionalText;
    }

    private void ResolveMissingCoreReferences()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<OddClawCatchManager>();
        if (clawController == null && gameManager != null)
            clawController = gameManager.clawController;
        if (rootCanvas == null && gameManager != null)
            rootCanvas = gameManager.rootCanvas;
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
    }

    private void HookCloseButton()
    {
        if (previewCloseButton == null)
            return;
        previewCloseButton.onClick.RemoveListener(ClosePreview);
        previewCloseButton.onClick.AddListener(ClosePreview);
    }
}
