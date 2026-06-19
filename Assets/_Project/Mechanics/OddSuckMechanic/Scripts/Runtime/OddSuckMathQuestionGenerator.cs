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
        DirectNumber,
        Addition,
        Subtraction,
        Mixed
    }

    public class OddSuckMathQuestionGenerator : OddSuckQuestionGeneratorBase
    {
        [Header("Math Rules")]
        [SerializeField] private OddSuckMathChallengeMode challengeMode = OddSuckMathChallengeMode.RandomOddEven;
        [SerializeField] private OddSuckMathExpressionMode expressionMode = OddSuckMathExpressionMode.Mixed;
        [SerializeField, Min(0)] private int minResultValue = 1;
        [SerializeField, Min(2)] private int maxResultValue = 50;
        [SerializeField, Min(1)] private int maxOperandValue = 30;
        [SerializeField] private bool avoidDuplicateAnswers = true;

        public override bool CanGenerate()
        {
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
                int pick = UnityEngine.Random.Range(0, 3);
                mode = (OddSuckMathExpressionMode)pick;
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

        private int GetResultWithParity(bool needsOdd)
        {
            int safeMin = Mathf.Min(minResultValue, maxResultValue - 1);
            int safeMax = Mathf.Max(safeMin + 1, maxResultValue);

            for (int attempt = 0; attempt < 80; attempt++)
            {
                int value = UnityEngine.Random.Range(safeMin, safeMax + 1);
                if ((value % 2 != 0) == needsOdd)
                {
                    return value;
                }
            }

            int fallback = safeMin;
            if ((fallback % 2 != 0) != needsOdd)
            {
                fallback++;
            }

            return Mathf.Clamp(fallback, safeMin, safeMax);
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
