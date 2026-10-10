using System.IO;
using MindParadox.Data;
using MindParadox.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.EditorTools
{
    /// <summary>
    /// 공통 설명 팝업과 데이터를 만들고, 퍼즐 씬 헤더에 설명 버튼을 붙인다.
    /// </summary>
    [InitializeOnLoad]
    public static class PuzzleExplanationSetup
    {
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string KoreanFontPath = "Assets/Fonts/NanumGothic SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string PopupPrefabPath = "Assets/Prefabs/PuzzleExplanationPopup.prefab";
        const string ExplanationFolder = "Assets/Data/Explanations";
        const string ButtonName = "ExplanationButton";

        static readonly Color CardColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color TitleColor = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color BodyColor = new Color32(0x4A, 0x55, 0x68, 0xFF);
        static readonly Color ButtonColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color ButtonHighlight = new Color32(0x3E, 0x78, 0xF0, 0xFF);
        static readonly Color ButtonPressed = new Color32(0x1E, 0x4C, 0xC0, 0xFF);
        static readonly Color ButtonDisabled = new Color32(0x9A, 0xA4, 0xB5, 0xFF);
        static readonly Color DimColor = new Color(0.063f, 0.094f, 0.149f, 0.86f);

        static int attempts;

        static PuzzleExplanationSetup()
        {
            EditorApplication.delayCall += TryEnsure;
        }

        [MenuItem("Mind Paradox/Add Puzzle Explanations")]
        public static void RebuildFromMenu()
        {
            if (!AssetsReady())
            {
                Debug.LogError("[Mind Paradox] 폰트 또는 버튼 스프라이트가 없어 설명 팝업을 만들지 못했습니다.");
                return;
            }

            EnsureData();
            EnsurePrefab();
            EnsureScenes();
            EnsureHeaderBars();
            Debug.Log("[Mind Paradox] 퍼즐 설명 팝업을 연결했습니다.");
        }

        public static void AttachInActiveScene(string puzzleId)
        {
            if (!AssetsReady())
                return;

            EnsureData();
            EnsurePrefab();
            AttachButton(puzzleId);
            StyleActiveHeader();
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

            EnsureData();
            EnsurePrefab();
            EnsureScenes();
            EnsureHeaderBars();
        }

        static bool AssetsReady()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null
                && AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) != null;
        }

        static void EnsureData()
        {
            Directory.CreateDirectory(ExplanationFolder);
            AddKoreanGlyphs();

            for (int i = 0; i < Entries.Length; i++)
            {
                ExplanationEntry entry = Entries[i];
                ExplanationData data = LoadOrCreate(entry);
                PuzzleData puzzle = AssetDatabase.LoadAssetAtPath<PuzzleData>(entry.PuzzlePath);
                if (data == null || puzzle == null)
                    continue;

                var serialized = new SerializedObject(puzzle);
                SerializedProperty property = serialized.FindProperty("explanation");
                if (property == null || property.objectReferenceValue == data)
                    continue;

                property.objectReferenceValue = data;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(puzzle);
            }

            AssetDatabase.SaveAssets();
        }

        static ExplanationData LoadOrCreate(ExplanationEntry entry)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ExplanationData>(entry.AssetPath);
            if (existing != null)
                return existing;

            var data = ScriptableObject.CreateInstance<ExplanationData>();
            data.puzzleId = entry.PuzzleId;
            data.explanationTitle = entry.Title;
            data.explanationSummary = entry.Summary;
            data.explanationBody = entry.Body;
            data.explanationFormula = entry.Formula;
            data.explanationKey = entry.Key;
            data.titleKey = entry.Key + ".title";
            data.summaryKey = entry.Key + ".summary";
            data.bodyKey = entry.Key + ".body";
            data.formulaKey = entry.Key + ".formula";
            AssetDatabase.CreateAsset(data, entry.AssetPath);
            return data;
        }

        static void EnsurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath) != null)
                return;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            PuzzleExplanationPopup popup = CreatePopupObject(font, sprite);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(popup.gameObject, PopupPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }

            AssetDatabase.ImportAsset(PopupPrefabPath);
        }

        static void EnsureScenes()
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                ExplanationEntry entry = Entries[i];
                if (!File.Exists(entry.ScenePath))
                    continue;
                if (File.ReadAllText(entry.ScenePath).Contains("m_Name: " + ButtonName))
                    continue;

                string returnPath = EditorSceneManager.GetActiveScene().path;
                if (EditorSceneManager.GetActiveScene().isDirty)
                    EditorSceneManager.SaveOpenScenes();

                EditorSceneManager.OpenScene(entry.ScenePath, OpenSceneMode.Single);
                bool added = AttachButton(entry.PuzzleId);
                if (added)
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

                if (!string.IsNullOrEmpty(returnPath) && returnPath != entry.ScenePath)
                    EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            }
        }

        static void EnsureHeaderBars()
        {
            string guid = AssetDatabase.AssetPathToGUID("Assets/Scripts/UI/PuzzleHeaderBar.cs");
            if (string.IsNullOrEmpty(guid))
            {
                EditorApplication.delayCall += TryEnsure;
                return;
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                string path = Entries[i].ScenePath;
                if (!File.Exists(path) || File.ReadAllText(path).Contains(guid))
                    continue;

                string returnPath = EditorSceneManager.GetActiveScene().path;
                if (EditorSceneManager.GetActiveScene().isDirty)
                    EditorSceneManager.SaveOpenScenes();

                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (StyleActiveHeader())
                    EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

                if (!string.IsNullOrEmpty(returnPath) && returnPath != path)
                    EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            }
        }

        static bool StyleActiveHeader()
        {
            Transform topRow = FindInActiveScene("TopRow");
            if (topRow == null)
                return false;

            if (topRow.GetComponent<PuzzleHeaderBar>() == null)
                topRow.gameObject.AddComponent<PuzzleHeaderBar>();

            PuzzleHeaderBar.Apply(topRow);
            return true;
        }

        static bool AttachButton(string puzzleId)
        {
            if (GameObject.Find(ButtonName) != null)
                return false;

            Transform topRow = FindInActiveScene("TopRow");
            Transform canvas = FindInActiveScene("Canvas");
            if (topRow == null || canvas == null)
            {
                Debug.LogError("[Mind Paradox] 설명 버튼을 붙일 Header를 찾지 못했습니다. " + puzzleId);
                return false;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPrefabPath);
            ExplanationData data = ExplanationFor(puzzleId);
            if (prefab == null)
                return false;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            instance.name = "PuzzleExplanationPopup";
            Stretch(instance.GetComponent<RectTransform>());
            instance.transform.SetAsLastSibling();
            var popup = instance.GetComponent<PuzzleExplanationPopup>();

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            Button button = CreateHeaderButton(topRow, font, sprite);
            button.transform.SetSiblingIndex(1);

            var link = button.gameObject.AddComponent<PuzzleExplanationButton>();
            var serialized = new SerializedObject(link);
            serialized.FindProperty("explanation").objectReferenceValue = data;
            serialized.FindProperty("popup").objectReferenceValue = popup;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static ExplanationData ExplanationFor(string puzzleId)
        {
            for (int i = 0; i < Entries.Length; i++)
            {
                if (Entries[i].PuzzleId == puzzleId)
                    return AssetDatabase.LoadAssetAtPath<ExplanationData>(Entries[i].AssetPath);
            }

            return null;
        }

        static Transform FindInActiveScene(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            return found != null ? found.transform : null;
        }

        static PuzzleExplanationPopup CreatePopupObject(TMP_FontAsset font, Sprite sprite)
        {
            var root = new GameObject("PuzzleExplanationPopup", typeof(RectTransform));
            Stretch(root.GetComponent<RectTransform>());

            RectTransform dim = CreateStretch("DimmedBackground", root.transform);
            var dimImage = dim.gameObject.AddComponent<Image>();
            dimImage.color = DimColor;
            dimImage.raycastTarget = true;

            RectTransform safe = CreateStretch("SafeArea", root.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            RectTransform panel = CreateUiObject("PopupPanel", safe);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = new Vector2(48f, 48f);
            panel.offsetMax = new Vector2(-48f, -48f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = sprite;
            panelImage.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            panelImage.color = CardColor;
            var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(36, 36, 32, 32);
            panelLayout.spacing = 16f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            TMP_Text title = CreateText(panel, "TitleText", "설명", 60f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, true);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 160f;
            titleElement.flexibleHeight = 0f;

            RectTransform scrollArea = CreateUiObject("ScrollView", panel);
            var scrollElement = scrollArea.gameObject.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1f;
            scrollElement.minHeight = 240f;
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
            contentLayout.spacing = 28f;
            contentLayout.padding = new RectOffset(8, 8, 8, 24);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            TMP_Text summary = CreateBodyText(content, "SummaryText", 50f, FontStyles.Bold, TitleColor, font);
            TMP_Text body = CreateBodyText(content, "ExplanationText", 50f, FontStyles.Normal, BodyColor, font);
            TMP_Text formula = CreateBodyText(content, "FormulaText", 60f, FontStyles.Bold, TitleColor, font);

            Button close = CreateCloseButton(panel, font, sprite);

            var popup = root.AddComponent<PuzzleExplanationPopup>();
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("summaryText").objectReferenceValue = summary;
            serialized.FindProperty("explanationText").objectReferenceValue = body;
            serialized.FindProperty("formulaText").objectReferenceValue = formula;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.FindProperty("scrollRect").objectReferenceValue = scroll;
            serialized.FindProperty("contentRoot").objectReferenceValue = content;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return popup;
        }

        static TMP_Text CreateBodyText(Transform parent, string name, float size, FontStyles style, Color color, TMP_FontAsset font)
        {
            TMP_Text text = CreateText(parent, name, "", size, style, color, TextAlignmentOptions.Center, font, true);
            text.overflowMode = TextOverflowModes.Overflow;
            var element = text.gameObject.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            var textFitter = text.gameObject.AddComponent<ContentSizeFitter>();
            textFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return text;
        }

        static Button CreateCloseButton(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            return CreateColoredButton(parent, "CloseButton", "닫기", font, sprite, 108f, 48f, 1f);
        }

        static Button CreateHeaderButton(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            Button button = CreateColoredButton(parent, ButtonName, UiText.ExplanationLabel, font, sprite, PuzzleHeaderBar.ButtonHeight, PuzzleHeaderBar.FontSize, 0f);
            var element = button.GetComponent<LayoutElement>();
            element.preferredWidth = PuzzleHeaderBar.ButtonWidth;
            element.minWidth = PuzzleHeaderBar.ButtonWidth;
            element.preferredHeight = PuzzleHeaderBar.ButtonHeight;
            element.minHeight = PuzzleHeaderBar.ButtonHeight;
            element.flexibleWidth = 0f;
            TMP_Text caption = button.GetComponentInChildren<TMP_Text>();
            if (caption != null)
            {
                caption.textWrappingMode = TextWrappingModes.NoWrap;
                caption.margin = new Vector4(PuzzleHeaderBar.TextPadding, 0f, PuzzleHeaderBar.TextPadding, 0f);
            }

            return button;
        }

        static Button CreateColoredButton(Transform parent, string name, string label, TMP_FontAsset font, Sprite sprite, float height, float fontSize, float flexibleWidth)
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
            element.preferredWidth = flexibleWidth > 0f ? -1f : 210f;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = 0f;

            TMP_Text caption = CreateText(buttonObject.transform, "Label", label, fontSize, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, true);
            Stretch(caption.rectTransform);
            return button;
        }

        static void AddKoreanGlyphs()
        {
            TMP_FontAsset korean = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (korean == null)
                return;

            const string glyphs = "설명왜문을바꾸는것이유리할까처음선택한자동차가있을확률은개가라면이다틀렸을진행자는위치를알고있고반드시염소있는만연다따라서였던경우남은으로바꾸면얻게된다개의사람만있어도넘을생일만보면드문일처럼보입니다실제로는모든쌍을서로비교합니다명에게서만들어지는수는늘어날수록비교되는매우빠르게증가따라서최소두같을약은모든생일이다를먼저계산하고그값을에서뺍니다닫기아직연결되지않았습니다금화나온뒤세가지중두가지는동전도";
            korean.TryAddCharacters(glyphs, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[Mind Paradox] 설명 문장 중 폰트에 없는 문자: " + missing);

            EditorUtility.SetDirty(korean);
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
            tmp.overflowMode = TextOverflowModes.Overflow;
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

        struct ExplanationEntry
        {
            public string PuzzleId;
            public string PuzzlePath;
            public string ScenePath;
            public string AssetPath;
            public string Key;
            public string Title;
            public string Summary;
            public string Body;
            public string Formula;
        }

        static readonly ExplanationEntry[] Entries =
        {
            new ExplanationEntry
            {
                PuzzleId = "monty-hall",
                PuzzlePath = "Assets/Data/Puzzles/PuzzleData_MontyHall.asset",
                ScenePath = "Assets/Scenes/MontyHallScene.unity",
                AssetPath = "Assets/Data/Explanations/ExplanationData_MontyHall.asset",
                Key = "explanation.monty-hall",
                Title = "왜 문을 바꾸는 것이 유리할까?",
                Summary = "바꾸는 쪽이 유리한 이유는\n처음 선택이 틀릴 확률이 더 크기 때문입니다.",
                Body =
                    "처음 선택한 문에 자동차가 있을 확률은\n문이 3개라면 1/3입니다.\n\n" +
                    "처음 선택이 틀렸을 확률은 2/3입니다.\n\n" +
                    "진행자는 자동차 위치를 알고 있고\n반드시 염소가 있는 문만 엽니다.\n\n" +
                    "따라서 처음 선택이 틀렸던 2/3의 경우,\n남은 문으로 바꾸면 자동차를 얻게 됩니다.",
                Formula =
                    "3 Doors\nStay 33.3%\nSwitch 66.7%\n\n" +
                    "N개의 문\nStay = 1 / N\nSwitch = (N - 1) / N\n\n" +
                    "10 Doors\nStay 10%\nSwitch 90%\n\n" +
                    "100 Doors\nStay 1%\nSwitch 99%"
            },
            new ExplanationEntry
            {
                PuzzleId = "birthday-paradox",
                PuzzlePath = "Assets/Data/Puzzles/PuzzleData_BirthdayParadox.asset",
                ScenePath = "Assets/Scenes/BirthdayParadoxScene.unity",
                AssetPath = "Assets/Data/Explanations/ExplanationData_BirthdayParadox.asset",
                Key = "explanation.birthday-paradox",
                Title = "왜 23명만 있어도 50%를 넘을까?",
                Summary = "한 사람의 생일만 보면 드문 일처럼 보입니다.\n실제로는 모든 사람 쌍을 서로 비교합니다.",
                Body =
                    "23명의 사람에게서 만들어지는 쌍의 수는\n\n" +
                    "23 × 22 / 2\n= 253쌍\n\n" +
                    "사람 수가 늘어날수록\n비교되는 쌍의 수는 매우 빠르게 증가합니다.\n\n" +
                    "따라서 23명만 있어도\n최소 두 사람의 생일이 같을 확률은 약 50.7%입니다.",
                Formula =
                    "P(match) = 1 - P(all different)\n\n" +
                    "모든 사람의 생일이 서로 다를 확률을 먼저 계산하고\n그 값을 1에서 뺍니다."
            },
            new ExplanationEntry
            {
                PuzzleId = "bertrand-box",
                PuzzlePath = "Assets/Data/Puzzles/PuzzleData_BertrandBox.asset",
                ScenePath = "Assets/Scenes/BertrandBoxScene.unity",
                AssetPath = "Assets/Data/Explanations/ExplanationData_BertrandBox.asset",
                Key = "explanation.bertrand-box",
                Title = "왜 남은 금화의 확률이 2/3일까?",
                Summary = "금화가 나온 경우를 하나씩 세면\n세 가지 중 두 가지는 남은 동전도 금화입니다.",
                Body =
                    "금화가 나올 수 있는 경우는 세 가지입니다.\n\n" +
                    "GG 상자의 첫 번째 금화\n" +
                    "GG 상자의 두 번째 금화\n" +
                    "GS 상자의 금화\n\n" +
                    "남은 동전도 금화인 경우는\nGG 상자의 두 가지입니다.",
                Formula = "2 / 3 = 66.7%"
            }
        };
    }
}
