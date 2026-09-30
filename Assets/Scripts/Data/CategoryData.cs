using UnityEngine;

namespace MindParadox.Data
{
    /// <summary>
    /// 카테고리 하나. 목록은 이 에셋만 추가하면 늘어난다.
    /// displayNameKey, descriptionKey는 이후 Localization 연결용이다.
    /// </summary>
    [CreateAssetMenu(fileName = "CategoryData", menuName = "Mind Paradox/Category Data")]
    public class CategoryData : ScriptableObject
    {
        public string categoryId;
        public string displayName;
        public string description;
        public int displayOrder;
        public Sprite icon;
        public string unlockProductId;
        public bool isEnabled = true;
        public string displayNameKey;
        public string descriptionKey;
    }
}
