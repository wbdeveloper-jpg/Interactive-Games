using UnityEngine;

public class SwitchPanel : MonoBehaviour
{
    public GameObject ClassThreeEnglishPanel;
    public GameObject AllGamesPanel;


    public void SwitchToMain()
    {
        ClassThreeEnglishPanel.SetActive(false);
        AllGamesPanel.SetActive(true); 
    }

    public void SwitchToClassThree()
    {
        AllGamesPanel.SetActive(false);
        ClassThreeEnglishPanel.SetActive(true);
    }
}
