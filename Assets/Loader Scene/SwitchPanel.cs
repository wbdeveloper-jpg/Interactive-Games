using System.Collections.Generic;
using UnityEngine;

public class SwitchPanel : MonoBehaviour
{
    [SerializeField] private List<GameObject> panels;
    [SerializeField] private int startingPanelId = 0;

    private int currentPanelId = -1;

    private void Awake()
    {
        // Run once to establish a clean initial state.
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null)
                panels[i].SetActive(false);
        }

        ShowPanel(startingPanelId);
    }

    public void ShowPanel(int id)
    {
        if (id < 0 || id >= panels.Count || panels[id] == null)
        {
            Debug.LogWarning($"Invalid panel ID: {id}", this);
            return;
        }

        if (id == currentPanelId)
            return;

        if (currentPanelId >= 0 && panels[currentPanelId] != null)
            panels[currentPanelId].SetActive(false);

        panels[id].SetActive(true);
        currentPanelId = id;
    }

    public void HideCurrentPanel()
    {
        if (currentPanelId < 0)
            return;

        if (panels[currentPanelId] != null)
            panels[currentPanelId].SetActive(false);

        currentPanelId = -1;
    }
}
