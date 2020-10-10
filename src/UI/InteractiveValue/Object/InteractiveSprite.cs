using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer.UI
{
    public class InteractiveSprite : InteractiveTexture2D
    {
        public override void GetTexture2D()
        {
#if CPP
            if (Value != null && Value.Il2CppCast(typeof(Sprite)) is Sprite sprite)
#else
            if (Value is Sprite sprite)
#endif
            {
                currentTex = sprite.texture;
                texContent = new GUIContent
                {
                    image = currentTex
                };
            }
        }

    }
}
