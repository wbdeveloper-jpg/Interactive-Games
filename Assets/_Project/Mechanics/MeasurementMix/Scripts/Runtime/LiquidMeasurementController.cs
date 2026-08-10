using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MeasurementMix
{
    public class LiquidMeasurementController : MonoBehaviour
    {
        [Header("Beaker")]
        public RectTransform liquidArea;
        public RectTransform waterFill;
        public CanvasGroup targetLineGroup;
        public RectTransform targetLine;
        public Image waterStream;

        [Header("Labels")]
        public TMP_Text targetLabel;
        public TMP_Text currentVolumeLabel;
        public TMP_Text stepLabel;
        public List<RectTransform> scaleMarks = new List<RectTransform>();
        public List<TMP_Text> scaleLabels = new List<TMP_Text>();

        [Header("Controls")]
        public Button addWaterButton;
        public Button removeWaterButton;

        [Header("Animation")]
        [Min(0.1f)] public float waterAnimationDuration = 0.3f;

        [Header("Optional")]
        public MeasurementAudioManager audioManager;

        public bool InteractionsEnabled { get; private set; }
        public int CurrentVolumeInMillilitres { get; private set; }
        public int TargetVolumeInMillilitres { get; private set; }
        public int CapacityInMillilitres { get; private set; } = 1000;
        public int StepInMillilitres { get; private set; } = 100;

        private float liquidAreaHeight;
        private Tween temporaryHintTween;
        private bool preferDecimalDisplay;

        private void Awake()
        {
            if (liquidArea != null)
                liquidAreaHeight = liquidArea.rect.height;

            if (addWaterButton != null)
                addWaterButton.onClick.AddListener(AddWater);
            if (removeWaterButton != null)
                removeWaterButton.onClick.AddListener(RemoveWater);
        }

        private void OnDisable()
        {
            temporaryHintTween?.Kill();
        }

        public void PrepareQuestion(
            MeasurementQuestion question,
            MeasurementDifficultyProfile profile)
        {
            CapacityInMillilitres =
                Mathf.Max(100, profile.containerCapacityInMillilitres);
            StepInMillilitres = Mathf.Max(1, profile.liquidStepInMillilitres);
            preferDecimalDisplay =
                profile.preferDecimalDisplayForPracticalQuestions;
            TargetVolumeInMillilitres = Mathf.Clamp(
                question.targetLiquidInMillilitres,
                0,
                CapacityInMillilitres);
            CurrentVolumeInMillilitres = 0;

            if (targetLabel != null)
            {
                targetLabel.text = string.IsNullOrEmpty(question.targetDisplay)
                    ? MeasurementQuestionGenerator.FormatPracticalLiquid(
                        TargetVolumeInMillilitres,
                        preferDecimalDisplay)
                    : question.targetDisplay;
            }

            if (stepLabel != null)
            {
                stepLabel.text = "Each tap: " +
                    MeasurementQuestionGenerator.FormatPracticalLiquid(
                        StepInMillilitres,
                        preferDecimalDisplay);
            }

            UpdateScaleLabels();
            PositionTargetLine();
            HideHintImmediate();
            RefreshWater(true);
            SetInteraction(true);
        }

        public void AddWater()
        {
            if (!InteractionsEnabled)
                return;

            CurrentVolumeInMillilitres = Mathf.Min(
                CapacityInMillilitres,
                CurrentVolumeInMillilitres + StepInMillilitres);
            audioManager?.PlayWater();
            PlayStream();
            RefreshWater(false);
        }

        public void RemoveWater()
        {
            if (!InteractionsEnabled)
                return;

            CurrentVolumeInMillilitres = Mathf.Max(
                0,
                CurrentVolumeInMillilitres - StepInMillilitres);
            audioManager?.PlayWater();
            RefreshWater(false);
        }

        public bool IsCorrect()
        {
            return CurrentVolumeInMillilitres == TargetVolumeInMillilitres;
        }

        public void SetInteraction(bool enabled)
        {
            InteractionsEnabled = enabled;
            if (addWaterButton != null)
                addWaterButton.interactable = enabled;
            if (removeWaterButton != null)
                removeWaterButton.interactable = enabled;
        }

        public void RevealTargetLine(bool keepVisible, float duration)
        {
            temporaryHintTween?.Kill();
            if (targetLineGroup == null)
                return;

            targetLineGroup.gameObject.SetActive(true);
            targetLineGroup.DOKill();
            targetLineGroup.DOFade(1f, 0.28f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            if (targetLine != null)
            {
                targetLine.DOKill();
                targetLine.localScale = Vector3.one;
                targetLine.DOPunchScale(Vector3.one * 0.16f, 0.55f, 6, 0.6f)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (!keepVisible)
            {
                temporaryHintTween = DOVirtual.DelayedCall(
                        Mathf.Max(1f, duration),
                        HideHintAnimated)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
        }

        public void HideHintImmediate()
        {
            temporaryHintTween?.Kill();
            if (targetLineGroup == null)
                return;

            targetLineGroup.DOKill();
            targetLineGroup.alpha = 0f;
            targetLineGroup.gameObject.SetActive(false);
        }

        private void HideHintAnimated()
        {
            if (targetLineGroup == null)
                return;

            targetLineGroup.DOKill();
            targetLineGroup.DOFade(0f, 0.25f)
                .OnComplete(() => targetLineGroup.gameObject.SetActive(false))
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void RefreshWater(bool immediate)
        {
            if (liquidArea != null && liquidAreaHeight <= 0f)
                liquidAreaHeight = liquidArea.rect.height;

            float normalised = CurrentVolumeInMillilitres /
                (float)Mathf.Max(1, CapacityInMillilitres);
            Vector2 targetSize = waterFill != null
                ? waterFill.sizeDelta
                : Vector2.zero;
            targetSize.y = liquidAreaHeight * Mathf.Clamp01(normalised);

            if (waterFill != null)
            {
                waterFill.DOKill();
                if (immediate)
                    waterFill.sizeDelta = targetSize;
                else
                    waterFill.DOSizeDelta(targetSize, waterAnimationDuration)
                        .SetEase(Ease.OutCubic)
                        .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }

            if (currentVolumeLabel != null)
            {
                currentVolumeLabel.text = "Current: " +
                    MeasurementQuestionGenerator.FormatPracticalLiquid(
                        CurrentVolumeInMillilitres,
                        preferDecimalDisplay);
            }
        }

        private void PositionTargetLine()
        {
            if (targetLine == null || liquidArea == null)
                return;

            if (liquidAreaHeight <= 0f)
                liquidAreaHeight = liquidArea.rect.height;

            float normalised = TargetVolumeInMillilitres /
                (float)Mathf.Max(1, CapacityInMillilitres);
            Vector2 position = targetLine.anchoredPosition;
            position.y = liquidAreaHeight * Mathf.Clamp01(normalised);
            targetLine.anchoredPosition = position;
        }

        private void UpdateScaleLabels()
        {
            int availableCount = Mathf.Min(scaleMarks.Count, scaleLabels.Count);
            if (availableCount <= 1 || liquidArea == null)
                return;

            int requestedDivisions = Mathf.Max(
                1,
                CapacityInMillilitres / Mathf.Max(1, StepInMillilitres));
            int shownDivisions = Mathf.Min(
                requestedDivisions,
                availableCount - 1);
            int representedSteps = Mathf.Max(
                1,
                Mathf.CeilToInt(requestedDivisions / (float)shownDivisions));
            int majorEvery = Mathf.Max(1, shownDivisions / 4);

            for (int index = 0; index < availableCount; index++)
            {
                RectTransform mark = scaleMarks[index];
                TMP_Text label = scaleLabels[index];
                bool visible = index <= shownDivisions;
                if (mark != null)
                    mark.gameObject.SetActive(visible);
                if (label != null)
                    label.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                float normalised = index / (float)shownDivisions;
                float localY = -liquidAreaHeight * 0.5f +
                    liquidAreaHeight * normalised;
                bool major = index == 0 ||
                    index == shownDivisions ||
                    index % majorEvery == 0;

                if (mark != null)
                {
                    Vector2 markPosition = mark.anchoredPosition;
                    markPosition.y = localY;
                    mark.anchoredPosition = markPosition;
                    Vector2 markSize = mark.sizeDelta;
                    markSize.x = major ? 58f : 34f;
                    mark.sizeDelta = markSize;
                }

                if (label != null)
                {
                    label.gameObject.SetActive(major);
                    int value = index == shownDivisions
                        ? CapacityInMillilitres
                        : Mathf.Min(
                            CapacityInMillilitres,
                            index * StepInMillilitres * representedSteps);
                    label.text =
                        MeasurementQuestionGenerator.FormatPracticalLiquid(
                            value,
                            preferDecimalDisplay);
                    Vector2 labelPosition = label.rectTransform.anchoredPosition;
                    labelPosition.y = liquidArea.anchoredPosition.y + localY;
                    label.rectTransform.anchoredPosition = labelPosition;
                }
            }
        }

        private void PlayStream()
        {
            if (waterStream == null)
                return;

            waterStream.DOKill();
            Color colour = waterStream.color;
            colour.a = 0f;
            waterStream.color = colour;
            waterStream.gameObject.SetActive(true);

            Sequence sequence = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(waterStream.DOFade(1f, 0.08f));
            sequence.AppendInterval(0.12f);
            sequence.Append(waterStream.DOFade(0f, 0.12f));
            sequence.OnComplete(() => waterStream.gameObject.SetActive(false));
        }
    }
}
