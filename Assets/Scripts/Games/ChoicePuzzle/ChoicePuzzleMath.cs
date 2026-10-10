using System.Globalization;

namespace MindParadox.Games.ChoicePuzzle
{
    public static class ChoicePuzzleMath
    {
        public static string Percent(double rate)
        {
            return (rate * 100d).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static string Percent(int hits, int total)
        {
            if (total <= 0)
                return Percent(0d);
            return Percent(hits / (double)total);
        }
    }
}
