using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.CacheObject;

namespace Explorer.UI.Inspectors
{
    public class StaticInspector : ReflectionInspector
    {
        public override bool IsStaticInspector => true;

        public override void Init()
        {
            base.Init();
        }

        public override bool ShouldProcessMember(CacheMember holder)
        {
            return base.ShouldProcessMember(holder);
        }

        public override void WindowFunction(int windowID)
        {
            base.WindowFunction(windowID);
        }
    }
}
