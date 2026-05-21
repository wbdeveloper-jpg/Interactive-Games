using System;
using UnityEngine;

namespace NGEducation.MemoryMatch
{
    public sealed class MemoryCountdownTimer : MonoBehaviour
    {
        public event Action<float, float> TimeChanged;
        public event Action<bool> WarningStateChanged;
        public event Action TimeExpired;

        private bool configured;
        private bool running;
        private bool paused;
        private bool timerEnabled;
        private bool isWarning;

        private float totalSeconds;
        private float remainingSeconds;
        private float warningRemainingPercent;

        public bool TimerEnabled => timerEnabled;
        public bool IsRunning => running;
        public bool IsPaused => paused;
        public bool IsWarning => isWarning;
        public float TotalSeconds => totalSeconds;
        public float RemainingSeconds => remainingSeconds;
        public float ElapsedSeconds => Mathf.Max(0f, totalSeconds - remainingSeconds);

        private void Update()
        {
            if (!configured || !timerEnabled || !running || paused)
            {
                return;
            }

            remainingSeconds -= Time.deltaTime;

            if (remainingSeconds <= 0f)
            {
                remainingSeconds = 0f;
                running = false;
                UpdateWarningState();
                NotifyTimeChanged();
                TimeExpired?.Invoke();
                return;
            }

            UpdateWarningState();
            NotifyTimeChanged();
        }

        public void Configure(bool enabled, float durationSeconds, float warningPercent)
        {
            timerEnabled = enabled;
            totalSeconds = Mathf.Max(1f, durationSeconds);
            remainingSeconds = totalSeconds;
            warningRemainingPercent = Mathf.Clamp(warningPercent, 0.01f, 0.75f);
            configured = true;
            running = false;
            paused = false;
            isWarning = false;

            NotifyTimeChanged();
            WarningStateChanged?.Invoke(false);
        }

        public void StartTimer()
        {
            if (!configured || !timerEnabled)
            {
                return;
            }

            running = true;
            paused = false;
            UpdateWarningState();
            NotifyTimeChanged();
        }

        public void PauseTimer()
        {
            if (!timerEnabled)
            {
                return;
            }

            paused = true;
        }

        public void ResumeTimer()
        {
            if (!timerEnabled || remainingSeconds <= 0f)
            {
                return;
            }

            paused = false;
            running = true;
        }

        public void StopTimer()
        {
            running = false;
            paused = false;
        }

        public void ResetTimer()
        {
            remainingSeconds = totalSeconds;
            running = false;
            paused = false;
            isWarning = false;
            NotifyTimeChanged();
            WarningStateChanged?.Invoke(false);
        }

        private void UpdateWarningState()
        {
            bool nextWarning =
                timerEnabled &&
                totalSeconds > 0f &&
                remainingSeconds > 0f &&
                remainingSeconds / totalSeconds <= warningRemainingPercent;

            if (nextWarning == isWarning)
            {
                return;
            }

            isWarning = nextWarning;
            WarningStateChanged?.Invoke(isWarning);
        }

        private void NotifyTimeChanged()
        {
            float normalized = totalSeconds <= 0f ? 0f : Mathf.Clamp01(remainingSeconds / totalSeconds);
            TimeChanged?.Invoke(remainingSeconds, normalized);
        }
    }
}
