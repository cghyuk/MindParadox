using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 메인 메뉴의 미니게임 카드. 제목, 설명, 이후 이동할 씬 이름을 설정한다.
    /// </summary>
    public class GameCardUI : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] Button playButton;
        [SerializeField] Image iconImage;

        [SerializeField] string playLogMessage = "Open Game";
        [SerializeField] string targetSceneName = "";

        public string TargetSceneName => targetSceneName;

        void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
        }

        public void Setup(string title, string description, string logMessage, string sceneName = "", Sprite icon = null)
        {
            if (titleText != null)
                titleText.text = title;

            if (descriptionText != null)
                descriptionText.text = description;

            playLogMessage = logMessage;
            targetSceneName = sceneName;

            if (icon != null && iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.gameObject.SetActive(true);
            }
        }

        public void OnPlayClicked()
        {
            Debug.Log(playLogMessage);
        }
    }
}
