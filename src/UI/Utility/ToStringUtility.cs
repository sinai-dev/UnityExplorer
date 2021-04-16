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

        public static string GetDefaultLabel(object value, Type fallbackType, bool includeNamespace = true, bool includeName = true)
        {
            if (value == null && fallbackType == null)
                return "<null>";

            var type = value?.GetActualType() ?? fallbackType;

            var richType = SignatureHighlighter.ParseFullSyntax(type, includeNamespace);

            if (!includeName)
                return richType;

            if (value.IsNullOrDestroyed())
                return $"<color=grey>null</color> ({richType})";

            string label;

            // Two dirty fixes for TextAsset and EventSystem, which can have very long ToString results.
            if (value is TextAsset textAsset)
            {
                label = textAsset.text;

                if (label.Length > 10)
                    label = $"{label.Substring(0, 10)}...";

                label = $"\"{label}\" {textAsset.name} ({richType})";
            }
            else if (value is EventSystem)
            {
                label = richType;
            }
            else // For everything else...
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

                string toString;
                if (f3Method != null)
                    toString = (string)f3Method.Invoke(value, new object[] { "F3" });
                else
                    toString = (string)stdMethod.Invoke(value, new object[0]);

                toString = toString ?? "";

                string typeName = type.FullName;
                if (typeName.StartsWith("Il2CppSystem."))
                    typeName = typeName.Substring(6, typeName.Length - 6);

                toString = ReflectionProvider.Instance.ProcessTypeNameInString(type, toString, ref typeName);

                // If the ToString is just the type name, use our syntax highlighted type name instead.
                if (toString == typeName)
                {
                    label = richType;
                }
                else // Otherwise, parse the result and put our highlighted name in.
                {
                    if (toString.Length > 200)
                        toString = toString.Substring(0, 200) + "...";

                    label = toString;

                    var unityType = $"({type.FullName})";
                    if (value is UnityEngine.Object && label.Contains(unityType))
                        label = label.Replace(unityType, $"({richType})");
                    else
                        label += $" ({richType})";
                }
            }

            return label;
        }
    }
}
