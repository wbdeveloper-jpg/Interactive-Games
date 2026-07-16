using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RewardSystem;

public class TreasureQuestGameManager : MonoBehaviour, IGameSceneCallbacks, IGameAudioCallbacks
{
    [Header("Managers")]
    public TreasureQuestUIManager uiManager;
    public TreasureQuestLevelManager levelManager;
    public TreasureQuestQuizManager quizManager;
    public TreasureQuestQuestionDatabase questionDatabase;
    public TreasureQuestAudioManager audioManager;

    [Header("Bloom Reward System")]
    [Tooltip("RewardManager must already exist in LoadingScene. Do not add RewardManager prefab inside this game scene.")]
    public bool useBloomRewardSystem = true;
    public bool showBloomPreGameBeforeLoading = true;
    public bool showBloomPostGameFromResultButton = true;
    [Tooltip("Bloom Home callback scene name. Keep this same as your loader/home scene.")]
    public string bloomHomeSceneName = "Loader Scene";
    [Tooltip("Used for normalized Bloom time score. Expected max time = questions per session x this value.")]
    [Min(1f)] public float expectedSecondsPerQuestion = 20f;
    public List<TreasureQuestBloomSkillSetting> bloomSkillSettings = new List<TreasureQuestBloomSkillSetting>
    {
        new TreasureQuestBloomSkillSetting(BloomSkillType.Remember, 100f),
        new TreasureQuestBloomSkillSetting(BloomSkillType.Understand, 75f)
    };

    [Header("Startup")]
    public float loadingPanelSeconds = 0.75f;
    [Tooltip("Recommended for kids: shows How To Play after loading every time the scene starts.")]
    public bool showHowToPlayEveryLaunch = true;
    [Tooltip("Legacy option. Used only when Show How To Play Every Launch is off.")]
    public bool showHowToPlayOnFirstLaunch = false;

    private readonly List<SkillEntry> runtimeBloomSkills = new List<SkillEntry>();
    private int currentGate = 1;
    private bool lastResultPassed = true;
    private bool initialized;

    private float gateSessionStartTime;
    private float lastTimeTaken;
    private int lastCorrectCount;
    private int lastTotalQuestions;
    private int lastMistakeCount;
    private bool bloomPostShownForCurrentResult;

    private void Awake()
    {
        AutoFindMissingReferences();
        Initialize();
    }

    private IEnumerator Start()
    {
        if (uiManager != null)
            uiManager.HideAllPanels();

        if (useBloomRewardSystem && showBloomPreGameBeforeLoading)
            yield return StartCoroutine(RunBloomPreGameFlow());

        if (uiManager != null)
            uiManager.ShowLoadingPanel();

        yield return new WaitForSecondsRealtime(loadingPanelSeconds);

        if (uiManager != null)
            uiManager.ShowMenuPanel();

        levelManager?.RefreshMenu();

        bool shouldShowHowToPlay = showHowToPlayEveryLaunch ||
                                   (showHowToPlayOnFirstLaunch && !TreasureQuestSaveManager.HasSeenHowToPlay());

        if (shouldShowHowToPlay)
        {
            uiManager?.ShowHowToPlayOverlay(true);

            if (!showHowToPlayEveryLaunch)
                TreasureQuestSaveManager.SaveHowToPlaySeen();
        }
    }

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;

        BuildBloomSkillList();

