using MindParadox.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 헤더의 설명 버튼. 연결된 ExplanationData를 공통 팝업에 넘긴다.
    /// </summary>
    public class PuzzleExplanationButton : MonoBehaviour
    {
        [SerializeField] ExplanationData explanation;
        [SerializeField] PuzzleExplanationPopup popup;
        [SerializeField] Button button;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(Open);
        }

        public void Open()
        {
            if (popup != null)
                popup.Show(explanation);
        }
    }
}
