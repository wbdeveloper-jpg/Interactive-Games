using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Redirection : MonoBehaviour
{
    [Header("Loading UI")]
    public GameObject loadingScreen;
    public Slider progressBar;
    public TextMeshProUGUI loadingText;

    // Load using Build Index
    public void LoadGameIndex(int sceneIndex)
    {
        StartCoroutine(LoadSceneAsync(sceneIndex));
    }

    // Load using Scene Name
    public void LoadGameScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        yield return StartCoroutine(HandleSceneLoading(operation));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        yield return StartCoroutine(HandleSceneLoading(operation));
    }

    private IEnumerator HandleSceneLoading(AsyncOperation operation)
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            if (loadingText != null)
                loadingText.text = $"Loading... {(int)(progress * 100)}%";

            if (operation.progress >= 0.9f)
            {
                if (progressBar != null)
                    progressBar.value = 1f;

                if (loadingText != null)
                    loadingText.text = "Loading... 100%";

                yield return new WaitForSecondsRealtime(0.2f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}