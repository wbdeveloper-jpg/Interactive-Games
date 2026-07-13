using UnityEngine;

public class TreasureQuestLevelManager : MonoBehaviour
{
    [Header("Managers")]
    public TreasureQuestGameManager gameManager;
    public TreasureQuestUIManager uiManager;
    public TreasureQuestAudioManager audioManager;

    [Header("Level Settings")]
    [Range(1, 5)] public int totalGates = 5;
    [Range(1, 5)] public int highestUnlockedGate = 1;
    [Range(1, 5)] public int lastSelectedGate = 1;

    [Header("Runtime Completed Gates")]
    public bool[] completedGates = new bool[6];

    public void Initialize(TreasureQuestGameManager game, TreasureQuestUIManager ui, TreasureQuestAudioManager audio)
    {
        gameManager = game;
        uiManager = ui;
        audioManager = audio;

        LoadProgress();
        SetupGateButtons();
        RefreshMenu();
    }

    public void LoadProgress()
    {
        highestUnlockedGate = TreasureQuestSaveManager.LoadHighestUnlockedGate();
        lastSelectedGate = TreasureQuestSaveManager.LoadLastSelectedGate();

        if (completedGates == null || completedGates.Length < totalGates + 1)
            completedGates = new bool[totalGates + 1];

        for (int gate = 1; gate <= totalGates; gate++)
            completedGates[gate] = TreasureQuestSaveManager.LoadGateCompleted(gate);
    }

    public void RefreshMenu()
    {
        if (uiManager == null) return;
        uiManager.RefreshGateButtons(this);
        uiManager.SetTreasureChestUnlocked(IsFinalTreasureUnlocked());
        uiManager.UpdateCoinText(TreasureQuestSaveManager.LoadCoins());
    }

    public void SetupGateButtons()
    {
        if (uiManager == null || uiManager.gateButtons == null) return;

        for (int i = 0; i < uiManager.gateButtons.Length; i++)
        {
            TreasureQuestGateButton gateButton = uiManager.gateButtons[i];
            if (gateButton == null) continue;
            gateButton.Setup(this);
        }
    }

    public TreasureQuestGateState GetGateState(int gateNumber)
    {
        if (gateNumber < 1 || gateNumber > totalGates)
            return TreasureQuestGateState.Locked;

        if (completedGates != null && completedGates.Length > gateNumber && completedGates[gateNumber])
            return TreasureQuestGateState.Completed;

        if (gateNumber <= highestUnlockedGate)
            return TreasureQuestGateState.Unlocked;

        return TreasureQuestGateState.Locked;
    }

    public bool CanPlayGate(int gateNumber)
    {
        return gateNumber >= 1 && gateNumber <= totalGates && gateNumber <= highestUnlockedGate;
    }

    public void TryOpenGate(int gateNumber, TreasureQuestGateButton sourceButton = null)
    {
        if (!CanPlayGate(gateNumber))
        {
            audioManager?.PlayLocked();
            sourceButton?.PlayLockedFeedback();
            uiManager?.ShowLockedGateFeedback("Gate " + gateNumber + " is locked. Complete the previous gate first!");
            return;
        }

        SetLastSelectedGate(gateNumber);
        audioManager?.PlayClick();
        gameManager?.StartGate(gateNumber);
    }

    public void SetLastSelectedGate(int gateNumber)
    {
        lastSelectedGate = Mathf.Clamp(gateNumber, 1, totalGates);
        TreasureQuestSaveManager.SaveLastSelectedGate(lastSelectedGate);
    }

    public int GetPreferredPlayGate()
    {
        if (CanPlayGate(lastSelectedGate))
            return lastSelectedGate;

        return Mathf.Clamp(highestUnlockedGate, 1, totalGates);
    }

    public void CompleteGate(int gateNumber)
    {
        if (gateNumber < 1 || gateNumber > totalGates) return;

        if (completedGates == null || completedGates.Length < totalGates + 1)
            completedGates = new bool[totalGates + 1];

        completedGates[gateNumber] = true;
        TreasureQuestSaveManager.SaveGateCompleted(gateNumber, true);

        if (gateNumber < totalGates)
        {
            int nextGate = gateNumber + 1;
            if (nextGate > highestUnlockedGate)
            {
                highestUnlockedGate = nextGate;
                TreasureQuestSaveManager.SaveHighestUnlockedGate(highestUnlockedGate);
            }
        }
        else
        {
            highestUnlockedGate = totalGates;
            TreasureQuestSaveManager.SaveHighestUnlockedGate(highestUnlockedGate);
        }

        audioManager?.PlayUnlock();
    }

    public bool IsFinalTreasureUnlocked()
    {
        return completedGates != null && completedGates.Length > totalGates && completedGates[totalGates];
    }
}
