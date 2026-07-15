using System.Text;
using TMPro;
using UnityEngine;

public class FractionPortionQuestionRenderer : MonoBehaviour
{
    [Header("References")]
    public RectTransform contentRoot;
    public TMP_Text fallbackText;

    [Header("Typography")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;
    public Color textColor = new Color(0.18f, 0.13f, 0.09f, 1f);
    public Color mutedColor = new Color(0.36f, 0.28f, 0.2f, 1f);
    public int labelFontSize = 26;
    public int itemFontSize = 27;
    public int operatorFontSize = 28;
    public int fractionFontSize = 27;

    [Header("Student Fraction Display")]
    public bool useTmpRichTextFractions = true;
    public bool showSliceCountPrefix = true;
    [Range(45, 120)] public int inlineFractionSizePercent = 100;
    [Range(45, 140)] public int fractionNumberSizePercent = 105;
    [Range(0f, 0.5f)] public float fractionVerticalOffsetEm = 0.18f;
    public bool useSupSubTags = false;
    public string fractionSlash = "⁄";
    [Tooltip("Very small visual breathing space around the fraction slash. Thin space is subtle and safer than normal spaces.")]
    public string fractionSlashSideSpace = " ";
    public string requestSeparator = "   •   ";

    private TMP_Text inlineText;

    public void RenderQuestion(FractionPortionFillManager.RuntimeQuestion question, TMP_FontAsset primary, TMP_FontAsset secondary)
    {
        if (question == null)
            return;

        SetFonts(primary, secondary);
        TMP_Text text = EnsureInlineText();
        text.text = BuildQuestionText(question);
        ApplyTextStyle(text, TextAlignmentOptions.Center);
    }

    public void RenderHint(FractionPortionFillManager.RuntimeQuestion question, TMP_FontAsset primary, TMP_FontAsset secondary)
    {
        if (question == null)
            return;

        SetFonts(primary, secondary);
        TMP_Text text = EnsureInlineText();
        text.text = BuildHintText(question);
        ApplyTextStyle(text, TextAlignmentOptions.Center);
    }

    public void SetFonts(TMP_FontAsset primary, TMP_FontAsset secondary)
    {
        if (primary != null)
            primaryFont = primary;
        if (secondary != null)
            secondaryFont = secondary;

        TMP_Text text = inlineText != null ? inlineText : fallbackText;
        if (text != null && primaryFont != null)
            text.font = primaryFont;
    }

    public void Clear()
    {
        TMP_Text text = inlineText != null ? inlineText : fallbackText;
        if (text != null)
            text.text = string.Empty;
    }

    private string BuildQuestionText(FractionPortionFillManager.RuntimeQuestion question)
    {
        StringBuilder builder = new StringBuilder();
        if (showSliceCountPrefix)
            builder.Append(question.portionCount).Append("-slice pizza  |  ");

        for (int i = 0; i < question.requests.Count; i++)
        {
            FractionPortionFillManager.RuntimeRequest request = question.requests[i];
            if (request == null)
                continue;

            builder.Append("Cover ");
            builder.Append(BuildRequestMath(request));
            builder.Append(" of the pizza with ").Append(request.itemName);

            if (i < question.requests.Count - 1)
                builder.Append(requestSeparator);
        }

        return builder.ToString();
    }

    private string BuildHintText(FractionPortionFillManager.RuntimeQuestion question)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("Hint: ").Append(question.portionCount).Append(" slices total  |  ");

        if (question.isImpossibleAtStart)
        {
            builder.Append("Not enough stock: ");
            for (int i = 0; i < question.requests.Count; i++)
            {
                FractionPortionFillManager.RuntimeRequest request = question.requests[i];
                if (request == null)
                    continue;

                int stock = question.initialStockByItemId.ContainsKey(request.itemId) ? question.initialStockByItemId[request.itemId] : 0;
                builder.Append(request.itemName)
                    .Append(" needs ").Append(request.requiredUnits)
                    .Append(request.requiredUnits == 1 ? " slice" : " slices")
                    .Append(", has ").Append(stock);

                if (i < question.requests.Count - 1)
                    builder.Append(requestSeparator);
            }

            return builder.ToString();
        }

        builder.Append("Place: ");
        for (int i = 0; i < question.requests.Count; i++)
        {
            FractionPortionFillManager.RuntimeRequest request = question.requests[i];
            if (request == null)
                continue;

            builder.Append(request.requiredUnits)
                .Append(request.requiredUnits == 1 ? " slice " : " slices ")
                .Append(request.itemName);

            if (i < question.requests.Count - 1)
                builder.Append(requestSeparator);
        }

        return builder.ToString();
    }

    private string BuildRequestMath(FractionPortionFillManager.RuntimeRequest request)
    {
        if (request == null)
            return string.Empty;

        if (request.terms.Count == 0)
            return request.requiredUnits.ToString();

        if (request.operationType == FractionPortionFillManager.OperationType.Addition && request.terms.Count >= 2)
            return FormatFraction(request.terms[0]) + " + " + FormatFraction(request.terms[1]);

        if (request.operationType == FractionPortionFillManager.OperationType.Subtraction && request.terms.Count >= 2)
            return FormatFraction(request.terms[0]) + " − " + FormatFraction(request.terms[1]);

        return FormatFraction(request.terms[0]);
    }

    private string FormatFraction(FractionPortionFillManager.FractionTerm term)
    {
        if (term == null)
            return string.Empty;

        if (term.denominator == 1)
            return term.numerator.ToString();

        int displayNumerator = term.ShouldDisplayAsMixedNumber ? term.RemainderNumerator : term.numerator;
        string mixedPrefix = term.ShouldDisplayAsMixedNumber ? term.WholeNumber + " " : string.Empty;

        if (!useTmpRichTextFractions)
            return mixedPrefix + displayNumerator + "/" + term.denominator;

        if (useSupSubTags)
            return mixedPrefix + "<size=" + inlineFractionSizePercent + "%><sup>" + displayNumerator + "</sup>" + fractionSlashSideSpace + fractionSlash + fractionSlashSideSpace + "<sub>" + term.denominator + "</sub></size>";

        string up = fractionVerticalOffsetEm.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        string down = (-fractionVerticalOffsetEm).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        return mixedPrefix + "<size=" + inlineFractionSizePercent + "%>"
            + "<voffset=" + up + "em><size=" + fractionNumberSizePercent + "%>" + displayNumerator + "</size></voffset>"
            + fractionSlashSideSpace + fractionSlash + fractionSlashSideSpace
            + "<voffset=" + down + "em><size=" + fractionNumberSizePercent + "%>" + term.denominator + "</size></voffset>"
            + "</size>";
    }

    private TMP_Text EnsureInlineText()
    {
        if (contentRoot == null)
            contentRoot = GetComponent<RectTransform>();

        if (fallbackText != null)
        {
            inlineText = fallbackText;
        }
        else if (inlineText == null)
        {
            GameObject go = new GameObject("Question Inline TMP Text", typeof(RectTransform));
            go.transform.SetParent(contentRoot != null ? contentRoot : transform, false);
            inlineText = go.AddComponent<TextMeshProUGUI>();
        }

        inlineText.gameObject.SetActive(true);
        RectTransform rect = inlineText.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 2f);
        rect.offsetMax = new Vector2(-12f, -2f);
        return inlineText;
    }

    private void ApplyTextStyle(TMP_Text text, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        if (primaryFont != null)
            text.font = primaryFont;

        text.fontSize = itemFontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = itemFontSize;
        text.color = textColor;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        text.richText = true;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.margin = new Vector4(10f, 0f, 10f, 0f);
    }
}
