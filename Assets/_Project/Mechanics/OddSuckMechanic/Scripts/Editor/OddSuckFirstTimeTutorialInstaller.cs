#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OddSuckMechanic.EditorTools
{
    public static class OddSuckFirstTimeTutorialInstaller
    {
        private const string TutorialRootName = "OddSuckFirstTimeTutorialRoot";

        [MenuItem("Tools/Odd Suck/Install or Upgrade First-Time Tutorial")]
        public static void InstallOrUpgrade()
        {
            OddSuckManager manager = Object.FindObjectOfType<OddSuckManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Odd Suck Tutorial", "No OddSuckManager was found in the open scene.", "OK");
                return;
            }

            Canvas canvas = manager.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Odd Suck Tutorial", "The OddSuckManager is not under a Canvas.", "OK");
                return;
            }

            OddSuckFirstTimeTutorialController tutorial = FindExistingTutorial(canvas);
            RectTransform root;
            bool createdRoot = false;

            if (tutorial == null)
            {
                root = FindDirectChild(canvas.transform, TutorialRootName);
                if (root == null)
                {
                    GameObject rootGo = new GameObject(TutorialRootName, typeof(RectTransform), typeof(CanvasGroup));
                    Undo.RegisterCreatedObjectUndo(rootGo, "Create Odd Suck Tutorial Root");
                    rootGo.transform.SetParent(canvas.transform, false);
                    root = rootGo.GetComponent<RectTransform>();
                    Stretch(root);
                    createdRoot = true;
                }

                tutorial = root.GetComponent<OddSuckFirstTimeTutorialController>();
                if (tutorial == null)
                {
                    tutorial = Undo.AddComponent<OddSuckFirstTimeTutorialController>(root.gameObject);
                }
            }
            else
            {
                root = tutorial.transform as RectTransform;
            }

            CanvasGroup rootCanvasGroup = GetOrAddComponent<CanvasGroup>(root.gameObject);
            root.SetAsLastSibling();

            Button inputButton = EnsureInputButton(root);
            Image dimImage = EnsureDimImage(root);
            RectTransform instructionCard = EnsureInstructionCard(root, out CanvasGroup instructionGroup, out TMP_Text instructionText);
            Image handImage = EnsureHandPointer(root);

            SerializedObject managerSo = new SerializedObject(manager);
            SerializedObject tutorialSo = new SerializedObject(tutorial);

            SetObjectIfMissing(tutorialSo, "tutorialRoot", root);
            SetObjectIfMissing(tutorialSo, "tutorialCanvasGroup", rootCanvasGroup);
            SetObjectIfMissing(tutorialSo, "tutorialInputButton", inputButton);
            SetObjectIfMissing(tutorialSo, "focusDimImage", dimImage);
            SetObjectIfMissing(tutorialSo, "instructionCard", instructionCard);
            SetObjectIfMissing(tutorialSo, "instructionCanvasGroup", instructionGroup);
            SetObjectIfMissing(tutorialSo, "instructionText", instructionText);
            SetObjectIfMissing(tutorialSo, "handPointerImage", handImage);
            SetObjectIfMissing(tutorialSo, "manager", manager);
            SetVector2(tutorialSo, "preferredInstructionCardSize", new Vector2(740f, 180f));
            SetVector2(tutorialSo, "questionHandTargetOffset", new Vector2(35f, -50f));
            SetFloat(tutorialSo, "completionHoldDuration", 2.5f);
            SetFloat(tutorialSo, "completionFadeOutDuration", 0.35f);
            SetString(tutorialSo, "completionMessage", "Great job! You're ready!\nThe real game starts now.");

            CopyManagerReference(managerSo, tutorialSo, "ufoMover", "ufoMover");
            CopyManagerReference(managerSo, tutorialSo, "ufoMoveTransform", "ufoMoveTransform");
            CopyManagerReference(managerSo, tutorialSo, "ufoVisualTransform", "ufoVisualTransform");
            CopyManagerReference(managerSo, tutorialSo, "beamTransform", "beamTransform");
            CopyManagerReference(managerSo, tutorialSo, "beamCanvasGroup", "beamCanvasGroup");
            CopyManagerReference(managerSo, tutorialSo, "pullVisualController", "pullVisualController");
            CopyManagerReference(managerSo, tutorialSo, "itemParent", "practiceItemParent");
            CopyManagerReference(managerSo, tutorialSo, "questionText", "gameplayQuestionText");
            CopyManagerReference(managerSo, tutorialSo, "itemTemplate", "fallbackItemTemplate");
            CopyManagerReference(managerSo, tutorialSo, "leftTextItemTemplate", "leftTextItemTemplate");
            CopyManagerReference(managerSo, tutorialSo, "centerTextItemTemplate", "centerTextItemTemplate");
            CopyManagerReference(managerSo, tutorialSo, "rightTextItemTemplate", "rightTextItemTemplate");
            CopyManagerReference(managerSo, tutorialSo, "imageItemTemplate", "imageItemTemplate");

            tutorialSo.ApplyModifiedProperties();
            SetObjectIfMissing(managerSo, "firstTimeTutorial", tutorial);
            managerSo.ApplyModifiedProperties();

            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
            root.gameObject.SetActive(false);

            EditorUtility.SetDirty(tutorial);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            EditorGUIUtility.PingObject(root.gameObject);

            string result = createdRoot ? "installed" : "upgraded";
            Debug.Log($"Odd Suck first-time tutorial {result}. Assign the hand sprite and scene-specific guided/independent practice content on '{TutorialRootName}'.", root.gameObject);
        }

        private static OddSuckFirstTimeTutorialController FindExistingTutorial(Canvas canvas)
        {
            OddSuckFirstTimeTutorialController[] tutorials = canvas.GetComponentsInChildren<OddSuckFirstTimeTutorialController>(true);
            return tutorials.Length > 0 ? tutorials[0] : null;
        }

        private static Button EnsureInputButton(RectTransform root)
        {
            RectTransform rect = EnsureRect(root, "TutorialInputSurface");
            Stretch(rect);
            rect.SetAsFirstSibling();

            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = GetOrAddComponent<Button>(rect.gameObject);
            button.transition = Selectable.Transition.None;
            return button;
        }

        private static Image EnsureDimImage(RectTransform root)
        {
            RectTransform rect = EnsureRect(root, "TutorialFocusDim");
            Stretch(rect);
            rect.SetSiblingIndex(Mathf.Min(1, root.childCount - 1));

            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }

        private static RectTransform EnsureInstructionCard(RectTransform root, out CanvasGroup group, out TMP_Text instructionText)
        {
            RectTransform card = EnsureRect(root, "TutorialInstructionCard");
            card.anchorMin = new Vector2(0.5f, 0.56f);
            card.anchorMax = new Vector2(0.5f, 0.56f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.sizeDelta = new Vector2(740f, 180f);
            card.anchoredPosition = Vector2.zero;

            Image cardImage = GetOrAddComponent<Image>(card.gameObject);
            if (cardImage.color.a <= 0.001f)
            {
                cardImage.color = new Color(0.035f, 0.055f, 0.12f, 0.96f);
            }
            cardImage.raycastTarget = false;
            group = GetOrAddComponent<CanvasGroup>(card.gameObject);
            group.blocksRaycasts = false;
            group.interactable = false;

            RectTransform textRect = EnsureRect(card, "InstructionText");
            Stretch(textRect, new Vector2(34f, 22f), new Vector2(-34f, -22f));
            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(textRect.gameObject);
            text.text = "Tutorial instruction";
            text.fontSize = 38f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 26f;
            text.fontSizeMax = 40f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            instructionText = text;
            return card;
        }

        private static Image EnsureHandPointer(RectTransform root)
        {
            RectTransform rect = EnsureRect(root, "TutorialHandPointer");
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.2f, 0.8f);
            if (rect.sizeDelta.sqrMagnitude < 1f)
            {
                rect.sizeDelta = new Vector2(120f, 120f);
            }

            Image image = GetOrAddComponent<Image>(rect.gameObject);
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            RectTransform existing = FindDirectChild(parent, name);
            if (existing != null)
            {
                return existing;
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child as RectTransform;
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(go);
        }

        private static void CopyManagerReference(SerializedObject managerSo, SerializedObject tutorialSo, string managerProperty, string tutorialProperty)
        {
            SerializedProperty source = managerSo.FindProperty(managerProperty);
            SerializedProperty destination = tutorialSo.FindProperty(tutorialProperty);
            if (source != null && destination != null && destination.objectReferenceValue == null)
            {
                destination.objectReferenceValue = source.objectReferenceValue;
            }
        }

        private static void SetObjectIfMissing(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null && property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector2Value = value;
            }
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
