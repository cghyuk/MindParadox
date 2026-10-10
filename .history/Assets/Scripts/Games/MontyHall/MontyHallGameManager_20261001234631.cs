using MindParadox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.Games.MontyHall
{
    /// <summary>
    /// 문 3개 몬티홀 한 판의 진행, 결과, 이번 실행의 기록.
    /// </summary>
    public class MontyHallGameManager : MonoBehaviour
    {
        const string ProbabilityCategoryId = "probability";

        const string OddsText =
            "유지 승률:\n33.3%\n\n변경 승률:\n66.7%\n\n" +
            "처음 선택이 맞을 확률은 1/3이고,\n처음 선택이 틀릴 확률은 2/3이다.\n" +
            "진행자가 염소 문 하나를 제거하므로\n처음 선택이 틀렸던 2/3의 경우\n문을 바꾸면 자동차를 얻게 된다.";

        [SerializeField] MontyHallDoorUI[] doors;
        [SerializeField] TMP_Text messageText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] Button stayButton;
        [SerializeField] Button switchButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button backButton;

        MontyHallRound round;
        int plays;
        int stayCount;
        int stayWins;
        int switchCount;
        int switchWins;

        void Awake()
        {
            if (doors != null)
            {
                for (int i = 0; i < doors.Length; i++)
                {
                    if (doors[i] == null)
                        continue;

                    doors[i].Setup(i);
                    doors[i].Clicked += OnDoorClicked;
                }
            }

            if (stayButton != null)
                stayButton.onClick.AddListener(OnStay);

            if (switchButton != null)
                switchButton.onClick.AddListener(OnSwitch);

            if (replayButton != null)
                replayButton.onClick.AddListener(StartRound);

            if (backButton != null)
                backButton.onClick.AddListener(ReturnToCategory);
        }

        void Start()
        {
            StartRound();
        }

        public void StartRound()
        {
            round = new MontyHallRound();
            round.Begin();
            Refresh();
        }

        void OnDoorClicked(int doorIndex)
        {
            if (round == null || round.Phase != MontyHallPhase.ChooseDoor)
                return;

            round.ChooseDoor(doorIndex);
            if (round.RevealedDoor == round.CarDoor || round.RevealedDoor == round.PlayerDoor)
                Debug.LogError("[Mind Paradox] 진행자가 자동차 문이나 고른 문을 열었습니다.");

            Refresh();
        }

        void OnStay()
        {
            if (round == null || round.Phase != MontyHallPhase.ChooseStayOrSwitch)
                return;

            round.Stay();
            stayCount++;
            plays++;
            if (round.Won)
                stayWins++;

            Refresh();
        }

        void OnSwitch()
        {
            if (round == null || round.Phase != MontyHallPhase.ChooseStayOrSwitch)
                return;

            round.Switch();
            switchCount++;
            plays++;
            if (round.Won)
                switchWins++;

            Refresh();
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

            if (stayButton != null)
                stayButton.interactable = deciding;

            if (switchButton != null)
                switchButton.interactable = deciding;

            if (replayButton != null)
                replayButton.interactable = finished;

            if (messageText != null)
                messageText.text = Message(choosing, deciding);

            if (resultText != null)
            {
                resultText.gameObject.SetActive(finished);
                if (finished)
                    resultText.text = (round.Won ? "성공\n자동차를 찾았습니다." : "실패\n염소를 선택했습니다.") + "\n\n" + OddsText;
            }

            if (statsText != null)
            {
                statsText.text =
                    "플레이 " + plays + "\n" +
                    "유지 " + stayCount + "회, 성공 " + stayWins + "\n" +
                    "변경 " + switchCount + "회, 성공 " + switchWins;
            }

            if (doors == null)
                return;

            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null)
                    continue;

                doors[i].Show(ViewFor(i, choosing, deciding, finished), choosing);
            }
        }

        string Message(bool choosing, bool deciding)
        {
            if (choosing)
                return "문 하나를 고르세요.\n자동차는 세 문 중 하나에만 있습니다.";

            if (deciding)
                return (round.RevealedDoor + 1) + "번 문에서 염소가 나왔습니다.\n처음 선택을 유지하거나, 남은 문으로 바꿀 수 있습니다.";

            if (round.Stayed)
                return "처음 고른 문을 유지했습니다.";

            return "남은 문으로 변경했습니다.";
        }

        MontyHallDoorUI.View ViewFor(int index, bool choosing, bool deciding, bool finished)
        {
            if (choosing)
                return MontyHallDoorUI.View.Closed;

            if (deciding)
            {
                if (index == round.RevealedDoor)
                    return MontyHallDoorUI.View.OpenedGoat;

                if (index == round.PlayerDoor)
                    return MontyHallDoorUI.View.Selected;

                return MontyHallDoorUI.View.Closed;
            }

            if (finished && index == round.CarDoor)
                return MontyHallDoorUI.View.Car;

            return MontyHallDoorUI.View.Goat;
        }
    }
}
