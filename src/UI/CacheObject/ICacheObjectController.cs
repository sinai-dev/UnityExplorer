using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.UI.CacheObject;

namespace UnityExplorer.UI.CacheObject
{
    public interface ICacheObjectController
    {
        CacheObjectBase ParentCacheObject { get; }

        object Target { get; }
        Type TargetType { get; }

        bool CanWrite { get; }
    }
}
