namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 진행자가 문을 열지 않아도 처음 고른 문은 1/3, 나머지 두 문은 2/3이다.
    /// </summary>
    public static class ThreeDoorChoicePuzzle
    {
        public const string Id = "three-door-choice";

        public static ChoicePuzzleModel Create()
        {
            var state = new State();
            return new ChoicePuzzleModel
            {
                Title = "세 문 선택 확률",
                Situation =
                    "문 3개가 있고 자동차는 한 곳에만 있습니다.\n" +
                    "문 하나를 골랐습니다.\n" +
                    "진행자는 아무 문도 열어주지 않습니다.",
                Steps = new[]
                {
                    new ChoiceStep
                    {
                        Question = "처음 고른 문에 자동차가 있을 확률은?",
                        Choices = new[] { "1/3", "1/2", "2/3" },
                        CorrectIndex = 0,
                        Reason = "아직 어떤 정보도 없으므로\n처음 고른 문 하나의 확률은 1/3입니다."
                    },
                    new ChoiceStep
                    {
                        Question = "선택하지 않은 두 문 전체에\n자동차가 있을 확률은?",
                        Choices = new[] { "1/3", "1/2", "2/3" },
                        CorrectIndex = 2,
                        Reason =
                            "선택하지 않은 두 문을 묶으면 2/3입니다.\n" +
                            "몬티홀에서 진행자가 그 두 문 중 염소 하나를 열면\n" +
                            "이 2/3가 남은 한 문으로 모입니다."
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
            int firstHit = 0;
            const int count = 8000;
            for (int i = 0; i < count; i++)
            {
                if (random.Next(3) == 0)
                    firstHit++;
            }

            double rate = firstHit / (double)count;
            if (rate < 0.28d || rate > 0.39d)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 세 문 문제에서 처음 고른 문의 적중률이 1/3에서 멀리 떨어졌습니다. " + firstHit);
                return false;
            }

            double other = 1d - rate;
            if (other < 0.61d || other > 0.72d)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 나머지 두 문의 비율이 2/3에서 멀리 떨어졌습니다. " + other);
                return false;
            }

            return true;
        }

        sealed class State
        {
            readonly System.Random random = new System.Random();
            int rounds;
            int firstHit;

            public void Run(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    rounds++;
                    if (random.Next(3) == 0)
                        firstHit++;
                }
            }

            public void Reset()
            {
                rounds = 0;
                firstHit = 0;
            }

            public string Board()
            {
                return "문 1    문 2    문 3\n<size=60>고른 문 1/3</size>\n<size=60>나머지 두 문 2/3</size>";
            }

            public string Stats()
            {
                int other = rounds - firstHit;
                return
                    "실험 " + rounds + "\n" +
                    "처음 고른 문 " + firstHit + "\n" +
                    "나머지 두 문 " + other + "\n" +
                    "처음 고른 문 Observed: " + ChoicePuzzleMath.Percent(firstHit, rounds) + "\n" +
                    "나머지 두 문 Observed: " + ChoicePuzzleMath.Percent(other, rounds) + "\n" +
                    "Theory: 33.3% / 66.7%";
            }
        }
    }
}
