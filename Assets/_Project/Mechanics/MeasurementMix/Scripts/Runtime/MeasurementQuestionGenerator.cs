using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MeasurementMix
{
    [Serializable]
    public class MeasurementAnswerOption
    {
        public string text;
        public bool isCorrect;
    }

    [Serializable]
    public class MeasurementQuestion
    {
        public MeasurementQuestionType type;
        public MeasurementDomain domain;
        public string prompt;
        public string targetDisplay;
        public int targetMassInGrams;
        public int targetLiquidInMillilitres;
        public List<int> solutionWeightValues = new List<int>();
        public List<MeasurementAnswerOption> options =
            new List<MeasurementAnswerOption>();

        public bool IsConversion =>
            type == MeasurementQuestionType.MassConversion ||
            type == MeasurementQuestionType.LiquidConversion;
    }

    public class MeasurementQuestionGenerator : MonoBehaviour
    {
        [Header("Editable Question Text")]
        [Tooltip("{0} is replaced with the generated mass.")]
        public string practicalMassTemplate = "Balance the scale at {0}.";
        [Tooltip("{0} is replaced with the generated volume.")]
        public string practicalLiquidTemplate = "Fill the beaker to {0}.";
        [Tooltip("{0} is the source measurement and {1} is the target unit.")]
        public string namedConversionTemplate = "Convert {0} to {1}.";
        [Tooltip("{0} is replaced with the source measurement.")]
        public string equivalentChoiceTemplate =
            "Which measurement is equal to {0}?";

        private readonly HashSet<string> usedQuestionKeys = new HashSet<string>();
        private readonly List<int> workingWeightValues = new List<int>(16);
        private readonly List<MassUnitSet> massUnits = new List<MassUnitSet>(4);
        private readonly List<LiquidUnitSet> liquidUnits = new List<LiquidUnitSet>(4);
        private readonly List<MeasurementAnswerOption> workingOptions =
            new List<MeasurementAnswerOption>(4);

        public void ResetForNewRun()
        {
            usedQuestionKeys.Clear();
        }

        public MeasurementQuestion Generate(
            MeasurementQuestionType type,
            MeasurementDifficultyProfile profile,
            IReadOnlyList<int> availableWeightTokens)
        {
            for (int attempt = 0; attempt < 60; attempt++)
            {
                MeasurementQuestion question;
                switch (type)
                {
                    case MeasurementQuestionType.PracticalMass:
                        question = GeneratePracticalMass(profile, availableWeightTokens);
                        break;
                    case MeasurementQuestionType.PracticalLiquid:
                        question = GeneratePracticalLiquid(profile);
                        break;
                    case MeasurementQuestionType.MassConversion:
                        question = GenerateMassConversion(profile);
                        break;
                    default:
                        question = GenerateLiquidConversion(profile);
                        break;
                }

                string key = type + "|" + question.prompt;
                if (usedQuestionKeys.Add(key))
                    return question;
            }

            // With very narrow custom ranges, repetition is safer than failing.
            switch (type)
            {
                case MeasurementQuestionType.PracticalMass:
                    return GeneratePracticalMass(profile, availableWeightTokens);
                case MeasurementQuestionType.PracticalLiquid:
                    return GeneratePracticalLiquid(profile);
                case MeasurementQuestionType.MassConversion:
                    return GenerateMassConversion(profile);
                default:
                    return GenerateLiquidConversion(profile);
            }
        }

        private MeasurementQuestion GeneratePracticalMass(
            MeasurementDifficultyProfile profile,
            IReadOnlyList<int> availableWeightTokens)
        {
            workingWeightValues.Clear();

            if (availableWeightTokens != null)
            {
                for (int index = 0; index < availableWeightTokens.Count; index++)
                {
                    int value = availableWeightTokens[index];
                    if (Contains(profile.allowedWeightValuesInGrams, value))
                        workingWeightValues.Add(value);
                }
            }

            if (workingWeightValues.Count == 0)
                workingWeightValues.Add(Mathf.Max(1, profile.minimumMassTargetInGrams));

            int minimumCount = Mathf.Clamp(
                profile.minimumWeightsInSolution,
                1,
                workingWeightValues.Count);
            int maximumCount = Mathf.Clamp(
                profile.maximumWeightsInSolution,
                minimumCount,
                workingWeightValues.Count);

            List<int> selected = new List<int>(maximumCount);
            int target = workingWeightValues[0];

            for (int attempt = 0; attempt < 100; attempt++)
            {
                Shuffle(workingWeightValues);
                selected.Clear();
                target = 0;
                int count = UnityEngine.Random.Range(minimumCount, maximumCount + 1);

                for (int index = 0; index < count; index++)
                {
                    int value = workingWeightValues[index];
                    selected.Add(value);
                    target += value;
                }

                if (target >= profile.minimumMassTargetInGrams &&
                    target <= profile.maximumMassTargetInGrams)
                    break;
            }

            return new MeasurementQuestion
            {
                type = MeasurementQuestionType.PracticalMass,
                domain = MeasurementDomain.Mass,
                prompt = SafeFormat(
                    practicalMassTemplate,
                    "Balance the scale at {0}.",
                    FormatPracticalMass(
                        target,
                        profile.preferDecimalDisplayForPracticalQuestions)),
                targetDisplay = FormatPracticalMass(
                    target,
                    profile.preferDecimalDisplayForPracticalQuestions),
                targetMassInGrams = target,
                solutionWeightValues = new List<int>(selected)
            };
        }

        private MeasurementQuestion GeneratePracticalLiquid(
            MeasurementDifficultyProfile profile)
        {
            int step = Mathf.Max(1, profile.liquidStepInMillilitres);
            int minimum = Mathf.Clamp(
                profile.minimumLiquidTargetInMillilitres,
                step,
                profile.containerCapacityInMillilitres);
            int maximum = Mathf.Clamp(
                profile.maximumLiquidTargetInMillilitres,
                minimum,
                profile.containerCapacityInMillilitres);
            int minimumIndex = Mathf.CeilToInt(minimum / (float)step);
            int maximumIndex = Mathf.Max(
                minimumIndex,
                Mathf.FloorToInt(maximum / (float)step));
            int target = UnityEngine.Random.Range(minimumIndex, maximumIndex + 1) * step;

            return new MeasurementQuestion
            {
                type = MeasurementQuestionType.PracticalLiquid,
                domain = MeasurementDomain.Liquid,
                prompt = SafeFormat(
                    practicalLiquidTemplate,
                    "Fill the beaker to {0}.",
                    FormatPracticalLiquid(
                        target,
                        profile.preferDecimalDisplayForPracticalQuestions)),
                targetDisplay = FormatPracticalLiquid(
                    target,
                    profile.preferDecimalDisplayForPracticalQuestions),
                targetLiquidInMillilitres = target
            };
        }

        private MeasurementQuestion GenerateMassConversion(
            MeasurementDifficultyProfile profile)
        {
            BuildMassUnitList(profile.allowedMassUnits);
            MassUnitSet sourceUnit;
            MassUnitSet targetUnit;
            double sourceValue;
            double targetValue;

            CreateConvertibleMass(
                profile,
                out sourceUnit,
                out targetUnit,
                out sourceValue,
                out targetValue);

            ConversionQuestionStyle style = ResolveStyle(profile.conversionStyle);
            string sourceText = FormatNumber(sourceValue) + " " + MassSymbol(sourceUnit);
            string prompt = style == ConversionQuestionStyle.ConvertToNamedUnit
                ? SafeFormat(
                    namedConversionTemplate,
                    "Convert {0} to {1}.",
                    sourceText,
                    MassSymbol(targetUnit))
                : SafeFormat(
                    equivalentChoiceTemplate,
                    "Which measurement is equal to {0}?",
                    sourceText);

            BuildMassOptions(
                profile,
                style,
                sourceValue * MassFactorInMilligrams(sourceUnit),
                targetUnit,
                targetValue);

            return new MeasurementQuestion
            {
                type = MeasurementQuestionType.MassConversion,
                domain = MeasurementDomain.Mass,
                prompt = prompt,
                options = CopyAndShuffleOptions(workingOptions)
            };
        }

        private MeasurementQuestion GenerateLiquidConversion(
            MeasurementDifficultyProfile profile)
        {
            BuildLiquidUnitList(profile.allowedLiquidUnits);
            LiquidUnitSet sourceUnit;
            LiquidUnitSet targetUnit;
            double sourceValue;
            double targetValue;

            CreateConvertibleLiquid(
                profile,
                out sourceUnit,
                out targetUnit,
                out sourceValue,
                out targetValue);

            ConversionQuestionStyle style = ResolveStyle(profile.conversionStyle);
            string sourceText = FormatNumber(sourceValue) + " " + LiquidSymbol(sourceUnit);
            string prompt = style == ConversionQuestionStyle.ConvertToNamedUnit
                ? SafeFormat(
                    namedConversionTemplate,
                    "Convert {0} to {1}.",
                    sourceText,
                    LiquidSymbol(targetUnit))
                : SafeFormat(
                    equivalentChoiceTemplate,
                    "Which measurement is equal to {0}?",
                    sourceText);

            BuildLiquidOptions(
                profile,
                style,
                sourceValue * LiquidFactorInMillilitres(sourceUnit),
                targetUnit,
                targetValue);

            return new MeasurementQuestion
            {
                type = MeasurementQuestionType.LiquidConversion,
                domain = MeasurementDomain.Liquid,
                prompt = prompt,
                options = CopyAndShuffleOptions(workingOptions)
            };
        }

        private void CreateConvertibleMass(
            MeasurementDifficultyProfile profile,
            out MassUnitSet sourceUnit,
            out MassUnitSet targetUnit,
            out double sourceValue,
            out double targetValue)
        {
            sourceUnit = massUnits[0];
            targetUnit = massUnits[1];
            sourceValue = 1d;
            targetValue = MassFactorInMilligrams(sourceUnit) /
                MassFactorInMilligrams(targetUnit);

            for (int attempt = 0; attempt < 100; attempt++)
            {
                int sourceIndex = UnityEngine.Random.Range(0, massUnits.Count);
                int targetIndex = UnityEngine.Random.Range(0, massUnits.Count - 1);
                if (targetIndex >= sourceIndex)
                    targetIndex++;

                sourceUnit = massUnits[sourceIndex];
                targetUnit = massUnits[targetIndex];
                sourceValue = GenerateSourceNumber(profile);
                targetValue = sourceValue * MassFactorInMilligrams(sourceUnit) /
                    MassFactorInMilligrams(targetUnit);

                if (profile.allowDecimalValues || IsWhole(targetValue))
                    return;
            }
        }

        private void CreateConvertibleLiquid(
            MeasurementDifficultyProfile profile,
            out LiquidUnitSet sourceUnit,
            out LiquidUnitSet targetUnit,
            out double sourceValue,
            out double targetValue)
        {
            sourceUnit = liquidUnits[0];
            targetUnit = liquidUnits[1];
            sourceValue = 1d;
            targetValue = LiquidFactorInMillilitres(sourceUnit) /
                LiquidFactorInMillilitres(targetUnit);

            for (int attempt = 0; attempt < 100; attempt++)
            {
                int sourceIndex = UnityEngine.Random.Range(0, liquidUnits.Count);
                int targetIndex = UnityEngine.Random.Range(0, liquidUnits.Count - 1);
                if (targetIndex >= sourceIndex)
                    targetIndex++;

                sourceUnit = liquidUnits[sourceIndex];
                targetUnit = liquidUnits[targetIndex];
                sourceValue = GenerateSourceNumber(profile);
                targetValue = sourceValue * LiquidFactorInMillilitres(sourceUnit) /
                    LiquidFactorInMillilitres(targetUnit);

                if (profile.allowDecimalValues || IsWhole(targetValue))
                    return;
            }
        }

        private void BuildMassOptions(
            MeasurementDifficultyProfile profile,
            ConversionQuestionStyle style,
            double correctBaseValue,
            MassUnitSet targetUnit,
            double correctTargetValue)
        {
            workingOptions.Clear();
            AddUniqueOption(
                FormatNumber(correctTargetValue) + " " + MassSymbol(targetUnit),
                true);

            double[] errorFactors = { 10d, 0.1d, 100d, 0.01d, 2d, 0.5d };
            int errorIndex = UnityEngine.Random.Range(0, errorFactors.Length);
            int safety = 0;

            while (workingOptions.Count < profile.conversionOptionCount &&
                   safety++ < 80)
            {
                double wrongBase = correctBaseValue *
                    errorFactors[errorIndex % errorFactors.Length];
                errorIndex++;
                MassUnitSet displayUnit = style ==
                    ConversionQuestionStyle.ChooseEquivalentMeasurement
                    ? massUnits[workingOptions.Count % massUnits.Count]
                    : targetUnit;
                double displayed = wrongBase / MassFactorInMilligrams(displayUnit);

                if (!profile.allowDecimalValues && !IsWhole(displayed))
                    continue;

                AddUniqueOption(
                    FormatNumber(displayed) + " " + MassSymbol(displayUnit),
                    false);
            }

            int fallbackOffset = 1;
            while (workingOptions.Count < profile.conversionOptionCount)
            {
                double fallback = correctTargetValue + fallbackOffset++;
                AddUniqueOption(
                    FormatNumber(fallback) + " " + MassSymbol(targetUnit),
                    false);
            }
        }

        private void BuildLiquidOptions(
            MeasurementDifficultyProfile profile,
            ConversionQuestionStyle style,
            double correctBaseValue,
            LiquidUnitSet targetUnit,
            double correctTargetValue)
        {
            workingOptions.Clear();
            AddUniqueOption(
                FormatNumber(correctTargetValue) + " " + LiquidSymbol(targetUnit),
                true);

            double[] errorFactors = { 10d, 0.1d, 100d, 0.01d, 2d, 0.5d };
            int errorIndex = UnityEngine.Random.Range(0, errorFactors.Length);
            int safety = 0;

            while (workingOptions.Count < profile.conversionOptionCount &&
                   safety++ < 80)
            {
                double wrongBase = correctBaseValue *
                    errorFactors[errorIndex % errorFactors.Length];
                errorIndex++;
                LiquidUnitSet displayUnit = style ==
                    ConversionQuestionStyle.ChooseEquivalentMeasurement
                    ? liquidUnits[workingOptions.Count % liquidUnits.Count]
                    : targetUnit;
                double displayed = wrongBase /
                    LiquidFactorInMillilitres(displayUnit);

                if (!profile.allowDecimalValues && !IsWhole(displayed))
                    continue;

                AddUniqueOption(
                    FormatNumber(displayed) + " " + LiquidSymbol(displayUnit),
                    false);
            }

            int fallbackOffset = 1;
            while (workingOptions.Count < profile.conversionOptionCount)
            {
                double fallback = correctTargetValue + fallbackOffset++;
                AddUniqueOption(
                    FormatNumber(fallback) + " " + LiquidSymbol(targetUnit),
                    false);
            }
        }

        private void AddUniqueOption(string text, bool correct)
        {
            for (int index = 0; index < workingOptions.Count; index++)
            {
                if (workingOptions[index].text == text)
                    return;
            }

            workingOptions.Add(new MeasurementAnswerOption
            {
                text = text,
                isCorrect = correct
            });
        }

        private static List<MeasurementAnswerOption> CopyAndShuffleOptions(
            List<MeasurementAnswerOption> source)
        {
            List<MeasurementAnswerOption> result =
                new List<MeasurementAnswerOption>(source.Count);

            for (int index = 0; index < source.Count; index++)
            {
                result.Add(new MeasurementAnswerOption
                {
                    text = source[index].text,
                    isCorrect = source[index].isCorrect
                });
            }

            Shuffle(result);
            return result;
        }

        private static double GenerateSourceNumber(
            MeasurementDifficultyProfile profile)
        {
            if (!profile.allowDecimalValues || UnityEngine.Random.value < 0.35f)
            {
                return UnityEngine.Random.Range(
                    profile.minimumConversionSourceNumber,
                    profile.maximumConversionSourceNumber + 1);
            }

            double step = Math.Max(0.1d, profile.decimalStep);
            int minimumStep = Mathf.CeilToInt(
                profile.minimumConversionSourceNumber / (float)step);
            int maximumStep = Mathf.FloorToInt(
                profile.maximumConversionSourceNumber / (float)step);
            int selected = UnityEngine.Random.Range(minimumStep, maximumStep + 1);
            double value = selected * step;

            // Hard mode should regularly create a visible fraction such as 1.5.
            if (IsWhole(value) && selected < maximumStep)
                value += step;

            return value;
        }

        private static ConversionQuestionStyle ResolveStyle(
            ConversionQuestionStyle configured)
        {
            if (configured != ConversionQuestionStyle.Mixed)
                return configured;

            return UnityEngine.Random.value < 0.5f
                ? ConversionQuestionStyle.ConvertToNamedUnit
                : ConversionQuestionStyle.ChooseEquivalentMeasurement;
        }

        private void BuildMassUnitList(MassUnitSet flags)
        {
            massUnits.Clear();
            AddIfSet(massUnits, flags, MassUnitSet.Milligram);
            AddIfSet(massUnits, flags, MassUnitSet.Gram);
            AddIfSet(massUnits, flags, MassUnitSet.Kilogram);
            AddIfSet(massUnits, flags, MassUnitSet.Tonne);

            if (massUnits.Count < 2)
            {
                massUnits.Clear();
                massUnits.Add(MassUnitSet.Gram);
                massUnits.Add(MassUnitSet.Kilogram);
            }
        }

        private void BuildLiquidUnitList(LiquidUnitSet flags)
        {
            liquidUnits.Clear();
            AddIfSet(liquidUnits, flags, LiquidUnitSet.Millilitre);
            AddIfSet(liquidUnits, flags, LiquidUnitSet.Centilitre);
            AddIfSet(liquidUnits, flags, LiquidUnitSet.Decilitre);
            AddIfSet(liquidUnits, flags, LiquidUnitSet.Litre);

            if (liquidUnits.Count < 2)
            {
                liquidUnits.Clear();
                liquidUnits.Add(LiquidUnitSet.Millilitre);
                liquidUnits.Add(LiquidUnitSet.Litre);
            }
        }

        private static void AddIfSet(
            List<MassUnitSet> list,
            MassUnitSet flags,
            MassUnitSet unit)
        {
            if ((flags & unit) != 0)
                list.Add(unit);
        }

        private static void AddIfSet(
            List<LiquidUnitSet> list,
            LiquidUnitSet flags,
            LiquidUnitSet unit)
        {
            if ((flags & unit) != 0)
                list.Add(unit);
        }

        private static bool Contains(int[] values, int value)
        {
            if (values == null)
                return false;

            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] == value)
                    return true;
            }
            return false;
        }

        private static bool IsWhole(double value)
        {
            return Math.Abs(value - Math.Round(value)) < 0.000001d;
        }

        public static string FormatPracticalMass(int grams)
        {
            return FormatPracticalMass(grams, false);
        }

        public static string FormatPracticalMass(int grams, bool preferDecimal)
        {
            if (preferDecimal && grams >= 1000)
                return FormatNumber(grams / 1000d) + " kg";

            if (grams >= 1000)
            {
                int kilograms = grams / 1000;
                int remainder = grams % 1000;
                if (remainder == 0)
                    return kilograms + " kg";
                return kilograms + " kg " + remainder + " g";
            }
            return grams + " g";
        }

        public static string FormatPracticalLiquid(int millilitres)
        {
            return FormatPracticalLiquid(millilitres, false);
        }

        public static string FormatPracticalLiquid(
            int millilitres,
            bool preferDecimal)
        {
            if (preferDecimal && millilitres >= 1000)
                return FormatNumber(millilitres / 1000d) + " L";

            if (millilitres >= 1000)
            {
                int litres = millilitres / 1000;
                int remainder = millilitres % 1000;
                if (remainder == 0)
                    return litres + " L";
                return litres + " L " + remainder + " mL";
            }
            return millilitres + " mL";
        }

        public static string FormatNumber(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string SafeFormat(
            string configuredTemplate,
            string fallbackTemplate,
            params object[] values)
        {
            string template = string.IsNullOrWhiteSpace(configuredTemplate)
                ? fallbackTemplate
                : configuredTemplate;

            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, values);
            }
            catch (FormatException)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    fallbackTemplate,
                    values);
            }
        }

        private static double MassFactorInMilligrams(MassUnitSet unit)
        {
            switch (unit)
            {
                case MassUnitSet.Milligram:
                    return 1d;
                case MassUnitSet.Gram:
                    return 1000d;
                case MassUnitSet.Kilogram:
                    return 1000000d;
                default:
                    return 1000000000d;
            }
        }

        private static string MassSymbol(MassUnitSet unit)
        {
            switch (unit)
            {
                case MassUnitSet.Milligram:
                    return "mg";
                case MassUnitSet.Gram:
                    return "g";
                case MassUnitSet.Kilogram:
                    return "kg";
                default:
                    return "t";
            }
        }

        private static double LiquidFactorInMillilitres(LiquidUnitSet unit)
        {
            switch (unit)
            {
                case LiquidUnitSet.Millilitre:
                    return 1d;
                case LiquidUnitSet.Centilitre:
                    return 10d;
                case LiquidUnitSet.Decilitre:
                    return 100d;
                default:
                    return 1000d;
            }
        }

        private static string LiquidSymbol(LiquidUnitSet unit)
        {
            switch (unit)
            {
                case LiquidUnitSet.Millilitre:
                    return "mL";
                case LiquidUnitSet.Centilitre:
                    return "cL";
                case LiquidUnitSet.Decilitre:
                    return "dL";
                default:
                    return "L";
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int index = list.Count - 1; index > 0; index--)
            {
                int swapIndex = UnityEngine.Random.Range(0, index + 1);
                T temporary = list[index];
                list[index] = list[swapIndex];
                list[swapIndex] = temporary;
            }
        }
    }
}
