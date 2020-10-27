using System;
using UnityEngine;
using UnityEngine.UI;

namespace ExplorerBeta.UI.Main.Inspectors
{
    public abstract class InspectorBase
    {
        public object Target;
        // just to cache a cast
        public UnityEngine.Object UnityTarget;

        public abstract string TabLabel { get; }

        public GameObject inspectorContent;
        public Button tabButton;
        public Text tabText;

        internal bool m_pendingDestroy;

        public InspectorBase(object target)
        {
            Target = target;
            UnityTarget = target as UnityEngine.Object;

            if (ObjectNullOrDestroyed(Target, UnityTarget))
            {
                Destroy();
                return;
            }

            AddInspectorTab();
        }

        public virtual void Update()
        {
            if (ObjectNullOrDestroyed(Target, UnityTarget))
            {
                Destroy();
                return;
            }

            tabText.text = TabLabel;
        }

        public virtual void Destroy()
        {
            m_pendingDestroy = true;

            GameObject tabGroup = tabButton?.transform.parent.gameObject;

            if (tabGroup)
            {
                GameObject.Destroy(tabGroup);
            }

            if (inspectorContent)
            {
                GameObject.Destroy(inspectorContent);
            }

            if (ReferenceEquals(InspectorManager.Instance.m_activeInspector, this))
            {
                InspectorManager.Instance.UnsetInspectorTab();
            }

            if (InspectorManager.Instance.m_currentInspectors.Contains(this))
            {
                InspectorManager.Instance.m_currentInspectors.Remove(this);
            }
        }

        public static bool ObjectNullOrDestroyed(object obj, UnityEngine.Object unityObj, bool suppressWarning = false)
        {
            if (obj == null)
            {
                if (!suppressWarning)
                {
                    ExplorerCore.LogWarning("The target instance is null!");
                }

                return true;
            }
            else if (obj is UnityEngine.Object)
            {
                if (!unityObj)
                {
                    if (!suppressWarning)
                    {
                        ExplorerCore.LogWarning("The target UnityEngine.Object was destroyed!");
                    }

                    return true;
                }
            }
            return false;
        }

        #region UI CONSTRUCTION

        public void AddInspectorTab()
        {
            var tabContent = InspectorManager.Instance.m_tabBarContent;

            var tabGroupObj = UIFactory.CreateHorizontalGroup(tabContent);
            var tabGroup = tabGroupObj.GetComponent<HorizontalLayoutGroup>();
            tabGroup.childForceExpandWidth = true;
            tabGroup.childControlWidth = true;
            var tabLayout = tabGroupObj.AddComponent<LayoutElement>();
            tabLayout.minWidth = 185;
            tabLayout.flexibleWidth = 0;
            tabGroupObj.AddComponent<Mask>();

            var targetButtonObj = UIFactory.CreateButton(tabGroupObj);
            var targetButtonLayout = targetButtonObj.AddComponent<LayoutElement>();
            targetButtonLayout.minWidth = 165;
            targetButtonLayout.flexibleWidth = 0;

            tabText = targetButtonObj.GetComponentInChildren<Text>();
            tabText.horizontalOverflow = HorizontalWrapMode.Overflow;
            tabText.alignment = TextAnchor.MiddleLeft;

            tabButton = targetButtonObj.GetComponent<Button>();
            tabButton.onClick.AddListener(new Action(() => { InspectorManager.Instance.SetInspectorTab(this); }));

            var closeBtnObj = UIFactory.CreateButton(tabGroupObj);
            var closeBtnLayout = closeBtnObj.AddComponent<LayoutElement>();
            closeBtnLayout.minWidth = 20;
            closeBtnLayout.flexibleWidth = 0;
            var closeBtnText = closeBtnObj.GetComponentInChildren<Text>();
            closeBtnText.text = "X";
            closeBtnText.color = new Color(1, 0, 0, 1);

            var closeBtn = closeBtnObj.GetComponent<Button>();
            closeBtn.onClick.AddListener(new Action(() => { Destroy(); }));
            var closeColors = closeBtn.colors;
            closeColors.normalColor = new Color(0.2f, 0.2f, 0.2f, 1);
            closeBtn.colors = closeColors;
        }

        #endregion
    }
}
