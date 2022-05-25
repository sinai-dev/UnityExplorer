using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public class GenericConstructorWidget
    {
        GenericArgumentHandler[] handlers;

        Type[] currentGenericParameters;
        Action<Type[]> currentOnSubmit;
        Action currentOnCancel;

        public GameObject UIRoot;
        Text Title;
        GameObject ArgsHolder;

        public void Show(Action<Type[]> onSubmit, Action onCancel, Type genericTypeDefinition)
        {
            Title.text = $"Setting generic arguments for {SignatureHighlighter.Parse(genericTypeDefinition, false)}...";

            OnShow(onSubmit, onCancel, genericTypeDefinition.GetGenericArguments());
        }

        public void Show(Action<Type[]> onSubmit, Action onCancel, MethodInfo genericMethodDefinition)
        {
            Title.text = $"Setting generic arguments for {SignatureHighlighter.ParseMethod(genericMethodDefinition)}...";

            OnShow(onSubmit, onCancel, genericMethodDefinition.GetGenericArguments());
        }

        void OnShow(Action<Type[]> onSubmit, Action onCancel, Type[] genericParameters)
        {
            currentOnSubmit = onSubmit;
            currentOnCancel = onCancel;

            SetGenericParameters(genericParameters);
        }

        void SetGenericParameters(Type[] genericParameters)
        {
            currentGenericParameters = genericParameters;

            handlers = new GenericArgumentHandler[genericParameters.Length];
            for (int i = 0; i < genericParameters.Length; i++)
            {
                Type type = genericParameters[i];

                GenericArgumentHandler holder = handlers[i] = Pool<GenericArgumentHandler>.Borrow();
                holder.UIRoot.transform.SetParent(ArgsHolder.transform, false);
                holder.OnBorrowed(type);
            }
        }

        public void TrySubmit()
        {
            Type[] args = new Type[currentGenericParameters.Length];

            for (int i = 0; i < args.Length; i++)
            {
                GenericArgumentHandler handler = handlers[i];
                Type arg;
                try
                {
                    arg = handler.Evaluate();
                    if (arg == null) throw new Exception();
                }
                catch
                {
                    ExplorerCore.LogWarning($"Generic argument '{handler.inputField.Text}' is not a valid type.");
                    return;
                }
                args[i] = arg;
            }

            OnClose();
            currentOnSubmit(args);
        }

        public void Cancel()
        {
            OnClose();

            currentOnCancel?.Invoke();
        }

        void OnClose()
        {
            if (handlers != null)
            {
                foreach (GenericArgumentHandler widget in handlers)
                {
                    widget.OnReturned();
                    Pool<GenericArgumentHandler>.Return(widget);
                }
                handlers = null;
            }
        }

        // UI Construction

        internal void ConstructUI(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "GenericArgsHandler", false, false, true, true, 5, new Vector4(5, 5, 5, 5), 
                childAlignment: TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(UIRoot, flexibleWidth: 9999, flexibleHeight: 9999);

            ButtonRef submitButton = UIFactory.CreateButton(UIRoot, "SubmitButton", "Submit", new Color(0.2f, 0.3f, 0.2f));
            UIFactory.SetLayoutElement(submitButton.GameObject, minHeight: 25, minWidth: 200);
            submitButton.OnClick += TrySubmit;

            ButtonRef cancelButton = UIFactory.CreateButton(UIRoot, "CancelButton", "Cancel", new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(cancelButton.GameObject, minHeight: 25, minWidth: 200);
            cancelButton.OnClick += Cancel;

            Title = UIFactory.CreateLabel(UIRoot, "Title", "Generic Arguments", TextAnchor.MiddleCenter);
            UIFactory.SetLayoutElement(Title.gameObject, minHeight: 25, flexibleWidth: 9999);

            GameObject scrollview = UIFactory.CreateScrollView(UIRoot, "GenericArgsScrollView", out ArgsHolder, out _, new(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(scrollview, flexibleWidth: 9999, flexibleHeight: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ArgsHolder, padTop: 5, padLeft: 5, padBottom: 5, padRight: 5);
        }
    }
}
