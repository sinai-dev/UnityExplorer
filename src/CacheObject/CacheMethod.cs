using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.Helpers;

namespace Explorer.CacheObject
{
    public class CacheMethod : CacheMember
    {
        private CacheObjectBase m_cachedReturnValue;

        public override bool HasParameters => base.HasParameters || GenericArgs.Length > 0;

        public override bool IsStatic => (MemInfo as MethodInfo).IsStatic;

        public Type[] GenericArgs { get; private set; }
        public Type[][] GenericConstraints { get; private set; }

        public string[] GenericArgInput = new string[0];

        public override void InitMember(MemberInfo member, object declaringInstance)
        {
            base.InitMember(member, declaringInstance);

            var mi = MemInfo as MethodInfo;
            GenericArgs = mi.GetGenericArguments();

            GenericConstraints = GenericArgs.Select(x => x.GetGenericParameterConstraints())
                                            .ToArray();

            GenericArgInput = new string[GenericArgs.Length];

            m_arguments = mi.GetParameters();
            m_argumentInput = new string[m_arguments.Length];

            base.Init(null, mi.ReturnType);
        }

        public override void UpdateValue()
        {
            // CacheMethod cannot UpdateValue directly. Need to Evaluate.
        }

        public void Evaluate()
        {
            MethodInfo mi;
            if (GenericArgs.Length > 0)
            {
                mi = MakeGenericMethodFromInput();
                if (mi == null) return;
            }
            else
            {
                mi = MemInfo as MethodInfo;
            }

            object ret = null;

            try
            {
                ret = mi.Invoke(mi.IsStatic ? null : DeclaringInstance, ParseArguments());
                m_evaluated = true;
                m_isEvaluating = false;
            }
            catch (Exception e)
            {
                ExplorerCore.LogWarning($"Exception evaluating: {e.GetType()}, {e.Message}");
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }

            if (ret != null)
            {
                //m_cachedReturnValue = CacheFactory.GetTypeAndCacheObject(ret);
                m_cachedReturnValue = CacheFactory.GetCacheObject(ret);
                m_cachedReturnValue.UpdateValue();
            }
            else
            {
                m_cachedReturnValue = null;
            }
        }

        private MethodInfo MakeGenericMethodFromInput()
        {
            var mi = MemInfo as MethodInfo;

            var list = new List<Type>();
            for (int i = 0; i < GenericArgs.Length; i++)
            {
                var input = GenericArgInput[i];
                if (ReflectionHelpers.GetTypeByName(input) is Type t)
                {
                    if (GenericConstraints[i].Length == 0)
                    {
                        list.Add(t);
                    }
                    else
                    {
                        foreach (var constraint in GenericConstraints[i].Where(x => x != null))
                        {
                            if (!constraint.IsAssignableFrom(t))
                            {
                                ExplorerCore.LogWarning($"Generic argument #{i}, '{input}' is not assignable from the constraint '{constraint}'!");
                                return null;
                            }
                        }

                        list.Add(t);
                    }
                }
                else
                {
                    ExplorerCore.LogWarning($"Generic argument #{i}, could not get any type by the name of '{input}'!" +
                        $" Make sure you use the full name, including the NameSpace.");
                    return null;
                }
            }

            // make into a generic with type list
            mi = mi.MakeGenericMethod(list.ToArray());

            return mi;
        }

        // ==== GUI DRAW ====

        //public override void Draw(Rect window, float width)
        //{
        //    base.Draw(window, width);
        //}

        public void DrawValue(Rect window, float width)
        {
            string typeLabel = $"<color={Syntax.Class_Instance}>{IValue.ValueType.FullName}</color>";

            if (m_evaluated)
            {
                if (m_cachedReturnValue != null)
                {
                    m_cachedReturnValue.IValue.DrawValue(window, width);
                }
                else
                {
                    GUILayout.Label($"null ({typeLabel})", new GUILayoutOption[0]);
                }
            }
            else
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> ({typeLabel})", new GUILayoutOption[0]);
            }
        }

        public void DrawGenericArgsInput()
        {
            GUILayout.Label($"<b><color=orange>Generic Arguments:</color></b>", new GUILayoutOption[0]);

            for (int i = 0; i < this.GenericArgs.Length; i++)
            {
                string types = "";
                if (this.GenericConstraints[i].Length > 0)
                {
                    foreach (var constraint in this.GenericConstraints[i])
                    {
                        if (types != "") types += ", ";

                        string type;

                        if (constraint == null)
                            type = "Any";
                        else
                            type = constraint.ToString();

                        types += $"<color={Syntax.Class_Instance}>{type}</color>";
                    }
                }
                else
                {
                    types = $"<color={Syntax.Class_Instance}>Any</color>";
                }
                var input = this.GenericArgInput[i];

                GUIHelper.BeginHorizontal(new GUILayoutOption[0]);

                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label(
                    $"<color={Syntax.StructGreen}>{this.GenericArgs[i].Name}</color>",
                    new GUILayoutOption[] { GUILayout.Width(15) }
                );
                this.GenericArgInput[i] = GUIHelper.TextField(input, new GUILayoutOption[] { GUILayout.Width(150) });
                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                GUILayout.Label(types, new GUILayoutOption[0]);

                GUILayout.EndHorizontal();
            }
        }
    }
}
