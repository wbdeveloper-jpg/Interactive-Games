using UnityEngine;
using UnityEngine.SceneManagement;

public class Redirection : MonoBehaviour
{
    public void LoadGameIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
