using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;

public class ResultPanelController : MonoBehaviour
{
    [Header("Main")]
    public GameObject panel;
    public Transform panelRoot;

    [Header("Text")]
    public TextMeshProUGUI headingText;
    public TextMeshProUGUI subHeadingText;

    [Header("Stars")]
    public Transform starsParent; // contains 3 stars
    private Transform[] stars;

    [Header("Buttons")]
    public GameObject playAgainBtn;
    public GameObject mainMenuBtn;

    [Header("Settings")]
    public float typeSpeed = 0.04f;

    void Awake()
    {
        // cache stars
        stars = new Transform[starsParent.childCount];
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i] = starsParent.GetChild(i);
        }
    }

    public void ShowResult(float accuracy)
    {
        panel.SetActive(true);
        StartCoroutine(PlaySequence(accuracy));
    }

    IEnumerator PlaySequence(float accuracy)
    {
        // 🔒 Reset
        playAgainBtn.SetActive(false);
        mainMenuBtn.SetActive(false);
        AudioManager.Instance.PlaySFX(3);
        // hide overlays initially
        foreach (var star in stars)
        {
            Transform overlay = star.GetChild(0);
            overlay.gameObject.SetActive(false);
        }

        // 🟡 Panel pop
        panelRoot.localScale = Vector3.zero;
        yield return panelRoot.DOScale(1f, 0.4f).SetEase(Ease.OutBack).WaitForCompletion();

        // 🎯 Get text
        string heading, sub;

        GetTextByAccuracy(accuracy, out heading, out sub);

        // ✍️ Heading
        yield return StartCoroutine(TypeText(headingText, heading));

        yield return new WaitForSeconds(0.3f);

        // ✍️ Subheading
        yield return StartCoroutine(TypeText(subHeadingText, sub));

        yield return new WaitForSeconds(0.5f);

        // ⭐ Fade in dim stars
        foreach (var star in stars)
        {
            CanvasGroup cg = star.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0;
                cg.DOFade(1f, 0.3f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // ⭐ Activate overlays with pop
        int starCount = GetStarCount(accuracy);

        for (int i = 0; i < starCount; i++)
        {
            Transform overlay = stars[i].GetChild(0);

            overlay.gameObject.SetActive(true);
            overlay.localScale = Vector3.zero;

            overlay.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);

        // 🔘 Buttons pop
        if(accuracy < 100)
            playAgainBtn.SetActive(true);


        mainMenuBtn.SetActive(true);

        playAgainBtn.transform.localScale = Vector3.zero;
        mainMenuBtn.transform.localScale = Vector3.zero;

        playAgainBtn.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        mainMenuBtn.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    // =========================
    // TEXT LOGIC
    // =========================

    void GetTextByAccuracy(float accuracy, out string heading, out string sub)
    {
        if (accuracy < 50f)
        {
            heading = "Nice Try";
            sub = "Take your time and try again!";
        }
        else if (accuracy < 100f)
        {
            heading = "Great Job!";
            sub = "You're very close to perfect!";
        }
        else
        {
            heading = "Amazing!";
            sub = "You remembered everything!";
        }
    }

    int GetStarCount(float accuracy)
    {
        if (accuracy < 50f) return 1;
        if (accuracy < 80f) return 2;
        return 3;
    }

    IEnumerator TypeText(TextMeshProUGUI textUI, string line)
    {
        textUI.text = "";

        foreach (char c in line)
        {
            textUI.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("Loader Scene");
        UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");
        GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");
    }
}