using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.UI;

namespace Explorer.CacheObject
{
    public class CacheEnumerated : CacheObjectBase
    {
        public int Index { get; set; }
        public IList RefIList { get; set; }
        public InteractiveEnumerable ParentEnumeration { get; set; }

        public override bool CanWrite => RefIList != null && ParentEnumeration.OwnerCacheObject.CanWrite;

        public override void SetValue()
        {
            RefIList[Index] = IValue.Value;
            ParentEnumeration.Value = RefIList;

            ParentEnumeration.OwnerCacheObject.SetValue();
        }
    }
}
