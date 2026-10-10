using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MindParadox.Games.BirthdayParadox
{
    /// <summary>
    /// 한 사람의 번호와 생일. 같은 생일이 있으면 색으로 구분한다.
    /// </summary>
    public class PersonBirthdayItem : MonoBehaviour
    {
        static readonly Color NormalColor = new Color32(0xF4, 0xF6, 0xFA, 0xFF);
        static readonly Color SharedColor = new Color32(0x2B, 0x62, 0xE3, 0xFF);
        static readonly Color DarkText = new Color32(0x15, 0x20, 0x33, 0xFF);

        [SerializeField] Image face;
        [SerializeField] TMP_Text numberText;
        [SerializeField] TMP_Text dateText;

        public void Bind(int personNumber, int personCount, int month, int day, bool shared)
        {
            if (numberText != null)
            {
                numberText.text = personNumber.ToString(personCount >= 100 ? "000" : "00");
                numberText.color = shared ? Color.white : DarkText;
            }

            if (dateText != null)
            {
                dateText.text = month + "/" + day;
                dateText.color = shared ? Color.white : DarkText;
            }

            if (face != null)
                face.color = shared ? SharedColor : NormalColor;
        }
    }
}
