using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // TextMeshPro

[RequireComponent(typeof(CanvasGroup))]
public class SelectYourZodiac : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown monthDropdown;      // 0 = Jan, 1 = Feb, ... 11 = Dec
    public TMP_InputField dateInput;        // numeric input for day
    public Button startButton;              // Start button to enable/disable
    public TextMeshProUGUI zodiacNameText;  // Big label to show zodiac name
    public Image zodiacImage;               // Image to change to zodiac animal sprite

    [Header("Zodiac Sprites")]
    [Tooltip("12 sprites in order: Capricorn, Aquarius, Pisces, Aries, Taurus, Gemini, Cancer, Leo, Virgo, Libra, Scorpio, Sagittarius")]
    public List<Sprite> zodiacSprites = new List<Sprite>(12);

    [Header("Validation Colors")]
    public Color validDateColor = Color.white;
    public Color invalidDateColor = Color.red;

    [Header("Optional Transition Animation")]
    public bool animatedTransition = true;
    [Tooltip("Total approximate duration of the scroll/slowdown animation in seconds")]
    public float animationDuration = 1.2f;
    [Tooltip("Initial cycle speed (time between swaps) in seconds")]
    public float initialCycleInterval = 0.06f;

    [Header("Invalid-date popup (assign prefab)")]
    [Tooltip("Small prefab to show when date is invalid. Prefab should contain a CanvasGroup or a TMP_Text/Graphic for fading.")]
    public GameObject invalidPopupPrefab;
    [Tooltip("Parent for popup. If empty, popup will be parented to the dateInput's parent.")]
    public Transform invalidPopupParent;
    [Tooltip("How long the popup stays & animates before being destroyed.")]
    public float invalidPopupDuration = 2.5f;
    [Tooltip("How many UI units up the popup floats while fading.")]
    public float invalidPopupFloatDistance = 40f;

    // internal
    private bool isValid = false;
    private Coroutine animCoroutine = null;

    void Start()
    {
        if (Screen.orientation != ScreenOrientation.LandscapeLeft)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        // safety checks
        if (monthDropdown == null || dateInput == null || startButton == null || zodiacNameText == null || zodiacImage == null)
        {
            Debug.LogError("[SelectYourZodiac] Please assign all UI references in the inspector.");
            enabled = false;
            return;
        }

        // Make sure Start is disabled initially
        startButton.interactable = false;

        // Add listeners
        monthDropdown.onValueChanged.AddListener(OnMonthChanged);
        dateInput.onValueChanged.AddListener(OnDateEdited);

        // Initialize UI state
        ValidateAndUpdate();
    }

    void OnDestroy()
    {
        // remove listeners to avoid leaks
        monthDropdown.onValueChanged.RemoveListener(OnMonthChanged);
        dateInput.onValueChanged.RemoveListener(OnDateEdited);
    }

    // Called when month dropdown changes
    private void OnMonthChanged(int idx)
    {
        ValidateAndUpdate();
    }

    // Called on each change in input field
    private void OnDateEdited(string s)
    {
        ValidateAndUpdate();
    }

    /// <summary>
    /// Validates the date+month input, updates the UI colors and Start button,
    /// and triggers zodiac update (with optional animation).
    /// Also shows an invalid-date popup when the date is invalid (but NOT when input is empty).
    /// </summary>
    private void ValidateAndUpdate()
    {
        int monthIndex = monthDropdown.value; // 0..11
        int month = monthIndex + 1; // 1..12

        // parse day
        int day;
        if (!int.TryParse(dateInput.text, out day))
        {
            // empty or invalid number -> invalid
            SetInvalidState();

            // Only show popup if user actually typed something (non-empty after trim)
            if (!string.IsNullOrWhiteSpace(dateInput.text))
            {
                ShowInvalidPopup("Invalid date");
            }
            return;
        }

        // Use year 2000 (leap year) so Feb 29 is accepted but Feb 30+ rejected.
        bool dateOk = false;
        try
        {
            int maxDay = DateTime.DaysInMonth(2000, month); // 29 for Feb
            dateOk = day >= 1 && day <= maxDay;
        }
        catch
        {
            dateOk = false;
        }

        if (!dateOk)
        {
            SetInvalidState();
            ShowInvalidPopup("This can’t be your birthday!");
            AudioManager.Instance.PlaySFX(4);
            return;
        }

        // If here, date is valid
        SetValidState();

        // Find zodiac index and name
        int zodiacIndex = GetZodiacIndex(month, day);
        string zodiacName = GetZodiacNameByIndex(zodiacIndex);

        // If animation is requested, start it; otherwise update immediately
        if (animatedTransition)
        {
            // If there's already an animation running, stop and start a fresh one
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(PlayZodiacScrollAnimation(zodiacIndex, zodiacName));
        }
        else
        {
            // immediate
            UpdateZodiacImmediate(zodiacIndex, zodiacName);
        }

        // Enable start button
        startButton.interactable = true;
    }

    private void SetInvalidState()
    {
        isValid = false;
        // mark date text red
        if (dateInput.textComponent != null)
            dateInput.textComponent.color = invalidDateColor;
        // disable Start
        startButton.interactable = false;
    }

    private void SetValidState()
    {
        isValid = true;
        // reset text color
        if (dateInput.textComponent != null)
            dateInput.textComponent.color = validDateColor;
    }

    /// <summary>
    /// Immediately update zodiac name and sprite.
    /// </summary>
    private void UpdateZodiacImmediate(int zodiacIndex, string zodiacName)
    {
        zodiacNameText.text = zodiacName;

        if (zodiacSprites != null && zodiacSprites.Count > zodiacIndex && zodiacSprites[zodiacIndex] != null)
            zodiacImage.sprite = zodiacSprites[zodiacIndex];
    }

    /// <summary>
    /// Simple scroll animation that cycles through zodiac sprites/names and slows down to the target.
    /// Non-complex, friendly to mobile / no external libs.
    /// </summary>
    private IEnumerator PlayZodiacScrollAnimation(int targetIndex, string targetName)
    {
        if (zodiacSprites == null || zodiacSprites.Count == 0)
        {
            UpdateZodiacImmediate(targetIndex, targetName);
            yield break;
        }

        float elapsed = 0f;
        float total = Mathf.Max(0.2f, animationDuration);
        float t = 0f;

        // start interval
        float interval = initialCycleInterval;
        int current = UnityEngine.Random.Range(0, zodiacSprites.Count); // start from random

        while (elapsed < total)
        {
            // show current
            if (current >= 0 && current < zodiacSprites.Count)
            {
                zodiacImage.sprite = zodiacSprites[current];
                zodiacNameText.text = GetZodiacNameByIndex(current);
            }

            // wait for interval
            yield return new WaitForSeconds(interval);

            // advance
            current = (current + 1) % zodiacSprites.Count;

            // gradually increase interval (slowdown)
            // map elapsed/total from 0->1 to a multiplicative factor
            t = elapsed / total;
            // Slowdown curve: start fast, end slow (ease-out)
            float slowdownFactor = 1f + Mathf.Pow(t, 1.5f) * 10f; // ramps up to make interval bigger
            interval = initialCycleInterval * slowdownFactor;

            // increase elapsed by a conservative amount (we also waited interval)
            elapsed += interval;
        }

        // final land on target
        zodiacImage.sprite = (zodiacSprites.Count > targetIndex && zodiacSprites[targetIndex] != null)
            ? zodiacSprites[targetIndex]
            : zodiacImage.sprite;

        zodiacNameText.text = targetName;

        animCoroutine = null;
        yield break;
    }

    /// <summary>
    /// Shows a small popup (instantiates invalidPopupPrefab) near the dateInput,
    /// plays a float-up + fade-out animation and destroys the popup.
    /// </summary>
    /// <param name="message">Optional message to set on the popup (if it has TMP text).</param>
    private void ShowInvalidPopup(string message = null)
    {
        if (invalidPopupPrefab == null)
        {
            // No prefab assigned - nothing to show (silent)
            return;
        }

        // Determine parent
        Transform parent = invalidPopupParent != null ? invalidPopupParent : dateInput.transform.parent;

        // Instantiate
        GameObject go = Instantiate(invalidPopupPrefab, parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();

        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 0f);   // bottom center


        // Try to set message if popup prefab has TMP_Text
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null && !string.IsNullOrEmpty(message))
        {
            tmp.text = message;
            tmp.enableAutoSizing = false;
            tmp.fontSize = 60f;
        }

        // Position: try to place above the date input field
        // If popup has RectTransform and dateInput is under same Canvas, we can compute anchored position
        RectTransform popupRt = go.GetComponent<RectTransform>();
        RectTransform inputRt = dateInput.GetComponent<RectTransform>();

        if (popupRt != null && inputRt != null)
        {
            // Set to same anchored position as input, then move up
            popupRt.anchoredPosition = inputRt.anchoredPosition + new Vector2(0f, inputRt.rect.height * 0.5f + 10f);
        }
        else
        {
            // fallback: place at local zero
            go.transform.localPosition = Vector3.zero;
        }

        // Animation: move up by invalidPopupFloatDistance and fade out over invalidPopupDuration
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            // move
            popupRt?.DOLocalMoveY(popupRt.localPosition.y + invalidPopupFloatDistance, invalidPopupDuration).SetEase(Ease.OutCubic);
            // fade
            cg.DOFade(0f, invalidPopupDuration).SetEase(Ease.InCubic).OnComplete(() =>
            {
                if (go != null) Destroy(go);
            });
        }
        else
        {
            // Try to fade TMP/Graphic if no CanvasGroup
            bool faded = false;
            if (tmp != null)
            {
                Color c = tmp.color;
                tmp.color = new Color(c.r, c.g, c.b, 1f);
                popupRt?.DOLocalMoveY(popupRt.localPosition.y + invalidPopupFloatDistance, invalidPopupDuration).SetEase(Ease.OutCubic);
                tmp.DOFade(0f, invalidPopupDuration).SetEase(Ease.InCubic).OnComplete(() =>
                {
                    if (go != null) Destroy(go);
                });
                faded = true;
            }
            else
            {
                // try Image/Graphic
                var img = go.GetComponentInChildren<UnityEngine.UI.Graphic>();
                if (img != null)
                {
                    Color c = img.color;
                    img.color = new Color(c.r, c.g, c.b, 1f);
                    popupRt?.DOLocalMoveY(popupRt.localPosition.y + invalidPopupFloatDistance, invalidPopupDuration).SetEase(Ease.OutCubic);
                    img.DOFade(0f, invalidPopupDuration).SetEase(Ease.InCubic).OnComplete(() =>
                    {
                        if (go != null) Destroy(go);
                    });
                    faded = true;
                }
            }

            if (!faded)
            {
                // No fade target found - just move and destroy after duration
                popupRt?.DOLocalMoveY(popupRt.localPosition.y + invalidPopupFloatDistance, invalidPopupDuration).SetEase(Ease.OutCubic)
                    .OnComplete(() => { if (go != null) Destroy(go); });
            }
        }
    }

    /// <summary>
    /// Returns zodiac name given index 0..11 in the expected order.
    /// </summary>
    private string GetZodiacNameByIndex(int idx)
    {
        switch (idx)
        {
            case 0: return "Capricorn";
            case 1: return "Aquarius";
            case 2: return "Pisces";
            case 3: return "Aries";
            case 4: return "Taurus";
            case 5: return "Gemini";
            case 6: return "Cancer";
            case 7: return "Leo";
            case 8: return "Virgo";
            case 9: return "Libra";
            case 10: return "Scorpio";
            case 11: return "Sagittarius";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Determine zodiac index (0..11) by month/day using standard western zodiac ranges.
    /// Uses year 2000 for day-of-year calculations.
    /// </summary>
    private int GetZodiacIndex(int month, int day)
    {
        // create DateTime in year 2000 (leap year) to compute dayOfYear
        DateTime dt = new DateTime(2000, month, day);
        int doy = dt.DayOfYear;

        // zodiac ranges using year-2000 dates
        Func<int, int, bool> InRange = (startDoy, endDoy) =>
        {
            if (startDoy <= endDoy) return doy >= startDoy && doy <= endDoy;
            // wrap-around (e.g. Dec -> Jan)
            return doy >= startDoy || doy <= endDoy;
        };

        int jan19 = new DateTime(2000, 1, 19).DayOfYear;
        int jan20 = new DateTime(2000, 1, 20).DayOfYear;
        int feb18 = new DateTime(2000, 2, 18).DayOfYear;
        int feb19 = new DateTime(2000, 2, 19).DayOfYear;
        int mar20 = new DateTime(2000, 3, 20).DayOfYear;
        int mar21 = new DateTime(2000, 3, 21).DayOfYear;
        int apr19 = new DateTime(2000, 4, 19).DayOfYear;
        int apr20 = new DateTime(2000, 4, 20).DayOfYear;
        int may20 = new DateTime(2000, 5, 20).DayOfYear;
        int may21 = new DateTime(2000, 5, 21).DayOfYear;
        int jun20 = new DateTime(2000, 6, 20).DayOfYear;
        int jun21 = new DateTime(2000, 6, 21).DayOfYear;
        int jul22 = new DateTime(2000, 7, 22).DayOfYear;
        int jul23 = new DateTime(2000, 7, 23).DayOfYear;
        int aug22 = new DateTime(2000, 8, 22).DayOfYear;
        int aug23 = new DateTime(2000, 8, 23).DayOfYear;
        int sep22 = new DateTime(2000, 9, 22).DayOfYear;
        int sep23 = new DateTime(2000, 9, 23).DayOfYear;
        int oct22 = new DateTime(2000, 10, 22).DayOfYear;
        int oct23 = new DateTime(2000, 10, 23).DayOfYear;
        int nov21 = new DateTime(2000, 11, 21).DayOfYear;
        int nov22 = new DateTime(2000, 11, 22).DayOfYear;
        int dec21 = new DateTime(2000, 12, 21).DayOfYear;
        int dec22 = new DateTime(2000, 12, 22).DayOfYear;

        // check each zodiac
        if (InRange(dec22, jan19)) return 0;        // Capricorn
        if (InRange(jan20, feb18)) return 1;        // Aquarius
        if (InRange(feb19, mar20)) return 2;        // Pisces
        if (InRange(mar21, apr19)) return 3;        // Aries
        if (InRange(apr20, may20)) return 4;        // Taurus
        if (InRange(may21, jun20)) return 5;        // Gemini
        if (InRange(jun21, jul22)) return 6;        // Cancer
        if (InRange(jul23, aug22)) return 7;        // Leo
        if (InRange(aug23, sep22)) return 8;        // Virgo
        if (InRange(sep23, oct22)) return 9;        // Libra
        if (InRange(oct23, nov21)) return 10;       // Scorpio
        if (InRange(nov22, dec21)) return 11;       // Sagittarius

        return 0; // fallback
    }
}
