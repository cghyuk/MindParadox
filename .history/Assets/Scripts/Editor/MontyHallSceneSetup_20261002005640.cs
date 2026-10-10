using System;
using System.Collections.Generic;
using System.IO;
using MindParadox.Games.MontyHall;
using MindParadox.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MindParadox.EditorTools
{
    /// <summary>
    /// MontyHallScene이 없으면 기본 카드/버튼만으로 3문 화면을 만든다.
    /// </summary>
    [InitializeOnLoad]
    public static class MontyHallSceneSetup
    {
        const string ScenePath = "Assets/Scenes/MontyHallScene.unity";
        const string MainScenePath = "Assets/Scenes/MainScene.unity";
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string KoreanFontPath = "Assets/Fonts/NanumGothic SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string DoorPrefabPath = "Assets/Prefabs/MontyHallDoor.prefab";
        const string SceneName = "MontyHallScene";

        static readonly Color BackgroundColor = new Color32(0x10, 0x18, 0x26, 0xFF);
        static readonly Color CardColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color TitleColor = new Color32(0xF7, 0xF8, 0xFB, 0xFF);
        static readonly Color CardTitleColor = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color ButtonColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color ButtonHighlight = new Color32(0x3E, 0x78, 0xF0, 0xFF);
        static readonly Color ButtonPressed = new Color32(0x1E, 0x4C, 0xC0, 0xFF);
        static readonly Color ButtonDisabled = new Color32(0x9A, 0xA4, 0xB5, 0xFF);

        static int attempts;

        static MontyHallSceneSetup()
        {
            EditorApplication.delayCall += TryEnsure;
        }

        [MenuItem("Mind Paradox/Rebuild Monty Hall Scene")]
        public static void RebuildFromMenu()
        {
            if (!AssetsReady())
            {
                Debug.LogError("[Mind Paradox] 폰트 또는 버튼 스프라이트가 없어 Monty Hall 씬을 만들지 못했습니다.");
                return;
            }

            BuildScene();
            AddToBuildSettings();
            ValidateRules();
            Debug.Log("[Mind Paradox] MontyHallScene을 다시 만들었습니다.");
        }

        static void TryEnsure()
        {
            if (Application.isBatchMode)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryEnsure;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!AssetsReady())
            {
                attempts++;
                if (attempts < 40)
                    EditorApplication.delayCall += TryEnsure;
                return;
            }

            PointOpenMainMenuAtMontyHall();
            bool missingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null;
            bool oldLayout = !missingScene && File.Exists(ScenePath) && !File.ReadAllText(ScenePath).Contains("m_Name: ModePanel");
            if (missingScene || oldLayout)
            {
                BuildScene();
                ValidateRules();
            }

            AddToBuildSettings();
            RepairDoorPrefabReference();
        }

        static void RepairDoorPrefabReference()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
            MontyHallDoorUI door = prefab != null ? prefab.GetComponent<MontyHallDoorUI>() : null;
            if (door == null)
                return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;

            var manager = UnityEngine.Object.FindFirstObjectByType<MontyHallGameManager>();
            if (manager == null)
                return;

            var serialized = new SerializedObject(manager);
            SerializedProperty property = serialized.FindProperty("doorPrefab");
            if (property == null || property.objectReferenceValue == door)
                return;

            property.objectReferenceValue = door;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        static bool AssetsReady()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null
                && AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) != null;
        }

        static void PointOpenMainMenuAtMontyHall()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MainScenePath)
                return;

            bool changed = false;
            MainMenuManager menu = UnityEngine.Object.FindFirstObjectByType<MainMenuManager>();
            if (menu != null)
            {
                var menuObject = new SerializedObject(menu);
                SerializedProperty entries = menuObject.FindProperty("entries");
                if (entries == null)
                    entries = menuObject.FindProperty("m_Entries");
                for (int i = 0; entries != null && i < entries.arraySize; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    if (entry.FindPropertyRelative("title").stringValue != "Monty Hall")
                        continue;

                    SerializedProperty sceneName = entry.FindPropertyRelative("targetSceneName");
                    if (sceneName.stringValue == SceneName)
                        continue;

                    sceneName.stringValue = SceneName;
                    changed = true;
                }

                if (changed)
                    menuObject.ApplyModifiedPropertiesWithoutUndo();
            }

            GameCardUI[] cards = UnityEngine.Object.FindObjectsByType<GameCardUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < cards.Length; i++)
            {
                var cardObject = new SerializedObject(cards[i]);
                if (cardObject.FindProperty("playLogMessage").stringValue != "Open Monty Hall")
                    continue;

                SerializedProperty sceneName = cardObject.FindProperty("targetSceneName");
                if (sceneName.stringValue == SceneName)
                    continue;

                sceneName.stringValue = SceneName;
                cardObject.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (!changed)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void BuildScene()
        {
            string returnPath = EditorSceneManager.GetActiveScene().path;
            if (EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveOpenScenes();

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            AddKoreanGlyphs();
            MontyHallDoorUI doorPrefab = EnsureDoorPrefab(font, sprite);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();

            var canvasObject = new GameObject("Canvas", typeof(RectTransform));
            Stretch(canvasObject.GetComponent<RectTransform>());
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.vertexColorAlwaysGammaSpace = true;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform background = CreateStretch("Background", canvasObject.transform);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = BackgroundColor;
            backgroundImage.raycastTarget = false;

            RectTransform safeArea = CreateStretch("SafeArea", canvasObject.transform);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            var safeLayout = safeArea.gameObject.AddComponent<VerticalLayoutGroup>();
            safeLayout.padding = new RectOffset(48, 48, 0, 36);
            safeLayout.spacing = 20f;
            safeLayout.childAlignment = TextAnchor.UpperCenter;
            safeLayout.childControlWidth = true;
            safeLayout.childControlHeight = true;
            safeLayout.childForceExpandWidth = true;
            safeLayout.childForceExpandHeight = false;

            RectTransform header = CreateUiObject("Header", safeArea);
            var headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.preferredHeight = 510f;
            headerElement.flexibleHeight = 0f;
            var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(8, 8, 48, 8);
            headerLayout.spacing = 14f;
            headerLayout.childAlignment = TextAnchor.UpperCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            RectTransform topRow = CreateUiObject("TopRow", header);
            var topElement = topRow.gameObject.AddComponent<LayoutElement>();
            topElement.preferredHeight = 88f;
            topElement.flexibleHeight = 0f;
            var topLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.spacing = PuzzleHeaderBar.Spacing;
            Button back = CreateActionButton(topRow, "BackButton", UiText.BackLabel, font, sprite);
            var backElement = back.gameObject.GetComponent<LayoutElement>();
            backElement.preferredWidth = PuzzleHeaderBar.ButtonWidth;
            backElement.minWidth = PuzzleHeaderBar.ButtonWidth;
            backElement.preferredHeight = PuzzleHeaderBar.ButtonHeight;
            backElement.minHeight = PuzzleHeaderBar.ButtonHeight;
            backElement.flexibleWidth = 0f;

            RectTransform topSpacer = CreateUiObject("Spacer", topRow);
            var topSpacerElement = topSpacer.gameObject.AddComponent<LayoutElement>();
            topSpacerElement.flexibleWidth = 1f;
            topSpacerElement.preferredHeight = 88f;

            Button mode = CreateActionButton(topRow, "ModeButton", "모드", font, sprite);
            var modeElement = mode.gameObject.GetComponent<LayoutElement>();
            modeElement.preferredWidth = 220f;
            modeElement.flexibleWidth = 0f;
            mode.interactable = false;

            TMP_Text title = CreateText(header, "TitleText", "Monty Hall", 70f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 100f;
            titleElement.flexibleHeight = 0f;

            TMP_Text description = CreateText(header, "DescriptionText", "문이 많아질수록\n바꾸기의 확률은 어떻게 달라질까요?", 60f, FontStyles.Normal, new Color32(0xA8, 0xB3, 0xC7, 0xFF), TextAlignmentOptions.Center, font, true);
            var descriptionElement = description.gameObject.AddComponent<LayoutElement>();
            descriptionElement.preferredHeight = 220f;
            descriptionElement.flexibleHeight = 0f;

            RectTransform modePanel = CreateUiObject("ModePanel", safeArea);
            var modePanelElement = modePanel.gameObject.AddComponent<LayoutElement>();
            modePanelElement.flexibleHeight = 1f;
            modePanelElement.minHeight = 640f;
            var modeLayout = modePanel.gameObject.AddComponent<VerticalLayoutGroup>();
            modeLayout.spacing = 22f;
            modeLayout.padding = new RectOffset(8, 8, 8, 8);
            modeLayout.childAlignment = TextAnchor.UpperCenter;
            modeLayout.childControlWidth = true;
            modeLayout.childControlHeight = true;
            modeLayout.childForceExpandWidth = true;
            modeLayout.childForceExpandHeight = false;

            Button threeDoors = CreateActionButton(modePanel, "ThreeDoorButton", "3 Doors", font, sprite);
            StyleModeChoice(threeDoors);
            CreateHint(modePanel, "기본 몬티홀 문제", font);
            Button tenDoors = CreateActionButton(modePanel, "TenDoorButton", "10 Doors", font, sprite);
            StyleModeChoice(tenDoors);
            CreateHint(modePanel, "문 10개로 확률 차이를 더 크게 체험", font);
            Button custom = CreateActionButton(modePanel, "CustomButton", "Custom", font, sprite);
            StyleModeChoice(custom);
            CreateHint(modePanel, "사용자가 문 개수를 직접 선택", font);

            RectTransform customPanel = CreateUiObject("CustomPanel", modePanel);
            customPanel.gameObject.SetActive(false);
            var customElement = customPanel.gameObject.AddComponent<LayoutElement>();
            customElement.preferredHeight = 360f;
            customElement.flexibleHeight = 0f;
            var customLayout = customPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            customLayout.spacing = 12f;
            customLayout.childAlignment = TextAnchor.UpperCenter;
            customLayout.childControlWidth = true;
            customLayout.childControlHeight = true;
            customLayout.childForceExpandWidth = true;
            customLayout.childForceExpandHeight = false;

            RectTransform countRow = CreateUiObject("CountRow", customPanel);
            var countRowElement = countRow.gameObject.AddComponent<LayoutElement>();
            countRowElement.preferredHeight = 88f;
            var countLayout = countRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            countLayout.spacing = 12f;
            countLayout.childAlignment = TextAnchor.MiddleCenter;
            countLayout.childControlWidth = true;
            countLayout.childControlHeight = true;
            countLayout.childForceExpandWidth = false;
            countLayout.childForceExpandHeight = false;
            Button minus = CreateActionButton(countRow, "MinusButton", "-", font, sprite);
            var minusElement = minus.gameObject.GetComponent<LayoutElement>();
            minusElement.preferredWidth = 120f;
            minusElement.flexibleWidth = 0f;
            TMP_Text customCount = CreateText(countRow, "CustomCountText", "Doors: 10", 36f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var customCountElement = customCount.gameObject.AddComponent<LayoutElement>();
            customCountElement.flexibleWidth = 1f;
            customCountElement.preferredHeight = 80f;
            Button plus = CreateActionButton(countRow, "PlusButton", "+", font, sprite);
            var plusElement = plus.gameObject.GetComponent<LayoutElement>();
            plusElement.preferredWidth = 120f;
            plusElement.flexibleWidth = 0f;

            Slider doorSlider = CreateDoorSlider(customPanel);
            TMP_Text customOdds = CreateText(customPanel, "CustomOddsText", "Stay:\n10%\n\nSwitch:\n90%", 32f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, true);
            var customOddsElement = customOdds.gameObject.AddComponent<LayoutElement>();
            customOddsElement.preferredHeight = 160f;
            Button customStart = CreateActionButton(customPanel, "CustomStartButton", "시작", font, sprite);

            RectTransform gameArea = CreateUiObject("GameArea", safeArea);
            gameArea.gameObject.SetActive(false);
            var gameAreaElement = gameArea.gameObject.AddComponent<LayoutElement>();
            gameAreaElement.flexibleHeight = 1f;
            gameAreaElement.minHeight = 320f;
            var scroll = gameArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            RectTransform viewport = CreateStretch("Viewport", gameArea);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            scroll.viewport = viewport;

            RectTransform content = CreateUiObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.cellSize = new Vector2(280f, 300f);
            grid.spacing = new Vector2(16f, 16f);
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            RectTransform messageArea = CreateUiObject("MessageArea", safeArea);
            messageArea.gameObject.SetActive(false);
            var messageElement = messageArea.gameObject.AddComponent<LayoutElement>();
            messageElement.preferredHeight = 640f;
            messageElement.flexibleHeight = 0f;
            var messageLayout = messageArea.gameObject.AddComponent<VerticalLayoutGroup>();
            messageLayout.spacing = 8f;
            messageLayout.childAlignment = TextAnchor.UpperCenter;
            messageLayout.childControlWidth = true;
            messageLayout.childControlHeight = true;
            messageLayout.childForceExpandWidth = true;
            messageLayout.childForceExpandHeight = false;

            TMP_Text odds = CreateText(messageArea, "OddsText", "", 32f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
            var oddsElement = odds.gameObject.AddComponent<LayoutElement>();
            oddsElement.preferredHeight = 180f;
            oddsElement.flexibleHeight = 0f;

            TMP_Text message = CreateText(messageArea, "MessageText", "", 32f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, true);
            var messageTextElement = message.gameObject.AddComponent<LayoutElement>();
            messageTextElement.preferredHeight = 150f;
            messageTextElement.flexibleHeight = 0f;

            TMP_Text result = CreateText(messageArea, "ResultText", "", 30f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, true);
            var resultElement = result.gameObject.AddComponent<LayoutElement>();
            resultElement.preferredHeight = 280f;
            resultElement.flexibleHeight = 0f;
            result.gameObject.SetActive(false);

            TMP_Text stats = CreateText(messageArea, "StatsText", "", 28f, FontStyles.Normal, new Color32(0xA8, 0xB3, 0xC7, 0xFF), TextAlignmentOptions.Center, font, true);
            var statsElement = stats.gameObject.AddComponent<LayoutElement>();
            statsElement.preferredHeight = 140f;
            statsElement.flexibleHeight = 0f;

            RectTransform actionArea = CreateUiObject("ActionArea", safeArea);
            actionArea.gameObject.SetActive(false);
            var actionElement = actionArea.gameObject.AddComponent<LayoutElement>();
            actionElement.preferredHeight = 248f;
            actionElement.flexibleHeight = 0f;
            var actionLayout = actionArea.gameObject.AddComponent<VerticalLayoutGroup>();
            actionLayout.spacing = 16f;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;

            RectTransform choiceRow = CreateUiObject("ChoiceRow", actionArea);
            var choiceElement = choiceRow.gameObject.AddComponent<LayoutElement>();
            choiceElement.preferredHeight = 108f;
            choiceElement.flexibleHeight = 0f;
            var choiceLayout = choiceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            choiceLayout.spacing = 16f;
            choiceLayout.childAlignment = TextAnchor.MiddleCenter;
            choiceLayout.childControlWidth = true;
            choiceLayout.childControlHeight = true;
            choiceLayout.childForceExpandWidth = true;
            choiceLayout.childForceExpandHeight = false;
            Button stay = CreateActionButton(choiceRow, "StayButton", "유지", font, sprite);
            Button changeDoor = CreateActionButton(choiceRow, "SwitchButton", "변경", font, sprite);
            stay.interactable = false;
            changeDoor.interactable = false;

            Button replay = CreateActionButton(actionArea, "ReplayButton", "다시 하기", font, sprite);
            replay.interactable = false;

            CreateEventSystem();

            var host = new GameObject("MontyHall");
            var game = host.AddComponent<MontyHallGameManager>();
            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("doorPrefab").objectReferenceValue = doorPrefab;
            serializedGame.FindProperty("doorContent").objectReferenceValue = content;
            serializedGame.FindProperty("doorGrid").objectReferenceValue = grid;
            serializedGame.FindProperty("modePanel").objectReferenceValue = modePanel.gameObject;
            serializedGame.FindProperty("customPanel").objectReferenceValue = customPanel.gameObject;
            serializedGame.FindProperty("gameArea").objectReferenceValue = gameArea.gameObject;
            serializedGame.FindProperty("messageArea").objectReferenceValue = messageArea.gameObject;
            serializedGame.FindProperty("actionArea").objectReferenceValue = actionArea.gameObject;
            serializedGame.FindProperty("doorSlider").objectReferenceValue = doorSlider;
            serializedGame.FindProperty("descriptionText").objectReferenceValue = description;
            serializedGame.FindProperty("customCountText").objectReferenceValue = customCount;
            serializedGame.FindProperty("customOddsText").objectReferenceValue = customOdds;
            serializedGame.FindProperty("oddsText").objectReferenceValue = odds;
            serializedGame.FindProperty("messageText").objectReferenceValue = message;
            serializedGame.FindProperty("resultText").objectReferenceValue = result;
            serializedGame.FindProperty("statsText").objectReferenceValue = stats;
            serializedGame.FindProperty("stayButton").objectReferenceValue = stay;
            serializedGame.FindProperty("switchButton").objectReferenceValue = changeDoor;
            serializedGame.FindProperty("replayButton").objectReferenceValue = replay;
            serializedGame.FindProperty("backButton").objectReferenceValue = back;
            serializedGame.FindProperty("modeButton").objectReferenceValue = mode;
            serializedGame.FindProperty("threeDoorButton").objectReferenceValue = threeDoors;
            serializedGame.FindProperty("tenDoorButton").objectReferenceValue = tenDoors;
            serializedGame.FindProperty("customButton").objectReferenceValue = custom;
            serializedGame.FindProperty("customStartButton").objectReferenceValue = customStart;
            serializedGame.FindProperty("plusButton").objectReferenceValue = plus;
            serializedGame.FindProperty("minusButton").objectReferenceValue = minus;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            Stretch(canvasObject.GetComponent<RectTransform>());
            PuzzleExplanationSetup.AttachInActiveScene("monty-hall");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            bool rewritten = FixSavedCanvasRect(ScenePath);

            if (!string.IsNullOrEmpty(returnPath) && returnPath != ScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            else if (rewritten)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void AssignArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void AddToBuildSettings()
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == ScenePath)
                    return;
            }

            var list = new List<EditorBuildSettingsScene>(existing);
            int mainIndex = list.FindIndex(scene => scene.path == MainScenePath);
            var entry = new EditorBuildSettingsScene(ScenePath, true);
            if (mainIndex >= 0)
                list.Insert(mainIndex + 1, entry);
            else
                list.Add(entry);

            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
        }

        static bool FixSavedCanvasRect(string path)
        {
            if (!File.Exists(path))
                return false;

            string text = File.ReadAllText(path);
            const string brokenScale = "  m_LocalScale: {x: 0, y: 0, z: 0}";
            if (!text.Contains(brokenScale))
                return false;

            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            string brokenAnchor =
                "  m_AnchorMax: {x: 0, y: 0}" + newline +
                "  m_AnchoredPosition: {x: 0, y: 0}" + newline +
                "  m_SizeDelta: {x: 0, y: 0}" + newline +
                "  m_Pivot: {x: 0, y: 0}";
            string fixedAnchor =
                "  m_AnchorMax: {x: 1, y: 1}" + newline +
                "  m_AnchoredPosition: {x: 0, y: 0}" + newline +
                "  m_SizeDelta: {x: 0, y: 0}" + newline +
                "  m_Pivot: {x: 0.5, y: 0.5}";

            text = text.Replace(brokenScale, "  m_LocalScale: {x: 1, y: 1, z: 1}");
            text = text.Replace(brokenAnchor, fixedAnchor);
            File.WriteAllText(path, text);
            AssetDatabase.ImportAsset(path);
            return true;
        }

        static void AddKoreanGlyphs()
        {
            TMP_FontAsset korean = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (korean == null)
                return;

            const string glyphs = "문하나를고르세요자동차는세중에도있습니다번에서염소가나왔습니다처음고른유지할까요바꿀까요결과다시하기메인메뉴선택바꿨습니다다른";
            korean.TryAddCharacters(glyphs, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[Mind Paradox] 몬티홀 안내 중 폰트에 없는 문자: " + missing);

            EditorUtility.SetDirty(korean);
        }

        static void ValidateRules()
        {
            var random = new System.Random(7);
            int switchWins = 0;
            int stayWins = 0;
            const int trials = 600;

            for (int i = 0; i < trials; i++)
            {
                var stayRound = new MontyHallRound(3, random);
                stayRound.Begin();
                stayRound.ChooseDoor(i % 3);
                if (!HostLeftTwoDoors(stayRound))
                    return;

                stayRound.Stay();
                if (stayRound.Won)
                    stayWins++;

                var switchRound = new MontyHallRound(3, random);
                switchRound.Begin();
                switchRound.ChooseDoor(i % 3);
                bool initialWrong = switchRound.PlayerDoor != switchRound.CarDoor;
                switchRound.Switch();
                if (switchRound.FinalDoor != switchRound.AlternateDoor || switchRound.FinalDoor == switchRound.PlayerDoor)
                {
                    Debug.LogError("[Mind Paradox] 변경이 남은 다른 문을 고르지 않았습니다.");
                    return;
                }

                if (switchRound.Won != initialWrong)
                {
                    Debug.LogError("[Mind Paradox] 처음 선택이 틀렸을 때 변경이 자동차가 아닙니다.");
                    return;
                }

                if (switchRound.Won)
                    switchWins++;
            }

            if (stayWins > trials * 0.45f || switchWins < trials * 0.55f)
                Debug.LogError("[Mind Paradox] 3문 몬티홀 확률이 예상과 다릅니다. 유지 " + stayWins + ", 변경 " + switchWins);

            CheckHost(10, 40);
            CheckHost(100, 20);
        }

        static void CheckHost(int doorCount, int trials)
        {
            var random = new System.Random(doorCount);
            for (int i = 0; i < trials; i++)
            {
                var round = new MontyHallRound(doorCount, random);
                round.Begin();
                round.ChooseDoor(i % doorCount);
                if (!HostLeftTwoDoors(round))
                    return;

                bool initialWrong = round.PlayerDoor != round.CarDoor;
                if (initialWrong && round.AlternateDoor != round.CarDoor)
                {
                    Debug.LogError("[Mind Paradox] 염소를 골랐는데 자동차 문을 남기지 않았습니다.");
                    return;
                }

                if (!initialWrong && round.AlternateDoor == round.CarDoor)
                {
                    Debug.LogError("[Mind Paradox] 자동차를 골랐는데 자동차가 아닌 문을 남기지 않았습니다.");
                    return;
                }
            }
        }

        static bool HostLeftTwoDoors(MontyHallRound round)
        {
            if (round.OpenedCount != round.DoorCount - 2 || round.IsOpened(round.PlayerDoor) || round.IsOpened(round.CarDoor) || round.AlternateDoor == round.PlayerDoor)
            {
                Debug.LogError("[Mind Paradox] 진행자가 문 " + round.DoorCount + "개에서 닫힌 문 두 개를 남기지 못했습니다.");
                return false;
            }

            return true;
        }

        static MontyHallDoorUI EnsureDoorPrefab(TMP_FontAsset font, Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
            if (existing != null)
                return existing.GetComponent<MontyHallDoorUI>();

            MontyHallDoorUI door = CreateDoor(null, "MontyHallDoor", "1", font, sprite);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(door.gameObject, DoorPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(door.gameObject);
            }

            AssetDatabase.ImportAsset(DoorPrefabPath);
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath);
            return saved != null ? saved.GetComponent<MontyHallDoorUI>() : null;
        }

        static void CreateHint(Transform parent, string text, TMP_FontAsset font)
        {
            TMP_Text hint = CreateText(parent, "Hint", text, 46f, FontStyles.Normal, new Color32(0xA8, 0xB3, 0xC7, 0xFF), TextAlignmentOptions.Center, font, true);
            var element = hint.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 160f;
            element.flexibleHeight = 0f;
        }

        static Slider CreateDoorSlider(Transform parent)
        {
            var root = new GameObject("DoorSlider", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var element = root.AddComponent<LayoutElement>();
            element.preferredHeight = 56f;
            element.flexibleWidth = 1f;

            var background = CreateStretch("Background", root.transform);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color32(0x2A, 0x35, 0x48, 0xFF);

            var fillArea = CreateStretch("Fill Area", root.transform);
            fillArea.offsetMin = new Vector2(10f, 18f);
            fillArea.offsetMax = new Vector2(-10f, -18f);
            var fill = CreateStretch("Fill", fillArea);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = ButtonColor;

            var handleArea = CreateStretch("Handle Slide Area", root.transform);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);
            var handle = new GameObject("Handle", typeof(RectTransform));
            handle.transform.SetParent(handleArea, false);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(36f, 36f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            handleImage.type = handleImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            handleImage.color = Color.white;

            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = MontyHallRound.MinDoors;
            slider.maxValue = MontyHallRound.MaxDoors;
            slider.wholeNumbers = true;
            slider.value = 10f;
            return slider;
        }

        static MontyHallDoorUI CreateDoor(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite)
        {
            Button button = CreateDoorButton(parent, name, label, font, sprite, out TMP_Text caption, out Image image);
            var door = button.gameObject.AddComponent<MontyHallDoorUI>();
            var serialized = new SerializedObject(door);
            serialized.FindProperty("face").objectReferenceValue = image;
            serialized.FindProperty("label").objectReferenceValue = caption;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        static Button CreateDoorButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite, out TMP_Text caption, out Image image)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            image = buttonObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = CardColor;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.95f, 0.97f, 1f);
            colors.pressedColor = new Color(0.88f, 0.90f, 0.94f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var element = buttonObject.AddComponent<LayoutElement>();
            element.preferredHeight = 260f;
            element.flexibleWidth = 1f;
            element.flexibleHeight = 0f;

            caption = CreateText(buttonObject.transform, "Label", label, 48f, FontStyles.Bold, CardTitleColor, TextAlignmentOptions.Center, font, true);
            Stretch(caption.rectTransform);
            return button;
        }

        static Button CreateActionButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = ButtonHighlight;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonColor;
            colors.disabledColor = ButtonDisabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var element = buttonObject.AddComponent<LayoutElement>();
            element.preferredHeight = 108f;
            element.minHeight = 100f;
            element.flexibleWidth = 1f;
            element.flexibleHeight = 0f;

            TMP_Text caption = CreateText(buttonObject.transform, "Label", label, 42f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, false);
            Stretch(caption.rectTransform);
            return button;
        }

        static void StyleModeChoice(Button button)
        {
            var element = button.GetComponent<LayoutElement>();
            element.preferredHeight = 124f;
            element.minHeight = 112f;
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            label.fontSize = 48f;
            label.enableAutoSizing = false;
        }

        static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.orthographic = true;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        static void CreateEventSystem()
        {
            var eventObject = new GameObject("EventSystem");
            eventObject.AddComponent<EventSystem>();
            var module = eventObject.AddComponent<InputSystemUIInputModule>();
            if (module.actionsAsset == null)
                module.AssignDefaultActions();
        }

        static TMP_Text CreateText(
            Transform parent,
            string objectName,
            string text,
            float fontSize,
            FontStyles style,
            Color color,
            TextAlignmentOptions alignment,
            TMP_FontAsset font,
            bool wrap)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var tmp = textObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableAutoSizing = false;
            tmp.enableWordWrapping = wrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            tmp.richText = false;
            return tmp;
        }

        static RectTransform CreateStretch(string objectName, Transform parent)
        {
            RectTransform rect = CreateUiObject(objectName, parent);
            Stretch(rect);
            return rect;
        }

        static RectTransform CreateUiObject(string objectName, Transform parent)
        {
            var uiObject = new GameObject(objectName, typeof(RectTransform));
            uiObject.transform.SetParent(parent, false);
            return uiObject.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }
    }
}
