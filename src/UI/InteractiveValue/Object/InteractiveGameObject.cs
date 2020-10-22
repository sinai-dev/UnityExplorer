using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI
{
    public class InteractiveGameObject : InteractiveValue
    {


        public override void DrawValue(Rect window, float width)
        {
            Buttons.GameObjectButton(Value, null, false, width);
        }

        public override void UpdateValue()
        {
            base.UpdateValue();
        }
    }
}
