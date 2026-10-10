namespace MindParadox.Games.ChoicePuzzle
{
    public static class ChoicePuzzleCatalog
    {
        public static ChoicePuzzleModel Create(string puzzleId)
        {
            if (puzzleId == BoyGirlParadoxPuzzle.Id)
                return BoyGirlParadoxPuzzle.Create();
            if (puzzleId == TwoEnvelopesPuzzle.Id)
                return TwoEnvelopesPuzzle.Create();
            if (puzzleId == FalsePositivePuzzle.Id)
                return FalsePositivePuzzle.Create();
            if (puzzleId == GamblersFallacyPuzzle.Id)
                return GamblersFallacyPuzzle.Create();
            if (puzzleId == TwoCardProblemPuzzle.Id)
                return TwoCardProblemPuzzle.Create();
            if (puzzleId == ThreeDoorChoicePuzzle.Id)
                return ThreeDoorChoicePuzzle.Create();
            return null;
        }
    }
}
