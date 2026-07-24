#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EmotionTimerQuiz.EditorTools
{
    public static class EmotionTimerQuizFirstTimeTutorialInstaller
    {
        private const string TutorialRootName = "EmotionTimerQuiz_FirstTimeTutorial";
        private const int CurrentPositioningDefaultsVersion = 7;

        [MenuItem("Tools/Emotion Timer Quiz/First-Time Tutorial/Install or Upgrade In Open Scene")]
        public static void InstallOrUpgradeInOpenScene()
        {
            EmotionTimerQuizManager manager = FindSceneManager();
            if (manager == null)
            {
                EditorUtility.DisplayDialog(
                    "Emotion Timer Quiz Manager Not Found",
                    "Open the game scene containing EmotionTimerQuizManager, then run this installer again.",
                    "OK");
                return;
            }

            Canvas canvas = manager.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = Object.FindObjectOfType<Canvas>();
            }

            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Canvas Not Found",
                    "The open scene does not contain a Canvas for the tutorial UI.",
                    "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Install Emotion Timer Quiz First-Time Tutorial");

            GameObject root = FindDirectChild(canvas.transform, TutorialRootName);
            bool createdRoot = root == null;
            if (createdRoot)
            {
                root = new GameObject(TutorialRootName, typeof(RectTransform), typeof(CanvasGroup));
                Undo.RegisterCreatedObjectUndo(root, "Create tutorial root");
                root.transform.SetParent(canvas.transform, false);
                Stretch(root.GetComponent<RectTransform>());
            }

            CanvasGroup canvasGroup = GetOrAddComponent<CanvasGroup>(root);
            EmotionFirstTimeTutorialController controller = GetOrAddComponent<EmotionFirstTimeTutorialController>(root);

            Image dimOverlay = EnsureDimOverlay(root.transform);
            RectTransform promptPanel = EnsurePromptPanel(root.transform);
            TextMeshProUGUI instructionText = EnsureInstructionText(promptPanel);
            Image handPointer = EnsureHandPointer(root.transform);
            Button clickCatcher = EnsureClickCatcher(root.transform);

            Undo.RecordObject(controller, "Assign tutorial references");
            controller.gameManager = manager;
            controller.tutorialCanvasGroup = canvasGroup;
            controller.dimOverlay = dimOverlay;
            controller.promptPanel = promptPanel;
            controller.instructionText = instructionText;
            controller.handPointer = handPointer;
            controller.clickCatcher = clickCatcher;
            ApplyPositioningDefaultsUpgrade(controller);

            Undo.RecordObject(manager, "Assign first-time tutorial controller");
            manager.firstTimeTutorialController = controller;

            if (manager.howToPlayButton == null)
            {
                Transform optionalHowToPlayButton = FindDescendantByExactName(canvas.transform, "HowToPlayButton", root.transform);
                if (optionalHowToPlayButton != null)
                {
                    manager.howToPlayButton = optionalHowToPlayButton.GetComponent<Button>();
                }
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            dimOverlay.gameObject.SetActive(false);
            promptPanel.gameObject.SetActive(false);
            handPointer.gameObject.SetActive(false);
            clickCatcher.gameObject.SetActive(false);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog(
                "First-Time Tutorial Ready",
                createdRoot
                    ? "The tutorial was added under " + TutorialRootName + ". Assign your hand sprite, then review the per-step pointer offsets and instruction prompt layout in the tutorial Inspector."
                    : "The existing tutorial installation was upgraded without rebuilding the scene. Missing tutorial-owned elements were added and references were refreshed.",
                "OK");
        }

        [MenuItem("Tools/Emotion Timer Quiz/First-Time Tutorial/Select Tutorial Root")]
        public static void SelectTutorialRoot()
        {
            EmotionTimerQuizManager manager = FindSceneManager();
            Canvas canvas = manager != null ? manager.GetComponentInParent<Canvas>() : Object.FindObjectOfType<Canvas>();
            GameObject root = canvas != null ? FindDirectChild(canvas.transform, TutorialRootName) : null;
            if (root != null)
            {
                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);
            }
            else
            {
                EditorUtility.DisplayDialog("Tutorial Not Installed", "Run Install or Upgrade In Open Scene first.", "OK");
            }
        }

        private static EmotionTimerQuizManager FindSceneManager()
        {
            EmotionTimerQuizManager[] managers = Resources.FindObjectsOfTypeAll<EmotionTimerQuizManager>();
            for (int i = 0; i < managers.Length; i++)
            {
                EmotionTimerQuizManager candidate = managers[i];
                if (candidate != null && !EditorUtility.IsPersistent(candidate) && candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void ApplyPositioningDefaultsUpgrade(EmotionFirstTimeTutorialController controller)
        {
            if (controller == null || controller.positioningDefaultsVersion >= CurrentPositioningDefaultsVersion)
            {
                return;
            }

            controller.situationTargetAnchor = new Vector2(0.5f, 0.30f);
            controller.situationHandOffset = Vector2.zero;
            controller.firstCardTargetAnchor = new Vector2(0.65f, 0.5f);
            controller.firstCardHandOffset = Vector2.zero;
            controller.secondCardTargetAnchor = new Vector2(0.65f, 0.5f);
            controller.secondCardHandOffset = Vector2.zero;
            controller.thirdCardTargetAnchor = new Vector2(0.65f, 0.5f);
            controller.thirdCardHandOffset = Vector2.zero;
            controller.correctCardTargetAnchor = new Vector2(0.65f, 0.5f);
            controller.correctCardHandOffset = Vector2.zero;
            controller.nextButtonTargetAnchor = new Vector2(0.5f, 0.20f);
            controller.nextButtonHandOffset = new Vector2(0f, -20f);
            controller.promptSize = new Vector2(620f, 180f);
            controller.autoPositionPromptToAvoidGameplay = true;
            controller.promptMoveDuration = 0.25f;
            controller.handPointerTipNormalized = new Vector2(0.25f, 0.82f);
            controller.positioningDefaultsVersion = CurrentPositioningDefaultsVersion;
        }

        private static Image EnsureDimOverlay(Transform parent)
        {
            GameObject obj = FindDirectChild(parent, "TutorialDimOverlay");
            bool created = obj == null;
            if (created)
            {
                obj = CreateUIObject("TutorialDimOverlay", parent);
                Stretch(obj.GetComponent<RectTransform>());
            }

            Image image = GetOrAddComponent<Image>(obj);
            if (created)
            {
                image.color = new Color(0f, 0f, 0f, 0.18f);
                image.raycastTarget = false;
            }

            return image;
        }

        private static RectTransform EnsurePromptPanel(Transform parent)
        {
            GameObject obj = FindDirectChild(parent, "TutorialInstructionPrompt");
            bool created = obj == null;
            if (created)
            {
                obj = CreateUIObject("TutorialInstructionPrompt", parent);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 55f);
                rect.sizeDelta = new Vector2(620f, 180f);
            }

            Image image = GetOrAddComponent<Image>(obj);
            if (created)
            {
                image.color = new Color(0.08f, 0.13f, 0.22f, 0.94f);
                image.raycastTarget = false;
            }

            return obj.GetComponent<RectTransform>();
        }

        private static TextMeshProUGUI EnsureInstructionText(RectTransform promptPanel)
        {
            GameObject obj = FindDirectChild(promptPanel, "TutorialInstructionText");
            bool created = obj == null;
            if (created)
            {
                obj = CreateUIObject("TutorialInstructionText", promptPanel);
                RectTransform rect = obj.GetComponent<RectTransform>();
                Stretch(rect);
                rect.offsetMin = new Vector2(45f, 20f);
                rect.offsetMax = new Vector2(-45f, -20f);
            }

            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(obj);
            if (created)
            {
                text.text = "Tutorial instruction";
                text.fontSize = 32f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 20f;
                text.fontSizeMax = 34f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.enableWordWrapping = true;
                text.raycastTarget = false;
            }

            return text;
        }

        private static Image EnsureHandPointer(Transform parent)
        {
            GameObject obj = FindDirectChild(parent, "HandPointerImage");
            bool created = obj == null;
            if (created)
            {
                obj = CreateUIObject("HandPointerImage", parent);
                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(140f, 140f);
            }

            Image image = GetOrAddComponent<Image>(obj);
            if (created)
            {
                image.sprite = null;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.enabled = false;
            }

            return image;
        }

        private static Button EnsureClickCatcher(Transform parent)
        {
            GameObject obj = FindDirectChild(parent, "TutorialClickCatcher");
            bool created = obj == null;
            if (created)
            {
                obj = CreateUIObject("TutorialClickCatcher", parent);
                Stretch(obj.GetComponent<RectTransform>());
            }

            Image image = GetOrAddComponent<Image>(obj);
            Button button = GetOrAddComponent<Button>(obj);
            if (created)
            {
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = true;
                button.transition = Selectable.Transition.None;
            }

            obj.transform.SetAsLastSibling();
            return button;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(obj);
        }

        private static GameObject FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static Transform FindDescendantByExactName(Transform root, string objectName, Transform excludedRoot)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == excludedRoot)
                {
                    continue;
                }

                if (child.name == objectName)
                {
                    return child;
                }

                Transform nested = FindDescendantByExactName(child, objectName, excludedRoot);
                if (nested != null)
                {
                    return nested;
                }
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
}
#endif
