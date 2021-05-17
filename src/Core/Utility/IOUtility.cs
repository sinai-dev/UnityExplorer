using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UnityExplorer
{
    public static class IOUtility
    {
        private static readonly char[] invalidDirectoryCharacters = Path.GetInvalidPathChars();
        private static readonly char[] invalidFilenameCharacters = Path.GetInvalidFileNameChars();

        public static string EnsureValidDirectory(string path)
        {
            path = string.Concat(path.Split(invalidDirectoryCharacters));

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return path;
        }

        public static string EnsureValidFilename(string filename)
        {
            return string.Concat(filename.Split(invalidFilenameCharacters));
        }
    }
}
