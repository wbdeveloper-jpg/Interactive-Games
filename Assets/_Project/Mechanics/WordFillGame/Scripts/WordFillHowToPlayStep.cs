using UnityEngine;

[System.Serializable]
public class WordFillHowToPlayStep
{
    [TextArea(2, 4)]
    public string instructionText;

    public Sprite instructionImage;
}
