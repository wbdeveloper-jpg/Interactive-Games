using System;
using UnityEngine;

namespace MeasurementMix
{
    public enum MeasurementDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public enum MeasurementDomain
    {
        Mass,
        Liquid
    }

    public enum MeasurementQuestionType
    {
        PracticalMass,
        PracticalLiquid,
        MassConversion,
        LiquidConversion
    }

    public enum ConversionQuestionStyle
    {
        ConvertToNamedUnit,
        ChooseEquivalentMeasurement,
        Mixed
    }

    [Flags]
    public enum MassUnitSet
    {
        None = 0,
        Milligram = 1 << 0,
        Gram = 1 << 1,
        Kilogram = 1 << 2,
        Tonne = 1 << 3
    }

    [Flags]
    public enum LiquidUnitSet
    {
        None = 0,
        Millilitre = 1 << 0,
        Centilitre = 1 << 1,
        Decilitre = 1 << 2,
        Litre = 1 << 3
    }

    [Serializable]
    public class MeasurementDifficultyProfile
    {
        [Header("Round")]
        [Min(10f)] public float secondsPerQuestion = 45f;

        [Tooltip("0 creates only practical questions. 1 creates only conversions.")]
        [Range(0f, 1f)] public float conversionQuestionChance = 0.25f;

        [Header("Practical Mass")]
        [Tooltip("Two copies of each selected denomination exist in the rough UI.")]
        public int[] allowedWeightValuesInGrams = { 100, 200, 500 };
        [Range(1, 8)] public int minimumWeightsInSolution = 1;
        [Range(1, 8)] public int maximumWeightsInSolution = 3;
        [Min(1)] public int minimumMassTargetInGrams = 100;
        [Min(1)] public int maximumMassTargetInGrams = 1000;

        [Header("Practical Liquid")]
        [Min(100)] public int containerCapacityInMillilitres = 1000;
        [Min(1)] public int liquidStepInMillilitres = 100;
        [Min(1)] public int minimumLiquidTargetInMillilitres = 100;
        [Min(1)] public int maximumLiquidTargetInMillilitres = 1000;

        [Header("Unit Conversion")]
        public MassUnitSet allowedMassUnits =
            MassUnitSet.Gram | MassUnitSet.Kilogram;
        public LiquidUnitSet allowedLiquidUnits =
            LiquidUnitSet.Millilitre | LiquidUnitSet.Litre;
        public ConversionQuestionStyle conversionStyle =
            ConversionQuestionStyle.ConvertToNamedUnit;
        [Range(3, 4)] public int conversionOptionCount = 4;
        [Min(1)] public int minimumConversionSourceNumber = 1;
        [Min(2)] public int maximumConversionSourceNumber = 100;

        [Tooltip("Allows values such as 1.5 L. Recommended only for Hard.")]
        public bool allowDecimalValues;

        [Tooltip("The fractional interval used when decimal questions are enabled.")]
        [Range(0.1f, 0.5f)] public float decimalStep = 0.5f;

        [Tooltip("Displays practical targets as 1.5 kg or 1.5 L when possible.")]
        public bool preferDecimalDisplayForPracticalQuestions;

        // Copy-over compatibility for the first package version. These aliases let
        // stale v1 controller files compile long enough to be overwritten or
        // removed without changing the production profile names.
        [Obsolete("Use allowedWeightValuesInGrams.")]
        public int[] allowedWeightValues => allowedWeightValuesInGrams;

        [Obsolete("Use minimumMassTargetInGrams.")]
        public int minimumWeightTarget => minimumMassTargetInGrams;

        [Obsolete("Use maximumMassTargetInGrams.")]
        public int maximumWeightTarget => maximumMassTargetInGrams;

        [Obsolete("Use liquidStepInMillilitres.")]
        public int liquidStep => liquidStepInMillilitres;

        [Obsolete("Use minimumLiquidTargetInMillilitres.")]
        public int minimumLiquidTarget => minimumLiquidTargetInMillilitres;

        [Obsolete("Use maximumLiquidTargetInMillilitres.")]
        public int maximumLiquidTarget => maximumLiquidTargetInMillilitres;
    }

