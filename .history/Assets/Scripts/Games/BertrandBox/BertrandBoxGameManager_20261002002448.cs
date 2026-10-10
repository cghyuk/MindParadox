using System;
using MindParadox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.Games.BertrandBox
{
    /// <summary>
    /// 퀴즈, 상자 선택, 100회 실험, 이번 실행의 기록.
    /// </summary>
    public class BertrandBoxGameManager : MonoBehaviour
    {
        const string ProbabilityCategoryId = "probability";
        const int CorrectChoice = 1;

        enum Phase
        {
            Quiz,
            Explained,
            Pick,
            Silver,
            GoldQuestion
        }

        [SerializeField] BertrandBoxUI[] boxes;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text promptText;
        [SerializeField] TMP_Text explainText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] GameObject choiceArea;
        [SerializeField] GameObject explainArea;
        [SerializeField] Button backButton;
        [SerializeField] Button choiceHalfButton;
        [SerializeField] Button choiceTwoThirdsButton;
        [SerializeField] Button choiceThirdButton;
        [SerializeField] Button redrawButton;
        [SerializeField] Button experimentButton;
        [SerializeField] Button simulateButton;

        readonly System.Random random = new System.Random();
        readonly BertrandBoxKind[] kinds = new BertrandBoxKind[3];

        Phase phase = Phase.Quiz;
        BertrandDraw pending;
        bool hasPending;
        bool roundDrawn;
        int selectedIndex = -1;
        int quizAttempts;
        int quizCorrect;
        int simulationDraws;
        int goldObserved;
        int otherGold;

        void Awake()
        {
            if (titleText != null)
                titleText.text = BertrandBoxCopy.Title;

            SetButtonLabel(choiceHalfButton, BertrandBoxCopy.ChoiceHalf);
            SetButtonLabel(choiceTwoThirdsButton, BertrandBoxCopy.ChoiceTwoThirds);
            SetButtonLabel(choiceThirdButton, BertrandBoxCopy.ChoiceThird);
            SetButtonLabel(redrawButton, BertrandBoxCopy.Redraw);
            SetButtonLabel(experimentButton, BertrandBoxCopy.Experiment);
            SetButtonLabel(simulateButton, BertrandBoxCopy.Simulate);

            if (boxes != null)
            {
                string[] names = { BertrandBoxCopy.BoxA, BertrandBoxCopy.BoxB, BertrandBoxCopy.BoxC };
                for (int i = 0; i < boxes.Length && i < names.Length; i++)
                {
                    if (boxes[i] == null)
                        continue;

                    boxes[i].SetName(names[i]);
                    boxes[i].Clicked += OnBoxClicked;
                }
            }

            Wire(backButton, ReturnToCategory);
            Wire(choiceHalfButton, () => Answer(0));
            Wire(choiceTwoThirdsButton, () => Answer(CorrectChoice));
            Wire(choiceThirdButton, () => Answer(2));
            Wire(redrawButton, BeginPick);
            Wire(experimentButton, BeginPick);
            Wire(simulateButton, RunBatch);
        }

        void Start()
        {
            ShowQuiz();
        }

        void ShowQuiz()
        {
            phase = Phase.Quiz;
            hasPending = false;
            roundDrawn = false;
            selectedIndex = -1;
            HideBoxes();
            SetBoxesInteractable(false);
            SetPrompt(BertrandBoxCopy.QuizPrompt);
            SetExplain("", false);
            SetResult("");
            SetChoices(true);
            SetActions(false, false, false);
            RefreshStats();
        }

        void Answer(int choice)
        {
            if (phase != Phase.Quiz && phase != Phase.GoldQuestion)
                return;

            quizAttempts++;
            bool correct = choice == CorrectChoice;
            if (correct)
                quizCorrect++;

            if (hasPending)
            {
                RevealPending();
                hasPending = false;
            }

            phase = Phase.Explained;
            SetPrompt(correct ? BertrandBoxCopy.Correct : BertrandBoxCopy.Wrong);
            SetExplain(BertrandBoxCopy.Explanation, true);
            SetChoices(false);
            SetBoxesInteractable(false);
            SetActions(roundDrawn, !roundDrawn, true);
            RefreshStats();
        }

        void BeginPick()
        {
            phase = Phase.Pick;
            hasPending = false;
            roundDrawn = false;
            selectedIndex = -1;
            ShuffleKinds();
            HideBoxes();
            SetBoxesInteractable(true);
            SetPrompt(BertrandBoxCopy.PickPrompt);
            SetExplain("", false);
            SetResult("");
            SetChoices(false);
            SetActions(false, false, true);
            RefreshStats();
        }

        void OnBoxClicked(BertrandBoxUI box)
        {
            if (phase != Phase.Pick || boxes == null)
                return;

            int index = IndexOf(box);
            if (index < 0)
                return;

            roundDrawn = true;
            selectedIndex = index;
            simulationDraws++;
            BertrandDraw draw = BertrandBoxSimulator.Draw(kinds[index], random);
            SetBoxesInteractable(false);

            if (draw.Drawn == BertrandCoin.Gold)
            {
                goldObserved++;
                if (draw.Remaining == BertrandCoin.Gold)
                    otherGold++;

                pending = draw;
                hasPending = true;
                phase = Phase.GoldQuestion;
                box.ShowCoin(BertrandBoxCopy.Gold, true);
                SetPrompt(BertrandBoxCopy.GoldPrompt);
                SetChoices(true);
                SetActions(false, false, true);
            }
            else
            {
                phase = Phase.Silver;
                box.ShowCoin(BertrandBoxCopy.Silver, false);
                box.ShowDetail(BertrandBoxCopy.Silver + "\n" + KindLabel(draw.Box));
                SetPrompt(BertrandBoxCopy.SilverPrompt);
                SetChoices(false);
                SetActions(true, false, true);
            }

            RefreshStats();
        }

        void RunBatch()
        {
            BertrandBoxSimulator.RunDraws(BertrandBoxSimulator.BatchSize, random, out int gold, out int other);
            simulationDraws += BertrandBoxSimulator.BatchSize;
            goldObserved += gold;
            otherGold += other;
            SetResult(BertrandBoxCopy.Simulated);
            RefreshStats();
        }

        void RevealPending()
        {
            if (boxes == null || selectedIndex < 0 || selectedIndex >= boxes.Length)
                return;

            BertrandBoxUI box = boxes[selectedIndex];
            if (box == null)
                return;

            string other = pending.Remaining == BertrandCoin.Gold ? BertrandBoxCopy.Gold : BertrandBoxCopy.Silver;
            box.ShowDetail(BertrandBoxCopy.Gold + "\n" + BertrandBoxCopy.Remaining + " " + other + "\n" + KindLabel(pending.Box));
        }

        void ShuffleKinds()
        {
            kinds[0] = BertrandBoxKind.DoubleGold;
            kinds[1] = BertrandBoxKind.DoubleSilver;
            kinds[2] = BertrandBoxKind.Mixed;
            for (int i = kinds.Length - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                BertrandBoxKind held = kinds[i];
                kinds[i] = kinds[swap];
                kinds[swap] = held;
            }
        }

        void HideBoxes()
        {
            if (boxes == null)
                return;

            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] != null)
                    boxes[i].HideContents();
            }
        }

        void SetBoxesInteractable(bool value)
        {
            if (boxes == null)
                return;

            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] != null)
                    boxes[i].SetInteractable(value);
            }
        }

        int IndexOf(BertrandBoxUI box)
        {
            if (boxes == null)
                return -1;

            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] == box)
                    return i;
            }

            return -1;
        }

        static string KindLabel(BertrandBoxKind kind)
        {
            switch (kind)
            {
                case BertrandBoxKind.DoubleGold:
                    return BertrandBoxCopy.DoubleGold;
                case BertrandBoxKind.DoubleSilver:
                    return BertrandBoxCopy.DoubleSilver;
                default:
                    return BertrandBoxCopy.Mixed;
            }
        }

        void RefreshStats()
        {
            if (statsText == null)
                return;

            statsText.text =
                "Quiz: " + quizCorrect + " / " + quizAttempts + "\n" +
                "Simulations: " + simulationDraws + "\n" +
                "Gold Observed: " + goldObserved + "\n" +
                "Other Coin Gold: " + otherGold + "\n" +
                "Observed: " + BertrandBoxSimulator.FormatObserved(otherGold, goldObserved) + "\n" +
                "Theory: " + BertrandBoxSimulator.FormatPercent(BertrandBoxSimulator.Theory);
        }

        void SetPrompt(string value)
        {
            if (promptText != null)
                promptText.text = value;
        }

        void SetResult(string value)
        {
            if (resultText != null)
                resultText.text = value;
        }

        void SetExplain(string value, bool visible)
        {
            if (explainText != null)
                explainText.text = value;

            if (explainArea != null)
                explainArea.SetActive(visible);
        }

        void SetChoices(bool visible)
        {
            if (choiceArea != null)
                choiceArea.SetActive(visible);
        }

        void SetActions(bool redraw, bool experiment, bool simulate)
        {
            if (redrawButton != null)
                redrawButton.gameObject.SetActive(redraw);

            if (experimentButton != null)
                experimentButton.gameObject.SetActive(experiment);

            if (simulateButton != null)
                simulateButton.gameObject.SetActive(simulate);
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

        static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        static void SetButtonLabel(Button button, string label)
        {
            if (button == null)
                return;

            TMP_Text text = button.GetComponentInChildren<TMP_Text>();
            if (text != null)
                text.text = label;
        }
    }
}
