using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityExplorer.Unstrip;

namespace UnityExplorer.UI.Shared
{
    public class UISyntaxHighlight
    {
        public const string Field_Static = "#8d8dc6";
        public const string Field_Instance = "#c266ff";

        public const string Method_Static = "#b55b02";
        public const string Method_Instance = "#ff8000";

        public const string Prop_Static = "#588075";
        public const string Prop_Instance = "#55a38e";

        public const string Class_Static = "#3a8d71";
        public const string Class_Instance = "#2df7b2";

        public const string Local = "#a6e9e9";

        public const string StructGreen = "#0e9931";

        public static string Enum = "#92c470";

        internal static readonly Color s_silver = new Color(0.66f, 0.66f, 0.66f);

        internal static string GetClassColor(Type type)
        {
            string classColor;
            if (type.IsAbstract && type.IsSealed)
                classColor = Class_Static;
            else if (type.IsEnum)
                classColor = Enum;
            else if (type.IsValueType)
                classColor = StructGreen;
            else
                classColor = Class_Instance;

            return classColor;
        }

        public static string GetHighlight(Type type, bool includeNamespace, MemberInfo memberInfo = null)
        {
            string ret = "";

            if (type.IsGenericParameter
                || type.GetGenericArguments().Any(it => it.IsGenericParameter)
                || (type.HasElementType && type.GetElementType().IsGenericParameter))
            {
                ret = $"<color={Enum}>{type.Name}</color>";
            }
            else
            {
                string ns = includeNamespace 
                    ? $"<color=#{s_silver.ToHex()}>{type.Namespace}</color>." 
                    : "";

                ret += ns;

                var declaring = type.DeclaringType;
                while (declaring != null)
                {
                    ret += $"<color={GetClassColor(declaring)}>{declaring.Name}</color>.";
                    declaring = declaring.DeclaringType;
                }

                ret += $"<color={GetClassColor(type)}>{type.Name}</color>";
            }

            // todo MemberInfo
            if (memberInfo != null)
            {
                ret += ".";

                string memberColor = "";
                bool isStatic = false;

                if (memberInfo is FieldInfo fi)
                {
                    if (fi.IsStatic)
                    {
                        isStatic = true;
                        memberColor = Field_Static;
                    }
                    else
                        memberColor = Field_Instance;
                }
                else if (memberInfo is MethodInfo mi)
                {
                    if (mi.IsStatic)
                    {
                        isStatic = true;
                        memberColor = Method_Static;
                    }
                    else
                        memberColor = Method_Instance;
                }
                else if (memberInfo is PropertyInfo pi)
                {
                    if (pi.GetAccessors(true)[0].IsStatic)
                    {
                        isStatic = true;
                        memberColor = Prop_Static;
                    }
                    else
                        memberColor = Prop_Instance;
                }

                if (isStatic) 
                    ret += "<i>";

                ret += $"<color={memberColor}>{memberInfo.Name}</color>";

                if (isStatic) 
                    ret += "</i>";

                // generic method args
                if (memberInfo is MethodInfo method)
                {
                    var gArgs = method.GetGenericArguments();
                    if (gArgs.Length > 0)
                    {
                        ret += "<";

                        var args = "";
                        for (int i = 0; i < gArgs.Length; i++)
                        {
                            if (i > 0) args += ", ";
                            args += $"<color={Enum}>{gArgs[i].Name}</color>";
                        }
                        ret += args;

                        ret += ">";
                    }
                }
            }

            return ret;
        }
    }
}
