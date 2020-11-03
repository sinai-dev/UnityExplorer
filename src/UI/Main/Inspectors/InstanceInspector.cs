using System;
using UnityExplorer.Helpers;

namespace UnityExplorer.UI.Main.Inspectors
{
    public class InstanceInspector : ReflectionInspector
    {
        // todo
        public override string TabLabel => $" [R] {base.TabLabel}";

        public InstanceInspector(object target) : base(target)
        {

        }

        public override void Update()
        {
            base.Update();

            if (m_pendingDestroy || InspectorManager.Instance.m_activeInspector != this)
            {
                return;
            }

            // todo
        }
    }
}
