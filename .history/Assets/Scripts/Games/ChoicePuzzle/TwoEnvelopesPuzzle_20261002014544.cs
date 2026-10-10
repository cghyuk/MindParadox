namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 관측 금액만으로 바꾸기가 항상 유리하다고 할 수 없다.
    /// 예시는 작은 금액이 1만, 2만, 4만, 8만 원 중 하나인 경우를 쓴다.
    /// </summary>
    public static class TwoEnvelopesPuzzle
    {
        public const string Id = "two-envelopes";
        static readonly int[] SmallerAmounts = { 10000, 20000, 40000, 80000 };

        public static ChoicePuzzleModel Create()
        {
            var state = new State();
            return new ChoicePuzzleModel
            {
                Title = "두 봉투 역설",
                Situation =
                    "두 봉투가 있습니다.\n" +
                    "한 봉투에는 금액 X가 들어 있고,\n" +
                    "다른 봉투에는 그 두 배인 2X가 들어 있습니다.\n" +
                    "봉투 하나를 골랐습니다.",
                Steps = new[]
                {
                    new ChoiceStep
                    {
                        Question = "봉투를 바꾸는 것이 항상 유리할까요?",
                        Choices = new[]
                        {
                            "항상 바꾸는 것이 유리하다",
                            "항상 유지하는 것이 유리하다",
                            "주어진 정보만으로는\n항상 유리하다고 할 수 없다"
                        },
                        CorrectIndex = 2,
                        Reason =
                            "현재 금액을 A로 두고\n다른 봉투가 A/2 또는 2A일 확률이 각각 1/2라고 두면\n" +
                            "바꾸는 쪽 기대값이 1.25A처럼 보입니다.\n\n" +
                            "하지만 그 가정은\n금액이 어떻게 정해졌는지 모르면 성립하지 않습니다.\n" +
                            "사전 분포가 있어야 기대값을 말할 수 있습니다."
                    }
                },
                Experiments = new[]
                {
                    new ChoiceExperiment { Label = "100회 예시", Run = () => state.Run(100) }
                },
                Board = state.Board,
                Stats = state.Stats,
                ResetSession = state.Reset
            };
        }

        public static bool Validate()
        {
            long stay = 0;
            long swap = 0;
            var random = new System.Random(11);
            const int count = 4000;
            for (int i = 0; i < count; i++)
            {
                int smaller = SmallerAmounts[random.Next(SmallerAmounts.Length)];
                bool pickSmaller = random.Next(2) == 0;
                int stayValue = pickSmaller ? smaller : smaller * 2;
                int swapValue = pickSmaller ? smaller * 2 : smaller;
                stay += stayValue;
                swap += swapValue;
            }

            double stayMean = stay / (double)count;
            double swapMean = swap / (double)count;
            double gap = System.Math.Abs(stayMean - swapMean) / stayMean;
            if (gap > 0.08d)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 두 봉투 예시에서 유지와 바꾸기의 평균이 멀리 떨어졌습니다. " + stayMean + " / " + swapMean);
                return false;
            }

            return true;
        }

        sealed class State
        {
            readonly System.Random random = new System.Random();
            int rounds;
            long stayTotal;
            long swapTotal;
            string latest = "고른 봉투의 금액만으로는\n다른 봉투가 절반인지 두 배인지 알 수 없습니다.";

            public void Run(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    int smaller = SmallerAmounts[random.Next(SmallerAmounts.Length)];
                    int larger = smaller * 2;
                    bool pickSmaller = random.Next(2) == 0;
                    int stayValue = pickSmaller ? smaller : larger;
                    int swapValue = pickSmaller ? larger : smaller;
                    stayTotal += stayValue;
                    swapTotal += swapValue;
                    rounds++;
                    string outcome = swapValue > stayValue ? "바꾸면 이득" : "바꾸면 손해";
                    latest =
                        "이번 예시\n" +
                        "<size=60>" + stayValue.ToString("N0") + "원</size>\n" +
                        "다른 봉투 " + swapValue.ToString("N0") + "원\n" +
                        outcome;
                }
            }

            public void Reset()
            {
                rounds = 0;
                stayTotal = 0;
                swapTotal = 0;
                latest = "고른 봉투의 금액만으로는\n다른 봉투가 절반인지 두 배인지 알 수 없습니다.";
            }

            public string Board()
            {
                return latest;
            }

            public string Stats()
            {
                if (rounds <= 0)
                {
                    return
                        "예시의 작은 금액\n1만, 2만, 4만, 8만 원\n" +
                        "Stay 평균: -\n" +
                        "Switch 평균: -";
                }

                double stayMean = stayTotal / (double)rounds;
                double swapMean = swapTotal / (double)rounds;
                return
                    "예시 " + rounds + "회\n" +
                    "작은 금액은 1만, 2만, 4만, 8만 원 중 하나\n" +
                    "Stay 평균: " + stayMean.ToString("N0") + "원\n" +
                    "Switch 평균: " + swapMean.ToString("N0") + "원";
            }
        }
    }
}
