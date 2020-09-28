using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Explorer
{
    public abstract class CacheObjectBase
    {
        public object Value;
        public Type ValueType;

        public MemberInfo MemInfo { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }

        public virtual bool HasParameters => m_arguments != null && m_arguments.Length > 0;

        public bool m_evaluated = false;
        public bool m_isEvaluating;
        public ParameterInfo[] m_arguments = new ParameterInfo[0];
        public string[] m_argumentInput = new string[0];

        public string ReflectionException { get; set; }

        public string RichTextName => m_richTextName ?? GetRichTextName();
        private string m_richTextName;

        public bool CanWrite => m_canWrite ?? (bool)(m_canWrite = GetCanWrite());
        private bool? m_canWrite;

        public virtual void Init() { }

        public abstract void DrawValue(Rect window, float width); 

        public virtual void UpdateValue()
        {
            if (MemInfo == null)
            {
                return;
            }

            if (HasParameters && !m_isEvaluating)
            {
                // Need to enter parameters first
                return;
            }

            try
            {
                if (MemInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemInfo as FieldInfo;
                    Value = fi.GetValue(fi.IsStatic ? null : DeclaringInstance);
                }
                else if (MemInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemInfo as PropertyInfo;
                    var target = pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance;

                    Value = pi.GetValue(target, ParseArguments());
                }

                ReflectionException = null;
                m_evaluated = true;
                m_isEvaluating = false;
            }
            catch (Exception e)
            {
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }
        }

        public void SetValue()
        {
            try
            {
                if (MemInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemInfo as FieldInfo;
                    fi.SetValue(fi.IsStatic ? null : DeclaringInstance, Value);
                }
                else if (MemInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemInfo as PropertyInfo;

                    if (HasParameters)
                    {
                        pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, Value, ParseArguments());
                    }
                    else
                    {
                        pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, Value, null);
                    }
                }
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Error setting value: {e.GetType()}, {e.Message}");
            }
        }

        public object[] ParseArguments()
        {
            var parsedArgs = new List<object>();
            for (int i = 0; i < m_arguments.Length; i++)
            {
                var input = m_argumentInput[i];
                var type = m_arguments[i].ParameterType;

                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }

                if (string.IsNullOrEmpty(input))
                {
                    // No input, see if there is a default value.
                    if (HasDefaultValue(m_arguments[i]))
                    {
                        parsedArgs.Add(m_arguments[i].DefaultValue);
                        continue;
                    }

                    // Try add a null arg I guess
                    parsedArgs.Add(null);
                    continue;
                }

                // strings can obviously just be used directly
                if (type == typeof(string))
                {
                    parsedArgs.Add(input);
                    continue;
                }
                else
                {
                    try
                    {
                        var arg = type.GetMethod("Parse", new Type[] { typeof(string) })
                                      .Invoke(null, new object[] { input });
                        parsedArgs.Add(arg);
                        continue;
                    }
                    catch
                    {
                        ExplorerCore.Log($"Argument #{i} '{m_arguments[i].Name}' ({type.Name}), could not parse input '{input}'.");
                    }
                }
            }

            return parsedArgs.ToArray();
        }

        public static bool HasDefaultValue(ParameterInfo arg) =>
#if NET35
                arg.DefaultValue != null;
#else
                arg.HasDefaultValue;
