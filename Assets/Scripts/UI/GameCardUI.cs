using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MindParadox.UI
{
    /// <summary>
    /// 카테고리 화면의 문제 카드. 제목, 설명, 이후 이동할 씬 이름을 설정한다.
    /// </summary>
    public class GameCardUI : MonoBehaviour
    {
        const float CardHeight = 464f;
        const float TitleHeight = 144f;
        const float DescriptionHeight = 176f;

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Button playButton;
        [SerializeField] Image iconImage;

        [SerializeField] string playLogMessage = "Open Game";
        [SerializeField] string targetSceneName = "";

        public string TargetSceneName => targetSceneName;

        void Awake()
        {
            Tighten();
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
        }

        public void SetStatus(string status)
        {
            if (statusText == null)
                return;

            bool show = !string.IsNullOrEmpty(status);
            statusText.gameObject.SetActive(show);
            statusText.text = show ? status : "";
            LayoutElement element = statusText.GetComponent<LayoutElement>();
            if (element != null)
                element.preferredHeight = show ? 56f : 0f;
        }

        public void Setup(string title, string description, string logMessage, string sceneName = "", Sprite icon = null)
        {
            if (titleText != null)
                titleText.text = title;

            if (descriptionText != null)
                descriptionText.text = description;

            playLogMessage = logMessage;
            targetSceneName = sceneName;

            ApplyIcon(icon);
            ApplyCardHeight();
        }

        void ApplyIcon(Sprite icon)
        {
            if (iconImage == null)
                return;

            bool show = icon != null;
            if (show)
            {
                iconImage.sprite = icon;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                iconImage.color = Color.white;
            }

            iconImage.gameObject.SetActive(show);
            Transform row = iconImage.transform.parent;
            if (row != null && row != transform && row.name == "IconRow")
                row.gameObject.SetActive(show);
        }

        public void OnPlayClicked()
        {
            Debug.Log(playLogMessage);

            if (string.IsNullOrEmpty(targetSceneName))
                return;

            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogWarning($"[Mind Paradox] Build Settings에서 '{targetSceneName}' 씬을 찾지 못했습니다.");
                return;
            }

            SceneManager.LoadScene(targetSceneName);
        }

        void Tighten()
        {
            ApplyCardHeight();

            VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
            if (layout != null)
                layout.enabled = false;

            Transform content = transform.Find("ContentArea");
            if (content != null)
            {
                VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
                if (contentLayout != null)
                    contentLayout.enabled = true;
            }

            if (titleText != null)
            {
                titleText.fontSize = 60f;
                titleText.enableAutoSizing = false;
                titleText.textWrappingMode = TextWrappingModes.Normal;
                titleText.overflowMode = TextOverflowModes.Ellipsis;
            }

            SetHeight(titleText, TitleHeight);
            SetHeight(descriptionText, DescriptionHeight);
            if (descriptionText != null)
            {
                descriptionText.fontSize = 50f;
                descriptionText.enableAutoSizing = false;
                descriptionText.textWrappingMode = TextWrappingModes.Normal;
                descriptionText.overflowMode = TextOverflowModes.Ellipsis;
            }

            IgnoreGap("TitleDescriptionGap");
            IgnoreGap("DescriptionButtonGap");
            EnsureStatus();
        }

        void ApplyCardHeight()
        {
            float height = CardHeight;

            LayoutElement card = GetComponent<LayoutElement>();
            if (card != null)
            {
                card.preferredHeight = height;
                card.minHeight = height - 20f;
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }

        void EnsureStatus()
        {
            if (statusText != null || descriptionText == null)
                return;

            var statusObject = new GameObject("StatusText", typeof(RectTransform));
            statusObject.transform.SetParent(transform, false);
            statusObject.transform.SetSiblingIndex(descriptionText.transform.GetSiblingIndex() + 1);
            statusText = statusObject.AddComponent<TextMeshProUGUI>();
            if (descriptionText.font != null)
                statusText.font = descriptionText.font;
            statusText.fontSize = 48f;
            statusText.enableAutoSizing = false;
            statusText.fontStyle = FontStyles.Bold;
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = descriptionText.color;
            statusText.textWrappingMode = TextWrappingModes.NoWrap;
            statusText.overflowMode = TextOverflowModes.Ellipsis;
            statusText.raycastTarget = false;
            var element = statusObject.AddComponent<LayoutElement>();
            element.preferredHeight = 0f;
            element.minHeight = 0f;
            element.flexibleHeight = 0f;
            statusObject.SetActive(false);
        }

        static void SetHeight(TMP_Text text, float height)
        {
            if (text == null)
                return;

            LayoutElement element = text.GetComponent<LayoutElement>();
            if (element == null)
                return;

            element.minWidth = 0f;
            element.flexibleWidth = 1f;
            element.layoutPriority = 2;
            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
        }

        void IgnoreGap(string gapName)
        {
            Transform gap = transform.Find(gapName);
            if (gap == null)
                return;

            LayoutElement element = gap.GetComponent<LayoutElement>();
            if (element != null)
                element.ignoreLayout = true;
            gap.gameObject.SetActive(false);
        }
    }
}
