using DG.Tweening;
using RewardSystem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EntryFlowController : MonoBehaviour
{
    [Header("Character")]
    public Transform characterParent;
    public SpriteRenderer colored;
    public GameObject uncolored;

    [Header("Background")]
    public Image backgroundImage;
    public float gameplayBgAlpha = 0.4f;
    public float fullBgAlpha = 1f;

    [Header("Dialogue")]
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;
    public CanvasGroup dialogueCanvasGroup;

    public List<string> introDialogues;
    public List<string> secondDialogues;

    [Header("UI")]
    public GameObject gameplayUI;

    [Header("Settings")]
    public float typeSpeed = 0.04f;
    public float memoryDuration = 6f;

    public static bool isGameActive = false;
    const string TUTORIAL_KEY = "TUTORIAL_DONE";

    static List<string> names = new List<string>()
    {
        "Dora",
        "Mia",
        "Lily",
        "Sofia",
        "Emma",
        "Ava",
        "Chloe",
        "Zoe",
        "Nina",
        "Ella"
    };

    public List<SkillEntry> _skills = new()
   {
       new SkillEntry(BloomSkillType.Remember,   100f),
       new SkillEntry(BloomSkillType.Understand,  50f),
   };

    void Start()
    {
        GirlsGameManager.instance.SetupColors();
        characterParent.localScale = Vector3.zero;

        AudioManager.Instance.PlayBGM(0);
        RewardManager.Instance.ShowPreGame(_skills);
        StartCoroutine(EntryFlow());
    }

    IEnumerator EntryFlow()
    {
        isGameActive = false;

        // Initial setup
        gameplayUI.SetActive(false);
        dialogueBox.SetActive(false);

        yield return new WaitUntil(() => RewardManager.Instance.IsPreGameComplete);

        SetBackgroundAlpha(fullBgAlpha);

        // 🔥 STEP 1: Assign dynamic colors
        //GirlsGameManager.instance.SetupColors();

        // 🎬 Character pop
        //characterParent.localScale = Vector3.zero;

        yield return characterParent.DOScale(1.1f, 0.4f).SetEase(Ease.OutBack).WaitForCompletion();
        yield return characterParent.DOScale(1f, 0.2f).WaitForCompletion();

        yield return new WaitForSeconds(0.5f);

        // 💬 INTRO DIALOGUES
        yield return ShowDialogueSequence(GetIntroLine());
        yield return ShowDialogueSequence(introDialogues);

        // 🫁 Breathing animation (memory phase)
        Tween breatheTween = characterParent.DOScale(1.05f, 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        // 👁️ Memory phase (user sees colored version)
        yield return new WaitForSeconds(memoryDuration);

        breatheTween.Kill();
        characterParent.localScale = Vector3.one;

        // 🎨 STEP 2: Transition → White (IMPORTANT CHANGE)
        foreach (var part in GirlsGameManager.instance.parts)
        {
            part.ResetToWhite();
        }

        yield return new WaitForSeconds(1f); // wait for color fade

        // 🌫️ Fade background to gameplay mode
        yield return backgroundImage.DOFade(gameplayBgAlpha, 1f).WaitForCompletion();

        // 💬 SECOND DIALOGUES
        yield return ShowDialogueSequence(secondDialogues);

        // 🎮 Enable gameplay UI
        gameplayUI.SetActive(true);
        gameplayUI.transform.localScale = Vector3.zero;
        gameplayUI.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        if (!PlayerPrefs.HasKey("TUTORIAL_DONE"))
        {
            TutorialController.Instance.StartTutorial();
        }
        isGameActive = true;
    }
    // =========================
    // Dialogue System
    // =========================

    public IEnumerator ShowDialogueSequence(List<string> lines)
    {
        dialogueBox.SetActive(true);
        dialogueCanvasGroup.alpha = 1f;

        foreach (string line in lines)
        {
            yield return StartCoroutine(TypeLine(line));
            yield return new WaitForSeconds(1f);
        }

        yield return dialogueCanvasGroup.DOFade(0f, 0.5f).WaitForCompletion();
        dialogueBox.SetActive(false);
    }

    IEnumerator TypeLine(string line)
    {
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    // =========================
    // Utility
    // =========================

    void SetBackgroundAlpha(float alpha)
    {
        Color c = backgroundImage.color;
        c.a = alpha;
        backgroundImage.color = c;
    }
    public static List<string> GetIntroLine()
    {
        string randomName = names[Random.Range(0, names.Count)];

        string line = $"Hi there! I'm {randomName} 😊";

        return new List<string> { line };
    }
}