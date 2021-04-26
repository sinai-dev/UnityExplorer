using System;
using System.Collections.Generic;
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
    public class SignatureHighlighter
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

        public static string NAMESPACE = "#a8a8a8";

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

        private static readonly StringBuilder syntaxBuilder = new StringBuilder(8192);

        public static string ParseFullSyntax(Type type, bool includeNamespace, MemberInfo memberInfo = null)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            syntaxBuilder.Clear();

            if (type.IsGenericParameter || (type.HasElementType && type.GetElementType().IsGenericParameter))
            {
                syntaxBuilder.Append($"<color={CONST_VAR}>{type.Name}</color>");
            }
            else
            {
                if (includeNamespace && !string.IsNullOrEmpty(type.Namespace))
                    syntaxBuilder.Append($"<color={NAMESPACE}>{type.Namespace}</color>.");

                var declaring = type.DeclaringType;
                while (declaring != null)
                {
                    syntaxBuilder.Append(HighlightTypeName(declaring) + ".");
                    declaring = declaring.DeclaringType;
                }

                syntaxBuilder.Append(HighlightTypeName(type));
            }

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
                    syntaxBuilder.Append(ParseGenericArgs(method.GetGenericArguments(), true));
            }

            return syntaxBuilder.ToString();
        }

        private static readonly Dictionary<string, string> typeToRichType = new Dictionary<string, string>();

        public static string HighlightTypeName(Type type)
        {
            if (typeToRichType.ContainsKey(type.AssemblyQualifiedName))
                return typeToRichType[type.AssemblyQualifiedName];

            var typeName = type.Name;

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
                typeName += ParseGenericArgs(args);

            typeToRichType.Add(type.AssemblyQualifiedName, typeName);

            return typeName;
        }

        private static readonly StringBuilder genericBuilder = new StringBuilder(4096);

        public static string ParseGenericArgs(Type[] args, bool isGenericParams = false)
        {
            if (args.Length < 1)
                return string.Empty;

            genericBuilder.Clear();
            genericBuilder.Append('<');

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    genericBuilder.Append(',');

                if (isGenericParams)
                {
                    genericBuilder.Append($"<color={CONST_VAR}>{args[i].Name}</color>");
                    continue;
                }

                // using HighlightTypeName makes it recursive, so we can parse nested generic args.
                genericBuilder.Append(HighlightTypeName(args[i]));
            }

            genericBuilder.Append('>');
            return genericBuilder.ToString();
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
                else
                    return FIELD_INSTANCE;
            }
            else if (memberInfo is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    isStatic = true;
                    return METHOD_STATIC;
                }
                else
                    return METHOD_INSTANCE;
            }
            else if (memberInfo is PropertyInfo pi)
            {
                if (pi.GetAccessors(true)[0].IsStatic)
                {
                    isStatic = true;
                    return PROP_STATIC;
                }
                else
                    return PROP_INSTANCE;
            }

            throw new NotImplementedException(memberInfo.GetType().Name + " is not supported");
        }
    }
}
