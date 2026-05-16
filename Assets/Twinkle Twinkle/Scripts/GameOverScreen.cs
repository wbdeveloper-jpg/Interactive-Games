using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GameOverScreen : MonoBehaviour
{
    [Header("Root")]
    public GameObject panelRoot;
    public CanvasGroup canvasGroup;

    [Header("UI")]
    public Image zodiacImage;
    public TextMeshProUGUI headingText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image[] starImages = new Image[3];
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Animation")]
    public float panelFadeDuration = 0.25f;
    public float starPopDuration = 0.25f;
    public float typewriterCharDelay = 0.025f;

    [Header("Audio")]
    public bool playAudio = true;
    [Tooltip("Played when the fallback result screen is a win. Original project used SFX 2.")]
    public int winSfxId = 2;
    [Tooltip("Played when the fallback result screen is a fail. Original project used SFX 3.")]
    public int failSfxId = 3;

    public event Action PlayAgainRequested;
    public event Action MainMenuRequested;

    private Sequence sequence;
    private Coroutine titleTypingCoroutine;
    private Coroutine descTypingCoroutine;

    private void Awake()
    {
        if (playAgainButton != null) playAgainButton.onClick.AddListener(HandlePlayAgainClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        Hide();
    }

    private void OnDestroy()
    {
        if (playAgainButton != null) playAgainButton.onClick.RemoveListener(HandlePlayAgainClicked);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
    }


    private void HandlePlayAgainClicked()
    {
        PlayAgainRequested?.Invoke();
    }

    private void HandleMainMenuClicked()
    {
        MainMenuRequested?.Invoke();
    }

    public void Show(ZodiacPuzzleData data, bool completed, float timeTakenSeconds, float maxTimeSeconds)
    {
        if (data == null)
        {
            Debug.LogError("GameOverScreen: Cannot show. Data is null.");
            return;
        }

        StopRunningAnimations();

        if (panelRoot != null) panelRoot.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        int stars = ComputeStarCount(completed, timeTakenSeconds, maxTimeSeconds);
        PlaySfx(stars > 0 ? winSfxId : failSfxId);

        if (zodiacImage != null)
        {
            zodiacImage.sprite = data.resultSprite != null ? data.resultSprite : data.fullPuzzleSprite;
            zodiacImage.enabled = zodiacImage.sprite != null;
            zodiacImage.color = Color.white;
            zodiacImage.transform.localScale = Vector3.one;
        }

        if (headingText != null)
        {
            headingText.text = completed ? "You won!" : "Try again!";
            headingText.gameObject.SetActive(true);
        }

        if (titleText != null)
        {
            string article = data.sign.GetArticle();
            titleText.text = "You are " + article + " " + data.DisplayName + "!";
            titleText.maxVisibleCharacters = 0;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.Description;
            descriptionText.maxVisibleCharacters = 0;
        }

        PrepareStars(stars);

        if (playAgainButton != null) playAgainButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);

        sequence = DOTween.Sequence();

        if (canvasGroup != null)
        {
            sequence.Append(canvasGroup.DOFade(1f, panelFadeDuration));
        }

        for (int i = 0; i < stars && i < starImages.Length; i++)
        {
            Image star = starImages[i];
            if (star == null) continue;
            sequence.Append(star.transform.DOScale(Vector3.one * 1.15f, starPopDuration).SetEase(Ease.OutBack));
            sequence.Append(star.transform.DOScale(Vector3.one, 0.08f).SetEase(Ease.InOutSine));
        }

        if (titleText != null)
        {
            sequence.AppendCallback(() => titleTypingCoroutine = StartCoroutine(Typewriter(titleText, typewriterCharDelay)));
            sequence.AppendInterval(Mathf.Max(0.4f, titleText.text.Length * typewriterCharDelay));
        }

        if (descriptionText != null)
        {
            sequence.AppendCallback(() => descTypingCoroutine = StartCoroutine(Typewriter(descriptionText, typewriterCharDelay)));
            sequence.AppendInterval(Mathf.Max(0.4f, descriptionText.text.Length * typewriterCharDelay));
        }

        sequence.AppendCallback(() =>
        {
            if (playAgainButton != null) playAgainButton.gameObject.SetActive(stars < 3);
            if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(true);
        });
    }

    public void Hide()
    {
        StopRunningAnimations();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null) panelRoot.SetActive(false);

        if (headingText != null)
        {
            headingText.text = string.Empty;
            headingText.gameObject.SetActive(false);
        }

        if (titleText != null)
        {
            titleText.text = string.Empty;
            titleText.maxVisibleCharacters = 0;
        }

        if (descriptionText != null)
        {
            descriptionText.text = string.Empty;
            descriptionText.maxVisibleCharacters = 0;
        }

        if (zodiacImage != null)
        {
            zodiacImage.sprite = null;
            zodiacImage.enabled = false;
        }

        if (starImages != null)
        {
            foreach (Image star in starImages)
            {
                if (star == null) continue;
                star.color = new Color(1f, 1f, 1f, 0f);
                star.transform.localScale = Vector3.one;
            }
        }

        if (playAgainButton != null) playAgainButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
    }

    private void PlaySfx(int sfxId)
    {
        if (!playAudio || sfxId < 0) return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfxId);
        }
    }

    private int ComputeStarCount(bool completed, float timeTakenSeconds, float maxTimeSeconds)
    {
        if (!completed) return 0;
        if (maxTimeSeconds <= 0f) return 1;
        if (timeTakenSeconds > maxTimeSeconds) return 0;

        float third = maxTimeSeconds / 3f;
        if (timeTakenSeconds <= third) return 3;
        if (timeTakenSeconds <= third * 2f) return 2;
        return 1;
    }

    private void PrepareStars(int earnedStars)
    {
        if (starImages == null) return;

        for (int i = 0; i < starImages.Length; i++)
        {
            Image star = starImages[i];
            if (star == null) continue;

            if (i < earnedStars)
            {
                star.gameObject.SetActive(true);
                star.color = Color.white;
                star.transform.localScale = Vector3.zero;
            }
            else
            {
                star.gameObject.SetActive(true);
                star.color = new Color(1f, 1f, 1f, 0f);
                star.transform.localScale = Vector3.one;
            }
        }
    }

    private IEnumerator Typewriter(TextMeshProUGUI text, float delay)
    {
        if (text == null) yield break;

        text.ForceMeshUpdate();
        int total = text.text.Length;
        text.maxVisibleCharacters = 0;

        for (int i = 1; i <= total; i++)
        {
            text.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
    }

    private void StopRunningAnimations()
    {
        sequence?.Kill();
        sequence = null;

        if (titleTypingCoroutine != null)
        {
            StopCoroutine(titleTypingCoroutine);
            titleTypingCoroutine = null;
        }

        if (descTypingCoroutine != null)
        {
            StopCoroutine(descTypingCoroutine);
            descTypingCoroutine = null;
        }
    }
}
