using MindParadox.Data;

namespace MindParadox.UI
{
    /// <summary>
    /// 공통 UI 문장. 키는 이후 8개 언어 테이블에 연결하고, 지금은 한국어를 표시한다.
    /// </summary>
    public static class UiText
    {
        public const string BackKey = "ui.back";
        public const string ExplanationKey = "ui.explanation";

        const string BackKorean = "뒤로";
        const string ExplanationKorean = "설명";

        public static string BackLabel => ExplanationText.Resolve(BackKorean, BackKey);
        public static string ExplanationLabel => ExplanationText.Resolve(ExplanationKorean, ExplanationKey);
    }
}
