using System.Collections.Generic;
using UnityEngine;

namespace OddSuckMechanic
{
    public enum OddSuckMathChallengeMode
    {
        OddAmongEven,
        EvenAmongOdd,
        RandomOddEven
    }

    public enum OddSuckMathExpressionMode
    {
        DirectNumber = 0,
        Addition = 1,
        Subtraction = 2,
        Mixed = 3,
        Multiplication = 4,
        Division = 5,
        AdditionAndSubtraction = 6,
        MultiplicationAndDivision = 7
    }

    public class OddSuckMathQuestionGenerator : OddSuckQuestionGeneratorBase
    {
        [Header("Math Rules")]
        [SerializeField] private OddSuckMathChallengeMode challengeMode = OddSuckMathChallengeMode.RandomOddEven;
        [SerializeField] private OddSuckMathExpressionMode expressionMode = OddSuckMathExpressionMode.Mixed;
        [Tooltip("Used by Direct Number, Addition, and Subtraction modes.")]
        [SerializeField, Min(0)] private int minResultValue = 1;
        [Tooltip("Used by Direct Number, Addition, and Subtraction modes.")]
        [SerializeField, Min(2)] private int maxResultValue = 50;
        [Tooltip("Used by Addition and Subtraction modes.")]
        [SerializeField, Min(1)] private int maxOperandValue = 30;
        [SerializeField] private bool avoidDuplicateAnswers = true;

        [Header("Number Table Rules")]
        [Tooltip("Highest table children are expected to know, such as 12, 15, or 20. This controls the first multiplication number and the division divisor.")]
        [SerializeField, Min(2)] private int maximumTableNumber = 12;
        [Tooltip("Number of steps in each table. Indian school tables normally run from ×1 through ×10.")]
        [SerializeField, Range(1, 10)] private int maximumTableMultiplier = 10;
        [SerializeField] private string multiplicationSymbol = "×";
        [SerializeField] private string divisionSymbol = "÷";

        public override bool CanGenerate()
        {
            if (expressionMode == OddSuckMathExpressionMode.Multiplication
                || expressionMode == OddSuckMathExpressionMode.Division
                || expressionMode == OddSuckMathExpressionMode.MultiplicationAndDivision)
            {
                return maximumTableNumber >= 2;
            }

            return maxResultValue > minResultValue;
        }

        public override OddSuckGeneratedQuestion Generate(int waveIndex)
        {
            bool oddItemShouldBeOdd = ResolveOddItemParity();
            int itemCount = GetRandomItemCount();

            OddSuckGeneratedQuestion question = new OddSuckGeneratedQuestion
            {
                displayMode = OddSuckItemDisplayMode.Text,
                questionText = oddItemShouldBeOdd ? "Pick the odd number" : "Pick the even number"
            };

            HashSet<int> usedResults = new HashSet<int>();
            bool majorityNeedsOdd = !oddItemShouldBeOdd;

            for (int i = 0; i < itemCount - 1; i++)
            {
                question.items.Add(CreateMathItem(majorityNeedsOdd, false, usedResults));
            }

            question.items.Add(CreateMathItem(oddItemShouldBeOdd, true, usedResults));
            Shuffle(question.items);
            return question;
        }

        private bool ResolveOddItemParity()
        {
            if (challengeMode == OddSuckMathChallengeMode.OddAmongEven)
            {
                return true;
            }

            if (challengeMode == OddSuckMathChallengeMode.EvenAmongOdd)
            {
                return false;
            }

            return UnityEngine.Random.value > 0.5f;
        }

        private OddSuckItemData CreateMathItem(bool needsOddResult, bool isOddItem, HashSet<int> usedResults)
        {
            MathExpression expression = new MathExpression("0", 0);

            for (int attempt = 0; attempt < 80; attempt++)
            {
                expression = CreateExpression(needsOddResult);
                if (!avoidDuplicateAnswers || !usedResults.Contains(expression.result))
                {
                    break;
                }
            }

            if (avoidDuplicateAnswers)
            {
                usedResults.Add(expression.result);
            }

            return new OddSuckItemData
            {
                displayText = expression.display,
                icon = null,
                isOdd = isOddItem
            };
        }

        private MathExpression CreateExpression(bool needsOddResult)
        {
            OddSuckMathExpressionMode mode = expressionMode;
            if (mode == OddSuckMathExpressionMode.Mixed)
            {
                mode = GetRandomExpressionMode();
            }
            else if (mode == OddSuckMathExpressionMode.AdditionAndSubtraction)
            {
                mode = UnityEngine.Random.value < 0.5f
                    ? OddSuckMathExpressionMode.Addition
                    : OddSuckMathExpressionMode.Subtraction;
            }
            else if (mode == OddSuckMathExpressionMode.MultiplicationAndDivision)
            {
                mode = UnityEngine.Random.value < 0.5f
                    ? OddSuckMathExpressionMode.Multiplication
                    : OddSuckMathExpressionMode.Division;
            }

            if (mode == OddSuckMathExpressionMode.Multiplication)
            {
                return CreateMultiplication(needsOddResult);
            }

            if (mode == OddSuckMathExpressionMode.Division)
            {
                return CreateDivision(needsOddResult);
            }

            int result = GetResultWithParity(needsOddResult);

            switch (mode)
            {
                case OddSuckMathExpressionMode.Addition:
                    return CreateAddition(result);
                case OddSuckMathExpressionMode.Subtraction:
                    return CreateSubtraction(result);
                default:
                    return new MathExpression(result.ToString(), result);
            }
        }

