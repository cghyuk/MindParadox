using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.Games.BertrandBox
{
    /// <summary>
    /// 상자 하나의 이미지와 글자. 어떤 동전이 들었는지는 알지 못한다.
    /// </summary>
    public class BertrandBoxUI : MonoBehaviour
    {
        static readonly Color NormalColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color SelectedColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color DarkText = new Color32(0x15, 0x20, 0x33, 0xFF);
        static readonly Color GoldColor = new Color32(0xE2, 0xC0, 0x56, 0xFF);
        static readonly Color SilverColor = new Color32(0xC5, 0xCD, 0xD6, 0xFF);

        [SerializeField] Image face;
        [SerializeField] Image coinImage;
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text coinText;
        [SerializeField] Button button;

        public event Action<BertrandBoxUI> Clicked;

        void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnClicked);
        }

        public void SetName(string boxName)
        {
            if (nameText != null)
                nameText.text = boxName;
        }

        public void SetInteractable(bool value)
        {
            if (button != null)
                button.interactable = value;
        }

        public void HideContents()
        {
            if (face != null)
                face.color = NormalColor;

            if (nameText != null)
                nameText.color = DarkText;

            if (coinImage != null)
                coinImage.gameObject.SetActive(false);

            if (coinText != null)
            {
                coinText.text = "";
                coinText.gameObject.SetActive(false);
            }
        }

        public void ShowCoin(string label, bool gold)
        {
            if (face != null)
                face.color = SelectedColor;

            if (nameText != null)
                nameText.color = Color.white;

            if (coinImage != null)
            {
                coinImage.gameObject.SetActive(true);
                coinImage.color = gold ? GoldColor : SilverColor;
            }

            if (coinText != null)
            {
                coinText.gameObject.SetActive(true);
                coinText.color = Color.white;
                coinText.text = label;
            }
        }

        public void ShowDetail(string detail)
        {
            if (coinText != null)
            {
                coinText.gameObject.SetActive(true);
                coinText.color = Color.white;
                coinText.text = detail;
            }
        }

        void OnClicked()
        {
            Clicked?.Invoke(this);
        }
    }
}
