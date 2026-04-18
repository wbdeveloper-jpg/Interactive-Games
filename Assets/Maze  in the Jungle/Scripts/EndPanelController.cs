using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class EndPanelController : MonoBehaviour
{
    public CanvasGroup panel;

    public GameObject baloo;
    public GameObject thoughtBubble;
    public TextMeshProUGUI text;
    public GameObject honey;

    public Button continueBtn;

    public float typeSpeed = 0.04f;

    private string message = "Yay! Thanks to you, Mowgli found the honey!";

    public IEnumerator PlayOutro()
    {
        // 🔴 FORCE CLEAN STATE
        baloo.SetActive(false);
        thoughtBubble.SetActive(false);
        honey.SetActive(false);
        continueBtn.gameObject.SetActive(false);
        text.text = "";

        panel.gameObject.SetActive(true);
        panel.alpha = 0;

        // ---- PANEL FADE ----
        yield return panel.DOFade(1, 0.4f).WaitForCompletion();
        AudioManager.Instance.PlaySFX(2);
        // ---- STEP 1: BALOO POP ----
        baloo.SetActive(true);
        baloo.transform.localScale = Vector3.zero;

        yield return baloo.transform.DOScale(1f, 0.35f)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        yield return new WaitForSeconds(0.1f);

        // ---- STEP 2: THOUGHT BUBBLE POP ----
        thoughtBubble.SetActive(true);
        thoughtBubble.transform.localScale = Vector3.zero;

        yield return thoughtBubble.transform.DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        // ---- STEP 3: TEXT TYPE ----
        //AudioManager.Instance.PlaySFX(6);
        yield return TypeText(message,6);

        // ---- STEP 4: HONEY POP ----
        honey.SetActive(true);
        honey.transform.localScale = Vector3.zero;

        yield return honey.transform.DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        yield return new WaitForSeconds(0.5f);

        // ---- STEP 5: CONTINUE BUTTON ----
        continueBtn.gameObject.SetActive(true);
        continueBtn.transform.localScale = Vector3.one;

        continueBtn.transform.DOScale(1.05f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        bool clicked = false;
        continueBtn.onClick.RemoveAllListeners();
        continueBtn.onClick.AddListener(() => clicked = true);

        yield return new WaitUntil(() => clicked);
    }

    IEnumerator TypeText(string msg, int clipNo)
    {
        text.text = "";


        AudioClip clip = AudioManager.Instance.sfxClips[clipNo];
        float delay = clip.length / msg.Length;

        AudioManager.Instance.PlaySFX(clipNo);

        foreach (char c in msg)
        {
            text.text += c;
            yield return new WaitForSeconds(delay);
        }
    }
}