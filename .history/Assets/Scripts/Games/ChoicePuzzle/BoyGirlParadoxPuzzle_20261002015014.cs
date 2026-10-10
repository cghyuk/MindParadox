using System;

namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 두 아이 중 적어도 한 명이 남자일 때, 둘 다 남자일 확률은 1/3.
    /// </summary>
    public static class BoyGirlParadoxPuzzle
    {
        public const string Id = "boy-girl-paradox";

        public static ChoicePuzzleModel Create()
        {
            var state = new State();
            return new ChoicePuzzleModel
            {
                Title = "소년·소녀 역설",
                Situation =
                    "어떤 가족에게 자녀가 두 명 있습니다.\n" +
                    "그중 적어도 한 명은 남자아이입니다.",
                Steps = new[]
                {
                    new ChoiceStep
                    {
                        Question = "두 아이가 모두 남자아이일 확률은?",
                        Choices = new[] { "50%", "33.3%", "25%" },
                        CorrectIndex = 1,
                        Reason =
                            "순서를 구분하면 남-남, 남-여, 여-남, 여-여입니다.\n" +
                            "적어도 한 명이 남자이므로 여-여는 빠집니다.\n" +
                            "남은 세 경우 중 남-남은 하나이므로 1/3입니다."
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
            if (RunSample(8000, 11, out int conditioned, out int bothBoys) == false)
                return false;

            if (conditioned < 5600 || conditioned > 6400)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 소년·소녀 역설의 조건 표본이 3/4에서 멀리 떨어졌습니다. " + conditioned);
                return false;
            }

            double rate = bothBoys / (double)conditioned;
            if (rate < 0.28d || rate > 0.39d)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 둘 다 남자인 비율이 1/3에서 멀리 떨어졌습니다. " + bothBoys + " / " + conditioned);
                return false;
            }

            return ChoicePuzzleMath.Percent(1d / 3d) == "33.3%";
        }

        static bool RunSample(int count, int seed, out int conditioned, out int bothBoys)
        {
            var random = new System.Random(seed);
            conditioned = 0;
            bothBoys = 0;
            for (int i = 0; i < count; i++)
            {
                bool firstBoy = random.Next(2) == 0;
                bool secondBoy = random.Next(2) == 0;
                if (!firstBoy && !secondBoy)
                    continue;

                conditioned++;
                if (firstBoy && secondBoy)
                    bothBoys++;
            }

            return true;
        }

        sealed class State
        {
            readonly System.Random random = new System.Random();
            int conditioned;
            int bothBoys;

            public void Run(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    bool firstBoy = random.Next(2) == 0;
                    bool secondBoy = random.Next(2) == 0;
                    if (!firstBoy && !secondBoy)
                        continue;

                    conditioned++;
                    if (firstBoy && secondBoy)
                        bothBoys++;
                }
            }

            public void Reset()
            {
                conditioned = 0;
                bothBoys = 0;
            }

            public string Board()
            {
                return "가능한 순서\n<size=60>남-남   남-여   여-남</size>\n여-여는 조건에서 빠집니다.";
            }

            public string Stats()
            {
                return
                    "조건에 맞는 가족 " + conditioned + "\n" +
                    "둘 다 남자 " + bothBoys + "\n" +
                    "Observed: " + ChoicePuzzleMath.Percent(bothBoys, conditioned) + "\n" +
                    "Theory: 33.3%";
            }
        }
    }
}
