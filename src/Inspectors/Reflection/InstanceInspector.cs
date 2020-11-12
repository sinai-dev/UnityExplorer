using System;
using UnityEngine;
using UnityExplorer.Helpers;

namespace UnityExplorer.Inspectors.Reflection
{
    public class InstanceInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[R]</color> {base.TabLabel}";

        public InstanceInspector(object target) : base(target)
        {
            // needed?
        }

        public void ConstructInstanceHelpers(GameObject parent)
        {
            // todo
        }

        public void ConstructInstanceFilters(GameObject parent)
        {
            // todo
        }
    }
}
