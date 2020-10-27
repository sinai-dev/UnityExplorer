using UnityEngine;

namespace ExplorerBeta
{
    public static class UnityExtensions
    {
        public static string GetTransformPath(this Transform _transform)
        {
            string path = _transform.name;

            while (_transform.parent != null)
            {
                _transform = _transform.parent;
                path = _transform.name + "/" + path;
            }

            return path;
        }
    }
}
