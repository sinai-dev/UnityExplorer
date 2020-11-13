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

        public static bool IsNullOrDestroyed(this object obj, bool suppressWarning = false)
        {
            var unityObj = obj as Object;
            if (obj == null)
            {
                if (!suppressWarning)
                    ExplorerCore.LogWarning("The target instance is null!");

                return true;
            }
            else if (obj is Object)
            {
                if (!unityObj)
                {
                    if (!suppressWarning)
                        ExplorerCore.LogWarning("The target UnityEngine.Object was destroyed!");

                    return true;
                }
            }
            return false;
        }

        public static string ToStringLong(this Vector3 vec)
        {
            return $"{vec.x:F3}, {vec.y:F3}, {vec.z:F3}";
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
