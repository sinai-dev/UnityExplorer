using UnityEngine;

namespace Explorer
{
    public class CacheGameObject : CacheObjectBase
    {
        public override void DrawValue(Rect window, float width)
        {
            UIHelpers.GOButton(Value, null, false, width);
        }

        public override void UpdateValue()
        {
            base.UpdateValue();
        }
    }
}
