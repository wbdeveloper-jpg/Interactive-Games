using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SentenceWordSearchHowToPlayMode
{
    FirstTimeAutomatically,
    EveryGameStartAutomatically,
    ManualButtonOnly
}

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
    [Tooltip("Optional dedicated Timer Card/root. This entire object is hidden in Hidden Unlimited mode or whenever Use Timer is disabled. Leave empty to hide only Timer Text and preserve the existing layout spacing.")]
    public GameObject timerDisplayRoot;

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

    [Header("How To Play Behaviour")]
    [Tooltip("The How To Play button continues to work in every mode.")]
    public SentenceWordSearchHowToPlayMode howToPlayMode = SentenceWordSearchHowToPlayMode.FirstTimeAutomatically;

    [Header("Animation Targets")]
    public RectTransform overlayRoot;
    public RectTransform sentenceAnswerTarget;
    [Tooltip("Optional. Leave empty to animate into the parent of Score Text (normally the complete score card).")]
    public RectTransform scoreAnimationTarget;

    [Header("Colors")]
    public Color sentenceNormalColor = new Color(0.22f, 0.18f, 0.18f, 1f);
    public Color sentenceActiveColor = new Color(0.82f, 0.22f, 0.22f, 1f);
    public Color positivePopupColor = new Color(0.78f, 0.12f, 0.12f, 1f);
    public Color negativePopupColor = new Color(0.75f, 0.18f, 0.18f, 1f);

    [Header("Animation")]
    public float scorePopupDuration = 0.75f;
    public float wordFlyDuration = 0.5f;

    [Header("Correct Score Reward Animation")]
    [Min(0.05f)] public float scoreGainFlyDuration = 0.72f;
    [Range(1f, 1.5f)] public float scoreGainImpactScale = 1.14f;
    [Min(0.05f)] public float scoreGainImpactDuration = 0.16f;

    private Sequence sentencePulseSequence;
    private string currentSentenceWithBlank = "";

    public bool IsResultOpen => resultPanel != null && resultPanel.activeInHierarchy;
    public bool IsHowToPlayOpen => howToPlayPanel != null && howToPlayPanel.activeInHierarchy;
    public bool IsPauseOpen => pausePanel != null && pausePanel.activeInHierarchy;
    public bool IsGameplayBlockingPanelOpen => IsResultOpen || IsHowToPlayOpen || IsPauseOpen;

    private string HowToPlaySeenKey =>
        $"SentenceWordSearch.HowToPlay.Seen.{SceneManager.GetActiveScene().name}";

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

    public void SetTimerVisible(bool visible)
    {
        GameObject display = ResolveTimerDisplay();

        if (display != null)
            display.SetActive(visible);
    }

    private GameObject ResolveTimerDisplay()
    {
        if (timerDisplayRoot != null)
            return timerDisplayRoot;

        if (timerText == null)
            return null;

        // Existing generated scenes use a TimerCard parent. Resolve a clearly
        // named Timer/Clock container without risking a shared top information row.
        Transform current = timerText.transform.parent;
        while (current != null && current != transform)
        {
            string lowerName = current.name.ToLowerInvariant();
            if (lowerName.Contains("timer") || lowerName.Contains("clock"))
                return current.gameObject;

            current = current.parent;
        }

        // Custom hierarchies with no dedicated named container remain safe.
        return timerText.gameObject;
    }

    public void ShowHintPenalty(int penalty, int resultingScore, Camera uiCamera)
    {
        UpdateScore(resultingScore);

        if (penalty <= 0 || hintButton == null)
            return;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            hintButton.transform.position);

        ShowScorePopup($"-{penalty}", screenPosition, uiCamera, false);
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

    public IEnumerator AnimateCorrectScoreToBar(int amount, int resultingScore, Vector2 startScreenPosition, Camera uiCamera)
    {
        // Older/custom scenes can still play safely even if an animation reference is missing.
        if (overlayRoot == null || scoreText == null)
        {
            UpdateScore(resultingScore);
            yield break;
        }

        GameObject rewardObject = new GameObject("FlyingScoreReward", typeof(RectTransform));
        rewardObject.transform.SetParent(overlayRoot, false);

        TextMeshProUGUI rewardText = rewardObject.AddComponent<TextMeshProUGUI>();
        rewardText.text = $"+{amount}";
        rewardText.alignment = TextAlignmentOptions.Center;
        rewardText.fontSize = 56f;
        rewardText.fontStyle = FontStyles.Bold;
        rewardText.raycastTarget = false;
        rewardText.color = positivePopupColor;

        if (primaryFont != null)
            rewardText.font = primaryFont;

        RectTransform rewardRect = rewardObject.GetComponent<RectTransform>();
        rewardRect.sizeDelta = new Vector2(240f, 100f);

        Vector2 startLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRoot,
            startScreenPosition,
            uiCamera,
            out startLocal);

        rewardRect.anchoredPosition = startLocal;
        rewardRect.localScale = Vector3.one;

        RectTransform target = ResolveScoreAnimationTarget();
        Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        Vector2 targetLocalPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRoot,
            targetScreenPosition,
            uiCamera,
            out targetLocalPosition);

        Sequence rewardSequence = DOTween.Sequence();
        rewardSequence.Append(rewardRect.DOAnchorPos(targetLocalPosition, scoreGainFlyDuration).SetEase(Ease.InOutCubic));
        rewardSequence.Join(rewardRect.DOScale(0.72f, scoreGainFlyDuration).SetEase(Ease.InCubic));

        yield return rewardSequence.WaitForCompletion();

        if (rewardObject != null)
            Destroy(rewardObject);

        // The visible score changes only when the reward reaches the score card.
        UpdateScore(resultingScore);

        Vector3 originalScale = target.localScale;
        Sequence impactSequence = DOTween.Sequence();
        impactSequence.Append(target.DOScale(originalScale * scoreGainImpactScale, scoreGainImpactDuration).SetEase(Ease.OutBack));
        impactSequence.Append(target.DOScale(originalScale, scoreGainImpactDuration).SetEase(Ease.InOutSine));

        yield return impactSequence.WaitForCompletion();

        if (target != null)
            target.localScale = originalScale;
    }

    private RectTransform ResolveScoreAnimationTarget()
    {
        if (scoreAnimationTarget != null)
            return scoreAnimationTarget;

        // Prefer the nearest visual card so customised UI hierarchies pulse the
        // score bar rather than only the text or an entire top navigation row.
        Transform current = scoreText != null ? scoreText.transform.parent : null;
        while (current != null && current != overlayRoot)
        {
            RectTransform currentRect = current as RectTransform;
            if (currentRect != null && current.GetComponent<Image>() != null)
                return currentRect;

            current = current.parent;
        }

        if (scoreText != null && scoreText.rectTransform.parent is RectTransform parentRect)
            return parentRect;

        return scoreText != null ? scoreText.rectTransform : overlayRoot;
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

    public bool ShouldShowHowToPlayAutomatically()
    {
        switch (howToPlayMode)
        {
            case SentenceWordSearchHowToPlayMode.EveryGameStartAutomatically:
                return true;

            case SentenceWordSearchHowToPlayMode.ManualButtonOnly:
                return false;

            default:
                return PlayerPrefs.GetInt(HowToPlaySeenKey, 0) == 0;
        }
    }

    public void MarkHowToPlaySeen()
    {
        PlayerPrefs.SetInt(HowToPlaySeenKey, 1);
        PlayerPrefs.Save();
    }

    [ContextMenu("Reset How To Play Seen For This Scene")]
    public void ResetHowToPlaySeenForThisScene()
    {
        PlayerPrefs.DeleteKey(HowToPlaySeenKey);
        PlayerPrefs.Save();
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
