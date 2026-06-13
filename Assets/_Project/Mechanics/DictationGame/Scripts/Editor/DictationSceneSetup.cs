#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DictationGame;

public static class DictationSceneSetup
{
    private static readonly Color BgColor = Hex("FCF7FA");
    private static readonly Color TopBarColor = Hex("F6E6EF");
    private static readonly Color SurfaceColor = Hex("FFFDFE");
    private static readonly Color SoftSurfaceColor = Hex("F9EFF5");
    private static readonly Color KeyboardSurfaceColor = Hex("F5E5EE");
    private static readonly Color InputSurfaceColor = Hex("F8F0F5");
    private static readonly Color OverlayColor = new Color(0.35f, 0.24f, 0.31f, 0.32f);

    private static readonly Color TextPrimary = Hex("4D3C46");
    private static readonly Color TextSecondary = Hex("7D6673");
    private static readonly Color AccentPrimary = Hex("EDBDD5");
    private static readonly Color AccentPrimaryDark = Hex("B9809C");
    private static readonly Color AccentSecondary = Hex("D7C2E9");
    private static readonly Color AccentSoftBlue = Hex("D6E6F2");
    private static readonly Color AccentSoftCream = Hex("FFF4D8");
    private static readonly Color PositiveColor = Hex("A8D5BA");
    private static readonly Color DangerColor = Hex("E6A7A0");
    private static readonly Color UtilityMauve = Hex("CC9FB7");

