#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SkyFallFirstCatchTutorialInstaller
{
    private const string TutorialRootName = "FirstCatchTutorialOverlay";
    private const string HandImageName = "TutorialHandImage_REPLACE_SPRITE";
    private const string InstructionTextName = "TutorialInstructionText";

    [MenuItem("Tools/SkyFall/Install First Catch Tutorial In Open Scene")]
    public static void InstallInOpenScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        SkyFallGameManager manager = FindInScene<SkyFallGameManager>(scene);

        if (manager == null)
        {
            Debug.LogError("SkyFall tutorial installer: No SkyFallGameManager was found in the open scene.");
            return;
        }

        SkyFallFirstCatchTutorial tutorial = FindInScene<SkyFallFirstCatchTutorial>(scene);

        if (tutorial == null)
        {
            Transform parent = FindNamedTransform(scene, "OverlayLayer");

            if (parent == null)
            {
                Canvas canvas = manager.playArea != null
                    ? manager.playArea.GetComponentInParent<Canvas>()
                    : manager.GetComponentInParent<Canvas>();

                if (canvas != null)
                    parent = canvas.transform;
            }

            if (parent == null)
            {
                Debug.LogError("SkyFall tutorial installer: OverlayLayer or a parent Canvas could not be found.");
                return;
            }

            GameObject rootObject = new GameObject(
                TutorialRootName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(SkyFallFirstCatchTutorial)
            );

            Undo.RegisterCreatedObjectUndo(rootObject, "Install SkyFall First Catch Tutorial");
            rootObject.transform.SetParent(parent, false);

            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            StretchToParent(rootRect);
            rootRect.SetAsFirstSibling();

            tutorial = rootObject.GetComponent<SkyFallFirstCatchTutorial>();
        }

        Undo.RecordObject(tutorial, "Configure SkyFall First Catch Tutorial");

        RectTransform tutorialRoot = tutorial.transform as RectTransform;
        StretchToParent(tutorialRoot);
        tutorialRoot.SetAsFirstSibling();

        CanvasGroup canvasGroup = tutorial.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = Undo.AddComponent<CanvasGroup>(tutorial.gameObject);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Image handImage = GetOrCreateHandImage(tutorialRoot);
        TMP_Text instructionText = GetOrCreateInstructionText(tutorialRoot, manager);

        tutorial.gameManager = manager;
        tutorial.basket = manager.basket;
        tutorial.overlayRoot = tutorialRoot;
        tutorial.questionTarget = manager.questionText != null ? manager.questionText.rectTransform : null;
        tutorial.handImage = handImage;
        tutorial.instructionText = instructionText;
        tutorial.readQuestionMessage = "Read the question. Tap anywhere to continue.";
        tutorial.instructionMessage = "Hold and drag the basket left and right!";
        tutorial.catchInstructionMessage = "Catch the correct answer!";
        tutorial.successMessage = "Great! Now catch the correct answers!";

        SkyFallFontThemeApplier fontTheme = FindInScene<SkyFallFontThemeApplier>(scene);
        if (fontTheme != null)
        {
            Undo.RecordObject(fontTheme, "Add Tutorial Text To SkyFall Font Theme");

            if (fontTheme.primaryTexts == null)
                fontTheme.primaryTexts = new System.Collections.Generic.List<TMP_Text>();

            if (!fontTheme.primaryTexts.Contains(instructionText))
                fontTheme.primaryTexts.Add(instructionText);

            fontTheme.ApplyFonts();
            EditorUtility.SetDirty(fontTheme);
        }

        EditorUtility.SetDirty(tutorial);
        EditorUtility.SetDirty(tutorial.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = tutorial.gameObject;

        Debug.Log(
            "SkyFall first-catch tutorial installed. Select '" + TutorialRootName +
            "', then assign your hand sprite to '" + HandImageName + "'."
        );
    }

    [MenuItem("Tools/SkyFall/Reset First Catch Tutorial For Testing")]
    public static void ResetForTesting()
    {
        SkyFallFirstCatchTutorial tutorial = FindInScene<SkyFallFirstCatchTutorial>(SceneManager.GetActiveScene());

        if (tutorial == null)
        {
            Debug.LogWarning("SkyFall tutorial reset: Install the tutorial in the open scene first.");
            return;
        }

        tutorial.ResetTutorialCompletion();
        Debug.Log("SkyFall first-catch tutorial completion was reset for the open scene.");
    }

    private static Image GetOrCreateHandImage(RectTransform parent)
    {
        Transform existing = parent.Find(HandImageName);
        Image image;

        if (existing != null)
        {
            image = existing.GetComponent<Image>();
            if (image == null)
                image = Undo.AddComponent<Image>(existing.gameObject);
        }
        else
        {
            GameObject handObject = new GameObject(
                HandImageName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

            Undo.RegisterCreatedObjectUndo(handObject, "Create Tutorial Hand Image");
            handObject.transform.SetParent(parent, false);
            image = handObject.GetComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(130f, 130f);
        rect.anchoredPosition = Vector2.zero;

        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = image.sprite != null;

        return image;
    }

    private static TMP_Text GetOrCreateInstructionText(RectTransform parent, SkyFallGameManager manager)
    {
        Transform existing = parent.Find(InstructionTextName);
        TextMeshProUGUI text;

        if (existing != null)
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = Undo.AddComponent<TextMeshProUGUI>(existing.gameObject);
        }
        else
        {
            GameObject textObject = new GameObject(
                InstructionTextName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

            Undo.RegisterCreatedObjectUndo(textObject, "Create Tutorial Instruction Text");
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 90f);
        rect.anchoredPosition = Vector2.zero;

        text.text = "Read the question. Tap anywhere to continue.";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 42f;
        text.enableAutoSizing = true;
        text.fontSizeMin = 26f;
        text.fontSizeMax = 42f;
        text.color = Color.white;
        text.outlineColor = new Color(0.08f, 0.12f, 0.22f, 0.9f);
        text.outlineWidth = 0.22f;
        text.raycastTarget = false;

        if (manager.questionText != null && manager.questionText.font != null)
            text.font = manager.questionText.font;

        return text;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        T[] all = Resources.FindObjectsOfTypeAll<T>();

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene == scene)
                return all[i];
        }

        return null;
    }

    private static Transform FindNamedTransform(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindNamedTransformRecursive(roots[i].transform, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindNamedTransformRecursive(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindNamedTransformRecursive(current.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
#endif
