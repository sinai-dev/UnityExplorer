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
        private GameObject m_gameObject;

        public CacheGameObject(object obj)
        {
            if (obj != null)
                m_gameObject = GetGameObject(obj);
        }

        private GameObject GetGameObject(object obj)
        {
            if (obj is Il2CppSystem.Object ilObj)
            {
                var ilType = ilObj.GetIl2CppType();

                if (ilType == ReflectionHelpers.GameObjectType || ilType == ReflectionHelpers.TransformType)
                {
                    return ilObj.TryCast<GameObject>() ?? ilObj.TryCast<Transform>()?.gameObject;
                }
            }

            return null;
        }

        public override void DrawValue(Rect window, float width)
        {
            UIHelpers.GameobjButton(m_gameObject, null, false, width);
        }

        public override void SetValue()
        {
            throw new NotImplementedException("TODO");
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            m_gameObject = GetGameObject(Value);
        }
    }
}
