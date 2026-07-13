#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class TreasureQuestFinalChestFeatureInstaller
{
    private static readonly Color DarkBrown = new Color(0.20f, 0.12f, 0.06f, 1f);
    private static readonly Color Brown = new Color(0.38f, 0.22f, 0.10f, 1f);
    private static readonly Color Gold = new Color(0.94f, 0.70f, 0.24f, 1f);
    private static readonly Color Cream = new Color(1f, 0.94f, 0.80f, 1f);
    private static readonly Color SoftPanel = new Color(1f, 0.90f, 0.67f, 0.98f);
    private static readonly Color TreasureImagePlaceholder = new Color(1f, 0.82f, 0.30f, 0.28f);

    [MenuItem("Tools/Treasure Quest/Add Final Chest Click Feature To Current Scene")]
    public static void AddFinalChestClickFeatureToCurrentScene()
    {
        TreasureQuestUIManager uiManager = Object.FindObjectOfType<TreasureQuestUIManager>(true);
        if (!ValidateUiManager(uiManager))
            return;

        Canvas rootCanvas = GetRootCanvas(uiManager);
        TreasureQuestFinalChestFeature feature = GetOrCreateFinalChestFeature(uiManager, rootCanvas);
        if (feature == null)
            return;

        feature.completedPanel = GetOrCreateCompletedPanel(rootCanvas.transform, feature);
        feature.uiCoinFxRoot = GetOrCreateUiCoinFxRoot(rootCanvas.transform);
        feature.coinParticleSystem = GetOrCreateCoinParticleSystem(feature);
        feature.BindButtons();

        EditorUtility.SetDirty(feature.gameObject);
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rootCanvas.gameObject);

        Selection.activeGameObject = feature.gameObject;
        Debug.Log("Treasure Quest final chest click feature added to current scene. Existing UI layout was not rebuilt.");
    }

    [MenuItem("Tools/Treasure Quest/Update Final Chest Complete Card Only")]
    public static void UpdateFinalChestCompleteCardOnly()
    {
        TreasureQuestUIManager uiManager = Object.FindObjectOfType<TreasureQuestUIManager>(true);
        if (!ValidateUiManager(uiManager))
            return;

        Canvas rootCanvas = GetRootCanvas(uiManager);
        TreasureQuestFinalChestFeature feature = GetOrCreateFinalChestFeature(uiManager, rootCanvas);
        if (feature == null)
            return;

        feature.completedPanel = GetOrCreateCompletedPanel(rootCanvas.transform, feature);
        feature.BindButtons();

        EditorUtility.SetDirty(feature.gameObject);
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rootCanvas.gameObject);

        Selection.activeGameObject = feature.completedPanel != null ? feature.completedPanel : feature.gameObject;
        Debug.Log("Treasure Quest final chest complete card updated only. Card is larger and now has CompletedTreasureImage for your treasure sprite.");
    }

    private static bool ValidateUiManager(TreasureQuestUIManager uiManager)
    {
        if (uiManager == null)
        {
            EditorUtility.DisplayDialog("Treasure Quest", "No TreasureQuestUIManager found in this scene.", "OK");
            return false;
        }

        if (uiManager.treasureChestImage == null)
        {
            EditorUtility.DisplayDialog("Treasure Quest", "TreasureQuestUIManager.treasureChestImage is not assigned. Assign your final chest Image first.", "OK");
            return false;
        }

        return true;
    }

    private static Canvas GetRootCanvas(TreasureQuestUIManager uiManager)
    {
        Canvas rootCanvas = uiManager.GetComponentInParent<Canvas>();
        if (rootCanvas == null)
            rootCanvas = Object.FindObjectOfType<Canvas>(true);

        if (rootCanvas == null)
            EditorUtility.DisplayDialog("Treasure Quest", "No Canvas found in this scene.", "OK");

        return rootCanvas;
    }

    private static TreasureQuestFinalChestFeature GetOrCreateFinalChestFeature(TreasureQuestUIManager uiManager, Canvas rootCanvas)
    {
        if (rootCanvas == null || uiManager == null || uiManager.treasureChestImage == null)
            return null;

        TreasureQuestLevelManager levelManager = Object.FindObjectOfType<TreasureQuestLevelManager>(true);
        TreasureQuestAudioManager audioManager = Object.FindObjectOfType<TreasureQuestAudioManager>(true);

        GameObject chestObject = uiManager.treasureChestImage.gameObject;
        Button chestButton = chestObject.GetComponent<Button>();
        if (chestButton == null)
            chestButton = Undo.AddComponent<Button>(chestObject);

        chestButton.targetGraphic = uiManager.treasureChestImage;
        chestButton.transition = Selectable.Transition.None;
        chestButton.interactable = true;
        uiManager.treasureChestImage.raycastTarget = true;
        uiManager.treasureChestImage.color = Color.white;

        TreasureQuestFinalChestFeature feature = chestObject.GetComponent<TreasureQuestFinalChestFeature>();
        if (feature == null)
            feature = Undo.AddComponent<TreasureQuestFinalChestFeature>(chestObject);

        feature.uiManager = uiManager;
        feature.levelManager = levelManager;
        feature.audioManager = audioManager;
        feature.chestButton = chestButton;
        feature.chestImage = uiManager.treasureChestImage;
        feature.chestRect = uiManager.treasureChestImage.rectTransform;

        return feature;
    }

    private static GameObject GetOrCreateCompletedPanel(Transform canvasRoot, TreasureQuestFinalChestFeature feature)
    {
        Transform existing = canvasRoot.Find("FinalChestCompletedPanel");
        GameObject panel;

        if (existing != null)
        {
            panel = existing.gameObject;
        }
        else
        {
            panel = new GameObject("FinalChestCompletedPanel", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            Undo.RegisterCreatedObjectUndo(panel, "Create Final Chest Completed Panel");
            panel.transform.SetParent(canvasRoot, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            StretchFull(panelRect);

            Canvas panelCanvas = panel.GetComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 4500;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.42f);
            panelImage.raycastTarget = true;
        }

        EnsureCompletedCardLayout(panel.transform, feature);
        panel.SetActive(false);
        return panel;
    }

    private static void EnsureCompletedCardLayout(Transform panelRoot, TreasureQuestFinalChestFeature feature)
    {
        Transform cardTransform = panelRoot.Find("CompleteCard");
        Image card;

        if (cardTransform == null)
        {
            card = CreateImage(panelRoot, "CompleteCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1050f, 720f), SoftPanel);
            Undo.RegisterCreatedObjectUndo(card.gameObject, "Create Final Chest Complete Card");
        }
        else
        {
            card = cardTransform.GetComponent<Image>();
            if (card == null)
                card = Undo.AddComponent<Image>(cardTransform.gameObject);
        }

        card.raycastTarget = true;
        card.color = SoftPanel;

        RectTransform cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1050f, 720f);
        feature.completedCardRect = cardRect;

        TMP_Text title = GetOrCreateText(card.transform, "CompletedTitleText", "Treasure Complete!", 60, TextAlignmentOptions.Center, DarkBrown);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(-100f, 95f));

        Image treasureImage = GetOrCreateImage(card.transform, "CompletedTreasureImage", TreasureImagePlaceholder);
        RectTransform treasureRect = treasureImage.rectTransform;
        treasureRect.anchorMin = new Vector2(0.5f, 0.5f);
        treasureRect.anchorMax = new Vector2(0.5f, 0.5f);
        treasureRect.anchoredPosition = new Vector2(0f, 95f);
        treasureRect.sizeDelta = new Vector2(380f, 270f);
        treasureImage.preserveAspect = true;
        treasureImage.raycastTarget = false;

        TMP_Text details = GetOrCreateText(card.transform, "CompletedDetailsText", "You opened every gate and found the treasure.\nTap Play Again to reset the map.", 34, TextAlignmentOptions.Center, DarkBrown);
        details.enableWordWrapping = true;
        SetRect(details.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -130f), new Vector2(-120f, 125f));

        Button playAgain = GetOrCreateButton(card.transform, "PlayAgainResetButton", "Play Again", Brown, Cream, 32);
        SetRect(playAgain.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170f, 95f), new Vector2(290f, 86f));

        Button close = GetOrCreateButton(card.transform, "CloseCompletedPanelButton", "Map", Gold, DarkBrown, 32);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(170f, 95f), new Vector2(230f, 86f));

        feature.completedTitleText = title;
        feature.completedTreasureImage = treasureImage;
        feature.completedDetailsText = details;
        feature.playAgainResetButton = playAgain;
        feature.closeButton = close;
    }

    private static RectTransform GetOrCreateUiCoinFxRoot(Transform canvasRoot)
    {
        Transform existing = canvasRoot.Find("FinalChestCoinFxRoot");
        if (existing != null)
            return existing as RectTransform;

        GameObject root = new GameObject("FinalChestCoinFxRoot", typeof(RectTransform), typeof(Canvas));
        Undo.RegisterCreatedObjectUndo(root, "Create Final Chest Coin FX Root");
        root.transform.SetParent(canvasRoot, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        StretchFull(rect);

        Canvas fxCanvas = root.GetComponent<Canvas>();
        fxCanvas.overrideSorting = true;
        fxCanvas.sortingOrder = 5000;

        return rect;
    }

    private static ParticleSystem GetOrCreateCoinParticleSystem(TreasureQuestFinalChestFeature feature)
    {
        GameObject existing = GameObject.Find("FinalChestCoinParticleSystem");
        if (existing != null)
        {
            ParticleSystem existingParticle = existing.GetComponent<ParticleSystem>();
            if (existingParticle != null)
                return existingParticle;
        }

        GameObject particleObject = new GameObject("FinalChestCoinParticleSystem");
        Undo.RegisterCreatedObjectUndo(particleObject, "Create Final Chest Coin Particle System");

        ParticleSystem particle = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particle.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.6f, 5.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.08f, 1f), new Color(1f, 0.95f, 0.25f, 1f));
        main.gravityModifier = 1.25f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particle.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

        ParticleSystem.ShapeModule shape = particle.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.35f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particle.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);
        velocity.y = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        ParticleSystem.RotationOverLifetimeModule rotation = particle.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-7f, 7f);

        ParticleSystemRenderer renderer = particle.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9999;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            renderer.sharedMaterial = new Material(shader);

        return particle;
    }

    private static Image GetOrCreateImage(Transform parent, string name, Color color)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            Image existingImage = existing.GetComponent<Image>();
            if (existingImage != null)
                return existingImage;

            return Undo.AddComponent<Image>(existing.gameObject);
        }

        return CreateImage(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f), color);
    }

    private static TMP_Text GetOrCreateText(Transform parent, string name, string text, int size, TextAlignmentOptions alignment, Color color)
    {
        Transform existing = parent.Find(name);
        TMP_Text label;

        if (existing != null)
        {
            label = existing.GetComponent<TMP_Text>();
            if (label == null)
                label = Undo.AddComponent<TextMeshProUGUI>(existing.gameObject);
        }
        else
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            label = go.AddComponent<TextMeshProUGUI>();
        }

        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.enableWordWrapping = true;
        return label;
    }

    private static Button GetOrCreateButton(Transform parent, string name, string text, Color bg, Color textColor, int textSize)
    {
        Transform existing = parent.Find(name);
        Image image;
        Button button;

        if (existing != null)
        {
            image = existing.GetComponent<Image>();
            if (image == null)
                image = Undo.AddComponent<Image>(existing.gameObject);

            button = existing.GetComponent<Button>();
            if (button == null)
                button = Undo.AddComponent<Button>(existing.gameObject);
        }
        else
        {
            image = CreateImage(parent, name, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(260f, 80f), bg);
            Undo.RegisterCreatedObjectUndo(image.gameObject, "Create " + name);
            button = image.gameObject.AddComponent<Button>();
        }

        image.color = bg;
        button.targetGraphic = image;

        TMP_Text label = GetOrCreateText(image.transform, "Text", text, textSize, TextAlignmentOptions.Center, textColor);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = Vector2.zero;

        return button;
    }

    private static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}
#endif