        uiManager?.Bind(this);
        levelManager?.Initialize(this, uiManager, audioManager);
        quizManager?.Initialize(this, levelManager, uiManager, audioManager, questionDatabase);
    }

    public void PlayHighestUnlockedGate()
    {
        if (levelManager == null) return;
        StartGate(levelManager.GetPreferredPlayGate());
    }

    public void StartGate(int gateNumber)
    {
        currentGate = Mathf.Clamp(gateNumber, 1, 5);
        gateSessionStartTime = Time.unscaledTime;
        bloomPostShownForCurrentResult = false;

        levelManager?.SetLastSelectedGate(currentGate);
        audioManager?.PlayClick();
        uiManager?.ShowGameplayPanel();
        quizManager?.StartGate(currentGate);
    }

    public void OnGateSessionFinished(bool passed, int gateNumber, int correctCount, int totalQuestions, int coinsEarned, bool finalTreasureUnlocked)
    {
        lastResultPassed = passed;
        lastCorrectCount = correctCount;
        lastTotalQuestions = Mathf.Max(1, totalQuestions);
        lastMistakeCount = Mathf.Max(0, lastTotalQuestions - lastCorrectCount);
        lastTimeTaken = Mathf.Max(0f, Time.unscaledTime - gateSessionStartTime);
        bloomPostShownForCurrentResult = false;

        uiManager?.ShowResultPanel(passed, gateNumber, correctCount, totalQuestions, coinsEarned, finalTreasureUnlocked);
    }

    public void ContinueFromResult()
    {
        audioManager?.PlayClick();

        uiManager?.ShowMenuPanel();
        levelManager?.RefreshMenu();

        if (useBloomRewardSystem && showBloomPostGameFromResultButton && !bloomPostShownForCurrentResult)
            ShowBloomPostGameForLastResult();
    }

    public void PauseGame()
    {
        audioManager?.PlayClick();
        Time.timeScale = 0f;
        uiManager?.ShowPauseOverlay(true);
    }

    public void ResumeGame()
    {
        audioManager?.PlayClick();
        Time.timeScale = 1f;
        uiManager?.ShowPauseOverlay(false);
    }

    public void RestartGate()
    {
        audioManager?.PlayClick();
        Time.timeScale = 1f;
        uiManager?.ShowPauseOverlay(false);
        StartGate(currentGate);
    }

    public void BackToMap()
    {
        audioManager?.PlayClick();
        Time.timeScale = 1f;
        uiManager?.ShowMenuPanel();
        levelManager?.RefreshMenu();
    }

    public void Home()
    {
        BackToMap();
    }

    public void OpenHowToPlay()
    {
        audioManager?.PlayClick();
        uiManager?.ShowHowToPlayOverlay(true);
    }

    public void CloseHowToPlay()
    {
        audioManager?.PlayClick();
        uiManager?.ShowHowToPlayOverlay(false);
    }

    public void OnPlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnHome()
    {
        Time.timeScale = 1f;

        if (RewardManager.Instance != null)
            RewardManager.Instance.HideAll();

        if (UnityAndroidMediator.Instance != null)
            UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");

        //if (GameLoader.Instance != null)
        //    GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");

        SceneManager.LoadScene(bloomHomeSceneName);
    }

    public void OnRewardScreenOpen()
    {
        audioManager?.StopMusic();
    }

    private IEnumerator RunBloomPreGameFlow()
    {
        RewardManager rewardManager = RewardManager.Instance;
        if (rewardManager == null)
        {
            Debug.LogWarning("TreasureQuestGameManager: RewardManager.Instance not found. Skipping Bloom pre-game panel.");
            yield break;
        }

        rewardManager.ShowPreGame(runtimeBloomSkills);
        yield return new WaitUntil(() => rewardManager.IsPreGameComplete);
    }

    private void ShowBloomPostGameForLastResult()
    {
        bloomPostShownForCurrentResult = true;

        RewardManager rewardManager = RewardManager.Instance;
        if (rewardManager == null)
        {
            Debug.LogWarning("TreasureQuestGameManager: RewardManager.Instance not found. Skipping Bloom post-game panel.");
            return;
        }

        float expectedMaxTime = Mathf.Max(1f, lastTotalQuestions * expectedSecondsPerQuestion);
        float timeScore = Mathf.Clamp01(1f - (lastTimeTaken / expectedMaxTime));
        float accuracyScore = lastTotalQuestions > 0
            ? Mathf.Clamp01((float)lastCorrectCount / lastTotalQuestions)
            : 0f;

        GameEvaluationData eval = new GameEvaluationData
        {
            timeScore = timeScore,
            accuracyScore = accuracyScore,
            mistakeCount = lastMistakeCount,
            timeTaken = lastTimeTaken
        };

        rewardManager.ShowPostGame(runtimeBloomSkills, eval);
    }

    private void BuildBloomSkillList()
    {
        runtimeBloomSkills.Clear();

        if (bloomSkillSettings == null || bloomSkillSettings.Count == 0)
        {
            runtimeBloomSkills.Add(new SkillEntry(BloomSkillType.Remember, 100f));
            runtimeBloomSkills.Add(new SkillEntry(BloomSkillType.Understand, 75f));
            return;
        }

        for (int i = 0; i < bloomSkillSettings.Count; i++)
        {
            if (bloomSkillSettings[i] == null) continue;
            runtimeBloomSkills.Add(bloomSkillSettings[i].ToSkillEntry());
        }
    }

    private void AutoFindMissingReferences()
    {
        if (uiManager == null) uiManager = FindObjectOfType<TreasureQuestUIManager>();
        if (levelManager == null) levelManager = FindObjectOfType<TreasureQuestLevelManager>();
        if (quizManager == null) quizManager = FindObjectOfType<TreasureQuestQuizManager>();
        if (questionDatabase == null) questionDatabase = FindObjectOfType<TreasureQuestQuestionDatabase>();
        if (audioManager == null) audioManager = FindObjectOfType<TreasureQuestAudioManager>();
    }
}

[System.Serializable]
public class TreasureQuestBloomSkillSetting
{
    public BloomSkillType skillType = BloomSkillType.Remember;
    [Min(1f)] public float maxScore = 100f;
    [Tooltip("Use -1 to use RewardManager global default time weight.")]
    public float timeWeight = -1f;
    [Tooltip("Use -1 to use RewardManager global default accuracy weight.")]
    public float accuracyWeight = -1f;

    public TreasureQuestBloomSkillSetting() { }

    public TreasureQuestBloomSkillSetting(BloomSkillType skillType, float maxScore)
    {
        this.skillType = skillType;
        this.maxScore = maxScore;
        timeWeight = -1f;
        accuracyWeight = -1f;
    }

    public SkillEntry ToSkillEntry()
    {
        return new SkillEntry(skillType, maxScore, timeWeight: timeWeight, accuracyWeight: accuracyWeight);
    }
}
