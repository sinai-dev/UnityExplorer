using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.Core.Runtime;

namespace UnityExplorer
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

        internal const string ARRAY_TOKEN = "[]";
        internal const string OPEN_COLOR = "<color=";
        internal const string CLOSE_COLOR = "</color>";
        internal const string OPEN_ITALIC = "<i>";
        internal const string CLOSE_ITALIC = "</i>";

        public static readonly Color StringOrange = new Color(0.83f, 0.61f, 0.52f);
        public static readonly Color EnumGreen = new Color(0.57f, 0.76f, 0.43f);
        public static readonly Color KeywordBlue = new Color(0.3f, 0.61f, 0.83f);
        public static readonly string keywordBlueHex = KeywordBlue.ToHex();
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

        //private static readonly StringBuilder syntaxBuilder = new StringBuilder(2156);

        private static bool GetNamespace(Type type, out string ns)
        {
            var ret = !string.IsNullOrEmpty(ns = type.Namespace?.Trim());
            return ret;
        }

        public static string Parse(Type type, bool includeNamespace, MemberInfo memberInfo = null)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var syntaxBuilder = new StringBuilder();

            // Namespace

            bool isGeneric = type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter);

            if (!isGeneric)
            {
                if (includeNamespace && GetNamespace(type, out string ns))
                    syntaxBuilder.Append(OPEN_COLOR).Append(NAMESPACE).Append('>').Append(ns).Append(CLOSE_COLOR).Append('.');

                // Declaring type

                var declaring = type.DeclaringType;
                while (declaring != null)
                {
                    syntaxBuilder.Append(HighlightType(declaring));
                    syntaxBuilder.Append('.');
                    declaring = declaring.DeclaringType;
                }
            }

            // Highlight the type name

            syntaxBuilder.Append(HighlightType(type));

            // If memberInfo, highlight the member info

            if (memberInfo != null)
            {
                syntaxBuilder.Append('.');

                int start = syntaxBuilder.Length - 1;
                syntaxBuilder.Append(OPEN_COLOR)
                    .Append(GetMemberInfoColor(memberInfo, out bool isStatic))
                    .Append('>')
                    .Append(memberInfo.Name)
                    .Append(CLOSE_COLOR);

                if (isStatic)
                {
                    syntaxBuilder.Insert(start, OPEN_ITALIC);
                    syntaxBuilder.Append(CLOSE_ITALIC);
                }

                if (memberInfo is MethodInfo method)
                {
                    var args = method.GetGenericArguments();
                    if (args.Length > 0)
                        syntaxBuilder.Append('<').Append(ParseGenericArgs(args, true)).Append('>');
                }
            }

            return syntaxBuilder.ToString();
        }

        private static readonly Dictionary<string, string> typeToRichType = new Dictionary<string, string>();

        private static bool EndsWith(this StringBuilder sb, string _string)
        {
            int len = _string.Length;

            if (sb.Length < len)
                return false;

            int stringpos = 0;
            for (int i = sb.Length - len; i < sb.Length; i++, stringpos++)
            {
                if (sb[i] != _string[stringpos])
                    return false;
            }
            return true;
        }

        private static string HighlightType(Type type)
        {
            string key = type.ToString();
         
            if (typeToRichType.ContainsKey(key))
                return typeToRichType[key];
            
            var sb = new StringBuilder(type.Name);

            bool isArray = false;
            if (sb.EndsWith(ARRAY_TOKEN))
            {
                isArray = true;
                sb.Remove(sb.Length - 2, 2);
                type = type.GetElementType();
            }

            if (type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter))
            {
                sb.Insert(0, $"<color={CONST}>");
                sb.Append(CLOSE_COLOR);
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
                    if (sb[sb.Length - suffixLen] == '`')
                        sb.Remove(sb.Length - suffixLen, suffixLen);
                }

                // highlight the base name itself
                // do this after removing the `N suffix, so only the name itself is in the color tags.
                sb.Insert(0, $"{OPEN_COLOR}{GetClassColor(type)}>");
                sb.Append(CLOSE_COLOR);

                // parse the generic args, if any
                if (args.Length > 0)
                {
                    sb.Append('<').Append(ParseGenericArgs(args)).Append('>');
                }
            }

            if (isArray)
                sb.Append('[').Append(']');

            var ret = sb.ToString();
            typeToRichType.Add(key, ret);

            return ret;
        }

        public static string ParseGenericArgs(Type[] args, bool isGenericParams = false)
        {
            if (args.Length < 1)
                return string.Empty;
            
            var sb = new StringBuilder();

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    sb.Append(',').Append(' ');

                if (isGenericParams)
                {
                    sb.Append(OPEN_COLOR).Append(CONST).Append('>').Append(args[i].Name).Append(CLOSE_COLOR);
                    continue;
                }

                sb.Append(HighlightType(args[i]));
            }

            return sb.ToString();
        }

        public static string GetMemberInfoColor(MemberTypes type)
        {
            switch (type)
            {
                case MemberTypes.Method: return METHOD_INSTANCE;
                case MemberTypes.Property: return PROP_INSTANCE;
                case MemberTypes.Field: return FIELD_INSTANCE;
                default: return null;
            }
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
