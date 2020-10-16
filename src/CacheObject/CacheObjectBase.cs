using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Explorer.UI;
using Explorer.UI.Shared;
using Explorer.Helpers;

namespace Explorer.CacheObject
{
    public class CacheObjectBase
    {
        public InteractiveValue IValue;

        public virtual bool CanWrite => false;
        public virtual bool HasParameters => false;
        public virtual bool IsMember => false;

        public bool IsStaticClassSearchResult { get; set; }

        public virtual void Init(object obj, Type valueType)
        {
            if (valueType == null && obj == null)
            {
                return;
            }

            //ExplorerCore.Log("Initializing InteractiveValue of type " + valueType.FullName);

            InteractiveValue interactive;

            if (valueType == typeof(GameObject) || valueType == typeof(Transform))
            {
                interactive = new InteractiveGameObject();
            }
            else if (valueType == typeof(Texture2D))
            {
                interactive = new InteractiveTexture2D();
            }
            else if (valueType == typeof(Texture))
            {
                interactive = new InteractiveTexture();
            }
            else if (valueType == typeof(Sprite))
            {
                interactive = new InteractiveSprite();
            }
            else if (valueType.IsPrimitive || valueType == typeof(string))
            {
                interactive = new InteractivePrimitive();
            }
            else if (valueType.IsEnum)
            {
                if (valueType.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] attributes && attributes.Length > 0)
                {
                    interactive = new InteractiveFlags();
                }
                else
                {
                    interactive = new InteractiveEnum();
                }
            }
            else if (valueType == typeof(Vector2) || valueType == typeof(Vector3) || valueType == typeof(Vector4))
            {
                interactive = new InteractiveVector();
            }
            else if (valueType == typeof(Quaternion))
            {
                interactive = new InteractiveQuaternion();
            }
            else if (valueType == typeof(Color))
            {
                interactive = new InteractiveColor();
            }
            else if (valueType == typeof(Rect))
            {
                interactive = new InteractiveRect();
            }
            // must check this before IsEnumerable
            else if (ReflectionHelpers.IsDictionary(valueType))
            {
                interactive = new InteractiveDictionary();
            }
            else if (ReflectionHelpers.IsEnumerable(valueType))
            {
                interactive = new InteractiveEnumerable();
            }
            else
            {
                interactive = new InteractiveValue();
            }

            interactive.Value = obj;
            interactive.ValueType = valueType;

            this.IValue = interactive;
            this.IValue.OwnerCacheObject = this;

            UpdateValue();

            this.IValue.Init();
        }

        public virtual void Draw(Rect window, float width)
        {
            IValue.Draw(window, width);
        }

        public virtual void UpdateValue()
        {
            IValue.UpdateValue();
        }

        public virtual void SetValue() => throw new NotImplementedException();
    }
}
