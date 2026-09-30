using System;
using System.Collections.Generic;
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
    /// 카테고리/문제 에셋을 만들고, 메인 메뉴가 그 목록으로 카드를 만들게 연결한다.
    /// </summary>
    [InitializeOnLoad]
    public static class GameContentSetup
    {
        const string MainScenePath = "Assets/Scenes/MainScene.unity";
        const string DatabasePath = "Assets/Data/GameContentDatabase.asset";
        const string CategoryFolder = "Assets/Data/Categories";
        const string PuzzleFolder = "Assets/Data/Puzzles";
        const string FontPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string PrefabPath = "Assets/Prefabs/GameCard.prefab";

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
            MigrateOpenOrSavedMainScene();
            Debug.Log("[Mind Paradox] 카테고리와 문제 목록을 갱신했습니다.");
        }

        static void TryEnsure()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryEnsure;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                attempts++;
                if (attempts < 40)
                    EditorApplication.delayCall += TryEnsure;
                return;
            }

            EnsureContent();
            MigrateOpenOrSavedMainScene();
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

        public static void AttachMenu(RectTransform safeArea, RectTransform gameList, RectTransform cardContent, GameObject menuHost, GameCardUI cardPrefab, TMP_FontAsset font)
        {
            EnsureContent();
            RectTransform categoryContent = EnsureCategoryBar(safeArea, gameList);
            WireMenu(menuHost, cardContent, categoryContent, cardPrefab, font);
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
            Transform safeArea = FindNamed(EditorSceneManager.GetActiveScene(), "SafeArea");
            Transform gameList = FindNamed(EditorSceneManager.GetActiveScene(), "GameList");
            if (safeArea == null || gameList == null)
                return false;

            Transform cardContent = gameList.Find("Viewport/Content");
            if (cardContent == null)
                return false;

            bool changed = false;
            GameCardUI[] placed = cardContent.GetComponentsInChildren<GameCardUI>(true);
            for (int i = 0; i < placed.Length; i++)
            {
                UnityEngine.Object.DestroyImmediate(placed[i].gameObject);
                changed = true;
            }

            Transform bar = FindNamed(EditorSceneManager.GetActiveScene(), "CategoryBar");
            if (bar == null)
            {
                EnsureCategoryBar(safeArea as RectTransform, gameList as RectTransform);
                changed = true;
            }

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                var rect = canvas.GetComponent<RectTransform>();
                if (rect != null && (rect.localScale == Vector3.zero || rect.anchorMax != Vector2.one))
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.localScale = Vector3.one;
                    changed = true;
                }
            }

            if (canvas == null)
                return changed;

            var menu = canvas.GetComponent<MainMenuManager>();
            if (menu == null)
                menu = canvas.gameObject.AddComponent<MainMenuManager>();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Transform categoryContent = FindNamed(EditorSceneManager.GetActiveScene(), "CategoryContent");
            if (WireMenu(canvas.gameObject, cardContent as RectTransform, categoryContent as RectTransform, prefab != null ? prefab.GetComponent<GameCardUI>() : null, font))
                changed = true;

            return changed;
        }

        static bool WireMenu(GameObject menuHost, RectTransform cardContent, RectTransform categoryContent, GameCardUI cardPrefab, TMP_FontAsset font)
        {
            if (menuHost == null)
                return false;

            var menu = menuHost.GetComponent<MainMenuManager>();
            if (menu == null)
                menu = menuHost.AddComponent<MainMenuManager>();

            var database = AssetDatabase.LoadAssetAtPath<GameContentDatabase>(DatabasePath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (cardPrefab == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab != null)
                    cardPrefab = prefab.GetComponent<GameCardUI>();
            }

            if (font == null)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var serialized = new SerializedObject(menu);
            bool changed = Assign(serialized.FindProperty("database"), database);
            changed |= Assign(serialized.FindProperty("cardPrefab"), cardPrefab);
            changed |= Assign(serialized.FindProperty("cardContent"), cardContent);
            changed |= Assign(serialized.FindProperty("categoryContent"), categoryContent);
            changed |= Assign(serialized.FindProperty("uiFont"), font);
            changed |= Assign(serialized.FindProperty("roundedSprite"), sprite);
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

        static RectTransform EnsureCategoryBar(RectTransform safeArea, RectTransform gameList)
        {
            Transform existing = safeArea.Find("CategoryBar");
            if (existing != null)
            {
                Transform content = existing.Find("Viewport/CategoryContent");
                return content as RectTransform;
            }

            var bar = new GameObject("CategoryBar", typeof(RectTransform));
            bar.transform.SetParent(safeArea, false);
            var barElement = bar.AddComponent<LayoutElement>();
            barElement.preferredHeight = 156f;
            barElement.minHeight = 140f;
            barElement.flexibleHeight = 0f;
            barElement.flexibleWidth = 1f;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform));
            viewportObject.transform.SetParent(bar.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            var viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0f);
            viewportImage.raycastTarget = true;
            viewportObject.AddComponent<RectMask2D>();

            var contentObject = new GameObject("CategoryContent", typeof(RectTransform));
            contentObject.transform.SetParent(viewport, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = contentObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 16, 16);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = bar.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.scrollSensitivity = 30f;

            if (gameList != null)
                bar.transform.SetSiblingIndex(gameList.GetSiblingIndex());

            return content;
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
            if (File.Exists(path))
            {
                if (AssetDatabase.LoadAssetAtPath<CategoryData>(path) == null)
                    AssetDatabase.ImportAsset(path);
                return;
            }

            var asset = ScriptableObject.CreateInstance<CategoryData>();
            asset.categoryId = seed.Id;
            asset.displayName = seed.DisplayName;
            asset.description = seed.Description;
            asset.displayOrder = seed.Order;
            asset.unlockProductId = seed.ProductId;
            asset.isEnabled = seed.Enabled;
            AssetDatabase.CreateAsset(asset, path);
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
            asset.isDefaultFree = true;
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
                new CategorySeed("CategoryData_Probability", "probability", "Probability", "확률을 직관과 다르게 만드는 문제", 0, "probability_pack", true),
                new CategorySeed("CategoryData_Logic", "logic", "Logic", "논리와 추론 문제", 1, "logic_pack", false),
                new CategorySeed("CategoryData_MathParadox", "math", "Math", "수학에서 생기는 역설", 2, "math_pack", false),
                new CategorySeed("CategoryData_Statistics", "statistics", "Statistics", "통계가 드러내는 착시", 3, "statistics_pack", false),
                new CategorySeed("CategoryData_CognitiveBias", "bias", "Bias", "판단이 치우치는 지점", 4, "bias_pack", false),
                new CategorySeed("CategoryData_DecisionMaking", "decision", "Decision", "선택과 결과의 문제", 5, "decision_pack", false),
                new CategorySeed("CategoryData_Infinity", "infinity", "Infinity", "무한과 집합이 만드는 역설", 6, "infinity_pack", false),
                new CategorySeed("CategoryData_Science", "science", "Science", "상식과 다른 과학 문제", 7, "science_pack", false)
            };
        }

        static PuzzleSeed[] PuzzleSeeds()
        {
            return new[]
            {
                new PuzzleSeed("PuzzleData_MontyHall", "monty-hall", "probability", "Monty Hall", "문을 바꾸면 정말 확률이 올라갈까?", 0, "MontyHallScene"),
                new PuzzleSeed("PuzzleData_BirthdayParadox", "birthday-paradox", "probability", "Birthday Paradox", "23명만 모여도 같은 생일 확률이 50%를 넘는다?", 1, "BirthdayParadox")
            };
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

            public PuzzleSeed(string fileName, string id, string categoryId, string title, string description, int order, string sceneName)
            {
                FileName = fileName;
                Id = id;
                CategoryId = categoryId;
                Title = title;
                Description = description;
                Order = order;
                SceneName = sceneName;
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
