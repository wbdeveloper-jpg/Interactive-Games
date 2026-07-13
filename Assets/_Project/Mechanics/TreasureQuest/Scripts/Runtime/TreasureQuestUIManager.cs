using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TreasureQuestUIManager : MonoBehaviour
{
    [Header("Global Fonts")]
    public TMP_FontAsset primaryFont;
    public TMP_FontAsset secondaryFont;
    public bool applyFontsOnBind = true;

    [Header("Major Panels")]
    public GameObject loadingPanel;
    public GameObject menuPanel;
    public GameObject gameplayPanel;
    public GameObject gateUnlockedPanel;
    public GameObject pausePanel;
    public GameObject howToPlayPanel;

    [Header("Loading UI")]
    public Image loadingTitleImage;
    public Sprite loadingTitleSprite;

    [Header("Menu UI")]
    public Image menuTitleImage;
    public Sprite menuTitleSprite;
    [Tooltip("Legacy title text support only. New template uses menuTitleImage instead.")]
    public TMP_Text menuTitleText;
    public Button playButton;
    public Button homeButton;
    [Tooltip("Legacy field. New template uses Menu How To Play Button instead of settings.")]
    public Button settingsButton;
    public Button menuHowToPlayButton;
    public TMP_Text lockedFeedbackText;
    public Image treasureChestImage;
    public Sprite treasureChestLockedSprite;
    public Sprite treasureChestUnlockedSprite;
    public TreasureQuestGateButton[] gateButtons = new TreasureQuestGateButton[5];

    [Header("Menu Gate Sprites")]
    public Sprite menuGateLockedSprite;
    public Sprite menuGateUnlockedSprite;
    public Sprite menuGateCompletedSprite;

    [Header("Common Icon Sprites")]
    public Sprite homeIconSprite;
    public Sprite howToPlayIconSprite;
    [Tooltip("Legacy icon support only.")]
    public Sprite settingsIconSprite;
    public Sprite gameplayHomeIconSprite;
    public Sprite pauseIconSprite;

    [Header("Gameplay UI")]
    public Button gameplayHomeButton;
    public Button pauseButton;
    public Image gateTitleBackgroundImage;
    public Sprite gateTitleBackgroundSprite;
    public TMP_Text gateTitleText;
    public Slider progressSlider;
    public TMP_Text progressText;
    public Image coinGroupBackgroundImage;
    public Sprite coinGroupBackgroundSprite;
    public TMP_Text coinText;
    public TMP_Text questionText;
    public TMP_Text gateStatusText;
    public Image gameplayGateImage;
    public Sprite gameplayGateClosedLockedSprite;
    public Sprite gameplayGateOpenUnlockedSprite;
    public Image[] progressSteps = new Image[5];
    public TreasureQuestAnswerButton[] answerButtons = new TreasureQuestAnswerButton[4];

    [Header("Progress Step Sprites")]
    public Sprite progressStepInactiveSprite;
    public Sprite progressStepActiveSprite;

    [Header("Result UI")]
    public TMP_Text resultTitleText;
    public TMP_Text resultDetailsText;
    public Image resultGateImage;
    public Button resultContinueButton;
    public TMP_Text resultContinueButtonText;

    [Header("Pause UI")]
    public Button resumeButton;
    public Button pauseHowToPlayButton;
    public Button restartGateButton;
    public Button backToMapButton;

    [Header("How To Play UI")]
    [Tooltip("Main guide image. Assign page sprites through howToPlayGuideSprites.")]
    public Image howToPlayGuideImage;
    public Sprite[] howToPlayGuideSprites = new Sprite[0];
    public TMP_Text howToPlayPageText;
    public Button howToPlayPrevButton;
    public Button howToPlayNextButton;
    public Button howToPlayContinueButton;

    [Header("How To Play Legacy Buttons")]
    [Tooltip("Legacy support only. New template uses Prev / Next / Continue.")]
    public Button howToPlayCloseButton;
    [Tooltip("Legacy support only. New template uses Prev / Next / Continue.")]
    public Button howToPlayStartButton;

    [Header("Messages")]
    public string gateLockedMessageFormat = "Answer all {0} correctly to unlock the gate!";
    public string gateOpenMessage = "Gate Unlocked! Treasure Path Open!";

    [Header("Animation")]
    public float panelScaleDuration = 0.22f;
    public float lockedMessageDuration = 1.2f;

    private TreasureQuestGameManager gameManager;
    private Sequence lockedMessageSequence;
    private int howToPlayPageIndex;

    public void Bind(TreasureQuestGameManager game)
    {
        gameManager = game;

        AddButton(playButton, gameManager.PlayHighestUnlockedGate);
        AddButton(homeButton, gameManager.Home);
        AddButton(menuHowToPlayButton, gameManager.OpenHowToPlay);

        // Legacy support for v1/v2 scenes where this button was named SettingsButton.
        if (settingsButton != null && settingsButton != menuHowToPlayButton)
            AddButton(settingsButton, gameManager.OpenHowToPlay);

        AddButton(gameplayHomeButton, gameManager.Home);
        AddButton(pauseButton, gameManager.PauseGame);
        AddButton(resultContinueButton, gameManager.ContinueFromResult);
        AddButton(resumeButton, gameManager.ResumeGame);
        AddButton(pauseHowToPlayButton, gameManager.OpenHowToPlay);
        AddButton(restartGateButton, gameManager.RestartGate);
        AddButton(backToMapButton, gameManager.BackToMap);
        AddButton(howToPlayPrevButton, ShowPreviousHowToPlayPage);
        AddButton(howToPlayNextButton, ShowNextHowToPlayPage);
        AddButton(howToPlayContinueButton, gameManager.CloseHowToPlay);

        // Legacy support for older text-based HTP panels.
        AddButton(howToPlayCloseButton, gameManager.CloseHowToPlay);
        AddButton(howToPlayStartButton, gameManager.CloseHowToPlay);

        ApplyOptionalSprites();
        if (applyFontsOnBind)
            ApplyConfiguredFonts();
    }

    [ContextMenu("Apply Configured Fonts")]
    public void ApplyConfiguredFonts()
    {
        TMP_FontAsset bodyFont = secondaryFont != null ? secondaryFont : primaryFont;
        if (bodyFont == null) return;

        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < allTexts.Length; i++)
        {
            if (allTexts[i] == null) continue;
            allTexts[i].font = bodyFont;
        }

        TMP_FontAsset headingFont = primaryFont != null ? primaryFont : bodyFont;
        ApplyFont(headingFont, menuTitleText);
        ApplyFont(headingFont, gateTitleText);
        ApplyFont(headingFont, resultTitleText);

        if (gateButtons != null)
        {
            for (int i = 0; i < gateButtons.Length; i++)
            {
                if (gateButtons[i] != null && gateButtons[i].gateLabel != null)
                    gateButtons[i].gateLabel.font = headingFont;
            }
        }

        if (answerButtons != null)
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null && answerButtons[i].answerText != null)
                    answerButtons[i].answerText.font = bodyFont;
            }
        }
    }

    public void ApplyOptionalSprites()
    {
        SetImageSprite(loadingTitleImage, loadingTitleSprite, false);
        SetImageSprite(menuTitleImage, menuTitleSprite, false);
        SetImageSprite(homeButton != null ? homeButton.targetGraphic as Image : null, homeIconSprite, false);
        SetImageSprite(menuHowToPlayButton != null ? menuHowToPlayButton.targetGraphic as Image : null, howToPlayIconSprite != null ? howToPlayIconSprite : settingsIconSprite, false);
        SetImageSprite(settingsButton != null ? settingsButton.targetGraphic as Image : null, howToPlayIconSprite != null ? howToPlayIconSprite : settingsIconSprite, false);
        SetImageSprite(gameplayHomeButton != null ? gameplayHomeButton.targetGraphic as Image : null, gameplayHomeIconSprite != null ? gameplayHomeIconSprite : homeIconSprite, false);
        SetImageSprite(pauseButton != null ? pauseButton.targetGraphic as Image : null, pauseIconSprite, false);
        SetImageSprite(gateTitleBackgroundImage, gateTitleBackgroundSprite, true);
        SetImageSprite(coinGroupBackgroundImage, coinGroupBackgroundSprite, true);
    }

    public void SetupAnswerButtons(TreasureQuestQuizManager quizManager)
    {
        if (answerButtons == null) return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null) continue;
            answerButtons[i].Setup(quizManager);
        }
    }

    public void ShowLoadingPanel()
    {
        ShowOnlyMajorPanel(loadingPanel);
    }

    public void ShowMenuPanel()
    {
        Time.timeScale = 1f;
        ShowOnlyMajorPanel(menuPanel);
        HideOverlayPanels();
        AnimatePanel(menuPanel);
    }

    public void ShowGameplayPanel()
    {
        Time.timeScale = 1f;
        ShowOnlyMajorPanel(gameplayPanel);
        HideOverlayPanels();
        AnimatePanel(gameplayPanel);
    }

    public void ShowResultPanel(bool passed, int gateNumber, int correctCount, int totalQuestions, int coinsEarned, bool finalTreasureUnlocked)
    {
        ShowOnlyMajorPanel(gateUnlockedPanel);
        HideOverlayPanels();

        if (resultTitleText != null)
            resultTitleText.text = passed ? "Gate Completed!" : "Gate Still Locked!";

        if (resultDetailsText != null)
        {
            string line1 = "Gate " + gateNumber + " Result";
            string line2 = "Correct: " + correctCount + " / " + totalQuestions;
            string line3 = "Coins Earned: " + coinsEarned;
            string line4 = finalTreasureUnlocked ? "Final treasure chest unlocked!" : (passed ? "Next gate unlocked!" : "All answers must be correct to open this gate.");
            resultDetailsText.text = line1 + "\n" + line2 + "\n" + line3 + "\n" + line4;
        }

        if (resultGateImage != null)
            resultGateImage.sprite = passed && gameplayGateOpenUnlockedSprite != null ? gameplayGateOpenUnlockedSprite : gameplayGateClosedLockedSprite;

        if (resultContinueButtonText != null)
            resultContinueButtonText.text = "Continue to Map";

        AnimatePanel(gateUnlockedPanel);
    }

    public void ShowPauseOverlay(bool visible)
    {
        if (pausePanel == null) return;
        pausePanel.SetActive(visible);
        if (visible) AnimatePanel(pausePanel);
    }

    public void ShowHowToPlayOverlay(bool visible)
    {
        if (howToPlayPanel == null) return;

        howToPlayPanel.SetActive(visible);
        if (visible)
        {
            SetHowToPlayPage(0);
            AnimatePanel(howToPlayPanel);
        }
    }

    public void ShowNextHowToPlayPage()
    {
        int count = GetHowToPlayPageCount();
        SetHowToPlayPage(Mathf.Min(howToPlayPageIndex + 1, count - 1));
    }

    public void ShowPreviousHowToPlayPage()
    {
        SetHowToPlayPage(Mathf.Max(howToPlayPageIndex - 1, 0));
    }

    public void SetHowToPlayPage(int pageIndex)
    {
        int count = GetHowToPlayPageCount();
        howToPlayPageIndex = Mathf.Clamp(pageIndex, 0, count - 1);

        if (howToPlayGuideImage != null)
        {
            Sprite pageSprite = GetHowToPlaySprite(howToPlayPageIndex);
            if (pageSprite != null)
            {
                howToPlayGuideImage.sprite = pageSprite;
                howToPlayGuideImage.color = Color.white;
                howToPlayGuideImage.preserveAspect = true;
            }
            else
            {
                howToPlayGuideImage.sprite = null;
                howToPlayGuideImage.color = new Color(1f, 0.90f, 0.67f, 0.96f);
            }
        }

        if (howToPlayPageText != null)
            howToPlayPageText.text = count > 1 ? (howToPlayPageIndex + 1) + " / " + count : "";

        if (howToPlayPrevButton != null)
            howToPlayPrevButton.interactable = count > 1 && howToPlayPageIndex > 0;

        if (howToPlayNextButton != null)
            howToPlayNextButton.interactable = count > 1 && howToPlayPageIndex < count - 1;
    }

    private int GetHowToPlayPageCount()
    {
        return howToPlayGuideSprites != null && howToPlayGuideSprites.Length > 0 ? howToPlayGuideSprites.Length : 1;
    }

    private Sprite GetHowToPlaySprite(int pageIndex)
    {
        if (howToPlayGuideSprites == null || howToPlayGuideSprites.Length == 0) return null;
        if (pageIndex < 0 || pageIndex >= howToPlayGuideSprites.Length) return null;
        return howToPlayGuideSprites[pageIndex];
    }

    public void HideOverlayPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void HideAllPanels()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (gateUnlockedPanel != null) gateUnlockedPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
    }

    public void RefreshGateButtons(TreasureQuestLevelManager levelManager)
    {
        if (gateButtons == null || levelManager == null) return;

        for (int i = 0; i < gateButtons.Length; i++)
        {
            TreasureQuestGateButton gateButton = gateButtons[i];
            if (gateButton == null) continue;
            TreasureQuestGateState state = levelManager.GetGateState(gateButton.gateNumber);
            gateButton.ApplyState(state, menuGateLockedSprite, menuGateUnlockedSprite, menuGateCompletedSprite);
        }
    }

    public void SetTreasureChestUnlocked(bool unlocked)
    {
        if (treasureChestImage == null) return;

        Sprite target = unlocked ? treasureChestUnlockedSprite : treasureChestLockedSprite;
        if (target != null)
            treasureChestImage.sprite = target;

        treasureChestImage.color = Color.white;
    }

    public void ShowLockedGateFeedback(string message)
    {
        if (lockedFeedbackText == null) return;

        lockedFeedbackText.text = message;
        lockedFeedbackText.gameObject.SetActive(true);
        lockedFeedbackText.alpha = 0f;

        lockedMessageSequence?.Kill();
        lockedMessageSequence = DOTween.Sequence();
        lockedMessageSequence.Append(lockedFeedbackText.DOFade(1f, 0.15f));
        lockedMessageSequence.AppendInterval(lockedMessageDuration);
        lockedMessageSequence.Append(lockedFeedbackText.DOFade(0f, 0.2f));
        lockedMessageSequence.OnComplete(() => lockedFeedbackText.gameObject.SetActive(false));
    }

    public void SetGameplayHeader(int gateNumber)
    {
        if (gateTitleText != null)
            gateTitleText.text = "Gate " + gateNumber;

        SetGameplayGate(false);
        SetGateStatus(false);
    }

    public void SetQuestion(string text)
    {
        if (questionText != null)
            questionText.text = text;
    }

    public void SetAnswerData(string[] answers)
    {
        if (answerButtons == null || answers == null) return;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] == null) continue;
            string answer = i < answers.Length ? answers[i] : string.Empty;
            answerButtons[i].gameObject.SetActive(!string.IsNullOrEmpty(answer));
            answerButtons[i].SetData(i, answer);
        }
    }

    public void SetAllAnswerButtonsInteractable(bool value)
    {
        if (answerButtons == null) return;
        for (int i = 0; i < answerButtons.Length; i++)
            if (answerButtons[i] != null) answerButtons[i].SetInteractable(value);
    }

    public void UpdateProgress(int answeredCount, int totalCount)
    {
        if (progressText != null)
            progressText.text = answeredCount + " / " + totalCount;

        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = Mathf.Max(1, totalCount);
            progressSlider.value = Mathf.Clamp(answeredCount, 0, totalCount);
        }

        if (progressSteps == null) return;

        for (int i = 0; i < progressSteps.Length; i++)
        {
            if (progressSteps[i] == null) continue;

            bool active = i < answeredCount;
            if (active && progressStepActiveSprite != null) progressSteps[i].sprite = progressStepActiveSprite;
            if (!active && progressStepInactiveSprite != null) progressSteps[i].sprite = progressStepInactiveSprite;
            progressSteps[i].color = Color.white;
        }
    }

    public void UpdateCoinText(int coins)
    {
        if (coinText != null)
            coinText.text = coins.ToString();
    }

    public void SetGameplayGate(bool open)
    {
        if (gameplayGateImage == null) return;

        Sprite sprite = open ? gameplayGateOpenUnlockedSprite : gameplayGateClosedLockedSprite;
        if (sprite != null)
            gameplayGateImage.sprite = sprite;

        gameplayGateImage.DOKill();
        if (open)
        {
            gameplayGateImage.transform.localScale = Vector3.one;
            gameplayGateImage.transform.DOPunchScale(Vector3.one * 0.12f, 0.35f, 8, 0.8f);
        }
    }

    public void SetGateStatus(bool open, int questionCount = 5, bool allCorrectRequired = true)
    {
        if (gateStatusText == null) return;

        if (open)
        {
            gateStatusText.text = gateOpenMessage;
            return;
        }

        gateStatusText.text = allCorrectRequired
            ? string.Format(gateLockedMessageFormat, Mathf.Max(1, questionCount))
            : "Finish the quiz to unlock the gate!";
    }

    private void ShowOnlyMajorPanel(GameObject panelToShow)
    {
        if (loadingPanel != null) loadingPanel.SetActive(loadingPanel == panelToShow);
        if (menuPanel != null) menuPanel.SetActive(menuPanel == panelToShow);
        if (gameplayPanel != null) gameplayPanel.SetActive(gameplayPanel == panelToShow);
        if (gateUnlockedPanel != null) gateUnlockedPanel.SetActive(gateUnlockedPanel == panelToShow);
    }

    private void AnimatePanel(GameObject panel)
    {
        if (panel == null) return;
        RectTransform rect = panel.transform as RectTransform;
        if (rect == null) return;

        rect.DOKill();
        rect.localScale = Vector3.one * 0.97f;
        rect.DOScale(1f, panelScaleDuration).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void AddButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void ApplyFont(TMP_FontAsset font, TMP_Text text)
    {
        if (font == null || text == null) return;
        text.font = font;
    }

    private void SetImageSprite(Image image, Sprite sprite, bool sliced)
    {
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
    }
}
