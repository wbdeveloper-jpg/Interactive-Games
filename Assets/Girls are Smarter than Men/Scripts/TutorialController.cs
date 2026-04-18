using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance;

    [Header("Canvas")]
    public Canvas tutorialCanvas;   // overlay canvas
    public Canvas paletteCanvas;    // palette canvas

    [Header("Hands")]
    public GameObject hand1; // palette hand
    public GameObject hand2; // object hand

    [Header("Dialogue")]
    public EntryFlowController entryFlow;

    private int step = 0;
    private bool isRunning = false;

    private Tween handTween1;
    private Tween handTween2;

    const string TUTORIAL_KEY = "TUTORIAL_DONE";

    private void Awake()
    {
        Instance = this;
    }

    public void StartTutorial()
    {
        isRunning = true;
        step = 0;

        tutorialCanvas.gameObject.SetActive(true);

        SetupStep1();
    }

    // =========================
    // STEP 1 (Select Color)
    // =========================

    void SetupStep1()
    {
        paletteCanvas.sortingOrder = 7;
        tutorialCanvas.sortingOrder = 7;

        hand1.SetActive(true);
        hand2.SetActive(false);

        StartHandAnimation(hand1, ref handTween1);

        ShowInstruction("Tap a color");
    }

    // =========================
    // STEP 2 (Color Object)
    // =========================

    void SetupStep2()
    {
        paletteCanvas.sortingOrder = 3;
        tutorialCanvas.sortingOrder = 4;

        StopHandAnimation(handTween1);
        hand1.SetActive(false);

        hand2.SetActive(true);
        StartHandAnimation(hand2, ref handTween2);

        ShowInstruction("Now tap to color it!");
    }

    // =========================
    // COMPLETE
    // =========================

    void EndTutorial()
    {
        StopHandAnimation(handTween2);

        hand1.SetActive(false);
        hand2.SetActive(false);
        ShowInstruction("Yay! You're ready!");
        tutorialCanvas.gameObject.SetActive(false);

        isRunning = false;
        PlayerPrefs.SetInt("TUTORIAL_DONE", 1);
        PlayerPrefs.Save();
    }

    // =========================
    // EVENTS (CALLED FROM GAME)
    // =========================

    public void OnColorSelected()
    {
        if (!isRunning || step != 0) return;

        step = 1;
        SetupStep2();
    }

    public void OnObjectColored()
    {
        if (!isRunning || step != 1) return;

        step = 2;
        EndTutorial();
    }

    // =========================
    // HAND ANIMATION
    // =========================

    void StartHandAnimation(GameObject hand, ref Tween tween)
    {
        hand.transform.localScale = Vector3.one;

        tween = hand.transform.DOScale(1.1f, 0.6f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    void StopHandAnimation(Tween tween)
    {
        if (tween != null)
            tween.Kill();
    }

    // =========================
    // DIALOGUE
    // =========================

    void ShowInstruction(string text)
    {
        List<string> lines = new List<string> { text };
        entryFlow.StartCoroutine(entryFlow.ShowDialogueSequence(lines));
    }
}