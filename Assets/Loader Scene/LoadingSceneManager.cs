/*
 * ============================================================
 * LoadingSceneManager.cs  —  MonoBehaviour on Loading Scene
 * ============================================================
 * SUMMARY:
 *   Handles the entry point of the application.
 *   Responsible for:
 *     1. Ensuring RewardManager is initialized before any scene loads
 *     2. Reading the target scene name (from Android/iOS deep link,
 *        WebGL URL parameter, or any other source)
 *     3. Loading the target game scene
 *
 * SETUP:
 *   • Place the RewardManager prefab in THIS scene (Loading Scene).
 *   • Attach this script to a LoadingManager GameObject.
 *   • Assign rewardManagerPrefab in inspector as a safety fallback.
 *   • Set defaultScene for editor testing.
 *
 * SCENE NAME PASSING:
 *   Android/iOS module: pass via PlayerPrefs or static field before loading
 *   WebGL: pass via URL ?scene=SceneName parameter
 *   Both methods shown below — comment out whichever you don't need.
 * ============================================================
 */

using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;

public class LoadingSceneManager : MonoBehaviour
{
    [Header("Reward System Prefab (safety fallback)")]
    [SerializeField] private GameObject rewardManagerPrefab;

    [Header("Fallback scene for editor testing")]
    [SerializeField] private string defaultScene = "Game_ColorFilling";

    // Static field — Android/iOS native layer or another scene sets this
    // before loading the loading scene
    public static string TargetSceneName = "";

    private void Awake()
    {
        // Safety: if somehow RewardManager wasn't in scene, spawn it
        if (RewardManager.Instance == null && rewardManagerPrefab != null)
        {
            Instantiate(rewardManagerPrefab);
        }
    }

    private void Start()
    {
        //string sceneName = ResolveTargetScene();

        //if (string.IsNullOrEmpty(sceneName))
        //{
        //    Debug.LogError("[LoadingScene] No target scene found. Using default.");
        //    sceneName = defaultScene;
        //}

        //Debug.Log($"[LoadingScene] Loading scene: {sceneName}");
        //SceneManager.LoadScene(sceneName);
    }

    // ── Scene Name Resolution ─────────────────────────────────

    private string ResolveTargetScene()
    {
        // Priority 1: Static field set by native layer (Android/iOS module)
        if (!string.IsNullOrEmpty(TargetSceneName))
            return TargetSceneName;

        // Priority 2: PlayerPrefs (set by native Android/iOS before launching)
        string fromPrefs = PlayerPrefs.GetString("TargetScene", "");
        if (!string.IsNullOrEmpty(fromPrefs))
            return fromPrefs;

        // Priority 3: WebGL URL parameter (?scene=SceneName)
        #if UNITY_WEBGL && !UNITY_EDITOR
        string fromUrl = GetWebGLSceneParam();
        if (!string.IsNullOrEmpty(fromUrl))
            return fromUrl;
        #endif

        return "";
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
    // Reads ?scene=XXXX from the browser URL
    // Requires a matching jslib plugin or use Application.absoluteURL
    private string GetWebGLSceneParam()
    {
        string url = Application.absoluteURL;
        int idx = url.IndexOf("?scene=");
        if (idx < 0) return "";

        string raw = url.Substring(idx + 7);
        // Strip any further query params
        int ampIdx = raw.IndexOf('&');
        if (ampIdx >= 0) raw = raw.Substring(0, ampIdx);

        return UnityEngine.Networking.UnityWebRequest.UnEscapeURL(raw);
    }
    #endif

    // ── Static helper — call from native layer ─────────────────

    /// <summary>
    /// Call this from Android/iOS plugin or any external entry point
    /// to set the target scene before the loading scene starts.
    /// </summary>
    public static void SetTargetScene(string sceneName)
    {
        TargetSceneName = sceneName;
        PlayerPrefs.SetString("TargetScene", sceneName);
        PlayerPrefs.Save();
    }
}
