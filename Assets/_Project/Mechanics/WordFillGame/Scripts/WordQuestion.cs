using UnityEngine;

[System.Serializable]
public class WordQuestion
{
    [Header("Visual")]
    public Sprite questionSprite;

    [Header("Hidden Hint")]
    [TextArea(2, 4)]
    public string clueText;

    [Header("Answer")]
    [Tooltip("Write full answer here. Example: brave, blissful, creative.")]
    public string answerWord;

    [Tooltip("Optional. If empty, script will create line like: I am brave.")]
    public string completedLineText;

    [Header("Narration")]
    [Tooltip("Audio clip that reads the completed line. Example: I am brave.")]
    public AudioClip completedLineNarration;

    [Header("Scoring")]
    public int points = 10;

    [Header("Letter Options")]
    [Tooltip("Extra wrong letters mixed with correct letters.")]
    [Min(0)]
    public int extraLetters = 3;

    public string GetCleanAnswer()
    {
        if (string.IsNullOrWhiteSpace(answerWord))
            return string.Empty;

        return answerWord.Trim().ToLowerInvariant();
    }

    public string GetCompletedLine()
    {
        string cleanAnswer = GetCleanAnswer();

        if (!string.IsNullOrWhiteSpace(completedLineText))
            return completedLineText.Trim();

        if (string.IsNullOrEmpty(cleanAnswer))
            return "I am.";

        return "I am " + cleanAnswer + ".";
    }
}
