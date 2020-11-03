using UnityEngine;

namespace UnityExplorer.Helpers
{
    public class UnityHelpers
    {
        private static Camera m_mainCamera;

        public static Camera MainCamera
        {
            get
            {
                if (!m_mainCamera)
                {
                    m_mainCamera = Camera.main;
                }
                return m_mainCamera;
            }
        }

        public static string ActiveSceneName
        {
            get
            {
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
        }
    }
}
