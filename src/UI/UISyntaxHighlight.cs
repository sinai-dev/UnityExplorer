using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Unstrip;

namespace UnityExplorer.UI
{
    public class UISyntaxHighlight
    {
        public const string FIELD_STATIC = "#8d8dc6";
        public const string FIELD_INSTANCE = "#c266ff";

        public const string METHOD_STATIC = "#b55b02";
        public const string METHOD_INSTANCE = "#ff8000";

        public const string PROP_STATIC = "#588075";
        public const string PROP_INSTANCE = "#55a38e";

        public const string CLASS_STATIC = "#3a8d71";
        public const string CLASS_INSTANCE = "#2df7b2";

        public const string CLASS_STRUCT = "#0fba3a";

        public const string LOCAL_ARG = "#a6e9e9";

        public static string CONST_VAR = "#92c470";

        internal static readonly Color s_silver = new Color(0.66f, 0.66f, 0.66f);

        internal static string GetClassColor(Type type)
        {
            if (type.IsAbstract && type.IsSealed)
                return CLASS_STATIC;
            else if (type.IsEnum || type.IsGenericParameter)
                return CONST_VAR;
            else if (type.IsValueType)
                return CLASS_STRUCT;
            else
                return CLASS_INSTANCE;
        }

        public static string ParseFullSyntax(Type type, bool includeNamespace, MemberInfo memberInfo = null)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            string ret = "";

            if (type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter))
            {
                ret = $"<color={CONST_VAR}>{type.Name}</color>";
            }
            else
            {
                if (includeNamespace && !string.IsNullOrEmpty(type.Namespace))
                    ret += $"<color=#{s_silver.ToHex()}>{type.Namespace}</color>.";

                var declaring = type.DeclaringType;
                while (declaring != null)
                {
                    ret += HighlightTypeName(declaring) + ".";
                    declaring = declaring.DeclaringType;
                }

                ret += HighlightTypeName(type);
            }

            if (memberInfo != null)
            {
                ret += ".";

                string memberColor = GetMemberInfoColor(memberInfo, out bool isStatic);
                string memberHighlight = $"<color={memberColor}>{memberInfo.Name}</color>";

                if (isStatic)
                    memberHighlight = $"<i>{memberHighlight}</i>";

                ret += memberHighlight;

                // generic method args
                if (memberInfo is MethodInfo method)
                {
                    var gArgs = method.GetGenericArguments();
                    ret += ParseGenericArgs(gArgs, true);
                }
            }

            return ret;
        }

        private static string HighlightTypeName(Type type)
        {
            var typeName = type.Name;

            var gArgs = type.GetGenericArguments();

            if (gArgs.Length > 0)
            {
                // remove the `N from the end of the type name
                // this could actually be >9 in some cases, so get the length of the length string and use that.
                // eg, if it was "List`15", we would remove the ending 3 chars

                int suffixLen = 1 + gArgs.Length.ToString().Length;

                // make sure the typename actually has expected "`N" format.
                if (typeName[typeName.Length - suffixLen] == '`')
                    typeName = typeName.Substring(0, typeName.Length - suffixLen);
            }

            // highlight the base name itself
            // do this after removing the `N suffix, so only the name itself is in the color tags.
            typeName = $"<color={GetClassColor(type)}>{typeName}</color>";

            // parse the generic args, if any
            if (gArgs.Length > 0)
                typeName += ParseGenericArgs(gArgs);

            return typeName;
        }

        private static string ParseGenericArgs(Type[] gArgs, bool allGeneric = false)
        {
            if (gArgs.Length < 1)
                return "";

            var args = "<";
            for (int i = 0; i < gArgs.Length; i++)
            {
                if (i > 0) 
                    args += ", ";

                var arg = gArgs[i];

                if (allGeneric)
                {
                    args += $"<color={CONST_VAR}>{arg.Name}</color>";
                    continue;
                }

                // using HighlightTypeName makes it recursive, so we can parse nested generic args.
                args += HighlightTypeName(arg);
            }
            return args + ">";
        }

        private static string GetMemberInfoColor(MemberInfo memberInfo, out bool isStatic)
        {
            string memberColor = "";
            isStatic = false;
            if (memberInfo is FieldInfo fi)
            {
                if (fi.IsStatic)
                {
                    isStatic = true;
                    memberColor = FIELD_STATIC;
                }
                else
                    memberColor = FIELD_INSTANCE;
            }
            else if (memberInfo is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    isStatic = true;
                    memberColor = METHOD_STATIC;
                }
                else
                    memberColor = METHOD_INSTANCE;
            }
            else if (memberInfo is PropertyInfo pi)
            {
                if (pi.GetAccessors(true)[0].IsStatic)
                {
                    isStatic = true;
                    memberColor = PROP_STATIC;
                }
                else
                    memberColor = PROP_INSTANCE;
            }
            return memberColor;
        }
    }
}
