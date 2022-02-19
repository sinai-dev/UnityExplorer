using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.CacheObject;

namespace UnityExplorer.Runtime
{
    public static class UnityCrashPrevention
    {
        public static void CheckPropertyInfoEvaluation(CacheProperty cacheProp)
        {
            if (cacheProp.PropertyInfo.Name == "renderingDisplaySize"
                && cacheProp.Owner.Target is Canvas canvas
                && canvas.renderMode == RenderMode.WorldSpace
                && !canvas.worldCamera)
            {
                throw new Exception("Canvas is set to RenderMode.WorldSpace but has no worldCamera, cannot get value.");
            }
        }
    }
}
