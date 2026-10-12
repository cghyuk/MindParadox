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
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text progressText;
        [SerializeField] TMP_Text accessText;
        [SerializeField] Image iconImage;
        [SerializeField] Image accessBadge;
        [SerializeField] Image backgroundImage;
        [SerializeField] Image arrowImage;
        [SerializeField] Sprite activeBackground;
        [SerializeField] Sprite inactiveBackground;
        [SerializeField] Sprite activeBadgeSprite;
        [SerializeField] Sprite inactiveBadgeSprite;
        [SerializeField] Button button;

        public void Bind(CategoryData category, int puzzleCount, CategoryAccessState state, Action onClick, int completedCount = -1)
        {
            if (category == null)
                return;

            bool comingSoon = puzzleCount <= 0;

            if (titleText != null)
                titleText.text = category.displayName;

            if (descriptionText != null)
                descriptionText.text = category.description;

            if (progressText != null)
                progressText.text = FormatProgress(puzzleCount, completedCount);

            bool muted = comingSoon || state == CategoryAccessState.Locked;

            if (accessText != null)
            {
                accessText.text = AccessBadgeText(state, comingSoon);
                accessText.color = Color.white;
            }

            if (accessBadge != null)
            {
                Sprite badgeSprite = muted ? inactiveBadgeSprite : activeBadgeSprite;
                if (badgeSprite != null)
                    accessBadge.sprite = badgeSprite;
                accessBadge.color = Color.white;
            }

            if (backgroundImage != null)
            {
                Sprite panel = muted || !category.isEnabled ? inactiveBackground : activeBackground;
                if (panel != null)
                    backgroundImage.sprite = panel;
                backgroundImage.color = Color.white;
            }

            if (iconImage != null)
            {
                bool showIcon = category.icon != null;
                iconImage.gameObject.SetActive(showIcon);
                if (showIcon)
                {
                    iconImage.sprite = category.icon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                }
            }

            if (arrowImage != null)
            {
                Color arrowColor = arrowImage.color;
                arrowColor.a = muted || !category.isEnabled ? 0.35f : 1f;
                arrowImage.color = arrowColor;
            }

            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            bool canOpen = !comingSoon && state != CategoryAccessState.Locked && category.isEnabled;
            button.interactable = canOpen;
            if (canOpen && onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        static string FormatProgress(int puzzleCount, int completedCount)
        {
            if (puzzleCount <= 0)
                return "";

            if (completedCount >= 0)
                return completedCount + " / " + puzzleCount + " Completed";

            if (puzzleCount == 1)
                return "1 Puzzle";

            return puzzleCount + " Puzzles";
        }

        /// <summary>
        /// 배지 문구. 지금은 FREE, COMING SOON, PARTIAL, AD, OWNED, LOCKED 를 그대로 쓴다.
        /// 해금 수를 넘기면 PARTIAL 자리만 "4 / 10 UNLOCKED" 로 바꿀 수 있다.
        /// </summary>
        public static string AccessBadgeText(CategoryAccessState state, bool comingSoon, int unlockedCount = -1, int totalCount = -1)
        {
            if (comingSoon)
                return "COMING SOON";

            if (state == CategoryAccessState.Partial && unlockedCount >= 0 && totalCount > 0)
                return unlockedCount + " / " + totalCount + " UNLOCKED";

            return Label(state);
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

    }
}
