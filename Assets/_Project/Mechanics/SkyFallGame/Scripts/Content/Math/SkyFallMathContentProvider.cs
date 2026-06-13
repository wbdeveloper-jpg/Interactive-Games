using System.Collections.Generic;
using UnityEngine;

public enum SkyFallMathRuleMode
{
    CatchEvenNumbers,
    CatchOddNumbers,
    CatchEvenAnswers,
    CatchOddAnswers,
    CatchSelectedOperation
}

public enum SkyFallMathOperation
{
    Plus,
    Minus,
    Multiply,
    Divide
}

public class SkyFallMathContentProvider : SkyFallContentProviderBase
{
    [Header("Rule")]
    public SkyFallMathRuleMode ruleMode = SkyFallMathRuleMode.CatchEvenNumbers;

    [Header("Operation Filter")]
    public SkyFallMathOperation selectedOperationToCatch = SkyFallMathOperation.Plus;
    public bool allowPlus = true;
    public bool allowMinus = true;
    public bool allowMultiply = false;
    public bool allowDivide = false;

    [Header("Difficulty")]
    [Range(1, 3)] public int minDigitCount = 1;
    [Range(1, 3)] public int maxDigitCount = 2;
    public bool allowThreeNumberEquations = true;
    [Range(0f, 1f)] public float threeNumberEquationChance = 0.25f;

    [Header("Drop Mix")]
    [Range(0f, 1f)] public float correctItemChance = 0.55f;
    [Range(0f, 1f)] public float equationChanceInNumberModes = 0.35f;
    [Min(10)] public int maxGenerateAttempts = 80;

    [Header("Division Safety")]
    public bool divisionUsesCleanAnswers = true;
    [Range(1, 2)] public int divisionAnswerMaxDigitCount = 2;

    public override string GetPromptText(SkyFallDropContext context)
    {
        switch (ruleMode)
        {
            case SkyFallMathRuleMode.CatchEvenNumbers:
                return "Catch only EVEN numbers";
            case SkyFallMathRuleMode.CatchOddNumbers:
                return "Catch only ODD numbers";
            case SkyFallMathRuleMode.CatchEvenAnswers:
                return "Catch equations with EVEN answers";
            case SkyFallMathRuleMode.CatchOddAnswers:
                return "Catch equations with ODD answers";
            case SkyFallMathRuleMode.CatchSelectedOperation:
                return "Catch only " + GetOperationDisplayName(selectedOperationToCatch) + " equations";
            default:
                return "Catch the correct answer";
        }
    }

    public override SkyFallDropData GenerateDrop(SkyFallDropContext context)
    {
        bool wantCorrect = Random.value <= correctItemChance;

        for (int i = 0; i < maxGenerateAttempts; i++)
        {
            SkyFallDropData candidate = CreateCandidateDrop();
            if (candidate.isCorrect == wantCorrect)
                return candidate;
        }

        return CreateCandidateDrop();
    }

    private SkyFallDropData CreateCandidateDrop()
    {
        if (ruleMode == SkyFallMathRuleMode.CatchEvenNumbers ||
            ruleMode == SkyFallMathRuleMode.CatchOddNumbers)
        {
            return CreateNumberModeDrop();
        }

        return CreateEquationModeDrop();
    }

    private SkyFallDropData CreateNumberModeDrop()
    {
        bool useEquation = Random.value < equationChanceInNumberModes;

        if (useEquation)
        {
            EquationResult equation = GenerateEquation();
            return new SkyFallDropData
            {
                displayText = equation.display,
                isCorrect = IsCorrectByParityRule(equation.answer)
            };
        }

        int value = RandomNumberByDigits();
        return new SkyFallDropData
        {
            displayText = value.ToString(),
            isCorrect = IsCorrectByParityRule(value)
        };
    }

    private SkyFallDropData CreateEquationModeDrop()
    {
        EquationResult equation = GenerateEquation();
        bool correct;

        if (ruleMode == SkyFallMathRuleMode.CatchSelectedOperation)
            correct = equation.mainOperation == selectedOperationToCatch;
        else
            correct = IsCorrectByParityRule(equation.answer);

        return new SkyFallDropData
        {
            displayText = equation.display,
            isCorrect = correct
        };
    }

    private bool IsCorrectByParityRule(int value)
    {
        bool even = value % 2 == 0;

        if (ruleMode == SkyFallMathRuleMode.CatchEvenNumbers ||
            ruleMode == SkyFallMathRuleMode.CatchEvenAnswers)
            return even;

        if (ruleMode == SkyFallMathRuleMode.CatchOddNumbers ||
            ruleMode == SkyFallMathRuleMode.CatchOddAnswers)
            return !even;

        return false;
    }

    private EquationResult GenerateEquation()
    {
        List<SkyFallMathOperation> operations = GetAllowedOperations();
        if (operations.Count == 0)
            operations.Add(SkyFallMathOperation.Plus);

        SkyFallMathOperation op1 = operations[Random.Range(0, operations.Count)];
        SkyFallMathOperation op2 = operations[Random.Range(0, operations.Count)];

        if (ruleMode == SkyFallMathRuleMode.CatchSelectedOperation && Random.value < 0.5f)
            op1 = selectedOperationToCatch;

        bool useThreeNumbers = allowThreeNumberEquations && Random.value < threeNumberEquationChance;

        if (useThreeNumbers)
            return GenerateThreeNumberEquation(op1, op2);

        return GenerateTwoNumberEquation(op1);
    }

