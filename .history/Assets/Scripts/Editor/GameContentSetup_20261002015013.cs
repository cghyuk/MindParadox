using System;
using System.Collections.Generic;
using System.IO;
using MindParadox.Data;
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
    /// 카테고리/문제 에셋을 만들고, 메인 메뉴와 공통 카테고리 씬을 연결한다.
    /// </summary>
    [InitializeOnLoad]
    public static class GameContentSetup
    {
        const string MainScenePath = "Assets/Scenes/MainScene.unity";
        const string CategoryScenePath = "Assets/Scenes/CategoryScene.unity";
        const string DatabasePath = "Assets/Data/GameContentDatabase.asset";
        const string CategoryFolder = "Assets/Data/Categories";
        const string PuzzleFolder = "Assets/Data/Puzzles";
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string GameCardPrefabPath = "Assets/Prefabs/GameCard.prefab";
        const string CategoryCardPrefabPath = "Assets/Prefabs/CategoryCard.prefab";

        static readonly Color BackgroundColor = new Color32(0x10, 0x18, 0x26, 0xFF);
        static readonly Color CardColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color TitleColor = new Color32(0xF7, 0xF8, 0xFB, 0xFF);
        static readonly Color SubtitleColor = new Color32(0xA8, 0xB3, 0xC7, 0xFF);
        static readonly Color CardTitleColor = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color CardBodyColor = new Color32(0x4A, 0x55, 0x68, 0xFF);
        static readonly Color ButtonColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color ButtonHighlight = new Color32(0x3E, 0x78, 0xF0, 0xFF);
        static readonly Color ButtonPressed = new Color32(0x1E, 0x4C, 0xC0, 0xFF);
        static readonly Color ButtonDisabled = new Color32(0x9A, 0xA4, 0xB5, 0xFF);

        const string ReadyKey = "MindParadox.GameContentReady";

        static bool refreshing;
        static int attempts;

        static GameContentSetup()
        {
            EditorApplication.delayCall += TryEnsure;
        }

        [MenuItem("Mind Paradox/Refresh Game Content")]
        public static void RefreshFromMenu()
        {
            EnsureContent();
            EnsureCategoryCardPrefab(null);
            EnsureCategoryScene();
            MigrateOpenOrSavedMainScene();
            Debug.Log("[Mind Paradox] 카테고리와 문제 목록을 갱신했습니다.");
        }

        static void TryEnsure()
        {
            if (SessionState.GetBool(ReadyKey, false))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryEnsure;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(GameCardPrefabPath) == null ||
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
            {
                attempts++;
                if (attempts < 40)
                    EditorApplication.delayCall += TryEnsure;
                return;
            }

            if (IsSetupCurrent())
            {
                SessionState.SetBool(ReadyKey, true);
                return;
            }

            EnsureContent();
            EnsureCategoryCardPrefab(null);
            EnsureCategoryScene();
            MigrateOpenOrSavedMainScene();
            SessionState.SetBool(ReadyKey, true);
        }

        static bool IsSetupCurrent()
        {
            if (!File.Exists(CategoryCardPrefabPath) || !File.Exists(CategoryScenePath) || !File.Exists(MainScenePath))
                return false;

            string cardGuid = AssetDatabase.AssetPathToGUID(CategoryCardPrefabPath);
            if (string.IsNullOrEmpty(cardGuid))
                return false;

            string scene = File.ReadAllText(MainScenePath);
            return scene.Contains("m_Name: CategoryScrollView")
                && scene.Contains(cardGuid)
                && !scene.Contains("m_Name: CategoryBar");
        }

        public static void EnsureContent()
        {
            if (refreshing)
                return;

            refreshing = true;
            try
            {
                Directory.CreateDirectory(CategoryFolder);
                Directory.CreateDirectory(PuzzleFolder);
                AssetDatabase.Refresh();

                foreach (CategorySeed seed in CategorySeeds())
                    EnsureCategory(seed);

                foreach (PuzzleSeed seed in PuzzleSeeds())
                    EnsurePuzzle(seed);

                RefreshDatabaseLists();
            }
            finally
            {
                refreshing = false;
            }
        }

        public static void AttachMenu(RectTransform safeArea, RectTransform categoryList, RectTransform categoryContent, GameObject menuHost, TMP_FontAsset font)
        {
            EnsureContent();
            if (categoryList != null && categoryList.name == "GameList")
                categoryList.name = "CategoryScrollView";

            if (safeArea != null)
            {
                Transform bar = safeArea.Find("CategoryBar");
                if (bar != null)
                    UnityEngine.Object.DestroyImmediate(bar.gameObject);
            }

            CategoryCardUI categoryPrefab = EnsureCategoryCardPrefab(font);
            WireMenu(menuHost, categoryContent, categoryPrefab);
        }

        static void MigrateOpenOrSavedMainScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
                return;

            var active = EditorSceneManager.GetActiveScene();
            string returnPath = active.path;
            bool switched = false;

            if (active.path != MainScenePath)
            {
                if (active.isDirty)
                    EditorSceneManager.SaveOpenScenes();

                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                switched = true;
            }

            bool changed = MigrateLoadedScene();
            if (changed)
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            if (switched && !string.IsNullOrEmpty(returnPath) && returnPath != MainScenePath)
                EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
        }

        static bool MigrateLoadedScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Transform safeArea = FindNamed(scene, "SafeArea");
            Transform categoryList = FindNamed(scene, "CategoryScrollView");
            if (categoryList == null)
                categoryList = FindNamed(scene, "GameList");

            if (safeArea == null || categoryList == null)
                return false;

            Transform categoryContent = categoryList.Find("Viewport/Content");
            if (categoryContent == null)
                return false;

            bool changed = false;
            if (categoryList.name != "CategoryScrollView")
            {
                categoryList.name = "CategoryScrollView";
                changed = true;
            }

            GameCardUI[] placed = categoryContent.GetComponentsInChildren<GameCardUI>(true);
            for (int i = 0; i < placed.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(placed[i].gameObject);
                changed = true;
            }

            Transform bar = FindNamed(scene, "CategoryBar");
            if (bar != null)
            {
                UnityEngine.Object.DestroyImmediate(bar.gameObject);
                changed = true;
            }

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var rect = canvas.GetComponent<RectTransform>();
                if (rect != null && (rect.localScale == Vector3.zero || rect.anchorMax != Vector2.one))
                {
                    Stretch(rect);
                    changed = true;
                }
            }

            if (canvas == null)
                return changed;

            if (canvas.GetComponent<MainMenuManager>() == null)
                canvas.gameObject.AddComponent<MainMenuManager>();

            CategoryCardUI categoryPrefab = EnsureCategoryCardPrefab(null);
            if (WireMenu(canvas.gameObject, categoryContent as RectTransform, categoryPrefab))
                changed = true;

            return changed;
        }

        static bool WireMenu(GameObject menuHost, RectTransform categoryContent, CategoryCardUI categoryCardPrefab)
        {
            if (menuHost == null)
                return false;

            var menu = menuHost.GetComponent<MainMenuManager>();
            if (menu == null)
                menu = menuHost.AddComponent<MainMenuManager>();

            var database = AssetDatabase.LoadAssetAtPath<GameContentDatabase>(DatabasePath);
            if (categoryCardPrefab == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CategoryCardPrefabPath);
                if (prefab != null)
                    categoryCardPrefab = prefab.GetComponent<CategoryCardUI>();
            }

            var serialized = new SerializedObject(menu);
            bool changed = Assign(serialized.FindProperty("database"), database);
            changed |= Assign(serialized.FindProperty("categoryCardPrefab"), categoryCardPrefab);
            changed |= Assign(serialized.FindProperty("categoryContent"), categoryContent);
            if (changed)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menu);
            }

            return changed;
        }

        static bool Assign(SerializedProperty property, UnityEngine.Object value)
        {
            if (property == null)
                return false;

            if (property.objectReferenceValue == value)
                return false;

            property.objectReferenceValue = value;
            return true;
        }

        static void RefreshDatabaseLists()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameContentDatabase>(DatabasePath);
            if (database == null && File.Exists(DatabasePath))
            {
                AssetDatabase.ImportAsset(DatabasePath);
                database = AssetDatabase.LoadAssetAtPath<GameContentDatabase>(DatabasePath);
            }

            if (database == null)
            {
                database = ScriptableObject.CreateInstance<GameContentDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            string[] categoryGuids = AssetDatabase.FindAssets("t:CategoryData", new[] { CategoryFolder });
            string[] puzzleGuids = AssetDatabase.FindAssets("t:PuzzleData", new[] { PuzzleFolder });
            var categories = new List<CategoryData>();
            var puzzles = new List<PuzzleData>();

            for (int i = 0; i < categoryGuids.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<CategoryData>(AssetDatabase.GUIDToAssetPath(categoryGuids[i]));
                if (asset != null)
                    categories.Add(asset);
            }

            for (int i = 0; i < puzzleGuids.Length; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<PuzzleData>(AssetDatabase.GUIDToAssetPath(puzzleGuids[i]));
                if (asset != null)
                    puzzles.Add(asset);
            }

            categories.Sort((a, b) => a.displayOrder.CompareTo(b.displayOrder));
            puzzles.Sort((a, b) =>
            {
                int order = string.CompareOrdinal(a.categoryId, b.categoryId);
                return order != 0 ? order : a.displayOrder.CompareTo(b.displayOrder);
            });

            database.categories = categories.ToArray();
            database.puzzles = puzzles.ToArray();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        static void EnsureCategory(CategorySeed seed)
        {
            string path = CategoryFolder + "/" + seed.FileName + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<CategoryData>(path);
            if (asset == null && File.Exists(path))
            {
                AssetDatabase.ImportAsset(path);
                asset = AssetDatabase.LoadAssetAtPath<CategoryData>(path);
            }

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CategoryData>();
                asset.categoryId = seed.Id;
                asset.unlockProductId = seed.ProductId;
                asset.isEnabled = seed.Enabled;
                asset.displayName = seed.DisplayName;
                asset.description = seed.Description;
                asset.displayOrder = seed.Order;
                asset.puzzleCapacity = 100;
                AssetDatabase.CreateAsset(asset, path);
                return;
            }

            bool changed = false;
            if (asset.displayName != seed.DisplayName)
            {
                asset.displayName = seed.DisplayName;
                changed = true;
            }

            if (asset.description != seed.Description)
            {
                asset.description = seed.Description;
                changed = true;
            }

            if (asset.displayOrder != seed.Order)
            {
                asset.displayOrder = seed.Order;
                changed = true;
            }

            if (asset.puzzleCapacity <= 0)
            {
                asset.puzzleCapacity = 100;
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(asset);
        }

        static void EnsurePuzzle(PuzzleSeed seed)
        {
            string path = PuzzleFolder + "/" + seed.FileName + ".asset";
            if (File.Exists(path))
            {
                if (AssetDatabase.LoadAssetAtPath<PuzzleData>(path) == null)
                    AssetDatabase.ImportAsset(path);
                return;
            }

            var asset = ScriptableObject.CreateInstance<PuzzleData>();
            asset.puzzleId = seed.Id;
            asset.categoryId = seed.CategoryId;
            asset.title = seed.Title;
            asset.shortDescription = seed.Description;
            asset.displayOrder = seed.Order;
            asset.sceneName = seed.SceneName;
            asset.puzzleType = "";
            asset.isDefaultFree = seed.DefaultFree;
            asset.unlockOrder = seed.Order;
            asset.difficulty = PuzzleDifficulty.Medium;
            asset.isEnabled = true;
            AssetDatabase.CreateAsset(asset, path);
        }

        static Transform FindNamed(UnityEngine.SceneManagement.Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindChildRecursive(roots[i].transform, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        static CategorySeed[] CategorySeeds()
        {
            return new[]
            {
                new CategorySeed("CategoryData_Probability", "probability", "확률", "확률과 선택이 직관을 어떻게 속이는지 알아보세요.", 0, "probability_pack", true),
                new CategorySeed("CategoryData_Logic", "logic", "논리", "논리적으로 맞아 보이지만 예상 밖의 답이 나오는 문제", 1, "logic_pack", false),
                new CategorySeed("CategoryData_MathParadox", "math", "수학 역설", "수학적 직관을 뒤집는 문제", 2, "math_pack", false),
                new CategorySeed("CategoryData_Statistics", "statistics", "통계", "통계가 어떻게 잘못 해석될 수 있는지 알아보는 문제", 3, "statistics_pack", false),
                new CategorySeed("CategoryData_CognitiveBias", "bias", "인지 편향", "생각이 한쪽으로 기우는 지점을 보는 문제", 4, "bias_pack", false),
                new CategorySeed("CategoryData_DecisionMaking", "decision", "의사결정", "선택이 결과를 어떻게 바꾸는지 보는 문제", 5, "decision_pack", false),
                new CategorySeed("CategoryData_Infinity", "infinity", "무한과 집합", "무한과 집합이 직관과 다르게 움직이는 문제", 6, "infinity_pack", false),
                new CategorySeed("CategoryData_Science", "science", "과학", "상식과 다르게 보이는 과학 문제", 7, "science_pack", false)
            };
        }

        static PuzzleSeed[] PuzzleSeeds()
        {
            return new[]
            {
                new PuzzleSeed("PuzzleData_MontyHall", "monty-hall", "probability", "몬티홀 문제", "문을 바꾸면 정말 확률이 올라갈까?", 0, "MontyHallScene"),
                new PuzzleSeed("PuzzleData_BirthdayParadox", "birthday-paradox", "probability", "생일 역설", "23명만 모여도 같은 생일 확률이 50%를 넘는다?", 1, "BirthdayParadoxScene"),
                new PuzzleSeed("PuzzleData_BertrandBox", "bertrand-box", "probability", "버트런드 상자 역설", "금화를 꺼냈다면, 남은 동전도 금화일 확률은?", 2, "BertrandBoxScene"),
                new PuzzleSeed("PuzzleData_ThreePrisoners", "three-prisoners", "probability", "세 죄수 문제", "간수가 B의 처형을 말한 뒤, A가 사면될 확률은?", 4, "ThreePrisonersScene", false),
                new PuzzleSeed("PuzzleData_BoyGirlParadox", "boy-girl-paradox", "probability", "소년·소녀 역설", "적어도 한 명이 남자아이라면, 둘 다 남자일 확률은?", 5, "BoyGirlParadoxScene", false),
                new PuzzleSeed("PuzzleData_TwoEnvelopes", "two-envelopes", "probability", "두 봉투 역설", "다른 봉투로 바꾸는 것이 항상 유리할까?", 6, "TwoEnvelopesScene", false),
                new PuzzleSeed("PuzzleData_FalsePositive", "false-positive", "probability", "검사 정확도 함정", "양성 결과가 나왔다면, 실제로 질병이 있을 확률은?", 7, "FalsePositiveScene", false),
                new PuzzleSeed("PuzzleData_GamblersFallacy", "gamblers-fallacy", "probability", "도박사의 오류", "앞면이 다섯 번 연속이어도, 다음 뒷면 확률은?", 8, "GamblersFallacyScene", false),
                new PuzzleSeed("PuzzleData_TwoCardProblem", "two-card-problem", "probability", "카드 두 장 문제", "빨간 면을 봤다면, 반대쪽도 빨강일 확률은?", 9, "TwoCardProblemScene", false),
                new PuzzleSeed("PuzzleData_ThreeDoorChoice", "three-door-choice", "probability", "세 문 선택 확률", "진행자가 문을 열지 않을 때, 처음 고른 문의 확률은?", 10, "ThreeDoorChoiceScene", false)
            };
        }

        static CategoryCardUI EnsureCategoryCardPrefab(TMP_FontAsset font)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(CategoryCardPrefabPath);
            if (existing != null)
                return existing.GetComponent<CategoryCardUI>();

            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
                return null;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            GameObject root = BuildCategoryCardObject(font, sprite);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, CategoryCardPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(CategoryCardPrefabPath);
            if (saved == null)
            {
                Debug.LogError("[Mind Paradox] CategoryCard 프리팹을 저장하지 못했습니다.");
                return null;
            }

            return saved.GetComponent<CategoryCardUI>();
        }

        static GameObject BuildCategoryCardObject(TMP_FontAsset font, Sprite sprite)
        {
            var root = new GameObject("CategoryCard", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(984f, 456f);

            var image = root.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = CardColor;

            var button = root.AddComponent<Button>();
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

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 36, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var element = root.AddComponent<LayoutElement>();
            element.preferredHeight = 456f;
            element.minHeight = 440f;
            element.flexibleHeight = 0f;
            element.flexibleWidth = 1f;

            Image icon = CreateIcon(root.transform);
            TMP_Text title = CreateText(root.transform, "TitleText", "Category", 60f, FontStyles.Bold, CardTitleColor, TextAlignmentOptions.MidlineLeft, font, false);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 88f;
            titleElement.flexibleWidth = 1f;
            titleElement.flexibleHeight = 0f;

            TMP_Text description = CreateText(root.transform, "DescriptionText", "Description", 50f, FontStyles.Normal, CardBodyColor, TextAlignmentOptions.TopLeft, font, true);
            var descriptionElement = description.gameObject.AddComponent<LayoutElement>();
            descriptionElement.preferredHeight = 188f;
            descriptionElement.flexibleWidth = 1f;
            descriptionElement.flexibleHeight = 0f;

            RectTransform info = CreateUiObject("InfoRow", root.transform);
            var infoElement = info.gameObject.AddComponent<LayoutElement>();
            infoElement.preferredHeight = 80f;
            infoElement.flexibleWidth = 1f;
            infoElement.flexibleHeight = 0f;
            var infoLayout = info.gameObject.AddComponent<HorizontalLayoutGroup>();
            infoLayout.spacing = 16f;
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = false;
            infoLayout.childForceExpandHeight = false;

            TMP_Text progress = CreateText(info, "ProgressText", "0 / 100", 48f, FontStyles.Normal, CardBodyColor, TextAlignmentOptions.MidlineLeft, font, false);
            var progressElement = progress.gameObject.AddComponent<LayoutElement>();
            progressElement.flexibleWidth = 1f;
            progressElement.preferredHeight = 64f;
            progressElement.flexibleHeight = 0f;

            Image badge = CreateAccessBadge(info, font, sprite, out TMP_Text accessText);

            var card = root.AddComponent<CategoryCardUI>();
            var serialized = new SerializedObject(card);
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("descriptionText").objectReferenceValue = description;
            serialized.FindProperty("progressText").objectReferenceValue = progress;
            serialized.FindProperty("accessText").objectReferenceValue = accessText;
            serialized.FindProperty("iconImage").objectReferenceValue = icon;
            serialized.FindProperty("accessBadge").objectReferenceValue = badge;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static Image CreateIcon(Transform parent)
        {
            var iconObject = new GameObject("IconImage", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);
            var icon = iconObject.AddComponent<Image>();
            icon.color = new Color32(0xD5, 0xDC, 0xE8, 0xFF);
            icon.raycastTarget = false;
            var iconElement = iconObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = 72f;
            iconElement.preferredHeight = 72f;
            iconElement.flexibleWidth = 0f;
            iconElement.flexibleHeight = 0f;
            iconObject.SetActive(false);
            return icon;
        }

        static Image CreateAccessBadge(Transform parent, TMP_FontAsset font, Sprite sprite, out TMP_Text label)
        {
            var badgeObject = new GameObject("AccessBadge", typeof(RectTransform));
            badgeObject.transform.SetParent(parent, false);
            var badge = badgeObject.AddComponent<Image>();
            badge.sprite = sprite;
            badge.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            badge.color = ButtonColor;
            badge.raycastTarget = false;
            var badgeElement = badgeObject.AddComponent<LayoutElement>();
            badgeElement.preferredWidth = 440f;
            badgeElement.minWidth = 320f;
            badgeElement.preferredHeight = 72f;
            badgeElement.minHeight = 72f;
            badgeElement.flexibleWidth = 0f;
            badgeElement.flexibleHeight = 0f;

            label = CreateText(badgeObject.transform, "Label", "FREE", 48f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, false);
            label.margin = new Vector4(18f, 0f, 18f, 0f);
            Stretch(label.rectTransform);
            return badge;
        }

        static void EnsureCategoryScene()
        {
            if (!File.Exists(CategoryScenePath))
            {
                string returnPath = EditorSceneManager.GetActiveScene().path;
                if (EditorSceneManager.GetActiveScene().isDirty)
                    EditorSceneManager.SaveOpenScenes();

                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildCategoryScene(font, sprite);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), CategoryScenePath);
                FixSavedCanvasRect(CategoryScenePath);

                if (!string.IsNullOrEmpty(returnPath))
                    EditorSceneManager.OpenScene(returnPath, OpenSceneMode.Single);
            }

            AddCategorySceneToBuildSettings();
        }

        static void BuildCategoryScene(TMP_FontAsset font, Sprite sprite)
        {
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
            safeLayout.padding = new RectOffset(0, 0, 0, 0);
            safeLayout.spacing = 0f;
            safeLayout.childAlignment = TextAnchor.UpperCenter;
            safeLayout.childControlWidth = true;
            safeLayout.childControlHeight = true;
            safeLayout.childForceExpandWidth = true;
            safeLayout.childForceExpandHeight = false;

            RectTransform header = CreateUiObject("Header", safeArea);
            var headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.preferredHeight = 360f;
            headerElement.flexibleHeight = 0f;
            var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(64, 64, 72, 16);
            headerLayout.spacing = 12f;
            headerLayout.childAlignment = TextAnchor.UpperCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            TMP_Text title = CreateText(header, "TitleText", "Category", 60f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 88f;
            titleElement.flexibleHeight = 0f;

            TMP_Text subtitle = CreateText(header, "SubtitleText", "", 50f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            var subtitleElement = subtitle.gameObject.AddComponent<LayoutElement>();
            subtitleElement.preferredHeight = 148f;
            subtitleElement.flexibleHeight = 0f;

            RectTransform list = CreateUiObject("PuzzleScrollView", safeArea);
            var listElement = list.gameObject.AddComponent<LayoutElement>();
            listElement.flexibleHeight = 1f;
            listElement.flexibleWidth = 1f;
            listElement.minHeight = 480f;

            RectTransform viewport = CreateStretch("Viewport", list);
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateUiObject("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(48, 48, 8, 32);
            contentLayout.spacing = 28f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.scrollSensitivity = 40f;

            RectTransform footer = CreateUiObject("Footer", safeArea);
            var footerElement = footer.gameObject.AddComponent<LayoutElement>();
            footerElement.preferredHeight = 188f;
            footerElement.flexibleHeight = 0f;
            var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.padding = new RectOffset(48, 48, 12, 36);
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;

            Button backButton = CreateBackButton(footer, font, sprite);

            CreateEventSystem();
            Stretch(canvasObject.GetComponent<RectTransform>());

            var menuObject = new GameObject("CategoryMenu");
            var menu = menuObject.AddComponent<CategoryMenuUI>();
            var cardObject = AssetDatabase.LoadAssetAtPath<GameObject>(GameCardPrefabPath);
            var serialized = new SerializedObject(menu);
            serialized.FindProperty("database").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameContentDatabase>(DatabasePath);
            serialized.FindProperty("cardPrefab").objectReferenceValue = cardObject != null ? cardObject.GetComponent<GameCardUI>() : null;
            serialized.FindProperty("cardContent").objectReferenceValue = content;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("subtitleText").objectReferenceValue = subtitle;
            serialized.FindProperty("backButton").objectReferenceValue = backButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static Button CreateBackButton(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            var buttonObject = new GameObject("BackButton", typeof(RectTransform));
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
            element.preferredHeight = 120f;
            element.minHeight = 100f;
            element.flexibleHeight = 0f;
            element.flexibleWidth = 1f;

            TMP_Text label = CreateText(buttonObject.transform, "Label", "뒤로", 48f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, false);
            Stretch(label.rectTransform);
            return button;
        }

        static void AddCategorySceneToBuildSettings()
        {
            if (!File.Exists(CategoryScenePath))
                return;

            string guid = AssetDatabase.AssetPathToGUID(CategoryScenePath);
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            var list = new List<EditorBuildSettingsScene>();
            bool found = false;
            int insertAt = 0;
            bool sawMain = false;

            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == CategoryScenePath)
                {
                    found = true;
                    list.Add(existing[i]);
                    continue;
                }

                list.Add(existing[i]);
                if (existing[i].path == MainScenePath)
                {
                    insertAt = list.Count;
                    sawMain = true;
                }
            }

            if (!found)
            {
                if (!sawMain)
                    insertAt = list.Count;

                list.Insert(insertAt, new EditorBuildSettingsScene(CategoryScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
                AssetDatabase.SaveAssets();
            }

            PatchBuildSettingsFile(guid);
        }

        static void PatchBuildSettingsFile(string guid)
        {
            const string settingsPath = "ProjectSettings/EditorBuildSettings.asset";
            if (!File.Exists(settingsPath) || string.IsNullOrEmpty(guid))
                return;

            string text = File.ReadAllText(settingsPath);
            if (text.Contains("Assets/Scenes/CategoryScene.unity"))
                return;

            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            string block =
                "  - enabled: 1" + newline +
                "    path: Assets/Scenes/CategoryScene.unity" + newline +
                "    guid: " + guid + newline;

            const string marker = "path: Assets/Scenes/MainScene.unity";
            int markerIndex = text.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
                return;

            int guidLine = text.IndexOf("guid:", markerIndex, StringComparison.Ordinal);
            if (guidLine < 0)
                return;

            int lineEnd = text.IndexOf('\n', guidLine);
            if (lineEnd < 0)
                return;

            File.WriteAllText(settingsPath, text.Insert(lineEnd + 1, block));
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

        readonly struct CategorySeed
        {
            public readonly string FileName;
            public readonly string Id;
            public readonly string DisplayName;
            public readonly string Description;
            public readonly int Order;
            public readonly string ProductId;
            public readonly bool Enabled;

            public CategorySeed(string fileName, string id, string displayName, string description, int order, string productId, bool enabled)
            {
                FileName = fileName;
                Id = id;
                DisplayName = displayName;
                Description = description;
                Order = order;
                ProductId = productId;
                Enabled = enabled;
            }
        }

        readonly struct PuzzleSeed
        {
            public readonly string FileName;
            public readonly string Id;
            public readonly string CategoryId;
            public readonly string Title;
            public readonly string Description;
            public readonly int Order;
            public readonly string SceneName;
            public readonly bool DefaultFree;

            public PuzzleSeed(string fileName, string id, string categoryId, string title, string description, int order, string sceneName, bool defaultFree = true)
            {
                FileName = fileName;
                Id = id;
                CategoryId = categoryId;
                Title = title;
                Description = description;
                Order = order;
                SceneName = sceneName;
                DefaultFree = defaultFree;
            }
        }
    }

    public class GameContentAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!TouchesContent(imported) && !TouchesContent(deleted) && !TouchesContent(moved) && !TouchesContent(movedFrom))
                return;

            EditorApplication.delayCall += GameContentSetup.EnsureContent;
        }

        static bool TouchesContent(string[] paths)
        {
            if (paths == null)
                return false;

            for (int i = 0; i < paths.Length; i++)
            {
                if (string.IsNullOrEmpty(paths[i]))
                    continue;

                string normalized = paths[i].Replace('\\', '/');
                if (normalized.StartsWith("Assets/Data/Categories/", StringComparison.Ordinal) ||
                    normalized.StartsWith("Assets/Data/Puzzles/", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
