using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer.UI.Utility
{
    public static class ToStringUtility
    {
        internal static Dictionary<string, MethodInfo> toStringMethods = new Dictionary<string, MethodInfo>();
        internal static Dictionary<string, MethodInfo> toStringFormattedMethods = new Dictionary<string, MethodInfo>();

        // string allocs
        private static readonly StringBuilder _stringBuilder = new StringBuilder(16384);
        private const string unknownString = "<unknown>";
        private const string nullString = "<color=grey>[null]</color>";
        private const string destroyedString = "<color=red>[Destroyed]</color>";

        public static string ToString(object value, Type type)
        {
            if (value.IsNullOrDestroyed())
            {
                if (value == null)
                    return nullString;
                else // destroyed unity object
                    return destroyedString;
            }

            if (!toStringMethods.ContainsKey(type.AssemblyQualifiedName))
            {
                try
                {
                    var formatMethod = type.GetMethod("ToString", new Type[] { typeof(string) });
                    formatMethod.Invoke(value, new object[] { "F3" });
                    toStringFormattedMethods.Add(type.AssemblyQualifiedName, formatMethod);
                    toStringMethods.Add(type.AssemblyQualifiedName, null);
                }
                catch
                {
                    var toStringMethod = type.GetMethod("ToString", new Type[0]);
                    toStringMethods.Add(type.AssemblyQualifiedName, toStringMethod);
                }
            }

            value = value.TryCast(type);

            string toString;
            if (toStringFormattedMethods.TryGetValue(type.AssemblyQualifiedName, out MethodInfo f3method))
                toString = (string)f3method.Invoke(value, new object[] { "F3" });
            else
                toString = (string)toStringMethods[type.AssemblyQualifiedName].Invoke(value, new object[0]);

            return toString;
        }

        public static string ToStringWithType(object value, Type fallbackType, bool includeNamespace = true)
        {
            if (value == null && fallbackType == null)
                return unknownString;

            Type type = value?.GetActualType() ?? fallbackType;

            string richType = SignatureHighlighter.ParseFullSyntax(type, includeNamespace);

            //if (!includeName)
            //    return richType;

            _stringBuilder.Clear();

            if (value.IsNullOrDestroyed())
            {
                if (value == null)
                {
                    _stringBuilder.Append(nullString);
                    AppendRichType(_stringBuilder, richType);
                    return _stringBuilder.ToString();
                }
                else // destroyed unity object
                {
                    _stringBuilder.Append(destroyedString);
                    AppendRichType(_stringBuilder, richType);
                    return _stringBuilder.ToString();
                }
            }

            if (value is UnityEngine.Object obj)
            { 
                _stringBuilder.Append(obj.name);
                AppendRichType(_stringBuilder, richType);
            }
            else
            {
                var toString = ToString(value, type);

                if (toString == type.FullName || toString == $"Il2Cpp{type.FullName}" || type.FullName == $"Il2Cpp{toString}")
                {
                    // the ToString was just the default object.ToString(), use our
                    // syntax highlighted type name instead.
                    _stringBuilder.Append(richType);
                }
                else // the ToString contains some actual implementation, use that value.
                {
                    if (toString.Length > 200)
                        _stringBuilder.Append(toString.Substring(0, 200));
                    else
                        _stringBuilder.Append(toString);

                    AppendRichType(_stringBuilder, richType);
                }
            }

            return _stringBuilder.ToString();
        }

        // Just a little optimization, append chars directly instead of allocating every time
        // we want to do this.
        private static void AppendRichType(StringBuilder sb, string richType)
        {
            sb.Append(' ');
            sb.Append('(');
            sb.Append(richType);
            sb.Append(')');
        }
    }
}
