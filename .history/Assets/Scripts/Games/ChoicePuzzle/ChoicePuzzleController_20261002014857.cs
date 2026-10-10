using MindParadox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.Games.ChoicePuzzle
{
    /// <summary>
    /// 질문, 선택지, 결과, 다시 하기, 선택적 실험을 공통으로 그린다.
    /// </summary>
    public class ChoicePuzzleController : MonoBehaviour
    {
        const string ProbabilityCategoryId = "probability";

        [SerializeField] string puzzleId;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text situationText;
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text boardText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] RectTransform choiceRoot;
        [SerializeField] Button choiceTemplate;
        [SerializeField] RectTransform experimentRoot;
        [SerializeField] Button experimentTemplate;
        [SerializeField] Button backButton;
        [SerializeField] Button replayButton;
        [SerializeField] Button nextButton;

        ChoicePuzzleModel model;
        int stepIndex;
        bool answered;

        void Awake()
        {
            model = ChoicePuzzleCatalog.Create(puzzleId);
            if (titleText != null && model != null)
                titleText.text = model.Title;
            if (situationText != null && model != null)
                situationText.text = model.Situation;
            if (resultText != null)
            {
                resultText.richText = true;
                resultText.text = "";
            }

            if (boardText != null)
                boardText.richText = true;

            if (choiceTemplate != null)
                choiceTemplate.gameObject.SetActive(false);
            if (experimentTemplate != null)
                experimentTemplate.gameObject.SetActive(false);

            SetLabel(replayButton, "다시 하기");
            SetLabel(nextButton, "다음 질문");
            Wire(backButton, ReturnToCategory);
            Wire(replayButton, Replay);
            Wire(nextButton, NextStep);
            BuildExperiments();
        }

        void Start()
        {
            if (model == null)
            {
                Debug.LogError("[Mind Paradox] 선택형 퍼즐을 찾지 못했습니다. " + puzzleId);
                return;
            }

            ShowStep();
            RefreshReadouts();
        }

        void ShowStep()
        {
            answered = false;
            ChoiceStep step = CurrentStep();
            if (questionText != null)
                questionText.text = step != null ? step.Question : "";
            if (resultText != null)
                resultText.text = "";

            BuildChoices(step);
            if (replayButton != null)
                replayButton.gameObject.SetActive(false);
            if (nextButton != null)
                nextButton.gameObject.SetActive(false);
        }

        void Answer(int index)
        {
            if (answered || model == null)
                return;

            ChoiceStep step = CurrentStep();
            if (step == null)
                return;

            answered = true;
            SetChoicesInteractable(false);
            bool correct = index == step.CorrectIndex;
            if (resultText != null)
            {
                resultText.text =
                    "<size=60>" + (correct ? "정답" : "오답") + "</size>\n" +
                    "<size=50>" + step.Reason + "</size>";
            }

            bool hasNext = stepIndex + 1 < model.Steps.Length;
            if (nextButton != null)
                nextButton.gameObject.SetActive(hasNext);
            if (replayButton != null)
                replayButton.gameObject.SetActive(!hasNext);
        }

        void NextStep()
        {
            if (model == null || stepIndex + 1 >= model.Steps.Length)
                return;

            stepIndex++;
            ShowStep();
        }

        void Replay()
        {
            stepIndex = 0;
            if (model != null && model.ResetSession != null)
                model.ResetSession();
            ShowStep();
            RefreshReadouts();
        }

        void RefreshReadouts()
        {
            if (boardText != null && model != null && model.Board != null)
                boardText.text = model.Board();
            if (statsText != null && model != null && model.Stats != null)
                statsText.text = model.Stats();
        }

        ChoiceStep CurrentStep()
        {
            if (model == null || model.Steps == null || model.Steps.Length == 0)
                return null;
            if (stepIndex < 0 || stepIndex >= model.Steps.Length)
                return model.Steps[0];
            return model.Steps[stepIndex];
        }

        void BuildChoices(ChoiceStep step)
        {
            if (choiceRoot == null || choiceTemplate == null)
                return;

            for (int i = choiceRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = choiceRoot.GetChild(i);
                if (child.gameObject == choiceTemplate.gameObject)
                    continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            if (step == null || step.Choices == null)
                return;

            for (int i = 0; i < step.Choices.Length; i++)
            {
                int index = i;
                Button button = Instantiate(choiceTemplate, choiceRoot);
                button.gameObject.SetActive(true);
                button.name = "ChoiceButton";
                button.transform.SetSiblingIndex(i);
                SetLabel(button, step.Choices[i]);
                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Answer(index));
            }
        }

        void BuildExperiments()
        {
            if (experimentRoot == null || experimentTemplate == null || model == null || model.Experiments == null)
                return;

            for (int i = 0; i < model.Experiments.Length; i++)
            {
                ChoiceExperiment experiment = model.Experiments[i];
                if (experiment == null)
                    continue;

                Button button = Instantiate(experimentTemplate, experimentRoot);
                button.gameObject.SetActive(true);
                button.name = "ExperimentButton";
                SetLabel(button, experiment.Label);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => RunExperiment(experiment));
            }
        }

        void RunExperiment(ChoiceExperiment experiment)
        {
            if (experiment == null || experiment.Run == null)
                return;

            experiment.Run();
            RefreshReadouts();
        }

        void SetChoicesInteractable(bool enabled)
        {
            if (choiceRoot == null)
                return;

            for (int i = 0; i < choiceRoot.childCount; i++)
            {
                Button button = choiceRoot.GetChild(i).GetComponent<Button>();
                if (button == null || button == choiceTemplate)
                    continue;
                button.interactable = enabled;
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

        static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        static void SetLabel(Button button, string value)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = value;
        }
    }
}
