using UnityExplorer.CacheObject;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;

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
        }

        public void OnReturnToPool()
        {
            foreach (ParameterHandler widget in paramHandlers)
            {
                widget.OnReturned();
                Pool<ParameterHandler>.Return(widget);
            }
            paramHandlers = null;

            foreach (GenericArgumentHandler widget in genericHandlers)
            {
                widget.OnReturned();
                Pool<GenericArgumentHandler>.Return(widget);
            }
            genericHandlers = null;

            this.Owner = null;
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
            if (!parameters.Any())
                return ArgumentUtility.EmptyArgs;

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
                Type type = genericArguments[i];

                GenericArgumentHandler holder = genericHandlers[i] = Pool<GenericArgumentHandler>.Borrow();
                holder.UIRoot.transform.SetParent(this.genericArgumentsHolder.transform, false);
                holder.OnBorrowed(type);
            }
        }

        private void SetNormalArgRows()
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo param = parameters[i];

                ParameterHandler holder = paramHandlers[i] = Pool<ParameterHandler>.Borrow();
                holder.UIRoot.transform.SetParent(this.parametersHolder.transform, false);
                holder.OnBorrowed(param);
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
            Text genericsTitle = UIFactory.CreateLabel(genericArgumentsHolder, "GenericsTitle", "Generic Arguments", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(genericsTitle.gameObject, minHeight: 25, flexibleWidth: 1000);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(genericArgumentsHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(genericArgumentsHolder, minHeight: 25, flexibleHeight: 750, minWidth: 50, flexibleWidth: 9999);
            //genericArgHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // args
            this.parametersHolder = UIFactory.CreateUIObject("ArgHolder", UIRoot);
            UIFactory.SetLayoutElement(parametersHolder, flexibleWidth: 1000);
            Text argsTitle = UIFactory.CreateLabel(parametersHolder, "ArgsTitle", "Arguments", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(argsTitle.gameObject, minHeight: 25, flexibleWidth: 1000);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(parametersHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(parametersHolder, minHeight: 25, flexibleHeight: 750, minWidth: 50, flexibleWidth: 9999);
            //argHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // evaluate button
            ButtonRef evalButton = UIFactory.CreateButton(UIRoot, "EvaluateButton", "Evaluate", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(evalButton.Component.gameObject, minHeight: 25, minWidth: 150, flexibleWidth: 0);
            evalButton.OnClick += () =>
            {
                Owner.EvaluateAndSetCell();
            };

            return UIRoot;
        }
    }
}
