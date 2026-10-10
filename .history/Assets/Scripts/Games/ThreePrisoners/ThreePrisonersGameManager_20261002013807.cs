using MindParadox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.Games.ThreePrisoners
{
    /// <summary>
    /// 간수가 B를 말한 뒤 A의 사면 확률을 고르고, 같은 규칙으로 100회를 집계한다.
    /// </summary>
    public class ThreePrisonersGameManager : MonoBehaviour
    {
        const string ProbabilityCategoryId = "probability";
        const int CorrectChoice = 1;

        static readonly Color CardColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color NamedColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color CardTitleColor = new Color32(0x15, 0x20, 0x33, 0xFF);

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text situationText;
        [SerializeField] TMP_Text wardenText;
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] Image prisonerA;
        [SerializeField] Image prisonerB;
        [SerializeField] Image prisonerC;
        [SerializeField] TMP_Text prisonerALabel;
        [SerializeField] TMP_Text prisonerBLabel;
        [SerializeField] TMP_Text prisonerCLabel;
        [SerializeField] Button backButton;
        [SerializeField] Button choiceHalfButton;
        [SerializeField] Button choiceThirdButton;
        [SerializeField] Button choiceTwoThirdsButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button simulateButton;

        readonly System.Random random = new System.Random();

        int conditionedRounds;
        int aPardonedRounds;
        bool answered;

        void Awake()
        {
            if (titleText != null)
                titleText.text = ThreePrisonersCopy.Title;
            if (situationText != null)
                situationText.text = ThreePrisonersCopy.Situation;
            if (wardenText != null)
                wardenText.text = ThreePrisonersCopy.WardenLine;
            if (questionText != null)
                questionText.text = ThreePrisonersCopy.Question;

            if (resultText != null)
            {
                resultText.richText = true;
                resultText.text = "";
            }

            SetLabel(prisonerALabel, ThreePrisonersCopy.PrisonerA);
            SetLabel(prisonerBLabel, ThreePrisonersCopy.PrisonerB);
            SetLabel(prisonerCLabel, ThreePrisonersCopy.PrisonerC);
            PaintPrisoners();

            SetButtonLabel(choiceHalfButton, ThreePrisonersCopy.ChoiceHalf);
            SetButtonLabel(choiceThirdButton, ThreePrisonersCopy.ChoiceThird);
            SetButtonLabel(choiceTwoThirdsButton, ThreePrisonersCopy.ChoiceTwoThirds);
            SetButtonLabel(replayButton, ThreePrisonersCopy.Replay);
            SetButtonLabel(simulateButton, ThreePrisonersCopy.Simulate);

            Wire(backButton, ReturnToCategory);
            Wire(choiceHalfButton, () => Answer(0));
            Wire(choiceThirdButton, () => Answer(CorrectChoice));
            Wire(choiceTwoThirdsButton, () => Answer(2));
            Wire(replayButton, Replay);
            Wire(simulateButton, RunHundred);
        }

        void Start()
        {
            ShowQuestion();
            RefreshStats();
        }

        void ShowQuestion()
        {
            answered = false;
            SetChoices(true);
            if (resultText != null)
                resultText.text = "";
            SetActions(false);
        }

        void Answer(int choice)
        {
            if (answered)
                return;

            answered = true;
            SetChoices(false);
            bool correct = choice == CorrectChoice;
            if (resultText != null)
            {
                resultText.text =
                    "<size=60>" + (correct ? ThreePrisonersCopy.Correct : ThreePrisonersCopy.Wrong) + "</size>\n" +
                    "<size=50>" + ThreePrisonersCopy.Reason + "</size>";
            }

            SetActions(true);
        }

        void Replay()
        {
            ShowQuestion();
        }

        void RunHundred()
        {
            ThreePrisonersRules.RunBatch(100, random, out int conditioned, out int aPardoned);
            conditionedRounds += conditioned;
            aPardonedRounds += aPardoned;
            RefreshStats();
        }

        void RefreshStats()
        {
            if (statsText == null)
                return;

            statsText.text =
                "조건에 맞는 라운드 " + conditionedRounds + "\n" +
                "A 사면 " + aPardonedRounds + "\n" +
                "Observed: " + ThreePrisonersRules.FormatObserved(aPardonedRounds, conditionedRounds) + "\n" +
                "Theory: " + ThreePrisonersCopy.Theory;
        }

        void PaintPrisoners()
        {
            Paint(prisonerA, prisonerALabel, false);
            Paint(prisonerB, prisonerBLabel, true);
            Paint(prisonerC, prisonerCLabel, false);
        }

        static void Paint(Image face, TMP_Text label, bool named)
        {
            if (face != null)
                face.color = named ? NamedColor : CardColor;
            if (label != null)
                label.color = named ? Color.white : CardTitleColor;
        }

        void SetActions(bool visible)
        {
            if (replayButton != null && replayButton.transform.parent != null)
                replayButton.transform.parent.gameObject.SetActive(visible);

            if (replayButton != null)
                replayButton.gameObject.SetActive(visible);
            if (simulateButton != null)
                simulateButton.gameObject.SetActive(visible);
        }

        void SetChoices(bool enabled)
        {
            if (choiceHalfButton != null)
                choiceHalfButton.interactable = enabled;
            if (choiceThirdButton != null)
                choiceThirdButton.interactable = enabled;
            if (choiceTwoThirdsButton != null)
                choiceTwoThirdsButton.interactable = enabled;
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
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        static void SetLabel(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value;
        }

        static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = value;
        }
    }
}
