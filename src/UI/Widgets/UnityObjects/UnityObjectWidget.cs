using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Inspectors;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;

namespace UnityExplorer.UI.Widgets
{
    public class UnityObjectWidget : IPooledObject
    {
        public UnityEngine.Object unityObject;
        public Component component;
        public ReflectionInspector owner;

        protected ButtonRef gameObjectButton;
        protected InputFieldRef nameInput;
        protected InputFieldRef instanceIdInput;

        // IPooledObject
        public GameObject UIRoot { get; set; }
        public float DefaultHeight => -1;

        public static UnityObjectWidget GetUnityWidget(object target, Type targetType, ReflectionInspector inspector)
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                return null;

            UnityObjectWidget widget = target switch
            {
                Texture2D or Cubemap => Pool<Texture2DWidget>.Borrow(),
                Sprite s when s.texture => Pool<Texture2DWidget>.Borrow(),
                Image i when i.sprite?.texture => Pool<Texture2DWidget>.Borrow(),

                Material when MaterialWidget.MaterialWidgetSupported => Pool<MaterialWidget>.Borrow(),

                AudioClip => Pool<AudioClipWidget>.Borrow(),

                _ => Pool<UnityObjectWidget>.Borrow()
            };

            widget.OnBorrowed(target, targetType, inspector);

            return widget;
        }

        public virtual void OnBorrowed(object target, Type targetType, ReflectionInspector inspector)
        {
            this.owner = inspector;

            if (!this.UIRoot)
                CreateContent(inspector.UIRoot);
            else
                this.UIRoot.transform.SetParent(inspector.UIRoot.transform);

            this.UIRoot.transform.SetSiblingIndex(inspector.UIRoot.transform.childCount - 2);

            unityObject = target.TryCast<UnityEngine.Object>();
            UIRoot.SetActive(true);

            nameInput.Text = unityObject.name;
            instanceIdInput.Text = unityObject.GetInstanceID().ToString();

            if (typeof(Component).IsAssignableFrom(targetType))
            {
                component = (Component)target.TryCast(typeof(Component));
                gameObjectButton.Component.gameObject.SetActive(true);
            }
            else
                gameObjectButton.Component.gameObject.SetActive(false);
        }

        public virtual void OnReturnToPool()
        {
            unityObject = null;
            component = null;
            owner = null;
        }

        // Update

        public virtual void Update()
        {
            if (this.unityObject)
            {
                nameInput.Text = unityObject.name;
                
                owner.Tab.TabText.text = $"{owner.TabButtonText} \"{unityObject.name}\"";
            }
        }

        // UI Listeners

        private void OnGameObjectButtonClicked()
        {
            if (!component)
            {
                ExplorerCore.LogWarning("Component reference is null or destroyed!");
                return;
            }

            InspectorManager.Inspect(component.gameObject);
        }

        // UI construction

        public virtual GameObject CreateContent(GameObject uiRoot)
        {
            UIRoot = UIFactory.CreateUIObject("UnityObjectRow", uiRoot);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(UIRoot, false, false, true, true, 5);
            UIFactory.SetLayoutElement(UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 9999);

            Text nameLabel = UIFactory.CreateLabel(UIRoot, "NameLabel", "Name:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(nameLabel.gameObject, minHeight: 25, minWidth: 45, flexibleWidth: 0);

            nameInput = UIFactory.CreateInputField(UIRoot, "NameInput", "untitled");
            UIFactory.SetLayoutElement(nameInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 1000);
            nameInput.Component.readOnly = true;

            gameObjectButton = UIFactory.CreateButton(UIRoot, "GameObjectButton", "Inspect GameObject", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(gameObjectButton.Component.gameObject, minHeight: 25, minWidth: 160);
            gameObjectButton.OnClick += OnGameObjectButtonClicked;

            Text instanceLabel = UIFactory.CreateLabel(UIRoot, "InstanceLabel", "Instance ID:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(instanceLabel.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);

            instanceIdInput = UIFactory.CreateInputField(UIRoot, "InstanceIDInput", "ERROR");
            UIFactory.SetLayoutElement(instanceIdInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            instanceIdInput.Component.readOnly = true;

            UIRoot.SetActive(false);

            return UIRoot;
        }
    }
}
