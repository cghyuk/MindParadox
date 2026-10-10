namespace MindParadox.Core
{
    /// <summary>
    /// 씬을 넘길 때 고른 카테고리만 기억한다. 카테고리별 씬은 만들지 않는다.
    /// </summary>
    public static class CategorySession
    {
        public const string SceneName = "CategoryScene";
        public const string MainSceneName = "MainScene";

        public static string SelectedCategoryId { get; set; }
    }
}
