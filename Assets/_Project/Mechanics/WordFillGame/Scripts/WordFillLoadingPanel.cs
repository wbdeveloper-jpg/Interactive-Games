using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordFillLoadingPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text gameNameText;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TMP_Text loadingLineText;

    [Header("Settings")]
    [SerializeField] private string gameName = "Affirmation Words";
    [SerializeField] private string loadingBaseText = "Loading";
    [SerializeField] private float loadingDuration = 1.5f;
    [SerializeField] private float dotAnimationSpeed = 0.35f;

    private Action onComplete;

    private void Awake()
    {
        CloseInstant();
    }

    public void Open(Action completeCallback)
    {
        onComplete = completeCallback;

        if (gameNameText != null)
            gameNameText.text = gameName;

        if (loadingSlider != null)
            loadingSlider.value = 0f;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        StartCoroutine(LoadingRoutine());
    }

    private IEnumerator LoadingRoutine()
    {
        float elapsed = 0f;
        float dotTimer = 0f;
        int dotCount = 0;

        while (elapsed < loadingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            dotTimer += Time.unscaledDeltaTime;

            if (loadingSlider != null)
                loadingSlider.value = Mathf.Clamp01(elapsed / loadingDuration);

            if (dotTimer >= dotAnimationSpeed)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;
                UpdateLoadingLine(dotCount);
            }

            yield return null;
        }

        if (loadingSlider != null)
            loadingSlider.value = 1f;

        UpdateLoadingLine(3);

        yield return new WaitForSecondsRealtime(0.15f);

        CloseInstant();
        onComplete?.Invoke();
        onComplete = null;
    }

    private void UpdateLoadingLine(int dotCount)
    {
        if (loadingLineText == null)
            return;

        loadingLineText.text = loadingBaseText + new string('.', dotCount);
    }

    public void CloseInstant()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
