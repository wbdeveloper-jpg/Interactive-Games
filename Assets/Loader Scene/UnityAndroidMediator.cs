using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UnityAndroidMediator : MonoBehaviour
{
    public static UnityAndroidMediator Instance;    
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

    // 1 - android app will open
    // 2 - user will click a button in the android app -> data will be sent from native app to unity (instead, unity will ask native app for the data)
    // 3 - the button will open the integrated unity app 
    // 4 - user will use the app and will close it -> data will be sent from unity to native app
    // 5 - the android app will again take over

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private string data;
    public TextMeshProUGUI receivedData;
    private AndroidJavaObject activity;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    StartCoroutine(InitAndroid()); // Don't do JNI calls on main thread directly
#endif
    }

    IEnumerator InitAndroid()
    {
        yield return null; // Wait a frame before touching JNI

        AndroidJavaClass ajc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        activity = ajc.GetStatic<AndroidJavaObject>("currentActivity");

        activity.Call("OnUnityReady");

        data = activity.Call<string>("GetDataForUnity");

        if (!string.IsNullOrEmpty(data))
        {
            receivedData.text = data;
            StartCoroutine(LoadSceneAsync(data)); // async, not sync!
        }
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        yield return null;
        yield return null;
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
            yield return null;
    }

    // PUSH METHOD (Android → Unity)
    // This will be called from Android using UnitySendMessage
    public void ReceiveDataFromAndroid(string jsonData)
    {
        Debug.Log("Data received from Android (Push): " + jsonData);
        receivedData.text = jsonData;
        SceneManager.LoadScene(jsonData);

        // You can parse JSON here if needed
        // Example: JsonUtility.FromJson<YourClass>(jsonData);
    }

    // Send data to Android (Unity → Android)
    public void PassDataToAndroid(string gameData)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (activity != null)
        {
            
            activity.Call("ReturnedDataFromUnity", gameData);
            Debug.Log("Data sent to Android: Game Data");
        }
        else
        {
            Debug.LogWarning("Android activity is null!");
        }
#else
        Debug.Log("Cannot send data - not running on Android device");
#endif
    }
}