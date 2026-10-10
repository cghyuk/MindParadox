namespace MindParadox.Games.ThreePrisoners
{
    public static class ThreePrisonersCopy
    {
        public const string Title = "세 죄수 문제";
        public const string Situation =
            "죄수 A, B, C 세 명이 있습니다.\n" +
            "세 명 중 정확히 한 명만 사면되고,\n나머지 두 명은 처형됩니다.\n\n" +
            "죄수 A는 간수에게 묻습니다.\n" +
            "나 말고 B와 C 중\n처형될 사람 한 명의 이름을 알려주세요.";
        public const string WardenLine = "B가 처형됩니다.";
        public const string Question = "A가 사면될 확률은?";
        public const string ChoiceHalf = "50%";
        public const string ChoiceThird = "33.3%";
        public const string ChoiceTwoThirds = "66.7%";
        public const string PrisonerA = "A";
        public const string PrisonerB = "B";
        public const string PrisonerC = "C";
        public const string Correct = "정답";
        public const string Wrong = "오답";
        public const string Reason =
            "간수는 사면될 사람을 말할 수 없습니다.\n" +
            "A가 사면되면 B와 C 중 하나를 무작위로 말하고,\n" +
            "C가 사면되면 반드시 B라고 말합니다.\n" +
            "그래서 A의 사면 확률은 1/3입니다.";
        public const string Replay = "다시 하기";
        public const string Simulate = "100회 실험";
        public const string Theory = "33.3%";
    }
}
