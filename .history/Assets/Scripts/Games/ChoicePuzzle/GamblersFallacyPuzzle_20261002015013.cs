using System;
using System.Text;

namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 공정한 동전의 다음 확률은 이전 결과와 무관하게 50%다.
    /// </summary>
    public static class GamblersFallacyPuzzle
    {
        public const string Id = "gamblers-fallacy";

        public static ChoicePuzzleModel Create()
        {
            var state = new State();
            return new ChoicePuzzleModel
            {
                Title = "도박사의 오류",
                Situation =
                    "공정한 동전을 던졌더니\n" +
                    "앞면이 다섯 번 연속으로 나왔습니다.",
                Steps = new[]
                {
                    new ChoiceStep
                    {
                        Question = "다음 번에 뒷면이 나올 확률은?",
                        Choices = new[] { "50%", "60%", "75%", "거의 확실하다" },
                        CorrectIndex = 0,
                        Reason =
                            "각 던지기는 독립 사건입니다.\n" +
                            "앞면이 다섯 번 나와도 다음 동전의 확률은 바뀌지 않습니다.\n" +
                            "다음 앞면은 50%, 다음 뒷면도 50%입니다."
                    }
                },
                Experiments = new[]
                {
                    new ChoiceExperiment { Label = "10회 던지기", Run = () => state.Flip(10) },
                    new ChoiceExperiment { Label = "100회 던지기", Run = () => state.Flip(100) }
                },
                Board = state.Board,
                Stats = state.Stats,
                ResetSession = state.Reset
            };
        }

        public static bool Validate()
        {
            var random = new System.Random(11);
            int heads = 0;
            const int count = 2000;
            for (int i = 0; i < count; i++)
            {
                if (random.Next(2) == 0)
                    heads++;
            }

            if (heads < 900 || heads > 1100)
            {
                UnityEngine.Debug.LogError("[Mind Paradox] 공정한 동전 2000회에서 앞면이 절반에서 멀리 떨어졌습니다. " + heads);
                return false;
            }

            return true;
        }

        sealed class State
        {
            readonly System.Random random = new System.Random();
            readonly StringBuilder history = new StringBuilder("앞 앞 앞 앞 앞");
            int heads;
            int tails;

            public void Flip(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    bool head = random.Next(2) == 0;
                    if (history.Length > 0)
                        history.Append(' ');
                    if (head)
                    {
                        heads++;
                        history.Append("앞");
                    }
                    else
                    {
                        tails++;
                        history.Append("뒤");
                    }
                }
            }

            public void Reset()
            {
                heads = 0;
                tails = 0;
                history.Clear();
                history.Append("앞 앞 앞 앞 앞");
            }

            public string Board()
            {
                string[] parts = history.ToString().Split(' ');
                int start = Math.Max(0, parts.Length - 24);
                var tail = new StringBuilder();
                for (int i = start; i < parts.Length; i++)
                {
                    if (tail.Length > 0)
                        tail.Append(' ');
                    tail.Append(parts[i]);
                }

                return tail + "\n<size=60>다음 뒷면 50%</size>";
            }

            public string Stats()
            {
                return
                    "실험 앞면 " + heads + "\n" +
                    "실험 뒷면 " + tails + "\n" +
                    "다음 앞면 50%\n" +
                    "다음 뒷면 50%";
            }
        }
    }
}
