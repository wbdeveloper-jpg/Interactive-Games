#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OddClawImageModeFeatureInstaller
{
    private const string RootName = "OddClawCatch_ImageModeFeatures";
    private const string ImageTextTemplateName = "ImageTextItemTemplate";
    private const string InstallMenu =
        "Tools/Odd Claw Catch/Image Mode Features/Install Or Upgrade In Open Scene";
    private const string SelectMenu =
        "Tools/Odd Claw Catch/Image Mode Features/Select Feature Root";
    private const string ResetMenu =
        "Tools/Odd Claw Catch/Image Mode Features/Reset Enlarged-Image Hint For Open Scene";

    [MenuItem(InstallMenu)]
    public static void InstallOrUpgradeInOpenScene()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog(
                "Odd Claw Image Mode Features",
                "No OddClawCatchManager was found in the open scene.",
                "OK");
            return;
        }

        Canvas canvas = manager.rootCanvas != null
            ? manager.rootCanvas
            : manager.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog(
                "Odd Claw Image Mode Features",
                "The manager has no Root Canvas reference. Assign the existing game Canvas first.",
                "OK");
            return;
        }

        Undo.SetCurrentGroupName("Install Odd Claw Image Mode Features");
        int undoGroup = Undo.GetCurrentGroup();

        RectTransform root = FindChildRecursive(canvas.transform, RootName);
        bool createdRoot = root == null;
        if (createdRoot)
        {
            GameObject rootObject = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootObject, "Create Odd Claw Image Feature Root");
            root = rootObject.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Stretch(root);
        }
        root.gameObject.SetActive(true);
        root.SetAsLastSibling();

        OddClawImageModeFeatureController feature =
            GetOrAddComponent<OddClawImageModeFeatureController>(root.gameObject);

        RectTransform previewRoot = GetOrCreateRect("EnlargedImagePreview", root, out bool previewCreated);
        Stretch(previewRoot);
        CanvasGroup previewGroup = GetOrAddComponent<CanvasGroup>(previewRoot.gameObject);
        Image previewBackdrop = GetOrAddComponent<Image>(previewRoot.gameObject);
        if (previewCreated)
        {
            previewBackdrop.color = new Color(0.88f, 0.95f, 1f, 0.72f);
            previewBackdrop.raycastTarget = true;
            previewGroup.alpha = 0f;
            previewGroup.blocksRaycasts = false;
            previewGroup.interactable = false;
        }

        RectTransform previewCard = GetOrCreateRect("ImageFrame", previewRoot, out bool cardCreated);
        Image cardImage = GetOrAddComponent<Image>(previewCard.gameObject);
        Outline cardOutline = GetOrAddComponent<Outline>(previewCard.gameObject);
        if (cardCreated)
        {
            Centre(previewCard, new Vector2(760f, 560f), Vector2.zero);
            cardImage.color = new Color(1f, 1f, 1f, 0.99f);
            cardImage.raycastTarget = true;
            cardOutline.effectColor = new Color(0.08f, 0.3f, 0.55f, 0.35f);
            cardOutline.effectDistance = new Vector2(0f, -6f);
        }

        RectTransform imageRect = GetOrCreateRect("EnlargedImage", previewCard, out bool imageCreated);
        Image enlargedImage = GetOrAddComponent<Image>(imageRect.gameObject);
        if (imageCreated)
        {
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(34f, 34f);
            imageRect.offsetMax = new Vector2(-34f, -78f);
            enlargedImage.color = Color.white;
            enlargedImage.preserveAspect = true;
            enlargedImage.raycastTarget = false;
        }

        Button closeButton = GetOrCreateButton(
            "ClosePreviewButton",
            previewCard,
            "CLOSE",
            new Color(0.13f, 0.43f, 0.72f, 1f),
            out RectTransform closeRect,
            out bool closeCreated,
            manager.primaryFont);
        if (closeCreated)
        {
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(210f, 62f);
            closeRect.anchoredPosition = new Vector2(0f, 12f);
        }

        RectTransform hintPanel = GetOrCreateRect("ImagePreviewHint", root, out bool hintCreated);
        CanvasGroup hintGroup = GetOrAddComponent<CanvasGroup>(hintPanel.gameObject);
        Image hintBackground = GetOrAddComponent<Image>(hintPanel.gameObject);
        Outline hintOutline = GetOrAddComponent<Outline>(hintPanel.gameObject);
        if (hintCreated)
        {
            hintPanel.anchorMin = new Vector2(0.5f, 0f);
            hintPanel.anchorMax = new Vector2(0.5f, 0f);
            hintPanel.pivot = new Vector2(0.5f, 0f);
            hintPanel.sizeDelta = new Vector2(620f, 92f);
            hintPanel.anchoredPosition = new Vector2(0f, 210f);
            hintBackground.color = new Color(1f, 1f, 1f, 0.97f);
            hintBackground.raycastTarget = false;
            hintOutline.effectColor = new Color(0.08f, 0.3f, 0.55f, 0.3f);
            hintOutline.effectDistance = new Vector2(0f, -4f);
            hintGroup.alpha = 0f;
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;
        }

        TMP_Text hintText = GetOrCreateText(
            "HintText",
            hintPanel,
            "Tap any picture to see it bigger.",
            30f,
            TextAlignmentOptions.Center,
            manager.primaryFont,
            out _);
        hintText.rectTransform.anchorMin = Vector2.zero;
        hintText.rectTransform.anchorMax = Vector2.one;
        hintText.rectTransform.offsetMin = new Vector2(24f, 12f);
        hintText.rectTransform.offsetMax = new Vector2(-24f, -12f);
        hintText.color = new Color(0.05f, 0.16f, 0.28f, 1f);
        hintText.enableWordWrapping = true;
        hintText.raycastTarget = false;

        RectTransform pointer = GetOrCreateRect("ImageHintPointer", root, out bool pointerCreated);
        Image pointerImage = GetOrAddComponent<Image>(pointer.gameObject);
        if (pointerCreated)
        {
            Centre(pointer, new Vector2(105f, 105f), Vector2.zero);
            pointerImage.color = Color.white;
            pointerImage.preserveAspect = true;
            pointerImage.raycastTarget = false;
            pointerImage.enabled = true;
            pointer.gameObject.SetActive(false);
        }

        RectTransform magnetSocket = null;
        if (manager.clawController != null && manager.clawController.clawHead != null)
        {
            magnetSocket = FindDirectChild(
                manager.clawController.clawHead,
                "ImageMagnetGrabSocket");
            if (magnetSocket == null)
            {
                GameObject socketObject = new GameObject("ImageMagnetGrabSocket", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(socketObject, "Create Image Magnet Grab Socket");
                magnetSocket = socketObject.GetComponent<RectTransform>();
                magnetSocket.SetParent(manager.clawController.clawHead, false);
                magnetSocket.anchorMin = new Vector2(0.5f, 0.5f);
                magnetSocket.anchorMax = new Vector2(0.5f, 0.5f);
                magnetSocket.pivot = new Vector2(0.5f, 0.5f);
                magnetSocket.sizeDelta = new Vector2(20f, 20f);
                magnetSocket.anchoredPosition = new Vector2(0f, -45f);
            }
        }

        OddClawItemView imageTextTemplate = GetOrCreateImageTextTemplate(
            manager,
            out bool imageTextTemplateCreated);
        if (manager.imageTextItemTemplate == null && imageTextTemplate != null)
        {
            Undo.RecordObject(manager, "Assign Image Text Item Template");
            manager.imageTextItemTemplate = imageTextTemplate;
            EditorUtility.SetDirty(manager);
        }

        Undo.RecordObject(feature, "Assign Odd Claw Image Feature References");
        AssignIfMissing(ref feature.gameManager, manager);
        AssignIfMissing(ref feature.clawController, manager.clawController);
        AssignIfMissing(ref feature.rootCanvas, canvas);
        AssignIfMissing(ref feature.previewRoot, previewRoot.gameObject);
        AssignIfMissing(ref feature.previewCanvasGroup, previewGroup);
        AssignIfMissing(ref feature.previewCard, previewCard);
        AssignIfMissing(ref feature.enlargedImage, enlargedImage);
        AssignIfMissing(ref feature.previewCloseButton, closeButton);
        AssignIfMissing(ref feature.imageHintCanvasGroup, hintGroup);
        AssignIfMissing(ref feature.imageHintText, hintText);
        AssignIfMissing(ref feature.imageHintPointer, pointer);
        AssignIfMissing(ref feature.imageHintPointerImage, pointerImage);
        EditorUtility.SetDirty(feature);

        if (manager.imageModeFeatures == null)
        {
            Undo.RecordObject(manager, "Assign Odd Claw Image Features");
            manager.imageModeFeatures = feature;
            EditorUtility.SetDirty(manager);
        }
        else if (manager.imageModeFeatures != feature)
        {
            Debug.LogWarning(
                "OddClawCatchManager already references a different image feature controller. " +
                "The installer kept the existing reference unchanged.",
                manager);
        }

        if (manager.clawController != null && magnetSocket != null
            && manager.clawController.imageMagnetGrabSocket == null)
        {
            Undo.RecordObject(manager.clawController, "Assign Image Magnet Socket");
            manager.clawController.imageMagnetGrabSocket = magnetSocket;
            EditorUtility.SetDirty(manager.clawController);
        }

        previewRoot.gameObject.SetActive(false);
        hintPanel.gameObject.SetActive(false);
        pointer.gameObject.SetActive(false);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = imageTextTemplateCreated && imageTextTemplate != null
            ? imageTextTemplate.gameObject
            : root.gameObject;

        EditorUtility.DisplayDialog(
            "Odd Claw Image Mode Features",
            BuildCompletionMessage(createdRoot, imageTextTemplateCreated, imageTextTemplate != null),
            "OK");
    }

    [MenuItem(SelectMenu)]
    public static void SelectFeatureRoot()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        Canvas canvas = manager != null ? manager.rootCanvas : null;
        RectTransform root = canvas != null ? FindChildRecursive(canvas.transform, RootName) : null;
        if (root != null)
            Selection.activeGameObject = root.gameObject;
        else
            EditorUtility.DisplayDialog("Odd Claw Image Mode Features", "Feature root not found.", "OK");
    }

    [MenuItem(ResetMenu)]
    public static void ResetHintForOpenScene()
    {
        OddClawCatchManager manager = Object.FindObjectOfType<OddClawCatchManager>();
        OddClawImageModeFeatureController feature = manager != null
            ? manager.imageModeFeatures
            : Object.FindObjectOfType<OddClawImageModeFeatureController>();
        if (feature == null)
        {
            EditorUtility.DisplayDialog("Odd Claw Image Mode Features", "Feature controller not found.", "OK");
            return;
        }

        feature.ResetImagePreviewHintForThisScene();
        EditorUtility.DisplayDialog(
            "Odd Claw Image Mode Features",
            "The enlarged-image guidance will appear again for this scene.",
            "OK");
    }

    private static OddClawItemView GetOrCreateImageTextTemplate(
        OddClawCatchManager manager,
        out bool created)
    {
        created = false;
        if (manager == null)
            return null;

        if (manager.imageTextItemTemplate != null)
        {
            manager.imageTextItemTemplate.gameObject.SetActive(false);
            return manager.imageTextItemTemplate;
        }

        if (manager.itemContainer == null || manager.imageItemTemplate == null)
        {
            Debug.LogWarning(
                "The Image Text Item Template could not be created because the manager has no "
                + "Item Container or Image Item Template. Existing games are unchanged; assign those "
                + "references and rerun the additive installer when the combined mode is needed.",
                manager);
            return null;
        }

        Transform templateParent = manager.imageItemTemplate.transform.parent != null
            ? manager.imageItemTemplate.transform.parent
            : manager.itemContainer;
        RectTransform existing = FindDirectChild(templateParent, ImageTextTemplateName);
        if (existing != null)
        {
            OddClawItemView existingView = existing.GetComponent<OddClawItemView>();
            if (existingView == null)
            {
                Debug.LogWarning(
                    ImageTextTemplateName + " exists but has no OddClawItemView component. "
                    + "The installer left it untouched.",
                    existing);
                return null;
            }

            existingView.gameObject.SetActive(false);
            return existingView;
        }

        GameObject clone = Object.Instantiate(
            manager.imageItemTemplate.gameObject,
            templateParent,
            false);
        clone.name = ImageTextTemplateName;
        Undo.RegisterCreatedObjectUndo(clone, "Create Image Text Item Template");

        OddClawItemView view = clone.GetComponent<OddClawItemView>();
        if (view == null)
        {
            Debug.LogWarning(
                "The existing Image Item Template has no OddClawItemView component, so the "
                + "combined template could not be prepared.",
                manager.imageItemTemplate);
            return null;
        }

        ConfigureNewImageTextTemplate(view);
        clone.SetActive(false);
        EditorUtility.SetDirty(view);
        created = true;
        return view;
    }

    private static void ConfigureNewImageTextTemplate(OddClawItemView view)
    {
        RectTransform root = view.root != null
            ? view.root
            : view.transform as RectTransform;
        if (root != null)
        {
            Vector2 size = root.sizeDelta;
            size.y = Mathf.Max(128f, size.y) + 42f;
            root.sizeDelta = size;

            LayoutElement layout = root.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = size.x;
                layout.preferredHeight = size.y;
            }
        }

        if (view.answerImage != null)
        {
            RectTransform imageRect = view.answerImage.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = new Vector2(12f, 46f);
            imageRect.offsetMax = new Vector2(-12f, -10f);
            view.answerImage.preserveAspect = true;
        }

        if (view.answerText != null)
        {
            RectTransform textRect = view.answerText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot = new Vector2(0.5f, 0f);
            textRect.offsetMin = new Vector2(8f, 4f);
            textRect.offsetMax = new Vector2(-8f, 42f);

            view.answerText.text = "Label";
            view.answerText.alignment = TextAlignmentOptions.Center;
            view.answerText.enableWordWrapping = true;
            view.answerText.enableAutoSizing = true;
            view.answerText.fontSizeMin = 13f;
            view.answerText.fontSizeMax = 24f;
            view.answerText.raycastTarget = false;
        }
    }

    private static string BuildCompletionMessage(
        bool createdRoot,
        bool createdImageTextTemplate,
        bool hasImageTextTemplate)
    {
        string featureMessage = createdRoot
            ? "Image mode features were installed additively."
            : "Image mode features were upgraded additively.";

        if (createdImageTextTemplate)
        {
            return featureMessage
                + " A separate Image Text Item Template was cloned from the existing image template "
                + "and assigned. Adjust only that new template if desired; the existing templates were not changed.";
        }

        if (hasImageTextTemplate)
            return featureMessage + " The existing Image Text Item Template was kept unchanged.";

        return featureMessage
            + " The combined template was not created because its source references are missing. "
            + "Existing games remain unchanged.";
    }

    private static RectTransform GetOrCreateRect(
        string name,
        RectTransform parent,
        out bool created)
    {
        RectTransform existing = FindDirectChild(parent, name);
        if (existing != null)
        {
            created = false;
            return existing;
        }

        GameObject child = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, "Create " + name);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        created = true;
        return rect;
    }

    private static Button GetOrCreateButton(
        string name,
        RectTransform parent,
        string label,
        Color color,
        out RectTransform rect,
        out bool created,
        TMP_FontAsset font)
    {
        rect = GetOrCreateRect(name, parent, out created);
        Image image = GetOrAddComponent<Image>(rect.gameObject);
        Button button = GetOrAddComponent<Button>(rect.gameObject);
        image.color = color;
        image.raycastTarget = true;
        button.targetGraphic = image;

        TMP_Text text = GetOrCreateText(
            "Text",
            rect,
            label,
            25f,
            TextAlignmentOptions.Center,
            font,
            out _);
        Stretch(text.rectTransform);
        text.color = Color.white;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        return button;
    }

    private static TMP_Text GetOrCreateText(
        string name,
        RectTransform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        TMP_FontAsset font,
        out bool created)
    {
        RectTransform rect = GetOrCreateRect(name, parent, out created);
        TMP_Text text = GetOrAddComponent<TextMeshProUGUI>(rect.gameObject);
        if (created)
        {
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            if (font != null)
                text.font = font;
        }
        return text;
    }

    private static T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component != null)
            return component;
        return Undo.AddComponent<T>(target);
    }

    private static void AssignIfMissing<T>(ref T destination, T value) where T : Object
    {
        if (destination == null)
            destination = value;
    }

    private static RectTransform FindDirectChild(Transform parent, string name)
    {
        if (parent == null)
            return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child as RectTransform;
        }
        return null;
    }

    private static RectTransform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null)
            return null;
        if (parent.name == name)
            return parent as RectTransform;
        for (int i = 0; i < parent.childCount; i++)
        {
            RectTransform found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Centre(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }
}
#endif
