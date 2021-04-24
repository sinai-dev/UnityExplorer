using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.UI.Inspectors.Reflection
{
    public class StaticInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[S]</color> {base.TabLabel}";

        public StaticInspector(Type type) : base(type) { }
    }
}
