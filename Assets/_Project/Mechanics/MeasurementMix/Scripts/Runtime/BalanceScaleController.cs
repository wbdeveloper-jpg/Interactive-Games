using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace MeasurementMix
{
    public class BalanceScaleController : MonoBehaviour
    {
        [Header("Scale")]
        public RectTransform beam;
        public RectTransform leftPan;
        public RectTransform rightPan;
        public RectTransform rightPanDropArea;
        public RectTransform rightPanContent;
        public TMP_Text targetLabel;
        public TMP_Text currentWeightLabel;

        [Header("Tokens")]
        public List<MeasurementWeightItem> weightItems =
            new List<MeasurementWeightItem>();

        [Header("Animation")]
        [Range(3f, 14f)] public float maximumTilt = 8f;
        [Min(5f)] public float panTravel = 30f;
        [Min(0.1f)] public float movementDuration = 0.32f;

        [Header("Optional")]
        public MeasurementAudioManager audioManager;

        public bool InteractionsEnabled { get; private set; }
        public int CurrentWeightInGrams { get; private set; }
        public int TargetWeightInGrams { get; private set; }

        private readonly List<int> allTokenValues = new List<int>(16);
        private readonly Dictionary<int, int> requiredHintCounts =
            new Dictionary<int, int>();
        private Vector2 leftPanStart;
        private Vector2 rightPanStart;
        private bool preferDecimalDisplay;

        private void Awake()
        {
            if (leftPan != null)
                leftPanStart = leftPan.anchoredPosition;
            if (rightPan != null)
                rightPanStart = rightPan.anchoredPosition;
        }

        public IReadOnlyList<int> GetAllTokenValues()
        {
            allTokenValues.Clear();
            for (int index = 0; index < weightItems.Count; index++)
            {
                MeasurementWeightItem item = weightItems[index];
                if (item != null)
                    allTokenValues.Add(item.valueInGrams);
            }
            return allTokenValues;
        }

        public void PrepareQuestion(
            MeasurementQuestion question,
            MeasurementDifficultyProfile profile)
        {
            ClearHint();
            ResetTokens();
            ApplyAllowedValues(profile.allowedWeightValuesInGrams);
            TargetWeightInGrams = question.targetMassInGrams;
            CurrentWeightInGrams = 0;
            preferDecimalDisplay =
                profile.preferDecimalDisplayForPracticalQuestions;
            InteractionsEnabled = true;

            if (targetLabel != null)
            {
                targetLabel.text = string.IsNullOrEmpty(question.targetDisplay)
                    ? MeasurementQuestionGenerator.FormatPracticalMass(
                        TargetWeightInGrams)
                    : question.targetDisplay;
            }

            RefreshCurrentLabel();
            AnimateBalance(true);
        }

        public void HandleDrop(
            MeasurementWeightItem item,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (item == null)
                return;

            bool insidePan = rightPanDropArea != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    rightPanDropArea,
                    screenPosition,
                    eventCamera);

            if (insidePan)
                item.PlaceOnPan(rightPanContent);
            else
                item.ReturnHome();

            audioManager?.PlayWeightDrop();
            Recalculate();
        }

        public bool IsCorrect()
        {
            return CurrentWeightInGrams == TargetWeightInGrams;
        }

        public void SetInteraction(bool enabled)
        {
            InteractionsEnabled = enabled;
        }

        public void ShowHint(IReadOnlyList<int> solutionValues, int pulseCount)
        {
            ClearHint();
            requiredHintCounts.Clear();

            if (solutionValues == null)
                return;

            for (int index = 0; index < solutionValues.Count; index++)
            {
                int value = solutionValues[index];
                requiredHintCounts.TryGetValue(value, out int existing);
                requiredHintCounts[value] = existing + 1;
            }

            for (int index = 0; index < weightItems.Count; index++)
            {
                MeasurementWeightItem item = weightItems[index];
                if (item == null || !item.gameObject.activeSelf)
                    continue;

                if (!requiredHintCounts.TryGetValue(item.valueInGrams, out int count) ||
                    count <= 0)
                    continue;

                item.PlayHintAnimation(pulseCount);
                requiredHintCounts[item.valueInGrams] = count - 1;
            }
        }

        public void ClearHint()
        {
            for (int index = 0; index < weightItems.Count; index++)
                weightItems[index]?.StopHintAnimation();
        }

        private void ResetTokens()
        {
            for (int index = 0; index < weightItems.Count; index++)
            {
                MeasurementWeightItem item = weightItems[index];
                if (item == null)
                    continue;

                item.ReturnHome();
                item.SetAvailable(true);
            }

            CurrentWeightInGrams = 0;
            RefreshCurrentLabel();
        }

        private void ApplyAllowedValues(int[] values)
        {
            for (int itemIndex = 0; itemIndex < weightItems.Count; itemIndex++)
            {
                MeasurementWeightItem item = weightItems[itemIndex];
                if (item == null)
                    continue;

                bool allowed = false;
                if (values != null)
                {
                    for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
                    {
                        if (values[valueIndex] != item.valueInGrams)
                            continue;
                        allowed = true;
                        break;
                    }
                }

                item.SetAvailable(allowed);
                if (allowed)
                    item.ReturnHome();
            }
        }

        private void Recalculate()
        {
            int total = 0;
            for (int index = 0; index < weightItems.Count; index++)
            {
                MeasurementWeightItem item = weightItems[index];
                if (item != null && item.gameObject.activeSelf && item.IsOnPan)
                    total += item.valueInGrams;
            }

            CurrentWeightInGrams = total;
            RefreshCurrentLabel();
            AnimateBalance(false);
        }

        private void RefreshCurrentLabel()
        {
            if (currentWeightLabel != null)
            {
                currentWeightLabel.text = "On pan: " +
                    MeasurementQuestionGenerator.FormatPracticalMass(
                        CurrentWeightInGrams,
                        preferDecimalDisplay);
            }
        }

        private void AnimateBalance(bool immediate)
        {
            float direction = 0f;
            if (CurrentWeightInGrams < TargetWeightInGrams)
                direction = 1f;
            else if (CurrentWeightInGrams > TargetWeightInGrams)
                direction = -1f;

            float duration = immediate ? 0f : movementDuration;
            float angle = direction * maximumTilt;

            if (beam != null)
            {
                beam.DOKill();
                if (immediate)
                    beam.localRotation = Quaternion.Euler(0f, 0f, angle);
                else
                    beam.DOLocalRotate(new Vector3(0f, 0f, angle), duration)
                        .SetEase(Ease.OutCubic)
                        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            AnimatePan(
                leftPan,
                leftPanStart + Vector2.down * direction * panTravel,
                duration,
                immediate);
            AnimatePan(
                rightPan,
                rightPanStart + Vector2.up * direction * panTravel,
                duration,
                immediate);
        }

        private void AnimatePan(
            RectTransform pan,
            Vector2 target,
            float duration,
            bool immediate)
        {
            if (pan == null)
                return;

            pan.DOKill();
            if (immediate)
                pan.anchoredPosition = target;
            else
                pan.DOAnchorPos(target, duration)
                    .SetEase(Ease.OutCubic)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }
    }
}
