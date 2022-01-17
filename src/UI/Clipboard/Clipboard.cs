using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI
{
    public static class Clipboard
    {
        public static object Current { get; private set; }

        public static void Init()
        {

        }

        public static void Copy(object obj)
        {
            Current = obj;
        }
    }
}
