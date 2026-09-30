using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MindParadox.Core;
using MindParadox.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace MindParadox.EditorTools
{
    /// <summary>
    /// 빈 프로젝트에 폴더, Android 설정, Boot/Main 씬, GameCard 프리팹을 한 번 구성한다.
    /// </summary>
    [InitializeOnLoad]
    public static class MindParadoxProjectSetup
    {
        const string BootScenePath = "Assets/Scenes/BootScene.unity";
        const string MainScenePath = "Assets/Scenes/MainScene.unity";
        const string PrefabPath = "Assets/Prefabs/GameCard.prefab";
        const string PrimaryFontPath = "Assets/Fonts/BRLNSR.TTF";
        const string PrimaryFontAssetPath = "Assets/Fonts/BRLNSR SDF.asset";
        const string KoreanFontPath = "Assets/Fonts/NanumGothic-Regular.ttf";
        const string KoreanFontAssetPath = "Assets/Fonts/NanumGothic SDF.asset";
        const string JapaneseFontPath = "Assets/Fonts/NotoSansJP-Regular.otf";
        const string JapaneseFontAssetPath = "Assets/Fonts/NotoSansJP SDF.asset";
        const string TraditionalChineseFontPath = "Assets/Fonts/NotoSansTC-Regular.otf";
        const string TraditionalChineseFontAssetPath = "Assets/Fonts/NotoSansTC SDF.asset";
        const string SpritePath = "Assets/Sprites/UIRounded.png";
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        const string RetryKey = "MindParadox.SetupRetries";
        const string TmpImportKey = "MindParadox.TmpImportStarted";

        const string LatinGlyphs =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            " .,!?'\"-()[]{}:;/@#%&*+=<>_" +
            "ÁÀÂÃÄÅÆÇÉÈÊËÍÌÎÏÑÓÒÔÕÖØÚÙÛÜÝŸáàâãäåæçéèêëíìîïñóòôõöøúùûüýÿ¿¡ßŒœ";

        const string KoreanGlyphs = "직관을 뒤집는 확률과 논리 퍼즐문을 바꾸면 정말 확률이 올라갈까23명만 모여도 같은 생일 확률이 50%를 넘는다";
        const string JapaneseGlyphs = "あア日本語";
        const string TraditionalChineseGlyphs = "繁體中文";

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
        static readonly Color FooterColor = new Color32(0x8E, 0x9B, 0xB0, 0xFF);

        static bool isBuilding;

        static MindParadoxProjectSetup()
        {
            EditorApplication.delayCall += OnDelayCall;
        }

        [MenuItem("Mind Paradox/Rebuild Main Menu")]
        public static void RebuildFromMenu()
        {
            SessionState.SetInt(RetryKey, 0);
            try
            {
                EditorUtility.DisplayProgressBar("Mind Paradox", "메인 메뉴를 구성하는 중...", 0.3f);
                ApplyPlayerSettings();
                EnsureFolders();

                if (!File.Exists(TmpSettingsPath))
                {
                    ImportTmpEssentials();
                    Debug.Log("[Mind Paradox] TextMeshPro 리소스를 가져오는 중입니다. 가져오기가 끝난 뒤 메뉴를 한 번 더 실행해 주세요.");
                    return;
                }

                if (!PrepareSourceFonts())
                {
                    Debug.Log("[Mind Paradox] 폰트 임포트 설정을 갱신했습니다. 메뉴를 한 번 더 실행해 주세요.");
                    return;
                }

                BuildContent();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static void OnDelayCall()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OnDelayCall;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || isBuilding)
                return;

            try
            {
                ApplyPlayerSettings();
                EnsureFolders();

                if (HasGeneratedContent())
                {
                    FixOpenCanvasRect();
                    if (!IsFontSystemReady())
                    {
                        if (!SourceFontsImported())
                        {
                            AssetDatabase.Refresh();
                            ScheduleRetry();
                            return;
                        }

                        if (!PrepareSourceFonts())
                        {
                            ScheduleRetry();
                            return;
                        }

                        EnsureFontSystem();
                    }

                    SessionState.SetInt(RetryKey, 0);
                    return;
                }

                if (!File.Exists(TmpSettingsPath))
                {
                    if (!SessionState.GetBool(TmpImportKey, false))
                    {
                        SessionState.SetBool(TmpImportKey, true);
                        ImportTmpEssentials();
                    }

                    ScheduleRetry();
                    return;
                }

                if (!SourceFontsImported())
                {
                    AssetDatabase.Refresh();
                    ScheduleRetry();
                    return;
                }

                if (!PrepareSourceFonts())
                {
                    ScheduleRetry();
                    return;
                }

                BuildContent();
                SessionState.SetInt(RetryKey, 0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ScheduleRetry();
            }
        }

        static void ScheduleRetry()
        {
            int retries = SessionState.GetInt(RetryKey, 0) + 1;
            SessionState.SetInt(RetryKey, retries);
            if (retries > 90)
            {
                Debug.LogError("[Mind Paradox] 자동 구성을 마치지 못했습니다. 메뉴 Mind Paradox/Rebuild Main Menu 를 실행해 주세요.");
                return;
            }

            double resumeAt = EditorApplication.timeSinceStartup + 1.0d;
            EditorApplication.update += WaitThenRetry;

            void WaitThenRetry()
            {
                if (EditorApplication.timeSinceStartup < resumeAt)
                    return;

                EditorApplication.update -= WaitThenRetry;
                OnDelayCall();
            }
        }

        static bool HasGeneratedContent()
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null
                && AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        }

        static void FixOpenCanvasRect()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != MainScenePath)
                return;

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var rect = canvas.GetComponent<RectTransform>();
            bool broken = rect.localScale == Vector3.zero || rect.anchorMax != Vector2.one;
            if (!broken)
                return;

            Stretch(rect);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void ImportTmpEssentials()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);

            if (packageInfo == null)
            {
                Debug.LogError("[Mind Paradox] com.unity.ugui 패키지를 찾지 못했습니다.");
                return;
            }

            string packageFile = Path.Combine(packageInfo.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (!File.Exists(packageFile))
            {
                Debug.LogError("[Mind Paradox] TMP Essential Resources를 찾지 못했습니다.");
                return;
            }

            Debug.Log("[Mind Paradox] TextMeshPro Essential Resources를 가져옵니다.");
            AssetDatabase.ImportPackage(packageFile, false);
        }

        static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Scenes",
                "Assets/Scripts",
                "Assets/Scripts/Core",
                "Assets/Scripts/UI",
                "Assets/Scripts/Games",
                "Assets/Scripts/Games/MontyHall",
                "Assets/Scripts/Games/BirthdayParadox",
                "Assets/Scripts/Editor",
                "Assets/Prefabs",
                "Assets/Sprites",
                "Assets/Fonts",
                "Assets/Audio",
                "Assets/Materials"
            };

            for (int i = 0; i < folders.Length; i++)
            {
                if (AssetDatabase.IsValidFolder(folders[i]))
                    continue;

                Directory.CreateDirectory(folders[i]);
                AssetDatabase.ImportAsset(folders[i]);
            }
        }

        static void ApplyPlayerSettings()
        {
            const string productName = "Mind Paradox";
            const string applicationId = "com.cowdragon.mindparadox";

            if (PlayerSettings.productName != productName)
                PlayerSettings.productName = productName;

            if (PlayerSettings.companyName != "cowdragon")
                PlayerSettings.companyName = "cowdragon";

            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != applicationId)
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationId);

            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            if (!PlayerSettings.allowedAutorotateToPortrait)
                PlayerSettings.allowedAutorotateToPortrait = true;

            if (PlayerSettings.allowedAutorotateToPortraitUpsideDown)
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            if (PlayerSettings.allowedAutorotateToLandscapeLeft)
                PlayerSettings.allowedAutorotateToLandscapeLeft = false;

            if (PlayerSettings.allowedAutorotateToLandscapeRight)
                PlayerSettings.allowedAutorotateToLandscapeRight = false;

            if (!PlayerSettings.Android.renderOutsideSafeArea)
                PlayerSettings.Android.renderOutsideSafeArea = true;
        }

        static void BuildContent()
        {
            if (isBuilding)
                return;

            isBuilding = true;
            try
            {
                EditorUtility.DisplayProgressBar("Mind Paradox", "폰트와 UI를 만드는 중...", 0.55f);

                TMP_FontAsset fontAsset = EnsureFontAsset();
                Sprite roundedSprite = EnsureRoundedSprite();

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject prefab = CreateCardPrefab(fontAsset, roundedSprite);

                CreateMainScene(prefab, fontAsset);
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MainScenePath);

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                CreateBootScene();
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), BootScenePath);

                EnsureBuildScenes();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
                Debug.Log("[Mind Paradox] 기본 구조와 메인 메뉴를 만들었습니다. BootScene을 재생하면 MainScene으로 이동합니다.");
            }
            finally
            {
                isBuilding = false;
                EditorUtility.ClearProgressBar();
            }
        }

        static void EnsureBuildScenes()
        {
            var result = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(BootScenePath, true),
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == BootScenePath || existing[i].path == MainScenePath)
                    continue;

                result.Add(existing[i]);
            }

            EditorBuildSettings.scenes = result.ToArray();
        }

        static TMP_FontAsset EnsureFontAsset()
        {
            return EnsureFontSystem();
        }

        [MenuItem("Mind Paradox/Apply Font System")]
        public static void ApplyFontSystemFromMenu()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Mind Paradox", "폰트를 적용하는 중...", 0.4f);
                if (!SourceFontsImported())
                    AssetDatabase.Refresh();

                if (!PrepareSourceFonts())
                {
                    Debug.Log("[Mind Paradox] 폰트 임포트 설정을 갱신했습니다. 메뉴를 한 번 더 실행해 주세요.");
                    return;
                }

                EnsureFontSystem();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        static bool IsFontSystemReady()
        {
            TMP_FontAsset primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryFontAssetPath);
            if (primary == null || primary.fallbackFontAssetTable == null || primary.fallbackFontAssetTable.Count < 3)
                return false;

            if (TMP_Settings.instance == null || TMP_Settings.defaultFontAsset != primary)
                return false;

            string koreanGuid = AssetDatabase.AssetPathToGUID(KoreanFontAssetPath);
            if (string.IsNullOrEmpty(koreanGuid))
                return false;

            string fontReference = "m_fontAsset: {fileID: 11400000, guid: " + koreanGuid;
            if (File.Exists(MainScenePath) && File.ReadAllText(MainScenePath).Contains(fontReference))
                return false;

            if (File.Exists(PrefabPath) && File.ReadAllText(PrefabPath).Contains(fontReference))
                return false;

            return true;
        }

        static TMP_FontAsset EnsureFontSystem()
        {
            FontEngine.InitializeFontEngine();

            TMP_FontAsset primary = CreateDynamicFontAsset(PrimaryFontPath, PrimaryFontAssetPath, "BRLNSR SDF");
            TMP_FontAsset korean = CreateDynamicFontAsset(KoreanFontPath, KoreanFontAssetPath, "NanumGothic SDF");
            TMP_FontAsset japanese = CreateDynamicFontAsset(JapaneseFontPath, JapaneseFontAssetPath, "NotoSansJP SDF");
            TMP_FontAsset traditionalChinese = CreateDynamicFontAsset(TraditionalChineseFontPath, TraditionalChineseFontAssetPath, "NotoSansTC SDF");

            AddCharacters(primary, LatinGlyphs);
            AddCharacters(korean, KoreanGlyphs);
            AddCharacters(japanese, JapaneseGlyphs);
            AddCharacters(traditionalChinese, TraditionalChineseGlyphs);

            primary.fallbackFontAssetTable = new List<TMP_FontAsset> { korean, japanese, traditionalChinese };
            EditorUtility.SetDirty(primary);

            if (TMP_Settings.instance != null)
            {
                TMP_Settings.defaultFontAsset = primary;
                TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset> { korean, japanese, traditionalChinese };
                EditorUtility.SetDirty(TMP_Settings.instance);
            }

            ApplyFontToPrefab(primary);
            ApplyFontToScene(primary);
            AssetDatabase.SaveAssets();
            Debug.Log("[Mind Paradox] 기본 폰트를 BRLNSR SDF로 바꿨습니다. 한국어, 일본어, 중국어 번체는 Fallback 폰트로 표시됩니다.");
            return primary;
        }

        static bool SourceFontsImported()
        {
            return AssetDatabase.LoadAssetAtPath<Font>(PrimaryFontPath) != null
                && AssetDatabase.LoadAssetAtPath<Font>(KoreanFontPath) != null
                && AssetDatabase.LoadAssetAtPath<Font>(JapaneseFontPath) != null
                && AssetDatabase.LoadAssetAtPath<Font>(TraditionalChineseFontPath) != null;
        }

        static bool PrepareSourceFonts()
        {
            string[] paths =
            {
                PrimaryFontPath,
                KoreanFontPath,
                JapaneseFontPath,
                TraditionalChineseFontPath
            };

            bool ready = true;
            for (int i = 0; i < paths.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(paths[i]) as TrueTypeFontImporter;
                if (importer == null)
                    return false;

                if (!importer.includeFontData)
                {
                    importer.includeFontData = true;
                    importer.SaveAndReimport();
                    ready = false;
                }
            }

            return ready;
        }

        static TMP_FontAsset CreateDynamicFontAsset(string fontPath, string assetPath, string assetName)
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
                throw new InvalidOperationException("폰트 파일을 찾지 못했습니다: " + fontPath);

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null || fontAsset.material == null || fontAsset.material.shader == null)
                throw new InvalidOperationException("TMP 폰트 에셋을 만들지 못했습니다: " + fontPath);

            SetSourceFontGuid(fontAsset, fontPath);
            fontAsset.name = assetName;
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);

            if (fontAsset.material != null)
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        static void AddCharacters(TMP_FontAsset fontAsset, string characters)
        {
            if (fontAsset == null || string.IsNullOrEmpty(characters))
                return;

            fontAsset.TryAddCharacters(characters, out string missing);
            if (!string.IsNullOrEmpty(missing))
                Debug.LogWarning("[Mind Paradox] " + fontAsset.name + "에 없는 문자: " + missing);

            EditorUtility.SetDirty(fontAsset);
        }

        static void ApplyFontToPrefab(TMP_FontAsset font)
        {
            if (!File.Exists(PrefabPath))
                return;

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].font = font;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void ApplyFontToScene(TMP_FontAsset font)
        {
            if (!File.Exists(MainScenePath))
                return;

            Scene scene = default;
            bool alreadyLoaded = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.path == MainScenePath)
                {
                    scene = loaded;
                    alreadyLoaded = true;
                    break;
                }
            }

            if (!alreadyLoaded)
                scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                TMP_Text[] texts = roots[i].GetComponentsInChildren<TMP_Text>(true);
                for (int t = 0; t < texts.Length; t++)
                    texts[t].font = font;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!alreadyLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        static void SetSourceFontGuid(TMP_FontAsset fontAsset, string fontPath)
        {
            FieldInfo field = typeof(TMP_FontAsset).GetField(
                "m_SourceFontFileGUID",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field != null)
                field.SetValue(fontAsset, AssetDatabase.AssetPathToGUID(fontPath));
        }

        static Sprite EnsureRoundedSprite()
        {
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (existing != null)
                return existing;

            const int size = 64;
            const int radius = 20;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = RoundedRectAlpha(x, y, size, radius);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(SpritePath);
            var spriteImporter = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            spriteImporter.textureType = TextureImporterType.Sprite;
            spriteImporter.spriteImportMode = SpriteImportMode.Single;
            spriteImporter.mipmapEnabled = false;
            spriteImporter.alphaIsTransparency = true;
            spriteImporter.spritePixelsPerUnit = size;
            spriteImporter.wrapMode = TextureWrapMode.Clamp;
            spriteImporter.filterMode = FilterMode.Bilinear;
            spriteImporter.textureCompression = TextureImporterCompression.Uncompressed;
            spriteImporter.spriteBorder = new Vector4(radius, radius, radius, radius);

            var settings = new TextureImporterSettings();
            spriteImporter.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            spriteImporter.SetTextureSettings(settings);
            spriteImporter.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        static float RoundedRectAlpha(int x, int y, int size, int radius)
        {
            float px = x + 0.5f;
            float py = y + 0.5f;
            float min = radius;
            float max = size - radius;
            float dx = px - Mathf.Clamp(px, min, max);
            float dy = py - Mathf.Clamp(py, min, max);
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Clamp01(radius - distance + 0.5f);
        }

        static GameObject CreateCardPrefab(TMP_FontAsset font, Sprite sprite)
        {
            var root = new GameObject("GameCard", typeof(RectTransform));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0f, 510f);

            var background = root.AddComponent<Image>();
            background.sprite = sprite;
            background.type = Image.Type.Sliced;
            background.color = CardColor;
            background.raycastTarget = true;

            var layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 32, 24);
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var element = root.AddComponent<LayoutElement>();
            element.preferredHeight = 510f;
            element.minHeight = 500f;
            element.flexibleWidth = 1f;

            var card = root.AddComponent<GameCardUI>();

            Image icon = CreateIcon(root.transform, sprite);
            TMP_Text title = CreateText(root.transform, "TitleText", "Game Title", 54f, FontStyles.Bold, CardTitleColor, TextAlignmentOptions.TopLeft, font, false);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = 80f;
            titleElement.flexibleHeight = 0f;

            CreateGap(root.transform, "TitleDescriptionGap", 22f);

            TMP_Text description = CreateText(root.transform, "DescriptionText", "Description", 42f, FontStyles.Normal, CardBodyColor, TextAlignmentOptions.TopLeft, font, true);
            var descriptionElement = description.gameObject.AddComponent<LayoutElement>();
            descriptionElement.preferredHeight = 218f;
            descriptionElement.flexibleHeight = 0f;

            CreateGap(root.transform, "DescriptionButtonGap", 26f);

            Button button = CreatePlayButton(root.transform, font, sprite);

            var serializedCard = new SerializedObject(card);
            serializedCard.FindProperty("titleText").objectReferenceValue = title;
            serializedCard.FindProperty("descriptionText").objectReferenceValue = description;
            serializedCard.FindProperty("playButton").objectReferenceValue = button;
            serializedCard.FindProperty("iconImage").objectReferenceValue = icon;
            serializedCard.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        static void CreateGap(Transform parent, string name, float height)
        {
            var gap = new GameObject(name, typeof(RectTransform));
            gap.transform.SetParent(parent, false);
            var element = gap.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
            element.flexibleWidth = 1f;
        }

        static Image CreateIcon(Transform parent, Sprite sprite)
        {
            var iconObject = new GameObject("IconImage", typeof(RectTransform));
            iconObject.transform.SetParent(parent, false);
            var image = iconObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color32(0x15, 0x20, 0x33, 0x18);
            image.raycastTarget = false;

            var element = iconObject.AddComponent<LayoutElement>();
            element.preferredWidth = 72f;
            element.preferredHeight = 72f;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
            iconObject.SetActive(false);
            return image;
        }

        static Button CreatePlayButton(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            var buttonObject = new GameObject("PlayButton", typeof(RectTransform));
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
            element.flexibleHeight = 0f;
            element.flexibleWidth = 1f;

            TMP_Text label = CreateText(buttonObject.transform, "Label", "PLAY", 42f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center, font, false);
            Stretch(label.rectTransform);
            return button;
        }

        static void CreateMainScene(GameObject cardPrefab, TMP_FontAsset font)
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
            headerElement.preferredHeight = 344f;
            headerElement.flexibleHeight = 0f;
            var headerLayout = header.gameObject.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset(64, 64, 72, 16);
            headerLayout.spacing = 16f;
            headerLayout.childAlignment = TextAnchor.UpperCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            TMP_Text headerTitle = CreateText(header, "TitleText", "Mind Paradox", 88f, FontStyles.Bold, TitleColor, TextAlignmentOptions.Center, font, false);
            var headerTitleElement = headerTitle.gameObject.AddComponent<LayoutElement>();
            headerTitleElement.preferredHeight = 116f;

            TMP_Text subtitle = CreateText(header, "SubtitleText", "직관을 뒤집는 확률과 논리 퍼즐", 42f, FontStyles.Normal, SubtitleColor, TextAlignmentOptions.Center, font, true);
            var subtitleElement = subtitle.gameObject.AddComponent<LayoutElement>();
            subtitleElement.preferredHeight = 124f;

            RectTransform gameList = CreateUiObject("GameList", safeArea);
            var gameListElement = gameList.gameObject.AddComponent<LayoutElement>();
            gameListElement.flexibleHeight = 1f;
            gameListElement.flexibleWidth = 1f;
            gameListElement.minHeight = 480f;

            RectTransform viewport = CreateStretch("Viewport", gameList);
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

            var scroll = gameList.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.inertia = true;
            scroll.scrollSensitivity = 40f;

            GameContentSetup.AttachMenu(safeArea, gameList, content, canvasObject, cardPrefab.GetComponent<GameCardUI>(), font);

            RectTransform footer = CreateUiObject("Footer", safeArea);
            var footerElement = footer.gameObject.AddComponent<LayoutElement>();
            footerElement.preferredHeight = 140f;
            footerElement.flexibleHeight = 0f;
            var footerLayout = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            footerLayout.padding = new RectOffset(40, 40, 12, 36);
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;

            TMP_Text footerText = CreateText(footer, "FooterText", "More paradoxes coming soon", 34f, FontStyles.Normal, FooterColor, TextAlignmentOptions.Center, font, false);
            var footerTextElement = footerText.gameObject.AddComponent<LayoutElement>();
            footerTextElement.preferredHeight = 64f;

            CreateEventSystem();
            Stretch(canvasObject.GetComponent<RectTransform>());
        }

        static void CreateBootScene()
        {
            CreateCamera();
            var bootstrap = new GameObject("Bootstrap");
            bootstrap.AddComponent<AppBootstrap>();
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