    [MenuItem("Tools/Dictation Game/Create Full Scene", false, 1)]
    public static void CreateDictationScene()
    {
        CreateEventSystemIfNeeded();

        Canvas canvas = CreateCanvas("DictationCanvas");
        CanvasGroup canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        DictationThemeFonts themeFonts = canvas.gameObject.AddComponent<DictationThemeFonts>();

        CreatePanel(canvas.transform, "Background", BgColor, Vector2.zero, Vector2.one);
        CreateDecorBlob(canvas.transform, "Decor_BlobTopLeft", AccentPrimary, new Vector2(0.10f, 0.90f), new Vector2(320f, 220f), 0.18f);
        CreateDecorBlob(canvas.transform, "Decor_BlobBottomRight", AccentSecondary, new Vector2(0.92f, 0.10f), new Vector2(360f, 240f), 0.14f);

        GameObject topBar = CreateRoundedCard(canvas.transform, "TopBar", TopBarColor, new Vector2(0.035f, 0.87f), new Vector2(0.965f, 0.975f));
        HorizontalLayoutGroup topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(28, 28, 10, 10);
        topLayout.spacing = 14;
        topLayout.childAlignment = TextAnchor.MiddleLeft;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = true;

        TextMeshProUGUI roundTitle = CreateLabel(topBar.transform, "RoundTitleText", "Warm Up Sentence", 23, TextAlignmentOptions.Left, TextPrimary);
        SetLayout(roundTitle.gameObject, flexW: 1f);

        TextMeshProUGUI progressText = CreateBadge(topBar.transform, "RoundProgressText", "Q 1 / 10", 18, AccentSoftBlue, TextPrimary, 122f, 46f);
        TextMeshProUGUI difficultyBadge = CreateBadge(topBar.transform, "DifficultyBadgeText", "EASY", 18, AccentSoftCream, TextPrimary, 94f, 46f);
        TextMeshProUGUI scoreText = CreateBadge(topBar.transform, "ScoreText", "Score: 100", 18, SurfaceColor, TextPrimary, 152f, 46f);
        Button pauseButton = CreateButton(topBar.transform, "PauseButton", "Pause", AccentPrimary, TextPrimary, 17, 48f, 112f);

        GameObject contentArea = CreateRect(canvas.transform, "ContentArea", new Vector2(0.035f, 0.33f), new Vector2(0.965f, 0.845f));
        HorizontalLayoutGroup contentLayout = contentArea.AddComponent<HorizontalLayoutGroup>();
        contentLayout.spacing = 16;
        contentLayout.padding = new RectOffset(0, 0, 0, 0);
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = true;

        GameObject leftPanel = CreateRoundedCard(contentArea.transform, "LeftPanel_Audio", SurfaceColor, Vector2.zero, Vector2.one);
        SetLayout(leftPanel, minW: 280f, flexW: 0.31f);
        VerticalLayoutGroup leftLayout = leftPanel.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(22, 22, 22, 22);
        leftLayout.spacing = 16;
        leftLayout.childAlignment = TextAnchor.UpperCenter;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        GameObject audioTopRow = new GameObject("AudioTopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        audioTopRow.transform.SetParent(leftPanel.transform, false);
        SetLayout(audioTopRow, prefH: 38f);
        HorizontalLayoutGroup audioTopLayout = audioTopRow.GetComponent<HorizontalLayoutGroup>();
        audioTopLayout.spacing = 8;
        audioTopLayout.childAlignment = TextAnchor.MiddleCenter;
        audioTopLayout.childForceExpandWidth = false;
        audioTopLayout.childForceExpandHeight = true;

        TextMeshProUGUI audioMiniTitle = CreateLabel(audioTopRow.transform, "AudioMiniTitleText", "Audio", 17, TextAlignmentOptions.Left, TextPrimary);
        SetLayout(audioMiniTitle.gameObject, flexW: 1f);

        Image[] replayIcons = CreateReplayIcons(audioTopRow.transform);

        TextMeshProUGUI audioHeading = CreateLabel(leftPanel.transform, "AudioPanelTitleText", "Listen Carefully", 22, TextAlignmentOptions.Center, TextPrimary);
        SetLayout(audioHeading.gameObject, prefH: 42f);

        GameObject audioControlCard = CreateRoundedCard(leftPanel.transform, "AudioControlCard", SoftSurfaceColor, Vector2.zero, Vector2.one);
        SetLayout(audioControlCard, prefH: 164f);
        HorizontalLayoutGroup audioControlLayout = audioControlCard.AddComponent<HorizontalLayoutGroup>();
        audioControlLayout.padding = new RectOffset(16, 16, 16, 16);
        audioControlLayout.spacing = 16;
        audioControlLayout.childAlignment = TextAnchor.MiddleCenter;
        audioControlLayout.childForceExpandWidth = false;
        audioControlLayout.childForceExpandHeight = true;

        Image playButtonIcon;
        Button playButton = CreateIconButton(audioControlCard.transform, "PlayAudioButton", AccentPrimaryDark, Color.white, 78f, out playButtonIcon);

        GameObject visualizerRoot = CreateRoundedCard(audioControlCard.transform, "AudioVisualizer", SurfaceColor, Vector2.zero, Vector2.one);
        SetLayout(visualizerRoot, prefH: 126f, flexW: 1f);
        VerticalLayoutGroup visualizerLayout = visualizerRoot.AddComponent<VerticalLayoutGroup>();
        visualizerLayout.padding = new RectOffset(14, 14, 10, 10);
        visualizerLayout.spacing = 8;
        visualizerLayout.childAlignment = TextAnchor.MiddleCenter;
        visualizerLayout.childForceExpandWidth = true;
        visualizerLayout.childForceExpandHeight = false;

        TextMeshProUGUI listeningLabel = CreateLabel(visualizerRoot.transform, "ListeningLabel", "Ready to listen", 15, TextAlignmentOptions.Center, TextSecondary);
        SetLayout(listeningLabel.gameObject, prefH: 26f);

        GameObject barsRoot = new GameObject("WaveformBars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        barsRoot.transform.SetParent(visualizerRoot.transform, false);
        SetLayout(barsRoot, prefH: 74f);
        HorizontalLayoutGroup barsLayout = barsRoot.GetComponent<HorizontalLayoutGroup>();
        barsLayout.spacing = 5;
        barsLayout.padding = new RectOffset(6, 6, 0, 0);
        barsLayout.childAlignment = TextAnchor.MiddleCenter;
        barsLayout.childForceExpandWidth = false;
        barsLayout.childForceExpandHeight = false;
        RectTransform[] waveformBars = CreateWaveformBars(barsRoot.transform, 20);

        TextMeshProUGUI audioHintText = CreateLabel(leftPanel.transform, "AudioHelperText", "Replay only when needed. Replays reduce score.", 15, TextAlignmentOptions.Center, TextSecondary);
        SetLayout(audioHintText.gameObject, prefH: 44f);

        GameObject rightPanel = CreateRoundedCard(contentArea.transform, "RightPanel_Answer", SurfaceColor, Vector2.zero, Vector2.one);
        SetLayout(rightPanel, flexW: 0.69f);
        VerticalLayoutGroup rightLayout = rightPanel.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(24, 24, 22, 22);
        rightLayout.spacing = 12;
        rightLayout.childAlignment = TextAnchor.UpperLeft;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        TextMeshProUGUI answerHeading = CreateLabel(rightPanel.transform, "AnswerPanelTitleText", "Type What You Heard", 20, TextAlignmentOptions.Left, TextPrimary);
        SetLayout(answerHeading.gameObject, prefH: 36f);

        GameObject hintCard = CreateRoundedCard(rightPanel.transform, "MainHintSection", SoftSurfaceColor, Vector2.zero, Vector2.one);
        SetLayout(hintCard, prefH: 126f);
        HorizontalLayoutGroup hintRowLayout = hintCard.AddComponent<HorizontalLayoutGroup>();
        hintRowLayout.padding = new RectOffset(16, 16, 14, 14);
        hintRowLayout.spacing = 14;
        hintRowLayout.childAlignment = TextAnchor.MiddleCenter;
        hintRowLayout.childForceExpandWidth = false;
        hintRowLayout.childForceExpandHeight = true;

        GameObject hintIconHolder = CreateRoundedCard(hintCard.transform, "HintIconHolder", AccentPrimary, Vector2.zero, Vector2.one);
        SetLayout(hintIconHolder, prefW: 76f, prefH: 76f);
        Image hintIconImage = CreatePlainImage(hintIconHolder.transform, "HintIconImage", Color.white, new Vector2(0.22f, 0.22f), new Vector2(0.78f, 0.78f));
        hintIconImage.raycastTarget = false;

        GameObject hintTextArea = CreateRect(hintCard.transform, "HintTextArea", Vector2.zero, Vector2.one);
        SetLayout(hintTextArea, flexW: 1f);
        VerticalLayoutGroup hintTextLayout = hintTextArea.AddComponent<VerticalLayoutGroup>();
        hintTextLayout.spacing = 4;
        hintTextLayout.childAlignment = TextAnchor.MiddleLeft;
        hintTextLayout.childForceExpandWidth = true;
        hintTextLayout.childForceExpandHeight = false;

        TextMeshProUGUI hintTitle = CreateLabel(hintTextArea.transform, "HintTitleText", "Helpful Hints", 16, TextAlignmentOptions.Left, TextPrimary);
        SetLayout(hintTitle.gameObject, prefH: 24f);
        TextMeshProUGUI hintDisplay = CreateLabel(hintTextArea.transform, "HintDisplayText", "Use hints only if you need help.", 16, TextAlignmentOptions.TopLeft, TextSecondary);
        SetLayout(hintDisplay.gameObject, prefH: 58f);

        Button hintButton = CreateButton(hintCard.transform, "HintButton", "Use Hint 1 (-5 pts)", UtilityMauve, Color.white, 16, 48f, 190f);

        TMP_InputField inputField = CreateInputField(rightPanel.transform, "AnswerInputField", "Type what you heard...", 19);
        Button submitButton = CreateButton(rightPanel.transform, "SubmitButton", "Submit Answer", AccentPrimaryDark, Color.white, 19, 54f, 220f);

        TextMeshProUGUI inlineFeedback = CreateLabel(rightPanel.transform, "InlineFeedbackText", "", 15, TextAlignmentOptions.Center, DangerColor);
        SetLayout(inlineFeedback.gameObject, prefH: 28f);
        inlineFeedback.gameObject.SetActive(false);

        KeyboardRefs keyboardRefs = CreateKeyboard(canvas.transform);

        PanelRefs howToPlay = CreateHowToPlayPanel(canvas.transform);
        PanelRefs pause = CreatePausePanel(canvas.transform);
        ResultRefs result = CreateResultPanel(canvas.transform);
        SummaryRefs summary = CreateSummaryPanel(canvas.transform);

        GameObject particlesGO = new GameObject("CorrectParticles", typeof(ParticleSystem));
        particlesGO.transform.SetParent(canvas.transform, false);
        ConfigureParticles(particlesGO.GetComponent<ParticleSystem>());

        GameObject gameManagerGO = new GameObject("DictationGameManager", typeof(DictationGameManager));
        GameObject audioManagerGO = new GameObject("DictationAudioManager", typeof(DictationAudioManager), typeof(AudioSource));
        GameObject hintSystemGO = new GameObject("DictationHintSystem", typeof(DictationHintSystem));

        DictationGameManager gameManager = gameManagerGO.GetComponent<DictationGameManager>();
        DictationAudioManager audioManager = audioManagerGO.GetComponent<DictationAudioManager>();
        DictationHintSystem hintSystem = hintSystemGO.GetComponent<DictationHintSystem>();
        DictationKeyboard keyboard = keyboardRefs.Keyboard;

        WireGameManager(gameManager, audioManager, hintSystem, keyboard, canvasGroup,
            roundTitle, progressText, difficultyBadge, scoreText, pauseButton,
            inputField, submitButton, inlineFeedback,
            howToPlay.Panel, howToPlay.Body, howToPlay.PrimaryButton,
            pause.Panel, pause.PrimaryButton, pause.SecondaryButton,
            result.Panel, result.Title, result.Detail, result.Body, result.PrimaryButton, result.SecondaryButton,
            summary.Panel, summary.Title, summary.Detail, summary.Body, summary.PrimaryButton, summary.SecondaryButton,
            particlesGO.GetComponent<ParticleSystem>());

        WireAudioManager(audioManager, playButton, playButtonIcon, replayIcons, visualizerRoot, listeningLabel, waveformBars);
        WireHintSystem(hintSystem, hintDisplay, hintButton);
        WireKeyboard(keyboard, inputField, keyboardRefs.KeyTemplate, keyboardRefs.SpaceTemplate, keyboardRefs.BackspaceTemplate,
            keyboardRefs.Row1, keyboardRefs.Row2, keyboardRefs.Row3, keyboardRefs.Row4);
        WireThemeFonts(themeFonts, canvas.transform);

        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("[DictationGame] Final redesign scene created. Next: assign your QuestionSet on DictationGameManager, assign Primary/Secondary TMP fonts on DictationCanvas > DictationThemeFonts, then press Play.");
    }

    private struct KeyboardRefs
    {
        public DictationKeyboard Keyboard;
        public GameObject KeyTemplate;
        public GameObject SpaceTemplate;
        public GameObject BackspaceTemplate;
        public Transform Row1;
        public Transform Row2;
        public Transform Row3;
        public Transform Row4;
    }

    private struct PanelRefs
    {
        public GameObject Panel;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Body;
        public Button PrimaryButton;
        public Button SecondaryButton;
    }

    private struct ResultRefs
    {
        public GameObject Panel;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Detail;
        public TextMeshProUGUI Body;
        public Button PrimaryButton;
        public Button SecondaryButton;
    }

    private struct SummaryRefs
    {
        public GameObject Panel;
        public TextMeshProUGUI Title;
        public TextMeshProUGUI Detail;
        public TextMeshProUGUI Body;
        public Button PrimaryButton;
        public Button SecondaryButton;
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject canvasGO = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreateEventSystemIfNeeded()
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
        if (existing != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return go;
    }

    private static GameObject CreateRoundedCard(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = CreatePanel(parent, name, color, anchorMin, anchorMax);
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.46f, 0.33f, 0.40f, 0.12f);
        shadow.effectDistance = new Vector2(0f, -6f);
        return go;
    }

    private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private static Image CreatePlainImage(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void CreateDecorBlob(Transform parent, string name, Color color, Vector2 anchorPos, Vector2 size, float alpha)
    {
        GameObject blob = new GameObject(name, typeof(RectTransform), typeof(Image));
        blob.transform.SetParent(parent, false);
        RectTransform rt = blob.GetComponent<RectTransform>();
        rt.anchorMin = anchorPos;
        rt.anchorMax = anchorPos;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        Image image = blob.GetComponent<Image>();
        image.color = new Color(color.r, color.g, color.b, alpha);
        image.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.enableWordWrapping = true;
        return label;
    }

    private static TextMeshProUGUI CreateBadge(Transform parent, string name, string text, float size, Color bg, Color fg, float width, float height)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = bg;
        SetLayout(root, prefW: width, prefH: height);

        TextMeshProUGUI label = CreateLabel(root.transform, "Label", text, size, TextAlignmentOptions.Center, fg);
        Stretch(label.rectTransform);
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color, Color textColor, float fontSize, float height, float width)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        SetLayout(go, prefH: height, prefW: width);

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.32f, 0.22f, 0.29f, 0.10f);
        shadow.effectDistance = new Vector2(0f, -4f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        TextMeshProUGUI text = labelGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;

        return go.GetComponent<Button>();
    }

    private static Button CreateIconButton(Transform parent, string name, Color backgroundColor, Color iconColor, float size, out Image iconImage)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = backgroundColor;
        SetLayout(go, prefW: size, prefH: size);

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.32f, 0.22f, 0.29f, 0.12f);
        shadow.effectDistance = new Vector2(0f, -4f);

        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.28f, 0.24f);
        iconRT.anchorMax = new Vector2(0.78f, 0.76f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        iconImage = iconGO.GetComponent<Image>();
        iconImage.color = iconColor;
        iconImage.raycastTarget = false;

        GameObject fallbackLabelGO = new GameObject("FallbackIconText", typeof(RectTransform), typeof(TextMeshProUGUI));
        fallbackLabelGO.transform.SetParent(go.transform, false);
        Stretch(fallbackLabelGO.GetComponent<RectTransform>());
        TextMeshProUGUI fallbackLabel = fallbackLabelGO.GetComponent<TextMeshProUGUI>();
        fallbackLabel.text = "▶";
        fallbackLabel.fontSize = size * 0.46f;
        fallbackLabel.alignment = TextAlignmentOptions.Center;
        fallbackLabel.color = iconColor;

        return go.GetComponent<Button>();
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, float fontSize)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = InputSurfaceColor;
        SetLayout(root, prefH: 60f);

        GameObject viewport = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(root.transform, false);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(16, 8);
        viewportRT.offsetMax = new Vector2(-16, -8);

        TextMeshProUGUI placeholderText = CreateLabel(viewport.transform, "Placeholder", placeholder, fontSize, TextAlignmentOptions.MidlineLeft, new Color(TextSecondary.r, TextSecondary.g, TextSecondary.b, 0.65f));
        placeholderText.fontStyle = FontStyles.Italic;
        Stretch(placeholderText.GetComponent<RectTransform>());

        TextMeshProUGUI textComponent = CreateLabel(viewport.transform, "Text", string.Empty, fontSize, TextAlignmentOptions.MidlineLeft, TextPrimary);
        Stretch(textComponent.GetComponent<RectTransform>());

        TMP_InputField field = root.GetComponent<TMP_InputField>();
        field.textViewport = viewportRT;
        field.placeholder = placeholderText;
        field.textComponent = textComponent;
        field.readOnly = true;
        field.shouldHideMobileInput = true;
        return field;
    }

    private static Image[] CreateReplayIcons(Transform parent)
    {
        GameObject container = new GameObject("ReplayIconsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        container.transform.SetParent(parent, false);
        SetLayout(container, prefW: 86f, prefH: 36f);
        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Image[] icons = new Image[2];
        for (int i = 0; i < icons.Length; i++)
        {
            GameObject icon = new GameObject($"ReplayIcon_{i + 1}", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(container.transform, false);
            RectTransform rt = icon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(22, 22);
            icons[i] = icon.GetComponent<Image>();
            icons[i].color = AccentPrimaryDark;
        }
        return icons;
    }

    private static RectTransform[] CreateWaveformBars(Transform parent, int count)
    {
        RectTransform[] bars = new RectTransform[count];
        for (int i = 0; i < count; i++)
        {
            GameObject bar = new GameObject($"Bar_{i + 1:00}", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            Image image = bar.GetComponent<Image>();
            image.color = i % 3 == 0 ? UtilityMauve : AccentPrimaryDark;
            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(8, i % 2 == 0 ? 18 : 26);
            bars[i] = rt;
        }
        return bars;
    }

    private static KeyboardRefs CreateKeyboard(Transform parent)
    {
        GameObject keyboardRoot = CreateRect(parent, "KeyboardRoot", Vector2.zero, new Vector2(1f, 0.30f));
        GameObject keyboardBackground = CreateRoundedCard(keyboardRoot.transform, "KeyboardBackground", KeyboardSurfaceColor, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.92f));

        GameObject rowsRoot = CreateRect(keyboardBackground.transform, "KeyRows", Vector2.zero, Vector2.one);
        RectTransform rowsRT = rowsRoot.GetComponent<RectTransform>();
        rowsRT.offsetMin = new Vector2(26, 18);
        rowsRT.offsetMax = new Vector2(-26, -18);

        VerticalLayoutGroup rowsLayout = rowsRoot.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 8;
        rowsLayout.childAlignment = TextAnchor.MiddleCenter;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = true;

        Transform row1 = CreateKeyRow(rowsRoot.transform, "Row1_QWERTY");
        Transform row2 = CreateKeyRow(rowsRoot.transform, "Row2_ASDF");
        Transform row3 = CreateKeyRow(rowsRoot.transform, "Row3_ZXCV_BACK");
        Transform row4 = CreateKeyRow(rowsRoot.transform, "Row4_SPACE");

        GameObject templatesRoot = CreateRect(keyboardBackground.transform, "Templates_Disabled", Vector2.zero, Vector2.zero);
        templatesRoot.SetActive(false);

        GameObject keyTemplate = CreateKeyTemplate(templatesRoot.transform, "KeyTemplate", "A", SurfaceColor, TextPrimary, new Vector2(70, 54), 70f, 0f);
        GameObject spaceTemplate = CreateKeyTemplate(templatesRoot.transform, "SpaceKeyTemplate", "SPACE", AccentSecondary, TextPrimary, new Vector2(620, 56), 620f, 360f);
        GameObject backspaceTemplate = CreateKeyTemplate(templatesRoot.transform, "BackspaceKeyTemplate", "BACK", new Color(0.87f, 0.79f, 0.84f), TextPrimary, new Vector2(128, 54), 128f, 96f);

        DictationKeyboard keyboard = keyboardBackground.AddComponent<DictationKeyboard>();
        return new KeyboardRefs
        {
            Keyboard = keyboard,
            KeyTemplate = keyTemplate,
            SpaceTemplate = spaceTemplate,
            BackspaceTemplate = backspaceTemplate,
            Row1 = row1,
            Row2 = row2,
            Row3 = row3,
            Row4 = row4
        };
    }

    private static Transform CreateKeyRow(Transform parent, string name)
    {
        GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 7;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        row.GetComponent<LayoutElement>().flexibleHeight = 1f;
        return row.transform;
    }

    private static GameObject CreateKeyTemplate(Transform parent, string name, string label, Color color, Color textColor, Vector2 size, float preferredWidth, float minWidth)
    {
        GameObject key = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        key.transform.SetParent(parent, false);
        key.GetComponent<Image>().color = color;
        RectTransform rt = key.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        LayoutElement le = key.GetComponent<LayoutElement>();
        le.preferredWidth = preferredWidth;
        le.preferredHeight = size.y;
        le.minWidth = minWidth;

        Shadow shadow = key.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.32f, 0.22f, 0.29f, 0.08f);
        shadow.effectDistance = new Vector2(0f, -3f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(key.transform, false);
        Stretch(labelGO.GetComponent<RectTransform>());
        TextMeshProUGUI text = labelGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;

        ColorBlock colors = key.GetComponent<Button>().colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        colors.pressedColor = AccentPrimary;
        key.GetComponent<Button>().colors = colors;
        return key;
    }

    private static PanelRefs CreateHowToPlayPanel(Transform parent)
    {
        GameObject panel = CreateOverlayPanel(parent, "HowToPlayPanel");
        GameObject card = CreateCard(panel.transform, "HowToPlayCard", new Vector2(780, 520));
        VerticalLayoutGroup layout = AddCardLayout(card);
        layout.spacing = 18;

        TextMeshProUGUI title = CreateLabel(card.transform, "Title", "How To Play", 42, TextAlignmentOptions.Center, TextPrimary);
        SetLayout(title.gameObject, prefH: 64);
        TextMeshProUGUI body = CreateLabel(card.transform, "Body", "Listen and type what you heard.", 22, TextAlignmentOptions.Center, TextSecondary);
        SetLayout(body.gameObject, prefH: 250);
        Button primary = CreateButton(card.transform, "GotItButton", "Got it", AccentPrimaryDark, Color.white, 21, 60, 220);

        panel.SetActive(false);
        return new PanelRefs { Panel = panel, Title = title, Body = body, PrimaryButton = primary };
    }

    private static PanelRefs CreatePausePanel(Transform parent)
    {
        GameObject panel = CreateOverlayPanel(parent, "PausePanel");
        GameObject card = CreateCard(panel.transform, "PauseCard", new Vector2(560, 380));
        VerticalLayoutGroup layout = AddCardLayout(card);
        layout.spacing = 16;

        TextMeshProUGUI title = CreateLabel(card.transform, "Title", "Paused", 42, TextAlignmentOptions.Center, TextPrimary);
        SetLayout(title.gameObject, prefH: 68);
        TextMeshProUGUI body = CreateLabel(card.transform, "Body", "Take a small break and continue when ready.", 20, TextAlignmentOptions.Center, TextSecondary);
        SetLayout(body.gameObject, prefH: 92);
        Button resume = CreateButton(card.transform, "ResumeButton", "Resume", AccentPrimaryDark, Color.white, 20, 58, 220);
        Button quit = CreateButton(card.transform, "QuitButton", "Quit", new Color(0.87f, 0.63f, 0.62f), TextPrimary, 20, 58, 220);

        panel.SetActive(false);
        return new PanelRefs { Panel = panel, Title = title, Body = body, PrimaryButton = resume, SecondaryButton = quit };
    }

    private static ResultRefs CreateResultPanel(Transform parent)
    {
        GameObject panel = CreateOverlayPanel(parent, "ResultPanel");
        GameObject card = CreateCard(panel.transform, "ResultCard", new Vector2(740, 530));
        VerticalLayoutGroup layout = AddCardLayout(card);
        layout.spacing = 16;

        TextMeshProUGUI title = CreateLabel(card.transform, "ResultTitleText", "Perfect", 44, TextAlignmentOptions.Center, TextPrimary);
        SetLayout(title.gameObject, prefH: 68);
        TextMeshProUGUI detail = CreateLabel(card.transform, "ResultDetailText", "Score: 100", 24, TextAlignmentOptions.Center, TextSecondary);
        SetLayout(detail.gameObject, prefH: 54);
        TextMeshProUGUI answer = CreateLabel(card.transform, "CorrectAnswerText", string.Empty, 20, TextAlignmentOptions.Center, TextSecondary);
        SetLayout(answer.gameObject, prefH: 120);

        GameObject buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttons.transform.SetParent(card.transform, false);
        HorizontalLayoutGroup buttonLayout = buttons.GetComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 16;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childForceExpandWidth = false;
        SetLayout(buttons, prefH: 70f);

        Button playAgain = CreateButton(buttons.transform, "PlayAgainButton", "Play Again", AccentSecondary, TextPrimary, 20, 58, 210);
        Button cont = CreateButton(buttons.transform, "ContinueButton", "Continue", AccentPrimaryDark, Color.white, 20, 58, 210);

        panel.SetActive(false);
        return new ResultRefs { Panel = panel, Title = title, Detail = detail, Body = answer, PrimaryButton = playAgain, SecondaryButton = cont };
    }

    private static SummaryRefs CreateSummaryPanel(Transform parent)
    {
        GameObject panel = CreateOverlayPanel(parent, "SessionSummaryPanel");
        GameObject card = CreateCard(panel.transform, "SummaryCard", new Vector2(980, 720));
        VerticalLayoutGroup layout = AddCardLayout(card);
        layout.spacing = 14;

        TextMeshProUGUI title = CreateLabel(card.transform, "SummaryTitleText", "Session Complete", 42, TextAlignmentOptions.Center, TextPrimary);
        SetLayout(title.gameObject, prefH: 62);
        TextMeshProUGUI score = CreateLabel(card.transform, "SummaryScoreText", "Total Score: 0", 26, TextAlignmentOptions.Center, AccentPrimaryDark);
        SetLayout(score.gameObject, prefH: 46);
        TextMeshProUGUI breakdown = CreateLabel(card.transform, "SummaryBreakdownText", string.Empty, 17, TextAlignmentOptions.TopLeft, TextSecondary);
        SetLayout(breakdown.gameObject, prefH: 420);

        GameObject buttons = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        buttons.transform.SetParent(card.transform, false);
        HorizontalLayoutGroup buttonLayout = buttons.GetComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 16;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childForceExpandWidth = false;
        SetLayout(buttons, prefH: 70f);

        Button replay = CreateButton(buttons.transform, "ReplaySessionButton", "Replay Session", AccentSecondary, TextPrimary, 20, 58, 230);
        Button quit = CreateButton(buttons.transform, "SummaryQuitButton", "Quit", AccentPrimary, TextPrimary, 20, 58, 180);

        panel.SetActive(false);
        return new SummaryRefs { Panel = panel, Title = title, Detail = score, Body = breakdown, PrimaryButton = replay, SecondaryButton = quit };
    }

    private static GameObject CreateOverlayPanel(Transform parent, string name)
    {
        return CreatePanel(parent, name, OverlayColor, Vector2.zero, Vector2.one);
    }

    private static GameObject CreateCard(Transform parent, string name, Vector2 size)
    {
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Shadow));
        card.transform.SetParent(parent, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        card.GetComponent<Image>().color = SurfaceColor;
        Shadow shadow = card.GetComponent<Shadow>();
        shadow.effectColor = new Color(0.34f, 0.24f, 0.30f, 0.18f);
        shadow.effectDistance = new Vector2(0f, -8f);
        return card;
    }

    private static VerticalLayoutGroup AddCardLayout(GameObject card)
    {
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 30, 30);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static void ConfigureParticles(ParticleSystem ps)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(AccentPrimaryDark, UtilityMauve);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.maxParticles = 60;
        main.duration = 0.8f;
        main.loop = false;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45) });
    }

    private static void SetLayout(GameObject go, float prefW = -1, float prefH = -1, float flexW = -1, float flexH = -1, float minW = -1)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null) element = go.AddComponent<LayoutElement>();
        if (prefW >= 0f) element.preferredWidth = prefW;
        if (prefH >= 0f) element.preferredHeight = prefH;
        if (flexW >= 0f) element.flexibleWidth = flexW;
        if (flexH >= 0f) element.flexibleHeight = flexH;
        if (minW >= 0f) element.minWidth = minW;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void WireGameManager(DictationGameManager gm, DictationAudioManager audio, DictationHintSystem hints, DictationKeyboard keyboard, CanvasGroup canvasGroup,
        TextMeshProUGUI roundTitle, TextMeshProUGUI progress, TextMeshProUGUI difficulty, TextMeshProUGUI score, Button pauseButton,
        TMP_InputField input, Button submit, TextMeshProUGUI feedback,
        GameObject howPanel, TextMeshProUGUI howBody, Button gotIt,
        GameObject pausePanel, Button resume, Button quit,
        GameObject resultPanel, TextMeshProUGUI resultTitle, TextMeshProUGUI resultDetail, TextMeshProUGUI correctAnswer, Button playAgain, Button cont,
        GameObject summaryPanel, TextMeshProUGUI summaryTitle, TextMeshProUGUI summaryScore, TextMeshProUGUI summaryBreakdown, Button replaySession, Button summaryQuit,
        ParticleSystem particles)
    {
        SerializedObject so = new SerializedObject(gm);
        so.FindProperty("audioManager").objectReferenceValue = audio;
        so.FindProperty("hintSystem").objectReferenceValue = hints;
        so.FindProperty("keyboard").objectReferenceValue = keyboard;
        so.FindProperty("roundTitleText").objectReferenceValue = roundTitle;
        so.FindProperty("roundProgressText").objectReferenceValue = progress;
        so.FindProperty("difficultyBadgeText").objectReferenceValue = difficulty;
        so.FindProperty("scoreText").objectReferenceValue = score;
        so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
        so.FindProperty("answerInputField").objectReferenceValue = input;
        so.FindProperty("submitButton").objectReferenceValue = submit;
        so.FindProperty("inlineFeedbackText").objectReferenceValue = feedback;
        so.FindProperty("howToPlayPanel").objectReferenceValue = howPanel;
        so.FindProperty("howToPlayBodyText").objectReferenceValue = howBody;
        so.FindProperty("gotItButton").objectReferenceValue = gotIt;
        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.FindProperty("resumeButton").objectReferenceValue = resume;
        so.FindProperty("quitButton").objectReferenceValue = quit;
        so.FindProperty("resultPanel").objectReferenceValue = resultPanel;
        so.FindProperty("resultTitleText").objectReferenceValue = resultTitle;
        so.FindProperty("resultDetailText").objectReferenceValue = resultDetail;
        so.FindProperty("correctAnswerText").objectReferenceValue = correctAnswer;
        so.FindProperty("playAgainButton").objectReferenceValue = playAgain;
        so.FindProperty("continueButton").objectReferenceValue = cont;
        so.FindProperty("sessionSummaryPanel").objectReferenceValue = summaryPanel;
        so.FindProperty("summaryTitleText").objectReferenceValue = summaryTitle;
        so.FindProperty("summaryScoreText").objectReferenceValue = summaryScore;
        so.FindProperty("summaryBreakdownText").objectReferenceValue = summaryBreakdown;
        so.FindProperty("replaySessionButton").objectReferenceValue = replaySession;
        so.FindProperty("summaryQuitButton").objectReferenceValue = summaryQuit;
        so.FindProperty("correctParticles").objectReferenceValue = particles;
        so.FindProperty("sceneCanvasGroup").objectReferenceValue = canvasGroup;
        so.ApplyModifiedProperties();
    }

    private static void WireAudioManager(DictationAudioManager audio, Button playButton, Image playButtonIcon, Image[] replayIcons,
        GameObject visualizerRoot, TextMeshProUGUI listeningLabel, RectTransform[] waveformBars)
    {
        SerializedObject so = new SerializedObject(audio);
        so.FindProperty("audioSource").objectReferenceValue = audio.GetComponent<AudioSource>();
        so.FindProperty("playButton").objectReferenceValue = playButton;
        so.FindProperty("playButtonIcon").objectReferenceValue = playButtonIcon;
        SerializedProperty playLabelProperty = so.FindProperty("playButtonLabel");
        if (playLabelProperty != null)
            playLabelProperty.objectReferenceValue = playButton.GetComponentInChildren<TextMeshProUGUI>(true);
        SerializedProperty icons = so.FindProperty("replayIcons");
        icons.arraySize = replayIcons.Length;
        for (int i = 0; i < replayIcons.Length; i++)
            icons.GetArrayElementAtIndex(i).objectReferenceValue = replayIcons[i];
        so.FindProperty("visualizerRoot").objectReferenceValue = visualizerRoot;
        so.FindProperty("listeningLabel").objectReferenceValue = listeningLabel;
        SerializedProperty bars = so.FindProperty("waveformBars");
        bars.arraySize = waveformBars.Length;
        for (int i = 0; i < waveformBars.Length; i++)
            bars.GetArrayElementAtIndex(i).objectReferenceValue = waveformBars[i];
        so.ApplyModifiedProperties();
    }

    private static void WireHintSystem(DictationHintSystem hints, TextMeshProUGUI display, Button button)
    {
        SerializedObject so = new SerializedObject(hints);
        so.FindProperty("hintDisplayText").objectReferenceValue = display;
        so.FindProperty("hintButton").objectReferenceValue = button;
        so.FindProperty("hintButtonLabel").objectReferenceValue = button.GetComponentInChildren<TextMeshProUGUI>(true);
        so.ApplyModifiedProperties();
    }

    private static void WireKeyboard(DictationKeyboard keyboard, TMP_InputField input,
        GameObject keyTemplate, GameObject spaceTemplate, GameObject backspaceTemplate,
        Transform row1, Transform row2, Transform row3, Transform row4)
    {
        SerializedObject so = new SerializedObject(keyboard);
        so.FindProperty("targetInputField").objectReferenceValue = input;
        so.FindProperty("keyTemplate").objectReferenceValue = keyTemplate;
        so.FindProperty("spaceKeyTemplate").objectReferenceValue = spaceTemplate;
        so.FindProperty("backspaceKeyTemplate").objectReferenceValue = backspaceTemplate;
        so.FindProperty("row1Container").objectReferenceValue = row1;
        so.FindProperty("row2Container").objectReferenceValue = row2;
        so.FindProperty("row3Container").objectReferenceValue = row3;
        so.FindProperty("row4Container").objectReferenceValue = row4;
        so.ApplyModifiedProperties();
    }

    private static void WireThemeFonts(DictationThemeFonts themeFonts, Transform root)
    {
        SerializedObject so = new SerializedObject(themeFonts);
        so.FindProperty("root").objectReferenceValue = root;
        so.ApplyModifiedProperties();
    }

    private static Color Hex(string html)
    {
        ColorUtility.TryParseHtmlString("#" + html, out Color color);
        return color;
    }
}
#endif
