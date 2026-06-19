using System.Collections.Generic;
using UnityEngine;

public abstract class OddClawQuestionGeneratorBase : ScriptableObject
{
    [Header("Generator Rules")]
    [Min(2)] public int minimumOptions = 2;
    [Min(2)] public int maximumOptions = 6;

    public abstract OddClawQuestionData GenerateQuestion(int wave, int requestedOptionCount);

    protected int ClampOptionCount(int requestedOptionCount)
    {
        return Mathf.Clamp(requestedOptionCount, minimumOptions, maximumOptions);
    }

    protected int ShuffleOptionsKeepingCorrect(List<OddClawAnswerOption> options, int correctIndex)
    {
        if (options == null || options.Count == 0)
            return 0;

        OddClawAnswerOption correct = options[Mathf.Clamp(correctIndex, 0, options.Count - 1)];

        for (int i = 0; i < options.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, options.Count);
            OddClawAnswerOption temp = options[i];
            options[i] = options[swapIndex];
            options[swapIndex] = temp;
        }

        return options.IndexOf(correct);
    }

    protected bool ContainsText(List<OddClawAnswerOption> options, string value)
    {
        if (options == null)
            return false;

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].text == value)
                return true;
        }

        return false;
    }
}
