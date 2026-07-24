using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BehaviourWheelStop
{
    public enum BehaviourWheelContentLayoutMode
    {
        RadialPrintedOnWheel,
        UprightNoAngleRotation
    }

    public class BehaviourWheelSpinner : MonoBehaviour
    {
        private const int MinSliceCount = 3;
        private const int MaxSliceCount = 6;

        [Header("Wheel References")]
        public RectTransform wheelRoot;
        public RectTransform sliceContentRoot;
        public BehaviourWheelWheelGraphic wheelGraphic;
        [Tooltip("Optional editable center cap Image. Assign a sprite/color here for custom artwork. Leave empty to use mesh center cap colour.")]
        public Image editableCenterCapImage;
        public List<BehaviourWheelSlice> slices = new List<BehaviourWheelSlice>();

        [Header("Spin Settings")]
        [Tooltip("Degrees per second. Kid-friendly range: 150 to 220.")]
        public float spinSpeed = 180f;
        [Tooltip("1 = counter-clockwise, -1 = clockwise.")]
        public int spinDirection = -1;
        [Tooltip("ON = STOP freezes the wheel immediately. Best for kids.")]
        public bool instantStop = true;
        [Tooltip("Only used if Instant Stop is OFF.")]
        public float smoothStopDuration = 0.45f;
        public bool autoStartSpin = true;
        [Tooltip("Fixed pointer angle. Top pointer = 90 degrees.")]
        public float pointerAngle = 90f;

        [Header("Dynamic Wheel Options")]
        [SerializeField, Range(3, 6)] private int activeSliceCount = 6;
        [Tooltip("When ON, the wheel uses 3/4/5/6 slices based on the current question option count.")]
        public bool useDynamicSliceCount = true;

        [Header("Slice Content Layout")]
        [Tooltip("Radial Printed On Wheel = icon/text follows the wheel naturally. No counter-rotation, no keep-horizontal logic.")]
        public BehaviourWheelContentLayoutMode contentLayoutMode = BehaviourWheelContentLayoutMode.RadialPrintedOnWheel;
        [Tooltip("Turn this OFF when this game should use text-only wheel options. Icons stay hidden even if option data has sprites.")]
        public bool showIcons = true;
        [Tooltip("When icons are OFF, this radius is used for the label so text sits nicely in the slice.")]
        [Range(0.45f, 0.82f)] public float labelRadiusWithoutIconsMultiplier = 0.60f;
        [Tooltip("Label radius = wheel radius * this value. Higher value moves label toward the outer/wider part of the slice.")]
        [Range(0.45f, 0.82f)] public float labelRadiusMultiplier = 0.66f;
        [Tooltip("Icon radius = wheel radius * this value. Lower value moves icon toward the center cap.")]
        [Range(0.18f, 0.62f)] public float iconRadiusMultiplier = 0.42f;
        [Tooltip("Icon size = wheel radius * this value.")]
        [Range(0.06f, 0.25f)] public float iconSizeMultiplier = 0.15f;
        [Tooltip("Label width = wheel radius * this value. Increase if text still gets cut.")]
        [Range(0.30f, 1.05f)] public float labelWidthMultiplier = 0.72f;
        [Tooltip("Label height = wheel radius * this value.")]
        [Range(0.08f, 0.30f)] public float labelHeightMultiplier = 0.16f;
        [Tooltip("Optional extra angle offset for content only. Usually keep 0 so visual order matches detection.")]
        public float contentRotationOffset = 0f;

        [Header("Debug")]
        public bool showDebugSelectedSlice;
        public TMP_Text debugSelectedSliceText;

        public event Action<int, string> StoppedOnSlice;

        private bool isSpinning;
        private bool isStoppingSmoothly;
        private float smoothStopTimer;
        private float smoothStopStartSpeed;
        private float cachedWidth = -1f;
        private float cachedHeight = -1f;
        private bool cachedShowIcons;
        private float cachedLabelRadiusMultiplier;
        private float cachedLabelRadiusWithoutIconsMultiplier;
        private float cachedIconRadiusMultiplier;
        private float cachedIconSizeMultiplier;
        private float cachedLabelWidthMultiplier;
        private float cachedLabelHeightMultiplier;
        private float cachedContentRotationOffset;
        private int cachedActiveSliceCount;
        private BehaviourWheelContentLayoutMode cachedContentLayoutMode;
        private readonly List<BehaviourWheelOptionData> currentOptions = new List<BehaviourWheelOptionData>();

        public bool IsSpinning => isSpinning;
        public int ActiveSliceCount => Mathf.Clamp(activeSliceCount, MinSliceCount, MaxSliceCount);
        private float ActiveSliceAngle => 360f / ActiveSliceCount;

        private void Reset()
        {
            wheelRoot = GetComponent<RectTransform>();
        }

        private void Awake()
        {
            if (wheelRoot == null)
                wheelRoot = GetComponent<RectTransform>();
        }

        private void Start()
        {
            ApplyActiveSliceCountToWheelGraphic();
            CacheLayoutSettings();
            RebuildSliceContentLayout(true);

            if (autoStartSpin)
                StartSpin();
        }

        private void Update()
        {
            bool layoutChanged = HasLayoutSettingsChanged();
            if (layoutChanged)
                CacheLayoutSettings();

            RebuildSliceContentLayout(layoutChanged);

            if (isSpinning)
            {
                float activeSpeed = spinSpeed;

                if (isStoppingSmoothly)
                {
                    smoothStopTimer += Time.deltaTime;
                    float t = smoothStopDuration <= 0.001f ? 1f : Mathf.Clamp01(smoothStopTimer / smoothStopDuration);
                    activeSpeed = Mathf.Lerp(smoothStopStartSpeed, 0f, 1f - Mathf.Pow(1f - t, 3f));

                    if (t >= 1f)
                    {
                        isStoppingSmoothly = false;
                        isSpinning = false;
                        NotifySelectedSlice();
                        return;
                    }
                }

                float delta = spinDirection >= 0 ? activeSpeed : -activeSpeed;
                wheelRoot.Rotate(0f, 0f, delta * Time.deltaTime, Space.Self);
            }

            UpdateDebugLabel();
        }

        private bool HasLayoutSettingsChanged()
        {
            return cachedContentLayoutMode != contentLayoutMode
                || cachedShowIcons != showIcons
                || cachedActiveSliceCount != ActiveSliceCount
                || !Mathf.Approximately(cachedLabelRadiusWithoutIconsMultiplier, labelRadiusWithoutIconsMultiplier)
                || !Mathf.Approximately(cachedLabelRadiusMultiplier, labelRadiusMultiplier)
                || !Mathf.Approximately(cachedIconRadiusMultiplier, iconRadiusMultiplier)
                || !Mathf.Approximately(cachedIconSizeMultiplier, iconSizeMultiplier)
                || !Mathf.Approximately(cachedLabelWidthMultiplier, labelWidthMultiplier)
                || !Mathf.Approximately(cachedLabelHeightMultiplier, labelHeightMultiplier)
                || !Mathf.Approximately(cachedContentRotationOffset, contentRotationOffset);
        }

        private void CacheLayoutSettings()
        {
            cachedContentLayoutMode = contentLayoutMode;
            cachedShowIcons = showIcons;
            cachedActiveSliceCount = ActiveSliceCount;
            cachedLabelRadiusWithoutIconsMultiplier = labelRadiusWithoutIconsMultiplier;
            cachedLabelRadiusMultiplier = labelRadiusMultiplier;
            cachedIconRadiusMultiplier = iconRadiusMultiplier;
            cachedIconSizeMultiplier = iconSizeMultiplier;
            cachedLabelWidthMultiplier = labelWidthMultiplier;
            cachedLabelHeightMultiplier = labelHeightMultiplier;
            cachedContentRotationOffset = contentRotationOffset;
        }

        public void SetupOptions(IReadOnlyList<BehaviourWheelOptionData> options)
        {
            currentOptions.Clear();
            if (options != null)
            {
                for (int i = 0; i < options.Count && currentOptions.Count < MaxSliceCount; i++)
                {
                    BehaviourWheelOptionData option = options[i];
                    if (option != null && !string.IsNullOrWhiteSpace(option.answerText))
                        currentOptions.Add(option);
                }
            }

            while (currentOptions.Count < MinSliceCount)
                currentOptions.Add(new BehaviourWheelOptionData($"Option {currentOptions.Count + 1}"));

            activeSliceCount = useDynamicSliceCount ? Mathf.Clamp(currentOptions.Count, MinSliceCount, MaxSliceCount) : MaxSliceCount;
            ApplyActiveSliceCountToWheelGraphic();

            for (int i = 0; i < slices.Count && i < MaxSliceCount; i++)
            {
                BehaviourWheelSlice slice = slices[i];
                if (slice == null)
                    continue;

                bool active = i < ActiveSliceCount;
                if (slice.contentRoot != null)
                    slice.contentRoot.gameObject.SetActive(active);

                if (!active)
                    continue;

                slice.SetIndex(i);
                slice.SetOption(currentOptions[i], showIcons);
            }

            RebuildSliceContentLayout(true);
        }

        public void StartSpin()
        {
            isStoppingSmoothly = false;
            isSpinning = true;
        }

        /// <summary>
        /// Stops the wheel without selecting a slice or raising StoppedOnSlice.
        /// This is used while preparing isolated tutorial practice.
        /// </summary>
        public void StopSilently()
        {
            isStoppingSmoothly = false;
            isSpinning = false;
            smoothStopTimer = 0f;
        }

        public void SetRotation(float zDegrees)
        {
            if (wheelRoot == null)
                return;

            Vector3 euler = wheelRoot.localEulerAngles;
            euler.z = zDegrees;
            wheelRoot.localEulerAngles = euler;
        }

        public RectTransform GetOptionTarget(int index)
        {
            if (index < 0 || index >= slices.Count || slices[index] == null)
                return null;

            if (slices[index].labelText != null)
                return slices[index].labelText.rectTransform;

            return slices[index].contentRoot;
        }

        public void StopNow()
        {
            if (!isSpinning)
            {
                NotifySelectedSlice();
                return;
            }

            if (instantStop)
            {
                isStoppingSmoothly = false;
                isSpinning = false;
                NotifySelectedSlice();
                return;
            }

            isStoppingSmoothly = true;
            smoothStopTimer = 0f;
            smoothStopStartSpeed = Mathf.Abs(spinSpeed);
        }

        public int GetSelectedSliceIndex()
        {
            if (wheelRoot == null)
                return 0;

            float wheelRotation = NormalizeAngle(wheelRoot.localEulerAngles.z);
            float startOffset = wheelGraphic != null ? wheelGraphic.StartAngleOffset : 0f;
            float localAngleAtPointer = NormalizeAngle(pointerAngle - wheelRotation - startOffset);
            int index = Mathf.FloorToInt(localAngleAtPointer / ActiveSliceAngle);
            return Mathf.Clamp(index, 0, ActiveSliceCount - 1);
        }

        public string GetSelectedAnswer()
        {
            int index = GetSelectedSliceIndex();
            if (index >= 0 && index < currentOptions.Count)
                return currentOptions[index].answerText;

            if (index >= 0 && index < slices.Count && slices[index] != null)
                return slices[index].AnswerText;

            return string.Empty;
        }

        public void RebuildSliceContentLayout(bool force)
        {
            if (wheelRoot == null)
                return;

            Rect rect = wheelRoot.rect;
            if (!force && Mathf.Approximately(cachedWidth, rect.width) && Mathf.Approximately(cachedHeight, rect.height))
                return;

            cachedWidth = rect.width;
            cachedHeight = rect.height;

            float wheelRadius = Mathf.Min(Mathf.Abs(rect.width), Mathf.Abs(rect.height)) * 0.5f;
            if (wheelRadius <= 0.01f)
                return;

            if (sliceContentRoot != null)
            {
                sliceContentRoot.anchorMin = new Vector2(0.5f, 0.5f);
                sliceContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
                sliceContentRoot.pivot = new Vector2(0.5f, 0.5f);
                sliceContentRoot.anchoredPosition = Vector2.zero;
                sliceContentRoot.sizeDelta = new Vector2(wheelRadius * 2f, wheelRadius * 2f);
                sliceContentRoot.localRotation = Quaternion.identity;
            }

            float sliceAngle = ActiveSliceAngle;
            float startOffset = wheelGraphic != null ? wheelGraphic.StartAngleOffset : 0f;

            for (int i = 0; i < slices.Count && i < MaxSliceCount; i++)
            {
                BehaviourWheelSlice slice = slices[i];
                if (slice == null || slice.contentRoot == null)
                    continue;

                bool active = i < ActiveSliceCount;
                slice.contentRoot.gameObject.SetActive(active);
                if (!active)
                    continue;

                float middleAngle = startOffset + i * sliceAngle + sliceAngle * 0.5f + contentRotationOffset;
                float radians = middleAngle * Mathf.Deg2Rad;
                Vector2 radialDirection = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

                slice.contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
                slice.contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
                slice.contentRoot.pivot = new Vector2(0.5f, 0.5f);
                slice.contentRoot.anchoredPosition = Vector2.zero;
                slice.contentRoot.sizeDelta = new Vector2(wheelRadius * 2f, wheelRadius * 2f);
                slice.contentRoot.localRotation = Quaternion.identity;

                float printedRotation = contentLayoutMode == BehaviourWheelContentLayoutMode.RadialPrintedOnWheel
                    ? middleAngle - 90f
                    : 0f;

                if (slice.iconImage != null)
                    slice.iconImage.gameObject.SetActive(showIcons);

                if (showIcons)
                    ApplyIconLayout(slice, wheelRadius, radialDirection * (wheelRadius * iconRadiusMultiplier), printedRotation);

                float activeLabelRadius = showIcons ? labelRadiusMultiplier : labelRadiusWithoutIconsMultiplier;
                ApplyLabelLayout(slice, wheelRadius, radialDirection * (wheelRadius * activeLabelRadius), printedRotation);
            }
        }

        private void ApplyIconLayout(BehaviourWheelSlice slice, float wheelRadius, Vector2 position, float zRotation)
        {
            if (slice.iconImage == null)
                return;

            RectTransform iconRect = slice.iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);

            float iconSize = wheelRadius * iconSizeMultiplier;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = position;
            iconRect.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }

        private void ApplyLabelLayout(BehaviourWheelSlice slice, float wheelRadius, Vector2 position, float zRotation)
        {
            if (slice.labelText == null)
                return;

            RectTransform labelRect = slice.labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(wheelRadius * labelWidthMultiplier, wheelRadius * labelHeightMultiplier);
            labelRect.anchoredPosition = position;
            labelRect.localRotation = Quaternion.Euler(0f, 0f, zRotation);

            slice.labelText.alignment = TextAlignmentOptions.Center;
            slice.labelText.enableWordWrapping = false;
            slice.labelText.overflowMode = TextOverflowModes.Overflow;
            slice.labelText.enableAutoSizing = true;
            slice.labelText.fontSizeMin = 10f;
            slice.labelText.fontSizeMax = Mathf.Clamp(wheelRadius * 0.105f, 22f, 36f);
        }

        private void ApplyActiveSliceCountToWheelGraphic()
        {
            activeSliceCount = Mathf.Clamp(activeSliceCount, MinSliceCount, MaxSliceCount);
            if (wheelGraphic != null)
                wheelGraphic.SetActiveSliceCount(activeSliceCount);
        }

        private void NotifySelectedSlice()
        {
            int index = GetSelectedSliceIndex();
            string answer = GetSelectedAnswer();
            UpdateDebugLabel();
            StoppedOnSlice?.Invoke(index, answer);
        }

        private void UpdateDebugLabel()
        {
            if (!showDebugSelectedSlice || debugSelectedSliceText == null)
                return;

            int index = GetSelectedSliceIndex();
            debugSelectedSliceText.text = $"Selected Slice: {index} | {GetSelectedAnswer()}";
        }

        private static float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0f)
                angle += 360f;

            return angle;
        }
    }
}
