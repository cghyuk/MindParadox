using MindParadox.Core;
using MindParadox.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MindParadox.UI
{
    /// <summary>
    /// 첫 화면에서 카테고리 카드만 만든다. 문제는 CategoryScene에서 보여 준다.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] GameContentDatabase database;
        [SerializeField] CategoryCardUI categoryCardPrefab;
        [SerializeField] RectTransform categoryContent;

        void Start()
        {
            BuildCategories();
        }

        void BuildCategories()
        {
            ClearChildren(categoryContent);

            if (database == null || categoryContent == null || categoryCardPrefab == null)
            {
                Debug.LogError("[Mind Paradox] 메인 메뉴에 콘텐츠 데이터베이스 또는 카테고리 카드가 없습니다.");
                return;
            }

            CategoryData[] categories = database.GetCategories();
            for (int i = 0; i < categories.Length; i++)
            {
                CategoryData category = categories[i];
                if (category == null)
                    continue;

                PuzzleData[] puzzles = database.GetPuzzles(category.categoryId);
                CategoryAccessState state = UnlockManager.GetAccessState(category, puzzles);
                CategoryCardUI card = Instantiate(categoryCardPrefab, categoryContent);
                card.name = category.categoryId;
                card.Bind(category, puzzles.Length, state, () => OpenCategory(category));
            }
        }

        void OpenCategory(CategoryData category)
        {
            if (category == null || !category.isEnabled)
                return;

            PuzzleData[] puzzles = database.GetPuzzles(category.categoryId);
            if (UnlockManager.GetAccessState(category, puzzles) == CategoryAccessState.Locked)
                return;

            CategorySession.SelectedCategoryId = category.categoryId;
            if (!Application.CanStreamedLevelBeLoaded(CategorySession.SceneName))
            {
                Debug.LogWarning("[Mind Paradox] Build Settings에서 CategoryScene을 찾지 못했습니다.");
                return;
            }

            SceneManager.LoadScene(CategorySession.SceneName);
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
