using System.Collections;
using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    [Min(0f)] public float fadeDuration = 0.25f;
    public GameObject currentPanel;
    public bool useUnscaledTime = true;

    private Coroutine switchRoutine;

    public void Switch(GameObject newPanel)
    {
        if (newPanel == null)
        {
            Debug.LogWarning("PanelSwitcher: newPanel is null.", this);
            return;
        }

        if (switchRoutine != null)
            StopCoroutine(switchRoutine);

        switchRoutine = StartCoroutine(SwitchRoutine(newPanel));
    }

    private IEnumerator SwitchRoutine(GameObject newPanel)
    {
        if (currentPanel == newPanel)
        {
            PreparePanel(GetCanvasGroup(newPanel), true);
            switchRoutine = null;
            yield break;
        }

        if (currentPanel != null)
        {
            CanvasGroup oldGroup = GetCanvasGroup(currentPanel);
            PreparePanel(oldGroup, false);
            yield return Fade(oldGroup, oldGroup.alpha, 0f);
            currentPanel.SetActive(false);
        }

        CanvasGroup newGroup = GetCanvasGroup(newPanel);
        newPanel.SetActive(true);
        PreparePanel(newGroup, false);
        newGroup.alpha = 0f;

        yield return Fade(newGroup, 0f, 1f);

        PreparePanel(newGroup, true);
        currentPanel = newPanel;
        switchRoutine = null;
    }

    private IEnumerator Fade(CanvasGroup canvasGroup, float from, float to)
    {
        if (canvasGroup == null)
            yield break;

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private static void PreparePanel(CanvasGroup canvasGroup, bool enabled)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    private static CanvasGroup GetCanvasGroup(GameObject panel)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : panel.AddComponent<CanvasGroup>();
    }

    private void OnDisable()
    {
        if (switchRoutine != null)
        {
            StopCoroutine(switchRoutine);
            switchRoutine = null;
        }
    }
}
