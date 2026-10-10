namespace MindParadox.Games.BertrandBox
{
    /// <summary>
    /// 화면에 보이는 문장. 나중에 이 값을 Localization Key로 바꾸면 된다.
    /// </summary>
    public static class BertrandBoxCopy
    {
        public const string Title = "Bertrand's Box";
        public const string Back = "뒤로";
        public const string BoxA = "BOX A";
        public const string BoxB = "BOX B";
        public const string BoxC = "BOX C";
        public const string Gold = "GOLD";
        public const string Silver = "SILVER";
        public const string DoubleGold = "GG";
        public const string DoubleSilver = "SS";
        public const string Mixed = "GS";
        public const string Remaining = "남은 동전";

        public const string QuizPrompt =
            "상자 3개가 있습니다.\n금화/금화, 은화/은화, 금화/은화.\n\n금화가 나왔다고 가정합니다.\n남은 동전도 금화일 확률은?";

        public const string PickPrompt = "상자 하나를 고르세요.";

        public const string GoldPrompt = "금화가 나왔습니다.\n\n남은 동전도 금화일 확률은?";

        public const string SilverPrompt =
            "은화가 나왔습니다.\n금화를 관찰한 경우가 아니므로\n다시 뽑을 수 있습니다.";

        public const string Correct = "정답";
        public const string Wrong = "오답";

        public const string Explanation =
            "금화가 나올 수 있는 경우는 세 가지입니다.\n\n" +
            "GG 상자의 첫 번째 금화\n" +
            "GG 상자의 두 번째 금화\n" +
            "GS 상자의 금화\n\n" +
            "남은 동전도 금화인 경우는\n" +
            "GG 상자의 두 가지입니다.\n\n" +
            "2 / 3 = 66.7%";

        public const string ChoiceHalf = "1/2 = 50%";
        public const string ChoiceTwoThirds = "2/3 ≈ 66.7%";
        public const string ChoiceThird = "1/3 ≈ 33.3%";
        public const string Redraw = "다시 뽑기";
        public const string Experiment = "직접 실험";
        public const string Simulate = "100회 실험";
        public const string Simulated = "100회 실험을 기록했습니다.";
    }
}
