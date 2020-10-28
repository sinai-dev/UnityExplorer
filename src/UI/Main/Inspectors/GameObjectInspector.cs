using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

            if (!TargetGO)
            {
                ExplorerCore.LogWarning("GameObjectInspector cctor: Target GameObject is null!");
                return;
            }

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
            var parent = InspectorManager.Instance.m_inspectorContent;

            this.Content = UIFactory.CreateScrollView(parent, out GameObject scrollContent, new Color(0.1f, 0.1f, 0.1f, 1));

            var nameObj = UIFactory.CreateLabel(scrollContent, TextAnchor.MiddleLeft);
            var nameText = nameObj.GetComponent<Text>();
            nameText.text = TargetGO.name;
            nameText.fontSize = 18;

            var childListObj = UIFactory.CreateLabel(scrollContent, TextAnchor.MiddleLeft);
            var childListText = childListObj.GetComponent<Text>();
            childListText.text = "Children:";

            foreach (Transform child in TargetGO.transform)
            {
                var childLabelObj = UIFactory.CreateLabel(scrollContent, TextAnchor.MiddleLeft);
                var childLabelText = childLabelObj.GetComponent<Text>();
                childLabelText.text = " - " + child.name;
            }

            var compListObj = UIFactory.CreateLabel(scrollContent, TextAnchor.MiddleLeft);
            var compListText = compListObj.GetComponent<Text>();
            compListText.text = "Components:";

            foreach (var comp in TargetGO.GetComponents<Component>())
            {
                var compLabelObj = UIFactory.CreateLabel(scrollContent, TextAnchor.MiddleLeft);
                var compText = compLabelObj.GetComponent<Text>();
                compText.text = " - " + comp.GetType().Name;
            }


        }

        #endregion
    }
}
