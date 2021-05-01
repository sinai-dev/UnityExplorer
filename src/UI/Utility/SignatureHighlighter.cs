using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer.UI.Utility
{
    /// <summary>
    /// Syntax-highlights a member's signature, by either the Type name or a Type and Member together.
    /// </summary>
    public static class SignatureHighlighter
    {
        public const string NAMESPACE = "#a8a8a8";

        public const string CONST = "#92c470";

        public const string CLASS_STATIC = "#3a8d71";
        public const string CLASS_INSTANCE = "#2df7b2";

        public const string STRUCT = "#0fba3a";
        public const string INTERFACE = "#9b9b82";

        public const string FIELD_STATIC = "#8d8dc6";
        public const string FIELD_INSTANCE = "#c266ff";

        public const string METHOD_STATIC = "#b55b02";
        public const string METHOD_INSTANCE = "#ff8000";

        public const string PROP_STATIC = "#588075";
        public const string PROP_INSTANCE = "#55a38e";

        public const string LOCAL_ARG = "#a6e9e9";

        public static readonly Color StringOrange = new Color(0.83f, 0.61f, 0.52f);
        public static readonly Color EnumGreen = new Color(0.57f, 0.76f, 0.43f);
        public static readonly Color KeywordBlue = new Color(0.3f, 0.61f, 0.83f);
        public static readonly Color NumberGreen = new Color(0.71f, 0.8f, 0.65f);

        internal static string GetClassColor(Type type)
        {
            if (type.IsAbstract && type.IsSealed)
                return CLASS_STATIC;
            else if (type.IsEnum || type.IsGenericParameter)
                return CONST;
            else if (type.IsValueType)
                return STRUCT;
            else if (type.IsInterface)
                return INTERFACE;
            else
                return CLASS_INSTANCE;
        }

        private static readonly StringBuilder syntaxBuilder = new StringBuilder(2156);

        private static bool GetNamespace(Type type, out string ns)
        {
            var ret = !string.IsNullOrEmpty(ns = type.Namespace?.Trim());
            return ret;
        }

        public static string ParseFullSyntax(Type type, bool includeNamespace, MemberInfo memberInfo = null)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            syntaxBuilder.Clear();

            // Namespace

            bool isGeneric = type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter);

            if (!isGeneric && includeNamespace && GetNamespace(type, out string ns))
                syntaxBuilder.Append($"<color={NAMESPACE}>{ns}</color>.");

            // Declaring type

            var declaring = type.DeclaringType;
            while (declaring != null)
            {
                syntaxBuilder.Append(HighlightType(declaring));
                syntaxBuilder.Append('.');
                declaring = declaring.DeclaringType;
            }

            // Highlight the type name

            syntaxBuilder.Append(HighlightType(type));

            // If memberInfo, highlight the member info

            if (memberInfo != null)
            {
                syntaxBuilder.Append('.');

                string memColor = GetMemberInfoColor(memberInfo, out bool isStatic);

                if (isStatic)
                    syntaxBuilder.Append("<i>");

                syntaxBuilder.Append($"<color={memColor}>{memberInfo.Name}</color>");

                if (isStatic)
                    syntaxBuilder.Append("</i>");

                if (memberInfo is MethodInfo method)
                {
                    var args = method.GetGenericArguments();
                    if (args.Length > 0)
                        syntaxBuilder.Append($"<{ParseGenericArgs(args, true)}>");
                }
            }

            return syntaxBuilder.ToString();
        }

        public static string ParseFullType(Type type, bool includeNamespace = false, bool includeDllName = false)
        {
            string ret = HighlightType(type);

            bool isGeneric = type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter);

            if (!isGeneric && includeNamespace && GetNamespace(type, out string ns))
                ret = $"<color={NAMESPACE}>{ns}</color>.{ret}";

            if (includeDllName)
            {
                if (!string.IsNullOrEmpty(type.Assembly.Location))
                    ret = $"{ret} ({Path.GetFileName(type.Assembly.Location)})";
                else
                    ret = $"{ret} ({type.Assembly.GetName().Name})";
            }

            return ret;
        }

        private static readonly Dictionary<string, string> typeToRichType = new Dictionary<string, string>();

        private static string HighlightType(Type type)
        {
            string key = type.ToString();
            if (typeToRichType.ContainsKey(key))
                return typeToRichType[key];

            var typeName = type.Name;

            bool isArray = false;
            if (typeName.EndsWith("[]"))
            {
                isArray = true;
                typeName = typeName.Substring(0, typeName.Length - 2);
                type = type.GetElementType();
            }

            if (type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter))
            {
                typeName = $"<color={CONST}>{typeName}</color>";
            }
            else
            {
                var args = type.GetGenericArguments();

                if (args.Length > 0)
                {
                    // remove the `N from the end of the type name
                    // this could actually be >9 in some cases, so get the length of the length string and use that.
                    // eg, if it was "List`15", we would remove the ending 3 chars

                    int suffixLen = 1 + args.Length.ToString().Length;

                    // make sure the typename actually has expected "`N" format.
                    if (typeName[typeName.Length - suffixLen] == '`')
                        typeName = typeName.Substring(0, typeName.Length - suffixLen);
                }

                // highlight the base name itself
                // do this after removing the `N suffix, so only the name itself is in the color tags.
                typeName = $"<color={GetClassColor(type)}>{typeName}</color>";

                // parse the generic args, if any
                if (args.Length > 0)
                {
                    typeName += $"<{ParseGenericArgs(args)}>";
                }
            }

            if (isArray)
                typeName += "[]";

            typeToRichType.Add(key, typeName);

            return typeName;
        }

        public static string ParseGenericArgs(Type[] args, bool isGenericParams = false)
        {
            if (args.Length < 1)
                return string.Empty;

            string ret = "";

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    ret += ",";

                if (isGenericParams)
                {
                    ret += $"<color={CONST}>{args[i].Name}</color>";
                    continue;
                }

                // using HighlightTypeName makes it recursive, so we can parse nested generic args.
                ret += ParseFullType(args[i]);
            }

            return ret;
        }

        public static string GetMemberInfoColor(MemberInfo memberInfo, out bool isStatic)
        {
            isStatic = false;
            if (memberInfo is FieldInfo fi)
            {
                if (fi.IsStatic)
                {
                    isStatic = true;
                    return FIELD_STATIC;
                }
                
                return FIELD_INSTANCE;
            }
            else if (memberInfo is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    isStatic = true;
                    return METHOD_STATIC;
                }
                
                return METHOD_INSTANCE;
            }
            else if (memberInfo is PropertyInfo pi)
            {
                if (pi.GetAccessors(true)[0].IsStatic)
                {
                    isStatic = true;
                    return PROP_STATIC;
                }
                
                return PROP_INSTANCE;
            }
            //else if (memberInfo is EventInfo ei)
            //{
            //    if (ei.GetAddMethod().IsStatic)
            //    {
            //        isStatic = true;
            //        return EVENT_STATIC;
            //    }
               
            //    return EVENT_INSTANCE;
            //}

            throw new NotImplementedException(memberInfo.GetType().Name + " is not supported");
        }
    }
}
