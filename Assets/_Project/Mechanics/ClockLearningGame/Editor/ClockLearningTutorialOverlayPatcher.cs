#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClockLearningGame
{
    public static class ClockLearningTutorialOverlayPatcher
    {
        private const string PatchMenu = "Tools/Clock Learning Game/Patch Existing Tutorial Overlay Only";
        private const string PreviewMenu = "Tools/Clock Learning Game/Preview Tutorial Overlay In Editor";
        private const string HidePreviewMenu = "Tools/Clock Learning Game/Hide Tutorial Overlay Preview";

        [MenuItem(PatchMenu)]
        private static void PatchExistingTutorialOverlayOnly()
        {
            ClockLearningTutorialController controller = FindSceneObject<ClockLearningTutorialController>("ClockLearningTutorialController");
            if (controller == null)
            {
                Debug.LogWarning("Clock Learning Game: No ClockLearningTutorialController found in the open scene.");
                return;
            }

            Undo.RecordObject(controller, "Patch Clock Guided Tutorial");
            SerializedObject so = new SerializedObject(controller);

            Button hintButton = FindSceneObject<Button>("Hint Button");
            Button singleSubmitButton = FindSceneObject<Button>("Single Submit Button");
            Button doubleSubmitButton = FindSceneObject<Button>("Double Submit Button");
            Toggle clockAToggle = FindSceneObject<Toggle>("Clock A AM PM Toggle");
            Toggle clockBToggle = FindSceneObject<Toggle>("Clock B AM PM Toggle");

            TextMeshProUGUI singlePrompt = FindSceneObject<TextMeshProUGUI>("Single Prompt");
            TextMeshProUGUI singleTarget = FindSceneObject<TextMeshProUGUI>("Single Target");
            TextMeshProUGUI doublePrompt = FindSceneObject<TextMeshProUGUI>("Difference Prompt");
            TextMeshProUGUI doubleTarget = FindSceneObject<TextMeshProUGUI>("Difference Target");
            TextMeshProUGUI doubleChip = FindSceneObject<TextMeshProUGUI>("Difference Chip");

            TrySetObject(so, "hintButton", hintButton);
            TrySetObject(so, "hintButtonTarget", GetRect(hintButton));
            TrySetObject(so, "singleSubmitButton", singleSubmitButton);
            TrySetObject(so, "doubleSubmitButton", doubleSubmitButton);
            TrySetObject(so, "clockAPmToggle", clockAToggle);
            TrySetObject(so, "clockBPmToggle", clockBToggle);
            TrySetObject(so, "doubleAmPmTarget", GetRect(clockBToggle != null ? clockBToggle : clockAToggle));

            TrySetObject(so, "singlePromptTextTarget", singlePrompt);
            TrySetObject(so, "singleTargetTextTarget", singleTarget);
            TrySetObject(so, "doublePromptTextTarget", doublePrompt);
            TrySetObject(so, "doubleTargetTextTarget", doubleTarget);
            TrySetObject(so, "doubleChipTextTarget", doubleChip);

            TrySetBool(so, "useDummyPracticeTutorial", true);
            TrySetInt(so, "singlePracticeHour", 3);
            TrySetInt(so, "singlePracticeMinute", 30);
            TrySetInt(so, "singlePracticeStartHour", 12);
            TrySetInt(so, "singlePracticeStartMinute", 15);
            TrySetString(so, "singlePracticeDisplayText", "3:30");
            TrySetString(so, "singlePracticePromptText", "Practice time");
            TrySetInt(so, "singleHintWrongHour", 8);
            TrySetInt(so, "singleHintWrongMinute", 10);

            TrySetInt(so, "doublePracticeClockAHour", 3);
            TrySetInt(so, "doublePracticeClockAMinute", 30);
            TrySetBool(so, "doublePracticeClockAIsPm", false);
            TrySetInt(so, "doublePracticeClockBHour", 4);
            TrySetInt(so, "doublePracticeClockBMinute", 30);
            TrySetBool(so, "doublePracticeClockBIsPm", false);
            TrySetBool(so, "doublePracticeStartClockBAsPm", true);
            TrySetInt(so, "doubleHintWrongClockAHour", 1);
            TrySetInt(so, "doubleHintWrongClockAMinute", 10);
            TrySetInt(so, "doubleHintWrongClockBHour", 8);
            TrySetInt(so, "doubleHintWrongClockBMinute", 25);
            TrySetString(so, "doublePracticeDifferenceText", "1 hour");
            TrySetString(so, "doublePracticePromptText", "Practice difference");

            TrySetVector2(so, "questionPointerOffset", new Vector2(70f, -115f));
            TrySetVector2(so, "normalPointerOffset", new Vector2(70f, -55f));
            TrySetVector2(so, "questionPromptOffset", new Vector2(0f, -185f));
            TrySetVector2(so, "clockPromptOffset", new Vector2(0f, -245f));
            TrySetVector2(so, "readyPromptOffset", Vector2.zero);
            TrySetVector2(so, "hintPromptOffset", new Vector2(0f, -120f));
            TrySetVector2(so, "amPmPromptOffset", new Vector2(0f, 190f));
            TrySetVector2(so, "promptCardSize", new Vector2(920f, 92f));
            TrySetVector2(so, "promptClampMargin", new Vector2(60f, 45f));

            TrySetString(so, "singleIntroPrompt", "Practice time: set the clock to half past three. Tap anywhere to begin.");
            TrySetString(so, "singleHourPrompt", "First, move the short hand near 3.");
            TrySetString(so, "singleHourSuccessPrompt", "Great! The short hand shows the hour.");
            TrySetString(so, "singleHourRetryPrompt", "Almost. Move the short hand near 3.");
            TrySetString(so, "singleMinutePrompt", "Now move the long hand to 6 for 30 minutes.");
            TrySetString(so, "singleMinuteSuccessPrompt", "Good! The long hand shows 30 minutes.");
            TrySetString(so, "singleMinuteRetryPrompt", "Almost. Move the long hand to 6.");
            TrySetString(so, "singleHintPrompt", "This clock is not right yet. Tap Hint to see help.");
            TrySetString(so, "singleSubmitPrompt", "All set! Tap Submit to start the real game.");

            TrySetString(so, "doubleIntroPrompt", "Practice: make a difference of 1 hour. Tap anywhere to begin.");
            TrySetString(so, "doubleAmPmPrompt", "First, set Clock B to AM.");
            TrySetString(so, "doubleAmPmSuccessPrompt", "Good! Both clocks are using AM.");
            TrySetString(so, "doubleAmPmRetryPrompt", "Tap the Clock B AM/PM button until it shows AM.");
            TrySetString(so, "doubleClockAHourPrompt", "Clock A: move the short hand near 3.");
            TrySetString(so, "doubleClockAMinutePrompt", "Clock A: move the long hand to 6.");
            TrySetString(so, "doubleClockBHourPrompt", "Clock B: move the short hand near 4.");
            TrySetString(so, "doubleClockBMinutePrompt", "Clock B: move the long hand to 6.");
            TrySetString(so, "doubleHandSuccessPrompt", "Good! That hand is in place.");
            TrySetString(so, "doubleHandRetryPrompt", "Almost. Try the highlighted hand again.");
            TrySetString(so, "doubleHintPrompt", "These clocks are not right yet. Tap Hint to see help.");
            TrySetString(so, "doubleSubmitPrompt", "Now the clocks are 1 hour apart. Tap Submit to start.");

            TrySetFloat(so, "promptBackgroundOpacity", 0.86f);
            TrySetFloat(so, "backgroundOpacity", 0f);
            TrySetFloat(so, "stepTransitionDelay", 0.65f);
            TrySetFloat(so, "retryPromptDelay", 0.65f);
            TrySetFloat(so, "pointerMoveDuration", 0.52f);
            TrySetFloat(so, "fakeHandMoveDuration", 1.25f);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("Clock Learning Game: Guided dummy-question tutorial carry-safe hint patch applied. This changed only tutorial script values/references in the existing scene; no UI was regenerated.");
        }

        [MenuItem(PreviewMenu)]
        private static void PreviewTutorialOverlayInEditor()
        {
            ClockLearningTutorialController controller = FindSceneObject<ClockLearningTutorialController>("ClockLearningTutorialController");
            if (controller == null)
            {
                Debug.LogWarning("Clock Learning Game: No ClockLearningTutorialController found in the open scene.");
                return;
            }

            SerializedObject so = new SerializedObject(controller);
            CanvasGroup overlayGroup = GetObject<CanvasGroup>(so, "overlayGroup");
            RectTransform promptCard = GetObject<RectTransform>(so, "promptCard");
            TextMeshProUGUI promptText = GetObject<TextMeshProUGUI>(so, "promptText");
            RectTransform pointer = GetObject<RectTransform>(so, "pointer");
            Image backgroundImage = GetObject<Image>(so, "backgroundImage");

            if (overlayGroup != null)
            {
                Undo.RecordObject(overlayGroup, "Preview Tutorial Overlay");
                overlayGroup.gameObject.SetActive(true);
                overlayGroup.alpha = 1f;
                overlayGroup.interactable = false;
                overlayGroup.blocksRaycasts = false;
            }

            if (backgroundImage != null)
            {
                Undo.RecordObject(backgroundImage, "Preview Tutorial Overlay Background");
                backgroundImage.gameObject.SetActive(true);
                Color bg = backgroundImage.color;
                bg.a = 0f;
                backgroundImage.color = bg;
                backgroundImage.raycastTarget = false;
            }

            if (promptCard != null)
            {
                Undo.RecordObject(promptCard, "Preview Tutorial Instruction Line");
                promptCard.gameObject.SetActive(true);
                promptCard.anchorMin = new Vector2(0.5f, 0.5f);
                promptCard.anchorMax = new Vector2(0.5f, 0.5f);
                promptCard.pivot = new Vector2(0.5f, 0.5f);
                promptCard.anchoredPosition = new Vector2(0f, -210f);
                promptCard.sizeDelta = new Vector2(920f, 92f);
            }

            if (promptText != null)
            {
                Undo.RecordObject(promptText, "Preview Tutorial Instruction Text");
                promptText.text = "Look at the time you need to set. Click anywhere to continue.";
                promptText.raycastTarget = false;
            }

            if (pointer != null)
            {
                Undo.RecordObject(pointer, "Preview Tutorial Pointer");
                pointer.gameObject.SetActive(true);
                pointer.anchorMin = new Vector2(0.5f, 0.5f);
                pointer.anchorMax = new Vector2(0.5f, 0.5f);
                pointer.pivot = new Vector2(0.5f, 0.5f);
                pointer.anchoredPosition = new Vector2(130f, -95f);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Clock Learning Game: Tutorial overlay preview shown in Edit Mode. Use Hide Tutorial Overlay Preview before saving if you do not want it visible in the scene.");
        }

        [MenuItem(HidePreviewMenu)]
        private static void HideTutorialOverlayPreview()
        {
            ClockLearningTutorialController controller = FindSceneObject<ClockLearningTutorialController>("ClockLearningTutorialController");
            if (controller == null) return;

            SerializedObject so = new SerializedObject(controller);
            CanvasGroup overlayGroup = GetObject<CanvasGroup>(so, "overlayGroup");
            RectTransform pointer = GetObject<RectTransform>(so, "pointer");
            RectTransform ghostHand = GetObject<RectTransform>(so, "ghostHand");

            if (pointer != null)
            {
                Undo.RecordObject(pointer.gameObject, "Hide Tutorial Pointer Preview");
                pointer.gameObject.SetActive(false);
            }

            if (ghostHand != null)
            {
                Undo.RecordObject(ghostHand.gameObject, "Hide Tutorial Ghost Preview");
                ghostHand.gameObject.SetActive(false);
            }

            if (overlayGroup != null)
            {
                Undo.RecordObject(overlayGroup, "Hide Tutorial Overlay Preview");
                overlayGroup.alpha = 0f;
                overlayGroup.interactable = false;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.gameObject.SetActive(false);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Clock Learning Game: Tutorial overlay preview hidden.");
        }

        private static T FindSceneObject<T>(string exactOrContainsName) where T : Component
        {
            T[] objects = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < objects.Length; i++)
            {
                T obj = objects[i];
                if (obj == null || !obj.gameObject.scene.IsValid()) continue;
                if (obj.name == exactOrContainsName) return obj;
            }

            for (int i = 0; i < objects.Length; i++)
            {
                T obj = objects[i];
                if (obj == null || !obj.gameObject.scene.IsValid()) continue;
                if (obj.name.Contains(exactOrContainsName)) return obj;
            }

            if (typeof(T) == typeof(ClockLearningTutorialController))
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    T obj = objects[i];
                    if (obj == null || !obj.gameObject.scene.IsValid()) continue;
                    return obj;
                }
            }

            return null;
        }

        private static RectTransform GetRect(Component component)
        {
            return component == null ? null : component.transform as RectTransform;
        }

        private static void TrySetObject(SerializedObject so, string fieldName, Object value)
        {
            if (value == null) return;
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void TrySetVector2(SerializedObject so, string fieldName, Vector2 value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.vector2Value = value;
        }

        private static void TrySetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.stringValue = value;
        }

        private static void TrySetFloat(SerializedObject so, string fieldName, float value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.floatValue = value;
        }

        private static void TrySetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.boolValue = value;
        }

        private static void TrySetInt(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.intValue = value;
        }

        private static T GetObject<T>(SerializedObject so, string fieldName) where T : Object
        {
            SerializedProperty property = so.FindProperty(fieldName);
            return property == null ? null : property.objectReferenceValue as T;
        }
    }
}
#endif
