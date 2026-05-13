using UnityEngine;

[CreateAssetMenu(menuName = "Zodiac Puzzle/Zodiac Database", fileName = "ZodiacDatabase")]
public class ZodiacDatabase : ScriptableObject
{
    public ZodiacPuzzleData[] allZodiacs = new ZodiacPuzzleData[12];

    public ZodiacPuzzleData GetData(ZodiacSign sign)
    {
        if (allZodiacs == null) return null;

        for (int i = 0; i < allZodiacs.Length; i++)
        {
            ZodiacPuzzleData data = allZodiacs[i];
            if (data != null && data.sign == sign)
            {
                return data;
            }
        }

        return null;
    }
}
