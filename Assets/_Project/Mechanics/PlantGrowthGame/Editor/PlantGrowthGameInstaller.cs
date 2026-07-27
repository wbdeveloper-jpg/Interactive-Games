#if UNITY_EDITOR
using System;
using System.IO;
using PlantGrowthGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PlantGrowthGameEditor
{
    public static class PlantGrowthGameInstaller
    {
        private const string RootName = "PlantGrowthGame_UI";
        private static readonly Color DarkBrown = new Color32(66, 27, 12, 255);
        private static readonly Color Cream = new Color32(255, 248, 222, 250);
        private static readonly Color Green = new Color32(142, 204, 25, 255);

        [MenuItem("Tools/Plant Growth Game/Install UI Into Current Scene")]
        public static void InstallIntoCurrentScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                EditorUtility.DisplayDialog(
                    "Plant Growth Game",
                    "Open the scene that should receive the game UI, then run this command again.",
                    "OK");
                return;
            }

            GameObject existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                Selection.activeGameObject = existingRoot;
                EditorUtility.DisplayDialog(
                    "Plant Growth Game",
                    "The UI is already installed in this scene. The existing game root has been selected.",
                    "OK");
                return;
            }

            Sprite[] stageSprites = LoadStageSprites();
            if (stageSprites == null)
            {
                return;
            }

            GameObject root = new GameObject(
                RootName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(PlantGrowthGameController));
            Undo.RegisterCreatedObjectUndo(root, "Install Plant Growth Game UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Image stageImage = CreateImage("Stage Artwork", root.transform);
            Stretch(stageImage.rectTransform);
            stageImage.sprite = stageSprites[0];
            stageImage.preserveAspect = false;
            stageImage.raycastTarget = false;
            CanvasGroup stageCanvas = stageImage.gameObject.AddComponent<CanvasGroup>();

            Button pauseButton = CreateTransparentButton("Pause Hit Zone", root.transform);
            SetTopCorner(pauseButton.GetComponent<RectTransform>(), true);

            Button soundButton = CreateTransparentButton("Sound Hit Zone", root.transform);
            SetTopCorner(soundButton.GetComponent<RectTransform>(), false);

            Button[] optionButtons = new Button[3];
            float[] optionX = { -470f, 0f, 470f };
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i] = CreateTransparentButton(
                    "Option " + (i + 1) + " Hit Zone",
                    root.transform);
                RectTransform rect = optionButtons[i].GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(390f, 320f);
                rect.anchoredPosition = new Vector2(optionX[i], 180f);
            }

            Button primaryAction = CreateTransparentButton(
                "Play Harvest Hit Zone",
                root.transform);
            RectTransform primaryRect = primaryAction.GetComponent<RectTransform>();
            primaryRect.anchorMin = new Vector2(0.5f, 0f);
            primaryRect.anchorMax = new Vector2(0.5f, 0f);
            primaryRect.pivot = new Vector2(0.5f, 0f);
            primaryRect.sizeDelta = new Vector2(620f, 185f);
            primaryRect.anchoredPosition = new Vector2(0f, 25f);

            CanvasGroup feedbackGroup;
            Text feedbackText;
            CreateFeedbackPanel(root.transform, out feedbackGroup, out feedbackText);

            GameObject pausePanel;
            Button resumeButton;
            Button restartButton;
            Button exitButton;
            CreatePausePanel(
                root.transform,
                out pausePanel,
                out resumeButton,
                out restartButton,
                out exitButton);

            AudioSource musicSource = root.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            AudioSource sfxSource = root.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;

            EnsureEventSystem();
            ConfigureController(
                root.GetComponent<PlantGrowthGameController>(),
                stageSprites,
                stageImage,
                stageCanvas,
                optionButtons,
                primaryAction,
                pauseButton,
                soundButton,
                pausePanel,
                resumeButton,
                restartButton,
                exitButton,
                feedbackGroup,
                feedbackText,
                musicSource,
                sfxSource);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorUtility.DisplayDialog(
                "Plant Growth Game Installed",
                "The complete UI was added to the currently open scene.\n\n" +
                "Your scene was not replaced or saved automatically. Review the selected PlantGrowthGame_UI object, connect optional audio and callbacks, then save your scene.",
                "Done");
        }

        private static Sprite[] LoadStageSprites()
        {
            string[] filenames =
            {
                "Stage_00_Welcome.png",
                "Stage_01_Water.png",
                "Stage_02_Warmth.png",
                "Stage_03_Sunlight.png",
                "Stage_04_Pollination.png",
                "Stage_05_Ripening.png",
                "Stage_06_Harvest.png"
            };

            Sprite[] sprites = new Sprite[filenames.Length];
            for (int i = 0; i < filenames.Length; i++)
            {
                string assetPath = FindAssetPath(filenames[i]);
                if (string.IsNullOrEmpty(assetPath))
                {
                    EditorUtility.DisplayDialog(
                        "Plant Growth Game",
                        "Could not find required artwork: " + filenames[i] +
                        "\nKeep the Art folder inside your Unity Assets folder.",
                        "OK");
                    return null;
                }

                EnsureSpriteImport(assetPath);
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprites[i] == null)
                {
                    EditorUtility.DisplayDialog(
                        "Plant Growth Game",
                        "Unity could not import this image as a Sprite:\n" + assetPath,
                        "OK");
                    return null;
                }
            }

            return sprites;
        }

        private static string FindAssetPath(string filename)
        {
            string nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            string[] guids = AssetDatabase.FindAssets(nameWithoutExtension + " t:Texture2D");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith("/PlantGrowthGame/Art/" + filename, StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private static void EnsureSpriteImport(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool needsImport = importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp;

            if (!needsImport)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Image CreateImage(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<Image>();
        }

        private static Button CreateTransparentButton(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);

            Image image = gameObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            Button button = gameObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            return button;
        }

        private static void CreateFeedbackPanel(
            Transform parent,
            out CanvasGroup canvasGroup,
            out Text feedbackText)
        {
            Image panel = CreateImage("Feedback Banner", parent);
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(920f, 125f);
            panelRect.anchoredPosition = new Vector2(0f, -40f);
            panel.color = new Color(0.88f, 1f, 0.78f, 0.98f);
            panel.raycastTarget = false;

            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            feedbackText = CreateText("Feedback Text", panel.transform);
            Stretch(feedbackText.rectTransform, 35f, 20f, 35f, 20f);
            feedbackText.fontSize = 38;
            feedbackText.fontStyle = FontStyle.Bold;
            feedbackText.alignment = TextAnchor.MiddleCenter;
            feedbackText.color = DarkBrown;
            feedbackText.resizeTextForBestFit = true;
            feedbackText.resizeTextMinSize = 22;
            feedbackText.resizeTextMaxSize = 38;
            feedbackText.raycastTarget = false;

            panel.gameObject.SetActive(false);
        }

        private static void CreatePausePanel(
            Transform parent,
            out GameObject pausePanel,
            out Button resumeButton,
            out Button restartButton,
            out Button exitButton)
        {
            Image overlay = CreateImage("Pause Panel", parent);
            Stretch(overlay.rectTransform);
            overlay.color = new Color(0.05f, 0.12f, 0.15f, 0.82f);
            CanvasGroup pauseCanvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.blocksRaycasts = false;
            pausePanel = overlay.gameObject;

            Image card = CreateImage("Pause Card", overlay.transform);
            RectTransform cardRect = card.rectTransform;
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(620f, 610f);
            cardRect.anchoredPosition = Vector2.zero;
            card.color = Cream;

            Text title = CreateText("Title", card.transform);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(520f, 90f), new Vector2(0f, -70f));
            title.text = "Game Paused";
            title.fontSize = 48;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = DarkBrown;

            resumeButton = CreateVisibleButton(
                "Resume Button",
                card.transform,
                "RESUME",
                new Vector2(0f, 85f),
                Green);
            restartButton = CreateVisibleButton(
                "Restart Button",
                card.transform,
                "RESTART",
                new Vector2(0f, -70f),
                new Color32(66, 167, 225, 255));
            exitButton = CreateVisibleButton(
                "Exit Button",
                card.transform,
                "EXIT",
                new Vector2(0f, -225f),
                new Color32(234, 125, 92, 255));

            pausePanel.SetActive(false);
        }

        private static Button CreateVisibleButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Color colour)
        {
            GameObject gameObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            gameObject.transform.SetParent(parent, false);

            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(430f, 115f);
            rect.anchoredPosition = position;

            Image image = gameObject.GetComponent<Image>();
            image.color = colour;

            Button button = gameObject.GetComponent<Button>();
            ColorBlock colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colours.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colours.selectedColor = Color.white;
            button.colors = colours;

            Text text = CreateText("Label", gameObject.transform);
            Stretch(text.rectTransform, 20f, 10f, 20f, 10f);
            text.text = label;
            text.fontSize = 38;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return button;
        }

        private static Text CreateText(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.GetComponent<Text>();
            text.font = GetBuiltInFont();
            return text;
        }

        private static Font GetBuiltInFont()
        {
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    return font;
                }
            }
            catch
            {
                // Older Unity releases use Arial.ttf instead.
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static void ConfigureController(
            PlantGrowthGameController controller,
            Sprite[] sprites,
            Image stageImage,
            CanvasGroup stageCanvas,
            Button[] options,
            Button primaryAction,
            Button pauseButton,
            Button soundButton,
            GameObject pausePanel,
            Button resumeButton,
            Button restartButton,
            Button exitButton,
            CanvasGroup feedbackGroup,
            Text feedbackText,
            AudioSource musicSource,
            AudioSource sfxSource)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            AssignObject(serializedController, "stageImage", stageImage);
            AssignObject(serializedController, "stageCanvasGroup", stageCanvas);
            AssignObject(serializedController, "primaryActionButton", primaryAction);
            AssignObject(serializedController, "pauseButton", pauseButton);
            AssignObject(serializedController, "soundButton", soundButton);
            AssignObject(serializedController, "pausePanel", pausePanel);
            AssignObject(serializedController, "resumeButton", resumeButton);
            AssignObject(serializedController, "restartButton", restartButton);
            AssignObject(serializedController, "exitButton", exitButton);
            AssignObject(serializedController, "feedbackCanvasGroup", feedbackGroup);
            AssignObject(serializedController, "feedbackText", feedbackText);
            AssignObject(serializedController, "musicSource", musicSource);
            AssignObject(serializedController, "sfxSource", sfxSource);

            SerializedProperty spriteArray = serializedController.FindProperty("stageSprites");
            spriteArray.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                spriteArray.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }

            SerializedProperty optionArray = serializedController.FindProperty("optionButtons");
            optionArray.arraySize = options.Length;
            for (int i = 0; i < options.Length; i++)
            {
                optionArray.GetArrayElementAtIndex(i).objectReferenceValue = options[i];
            }

            SerializedProperty answers = serializedController.FindProperty("correctOptionIndexes");
            answers.arraySize = 5;
            int[] answerIndexes = { 0, 1, 2, 0, 1 };
            for (int i = 0; i < answerIndexes.Length; i++)
            {
                answers.GetArrayElementAtIndex(i).intValue = answerIndexes[i];
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void AssignObject(
            SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create Event System");

            Type inputSystemModule = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSystemModule != null)
            {
                eventSystem.AddComponent(inputSystemModule);
            }
            else
            {
                eventSystem.AddComponent<StandaloneInputModule>();
            }
        }

        private static void SetTopCorner(RectTransform rect, bool left)
        {
            float x = left ? 78f : -78f;
            rect.anchorMin = new Vector2(left ? 0f : 1f, 1f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(135f, 135f);
            rect.anchoredPosition = new Vector2(x, -68f);
        }

        private static void Stretch(
            RectTransform rect,
            float left = 0f,
            float bottom = 0f,
            float right = 0f,
            float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Vector2 position)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
#endif
