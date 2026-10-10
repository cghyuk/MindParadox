using System;
using System.Globalization;

namespace MindParadox.Games.BertrandBox
{
    public enum BertrandBoxKind
    {
        DoubleGold,
        DoubleSilver,
        Mixed
    }

    public enum BertrandCoin
    {
        Gold,
        Silver
    }

    public readonly struct BertrandDraw
    {
        public BertrandDraw(BertrandBoxKind box, BertrandCoin drawn, BertrandCoin remaining)
        {
            Box = box;
            Drawn = drawn;
            Remaining = remaining;
        }

        public BertrandBoxKind Box { get; }
        public BertrandCoin Drawn { get; }
        public BertrandCoin Remaining { get; }
    }

    /// <summary>
    /// 상자와 동전을 뽑고, 금화가 나온 경우만 모아 2/3를 확인한다.
    /// </summary>
    public static class BertrandBoxSimulator
    {
        public const double Theory = 2d / 3d;
        public const int BatchSize = 100;

        public static BertrandDraw Draw(BertrandBoxKind box, Random random)
        {
            if (random == null)
                random = new Random();

            switch (box)
            {
                case BertrandBoxKind.DoubleGold:
                    return new BertrandDraw(box, BertrandCoin.Gold, BertrandCoin.Gold);
                case BertrandBoxKind.DoubleSilver:
                    return new BertrandDraw(box, BertrandCoin.Silver, BertrandCoin.Silver);
                default:
                    if (random.Next(2) == 0)
                        return new BertrandDraw(box, BertrandCoin.Gold, BertrandCoin.Silver);
                    return new BertrandDraw(box, BertrandCoin.Silver, BertrandCoin.Gold);
            }
        }

        public static BertrandDraw DrawRandomBox(Random random)
        {
            if (random == null)
                random = new Random();

            var box = (BertrandBoxKind)random.Next(3);
            return Draw(box, random);
        }

        public static void RunDraws(int count, Random random, out int goldObserved, out int otherGold)
        {
            goldObserved = 0;
            otherGold = 0;
            if (count <= 0)
                return;

            if (random == null)
                random = new Random();

            for (int i = 0; i < count; i++)
            {
                BertrandDraw draw = DrawRandomBox(random);
                if (draw.Drawn != BertrandCoin.Gold)
                    continue;

                goldObserved++;
                if (draw.Remaining == BertrandCoin.Gold)
                    otherGold++;
            }
        }

        public static string FormatPercent(double probability)
        {
            return (probability * 100d).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        public static string FormatObserved(int otherGold, int goldObserved)
        {
            if (goldObserved <= 0)
                return "0.0%";

            return FormatPercent(otherGold / (double)goldObserved);
        }
    }
}
