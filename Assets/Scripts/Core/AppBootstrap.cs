using UnityEngine;
using UnityEngine.SceneManagement;

namespace MindParadox.Core
{
    /// <summary>
    /// BootScene에서 메인 메뉴 씬으로 넘긴다.
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        [SerializeField] string mainSceneName = "MainScene";

        void Start()
        {
            if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
            {
                Debug.LogError($"[Mind Paradox] Build Settings에서 '{mainSceneName}' 씬을 찾지 못했습니다.");
                return;
            }

            SceneManager.LoadScene(mainSceneName);
        }
    }
}
