namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 빨간 면을 봤을 때 반대쪽도 빨강일 확률은 2/3.
    /// </summary>
    public static class TwoCardProblemPuzzle
    {
        public const string Id = "two-card-problem";

        public static ChoicePuzzleModel Create()
        {
            var state = new State();
            return new ChoicePuzzleModel
            {
                Title = "카드 두 장 문제",
                Situation =
                    "카드 두 장이 있습니다.\n" +
                    "한 장은 양면이 빨강입니다. RR\n" +
                    "다른 한 장은 빨강과 파랑입니다. RB\n" +
                    "카드 한 장을 무작위로 골라 한 면을 봤더니 빨강이었습니다.",
                Steps = new[]
                {
                    new ChoiceStep
                    {
                        Question = "반대쪽도 빨강일 확률은?",
                        Choices = new[] { "50%", "66.7%", "33.3%" },
                        CorrectIndex = 1,
                        Reason =
                            "빨강 면을 볼 수 있는 경우는 세 가지입니다.\n" +
                            "RR의 첫 번째 빨강, RR의 두 번째 빨강, RB의 빨강.\n" +
                            "반대쪽도 빨강인 경우는 RR의 두 면이므로 2/3입니다."
                    }
                },
                Experiments = new[]
                {
                    new ChoiceExperiment { Label = "100회 실험", Run = () => state.Run(100) }
                },
                Board = state.Board,
                Stats = state.Stats,
                ResetSession = state.Reset
            };
        }

        public static bool Validate()
        {
            var random = new System.Random(11);
            int conditioned = 0;
            int otherRed = 0;
            const int count = 8000;
            for (int i = 0; i < count; i++)
            {
                bool doubleRed = random.Next(2) == 0;
                if (doubleRed)
                {
                    conditioned++;
                    otherRed++;
                    continue;
                }

                bool sawRed = random.Next(2) == 0;
                if (!sawRed)
                    continue;

                conditioned++;
            }

            if (conditioned < 5600 || conditioned > 6400)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 카드 문제에서 빨강 면을 본 횟수가 멀리 떨어졌습니다. " + conditioned);
                return false;
            }

            double rate = otherRed / (double)conditioned;
            if (rate < 0.60d || rate > 0.74d)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 반대쪽이 빨강인 비율이 2/3에서 멀리 떨어졌습니다. " + otherRed + " / " + conditioned);
                return false;
            }

            return ChoicePuzzleMath.Percent(2d / 3d) == "66.7%";
        }

        sealed class State
        {
            readonly System.Random random = new System.Random();
            int conditioned;
            int otherRed;

            public void Run(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    bool doubleRed = random.Next(2) == 0;
                    if (doubleRed)
                    {
                        conditioned++;
                        otherRed++;
                        continue;
                    }

                    bool sawRed = random.Next(2) == 0;
                    if (!sawRed)
                        continue;

                    conditioned++;
                }
            }

            public void Reset()
            {
                conditioned = 0;
                otherRed = 0;
            }

            public string Board()
            {
                return "RR의 빨강 두 면\nRB의 빨강 한 면\n<size=60>본 면: 빨강</size>";
            }

            public string Stats()
            {
                return
                    "빨강을 본 횟수 " + conditioned + "\n" +
                    "반대쪽도 빨강 " + otherRed + "\n" +
                    "Observed: " + ChoicePuzzleMath.Percent(otherRed, conditioned) + "\n" +
                    "Theory: 66.7%";
            }
        }
    }
}
