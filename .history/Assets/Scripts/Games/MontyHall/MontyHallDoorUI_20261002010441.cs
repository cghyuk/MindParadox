using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.Games.MontyHall
{
    /// <summary>
    /// 문 하나의 화면 상태. 자동차 위치는 알지 못한다.
    /// </summary>
    public class MontyHallDoorUI : MonoBehaviour
    {
        public enum View
        {
            Closed,
            Selected,
            OpenedGoat,
            Car,
            Goat
        }

        static readonly Color ClosedColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color SelectedColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color GoatColor = new Color32(0x9A, 0xA4, 0xB5, 0xFF);
        static readonly Color CarColor = new Color32(0x1F, 0x8A, 0x4C, 0xFF);
        static readonly Color DarkText = new Color32(0x15, 0x20, 0x33, 0xFF);

        [SerializeField] Image face;
        [SerializeField] TMP_Text label;
        [SerializeField] Button button;

        public int Index { get; private set; }

        int numberDigits = 1;
        bool compact;

        public event Action<int> Clicked;

        public void Setup(int index, int doorCount)
        {
            Index = index;
            numberDigits = doorCount >= 100 ? 3 : doorCount >= 10 ? 2 : 1;
            compact = doorCount >= 20;
        }

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnClicked);
        }

        public void Show(View view, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;

            if (face != null)
                face.color = Fill(view);

            if (label == null)
                return;

            label.fontSize = 50f;
            label.enableAutoSizing = false;
            label.text = Caption(view);
            label.color = view == View.Selected || view == View.Car ? Color.white : DarkText;
        }

        void OnClicked()
        {
            Clicked?.Invoke(Index);
        }

        static Color Fill(View view)
        {
            switch (view)
            {
                case View.Selected:
                    return SelectedColor;
                case View.OpenedGoat:
                case View.Goat:
                    return GoatColor;
                case View.Car:
                    return CarColor;
                default:
                    return ClosedColor;
            }
        }

        string Caption(View view)
        {
            string number = (Index + 1).ToString(numberDigits == 3 ? "000" : numberDigits == 2 ? "00" : "0");
            if (compact)
            {
                if (view == View.OpenedGoat)
                    return "×";

                if (view == View.Car)
                    return "차";

                if (view == View.Goat)
                    return "염";

                return number;
            }

            switch (view)
            {
                case View.Selected:
                    return number + "\n선택";
                case View.OpenedGoat:
                case View.Goat:
                    return number + "\n염소";
                case View.Car:
                    return number + "\n자동차";
                default:
                    return number;
            }
        }
    }
}
