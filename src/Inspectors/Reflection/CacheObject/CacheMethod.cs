using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UnityExplorer.Helpers;

namespace UnityExplorer.Inspectors.Reflection
{
    public class CacheMethod : CacheMember
    {
        //private CacheObjectBase m_cachedReturnValue;

        public override bool HasParameters => base.HasParameters || GenericArgs.Length > 0;

        public override bool IsStatic => (MemInfo as MethodInfo).IsStatic;

        public override int ParamCount => base.ParamCount + m_genericArgInput.Length;

        public Type[] GenericArgs { get; private set; }
        public Type[][] GenericConstraints { get; private set; }

        public string[] m_genericArgInput = new string[0];

        public CacheMethod(MethodInfo methodInfo, object declaringInstance) : base(methodInfo, declaringInstance)
        {
            GenericArgs = methodInfo.GetGenericArguments();

            GenericConstraints = GenericArgs.Select(x => x.GetGenericParameterConstraints())
                                            .Where(x => x != null)
                                            .ToArray();

            m_genericArgInput = new string[GenericArgs.Length];

            m_arguments = methodInfo.GetParameters();
            m_argumentInput = new string[m_arguments.Length];

            base.InitValue(null, methodInfo.ReturnType);
        }

        public override void UpdateValue()
        {
            base.UpdateValue();
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
                ExplorerCore.LogWarning($"Exception evaluating: {e.GetType()}, {e.Message}");
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }

            // todo do InitValue again for new value, in case type changed fundamentally.

            IValue.Value = ret;
            IValue.OnValueUpdated();
        }

        private MethodInfo MakeGenericMethodFromInput()
        {
            var mi = MemInfo as MethodInfo;

            var list = new List<Type>();
            for (int i = 0; i < GenericArgs.Length; i++)
            {
                var input = m_genericArgInput[i];
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
            var titleObj = UIFactory.CreateLabel(parent, TextAnchor.MiddleLeft);
            var titleText = titleObj.GetComponent<Text>();
            titleText.text = "<b>Generic Arguments:</b>";

            for (int i = 0; i < GenericArgs.Length; i++)
            {
                AddGenericArgRow(i, parent);
            }
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

                    constrainTxt += $"{UISyntaxHighlight.ParseFullSyntax(constraint, false)}";
                }
            }
            else
                constrainTxt = $"Any";

            var rowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowLayout = rowObj.AddComponent<LayoutElement>();
            rowLayout.minHeight = 25;
            rowLayout.flexibleWidth = 5000;
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandHeight = true;
            rowGroup.spacing = 4;

            var argLabelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            //var argLayout = argLabelObj.AddComponent<LayoutElement>();
            //argLayout.minWidth = 20;
            var argText = argLabelObj.GetComponent<Text>();
            argText.text = $"{constrainTxt} <color={UISyntaxHighlight.Enum}>{arg.Name}</color>";

            var argInputObj = UIFactory.CreateInputField(rowObj, 14, (int)TextAnchor.MiddleLeft, 1);
            var argInputLayout = argInputObj.AddComponent<LayoutElement>();
            argInputLayout.flexibleWidth = 1200;

            var argInput = argInputObj.GetComponent<InputField>();
            argInput.onValueChanged.AddListener((string val) => { m_genericArgInput[i] = val; });

            //var constraintLabelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            //var constraintLayout = constraintLabelObj.AddComponent<LayoutElement>();
            //constraintLayout.minWidth = 60;
            //constraintLayout.flexibleWidth = 100;
            //var constraintText = constraintLabelObj.GetComponent<Text>();
            //constraintText.text = ;
        }

        #endregion
    }
}