    private EquationResult GenerateTwoNumberEquation(SkyFallMathOperation operation)
    {
        int a = RandomNumberByDigits();
        int b = RandomNumberByDigits();

        if (operation == SkyFallMathOperation.Minus && b > a)
            Swap(ref a, ref b);

        if (operation == SkyFallMathOperation.Divide && divisionUsesCleanAnswers)
        {
            int answer = RandomNumber(1, MaxValueForDigits(divisionAnswerMaxDigitCount));
            b = RandomNumber(1, MaxValueForDigits(Mathf.Clamp(maxDigitCount - 1, 1, 3)));
            a = answer * b;
        }

        int result = ApplyOperation(a, b, operation);

        return new EquationResult
        {
            display = a + " " + OperationSymbol(operation) + " " + b,
            answer = result,
            mainOperation = operation
        };
    }

    private EquationResult GenerateThreeNumberEquation(SkyFallMathOperation op1, SkyFallMathOperation op2)
    {
        // For readability, avoid division in 3-number equations by default.
        if (op1 == SkyFallMathOperation.Divide)
            op1 = SkyFallMathOperation.Plus;
        if (op2 == SkyFallMathOperation.Divide)
            op2 = SkyFallMathOperation.Minus;

        int a = RandomNumberByDigits();
        int b = RandomNumberByDigits();
        int c = RandomNumberByDigits();

        if (op1 == SkyFallMathOperation.Minus && b > a)
            Swap(ref a, ref b);

        int firstResult = ApplyOperation(a, b, op1);

        if (op2 == SkyFallMathOperation.Minus && c > firstResult)
            c = Mathf.Max(1, firstResult);

        int finalResult = ApplyOperation(firstResult, c, op2);

        return new EquationResult
        {
            display = a + " " + OperationSymbol(op1) + " " + b + " " + OperationSymbol(op2) + " " + c,
            answer = finalResult,
            mainOperation = op1
        };
    }

    private int ApplyOperation(int a, int b, SkyFallMathOperation operation)
    {
        b = Mathf.Max(1, b);

        switch (operation)
        {
            case SkyFallMathOperation.Plus:
                return a + b;
            case SkyFallMathOperation.Minus:
                return a - b;
            case SkyFallMathOperation.Multiply:
                return a * b;
            case SkyFallMathOperation.Divide:
                return a / b;
            default:
                return a + b;
        }
    }

    private List<SkyFallMathOperation> GetAllowedOperations()
    {
        List<SkyFallMathOperation> operations = new List<SkyFallMathOperation>();

        if (allowPlus)
            operations.Add(SkyFallMathOperation.Plus);
        if (allowMinus)
            operations.Add(SkyFallMathOperation.Minus);
        if (allowMultiply)
            operations.Add(SkyFallMathOperation.Multiply);
        if (allowDivide)
            operations.Add(SkyFallMathOperation.Divide);

        return operations;
    }

    private int RandomNumberByDigits()
    {
        int minDigits = Mathf.Clamp(minDigitCount, 1, 3);
        int maxDigits = Mathf.Clamp(maxDigitCount, minDigits, 3);
        int digits = Random.Range(minDigits, maxDigits + 1);
        return RandomNumber(MinValueForDigits(digits), MaxValueForDigits(digits));
    }

    private int RandomNumber(int minInclusive, int maxInclusive)
    {
        return Random.Range(minInclusive, maxInclusive + 1);
    }

    private int MinValueForDigits(int digits)
    {
        digits = Mathf.Clamp(digits, 1, 3);
        if (digits == 1)
            return 1;
        if (digits == 2)
            return 10;
        return 100;
    }

    private int MaxValueForDigits(int digits)
    {
        digits = Mathf.Clamp(digits, 1, 3);
        if (digits == 1)
            return 9;
        if (digits == 2)
            return 99;
        return 999;
    }

    private void Swap(ref int a, ref int b)
    {
        int temp = a;
        a = b;
        b = temp;
    }

    private string OperationSymbol(SkyFallMathOperation operation)
    {
        switch (operation)
        {
            case SkyFallMathOperation.Plus:
                return "+";
            case SkyFallMathOperation.Minus:
                return "-";
            case SkyFallMathOperation.Multiply:
                return "×";
            case SkyFallMathOperation.Divide:
                return "÷";
            default:
                return "+";
        }
    }

    private string GetOperationDisplayName(SkyFallMathOperation operation)
    {
        switch (operation)
        {
            case SkyFallMathOperation.Plus:
                return "PLUS";
            case SkyFallMathOperation.Minus:
                return "MINUS";
            case SkyFallMathOperation.Multiply:
                return "MULTIPLY";
            case SkyFallMathOperation.Divide:
                return "DIVISION";
            default:
                return "OPERATION";
        }
    }

    private struct EquationResult
    {
        public string display;
        public int answer;
        public SkyFallMathOperation mainOperation;
    }
}
