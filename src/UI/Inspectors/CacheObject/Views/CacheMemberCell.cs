using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.Inspectors.CacheObject.Views
{
    public class CacheMemberCell : CacheObjectCell
    {
        public ReflectionInspector Owner { get; set; }

        public CacheMember MemberOccupant => Occupant as CacheMember;

        public GameObject EvaluateHolder;
        public ButtonRef EvaluateButton;

        //public Toggle UpdateToggle;

        protected virtual void EvaluateClicked()
        {
            // TODO
        }

        protected override void ConstructEvaluateHolder(GameObject parent)
        {
            // Evaluate vert group

            EvaluateHolder = UIFactory.CreateUIObject("EvalGroup", parent);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(EvaluateHolder, false, false, true, true, 3);
            UIFactory.SetLayoutElement(EvaluateHolder, minHeight: 25, flexibleWidth: 9999, flexibleHeight: 775);

            EvaluateButton = UIFactory.CreateButton(EvaluateHolder, "EvaluateButton", "Evaluate", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(EvaluateButton.Button.gameObject, minWidth: 100, minHeight: 25);
            EvaluateButton.OnClick += EvaluateClicked;
        }

        //protected override void ConstructUpdateToggle(GameObject parent)
        //{
        //    // Auto-update toggle
        //
        //    var updateToggle = UIFactory.CreateToggle(parent, "AutoUpdate", out UpdateToggle, out Text autoText);
        //    UIFactory.SetLayoutElement(updateToggle, minHeight: 25, minWidth: 30, flexibleWidth: 0, flexibleHeight: 0);
        //    GameObject.Destroy(autoText);
        //    UpdateToggle.isOn = false;
        //    UpdateToggle.onValueChanged.AddListener((bool val) => { MemberOccupant.AutoUpdateWanted = val; });
        //}
    }
}
