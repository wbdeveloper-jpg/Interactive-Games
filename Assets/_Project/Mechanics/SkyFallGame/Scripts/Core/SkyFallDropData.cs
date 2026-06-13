using UnityEngine;

[System.Serializable]
public class SkyFallDropData
{
    [Header("Visual")]
    public string displayText;
    public Sprite sprite;

    [Header("Audio")]
    public AudioClip audioClip;

    [Header("Rule")]
    public bool isCorrect;
}

public struct SkyFallDropContext
{
    public int score;
    public int correctCaught;
    public int wrongCaught;
    public int missedCorrect;
    public float elapsedTime;
    public float progress01;
}
