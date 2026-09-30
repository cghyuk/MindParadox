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
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                BuildScene();
                ValidateRules();
            }

            AddToBuildSettings();
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
                for (int i = 0; i < entries.arraySize; i++)
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
            headerElement.preferredHeight = 188f;
            headerElement.flexibleHeight = 0f;
            var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(16, 16, 72, 8);
            headerLayout.spacing = 0f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            TMP_Text title = CreateText(header, "TitleText", "Monty Hall", 72f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 100f;
            titleElement.flexibleHeight = 0f;

            TMP_Text status = CreateText(safeArea, "StatusText", "문 하나를 고르세요.", 40f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, true);
            var statusElement = status.gameObject.AddComponent<LayoutElement>();
            statusElement.preferredHeight = 180f;
            statusElement.flexibleHeight = 0f;

            RectTransform doorRow = CreateUiObject("DoorRow", safeArea);
            var doorRowElement = doorRow.gameObject.AddComponent<LayoutElement>();
            doorRowElement.preferredHeight = 280f;
            doorRowElement.flexibleHeight = 0f;
            var doorLayout = doorRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            doorLayout.spacing = 16f;
            doorLayout.childAlignment = TextAnchor.MiddleCenter;
            doorLayout.childControlWidth = true;
            doorLayout.childControlHeight = true;
            doorLayout.childForceExpandWidth = true;
            doorLayout.childForceExpandHeight = false;

            var doors = new Button[MontyHallRound.DoorCount];
            var doorLabels = new TMP_Text[MontyHallRound.DoorCount];
            var doorImages = new Image[MontyHallRound.DoorCount];
            for (int i = 0; i < MontyHallRound.DoorCount; i++)
            {
                doors[i] = CreateDoorButton(doorRow, "Door" + (i + 1), (i + 1).ToString(), font, sprite, out doorLabels[i], out doorImages[i]);
            }

            RectTransform choiceRow = CreateUiObject("ChoiceRow", safeArea);
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
            choiceRow.gameObject.SetActive(false);

            TMP_Text result = CreateText(safeArea, "ResultText", "결과: 자동차", 48f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var resultElement = result.gameObject.AddComponent<LayoutElement>();
            resultElement.preferredHeight = 88f;
            resultElement.flexibleHeight = 0f;
            result.gameObject.SetActive(false);

            Button replay = CreateActionButton(safeArea, "ReplayButton", "다시 하기", font, sprite);
            replay.gameObject.SetActive(false);

            RectTransform spacer = CreateUiObject("Spacer", safeArea);
            var spacerElement = spacer.gameObject.AddComponent<LayoutElement>();
            spacerElement.flexibleHeight = 1f;
            spacerElement.preferredHeight = 24f;

            Button menu = CreateActionButton(safeArea, "MenuButton", "메인 메뉴", font, sprite);

            CreateEventSystem();

            var host = new GameObject("MontyHall");
            var game = host.AddComponent<MontyHallGameUI>();
            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("statusText").objectReferenceValue = status;
            serializedGame.FindProperty("resultText").objectReferenceValue = result;
            serializedGame.FindProperty("choiceRow").objectReferenceValue = choiceRow.gameObject;
            serializedGame.FindProperty("stayButton").objectReferenceValue = stay;
            serializedGame.FindProperty("switchButton").objectReferenceValue = changeDoor;
            serializedGame.FindProperty("replayButton").objectReferenceValue = replay;
            serializedGame.FindProperty("menuButton").objectReferenceValue = menu;
            serializedGame.FindProperty("mainSceneName").stringValue = "MainScene";
            AssignArray(serializedGame.FindProperty("doorButtons"), doors);
            AssignArray(serializedGame.FindProperty("doorLabels"), doorLabels);
            AssignArray(serializedGame.FindProperty("doorImages"), doorImages);
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            Stretch(canvasObject.GetComponent<RectTransform>());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            FixSavedCanvasRect(ScenePath);

            if (!string.IsNullOrEmpty(returnPath) && returnPath != ScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
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

        static void FixSavedCanvasRect(string path)
        {
            if (!File.Exists(path))
                return;

            string text = File.ReadAllText(path);
            const string brokenScale = "  m_LocalScale: {x: 0, y: 0, z: 0}";
            if (!text.Contains(brokenScale))
                return;

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
                var stayRound = new MontyHallRound(random);
                stayRound.Begin();
                stayRound.ChooseDoor(i % MontyHallRound.DoorCount);
                if (stayRound.RevealedDoor == stayRound.PlayerDoor || stayRound.RevealedDoor == stayRound.CarDoor || stayRound.RevealedDoor < 0)
                {
                    Debug.LogError("[Mind Paradox] 진행자가 고른 문이나 자동차 문을 열었습니다.");
                    return;
                }

                stayRound.Stay();
                if (stayRound.Won)
                    stayWins++;

                var switchRound = new MontyHallRound(random);
                switchRound.Begin();
                switchRound.ChooseDoor(i % MontyHallRound.DoorCount);
                switchRound.Switch();
                if (switchRound.FinalDoor == switchRound.RevealedDoor || switchRound.FinalDoor == switchRound.PlayerDoor)
                {
                    Debug.LogError("[Mind Paradox] 변경이 공개된 문이나 처음 고른 문을 골랐습니다.");
                    return;
                }

                if (switchRound.Won)
                    switchWins++;
            }

            if (stayWins > trials * 0.45f || switchWins < trials * 0.55f)
                Debug.LogError("[Mind Paradox] 3문 몬티홀 확률이 예상과 다릅니다. 유지 " + stayWins + ", 변경 " + switchWins);
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
