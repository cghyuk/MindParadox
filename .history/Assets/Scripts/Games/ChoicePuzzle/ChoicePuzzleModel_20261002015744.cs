using System;

namespace MindParadox.Games.ChoicePuzzle
{
    public sealed class ChoiceStep
    {
        public string Question;
        public string[] Choices;
        public int CorrectIndex;
        public string Reason;
    }

    public sealed class ChoiceExperiment
    {
        public string Label;
        public Action Run;
    }

    public sealed class ChoiceControl
    {
        public string Label;
        public Func<string> Value;
        public Action Decrease;
        public Action Increase;
        public Func<bool> CanDecrease;
        public Func<bool> CanIncrease;
    }

    /// <summary>
    /// 선택형 퍼즐이 공통 화면에 넘기는 문장과 실험.
    /// </summary>
    public sealed class ChoicePuzzleModel
    {
        public string Title;
        public string Situation;
        public ChoiceStep[] Steps = Array.Empty<ChoiceStep>();
        public ChoiceExperiment[] Experiments = Array.Empty<ChoiceExperiment>();
        public ChoiceControl[] Controls = Array.Empty<ChoiceControl>();
        public Func<string> Board = () => "";
        public Func<string> Stats = () => "";
        public Action ResetSession = () => { };
    }
}
