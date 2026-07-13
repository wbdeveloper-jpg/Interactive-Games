using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace ClockLearningGame
{
    public enum ClockLearningHandType
    {
        None,
        Hour,
        Minute
    }

    public enum ClockLearningHandRelationMode
    {
        RealisticLinked,
        IndependentHands
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ClockLearningClockView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const float HandVisualReferenceRadius = 240f;
        private const float MinHandResponsiveScale = 0.65f;
        private const float MaxHandResponsiveScale = 1.6f;
        private const float MaxHourHandHeightByRadius = 0.56f;
        private const float MaxMinuteHandHeightByRadius = 0.82f;

        [Header("Clock Parts")]
        [SerializeField] private RectTransform clockFace;
        [SerializeField] private RectTransform numbersRoot;
        [SerializeField] private RectTransform ticksRoot;
        [SerializeField] private RectTransform hourHand;
        [SerializeField] private RectTransform minuteHand;
        [SerializeField] private TextMeshProUGUI timeLabel;

        [Header("Clock Behavior")]
        [Tooltip("RealisticLinked: hour hand moves between numbers as minutes change. IndependentHands: hour hand stays exactly on the chosen hour.")]
        [SerializeField] private ClockLearningHandRelationMode handRelationMode = ClockLearningHandRelationMode.RealisticLinked;
        [SerializeField] private bool smoothDrivenHand = true;
        [Tooltip("When the minute hand crosses 12 in RealisticLinked mode, carry the hour forward/backward like a real clock. Turn off for easier educational set-time mode.")]
        [SerializeField] private bool carryHourWhenMinuteCrosses12 = false;
        [SerializeField, Range(0.02f, 0.35f)] private float drivenHandSmoothDuration = 0.08f;

        [Header("Interaction")]
        [SerializeField] private bool draggable = true;
        [SerializeField] private bool allowHourHandDrag = true;
        [SerializeField] private bool allowMinuteHandDrag = true;
        [SerializeField, Min(1)] private int minuteSnapInterval = 5;
        [SerializeField] private bool showDebugTimeLabel;

        [Header("Responsive Mark Spacing")]
        [Tooltip("Moves clock numbers inward so they do not overlap a sprite frame/border.")]
        [SerializeField, Range(0.05f, 0.35f)] private float numberInsetFromClockEdge = 0.24f;
        [Tooltip("Moves minute ticks inward so they do not overlap a sprite frame/border.")]
        [SerializeField, Range(0.03f, 0.25f)] private float tickInsetFromClockEdge = 0.14f;
        [Tooltip("Extra pixel padding for imported clock-face sprites with thick decorative borders.")]
        [SerializeField, Min(0f)] private float extraMarkInsetPixels = 6f;

        [Header("Responsive Hand Visual Size")]
        [Tooltip("Visual width for the hour hand at the reference clock size. It scales responsively and keeps the sprite aspect ratio.")]
        [SerializeField, Min(1f)] private float hourHandWidth = 18f;
        [Tooltip("Visual height for the hour hand at the reference clock size. It is capped internally so it cannot overflow the clock.")]
        [SerializeField, Min(1f)] private float hourHandHeight = 120f;
        [Tooltip("Visual width for the minute hand at the reference clock size. Increase this when your custom sprite looks too thin.")]
        [SerializeField, Min(1f)] private float minuteHandWidth = 12f;
        [Tooltip("Visual height for the minute hand at the reference clock size. It is capped internally so it cannot overflow the clock.")]
        [SerializeField, Min(1f)] private float minuteHandHeight = 170f;

        [Header("Generated Placeholder Style")]
        [SerializeField] private Color faceColor = new Color(1f, 0.96f, 0.78f, 1f);
        [SerializeField] private Color numberColor = new Color(0.22f, 0.18f, 0.12f, 1f);
        [SerializeField] private Color tickColor = new Color(0.45f, 0.35f, 0.18f, 1f);
        [SerializeField] private Color hourHandColor = new Color(0.24f, 0.18f, 0.1f, 1f);
        [SerializeField] private Color minuteHandColor = new Color(0.95f, 0.48f, 0.14f, 1f);

        [SerializeField] private List<TextMeshProUGUI> numberLabels = new List<TextMeshProUGUI>();
        [SerializeField] private List<RectTransform> tickMarks = new List<RectTransform>();

        private RectTransform _rectTransform;
        private ClockLearningHandType _activeHand = ClockLearningHandType.None;
        private int _hour0To11;
        private int _minute;

        public event Action<int, int> TimeChanged;
        public event Action<ClockLearningClockView, ClockLearningHandType> UserChangedTimeByDrag;

        public int Hour1To12 => _hour0To11 == 0 ? 12 : _hour0To11;
        public int Minute => _minute;
        public int TotalMinutes12 => (_hour0To11 * 60) + _minute;
        public ClockLearningHandRelationMode HandRelationMode => handRelationMode;
        public bool CarryHourWhenMinuteCrosses12 => carryHourWhenMinuteCrosses12;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;

            if (clockFace == null || hourHand == null || minuteHand == null)
            {
                BuildPlaceholderClock();
            }

            UpdateStaticMarks();
            UpdateHands(false, ClockLearningHandType.None);
        }

        private void OnValidate()
        {
            minuteSnapInterval = Mathf.Clamp(minuteSnapInterval, 1, 30);
            drivenHandSmoothDuration = Mathf.Clamp(drivenHandSmoothDuration, 0.02f, 0.35f);
            numberInsetFromClockEdge = Mathf.Clamp(numberInsetFromClockEdge, 0.05f, 0.35f);
            tickInsetFromClockEdge = Mathf.Clamp(tickInsetFromClockEdge, 0.03f, 0.25f);
            extraMarkInsetPixels = Mathf.Max(0f, extraMarkInsetPixels);
            hourHandWidth = Mathf.Max(1f, hourHandWidth);
            hourHandHeight = Mathf.Max(1f, hourHandHeight);
            minuteHandWidth = Mathf.Max(1f, minuteHandWidth);
            minuteHandHeight = Mathf.Max(1f, minuteHandHeight);

            if (clockFace != null && hourHand != null && minuteHand != null)
            {
                UpdateStaticMarks();
                UpdateHands(false, ClockLearningHandType.None);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!isActiveAndEnabled) return;
            UpdateStaticMarks();
            UpdateHands(false, ClockLearningHandType.None);
        }

        private void OnDisable()
        {
            KillHandTweens(hourHand);
            KillHandTweens(minuteHand);
            transform.DOKill();
        }

        public void SetDraggable(bool value)
        {
            draggable = value;
        }

        public void SetHandRelationMode(ClockLearningHandRelationMode mode)
        {
            handRelationMode = mode;
            UpdateHands(false, ClockLearningHandType.None);
        }

        public void SetDrivenHandSmoothing(bool enabled, float duration)
        {
            smoothDrivenHand = enabled;
            drivenHandSmoothDuration = Mathf.Clamp(duration, 0.02f, 0.35f);
        }

        public void SetCarryHourWhenMinuteCrosses12(bool enabled)
        {
            carryHourWhenMinuteCrosses12 = enabled;
        }

        public void SetVisualStyle(
            float numberInset,
            float tickInset,
            float extraInsetPixels,
            float hourWidth,
            float hourHeight,
            float minuteWidth,
            float minuteHeight)
        {
            numberInsetFromClockEdge = Mathf.Clamp(numberInset, 0.05f, 0.35f);
            tickInsetFromClockEdge = Mathf.Clamp(tickInset, 0.03f, 0.25f);
            extraMarkInsetPixels = Mathf.Max(0f, extraInsetPixels);
            hourHandWidth = Mathf.Max(1f, hourWidth);
            hourHandHeight = Mathf.Max(1f, hourHeight);
            minuteHandWidth = Mathf.Max(1f, minuteWidth);
            minuteHandHeight = Mathf.Max(1f, minuteHeight);

            UpdateStaticMarks();
            UpdateHands(false, ClockLearningHandType.None);
        }

        public void ForceVisualRefresh()
        {
            UpdateStaticMarks();
            UpdateHands(false, ClockLearningHandType.None);
        }

        public void SetMinuteSnapInterval(int snapInterval)
        {
            minuteSnapInterval = Mathf.Clamp(snapInterval, 1, 30);
            _minute = SnapValue(_minute, minuteSnapInterval, 60);
            UpdateHands(true, ClockLearningHandType.None);
        }

        public void SetTime(int hour1To12, int minute, bool animate = false)
        {
            int safeHour = Mathf.Clamp(hour1To12, 1, 12);
            _hour0To11 = safeHour == 12 ? 0 : safeHour;
            _minute = Mathf.Clamp(minute, 0, 59);
            _minute = SnapValue(_minute, minuteSnapInterval, 60);
            UpdateHands(animate, ClockLearningHandType.None);
            TimeChanged?.Invoke(Hour1To12, _minute);
        }

        public void SetRandomTime(bool animate = false)
        {
            int randomHour = UnityEngine.Random.Range(1, 13);
            int randomMinute = SnapValue(UnityEngine.Random.Range(0, 60), minuteSnapInterval, 60);
            SetTime(randomHour, randomMinute, animate);
        }

        public int GetTotalMinutes24(bool isPm)
        {
            int hour24 = isPm ? _hour0To11 + 12 : _hour0To11;
            return (hour24 * 60) + _minute;
        }

        public string GetFormattedTime(bool useAmPm, bool isPm)
        {
            string suffix = useAmPm ? (isPm ? " PM" : " AM") : string.Empty;
            return $"{Hour1To12}:{_minute:00}{suffix}";
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!draggable) return;
            _activeHand = PickNearestHand(eventData);
            PunchSelectedHand();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!draggable || _activeHand == ClockLearningHandType.None) return;
            UpdateFromPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _activeHand = ClockLearningHandType.None;
        }

        private ClockLearningHandType PickNearestHand(PointerEventData eventData)
        {
            float pointerAngle = GetClockwiseAngle(eventData);
            float hourAngle = GetHourHandAngle();
            float minuteAngle = _minute * 6f;

            float hourDistance = Mathf.Abs(Mathf.DeltaAngle(pointerAngle, hourAngle));
            float minuteDistance = Mathf.Abs(Mathf.DeltaAngle(pointerAngle, minuteAngle));

            if (!allowHourHandDrag && allowMinuteHandDrag) return ClockLearningHandType.Minute;
            if (!allowMinuteHandDrag && allowHourHandDrag) return ClockLearningHandType.Hour;
            if (!allowHourHandDrag && !allowMinuteHandDrag) return ClockLearningHandType.None;

            return hourDistance <= minuteDistance ? ClockLearningHandType.Hour : ClockLearningHandType.Minute;
        }

        private void UpdateFromPointer(PointerEventData eventData)
        {
            float angle = GetClockwiseAngle(eventData);

            if (_activeHand == ClockLearningHandType.Minute)
            {
                int previousMinute = _minute;
                int newMinute = Mathf.RoundToInt(angle / 6f) % 60;
                int snappedMinute = SnapValue(newMinute, minuteSnapInterval, 60);

                if (handRelationMode == ClockLearningHandRelationMode.RealisticLinked && carryHourWhenMinuteCrosses12)
                {
                    ApplyHourCarryFromMinuteDrag(previousMinute, snappedMinute);
                }

                _minute = snappedMinute;
            }
            else if (_activeHand == ClockLearningHandType.Hour)
            {
                if (handRelationMode == ClockLearningHandRelationMode.RealisticLinked)
                {
                    int totalMinutes = Mathf.RoundToInt(angle / 0.5f) % 720;
                    totalMinutes = SnapValue(totalMinutes, minuteSnapInterval, 720);
                    _hour0To11 = Mathf.FloorToInt(totalMinutes / 60f) % 12;
                    _minute = totalMinutes % 60;
                }
                else
                {
                    int newHour = Mathf.RoundToInt(angle / 30f) % 12;
                    _hour0To11 = newHour;
                }
            }

            UpdateHands(false, _activeHand);
            TimeChanged?.Invoke(Hour1To12, _minute);
            UserChangedTimeByDrag?.Invoke(this, _activeHand);
        }

        private float GetClockwiseAngle(PointerEventData eventData)
        {
            RectTransform face = clockFace != null ? clockFace : _rectTransform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(face, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            float angle = Mathf.Atan2(localPoint.x, localPoint.y) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            return angle;
        }

        private void ApplyHourCarryFromMinuteDrag(int previousMinute, int newMinute)
        {
            if (previousMinute == newMinute) return;

            float previousAngle = previousMinute * 6f;
            float newAngle = newMinute * 6f;
            float signedDelta = Mathf.DeltaAngle(previousAngle, newAngle);

            // Clockwise drag across 12: 7:55 -> 8:00. Counter-clockwise drag across 12: 8:00 -> 7:55.
            if (newMinute < previousMinute && signedDelta > 0f)
            {
                _hour0To11 = (_hour0To11 + 1) % 12;
            }
            else if (newMinute > previousMinute && signedDelta < 0f)
            {
                _hour0To11 = (_hour0To11 + 11) % 12;
            }
        }

        private void UpdateHands(bool animate, ClockLearningHandType handBeingDragged)
        {
            if (hourHand == null || minuteHand == null) return;

            float hourAngle = GetHourHandAngle();
            float minuteAngle = _minute * 6f;

            bool smoothHour = ShouldSmoothDrivenHand(handBeingDragged, ClockLearningHandType.Hour);
            bool smoothMinute = ShouldSmoothDrivenHand(handBeingDragged, ClockLearningHandType.Minute);

            SetHandRotation(hourHand, hourAngle, animate || smoothHour, smoothHour ? drivenHandSmoothDuration : 0.18f);
            SetHandRotation(minuteHand, minuteAngle, animate || smoothMinute, smoothMinute ? drivenHandSmoothDuration : 0.18f);

            if (timeLabel != null)
            {
                timeLabel.gameObject.SetActive(showDebugTimeLabel);
                timeLabel.text = GetFormattedTime(false, false);
            }
        }

        private bool ShouldSmoothDrivenHand(ClockLearningHandType handBeingDragged, ClockLearningHandType candidate)
        {
            if (!Application.isPlaying) return false;
            if (!smoothDrivenHand) return false;
            if (handRelationMode != ClockLearningHandRelationMode.RealisticLinked) return false;
            if (handBeingDragged == ClockLearningHandType.None) return false;
            if (handBeingDragged == candidate) return false;
            return true;
        }

        private void SetHandRotation(RectTransform hand, float clockwiseAngle, bool animate, float duration)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, -clockwiseAngle);
            if (animate && Application.isPlaying)
            {
                hand.DOKill();
                hand.DORotateQuaternion(targetRotation, Mathf.Max(0.01f, duration)).SetEase(Ease.OutQuad);
            }
            else
            {
                hand.localRotation = targetRotation;
            }
        }

        private float GetHourHandAngle()
        {
            if (handRelationMode == ClockLearningHandRelationMode.IndependentHands)
            {
                return _hour0To11 * 30f;
            }

            return ((_hour0To11 * 60f) + _minute) * 0.5f;
        }

        private void PunchSelectedHand()
        {
            if (!Application.isPlaying) return;
            RectTransform hand = _activeHand == ClockLearningHandType.Hour ? hourHand : minuteHand;
            if (hand == null) return;
            hand.DOKill();
            hand.DOPunchScale(Vector3.one * 0.08f, 0.12f, 5, 0.5f);
        }

        private static int SnapValue(int value, int snap, int cycle)
        {
            snap = Mathf.Max(1, snap);
            int snapped = Mathf.RoundToInt(value / (float)snap) * snap;
            snapped %= cycle;
            if (snapped < 0) snapped += cycle;
            return snapped;
        }

        [ContextMenu("Build Placeholder Clock")]
        public void BuildPlaceholderClock()
        {
            _rectTransform = (RectTransform)transform;
            ClearChildren();

            clockFace = CreateRect("Clock Face", _rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image faceImage = clockFace.gameObject.AddComponent<Image>();
            faceImage.color = faceColor;
            faceImage.raycastTarget = true;

            ticksRoot = CreateRect("Ticks", _rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            numbersRoot = CreateRect("Numbers", _rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            numberLabels.Clear();
            for (int i = 1; i <= 12; i++)
            {
                RectTransform numberRect = CreateRect($"Number {i}", numbersRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                numberRect.sizeDelta = new Vector2(52f, 42f);
                TextMeshProUGUI label = numberRect.gameObject.AddComponent<TextMeshProUGUI>();
                label.text = i.ToString();
                label.fontSize = 32f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = numberColor;
                label.raycastTarget = false;
                numberLabels.Add(label);
            }

            tickMarks.Clear();
            for (int i = 0; i < 60; i++)
            {
                RectTransform tick = CreateRect($"Tick {i:00}", ticksRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                bool major = i % 5 == 0;
                tick.sizeDelta = major ? new Vector2(5f, 18f) : new Vector2(2f, 9f);
                Image tickImage = tick.gameObject.AddComponent<Image>();
                tickImage.color = tickColor;
                tickImage.raycastTarget = false;
                tickMarks.Add(tick);
            }

            hourHand = CreateHand("Hour Hand", _rectTransform, hourHandColor, hourHandWidth, hourHandHeight, 26f);
            minuteHand = CreateHand("Minute Hand", _rectTransform, minuteHandColor, minuteHandWidth, minuteHandHeight, 28f);

            RectTransform centerDot = CreateRect("Center Dot", _rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            centerDot.sizeDelta = new Vector2(34f, 34f);
            Image centerImage = centerDot.gameObject.AddComponent<Image>();
            centerImage.color = numberColor;
            centerImage.raycastTarget = false;

            RectTransform debugTime = CreateRect("Debug Time Label", _rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-75f, 10f), new Vector2(75f, 42f));
            timeLabel = debugTime.gameObject.AddComponent<TextMeshProUGUI>();
            timeLabel.alignment = TextAlignmentOptions.Center;
            timeLabel.fontSize = 24f;
            timeLabel.color = numberColor;
            timeLabel.raycastTarget = false;
            timeLabel.gameObject.SetActive(showDebugTimeLabel);

            UpdateStaticMarks();
            UpdateHands(false, ClockLearningHandType.None);
        }

        private RectTransform CreateHand(string objectName, RectTransform parent, Color color, float width, float length, float arrowFontSize)
        {
            RectTransform hand = CreateRect(objectName, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hand.pivot = new Vector2(0.5f, 0f);
            hand.sizeDelta = new Vector2(width, length);
            Image image = hand.gameObject.AddComponent<Image>();
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;

            RectTransform arrow = CreateRect("Arrow Head", hand, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            arrow.sizeDelta = new Vector2(48f, 42f);
            arrow.anchoredPosition = new Vector2(0f, 10f);
            TextMeshProUGUI arrowText = arrow.gameObject.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▲";
            arrowText.fontSize = arrowFontSize;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = color;
            arrowText.raycastTarget = false;
            return hand;
        }

        private void UpdateStaticMarks()
        {
            RectTransform face = clockFace != null ? clockFace : _rectTransform;
            if (face == null) return;

            float radius = Mathf.Min(face.rect.width, face.rect.height) * 0.5f;
            if (radius <= 0f) return;

            float numberRadius = Mathf.Max(0f, radius - (radius * numberInsetFromClockEdge) - extraMarkInsetPixels);
            for (int i = 0; i < numberLabels.Count; i++)
            {
                if (numberLabels[i] == null) continue;
                int number = i + 1;
                float angle = number * 30f;
                Vector2 position = ClockPosition(angle, numberRadius);
                RectTransform labelRect = (RectTransform)numberLabels[i].transform;
                labelRect.anchoredPosition = position;
                labelRect.sizeDelta = new Vector2(Mathf.Clamp(radius * 0.23f, 40f, 68f), Mathf.Clamp(radius * 0.18f, 34f, 56f));
                numberLabels[i].fontSize = Mathf.Clamp(radius * 0.13f, 20f, 42f);
            }

            float tickRadius = Mathf.Max(0f, radius - (radius * tickInsetFromClockEdge) - extraMarkInsetPixels);
            for (int i = 0; i < tickMarks.Count; i++)
            {
                if (tickMarks[i] == null) continue;
                float angle = i * 6f;
                bool major = i % 5 == 0;
                tickMarks[i].sizeDelta = major
                    ? new Vector2(Mathf.Clamp(radius * 0.018f, 3f, 6f), Mathf.Clamp(radius * 0.065f, 12f, 22f))
                    : new Vector2(Mathf.Clamp(radius * 0.008f, 1.5f, 3f), Mathf.Clamp(radius * 0.035f, 6f, 12f));
                tickMarks[i].anchoredPosition = ClockPosition(angle, tickRadius);
                tickMarks[i].localRotation = Quaternion.Euler(0f, 0f, -angle);
            }

            ApplyResponsiveHandVisualSize(hourHand, radius, hourHandWidth, hourHandHeight, MaxHourHandHeightByRadius);
            ApplyResponsiveHandVisualSize(minuteHand, radius, minuteHandWidth, minuteHandHeight, MaxMinuteHandHeightByRadius);
        }

        private void ApplyResponsiveHandVisualSize(RectTransform hand, float radius, float widthAtReferenceSize, float heightAtReferenceSize, float maxHeightByRadius)
        {
            if (hand == null) return;

            float responsiveScale = Mathf.Clamp(radius / HandVisualReferenceRadius, MinHandResponsiveScale, MaxHandResponsiveScale);
            float finalWidth = Mathf.Max(1f, widthAtReferenceSize * responsiveScale);
            float maxHeight = Mathf.Max(1f, radius * maxHeightByRadius);
            float finalHeight = Mathf.Min(Mathf.Max(1f, heightAtReferenceSize * responsiveScale), maxHeight);

            hand.sizeDelta = new Vector2(finalWidth, finalHeight);

            Image handImage = hand.GetComponent<Image>();
            if (handImage != null)
            {
                handImage.preserveAspect = true;
            }

            RectTransform arrow = hand.Find("Arrow Head") as RectTransform;
            if (arrow != null)
            {
                arrow.anchoredPosition = new Vector2(0f, Mathf.Clamp(finalHeight * 0.04f, 6f, 14f));
            }
        }

        private static Vector2 ClockPosition(float clockwiseAngle, float radius)
        {
            float radians = clockwiseAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)) * radius;
        }

        private static RectTransform CreateRect(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void KillHandTweens(RectTransform hand)
        {
            if (hand == null) return;
            hand.DOKill();
            hand.transform.DOKill();
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
