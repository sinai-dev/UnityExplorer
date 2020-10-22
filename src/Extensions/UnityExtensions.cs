using UnityEngine;

namespace Explorer
{
    public static class UnityExtensions
    {
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
