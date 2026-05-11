using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject startingPanel;

    [Header("Inspector Settings")]
    public float maxTime = 90f;

    [Header("UI References")]
    public CanvasGroup panelCanvasGroup;
    public Image zodiacImage;
    public TextMeshProUGUI headingText;      // <--- new heading field
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;
    public Image[] starImages = new Image[3];
    public Button playAgainButton;
    public Button mainMenuButton;

    [Header("Zodiac Sprites")]
    public Sprite[] zodiacSprites = new Sprite[12];

    [Header("Animation Settings")]
    public float panelFadeDuration = 0.25f;
    public float typewriterCharDelay = 0.03f;

    private Sequence fullSequence;
    private bool isShowing = false;

    void Awake()
    {
        ResetVisuals();
    }

    private void Start()
    {
        playAgainButton.onClick.AddListener(OnPlayAgain);
        mainMenuButton.onClick.AddListener(OnMainButton);
    }

    /// <summary>
    /// ShowGameOver flow:
    /// - Fade panel
    /// - Pop earned stars sequentially
    /// - Typewriter title & desc
    /// - Show buttons (PlayAgain hidden if 3 stars)
    /// </summary>
    public void ShowGameOver(bool completed, float timeTakenSeconds, ZodiacSign zodiac, float optionalMaxTime = -1f)
    {
        Debug.Log("The Animation is Starting +" + timeTakenSeconds);
        // Ensure panel is active
        panelCanvasGroup.gameObject.SetActive(true);

        // Stop any old sequence
        if (isShowing)
        {
            if (fullSequence != null && fullSequence.IsActive()) fullSequence.Kill();
            StopAllCoroutines();
            ResetVisuals();
        }

        isShowing = true;

        float effectiveMax = (optionalMaxTime > 0f) ? optionalMaxTime : maxTime;
        int earnedStars = ComputeStarCount(completed, timeTakenSeconds, effectiveMax);

        // Zodiac image — visible instantly (NO ANIMATION)
        if (zodiacImage != null && zodiacSprites != null && zodiacSprites.Length > (int)zodiac)
        {
            zodiacImage.sprite = zodiacSprites[(int)zodiac];
            zodiacImage.transform.localScale = Vector3.one;
            zodiacImage.color = Color.white; // ensure visible
        }
        // Playing Audio
        if (earnedStars <= 0)
            AudioManager.Instance.PlaySFX(3);
        else
            AudioManager.Instance.PlaySFX(2);


        // --- NEW: set heading based on earned stars ---
        if (headingText != null)
        {
            if (earnedStars <= 0)
                headingText.text = "Try again!";
            else
                headingText.text = "You won!";
            headingText.gameObject.SetActive(true);
        }
        // --- end heading setup ---

        // Texts
        string name = zodiac.GetDisplayName();
        string article = zodiac.GetArticle();

        titleText.text = $"You are {article} {name}!";
        titleText.maxVisibleCharacters = 0;

        descText.text = zodiac.GetDefaultDescription();
        descText.maxVisibleCharacters = 0;

        Debug.Log("Earned Star " + earnedStars);

        // Setup stars: show/hide and set initial scale for pop animation
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;

            if (i < earnedStars)
            {
                // Make visible but scale to zero to allow pop
                starImages[i].color = Color.white;
                starImages[i].transform.localScale = Vector3.zero;
            }
            else
            {
                // Hide unearned stars
                starImages[i].color = new Color(1, 1, 1, 0);
                starImages[i].transform.localScale = Vector3.one;
            }
        }

        // Build master sequence
        fullSequence = DOTween.Sequence();

        // Fade in panel
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.blocksRaycasts = true;
        fullSequence.Append(panelCanvasGroup.DOFade(1f, panelFadeDuration));

        // STAR POP SEQUENCE (only for earned stars) - simple and short
        if (earnedStars > 0)
        {
            // Create an inner sequence for star pops so they play before typewriter
            Sequence starSeq = DOTween.Sequence();

            for (int i = 0; i < earnedStars && i < starImages.Length; i++)
            {
                Image sImg = starImages[i];
                if (sImg == null) continue;

                // Ensure starting scale is zero (we already set above)
                sImg.transform.localScale = Vector3.zero;

                // Pop animation: scale 0 -> 1.15 -> 1 (overshoot then settle)
                starSeq.Append(sImg.transform.DOScale(1.15f, 0.28f).SetEase(Ease.OutBack));
                starSeq.Append(sImg.transform.DOScale(1f, 0.12f).SetEase(Ease.InBack));
                // small stagger automatically provided by sequence appends
            }

            fullSequence.Append(starSeq);
        }

        // Typewriter title
        fullSequence.AppendCallback(() => StartCoroutine(Typewriter(titleText, typewriterCharDelay)));
        fullSequence.AppendInterval(1f);

        // Typewriter desc
        fullSequence.AppendCallback(() => StartCoroutine(Typewriter(descText, typewriterCharDelay)));
        fullSequence.AppendInterval(1f);

        // Show buttons: only show PlayAgain if earnedStars < 3
        fullSequence.AppendCallback(() =>
        {
            bool showPlayAgain = earnedStars < 3;

            playAgainButton.gameObject.SetActive(showPlayAgain);
            mainMenuButton.gameObject.SetActive(true);

            // Ensure scale and visible states are normalized
            if (playAgainButton.gameObject.activeSelf)
                playAgainButton.transform.localScale = Vector3.one;
            if (mainMenuButton.gameObject.activeSelf)
                mainMenuButton.transform.localScale = Vector3.one;
        });

        fullSequence.Play();
    }

    private int ComputeStarCount(bool completed, float timeTakenSeconds, float effectiveMaxTime)
    {
        Debug.Log("Time taken " + timeTakenSeconds + "Effectivev max time" + effectiveMaxTime);
        if (!completed) return 0;
        if (timeTakenSeconds > effectiveMaxTime) return 0;

        float third = effectiveMaxTime / 3f;
        if (timeTakenSeconds <= third) return 3;
        if (timeTakenSeconds <= third * 2f) return 2;

        return 1;
    }

    private IEnumerator Typewriter(TextMeshProUGUI tmp, float delay)
    {
        if (tmp == null) yield break;
        tmp.ForceMeshUpdate();
        int total = tmp.text.Length;
        tmp.maxVisibleCharacters = 0;

        for (int i = 1; i <= total; i++)
        {
            tmp.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }
    }

    public void ResetVisuals()
    {
        isShowing = false;

        // Hide panel
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        // Reset zodiac image
        if (zodiacImage != null)
        {
            zodiacImage.color = Color.white;
            zodiacImage.transform.localScale = Vector3.one;
        }

        // Reset heading
        if (headingText != null)
        {
            headingText.text = "";
            headingText.gameObject.SetActive(false);
        }

        // Reset texts
        if (titleText != null)
        {
            titleText.text = "";
            titleText.maxVisibleCharacters = 0;
        }

        if (descText != null)
        {
            descText.text = "";
            descText.maxVisibleCharacters = 0;
        }

        // Hide stars
        foreach (var s in starImages)
        {
            if (s != null)
            {
                s.color = new Color(1, 1, 1, 0);
                s.transform.localScale = Vector3.one;
            }
        }

        // Hide buttons
        if (playAgainButton != null) playAgainButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
    }

    public void PanelControl()
    {
        startingPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnMainButton()
    {
    }

    public void OnPlayAgain()
    {
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
