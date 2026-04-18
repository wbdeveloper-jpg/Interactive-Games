using UnityEngine;
using DG.Tweening;
using TMPro;
using System.Collections;

public class IntroPanelController : MonoBehaviour
{
    public CanvasGroup panel;

    public GameObject baloo;
    public GameObject thoughtBubble;
    public TextMeshProUGUI text;
    public GameObject honey;

    public float typeSpeed = 0.04f;

    private string message = "Hey Buddy, Help Mowgli to find my honey!";

    private void Start()
    {
        // 🔴 FORCE CLEAN STATE
        baloo.SetActive(false);
        thoughtBubble.SetActive(false);
        honey.SetActive(false);
        text.text = "";
        panel.alpha = 0;
    }

    public IEnumerator PlayIntro()
    {
        // 🔴 FORCE CLEAN STATE
        baloo.SetActive(false);
        thoughtBubble.SetActive(false);
        honey.SetActive(false);
        text.text = "";

        // ---- PANEL FADE ----
        panel.alpha = 0;
        yield return panel.DOFade(1, 0.4f).WaitForCompletion();

        // ---- STEP 1: BALOO POP (APPEARS NOW) ----
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
        //AudioManager.Instance.PlaySFX(5);
        yield return TypeText(message, 5);

        // ---- STEP 4: HONEY POP ----
        honey.SetActive(true);
        honey.transform.localScale = Vector3.zero;

        yield return honey.transform.DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        yield return new WaitForSeconds(2f);

        // ---- STEP 5: FADE OUT ----
        yield return panel.DOFade(0, 0.4f).WaitForCompletion();

        panel.gameObject.SetActive(false);
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