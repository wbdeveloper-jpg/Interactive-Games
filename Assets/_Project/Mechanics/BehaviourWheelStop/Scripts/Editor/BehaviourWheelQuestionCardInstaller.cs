#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BehaviourWheelStop.Editor
{
    public static class BehaviourWheelQuestionCardInstaller
    {
        private const string QuestionCardRootName = "[BehaviourWheel Question Card]";

        [MenuItem("Tools/Behaviour Wheel Stop/Install or Upgrade Question Card")]
        public static void InstallOrUpgrade()
        {
            BehaviourWheelGameManager manager = Object.FindObjectOfType<BehaviourWheelGameManager>();
            if (manager == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Question Card",
                    "No BehaviourWheelGameManager was found in the open scene.", "OK");
                return;
            }

            if (manager.ui == null || manager.ui.questionText == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Question Card",
                    "The Game Manager needs a BehaviourWheelUI with Question Text assigned before installation.", "OK");
                return;
            }

            Canvas canvas = manager.ui.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = Object.FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Question Card",
                    "No Canvas was found in the open scene.", "OK");
                return;
            }

            BehaviourWheelQuestionCard presenter = FindSceneQuestionCard();
            if (presenter == null)
            {
                Transform existingRoot = canvas.transform.Find(QuestionCardRootName);
                GameObject rootObject;
                if (existingRoot != null)
                {
                    rootObject = existingRoot.gameObject;
                }
                else
                {
                    rootObject = new GameObject(QuestionCardRootName, typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(rootObject, "Create Behaviour Wheel Question Card");
                    rootObject.transform.SetParent(canvas.transform, false);
                }

                presenter = GetOrAddComponent<BehaviourWheelQuestionCard>(rootObject);
            }

            RectTransform root = presenter.transform as RectTransform;
            if (root == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Question Card",
                    "The Question Card root must use a RectTransform.", "OK");
                return;
            }

            if (root.parent != canvas.transform)
                root.SetParent(canvas.transform, false);

            presenter.gameObject.SetActive(true);
            StretchFull(root);
            root.SetAsLastSibling();

            Image dimBackground = GetOrAddComponent<Image>(presenter.gameObject);
            dimBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            dimBackground.type = Image.Type.Sliced;
            dimBackground.color = new Color(0.025f, 0.04f, 0.075f, 0.68f);
            dimBackground.raycastTarget = true;

            CanvasGroup overlayGroup = GetOrAddComponent<CanvasGroup>(presenter.gameObject);
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;

            RectTransform card = GetOrCreateRect(root, "Question Card", out _);
            SetCenteredRect(card, new Vector2(860f, 400f));
            Image cardImage = GetOrAddComponent<Image>(card.gameObject);
            cardImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            cardImage.type = Image.Type.Sliced;
            cardImage.color = new Color(0.965f, 0.98f, 1f, 1f);
            cardImage.raycastTarget = false;

            RectTransform titleRect = GetOrCreateRect(card, "Title", out _);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(34f, -82f);
            titleRect.offsetMax = new Vector2(-34f, -22f);
            TextMeshProUGUI title = GetOrAddComponent<TextMeshProUGUI>(titleRect.gameObject);
            title.text = "QUESTION";
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.fontSize = 30f;
            title.color = new Color(0.12f, 0.28f, 0.52f, 1f);
            title.raycastTarget = false;
            if (manager.ui.questionText.font != null)
                title.font = manager.ui.questionText.font;

            RectTransform questionRect = GetOrCreateRect(card, "Question Text", out _);
            questionRect.anchorMin = Vector2.zero;
            questionRect.anchorMax = Vector2.one;
            questionRect.pivot = new Vector2(0.5f, 0.5f);
            questionRect.offsetMin = new Vector2(48f, 120f);
            questionRect.offsetMax = new Vector2(-48f, -88f);
            TextMeshProUGUI questionText = GetOrAddComponent<TextMeshProUGUI>(questionRect.gameObject);
            questionText.text = "Question appears here";
            questionText.alignment = TextAlignmentOptions.Center;
            questionText.fontStyle = FontStyles.Bold;
            questionText.color = new Color(0.07f, 0.10f, 0.16f, 1f);
            questionText.enableWordWrapping = true;
            questionText.enableAutoSizing = true;
            questionText.fontSize = 44f;
            questionText.fontSizeMin = 28f;
            questionText.fontSizeMax = 48f;
            questionText.overflowMode = TextOverflowModes.Overflow;
            questionText.margin = new Vector4(4f, 4f, 4f, 4f);
            questionText.raycastTarget = false;
            if (manager.ui.questionText.font != null)
                questionText.font = manager.ui.questionText.font;

            RectTransform continueRect = GetOrCreateRect(card, "Countdown Continue Button", out _);
            continueRect.anchorMin = new Vector2(0.5f, 0f);
            continueRect.anchorMax = new Vector2(0.5f, 0f);
            continueRect.pivot = new Vector2(0.5f, 0f);
            continueRect.anchoredPosition = new Vector2(0f, 28f);
            continueRect.sizeDelta = new Vector2(390f, 68f);
            Image continueImage = GetOrAddComponent<Image>(continueRect.gameObject);
            continueImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            continueImage.type = Image.Type.Sliced;
            continueImage.color = new Color(0.14f, 0.42f, 0.82f, 1f);
            continueImage.raycastTarget = true;
            Button continueButton = GetOrAddComponent<Button>(continueRect.gameObject);
            continueButton.targetGraphic = continueImage;
            continueButton.transition = Selectable.Transition.ColorTint;

            RectTransform continueTextRect = GetOrCreateRect(continueRect, "Text", out _);
            StretchFull(continueTextRect);
            continueTextRect.offsetMin = new Vector2(18f, 8f);
            continueTextRect.offsetMax = new Vector2(-18f, -8f);
            TextMeshProUGUI continueText = GetOrAddComponent<TextMeshProUGUI>(continueTextRect.gameObject);
            continueText.text = "Continuing in... 3";
            continueText.alignment = TextAlignmentOptions.Center;
            continueText.fontStyle = FontStyles.Bold;
            continueText.fontSize = 28f;
            continueText.enableAutoSizing = true;
            continueText.fontSizeMin = 22f;
            continueText.fontSizeMax = 30f;
            continueText.color = Color.white;
            continueText.raycastTarget = false;
            if (manager.ui.questionText.font != null)
                continueText.font = manager.ui.questionText.font;

            RectTransform gameplayQuestionCard = manager.ui.questionText.transform.parent as RectTransform;
            if (gameplayQuestionCard == null)
            {
                EditorUtility.DisplayDialog("Behaviour Wheel Question Card",
                    "The existing gameplay Question Text must be inside a RectTransform card.", "OK");
                return;
            }

            CanvasGroup gameplayQuestionGroup = GetOrAddComponent<CanvasGroup>(gameplayQuestionCard.gameObject);
            gameplayQuestionGroup.alpha = 1f;
            gameplayQuestionGroup.interactable = true;
            gameplayQuestionGroup.blocksRaycasts = true;

            RectTransform tapTarget = GetOrCreateRect(gameplayQuestionCard, "[Question Card Tap Target]", out _);
            StretchFull(tapTarget);
            tapTarget.SetAsLastSibling();
            Image tapTargetImage = GetOrAddComponent<Image>(tapTarget.gameObject);
            tapTargetImage.color = Color.clear;
            tapTargetImage.raycastTarget = true;
            Button gameplayQuestionButton = GetOrAddComponent<Button>(tapTarget.gameObject);
            gameplayQuestionButton.transition = Selectable.Transition.None;
            gameplayQuestionButton.targetGraphic = tapTargetImage;
            gameplayQuestionButton.interactable = true;
            Navigation navigation = gameplayQuestionButton.navigation;
            navigation.mode = Navigation.Mode.None;
            gameplayQuestionButton.navigation = navigation;

            Undo.RecordObject(presenter, "Configure Behaviour Wheel Question Card");
            // Upgrade only the former package defaults. Deliberately preserve any
            // duration the project owner already customized in the Inspector.
            if (Mathf.Approximately(presenter.questionStartDuration, 2.8f))
                presenter.questionStartDuration = 5f;
            if (Mathf.Approximately(presenter.reopenedDuration, 2.2f))
                presenter.reopenedDuration = 5f;
            presenter.overlayCanvasGroup = overlayGroup;
            presenter.questionCardPanel = card;
            presenter.questionCardText = questionText;
            presenter.continueButton = continueButton;
            presenter.continueButtonText = continueText;
            presenter.gameplayQuestionCanvasGroup = gameplayQuestionGroup;
            presenter.gameplayQuestionButton = gameplayQuestionButton;

            Undo.RecordObject(manager, "Connect Behaviour Wheel Question Card");
            manager.questionCard = presenter;

            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(continueButton);
            EditorUtility.SetDirty(gameplayQuestionGroup);
            EditorUtility.SetDirty(gameplayQuestionButton);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = presenter.gameObject;

            EditorUtility.DisplayDialog("Behaviour Wheel Question Card",
                "Installation/upgrade complete.\n\nThe normal gameplay question now fades while the expanded card is open. The countdown button closes automatically at zero and can also be tapped to continue early.",
                "OK");
        }

        private static BehaviourWheelQuestionCard FindSceneQuestionCard()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            return Resources.FindObjectsOfTypeAll<BehaviourWheelQuestionCard>()
                .FirstOrDefault(item => item != null && item.gameObject.scene == activeScene);
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static RectTransform GetOrCreateRect(RectTransform parent, string name, out bool created)
        {
            Transform existing = parent.Find(name);
            if (existing is RectTransform existingRect)
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

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
    }
}
#endif
