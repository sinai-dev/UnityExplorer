using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.Core;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheMethod : CacheMember
    {
        //private CacheObjectBase m_cachedReturnValue;

        public override Type FallbackType => (MemInfo as MethodInfo).ReturnType;

        public override bool HasParameters => base.HasParameters || GenericArgs.Length > 0;

        public override bool IsStatic => (MemInfo as MethodInfo).IsStatic;

        public override int ParamCount => base.ParamCount + m_genericArgInput.Length;

        public Type[] GenericArgs { get; private set; }
        public Type[][] GenericConstraints { get; private set; }

        public string[] m_genericArgInput = new string[0];

        public CacheMethod(MethodInfo methodInfo, object declaringInstance, GameObject parent) : base(methodInfo, declaringInstance, parent)
        {
            GenericArgs = methodInfo.GetGenericArguments();

            GenericConstraints = GenericArgs.Select(x => x.GetGenericParameterConstraints())
                                            .Where(x => x != null)
                                            .ToArray();

            m_genericArgInput = new string[GenericArgs.Length];

            m_arguments = methodInfo.GetParameters();
            m_argumentInput = new string[m_arguments.Length];

            CreateIValue(null, methodInfo.ReturnType);
        }

        public override void UpdateReflection()
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
                ReflectionException = null;
            }
            catch (Exception e)
            {
                while (e.InnerException != null)
                    e = e.InnerException;

                ExplorerCore.LogWarning($"Exception evaluating: {e.GetType()}, {e.Message}");
                ReflectionException = ReflectionUtility.ReflectionExToString(e);
            }

            IValue.Value = ret;
            UpdateValue();
        }

        private MethodInfo MakeGenericMethodFromInput()
        {
            var mi = MemInfo as MethodInfo;

            var list = new List<Type>();
            for (int i = 0; i < GenericArgs.Length; i++)
            {
                var input = m_genericArgInput[i];
                if (ReflectionUtility.GetTypeByName(input) is Type t)
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
                        $" Make sure you use the full name including the namespace.");
                    return null;
                }
            }

            // make into a generic with type list
            mi = mi.MakeGenericMethod(list.ToArray());

            return mi;
        }

        #region UI CONSTRUCTION

        internal void ConstructGenericArgInput(GameObject parent)
        {
            UIFactory.CreateLabel(parent, "GenericArgLabel", "Generic Arguments:", TextAnchor.MiddleLeft);

            for (int i = 0; i < GenericArgs.Length; i++)
                AddGenericArgRow(i, parent);
        }

        internal void AddGenericArgRow(int i, GameObject parent)
        {
            var arg = GenericArgs[i];

            string constrainTxt = "";
            if (this.GenericConstraints[i].Length > 0)
            {
                foreach (var constraint in this.GenericConstraints[i])
                {
                    if (constrainTxt != "") 
                        constrainTxt += ", ";

                    constrainTxt += $"{SignatureHighlighter.ParseFullSyntax(constraint, false)}";
                }
            }
            else
                constrainTxt = $"Any";

            var rowObj = UIFactory.CreateHorizontalGroup(parent, "ArgRowObj", false, true, true, true, 4, default, new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(rowObj, minHeight: 25, flexibleWidth: 5000);

            var argLabelObj = UIFactory.CreateLabel(rowObj, "ArgLabelObj", $"{constrainTxt} <color={SignatureHighlighter.CONST_VAR}>{arg.Name}</color>", 
                TextAnchor.MiddleLeft);

            var argInputObj = UIFactory.CreateInputField(rowObj, "ArgInput", "...", 14, (int)TextAnchor.MiddleLeft, 1);
            UIFactory.SetLayoutElement(argInputObj, flexibleWidth: 1200);

            var argInput = argInputObj.GetComponent<InputField>();
            argInput.onValueChanged.AddListener((string val) => { m_genericArgInput[i] = val; });

        }

        #endregion
    }
}
