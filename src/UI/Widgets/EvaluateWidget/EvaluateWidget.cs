using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI;
using UniverseLib.UI.Models;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.UI;
using UniverseLib;
using UnityExplorer.CacheObject;

namespace UnityExplorer.UI.Widgets
{
    public class EvaluateWidget : IPooledObject
    {
        public CacheMember Owner { get; set; }

        public GameObject UIRoot { get; set; }
        public float DefaultHeight => -1f;

        private ParameterInfo[] parameters;
        internal GameObject parametersHolder;
        private ParameterHandler[] paramHandlers;

        private Type[] genericArguments;
        internal GameObject genericArgumentsHolder;
        private GenericArgumentHandler[] genericHandlers;

        public void OnBorrowedFromPool(CacheMember owner)
        {
            this.Owner = owner;

            parameters = owner.Arguments;
            paramHandlers = new ParameterHandler[parameters.Length];

            genericArguments = owner.GenericArguments;
            genericHandlers = new GenericArgumentHandler[genericArguments.Length];

            SetArgRows();

            this.UIRoot.SetActive(true);

            InspectorManager.OnInspectedTabsChanged += InspectorManager_OnInspectedTabsChanged;
        }

        public void OnReturnToPool()
        {
            foreach (var widget in paramHandlers)
            {
                widget.OnReturned();
                Pool<ParameterHandler>.Return(widget);
            }
            paramHandlers = null;

            foreach (var widget in genericHandlers)
            {
                widget.OnReturned();
                Pool<GenericArgumentHandler>.Return(widget);
            }
            genericHandlers = null;

            this.Owner = null;

            InspectorManager.OnInspectedTabsChanged -= InspectorManager_OnInspectedTabsChanged;
        }

        private void InspectorManager_OnInspectedTabsChanged()
        {
            foreach (var handler in this.paramHandlers)
                handler.PopulateDropdown();
        }

        public Type[] TryParseGenericArguments()
        {
            Type[] outArgs = new Type[genericArguments.Length];

            for (int i = 0; i < genericArguments.Length; i++)
                outArgs[i] = genericHandlers[i].Evaluate();

            return outArgs;
        }

        public object[] TryParseArguments()
        {
            object[] outArgs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                outArgs[i] = paramHandlers[i].Evaluate();

            return outArgs;
        }

        private void SetArgRows()
        {
            if (genericArguments.Any())
            {
                genericArgumentsHolder.SetActive(true);
                SetGenericRows();
            }
            else
                genericArgumentsHolder.SetActive(false);

            if (parameters.Any())
            {
                parametersHolder.SetActive(true);
                SetNormalArgRows();
            }
            else
                parametersHolder.SetActive(false);
        }

        private void SetGenericRows()
        {
            for (int i = 0; i < genericArguments.Length; i++)
            {
                var type = genericArguments[i];

                var holder = genericHandlers[i] = Pool<GenericArgumentHandler>.Borrow();
                holder.UIRoot.transform.SetParent(this.genericArgumentsHolder.transform, false);
                holder.OnBorrowed(this, type);
            }
        }

        private void SetNormalArgRows()
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                var holder = paramHandlers[i] = Pool<ParameterHandler>.Borrow();
                holder.UIRoot.transform.SetParent(this.parametersHolder.transform, false);
                holder.OnBorrowed(this, param);
            }
        }


        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "EvaluateWidget", false, false, true, true, 3, new Vector4(2, 2, 2, 2),
                new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(UIRoot, minWidth: 50, flexibleWidth: 9999, minHeight: 50, flexibleHeight: 800);
            //UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // generic args
            this.genericArgumentsHolder = UIFactory.CreateUIObject("GenericHolder", UIRoot);
            UIFactory.SetLayoutElement(genericArgumentsHolder, flexibleWidth: 1000);
            var genericsTitle = UIFactory.CreateLabel(genericArgumentsHolder, "GenericsTitle", "Generic Arguments", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(genericsTitle.gameObject, minHeight: 25, flexibleWidth: 1000);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(genericArgumentsHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(genericArgumentsHolder, minHeight: 25, flexibleHeight: 750, minWidth: 50, flexibleWidth: 9999);
            //genericArgHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // args
            this.parametersHolder = UIFactory.CreateUIObject("ArgHolder", UIRoot);
            UIFactory.SetLayoutElement(parametersHolder, flexibleWidth: 1000);
            var argsTitle = UIFactory.CreateLabel(parametersHolder, "ArgsTitle", "Arguments", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(argsTitle.gameObject, minHeight: 25, flexibleWidth: 1000);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(parametersHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(parametersHolder, minHeight: 25, flexibleHeight: 750, minWidth: 50, flexibleWidth: 9999);
            //argHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // evaluate button
            var evalButton = UIFactory.CreateButton(UIRoot, "EvaluateButton", "Evaluate", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(evalButton.Component.gameObject, minHeight: 25, minWidth: 150, flexibleWidth: 0);
            evalButton.OnClick += () =>
            {
                Owner.EvaluateAndSetCell();
            };

            return UIRoot;
        }
    }
}
