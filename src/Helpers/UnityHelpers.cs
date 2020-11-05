using UnityEngine;

namespace UnityExplorer.Helpers
{
    public static class UnityHelpers
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

        public static string ToStringLong(this Vector3 vec)
        {
            return $"X: {vec.x:F3}, Y: {vec.y:F3}, Z: {vec.z:F3}";
        }

        public static string GetTransformPath(this Transform t, bool includeThisName = false)
        {
            string path = includeThisName ? t.transform.name : "";

            while (t.parent != null)
            {
                t = t.parent;
                path = $"{t.name}/{path}";
            }

            return path;
        }
    }
}
