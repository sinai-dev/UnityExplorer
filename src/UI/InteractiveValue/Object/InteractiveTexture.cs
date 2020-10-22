using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer.UI
{
    // This class is possibly unnecessary.
    // It's just for CacheMembers that have 'Texture' as the value type, but is actually a Texture2D.

    public class InteractiveTexture : InteractiveTexture2D
    {
        public override void GetTexture2D()
        {
#if CPP
            if (Value != null && Value.Il2CppCast(typeof(Texture2D)) is Texture2D tex)
#else
            if (Value is Texture2D tex)
#endif
            {
                currentTex = tex;
                texContent = new GUIContent
                {
                    image = currentTex
                };
            }
        }
    }
}
