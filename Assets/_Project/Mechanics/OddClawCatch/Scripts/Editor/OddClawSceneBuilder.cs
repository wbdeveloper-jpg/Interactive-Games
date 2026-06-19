#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class OddClawSceneBuilder
{
    private const string RootName = "OddClawCatch_Root";
    private const string CurrentSceneMenuPath = "Tools/Odd Claw Catch/Create Layout In Current Scene";
    private const string LegacyMenuPath = "Tools/Odd Claw Catch/Create Rough Playable UI Scene";

    private static readonly Color BackgroundTop = new Color(0.06f, 0.09f, 0.18f, 1f);
    private static readonly Color TopBarColor = new Color(0.04f, 0.06f, 0.12f, 0.86f);
    private static readonly Color CardColor = new Color(0.97f, 0.98f, 1f, 1f);
    private static readonly Color ButtonColor = new Color(0.13f, 0.32f, 0.72f, 1f);
    private static readonly Color ButtonAltColor = new Color(0.06f, 0.64f, 0.82f, 1f);
    private static readonly Color GroundColor = new Color(0.12f, 0.09f, 0.07f, 1f);
    private static readonly Color TextDark = new Color(0.04f, 0.06f, 0.11f, 1f);
    private static readonly Color TextLight = new Color(0.96f, 0.98f, 1f, 1f);

    [MenuItem(CurrentSceneMenuPath)]
    public static void CreateLayoutInCurrentScene()
    {
        CreateLayoutInCurrentSceneInternal();
    }

    [MenuItem(LegacyMenuPath)]
    public static void CreateRoughPlayableSceneLegacy()
    {
        CreateLayoutInCurrentSceneInternal();
    }

    private static void CreateLayoutInCurrentSceneInternal()
    {
        GameObject existingRoot = GameObject.Find(RootName);
        if (existingRoot != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Replace Odd Claw Catch Layout?",
                "This scene already has an OddClawCatch_Root. Only that generated root will be replaced. Your scene itself will not be recreated.",
                "Replace Generated Root",
                "Cancel");

            if (!replace)
                return;

            Object.DestroyImmediate(existingRoot);
        }
        else
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Create Odd Claw Catch Layout",
                "This will add the Odd Claw Catch UI elements into the currently active scene. It will not create a new scene and it will not delete other scene objects.",
                "Create In Current Scene",
                "Cancel");

            if (!proceed)
                return;
        }

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;

        GameObject root = new GameObject(RootName);
        Canvas canvas = CreateCanvas(root.transform);
        CreateEventSystemIfMissing();

        RectTransform background = CreateImage("Background", canvas.transform, BackgroundTop);
        Stretch(background);

        RectTransform softGlow = CreateImage("SoftPlayAreaGlow", canvas.transform, new Color(0.05f, 0.45f, 0.75f, 0.14f));
        Anchor(softGlow, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.86f), new Vector2(0.5f, 0.5f));
        softGlow.offsetMin = Vector2.zero;
        softGlow.offsetMax = Vector2.zero;

        OddClawAudioManager audioManager = new GameObject("OddClawAudioManager").AddComponent<OddClawAudioManager>();
        audioManager.transform.SetParent(root.transform, false);

        GameObject managerObject = new GameObject("OddClawCatchManager");
        managerObject.transform.SetParent(root.transform, false);
        OddClawCatchManager manager = managerObject.AddComponent<OddClawCatchManager>();
        manager.rootCanvas = canvas;
        manager.primaryFont = defaultFont;
        manager.secondaryFont = defaultFont;
        manager.audioManager = audioManager;
        manager.aimMode = OddClawAimMode.EasyWithGuideLine;
        manager.itemSpacing = 34f;
        manager.lockItemPositionsAfterSpawn = true;
        manager.organicItemPlacement = true;
        manager.penalizeMiss = false;
        manager.countMissAsAttempt = false;
        manager.dynamicReachPadding = 110f;

        BuildTopBar(canvas.transform, manager, defaultFont);
        BuildClaw(canvas.transform, manager, audioManager);
        BuildGroundItems(canvas.transform, manager, defaultFont);
        BuildFirstPickHint(canvas.transform, manager, defaultFont);
        BuildFeedback(canvas.transform, manager, defaultFont);
        BuildPanels(canvas.transform, manager, defaultFont);

        manager.questionGenerator = CreateOrLoadDefaultMathGenerator();
        manager.correctDelay = 0.15f;
        manager.nextWaveDelay = 0.15f;
        manager.lockClawDuringEvaluation = true;
        manager.evaluationItemHoldBeforeFade = 0.42f;
        manager.evaluationItemFadeDuration = 0.28f;
        manager.waitForPopupBeforeContinuing = true;
        manager.ApplyFontsToAllTexts();

        Selection.activeGameObject = root;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Odd Claw Catch Layout Created",
            "Done. The layout was added inside the current scene under OddClawCatch_Root. The scene was not recreated. Replace the rough claw sprites later from OddClawController.",
            "OK");
    }

    private static Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObject = new GameObject("OddClawCatchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreateEventSystemIfMissing()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Selection.activeGameObject = eventSystem;
    }

    private static void BuildTopBar(Transform parent, OddClawCatchManager manager, TMP_FontAsset font)
    {
        RectTransform topBar = CreateImage("TopBar", parent, TopBarColor);
        Anchor(topBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        topBar.sizeDelta = new Vector2(0f, 150f);
        topBar.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup mainLayout = topBar.gameObject.AddComponent<VerticalLayoutGroup>();
        mainLayout.padding = new RectOffset(18, 18, 10, 10);
        mainLayout.spacing = 8f;
        mainLayout.childAlignment = TextAnchor.MiddleCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = true;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = false;

        RectTransform upperRow = CreateLayoutPanel("Upper_ScoreQuestionPause", topBar, 1f);
        LayoutElement upperLayoutElement = upperRow.gameObject.GetComponent<LayoutElement>();
        upperLayoutElement.preferredHeight = 70f;
        HorizontalLayoutGroup upperLayout = upperRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        upperLayout.spacing = 12f;
        upperLayout.childAlignment = TextAnchor.MiddleCenter;
        upperLayout.childControlWidth = true;
        upperLayout.childControlHeight = true;
        upperLayout.childForceExpandWidth = false;
        upperLayout.childForceExpandHeight = true;

        manager.scoreText = CreateText("ScoreText", upperRow, "Score 0", 25, font, TextAlignmentOptions.Left);
        manager.scoreText.color = TextLight;
        LayoutElement scoreLayout = manager.scoreText.gameObject.AddComponent<LayoutElement>();
        scoreLayout.preferredWidth = 150f;
        scoreLayout.flexibleWidth = 0f;

        RectTransform questionCard = CreateImage("QuestionCard", upperRow, new Color(0.98f, 0.99f, 1f, 0.97f));
        LayoutElement questionLayout = questionCard.gameObject.AddComponent<LayoutElement>();
        questionLayout.flexibleWidth = 1f;
        questionLayout.preferredHeight = 64f;
        Outline questionOutline = questionCard.gameObject.AddComponent<Outline>();
        questionOutline.effectColor = new Color(0f, 0f, 0f, 0.18f);
        questionOutline.effectDistance = new Vector2(0f, -3f);

        TMP_Text question = CreateText("QuestionText", questionCard, "Catch the correct answer", 32, font, TextAlignmentOptions.Center);
        Stretch(question.rectTransform, 16f, 16f, 6f, 6f);
        question.color = TextDark;
        question.enableWordWrapping = true;
        manager.questionText = question;
        manager.questionHeaderText = question;

        RectTransform pauseHolder = CreateLayoutPanel("PauseButtonHolder", upperRow, 0f);
        LayoutElement pauseLayout = pauseHolder.gameObject.GetComponent<LayoutElement>();
        pauseLayout.preferredWidth = 62f;
        pauseLayout.flexibleWidth = 0f;
        manager.pauseButton = CreateIconButton("PauseButton", pauseHolder, 54f);

        RectTransform lowerRow = CreateLayoutPanel("Lower_StatusRow", topBar, 1f);
        LayoutElement lowerLayoutElement = lowerRow.gameObject.GetComponent<LayoutElement>();
        lowerLayoutElement.preferredHeight = 48f;
        HorizontalLayoutGroup lowerLayout = lowerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        lowerLayout.spacing = 12f;
        lowerLayout.childAlignment = TextAnchor.MiddleCenter;
        lowerLayout.childControlWidth = true;
        lowerLayout.childControlHeight = true;
        lowerLayout.childForceExpandWidth = false;
        lowerLayout.childForceExpandHeight = true;

        manager.healthLabel = CreateText("HealthLabel", lowerRow, "HP 3/3", 21, font, TextAlignmentOptions.Left);
        manager.healthLabel.color = TextLight;
        LayoutElement healthLabelLayout = manager.healthLabel.gameObject.AddComponent<LayoutElement>();
        healthLabelLayout.preferredWidth = 78f;
        healthLabelLayout.flexibleWidth = 0f;

        manager.healthSlider = CreateSlider("HealthSlider", lowerRow, new Color(0.13f, 0.9f, 0.35f, 1f), 15f);
        LayoutElement hpSliderLayout = manager.healthSlider.gameObject.GetComponent<LayoutElement>();
        hpSliderLayout.preferredWidth = 180f;
        hpSliderLayout.flexibleWidth = 0f;

        manager.waveText = CreateText("WaveText", lowerRow, "Wave 1", 21, font, TextAlignmentOptions.Center);
        manager.waveText.color = TextLight;
        LayoutElement waveLayout = manager.waveText.gameObject.AddComponent<LayoutElement>();
        waveLayout.preferredWidth = 115f;
        waveLayout.flexibleWidth = 0f;

        manager.timerSlider = CreateSlider("TimerSlider", lowerRow, new Color(1f, 0.78f, 0.18f, 1f), 15f);
        LayoutElement timerSliderLayout = manager.timerSlider.gameObject.GetComponent<LayoutElement>();
        timerSliderLayout.preferredWidth = 260f;
        timerSliderLayout.flexibleWidth = 0f;
        manager.timerLabel = null;

        manager.speedMultiplierText = CreateText("SpeedMultiplierText", lowerRow, "1X", 21, font, TextAlignmentOptions.Center);
        manager.speedMultiplierText.color = new Color(0.2f, 1f, 0.95f, 1f);
        LayoutElement speedLayout = manager.speedMultiplierText.gameObject.AddComponent<LayoutElement>();
        speedLayout.preferredWidth = 95f;
        speedLayout.flexibleWidth = 0f;
    }

    private static void BuildQuestionArea(Transform parent, OddClawCatchManager manager, TMP_FontAsset font)
    {
        RectTransform questionBg = CreateImage("QuestionCard", parent, new Color(0.98f, 0.99f, 1f, 0.96f));
        Anchor(questionBg, new Vector2(0.07f, 1f), new Vector2(0.93f, 1f), new Vector2(0.5f, 1f));
        questionBg.sizeDelta = new Vector2(0f, 92f);
        questionBg.anchoredPosition = new Vector2(0f, -112f);

        Outline outline = questionBg.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
        outline.effectDistance = new Vector2(0f, -3f);

        manager.questionText = CreateText("QuestionText", questionBg, "Catch the correct answer", 36, font, TextAlignmentOptions.Center);
        Stretch(manager.questionText.rectTransform, 18f, 18f, 8f, 8f);
        manager.questionText.color = TextDark;
        manager.questionText.enableWordWrapping = true;
    }

    private static void BuildClaw(Transform parent, OddClawCatchManager manager, OddClawAudioManager audioManager)
    {
        GameObject pivotObject = new GameObject("ClawPivot", typeof(RectTransform));
        pivotObject.transform.SetParent(parent, false);
        RectTransform pivot = pivotObject.GetComponent<RectTransform>();
        Anchor(pivot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        pivot.anchoredPosition = new Vector2(0f, -245f);
        pivot.sizeDelta = new Vector2(1f, 1f);

        RectTransform guide = CreateImage("EasyAimGuideLine", pivot, new Color(0.2f, 1f, 0.95f, 0.25f));
        Anchor(guide, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        guide.sizeDelta = new Vector2(7f, 720f);
        guide.anchoredPosition = new Vector2(0f, -360f);

        RectTransform arm = CreateImage("ClawArm", pivot, new Color(0.76f, 0.84f, 0.95f, 1f));
        Anchor(arm, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        arm.pivot = new Vector2(0.5f, 1f);
        arm.sizeDelta = new Vector2(22f, 215f);
        arm.anchoredPosition = Vector2.zero;

        RectTransform head = CreateImage("ClawHeadGrabber", pivot, new Color(1f, 1f, 1f, 1f));
        Anchor(head, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f));
        head.sizeDelta = new Vector2(120f, 82f);
        head.anchoredPosition = new Vector2(0f, -215f);
        Image headImage = head.GetComponent<Image>();
        headImage.sprite = CreateOrLoadClawSprite("OddClaw_NormalClaw.png", new Color(0.98f, 0.7f, 0.18f, 1f), new Color(0.6f, 0.28f, 0.04f, 1f), false);
        headImage.preserveAspect = true;

        RectTransform grabSocket = new GameObject("GrabSocket", typeof(RectTransform)).GetComponent<RectTransform>();
        grabSocket.SetParent(head, false);
        Anchor(grabSocket, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        grabSocket.sizeDelta = new Vector2(24f, 24f);
        grabSocket.anchoredPosition = new Vector2(0f, -34f);

        OddClawController claw = pivotObject.AddComponent<OddClawController>();
        claw.clawPivot = pivot;
        claw.clawArm = arm;
        claw.clawHead = head;
        claw.clawHeadImage = headImage;
        claw.grabSocket = grabSocket;
        claw.normalClawSprite = headImage.sprite;
        claw.grabbingClawSprite = CreateOrLoadClawSprite("OddClaw_GrabbingClaw.png", new Color(1f, 0.52f, 0.16f, 1f), new Color(0.5f, 0.18f, 0.04f, 1f), true);
        claw.easyAimGuideLine = guide;
        claw.audioManager = audioManager;
        claw.minRotationAngle = -55f;
        claw.maxRotationAngle = 55f;
        claw.rotationSpeed = 70f;
        claw.speedIncreasePerWave = 4f;
        claw.maxRotationSpeed = 160f;
        claw.useSceneRectTransformValues = true;
        claw.useGrabberYAsIdleLength = true;
        claw.overrideArmIdleSize = false;
        claw.overrideHeadIdlePosition = false;
        claw.animateArmSizeDuringExtension = true;
        claw.animateHeadPositionDuringExtension = true;
        claw.globalGrabbedItemOffset = Vector2.zero;
        claw.globalGrabbedItemRotation = Vector3.zero;
        claw.globalGrabbedItemScale = 1f;
        claw.usePerItemGrabOffset = true;
        claw.extensionLength = 660f;
        claw.extensionDuration = 0.68f;
        claw.retractDuration = 0.62f;
        claw.catchRadius = 62f;
        claw.holdBeforeGrabDelay = 0.24f;
        claw.clawCloseDuration = 0.16f;
        claw.clawCloseScale = 1f;
        claw.holdAfterGrabDelay = 0.28f;
        claw.evaluateAfterRetractDelay = 0.2f;
        claw.easyModeAimGuide = true;
        claw.useSpriteSwap = true;

        manager.clawController = claw;
    }

    private static void BuildGroundItems(Transform parent, OddClawCatchManager manager, TMP_FontAsset font)
    {
        RectTransform ground = CreateImage("GroundItemArea", parent, GroundColor);
        Anchor(ground, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
        ground.sizeDelta = new Vector2(0f, 250f);
        ground.anchoredPosition = Vector2.zero;

        RectTransform groundLine = CreateImage("GroundHighlight", ground, new Color(0.35f, 0.24f, 0.12f, 1f));
        Anchor(groundLine, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
        groundLine.sizeDelta = new Vector2(0f, 12f);
        groundLine.anchoredPosition = Vector2.zero;

        HorizontalLayoutGroup layout = ground.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(45, 45, 36, 26);
        layout.spacing = 34f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.enabled = false;

        manager.itemContainer = ground;
        manager.itemSpacing = 34f;

        manager.textItemTemplate = CreateAnswerTemplate("TextItemTemplate", ground, true, font);
        manager.imageItemTemplate = CreateAnswerTemplate("ImageItemTemplate", ground, false, font);
        manager.textItemTemplate.gameObject.SetActive(false);
        manager.imageItemTemplate.gameObject.SetActive(false);
    }

    private static OddClawItemView CreateAnswerTemplate(string name, Transform parent, bool textMode, TMP_FontAsset font)
    {
        RectTransform root = CreateImage(name, parent, new Color(0.98f, 0.94f, 0.78f, 1f));
        root.sizeDelta = new Vector2(155f, 128f);
        LayoutElement layoutElement = root.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 155f;
        layoutElement.preferredHeight = 128f;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
        layoutElement.ignoreLayout = true;

        Outline outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.11f, 0.02f, 0.35f);
        outline.effectDistance = new Vector2(0f, -4f);

        CanvasGroup canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        OddClawItemView view = root.gameObject.AddComponent<OddClawItemView>();
        view.root = root;
        view.catchZone = root;
        view.backgroundImage = root.GetComponent<Image>();
        view.canvasGroup = canvasGroup;
        view.normalColor = new Color(0.98f, 0.94f, 0.78f, 1f);
        view.caughtColor = new Color(1f, 0.82f, 0.24f, 1f);
        view.correctColor = new Color(0.3f, 0.95f, 0.48f, 1f);
        view.wrongColor = new Color(1f, 0.34f, 0.32f, 1f);

        if (textMode)
        {
            view.grabbedLocalOffset = new Vector2(0f, -50f);
            view.grabbedLocalRotation = Vector3.zero;
            view.grabbedLocalScale = 0.76f;

            TMP_Text label = CreateText("AnswerText", root, "123", 42, font, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 12f, 12f, 12f, 12f);
            label.color = TextDark;
            label.enableWordWrapping = true;
            view.answerText = label;
        }
        else
        {
            view.grabbedLocalOffset = new Vector2(0f, -32f);
            view.grabbedLocalRotation = Vector3.zero;
            view.grabbedLocalScale = 0.88f;

            RectTransform icon = CreateImage("AnswerImage", root, new Color(1f, 1f, 1f, 0f));
            Stretch(icon, 18f, 18f, 18f, 18f);
            Image image = icon.GetComponent<Image>();
            image.preserveAspect = true;
            view.answerImage = image;

            TMP_Text fallback = CreateText("FallbackText", root, "IMG", 26, font, TextAlignmentOptions.Center);
            Stretch(fallback.rectTransform, 8f, 8f, 8f, 8f);
            fallback.color = TextDark;
            view.answerText = fallback;
        }

        return view;
    }

    private static void BuildFirstPickHint(Transform parent, OddClawCatchManager manager, TMP_FontAsset font)
    {
        RectTransform hintRoot = CreateImage("FirstPickHintOverlay", parent, new Color(0f, 0f, 0f, 0f));
        Stretch(hintRoot);
        CanvasGroup group = hintRoot.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;

        RectTransform bubble = CreateImage("HintBubble", hintRoot, new Color(0.03f, 0.08f, 0.16f, 0.88f));
        Anchor(bubble, new Vector2(0.12f, 0.5f), new Vector2(0.88f, 0.5f), new Vector2(0.5f, 0.5f));
        bubble.sizeDelta = new Vector2(0f, 110f);
        bubble.anchoredPosition = new Vector2(0f, 150f);

        Outline outline = bubble.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.95f, 1f, 0.55f);
        outline.effectDistance = new Vector2(0f, -3f);

        TMP_Text label = CreateText("HintText", bubble, manager.firstPickHintMessage, 36, font, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, 22f, 22f, 8f, 8f);
        label.color = TextLight;
        label.enableWordWrapping = true;

        hintRoot.gameObject.SetActive(false);
        manager.firstPickHintOverlay = group;
        manager.firstPickHintText = label;
    }

    private static void BuildFeedback(Transform parent, OddClawCatchManager manager, TMP_FontAsset font)
    {
        RectTransform popup = CreateImage("FeedbackPopup", parent, new Color(0f, 0f, 0f, 0f));
        Anchor(popup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        popup.sizeDelta = new Vector2(520f, 130f);
        popup.anchoredPosition = new Vector2(0f, 250f);

        CanvasGroup group = popup.gameObject.AddComponent<CanvasGroup>();
        TMP_Text text = CreateText("FeedbackText", popup, "Correct!", 64, font, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.color = TextLight;

        OddClawFeedbackPopup feedback = popup.gameObject.AddComponent<OddClawFeedbackPopup>();
        feedback.root = popup;
        feedback.canvasGroup = group;
        feedback.messageText = text;
        feedback.showDuration = 0.18f;
        feedback.holdDuration = 0.62f;
        feedback.hideDuration = 0.18f;
        feedback.punchScale = 0.15f;
        manager.feedbackPopup = feedback;
    }

    private static void BuildPanels(Transform parent, OddClawCatchManager manager, TMP_FontAsset font)
    {
        manager.loadingPanel = CreateLoadingPanel(parent, manager, font);

        GameObject howTo = CreatePanel("HowToPlayPanel", parent, font, "How To Play", string.Empty, out RectTransform howBody, out RectTransform howFooter, out _);
        manager.howToPlayPanel = howTo;
        manager.howToPlayGuideImage = CreatePanelImage("GuideImage", howBody);
        manager.howToPlayFallbackText = CreateText("FallbackInstructions", howBody, manager.howToPlayFallbackInstructions, 31, font, TextAlignmentOptions.Center);
        manager.howToPlayFallbackText.color = TextDark;
        manager.howToPlayFallbackText.enableWordWrapping = true;
        manager.howToPlayStepCounterText = CreateText("StepCounterText", howBody, "Guide", 24, font, TextAlignmentOptions.Center);
        manager.howToPlayStepCounterText.color = new Color(0.2f, 0.25f, 0.36f, 1f);
        manager.howToPlayPrevButton = CreateButton("PREV", howFooter, font, ButtonColor);
        manager.howToPlayNextButton = CreateButton("NEXT", howFooter, font, ButtonColor);
        manager.howToPlayStartButton = CreateButton("START", howFooter, font, ButtonAltColor);

        GameObject pause = CreatePanel("PausePanel", parent, font, "Paused", "Take a short break.", out _, out RectTransform pauseFooter, out _);
        manager.pausePanel = pause;
        manager.resumeButton = CreateButton("RESUME", pauseFooter, font, ButtonAltColor);
        manager.restartButton = CreateButton("RESTART", pauseFooter, font, ButtonColor);
        manager.homeButton = null;

        GameObject result = CreatePanel("ResultPanel", parent, font, "Game Over", string.Empty, out RectTransform resultBody, out RectTransform resultFooter, out TMP_Text resultHeader);
        manager.resultPanel = result;
        manager.resultTitleText = resultHeader;
        manager.resultBodyText = CreateText("ResultBodyText", resultBody, "Score: 0", 34, font, TextAlignmentOptions.Center);
        manager.resultBodyText.color = TextDark;
        manager.resultBodyText.enableWordWrapping = true;
        manager.resultContinueButton = CreateButton("CONTINUE", resultFooter, font, ButtonAltColor);
        manager.resultPlayAgainButton = CreateButton("PLAY AGAIN", resultFooter, font, ButtonColor);
        manager.resultHomeButton = null;

        manager.loadingPanel.SetActive(false);
        manager.howToPlayPanel.SetActive(false);
        manager.pausePanel.SetActive(false);
        manager.resultPanel.SetActive(false);
    }

    private static GameObject CreateLoadingPanel(Transform parent, OddClawCatchManager manager, TMP_FontAsset font)
    {
        GameObject wrapper = new GameObject("LoadingPanel", typeof(RectTransform));
        wrapper.transform.SetParent(parent, false);
        RectTransform wrapperRect = wrapper.GetComponent<RectTransform>();
        Stretch(wrapperRect);

        GameObject panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
        panelRoot.transform.SetParent(wrapper.transform, false);
        RectTransform root = panelRoot.GetComponent<RectTransform>();
        Stretch(root);

        RectTransform dim = CreateImage("OverlayDim", root, new Color(0.03f, 0.06f, 0.14f, 1f));
        Stretch(dim);

        RectTransform content = new GameObject("LoadingContent", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(root, false);
        Anchor(content, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0.5f, 0.5f));
        content.sizeDelta = new Vector2(0f, 300f);
        content.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 36f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = CreateText("GameNameText", content, manager.gameTitle, 72, font, TextAlignmentOptions.Center);
        title.color = TextLight;
        title.enableWordWrapping = true;
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 150f;

        Slider slider = CreateSlider("LoadingBar", content, new Color(0.08f, 0.85f, 1f, 1f), 34f);
        LayoutElement sliderLayout = slider.gameObject.GetComponent<LayoutElement>();
        sliderLayout.preferredWidth = 620f;
        slider.value = 0f;

        manager.loadingTitleText = title;
        manager.loadingSlider = slider;
        return wrapper;
    }

    private static GameObject CreatePanel(string name, Transform parent, TMP_FontAsset font, string title, string bodyText, out RectTransform body, out RectTransform footer, out TMP_Text headerText)
    {
        GameObject wrapper = new GameObject(name, typeof(RectTransform));
        wrapper.transform.SetParent(parent, false);
        RectTransform wrapperRect = wrapper.GetComponent<RectTransform>();
        Stretch(wrapperRect);

        GameObject panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
        panelRoot.transform.SetParent(wrapper.transform, false);
        RectTransform root = panelRoot.GetComponent<RectTransform>();
        Stretch(root);

        RectTransform dim = CreateImage("OverlayDim", root, new Color(0f, 0f, 0f, 0.66f));
        Stretch(dim);

        RectTransform card = CreateImage("PanelCard", root, CardColor);
        Anchor(card, new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.5f), new Vector2(0.5f, 0.5f));
        card.sizeDelta = new Vector2(0f, 620f);
        card.anchoredPosition = Vector2.zero;

        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(0f, -5f);

        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(34, 34, 34, 34);
        cardLayout.spacing = 22f;
        cardLayout.childAlignment = TextAnchor.MiddleCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        RectTransform header = CreateLayoutPanel("Header", card, 0f);
        LayoutElement headerLayout = header.gameObject.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 82f;
        headerText = CreateText("HeaderText", header, title, 46, font, TextAlignmentOptions.Center);
        Stretch(headerText.rectTransform);
        headerText.color = TextDark;

        body = CreateLayoutPanel("Body", card, 0f);
        LayoutElement bodyLayout = body.gameObject.AddComponent<LayoutElement>();
        bodyLayout.preferredHeight = 350f;
        bodyLayout.flexibleHeight = 1f;
        VerticalLayoutGroup bodyGroup = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyGroup.spacing = 14f;
        bodyGroup.childAlignment = TextAnchor.MiddleCenter;
        bodyGroup.childControlWidth = true;
        bodyGroup.childControlHeight = true;
        bodyGroup.childForceExpandWidth = true;
        bodyGroup.childForceExpandHeight = true;

        if (!string.IsNullOrEmpty(bodyText))
        {
            TMP_Text bodyLabel = CreateText("BodyText", body, bodyText, 32, font, TextAlignmentOptions.Center);
            bodyLabel.color = TextDark;
            bodyLabel.enableWordWrapping = true;
        }

        footer = CreateLayoutPanel("Footer", card, 0f);
        LayoutElement footerLayout = footer.gameObject.AddComponent<LayoutElement>();
        footerLayout.preferredHeight = 92f;
        HorizontalLayoutGroup footerGroup = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerGroup.spacing = 16f;
        footerGroup.childAlignment = TextAnchor.MiddleCenter;
        footerGroup.childControlWidth = true;
        footerGroup.childControlHeight = true;
        footerGroup.childForceExpandWidth = true;
        footerGroup.childForceExpandHeight = true;

        return wrapper;
    }

    private static Image CreatePanelImage(string name, Transform parent)
    {
        RectTransform imageRect = CreateImage(name, parent, new Color(0.86f, 0.91f, 1f, 1f));
        LayoutElement layout = imageRect.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 230f;
        Image image = imageRect.GetComponent<Image>();
        image.preserveAspect = true;
        return image;
    }

    private static Button CreateButton(string label, Transform parent, TMP_FontAsset font, Color color)
    {
        RectTransform rect = CreateImage(label + "Button", parent, color);
        rect.sizeDelta = new Vector2(220f, 68f);
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 220f;
        layout.preferredHeight = 68f;

        Image buttonImage = rect.GetComponent<Image>();
        buttonImage.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        TMP_Text text = CreateText("Text", rect, label, 26, font, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.color = Color.white;

        return button;
    }

    private static Button CreateIconButton(string name, Transform parent, float size)
    {
        RectTransform rect = CreateImage(name, parent, ButtonColor);
        rect.sizeDelta = new Vector2(size, size);
        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = size;
        layout.preferredHeight = size;
        layout.flexibleWidth = 0f;

        Image buttonImage = rect.GetComponent<Image>();
        buttonImage.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        RectTransform barA = CreateImage("IconBarA", rect, Color.white);
        Anchor(barA, new Vector2(0.38f, 0.5f), new Vector2(0.38f, 0.5f), new Vector2(0.5f, 0.5f));
        barA.sizeDelta = new Vector2(8f, 28f);
        RectTransform barB = CreateImage("IconBarB", rect, Color.white);
        Anchor(barB, new Vector2(0.62f, 0.5f), new Vector2(0.62f, 0.5f), new Vector2(0.5f, 0.5f));
        barB.sizeDelta = new Vector2(8f, 28f);

        return button;
    }

    private static Slider CreateSlider(string name, Transform parent, Color fillColor, float height)
    {
        RectTransform root = CreateImage(name, parent, new Color(1f, 1f, 1f, 0.22f));
        root.sizeDelta = new Vector2(360f, height);
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        RectTransform fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(root, false);
        Stretch(fillArea, 3f, 3f, 3f, 3f);

        RectTransform fill = CreateImage("Fill", fillArea, fillColor);
        Stretch(fill);

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fill;
        slider.targetGraphic = root.GetComponent<Image>();

        return slider;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, int size, TMP_FontAsset font, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.font = font;
        text.enableWordWrapping = false;
        RectTransform rect = text.rectTransform;
        rect.sizeDelta = new Vector2(300f, 70f);
        return text;
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imageObject.GetComponent<RectTransform>();
    }

    private static RectTransform CreateLayoutPanel(string name, Transform parent, float flexibleWidth)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        LayoutElement layout = panel.AddComponent<LayoutElement>();
        layout.flexibleWidth = flexibleWidth;
        return panel.GetComponent<RectTransform>();
    }

    private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
    }

    private static void Stretch(RectTransform rect)
    {
        Stretch(rect, 0f, 0f, 0f, 0f);
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static OddClawQuestionGeneratorBase CreateOrLoadDefaultMathGenerator()
    {
        string folder = "Assets/OddClawCatch/Generated";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = folder + "/DefaultOddClawMathGenerator.asset";
        OddClawMathQuestionGenerator asset = AssetDatabase.LoadAssetAtPath<OddClawMathQuestionGenerator>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<OddClawMathQuestionGenerator>();
            asset.mode = OddClawMathMode.Mixed;
            asset.minimumOptions = 2;
            asset.maximumOptions = 6;
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        return asset;
    }

    private static Sprite CreateOrLoadClawSprite(string fileName, Color mainColor, Color darkColor, bool grabbing)
    {
        string folder = "Assets/OddClawCatch/Generated";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string path = folder + "/" + fileName;
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
            return existing;

        int width = 128;
        int height = 88;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, clear);
        }

        DrawRect(texture, 42, 50, 44, 22, mainColor);
        DrawRect(texture, 50, 38, 28, 18, mainColor);
        DrawRect(texture, 56, 26, 16, 16, darkColor);

        if (grabbing)
        {
            DrawRect(texture, 30, 18, 18, 42, darkColor);
            DrawRect(texture, 80, 18, 18, 42, darkColor);
            DrawRect(texture, 42, 12, 44, 16, darkColor);
        }
        else
        {
            DrawRect(texture, 22, 12, 18, 44, darkColor);
            DrawRect(texture, 88, 12, 18, 44, darkColor);
            DrawRect(texture, 16, 8, 18, 14, darkColor);
            DrawRect(texture, 94, 8, 18, 14, darkColor);
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void DrawRect(Texture2D texture, int startX, int startY, int width, int height, Color color)
    {
        int maxX = Mathf.Min(texture.width, startX + width);
        int maxY = Mathf.Min(texture.height, startY + height);
        for (int y = Mathf.Max(0, startY); y < maxY; y++)
        {
            for (int x = Mathf.Max(0, startX); x < maxX; x++)
                texture.SetPixel(x, y, color);
        }
    }
}
#endif
