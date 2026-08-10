#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OddClawFirstTimeTutorialInstaller
{
    private const string RootName = "OddClawCatch_FirstTimeTutorial";
    private const string InstallMenu = "Tools/Odd Claw Catch/First-Time Tutorial/Install Or Upgrade In Open Scene";
    private const string SelectMenu = "Tools/Odd Claw Catch/First-Time Tutorial/Select Tutorial Root";
    private const string ResetMenu = "Tools/Odd Claw Catch/First-Time Tutorial/Reset Save For Open Scene";

    [MenuItem(InstallMenu)]
    public static void InstallOrUpgradeInOpenScene()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog(
                "Odd Claw First-Time Tutorial",
                "No OddClawCatchManager was found in the open scene. Open the prepared game scene and try again.",
                "OK");
            return;
        }

        Canvas canvas = manager.rootCanvas != null
            ? manager.rootCanvas
            : manager.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Odd Claw First-Time Tutorial",
                "The scene manager has no Root Canvas reference. Assign the existing game Canvas first.",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Install Odd Claw First-Time Tutorial");
        int undoGroup = Undo.GetCurrentGroup();

        RectTransform root = FindChildRecursive(canvas.transform, RootName);
        bool createdRoot = root == null;

        if (createdRoot)
        {
            GameObject rootObject = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Odd Claw Tutorial Root");
            root = rootObject.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Stretch(root);
        }

        root.gameObject.SetActive(true);
        CanvasGroup rootGroup = GetOrAddComponent<CanvasGroup>(root.gameObject);
        OddClawFirstTimeTutorialController controller =
            GetOrAddComponent<OddClawFirstTimeTutorialController>(root.gameObject);

        RectTransform focus = GetOrCreateRect("FocusHighlight", root, out bool focusCreated);
        Image focusImage = GetOrAddComponent<Image>(focus.gameObject);
        CanvasGroup focusGroup = GetOrAddComponent<CanvasGroup>(focus.gameObject);
        Outline focusOutline = GetOrAddComponent<Outline>(focus.gameObject);
        if (focusCreated)
        {
            Centre(focus, new Vector2(360f, 120f), Vector2.zero);
            focusImage.color = new Color(0.16f, 0.82f, 1f, 0.08f);
            focusImage.raycastTarget = false;
            focusOutline.effectColor = new Color(0.12f, 0.72f, 1f, 0.9f);
            focusOutline.effectDistance = new Vector2(4f, -4f);
            focusGroup.blocksRaycasts = false;
            focusGroup.interactable = false;
        }

        RectTransform instruction = GetOrCreateRect("InstructionPanel", root, out bool instructionCreated);
        Image instructionImage = GetOrAddComponent<Image>(instruction.gameObject);
        CanvasGroup instructionGroup = GetOrAddComponent<CanvasGroup>(instruction.gameObject);
        Outline instructionOutline = GetOrAddComponent<Outline>(instruction.gameObject);
        if (instructionCreated)
        {
            Centre(instruction, new Vector2(560f, 150f), new Vector2(0f, 330f));
            instructionImage.color = new Color(1f, 1f, 1f, 0.94f);
            instructionImage.raycastTarget = false;
            instructionOutline.effectColor = new Color(0.12f, 0.32f, 0.52f, 0.28f);
            instructionOutline.effectDistance = new Vector2(0f, -4f);
            instructionGroup.blocksRaycasts = false;
            instructionGroup.interactable = false;
        }

        TMP_Text instructionText = GetOrCreateText(
            "InstructionText",
            instruction,
            "Tutorial instruction",
            31f,
            TextAlignmentOptions.Center,
            out _);
        StretchWithMargins(instructionText.rectTransform, 24f, 24f, 15f, 15f);
        instructionText.color = new Color(0.06f, 0.12f, 0.2f, 1f);
        instructionText.enableWordWrapping = true;
        instructionText.raycastTarget = false;

        RectTransform pointer = GetOrCreateRect("HandPointer", root, out bool pointerCreated);
        Image pointerImage = GetOrAddComponent<Image>(pointer.gameObject);
        if (pointerCreated)
        {
            Centre(pointer, new Vector2(120f, 120f), Vector2.zero);
            pointerImage.color = Color.white;
            pointerImage.preserveAspect = true;
            pointerImage.raycastTarget = false;
            pointerImage.enabled = false;
        }

        Button skipButton = GetOrCreateButton(
            "SkipButton",
            root,
            "SKIP",
            new Color(0.15f, 0.37f, 0.68f, 0.95f),
            out RectTransform skipRect,
            out bool skipCreated);
        bool usesOldTopRightPlacement =
            Vector2.Distance(skipRect.anchorMin, Vector2.one) < 0.01f
            && Vector2.Distance(skipRect.anchorMax, Vector2.one) < 0.01f;
        if (skipCreated || usesOldTopRightPlacement)
        {
            skipRect.anchorMin = new Vector2(1f, 0f);
            skipRect.anchorMax = new Vector2(1f, 0f);
            skipRect.pivot = new Vector2(1f, 0f);
            skipRect.sizeDelta = new Vector2(160f, 62f);
            skipRect.anchoredPosition = new Vector2(-28f, 28f);
        }

        RectTransform confirmation = GetOrCreateRect("SkipConfirmationPanel", root, out bool confirmationCreated);
        if (confirmationCreated)
            Stretch(confirmation);

        RectTransform confirmationCard = GetOrCreateRect("ConfirmationCard", confirmation, out bool cardCreated);
        Image cardImage = GetOrAddComponent<Image>(confirmationCard.gameObject);
        Outline cardOutline = GetOrAddComponent<Outline>(confirmationCard.gameObject);
        if (cardCreated)
        {
            Centre(confirmationCard, new Vector2(620f, 300f), Vector2.zero);
            cardImage.color = new Color(1f, 1f, 1f, 0.98f);
            cardImage.raycastTarget = true;
            cardOutline.effectColor = new Color(0.05f, 0.12f, 0.2f, 0.32f);
            cardOutline.effectDistance = new Vector2(0f, -5f);
        }

        TMP_Text confirmationText = GetOrCreateText(
            "ConfirmationText",
            confirmationCard,
            "Skip this practice tutorial?",
            32f,
            TextAlignmentOptions.Center,
            out bool confirmationTextCreated);
        if (confirmationTextCreated)
        {
            confirmationText.rectTransform.anchorMin = new Vector2(0.08f, 0.5f);
            confirmationText.rectTransform.anchorMax = new Vector2(0.92f, 1f);
            confirmationText.rectTransform.offsetMin = new Vector2(0f, -8f);
            confirmationText.rectTransform.offsetMax = new Vector2(0f, -20f);
            confirmationText.color = new Color(0.06f, 0.12f, 0.2f, 1f);
            confirmationText.enableWordWrapping = true;
            confirmationText.raycastTarget = false;
        }

        Button confirmButton = GetOrCreateButton(
            "ConfirmSkipButton",
            confirmationCard,
            "YES, SKIP",
            new Color(0.84f, 0.28f, 0.27f, 1f),
            out RectTransform confirmRect,
            out bool confirmCreated);
        if (confirmCreated)
        {
            confirmRect.anchorMin = new Vector2(0.5f, 0f);
            confirmRect.anchorMax = new Vector2(0.5f, 0f);
            confirmRect.pivot = new Vector2(1f, 0f);
            confirmRect.sizeDelta = new Vector2(230f, 68f);
            confirmRect.anchoredPosition = new Vector2(-12f, 34f);
        }

        Button cancelButton = GetOrCreateButton(
            "CancelSkipButton",
            confirmationCard,
            "KEEP PLAYING",
            new Color(0.12f, 0.58f, 0.48f, 1f),
            out RectTransform cancelRect,
            out bool cancelCreated);
        if (cancelCreated)
        {
            cancelRect.anchorMin = new Vector2(0.5f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0f, 0f);
            cancelRect.sizeDelta = new Vector2(230f, 68f);
            cancelRect.anchoredPosition = new Vector2(12f, 34f);
        }

        confirmation.gameObject.SetActive(false);

        Undo.RecordObject(controller, "Assign Odd Claw Tutorial References");
        AssignIfMissing(ref controller.gameManager, manager);
        AssignIfMissing(ref controller.rootCanvas, canvas);
        AssignIfMissing(ref controller.tutorialRoot, root);
        AssignIfMissing(ref controller.tutorialCanvasGroup, rootGroup);
        AssignIfMissing(ref controller.focusHighlight, focus);
        AssignIfMissing(ref controller.focusCanvasGroup, focusGroup);
        AssignIfMissing(ref controller.instructionRoot, instruction);
        AssignIfMissing(ref controller.instructionCanvasGroup, instructionGroup);
        AssignIfMissing(ref controller.instructionText, instructionText);
        AssignIfMissing(ref controller.handPointer, pointer);
        AssignIfMissing(ref controller.handPointerImage, pointerImage);
        AssignIfMissing(ref controller.skipButton, skipButton);
        AssignIfMissing(ref controller.skipConfirmationPanel, confirmation.gameObject);
        AssignIfMissing(ref controller.skipConfirmationText, confirmationText);
        AssignIfMissing(ref controller.skipConfirmButton, confirmButton);
        AssignIfMissing(ref controller.skipCancelButton, cancelButton);
        MigrateOldDefaultPointerPlacement(controller);
        EditorUtility.SetDirty(controller);

        if (manager.firstTimeTutorial == null)
        {
            Undo.RecordObject(manager, "Assign Odd Claw First-Time Tutorial");
            manager.firstTimeTutorial = controller;
            EditorUtility.SetDirty(manager);
        }
        else if (manager.firstTimeTutorial != controller)
        {
            Debug.LogWarning(
                "OddClawCatchManager already references a different first-time tutorial. " +
                "The installer kept that existing reference unchanged.");
        }

        if (createdRoot)
        {
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = false;
            rootGroup.interactable = false;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = root.gameObject;

        EditorUtility.DisplayDialog(
            "Odd Claw First-Time Tutorial",
            createdRoot
                ? "Installed successfully. Only the new tutorial root and the manager reference were added. Assign your hand sprite and review the editable question, answers, texts, offsets, and timings in the tutorial Inspector."
                : "Upgrade complete. Missing tutorial-owned elements were added without rebuilding the scene or changing the existing game layout.",
            "OK");
    }

    [MenuItem(SelectMenu)]
    public static void SelectTutorialRoot()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        Canvas canvas = manager != null ? manager.rootCanvas : null;
        RectTransform root = canvas != null ? FindChildRecursive(canvas.transform, RootName) : null;

        if (root == null)
        {
            EditorUtility.DisplayDialog(
                "Odd Claw First-Time Tutorial",
                "No installed tutorial root was found in the open scene.",
                "OK");
            return;
        }

        root.gameObject.SetActive(true);
        Selection.activeGameObject = root.gameObject;
        EditorGUIUtility.PingObject(root.gameObject);
    }

    [MenuItem(ResetMenu)]
    public static void ResetSaveForOpenScene()
    {
        OddClawFirstTimeTutorialController controller =
            Object.FindObjectOfType<OddClawFirstTimeTutorialController>(true);

        if (controller != null)
        {
            controller.ResetTutorialSaveForThisScene();
        }
        else
        {
            string fallbackKey = "OddClawCatch_InteractiveTutorialCompleted_" + SceneManager.GetActiveScene().name;
            PlayerPrefs.DeleteKey(fallbackKey);
            PlayerPrefs.Save();
        }

        EditorUtility.DisplayDialog(
            "Odd Claw First-Time Tutorial",
            "The first-time tutorial save was reset for the open scene.",
            "OK");
    }

    private static RectTransform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child as RectTransform;

            RectTransform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static RectTransform GetOrCreateRect(string name, Transform parent, out bool created)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            created = false;
            RectTransform existingRect = existing as RectTransform;
            return existingRect != null
                ? existingRect
                : GetOrAddComponent<RectTransform>(existing.gameObject);
        }

        GameObject child = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, "Create " + name);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        created = true;
        return rect;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static TMP_Text GetOrCreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        out bool created)
    {
        RectTransform rect = GetOrCreateRect(name, parent, out created);
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text == null)
            text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);

        if (created)
        {
            text.text = value;
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
        }

        return text;
    }

    private static Button GetOrCreateButton(
        string name,
        Transform parent,
        string label,
        Color color,
        out RectTransform rect,
        out bool created)
    {
        rect = GetOrCreateRect(name, parent, out created);
        Image image = GetOrAddComponent<Image>(rect.gameObject);
        Button button = GetOrAddComponent<Button>(rect.gameObject);

        if (created)
        {
            image.color = color;
            image.raycastTarget = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
        }

        TMP_Text text = GetOrCreateText(
            "Text",
            rect,
            label,
            24f,
            TextAlignmentOptions.Center,
            out bool textCreated);

        if (textCreated)
        {
            Stretch(text.rectTransform);
            text.color = Color.white;
            text.raycastTarget = false;
        }

        return button;
    }

    private static void AssignIfMissing<T>(ref T field, T value) where T : Object
    {
        if (field == null)
            field = value;
    }

    private static void MigrateOldDefaultPointerPlacement(
        OddClawFirstTimeTutorialController controller)
    {
        const int currentPositioningRevision = 5;
        if (controller.positioningRevision < currentPositioningRevision)
        {
            // Revision 5 intentionally replaces earlier tutorial-owned placement
            // values once, including values serialized by versions 1 through 4.
            controller.answersPointerOffset = new Vector2(55f, -95f);
            controller.demonstrationInstructionPosition = new Vector2(-300f, -300f);
            controller.autoPlaceDemonstrationInstructionAwayFromClaw = true;
            controller.positioningRevision = currentPositioningRevision;
        }

        if (Approximately(controller.answersPointerOffset, new Vector2(-70f, 80f))
            || Approximately(controller.answersPointerOffset, new Vector2(-110f, -15f))
            || Approximately(controller.answersPointerOffset, new Vector2(0f, -95f))
            || Approximately(controller.answersPointerOffset, new Vector2(-45f, -95f)))
        {
            controller.answersPointerOffset = new Vector2(55f, -95f);
        }

        if (Approximately(controller.clawPointerOffset, new Vector2(110f, -20f)))
            controller.clawPointerOffset = new Vector2(120f, -10f);

        if (Approximately(controller.demonstrationTapPosition, new Vector2(-360f, 260f)))
            controller.demonstrationTapPosition = new Vector2(-360f, 120f);

        if (Approximately(controller.demonstrationInstructionPosition, new Vector2(0f, 350f))
            || Approximately(controller.demonstrationInstructionPosition, new Vector2(0f, 0f))
            || Approximately(controller.demonstrationInstructionPosition, new Vector2(0f, -300f)))
        {
            controller.demonstrationInstructionPosition = new Vector2(-300f, -300f);
        }

        if (Approximately(controller.clawInstructionOffset, new Vector2(250f, -40f)))
            controller.clawInstructionOffset = new Vector2(-330f, -40f);
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude < 0.01f;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void StretchWithMargins(
        RectTransform rect,
        float left,
        float right,
        float top,
        float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void Centre(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}
#endif
