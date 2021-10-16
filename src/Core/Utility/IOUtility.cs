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

        public static string EnsureValidFilePath(string fullPathWithFile)
        {
            // Remove invalid path characters
            fullPathWithFile = string.Concat(fullPathWithFile.Split(invalidDirectoryCharacters));

            // Create directory (does nothing if it exists)
            Directory.CreateDirectory(Path.GetDirectoryName(fullPathWithFile));

            return fullPathWithFile;
        }

        public static string EnsureValidFilename(string filename)
        {
            return string.Concat(filename.Split(invalidFilenameCharacters));
        }
    }
}