    public class MeasurementGameSettings : MonoBehaviour
    {
        [Header("Game")]
        public MeasurementDifficulty difficulty = MeasurementDifficulty.Easy;
        [Range(1, 5)] public int questionsPerRun = 5;
        [Range(0.2f, 0.8f)] public float massQuestionChance = 0.5f;
        public bool showHowToPlayAtStart = true;

        [Header("Scoring")]
        [Min(0)] public int pointsPerCorrectAnswer = 100;
        [Min(0)] public int maximumTimeBonus = 50;
        [Min(0)] public int hintPenalty = 20;

        [Header("Round Transitions")]
        [Tooltip("Keeps positive feedback visible before the next round.")]
        [Min(1f)] public float correctFeedbackDuration = 2.5f;
        [Tooltip("Keeps timeout feedback visible before the next round.")]
        [Min(1f)] public float timeoutFeedbackDuration = 3f;
        [Min(0.1f)] public float panelFadeDuration = 0.3f;

        [Header("Hints")]
        public bool keepLiquidTargetLineVisibleAfterHint = true;
        [Min(1f)] public float temporaryLiquidHintDuration = 4f;
        [Range(1, 4)] public int weightHintPulseCount = 2;

        [Header("Difficulty Profiles")]
        public MeasurementDifficultyProfile easy = new MeasurementDifficultyProfile();
        public MeasurementDifficultyProfile normal = new MeasurementDifficultyProfile();
        public MeasurementDifficultyProfile hard = new MeasurementDifficultyProfile();

        public MeasurementDifficultyProfile CurrentProfile
        {
            get
            {
                switch (difficulty)
                {
                    case MeasurementDifficulty.Normal:
                        return normal;
                    case MeasurementDifficulty.Hard:
                        return hard;
                    default:
                        return easy;
                }
            }
        }

        public void ApplyRecommendedDefaults()
        {
            difficulty = MeasurementDifficulty.Easy;
            questionsPerRun = 5;
            massQuestionChance = 0.5f;
            showHowToPlayAtStart = true;
            pointsPerCorrectAnswer = 100;
            maximumTimeBonus = 50;
            hintPenalty = 20;
            correctFeedbackDuration = 2.5f;
            timeoutFeedbackDuration = 3f;
            panelFadeDuration = 0.3f;
            keepLiquidTargetLineVisibleAfterHint = true;
            temporaryLiquidHintDuration = 4f;
            weightHintPulseCount = 2;

            easy = new MeasurementDifficultyProfile
            {
                secondsPerQuestion = 50f,
                conversionQuestionChance = 0.2f,
                allowedWeightValuesInGrams = new[] { 100, 200, 500 },
                minimumWeightsInSolution = 1,
                maximumWeightsInSolution = 3,
                minimumMassTargetInGrams = 100,
                maximumMassTargetInGrams = 1000,
                containerCapacityInMillilitres = 1000,
                liquidStepInMillilitres = 100,
                minimumLiquidTargetInMillilitres = 100,
                maximumLiquidTargetInMillilitres = 1000,
                allowedMassUnits = MassUnitSet.Gram | MassUnitSet.Kilogram,
                allowedLiquidUnits = LiquidUnitSet.Millilitre | LiquidUnitSet.Litre,
                conversionStyle = ConversionQuestionStyle.ConvertToNamedUnit,
                conversionOptionCount = 3,
                minimumConversionSourceNumber = 1,
                maximumConversionSourceNumber = 100,
                allowDecimalValues = false,
                decimalStep = 0.5f,
                preferDecimalDisplayForPracticalQuestions = false
            };

            normal = new MeasurementDifficultyProfile
            {
                secondsPerQuestion = 45f,
                conversionQuestionChance = 0.4f,
                allowedWeightValuesInGrams = new[] { 50, 100, 200, 500, 1000 },
                minimumWeightsInSolution = 2,
                maximumWeightsInSolution = 5,
                minimumMassTargetInGrams = 150,
                maximumMassTargetInGrams = 2500,
                containerCapacityInMillilitres = 2000,
                liquidStepInMillilitres = 50,
                minimumLiquidTargetInMillilitres = 100,
                maximumLiquidTargetInMillilitres = 2000,
                allowedMassUnits = MassUnitSet.Gram | MassUnitSet.Kilogram,
                allowedLiquidUnits = LiquidUnitSet.Millilitre | LiquidUnitSet.Litre,
                conversionStyle = ConversionQuestionStyle.Mixed,
                conversionOptionCount = 4,
                minimumConversionSourceNumber = 1,
                maximumConversionSourceNumber = 150,
                allowDecimalValues = false,
                decimalStep = 0.5f,
                preferDecimalDisplayForPracticalQuestions = false
            };

            hard = new MeasurementDifficultyProfile
            {
                secondsPerQuestion = 55f,
                conversionQuestionChance = 0.55f,
                allowedWeightValuesInGrams =
                    new[] { 25, 50, 100, 200, 500, 1000 },
                minimumWeightsInSolution = 2,
                maximumWeightsInSolution = 7,
                minimumMassTargetInGrams = 200,
                maximumMassTargetInGrams = 3500,
                containerCapacityInMillilitres = 2000,
                liquidStepInMillilitres = 50,
                minimumLiquidTargetInMillilitres = 100,
                maximumLiquidTargetInMillilitres = 2000,
                allowedMassUnits =
                    MassUnitSet.Milligram | MassUnitSet.Gram | MassUnitSet.Kilogram,
                allowedLiquidUnits =
                    LiquidUnitSet.Millilitre | LiquidUnitSet.Centilitre |
                    LiquidUnitSet.Litre,
                conversionStyle = ConversionQuestionStyle.Mixed,
                conversionOptionCount = 4,
                minimumConversionSourceNumber = 1,
                maximumConversionSourceNumber = 200,
                allowDecimalValues = true,
                decimalStep = 0.5f,
                preferDecimalDisplayForPracticalQuestions = true
            };
        }

