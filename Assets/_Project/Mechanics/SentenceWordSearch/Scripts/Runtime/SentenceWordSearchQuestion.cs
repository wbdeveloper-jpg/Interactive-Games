using UnityEngine;

[System.Serializable]
public class SentenceWordSearchQuestion
{
    [Header("Sentence")]
    [TextArea(2, 4)]
    public string sentenceWithBlank = "The wind is _________.";

    [Tooltip("The exact missing word the player must find in the fixed grid.")]
    public string answer = "STRONG";

    [Header("Optional Visual / Audio")]
    public Sprite questionSprite;
    public AudioClip narrationClip;
}
