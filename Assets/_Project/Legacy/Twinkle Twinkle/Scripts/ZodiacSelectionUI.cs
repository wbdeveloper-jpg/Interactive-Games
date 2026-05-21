using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ZodiacSelectionUI : MonoBehaviour
{
    [Header("Data")]
    public ZodiacDatabase zodiacDatabase;

    [Header("Input UI")]
    public TMP_Dropdown monthDropdown;
    public TMP_InputField dayInput;
    public Button startButton;

    [Header("Preview UI")]
    public TextMeshProUGUI zodiacNameText;
    public Image zodiacPreviewImage;
    public Color validDateColor = Color.white;
    public Color invalidDateColor = Color.red;

    [Header("Optional Preview Animation")]
    public bool animatePreview = true;
    public float previewAnimationDuration = 0.8f;
    public float previewCycleInterval = 0.05f;

    [Header("Invalid Popup")]
    public GameObject invalidPopupPrefab;
    public Transform invalidPopupParent;
    public float invalidPopupDuration = 1.5f;
    public float invalidPopupFloatDistance = 40f;

    [Header("Audio")]
    public bool playAudio = true;
    [Tooltip("Played when an invalid birthday popup is shown. Original project used SFX 4.")]
    public int invalidDateSfxId = 4;
    [Tooltip("Optional start button sound. Set to -1 to disable.")]
    public int startButtonSfxId = -1;

    public event Action<ZodiacPuzzleData> StartRequested;

    private ZodiacPuzzleData selectedData;
    private Coroutine previewCoroutine;
    private string lastInvalidTextShown;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartClicked);
        }

        if (monthDropdown != null)
        {
            monthDropdown.onValueChanged.AddListener(HandleMonthChanged);
        }

        if (dayInput != null)
        {
            dayInput.onValueChanged.AddListener(HandleDayChanged);
        }
    }

    private void OnEnable()
    {
        ValidateAndPreview();
    }

    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(HandleStartClicked);
        if (monthDropdown != null) monthDropdown.onValueChanged.RemoveListener(HandleMonthChanged);
        if (dayInput != null) dayInput.onValueChanged.RemoveListener(HandleDayChanged);
    }


    private void HandleMonthChanged(int value)
    {
        ValidateAndPreview();
    }

    private void HandleDayChanged(string value)
    {
        ValidateAndPreview();
    }

    private void ValidateAndPreview()
    {
        selectedData = null;

        if (startButton != null) startButton.interactable = false;

        if (monthDropdown == null || dayInput == null || zodiacDatabase == null)
        {
            Debug.LogError("ZodiacSelectionUI: Assign monthDropdown, dayInput, and zodiacDatabase.");
            return;
        }

        if (!int.TryParse(dayInput.text, out int day))
        {
            SetInvalidVisualState(showPopup: !string.IsNullOrWhiteSpace(dayInput.text), message: "Invalid date");
            return;
        }

        int month = monthDropdown.value + 1;
        if (!ZodiacBirthdayCalculator.IsValidMonthDay(month, day))
        {
            SetInvalidVisualState(showPopup: true, message: "This can't be your birthday!");
            return;
        }

        ZodiacSign sign = ZodiacBirthdayCalculator.GetSign(month, day);
        ZodiacPuzzleData data = zodiacDatabase.GetData(sign);

        if (data == null)
        {
            SetInvalidVisualState(showPopup: true, message: sign.GetDisplayName() + " data is missing");
            Debug.LogError("ZodiacSelectionUI: Missing ZodiacPuzzleData for " + sign);
            return;
        }

        selectedData = data;
        SetValidVisualState();
        Preview(data);

        if (startButton != null) startButton.interactable = true;
    }

    private void SetInvalidVisualState(bool showPopup, string message)
    {
        selectedData = null;

        if (dayInput != null && dayInput.textComponent != null)
        {
            dayInput.textComponent.color = invalidDateColor;
        }

        if (startButton != null) startButton.interactable = false;

        if (showPopup && !string.Equals(lastInvalidTextShown, dayInput.text, StringComparison.Ordinal))
        {
            ShowInvalidPopup(message);
            PlaySfx(invalidDateSfxId);
            lastInvalidTextShown = dayInput.text;
        }
    }

    private void SetValidVisualState()
    {
        lastInvalidTextShown = string.Empty;

        if (dayInput != null && dayInput.textComponent != null)
        {
            dayInput.textComponent.color = validDateColor;
        }
    }

    private void Preview(ZodiacPuzzleData data)
    {
        if (data == null) return;

        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }

        if (animatePreview)
        {
            previewCoroutine = StartCoroutine(PreviewScrollCoroutine(data));
        }
        else
        {
            ApplyPreview(data);
        }
    }

    private IEnumerator PreviewScrollCoroutine(ZodiacPuzzleData targetData)
    {
        if (zodiacDatabase == null || zodiacDatabase.allZodiacs == null || zodiacDatabase.allZodiacs.Length == 0)
        {
            ApplyPreview(targetData);
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, previewAnimationDuration);
        float interval = Mathf.Max(0.01f, previewCycleInterval);
        int index = UnityEngine.Random.Range(0, zodiacDatabase.allZodiacs.Length);

        while (elapsed < duration)
        {
            ZodiacPuzzleData data = zodiacDatabase.allZodiacs[index % zodiacDatabase.allZodiacs.Length];
            if (data != null)
            {
                ApplyPreview(data);
            }

            yield return new WaitForSeconds(interval);
            elapsed += interval;
            index++;
            float t = Mathf.Clamp01(elapsed / duration);
            interval = previewCycleInterval * Mathf.Lerp(1f, 6f, t * t);
        }

        ApplyPreview(targetData);
        previewCoroutine = null;
    }

    private void ApplyPreview(ZodiacPuzzleData data)
    {
        if (zodiacNameText != null)
        {
            zodiacNameText.text = data.DisplayName;
        }

        if (zodiacPreviewImage != null)
        {
            Sprite previewSprite = data.resultSprite != null ? data.resultSprite : data.fullPuzzleSprite;
            zodiacPreviewImage.sprite = previewSprite;
            zodiacPreviewImage.enabled = previewSprite != null;
        }
    }

    private void HandleStartClicked()
    {
        if (selectedData == null) return;
        PlaySfx(startButtonSfxId);
        StartRequested?.Invoke(selectedData);
    }

    private void PlaySfx(int sfxId)
    {
        if (!playAudio || sfxId < 0) return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfxId);
        }
    }

    private void ShowInvalidPopup(string message)
    {
        if (invalidPopupPrefab == null || dayInput == null) return;

        Transform parent = invalidPopupParent != null ? invalidPopupParent : dayInput.transform.parent;
        GameObject popup = Instantiate(invalidPopupPrefab, parent, false);
        RectTransform popupRt = popup.GetComponent<RectTransform>();
        RectTransform inputRt = dayInput.GetComponent<RectTransform>();

        if (popupRt != null && inputRt != null)
        {
            popupRt.anchoredPosition = inputRt.anchoredPosition + new Vector2(0f, inputRt.rect.height * 0.5f + 10f);
        }

        TextMeshProUGUI tmp = popup.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = message;

        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg == null) cg = popup.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        if (popupRt != null)
        {
            popupRt.DOAnchorPosY(popupRt.anchoredPosition.y + invalidPopupFloatDistance, invalidPopupDuration).SetEase(Ease.OutCubic);
        }

        cg.DOFade(0f, invalidPopupDuration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            if (popup != null) Destroy(popup);
        });
    }
}
