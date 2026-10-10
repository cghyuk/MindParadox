namespace MindParadox.Data
{
    /// <summary>
    /// 설명 문장을 고르는 한 곳. 다국어 테이블이 생기면 이 메서드만 키 조회로 바꾸면 된다.
    /// </summary>
    public static class ExplanationText
    {
        public static string Resolve(string value, string localizationKey)
        {
            if (!string.IsNullOrEmpty(value))
                return value;

            return localizationKey ?? "";
        }
    }
}
