using System;
using System.Collections;
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
        private const string nullString = "<color=grey>null</color>";
        private const string destroyedString = "<color=red>Destroyed</color>";
        private const string untitledString = "<i><color=grey>untitled</color></i>";

        public static string ToStringWithType(object value, Type fallbackType, bool includeNamespace = true)
        {
            if (value == null && fallbackType == null)
                return nullString;

            Type type = value?.GetActualType() ?? fallbackType;

            string richType = SignatureHighlighter.ParseFullSyntax(type, includeNamespace);

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
                _stringBuilder.Append(string.IsNullOrEmpty(obj.name) ? untitledString : obj.name);
                AppendRichType(_stringBuilder, richType);
            }
            else
            {
                var toString = ToString(value);

                if (type.IsEnumerable())
                {
                    if (value is IList iList)
                        _stringBuilder.Append($"[{iList.Count}] ");
                    else
                        if (value is ICollection iCol)
                        _stringBuilder.Append($"[{iCol.Count}] ");
                    else
                        _stringBuilder.Append("[?] ");
                }
                else if (type.IsDictionary())
                {
                    if (value is IDictionary iDict)
                        _stringBuilder.Append($"[{iDict.Count}] ");
                    else
                        _stringBuilder.Append("[?] ");
                }

                if (type.IsGenericType 
                    || toString == type.FullName 
                    || toString == $"{type.FullName} {type.FullName}"
                    || toString == $"Il2Cpp{type.FullName}" || type.FullName == $"Il2Cpp{toString}")
                {
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

        private static void AppendRichType(StringBuilder sb, string richType)
        {
            sb.Append(' ');
            sb.Append('(');
            sb.Append(richType);
            sb.Append(')');
        }

        private static string ToString(object value)
        {
            if (value.IsNullOrDestroyed())
            {
                if (value == null)
                    return nullString;
                else // destroyed unity object
                    return destroyedString;
            }

            var type = value.GetActualType();

            // Find and cache the relevant ToString method for this Type, if haven't already.

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

            // Invoke the ToString method on the object

            value = value.TryCast(type);

            string toString;
            try
            {
                if (toStringFormattedMethods.TryGetValue(type.AssemblyQualifiedName, out MethodInfo f3method))
                    toString = (string)f3method.Invoke(value, new object[] { "F3" });
                else
                    toString = (string)toStringMethods[type.AssemblyQualifiedName].Invoke(value, new object[0]);
            }
            catch (Exception ex)
            {
                toString = ex.ReflectionExToString();
            }

            string _ = null;
            toString = ReflectionProvider.Instance.ProcessTypeFullNameInString(type, toString, ref _);

            return toString;
        }
    }
}
