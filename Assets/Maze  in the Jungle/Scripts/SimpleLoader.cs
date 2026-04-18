using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class SimpleLoader : MonoBehaviour
{
    public Image fillImage;
    public RectTransform circle;
    public CanvasGroup loaderCanvas;

    public float loadDuration = 2f;

    private float width;

    // 🔥 Gameplay
    public GameObject player;
    public GameObject[] gameObjects;
    public GameObject controlsUI;

    // 🔥 NEW: Intro Panel
    public IntroPanelController introPanel;

    void Start()
    {
        width = ((RectTransform)fillImage.transform.parent).rect.width;

        player.SetActive(false);
        controlsUI.SetActive(false);

        foreach (var obj in gameObjects)
            obj.SetActive(false);

        StartCoroutine(StartLoading());
    }

    IEnumerator StartLoading()
    {
        AudioManager.Instance.PlayBGM(1, false);

        fillImage.fillAmount = 0;

        DOTween.To(() => fillImage.fillAmount, x => fillImage.fillAmount = x, 1f, loadDuration)
            .SetEase(Ease.Linear)
            .SetLink(gameObject);

        circle.DOAnchorPosX(width - 450f, loadDuration)
            .SetEase(Ease.Linear)
            .SetLink(gameObject);

        yield return new WaitForSeconds(loadDuration);

        yield return IntroSequence();
    }

    IEnumerator IntroSequence()
    {
        // 🔥 Stop loader tweens
        DOTween.Kill(circle);
        DOTween.Kill(gameObject);

        // ---- Fade Loader ----
        yield return loaderCanvas.DOFade(0f, 0.5f)
            .SetEase(Ease.InOutCubic)
            .WaitForCompletion();

        loaderCanvas.gameObject.SetActive(false);

        // 🔥 Intro Panel (NEW FLOW)
        yield return introPanel.PlayIntro();

        // 🔥 Start game BGM
        AudioManager.Instance.PlayBGM(0, true);

        // ---- GAME ENTRY ----
        player.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        foreach (var obj in gameObjects)
        {
            obj.SetActive(true);
            obj.transform.localScale = Vector3.zero;

            obj.transform.DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack)
                .SetLink(obj);

            yield return new WaitForSeconds(0.15f);
        }

        controlsUI.SetActive(true);

        CanvasGroup cg = controlsUI.GetComponent<CanvasGroup>();
        if (cg == null) cg = controlsUI.AddComponent<CanvasGroup>();

        cg.alpha = 0f;

        yield return cg.DOFade(1f, 0.4f)
            .SetEase(Ease.OutCubic)
            .WaitForCompletion();
    }
}