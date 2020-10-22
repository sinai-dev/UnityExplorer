using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Helpers;
using UnityEngine;

namespace Explorer.UI
{
    public class InteractiveSprite : InteractiveTexture2D
    {
        private Sprite refSprite;

        public override void UpdateValue()
        {
#if CPP
            if (Value != null && Value.Il2CppCast(typeof(Sprite)) is Sprite sprite)
            {
                refSprite = sprite;
            }
#else
            if (Value is Sprite sprite)
            {
                refSprite = sprite;
            }
#endif

            base.UpdateValue();
        }

        public override void GetTexture2D()
        {
            if (refSprite)
            {
                currentTex = refSprite.texture;
            }
        }

        public override void GetGUIContent()
        {
            // Check if the Sprite.textureRect is just the entire texture
            if (refSprite.textureRect != new Rect(0, 0, currentTex.width, currentTex.height))
            {
                // It's not, do a sub-copy.
                currentTex = Texture2DHelpers.Copy(refSprite.texture, refSprite.textureRect);
            }

            base.GetGUIContent();
        }
    }
}
