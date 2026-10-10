using System;
using System.Globalization;

namespace MindParadox.Games.BirthdayParadox
{
    /// <summary>
    /// 윤년 없는 365일 생일 문제의 이론 확률, 생일 생성, 중복 판정.
    /// </summary>
    public static class BirthdayParadoxCalculator
    {
        public const int DaysInYear = 365;
        public const int MinPeople = 2;
        public const int MaxPeople = 100;
        public const int DefaultPeople = 23;

        static readonly int[] DaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        public static int ClampPeople(int count)
        {
            if (count < MinPeople)
                return MinPeople;
            if (count > MaxPeople)
                return MaxPeople;
            return count;
        }

        public static int PairCount(int people)
        {
            people = ClampPeople(people);
            return people * (people - 1) / 2;
        }

        /// <summary>
        /// 한 명을 기준으로 나머지가 모두 다른 날일 확률을 곱한 뒤, 1에서 뺀다.
        /// </summary>
        public static double MatchProbability(int people)
        {
            people = ClampPeople(people);
            double noMatch = 1d;
            for (int i = 1; i < people; i++)
                noMatch *= (DaysInYear - i) / (double)DaysInYear;
            return 1d - noMatch;
        }

        public static string FormatPercent(double probability)
        {
            if (probability >= 0.999d)
                return "99.9% 이상";

            return (probability * 100d).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static string FormatObservedPercent(int matches, int trials)
        {
            if (trials <= 0)
                return "0.0%";

            double percent = matches * 100d / trials;
            return percent.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static int[] CreateBirthdays(int people, Random random)
        {
            people = ClampPeople(people);
            if (random == null)
                random = new Random();

            var days = new int[people];
            for (int i = 0; i < people; i++)
                days[i] = random.Next(0, DaysInYear);
            return days;
        }

        public static bool HasSharedBirthday(int[] days)
        {
            if (days == null || days.Length < 2)
                return false;

            var seen = new bool[DaysInYear];
            for (int i = 0; i < days.Length; i++)
            {
                int day = days[i];
                if ((uint)day >= DaysInYear)
                    continue;
                if (seen[day])
                    return true;
                seen[day] = true;
            }

            return false;
        }

        public static bool[] DuplicateFlags(int[] days)
        {
            var flags = days == null ? Array.Empty<bool>() : new bool[days.Length];
            if (days == null)
                return flags;

            var counts = new int[DaysInYear];
            for (int i = 0; i < days.Length; i++)
            {
                int day = days[i];
                if ((uint)day < DaysInYear)
                    counts[day]++;
            }

            for (int i = 0; i < days.Length; i++)
            {
                int day = days[i];
                flags[i] = (uint)day < DaysInYear && counts[day] >= 2;
            }

            return flags;
        }

        public static void GetMonthDay(int dayIndex, out int month, out int day)
        {
            if (dayIndex < 0)
                dayIndex = 0;
            if (dayIndex >= DaysInYear)
                dayIndex = DaysInYear - 1;

            int remaining = dayIndex;
            for (int i = 0; i < DaysInMonth.Length; i++)
            {
                if (remaining < DaysInMonth[i])
                {
                    month = i + 1;
                    day = remaining + 1;
                    return;
                }

                remaining -= DaysInMonth[i];
            }

            month = 12;
            day = 31;
        }
    }
}