        private static OddSuckMathExpressionMode GetRandomExpressionMode()
        {
            switch (UnityEngine.Random.Range(0, 5))
            {
                case 0:
                    return OddSuckMathExpressionMode.DirectNumber;
                case 1:
                    return OddSuckMathExpressionMode.Addition;
                case 2:
                    return OddSuckMathExpressionMode.Subtraction;
                case 3:
                    return OddSuckMathExpressionMode.Multiplication;
                default:
                    return OddSuckMathExpressionMode.Division;
            }
        }

        private int GetResultWithParity(bool needsOdd)
        {
            GetSafeResultRange(out int safeMin, out int safeMax);
            if (TryGetResultWithParity(safeMin, safeMax, needsOdd, out int result))
            {
                return result;
            }

            int fallback = safeMin;
            if ((fallback % 2 != 0) != needsOdd)
            {
                fallback++;
            }

            return Mathf.Clamp(fallback, safeMin, safeMax);
        }

        private void GetSafeResultRange(out int safeMin, out int safeMax)
        {
            safeMin = Mathf.Min(minResultValue, maxResultValue - 1);
            safeMax = Mathf.Max(safeMin + 1, maxResultValue);
        }

        private static bool TryGetResultWithParity(int minimum, int maximum, bool needsOdd, out int result)
        {
            int first = minimum;
            if ((first % 2 != 0) != needsOdd)
            {
                first++;
            }

            if (first > maximum)
            {
                result = 0;
                return false;
            }

            int choiceCount = ((maximum - first) / 2) + 1;
            result = first + UnityEngine.Random.Range(0, choiceCount) * 2;
            return true;
        }

        private MathExpression CreateAddition(int result)
        {
            int safeMaxOperand = Mathf.Max(1, maxOperandValue);
            int a = UnityEngine.Random.Range(0, Mathf.Min(result, safeMaxOperand) + 1);
            int b = result - a;

            if (b > safeMaxOperand)
            {
                b = UnityEngine.Random.Range(0, safeMaxOperand + 1);
                a = Mathf.Max(0, result - b);
            }

            return new MathExpression($"{a}+{b}", result);
        }

        private MathExpression CreateSubtraction(int result)
        {
            int safeMaxOperand = Mathf.Max(1, maxOperandValue);
            int b = UnityEngine.Random.Range(0, safeMaxOperand + 1);
            int a = result + b;

            return new MathExpression($"{a}-{b}", result);
        }

        private MathExpression CreateMultiplication(bool needsOddResult)
        {
            int tableMax = GetSafeMaximumTableNumber();
            int multiplierMax = GetSafeMaximumTableMultiplier();

            for (int attempt = 0; attempt < 120; attempt++)
            {
                int tableNumber = UnityEngine.Random.Range(1, tableMax + 1);
                int multiplier = UnityEngine.Random.Range(1, multiplierMax + 1);
                long product = (long)tableNumber * multiplier;
                if (product <= int.MaxValue && (((int)product % 2 != 0) == needsOddResult))
                {
                    return FormatMultiplication(tableNumber, multiplier, (int)product);
                }
            }

            if (needsOddResult)
            {
                int oddTable = GetTableValueWithParity(true, tableMax);
                int oddMultiplier = GetTableValueWithParity(true, multiplierMax);
                return FormatMultiplication(oddTable, oddMultiplier, oddTable * oddMultiplier);
            }

            int fallbackMultiplier = UnityEngine.Random.Range(1, multiplierMax + 1);
            return FormatMultiplication(2, fallbackMultiplier, 2 * fallbackMultiplier);
        }

        private MathExpression FormatMultiplication(int tableNumber, int multiplier, int result)
        {
            string symbol = string.IsNullOrWhiteSpace(multiplicationSymbol) ? "×" : multiplicationSymbol;
            return new MathExpression($"{tableNumber}{symbol}{multiplier}", result);
        }

        private MathExpression CreateDivision(bool needsOddResult)
        {
            int tableMax = GetSafeMaximumTableNumber();
            int multiplierMax = GetSafeMaximumTableMultiplier();
            int tableNumber = UnityEngine.Random.Range(1, tableMax + 1);
            int answer = GetTableValueWithParity(needsOddResult, multiplierMax);
            int dividend = tableNumber * answer;
            return FormatDivision(dividend, tableNumber, answer);
        }

        private MathExpression FormatDivision(int dividend, int divisor, int result)
        {
            string symbol = string.IsNullOrWhiteSpace(divisionSymbol) ? "÷" : divisionSymbol;
            return new MathExpression($"{dividend}{symbol}{divisor}", result);
        }

        private int GetSafeMaximumTableNumber()
        {
            return Mathf.Clamp(maximumTableNumber, 2, 1000);
        }

        private int GetSafeMaximumTableMultiplier()
        {
            return Mathf.Clamp(maximumTableMultiplier, 1, 10);
        }

        private static int GetTableValueWithParity(bool needsOdd, int tableMaximum)
        {
            if (TryGetResultWithParity(1, tableMaximum, needsOdd, out int value))
            {
                return value;
            }

            return needsOdd ? 1 : 2;
        }

        private struct MathExpression
        {
            public readonly string display;
            public readonly int result;

            public MathExpression(string display, int result)
            {
                this.display = display;
                this.result = result;
            }
        }
    }
}
