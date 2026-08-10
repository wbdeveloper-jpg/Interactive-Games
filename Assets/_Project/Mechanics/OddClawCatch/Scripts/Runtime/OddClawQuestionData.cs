using System.Collections.Generic;
using UnityEngine;

public enum OddClawAnswerDisplayMode
{
    Text = 0,
    Sprite = 1,
    SpriteWithOptionalText = 2
}

[System.Serializable]
public class OddClawAnswerOption
{
    public string text;
    public Sprite sprite;
    public string category;

    public OddClawAnswerOption() { }

    public OddClawAnswerOption(string textValue)
    {
        text = textValue;
    }

    public OddClawAnswerOption(string textValue, Sprite spriteValue, string categoryValue)
    {
        text = textValue;
        sprite = spriteValue;
        category = categoryValue;
    }
}

[System.Serializable]
public class OddClawQuestionData
{
    public string questionText;
    public List<OddClawAnswerOption> answerOptions = new List<OddClawAnswerOption>();
    public int correctAnswerIndex;
    public OddClawAnswerDisplayMode displayMode = OddClawAnswerDisplayMode.Text;

    public bool IsValid(int minimumOptions, out string error)
    {
        if (string.IsNullOrWhiteSpace(questionText))
        {
            error = "Question text is empty.";
            return false;
        }

        if (answerOptions == null || answerOptions.Count < minimumOptions)
        {
            error = "Question does not have enough answer options.";
            return false;
        }

        if (correctAnswerIndex < 0 || correctAnswerIndex >= answerOptions.Count)
        {
            error = "Correct answer index is out of range.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
