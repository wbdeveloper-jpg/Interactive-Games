using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DictationGame
{
    public sealed class DictationAudioManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Button playButton;
        [SerializeField] private Image playButtonIcon;
        [Tooltip("Assign your play sprite here. If empty, the generated placeholder image stays visible.")]
        [SerializeField] private Sprite playIconSprite;
        [Tooltip("Visual-only pause sprite shown while audio is playing. The button still does not pause audio.")]
        [SerializeField] private Sprite playingIconSprite;
        [SerializeField] private Color playIconReadyColor = Color.white;
        [SerializeField] private Color playIconPlayingColor = Color.white;
        [SerializeField] private TextMeshProUGUI playButtonLabel;

        [Header("Replay Icons")]
        [SerializeField] private Image[] replayIcons;
        [Tooltip("Optional sprite for available replay lives. If empty, the generated placeholder image stays.")]
        [SerializeField] private Sprite replayAvailableSprite;
        [Tooltip("Optional sprite for used replay lives. If empty, the same icon is faded by color.")]
        [SerializeField] private Sprite replayUsedSprite;
        [SerializeField] private Color iconAvailableColor = new Color(0.73f, 0.50f, 0.61f);
        [SerializeField] private Color iconUsedColor = new Color(0.73f, 0.50f, 0.61f, 0.22f);

        [Header("Playback Rules")]
        [Min(0)] [SerializeField] private int maxReplays = 2;
        [Min(0)] [SerializeField] private int replayCostPoints = 5;
        [SerializeField] private bool autoPlayOnRoundStart = true;
        [Min(0f)] [SerializeField] private float autoPlayDelay = 0.35f;
        [SerializeField] private bool disablePlayButtonWhilePlaying = true;

        [Header("Optional SFX")]
        [SerializeField] private AudioClip keyTapSfx;
        [SerializeField] private AudioClip correctSfx;
        [SerializeField] private AudioClip wrongSfx;
        [SerializeField] private AudioClip hintUsedSfx;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;

        [Header("Fake Waveform Visualizer")]
        [SerializeField] private bool showAudioVisualizer = true;
        [SerializeField] private GameObject visualizerRoot;
        [SerializeField] private TextMeshProUGUI listeningLabel;
        [SerializeField] private RectTransform[] waveformBars;
        [SerializeField] private Vector2 idleBarHeightRange = new Vector2(10f, 20f);
        [SerializeField] private Vector2 activeBarHeightRange = new Vector2(18f, 70f);
        [Min(0.1f)] [SerializeField] private float waveformSpeed = 8f;
        [Range(0f, 1f)] [SerializeField] private float waveformNoise = 0.45f;
        [SerializeField] private string idleLabelText = "Ready to listen";
        [SerializeField] private string playingLabelText = "Listening...";

        [Header("Speech-Like Visualizer Motion")]
        [Tooltip("ON = randomized speech-like bar movement. OFF = legacy rhythmic sine/noise movement.")]
        [SerializeField] private bool useSpeechLikeRandomMotion = true;
        [Tooltip("How often each bar picks a new random target height while audio plays.")]
        [SerializeField] private Vector2 speechRetargetIntervalRange = new Vector2(0.045f, 0.16f);
        [Tooltip("How quickly bars chase their random targets. Higher = snappier speech movement.")]
        [Min(1f)] [SerializeField] private float speechSmoothSpeed = 18f;
        [Tooltip("Chance that the whole visualizer dips briefly, like silence between words.")]
        [Range(0f, 1f)] [SerializeField] private float speechQuietChance = 0.16f;
        [Tooltip("Chance that the whole visualizer jumps briefly, like a stressed syllable.")]
        [Range(0f, 1f)] [SerializeField] private float speechAccentChance = 0.22f;
        [Tooltip("Makes middle bars usually taller than edge bars. 0 = flat, 1 = strong center shape.")]
        [Range(0f, 1f)] [SerializeField] private float speechCenterBias = 0.38f;
        [Tooltip("Small per-bar randomness so it does not look like a repeating equalizer pattern.")]
        [Range(0f, 1f)] [SerializeField] private float speechJitter = 0.2f;

        public event Action<int> OnReplayUsed;
        public event Action OnPlaybackStarted;
        public event Action OnPlaybackFinished;

        public bool HasAudioPlayed => playsUsed > 0;
        public bool HasAudioClip => audioSource != null && audioSource.clip != null;
        public bool IsPlaying => audioSource != null && audioSource.isPlaying;
        public int PlaysUsed => playsUsed;
        public int ReplaysUsed => Mathf.Max(0, playsUsed - 1);
        public int ReplaysRemaining => Mathf.Max(0, maxReplays - ReplaysUsed);
        public bool AutoPlayOnRoundStart => autoPlayOnRoundStart;

        private int playsUsed;
        private Coroutine autoPlayRoutine;
        private Coroutine playbackWatcherRoutine;
        private bool roundLoaded;
        private bool isPausedByGame;

        private float[] waveformCurrent01;
        private float[] waveformTarget01;
        private float[] waveformNextRetargetTime;
        private float speechEnvelope01 = 0.25f;
        private float speechTargetEnvelope01 = 0.25f;
        private float nextEnvelopeRetargetTime;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            BindButton();
            EnsureWaveformCache();
            SetVisualizerActive(showAudioVisualizer);
            SetVisualizerIdle(true);
        }

        private void Update()
        {
            UpdateWaveform();
        }

        private void OnDisable()
        {
            StopManagedCoroutines();
        }

        public void LoadRound(DictationRoundData data)
        {
            StopAudio();
            StopManagedCoroutines();

            if (audioSource == null)
            {
                Debug.LogError("[DictationAudioManager] AudioSource is missing.", this);
                return;
            }

            audioSource.clip = data != null ? data.AudioClip : null;
            playsUsed = 0;
            roundLoaded = data != null;
            isPausedByGame = false;

            RefreshReplayIcons();
            RefreshPlayButton();
            SetVisualizerActive(showAudioVisualizer && HasAudioClip);
            SetVisualizerIdle(true);
        }

        public void TryAutoPlayCurrentRound()
        {
            if (!roundLoaded || !autoPlayOnRoundStart || !HasAudioClip || HasAudioPlayed)
                return;

            StopAutoPlayRoutine();
            autoPlayRoutine = StartCoroutine(AutoPlayAfterDelay());
        }

        public void OnPlayButtonPressed()
        {
            PlayCurrentClip();
        }

        public void PlaySfx_KeyTap() => PlayOneShot(keyTapSfx);
        public void PlaySfx_Correct() => PlayOneShot(correctSfx);
        public void PlaySfx_Wrong() => PlayOneShot(wrongSfx);
        public void PlaySfx_HintUsed() => PlayOneShot(hintUsedSfx);

        public void StopAudio()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

            isPausedByGame = false;
            RefreshPlayButton();
            SetVisualizerIdle(true);
        }

        public void PauseAudio()
        {
            if (audioSource == null || !audioSource.isPlaying) return;
            audioSource.Pause();
            isPausedByGame = true;
            RefreshPlayButton();
            SetVisualizerIdle(true);
        }

        public void ResumeAudio()
        {
            if (audioSource == null || !isPausedByGame) return;
            audioSource.UnPause();
            isPausedByGame = false;
            RefreshPlayButton();
            SetVisualizerIdle(false);
        }

        private IEnumerator AutoPlayAfterDelay()
        {
            if (autoPlayDelay > 0f)
                yield return new WaitForSeconds(autoPlayDelay);

            autoPlayRoutine = null;
            PlayCurrentClip();
        }

        private void PlayCurrentClip()
        {
            if (!HasAudioClip)
            {
                RefreshPlayButton();
                return;
            }

            int maxTotalPlays = maxReplays + 1;
            if (playsUsed >= maxTotalPlays)
            {
                RefreshPlayButton();
                return;
            }

            if (audioSource.isPlaying)
                audioSource.Stop();

            if (playsUsed > 0)
                OnReplayUsed?.Invoke(replayCostPoints);

            playsUsed++;
            audioSource.Play();
            OnPlaybackStarted?.Invoke();

            RefreshReplayIcons();
            RefreshPlayButton();
            SetVisualizerIdle(false);
            StartPlaybackWatcher();
        }

        private void StartPlaybackWatcher()
        {
            if (playbackWatcherRoutine != null)
                StopCoroutine(playbackWatcherRoutine);
            playbackWatcherRoutine = StartCoroutine(WatchPlaybackEnd());
        }

        private IEnumerator WatchPlaybackEnd()
        {
            yield return null;

            while (audioSource != null && audioSource.isPlaying)
                yield return null;

            playbackWatcherRoutine = null;
            if (!isPausedByGame)
            {
                RefreshPlayButton();
                SetVisualizerIdle(true);
                OnPlaybackFinished?.Invoke();
            }
        }

        private void BindButton()
        {
            if (playButton == null) return;
            playButton.onClick.RemoveListener(OnPlayButtonPressed);
            playButton.onClick.AddListener(OnPlayButtonPressed);
        }

        private void RefreshPlayButton()
        {
            if (playButton == null) return;

            bool canPlay = HasAudioClip && playsUsed < maxReplays + 1 && !isPausedByGame;
            if (disablePlayButtonWhilePlaying && IsPlaying)
                canPlay = false;

            playButton.interactable = canPlay;

            RefreshPlayButtonIcon();
            RefreshPlayButtonFallbackLabel();
        }

        private void RefreshPlayButtonIcon()
        {
            if (playButtonIcon == null) return;

            bool playing = IsPlaying && !isPausedByGame;
            Sprite targetSprite = playing ? playingIconSprite : playIconSprite;

            if (targetSprite != null)
                playButtonIcon.sprite = targetSprite;

            playButtonIcon.enabled = targetSprite != null;
            playButtonIcon.color = playing ? playIconPlayingColor : playIconReadyColor;
        }

        private void RefreshPlayButtonFallbackLabel()
        {
            if (playButtonLabel == null) return;

            bool playing = IsPlaying && !isPausedByGame;
            bool hasSprite = playing ? playingIconSprite != null : playIconSprite != null;
            playButtonLabel.gameObject.SetActive(!hasSprite);

            if (hasSprite)
            {
                playButtonLabel.text = string.Empty;
                return;
            }

            if (!HasAudioClip)
                playButtonLabel.text = "×";
            else if (playing)
                playButtonLabel.text = "Ⅱ";
            else
                playButtonLabel.text = "▶";
        }

        private void RefreshReplayIcons()
        {
            if (replayIcons == null) return;

            int remaining = ReplaysRemaining;
            for (int i = 0; i < replayIcons.Length; i++)
            {
                Image icon = replayIcons[i];
                if (icon == null) continue;

                bool available = i < remaining;
                Sprite targetSprite = available ? replayAvailableSprite : replayUsedSprite;
                if (targetSprite != null)
                    icon.sprite = targetSprite;
                else if (available && replayAvailableSprite != null)
                    icon.sprite = replayAvailableSprite;

                icon.color = available ? iconAvailableColor : iconUsedColor;
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || audioSource == null) return;
            audioSource.PlayOneShot(clip, sfxVolume);
        }

        private void StopManagedCoroutines()
        {
            StopAutoPlayRoutine();
            if (playbackWatcherRoutine != null)
            {
                StopCoroutine(playbackWatcherRoutine);
                playbackWatcherRoutine = null;
            }
        }

        private void StopAutoPlayRoutine()
        {
            if (autoPlayRoutine != null)
            {
                StopCoroutine(autoPlayRoutine);
                autoPlayRoutine = null;
            }
        }

        private void SetVisualizerActive(bool active)
        {
            if (visualizerRoot != null)
                visualizerRoot.SetActive(active);
        }

        private void SetVisualizerIdle(bool idle)
        {
            EnsureWaveformCache();

            if (listeningLabel != null)
                listeningLabel.text = idle ? idleLabelText : playingLabelText;

            if (waveformBars == null) return;

            for (int i = 0; i < waveformBars.Length; i++)
            {
                RectTransform bar = waveformBars[i];
                if (bar == null) continue;

                float normalized = GetIdleNormalizedHeight(i);
                if (waveformCurrent01 != null && i < waveformCurrent01.Length)
                    waveformCurrent01[i] = normalized;
                if (waveformTarget01 != null && i < waveformTarget01.Length)
                    waveformTarget01[i] = normalized;

                SetBarHeight(bar, Mathf.Lerp(idleBarHeightRange.x, idleBarHeightRange.y, normalized));
            }

            speechEnvelope01 = idle ? 0.18f : 0.45f;
            speechTargetEnvelope01 = speechEnvelope01;
            nextEnvelopeRetargetTime = 0f;
        }

        private void UpdateWaveform()
        {
            if (!showAudioVisualizer || waveformBars == null || waveformBars.Length == 0) return;
            if (!IsPlaying || isPausedByGame) return;

            if (useSpeechLikeRandomMotion)
                UpdateSpeechLikeWaveform();
            else
                UpdateLegacyRhythmicWaveform();
        }

        private void UpdateSpeechLikeWaveform()
        {
            EnsureWaveformCache();
            if (waveformCurrent01 == null || waveformTarget01 == null || waveformNextRetargetTime == null) return;

            float now = Time.unscaledTime;
            float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            Vector2 interval = GetValidRetargetRange();

            if (now >= nextEnvelopeRetargetTime)
            {
                speechTargetEnvelope01 = PickSpeechEnvelope();
                nextEnvelopeRetargetTime = now + UnityEngine.Random.Range(interval.x * 1.1f, interval.y * 2.15f);
            }

            float envelopeLerp = 1f - Mathf.Exp(-deltaTime * speechSmoothSpeed * 0.65f);
            speechEnvelope01 = Mathf.Lerp(speechEnvelope01, speechTargetEnvelope01, envelopeLerp);

            float barLerp = 1f - Mathf.Exp(-deltaTime * speechSmoothSpeed);
            int count = waveformBars.Length;
            float center = Mathf.Max(1f, (count - 1) * 0.5f);

            for (int i = 0; i < count; i++)
            {
                RectTransform bar = waveformBars[i];
                if (bar == null) continue;

                if (now >= waveformNextRetargetTime[i])
                {
                    waveformTarget01[i] = PickBarTarget(i, count, center);
                    waveformNextRetargetTime[i] = now + UnityEngine.Random.Range(interval.x, interval.y) * UnityEngine.Random.Range(0.75f, 1.45f);
                }

                waveformCurrent01[i] = Mathf.Lerp(waveformCurrent01[i], waveformTarget01[i], barLerp);
                float shaped = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(waveformCurrent01[i]));
                float height = Mathf.Lerp(activeBarHeightRange.x, activeBarHeightRange.y, shaped);
                SetBarHeight(bar, height);
            }
        }

        private void UpdateLegacyRhythmicWaveform()
        {
            float time = Time.unscaledTime * waveformSpeed;
            for (int i = 0; i < waveformBars.Length; i++)
            {
                RectTransform bar = waveformBars[i];
                if (bar == null) continue;

                float wave = Mathf.Abs(Mathf.Sin(time + i * 0.55f));
                float noise = Mathf.PerlinNoise(i * 0.31f, time * 0.12f);
                float mixed = Mathf.Lerp(wave, noise, waveformNoise);
                float height = Mathf.Lerp(activeBarHeightRange.x, activeBarHeightRange.y, mixed);
                SetBarHeight(bar, height);
            }
        }

        private float PickSpeechEnvelope()
        {
            float roll = UnityEngine.Random.value;

            if (roll < speechQuietChance)
                return UnityEngine.Random.Range(0.04f, 0.23f);

            if (roll < speechQuietChance + speechAccentChance)
                return UnityEngine.Random.Range(0.72f, 1f);

            return UnityEngine.Random.Range(0.28f, 0.82f);
        }

        private float PickBarTarget(int index, int count, float center)
        {
            float distanceFromCenter = Mathf.Abs(index - center) / center;
            float centerStrength = 1f - distanceFromCenter;
            float centerMultiplier = Mathf.Lerp(0.9f, 1f + speechCenterBias, centerStrength);

            float naturalBarVariation = UnityEngine.Random.Range(0.42f, 1.05f);
            float jitter = UnityEngine.Random.Range(-speechJitter, speechJitter);
            float target = (speechEnvelope01 * naturalBarVariation + jitter) * centerMultiplier;

            // Small gaps and occasional peaks stop the pattern from feeling like a looping sine wave.
            if (UnityEngine.Random.value < 0.08f)
                target *= UnityEngine.Random.Range(0.05f, 0.28f);

            if (UnityEngine.Random.value < 0.12f)
                target += UnityEngine.Random.Range(0.12f, 0.35f);

            return Mathf.Clamp01(target);
        }

        private Vector2 GetValidRetargetRange()
        {
            float min = Mathf.Max(0.025f, Mathf.Min(speechRetargetIntervalRange.x, speechRetargetIntervalRange.y));
            float max = Mathf.Max(min + 0.01f, Mathf.Max(speechRetargetIntervalRange.x, speechRetargetIntervalRange.y));
            return new Vector2(min, max);
        }

        private void EnsureWaveformCache()
        {
            int count = waveformBars != null ? waveformBars.Length : 0;
            if (count <= 0)
            {
                waveformCurrent01 = null;
                waveformTarget01 = null;
                waveformNextRetargetTime = null;
                return;
            }

            if (waveformCurrent01 != null && waveformCurrent01.Length == count &&
                waveformTarget01 != null && waveformTarget01.Length == count &&
                waveformNextRetargetTime != null && waveformNextRetargetTime.Length == count)
            {
                return;
            }

            waveformCurrent01 = new float[count];
            waveformTarget01 = new float[count];
            waveformNextRetargetTime = new float[count];

            float now = Time.unscaledTime;
            Vector2 interval = GetValidRetargetRange();
            for (int i = 0; i < count; i++)
            {
                float idle = GetIdleNormalizedHeight(i);
                waveformCurrent01[i] = idle;
                waveformTarget01[i] = idle;
                waveformNextRetargetTime[i] = now + UnityEngine.Random.Range(interval.x, interval.y);
            }
        }

        private static float GetIdleNormalizedHeight(int index)
        {
            return index % 2 == 0 ? 0.25f : 0.75f;
        }

        private static void SetBarHeight(RectTransform bar, float height)
        {
            Vector2 size = bar.sizeDelta;
            size.y = height;
            bar.sizeDelta = size;
        }
    }
}
