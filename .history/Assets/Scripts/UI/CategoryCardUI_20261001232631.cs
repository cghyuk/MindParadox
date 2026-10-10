using System;
using MindParadox.Core;
using MindParadox.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 메인 화면의 카테고리 카드. 제목은 한 줄, 설명은 최대 세 줄이다.
    /// </summary>
    public class CategoryCardUI : MonoBehaviour
    {
        static readonly Color FreeColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color PartialColor = new Color32(0x3E, 0x78, 0xF0, 0xFF);
        static readonly Color AdColor = new Color32(0x1E, 0x4C, 0xC0, 0xFF);
        static readonly Color OwnedColor = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color LockedColor = new Color32(0x9A, 0xA4, 0xB5, 0xFF);
        static readonly Color LockedLabelColor = new Color32(0x15, 0x20, 0x33, 0xFF);

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] TMP_Text accessText;
        [SerializeField] Image iconImage;
        [SerializeField] Image accessBadge;
        [SerializeField] Button button;

        public void Bind(CategoryData category, int puzzleCount, CategoryAccessState state, Action onClick)
        {
            if (category == null)
                return;

            if (titleText != null)
                titleText.text = category.displayName;

            if (descriptionText != null)
                descriptionText.text = category.description;

            if (progressText != null)
                progressText.text = FormatProgress(puzzleCount, category.Capacity);

            if (accessText != null)
            {
                accessText.text = Label(state);
                accessText.color = state == CategoryAccessState.Locked ? LockedLabelColor : Color.white;
            }

            if (accessBadge != null)
                accessBadge.color = BadgeColor(state);

            if (iconImage != null)
            {
                bool showIcon = category.icon != null;
                iconImage.gameObject.SetActive(showIcon);
                if (showIcon)
                    iconImage.sprite = category.icon;
            }

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            bool canOpen = state != CategoryAccessState.Locked && category.isEnabled;
            button.interactable = canOpen;
            if (canOpen && onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        static string FormatProgress(int puzzleCount, int capacity)
        {
            if (puzzleCount < 0)
                puzzleCount = 0;

            if (capacity < puzzleCount)
                capacity = puzzleCount;

            return puzzleCount + " / " + capacity;
        }

        static string Label(CategoryAccessState state)
        {
            switch (state)
            {
                case CategoryAccessState.Free:
                    return "FREE";
                case CategoryAccessState.Partial:
                    return "PARTIAL";
                case CategoryAccessState.AdAvailable:
                    return "AD";
                case CategoryAccessState.Purchased:
                    return "OWNED";
                default:
                    return "LOCKED";
            }
        }

        static Color BadgeColor(CategoryAccessState state)
        {
            switch (state)
            {
                case CategoryAccessState.Free:
                    return FreeColor;
                case CategoryAccessState.Partial:
                    return PartialColor;
                case CategoryAccessState.AdAvailable:
                    return AdColor;
                case CategoryAccessState.Purchased:
                    return OwnedColor;
                default:
                    return LockedColor;
            }
        }
    }
}
