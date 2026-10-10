using System.Collections.Generic;
using MindParadox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.Games.MontyHall
{
    /// <summary>
    /// 모드 선택, N개 문 생성, 진행, 결과, 이번 실행의 기록.
    /// </summary>
    public class MontyHallGameManager : MonoBehaviour
    {
        const string ProbabilityCategoryId = "probability";
        const string ThreeDoorLesson =
            "처음 선택이 맞을 확률은 1/3이고,\n처음 선택이 틀릴 확률은 2/3이다.\n" +
            "진행자가 염소 문 하나를 제거하므로\n처음 선택이 틀렸던 2/3의 경우\n문을 바꾸면 자동차를 얻게 된다.";

        [SerializeField] MontyHallDoorUI doorPrefab;
        [SerializeField] RectTransform doorContent;
        [SerializeField] GridLayoutGroup doorGrid;
        [SerializeField] GameObject modePanel;
        [SerializeField] GameObject customPanel;
        [SerializeField] GameObject gameArea;
        [SerializeField] GameObject messageArea;
        [SerializeField] GameObject actionArea;
        [SerializeField] Slider doorSlider;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text customCountText;
        [SerializeField] TMP_Text customOddsText;
        [SerializeField] TMP_Text oddsText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] Button stayButton;
        [SerializeField] Button switchButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button backButton;
        [SerializeField] Button modeButton;
        [SerializeField] Button threeDoorButton;
        [SerializeField] Button tenDoorButton;
        [SerializeField] Button customButton;
        [SerializeField] Button customStartButton;
        [SerializeField] Button plusButton;
        [SerializeField] Button minusButton;

        readonly List<MontyHallDoorUI> doors = new List<MontyHallDoorUI>();
        readonly Dictionary<int, Tally> records = new Dictionary<int, Tally>();

        MontyHallRound round;
        int doorCount = 3;
        int customCount = 10;

        void Awake()
        {
            if (stayButton != null)
                stayButton.onClick.AddListener(OnStay);

            if (switchButton != null)
                switchButton.onClick.AddListener(OnSwitch);

            if (replayButton != null)
                replayButton.onClick.AddListener(StartRound);

            if (backButton != null)
                backButton.onClick.AddListener(ReturnToCategory);

            if (modeButton != null)
                modeButton.onClick.AddListener(ShowModeSelect);

            if (threeDoorButton != null)
                threeDoorButton.onClick.AddListener(SelectThreeDoors);

            if (tenDoorButton != null)
                tenDoorButton.onClick.AddListener(SelectTenDoors);

            if (customButton != null)
                customButton.onClick.AddListener(ShowCustomSetup);

            if (customStartButton != null)
                customStartButton.onClick.AddListener(StartCustomGame);

            if (plusButton != null)
                plusButton.onClick.AddListener(AddCustomDoor);

            if (minusButton != null)
                minusButton.onClick.AddListener(RemoveCustomDoor);

            if (doorSlider != null)
            {
                doorSlider.minValue = MontyHallRound.MinDoors;
                doorSlider.maxValue = MontyHallRound.MaxDoors;
                doorSlider.wholeNumbers = true;
                doorSlider.value = customCount;
                doorSlider.onValueChanged.AddListener(OnSliderChanged);
            }
        }

        void Start()
        {
            ShowModeSelect();
        }

        public void SelectThreeDoors()
        {
            BeginGame(3);
        }

        public void SelectTenDoors()
        {
            BeginGame(10);
        }

        public void ShowCustomSetup()
        {
            if (customPanel != null)
                customPanel.SetActive(true);

            ApplyCustomCount(customCount);
        }

        public void AddCustomDoor()
        {
            ApplyCustomCount(customCount + 1);
        }

        public void RemoveCustomDoor()
        {
            ApplyCustomCount(customCount - 1);
        }

        public void StartCustomGame()
        {
            BeginGame(customCount);
        }

        public void StartRound()
        {
            if (doorCount < MontyHallRound.MinDoors)
                return;

            if (doors.Count != doorCount)
                BuildDoors();

            round = new MontyHallRound(doorCount);
            round.Begin();
            Refresh();
        }

        void BeginGame(int count)
        {
            doorCount = MontyHallRound.ClampCount(count);
            SetPlayChrome(true);
            BuildDoors();
            StartRound();
        }

        void ShowModeSelect()
        {
            round = null;
            ClearDoors();
            SetPlayChrome(false);

            if (descriptionText != null)
                descriptionText.text = "문이 많아질수록\n바꾸기의 확률은 어떻게 달라질까요?";

            if (customPanel != null)
                customPanel.SetActive(false);

            ApplyCustomCount(customCount);
        }

        void SetPlayChrome(bool playing)
        {
            if (modePanel != null)
                modePanel.SetActive(!playing);

            if (gameArea != null)
                gameArea.SetActive(playing);

            if (messageArea != null)
                messageArea.SetActive(playing);

            if (actionArea != null)
                actionArea.SetActive(playing);

            if (modeButton != null)
                modeButton.interactable = playing;
        }

        void OnSliderChanged(float value)
        {
            ApplyCustomCount(Mathf.RoundToInt(value));
        }

        void ApplyCustomCount(int count)
        {
            customCount = MontyHallRound.ClampCount(count);

            if (doorSlider != null && Mathf.RoundToInt(doorSlider.value) != customCount)
                doorSlider.SetValueWithoutNotify(customCount);

            if (customCountText != null)
                customCountText.text = "Doors: " + customCount;

            if (customOddsText != null)
                customOddsText.text = "Stay:\n" + MontyHallRound.StayPercent(customCount) + "\n\nSwitch:\n" + MontyHallRound.SwitchPercent(customCount);
        }

        void BuildDoors()
        {
            ClearDoors();
            ConfigureGrid(doorCount);

            if (doorPrefab == null || doorContent == null)
            {
                Debug.LogError("[Mind Paradox] Monty Hall 문 프리팹 또는 배치 영역이 없습니다.");
                return;
            }

            for (int i = 0; i < doorCount; i++)
            {
                MontyHallDoorUI door = Instantiate(doorPrefab, doorContent);
                door.name = "Door" + (i + 1);
                door.Setup(i, doorCount);
                door.Clicked += OnDoorClicked;
                doors.Add(door);
            }
        }

        void ConfigureGrid(int count)
        {
            if (doorGrid == null)
                return;

            int columns = count <= 3 ? 3 : count <= 20 ? 5 : 10;
            float cell = count <= 3 ? 280f : count <= 10 ? 168f : count <= 20 ? 150f : 88f;
            float spacing = count <= 3 ? 16f : 8f;
            doorGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            doorGrid.constraintCount = columns;
            doorGrid.cellSize = new Vector2(cell, count <= 3 ? 300f : cell);
            doorGrid.spacing = new Vector2(spacing, spacing);
        }

        void ClearDoors()
        {
            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i] == null)
                    continue;

                doors[i].Clicked -= OnDoorClicked;
                Destroy(doors[i].gameObject);
            }

            doors.Clear();

            if (doorContent == null)
                return;

            for (int i = doorContent.childCount - 1; i >= 0; i--)
                Destroy(doorContent.GetChild(i).gameObject);
        }

        void OnDoorClicked(int doorIndex)
        {
            if (round == null || round.Phase != MontyHallPhase.ChooseDoor || round.IsOpened(doorIndex))
                return;

            round.ChooseDoor(doorIndex);
            if (round.IsOpened(round.CarDoor) || round.IsOpened(round.PlayerDoor) || round.OpenedCount != round.DoorCount - 2)
                Debug.LogError("[Mind Paradox] 진행자가 닫아 두어야 할 문을 열었습니다.");

            Refresh();
        }

        void OnStay()
        {
            if (round == null || round.Phase != MontyHallPhase.ChooseStayOrSwitch)
                return;

            round.Stay();
            Record(true);
            Refresh();
        }

        void OnSwitch()
        {
            if (round == null || round.Phase != MontyHallPhase.ChooseStayOrSwitch)
                return;

            round.Switch();
            Record(false);
            Refresh();
        }

        void Record(bool stayed)
        {
            if (!records.TryGetValue(doorCount, out Tally tally))
            {
                tally = new Tally();
                records.Add(doorCount, tally);
            }

            tally.Plays++;
            if (stayed)
            {
                tally.Stay++;
                if (round.Won)
                    tally.StayWins++;
            }
            else
            {
                tally.Switch++;
                if (round.Won)
                    tally.SwitchWins++;
            }
        }

        void ReturnToCategory()
        {
            if (string.IsNullOrEmpty(CategorySession.SelectedCategoryId))
                CategorySession.SelectedCategoryId = ProbabilityCategoryId;

            if (!Application.CanStreamedLevelBeLoaded(CategorySession.SceneName))
            {
                Debug.LogWarning("[Mind Paradox] Build Settings에서 CategoryScene을 찾지 못했습니다.");
                return;
            }

            SceneManager.LoadScene(CategorySession.SceneName);
        }

        void Refresh()
        {
            if (round == null)
                return;

            bool choosing = round.Phase == MontyHallPhase.ChooseDoor;
            bool deciding = round.Phase == MontyHallPhase.ChooseStayOrSwitch;
            bool finished = round.Phase == MontyHallPhase.Result;
            string stay = MontyHallRound.StayPercent(doorCount);
            string change = MontyHallRound.SwitchPercent(doorCount);

            if (descriptionText != null)
                descriptionText.text = doorCount + " Doors";

            if (stayButton != null)
                stayButton.interactable = deciding;

            if (switchButton != null)
                switchButton.interactable = deciding;

            if (replayButton != null)
                replayButton.interactable = finished;

            if (oddsText != null)
                oddsText.text = doorCount + " Doors\n\nSTAY\n" + stay + "\n\nSWITCH\n" + change;

            if (messageText != null)
                messageText.text = Message(choosing, deciding, stay, change);

            if (resultText != null)
            {
                resultText.gameObject.SetActive(finished);
                if (finished)
                {
                    string outcome = round.Won ? "성공\n자동차를 찾았습니다." : "실패\n염소를 선택했습니다.";
                    resultText.text = outcome + "\n\nSTAY " + stay + "\nSWITCH " + change;
                    if (doorCount == 3)
                        resultText.text += "\n\n" + ThreeDoorLesson;
                }
            }

            if (statsText != null)
                statsText.text = StatsLine();

            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i] == null)
                    continue;

                MontyHallDoorUI.View view = ViewFor(i, choosing, deciding, finished);
                bool canClick = choosing && view == MontyHallDoorUI.View.Closed;
                doors[i].Show(view, canClick);
            }
        }

        string Message(bool choosing, bool deciding, string stay, string change)
        {
            if (choosing)
                return doorCount + "개의 문 중 처음부터 자동차를 고를 확률은 " + stay + "입니다.\n문 하나를 고르세요.";

            if (deciding)
                return "진행자가 염소가 있는 문 " + round.OpenedCount + "개를 열었습니다.\n처음 선택을 유지하면 " + stay + ",\n남은 문으로 바꾸면 " + change + "입니다.";

            if (round.Stayed)
                return "처음 고른 문을 유지했습니다.";

            return "남은 문으로 변경했습니다.";
        }

        string StatsLine()
        {
            if (!records.TryGetValue(doorCount, out Tally tally))
                tally = new Tally();

            return doorCount + "문\n플레이 " + tally.Plays + "\n유지 " + tally.Stay + "회, 성공 " + tally.StayWins + "\n변경 " + tally.Switch + "회, 성공 " + tally.SwitchWins;
        }

        MontyHallDoorUI.View ViewFor(int index, bool choosing, bool deciding, bool finished)
        {
            if (choosing || round == null)
                return MontyHallDoorUI.View.Closed;

            if (deciding)
            {
                if (round.IsOpened(index))
                    return MontyHallDoorUI.View.OpenedGoat;

                if (index == round.PlayerDoor)
                    return MontyHallDoorUI.View.Selected;

                return MontyHallDoorUI.View.Closed;
            }

            if (finished && index == round.CarDoor)
                return MontyHallDoorUI.View.Car;

            return MontyHallDoorUI.View.Goat;
        }

        sealed class Tally
        {
            public int Plays;
            public int Stay;
            public int StayWins;
            public int Switch;
            public int SwitchWins;
        }
    }
}
