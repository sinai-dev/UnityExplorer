using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors
{
    public class ComponentCell : ButtonCell
    {
        public Toggle BehaviourToggle;
        public ButtonRef DestroyButton;

        public Action<bool, int> OnBehaviourToggled;
        public Action<int> OnDestroyClicked;

        private void BehaviourToggled(bool val)
        {
            OnBehaviourToggled?.Invoke(val, CurrentDataIndex);
        }

        private void DestroyClicked()
        {
            OnDestroyClicked?.Invoke(CurrentDataIndex);
        }

        public override GameObject CreateContent(GameObject parent)
        {
            var root = base.CreateContent(parent);

            // Add mask to button so text doesnt overlap on Close button
            this.Button.Component.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            // Behaviour toggle

            var toggleObj = UIFactory.CreateToggle(UIRoot, "BehaviourToggle", out BehaviourToggle, out var behavText);
            UIFactory.SetLayoutElement(toggleObj, minHeight: 25, minWidth: 25);
            BehaviourToggle.onValueChanged.AddListener(BehaviourToggled);
            // put at first object
            toggleObj.transform.SetSiblingIndex(0);

            // Destroy button

            DestroyButton = UIFactory.CreateButton(UIRoot, "DestroyButton", "X", new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(DestroyButton.Component.gameObject, minHeight: 21, minWidth: 25);
            DestroyButton.OnClick += DestroyClicked;

            return root;
        }
    }
}
