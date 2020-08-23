using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CacheGameObject : CacheObject
    {
        private GameObject GameObj
        {
            get 
            {
                if (m_gameObject == null)
                {
                    if (Value is Il2CppSystem.Object ilObj)
                    {
                        var ilType = ilObj.GetIl2CppType();

                        if (ilType == ReflectionHelpers.GameObjectType || ilType == ReflectionHelpers.TransformType)
                        {
                            m_gameObject = ilObj.TryCast<GameObject>() ?? ilObj.TryCast<Transform>()?.gameObject;
                        }
                    }
                }

                return m_gameObject;
            }
        }

        private GameObject m_gameObject;

        public override void DrawValue(Rect window, float width)
        {
            UIHelpers.GameobjButton(GameObj, null, false, width);
        }

        public override void UpdateValue()
        {
            base.UpdateValue();
        }
    }
}
