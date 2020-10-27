using UnityEngine;

namespace ExplorerBeta.UI.Main.Inspectors
{
    public class GameObjectInspector : InspectorBase
    {
        public override string TabLabel => $" [G] {TargetGO?.name}";

        // just to help with casting in il2cpp
        public GameObject TargetGO;

        public GameObjectInspector(GameObject target) : base(target)
        {
            TargetGO = target;

            ConstructUI();
        }

        public override void Update()
        {
            base.Update();

            if (m_pendingDestroy || InspectorManager.Instance.m_activeInspector != this)
            {
                return;
            }

            // TODO refresh children and components
        }

        #region UI CONSTRUCTION

        private void ConstructUI()
        {
            // todo create gameobject inspector pane


        }

        #endregion
    }
}
