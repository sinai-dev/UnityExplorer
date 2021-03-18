using System;
using System.Collections.Generic;
using System.Linq;
using UnityExplorer.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Runtime;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI.Main.Home.Inspectors;

namespace UnityExplorer.Core.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public override string TabLabel => $" <color=cyan>[G]</color> {TargetGO?.name}";

        public static GameObjectInspector ActiveInstance { get; private set; }

        public GameObject TargetGO;

        public GameObjectInspectorUI UIModule;

        // sub modules
        internal static ChildList s_childList;
        internal static ComponentList s_compList;
        internal static GameObjectControls s_controls;

        internal static bool m_UIConstructed;

        public GameObjectInspector(GameObject target) : base(target)
        {
            ActiveInstance = this;

            TargetGO = target;

            if (!TargetGO)
            {
                ExplorerCore.LogWarning("Target GameObject is null!");
                return;
            }

            // one UI is used for all gameobject inspectors. no point recreating it.
            if (!m_UIConstructed)
            {
                m_UIConstructed = true;

                s_childList = new ChildList();
                s_compList = new ComponentList();
                s_controls = new GameObjectControls();

                UIModule.ConstructUI();
            }
        }

        public override void SetActive()
        {
            base.SetActive();
            ActiveInstance = this;
        }

        public override void SetInactive()
        {
            base.SetInactive();
            ActiveInstance = null;
        }

        internal void ChangeInspectorTarget(GameObject newTarget)
        {
            if (!newTarget)
                return;

            this.Target = this.TargetGO = newTarget;
        }

        // Update

        public override void Update()
        {
            base.Update();

            if (m_pendingDestroy || !this.IsActive)
                return;

            UIModule.RefreshTopInfo();

            s_childList.RefreshChildObjectList();

            s_compList.RefreshComponentList();

            s_controls.RefreshControls();

            if (GameObjectControls.s_sliderChangedWanted)
                GameObjectControls.UpdateSliderControl();
        }

        public override void CreateUIModule()
        {
            base.BaseUI = UIModule = new GameObjectInspectorUI();
        }
    }
}
