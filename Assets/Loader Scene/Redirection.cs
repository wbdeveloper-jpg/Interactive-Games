using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class Redirection : MonoBehaviour
{
    [Header("Loading UI")]
    public GameObject loadingScreen;      // A UI Panel for the loading overlay
    public Slider progressBar;            // Optional: progress bar (0 to 1)
    public TextMeshProUGUI loadingText;              // Optional: "Loading... 72%"

    public void LoadGameIndex(int sceneIndex)
    {
        StartCoroutine(LoadSceneAsync(sceneIndex));
    }

    private IEnumerator LoadSceneAsync(int sceneIndex)
    {
        // Show loading screen
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Begin loading in background (don't activate yet)
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;  // Pause at 90% until we're ready

        while (!operation.isDone)
        {
            // Unity loads to 0.9 then waits if allowSceneActivation = false
            float progress = Mathf.Clamp01(operation.progress / 0.9f);  // Normalize to 0-1

            // Update UI
            if (progressBar != null)
                progressBar.value = progress;

            if (loadingText != null)
                loadingText.text = $"Loading... {(int)(progress * 100)}%";

            // Once fully loaded (progress hits 1.0 after normalization)
            if (operation.progress >= 0.9f)
            {
                // Optional: wait a moment so user sees 100%
                yield return new WaitForSeconds(0.2f);

                // Now actually switch the scene
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}