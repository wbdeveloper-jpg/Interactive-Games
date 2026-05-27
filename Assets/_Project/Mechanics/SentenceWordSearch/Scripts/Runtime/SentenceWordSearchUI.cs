using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SentenceWordSearchUI : MonoBehaviour
{
    [Header("Universal Fonts")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;
    public TextMeshProUGUI[] primaryFontTexts;
    public TextMeshProUGUI[] secondaryFontTexts;

    [Header("Main UI")]
    public Canvas canvas;
    public TextMeshProUGUI sentenceText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Image questionImage;

    [Header("Panels")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public GameObject howToPlayPanel;
    public GameObject pausePanel;

    [Header("Animation Text")]
    public TextMeshProUGUI flyingWordText;
    public TextMeshProUGUI scorePopupText;

    [Header("Animation Settings")]
    public Color readingHighlightColor = new Color(0.1f, 0.42f, 1f);
    public Color normalSentenceColor = new Color(0.08f, 0.1f, 0.16f);
    public Color positiveScoreColor = new Color(0.05f, 0.65f, 0.22f);
    public Color negativeScoreColor = new Color(0.9f, 0.18f, 0.12f);

    private Sequence readingSequence;

    private void Awake()
    {
        ApplyFonts();
    }

    private void OnValidate()
    {
        ApplyFonts();
    }

    public void ApplyFonts()
    {
        if (primaryFont != null && primaryFontTexts != null)
        {
            for (int i = 0; i < primaryFontTexts.Length; i++)
            {
                if (primaryFontTexts[i] != null)
                    primaryFontTexts[i].font = primaryFont;
            }
        }

        if (secondaryFont != null && secondaryFontTexts != null)
        {
            for (int i = 0; i < secondaryFontTexts.Length; i++)
            {
                if (secondaryFontTexts[i] != null)
                    secondaryFontTexts[i].font = secondaryFont;
            }
        }
    }

    public void SetQuestion(SentenceWordSearchQuestion question, int index, int total)
    {
        StopSentenceReadingPulse();

        if (sentenceText != null)
        {
            sentenceText.DOKill();
            sentenceText.color = normalSentenceColor;
            sentenceText.transform.localScale = Vector3.one;
            sentenceText.text = question.sentenceWithBlank;
        }

        if (questionImage != null)
        {
            questionImage.sprite = question.questionSprite;
            questionImage.gameObject.SetActive(question.questionSprite != null);
        }

        SetProgress(index, total);
    }

    public void SetProgress(int index, int total)
    {
        if (progressText != null)
            progressText.text = $"{index + 1} / {total}";
    }

    public void SetScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void SetTimer(float remaining, bool useTimer)
    {
        if (timerText != null)
            timerText.text = useTimer ? SentenceWordSearchUtility.FormatTime(remaining) : "--";
    }

    public IEnumerator PlayScorePopup(string message, Vector3 startWorldPosition, bool positive)
    {
        EnsureScorePopupText();

        if (scorePopupText == null)
            yield break;

        scorePopupText.gameObject.SetActive(true);
        scorePopupText.DOKill();
        scorePopupText.text = message;
        scorePopupText.color = positive ? positiveScoreColor : negativeScoreColor;
        scorePopupText.alpha = 1f;
        scorePopupText.transform.position = startWorldPosition;
        scorePopupText.transform.localScale = Vector3.one * 0.75f;

        Sequence seq = DOTween.Sequence();
        seq.Join(scorePopupText.transform.DOScale(1.25f, 0.2f).SetEase(Ease.OutBack));
        seq.Append(scorePopupText.transform.DOMoveY(startWorldPosition.y + 85f, 0.45f).SetEase(Ease.OutQuad));
        seq.Join(scorePopupText.DOFade(0f, 0.45f));
        yield return seq.WaitForCompletion();

        scorePopupText.gameObject.SetActive(false);
    }

    public IEnumerator PlayWordToSentenceAnimation(string sentenceWithBlank, string answer, Vector3 startWorldPosition)
    {
        EnsureFlyingWordText();

        if (flyingWordText != null)
        {
            flyingWordText.gameObject.SetActive(true);
            flyingWordText.DOKill();
            flyingWordText.text = answer;
            flyingWordText.transform.position = startWorldPosition;
            flyingWordText.transform.localScale = Vector3.one * 0.75f;
            flyingWordText.alpha = 1f;

            Vector3 targetPosition = sentenceText != null ? sentenceText.transform.position : startWorldPosition;
            Sequence flySeq = DOTween.Sequence();
            flySeq.Join(flyingWordText.transform.DOMove(targetPosition, 0.48f).SetEase(Ease.InOutQuad));
            flySeq.Join(flyingWordText.transform.DOScale(1.15f, 0.48f).SetEase(Ease.OutBack));
            yield return flySeq.WaitForCompletion();

            flyingWordText.gameObject.SetActive(false);
        }

        if (sentenceText != null)
        {
            sentenceText.text = SentenceWordSearchUtility.CompleteSentence(sentenceWithBlank, answer);
            sentenceText.color = readingHighlightColor;
            sentenceText.transform.DOKill();
            sentenceText.transform.localScale = Vector3.one;
            sentenceText.transform.DOPunchScale(Vector3.one * 0.08f, 0.28f, 8, 0.7f);
            yield return new WaitForSeconds(0.28f);
        }
    }

    public IEnumerator PlaySentenceReadingPulse(float duration)
    {
        if (sentenceText == null)
            yield break;

        duration = Mathf.Max(0.35f, duration);
        StopSentenceReadingPulse();

        sentenceText.color = readingHighlightColor;
        sentenceText.transform.localScale = Vector3.one;

        readingSequence = DOTween.Sequence();
        readingSequence.Join(sentenceText.DOColor(readingHighlightColor, 0.12f));
        readingSequence.Join(sentenceText.transform.DOScale(1.035f, 0.38f).SetEase(Ease.InOutSine));
        readingSequence.SetLoops(-1, LoopType.Yoyo);

        yield return new WaitForSeconds(duration);

        StopSentenceReadingPulse();
    }

    public void StopSentenceReadingPulse()
    {
        if (readingSequence != null)
        {
            readingSequence.Kill();
            readingSequence = null;
        }

        if (sentenceText != null)
        {
            sentenceText.DOKill();
            sentenceText.transform.localScale = Vector3.one;
            sentenceText.color = normalSentenceColor;
        }
    }

    public void ShowResult(bool completed, int score)
    {
        StopSentenceReadingPulse();

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultTitleText != null)
            resultTitleText.text = completed ? "Great Job!" : "Time Up!";

        if (resultScoreText != null)
            resultScoreText.text = $"Score: {score}";
    }

    public void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    public void ShowHowToPlay(bool show)
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(show);
    }

    public void ShowPause(bool show)
    {
        if (pausePanel != null)
            pausePanel.SetActive(show);
    }

    private void EnsureFlyingWordText()
    {
        if (flyingWordText != null)
            return;

        flyingWordText = CreateFloatingText("FlyingWordText", 52f, new Color(0.08f, 0.1f, 0.16f));
    }

    private void EnsureScorePopupText()
    {
        if (scorePopupText != null)
            return;

        scorePopupText = CreateFloatingText("ScorePopupText", 46f, positiveScoreColor);
    }

    private TextMeshProUGUI CreateFloatingText(string objectName, float fontSize, Color color)
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
            return null;

        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        tmp.gameObject.SetActive(false);

        if (primaryFont != null)
            tmp.font = primaryFont;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 110f);

        return tmp;
    }
}