#endif

        // ========= Gui Draw ==========

        public const float MAX_LABEL_WIDTH = 400f;
        public const string EVALUATE_LABEL = "<color=lime>Evaluate</color>";

        public float CalcWhitespace(Rect window)
        {
            if (!(this is IExpandHeight)) return 0f;

            float whitespace = (this as IExpandHeight).WhiteSpace;
            if (whitespace > 0)
            {
                ClampLabelWidth(window, ref whitespace);
            }

            return whitespace;
        }

        public static void ClampLabelWidth(Rect window, ref float labelWidth)
        {
            float min = window.width * 0.37f;
            if (min > MAX_LABEL_WIDTH) min = MAX_LABEL_WIDTH;

            labelWidth = Mathf.Clamp(labelWidth, min, MAX_LABEL_WIDTH);
        }

        public void Draw(Rect window, float labelWidth = 215f)
        {
            if (labelWidth > 0)
            {
                ClampLabelWidth(window, ref labelWidth);
            }

            if (MemInfo != null)
            {
                GUILayout.Label(RichTextName, new GUILayoutOption[] { GUILayout.Width(labelWidth) });
            }
            else
            {
                GUIUnstrip.Space(labelWidth);
            }

            var cm = this as CacheMethod;

            if (HasParameters)
            {
                GUILayout.BeginVertical(new GUILayoutOption[0]);

                if (m_isEvaluating)
                {
                    if (cm != null && cm.GenericArgs.Length > 0)
                    {
                        GUILayout.Label($"<b><color=orange>Generic Arguments:</color></b>", new GUILayoutOption[0]);

                        for (int i = 0; i < cm.GenericArgs.Length; i++)
                        {
                            string types = "";
                            if (cm.GenericConstraints[i].Length > 0)
                            {
                                foreach (var constraint in cm.GenericConstraints[i])
                                {
                                    if (types != "") types += ", ";

                                    string type;

                                    if (constraint == null)
                                        type = "Any";
                                    else
                                        type = constraint.ToString();

                                    types += $"<color={UIStyles.Syntax.Class_Instance}>{type}</color>";
                                }
                            }
                            else
                            {
                                types = $"<color={UIStyles.Syntax.Class_Instance}>Any</color>";
                            }
                            var input = cm.GenericArgInput[i];

                            GUILayout.BeginHorizontal(new GUILayoutOption[0]);

                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            GUILayout.Label(
                                $"<color={UIStyles.Syntax.StructGreen}>{cm.GenericArgs[i].Name}</color>", 
                                new GUILayoutOption[] { GUILayout.Width(15) }
                            );
                            cm.GenericArgInput[i] = GUILayout.TextField(input, new GUILayoutOption[] { GUILayout.Width(150) });
                            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                            GUILayout.Label(types, new GUILayoutOption[0]);

                            GUILayout.EndHorizontal();
                        }
                    }

                    if (m_arguments.Length > 0)
                    {
                        GUILayout.Label($"<b><color=orange>Arguments:</color></b>", new GUILayoutOption[0]);
                        for (int i = 0; i < m_arguments.Length; i++)
                        {
                            var name = m_arguments[i].Name;
                            var input = m_argumentInput[i];
                            var type = m_arguments[i].ParameterType.Name;

                            var label = $"<color={UIStyles.Syntax.Class_Instance}>{type}</color> ";
                            label += $"<color={UIStyles.Syntax.Local}>{name}</color>";
                            if (HasDefaultValue(m_arguments[i]))
                            {
                                label = $"<i>[{label} = {m_arguments[i].DefaultValue ?? "null"}]</i>";
                            }

                            GUILayout.BeginHorizontal(new GUILayoutOption[0]);

                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            GUILayout.Label(i.ToString(), new GUILayoutOption[] { GUILayout.Width(15) });
                            m_argumentInput[i] = GUILayout.TextField(input, new GUILayoutOption[] { GUILayout.Width(150) });
                            GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                            GUILayout.Label(label, new GUILayoutOption[0]);

                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                    if (GUILayout.Button(EVALUATE_LABEL, new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        if (cm != null)
                            cm.Evaluate();
                        else
                            UpdateValue();
                    }
                    if (GUILayout.Button("Cancel", new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        m_isEvaluating = false;
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    var lbl = $"Evaluate (";
                    int len = m_arguments.Length;
                    if (cm != null) len += cm.GenericArgs.Length;
                    lbl += len + " params)";

                    if (GUILayout.Button(lbl, new GUILayoutOption[] { GUILayout.Width(150) }))
                    {
                        m_isEvaluating = true;
                    }
                }

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUIUnstrip.Space(labelWidth);
            }
            else if (cm != null)
            {
                if (GUILayout.Button(EVALUATE_LABEL, new GUILayoutOption[] { GUILayout.Width(70) }))
                {
                    cm.Evaluate();
                }

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUIUnstrip.Space(labelWidth);
            }

            string typeName = $"<color={UIStyles.Syntax.Class_Instance}>{ValueType.FullName}</color>";

            if (!string.IsNullOrEmpty(ReflectionException))
            {
                GUILayout.Label("<color=red>Reflection failed!</color> (" + ReflectionException + ")", new GUILayoutOption[0]);
            }
            else if ((HasParameters || this is CacheMethod) && !m_evaluated)
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> ({typeName})", new GUILayoutOption[0]);
            }
            else if (Value == null && !(this is CacheMethod))
            {
                GUILayout.Label($"<i>null ({typeName})</i>", new GUILayoutOption[0]);
            }
            else
            {
                DrawValue(window, window.width - labelWidth - 90);
            }
        }

        private bool GetCanWrite()
        {
            if (MemInfo is FieldInfo fi)
                return !(fi.IsLiteral && !fi.IsInitOnly);
            else if (MemInfo is PropertyInfo pi)
                return pi.CanWrite;
            else
                return false;
        }

        private string GetRichTextName()
        {
            string memberColor = "";
            bool isStatic = false;

            if (MemInfo is FieldInfo fi)
            {
                if (fi.IsStatic)
                {
                    isStatic = true;
                    memberColor = UIStyles.Syntax.Field_Static;
                }
                else
                    memberColor = UIStyles.Syntax.Field_Instance;
            }
            else if (MemInfo is MethodInfo mi)
            {
                if (mi.IsStatic)
                {
                    isStatic = true;
                    memberColor = UIStyles.Syntax.Method_Static;
                }   
                else
                    memberColor = UIStyles.Syntax.Method_Instance;
            }
            else if (MemInfo is PropertyInfo pi)
            {
                if (pi.GetAccessors()[0].IsStatic)
                {
                    isStatic = true;
                    memberColor = UIStyles.Syntax.Prop_Static;
                }
                else
                    memberColor = UIStyles.Syntax.Prop_Instance;
            }

            string classColor;
            if (MemInfo.DeclaringType.IsValueType)
            {
                classColor = UIStyles.Syntax.StructGreen;
            }
            else if (MemInfo.DeclaringType.IsAbstract && MemInfo.DeclaringType.IsSealed)
            {
                classColor = UIStyles.Syntax.Class_Static;
            }
            else
            {
                classColor = UIStyles.Syntax.Class_Instance;
            }

            m_richTextName = $"<color={classColor}>{MemInfo.DeclaringType.Name}</color>.";
            if (isStatic) m_richTextName += "<i>";
            m_richTextName += $"<color={memberColor}>{MemInfo.Name}</color>";
            if (isStatic) m_richTextName += "</i>";

            // generic method args
            if (this is CacheMethod cm && cm.GenericArgs.Length > 0)
            {
                m_richTextName += "<";

                var args = "";
                for (int i = 0; i < cm.GenericArgs.Length; i++)
                {
                    if (args != "") args += ", ";
                    args += $"<color={UIStyles.Syntax.StructGreen}>{cm.GenericArgs[i].Name}</color>";
                }
                m_richTextName += args;

                m_richTextName += ">";
            }

            return m_richTextName;
        }
    }
}
