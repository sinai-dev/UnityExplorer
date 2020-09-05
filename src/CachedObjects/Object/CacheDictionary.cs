using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CacheDictionary : CacheObjectBase
    {


        public override void Init()
        {
            //base.Init();

            Value = "Unsupported";
        }

        public override void UpdateValue()
        {
            //base.UpdateValue();


        }

        public override void DrawValue(Rect window, float width)
        {
            GUILayout.Label("<color=red>Dictionary (unsupported)</color>", null);
        }
    }
}
