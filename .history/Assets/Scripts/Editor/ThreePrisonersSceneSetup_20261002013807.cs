using System.Collections.Generic;
using System.IO;
using MindParadox.Games.ThreePrisoners;
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
    /// ThreePrisonersScene이 없으면 세 죄수 문제 화면을 만든다.
    /// </summary>
    [InitializeOnLoad]
    public static class ThreePrisonersSceneSetup
    {
        const string ScenePath = "Assets/Scenes/ThreePrisonersScene.unity";
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string KoreanFontPath = "Assets/Fonts/NanumGothic SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string SceneMarker = "m_Name: PrisonerRow";

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

        static ThreePrisonersSceneSetup()
        {
            EditorApplication.delayCall += TryEnsure;
        }

        [MenuItem("Mind Paradox/Rebuild Three Prisoners Scene")]
        public static void RebuildFromMenu()
        {
            if (!AssetsReady())
            {
                Debug.LogError("[Mind Paradox] 폰트 또는 버튼 스프라이트가 없어 세 죄수 문제 씬을 만들지 못했습니다.");
                return;
            }

            BuildScene();
            AddToBuildSettings();
            ValidateRules();
            Debug.Log("[Mind Paradox] ThreePrisonersScene을 다시 만들었습니다.");
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
            safeLayout.spacing = 16f;
            safeLayout.childAlignment = TextAnchor.UpperCenter;
            safeLayout.childControlWidth = true;
            safeLayout.childControlHeight = true;
            safeLayout.childForceExpandWidth = true;
            safeLayout.childForceExpandHeight = false;

            RectTransform header = CreateColumn("Header", safeArea, 260f);
            RectTransform topRow = CreateUiObject("TopRow", header);
            var topElement = topRow.gameObject.AddComponent<LayoutElement>();
            topElement.preferredHeight = PuzzleHeaderBar.ButtonHeight;
            var topLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            topLayout.spacing = PuzzleHeaderBar.Spacing;
            Button back = CreateActionButton(topRow, "BackButton", UiText.BackLabel, font, sprite, PuzzleHeaderBar.ButtonHeight);
            var backElement = back.gameObject.GetComponent<LayoutElement>();
            backElement.preferredWidth = PuzzleHeaderBar.ButtonWidth;
            backElement.minWidth = PuzzleHeaderBar.ButtonWidth;
            backElement.preferredHeight = PuzzleHeaderBar.ButtonHeight;
            backElement.minHeight = PuzzleHeaderBar.ButtonHeight;
            backElement.flexibleWidth = 0f;
            TMP_Text title = CreateText(header, "Title", ThreePrisonersCopy.Title, 70f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
            title.overflowMode = TextOverflowModes.Overflow;
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 120f;
            titleElement.minHeight = 100f;

            RectTransform scrollArea = CreateUiObject("BodyScroll", safeArea);
            var scrollElement = scrollArea.gameObject.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1f;
            scrollElement.minHeight = 900f;
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            RectTransform viewport = CreateStretch("Viewport", scrollArea);
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
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 20f;
            contentLayout.padding = new RectOffset(0, 0, 8, 24);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            TMP_Text situation = CreateText(content, "SituationText", ThreePrisonersCopy.Situation, 50f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, true);
            situation.overflowMode = TextOverflowModes.Overflow;

            RectTransform prisonerRow = CreateUiObject("PrisonerRow", content);
            var prisonerElement = prisonerRow.gameObject.AddComponent<LayoutElement>();
            prisonerElement.preferredHeight = 160f;
            prisonerElement.minHeight = 140f;
            var prisonerLayout = prisonerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            prisonerLayout.spacing = 16f;
            prisonerLayout.childAlignment = TextAnchor.MiddleCenter;
            prisonerLayout.childControlWidth = true;
            prisonerLayout.childControlHeight = true;
            prisonerLayout.childForceExpandWidth = true;
            prisonerLayout.childForceExpandHeight = true;
            PrisonerView prisonerA = CreatePrisoner(prisonerRow, "PrisonerA", ThreePrisonersCopy.PrisonerA, font, sprite, false);
            PrisonerView prisonerB = CreatePrisoner(prisonerRow, "PrisonerB", ThreePrisonersCopy.PrisonerB, font, sprite, true);
            PrisonerView prisonerC = CreatePrisoner(prisonerRow, "PrisonerC", ThreePrisonersCopy.PrisonerC, font, sprite, false);

            TMP_Text warden = CreateText(content, "WardenText", ThreePrisonersCopy.WardenLine, 60f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
            warden.overflowMode = TextOverflowModes.Overflow;
            var wardenElement = warden.gameObject.AddComponent<LayoutElement>();
            wardenElement.minHeight = 88f;

            TMP_Text question = CreateText(content, "QuestionText", ThreePrisonersCopy.Question, 50f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            question.overflowMode = TextOverflowModes.Overflow;
            var questionElement = question.gameObject.AddComponent<LayoutElement>();
            questionElement.minHeight = 72f;

            RectTransform choiceArea = CreateColumn("ChoiceArea", content, 360f);
            Button half = CreateActionButton(choiceArea, "ChoiceHalfButton", ThreePrisonersCopy.ChoiceHalf, font, sprite, 108f);
            Button third = CreateActionButton(choiceArea, "ChoiceThirdButton", ThreePrisonersCopy.ChoiceThird, font, sprite, 108f);
            Button twoThirds = CreateActionButton(choiceArea, "ChoiceTwoThirdsButton", ThreePrisonersCopy.ChoiceTwoThirds, font, sprite, 108f);

            TMP_Text result = CreateText(content, "ResultText", "", 60f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
            result.richText = true;
            result.overflowMode = TextOverflowModes.Overflow;
            var resultElement = result.gameObject.AddComponent<LayoutElement>();
            resultElement.minHeight = 80f;

            RectTransform actionArea = CreateUiObject("ActionArea", content);
            var actionElement = actionArea.gameObject.AddComponent<LayoutElement>();
            actionElement.preferredHeight = 120f;
            actionElement.minHeight = 108f;
            var actionLayout = actionArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 16f;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;
            Button replay = CreateActionButton(actionArea, "ReplayButton", ThreePrisonersCopy.Replay, font, sprite, 108f);
            Button simulate = CreateActionButton(actionArea, "SimulateButton", ThreePrisonersCopy.Simulate, font, sprite, 108f);
            replay.gameObject.SetActive(false);
            simulate.gameObject.SetActive(false);

            TMP_Text stats = CreateText(content, "StatsText", "조건에 맞는 라운드 0\nA 사면 0\nObserved: 0.0%\nTheory: 33.3%", 48f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            stats.overflowMode = TextOverflowModes.Overflow;
            var statsElement = stats.gameObject.AddComponent<LayoutElement>();
            statsElement.minHeight = 200f;

            CreateEventSystem();

            var host = new GameObject("ThreePrisoners");
            var game = host.AddComponent<ThreePrisonersGameManager>();
            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("titleText").objectReferenceValue = title;
            serializedGame.FindProperty("situationText").objectReferenceValue = situation;
            serializedGame.FindProperty("wardenText").objectReferenceValue = warden;
            serializedGame.FindProperty("questionText").objectReferenceValue = question;
            serializedGame.FindProperty("resultText").objectReferenceValue = result;
            serializedGame.FindProperty("statsText").objectReferenceValue = stats;
            serializedGame.FindProperty("prisonerA").objectReferenceValue = prisonerA.Face;
            serializedGame.FindProperty("prisonerB").objectReferenceValue = prisonerB.Face;
            serializedGame.FindProperty("prisonerC").objectReferenceValue = prisonerC.Face;
            serializedGame.FindProperty("prisonerALabel").objectReferenceValue = prisonerA.Label;
            serializedGame.FindProperty("prisonerBLabel").objectReferenceValue = prisonerB.Label;
            serializedGame.FindProperty("prisonerCLabel").objectReferenceValue = prisonerC.Label;
            serializedGame.FindProperty("backButton").objectReferenceValue = back;
            serializedGame.FindProperty("choiceHalfButton").objectReferenceValue = half;
            serializedGame.FindProperty("choiceThirdButton").objectReferenceValue = third;
            serializedGame.FindProperty("choiceTwoThirdsButton").objectReferenceValue = twoThirds;
            serializedGame.FindProperty("replayButton").objectReferenceValue = replay;
            serializedGame.FindProperty("simulateButton").objectReferenceValue = simulate;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            Stretch(canvasObject.GetComponent<RectTransform>());
            PuzzleExplanationSetup.AttachInActiveScene("three-prisoners");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            bool rewritten = FixSavedCanvasRect(ScenePath);

            if (!string.IsNullOrEmpty(returnPath) && returnPath != ScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            else if (rewritten)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        struct PrisonerView
        {
            public Image Face;
            public TMP_Text Label;
        }

        static PrisonerView CreatePrisoner(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite, bool named)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var face = root.AddComponent<Image>();
            face.sprite = sprite;
            face.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            face.color = named ? ButtonColor : CardColor;
            face.raycastTarget = false;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 24, 24);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TMP_Text nameText = CreateText(root.transform, "Name", label, 50f, FontStyles.Bold, named ? Color.white : CardTitleColor, TextAlignmentOptions.Center, font, false);
            var nameElement = nameText.gameObject.AddComponent<LayoutElement>();
            nameElement.preferredHeight = 72f;

            return new PrisonerView { Face = face, Label = nameText };
        }

        static void AddToBuildSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                return;

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

            const string glyphs = "세죄수문제명이있습니다정확히한명만사면되고나머지두명은처형됩니다에게묻습니다나말고중될사람이름을알려주세요가처형됩니다확률은정답오답다시하기회실험조건에맞는라운드사면간수는말할수없습니다되면반드시라고그래서입니다";
            korean.TryAddCharacters(glyphs, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[Mind Paradox] 세 죄수 문제 안내 중 폰트에 없는 문자: " + missing);

            EditorUtility.SetDirty(korean);
        }

        static void ValidateRules()
        {
            if (ThreePrisonersRules.FormatPercent(ThreePrisonersRules.Theory) != "33.3%")
                Debug.LogError("[Mind Paradox] 세 죄수 문제 이론값이 33.3%가 아닙니다.");

            var forced = new System.Random(1);
            if (ThreePrisonersRules.WardenNames(Prisoner.B, forced) != Prisoner.C)
                Debug.LogError("[Mind Paradox] B가 사면되면 간수는 C를 말해야 합니다.");
            if (ThreePrisonersRules.WardenNames(Prisoner.C, forced) != Prisoner.B)
                Debug.LogError("[Mind Paradox] C가 사면되면 간수는 B를 말해야 합니다.");

            var random = new System.Random(11);
            ThreePrisonersRules.RunBatch(8000, random, out int conditioned, out int aPardoned);
            if (conditioned < 3400 || conditioned > 4600)
                Debug.LogError("[Mind Paradox] B가 처형된 라운드 수가 절반에서 멀리 떨어졌습니다. " + conditioned);

            if (conditioned > 0)
            {
                double rate = aPardoned / (double)conditioned;
                if (rate < 0.28d || rate > 0.39d)
                    Debug.LogError("[Mind Paradox] B 발언 조건에서 A 사면 비율이 1/3에서 멀리 떨어졌습니다. " + aPardoned + " / " + conditioned);
            }
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

            TMP_Text caption = CreateText(buttonObject.transform, "Label", label, 48f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, false);
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

        static RectTransform CreateColumn(string objectName, Transform parent, float height)
        {
            RectTransform rect = CreateUiObject(objectName, parent);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
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
