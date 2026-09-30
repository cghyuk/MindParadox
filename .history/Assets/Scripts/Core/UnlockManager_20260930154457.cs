using MindParadox.Data;
using UnityEngine;

namespace MindParadox.Core
{
    /// <summary>
    /// 카테고리 단위 해금과 전체 해금만 기억한다. 상점 SDK는 연결하지 않는다.
    /// </summary>
    public static class UnlockManager
    {
        const string FullUnlockKey = "mindparadox.full_unlock";
        const string CategoryKeyPrefix = "mindparadox.category.";

        public static bool FullUnlock
        {
            get => PlayerPrefs.GetInt(FullUnlockKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FullUnlockKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static bool IsCategoryUnlocked(string categoryId)
        {
            if (FullUnlock)
                return true;

            if (string.IsNullOrEmpty(categoryId))
                return false;

            return PlayerPrefs.GetInt(CategoryKeyPrefix + categoryId, 0) == 1;
        }

        public static bool IsCategoryUnlocked(CategoryData category)
        {
            if (category == null)
                return false;

            if (FullUnlock)
                return true;

            if (string.IsNullOrEmpty(category.unlockProductId))
                return true;

            return IsCategoryUnlocked(category.categoryId);
        }

        public static void SetCategoryUnlocked(string categoryId, bool unlocked)
        {
            if (string.IsNullOrEmpty(categoryId))
                return;

            PlayerPrefs.SetInt(CategoryKeyPrefix + categoryId, unlocked ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool CanPlay(PuzzleData puzzle, CategoryData category)
        {
            if (puzzle == null || !puzzle.isEnabled)
                return false;

            if (category != null && !category.isEnabled)
                return false;

            if (puzzle.isDefaultFree || FullUnlock)
                return true;

            if (category != null && string.IsNullOrEmpty(category.unlockProductId))
                return true;

            return IsCategoryUnlocked(puzzle.categoryId);
        }
    }
}
