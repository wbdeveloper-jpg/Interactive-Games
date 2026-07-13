using UnityEngine;

public static class TreasureQuestSaveManager
{
    private const string Prefix = "TreasureQuest_";
    private const string HighestUnlockedGateKey = Prefix + "HighestUnlockedGate";
    private const string CoinKey = Prefix + "Coins";
    private const string LastSelectedGateKey = Prefix + "LastSelectedGate";
    private const string HowToPlaySeenKey = Prefix + "HowToPlaySeen";

    public static int LoadHighestUnlockedGate()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(HighestUnlockedGateKey, 1), 1, 5);
    }

    public static void SaveHighestUnlockedGate(int gate)
    {
        PlayerPrefs.SetInt(HighestUnlockedGateKey, Mathf.Clamp(gate, 1, 5));
        PlayerPrefs.Save();
    }

    public static bool LoadGateCompleted(int gateNumber)
    {
        return PlayerPrefs.GetInt(GetCompletedKey(gateNumber), 0) == 1;
    }

    public static void SaveGateCompleted(int gateNumber, bool completed)
    {
        PlayerPrefs.SetInt(GetCompletedKey(gateNumber), completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static int LoadCoins()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(CoinKey, 0));
    }

    public static void SaveCoins(int coins)
    {
        PlayerPrefs.SetInt(CoinKey, Mathf.Max(0, coins));
        PlayerPrefs.Save();
    }

    public static int LoadLastSelectedGate()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(LastSelectedGateKey, 1), 1, 5);
    }

    public static void SaveLastSelectedGate(int gate)
    {
        PlayerPrefs.SetInt(LastSelectedGateKey, Mathf.Clamp(gate, 1, 5));
        PlayerPrefs.Save();
    }

    public static bool HasSeenHowToPlay()
    {
        return PlayerPrefs.GetInt(HowToPlaySeenKey, 0) == 1;
    }

    public static void SaveHowToPlaySeen()
    {
        PlayerPrefs.SetInt(HowToPlaySeenKey, 1);
        PlayerPrefs.Save();
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(HighestUnlockedGateKey);
        PlayerPrefs.DeleteKey(CoinKey);
        PlayerPrefs.DeleteKey(LastSelectedGateKey);
        PlayerPrefs.DeleteKey(HowToPlaySeenKey);

        for (int i = 1; i <= 5; i++)
            PlayerPrefs.DeleteKey(GetCompletedKey(i));

        PlayerPrefs.Save();
    }

    private static string GetCompletedKey(int gateNumber)
    {
        return Prefix + "Gate_" + gateNumber + "_Completed";
    }
}
