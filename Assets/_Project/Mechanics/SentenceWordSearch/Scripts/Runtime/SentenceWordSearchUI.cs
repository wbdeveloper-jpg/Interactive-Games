using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SentenceWordSearchUI : MonoBehaviour
{
    [Header("Universal Fonts")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;
    public List<TextMeshProUGUI> primaryTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> secondaryTexts = new List<TextMeshProUGUI>();

    [Header("Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI sentenceText;
    public TextMeshProUGUI progressText; // Legacy/backward compatible. Prefer questionCounterText for this layout.
    public TextMeshProUGUI questionCounterText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public TextMeshProUGUI howToPlayBodyText;

    [Header("Images")]
    public Image questionImage;

    [Header("Panels")]
    public GameObject resultPanel;
    public GameObject howToPlayPanel;
    public GameObject pausePanel;

    [Header("Buttons")]
    public Button pauseButton;
    public Button resumeButton;
    public Button howToPlayButton;
    public Button closeHowToPlayButton;
    public Button hintButton;
    public Button restartButton;
    public Button resultRestartButton;
    [Tooltip("Optional. Assign the Continue button in Result Panel here. It opens Bloom post-game reward screen.")]
    public Button resultContinueButton;

    [Header("Animation Targets")]
    public RectTransform overlayRoot;
    public RectTransform sentenceAnswerTarget;

    [Header("Colors")]
    public Color sentenceNormalColor = new Color(0.22f, 0.18f, 0.18f, 1f);
    public Color sentenceActiveColor = new Color(0.82f, 0.22f, 0.22f, 1f);
    public Color positivePopupColor = new Color(0.78f, 0.12f, 0.12f, 1f);
    public Color negativePopupColor = new Color(0.75f, 0.18f, 0.18f, 1f);

    [Header("Animation")]
    public float scorePopupDuration = 0.75f;
    public float wordFlyDuration = 0.5f;

    private Sequence sentencePulseSequence;
    private string currentSentenceWithBlank = "";

    public bool IsResultOpen => resultPanel != null && resultPanel.activeInHierarchy;
    public bool IsHowToPlayOpen => howToPlayPanel != null && howToPlayPanel.activeInHierarchy;
    public bool IsPauseOpen => pausePanel != null && pausePanel.activeInHierarchy;
    public bool IsGameplayBlockingPanelOpen => IsResultOpen || IsHowToPlayOpen || IsPauseOpen;

    private void Awake()
    {
        ApplyFonts();
        HideResult();
        HidePause();
    }

    public void ApplyFonts()
    {
        if (primaryFont != null)
        {
            for (int i = 0; i < primaryTexts.Count; i++)
            {
                if (primaryTexts[i] != null)
                    primaryTexts[i].font = primaryFont;
            }
        }

        if (secondaryFont != null)
        {
            for (int i = 0; i < secondaryTexts.Count; i++)
            {
                if (secondaryTexts[i] != null)
                    secondaryTexts[i].font = secondaryFont;
            }
        }
    }

    public void ShowQuestion(SentenceWordSearchQuestion question, int questionNumber, int totalQuestions)
    {
        currentSentenceWithBlank = question != null ? question.sentenceWithBlank : "";

        if (sentenceText != null)
        {
            sentenceText.text = currentSentenceWithBlank;
            sentenceText.color = sentenceNormalColor;
            sentenceText.transform.localScale = Vector3.one;
        }

        if (questionImage != null)
        {
            bool hasSprite = question != null && question.questionSprite != null;
            questionImage.gameObject.SetActive(hasSprite);
            questionImage.sprite = hasSprite ? question.questionSprite : null;
        }

        UpdateProgress(questionNumber, totalQuestions);
    }

    public void UpdateProgress(int questionNumber, int totalQuestions)
    {
        string value = $"Question {questionNumber}/{totalQuestions}";

        if (questionCounterText != null)
            questionCounterText.text = value;

        // Backward compatibility for older scenes that used ProgressText.
        if (progressText != null)
            progressText.text = value;
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void UpdateTimer(float seconds, bool useTimer)
    {
        if (timerText == null)
            return;

        if (!useTimer)
        {
            timerText.text = "--";
            return;
        }

        int total = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = total / 60;
        int sec = total % 60;

        timerText.text = $"{minutes:00}:{sec:00}";
    }

    public void FillSentenceAnswer(string answer)
    {
        if (sentenceText == null)
            return;

        string cleanAnswer = answer.ToUpperInvariant();
        string source = string.IsNullOrWhiteSpace(currentSentenceWithBlank) ? sentenceText.text : currentSentenceWithBlank;
        string coloredAnswer = $"<color=#{ColorUtility.ToHtmlStringRGB(sentenceActiveColor)}><b>{cleanAnswer}</b></color>";
        sentenceText.text = ReplaceBlankWithAnswer(source, coloredAnswer);
    }

    public void StartSentenceReadPulse()
    {
        if (sentenceText == null)
            return;

        StopSentenceReadPulse(false);

        sentenceText.color = sentenceNormalColor;
        sentenceText.transform.localScale = Vector3.one;

        sentencePulseSequence = DOTween.Sequence();
        sentencePulseSequence.Append(sentenceText.DOColor(sentenceActiveColor, 0.28f));
        sentencePulseSequence.Join(sentenceText.transform.DOScale(1.035f, 0.28f));
        sentencePulseSequence.Append(sentenceText.DOColor(sentenceNormalColor, 0.28f));
        sentencePulseSequence.Join(sentenceText.transform.DOScale(1f, 0.28f));
        sentencePulseSequence.SetLoops(-1);
    }

    public void StopSentenceReadPulse(bool resetColor = true)
    {
        if (sentencePulseSequence != null)
        {
            sentencePulseSequence.Kill();
            sentencePulseSequence = null;
        }

        if (sentenceText != null)
        {
            sentenceText.transform.DOKill();
            sentenceText.transform.localScale = Vector3.one;

            if (resetColor)
                sentenceText.color = sentenceNormalColor;
        }
    }

    public void ShowScorePopup(string message, Vector2 screenPosition, Camera uiCamera, bool positive)
    {
        if (overlayRoot == null)
            return;

        GameObject popupObject = new GameObject("ScorePopup", typeof(RectTransform));
        popupObject.transform.SetParent(overlayRoot, false);

        TextMeshProUGUI popupText = popupObject.AddComponent<TextMeshProUGUI>();
        popupText.text = message;
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.fontSize = 48f;
        popupText.fontStyle = FontStyles.Bold;
        popupText.raycastTarget = false;
        popupText.color = positive ? positivePopupColor : negativePopupColor;

        if (primaryFont != null)
            popupText.font = primaryFont;

        RectTransform rect = popupObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(220f, 90f);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPosition, uiCamera, out localPoint);
        rect.anchoredPosition = localPoint;
        rect.localScale = Vector3.one * 0.65f;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(rect.DOScale(1.15f, 0.16f).SetEase(Ease.OutBack));
        sequence.Join(rect.DOAnchorPos(localPoint + new Vector2(0f, 80f), scorePopupDuration).SetEase(Ease.OutCubic));
        sequence.Join(popupText.DOFade(0f, scorePopupDuration).SetDelay(0.2f));
        sequence.OnComplete(() =>
        {
            if (popupObject != null)
                Destroy(popupObject);
        });
    }

    public IEnumerator AnimateWordToSentence(string answer, Vector2 startScreenPosition, Camera uiCamera)
    {
        if (overlayRoot == null)
        {
            FillSentenceAnswer(answer);
            yield break;
        }

        GameObject wordObject = new GameObject("FlyingAnswerWord", typeof(RectTransform));
        wordObject.transform.SetParent(overlayRoot, false);

        TextMeshProUGUI wordText = wordObject.AddComponent<TextMeshProUGUI>();
        wordText.text = answer.ToUpperInvariant();
        wordText.alignment = TextAlignmentOptions.Center;
        wordText.fontSize = 54f;
        wordText.fontStyle = FontStyles.Bold;
        wordText.raycastTarget = false;
        wordText.color = sentenceActiveColor;

        if (primaryFont != null)
            wordText.font = primaryFont;

        RectTransform wordRect = wordObject.GetComponent<RectTransform>();
        wordRect.sizeDelta = new Vector2(340f, 100f);

        Vector2 startLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, startScreenPosition, uiCamera, out startLocal);
        wordRect.anchoredPosition = startLocal;
        wordRect.localScale = Vector3.one * 0.85f;

        RectTransform target = sentenceAnswerTarget != null ? sentenceAnswerTarget : sentenceText != null ? sentenceText.rectTransform : overlayRoot;
        Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        Vector2 endLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, targetScreen, uiCamera, out endLocal);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(wordRect.DOScale(1.15f, 0.14f).SetEase(Ease.OutBack));
        sequence.Append(wordRect.DOAnchorPos(endLocal, wordFlyDuration).SetEase(Ease.InOutCubic));
        sequence.Join(wordRect.DOScale(0.92f, wordFlyDuration));
        sequence.Append(wordText.DOFade(0f, 0.12f));

        yield return sequence.WaitForCompletion();

        if (wordObject != null)
            Destroy(wordObject);

        FillSentenceAnswer(answer);
    }

    public void ShowResult(int score, bool completed)
    {
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

    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void HideHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    public void ShowPause()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void HidePause()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private string ReplaceBlankWithAnswer(string source, string answerRichText)
    {
        if (string.IsNullOrEmpty(source))
            return answerRichText;

        int start = -1;
        int length = 0;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == '_')
            {
                if (start < 0)
                    start = i;

                length++;
            }
            else if (start >= 0)
            {
                break;
            }
        }

        if (start >= 0 && length > 0)
            return source.Remove(start, length).Insert(start, answerRichText);

        return source + " " + answerRichText;
    }
}
