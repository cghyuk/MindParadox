using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MindParadox.Data;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MindParadox.EditorTools
{
    /// <summary>
    /// 화면에 나오는 한국어를 모아 NanumGothic에 없는 글자만 확인한다.
    /// </summary>
    public static class KoreanGlyphChecker
    {
        const string KoreanFontPath = "Assets/Fonts/NanumGothic SDF.asset";
        const int AddChunkSize = 24;

        static readonly Regex UnicodeEscape = new Regex(@"\\u([0-9A-Fa-f]{4})", RegexOptions.Compiled);

        public readonly struct Report
        {
            public readonly int StringCount;
            public readonly int UniqueHangul;
            public readonly string Missing;

            public Report(int stringCount, int uniqueHangul, string missing)
            {
                StringCount = stringCount;
                UniqueHangul = uniqueHangul;
                Missing = missing ?? "";
            }
        }

        [MenuItem("Mind Paradox/Check Korean Glyphs")]
        public static void CheckFromMenu()
        {
            Report report = Scan();
            Debug.Log(
                "[Mind Paradox] 한국어 글리프 검사: 문자열 " + report.StringCount
                + ", 고유 한글 " + report.UniqueHangul
                + ", 누락 " + report.Missing.Length
                + (report.Missing.Length == 0 ? "" : " (" + report.Missing + ")"));
        }

        [MenuItem("Mind Paradox/Add Missing Korean Glyphs")]
        public static void AddFromMenu()
        {
            int added = AddMissing();
            Report report = Scan();
            Debug.Log(
                "[Mind Paradox] 한국어 글리프 추가: " + added
                + "자, 남은 누락 " + report.Missing.Length
                + (report.Missing.Length == 0 ? "" : " (" + report.Missing + ")"));
        }

        internal static Report Scan()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            var texts = new HashSet<string>();
            CollectData(texts);
            CollectSceneText(texts);
            CollectCode(texts);

            var hangul = new HashSet<char>();
            var missing = new StringBuilder();
            var missingSeen = new HashSet<char>();
            foreach (string text in texts)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char character = text[i];
                    if (!IsHangul(character) || !hangul.Add(character))
                        continue;

                    if (font != null && font.HasCharacter(character, false, false))
                        continue;

                    if (missingSeen.Add(character))
                        missing.Append(character);
                }
            }

            return new Report(texts.Count, hangul.Count, missing.ToString());
        }

        internal static int AddMissing()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
            if (font == null)
                return 0;

            string missing = Scan().Missing;
            if (missing.Length == 0)
                return 0;

            int before = font.characterTable.Count;
            for (int index = 0; index < missing.Length; index += AddChunkSize)
            {
                int length = Mathf.Min(AddChunkSize, missing.Length - index);
                PuzzleExplanationSetup.AddMissingGlyphs(
                    font,
                    missing.Substring(index, length),
                    "화면 문구 중 폰트에 없는 문자: ");
            }

            AssetDatabase.SaveAssetIfDirty(font);
            return font.characterTable.Count - before;
        }

        static void CollectData(HashSet<string> texts)
        {
            CollectAssets<CategoryData>("t:CategoryData", data =>
            {
                AddText(texts, data.displayName);
                AddText(texts, data.description);
                if (string.IsNullOrEmpty(data.displayName))
                    AddText(texts, data.displayNameKey);
                if (string.IsNullOrEmpty(data.description))
                    AddText(texts, data.descriptionKey);
            });

            CollectAssets<PuzzleData>("t:PuzzleData", data =>
            {
                AddText(texts, data.title);
                AddText(texts, data.shortDescription);
                if (string.IsNullOrEmpty(data.title))
                    AddText(texts, data.titleKey);
                if (string.IsNullOrEmpty(data.shortDescription))
                    AddText(texts, data.shortDescriptionKey);
            });

            CollectAssets<ExplanationData>("t:ExplanationData", data =>
            {
                AddText(texts, data.explanationTitle);
                AddText(texts, data.explanationSummary);
                AddText(texts, data.explanationBody);
                AddText(texts, data.explanationFormula);
                if (string.IsNullOrEmpty(data.explanationTitle))
                    AddText(texts, data.TitleLocalizationKey);
                if (string.IsNullOrEmpty(data.explanationSummary))
                    AddText(texts, data.SummaryLocalizationKey);
                if (string.IsNullOrEmpty(data.explanationBody))
                    AddText(texts, data.BodyLocalizationKey);
                if (string.IsNullOrEmpty(data.explanationFormula))
                    AddText(texts, data.FormulaLocalizationKey);
            });
        }

        static void CollectAssets<T>(string filter, System.Action<T> collect) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    collect(asset);
            }
        }

        static void CollectSceneText(HashSet<string> texts)
        {
            string[] folders =
            {
                Path.Combine(Application.dataPath, "Scenes"),
                Path.Combine(Application.dataPath, "Prefabs")
            };

            for (int folderIndex = 0; folderIndex < folders.Length; folderIndex++)
            {
                string folder = folders[folderIndex];
                if (!Directory.Exists(folder))
                    continue;

                string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
                for (int i = 0; i < files.Length; i++)
                {
                    string path = files[i];
                    if (!path.EndsWith(".unity") && !path.EndsWith(".prefab"))
                        continue;

                    string[] lines = File.ReadAllLines(path);
                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        string line = lines[lineIndex];
                        int marker = line.IndexOf("m_text:", System.StringComparison.Ordinal);
                        if (marker < 0)
                            continue;

                        string value = line.Substring(marker + "m_text:".Length).Trim();
                        if (value.Length == 0)
                            continue;

                        if (value.Length >= 2 && value[0] == '"')
                        {
                            int end = value.LastIndexOf('"');
                            if (end > 0)
                                value = value.Substring(1, end - 1);
                        }

                        AddText(texts, DecodeEscapes(value));
                    }
                }
            }
        }

        static void CollectCode(HashSet<string> texts)
        {
            string root = Path.Combine(Application.dataPath, "Scripts");
            if (!Directory.Exists(root))
                return;

            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string path = files[i].Replace('\\', '/');
                if (path.Contains("/Editor/"))
                    continue;

                CollectLiterals(File.ReadAllText(path), texts);
            }
        }

        static void CollectLiterals(string code, HashSet<string> texts)
        {
            var literal = new StringBuilder();
            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '/' && i + 1 < code.Length && code[i + 1] == '/')
                {
                    i += 2;
                    while (i < code.Length && code[i] != '\n')
                        i++;
                    continue;
                }

                if (code[i] == '/' && i + 1 < code.Length && code[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < code.Length && !(code[i] == '*' && code[i + 1] == '/'))
                        i++;
                    continue;
                }

                if (code[i] != '"')
                    continue;

                bool log = IsLogLiteral(code, i);
                bool verbatim = i > 0 && code[i - 1] == '@';
                i++;
                literal.Length = 0;
                while (i < code.Length)
                {
                    char character = code[i];
                    if (verbatim && character == '"' && i + 1 < code.Length && code[i + 1] == '"')
                    {
                        literal.Append('"');
                        i += 2;
                        continue;
                    }

                    if (character == '"')
                        break;

                    if (!verbatim && character == '\\' && i + 1 < code.Length)
                    {
                        literal.Append(Unescape(code[i + 1]));
                        i += 2;
                        continue;
                    }

                    literal.Append(character);
                    i++;
                }

                if (!log)
                    AddText(texts, literal.ToString());
            }
        }

        static bool IsLogLiteral(string code, int quoteIndex)
        {
            int start = Mathf.Max(0, quoteIndex - 80);
            string window = code.Substring(start, quoteIndex - start);
            return window.Contains("Debug.Log(")
                || window.Contains("Debug.LogWarning(")
                || window.Contains("Debug.LogError(");
        }

        static char Unescape(char escaped)
        {
            switch (escaped)
            {
                case 'n': return '\n';
                case 'r': return '\r';
                case 't': return '\t';
                default: return escaped;
            }
        }

        static string DecodeEscapes(string value)
        {
            return UnicodeEscape.Replace(value, match => ((char)System.Convert.ToInt32(match.Groups[1].Value, 16)).ToString());
        }

        static void AddText(HashSet<string> texts, string value)
        {
            if (string.IsNullOrEmpty(value) || !ContainsHangul(value))
                return;

            texts.Add(value);
        }

        static bool ContainsHangul(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (IsHangul(value[i]))
                    return true;
            }

            return false;
        }

        static bool IsHangul(char character)
        {
            return character >= '\uAC00' && character <= '\uD7A3';
        }
    }
}
