using System;
using System.Collections.Generic;
using UnityEngine;

namespace MindParadox.Data
{
    /// <summary>
    /// 카테고리와 문제 에셋 목록. 런타임은 이 목록만 읽는다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameContentDatabase", menuName = "Mind Paradox/Game Content Database")]
    public class GameContentDatabase : ScriptableObject
    {
        public CategoryData[] categories = Array.Empty<CategoryData>();
        public PuzzleData[] puzzles = Array.Empty<PuzzleData>();

        public CategoryData[] GetCategories()
        {
            var list = new List<CategoryData>();
            if (categories == null)
                return Array.Empty<CategoryData>();

            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i] != null && !string.IsNullOrEmpty(categories[i].categoryId))
                    list.Add(categories[i]);
            }

            list.Sort((a, b) =>
            {
                int order = a.displayOrder.CompareTo(b.displayOrder);
                return order != 0 ? order : string.CompareOrdinal(a.categoryId, b.categoryId);
            });
            return list.ToArray();
        }

        public PuzzleData[] GetPuzzles(string categoryId)
        {
            var list = new List<PuzzleData>();
            if (puzzles == null || string.IsNullOrEmpty(categoryId))
                return Array.Empty<PuzzleData>();

            for (int i = 0; i < puzzles.Length; i++)
            {
                PuzzleData puzzle = puzzles[i];
                if (puzzle == null || !puzzle.isEnabled)
                    continue;

                if (puzzle.categoryId != categoryId)
                    continue;

                list.Add(puzzle);
            }

            list.Sort((a, b) =>
            {
                int order = a.displayOrder.CompareTo(b.displayOrder);
                if (order != 0)
                    return order;

                order = a.unlockOrder.CompareTo(b.unlockOrder);
                return order != 0 ? order : string.CompareOrdinal(a.puzzleId, b.puzzleId);
            });
            return list.ToArray();
        }
    }
}
