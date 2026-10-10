using System;
using System.Globalization;

namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 독립 시행에서 적어도 한 번 성공할 확률은 1 - (1 - p)^n 이다.
    /// </summary>
    public static class AtLeastOneSuccessPuzzle
    {
        public const string Id = "at-least-one-success";
        const int DefaultPercent = 30;
        const int DefaultTrials = 3;

        public static double AtLeastOne(double probability, int trials)
        {
            if (trials <= 0)
                return 0d;
            if (probability <= 0d)
                return 0d;
            if (probability >= 1d)
                return 1d;
            return 1d - Math.Pow(1d - probability, trials);
        }

        public static ChoicePuzzleModel Create()
        {
            var state = new State();
            return new ChoicePuzzleModel
            {
                Title = "적어도 하나의 성공",
                Situation =
                    "어떤 사건이 한 번 성공할 확률이 30%라고 하자.\n" +
                    "이 시도를 독립적으로 3번 한다.",
                Steps = new[]
                {
                    new ChoiceStep
                    {
                        Question = "적어도 한 번 성공할 확률은?",
                        Choices = new[] { "30%", "60%", "약 65.7%", "90%" },
                        CorrectIndex = 2,
                        Reason =
                            "한 번 실패할 확률은 70%입니다.\n" +
                            "3번 모두 실패할 확률은 0.7 × 0.7 × 0.7 = 0.343입니다.\n" +
                            "적어도 한 번 성공할 확률은 1 - 0.343 = 0.657, 약 65.7%입니다.\n" +
                            "30%를 3번 더한 90%가 아닙니다."
                    }
                },
                Controls = new[]
                {
                    new ChoiceControl
                    {
                        Label = "성공 확률",
                        Value = () => state.Percent + "%",
                        Decrease = () => state.SetPercent(state.Percent - 10),
                        Increase = () => state.SetPercent(state.Percent + 10),
                        CanDecrease = () => state.Percent > 10,
                        CanIncrease = () => state.Percent < 90
                    },
                    new ChoiceControl
                    {
                        Label = "시도 횟수",
                        Value = () => state.Trials.ToString(CultureInfo.InvariantCulture),
                        Decrease = () => state.SetTrials(state.Trials - 1),
                        Increase = () => state.SetTrials(state.Trials + 1),
                        CanDecrease = () => state.Trials > 1,
                        CanIncrease = () => state.Trials < 10
                    }
                },
                Experiments = new[]
                {
                    new ChoiceExperiment { Label = "100회 시뮬레이션", Run = () => state.Run(100) }
                },
                Board = state.Board,
                Stats = state.Stats,
                ResetSession = state.Reset
            };
        }

        public static bool Validate()
        {
            string theory = ChoicePuzzleMath.Percent(AtLeastOne(0.30d, 3));
            if (theory != "65.7%")
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 적어도 한 번 성공할 확률이 65.7%가 아닙니다. " + theory);
                return false;
            }

            var random = new Random(11);
            int hits = 0;
            const int rounds = 4000;
            const double probability = 0.30d;
            const int trials = 3;
            for (int i = 0; i < rounds; i++)
            {
                bool success = false;
                for (int t = 0; t < trials; t++)
                {
                    if (random.NextDouble() < probability)
                        success = true;
                }

                if (success)
                    hits++;
            }

            double rate = hits / (double)rounds;
            if (rate < 0.60d || rate > 0.72d)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 30%를 3번 시도한 시뮬레이션이 65.7%에서 멀리 떨어졌습니다. " + hits + " / " + rounds);
                return false;
            }

            return true;
        }

        sealed class State
        {
            readonly Random random = new Random();
            int rounds;
            int hits;

            public int Percent { get; private set; } = DefaultPercent;
            public int Trials { get; private set; } = DefaultTrials;

            public void SetPercent(int value)
            {
                int next = Math.Max(10, Math.Min(90, value));
                if (next == Percent)
                    return;
                Percent = next;
                ClearSamples();
            }

            public void SetTrials(int value)
            {
                int next = Math.Max(1, Math.Min(10, value));
                if (next == Trials)
                    return;
                Trials = next;
                ClearSamples();
            }

            public void Run(int count)
            {
                double probability = Percent / 100d;
                for (int i = 0; i < count; i++)
                {
                    bool success = false;
                    for (int t = 0; t < Trials; t++)
                    {
                        if (random.NextDouble() < probability)
                            success = true;
                    }

                    rounds++;
                    if (success)
                        hits++;
                }
            }

            public void Reset()
            {
                Percent = DefaultPercent;
                Trials = DefaultTrials;
                ClearSamples();
            }

            public string Board()
            {
                double probability = Percent / 100d;
                double rate = AtLeastOne(probability, Trials);
                string p = probability.ToString("0.00", CultureInfo.InvariantCulture);
                return
                    "1 - (1 - " + p + ")^" + Trials.ToString(CultureInfo.InvariantCulture) + "\n" +
                    "<size=60>" + ChoicePuzzleMath.Percent(rate) + "</size>";
            }

            public string Stats()
            {
                return
                    "시뮬레이션 " + rounds + "\n" +
                    "적어도 한 번 성공 " + hits + "\n" +
                    "Observed: " + ChoicePuzzleMath.Percent(hits, rounds) + "\n" +
                    "Theory: " + ChoicePuzzleMath.Percent(AtLeastOne(Percent / 100d, Trials));
            }

            void ClearSamples()
            {
                rounds = 0;
                hits = 0;
            }
        }
    }
}
