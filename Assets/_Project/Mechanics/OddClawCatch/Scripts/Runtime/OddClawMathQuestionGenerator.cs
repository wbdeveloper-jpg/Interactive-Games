using System.Collections.Generic;
using UnityEngine;

public enum OddClawMathMode
{
    OddEven,
    DirectNumber,
    Addition,
    Subtraction,
    Mixed
}

[CreateAssetMenu(menuName = "Odd Claw Catch/Question Generators/Math Generator", fileName = "OddClawMathQuestionGenerator")]
public class OddClawMathQuestionGenerator : OddClawQuestionGeneratorBase
{
    [Header("Math Mode")]
    public OddClawMathMode mode = OddClawMathMode.Mixed;

    [Header("Number Range")]
    public int minNumber = 1;
    public int maxNumber = 30;

    [Header("Difficulty")]
    public int rangeIncreaseEveryWaves = 3;
    public int rangeIncreaseAmount = 5;
    public int maxGeneratedNumber = 99;

    public override OddClawQuestionData GenerateQuestion(int wave, int requestedOptionCount)
    {
        int optionCount = ClampOptionCount(requestedOptionCount);
        OddClawMathMode selectedMode = mode;

        if (mode == OddClawMathMode.Mixed)
        {
            selectedMode = (OddClawMathMode)UnityEngine.Random.Range(0, 4);
        }

        switch (selectedMode)
        {
            case OddClawMathMode.OddEven:
                return GenerateOddEven(wave, optionCount);
            case OddClawMathMode.DirectNumber:
                return GenerateDirectNumber(wave, optionCount);
            case OddClawMathMode.Addition:
                return GenerateAddition(wave, optionCount);
            case OddClawMathMode.Subtraction:
                return GenerateSubtraction(wave, optionCount);
            default:
                return GenerateAddition(wave, optionCount);
        }
    }

    private int CurrentMax(int wave)
    {
        int safeWave = Mathf.Max(1, wave);
        int extra = rangeIncreaseEveryWaves <= 0 ? 0 : ((safeWave - 1) / rangeIncreaseEveryWaves) * rangeIncreaseAmount;
        return Mathf.Clamp(maxNumber + extra, minNumber + 2, maxGeneratedNumber);
    }

    private OddClawQuestionData GenerateOddEven(int wave, int optionCount)
    {
        bool wantsOdd = UnityEngine.Random.Range(0, 2) == 0;
        int high = CurrentMax(wave);
        List<OddClawAnswerOption> options = new List<OddClawAnswerOption>();

        int correct = GetNumberWithParity(minNumber, high, wantsOdd);
        options.Add(new OddClawAnswerOption(correct.ToString()));

        int guard = 0;
        while (options.Count < optionCount && guard < 300)
        {
            guard++;
            int value = GetNumberWithParity(minNumber, high, !wantsOdd);
            string text = value.ToString();
            if (!ContainsText(options, text))
                options.Add(new OddClawAnswerOption(text));
        }

        FillUniqueNumberOptions(options, optionCount, minNumber, high);
        int correctIndex = ShuffleOptionsKeepingCorrect(options, 0);

        return new OddClawQuestionData
        {
            questionText = wantsOdd ? "Catch the ODD number" : "Catch the EVEN number",
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = OddClawAnswerDisplayMode.Text
        };
    }

    private OddClawQuestionData GenerateDirectNumber(int wave, int optionCount)
    {
        int high = CurrentMax(wave);
        int target = UnityEngine.Random.Range(minNumber, high + 1);
        List<OddClawAnswerOption> options = new List<OddClawAnswerOption> { new OddClawAnswerOption(target.ToString()) };
        FillUniqueNumberOptions(options, optionCount, minNumber, high);
        int correctIndex = ShuffleOptionsKeepingCorrect(options, 0);

        return new OddClawQuestionData
        {
            questionText = "Catch number " + target,
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = OddClawAnswerDisplayMode.Text
        };
    }

    private OddClawQuestionData GenerateAddition(int wave, int optionCount)
    {
        int high = Mathf.Max(6, CurrentMax(wave) / 2);
        int a = UnityEngine.Random.Range(1, high + 1);
        int b = UnityEngine.Random.Range(1, high + 1);
        int answer = a + b;

        List<OddClawAnswerOption> options = new List<OddClawAnswerOption> { new OddClawAnswerOption(answer.ToString()) };
        FillNearbyNumberOptions(options, optionCount, answer, 1, Mathf.Min(maxGeneratedNumber, answer + 12));
        int correctIndex = ShuffleOptionsKeepingCorrect(options, 0);

        return new OddClawQuestionData
        {
            questionText = a + " + " + b + " = ?",
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = OddClawAnswerDisplayMode.Text
        };
    }

    private OddClawQuestionData GenerateSubtraction(int wave, int optionCount)
    {
        int high = Mathf.Max(8, CurrentMax(wave));
        int a = UnityEngine.Random.Range(4, high + 1);
        int b = UnityEngine.Random.Range(1, a);
        int answer = a - b;

        List<OddClawAnswerOption> options = new List<OddClawAnswerOption> { new OddClawAnswerOption(answer.ToString()) };
        FillNearbyNumberOptions(options, optionCount, answer, 0, Mathf.Min(maxGeneratedNumber, answer + 12));
        int correctIndex = ShuffleOptionsKeepingCorrect(options, 0);

        return new OddClawQuestionData
        {
            questionText = a + " - " + b + " = ?",
            answerOptions = options,
            correctAnswerIndex = correctIndex,
            displayMode = OddClawAnswerDisplayMode.Text
        };
    }

    private int GetNumberWithParity(int low, int high, bool odd)
    {
        int value = UnityEngine.Random.Range(low, high + 1);
        if ((value % 2 != 0) != odd)
            value += 1;

        if (value > high)
            value -= 2;
        if (value < low)
            value += 2;

        return Mathf.Clamp(value, low, high);
    }

    private void FillUniqueNumberOptions(List<OddClawAnswerOption> options, int optionCount, int low, int high)
    {
        int guard = 0;
        while (options.Count < optionCount && guard < 500)
        {
            guard++;
            int value = UnityEngine.Random.Range(low, high + 1);
            string text = value.ToString();
            if (!ContainsText(options, text))
                options.Add(new OddClawAnswerOption(text));
        }
    }

    private void FillNearbyNumberOptions(List<OddClawAnswerOption> options, int optionCount, int correctValue, int low, int high)
    {
        int guard = 0;
        while (options.Count < optionCount && guard < 500)
        {
            guard++;
            int offset = UnityEngine.Random.Range(-8, 9);
            if (offset == 0)
                continue;

            int value = Mathf.Clamp(correctValue + offset, low, high);
            string text = value.ToString();
            if (!ContainsText(options, text))
                options.Add(new OddClawAnswerOption(text));
        }
    }
}
