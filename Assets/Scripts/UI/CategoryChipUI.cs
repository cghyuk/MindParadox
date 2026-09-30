using System;
using MindParadox.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 카테고리 선택 칩. 데이터에서 런타임에 만든다.
    /// </summary>
    public class CategoryChipUI : MonoBehaviour
    {
        static readonly Color NormalColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color NormalTextColor = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color SelectedColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color DisabledColor = new Color32(0x9A, 0xA4, 0xB5, 0xFF);

        Image background;
        TMP_Text label;
        Button button;
        CategoryData category;

        public CategoryData Category => category;

        public void Build(CategoryData data, TMP_FontAsset font, Sprite sprite, Action<CategoryData> onSelected)
        {
            category = data;

            background = gameObject.AddComponent<Image>();
            background.sprite = sprite;
            background.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            background.color = NormalColor;

            button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.95f, 0.97f, 1f);
            colors.pressedColor = new Color(0.88f, 0.90f, 0.94f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var element = gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = 250f;
            element.preferredHeight = 112f;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 8f);
            labelRect.offsetMax = new Vector2(-12f, -8f);

            label = labelObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 32f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.richText = false;
            label.text = data != null && data.isEnabled
                ? data.displayName
                : (data != null ? data.displayName : "") + "\nCOMING SOON";

            if (data == null || !data.isEnabled)
            {
                button.interactable = false;
                SetSelected(false);
                return;
            }

            button.onClick.AddListener(() => onSelected?.Invoke(category));
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            bool enabled = category != null && category.isEnabled;
            if (!enabled)
            {
                if (background != null)
                    background.color = DisabledColor;
                if (label != null)
                    label.color = Color.white;
                return;
            }

            if (background != null)
                background.color = selected ? SelectedColor : NormalColor;
            if (label != null)
                label.color = selected ? Color.white : NormalTextColor;
        }
    }
}
