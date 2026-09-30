using MindParadox.Core;
using MindParadox.Data;
using TMPro;
using UnityEngine;

namespace MindParadox.UI
{
    /// <summary>
    /// 카테고리를 고르면 그 문제만 GameCard로 만든다. 카드는 씬에 미리 두지 않는다.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] GameContentDatabase database;
        [SerializeField] GameCardUI cardPrefab;
        [SerializeField] RectTransform cardContent;
        [SerializeField] RectTransform categoryContent;
        [SerializeField] TMP_FontAsset uiFont;
        [SerializeField] Sprite roundedSprite;

        CategoryChipUI[] chips = System.Array.Empty<CategoryChipUI>();
        string selectedCategoryId;

        void Start()
        {
            BuildCategories();
        }

        void BuildCategories()
        {
            ClearChildren(categoryContent);
            chips = System.Array.Empty<CategoryChipUI>();

            if (database == null || categoryContent == null)
            {
                Debug.LogError("[Mind Paradox] 메인 메뉴에 콘텐츠 데이터베이스 또는 카테고리 영역이 없습니다.");
                return;
            }

            CategoryData[] categories = database.GetCategories();
            chips = new CategoryChipUI[categories.Length];
            string firstEnabled = null;

            for (int i = 0; i < categories.Length; i++)
            {
                var chipObject = new GameObject(categories[i].categoryId, typeof(RectTransform));
                chipObject.transform.SetParent(categoryContent, false);
                var chip = chipObject.AddComponent<CategoryChipUI>();
                chip.Build(categories[i], uiFont, roundedSprite, SelectCategory);
                chips[i] = chip;

                if (firstEnabled == null && categories[i].isEnabled)
                    firstEnabled = categories[i].categoryId;
            }

            if (!string.IsNullOrEmpty(firstEnabled))
                SelectCategory(FindCategory(firstEnabled));
            else
                ShowPuzzles(null);
        }

        void SelectCategory(CategoryData category)
        {
            if (category == null || !category.isEnabled)
                return;

            selectedCategoryId = category.categoryId;
            for (int i = 0; i < chips.Length; i++)
            {
                if (chips[i] == null)
                    continue;

                chips[i].SetSelected(chips[i].Category != null && chips[i].Category.categoryId == selectedCategoryId);
            }

            ShowPuzzles(category);
        }

        void ShowPuzzles(CategoryData category)
        {
            ClearChildren(cardContent);

            if (database == null || cardContent == null || cardPrefab == null || category == null)
                return;

            PuzzleData[] puzzles = database.GetPuzzles(category.categoryId);
            for (int i = 0; i < puzzles.Length; i++)
            {
                PuzzleData puzzle = puzzles[i];
                GameCardUI card = Instantiate(cardPrefab, cardContent);
                card.name = puzzle.puzzleId;

                bool canPlay = UnlockManager.CanPlay(puzzle, category);
                string description = puzzle.shortDescription;
                if (!canPlay)
                    description = string.IsNullOrEmpty(description) ? "잠금" : description + "\n잠금";

                card.Setup(
                    puzzle.title,
                    description,
                    canPlay ? "Open " + puzzle.title : "Locked " + puzzle.puzzleId,
                    canPlay ? puzzle.sceneName : "",
                    puzzle.icon);
            }
        }

        CategoryData FindCategory(string categoryId)
        {
            CategoryData[] categories = database.GetCategories();
            for (int i = 0; i < categories.Length; i++)
            {
                if (categories[i].categoryId == categoryId)
                    return categories[i];
            }

            return null;
        }

        static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
        }
    }
}
