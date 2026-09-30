using System;
using UnityEngine;

namespace MindParadox.UI
{
    /// <summary>
    /// 메인 메뉴 카드 목록을 채운다. 게임을 추가할 때는 카드를 복제하고 항목을 하나 더 넣으면 된다.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Serializable]
        public class GameMenuEntry
        {
            public GameCardUI card;
            public string title;
            public string description;
            public string playLogMessage;
            public string targetSceneName;
        }

        [SerializeField] GameMenuEntry[] entries;

        void Start()
        {
            ApplyEntries();
        }

        void ApplyEntries()
        {
            if (entries == null)
                return;

            for (int i = 0; i < entries.Length; i++)
            {
                GameMenuEntry entry = entries[i];
                if (entry == null || entry.card == null)
                    continue;

                entry.card.Setup(entry.title, entry.description, entry.playLogMessage, entry.targetSceneName);
            }
        }
    }
}
