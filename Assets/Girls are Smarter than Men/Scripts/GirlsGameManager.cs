using DG.Tweening;
using RewardSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GirlsGameManager : MonoBehaviour, IGameSceneCallbacks
{
    public ColorablePart[] parts;
    public float delayBetweenParts = 0.2f;

    public GameObject floatingTextPrefab;
    public Transform floatingTextParent;

    public Transform mainParent;

    [Header("Flight Settings")]
    public float flyDuration = 8f;
    public float moveUpDistance = 6f;
    public float horizontalAmplitude = 1f;
    public float wobbleSpeed = 4f;

    [Header("Scale Settings")]
    public float minScale = 0.2f;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    [Header("Dialogue")]
    public EntryFlowController entryFlow;

    public string[] lowDialogues;
    public string[] midDialogues;
    public string[] highDialogues;

    private bool isEvaluating = false;
    public ResultPanelController resultPanel;
    public static GirlsGameManager instance;

    [Header("UI & Background")]
    public GameObject gameplayUI;
    public UnityEngine.UI.Image backgroundImage;
    public float fullBgAlpha = 1f;

    [Header("Color System")]
    public List<ColorData> generalColors; // all colors except skin
    public List<ColorData> skinColors;

    Dictionary<ColorData, int> colorUsage = new Dictionary<ColorData, int>();
    GameEvaluationData gameEvaluationData;
    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void OnDoneClicked()
    {
        if (!isEvaluating)
        {
            StartCoroutine(EvaluateRoutine());
        }
    }

    IEnumerator EvaluateRoutine()
    {
        isEvaluating = true;

        // 🚫 Disable gameplay input
        EntryFlowController.isGameActive = false;

        // ❌ Disable gameplay UI
        gameplayUI.SetActive(false);

        // 🌅 Restore background
        backgroundImage.DOFade(fullBgAlpha, 0.5f);

        int correctCount = 0;

        foreach (var part in parts)
        {
            yield return StartCoroutine(part.EvaluateVisual());

            if (part.IsCorrect())
                correctCount++;

            yield return new WaitForSeconds(delayBetweenParts);
        }

        float accuracy = (float)correctCount / parts.Length * 100f;

        Debug.Log("Accuracy: " + accuracy);

        gameEvaluationData = new GameEvaluationData();

        gameEvaluationData.timeScore = 0.4f;
        gameEvaluationData.timeTaken = 250f;
        gameEvaluationData.accuracyScore = accuracy;
        gameEvaluationData.mistakeCount = parts.Length - correctCount;


        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(HandleResult(accuracy));

        isEvaluating = false;
    }

    // =========================
    // RESULT HANDLER
    // =========================

    IEnumerator HandleResult(float accuracy)
    {
        string[] selectedDialogues;

        if (accuracy < 50f)
        {
            selectedDialogues = lowDialogues;
        }
        else if (accuracy < 80f)
        {
            selectedDialogues = midDialogues;
        }
        else
        {
            selectedDialogues = highDialogues;
        }

        // 💬 Show dialogue
        yield return entryFlow.StartCoroutine(
            entryFlow.ShowDialogueSequence(new List<string>(selectedDialogues))
        );

        yield return new WaitForSeconds(0.3f);
        ClearAllPartVisuals();
        // 🎬 Play animation
        yield return StartCoroutine(PlayEndTransition(accuracy));
    }

    IEnumerator PlayEndTransition(float accuracy)
    {
        mainParent.DOKill();

        Vector3 originalScale = mainParent.localScale;

        Sequence seq = DOTween.Sequence();

        // 🟡 Small reaction (acknowledgement)
        seq.Append(mainParent.DOScale(originalScale * 1.05f, 0.2f))
           .Append(mainParent.DOScale(originalScale, 0.2f));

        // 🫁 Calm settle (very subtle)
        seq.Append(mainParent.DOScale(originalScale * 0.98f, 0.3f))
           .Append(mainParent.DOScale(originalScale, 0.3f));

        // 🌫️ Fade out whole object
        SpriteRenderer[] renderers = mainParent.GetComponentsInChildren<SpriteRenderer>();

        foreach (var r in renderers)
        {
            seq.Join(r.DOFade(0f, 0.6f));
        }

        yield return seq.WaitForCompletion();

        mainParent.gameObject.SetActive(false);
        backgroundImage.DOFade(0.8f, 0.3f);
        // 🎉 Show result panel AFTER fade
        //resultPanel.ShowResult(accuracy);
        RewardManager.Instance.ShowPostGame(entryFlow._skills, gameEvaluationData);
    }

    // =========================
    // FLOATING MESSAGE
    // =========================

    public void ShowMessage(string msg, Color _color)
    {
        GameObject obj = Instantiate(floatingTextPrefab, floatingTextParent);

        FloatingText ft = obj.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Show(msg, _color);
        }
    }

    void ClearAllPartVisuals()
    {
        foreach (var part in parts)
        {
            part.ResetVisual();
        }
    }

    public void SetupColors()
    {
        colorUsage.Clear();

        foreach (var part in parts)
        {
            Debug.Log(part.ToString()); 
            ColorData selected = GetColorForPart(part.partType);
            part.SetCorrectColor(selected);
        }
    }

    ColorData GetColorForPart(PartType partType)
    {
        // 👁 Eyes → always white
        if (partType == PartType.Eyes)
        {
            return new ColorData
            {
                colorName = "White",
                color = Color.white
            };
        }

        // 🧑 Skin → only from skin list
        if (partType == PartType.Skin)
        {
            return skinColors[Random.Range(0, skinColors.Count)];
        }

        // 🎨 Other parts → general colors with max 2 usage
        List<ColorData> candidates = new List<ColorData>(generalColors);

        // Shuffle
        for (int i = 0; i < candidates.Count; i++)
        {
            int rand = Random.Range(i, candidates.Count);
            var temp = candidates[i];
            candidates[i] = candidates[rand];
            candidates[rand] = temp;
        }

        foreach (var c in candidates)
        {
            if (!colorUsage.ContainsKey(c))
                colorUsage[c] = 0;

            if (colorUsage[c] < 2)
            {
                colorUsage[c]++;
                return c;
            }
        }

        // fallback (rare)
        ColorData fallback = candidates[0];
        colorUsage[fallback]++;
        return fallback;
    }

    public void OnPlayAgain()
    {
        PlayAgain();
    }

    public void OnHome()
    {
        MainMenu();
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void MainMenu()
    {
        SceneManager.LoadScene("Loader Scene");
        UnityAndroidMediator.Instance.PassDataToAndroid("Game Done");
        GameLoader.Instance.SendEventToJS("Game Done", "Girls are wiser than man");
    }
}