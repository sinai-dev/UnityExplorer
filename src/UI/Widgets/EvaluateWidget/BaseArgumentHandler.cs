using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets.AutoComplete;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public abstract class BaseArgumentHandler : IPooledObject
    {
        protected EvaluateWidget evaluator;

        internal Text argNameLabel;
        internal InputFieldRef inputField;
        internal TypeCompleter typeCompleter;

        // IPooledObject
        public float DefaultHeight => 25f;
        public GameObject UIRoot { get; set; }

        public abstract void CreateSpecialContent();

        public GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateUIObject("ArgRow", parent);
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, flexibleHeight: 50, minWidth: 50, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 5);
            UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            argNameLabel = UIFactory.CreateLabel(UIRoot, "ArgLabel", "not set", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(argNameLabel.gameObject, minWidth: 40, flexibleWidth: 90, minHeight: 25, flexibleHeight: 50);
            argNameLabel.horizontalOverflow = HorizontalWrapMode.Wrap;

            inputField = UIFactory.CreateInputField(UIRoot, "InputField", "...");
            UIFactory.SetLayoutElement(inputField.UIRoot, minHeight: 25, flexibleHeight: 50, minWidth: 100, flexibleWidth: 1000);
            inputField.Component.lineType = InputField.LineType.MultiLineNewline;
            inputField.UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            typeCompleter = new TypeCompleter(typeof(object), this.inputField)
            {
                Enabled = false
            };

            CreateSpecialContent();

            return UIRoot;
        }
    }
}
