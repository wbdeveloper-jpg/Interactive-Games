#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClockLearningGame
{
    public static class ClockLearningExistingSceneUpgradeInstaller
    {
        private const string InstallMenu = "Tools/Clock Learning Game/Upgrade Existing Scene/Install Skip + Exit Confirmation Only";

        [MenuItem(InstallMenu)]
        private static void InstallSkipAndExitConfirmationOnly()
        {
            ClockLearningGameManager manager = FindSceneObject<ClockLearningGameManager>("ClockLearningGameManager");
            ClockLearningTutorialController tutorial = FindSceneObject<ClockLearningTutorialController>("ClockLearningTutorialController");

            if (manager == null && tutorial == null)
            {
                Debug.LogWarning("Clock Learning Game: No ClockLearningGameManager or ClockLearningTutorialController found in the open scene.");
                return;
            }

            Canvas canvas = FindSceneCanvas(manager, tutorial);
            if (canvas == null)
            {
                Debug.LogWarning("Clock Learning Game: No Canvas found. Open your game scene and run this upgrade again.");
                return;
            }

            ClockLearningConfirmationDialog dialog = FindSceneObject<ClockLearningConfirmationDialog>("Clock Learning Confirmation Dialog");
            if (dialog == null)
            {
                dialog = CreateConfirmationDialog(canvas.transform as RectTransform);
            }

            if (tutorial != null)
            {
                Undo.RecordObject(tutorial, "Install Tutorial Skip Confirmation");
                SerializedObject tutorialSo = new SerializedObject(tutorial);
                CanvasGroup overlayGroup = GetObject<CanvasGroup>(tutorialSo, "overlayGroup");
                Button skipButton = FindSceneObject<Button>("Tutorial Skip Button");
                if (skipButton == null && overlayGroup != null)
                {
                    skipButton = CreateSkipButton(overlayGroup.transform as RectTransform);
                }

                TrySetObject(tutorialSo, "confirmationDialog", dialog);
                TrySetObject(tutorialSo, "skipTutorialButton", skipButton);
                TrySetBool(tutorialSo, "enableSkipButton", true);
                TrySetString(tutorialSo, "skipTutorialConfirmMessage", "Skip the tutorial?\nThe game will start now.");
                tutorialSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tutorial);
            }

            if (manager != null)
            {
                Undo.RecordObject(manager, "Install Exit Confirmation");
                SerializedObject managerSo = new SerializedObject(manager);
                TrySetObject(managerSo, "confirmationDialog", dialog);
                TrySetBool(managerSo, "confirmUnfinishedExit", true);
                TrySetString(managerSo, "unfinishedExitConfirmMessage", "Leave this game?\nYour game is not finished yet.");
                managerSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
            }

            EditorUtility.SetDirty(dialog);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Clock Learning Game: Installed skip tutorial and unfinished-exit confirmation only. No gameplay layout or finished UI art was regenerated.");
        }

        private static ClockLearningConfirmationDialog CreateConfirmationDialog(RectTransform canvasRoot)
        {
            GameObject root = new GameObject("Clock Learning Confirmation Dialog", typeof(RectTransform), typeof(CanvasGroup));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(canvasRoot, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.SetAsLastSibling();

            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            root.SetActive(false);

            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.38f);
            blocker.raycastTarget = true;

            RectTransform card = CreatePanel("Confirmation Card", rootRect, new Vector2(0.34f, 0.34f), new Vector2(0.66f, 0.66f), new Color(1f, 0.97f, 0.88f, 1f));
            TextMeshProUGUI title = CreateText(card, "Title", "Are you sure?", new Vector2(0.06f, 0.68f), new Vector2(0.94f, 0.92f), 34, FontStyles.Bold);
            TextMeshProUGUI message = CreateText(card, "Message", "Leave this game?", new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.68f), 24, FontStyles.Normal);

            Button confirmButton = CreateButton(card, "Confirm Button", "Yes", new Vector2(0.10f, 0.12f), new Vector2(0.46f, 0.34f), new Color(1f, 0.62f, 0.16f, 1f), out TextMeshProUGUI confirmText);
            Button cancelButton = CreateButton(card, "Cancel Button", "No", new Vector2(0.54f, 0.12f), new Vector2(0.90f, 0.34f), new Color(1f, 1f, 1f, 1f), out TextMeshProUGUI cancelText);

            ClockLearningConfirmationDialog dialog = root.AddComponent<ClockLearningConfirmationDialog>();
            SerializedObject so = new SerializedObject(dialog);
            TrySetObject(so, "dialogGroup", group);
            TrySetObject(so, "titleText", title);
            TrySetObject(so, "messageText", message);
            TrySetObject(so, "confirmButton", confirmButton);
            TrySetObject(so, "cancelButton", cancelButton);
            TrySetObject(so, "confirmButtonText", confirmText);
            TrySetObject(so, "cancelButtonText", cancelText);
            TrySetString(so, "defaultTitle", "Are you sure?");
            TrySetString(so, "defaultConfirmText", "Yes");
            TrySetString(so, "defaultCancelText", "No");
            so.ApplyModifiedPropertiesWithoutUndo();
            return dialog;
        }

        private static Button CreateSkipButton(RectTransform overlayRoot)
        {
            RectTransform rect = CreatePanel("Tutorial Skip Button", overlayRoot, new Vector2(0.84f, 0.88f), new Vector2(0.97f, 0.96f), new Color(1f, 0.86f, 0.36f, 0.95f));
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            TextMeshProUGUI label = CreateText(rect, "Label", "Skip", Vector2.zero, Vector2.one, 26, FontStyles.Bold);
            label.raycastTarget = false;
            rect.gameObject.SetActive(false);
            return button;
        }

        private static Button CreateButton(RectTransform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color color, out TextMeshProUGUI text)
        {
            RectTransform rect = CreatePanel(name, parent, anchorMin, anchorMax, color);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            text = CreateText(rect, "Label", label, Vector2.zero, Vector2.one, 24, FontStyles.Bold);
            text.raycastTarget = false;
            return button;
        }

        private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return rect;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string name, string value, Vector2 anchorMin, Vector2 anchorMax, int size, FontStyles style)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.23f, 0.17f, 0.08f, 1f);
            return text;
        }

        private static Canvas FindSceneCanvas(Component a, Component b)
        {
            Canvas canvas = a != null ? a.GetComponentInParent<Canvas>(true) : null;
            if (canvas != null) return canvas;
            canvas = b != null ? b.GetComponentInParent<Canvas>(true) : null;
            if (canvas != null) return canvas;
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject.scene.IsValid()) return canvases[i];
            }
            return null;
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
            return null;
        }

        private static T GetObject<T>(SerializedObject so, string fieldName) where T : Object
        {
            SerializedProperty property = so.FindProperty(fieldName);
            return property == null ? null : property.objectReferenceValue as T;
        }

        private static void TrySetObject(SerializedObject so, string fieldName, Object value)
        {
            if (value == null) return;
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void TrySetBool(SerializedObject so, string fieldName, bool value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.boolValue = value;
        }

        private static void TrySetString(SerializedObject so, string fieldName, string value)
        {
            SerializedProperty property = so.FindProperty(fieldName);
            if (property != null) property.stringValue = value;
        }
    }
}
#endif
