using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 퍼즐 헤더의 뒤로 / 설명 버튼을 같은 크기로 맞춘다.
    /// 오른쪽의 모드 버튼은 건드리지 않는다.
    /// </summary>
    public class PuzzleHeaderBar : MonoBehaviour
    {
        public const float ButtonWidth = 300f;
        public const float ButtonHeight = 96f;
        public const float FontSize = 48f;
        public const float Spacing = 16f;
        public const float TextPadding = 16f;

        void Awake()
        {
            Apply(transform);
        }

        public static void Apply(Transform topRow)
        {
            if (topRow == null)
                return;

            var rowLayout = topRow.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
                rowLayout.spacing = Spacing;

            var rowElement = topRow.GetComponent<LayoutElement>();
            if (rowElement != null)
            {
                rowElement.preferredHeight = ButtonHeight;
                rowElement.minHeight = ButtonHeight;
                rowElement.flexibleHeight = 0f;
            }

            LayoutElement headerElement = topRow.parent != null ? topRow.parent.GetComponent<LayoutElement>() : null;
            if (headerElement != null && headerElement.preferredHeight > 0f && headerElement.preferredHeight < ButtonHeight + 76f)
                headerElement.preferredHeight = ButtonHeight + 80f;

            Transform back = topRow.Find("BackButton");
            Transform explanation = topRow.Find("ExplanationButton");
            if (back != null)
                back.SetSiblingIndex(0);
            if (explanation != null)
                explanation.SetSiblingIndex(back != null ? 1 : 0);

            Fit(back, UiText.BackLabel);
            Fit(explanation, UiText.ExplanationLabel);
        }

        static void Fit(Transform buttonTransform, string label)
        {
            if (buttonTransform == null)
                return;

            var element = buttonTransform.GetComponent<LayoutElement>();
            if (element == null)
                element = buttonTransform.gameObject.AddComponent<LayoutElement>();

            element.preferredWidth = ButtonWidth;
            element.minWidth = ButtonWidth;
            element.preferredHeight = ButtonHeight;
            element.minHeight = ButtonHeight;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;

            var rect = buttonTransform as RectTransform;
            if (rect != null)
                rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            TMP_Text text = buttonTransform.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
                return;

            text.text = label;
            text.fontSize = FontSize;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = new Vector4(TextPadding, 0f, TextPadding, 0f);
        }
    }
}
