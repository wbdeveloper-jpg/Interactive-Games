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

        [Header("Canvas")]
        [SerializeField] private Canvas mainCanvas;

        [Header("Score Weights (must sum to 1 or will be normalized)")]
        [Range(0f, 1f)]
        [SerializeField] private float timeWeight = 0.4f;
        [Range(0f, 1f)]
        [SerializeField] private float accuracyWeight = 0.6f;

        [Header("Medal Thresholds (normalized 0-1)")]
        [SerializeField] private float silverThreshold = 0.4f;
        [SerializeField] private float goldThreshold = 0.7f;

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
            if (mainCanvas != null && Camera.main != null)
            {
                mainCanvas.worldCamera = Camera.main;
            }
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

            // When panel fully fades out, flip the flag so waiting game coroutines unblock
            preGamePanel.OnPanelComplete = () => IsPreGameComplete = true;

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

            // Show post-game panel
            postGamePanel.Show(
                results,
                allSkillData,
                onInfoClicked: () => infoPanel.Open(fromPreGame: false)
            );
        }

        /// <summary>
        /// Force hide all reward panels. Useful for edge cases like
        /// scene transitions triggered outside the reward system.
        /// </summary>
        public void HideAll()
        {
            preGamePanel.gameObject.SetActive(false);
            postGamePanel.gameObject.SetActive(false);
            infoPanel.gameObject.SetActive(false);
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