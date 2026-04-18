using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameLoader : MonoBehaviour
{
    public static GameLoader Instance;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("Loader Started");

        // Try loading from URL first
        string gameFromURL = GetGameFromURL();
        //LoadGame("Maze in The Jungle");
        //if (!string.IsNullOrEmpty(gameFromURL))
        //{
        //    LoadGame(gameFromURL);
        //}
        //else
        //{
        //    Debug.Log("No game passed, staying in loader.");
        //}
    }

    // 🌐 Get game from URL
    string GetGameFromURL()
    {
        string url = Application.absoluteURL;

        if (url.Contains("game="))
        {
            int index = url.IndexOf("game=") + 5;
            string game = url.Substring(index);

            // remove extra params if any
            int ampIndex = game.IndexOf("&");
            if (ampIndex != -1)
                game = game.Substring(0, ampIndex);

            Debug.Log("Game from URL: " + game);
            return game;
        }

        return "";
    }

    // 🎯 PUBLIC FUNCTION (JS can call this)
    public void LoadGame(string sceneName)
    {
        Debug.Log("Loading Game: " + sceneName);
        
        //SceneManager.LoadScene(sceneName);  
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public void LoadGameIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex); 
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        if (op == null)
        {
            SendEventToJS("LOAD_FAILED", sceneName);
            yield break;
        }

        while (!op.isDone)
        {
            yield return null;
        }

        Debug.Log("Game Loaded: " + sceneName);
        SendEventToJS("LOAD_SUCCESS", sceneName);
    }

    // 🔌 Send message back to JS
    public void SendEventToJS(string type, string scene)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalCall("onUnityEvent", type, scene);
#endif
    }
}