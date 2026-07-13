using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourWheelStop
{
    public enum BehaviourWheelQuizMode
    {
        Behaviour,
        General,
        Maths
    }

    public enum BehaviourWheelDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public enum BehaviourWheelMathOperator
    {
        Addition,
        Subtraction,
        Multiplication,
        Division
    }

    [Serializable]
    public class BehaviourWheelOptionData
    {
        public string answerText;
        public Sprite icon;

        public BehaviourWheelOptionData() { }

        public BehaviourWheelOptionData(string answerText)
        {
            this.answerText = answerText;
        }

        public BehaviourWheelOptionData(string answerText, Sprite icon)
        {
            this.answerText = answerText;
            this.icon = icon;
        }
    }

    [Serializable]
    public class BehaviourWheelQuestionData
    {
        [TextArea(2, 4)] public string questionText;
        [Tooltip("Use 3 to 6 options. Icons are optional.")]
        public List<BehaviourWheelOptionData> options = new List<BehaviourWheelOptionData>();
        public string correctAnswer;
        [TextArea(1, 3)] public string explanation;
        public BehaviourWheelDifficulty difficulty = BehaviourWheelDifficulty.Easy;

        public bool HasValidOptions(int minOptionCount, int maxOptionCount)
        {
            if (options == null || string.IsNullOrWhiteSpace(correctAnswer))
                return false;

            int validCount = 0;
            for (int i = 0; i < options.Count && validCount < maxOptionCount; i++)
            {
                if (options[i] != null && !string.IsNullOrWhiteSpace(options[i].answerText))
                    validCount++;
            }

            return validCount >= minOptionCount && GetCorrectIndex(maxOptionCount) >= 0;
        }

        public int GetCorrectIndex(int maxOptionCount = 6)
        {
            if (options == null || string.IsNullOrWhiteSpace(correctAnswer))
                return -1;

            int checkedCount = 0;
            for (int i = 0; i < options.Count && checkedCount < maxOptionCount; i++)
            {
                if (options[i] == null || string.IsNullOrWhiteSpace(options[i].answerText))
                    continue;

                if (string.Equals(options[i].answerText.Trim(), correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                    return checkedCount;

                checkedCount++;
            }

            return -1;
        }
    }

    [Serializable]
    public class BehaviourWheelMathSettings
    {
        [Header("Math Operators")]
        public bool addition = true;
        public bool subtraction = true;
        public bool multiplication = true;
        public bool division = false;

        [Header("Addition / Subtraction Range")]
        [Tooltip("Used only for addition and subtraction.")]
        [Min(1)] public int minDigits = 1;
        [Range(1, 4)] public int maxDigits = 2;
        [Min(1)] public int minNumber = 1;
        [Min(2)] public int maxNumber = 50;
        public bool keepSubtractionPositive = true;

        [Header("Multiplication Factor Sizes")]
        [Tooltip("A in A x B. Example: 2 digits means 10 to 99.")]
        [Range(1, 4)] public int multiplicationLeftMinDigits = 1;
        [Range(1, 4)] public int multiplicationLeftMaxDigits = 2;
        [Tooltip("B in A x B. Keep this smaller for kid-friendly tables.")]
        [Range(1, 4)] public int multiplicationRightMinDigits = 1;
        [Range(1, 4)] public int multiplicationRightMaxDigits = 1;

        [Header("Division Number Sizes")]
        [Tooltip("A in A ÷ B. This is the dividend.")]
        [Range(1, 4)] public int divisionDividendMinDigits = 1;
        [Range(1, 4)] public int divisionDividendMaxDigits = 2;
        [Tooltip("B in A ÷ B. This is the divisor / lower number.")]
        [Range(1, 4)] public int divisionDivisorMinDigits = 1;
        [Range(1, 4)] public int divisionDivisorMaxDigits = 1;
        public bool divisionAnswersWholeNumber = true;

        [Header("Generated Wheel Options")]
        [Range(3, 6)] public int optionCount = 4;
        [Min(1)] public int wrongAnswerSpread = 12;
    }
}
