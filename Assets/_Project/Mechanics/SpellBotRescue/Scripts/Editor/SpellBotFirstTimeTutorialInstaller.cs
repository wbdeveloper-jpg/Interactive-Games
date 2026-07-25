#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace NarayanaGames.SpellBotRescue
{
    public static class SpellBotFirstTimeTutorialInstaller
    {
        private const string TutorialRootName = "SpellBotFirstTimeTutorial";

        [MenuItem("Tools/Spell Bot Rescue/First-Time Tutorial/Install or Upgrade")]
        public static void InstallOrUpgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Spell-Bot Tutorial",
                    "Stop Play Mode before installing or upgrading the tutorial.",
                    "OK");
                return;
            }

            SpellBotRescueManager manager = Object.FindObjectOfType<SpellBotRescueManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog(
                    "Spell-Bot Tutorial",
                    "No SpellBotRescueManager was found in the open scene.",
                    "OK");
                return;
            }

            RectTransform parent = ResolveTutorialParent(manager);
            if (parent == null)
            {
                EditorUtility.DisplayDialog(
                    "Spell-Bot Tutorial",
                    "No Canvas or Safe Area parent could be found. Assign the manager UI references first.",
                    "OK");
                return;
            }

            Undo.SetCurrentGroupName("Install or Upgrade Spell-Bot First-Time Tutorial");
            int undoGroup = Undo.GetCurrentGroup();

            RectTransform tutorialRoot = FindDeepChild(parent, TutorialRootName) as RectTransform;
            bool createdRoot = tutorialRoot == null;

            if (createdRoot)
            {
                tutorialRoot = CreateRect(TutorialRootName, parent);
                Stretch(tutorialRoot);
            }

            SpellBotFirstTimeTutorialController controller =
                tutorialRoot.GetComponent<SpellBotFirstTimeTutorialController>();

            if (controller == null)
            {
                controller = Undo.AddComponent<SpellBotFirstTimeTutorialController>(tutorialRoot.gameObject);
            }

            Image dimOverlay = EnsureImage(tutorialRoot, "DimOverlay", out bool createdDim);
            if (createdDim)
            {
                Stretch(dimOverlay.rectTransform);
                dimOverlay.color = new Color(0f, 0f, 0f, 0f);
            }
            dimOverlay.raycastTarget = false;

            Image tapCatcher = EnsureImage(tutorialRoot, "FullScreenTapCatcher", out bool createdTapCatcher);
            if (createdTapCatcher)
            {
                Stretch(tapCatcher.rectTransform);
                tapCatcher.color = new Color(1f, 1f, 1f, 0f);
            }
            tapCatcher.raycastTarget = false;

            Image instructionBackground = EnsureImage(tutorialRoot, "InstructionPanel", out bool createdInstruction);
            RectTransform instructionPanel = instructionBackground.rectTransform;
            if (createdInstruction)
            {
                SetCentredRect(instructionPanel, new Vector2(900f, 132f), Vector2.zero);
                instructionBackground.color = new Color(1f, 1f, 1f, 0f);
            }
            instructionBackground.raycastTarget = false;

            CanvasGroup instructionCanvasGroup = instructionPanel.GetComponent<CanvasGroup>();
            if (instructionCanvasGroup == null)
            {
                instructionCanvasGroup = Undo.AddComponent<CanvasGroup>(instructionPanel.gameObject);
            }
            instructionCanvasGroup.interactable = false;
            instructionCanvasGroup.blocksRaycasts = false;

            TextMeshProUGUI instructionText = EnsureText(instructionPanel, "InstructionText", out bool createdInstructionText);
            if (createdInstructionText)
            {
                Stretch(instructionText.rectTransform, new Vector2(24f, 12f), new Vector2(-24f, -12f));
                instructionText.text = "Tutorial instruction";
                instructionText.fontSize = 32f;
                instructionText.enableAutoSizing = true;
                instructionText.fontSizeMin = 22f;
                instructionText.fontSizeMax = 34f;
                instructionText.fontStyle = FontStyles.Bold;
                instructionText.alignment = TextAlignmentOptions.Center;
                instructionText.color = new Color(0.06f, 0.09f, 0.15f, 1f);
                instructionText.outlineColor = new Color(1f, 1f, 1f, 0.92f);
                instructionText.outlineWidth = 0.16f;
            }
            instructionText.raycastTarget = false;

            if (instructionText.font == null)
            {
                instructionText.font = manager.primaryFont != null ? manager.primaryFont : manager.secondaryFont;
            }

            Image handPointer = EnsureImage(tutorialRoot, "HandPointerImage", out bool createdHand);
            if (createdHand)
            {
                SetCentredRect(handPointer.rectTransform, new Vector2(120f, 120f), Vector2.zero);
                handPointer.sprite = null;
                handPointer.color = Color.white;
                handPointer.preserveAspect = true;
            }
            handPointer.raycastTarget = false;

            TextMeshProUGUI ghostWord = EnsureText(tutorialRoot, "GhostWordText", out bool createdGhostWord);
            if (createdGhostWord)
            {
                SetCentredRect(ghostWord.rectTransform, new Vector2(760f, 126f), Vector2.zero);
                ghostWord.text = string.Empty;
                ghostWord.fontSize = manager.wordText != null ? manager.wordText.fontSize : 70f;
                ghostWord.fontStyle = FontStyles.Bold;
                ghostWord.alignment = TextAlignmentOptions.Center;
                ghostWord.color = new Color(0.16f, 0.48f, 0.92f, 1f);
            }
            ghostWord.raycastTarget = false;

            if (ghostWord.font == null && manager.wordText != null)
            {
                ghostWord.font = manager.wordText.font;
            }

            CanvasGroup ghostCanvasGroup = ghostWord.GetComponent<CanvasGroup>();
            if (ghostCanvasGroup == null)
            {
                ghostCanvasGroup = Undo.AddComponent<CanvasGroup>(ghostWord.gameObject);
            }
            ghostCanvasGroup.alpha = 0f;
            ghostCanvasGroup.interactable = false;
            ghostCanvasGroup.blocksRaycasts = false;

            Image ghostCaret = EnsureImage(tutorialRoot, "GhostCaret", out bool createdGhostCaret);
            if (createdGhostCaret)
            {
                SetCentredRect(ghostCaret.rectTransform, new Vector2(6f, 74f), Vector2.zero);
                ghostCaret.color = new Color(0.16f, 0.48f, 0.92f, 0.82f);
            }
            ghostCaret.raycastTarget = false;

            Image skipButtonBackground = EnsureImage(tutorialRoot, "SkipTutorialButton", out bool createdSkipButton);
            Button skipButton = skipButtonBackground.GetComponent<Button>();
            if (skipButton == null)
            {
                skipButton = Undo.AddComponent<Button>(skipButtonBackground.gameObject);
            }

            if (createdSkipButton)
            {
                RectTransform skipRect = skipButtonBackground.rectTransform;
                skipRect.anchorMin = Vector2.one;
                skipRect.anchorMax = Vector2.one;
                skipRect.pivot = Vector2.one;
                skipRect.sizeDelta = new Vector2(250f, 68f);
                skipRect.anchoredPosition = new Vector2(-28f, -28f);
                skipRect.localScale = Vector3.one;
                skipButtonBackground.color = new Color(0.06f, 0.09f, 0.15f, 0.84f);

                ColorBlock colors = skipButton.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
                colors.pressedColor = new Color(0.78f, 0.86f, 1f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(1f, 1f, 1f, 0.45f);
                colors.fadeDuration = 0.12f;
                skipButton.colors = colors;
            }
            skipButtonBackground.raycastTarget = true;

            TextMeshProUGUI skipButtonText = EnsureText(
                skipButtonBackground.transform,
                "Label",
                out bool createdSkipButtonText);

            if (createdSkipButtonText)
            {
                Stretch(skipButtonText.rectTransform, new Vector2(16f, 8f), new Vector2(-16f, -8f));
                skipButtonText.text = "SKIP TUTORIAL";
                skipButtonText.fontSize = 24f;
                skipButtonText.enableAutoSizing = true;
                skipButtonText.fontSizeMin = 17f;
                skipButtonText.fontSizeMax = 25f;
                skipButtonText.fontStyle = FontStyles.Bold;
                skipButtonText.alignment = TextAlignmentOptions.Center;
                skipButtonText.color = Color.white;
            }
            skipButtonText.raycastTarget = false;

            if (skipButtonText.font == null)
            {
                skipButtonText.font = manager.primaryFont != null ? manager.primaryFont : manager.secondaryFont;
            }

            EnsureWordCaretInput(manager);

            Undo.RecordObject(controller, "Assign Spell-Bot Tutorial References");
            controller.manager = manager;
            controller.tutorialRoot = tutorialRoot;
            controller.fullScreenTapCatcher = tapCatcher;
            controller.dimOverlay = dimOverlay;
            controller.instructionPanel = instructionPanel;
            controller.instructionCanvasGroup = instructionCanvasGroup;
            controller.instructionText = instructionText;
            controller.handPointerImage = handPointer;
            controller.ghostWordText = ghostWord;
            controller.ghostWordCanvasGroup = ghostCanvasGroup;
            controller.ghostCaretImage = ghostCaret;
            controller.skipTutorialButton = skipButton;
            controller.skipTutorialButtonText = skipButtonText;

            if (controller.installedTutorialVersion < 2)
            {
                controller.handTipNormalised = new Vector2(0.5f, 1f);
                controller.targetPointNormalised = new Vector2(0.5f, 0.5f);
                controller.keepWholeHandOnScreen = false;
                controller.wordPointerOffset = Vector2.zero;
                controller.backPointerOffset = Vector2.zero;
                controller.caretPointerOffset = Vector2.zero;
                controller.letterPointerOffset = Vector2.zero;
                controller.hintPointerOffset = Vector2.zero;
                controller.fixedPointerOffset = Vector2.zero;
                controller.instructionReadDelay = 1.35f;
                controller.actionAdvanceDelay = 0.85f;
                controller.successMessageDuration = 1.8f;
                controller.installedTutorialVersion = 2;
            }

            if (controller.installedTutorialVersion < 3)
            {
                controller.showSkipButton = true;
                controller.skipMarksTutorialComplete = true;
                controller.skipButtonLabel = "SKIP TUTORIAL";
                controller.installedTutorialVersion = 3;
            }

            Undo.RecordObject(manager, "Assign First-Time Tutorial");
            manager.firstTimeTutorial = controller;

            handPointer.gameObject.SetActive(false);
            ghostWord.gameObject.SetActive(false);
            ghostCaret.gameObject.SetActive(false);
            instructionPanel.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            skipButton.transform.SetAsLastSibling();
            tutorialRoot.SetAsLastSibling();
            tutorialRoot.gameObject.SetActive(false);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = tutorialRoot.gameObject;

            EditorUtility.DisplayDialog(
                "Spell-Bot Tutorial",
                createdRoot
                    ? "First-time tutorial installed. Assign your hand sprite to HandPointerImage."
                    : "First-time tutorial upgraded. Existing tutorial-owned layout values were preserved.",
                "OK");
        }

        [MenuItem("Tools/Spell Bot Rescue/First-Time Tutorial/Select Tutorial Root")]
        public static void SelectTutorialRoot()
        {
            SpellBotFirstTimeTutorialController controller = FindSceneTutorialController();
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Spell-Bot Tutorial", "The tutorial root is not installed in this scene.", "OK");
                return;
            }

            Selection.activeGameObject = controller.gameObject;
            EditorGUIUtility.PingObject(controller.gameObject);
        }

        [MenuItem("Tools/Spell Bot Rescue/First-Time Tutorial/Reset Current Scene Completion")]
        public static void ResetCurrentSceneCompletion()
        {
            SpellBotFirstTimeTutorialController controller = FindSceneTutorialController();

            if (controller == null)
            {
                EditorUtility.DisplayDialog("Spell-Bot Tutorial", "Install the tutorial first.", "OK");
                return;
            }

            controller.ResetTutorialForCurrentScene();
            Debug.Log("Spell-Bot first-time tutorial completion was reset for the current scene.");
        }

        private static SpellBotFirstTimeTutorialController FindSceneTutorialController()
        {
            SpellBotFirstTimeTutorialController[] controllers =
                Resources.FindObjectsOfTypeAll<SpellBotFirstTimeTutorialController>();

            for (int i = 0; i < controllers.Length; i++)
            {
                SpellBotFirstTimeTutorialController controller = controllers[i];
                if (controller != null &&
                    controller.gameObject.scene.IsValid() &&
                    !EditorUtility.IsPersistent(controller))
                {
                    return controller;
                }
            }

            return null;
        }

        private static RectTransform ResolveTutorialParent(SpellBotRescueManager manager)
        {
            if (manager.fontApplyRoot is RectTransform configuredRoot)
            {
                return configuredRoot;
            }

            if (manager.wordText != null && manager.wordText.canvas != null)
            {
                return manager.wordText.canvas.transform as RectTransform;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            return canvas != null ? canvas.transform as RectTransform : null;
        }

        private static void EnsureWordCaretInput(SpellBotRescueManager manager)
        {
            if (manager.wordInputField == null)
            {
                return;
            }

            SpellBotWordCaretInput caretInput =
                manager.wordInputField.GetComponent<SpellBotWordCaretInput>();

            if (caretInput == null)
            {
                caretInput = Undo.AddComponent<SpellBotWordCaretInput>(manager.wordInputField.gameObject);
            }

            Undo.RecordObject(caretInput, "Assign Spell-Bot Caret Input");
            caretInput.manager = manager;
            caretInput.targetInputField = manager.wordInputField;
            caretInput.targetText = manager.wordText;
            EditorUtility.SetDirty(caretInput);
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create " + objectName);
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static Image EnsureImage(Transform parent, string objectName, out bool created)
        {
            Transform existing = parent.Find(objectName);
            created = existing == null;
            RectTransform rect = created ? CreateRect(objectName, parent) : existing as RectTransform;

            Image image = rect.GetComponent<Image>();
            if (image == null)
            {
                image = Undo.AddComponent<Image>(rect.gameObject);
            }

            return image;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string objectName, out bool created)
        {
            Transform existing = parent.Find(objectName);
            created = existing == null;
            RectTransform rect = created ? CreateRect(objectName, parent) : existing as RectTransform;

            TextMeshProUGUI text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null)
            {
                text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            }

            return text;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, Vector2.zero, Vector2.zero);
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static void SetCentredRect(RectTransform rect, Vector2 size, Vector2 anchoredPosition)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
