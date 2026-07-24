#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WordShuffleDragSwap.EditorTools
{
    public static class WordShuffleFirstTimeTutorialInstaller
    {
        private const string TutorialRootName = "WordShuffle_FirstTimeTutorial_ROOT";
        private const string MenuRoot = "Tools/Word Shuffle Drag Swap/First-Time Tutorial/";

        [MenuItem(MenuRoot + "Install or Upgrade In Open Scene")]
        public static void InstallOrUpgrade()
        {
            InstallOrUpgradeInternal(WordShuffleTutorialContentMode.AutoFromGameMode);
        }

        [MenuItem(MenuRoot + "Install or Upgrade English Tutorial")]
        public static void InstallOrUpgradeEnglishTutorial()
        {
            InstallOrUpgradeInternal(WordShuffleTutorialContentMode.EnglishLetters);
        }

        [MenuItem(MenuRoot + "Install or Upgrade Maths Tutorial")]
        public static void InstallOrUpgradeMathsTutorial()
        {
            InstallOrUpgradeInternal(WordShuffleTutorialContentMode.MathsDigits);
        }

        private static void InstallOrUpgradeInternal(WordShuffleTutorialContentMode requestedMode)
        {
            WordShuffleDragSwapManager manager = FindManagerInActiveScene();
            if (manager == null)
            {
                EditorUtility.DisplayDialog(
                    "Word Shuffle Tutorial",
                    "No WordShuffleDragSwapManager was found in the active scene.",
                    "OK");
                return;
            }

            Canvas canvas = manager.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog(
                    "Word Shuffle Tutorial",
                    "The WordShuffleDragSwapManager is not under a Canvas.",
                    "OK");
                return;
            }

            SerializedObject serializedManager = new SerializedObject(manager);
            TextMeshProUGUI questionText = GetObject<TextMeshProUGUI>(serializedManager, "hintText");
            Button hintButton = GetObject<Button>(serializedManager, "hintButton");
            WordShuffleLetterTile tileTemplate =
                GetObject<WordShuffleLetterTile>(serializedManager, "letterTileTemplate");
            Sprite slotSprite = GetObject<Sprite>(serializedManager, "slotSprite");

            GameObject root = FindDirectChild(canvas.transform, TutorialRootName);
            if (root == null)
                root = CreateUIObject(TutorialRootName, canvas.transform);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            SetStretch(rootRect);
            root.transform.SetAsLastSibling();

            CanvasGroup rootCanvasGroup = EnsureComponent<CanvasGroup>(root);
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;

            WordShuffleFirstTimeTutorialController controller =
                EnsureComponent<WordShuffleFirstTimeTutorialController>(root);

            GameObject dimObject = GetOrCreateUIObject("TutorialDimOverlay", root.transform);
            SetStretch(dimObject.GetComponent<RectTransform>());
            Image dimOverlay = EnsureComponent<Image>(dimObject);
            dimOverlay.color = new Color(0.02f, 0.03f, 0.07f, 0.34f);
            dimOverlay.raycastTarget = true;

            GameObject focusObject = GetOrCreateUIObject("TutorialFocusHighlight", root.transform);
            RectTransform focusRect = focusObject.GetComponent<RectTransform>();
            SetCentered(focusRect, Vector2.zero, new Vector2(420f, 180f));
            Image focusImage = EnsureComponent<Image>(focusObject);
            focusImage.color = Color.clear;
            focusImage.raycastTarget = false;

            Outline oldFocusOutline = focusObject.GetComponent<Outline>();
            if (oldFocusOutline != null)
                Undo.DestroyObjectImmediate(oldFocusOutline);

            Color borderColor = new Color(1f, 0.82f, 0.18f, 0.95f);
            CreateOrUpdateFocusBorder("FocusBorderTop", focusObject.transform, true, true, borderColor);
            CreateOrUpdateFocusBorder("FocusBorderBottom", focusObject.transform, true, false, borderColor);
            CreateOrUpdateFocusBorder("FocusBorderLeft", focusObject.transform, false, false, borderColor);
            CreateOrUpdateFocusBorder("FocusBorderRight", focusObject.transform, false, true, borderColor);

            GameObject practiceAreaObject = GetOrCreateUIObject("TutorialPracticeArea", root.transform);
            RectTransform practiceArea = practiceAreaObject.GetComponent<RectTransform>();
            SetCentered(practiceArea, new Vector2(0f, -135f), new Vector2(1500f, 320f));

            GameObject practiceFocusObject =
                GetOrCreateUIObject("TutorialTileFocusTarget", practiceAreaObject.transform);
            RectTransform practiceFocusTarget = practiceFocusObject.GetComponent<RectTransform>();
            SetCentered(practiceFocusTarget, Vector2.zero, new Vector2(560f, 180f));

            GameObject slotLayerObject = GetOrCreateUIObject("TutorialPracticeSlots", practiceAreaObject.transform);
            RectTransform practiceSlotLayer = slotLayerObject.GetComponent<RectTransform>();
            SetStretch(practiceSlotLayer);

            GameObject tileLayerObject = GetOrCreateUIObject("TutorialPracticeTiles", practiceAreaObject.transform);
            RectTransform practiceTileLayer = tileLayerObject.GetComponent<RectTransform>();
            SetStretch(practiceTileLayer);

            GameObject instructionCardObject = GetOrCreateUIObject("TutorialInstructionCard", root.transform);
            RectTransform instructionCard = instructionCardObject.GetComponent<RectTransform>();
            SetCentered(instructionCard, new Vector2(0f, -420f), new Vector2(1120f, 150f));
            Image instructionCardImage = EnsureComponent<Image>(instructionCardObject);
            instructionCardImage.color = new Color(0.055f, 0.09f, 0.19f, 0.98f);
            instructionCardImage.raycastTarget = false;
            Outline instructionOutline = EnsureComponent<Outline>(instructionCardObject);
            instructionOutline.effectColor = new Color(1f, 1f, 1f, 0.2f);
            instructionOutline.effectDistance = new Vector2(2f, -2f);

            TextMeshProUGUI instructionText = EnsureText(
                "TutorialInstructionText",
                instructionCardObject.transform,
                Vector2.zero,
                new Vector2(1040f, 118f),
                36f,
                TextAlignmentOptions.Center);
            bool installMathsContent = requestedMode == WordShuffleTutorialContentMode.MathsDigits ||
                                       (requestedMode == WordShuffleTutorialContentMode.AutoFromGameMode &&
                                        manager.RoundMode == WordShuffleRoundMode.MathLargeNumbers);
            instructionText.text = installMathsContent
                ? "Read the number words. They tell you which number to make.\nClick anywhere to continue."
                : "Read the clue. It tells you which word to make.\nClick anywhere to continue.";

            GameObject oldNextButton = FindDirectChild(instructionCardObject.transform, "TutorialNextButton");
            if (oldNextButton != null)
                Undo.DestroyObjectImmediate(oldNextButton);

            GameObject handObject = GetOrCreateUIObject("HandPointer_EMPTY_ASSIGN_SPRITE", root.transform);
            RectTransform handRect = handObject.GetComponent<RectTransform>();
            SetCentered(handRect, Vector2.zero, new Vector2(118f, 118f));
            Image handImage = EnsureComponent<Image>(handObject);
            handImage.color = Color.white;
            handImage.preserveAspect = true;
            handImage.raycastTarget = false;

            if (questionText != null && questionText.font != null)
                instructionText.font = questionText.font;

            handObject.transform.SetAsLastSibling();
            instructionCardObject.transform.SetAsLastSibling();

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty tutorialContentModeProperty =
                serializedController.FindProperty("tutorialContentMode");
            if (tutorialContentModeProperty != null)
                tutorialContentModeProperty.enumValueIndex = (int)requestedMode;

            SetObject(serializedController, "gameManager", manager);
            SetObject(serializedController, "rootCanvas", canvas);
            SetObject(serializedController, "letterTileTemplate", tileTemplate);
            SetObject(serializedController, "questionText", questionText);
            SetObject(
                serializedController,
                "questionFocusTarget",
                questionText != null && questionText.transform.parent != null
                    ? questionText.transform.parent as RectTransform
                    : questionText != null ? questionText.rectTransform : null);
            SetObject(serializedController, "hintButton", hintButton);
            SetObject(serializedController, "tutorialCanvasGroup", rootCanvasGroup);
            SetObject(serializedController, "dimOverlay", dimOverlay);
            SetObject(serializedController, "focusHighlight", focusImage);
            SetObject(serializedController, "instructionCard", instructionCard);
            SetObject(serializedController, "instructionText", instructionText);
            SetObject(serializedController, "handPointer", handImage);
            SetObject(serializedController, "practiceArea", practiceArea);
            SetObject(serializedController, "practiceFocusTarget", practiceFocusTarget);
            SetObject(serializedController, "practiceSlotLayer", practiceSlotLayer);
            SetObject(serializedController, "practiceTileLayer", practiceTileLayer);

            // Upgrade existing scenes too. New field defaults do not replace values that Unity
            // has already serialized on an installed tutorial controller.
            SerializedProperty lettersHandOffsetProperty =
                serializedController.FindProperty("lettersHandOffset");
            if (lettersHandOffsetProperty != null)
                lettersHandOffsetProperty.vector2Value = new Vector2(0f, -180f);

            SetString(
                serializedController,
                "questionInstruction",
                "Read the clue. It tells you which word to make.\nClick anywhere to continue.");
            SetString(
                serializedController,
                "lettersInstruction",
                "These letters are mixed up.\nClick anywhere to continue.");
            SetString(
                serializedController,
                "completeInstruction",
                "Great job! You can swap letters and use a hint.\nClick anywhere to continue.");
            SetString(
                serializedController,
                "mathsQuestionInstruction",
                "Read the number words. They tell you which number to make.\nClick anywhere to continue.");
            SetString(
                serializedController,
                "mathsDigitsInstruction",
                "These digits are mixed up.\nClick anywhere to continue.");
            SetString(
                serializedController,
                "mathsDemonstrationInstruction",
                "Drag one digit onto another. They will swap places.");
            SetString(
                serializedController,
                "mathsGuidedSwapInstruction",
                "Your turn! Drag 5 onto 3.");
            SetString(
                serializedController,
                "mathsPracticeInstruction",
                "Now make 6172 by yourself.");
            SetString(
                serializedController,
                "mathsCompleteInstruction",
                "Great job! You can swap digits and use a hint.\nClick anywhere to continue.");
            SetString(
                serializedController,
                "mathsGuidedQuestion",
                "Three Thousand Four Hundred and Twenty-five");
            SetString(serializedController, "mathsGuidedShuffled", "5423");
            SetString(serializedController, "mathsGuidedAnswer", "3425");
            SetString(
                serializedController,
                "mathsPracticeQuestion",
                "Six Thousand One Hundred and Seventy-two");
            SetString(serializedController, "mathsPracticeShuffled", "2716");
            SetString(serializedController, "mathsPracticeAnswer", "6172");
            SetString(
                serializedController,
                "mathsHintQuestion",
                "Two Thousand Eight Hundred and Forty-six");
            SetString(serializedController, "mathsHintShuffled", "6842");
            SetString(serializedController, "mathsHintAnswer", "2846");

            SerializedProperty practiceSlotSpriteProperty = serializedController.FindProperty("practiceSlotSprite");
            if (practiceSlotSpriteProperty != null && practiceSlotSpriteProperty.objectReferenceValue == null)
                practiceSlotSpriteProperty.objectReferenceValue = slotSprite;

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            SetObject(serializedManager, "firstTimeTutorial", controller);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);

            root.SetActive(false);
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log(
                $"Installed or upgraded the Word Shuffle first-time tutorial ({requestedMode}). " +
                "Assign a sprite to " +
                "HandPointer_EMPTY_ASSIGN_SPRITE, then review the controller settings in the Inspector.",
                root);
        }

        [MenuItem(MenuRoot + "Install or Upgrade In Open Scene", true)]
        [MenuItem(MenuRoot + "Install or Upgrade English Tutorial", true)]
        [MenuItem(MenuRoot + "Install or Upgrade Maths Tutorial", true)]
        private static bool ValidateInstallOrUpgrade()
        {
            return !Application.isPlaying;
        }

        [MenuItem(MenuRoot + "Reset Tutorial Completion For Active Scene")]
        public static void ResetTutorialCompletion()
        {
            string key = WordShuffleFirstTimeTutorialController.GetCompletionKeyForActiveScene();
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"Reset Word Shuffle tutorial completion for scene '{SceneManager.GetActiveScene().name}'.");
        }

        [MenuItem(MenuRoot + "Reset How To Play Status For Active Scene")]
        public static void ResetHowToPlayStatus()
        {
            string key = $"WordShuffle.HowToPlay.Seen.{SceneManager.GetActiveScene().name}";
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"Reset Word Shuffle How to Play status for scene '{SceneManager.GetActiveScene().name}'.");
        }

        private static WordShuffleDragSwapManager FindManagerInActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return Resources
                .FindObjectsOfTypeAll<WordShuffleDragSwapManager>()
                .FirstOrDefault(manager =>
                    manager != null &&
                    manager.gameObject.scene == activeScene &&
                    !EditorUtility.IsPersistent(manager));
        }

        private static GameObject FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            Transform child = parent.Find(childName);
            return child != null ? child.gameObject : null;
        }

        private static GameObject GetOrCreateUIObject(string name, Transform parent)
        {
            GameObject existing = FindDirectChild(parent, name);
            return existing != null ? existing : CreateUIObject(name, parent);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject created = new GameObject(name, typeof(RectTransform));
            created.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            return created;
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static TextMeshProUGUI EnsureText(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = GetOrCreateUIObject(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetCentered(rect, position, size);

            TextMeshProUGUI label = EnsureComponent<TextMeshProUGUI>(textObject);
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Normal;
            label.alignment = alignment;
            label.color = Color.white;
            label.enableWordWrapping = true;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(18f, fontSize * 0.62f);
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;
            return label;
        }

        private static void CreateOrUpdateFocusBorder(
            string name,
            Transform parent,
            bool horizontal,
            bool positiveSide,
            Color color)
        {
            GameObject borderObject = GetOrCreateUIObject(name, parent);
            RectTransform rect = borderObject.GetComponent<RectTransform>();
            const float thickness = 6f;

            if (horizontal)
            {
                float y = positiveSide ? 1f : 0f;
                rect.anchorMin = new Vector2(0f, y);
                rect.anchorMax = new Vector2(1f, y);
                rect.pivot = new Vector2(0.5f, y);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, thickness);
            }
            else
            {
                float x = positiveSide ? 1f : 0f;
                rect.anchorMin = new Vector2(x, 0f);
                rect.anchorMax = new Vector2(x, 1f);
                rect.pivot = new Vector2(x, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(thickness, 0f);
            }

            rect.localScale = Vector3.one;
            Image borderImage = EnsureComponent<Image>(borderObject);
            borderImage.color = color;
            borderImage.raycastTarget = false;
        }

        private static void SetStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static T GetObject<T>(SerializedObject serializedObject, string propertyName) where T : Object
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            return property != null ? property.objectReferenceValue as T : null;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }
    }
}
#endif
