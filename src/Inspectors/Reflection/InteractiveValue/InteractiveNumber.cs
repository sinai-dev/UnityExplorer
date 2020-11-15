using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.Inspectors.Reflection
{
    public class InteractiveNumber : InteractiveValue
    {
        public InteractiveNumber(object value, Type valueType) : base(value, valueType)
        {
        }

        public override IValueTypes IValueType => IValueTypes.Number;
        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;
        public override bool WantInspectBtn => false;

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);


        }

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }
    }
}
