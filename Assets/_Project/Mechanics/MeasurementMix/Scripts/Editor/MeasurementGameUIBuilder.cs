#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MeasurementMix.Editor
{
    public static class MeasurementGameUIBuilder
    {
        private const string RootName = "MeasurementGameRoot";

        private static readonly Color Background = Hex("F3F7FF");
        private static readonly Color Navy = Hex("243B64");
        private static readonly Color Blue = Hex("4E8DFF");
        private static readonly Color BlueDark = Hex("326FD8");
        private static readonly Color Water = Hex("47B9F2");
        private static readonly Color Cream = Hex("FFF7DF");
        private static readonly Color Orange = Hex("F6A94A");
        private static readonly Color Green = Hex("52C78A");
        private static readonly Color Red = Hex("EF6A78");
        private static readonly Color White = Color.white;
        private static readonly Color SoftGrey = Hex("DDE6F4");

        [MenuItem("Tools/Measurement Game/Build Rough UI In Current Scene")]
        public static void BuildRoughUI()
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Measurement UI Already Exists",
                    "Replace the existing MeasurementGameRoot in the current " +
                    "scene? Other scene objects will not be changed.",
                    "Replace It",
                    "Cancel");

                if (!replace)
                    return;

                Undo.DestroyObjectImmediate(existing);
            }

            EnsureEventSystem();

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Build Measurement Game UI");

            ManagerBundle managers = CreateManagers(root.transform);
            Canvas canvas = CreateCanvas(root.transform);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();

            CreateImage(
                "Background_ArtworkSlot_1920x1080",
                canvasRect,
                Background,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);

            RectTransform safeArea = CreateRect(
                "SafeArea",
                canvasRect,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);

            RectTransform dragLayer = CreateRect(
                "DragLayer",
                canvasRect,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            dragLayer.SetAsLastSibling();

            TopUI top = CreateTopUI(safeArea);

            RectTransform gameplayArea = CreateRect(
                "GameplayArea",
                safeArea,
                Vector2.zero,
                new Vector2(-70f, -350f),
                Vector2.zero,
                Vector2.one);

            MassUI mass = CreateMassPanel(gameplayArea, dragLayer, managers.audio);
            LiquidUI liquid = CreateLiquidPanel(gameplayArea, managers.audio);
            ConversionUI conversion = CreateConversionPanel(gameplayArea);
            BottomUI bottom = CreateBottomUI(safeArea);
            ModalUI howTo = CreateHowToPlayPanel(safeArea);
            PauseUI pause = CreatePausePanel(safeArea);
            ResultUI result = CreateResultPanel(safeArea);

            MeasurementHintController hintController =
                bottom.hintButton.gameObject.AddComponent<MeasurementHintController>();
            hintController.hintButton = bottom.hintButton;
            hintController.hintButtonRect =
                bottom.hintButton.transform as RectTransform;
            hintController.hintButtonLabel = bottom.hintLabel;

            MeasurementGameManager gameManager =
                managers.managerObject.AddComponent<MeasurementGameManager>();
            gameManager.settings = managers.settings;
            gameManager.questionGenerator = managers.generator;
            gameManager.audioManager = managers.audio;
            gameManager.hintController = hintController;
            gameManager.balanceScaleController = mass.controller;
            gameManager.liquidController = liquid.controller;
            gameManager.conversionController = conversion.controller;
            gameManager.massPanel = mass.panel;
            gameManager.liquidPanel = liquid.panel;
            gameManager.conversionPanel = conversion.panel;
            gameManager.questionText = top.question;
            gameManager.roundText = top.round;
            gameManager.timerText = top.timer;
            gameManager.scoreText = top.score;
            gameManager.feedbackText = bottom.feedback;
            gameManager.checkButton = bottom.checkButton;
            gameManager.pauseButton = top.pauseButton;
            gameManager.howToPlayPanel = howTo.panel;
            gameManager.howToPlayStartButton = howTo.primaryButton;
            gameManager.pausePanel = pause.panel;
            gameManager.resumeButton = pause.resume;
            gameManager.restartButton = pause.restart;
            gameManager.pauseHomeButton = pause.home;
            gameManager.resultPanel = result.panel;
            gameManager.resultTitleText = result.title;
            gameManager.resultScoreText = result.score;
            gameManager.resultDetailText = result.detail;
            gameManager.replayButton = result.replay;
            gameManager.resultHomeButton = result.home;
            hintController.gameManager = gameManager;

            mass.panel.SetActive(true);
            liquid.panel.SetActive(true);
            conversion.panel.SetActive(true);
            howTo.panel.SetActive(true);
            pause.panel.SetActive(false);
            result.panel.SetActive(false);

            root.transform.SetAsLastSibling();
            Selection.activeGameObject = managers.managerObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "Rough Measurement UI Created",
                "The UI was added to the current scene and references were " +
                "connected. The scene was not saved.\n\nSelect " +
                "MeasurementGameSettings to choose the grade difficulty and units.",
                "Done");

            if (TMP_Settings.defaultFontAsset == null)
            {
                Debug.LogWarning(
                    "TextMeshPro has no default font asset. Import TMP Essential " +
                    "Resources before entering Play Mode.");
            }
        }

        private static ManagerBundle CreateManagers(Transform parent)
        {
            GameObject managersRoot = new GameObject("Managers");
            managersRoot.transform.SetParent(parent, false);

            GameObject settingsObject = new GameObject("MeasurementGameSettings");
            settingsObject.transform.SetParent(managersRoot.transform, false);
            MeasurementGameSettings settings =
                settingsObject.AddComponent<MeasurementGameSettings>();
            settings.ApplyRecommendedDefaults();

            GameObject generatorObject = new GameObject("MeasurementQuestionGenerator");
            generatorObject.transform.SetParent(managersRoot.transform, false);
            MeasurementQuestionGenerator generator =
                generatorObject.AddComponent<MeasurementQuestionGenerator>();

            GameObject audioObject = new GameObject("MeasurementAudioManager");
            audioObject.transform.SetParent(managersRoot.transform, false);
            MeasurementAudioManager audio =
                audioObject.AddComponent<MeasurementAudioManager>();
            AudioSource music = audioObject.AddComponent<AudioSource>();
            AudioSource effects = audioObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            effects.playOnAwake = false;
            audio.musicSource = music;
            audio.effectsSource = effects;

            GameObject managerObject = new GameObject("MeasurementGameManager");
            managerObject.transform.SetParent(managersRoot.transform, false);

            return new ManagerBundle
            {
                settings = settings,
                generator = generator,
                audio = audio,
                managerObject = managerObject
            };
        }

        private static TopUI CreateTopUI(RectTransform parent)
        {
            RectTransform bar = CreateImage(
                "TopBar",
                parent,
                White,
                new Vector2(0f, -68f),
                new Vector2(-50f, 120f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f));

            TMP_Text round = CreateText(
                "RoundText",
                bar,
                "Round 1 / 5",
                28,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(150f, 0f),
                new Vector2(250f, 70f),
                new Vector2(0f, 0.5f));

            TMP_Text score = CreateText(
                "ScoreText",
                bar,
                "Score: 0",
                28,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(-520f, 0f),
                new Vector2(220f, 70f),
                new Vector2(1f, 0.5f));

            TMP_Text timer = CreateText(
                "TimerText",
                bar,
                "Time: 50s",
                28,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(-285f, 0f),
                new Vector2(220f, 70f),
                new Vector2(1f, 0.5f));

            Button pause = CreateButton(
                "PauseButton",
                bar,
                "PAUSE",
                Navy,
                new Vector2(-92f, 0f),
                new Vector2(150f, 70f),
                new Vector2(1f, 0.5f),
                out TMP_Text unused);

            RectTransform questionCard = CreateImage(
                "QuestionCard",
                parent,
                Cream,
                new Vector2(0f, -155f),
                new Vector2(1100f, 88f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f));

            TMP_Text question = CreateText(
                "QuestionText",
                questionCard,
                "Measurement question",
                38,
                Navy,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(-44f, -16f),
                Vector2.zero,
                Vector2.one);

            return new TopUI
            {
                round = round,
                score = score,
                timer = timer,
                question = question,
                pauseButton = pause
            };
        }

        private static MassUI CreateMassPanel(
            RectTransform parent,
            RectTransform dragLayer,
            MeasurementAudioManager audio)
        {
            RectTransform panel = CreateImage(
                "PracticalMassPanel",
                parent,
                new Color(1f, 1f, 1f, 0.72f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            panel.gameObject.AddComponent<CanvasGroup>();

            BalanceScaleController controller =
                panel.gameObject.AddComponent<BalanceScaleController>();
            controller.audioManager = audio;

            CreateText(
                "Instruction",
                panel,
                "Drag the correct weights onto the right pan.",
                25,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(370f, -36f),
                new Vector2(690f, 50f),
                new Vector2(0f, 1f));

            CreateImage(
                "ScaleStand",
                panel,
                Navy,
                new Vector2(-220f, -115f),
                new Vector2(34f, 390f));
            CreateImage(
                "ScaleBase",
                panel,
                Navy,
                new Vector2(-220f, -320f),
                new Vector2(340f, 30f));

            RectTransform beam = CreateImage(
                "ScaleBeam_ArtworkSlot",
                panel,
                BlueDark,
                new Vector2(-220f, 55f),
                new Vector2(900f, 30f));
            controller.beam = beam;

            RectTransform leftPan = CreateImage(
                "ObjectPan_ArtworkSlot",
                panel,
                Cream,
                new Vector2(-665f, -85f),
                new Vector2(350f, 175f));
            RectTransform rightPan = CreateImage(
                "WeightPanDropArea",
                panel,
                new Color(0.88f, 0.95f, 1f, 1f),
                new Vector2(225f, -85f),
                new Vector2(430f, 175f));
            controller.leftPan = leftPan;
            controller.rightPan = rightPan;
            controller.rightPanDropArea = rightPan;

            CreateText(
                "ObjectTitle",
                leftPan,
                "TARGET MASS",
                21,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 50f),
                new Vector2(280f, 40f));

            controller.targetLabel = CreateText(
                "TargetMass",
                leftPan,
                "500 g",
                37,
                BlueDark,
                TextAlignmentOptions.Center,
                new Vector2(0f, -12f),
                new Vector2(290f, 72f));

            RectTransform panContent = CreateRect(
                "PlacedWeights",
                rightPan,
                Vector2.zero,
                new Vector2(-24f, -18f),
                Vector2.zero,
                Vector2.one);
            GridLayoutGroup panGrid =
                panContent.gameObject.AddComponent<GridLayoutGroup>();
            panGrid.cellSize = new Vector2(88f, 62f);
            panGrid.spacing = new Vector2(6f, 6f);
            panGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            panGrid.constraintCount = 4;
            panGrid.childAlignment = TextAnchor.MiddleCenter;
            controller.rightPanContent = panContent;

            controller.currentWeightLabel = CreateText(
                "CurrentMass",
                panel,
                "On pan: 0 g",
                28,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(225f, -215f),
                new Vector2(430f, 54f));

            RectTransform rack = CreateImage(
                "WeightRack",
                panel,
                SoftGrey,
                new Vector2(-30f, -310f),
                new Vector2(1240f, 170f));

            CreateText(
                "RackTitle",
                rack,
                "AVAILABLE WEIGHTS",
                20,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(150f, 56f),
                new Vector2(260f, 38f),
                new Vector2(0f, 0.5f));

            RectTransform tokenHome = CreateRect(
                "WeightTokens",
                rack,
                new Vector2(900f, 140f),
                new Vector2(135f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));
            GridLayoutGroup rackGrid =
                tokenHome.gameObject.AddComponent<GridLayoutGroup>();
            rackGrid.cellSize = new Vector2(136f, 58f);
            rackGrid.spacing = new Vector2(10f, 10f);
            rackGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            rackGrid.constraintCount = 6;
            rackGrid.childAlignment = TextAnchor.MiddleCenter;

            int[] values =
            {
                25, 25,
                50, 50,
                100, 100,
                200, 200,
                500, 500,
                1000, 1000
            };

            for (int index = 0; index < values.Length; index++)
            {
                int value = values[index];
                RectTransform tokenRect = CreateImage(
                    "Weight_" + value + "_" + index,
                    tokenHome,
                    value >= 500 ? Orange : Blue,
                    Vector2.zero,
                    new Vector2(136f, 58f));
                Image visual = tokenRect.GetComponent<Image>();
                TMP_Text label = CreateText(
                    "Value",
                    tokenRect,
                    value + " g",
                    21,
                    White,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.zero,
                    Vector2.one);

                MeasurementWeightItem item =
                    tokenRect.gameObject.AddComponent<MeasurementWeightItem>();
                item.Configure(value, controller, tokenHome, dragLayer, label, visual);
                controller.weightItems.Add(item);
            }

            return new MassUI
            {
                panel = panel.gameObject,
                controller = controller
            };
        }

        private static LiquidUI CreateLiquidPanel(
            RectTransform parent,
            MeasurementAudioManager audio)
        {
            RectTransform panel = CreateImage(
                "PracticalLiquidPanel",
                parent,
                new Color(1f, 1f, 1f, 0.72f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            panel.gameObject.AddComponent<CanvasGroup>();

            LiquidMeasurementController controller =
                panel.gameObject.AddComponent<LiquidMeasurementController>();
            controller.audioManager = audio;

            CreateText(
                "Instruction",
                panel,
                "Use the taps to reach the requested volume.",
                25,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(370f, -36f),
                new Vector2(690f, 50f),
                new Vector2(0f, 1f));

            RectTransform targetCard = CreateImage(
                "TargetCard",
                panel,
                Cream,
                new Vector2(-610f, 45f),
                new Vector2(380f, 190f));
            CreateText(
                "Title",
                targetCard,
                "TARGET VOLUME",
                23,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 50f),
                new Vector2(320f, 44f));
            controller.targetLabel = CreateText(
                "TargetVolume",
                targetCard,
                "1 L 500 mL",
                39,
                BlueDark,
                TextAlignmentOptions.Center,
                new Vector2(0f, -20f),
                new Vector2(340f, 76f));

            RectTransform beakerFrame = CreateImage(
                "Beaker_ArtworkSlot_420x580",
                panel,
                Navy,
                new Vector2(-105f, -55f),
                new Vector2(420f, 570f));

            RectTransform liquidArea = CreateImage(
                "LiquidArea",
                beakerFrame,
                White,
                new Vector2(-8f, -10f),
                new Vector2(330f, 500f));
            liquidArea.gameObject.AddComponent<RectMask2D>();
            controller.liquidArea = liquidArea;

            RectTransform waterFill = CreateImage(
                "WaterFill",
                liquidArea,
                Water,
                Vector2.zero,
                new Vector2(-18f, 0f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f));
            waterFill.pivot = new Vector2(0.5f, 0f);
            controller.waterFill = waterFill;

            RectTransform line = CreateImage(
                "HintTargetLine_HiddenAtStart",
                liquidArea,
                Red,
                Vector2.zero,
                new Vector2(322f, 8f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f));
            line.pivot = new Vector2(0.5f, 0.5f);
            CanvasGroup lineGroup = line.gameObject.AddComponent<CanvasGroup>();
            lineGroup.alpha = 0f;
            controller.targetLine = line;
            controller.targetLineGroup = lineGroup;

            for (int index = 0; index <= 40; index++)
            {
                float normalised = index / 40f;
                float y = -250f + normalised * 500f;
                bool major = index % 10 == 0;
                float width = major ? 58f : 34f;
                RectTransform mark = CreateImage(
                    "ScaleMark_" + index,
                    liquidArea,
                    Navy,
                    new Vector2(136f, y),
                    new Vector2(width, 3f));
                controller.scaleMarks.Add(mark);

                TMP_Text label = CreateText(
                    "ScaleLabel_" + index,
                    beakerFrame,
                    MeasurementQuestionGenerator.FormatPracticalLiquid(index * 50),
                    17,
                    Navy,
                    TextAlignmentOptions.MidlineLeft,
                    new Vector2(172f, y - 10f),
                    new Vector2(105f, 30f));
                label.gameObject.SetActive(major);
                controller.scaleLabels.Add(label);
            }

            controller.currentVolumeLabel = CreateText(
                "CurrentVolume",
                panel,
                "Current: 0 mL",
                29,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(-105f, -365f),
                new Vector2(460f, 58f));

            RectTransform controls = CreateImage(
                "TapControls_ArtworkSlot",
                panel,
                SoftGrey,
                new Vector2(560f, -30f),
                new Vector2(440f, 420f));
            CreateText(
                "Title",
                controls,
                "WATER TAPS",
                24,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 153f),
                new Vector2(340f, 45f));

            Button add = CreateButton(
                "AddWaterButton",
                controls,
                "ADD WATER  +",
                Green,
                new Vector2(0f, 68f),
                new Vector2(330f, 90f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text addLabel);
            Button remove = CreateButton(
                "RemoveWaterButton",
                controls,
                "REMOVE WATER  −",
                Orange,
                new Vector2(0f, -48f),
                new Vector2(330f, 90f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text removeLabel);

            controller.stepLabel = CreateText(
                "StepLabel",
                controls,
                "Each tap: 100 mL",
                21,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, -143f),
                new Vector2(370f, 48f));
            controller.addWaterButton = add;
            controller.removeWaterButton = remove;

            RectTransform streamRect = CreateImage(
                "WaterStream",
                controls,
                Water,
                new Vector2(-155f, 7f),
                new Vector2(22f, 150f));
            Image streamImage = streamRect.GetComponent<Image>();
            streamImage.color = new Color(Water.r, Water.g, Water.b, 0f);
            streamRect.gameObject.SetActive(false);
            controller.waterStream = streamImage;

            line.gameObject.SetActive(false);

            return new LiquidUI
            {
                panel = panel.gameObject,
                controller = controller
            };
        }

        private static ConversionUI CreateConversionPanel(RectTransform parent)
        {
            RectTransform panel = CreateImage(
                "UnitConversionPanel",
                parent,
                new Color(1f, 1f, 1f, 0.78f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            panel.gameObject.AddComponent<CanvasGroup>();
            MeasurementConversionController controller =
                panel.gameObject.AddComponent<MeasurementConversionController>();

            controller.categoryLabel = CreateText(
                "CategoryLabel",
                panel,
                "MASS CONVERSION",
                24,
                BlueDark,
                TextAlignmentOptions.Center,
                new Vector2(0f, -50f),
                new Vector2(500f, 54f),
                new Vector2(0.5f, 1f));

            CreateText(
                "Instruction",
                panel,
                "Choose the measurement with the same value.",
                27,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, -115f),
                new Vector2(900f, 55f),
                new Vector2(0.5f, 1f));

            Vector2[] positions =
            {
                new Vector2(-350f, 120f),
                new Vector2(350f, 120f),
                new Vector2(-350f, -55f),
                new Vector2(350f, -55f)
            };

            for (int index = 0; index < positions.Length; index++)
            {
                Button option = CreateButton(
                    "Option_" + (index + 1),
                    panel,
                    "1,000 g",
                    new Color(0.93f, 0.96f, 1f, 1f),
                    positions[index],
                    new Vector2(570f, 130f),
                    new Vector2(0.5f, 0.5f),
                    out TMP_Text label);
                label.color = Navy;
                label.fontSize = 34f;
                controller.optionButtons.Add(option);
                controller.optionLabels.Add(label);
                controller.optionBackgrounds.Add(option.GetComponent<Image>());
            }

            CreateText(
                "ConversionHelp",
                panel,
                "Think about how many smaller units make one larger unit.",
                23,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, -255f),
                new Vector2(1100f, 58f));

            return new ConversionUI
            {
                panel = panel.gameObject,
                controller = controller
            };
        }

        private static BottomUI CreateBottomUI(RectTransform parent)
        {
            TMP_Text feedback = CreateText(
                "FeedbackText",
                parent,
                "Get ready to measure.",
                24,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(600f, 62f),
                new Vector2(1120f, 72f),
                new Vector2(0f, 0f));

            Button hint = CreateButton(
                "HintButton",
                parent,
                "HINT",
                Orange,
                new Vector2(-475f, 62f),
                new Vector2(250f, 82f),
                new Vector2(1f, 0f),
                out TMP_Text hintLabel);

            Button check = CreateButton(
                "CheckButton",
                parent,
                "CHECK",
                Blue,
                new Vector2(-180f, 62f),
                new Vector2(280f, 82f),
                new Vector2(1f, 0f),
                out TMP_Text checkLabel);

            return new BottomUI
            {
                feedback = feedback,
                hintButton = hint,
                hintLabel = hintLabel,
                checkButton = check
            };
        }

        private static ModalUI CreateHowToPlayPanel(RectTransform parent)
        {
            RectTransform overlay = CreateOverlay("HowToPlayPanel", parent);
            RectTransform card = CreateImage(
                "HowToPlayCard",
                overlay,
                White,
                Vector2.zero,
                new Vector2(920f, 620f));

            CreateText(
                "Title",
                card,
                "HOW TO PLAY",
                43,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 230f),
                new Vector2(700f, 70f));

            CreateText(
                "Instructions",
                card,
                "• Balance mass by dragging weights onto the pan.\n\n" +
                "• Measure liquid by adding or removing water.\n\n" +
                "• Choose equal values in conversion questions.\n\n" +
                "• If you are stuck, tap HINT.",
                28,
                Navy,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 25f),
                new Vector2(720f, 320f));

            Button start = CreateButton(
                "StartButton",
                card,
                "START GAME",
                Green,
                new Vector2(0f, -235f),
                new Vector2(320f, 88f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text label);

            return new ModalUI
            {
                panel = overlay.gameObject,
                primaryButton = start
            };
        }

        private static PauseUI CreatePausePanel(RectTransform parent)
        {
            RectTransform overlay = CreateOverlay("PausePanel", parent);
            RectTransform card = CreateImage(
                "PauseCard",
                overlay,
                White,
                Vector2.zero,
                new Vector2(700f, 560f));

            CreateText(
                "Title",
                card,
                "GAME PAUSED",
                43,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 190f),
                new Vector2(560f, 70f));

            Button resume = CreateButton(
                "ResumeButton",
                card,
                "RESUME",
                Green,
                new Vector2(0f, 80f),
                new Vector2(330f, 82f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text resumeLabel);
            Button restart = CreateButton(
                "RestartButton",
                card,
                "RESTART",
                Blue,
                new Vector2(0f, -20f),
                new Vector2(330f, 82f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text restartLabel);
            Button home = CreateButton(
                "HomeButton",
                card,
                "HOME",
                Orange,
                new Vector2(0f, -120f),
                new Vector2(330f, 82f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text homeLabel);

            return new PauseUI
            {
                panel = overlay.gameObject,
                resume = resume,
                restart = restart,
                home = home
            };
        }

        private static ResultUI CreateResultPanel(RectTransform parent)
        {
            RectTransform overlay = CreateOverlay("ResultPanel", parent);
            RectTransform card = CreateImage(
                "ResultCard",
                overlay,
                White,
                Vector2.zero,
                new Vector2(800f, 580f));

            TMP_Text title = CreateText(
                "ResultTitle",
                card,
                "Measurement challenge complete!",
                41,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 190f),
                new Vector2(690f, 90f));
            TMP_Text score = CreateText(
                "ResultScore",
                card,
                "Score: 0",
                36,
                BlueDark,
                TextAlignmentOptions.Center,
                new Vector2(0f, 90f),
                new Vector2(570f, 64f));
            TMP_Text detail = CreateText(
                "ResultDetail",
                card,
                "Correct answers: 0 / 5",
                27,
                Navy,
                TextAlignmentOptions.Center,
                new Vector2(0f, 25f),
                new Vector2(570f, 54f));

            Button replay = CreateButton(
                "ReplayButton",
                card,
                "PLAY AGAIN",
                Green,
                new Vector2(0f, -90f),
                new Vector2(330f, 84f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text replayLabel);
            Button home = CreateButton(
                "HomeButton",
                card,
                "HOME",
                Orange,
                new Vector2(0f, -190f),
                new Vector2(330f, 84f),
                new Vector2(0.5f, 0.5f),
                out TMP_Text homeLabel);

            return new ResultUI
            {
                panel = overlay.gameObject,
                title = title,
                score = score,
                detail = detail,
                replay = replay,
                home = home
            };
        }

        private static RectTransform CreateOverlay(string name, RectTransform parent)
        {
            RectTransform overlay = CreateImage(
                name,
                parent,
                new Color(0.07f, 0.12f, 0.22f, 0.75f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            overlay.SetAsLastSibling();
            return overlay;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject(
                "MeasurementGameCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            Type inputSystemModule = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemModule != null)
                eventSystem.AddComponent(inputSystemModule);
            else
                eventSystem.AddComponent<StandaloneInputModule>();

            Undo.RegisterCreatedObjectUndo(eventSystem, "Create Event System");
        }

        private static RectTransform CreateImage(
            string name,
            RectTransform parent,
            Color colour,
            Vector2 position,
            Vector2 size)
        {
            return CreateImage(
                name,
                parent,
                colour,
                position,
                size,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));
        }

        private static RectTransform CreateImage(
            string name,
            RectTransform parent,
            Color colour,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rect = CreateRect(
                name,
                parent,
                position,
                size,
                anchorMin,
                anchorMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = colour;
            return rect;
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            string textValue,
            float fontSize,
            Color colour,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size)
        {
            return CreateText(
                name,
                parent,
                textValue,
                fontSize,
                colour,
                alignment,
                position,
                size,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f));
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            string textValue,
            float fontSize,
            Color colour,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            Vector2 anchor)
        {
            return CreateText(
                name,
                parent,
                textValue,
                fontSize,
                colour,
                alignment,
                position,
                size,
                anchor,
                anchor);
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            string textValue,
            float fontSize,
            Color colour,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rect = CreateRect(
                name,
                parent,
                position,
                size,
                anchorMin,
                anchorMax);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.fontSize = fontSize;
            text.color = colour;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(14f, fontSize - 10f);
            text.fontSizeMax = fontSize;
            text.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            string labelText,
            Color colour,
            Vector2 position,
            Vector2 size,
            Vector2 anchor,
            out TMP_Text label)
        {
            RectTransform rect = CreateImage(
                name,
                parent,
                colour,
                position,
                size,
                anchor,
                anchor);
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            ColorBlock colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colours.pressedColor = new Color(0.82f, 0.88f, 1f, 1f);
            colours.disabledColor = new Color(0.65f, 0.68f, 0.72f, 0.65f);
            colours.colorMultiplier = 1f;
            button.colors = colours;

            label = CreateText(
                "Label",
                rect,
                labelText,
                27,
                White,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                Vector2.one);
            return button;
        }

        private static RectTransform CreateRect(
            string name,
            RectTransform parent,
            Vector2 position,
            Vector2 size,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color colour);
            return colour;
        }

        private struct ManagerBundle
        {
            public MeasurementGameSettings settings;
            public MeasurementQuestionGenerator generator;
            public MeasurementAudioManager audio;
            public GameObject managerObject;
        }

        private struct TopUI
        {
            public TMP_Text round;
            public TMP_Text score;
            public TMP_Text timer;
            public TMP_Text question;
            public Button pauseButton;
        }

        private struct MassUI
        {
            public GameObject panel;
            public BalanceScaleController controller;
        }

        private struct LiquidUI
        {
            public GameObject panel;
            public LiquidMeasurementController controller;
        }

        private struct ConversionUI
        {
            public GameObject panel;
            public MeasurementConversionController controller;
        }

        private struct BottomUI
        {
            public TMP_Text feedback;
            public Button hintButton;
            public TMP_Text hintLabel;
            public Button checkButton;
        }

        private struct ModalUI
        {
            public GameObject panel;
            public Button primaryButton;
        }

        private struct PauseUI
        {
            public GameObject panel;
            public Button resume;
            public Button restart;
            public Button home;
        }

        private struct ResultUI
        {
            public GameObject panel;
            public TMP_Text title;
            public TMP_Text score;
            public TMP_Text detail;
            public Button replay;
            public Button home;
        }
    }
}
#endif
