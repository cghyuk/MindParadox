using System.Collections.Generic;
using System.IO;
using MindParadox.Games.BirthdayParadox;
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
    /// BirthdayParadoxScene이 없으면 기본 카드/버튼으로 생일 문제 화면을 만든다.
    /// </summary>
    [InitializeOnLoad]
    public static class BirthdayParadoxSceneSetup
    {
        const string ScenePath = "Assets/Scenes/BirthdayParadoxScene.unity";
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string KoreanFontPath = "Assets/Fonts/NanumGothic SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string PersonPrefabPath = "Assets/Prefabs/PersonBirthdayItem.prefab";
        const string SceneMarker = "m_Name: BirthdayScrollView";

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

        static BirthdayParadoxSceneSetup()
        {
            EditorApplication.delayCall += TryEnsure;
        }

        [MenuItem("Mind Paradox/Rebuild Birthday Paradox Scene")]
        public static void RebuildFromMenu()
        {
            if (!AssetsReady())
            {
                Debug.LogError("[Mind Paradox] 폰트 또는 버튼 스프라이트가 없어 Birthday Paradox 씬을 만들지 못했습니다.");
                return;
            }

            BuildScene();
            AddToBuildSettings();
            ValidateRules();
            Debug.Log("[Mind Paradox] BirthdayParadoxScene을 다시 만들었습니다.");
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
            RepairPersonPrefabReference();
        }

        static void RepairPersonPrefabReference()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPath);
            PersonBirthdayItem item = prefab != null ? prefab.GetComponent<PersonBirthdayItem>() : null;
            if (item == null)
                return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;

            var manager = Object.FindFirstObjectByType<BirthdayParadoxGameManager>();
            if (manager == null)
                return;

            var serialized = new SerializedObject(manager);
            SerializedProperty property = serialized.FindProperty("personPrefab");
            if (property == null || property.objectReferenceValue == item)
                return;

            property.objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
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
            PersonBirthdayItem personPrefab = EnsurePersonPrefab(font, sprite);
            if (EditorSceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveOpenScenes();

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

            RectTransform header = CreateUiObject("Header", safeArea);
            var headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.preferredHeight = 188f;
            headerElement.flexibleHeight = 0f;
            var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.UpperCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            RectTransform topRow = CreateUiObject("TopRow", header);
            var topElement = topRow.gameObject.AddComponent<LayoutElement>();
            topElement.preferredHeight = 88f;
            var topLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.childAlignment = TextAnchor.MiddleLeft;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = false;
            Button back = CreateActionButton(topRow, "BackButton", "뒤로", font, sprite);
            var backElement = back.gameObject.GetComponent<LayoutElement>();
            backElement.preferredWidth = 220f;
            backElement.flexibleWidth = 0f;

            TMP_Text title = CreateText(header, "Title", "Birthday Paradox", 56f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 72f;

            RectTransform settings = CreateColumn("SettingsArea", safeArea, 248f);
            RectTransform countRow = CreateUiObject("CountRow", settings);
            var countElement = countRow.gameObject.AddComponent<LayoutElement>();
            countElement.preferredHeight = 88f;
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
            TMP_Text peopleCount = CreateText(countRow, "PeopleCountText", "23", 40f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var peopleCountElement = peopleCount.gameObject.AddComponent<LayoutElement>();
            peopleCountElement.flexibleWidth = 1f;
            peopleCountElement.preferredHeight = 80f;
            Button plus = CreateActionButton(countRow, "PlusButton", "+", font, sprite);
            var plusElement = plus.gameObject.GetComponent<LayoutElement>();
            plusElement.preferredWidth = 120f;
            plusElement.flexibleWidth = 0f;

            Slider peopleSlider = CreatePeopleSlider(settings);

            RectTransform quickRow = CreateUiObject("QuickRow", settings);
            var quickElement = quickRow.gameObject.AddComponent<LayoutElement>();
            quickElement.preferredHeight = 80f;
            var quickLayout = quickRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            quickLayout.spacing = 8f;
            quickLayout.childAlignment = TextAnchor.MiddleCenter;
            quickLayout.childControlWidth = true;
            quickLayout.childControlHeight = true;
            quickLayout.childForceExpandWidth = true;
            quickLayout.childForceExpandHeight = false;
            Button quick10 = CreateQuickButton(quickRow, "Quick10Button", "10", font, sprite);
            Button quick23 = CreateQuickButton(quickRow, "Quick23Button", "23", font, sprite);
            Button quick30 = CreateQuickButton(quickRow, "Quick30Button", "30", font, sprite);
            Button quick50 = CreateQuickButton(quickRow, "Quick50Button", "50", font, sprite);
            Button quick70 = CreateQuickButton(quickRow, "Quick70Button", "70", font, sprite);

            RectTransform probability = CreateColumn("ProbabilityArea", safeArea, 168f);
            TMP_Text people = CreateText(probability, "PeopleText", "People: 23", 34f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, false);
            var peopleElement = people.gameObject.AddComponent<LayoutElement>();
            peopleElement.preferredHeight = 40f;
            TMP_Text probabilityLabel = CreateText(probability, "ProbabilityLabel", "Birthday Match Probability", 30f, FontStyles.Normal, TitleColor, TextAlignmentOptions.Center, font, false);
            var probabilityLabelElement = probabilityLabel.gameObject.AddComponent<LayoutElement>();
            probabilityLabelElement.preferredHeight = 40f;
            TMP_Text probabilityValue = CreateText(probability, "ProbabilityText", "50.7%", 56f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var probabilityValueElement = probabilityValue.gameObject.AddComponent<LayoutElement>();
            probabilityValueElement.preferredHeight = 72f;

            RectTransform experiment = CreateColumn("ExperimentArea", safeArea, 248f);
            RectTransform trialRow = CreateUiObject("TrialRow", experiment);
            var trialElement = trialRow.gameObject.AddComponent<LayoutElement>();
            trialElement.preferredHeight = 96f;
            var trialLayout = trialRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            trialLayout.spacing = 12f;
            trialLayout.childAlignment = TextAnchor.MiddleCenter;
            trialLayout.childControlWidth = true;
            trialLayout.childControlHeight = true;
            trialLayout.childForceExpandWidth = true;
            trialLayout.childForceExpandHeight = false;
            Button once = CreateActionButton(trialRow, "OnceButton", "1회 실험", font, sprite);
            Button hundred = CreateActionButton(trialRow, "HundredButton", "100회 실험", font, sprite);
            Button reset = CreateActionButton(experiment, "ResetButton", "기록 초기화", font, sprite);
            TMP_Text result = CreateText(experiment, "ResultText", "", 32f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var resultElement = result.gameObject.AddComponent<LayoutElement>();
            resultElement.preferredHeight = 44f;

            RectTransform scrollArea = CreateUiObject("BirthdayScrollView", safeArea);
            var scrollElement = scrollArea.gameObject.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1f;
            scrollElement.minHeight = 240f;
            var scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
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
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = 8f;
            list.padding = new RectOffset(0, 0, 4, 4);
            list.childAlignment = TextAnchor.UpperCenter;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            RectTransform stats = CreateColumn("StatsArea", safeArea, 168f);
            TMP_Text statsText = CreateText(stats, "StatsText", "Trials: 0\nMatches: 0\nObserved: 0.0%\n\nTheory: 50.7%", 28f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            var statsTextElement = statsText.gameObject.AddComponent<LayoutElement>();
            statsTextElement.preferredHeight = 160f;

            RectTransform footer = CreateColumn("Footer", safeArea, 168f);
            TMP_Text explanation = CreateText(footer, "ExplanationText", "", 28f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            explanation.overflowMode = TextOverflowModes.Overflow;
            var explanationElement = explanation.gameObject.AddComponent<LayoutElement>();
            explanationElement.preferredHeight = 160f;

            CreateEventSystem();

            var host = new GameObject("BirthdayParadox");
            var game = host.AddComponent<BirthdayParadoxGameManager>();
            var serializedGame = new SerializedObject(game);
            serializedGame.FindProperty("personPrefab").objectReferenceValue = personPrefab;
            serializedGame.FindProperty("birthdayContent").objectReferenceValue = content;
            serializedGame.FindProperty("birthdayScroll").objectReferenceValue = scroll;
            serializedGame.FindProperty("peopleSlider").objectReferenceValue = peopleSlider;
            serializedGame.FindProperty("peopleCountText").objectReferenceValue = peopleCount;
            serializedGame.FindProperty("peopleText").objectReferenceValue = people;
            serializedGame.FindProperty("probabilityText").objectReferenceValue = probabilityValue;
            serializedGame.FindProperty("resultText").objectReferenceValue = result;
            serializedGame.FindProperty("statsText").objectReferenceValue = statsText;
            serializedGame.FindProperty("explanationText").objectReferenceValue = explanation;
            serializedGame.FindProperty("backButton").objectReferenceValue = back;
            serializedGame.FindProperty("minusButton").objectReferenceValue = minus;
            serializedGame.FindProperty("plusButton").objectReferenceValue = plus;
            serializedGame.FindProperty("onceButton").objectReferenceValue = once;
            serializedGame.FindProperty("hundredButton").objectReferenceValue = hundred;
            serializedGame.FindProperty("resetButton").objectReferenceValue = reset;
            serializedGame.FindProperty("quick10Button").objectReferenceValue = quick10;
            serializedGame.FindProperty("quick23Button").objectReferenceValue = quick23;
            serializedGame.FindProperty("quick30Button").objectReferenceValue = quick30;
            serializedGame.FindProperty("quick50Button").objectReferenceValue = quick50;
            serializedGame.FindProperty("quick70Button").objectReferenceValue = quick70;
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            Stretch(canvasObject.GetComponent<RectTransform>());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
            bool rewritten = FixSavedCanvasRect(ScenePath);

            if (!string.IsNullOrEmpty(returnPath) && returnPath != ScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            else if (rewritten)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static RectTransform CreateColumn(string name, Transform parent, float preferredHeight)
        {
            RectTransform column = CreateUiObject(name, parent);
            var element = column.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = preferredHeight;
            element.flexibleHeight = 0f;
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
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

            const string glyphs = "뒤로생일실험기록초기화같은있음없음명이있으면서로비교되는사람쌍은쌍입니다그래서생각보다적은인원에서도나올확률이빠르게높아집니다회";
            korean.TryAddCharacters(glyphs, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[Mind Paradox] 생일 문제 안내 중 폰트에 없는 문자: " + missing);

            EditorUtility.SetDirty(korean);
        }

        static void ValidateRules()
        {
            ExpectPercent(10, "11.7%");
            ExpectPercent(23, "50.7%");
            ExpectPercent(30, "70.6%");
            ExpectPercent(50, "97.0%");
            ExpectPercent(70, "99.9% 이상");
            ExpectPercent(100, "99.9% 이상");

            if (BirthdayParadoxCalculator.PairCount(23) != 253)
                Debug.LogError("[Mind Paradox] 23명의 비교 쌍이 253이 아닙니다.");

            BirthdayParadoxCalculator.GetMonthDay(0, out int month, out int day);
            if (month != 1 || day != 1)
                Debug.LogError("[Mind Paradox] 생일 0이 1/1이 아닙니다.");

            BirthdayParadoxCalculator.GetMonthDay(73, out month, out day);
            if (month != 3 || day != 15)
                Debug.LogError("[Mind Paradox] 생일 73이 3/15가 아닙니다.");

            var random = new System.Random(3);
            int sharedTrials = 0;
            for (int i = 0; i < 200; i++)
            {
                int[] days = BirthdayParadoxCalculator.CreateBirthdays(23, random);
                bool shared = BirthdayParadoxCalculator.HasSharedBirthday(days);
                bool[] flags = BirthdayParadoxCalculator.DuplicateFlags(days);
                int flagged = 0;
                for (int j = 0; j < flags.Length; j++)
                {
                    if (flags[j])
                        flagged++;
                }

                if (shared != (flagged >= 2))
                {
                    Debug.LogError("[Mind Paradox] 생일 중복 판정과 표시가 서로 다릅니다.");
                    return;
                }

                if (shared)
                    sharedTrials++;
            }

            if (sharedTrials < 70 || sharedTrials > 140)
                Debug.LogError("[Mind Paradox] 23명 200회 실험의 중복 횟수가 이론과 멀리 떨어졌습니다. " + sharedTrials);
        }

        static void ExpectPercent(int people, string expected)
        {
            string actual = BirthdayParadoxCalculator.FormatPercent(BirthdayParadoxCalculator.MatchProbability(people));
            if (actual != expected)
                Debug.LogError("[Mind Paradox] " + people + "명 이론 확률이 " + expected + "이 아닙니다. 실제 " + actual);
        }

        static PersonBirthdayItem EnsurePersonPrefab(TMP_FontAsset font, Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPath);
            if (existing != null)
            {
                PersonBirthdayItem ready = existing.GetComponent<PersonBirthdayItem>();
                if (ready != null)
                    return ready;
            }

            PersonBirthdayItem item = CreatePersonItem(font, sprite);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(item.gameObject, PersonPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(item.gameObject);
            }

            AssetDatabase.ImportAsset(PersonPrefabPath);
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPath);
            return saved != null ? saved.GetComponent<PersonBirthdayItem>() : null;
        }

        static PersonBirthdayItem CreatePersonItem(TMP_FontAsset font, Sprite sprite)
        {
            var root = new GameObject("PersonBirthdayItem", typeof(RectTransform));
            var image = root.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = CardColor;

            var element = root.AddComponent<LayoutElement>();
            element.preferredHeight = 72f;
            element.flexibleWidth = 1f;
            element.flexibleHeight = 0f;

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 8, 8);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TMP_Text number = CreateText(root.transform, "Number", "01", 32f, FontStyles.Bold, CardTitleColor, TextAlignmentOptions.Left, font, false);
            var numberElement = number.gameObject.AddComponent<LayoutElement>();
            numberElement.preferredWidth = 120f;
            numberElement.flexibleWidth = 0f;

            TMP_Text date = CreateText(root.transform, "Date", "1/1", 32f, FontStyles.Normal, CardTitleColor, TextAlignmentOptions.Left, font, false);
            var dateElement = date.gameObject.AddComponent<LayoutElement>();
            dateElement.flexibleWidth = 1f;

            var item = root.AddComponent<PersonBirthdayItem>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("face").objectReferenceValue = image;
            serialized.FindProperty("numberText").objectReferenceValue = number;
            serialized.FindProperty("dateText").objectReferenceValue = date;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        static Button CreateQuickButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite)
        {
            Button button = CreateActionButton(parent, name, label, font, sprite);
            var element = button.GetComponent<LayoutElement>();
            element.preferredHeight = 80f;
            element.minHeight = 72f;
            TMP_Text caption = button.GetComponentInChildren<TMP_Text>();
            if (caption != null)
                caption.fontSize = 32f;
            return button;
        }

        static Slider CreatePeopleSlider(Transform parent)
        {
            var root = new GameObject("PeopleSlider", typeof(RectTransform));
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
            slider.minValue = BirthdayParadoxCalculator.MinPeople;
            slider.maxValue = BirthdayParadoxCalculator.MaxPeople;
            slider.wholeNumbers = true;
            slider.value = BirthdayParadoxCalculator.DefaultPeople;
            return slider;
        }

        static Button CreateActionButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite)
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
            element.preferredHeight = 96f;
            element.minHeight = 88f;
            element.flexibleWidth = 1f;
            element.flexibleHeight = 0f;

            TMP_Text caption = CreateText(buttonObject.transform, "Label", label, 36f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, false);
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
