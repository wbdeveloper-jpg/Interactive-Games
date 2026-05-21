/*
 * ============================================================
 * ExampleGameManager.cs  —  Template for any Game Scene
 * ============================================================
 * SUMMARY:
 *   This is a TEMPLATE / EXAMPLE showing how any game scene
 *   integrates with the RewardSystem module.
 *
 *   Copy this into your game scene, rename it, and fill in
 *   your own game logic where marked with TODO comments.
 *
 * CHECKLIST FOR EACH GAME SCENE:
 *   ✅ Implement IGameSceneCallbacks (OnPlayAgain + OnHome)
 *   ✅ Define your skill list with correct maxScore values
 *   ✅ Call RewardManager.Instance.ShowPreGame(skills) at start
 *   ✅ Build GameEvaluationData at game over
 *   ✅ Call RewardManager.Instance.ShowPostGame(skills, eval)
 *   ✅ That's it — reward system handles everything else
 *
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;

public class ExampleGameManager : MonoBehaviour, IGameSceneCallbacks
{
    // ── Skill Definition ──────────────────────────────────────
    // Define ONCE per game — which Bloom skills this game trains
    // and what their relative max score weights are.
    private List<SkillEntry> _gameSkills = new List<SkillEntry>
    {
        // This example game trains Remember (primary, max 100)
        // and Understand (secondary, max 50)
        new SkillEntry(BloomSkillType.Remember,   maxScore: 100f),
        new SkillEntry(BloomSkillType.Understand, maxScore: 50f),
    };

    // ── Your Game State ───────────────────────────────────────
    // TODO: replace with your actual game variables
    private int   _mistakeCount  = 0;
    private float _startTime     = 0f;
    private float _correctRatio  = 0f; // e.g. 0.75 = 75% correct

    // ── Unity Lifecycle ───────────────────────────────────────

    private void Start()
    {
        _startTime = Time.time;

        // Show pre-game Bloom skills panel
        // RewardManager persists from loading scene — always available
        RewardManager.Instance.ShowPreGame(_gameSkills);

        // TODO: Initialize your own game here
    }

    // ── IGameSceneCallbacks — REQUIRED ────────────────────────

    /// <summary>Called by RewardSystem when player taps "Play Again".</summary>
    public void OnPlayAgain()
    {
        // Reload this scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Called by RewardSystem when player taps "Home".</summary>
    public void OnHome()
    {
        // Go back to your loading/hub scene
        SceneManager.LoadScene("LoadingScene"); // TODO: replace with your scene name
    }

    // ── Game Over ─────────────────────────────────────────────

    /// <summary>
    /// Call this from your game logic when the game ends.
    /// Builds evaluation data and hands off to RewardManager.
    /// </summary>
    public void OnGameOver()
    {
        float timeTaken = Time.time - _startTime;

        // TODO: Replace these with your actual calculated values

        // timeScore: normalize based on your game's expected time range
        // Example: game expects 30-120 seconds. Fast = 1.0, slow = 0.0
        float expectedMaxTime = 120f;
        float timeScore = Mathf.Clamp01(1f - (timeTaken / expectedMaxTime));

        // accuracyScore: your game tracks this directly (60% correct = 0.6f)
        float accuracyScore = _correctRatio;

        GameEvaluationData eval = new GameEvaluationData
        {
            timeScore     = timeScore,
            accuracyScore = accuracyScore,
            mistakeCount  = _mistakeCount,
            timeTaken     = timeTaken
        };

        RewardManager.Instance.ShowPostGame(_gameSkills, eval);
    }

    // ── Example: Track mistakes during gameplay ───────────────

    /// <summary>Call this whenever player makes a mistake in your game.</summary>
    public void RegisterMistake()
    {
        _mistakeCount++;
        // TODO: your game's mistake handling
    }

    /// <summary>Call this when player answers correctly.</summary>
    public void RegisterCorrect(int totalQuestions, int correctSoFar)
    {
        _correctRatio = totalQuestions > 0 ? (float)correctSoFar / totalQuestions : 0f;
    }
}
