using System;
using UnityEngine;

/// <summary>
/// Standalone persistent timer. Attach to any GameObject once — it survives scene loads.
/// Access it from anywhere via GameTimer.Instance
///
/// QUICK USAGE:
///   GameTimer.Instance.StartTimer();
///   GameTimer.Instance.PauseTimer();
///   GameTimer.Instance.ResumeTimer();
///   float seconds = GameTimer.Instance.StopTimer();   // returns elapsed seconds
///   float seconds = GameTimer.Instance.ElapsedSeconds; // peek without stopping
/// </summary>
public class GameTimer : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static GameTimer Instance { get; private set; }

    // ─── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired when StopTimer() is called. Arg = total elapsed seconds.</summary>
    public event Action<float> OnTimerStopped;

    /// <summary>Fired every second while the timer is running. Arg = elapsed seconds so far.</summary>
    public event Action<float> OnTimerTick;

    // ─── State ────────────────────────────────────────────────────────────────
    public enum TimerState { Idle, Running, Paused }
    public TimerState State { get; private set; } = TimerState.Idle;

    /// <summary>Total elapsed seconds (excludes paused time). Safe to read at any time.</summary>
    public float ElapsedSeconds { get; private set; }

    /// <summary>Elapsed time as a formatted string  "mm:ss" or "hh:mm:ss".</summary>
    public string ElapsedFormatted => FormatTime(ElapsedSeconds);

    // internal
    private float _tickAccumulator;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────
    private void Awake()
    {
        // Enforce singleton — destroy duplicate if a second one ever appears
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (State != TimerState.Running) return;

        ElapsedSeconds += Time.deltaTime;

        // Fire OnTimerTick once per second
        _tickAccumulator += Time.deltaTime;
        if (_tickAccumulator >= 1f)
        {
            _tickAccumulator -= 1f;
            OnTimerTick?.Invoke(ElapsedSeconds);
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Start (or restart) the timer from zero.
    /// Calling this while already running resets and restarts.
    /// </summary>
    public void StartTimer()
    {
        ElapsedSeconds = 0f;
        _tickAccumulator = 0f;
        State = TimerState.Running;
        Debug.Log("[GameTimer] Started.");
    }

    /// <summary>
    /// Pause the timer. Elapsed time is preserved.
    /// Has no effect if already paused or idle.
    /// </summary>
    public void PauseTimer()
    {
        if (State != TimerState.Running)
        {
            Debug.LogWarning("[GameTimer] PauseTimer called but timer is not running.");
            return;
        }
        State = TimerState.Paused;
        Debug.Log($"[GameTimer] Paused at {ElapsedFormatted}.");
    }

    /// <summary>
    /// Resume a paused timer. Has no effect if already running or idle.
    /// </summary>
    public void ResumeTimer()
    {
        if (State != TimerState.Paused)
        {
            Debug.LogWarning("[GameTimer] ResumeTimer called but timer is not paused.");
            return;
        }
        State = TimerState.Running;
        Debug.Log($"[GameTimer] Resumed from {ElapsedFormatted}.");
    }

    /// <summary>
    /// Stop the timer, fire OnTimerStopped, and return elapsed seconds.
    /// Timer goes back to Idle; ElapsedSeconds holds the final value until next StartTimer().
    /// </summary>
    /// <returns>Total elapsed seconds.</returns>
    public float StopTimer()
    {
        if (State == TimerState.Idle)
        {
            Debug.LogWarning("[GameTimer] StopTimer called but timer was never started.");
            return 0f;
        }

        State = TimerState.Idle;
        float result = ElapsedSeconds;
        Debug.Log($"[GameTimer] Stopped. Total time: {ElapsedFormatted} ({result:F2}s)");
        OnTimerStopped?.Invoke(result);
        return result;
    }

    /// <summary>
    /// Reset without firing events. Puts timer back to Idle with 0 elapsed time.
    /// </summary>
    public void ResetTimer()
    {
        State = TimerState.Idle;
        ElapsedSeconds = 0f;
        _tickAccumulator = 0f;
        Debug.Log("[GameTimer] Reset.");
    }


    // ─── Scoring ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Score the user's time against an average using a sigmoid curve.
    ///
    /// Returns a float in [0, 1]:
    ///   • timeTaken == avgTime   → 0.5  (exactly average)
    ///   • timeTaken &lt;&lt; avgTime  → approaches 1.0  (much faster = great)
    ///   • timeTaken >> avgTime  → approaches 0.0  (much slower = poor)
    ///
    /// The curve steepness is controlled by <paramref name="sensitivity"/>:
    ///   • Lower  (~2–3) = forgiving, scores stay near 0.5 for most players
    ///   • Default (5)   = balanced
    ///   • Higher (~8+)  = punishing, only well-above-avg times score high
    /// </summary>
    /// <param name="timeTaken">How long the user actually took (seconds).</param>
    /// <param name="avgTime">The reference / expected average time (seconds).</param>
    /// <param name="sensitivity">Steepness of the curve. Default = 5.</param>
    /// <returns>Score between 0 (worst) and 1 (best).</returns>
    public static float CalculateTimeScore(float timeTaken, float avgTime, float sensitivity = 5f)
    {
        if (avgTime <= 0f)
        {
            Debug.LogError("[GameTimer] CalculateTimeScore: avgTime must be > 0.");
            return 0f;
        }

        // Normalised delta: how far from avg, relative to avg
        // positive = user was FASTER (good), negative = slower (bad)
        float delta = (avgTime - timeTaken) / avgTime;

        // Sigmoid:  1 / (1 + e^(-sensitivity * delta))
        // delta=0  → 0.5  (avg)
        // delta>0  → >0.5 (faster than avg)
        // delta<0  → <0.5 (slower than avg)
        float score = 1f / (1f + Mathf.Exp(-sensitivity * delta));

        return Mathf.Clamp01(score);
    }


    // ─── Helpers ──────────────────────────────────────────────────────────────
    private static string FormatTime(float totalSeconds)
    {
        int h = (int)(totalSeconds / 3600);
        int m = (int)(totalSeconds % 3600 / 60);
        int s = (int)(totalSeconds % 60);
        return h > 0
            ? $"{h:D2}:{m:D2}:{s:D2}"
            : $"{m:D2}:{s:D2}";
    }
}
