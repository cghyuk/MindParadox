using System;
using System.Globalization;

namespace MindParadox.Games.MontyHall
{
    public enum MontyHallPhase
    {
        ChooseDoor,
        ChooseStayOrSwitch,
        Result
    }

    /// <summary>
    /// N개 문 몬티홀 한 판. 자동차는 한 문이고, 마지막에는 닫힌 문이 두 개만 남는다.
    /// </summary>
    public sealed class MontyHallRound
    {
        public const int MinDoors = 3;
        public const int MaxDoors = 100;

        readonly Random random;
        readonly bool[] opened;

        public MontyHallRound(int doorCount, Random random = null)
        {
            DoorCount = ClampCount(doorCount);
            this.random = random ?? new Random();
            opened = new bool[DoorCount];
        }

        public int DoorCount { get; }
        public MontyHallPhase Phase { get; private set; } = MontyHallPhase.ChooseDoor;
        public int CarDoor { get; private set; }
        public int PlayerDoor { get; private set; } = -1;
        public int AlternateDoor { get; private set; } = -1;
        public int OpenedCount { get; private set; }
        public int FinalDoor { get; private set; } = -1;
        public bool Stayed { get; private set; }
        public bool Won => Phase == MontyHallPhase.Result && FinalDoor == CarDoor;

        public static int ClampCount(int doorCount)
        {
            if (doorCount < MinDoors)
                return MinDoors;

            if (doorCount > MaxDoors)
                return MaxDoors;

            return doorCount;
        }

        public static string StayPercent(int doorCount)
        {
            return FormatPercent(100.0 / ClampCount(doorCount));
        }

        public static string SwitchPercent(int doorCount)
        {
            int count = ClampCount(doorCount);
            return FormatPercent(100.0 * (count - 1) / count);
        }

        public bool IsOpened(int doorIndex)
        {
            if (doorIndex < 0 || doorIndex >= DoorCount)
                return false;

            return opened[doorIndex];
        }

        public void Begin()
        {
            CarDoor = random.Next(DoorCount);
            PlayerDoor = -1;
            AlternateDoor = -1;
            FinalDoor = -1;
            OpenedCount = 0;
            Stayed = false;
            Phase = MontyHallPhase.ChooseDoor;

            for (int i = 0; i < opened.Length; i++)
                opened[i] = false;
        }

        public void ChooseDoor(int doorIndex)
        {
            if (Phase != MontyHallPhase.ChooseDoor)
                return;

            if (doorIndex < 0 || doorIndex >= DoorCount)
                return;

            PlayerDoor = doorIndex;
            LeaveOneOtherDoorClosed();
            Phase = MontyHallPhase.ChooseStayOrSwitch;
        }

        public void Stay()
        {
            if (Phase != MontyHallPhase.ChooseStayOrSwitch)
                return;

            FinalDoor = PlayerDoor;
            Stayed = true;
            Phase = MontyHallPhase.Result;
        }

        public void Switch()
        {
            if (Phase != MontyHallPhase.ChooseStayOrSwitch)
                return;

            FinalDoor = AlternateDoor;
            Stayed = false;
            Phase = MontyHallPhase.Result;
        }

        void LeaveOneOtherDoorClosed()
        {
            AlternateDoor = PlayerDoor == CarDoor ? RandomOtherDoor() : CarDoor;
            OpenedCount = 0;

            for (int i = 0; i < DoorCount; i++)
            {
                bool open = i != PlayerDoor && i != AlternateDoor;
                opened[i] = open;
                if (open)
                    OpenedCount++;
            }
        }

        int RandomOtherDoor()
        {
            int pick = -1;
            int matches = 0;
            for (int i = 0; i < DoorCount; i++)
            {
                if (i == PlayerDoor)
                    continue;

                matches++;
                if (random.Next(matches) == 0)
                    pick = i;
            }

            return pick;
        }

        static string FormatPercent(double percent)
        {
            double rounded = Math.Round(percent, 1, MidpointRounding.AwayFromZero);
            if (Math.Abs(rounded - Math.Round(rounded, MidpointRounding.AwayFromZero)) < 0.05)
                return Math.Round(rounded, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture) + "%";

            return rounded.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }
    }
}
