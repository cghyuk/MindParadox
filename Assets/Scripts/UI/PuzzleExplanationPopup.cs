using MindParadox.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 모든 퍼즐이 같이 쓰는 설명 팝업. 열리고 닫혀도 게임 진행은 바꾸지 않는다.
    /// </summary>
    public class PuzzleExplanationPopup : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text explanationText;
        [SerializeField] TMP_Text formulaText;
        [SerializeField] Button closeButton;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] RectTransform contentRoot;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        void Update()
        {
            if (WasBackPressed())
                Hide();
        }

        public void Show(ExplanationData data)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (data == null)
            {
                Set(titleText, "설명");
                Set(summaryText, "");
                Set(explanationText, "이 문제의 설명이 아직 연결되지 않았습니다.");
                Set(formulaText, "");
            }
            else
            {
                Set(titleText, data.ResolvedTitle);
                Set(summaryText, data.ResolvedSummary);
                Set(explanationText, data.ResolvedBody);
                Set(formulaText, data.ResolvedFormula);
            }

            if (contentRoot != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        static void Set(TMP_Text label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static bool WasBackPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }
    }
}
