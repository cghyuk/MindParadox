using MindParadox.Core;
using MindParadox.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 고른 카테고리의 문제만 GameCard로 만든다.
    /// </summary>
    public class CategoryMenuUI : MonoBehaviour
    {
        [SerializeField] GameContentDatabase database;
        [SerializeField] GameCardUI cardPrefab;
        [SerializeField] RectTransform cardContent;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subtitleText;
        [SerializeField] Button backButton;

        void Awake()
        {
            if (backButton != null)
                backButton.onClick.AddListener(BackToMenu);
        }

        void Start()
        {
            ShowCategory(CategorySession.SelectedCategoryId);
        }

        public void ShowCategory(string categoryId)
        {
            ClearChildren(cardContent);

            if (database == null)
            {
                Debug.LogError("[Mind Paradox] 카테고리 화면에 콘텐츠 데이터베이스가 없습니다.");
                return;
            }

            CategoryData category = database.FindCategory(categoryId);
            if (titleText != null)
                titleText.text = category != null ? category.displayName : "Category";

            if (subtitleText != null)
                subtitleText.text = category != null ? category.description : "";

            if (category == null || cardContent == null || cardPrefab == null)
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

        public void BackToMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(CategorySession.MainSceneName))
            {
                Debug.LogWarning("[Mind Paradox] Build Settings에서 MainScene을 찾지 못했습니다.");
                return;
            }

            SceneManager.LoadScene(CategorySession.MainSceneName);
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
