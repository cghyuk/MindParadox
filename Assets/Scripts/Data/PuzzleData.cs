using UnityEngine;

namespace MindParadox.Data
{
    public enum PuzzleDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// 문제 하나. 씬에 카드를 두지 않고 이 에셋으로 목록을 만든다.
    /// titleKey, shortDescriptionKey는 이후 Localization 연결용이다.
    /// sceneName이 있으면 그 씬을 열고, puzzleType은 이후 씬 안 모드용이다.
    /// </summary>
    [CreateAssetMenu(fileName = "PuzzleData", menuName = "Mind Paradox/Puzzle Data")]
    public class PuzzleData : ScriptableObject
    {
        public string puzzleId;
        public string categoryId;
        public string title;
        public string shortDescription;
        public int displayOrder;
        public string sceneName;
        public string puzzleType;
        public bool isDefaultFree = true;
        public int unlockOrder;
        public Sprite icon;
        public PuzzleDifficulty difficulty = PuzzleDifficulty.Medium;
        public bool isEnabled = true;
        public string titleKey;
        public string shortDescriptionKey;
    }
}
