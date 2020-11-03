//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using UnityEngine;
//using UnityExplorer.UI;
//using UnityExplorer.UI.Shared;

//namespace UnityExplorer.CacheObject
//{
//    public class CacheMember : CacheObjectBase
//    {
//        public MemberInfo MemInfo { get; set; }
//        public Type DeclaringType { get; set; }
//        public object DeclaringInstance { get; set; }

//        public virtual bool IsStatic { get; private set; }

//        public override bool HasParameters => m_arguments != null && m_arguments.Length > 0;
//        public override bool IsMember => true;

//        public string RichTextName => m_richTextName ?? GetRichTextName();
//        private string m_richTextName;

//        public override bool CanWrite => m_canWrite ?? GetCanWrite();
//        private bool? m_canWrite;

//        public string ReflectionException { get; set; }

//        public bool m_evaluated = false;
//        public bool m_isEvaluating;
//        public ParameterInfo[] m_arguments = new ParameterInfo[0];
//        public string[] m_argumentInput = new string[0];

//        public virtual void InitMember(MemberInfo member, object declaringInstance)
//        {
//            MemInfo = member;
//            DeclaringInstance = declaringInstance;
//            DeclaringType = member.DeclaringType;
//        }

//        public override void UpdateValue()
//        {
//            base.UpdateValue();
//        }

//        public override void SetValue()
//        {
//            // ...
//        }

//        public object[] ParseArguments()
//        {
//            if (m_arguments.Length < 1)
//            {
//                return new object[0];
//            }

//            var parsedArgs = new List<object>();
//            for (int i = 0; i < m_arguments.Length; i++)
//            {
//                var input = m_argumentInput[i];
//                var type = m_arguments[i].ParameterType;

//                if (type.IsByRef)
//                {
//                    type = type.GetElementType();
//                }

//                if (!string.IsNullOrEmpty(input))
//                {
//                    if (type == typeof(string))
//                    {
//                        parsedArgs.Add(input);
//                        continue;
//                    }
//                    else
//                    {
//                        try
//                        {
//                            var arg = type.GetMethod("Parse", new Type[] { typeof(string) })
//                                          .Invoke(null, new object[] { input });

//                            parsedArgs.Add(arg);
//                            continue;
//                        }
//                        catch
//                        {
//                            ExplorerCore.Log($"Argument #{i} '{m_arguments[i].Name}' ({type.Name}), could not parse input '{input}'.");
//                        }
//                    }
//                }

//                // No input, see if there is a default value.
//                if (HasDefaultValue(m_arguments[i]))
//                {
//                    parsedArgs.Add(m_arguments[i].DefaultValue);
//                    continue;
//                }

//                // Try add a null arg I guess
//                parsedArgs.Add(null);
//            }

//            return parsedArgs.ToArray();
//        }

//        public static bool HasDefaultValue(ParameterInfo arg) => arg.DefaultValue != DBNull.Value;

//        public void DrawArgsInput()
//        {
//            GUILayout.Label($"<b><color=orange>Arguments:</color></b>", new GUILayoutOption[0]);
//            for (int i = 0; i < this.m_arguments.Length; i++)
//            {
//                var name = this.m_arguments[i].Name;
//                var input = this.m_argumentInput[i];
//                var type = this.m_arguments[i].ParameterType.Name;

//                var label = $"<color={Syntax.Class_Instance}>{type}</color> ";
//                label += $"<color={Syntax.Local}>{name}</color>";
//                if (HasDefaultValue(this.m_arguments[i]))
//                {
//                    label = $"<i>[{label} = {this.m_arguments[i].DefaultValue ?? "null"}]</i>";
//                }

//                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);

//                GUI.skin.label.alignment = TextAnchor.MiddleCenter;

//                GUILayout.Label(i.ToString(), new GUILayoutOption[] { GUILayout.Width(15) });
//                GUILayout.Label(label, new GUILayoutOption[] { GUILayout.ExpandWidth(false) });
//                this.m_argumentInput[i] = GUIHelper.TextField(input, new GUILayoutOption[] { GUILayout.ExpandWidth(true) });

//                GUI.skin.label.alignment = TextAnchor.MiddleLeft;

//                GUILayout.EndHorizontal();
//            }
//        }

//        private bool GetCanWrite()
//        {
//            if (MemInfo is FieldInfo fi)
//                m_canWrite = !(fi.IsLiteral && !fi.IsInitOnly);
//            else if (MemInfo is PropertyInfo pi)
//                m_canWrite = pi.CanWrite;
//            else
//                m_canWrite = false;

//            return (bool)m_canWrite;
//        }

//        private string GetRichTextName()
//        {
//            string memberColor = "";
//            bool isStatic = false;

//            if (MemInfo is FieldInfo fi)
//            {
//                if (fi.IsStatic)
//                {
//                    isStatic = true;
//                    memberColor = Syntax.Field_Static;
//                }
//                else
//                    memberColor = Syntax.Field_Instance;
//            }
//            else if (MemInfo is MethodInfo mi)
//            {
//                if (mi.IsStatic)
//                {
//                    isStatic = true;
//                    memberColor = Syntax.Method_Static;
//                }
//                else
//                    memberColor = Syntax.Method_Instance;
//            }
//            else if (MemInfo is PropertyInfo pi)
//            {
//                if (pi.GetAccessors()[0].IsStatic)
//                {
//                    isStatic = true;
//                    memberColor = Syntax.Prop_Static;
//                }
//                else
//                    memberColor = Syntax.Prop_Instance;
//            }

//            string classColor;
//            if (MemInfo.DeclaringType.IsValueType)
//            {
//                classColor = Syntax.StructGreen;
//            }
//            else if (MemInfo.DeclaringType.IsAbstract && MemInfo.DeclaringType.IsSealed)
//            {
//                classColor = Syntax.Class_Static;
//            }
//            else
//            {
//                classColor = Syntax.Class_Instance;
//            }

//            m_richTextName = $"<color={classColor}>{MemInfo.DeclaringType.Name}</color>.";
//            if (isStatic) m_richTextName += "<i>";
//            m_richTextName += $"<color={memberColor}>{MemInfo.Name}</color>";
//            if (isStatic) m_richTextName += "</i>";

//            // generic method args
//            if (this is CacheMethod cm && cm.GenericArgs.Length > 0)
//            {
//                m_richTextName += "<";

//                var args = "";
//                for (int i = 0; i < cm.GenericArgs.Length; i++)
//                {
//                    if (args != "") args += ", ";
//                    args += $"<color={Syntax.StructGreen}>{cm.GenericArgs[i].Name}</color>";
//                }
//                m_richTextName += args;

//                m_richTextName += ">";
//            }

//            return m_richTextName;
//        }
//    }
//}
