using System;
using ExplorerBeta.Helpers;

namespace ExplorerBeta.UI.Main.Inspectors
{
    public class InstanceInspector : InspectorBase
    {
        // todo
        public override string TabLabel => $" [R] {m_targetTypeShortName}";
        private readonly string m_targetTypeShortName;

        public InstanceInspector(object target) : base(target)
        {
            // todo

            Type type = ReflectionHelpers.GetActualType(target);

            if (type == null)
            {
                // TODO
                return;
            }

            m_targetTypeShortName = type.Name;

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
