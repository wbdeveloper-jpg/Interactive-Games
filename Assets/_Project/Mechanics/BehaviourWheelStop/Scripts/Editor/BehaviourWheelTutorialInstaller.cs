#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BehaviourWheelStop.Editor
{
    public static class BehaviourWheelTutorialInstaller
    {
        private const string TutorialRootName = "[BehaviourWheel First-Time Tutorial]";

        [MenuItem("Tools/Behaviour Wheel Stop/Install or Upgrade First-Time Tutorial")]
        public static void InstallOrUpgrade()
        {
            BehaviourWheelGameManager manager = UnityEngine.Object.FindObjectOfType<BehaviourWheelGameManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Tutorial",
                    "No BehaviourWheelGameManager was found in the open scene.", "OK");
                return;
            }

            Canvas canvas = manager.ui != null ? manager.ui.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
                canvas = UnityEngine.Object.FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Tutorial",
                    "No Canvas was found in the open scene.", "OK");
                return;
            }

            BehaviourWheelFirstTimeTutorial tutorial = FindSceneTutorial();
            bool createdRoot = false;
            if (tutorial == null)
            {
                Transform existingRoot = canvas.transform.Find(TutorialRootName);
                GameObject rootObject;
                if (existingRoot != null)
                {
                    rootObject = existingRoot.gameObject;
                }
                else
                {
                    rootObject = new GameObject(TutorialRootName, typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(rootObject, "Create Behaviour Wheel Tutorial");
                    rootObject.transform.SetParent(canvas.transform, false);
                    createdRoot = true;
                }

                tutorial = GetOrAddComponent<BehaviourWheelFirstTimeTutorial>(rootObject);
            }

            RectTransform root = tutorial.transform as RectTransform;
            if (root == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Tutorial",
                    "The tutorial root must use a RectTransform.", "OK");
                return;
            }

            if (createdRoot)
            {
                StretchFull(root);
                root.SetAsLastSibling();
            }

            CanvasGroup rootGroup = GetOrAddComponent<CanvasGroup>(tutorial.gameObject);
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;

            bool instructionCreated;
            RectTransform instructionPanel = GetOrCreateRect(root, "Instruction Panel", out instructionCreated);
            Image instructionBackground = GetOrAddComponent<Image>(instructionPanel.gameObject);
            CanvasGroup instructionGroup = GetOrAddComponent<CanvasGroup>(instructionPanel.gameObject);
            instructionGroup.interactable = false;
            instructionGroup.blocksRaycasts = false;
            instructionPanel.anchorMin = new Vector2(0.5f, 0.5f);
            instructionPanel.anchorMax = new Vector2(0.5f, 0.5f);
            instructionPanel.pivot = new Vector2(0.5f, 0.5f);
            instructionPanel.sizeDelta = new Vector2(480f, 200f);
            if (instructionCreated)
            {
                instructionBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                instructionBackground.type = Image.Type.Sliced;
                instructionBackground.color = new Color(0.075f, 0.15f, 0.25f, 0.96f);
                instructionBackground.raycastTarget = false;
            }

            bool textCreated;
            RectTransform textRect = GetOrCreateRect(instructionPanel, "Instruction Text", out textCreated);
            TextMeshProUGUI instructionText = GetOrAddComponent<TextMeshProUGUI>(textRect.gameObject);
            if (textCreated)
            {
                StretchFull(textRect);
                textRect.offsetMin = new Vector2(22f, 16f);
                textRect.offsetMax = new Vector2(-22f, -16f);
                instructionText.alignment = TextAlignmentOptions.Center;
                instructionText.color = Color.white;
                instructionText.fontSize = 36f;
                instructionText.enableAutoSizing = true;
                instructionText.fontSizeMin = 26f;
                instructionText.fontSizeMax = 38f;
                instructionText.enableWordWrapping = true;
                instructionText.raycastTarget = false;
                if (manager.ui != null && manager.ui.questionText != null)
                    instructionText.font = manager.ui.questionText.font;
            }
            instructionText.enableAutoSizing = true;
            instructionText.fontSize = 36f;
            instructionText.fontSizeMin = 26f;
            instructionText.fontSizeMax = 38f;

            bool handCreated;
            RectTransform handRect = GetOrCreateRect(root, "Hand Pointer (Assign Sprite)", out handCreated);
            Image handImage = GetOrAddComponent<Image>(handRect.gameObject);
            if (handCreated)
            {
                handRect.anchorMin = new Vector2(0.5f, 0.5f);
                handRect.anchorMax = new Vector2(0.5f, 0.5f);
                handRect.pivot = new Vector2(0.5f, 0.92f);
                handRect.sizeDelta = new Vector2(92f, 112f);
                handImage.sprite = null;
                handImage.color = Color.white;
                handImage.preserveAspect = true;
                handImage.raycastTarget = false;
            }

            bool focusCreated;
            RectTransform focusFrame = GetOrCreateRect(root, "Focus Frame", out focusCreated);
            CanvasGroup focusGroup = GetOrAddComponent<CanvasGroup>(focusFrame.gameObject);
            focusGroup.interactable = false;
            focusGroup.blocksRaycasts = false;
            if (focusCreated)
            {
                focusFrame.anchorMin = new Vector2(0.5f, 0.5f);
                focusFrame.anchorMax = new Vector2(0.5f, 0.5f);
                focusFrame.pivot = new Vector2(0.5f, 0.5f);
                focusFrame.sizeDelta = new Vector2(200f, 100f);
            }

            CreateFocusEdge(focusFrame, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -5f), new Vector2(0f, 0f));
            CreateFocusEdge(focusFrame, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 0f), new Vector2(0f, 5f));
            CreateFocusEdge(focusFrame, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(5f, 0f));
            CreateFocusEdge(focusFrame, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-5f, 0f), new Vector2(0f, 0f));

            Undo.RecordObject(tutorial, "Assign Behaviour Wheel Tutorial References");
            tutorial.gameManager = manager;
            tutorial.spinner = manager.spinner;
            tutorial.ui = manager.ui;
            tutorial.overlayRoot = root;
            tutorial.instructionPanel = instructionPanel;
            tutorial.instructionText = instructionText;
            tutorial.instructionCanvasGroup = instructionGroup;
            tutorial.handPointerImage = handImage;
            tutorial.focusFrame = focusFrame;
            tutorial.focusCanvasGroup = focusGroup;
            tutorial.instructionPanelSize = new Vector2(480f, 200f);
            tutorial.instructionFontSizeMin = 26f;
            tutorial.instructionFontSizeMax = 38f;
            tutorial.finalInstructionPanelSize = new Vector2(620f, 260f);
            tutorial.finalInstructionFontSizeMin = 34f;
            tutorial.finalInstructionFontSizeMax = 48f;

            instructionPanel.gameObject.SetActive(false);
            handImage.gameObject.SetActive(false);
            focusFrame.gameObject.SetActive(false);

            if (tutorial.wheelPointerTarget == null)
                tutorial.wheelPointerTarget = FindWheelPointer(manager, tutorial);

            Undo.RecordObject(manager, "Connect Behaviour Wheel Tutorial");
            manager.firstTimeTutorial = tutorial;

            EditorUtility.SetDirty(tutorial);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = tutorial.gameObject;

            string pointerNote = tutorial.wheelPointerTarget == null
                ? "\n\nThe wheel pointer was not found automatically. Assign Wheel Pointer Target on the tutorial component for exact pointing."
                : string.Empty;

            EditorUtility.DisplayDialog("Behaviour Wheel Tutorial",
                "Installation/upgrade complete.\n\nAssign your hand sprite to 'Hand Pointer (Assign Sprite)'." + pointerNote,
                "OK");
        }

        [MenuItem("Tools/Behaviour Wheel Stop/Reset First-Time Tutorial For Open Scene")]
        public static void ResetTutorialForOpenScene()
        {
            string key = $"BehaviourWheelStop.InteractiveTutorial.Completed.{SceneManager.GetActiveScene().name}";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"Reset first-time tutorial state for scene '{SceneManager.GetActiveScene().name}'.");
        }

        private static BehaviourWheelFirstTimeTutorial FindSceneTutorial()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return Resources.FindObjectsOfTypeAll<BehaviourWheelFirstTimeTutorial>()
                .FirstOrDefault(item => item != null && item.gameObject.scene == activeScene);
        }

        private static RectTransform FindWheelPointer(BehaviourWheelGameManager manager,
            BehaviourWheelFirstTimeTutorial tutorial)
        {
            RectTransform searchRoot = manager.ui != null && manager.ui.gameplayPanel != null
                ? manager.ui.gameplayPanel.transform as RectTransform
                : manager.transform.root as RectTransform;

            if (searchRoot == null)
                return null;

            return searchRoot.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect != tutorial.transform &&
                    rect.name.IndexOf("pointer", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    rect.name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) < 0);
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static RectTransform GetOrCreateRect(RectTransform parent, string name, out bool created)
        {
            Transform existing = parent.Find(name);
            if (existing != null && existing is RectTransform existingRect)
            {
                created = false;
                return existingRect;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, $"Create {name}");
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            created = true;
            return rect;
        }

        private static void CreateFocusEdge(RectTransform parent, string name, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            bool created;
            RectTransform edge = GetOrCreateRect(parent, name, out created);
            Image image = GetOrAddComponent<Image>(edge.gameObject);
            if (!created)
                return;

            edge.anchorMin = anchorMin;
            edge.anchorMax = anchorMax;
            edge.pivot = new Vector2(0.5f, 0.5f);
            edge.offsetMin = offsetMin;
            edge.offsetMax = offsetMax;
            image.color = new Color(1f, 0.82f, 0.18f, 1f);
            image.raycastTarget = false;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
#endif
