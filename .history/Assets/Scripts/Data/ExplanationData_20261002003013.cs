using UnityEngine;

namespace MindParadox.Data
{
    /// <summary>
    /// 퍼즐 설명 팝업에 넣을 문장.
    /// explanationKey와 각 Key 필드는 이후 8개 언어 테이블로 바꿀 자리이고, 지금은 작성된 문장을 표시한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ExplanationData", menuName = "Mind Paradox/Explanation Data")]
    public class ExplanationData : ScriptableObject
    {
        public string puzzleId;
        public string explanationTitle;
        public string explanationSummary;
        public string explanationBody;
        public string explanationFormula;
        public string explanationKey;
        public string titleKey;
        public string summaryKey;
        public string bodyKey;
        public string formulaKey;

        public string ResolvedTitle => ExplanationText.Resolve(explanationTitle, TitleLocalizationKey);
        public string ResolvedSummary => ExplanationText.Resolve(explanationSummary, SummaryLocalizationKey);
        public string ResolvedBody => ExplanationText.Resolve(explanationBody, BodyLocalizationKey);
        public string ResolvedFormula => ExplanationText.Resolve(explanationFormula, FormulaLocalizationKey);

        public string TitleLocalizationKey => KeyOrRoot(titleKey, "title");
        public string SummaryLocalizationKey => KeyOrRoot(summaryKey, "summary");
        public string BodyLocalizationKey => KeyOrRoot(bodyKey, "body");
        public string FormulaLocalizationKey => KeyOrRoot(formulaKey, "formula");

        string KeyOrRoot(string specificKey, string suffix)
        {
            if (!string.IsNullOrEmpty(specificKey))
                return specificKey;
            if (string.IsNullOrEmpty(explanationKey))
                return "";
            return explanationKey + "." + suffix;
        }
    }
}
