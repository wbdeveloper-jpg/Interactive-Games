using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HeroInMaking : MonoBehaviour
{
    [Header("Character Levels")]
    [Tooltip("Index 0 = Level 1")]
    [SerializeField] private List<GameObject> heroLevelCharacters;
    [SerializeField] private float entryDelay;
    private Animator currentHeroAnimator;

    [Header("Animator Parameters")]
    [SerializeField] private string talkingBool = "Talking";
    [SerializeField] private string correctTrigger = "SPL";
    [SerializeField] private string sadTrigger = "Sad";

    [Header("Hero Level UI")]
    [SerializeField] private TMP_Text heroLevelText;

    [Header("Dialogue UI")]
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Dialogue Audio")]
    [SerializeField] private AudioSource narrationSource;
    [SerializeField] private float fallbackTypewriterSpeed = 0.03f;

    [Header("Dialogue Timing")]
    [SerializeField] private float dialogueLineGap = 0.7f;

    [Header("Intro / Outro Dialogues")]
    public List<DialogueData> introDialogues;
    public List<DialogueData> outroDialoguesMax;
    public List<DialogueData> outroDialoguesMid;
    public List<DialogueData> outroDialoguesMin;

    [Header("Question Board")]
    [SerializeField] private CanvasGroup questionBoardGroup;
    [SerializeField] private RectTransform questionBoard;
    [SerializeField] private TMP_Text questionText;

    [Header("Replay Audio Button")]
    [SerializeField] private CanvasGroup replayAudioGroup;
    [SerializeField] private CanvasGroup instruction;

    [Header("Answer Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Particles")]
    [SerializeField] private ParticleSystem correctParticle;
    [SerializeField] private ParticleSystem wrongParticle;
    [SerializeField] private ParticleSystem cardBgParticle;

    [Header("Timings")]
    [SerializeField] private float boardEntryDuration = 0.5f;
    [SerializeField] private float boardRotateDuration = 0.6f;
    [SerializeField] private float buttonPopDelay = 0.1f;
    [SerializeField] private float afterAnswerDelay = 1.5f;

    [Header("Game Over UI")]
    [SerializeField] private CanvasGroup gameOverGroup;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private float gameOverFadeDuration = 0.4f;

    [Header("Questions")]
    public List<QuestionData> questions;

    [Header("Answer Feedback Dialogues")]
    [SerializeField] private List<string> correctFeedbackLines;
    [SerializeField] private List<string> wrongFeedbackLines;
    [SerializeField] private float feedbackFadeDuration = 0.25f;

    private int heroLevel = 1;
    private int currentQuestionIndex = 0;
    private bool inputLocked = false;
    private bool questionResolved = false;

    void Start()
    {
        Application.targetFrameRate = 60;
        ResetUI();
        StopAllParticles();
        StopNarration();
        UpdateHeroLevelUI();
        ActivateHeroLevel(heroLevel);
        StartCoroutine(MainFlow());
    }

    #region Main Flow

    private IEnumerator MainFlow()
    {
        yield return new WaitForSeconds(entryDelay);

        yield return PlayDialogueSequence(introDialogues, true);

        yield return ShowQuestionBoard();
        yield return QuestionLoop();
        yield return HideQuestionBoard();

        if(heroLevel <= 1)
        {
            Debug.Log("I failed to powerup any means, Hero Level is -> " + heroLevel);
            yield return PlayDialogueSequence(outroDialoguesMin, false);
        }
        else if(heroLevel > 1 && heroLevel <= 5)
        {
            Debug.Log("I am really very powerful right now, Hero Level is -> " + heroLevel);
            yield return PlayDialogueSequence(outroDialoguesMid, false);
        }
        else
        {
            Debug.Log("I reached my maximum potential. Hero Level is -> " + heroLevel);
            yield return PlayDialogueSequence(outroDialoguesMax, false);
        }
        HideDialogueText();
        ShowGameOverPanel();
    }

    #endregion

    #region Character Handling

    private void ActivateHeroLevel(int level)
    {
        foreach (var hero in heroLevelCharacters)
            hero.SetActive(false);

        int index = Mathf.Clamp(level - 1, 0, heroLevelCharacters.Count - 1);
        heroLevelCharacters[index].SetActive(true);
        currentHeroAnimator = heroLevelCharacters[index].GetComponentInChildren<Animator>();
    }

    #endregion

    #region Dialogue

    private IEnumerator PlayDialogueSequence(List<DialogueData> dialogues, bool hideAfter)
    {
        dialogueGroup.gameObject.SetActive(true);
        dialogueGroup.alpha = 1;
        dialogueText.gameObject.SetActive(true);

        foreach (DialogueData line in dialogues)
        {
            yield return PlayTextWithAudio(dialogueText, line);
            yield return new WaitForSeconds(dialogueLineGap);
        }

        if (hideAfter)
            yield return FadeOutDialogue(0.3f);
    }

    private IEnumerator FadeOutDialogue(float duration)
    {
        dialogueGroup.DOFade(0, duration);
        yield return new WaitForSeconds(duration);
        dialogueGroup.gameObject.SetActive(false);
    }

    private IEnumerator PlayTextWithAudio(TMP_Text textUI, DialogueData data)
    {
        textUI.text = "";
        SetTalking(true);

        float duration = fallbackTypewriterSpeed * data.text.Length;

        if (data.audio != null)
        {
            narrationSource.Stop();
            narrationSource.clip = data.audio;
            narrationSource.Play();
            duration = data.audio.length;
        }

        float delay = duration / Mathf.Max(1, data.text.Length);

        foreach (char c in data.text)
        {
            textUI.text += c;
            yield return new WaitForSeconds(delay);
        }

        if (data.audio != null)
            yield return new WaitUntil(() => !narrationSource.isPlaying);

        SetTalking(false);
    }

    private void SetTalking(bool value)
    {
        currentHeroAnimator?.SetBool(talkingBool, value);
    }

    private void HideDialogueText()
    {
        dialogueText.gameObject.SetActive(false);
    }

    #endregion

    #region Questions

    private IEnumerator ShowQuestionBoard()
    {
        questionBoardGroup.gameObject.SetActive(true);
        questionBoardGroup.alpha = 0;
        questionBoard.localScale = Vector3.one * 0.95f;

        questionBoardGroup.DOFade(1, boardEntryDuration);
        questionBoard.DOScale(1, boardEntryDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(boardEntryDuration);
    }

    private IEnumerator HideQuestionBoard()
    {
        questionBoardGroup.DOFade(0, boardRotateDuration);
        questionBoard.DOScale(0.95f, boardRotateDuration);

        yield return new WaitForSeconds(boardRotateDuration);
        questionBoardGroup.gameObject.SetActive(false);
    }

    private IEnumerator QuestionLoop()
    {
        while (currentQuestionIndex < questions.Count)
        {
            yield return ShowQuestion(questions[currentQuestionIndex]);
            currentQuestionIndex++;
        }
    }

    private IEnumerator ShowQuestion(QuestionData data)
    {
        inputLocked = true;
        questionResolved = false;

        replayAudioGroup.gameObject.SetActive(false);
        instruction.gameObject.SetActive(false);
        questionText.text = "";

        yield return PlayTextWithAudio(
            questionText,
            new DialogueData { text = data.question, audio = data.questionAudio }
        );

        yield return ShowButtons();

        replayAudioGroup.alpha = 0;
        instruction.alpha = 0;
        replayAudioGroup.gameObject.SetActive(data.questionAudio != null);
        instruction.gameObject.SetActive(data.questionAudio != null);
        replayAudioGroup.DOFade(1, 0.25f);
        instruction.DOFade(1, 0.25f);

        inputLocked = false;
        yield return new WaitUntil(() => questionResolved);
    }

    private IEnumerator ShowButtons()
    {
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);

        yesButton.transform.localScale = Vector3.zero;
        noButton.transform.localScale = Vector3.zero;

        yesButton.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(buttonPopDelay);
        noButton.transform.DOScale(1, 0.3f).SetEase(Ease.OutBack);
    }

    public void OnAnswer(int index)
    {
        if (inputLocked) return;
        inputLocked = true;

        StartCoroutine(OnAnswerSelected(index));
    }

    private IEnumerator OnAnswerSelected(int index)
    {
        

        replayAudioGroup.DOFade(0, 0.2f).OnComplete(() =>
            replayAudioGroup.gameObject.SetActive(false));

        bool correct = index == questions[currentQuestionIndex].correctAnswerIndex;

        if (correct)
        {
            AudioManager.Instance.PlaySFX(0);
            correctParticle.Play();
            cardBgParticle?.Play();
            heroLevel++;
        }
        else
        {
            AudioManager.Instance.PlaySFX(1);
            wrongParticle.Play();
            heroLevel = Mathf.Max(1, heroLevel - 1);
        }

        ActivateHeroLevel(heroLevel);

        if(heroLevel <= 1)
        {
            currentHeroAnimator.Play("Level 0 Idle", 0, 0f);
        }

        yield return ShowAnswerFeedback(correct);

        yield  return new WaitForSeconds(1.5f);
        currentHeroAnimator?.SetTrigger(correct ? correctTrigger : sadTrigger);
        UpdateHeroLevelUI();
        StartCoroutine(ResolveQuestionAfterDelay());
    }

    private IEnumerator ResolveQuestionAfterDelay()
    {
        yield return new WaitForSeconds(afterAnswerDelay);
        yield return NextQuestionTransition();
        questionResolved = true;
    }

    private IEnumerator NextQuestionTransition()
    {
        // Rotate + fade OUT
        questionBoardGroup.DOFade(0.3f, boardRotateDuration / 2);

        questionBoard
            .DORotate(new Vector3(0, 90, 0), boardRotateDuration / 2)
            .SetEase(Ease.InSine);

        yield return new WaitForSeconds(boardRotateDuration / 2);

        // Reset rotation to opposite side
        questionBoard.localRotation = Quaternion.Euler(0, -90, 0);

        // Fade + rotate IN
        questionBoardGroup.DOFade(1f, boardRotateDuration / 2);

        questionBoard
            .DORotate(Vector3.zero, boardRotateDuration / 2)
            .SetEase(Ease.OutSine);

        // Hide buttons AFTER rotation
        yesButton.transform.DOScale(0, 0.2f);
        noButton.transform.DOScale(0, 0.2f);

        yield return new WaitForSeconds(0.2f);

        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }

    private IEnumerator ShowAnswerFeedback(bool correct)
    {
        List<string> pool = correct ? correctFeedbackLines : wrongFeedbackLines;

        if (pool == null || pool.Count == 0)
            yield break;

        string line = pool[Random.Range(0, pool.Count)];

        // Show dialogue
        dialogueGroup.gameObject.SetActive(true);
        dialogueGroup.alpha = 0;

        dialogueText.gameObject.SetActive(true);
        dialogueText.text = "";

        // Fade in
        dialogueGroup.DOFade(1, feedbackFadeDuration);

        // Talking ON
        SetTalking(true);

        // Typewriter animation (FAST)
        float speed = 0.05f;
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(speed);
        }

        // Small hold time
        yield return new WaitForSeconds(1.5f);

        // Talking OFF
        SetTalking(false);

        // Fade OUT
        dialogueGroup.DOFade(0, feedbackFadeDuration);
        yield return new WaitForSeconds(feedbackFadeDuration);

        dialogueGroup.gameObject.SetActive(false);
    }
    #endregion

    #region Game Over

    private void ShowGameOverPanel()
    {
        gameOverGroup.gameObject.SetActive(true);
        gameOverGroup.alpha = 0;
        gameOverGroup.transform.localScale = Vector3.one * 0.9f;

        gameOverGroup.DOFade(1, gameOverFadeDuration);
        gameOverGroup.transform.DOScale(1, gameOverFadeDuration).SetEase(Ease.OutBack);

        playAgainButton.onClick.AddListener(() => { Debug.Log("Play Again"); LoadScene(); });
        mainMenuButton.onClick.AddListener(() => { 
            Debug.Log("Main Menu");
            UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");
            GameLoader.Instance.SendEventToJS("Game Done", "Being Honest"); 
            MainMenu(); 
        });
    }

    public void LoadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Loader Scene");
    }

    #endregion

    #region Utility

    private void UpdateHeroLevelUI()
    {
        heroLevelText.text = $"Hero Level: {heroLevel}";
    }

    private void ResetUI()
    {
        dialogueGroup.gameObject.SetActive(false);
        questionBoardGroup.gameObject.SetActive(false);
        replayAudioGroup.gameObject.SetActive(false);
        instruction.gameObject.SetActive(false);
        gameOverGroup.gameObject.SetActive(false);
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }

    private void StopNarration()
    {
        narrationSource?.Stop();
        SetTalking(false);
    }

    private void StopAllParticles()
    {
        correctParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        wrongParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        cardBgParticle?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void PlayCurrentQuestionAudio()
    {
        if (currentQuestionIndex >= questions.Count) return;

        var clip = questions[currentQuestionIndex].questionAudio;
        if (clip == null) return;

        narrationSource.Stop();
        narrationSource.clip = clip;
        narrationSource.Play();

        SetTalking(true);
        StartCoroutine(StopTalkingAfterAudio(clip.length));
    }

    private IEnumerator StopTalkingAfterAudio(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetTalking(false);
    }

    #endregion
}

[System.Serializable]
public class DialogueData
{
    [TextArea] public string text;
    public AudioClip audio;
}

[System.Serializable]
public class QuestionData
{
    [TextArea] public string question;
    public AudioClip questionAudio;
    public int correctAnswerIndex;
}
