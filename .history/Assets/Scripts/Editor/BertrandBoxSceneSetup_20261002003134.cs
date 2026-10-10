using System.Collections.Generic;
using System.IO;
using MindParadox.Games.BertrandBox;
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
    /// BertrandBoxScene이 없으면 기본 카드와 글자로 상자 문제 화면을 만든다.
    /// </summary>
    [InitializeOnLoad]
    public static class BertrandBoxSceneSetup
    {
        const string ScenePath = "Assets/Scenes/BertrandBoxScene.unity";
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string KoreanFontPath = "Assets/Fonts/NanumGothic SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string SceneMarker = "m_Name: BoxRow";

        static readonly Color BackgroundColor = new Color32(0x10, 0x18, 0x26, 0xFF);
        static readonly Color CardColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color TitleColor = new Color32(0xF7, 0xF8, 0xFB, 0xFF);
        static readonly Color SubtitleColor = new Color32(0xA8, 0xB3, 0xC7, 0xFF);
        static readonly Color CardTitleColor = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color ButtonColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color ButtonHighlight = new Color32(0x3E, 0x78, 0xF0, 0xFF);
        static readonly Color ButtonPressed = new Color32(0x1E, 0x4C, 0xC0, 0xFF);
        static readonly Color ButtonDisabled = new Color32(0x9A, 0xA4, 0xB5, 0xFF);

        static int attempts;

        static BertrandBoxSceneSetup()
        {
            EditorApplication.delayCall += TryEnsure;
        }

        [MenuItem("Mind Paradox/Rebuild Bertrand Box Scene")]
        public static void RebuildFromMenu()
        {
            if (!AssetsReady())
            {
                Debug.LogError("[Mind Paradox] 폰트 또는 버튼 스프라이트가 없어 Bertrand Box 씬을 만들지 못했습니다.");
                return;
            }

            BuildScene();
            AddToBuildSettings();
            ValidateRules();
            Debug.Log("[Mind Paradox] BertrandBoxScene을 다시 만들었습니다.");
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

            bool missingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null;
            bool oldLayout = !missingScene && File.Exists(ScenePath) && !File.ReadAllText(ScenePath).Contains(SceneMarker);
            if (missingScene || oldLayout)
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
            safeLayout.padding = new RectOffset(48, 48, 24, 28);
            safeLayout.spacing = 12f;
            safeLayout.childAlignment = TextAnchor.UpperCenter;
            safeLayout.childControlWidth = true;
            safeLayout.childControlHeight = true;
            safeLayout.childForceExpandWidth = true;
            safeLayout.childForceExpandHeight = false;

            RectTransform header = CreateColumn("Header", safeArea, 156f);
            RectTransform topRow = CreateUiObject("TopRow", header);
            var topElement = topRow.gameObject.AddComponent<LayoutElement>();
            topElement.preferredHeight = 80f;
            var topLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            Button back = CreateActionButton(topRow, "BackButton", BertrandBoxCopy.Back, font, sprite, 80f);
            var backElement = back.gameObject.GetComponent<LayoutElement>();
            backElement.preferredWidth = 220f;
            backElement.flexibleWidth = 0f;
            TMP_Text title = CreateText(header, "Title", BertrandBoxCopy.Title, 48f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 64f;

            RectTransform promptArea = CreateColumn("PromptArea", safeArea, 168f);
            TMP_Text prompt = CreateText(promptArea, "PromptText", BertrandBoxCopy.QuizPrompt, 30f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, true);
            prompt.overflowMode = TextOverflowModes.Overflow;
            var promptElement = prompt.gameObject.AddComponent<LayoutElement>();
            promptElement.preferredHeight = 160f;

            RectTransform boxRow = CreateUiObject("BoxRow", safeArea);
            var boxRowElement = boxRow.gameObject.AddComponent<LayoutElement>();
            boxRowElement.preferredHeight = 300f;
            boxRowElement.flexibleHeight = 0f;
            var boxLayout = boxRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            boxLayout.spacing = 16f;
            boxLayout.childAlignment = TextAnchor.MiddleCenter;
            boxLayout.childControlWidth = true;
            boxLayout.childControlHeight = true;
            boxLayout.childForceExpandWidth = true;
            boxLayout.childForceExpandHeight = true;
            BertrandBoxUI boxA = CreateBox(boxRow, "BoxA", BertrandBoxCopy.BoxA, font, sprite);
            BertrandBoxUI boxB = CreateBox(boxRow, "BoxB", BertrandBoxCopy.BoxB, font, sprite);
            BertrandBoxUI boxC = CreateBox(boxRow, "BoxC", BertrandBoxCopy.BoxC, font, sprite);

            RectTransform choiceArea = CreateColumn("ChoiceArea", safeArea, 276f);
            Button half = CreateActionButton(choiceArea, "ChoiceHalfButton", BertrandBoxCopy.ChoiceHalf, font, sprite, 84f);
            Button twoThirds = CreateActionButton(choiceArea, "ChoiceTwoThirdsButton", BertrandBoxCopy.ChoiceTwoThirds, font, sprite, 84f);
            Button third = CreateActionButton(choiceArea, "ChoiceThirdButton", BertrandBoxCopy.ChoiceThird, font, sprite, 84f);

            RectTransform actionArea = CreateUiObject("ActionArea", safeArea);
            var actionElement = actionArea.gameObject.AddComponent<LayoutElement>();
            actionElement.preferredHeight = 88f;
            var actionLayout = actionArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 12f;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;
            Button redraw = CreateActionButton(actionArea, "RedrawButton", BertrandBoxCopy.Redraw, font, sprite, 84f);
            Button experiment = CreateActionButton(actionArea, "ExperimentButton", BertrandBoxCopy.Experiment, font, sprite, 84f);
            Button simulate = CreateActionButton(actionArea, "SimulateButton", BertrandBoxCopy.Simulate, font, sprite, 84f);
            redraw.gameObject.SetActive(false);
            experiment.gameObject.SetActive(false);
            simulate.gameObject.SetActive(false);

            TMP_Text result = CreateText(safeArea, "ResultText", "", 28f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var resultElement = result.gameObject.AddComponent<LayoutElement>();
            resultElement.preferredHeight = 40f;

            RectTransform explainArea = CreateColumn("ExplainArea", safeArea, 280f);
            TMP_Text explain = CreateText(explainArea, "ExplainText", "", 28f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            explain.overflowMode = TextOverflowModes.Overflow;
            var explainElement = explain.gameObject.AddComponent<LayoutElement>();
            explainElement.preferredHeight = 272f;
            explainArea.gameObject.SetActive(false);

            RectTransform statsArea = CreateColumn("StatsArea", safeArea, 180f);
            TMP_Text stats = CreateText(statsArea, "StatsText", "Quiz: 0 / 0\nSimulations: 0\nGold Observed: 0\nOther Coin Gold: 0\nObserved: 0.0%\nTheory: 66.7%", 26f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            var statsElement = stats.gameObject.AddComponent<LayoutElement>();
            statsElement.preferredHeight = 172f;

            CreateEventSystem();

            var host = new GameObject("BertrandBox");
            var game = host.AddComponent<BertrandBoxGameManager>();
            var serializedGame = new SerializedObject(game);
            AssignBoxes(serializedGame.FindProperty("boxes"), boxA, boxB, boxC);
            serializedGame.FindProperty("titleText").objectReferenceValue = title;
            serializedGame.FindProperty("promptText").objectReferenceValue = prompt;
            serializedGame.FindProperty("explainText").objectReferenceValue = explain;
            serializedGame.FindProperty("resultText").objectReferenceValue = result;
            serializedGame.FindProperty("statsText").objectReferenceValue = stats;
            serializedGame.FindProperty("choiceArea").objectReferenceValue = choiceArea.gameObject;
            serializedGame.FindProperty("explainArea").objectReferenceValue = explainArea.gameObject;
            serializedGame.FindProperty("backButton").objectReferenceValue = back;
            serializedGame.FindProperty("choiceHalfButton").objectReferenceValue = half;
            serializedGame.FindProperty("choiceTwoThirdsButton").objectReferenceValue = twoThirds;
            serializedGame.FindProperty("choiceThirdButton").objectReferenceValue = third;
            serializedGame.FindProperty("redrawButton").objectReferenceValue = redraw;
            serializedGame.FindProperty("experimentButton").objectReferenceValue = experiment;
            serializedGame.FindProperty("simulateButton").objectReferenceValue = simulate;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            Stretch(canvasObject.GetComponent<RectTransform>());
            PuzzleExplanationSetup.AttachInActiveScene("bertrand-box");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            bool rewritten = FixSavedCanvasRect(ScenePath);

            if (!string.IsNullOrEmpty(returnPath) && returnPath != ScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            else if (rewritten)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void AssignBoxes(SerializedProperty property, BertrandBoxUI first, BertrandBoxUI second, BertrandBoxUI third)
        {
            property.arraySize = 3;
            property.GetArrayElementAtIndex(0).objectReferenceValue = first;
            property.GetArrayElementAtIndex(1).objectReferenceValue = second;
            property.GetArrayElementAtIndex(2).objectReferenceValue = third;
        }

        static BertrandBoxUI CreateBox(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var face = root.AddComponent<Image>();
            face.sprite = sprite;
            face.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            face.color = CardColor;

            var button = root.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.95f, 0.97f, 1f);
            colors.pressedColor = new Color(0.88f, 0.90f, 0.94f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 16, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text nameText = CreateText(root.transform, "Name", label, 32f, FontStyles.Bold, CardTitleColor, TextAlignmentOptions.Center, font, false);
            var nameElement = nameText.gameObject.AddComponent<LayoutElement>();
            nameElement.preferredHeight = 44f;

            var coinObject = new GameObject("CoinImage", typeof(RectTransform));
            coinObject.transform.SetParent(root.transform, false);
            var coinImage = coinObject.AddComponent<Image>();
            coinImage.sprite = sprite;
            coinImage.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            coinImage.color = Color.white;
            coinImage.raycastTarget = false;
            var coinElement = coinObject.AddComponent<LayoutElement>();
            coinElement.preferredWidth = 72f;
            coinElement.preferredHeight = 72f;
            coinElement.flexibleWidth = 0f;
            coinObject.SetActive(false);

            TMP_Text coinText = CreateText(root.transform, "CoinText", "", 28f, FontStyles.Bold, CardTitleColor, TextAlignmentOptions.Center, font, true);
            var coinTextElement = coinText.gameObject.AddComponent<LayoutElement>();
            coinTextElement.preferredHeight = 120f;
            coinText.gameObject.SetActive(false);

            var view = root.AddComponent<BertrandBoxUI>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("face").objectReferenceValue = face;
            serialized.FindProperty("coinImage").objectReferenceValue = coinImage;
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("coinText").objectReferenceValue = coinText;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        static RectTransform CreateColumn(string name, Transform parent, float preferredHeight)
        {
            RectTransform column = CreateUiObject(name, parent);
            var element = column.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
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
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
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

            const string glyphs = "상자개가있습니다금화은화나왔다고가정합니다남은동전도일확률은하나를고르세요왔습니다관찰한경우가아니므로다시뽑을수있습니다정답오답나올수있는경우는세가지입니다상자의첫번째두번째가지실험기록했습니다뒤로직접회";
            korean.TryAddCharacters(glyphs, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[Mind Paradox] 베르트랑 상자 안내 중 폰트에 없는 문자: " + missing);

            EditorUtility.SetDirty(korean);
        }

        static void ValidateRules()
        {
            string theory = BertrandBoxSimulator.FormatPercent(BertrandBoxSimulator.Theory);
            if (theory != "66.7%")
                Debug.LogError("[Mind Paradox] 베르트랑 상자 이론값이 66.7%가 아닙니다. 실제 " + theory);

            var random = new System.Random(11);
            BertrandBoxSimulator.RunDraws(4000, random, out int gold, out int other);
            if (gold < 1700 || gold > 2300)
                Debug.LogError("[Mind Paradox] 4000번 중 금화 관찰 수가 절반에서 멀리 떨어졌습니다. " + gold);

            double rate = other / (double)gold;
            if (rate < 0.60d || rate > 0.74d)
                Debug.LogError("[Mind Paradox] 금화 관찰 후 남은 금화 비율이 2/3에서 멀리 떨어졌습니다. " + other + " / " + gold);
        }

        static Button CreateActionButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite, float height)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
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
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleWidth = 1f;
            element.flexibleHeight = 0f;

            TMP_Text caption = CreateText(buttonObject.transform, "Label", label, 32f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, false);
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
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
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
