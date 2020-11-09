using System;
using UnityExplorer.Helpers;

namespace UnityExplorer.Inspectors.Reflection
{
    public class InstanceInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[R]</color> {base.TabLabel}";

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
