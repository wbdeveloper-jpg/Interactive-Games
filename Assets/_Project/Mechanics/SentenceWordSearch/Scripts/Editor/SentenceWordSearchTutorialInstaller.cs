#if UNITY_EDITOR

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SentenceWordSearchTutorialInstaller
{
    private const string TutorialRootName = "SentenceWordSearchFirstTimeTutorialRoot";

    [MenuItem("Mini Games/Sentence Word Search/First-Time Tutorial/Install Or Upgrade In Open Scene")]
    public static void InstallOrUpgrade()
    {
        SentenceWordSearchManager manager = Object.FindObjectOfType<SentenceWordSearchManager>();
        SentenceWordSearchBoard board = Object.FindObjectOfType<SentenceWordSearchBoard>();
        SentenceWordSearchInputController input = Object.FindObjectOfType<SentenceWordSearchInputController>();
        SentenceWordSearchUI ui = Object.FindObjectOfType<SentenceWordSearchUI>();

        if (manager == null || board == null || input == null || ui == null)
        {
            EditorUtility.DisplayDialog(
                "Sentence Word Search Tutorial",
                "The open scene must contain SentenceWordSearchManager, Board, InputController, and UI components.",
                "OK");
            return;
        }

        RectTransform parent = ResolveTutorialParent(ui);

        if (parent == null)
        {
            EditorUtility.DisplayDialog(
                "Sentence Word Search Tutorial",
                "No Canvas or Overlay Root was found in the open scene.",
                "OK");
            return;
        }

        SentenceWordSearchFirstTimeTutorial tutorial = manager.tutorialController;
        GameObject root;

        if (tutorial != null)
        {
            root = tutorial.gameObject;
        }
        else
        {
            Transform existing = FindChildRecursive(parent, TutorialRootName);
            root = existing != null ? existing.gameObject : CreateUiObject(TutorialRootName, parent);

            tutorial = root.GetComponent<SentenceWordSearchFirstTimeTutorial>();
            if (tutorial == null)
                tutorial = Undo.AddComponent<SentenceWordSearchFirstTimeTutorial>(root);
        }

        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);
        rootRect.SetAsLastSibling();

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = Undo.AddComponent<CanvasGroup>(root);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform focusFrame = GetOrCreateRect("FocusFrame", rootRect);
        focusFrame.anchorMin = focusFrame.anchorMax = new Vector2(0.5f, 0.5f);
        focusFrame.pivot = new Vector2(0.5f, 0.5f);
        EnsureFocusBorders(focusFrame);
        focusFrame.gameObject.SetActive(false);

        RectTransform instructionPanel = GetOrCreateRect("InstructionPanel", rootRect);
        bool instructionPanelWasNew = instructionPanel.GetComponent<Image>() == null;
        Image instructionBackground = GetOrAddImage(instructionPanel.gameObject);

        instructionPanel.anchorMin = instructionPanel.anchorMax = new Vector2(0.5f, 0.5f);
        instructionPanel.pivot = new Vector2(0.5f, 0.5f);
        instructionPanel.sizeDelta = new Vector2(430f, 220f);

        CanvasGroup instructionCanvasGroup = instructionPanel.GetComponent<CanvasGroup>();
        if (instructionCanvasGroup == null)
            instructionCanvasGroup = Undo.AddComponent<CanvasGroup>(instructionPanel.gameObject);

        instructionCanvasGroup.alpha = 1f;
        instructionCanvasGroup.interactable = false;
        instructionCanvasGroup.blocksRaycasts = false;

        if (instructionPanelWasNew)
        {
            instructionPanel.anchoredPosition = new Vector2(0f, 250f);
            instructionBackground.color = new Color(1f, 0.96f, 0.92f, 0.98f);
            instructionBackground.raycastTarget = false;

            Outline outline = instructionPanel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.65f, 0.15f, 0.14f, 0.45f);
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = true;
        }

        RectTransform instructionTextRect = GetOrCreateRect("InstructionText", instructionPanel);
        Stretch(instructionTextRect);
        instructionTextRect.offsetMin = new Vector2(34f, 20f);
        instructionTextRect.offsetMax = new Vector2(-34f, -20f);

        TextMeshProUGUI instructionText = instructionTextRect.GetComponent<TextMeshProUGUI>();
        if (instructionText == null)
        {
            instructionText = Undo.AddComponent<TextMeshProUGUI>(instructionTextRect.gameObject);
            instructionText.text = "Tutorial instruction";
            instructionText.fontSize = 34f;
            instructionText.alignment = TextAlignmentOptions.Center;
            instructionText.color = new Color(0.22f, 0.16f, 0.15f, 1f);
            instructionText.enableWordWrapping = true;
            instructionText.raycastTarget = false;
        }

        if (ui.secondaryFont != null)
            instructionText.font = ui.secondaryFont;

        instructionText.enableAutoSizing = true;
        instructionText.fontSizeMin = 22f;
        instructionText.fontSizeMax = 34f;

        RectTransform handPointer = GetOrCreateRect("HandPointer", rootRect);
        bool handWasNew = handPointer.GetComponent<Image>() == null;
        Image handImage = GetOrAddImage(handPointer.gameObject);

        if (handWasNew)
        {
            handPointer.anchorMin = handPointer.anchorMax = new Vector2(0.5f, 0.5f);
            handPointer.pivot = new Vector2(0.5f, 0.9f);
            handPointer.sizeDelta = new Vector2(96f, 120f);
            handImage.sprite = null;
            handImage.color = Color.white;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;
        }

        handPointer.gameObject.SetActive(false);

        Undo.RecordObject(tutorial, "Assign Sentence Word Search tutorial references");
        tutorial.manager = manager;
        tutorial.board = board;
        tutorial.inputController = input;
        tutorial.ui = ui;
        tutorial.tutorialCanvasRoot = rootRect;
        tutorial.tutorialCanvasGroup = canvasGroup;
        tutorial.instructionPanel = instructionPanel;
        tutorial.instructionPanelCanvasGroup = instructionCanvasGroup;
        tutorial.instructionText = instructionText;
        tutorial.handPointer = handPointer;
        tutorial.handPointerImage = handImage;
        tutorial.focusFrame = focusFrame;
        tutorial.initialDemonstrationRepeats = Mathf.Max(4, tutorial.initialDemonstrationRepeats);
        tutorial.idleDemonstrationRepeats = Mathf.Max(2, tutorial.idleDemonstrationRepeats);
        tutorial.instructionCenterNudge = Mathf.Max(65f, tutorial.instructionCenterNudge);

        Undo.RecordObject(manager, "Assign tutorial controller");
        manager.tutorialController = tutorial;

        Undo.RecordObject(input, "Assign tutorial controller");
        input.tutorialController = tutorial;

        EditorUtility.SetDirty(tutorial);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(input);
        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        EditorUtility.DisplayDialog(
            "Sentence Word Search Tutorial",
            "First-time tutorial installed or upgraded.\n\nAssign your hand sprite to Hand Pointer Image on the selected tutorial root.",
            "OK");
    }

    private static RectTransform ResolveTutorialParent(SentenceWordSearchUI ui)
    {
        if (ui != null && ui.overlayRoot != null)
            return ui.overlayRoot;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        return canvas != null ? canvas.transform as RectTransform : null;
    }

    private static RectTransform GetOrCreateRect(string name, RectTransform parent)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
            return existing as RectTransform;

        return CreateUiObject(name, parent).GetComponent<RectTransform>();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject created = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, "Create " + name);
        created.transform.SetParent(parent, false);
        return created;
    }

    private static Image GetOrAddImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        return image != null ? image : Undo.AddComponent<Image>(target);
    }

    private static void EnsureFocusBorders(RectTransform frame)
    {
        Color color = new Color(0.95f, 0.25f, 0.2f, 0.95f);
        CreateBorder(frame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 7f), color);
        CreateBorder(frame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), color);
        CreateBorder(frame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(7f, 0f), color);
        CreateBorder(frame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(7f, 0f), color);
    }

    private static void CreateBorder(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta,
        Color color)
    {
        RectTransform border = GetOrCreateRect(name, parent);
        border.anchorMin = anchorMin;
        border.anchorMax = anchorMax;
        border.pivot = pivot;
        border.anchoredPosition = Vector2.zero;
        border.sizeDelta = sizeDelta;

        Image image = GetOrAddImage(border.gameObject);
        image.color = color;
        image.raycastTarget = false;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null)
                return result;
        }

        return null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

#endif
