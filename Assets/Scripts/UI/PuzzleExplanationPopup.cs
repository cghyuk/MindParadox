using MindParadox.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 모든 퍼즐이 같이 쓰는 설명 팝업. 열리고 닫혀도 게임 진행은 바꾸지 않는다.
    /// </summary>
    public class PuzzleExplanationPopup : MonoBehaviour
    {
        static readonly Color SectionColor = new Color32(0x15, 0x20, 0x33, 0xFF);

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text explanationText;
        [SerializeField] TMP_Text formulaText;
        [SerializeField] Button closeButton;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform contentRoot;
        const string FormulaSplit = "\n---\n";

        TMP_Text whyHeading;
        TMP_Text exampleHeading;
        TMP_Text exampleText;
        TMP_Text formulaHeading;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            EnsureSections();
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        void Update()
        {
            if (WasBackPressed())
                Hide();
        }

        public void Show(ExplanationData data)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (data == null)
            {
                Set(titleText, "설명");
                Set(summaryText, "");
                Set(explanationText, "이 문제의 설명이 아직 연결되지 않았습니다.");
                ShowFormula("");
            }
            else
            {
                Set(titleText, data.ResolvedTitle);
                Set(summaryText, data.ResolvedSummary);
                Set(explanationText, data.ResolvedBody);
                ShowFormula(data.ResolvedFormula);
            }

            bool hasWhy = explanationText != null && !string.IsNullOrEmpty(explanationText.text);
            if (whyHeading != null)
                whyHeading.gameObject.SetActive(hasWhy);

            if (contentRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void EnsureSections()
        {
            if (contentRoot != null)
            {
                VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 48f;
                    layout.padding = new RectOffset(8, 8, 12, 36);
                }
            }

            if (explanationText != null)
                whyHeading = EnsureHeading(explanationText.transform, "WhyHeading", "Why?");

            if (formulaText != null)
            {
                exampleHeading = EnsureHeading(formulaText.transform, "ExampleHeading", "Example");
                exampleText = EnsureBody(formulaText.transform, "ExampleText");
                formulaHeading = EnsureHeading(formulaText.transform, "FormulaHeading", "Formula / Key Point");
                StyleKeyPoint(formulaText);
                StyleKeyPoint(exampleText);
            }
        }

        void ShowFormula(string value)
        {
            string formula = value ?? "";
            string example = "";
            int split = formula.IndexOf(FormulaSplit, System.StringComparison.Ordinal);
            if (split >= 0)
            {
                example = formula.Substring(0, split).Trim();
                formula = formula.Substring(split + FormulaSplit.Length).Trim();
            }

            bool hasExample = example.Length > 0;
            bool hasFormula = formula.Length > 0;
            if (exampleHeading != null)
                exampleHeading.gameObject.SetActive(hasExample);
            if (exampleText != null)
            {
                exampleText.gameObject.SetActive(hasExample);
                exampleText.text = example;
            }

            if (formulaHeading != null)
                formulaHeading.gameObject.SetActive(hasFormula);
            Set(formulaText, formula);
            if (formulaText != null)
                formulaText.gameObject.SetActive(hasFormula);
        }

        static void StyleKeyPoint(TMP_Text text)
        {
            if (text == null)
                return;

            text.fontSize = 60f;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.color = SectionColor;
        }

        TMP_Text EnsureBody(Transform before, string objectName)
        {
            Transform parent = before.parent;
            Transform existing = parent.Find(objectName);
            if (existing != null)
                return existing.GetComponent<TMP_Text>();

            var bodyObject = new GameObject(objectName, typeof(RectTransform));
            bodyObject.transform.SetParent(parent, false);
            bodyObject.transform.SetSiblingIndex(before.GetSiblingIndex());
            TMP_Text body = bodyObject.AddComponent<TextMeshProUGUI>();
            if (formulaText != null && formulaText.font != null)
                body.font = formulaText.font;
            body.raycastTarget = false;
            var element = bodyObject.AddComponent<LayoutElement>();
            element.preferredHeight = -1f;
            element.flexibleHeight = 0f;
            var fitter = bodyObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return body;
        }

        TMP_Text EnsureHeading(Transform before, string objectName, string label)
        {
            Transform parent = before.parent;
            Transform existing = parent.Find(objectName);
            TMP_Text heading;
            if (existing != null)
            {
                heading = existing.GetComponent<TMP_Text>();
            }
            else
            {
                var headingObject = new GameObject(objectName, typeof(RectTransform));
                headingObject.transform.SetParent(parent, false);
                heading = headingObject.AddComponent<TextMeshProUGUI>();
                if (titleText != null && titleText.font != null)
                    heading.font = titleText.font;
                heading.raycastTarget = false;
                var element = headingObject.AddComponent<LayoutElement>();
                element.preferredHeight = 64f;
                element.minHeight = 64f;
                element.flexibleHeight = 0f;
            }

            heading.transform.SetSiblingIndex(before.GetSiblingIndex());
            heading.text = label;
            heading.fontSize = 50f;
            heading.enableAutoSizing = false;
            heading.fontStyle = FontStyles.Bold;
            heading.alignment = TextAlignmentOptions.TopLeft;
            heading.color = SectionColor;
            heading.textWrappingMode = TextWrappingModes.Normal;
            heading.overflowMode = TextOverflowModes.Overflow;
            return heading;
        }

        static void Set(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static bool WasBackPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }
    }
}
