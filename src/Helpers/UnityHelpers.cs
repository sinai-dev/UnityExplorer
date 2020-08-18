using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public static class UnityHelpers
    {
        private static Camera m_mainCamera;

        public static Camera MainCamera
        {
            get
            {
                if (m_mainCamera == null)
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

        public static string GetGameObjectPath(this Transform _transform)
        {
            return GetGameObjectPath(_transform, true);
        }

        public static string GetGameObjectPath(this Transform _transform, bool _includeThisName)
        {
            string path = _includeThisName ? ("/" + _transform.name) : "";
            GameObject gameObject = _transform.gameObject;
            while (gameObject.transform.parent != null)
            {
                gameObject = gameObject.transform.parent.gameObject;
                path = "/" + gameObject.name + path;
            }
            return path;
        }
    }
}
