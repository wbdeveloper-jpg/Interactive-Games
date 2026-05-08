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

    [SerializeField] private string message = "Hey Buddy, Help Mowgli to find my honey!";

    private void Start()
    {
        SetCleanState(false);
    }

    public IEnumerator PlayIntro()
    {
        SetCleanState(true);

        if (panel == null)
        {
            yield break;
        }

        panel.alpha = 0f;
        yield return panel.DOFade(1f, 0.4f).SetLink(panel.gameObject).WaitForCompletion();

        yield return PopIn(baloo, 0.35f);
        yield return new WaitForSeconds(0.1f);
        yield return PopIn(thoughtBubble, 0.3f);
        yield return TypeText(message, 5);
        yield return PopIn(honey, 0.3f);

        yield return new WaitForSeconds(2f);

        yield return panel.DOFade(0f, 0.4f).SetLink(panel.gameObject).WaitForCompletion();
        panel.gameObject.SetActive(false);
    }

    private void SetCleanState(bool showPanel)
    {
        SafeSetActive(baloo, false);
        SafeSetActive(thoughtBubble, false);
        SafeSetActive(honey, false);

        if (text != null)
        {
            text.text = string.Empty;
        }

        if (panel != null)
        {
            panel.alpha = 0f;
            panel.gameObject.SetActive(showPanel);
        }
    }

    private IEnumerator PopIn(GameObject obj, float duration)
    {
        if (obj == null)
        {
            yield break;
        }

        obj.SetActive(true);
        obj.transform.DOKill(false);
        obj.transform.localScale = Vector3.zero;

        yield return obj.transform.DOScale(1f, duration)
            .SetEase(Ease.OutBack)
            .SetLink(obj)
            .WaitForCompletion();
    }

    private IEnumerator TypeText(string msg, int clipNo)
    {
        if (text == null)
        {
            yield break;
        }

        text.text = string.Empty;

        if (string.IsNullOrEmpty(msg))
        {
            yield break;
        }

        float delay = Mathf.Max(0.001f, typeSpeed);
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null && audioManager.HasSFXClip(clipNo))
        {
            AudioClip clip = audioManager.sfxClips[clipNo];
            delay = Mathf.Max(0.001f, clip.length / msg.Length);
            audioManager.PlaySFX(clipNo);
        }

        foreach (char c in msg)
        {
            text.text += c;
            yield return new WaitForSeconds(delay);
        }
    }

    private void SafeSetActive(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    private void OnValidate()
    {
        typeSpeed = Mathf.Max(0.001f, typeSpeed);
    }
}
