using System;

namespace MindParadox.Games.MontyHall
{
    public enum MontyHallPhase
    {
        ChooseDoor,
        ChooseStayOrSwitch,
        Result
    }

    /// <summary>
    /// 문 3개 몬티홀 한 판. 자동차는 한 문, 나머지는 염소다.
    /// </summary>
    public sealed class MontyHallRound
    {
        public const int DoorCount = 3;

        readonly Random random;

        public MontyHallRound()
            : this(new Random())
        {
        }

        public MontyHallRound(Random random)
        {
            this.random = random ?? new Random();
        }

        public MontyHallPhase Phase { get; private set; } = MontyHallPhase.ChooseDoor;
        public int CarDoor { get; private set; }
        public int PlayerDoor { get; private set; } = -1;
        public int RevealedDoor { get; private set; } = -1;
        public int FinalDoor { get; private set; } = -1;
        public bool Stayed { get; private set; }
        public bool Won => Phase == MontyHallPhase.Result && FinalDoor == CarDoor;

        public void Begin()
        {
            CarDoor = random.Next(DoorCount);
            PlayerDoor = -1;
            RevealedDoor = -1;
            FinalDoor = -1;
            Stayed = false;
            Phase = MontyHallPhase.ChooseDoor;
        }

        public void ChooseDoor(int doorIndex)
        {
            if (Phase != MontyHallPhase.ChooseDoor)
                return;

            if (doorIndex < 0 || doorIndex >= DoorCount)
                return;

            PlayerDoor = doorIndex;
            RevealedDoor = PickGoatDoor();
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

            FinalDoor = OtherClosedDoor();
            Stayed = false;
            Phase = MontyHallPhase.Result;
        }

        int PickGoatDoor()
        {
            int pick = -1;
            int matches = 0;
            for (int i = 0; i < DoorCount; i++)
            {
                if (i == PlayerDoor || i == CarDoor)
                    continue;

                matches++;
                if (random.Next(matches) == 0)
                    pick = i;
            }

            return pick;
        }

        int OtherClosedDoor()
        {
            for (int i = 0; i < DoorCount; i++)
            {
                if (i != PlayerDoor && i != RevealedDoor)
                    return i;
            }

            return PlayerDoor;
        }
    }
}
