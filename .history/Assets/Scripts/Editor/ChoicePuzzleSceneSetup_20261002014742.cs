using System.Collections.Generic;
using System.IO;
using MindParadox.Games.ChoicePuzzle;
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
    /// 선택형 퍼즐 6개의 씬을 같은 화면 구조로 만든다.
    /// 앞 퍼즐 규칙이 맞지 않으면 다음 씬은 만들지 않는다.
    /// </summary>
    [InitializeOnLoad]
    public static class ChoicePuzzleSceneSetup
    {
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string SceneMarker = "m_Name: ChoiceBoard";

        static readonly Color BackgroundColor = new Color32(0x10, 0x18, 0x26, 0xFF);
        static readonly Color TitleColor = new Color32(0xF7, 0xF8, 0xFB, 0xFF);
        static readonly Color SubtitleColor = new Color32(0xA8, 0xB3, 0xC7, 0xFF);
        static readonly Color ButtonColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color ButtonHighlight = new Color32(0x3E, 0x78, 0xF0, 0xFF);
        static readonly Color ButtonPressed = new Color32(0x1E, 0x4C, 0xC0, 0xFF);
        static readonly Color ButtonDisabled = new Color32(0x9A, 0xA4, 0xB5, 0xFF);

        struct PuzzleScene
        {
            public string Id;
            public string Path;
            public string Title;
            public System.Func<bool> Validate;
        }

        static readonly PuzzleScene[] Puzzles =
        {
            new PuzzleScene { Id = BoyGirlParadoxPuzzle.Id, Path = "Assets/Scenes/BoyGirlParadoxScene.unity", Title = "소년·소녀 역설", Validate = BoyGirlParadoxPuzzle.Validate },
            new PuzzleScene { Id = TwoEnvelopesPuzzle.Id, Path = "Assets/Scenes/TwoEnvelopesScene.unity", Title = "두 봉투 역설", Validate = TwoEnvelopesPuzzle.Validate },
            new PuzzleScene { Id = FalsePositivePuzzle.Id, Path = "Assets/Scenes/FalsePositiveScene.unity", Title = "검사 정확도 함정", Validate = FalsePositivePuzzle.Validate },
            new PuzzleScene { Id = GamblersFallacyPuzzle.Id, Path = "Assets/Scenes/GamblersFallacyScene.unity", Title = "도박사의 오류", Validate = GamblersFallacyPuzzle.Validate },
            new PuzzleScene { Id = TwoCardProblemPuzzle.Id, Path = "Assets/Scenes/TwoCardProblemScene.unity", Title = "카드 두 장 문제", Validate = TwoCardProblemPuzzle.Validate },
            new PuzzleScene { Id = ThreeDoorChoicePuzzle.Id, Path = "Assets/Scenes/ThreeDoorChoiceScene.unity", Title = "세 문 선택 확률", Validate = ThreeDoorChoicePuzzle.Validate }
        };

        static int attempts;

        static ChoicePuzzleSceneSetup()
        {
            EditorApplication.delayCall += TryEnsure;
        }

        [MenuItem("Mind Paradox/Rebuild Choice Puzzle Scenes")]
        public static void RebuildFromMenu()
        {
            if (!AssetsReady())
            {
                Debug.LogError("[Mind Paradox] 폰트 또는 버튼 스프라이트가 없어 선택형 퍼즐 씬을 만들지 못했습니다.");
                return;
            }

            BuildMissing(true);
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

            BuildMissing(false);
        }

        static bool AssetsReady()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null
                && AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) != null;
        }

        static void BuildMissing(bool force)
        {
            string returnPath = EditorSceneManager.GetActiveScene().path;
            if (EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveOpenScenes();

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            for (int i = 0; i < Puzzles.Length; i++)
            {
                PuzzleScene puzzle = Puzzles[i];
                if (!puzzle.Validate())
                {
                    Debug.LogError("[Mind Paradox] " + puzzle.Title + " 규칙을 확인하지 못해 다음 퍼즐은 만들지 않았습니다.");
                    break;
                }

                Debug.Log("[Mind Paradox] " + puzzle.Title + " 규칙을 확인했습니다.");
                bool missing = AssetDatabase.LoadAssetAtPath<SceneAsset>(puzzle.Path) == null;
                bool oldLayout = !missing && File.Exists(puzzle.Path) && !File.ReadAllText(puzzle.Path).Contains(SceneMarker);
                bool wrongPuzzle = !missing && File.Exists(puzzle.Path) && !File.ReadAllText(puzzle.Path).Contains("puzzleId: " + puzzle.Id);
                if (force || missing || oldLayout || wrongPuzzle)
                    BuildScene(puzzle, font, sprite);

                AddToBuildSettings(puzzle.Path);
            }

            if (!string.IsNullOrEmpty(returnPath) && returnPath != EditorSceneManager.GetActiveScene().path)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
        }

        static void BuildScene(PuzzleScene puzzle, TMP_FontAsset font, Sprite sprite)
        {
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
            Button back = CreateActionButton(topRow, "BackButton", UiText.BackLabel, font, sprite, PuzzleHeaderBar.ButtonHeight, false);
            var backElement = back.gameObject.GetComponent<LayoutElement>();
            backElement.preferredWidth = PuzzleHeaderBar.ButtonWidth;
            backElement.minWidth = PuzzleHeaderBar.ButtonWidth;
            backElement.flexibleWidth = 0f;

            TMP_Text title = CreateText(header, "Title", puzzle.Title, 70f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
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

            TMP_Text situation = CreateText(content, "SituationText", "", 50f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, true);
            situation.overflowMode = TextOverflowModes.Overflow;

            TMP_Text board = CreateText(content, "ChoiceBoard", "", 50f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
            board.richText = true;
            board.overflowMode = TextOverflowModes.Overflow;
            var boardElement = board.gameObject.AddComponent<LayoutElement>();
            boardElement.minHeight = 160f;

            TMP_Text question = CreateText(content, "QuestionText", "", 50f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            question.overflowMode = TextOverflowModes.Overflow;
            var questionElement = question.gameObject.AddComponent<LayoutElement>();
            questionElement.minHeight = 80f;

            RectTransform choiceArea = CreateColumn("ChoiceArea", content, 0f);
            var choiceElement = choiceArea.gameObject.GetComponent<LayoutElement>();
            choiceElement.preferredHeight = -1f;
            choiceElement.minHeight = 120f;
            Button choiceTemplate = CreateActionButton(choiceArea, "ChoiceTemplate", "선택", font, sprite, 168f, true);
            choiceTemplate.gameObject.SetActive(false);

            TMP_Text result = CreateText(content, "ResultText", "", 60f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
            result.richText = true;
            result.overflowMode = TextOverflowModes.Overflow;
            var resultElement = result.gameObject.AddComponent<LayoutElement>();
            resultElement.minHeight = 80f;

            RectTransform actionArea = CreateUiObject("ActionArea", content);
            var actionElement = actionArea.gameObject.AddComponent<LayoutElement>();
            actionElement.minHeight = 120f;
            actionElement.preferredHeight = -1f;
            var actionLayout = actionArea.gameObject.AddComponent<VerticalLayoutGroup>();
            actionLayout.spacing = 16f;
            actionLayout.childAlignment = TextAnchor.UpperCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = true;
            actionLayout.childForceExpandHeight = false;

            RectTransform experimentRow = CreateUiObject("ExperimentRow", actionArea);
            var experimentElement = experimentRow.gameObject.AddComponent<LayoutElement>();
            experimentElement.preferredHeight = 124f;
            experimentElement.minHeight = 108f;
            var experimentLayout = experimentRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            experimentLayout.spacing = 16f;
            experimentLayout.childAlignment = TextAnchor.MiddleCenter;
            experimentLayout.childControlWidth = true;
            experimentLayout.childControlHeight = true;
            experimentLayout.childForceExpandWidth = true;
            experimentLayout.childForceExpandHeight = true;
            Button experimentTemplate = CreateActionButton(experimentRow, "ExperimentTemplate", "실험", font, sprite, 124f, false);
            experimentTemplate.gameObject.SetActive(false);

            Button next = CreateActionButton(actionArea, "NextButton", "다음 질문", font, sprite, 124f, true);
            next.gameObject.SetActive(false);
            Button replay = CreateActionButton(actionArea, "ReplayButton", "다시 하기", font, sprite, 124f, false);
            replay.gameObject.SetActive(false);

            TMP_Text stats = CreateText(content, "StatsText", "", 48f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            stats.overflowMode = TextOverflowModes.Overflow;
            var statsElement = stats.gameObject.AddComponent<LayoutElement>();
            statsElement.minHeight = 180f;

            CreateEventSystem();

            var host = new GameObject("ChoicePuzzle");
            var game = host.AddComponent<ChoicePuzzleController>();
            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("puzzleId").stringValue = puzzle.Id;
            serializedGame.FindProperty("titleText").objectReferenceValue = title;
            serializedGame.FindProperty("situationText").objectReferenceValue = situation;
            serializedGame.FindProperty("questionText").objectReferenceValue = question;
            serializedGame.FindProperty("boardText").objectReferenceValue = board;
            serializedGame.FindProperty("resultText").objectReferenceValue = result;
            serializedGame.FindProperty("statsText").objectReferenceValue = stats;
            serializedGame.FindProperty("choiceRoot").objectReferenceValue = choiceArea;
            serializedGame.FindProperty("choiceTemplate").objectReferenceValue = choiceTemplate;
            serializedGame.FindProperty("experimentRoot").objectReferenceValue = experimentRow;
            serializedGame.FindProperty("experimentTemplate").objectReferenceValue = experimentTemplate;
            serializedGame.FindProperty("backButton").objectReferenceValue = back;
            serializedGame.FindProperty("replayButton").objectReferenceValue = replay;
            serializedGame.FindProperty("nextButton").objectReferenceValue = next;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            Stretch(canvasObject.GetComponent<RectTransform>());
            PuzzleExplanationSetup.AttachInActiveScene(puzzle.Id);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), puzzle.Path);
            FixSavedCanvasRect(puzzle.Path);
        }

        static void AddToBuildSettings(string scenePath)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                return;

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == scenePath)
                    return;
            }

            var list = new List<EditorBuildSettingsScene>(existing);
            list.Add(new EditorBuildSettingsScene(scenePath, true));
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

        static Button CreateActionButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite, float height, bool wrap)
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

            TMP_Text caption = CreateText(buttonObject.transform, "Label", label, 48f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, wrap);
            caption.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
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
            element.preferredHeight = height > 0f ? height : -1f;
            if (height > 0f)
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
