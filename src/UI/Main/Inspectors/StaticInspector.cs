using System;

namespace ExplorerBeta.UI.Main.Inspectors
{
    public class StaticInspector : InspectorBase
    {
        public override string TabLabel => " [S] TODO";

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
