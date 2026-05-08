/*
 * ============================================================
 * RewardManager.cs  —  MonoBehaviour | Singleton | DontDestroyOnLoad
 * ============================================================
 * SUMMARY:
 *   The central controller of the entire Reward System module.
 *   Lives persistently across all game scenes from the moment
 *   it is instantiated in the Loading Scene.
 *
 * RESPONSIBILITIES:
 *   • Singleton lifecycle with DontDestroyOnLoad
 *   • Camera auto-reassignment on every scene load
 *   • Exposes ShowPreGame() and ShowPostGame() for game scenes
 *   • Delegates panel control to PreGamePanel, PostGamePanel, InfoPanel
 *   • Runs score calculation via ScoreCalculator utility
 *   • Manages Info Panel open/close and pre-game countdown pause
 *
 * HOW TO USE FROM A GAME SCENE:
 *
 *   Step 1 — Define skills this game trains:
 *       var skills = new List<SkillEntry>
 *       {
 *           new SkillEntry(BloomSkillType.Remember,   100f),
 *           new SkillEntry(BloomSkillType.Understand,  50f),
 *       };
 *
 *   Step 2 — Show pre-game panel at scene start:
 *       RewardManager.Instance.ShowPreGame(skills);
 *
 *   Step 3 — On game over, build evaluation data and show results:
 *       var eval = new GameEvaluationData
 *       {
 *           timeScore     = 0.85f,
 *           accuracyScore = 0.70f,
 *           mistakeCount  = 2,
 *           timeTaken     = 38.5f
 *       };
 *       RewardManager.Instance.ShowPostGame(skills, eval);
 *
 * SETUP IN EDITOR:
 *   • Place RewardManager prefab in LoadingScene.
 *   • Assign all 6 BloomSkillData ScriptableObjects in inspector list.
 *   • Assign panel references (PreGamePanel, PostGamePanel, InfoPanel).
 *   • Tune score weights and medal thresholds in inspector.
 *   • Canvas sortingOrder set high (e.g. 999) to overlay all game UI.
 *
 * ============================================================
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RewardSystem
{
    public class RewardManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────

        public static RewardManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────

        [Header("Bloom Skill Data (assign all 6)")]
        [SerializeField] private List<BloomSkillData> allSkillData = new();

        [Header("Panel References")]
        [SerializeField] private PreGamePanel preGamePanel;
        [SerializeField] private PostGamePanel postGamePanel;
        [SerializeField] private InfoPanel infoPanel;

        [Header("Canvases")]
        [Tooltip("UI Canvas — interactive panels, buttons, cards. SortOrder 999.")]
        [SerializeField] private Canvas uiCanvas;
        [Tooltip("BG Canvas — decorative backgrounds. SortOrder 997. Particles sit between 997-999.")]
        [SerializeField] private Canvas bgCanvas;

        [Header("Score Weights (must sum to 1 or will be normalized)")]
        [Range(0f, 1f)]
        [SerializeField] private float timeWeight = 0.4f;
        [Range(0f, 1f)]
        [SerializeField] private float accuracyWeight = 0.6f;

        [Header("Medal Thresholds (normalized 0-1)")]
        [SerializeField] private float silverThreshold = 0.4f;
        [SerializeField] private float goldThreshold = 0.7f;

        [Header("Reward Audio")]
        [Tooltip("Dedicated AudioSource on this prefab — independent from any game scene audio.")]
        [SerializeField] private AudioSource rewardAudioSource;
        [Tooltip("Played when best skill result is Gold.")]
        [SerializeField] private AudioClip goldClip;
        [Tooltip("Played when best skill result is Silver (and no Gold).")]
        [SerializeField] private AudioClip silverClip;
        [Tooltip("Played when all results are Bronze.")]
        [SerializeField] private AudioClip bronzeClip;

        // ── Private state ─────────────────────────────────────────

        private List<SkillEntry> _currentSkills;

        /// <summary>
        /// True once the pre-game panel has fully faded out.
        /// Game scenes poll this via WaitUntil before starting gameplay.
        /// Automatically reset to false each time ShowPreGame() is called.
        /// </summary>
        public bool IsPreGameComplete { get; private set; } = true;

        // ── Unity Lifecycle ───────────────────────────────────────

        private void Awake()
        {
            // Singleton guard — destroy duplicate if reloaded
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Hide all panels on start
            preGamePanel.gameObject.SetActive(false);
            postGamePanel.gameObject.SetActive(false);
            infoPanel.gameObject.SetActive(false);

            // Setup info panel with skill data and countdown callbacks
            infoPanel.SetupSkills(
                allSkillData,
                onOpenFromPreGame: () => preGamePanel.PauseCountdown(),
                onCloseToPreGame: () => preGamePanel.ResumeCountdown()
            );

            // Wire pre-game eye button to open info panel
            preGamePanel.gameObject.SetActive(false); // ensure hidden
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Reassign canvas camera whenever a new scene loads.
        /// Screen Space - Camera mode requires a valid camera reference.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Camera.main == null) return;

            // Both canvases need the new scene's camera assigned
            // Screen Space - Camera mode loses its reference on scene transition
            if (uiCanvas != null) uiCanvas.worldCamera = Camera.main;
            if (bgCanvas != null) bgCanvas.worldCamera = Camera.main;
        }

        // ── Public API ─────────────────────────────────────────────

        /// <summary>
        /// Show the Pre-Game panel for the given skills.
        /// Call this at the start of each game scene.
        /// </summary>
        /// <param name="skills">Skills this game trains with their max score weights.</param>
        public void ShowPreGame(List<SkillEntry> skills)
        {
            if (skills == null || skills.Count == 0)
            {
                Debug.LogWarning("[RewardManager] ShowPreGame called with empty skill list.");
                return;
            }

            _currentSkills = skills;
            IsPreGameComplete = false; // reset — game must wait

            // Close others
            postGamePanel.gameObject.SetActive(false);
            infoPanel.gameObject.SetActive(false);

            // Wire info button to open InfoPanel in pre-game context (pauses countdown)
            preGamePanel.OnInfoClicked = () => infoPanel.Open(fromPreGame: true);
            // When panel fully fades out: flip game-ready flag AND disable BG canvas
            preGamePanel.OnPanelComplete = () =>
            {
                IsPreGameComplete = true;
                SetBGCanvas(false);
            };

            SetBGCanvas(true);
            preGamePanel.Show(skills, allSkillData);
        }

        /// <summary>
        /// Show the Post-Game panel after game over.
        /// Call this when the game session ends.
        /// </summary>
        /// <param name="skills">Same skill list passed to ShowPreGame.</param>
        /// <param name="evalData">Normalized performance metrics from the game.</param>
        public void ShowPostGame(List<SkillEntry> skills, GameEvaluationData evalData)
        {
            if (skills == null || skills.Count == 0 || evalData == null)
            {
                Debug.LogWarning("[RewardManager] ShowPostGame called with invalid data.");
                return;
            }

            // Close pre-game if still visible
            preGamePanel.gameObject.SetActive(false);
            infoPanel.gameObject.SetActive(false);

            // Calculate results for each skill
            List<SkillResult> results = new();
            foreach (var skill in skills)
            {
                SkillResult result = ScoreCalculator.Calculate(
                    skill,
                    evalData,
                    timeWeight,
                    accuracyWeight,
                    silverThreshold,
                    goldThreshold
                );
                results.Add(result);
            }

            // Notify game scene to stop its own audio (optional interface — safe if not implemented)
            NotifyGameAudioStop();

            // Play reward audio based on best medal achieved across all skills
            PlayRewardAudio(results);

            // Show post-game panel
            // Wire info button once via the stored action (not passed into Show to avoid stacking)
            postGamePanel.OnInfoClicked = () => infoPanel.Open(fromPreGame: false);
            // On hidden: stop reward audio immediately + disable BG canvas
            postGamePanel.OnHidden = () =>
            {
                StopRewardAudio();
                SetBGCanvas(false);
            };
            SetBGCanvas(true);
            postGamePanel.Show(results, allSkillData);
        }

        /// <summary>
        /// Force hide all reward panels immediately and cleanly.
        /// Stops all running coroutines and resets alpha — safe to call before scene transitions.
        /// </summary>
        public void HideAll()
        {
            preGamePanel.StopAllCoroutines();
            postGamePanel.StopAllCoroutines();

            // Reset CanvasGroup alphas explicitly before deactivating
            // Prevents ghost-panel rendering on Android during scene transitions
            if (preGamePanel.TryGetComponent<CanvasGroup>(out var preCG)) preCG.alpha = 0f;
            if (postGamePanel.TryGetComponent<CanvasGroup>(out var postCG)) postCG.alpha = 0f;

            preGamePanel.gameObject.SetActive(false);
            postGamePanel.gameObject.SetActive(false);
            infoPanel.gameObject.SetActive(false);

            // Stop reward audio and disable BG canvas
            StopRewardAudio();
            SetBGCanvas(false);
        }

        /// <summary>
        /// Plays the appropriate reward audio clip based on the best medal
        /// achieved across all skill results. Gold > Silver > Bronze priority.
        /// </summary>
        private void PlayRewardAudio(List<SkillResult> results)
        {
            if (rewardAudioSource == null) return;

            // Find the best medal across all skills
            MedalTier best = MedalTier.Bronze;
            foreach (var r in results)
            {
                if (r.medal == MedalTier.Gold) { best = MedalTier.Gold; break; }
                if (r.medal == MedalTier.Silver) best = MedalTier.Silver;
            }

            AudioClip clip = best switch
            {
                MedalTier.Gold => goldClip,
                MedalTier.Silver => silverClip,
                _ => bronzeClip,
            };

            if (clip == null)
            {
                Debug.LogWarning($"[RewardSystem] No audio clip assigned for {best} medal.");
                return;
            }

            rewardAudioSource.Stop();
            rewardAudioSource.clip = clip;
            rewardAudioSource.Play();
        }

        /// <summary>
        /// Stops reward audio immediately — called when Play Again or Home is pressed.
        /// Does not wait for clip to finish.
        /// </summary>
        private void StopRewardAudio()
        {
            if (rewardAudioSource != null && rewardAudioSource.isPlaying)
                rewardAudioSource.Stop();
        }

        /// <summary>
        /// Finds IGameAudioCallbacks in the current scene and calls OnRewardScreenOpen.
        /// Safe to call even if no scene implements this interface.
        /// </summary>
        private void NotifyGameAudioStop()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IGameAudioCallbacks audioCallbacks)
                {
                    audioCallbacks.OnRewardScreenOpen();
                    return; // one implementer per scene is enough
                }
            }
            // No implementation found — silently skip, not an error
        }

        /// <summary>
        /// Enables or disables the background canvas.
        /// Called internally — never needed from game scenes.
        /// </summary>
        private void SetBGCanvas(bool active)
        {
            if (bgCanvas != null)
                bgCanvas.gameObject.SetActive(active);
        }

        /// <summary>
        /// Manually open the info panel from a game scene if needed.
        /// </summary>
        public void OpenInfoPanel()
        {
            infoPanel.Open(fromPreGame: false);
        }
    }
}