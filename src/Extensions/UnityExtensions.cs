using UnityEngine;

namespace UnityExplorer
{
    public static class UnityExtensions
    {
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
