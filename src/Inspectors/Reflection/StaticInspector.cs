using System;

namespace UnityExplorer.Inspectors.Reflection
{
    public class StaticInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[S]</color> {base.TabLabel}";

        public StaticInspector(Type type) : base(type)
        {
            // TODO
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
