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

        /// <summary>
        /// Check if a string contains another string, case-insensitive.
        /// </summary>
        public static bool ContainsIgnoreCase(this string _this, string s)
        {
            return _enCulture.CompareInfo.IndexOf(_this, s, CompareOptions.IgnoreCase) >= 0;
        }

        /// <summary>
        /// Just to allow Enum to do .HasFlag() in NET 3.5
        /// </summary>
        public static bool HasFlag(this Enum flags, Enum value)
        {
            ulong num = Convert.ToUInt64(value);
            return (Convert.ToUInt64(flags) & num) == num;
        }
    }
}
