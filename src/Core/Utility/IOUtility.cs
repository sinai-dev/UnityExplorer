using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityExplorer
{
    public static class IOUtility
    {
        public static string EnsureValid(string path)
        {
            path = RemoveInvalidChars(path);

            var dir = Path.GetDirectoryName(path);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            return path;
        }

        public static string RemoveInvalidChars(string path)
        {
            return string.Concat(path.Split(Path.GetInvalidPathChars()));
        }
    }
}
