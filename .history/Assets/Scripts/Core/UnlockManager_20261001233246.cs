using MindParadox.Data;
using UnityEngine;

namespace MindParadox.Core
{
    /// <summary>
    /// 카테고리 카드에 보여줄 해금 상태. 광고와 결제는 아직 연결하지 않는다.
    /// </summary>
    public enum CategoryAccessState
    {
        Free,
        Partial,
        AdAvailable,
        Purchased,
        Locked
    }

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

        public static CategoryAccessState GetAccessState(CategoryData category, PuzzleData[] puzzles)
        {
            if (category == null || !category.isEnabled)
                return CategoryAccessState.Locked;

            if (FullUnlock || IsCategoryUnlocked(category.categoryId))
                return CategoryAccessState.Purchased;

            if (string.IsNullOrEmpty(category.unlockProductId))
                return CategoryAccessState.Free;

            int total = 0;
            int freeCount = 0;
            if (puzzles != null)
            {
                for (int i = 0; i < puzzles.Length; i++)
                {
                    PuzzleData puzzle = puzzles[i];
                    if (puzzle == null || !puzzle.isEnabled)
                        continue;

                    total++;
                    if (puzzle.isDefaultFree)
                        freeCount++;
                }
            }

            if (total == 0 || freeCount == total)
                return CategoryAccessState.Free;

            if (freeCount > 0)
                return CategoryAccessState.Partial;

            return CategoryAccessState.Locked;
        }
    }
}