        private void OnValidate()
        {
            questionsPerRun = Mathf.Clamp(questionsPerRun, 1, 5);
            ValidateProfile(easy);
            ValidateProfile(normal);
            ValidateProfile(hard);
        }

        private static void ValidateProfile(MeasurementDifficultyProfile profile)
        {
            if (profile == null)
                return;

            profile.secondsPerQuestion = Mathf.Max(10f, profile.secondsPerQuestion);
            profile.minimumWeightsInSolution =
                Mathf.Clamp(profile.minimumWeightsInSolution, 1, 8);
            profile.maximumWeightsInSolution = Mathf.Clamp(
                profile.maximumWeightsInSolution,
                profile.minimumWeightsInSolution,
                8);
            profile.maximumMassTargetInGrams = Mathf.Max(
                profile.minimumMassTargetInGrams,
                profile.maximumMassTargetInGrams);
            profile.containerCapacityInMillilitres =
                Mathf.Max(100, profile.containerCapacityInMillilitres);
            profile.liquidStepInMillilitres =
                Mathf.Max(1, profile.liquidStepInMillilitres);
            profile.containerCapacityInMillilitres = Mathf.Max(
                profile.liquidStepInMillilitres,
                Mathf.RoundToInt(
                    profile.containerCapacityInMillilitres /
                    (float)profile.liquidStepInMillilitres) *
                profile.liquidStepInMillilitres);
            profile.minimumLiquidTargetInMillilitres = Mathf.Clamp(
                profile.minimumLiquidTargetInMillilitres,
                1,
                profile.containerCapacityInMillilitres);
            profile.maximumLiquidTargetInMillilitres = Mathf.Clamp(
                profile.maximumLiquidTargetInMillilitres,
                profile.minimumLiquidTargetInMillilitres,
                profile.containerCapacityInMillilitres);
            profile.conversionOptionCount =
                Mathf.Clamp(profile.conversionOptionCount, 3, 4);
            profile.maximumConversionSourceNumber = Mathf.Max(
                profile.minimumConversionSourceNumber + 1,
                profile.maximumConversionSourceNumber);

            if (CountFlags((int)profile.allowedMassUnits) < 2)
                profile.allowedMassUnits =
                    MassUnitSet.Gram | MassUnitSet.Kilogram;
            if (CountFlags((int)profile.allowedLiquidUnits) < 2)
                profile.allowedLiquidUnits =
                    LiquidUnitSet.Millilitre | LiquidUnitSet.Litre;
        }

        private static int CountFlags(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }
            return count;
        }
    }
}
