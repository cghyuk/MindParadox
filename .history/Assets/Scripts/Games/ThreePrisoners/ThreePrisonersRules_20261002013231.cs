using System;
using System.Globalization;

namespace MindParadox.Games.ThreePrisoners
{
    public enum Prisoner
    {
        A = 0,
        B = 1,
        C = 2
    }

    /// <summary>
    /// 사면자는 균등하고, 간수는 사면된 사람을 말하지 않는다.
    /// A가 사면되면 B와 C를 반반으로 말하고, B가 사면되면 C만, C가 사면되면 B만 말한다.
    /// </summary>
    public static class ThreePrisonersRules
    {
        public const double Theory = 1d / 3d;

        public static Prisoner WardenNames(Prisoner pardoned, Random random)
        {
            if (pardoned == Prisoner.A)
                return random.Next(2) == 0 ? Prisoner.B : Prisoner.C;

            if (pardoned == Prisoner.B)
                return Prisoner.C;

            return Prisoner.B;
        }

        public static void RunBatch(int count, Random random, out int conditioned, out int aPardoned)
        {
            conditioned = 0;
            aPardoned = 0;
            if (count <= 0 || random == null)
                return;

            for (int i = 0; i < count; i++)
            {
                Prisoner pardoned = (Prisoner)random.Next(3);
                Prisoner named = WardenNames(pardoned, random);
                if (named != Prisoner.B)
                    continue;

                conditioned++;
                if (pardoned == Prisoner.A)
                    aPardoned++;
            }
        }

        public static string FormatPercent(double rate)
        {
            return (rate * 100d).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static string FormatObserved(int aPardoned, int conditioned)
        {
            if (conditioned <= 0)
                return FormatPercent(0d);

            return FormatPercent(aPardoned / (double)conditioned);
        }
    }
}
