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
        internal static Dictionary<Type, MethodInfo> toStringMethods = new Dictionary<Type, MethodInfo>();
        internal static Dictionary<Type, MethodInfo> toStringFormattedMethods = new Dictionary<Type, MethodInfo>();

        // string allocs
        private static readonly StringBuilder _stringBuilder = new StringBuilder(16384);
        private const string unknownString = "<unknown>";
        private const string nullString = "<color=grey>[null]</color>";
        private const string destroyedString = "<color=red>[Destroyed]</color>";

        public static string ToString(object value, Type fallbackType, bool includeNamespace = true, bool includeName = true)
        {
            if (value == null && fallbackType == null)
                return unknownString;

            Type type = value?.GetActualType() ?? fallbackType;

            // todo SB this too
            string richType = SignatureHighlighter.ParseFullSyntax(type, includeNamespace);

            if (!includeName)
                return richType;

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
                if (!toStringMethods.ContainsKey(type))
                {
                    var toStringMethod = type.GetMethod("ToString", new Type[0]);
                    var formatMethod = type.GetMethod("ToString", new Type[] { typeof(string) });

                    if (formatMethod != null)
                    {
                        try { formatMethod.Invoke(value, new object[] { "F3" }); }
                        catch { formatMethod = null; }
                    }

                    toStringMethods.Add(type, toStringMethod);
                    toStringFormattedMethods.Add(type, formatMethod);
                }

                var f3Method = toStringFormattedMethods[type];
                var stdMethod = toStringMethods[type];

                value = value.TryCast(type);

                string toString;
                if (f3Method != null)
                    toString = (string)f3Method.Invoke(value, new object[] { "F3" });
                else
                    toString = (string)stdMethod.Invoke(value, new object[0]);

                if (toString == type.FullName || toString == $"Il2Cpp{type.FullName}")
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

                ////string toString;

                //
                //toString = toString ?? "";
                //
                //string typeName = type.FullName;
                //if (typeName.StartsWith("Il2CppSystem."))
                //    typeName = typeName.Substring(6, typeName.Length - 6);
                //
                //toString = ReflectionProvider.Instance.ProcessTypeFullNameInString(type, toString, ref typeName);
                //
                //// If the ToString is just the type name, use our syntax highlighted type name instead.
                //if (toString == typeName)
                //{
                //    label = richType;
                //}
                //else // Otherwise, parse the result and put our highlighted name in.
                //{
                //    if (toString.Length > 200)
                //        toString = toString.Substring(0, 200) + "...";
                //
                //    label = toString;
                //
                //    var unityType = $"({type.FullName})";
                //    if (value is UnityEngine.Object && label.Contains(unityType))
                //        label = label.Replace(unityType, $"({richType})");
                //    else
                //        label += $" ({richType})";
                //}
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
