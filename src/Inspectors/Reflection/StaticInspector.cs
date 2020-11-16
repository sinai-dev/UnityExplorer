using System;

namespace UnityExplorer.Inspectors.Reflection
{
    public class StaticInspector : ReflectionInspector
    {
        public override string TabLabel => $" <color=cyan>[S]</color> {base.TabLabel}";

        public StaticInspector(Type type) : base(type) { }
    }
}
