using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer
{
    public static class ToStringUtility
    {
        internal static Dictionary<string, MethodInfo> toStringMethods = new Dictionary<string, MethodInfo>();
        internal static Dictionary<string, MethodInfo> toStringFormattedMethods = new Dictionary<string, MethodInfo>();

        // string allocs
        private const string nullString = "<color=grey>null</color>";
        private const string nullUnknown = nullString + " (?)";
        private const string destroyedString = "<color=red>Destroyed</color>";
        private const string untitledString = "<i><color=grey>untitled</color></i>";

        private const string eventSystemNamespace = "UnityEngine.EventSystem";

        public static string PruneString(string s, int chars = 200, int lines = 5)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var sb = new StringBuilder(Math.Max(chars, s.Length));
            int newlines = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (newlines >= lines || i >= chars)
                {
                    sb.Append("...");
                    break;
                }
                char c = s[i];
                if (c == '\r' || c == '\n')
                    newlines++;
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static string ToStringWithType(object value, Type fallbackType, bool includeNamespace = true)
        {
            if (value.IsNullOrDestroyed() && fallbackType == null)
                return nullUnknown;

            Type type = value?.GetActualType() ?? fallbackType;

            string richType = SignatureHighlighter.Parse(type, includeNamespace);

            var sb = new StringBuilder();

            if (value.IsNullOrDestroyed())
            {
                if (value == null)
                {
                    sb.Append(nullString);
                    AppendRichType(sb, richType);
                    return sb.ToString();
                }
                else // destroyed unity object
                {
                    sb.Append(destroyedString);
                    AppendRichType(sb, richType);
                    return sb.ToString();
                }
            }

            if (value is UnityEngine.Object obj)
            {
                if (string.IsNullOrEmpty(obj.name))
                    sb.Append(untitledString);
                else
                {
                    sb.Append('"');
                    sb.Append(PruneString(obj.name, 50, 1));
                    sb.Append('"');
                }
                
                AppendRichType(sb, richType);
            }
            else if (type.FullName.StartsWith(eventSystemNamespace))
            {
                // UnityEngine.EventSystem classes can have some obnoxious ToString results with rich text.
                sb.Append(richType);
            }
            else
            {
                var toString = ToString(value);

                if (type.IsGenericType 
                    || toString == type.FullName 
                    || toString == $"{type.FullName} {type.FullName}"
                    || toString == $"Il2Cpp{type.FullName}" || type.FullName == $"Il2Cpp{toString}")
                {
                    sb.Append(richType);
                }
                else // the ToString contains some actual implementation, use that value.
                {
                    sb.Append(PruneString(toString, 200, 5));

                    AppendRichType(sb, richType);
                }
            }

            return sb.ToString();
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
                    var formatMethod = type.GetMethod("ToString", ArgumentUtility.ParseArgs);
                    formatMethod.Invoke(value, new object[] { "F3" });
                    toStringFormattedMethods.Add(type.AssemblyQualifiedName, formatMethod);
                    toStringMethods.Add(type.AssemblyQualifiedName, null);
                }
                catch
                {
                    var toStringMethod = type.GetMethod("ToString", ArgumentUtility.EmptyTypes);
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
                    toString = (string)toStringMethods[type.AssemblyQualifiedName].Invoke(value, ArgumentUtility.EmptyArgs);
            }
            catch (Exception ex)
            {
                toString = ex.ReflectionExToString();
            }

            toString = ReflectionUtility.ProcessTypeInString(type, toString);

#if CPP
            if (value is Il2CppSystem.Type cppType)
            {
                var monoType = Il2CppReflection.GetUnhollowedType(cppType);
                if (monoType != null)
                    toString = ReflectionUtility.ProcessTypeInString(monoType, toString);
            }
#endif

            return toString;
        }
    }
}
