using System;
using MindParadox.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.Games.BirthdayParadox
{
    /// <summary>
    /// 인원 선택, 이론 확률, 1회/100회 실험, 이번 실행의 통계.
    /// </summary>
    public class BirthdayParadoxGameManager : MonoBehaviour
    {
        const string ProbabilityCategoryId = "probability";

        [SerializeField] PersonBirthdayItem personPrefab;
        [SerializeField] RectTransform birthdayContent;
        [SerializeField] ScrollRect birthdayScroll;
        [SerializeField] Slider peopleSlider;
        [SerializeField] TMP_Text peopleCountText;
        [SerializeField] TMP_Text peopleText;
        [SerializeField] TMP_Text probabilityText;
        [SerializeField] TMP_Text resultText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text explanationText;
        [SerializeField] Button backButton;
        [SerializeField] Button minusButton;
        [SerializeField] Button plusButton;
        [SerializeField] Button onceButton;
        [SerializeField] Button hundredButton;
        [SerializeField] Button resetButton;
        [SerializeField] Button quick10Button;
        [SerializeField] Button quick23Button;
        [SerializeField] Button quick30Button;
        [SerializeField] Button quick50Button;
        [SerializeField] Button quick70Button;

        readonly System.Random random = new System.Random();

        int people = BirthdayParadoxCalculator.DefaultPeople;
        int trials;
        int matches;

        void Awake()
        {
            if (peopleSlider != null)
            {
                peopleSlider.minValue = BirthdayParadoxCalculator.MinPeople;
                peopleSlider.maxValue = BirthdayParadoxCalculator.MaxPeople;
                peopleSlider.wholeNumbers = true;
                peopleSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            Wire(backButton, ReturnToCategory);
            Wire(minusButton, () => SetPeople(people - 1));
            Wire(plusButton, () => SetPeople(people + 1));
            Wire(onceButton, RunOnce);
            Wire(hundredButton, RunHundred);
            Wire(resetButton, ResetRecords);
            Wire(quick10Button, () => SetPeople(10));
            Wire(quick23Button, () => SetPeople(23));
            Wire(quick30Button, () => SetPeople(30));
            Wire(quick50Button, () => SetPeople(50));
            Wire(quick70Button, () => SetPeople(70));
        }

        void Start()
        {
            SetPeople(BirthdayParadoxCalculator.DefaultPeople);
        }

        void OnSliderChanged(float value)
        {
            SetPeople(Mathf.RoundToInt(value), fromSlider: true);
        }

        void SetPeople(int count, bool fromSlider = false)
        {
            people = BirthdayParadoxCalculator.ClampPeople(count);
            if (!fromSlider && peopleSlider != null)
                peopleSlider.SetValueWithoutNotify(people);

            RefreshPresentation();
        }

        void RunOnce()
        {
            int[] days = BirthdayParadoxCalculator.CreateBirthdays(people, random);
            bool shared = BirthdayParadoxCalculator.HasSharedBirthday(days);
            trials++;
            if (shared)
                matches++;

            ShowBirthdays(days);
            if (resultText != null)
                resultText.text = shared ? "같은 생일 있음" : "같은 생일 없음";

            RefreshPresentation();
        }

        void RunHundred()
        {
            int sharedCount = 0;
            for (int i = 0; i < 100; i++)
            {
                int[] days = BirthdayParadoxCalculator.CreateBirthdays(people, random);
                if (BirthdayParadoxCalculator.HasSharedBirthday(days))
                    sharedCount++;
            }

            trials += 100;
            matches += sharedCount;
            if (resultText != null)
                resultText.text = "100회 실험을 기록했습니다.";

            RefreshPresentation();
        }

        void ResetRecords()
        {
            trials = 0;
            matches = 0;
            ClearBirthdays();
            if (resultText != null)
                resultText.text = "";

            RefreshPresentation();
        }

        void ShowBirthdays(int[] days)
        {
            if (personPrefab == null || birthdayContent == null)
            {
                Debug.LogError("[Mind Paradox] Birthday Paradox 사람 항목 또는 목록 영역이 없습니다.");
                return;
            }

            ClearBirthdays();
            bool[] shared = BirthdayParadoxCalculator.DuplicateFlags(days);
            for (int i = 0; i < days.Length; i++)
            {
                BirthdayParadoxCalculator.GetMonthDay(days[i], out int month, out int day);
                PersonBirthdayItem item = Instantiate(personPrefab, birthdayContent);
                item.Bind(i + 1, days.Length, month, day, shared[i]);
            }

            Canvas.ForceUpdateCanvases();
            if (birthdayScroll != null)
                birthdayScroll.verticalNormalizedPosition = 1f;
        }

        void ClearBirthdays()
        {
            if (birthdayContent == null)
                return;

            for (int i = birthdayContent.childCount - 1; i >= 0; i--)
                DestroyImmediate(birthdayContent.GetChild(i).gameObject);
        }

        void RefreshPresentation()
        {
            if (peopleCountText != null)
                peopleCountText.text = people.ToString();

            if (peopleText != null)
                peopleText.text = "People: " + people;

            if (probabilityText != null)
                probabilityText.text = BirthdayParadoxCalculator.FormatPercent(BirthdayParadoxCalculator.MatchProbability(people));

            if (statsText != null)
            {
                statsText.text =
                    "Trials: " + trials + "\n" +
                    "Matches: " + matches + "\n" +
                    "Observed: " + BirthdayParadoxCalculator.FormatObservedPercent(matches, trials) + "\n\n" +
                    "Theory: " + BirthdayParadoxCalculator.FormatPercent(BirthdayParadoxCalculator.MatchProbability(people));
            }

            if (explanationText != null)
            {
                explanationText.text =
                    people + "명이 있으면\n" +
                    "서로 비교되는 사람 쌍은 " + BirthdayParadoxCalculator.PairCount(people) + "쌍입니다.\n\n" +
                    "그래서 생각보다 적은 인원에서도\n" +
                    "같은 생일이 나올 확률이 빠르게 높아집니다.";
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
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}
