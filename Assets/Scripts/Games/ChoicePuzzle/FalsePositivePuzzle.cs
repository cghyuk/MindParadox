namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 유병률 1/1000, 민감도 99%, 위양성 1%이면 양성 예측도는 약 9%.
    /// </summary>
    public static class FalsePositivePuzzle
    {
        public const string Id = "false-positive";
        public const double Theory = 0.99d / 10.98d;

        public static ChoicePuzzleModel Create()
        {
            var state = new State();
            return new ChoicePuzzleModel
            {
                Title = "검사 정확도 함정",
                Situation =
                    "어떤 질병은 1000명 중 1명에게 있습니다.\n" +
                    "검사 민감도는 99%이고,\n" +
                    "건강한 사람도 1% 확률로 양성이 나옵니다.",
                Steps = new[]
                {
                    new ChoiceStep
                    {
                        Question = "검사 결과가 양성이라면\n실제로 질병이 있을 확률은?",
                        Choices = new[] { "약 99%", "약 50%", "약 9%", "약 1%" },
                        CorrectIndex = 2,
                        Reason =
                            "1000명 중 환자는 1명이고, 그중 양성은 약 0.99명입니다.\n" +
                            "건강한 999명 중 위양성은 약 9.99명입니다.\n" +
                            "양성 약 10.98명 가운데 실제 환자는 약 0.99명이므로\n" +
                            "0.99 / 10.98 ≈ 9%입니다.\n" +
                            "검사 정확도만 보면 안 되고 기저율을 같이 봐야 합니다."
                    }
                },
                Experiments = new[]
                {
                    new ChoiceExperiment { Label = "1000명 실험", Run = () => state.Run(1000) }
                },
                Board = state.Board,
                Stats = state.Stats,
                ResetSession = state.Reset
            };
        }

        public static bool Validate()
        {
            if (ChoicePuzzleMath.Percent(Theory) != "9.0%")
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 위양성 문제의 이론값이 9.0%가 아닙니다.");
                return false;
            }

            var random = new System.Random(11);
            int positive = 0;
            int truePositive = 0;
            const int people = 200000;
            for (int i = 0; i < people; i++)
            {
                bool sick = random.NextDouble() < 0.001d;
                bool detected = random.NextDouble() < (sick ? 0.99d : 0.01d);
                if (!detected)
                    continue;

                positive++;
                if (sick)
                    truePositive++;
            }

            if (positive <= 0)
                return false;

            double rate = truePositive / (double)positive;
            if (rate < 0.06d || rate > 0.13d)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 양성 중 실제 환자 비율이 9%에서 멀리 떨어졌습니다. " + truePositive + " / " + positive);
                return false;
            }

            return true;
        }

        sealed class State
        {
            readonly System.Random random = new System.Random();
            int people;
            int positive;
            int truePositive;

            public void Run(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    bool sick = random.NextDouble() < 0.001d;
                    bool detected = random.NextDouble() < (sick ? 0.99d : 0.01d);
                    people++;
                    if (!detected)
                        continue;

                    positive++;
                    if (sick)
                        truePositive++;
                }
            }

            public void Reset()
            {
                people = 0;
                positive = 0;
                truePositive = 0;
            }

            public string Board()
            {
                return
                    "1000명 기준\n" +
                    "환자 1명 → 양성 약 0.99명\n" +
                    "건강 999명 → 위양성 약 9.99명\n" +
                    "<size=60>0.99 / 10.98 ≈ 9%</size>";
            }

            public string Stats()
            {
                return
                    "실험 인원 " + people + "\n" +
                    "양성 " + positive + "\n" +
                    "실제 환자 " + truePositive + "\n" +
                    "Observed: " + ChoicePuzzleMath.Percent(truePositive, positive) + "\n" +
                    "Theory: 9.0%";
            }
        }
    }
}
