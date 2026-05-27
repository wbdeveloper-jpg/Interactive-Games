using System;
using UnityEngine;

[Serializable]
public class SentenceWordSearchQuestion
{
    [TextArea(2, 4)]
    public string sentenceWithBlank = "The wind is _________.";

    public string answer = "STRONG";

    [Header("Optional Visual / Audio")]
    public Sprite questionSprite;
    public AudioClip narrationAudio;
}

public enum SentenceWordSearchDifficulty
{
    Easy,
    Medium,
    Hard
}
