using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UnityExplorer
{
    public static class MiscUtility
    {
        private static CultureInfo _enCulture = new CultureInfo("en-US");

        public static bool ContainsIgnoreCase(this string _this, string s)
        {
            return _enCulture.CompareInfo.IndexOf(_this, s, CompareOptions.IgnoreCase) >= 0;
        }
    }
}
