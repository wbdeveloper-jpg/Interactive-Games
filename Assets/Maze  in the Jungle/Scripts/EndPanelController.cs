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

    [SerializeField] private string message = "Yay! Thanks to you, Mowgli found the honey!";
    [SerializeField] private bool waitForContinueButton = false;

    public IEnumerator PlayOutro()
    {
        SetCleanState();

        if (panel == null)
        {
            yield break;
        }

        panel.gameObject.SetActive(true);
        panel.alpha = 0f;

        yield return panel.DOFade(1f, 0.4f).SetLink(panel.gameObject).WaitForCompletion();
        PlaySfx(2);

        yield return PopIn(baloo, 0.35f);
        yield return new WaitForSeconds(0.1f);
        yield return PopIn(thoughtBubble, 0.3f);
        yield return TypeText(message, 6);
        yield return PopIn(honey, 0.3f);
        yield return new WaitForSeconds(0.5f);

        if (waitForContinueButton && continueBtn != null)
        {
            yield return WaitForContinue();
        }
    }

    private void SetCleanState()
    {
        SafeSetActive(baloo, false);
        SafeSetActive(thoughtBubble, false);
        SafeSetActive(honey, false);

        if (continueBtn != null)
        {
            continueBtn.gameObject.SetActive(false);
            continueBtn.onClick.RemoveAllListeners();
        }

        if (text != null)
        {
            text.text = string.Empty;
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

    private IEnumerator WaitForContinue()
    {
        bool clicked = false;
        continueBtn.gameObject.SetActive(true);
        continueBtn.transform.localScale = Vector3.one;

        continueBtn.transform.DOKill(false);
        continueBtn.transform.DOScale(1.05f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(continueBtn.gameObject);

        continueBtn.onClick.RemoveAllListeners();
        continueBtn.onClick.AddListener(() => clicked = true);

        yield return new WaitUntil(() => clicked);

        continueBtn.transform.DOKill(false);
        continueBtn.onClick.RemoveAllListeners();
    }

    private void PlaySfx(int clipNo)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clipNo);
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
