using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Explorer
{
    public abstract class MemberInfoHolder
    {
        public Type classType;
        public bool IsExpanded = false;
        public int arrayOffset = 0;

        public abstract void Draw(ReflectionWindow window);
        public abstract void UpdateValue(object obj);
        public abstract void SetValue(object obj);
    }
}
