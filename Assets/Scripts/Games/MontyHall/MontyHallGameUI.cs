using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.Games.MontyHall
{
    /// <summary>
    /// 3문 몬티홀 화면. 문 선택, 염소 공개, 유지/변경, 결과, 다시 하기, 메인 메뉴.
    /// </summary>
    public class MontyHallGameUI : MonoBehaviour
    {
        static readonly Color CardColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color TitleColor = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color SelectedColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color RevealedColor = new Color32(0x9A, 0xA4, 0xB5, 0xFF);

        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] Button[] doorButtons;
        [SerializeField] TMP_Text[] doorLabels;
        [SerializeField] Image[] doorImages;
        [SerializeField] GameObject choiceRow;
        [SerializeField] Button stayButton;
        [SerializeField] Button switchButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button menuButton;
        [SerializeField] string mainSceneName = "MainScene";

        MontyHallRound round;

        void Awake()
        {
            if (doorButtons != null)
            {
                for (int i = 0; i < doorButtons.Length; i++)
                {
                    int doorIndex = i;
                    if (doorButtons[i] != null)
                        doorButtons[i].onClick.AddListener(() => OnDoorClicked(doorIndex));
                }
            }

            if (stayButton != null)
                stayButton.onClick.AddListener(OnStay);

            if (switchButton != null)
                switchButton.onClick.AddListener(OnSwitch);

            if (replayButton != null)
                replayButton.onClick.AddListener(StartRound);

            if (menuButton != null)
                menuButton.onClick.AddListener(ReturnToMenu);
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
            if (round == null)
                return;

            round.ChooseDoor(doorIndex);
            Refresh();
        }

        void OnStay()
        {
            if (round == null)
                return;

            round.Stay();
            Refresh();
        }

        void OnSwitch()
        {
            if (round == null)
                return;

            round.Switch();
            Refresh();
        }

        void ReturnToMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
            {
                Debug.LogError($"[Mind Paradox] Build Settings에서 '{mainSceneName}' 씬을 찾지 못했습니다.");
                return;
            }

            SceneManager.LoadScene(mainSceneName);
        }

        void Refresh()
        {
            if (round == null || doorButtons == null || doorLabels == null || doorImages == null)
                return;

            bool choosing = round.Phase == MontyHallPhase.ChooseDoor;
            bool deciding = round.Phase == MontyHallPhase.ChooseStayOrSwitch;
            bool finished = round.Phase == MontyHallPhase.Result;

            if (choiceRow != null)
                choiceRow.SetActive(deciding);

            if (resultText != null)
                resultText.gameObject.SetActive(finished);

            if (replayButton != null)
                replayButton.gameObject.SetActive(finished);

            if (statusText != null)
            {
                if (choosing)
                    statusText.text = "문 하나를 고르세요.\n자동차는 세 문 중 하나에만 있습니다.";
                else if (deciding)
                    statusText.text = $"{round.RevealedDoor + 1}번 문에서 염소가 나왔습니다.\n{round.PlayerDoor + 1}번 문을 유지할까요, 바꿀까요?";
                else if (round.Stayed)
                    statusText.text = "처음 고른 문을 유지했습니다.";
                else
                    statusText.text = "다른 문으로 바꿨습니다.";
            }

            if (finished && resultText != null)
                resultText.text = round.Won ? "결과: 자동차" : "결과: 염소";

            int count = Mathf.Min(doorButtons.Length, Mathf.Min(doorLabels.Length, doorImages.Length));
            for (int i = 0; i < count; i++)
            {
                if (doorButtons[i] != null)
                    doorButtons[i].interactable = choosing;

                string caption = (i + 1).ToString();
                Color fill = CardColor;
                Color label = TitleColor;

                if (deciding)
                {
                    if (i == round.RevealedDoor)
                    {
                        caption = (i + 1) + "\n염소";
                        fill = RevealedColor;
                    }
                    else if (i == round.PlayerDoor)
                    {
                        caption = (i + 1) + "\n선택";
                        fill = SelectedColor;
                        label = Color.white;
                    }
                }
                else if (finished)
                {
                    bool car = i == round.CarDoor;
                    caption = (i + 1) + (car ? "\n자동차" : "\n염소");
                    if (i == round.FinalDoor)
                    {
                        fill = SelectedColor;
                        label = Color.white;
                    }
                }

                if (doorLabels[i] != null)
                {
                    doorLabels[i].text = caption;
                    doorLabels[i].color = label;
                }

                if (doorImages[i] != null)
                    doorImages[i].color = fill;
            }
        }
    }
}
