#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GridAdventureFirstTimeTutorialInstaller
{
    private const string TutorialRootName = "GridAdventureFirstTimeTutorialRoot";

    [MenuItem("Tools/Grid Adventure/Install or Upgrade First-Time Tutorial")]
    public static void InstallOrUpgrade()
    {
        GridAdventureManager manager = Object.FindObjectOfType<GridAdventureManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog(
                "Grid Adventure Tutorial",
                "No GridAdventureManager was found in the open scene.",
                "OK");
            return;
        }

        Canvas canvas = manager.rootCanvas != null
            ? manager.rootCanvas
            : manager.GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Grid Adventure Tutorial",
                "The GridAdventureManager does not have a Canvas reference.",
                "OK");
            return;
        }

        GridAdventureFirstTimeTutorialController controller = FindSceneTutorialController();
        bool createdRoot = controller == null;

        RectTransform root;
        if (controller == null)
        {
            root = FindSceneRectByName(TutorialRootName);
            if (root == null)
            {
                root = CreateRect(TutorialRootName, canvas.transform, true);
                Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Grid Adventure Tutorial");
            }
            else if (root.parent != canvas.transform)
            {
                Undo.SetTransformParent(root, canvas.transform, "Move Grid Adventure Tutorial");
            }

            controller = root.GetComponent<GridAdventureFirstTimeTutorialController>();
            if (controller == null)
                controller = Undo.AddComponent<GridAdventureFirstTimeTutorialController>(root.gameObject);
        }
        else
        {
            root = controller.transform as RectTransform;
            if (root != null && root.parent != canvas.transform)
                Undo.SetTransformParent(root, canvas.transform, "Move Grid Adventure Tutorial");
        }

        if (root == null)
            return;

        root.SetAsLastSibling();

        CanvasGroup rootGroup = root.GetComponent<CanvasGroup>();
        if (rootGroup == null)
            rootGroup = Undo.AddComponent<CanvasGroup>(root.gameObject);
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;

        RectTransform blocker = EnsureRect(root, "Tutorial Dimmer And Continue", true);
        Image blockerImage = blocker.GetComponent<Image>();
        if (blockerImage == null)
            blockerImage = Undo.AddComponent<Image>(blocker.gameObject);
        blockerImage.color = new Color(0f, 0f, 0f, 0.16f);
        blockerImage.raycastTarget = true;

        Button blockerButton = blocker.GetComponent<Button>();
        if (blockerButton == null)
            blockerButton = Undo.AddComponent<Button>(blocker.gameObject);
        blockerButton.targetGraphic = blockerImage;
        blockerButton.transition = Selectable.Transition.None;

        RectTransform practiceLayer = EnsureRect(root, "Tutorial Practice Layer", true);

        RectTransform focus = EnsureRect(practiceLayer, "Tutorial Focus Highlight", false);
        Image focusImage = focus.GetComponent<Image>();
        if (focusImage == null)
            focusImage = Undo.AddComponent<Image>(focus.gameObject);
        focusImage.color = new Color(1f, 0.86f, 0.25f, 0.28f);
        focusImage.raycastTarget = false;
        focus.gameObject.SetActive(false);

        RectTransform instructionCard = EnsureRect(root, "Tutorial Instruction Card", false);
        if (instructionCard.sizeDelta == Vector2.zero)
        {
            instructionCard.anchorMin = new Vector2(0.5f, 1f);
            instructionCard.anchorMax = new Vector2(0.5f, 1f);
            instructionCard.pivot = new Vector2(0.5f, 1f);
            instructionCard.sizeDelta = new Vector2(860f, 112f);
            instructionCard.anchoredPosition = new Vector2(0f, -118f);
        }

        Image instructionBackground = instructionCard.GetComponent<Image>();
        if (instructionBackground == null)
            instructionBackground = Undo.AddComponent<Image>(instructionCard.gameObject);
        instructionBackground.color = new Color(1f, 0.93f, 0.70f, 0.98f);
        instructionBackground.raycastTarget = false;

        RectTransform textRect = EnsureRect(instructionCard, "Tutorial Instruction Text", true);
        textRect.offsetMin = new Vector2(30f, 14f);
        textRect.offsetMax = new Vector2(-30f, -14f);

        TextMeshProUGUI instructionText = textRect.GetComponent<TextMeshProUGUI>();
        if (instructionText == null)
            instructionText = Undo.AddComponent<TextMeshProUGUI>(textRect.gameObject);
        instructionText.text = "Read the clue.";
        instructionText.fontSize = 27f;
        instructionText.fontStyle = FontStyles.Bold;
        instructionText.alignment = TextAlignmentOptions.Center;
        instructionText.enableWordWrapping = true;
        instructionText.color = new Color(0.20f, 0.23f, 0.28f, 1f);
        instructionText.raycastTarget = false;
        if (instructionText.font == null && manager.primaryFontAsset != null)
            instructionText.font = manager.primaryFontAsset;

        GridAdventureTextFontRole fontRole = textRect.GetComponent<GridAdventureTextFontRole>();
        if (fontRole == null)
            fontRole = Undo.AddComponent<GridAdventureTextFontRole>(textRect.gameObject);
        fontRole.fontRole = GridAdventureFontRole.Primary;

        RectTransform handRect = EnsureRect(root, "Tutorial Hand Pointer", false);
        if (handRect.sizeDelta == Vector2.zero)
            handRect.sizeDelta = new Vector2(104f, 104f);

        Image handImage = handRect.GetComponent<Image>();
        if (handImage == null)
            handImage = Undo.AddComponent<Image>(handRect.gameObject);
        handImage.preserveAspect = true;
        handImage.raycastTarget = false;
        handRect.gameObject.SetActive(false);

        controller.manager = manager;
        controller.rootCanvas = canvas;
        controller.backgroundContinueButton = blockerButton;
        controller.dimmerImage = blockerImage;
        controller.practiceLayer = practiceLayer;
        controller.focusImage = focus;
        controller.instructionMotionRoot = instructionCard;
        controller.instructionText = instructionText;
        controller.handImage = handImage;
        controller.dragDemoDuration = Mathf.Max(controller.dragDemoDuration, 2.2f);
        controller.dragDemoStartHold = Mathf.Max(controller.dragDemoStartHold, 0.65f);
        controller.dragDemoTargetHold = Mathf.Max(controller.dragDemoTargetHold, 0.9f);
        controller.idleRepeatSeconds = Mathf.Max(controller.idleRepeatSeconds, 15f);
        controller.handScreenEdgePadding = Mathf.Max(controller.handScreenEdgePadding, 24f);
        if (controller.pointerPlacementVersion < 1)
        {
            controller.handTipNormalized = new Vector2(0f, 1f);
            controller.clueHandOffset = new Vector2(0f, 8f);
            controller.clueHandRotation = 180f;
            controller.hintHandOffset = Vector2.zero;
            controller.hintHandRotation = -90f;
            controller.pointerPlacementVersion = 1;
        }
        if (controller.successStageVersion < 1)
        {
            controller.successInstruction =
                "You have successfully completed the tutorial!\nClick anywhere to start the game.";
            controller.successStageVersion = 1;
        }

        if (createdRoot)
        {
            manager.howToPlayStartupMode = manager.showHowToPlayOnStart
                ? GridAdventureHowToPlayStartupMode.FirstTimeAutomatically
                : GridAdventureHowToPlayStartupMode.ManualButtonOnly;
        }

        manager.firstTimeTutorialController = controller;

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        root.gameObject.SetActive(false);
        Selection.activeGameObject = root.gameObject;

        Debug.Log(
            "Grid Adventure first-time tutorial installed/upgraded additively. " +
            "Assign a hand sprite on Tutorial Hand Pointer, then test with Force Play For Testing.",
            controller);
    }

    [MenuItem("Tools/Grid Adventure/Install or Upgrade First-Time Tutorial", true)]
    private static bool ValidateInstallOrUpgrade()
    {
        return !Application.isPlaying;
    }

    private static GridAdventureFirstTimeTutorialController FindSceneTutorialController()
    {
        GridAdventureFirstTimeTutorialController[] controllers =
            Resources.FindObjectsOfTypeAll<GridAdventureFirstTimeTutorialController>();

        for (int i = 0; i < controllers.Length; i++)
        {
            GridAdventureFirstTimeTutorialController controller = controllers[i];
            if (controller == null || EditorUtility.IsPersistent(controller))
                continue;

            if (controller.gameObject.scene == SceneManager.GetActiveScene())
                return controller;
        }

        return null;
    }

    private static RectTransform FindSceneRectByName(string objectName)
    {
        RectTransform[] rects = Resources.FindObjectsOfTypeAll<RectTransform>();
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];
            if (rect == null || EditorUtility.IsPersistent(rect))
                continue;

            if (rect.gameObject.scene == SceneManager.GetActiveScene() && rect.name == objectName)
                return rect;
        }

        return null;
    }

    private static RectTransform EnsureRect(RectTransform parent, string name, bool stretch)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing as RectTransform;

        RectTransform rect = CreateRect(name, parent, stretch);
        Undo.RegisterCreatedObjectUndo(rect.gameObject, "Create " + name);
        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent, bool stretch)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        return rect;
    }
}
#endif
