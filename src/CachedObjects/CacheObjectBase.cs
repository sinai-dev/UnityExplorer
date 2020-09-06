using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnhollowerBaseLib;
using UnityEngine;

namespace Explorer
{
    public abstract class CacheObjectBase
    {
        public object Value;
        public string ValueTypeName;
        public Type ValueType;

        // Reflection Inspector only
        public MemberInfo MemInfo { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }
        public string ReflectionException { get; set; }

        public int PropertyIndex { get; private set; }
        private string m_propertyIndexInput = "0";

        public string RichTextName => m_richTextName ?? GetRichTextName();
        private string m_richTextName;

        public bool CanWrite
        {
            get
            {
                if (MemInfo is FieldInfo fi)
                    return !(fi.IsLiteral && !fi.IsInitOnly);
                else if (MemInfo is PropertyInfo pi)
                    return pi.CanWrite;
                else
                    return false;
            }
        }

        // ===== Abstract/Virtual Methods ===== //

        public virtual void Init() { }

        public abstract void DrawValue(Rect window, float width);

        // ===== Static Methods ===== //

        /// <summary>
        /// Get CacheObject from only an object instance
        /// Calls GetCacheObject(obj, memberInfo, declaringInstance) with (obj, null, null)</summary>
        public static CacheObjectBase GetCacheObject(object obj)
        {
            return GetCacheObject(obj, null, null);
        }

        /// <summary>
        /// Get CacheObject from an object instance and provide the value type
        /// Calls GetCacheObjectImpl directly</summary>
        public static CacheObjectBase GetCacheObject(object obj, Type valueType)
        {
            return GetCacheObjectImpl(obj, null, null, valueType);
        }

        /// <summary>
        /// Get CacheObject from only a MemberInfo and declaring instance
        /// Calls GetCacheObject(obj, memberInfo, declaringInstance) with (null, memberInfo, declaringInstance)</summary>
        public static CacheObjectBase GetCacheObject(MemberInfo memberInfo, object declaringInstance)
        {
            return GetCacheObject(null, memberInfo, declaringInstance);
        }

        /// <summary>
        /// Get CacheObject from either an object or MemberInfo, and don't provide the type.
        /// This gets the type and then calls GetCacheObjectImpl</summary>
        public static CacheObjectBase GetCacheObject(object obj, MemberInfo memberInfo, object declaringInstance)
        {
            Type type = null;

            if (memberInfo != null)
            {
                if (memberInfo is FieldInfo fi)
                {
                    type = fi.FieldType;
                }
                else if (memberInfo is PropertyInfo pi)
                {
                    type = pi.PropertyType;
                }
                else if (memberInfo is MethodInfo mi)
                {
                    type = mi.ReturnType;
                }
            }
            else if (obj != null)
            {
                type = ReflectionHelpers.GetActualType(obj);
            }

            if (type == null)
            {
                return null;
            }

            return GetCacheObjectImpl(obj, memberInfo, declaringInstance, type);
        }

        /// <summary>
        /// Actual GetCacheObject implementation (private)
        /// </summary>
        private static CacheObjectBase GetCacheObjectImpl(object obj, MemberInfo memberInfo, object declaringInstance, Type valueType)
        {
            CacheObjectBase holder;

            if (memberInfo is MethodInfo mi)
            {
                if (CacheMethod.CanEvaluate(mi))
                {
                    holder = new CacheMethod();
                }
                else
                {
                    return null;
                }
            }
            else if (valueType == typeof(GameObject) || valueType == typeof(Transform))
            {
                holder = new CacheGameObject();
            }
            else if (valueType.IsPrimitive || valueType == typeof(string))
            {
                holder = new CachePrimitive();
            }
            else if (valueType.IsEnum)
            {
                holder = new CacheEnum();
            }
            else if (valueType == typeof(Vector2) || valueType == typeof(Vector3) || valueType == typeof(Vector4))
            {
                holder = new CacheVector();
            }
            else if (valueType == typeof(Quaternion))
            {
                holder = new CacheQuaternion();
            }
            else if (valueType == typeof(Color))
            {
                holder = new CacheColor();
            }
            else if (valueType == typeof(Rect))
            {
                holder = new CacheRect();
            }
            else if (ReflectionHelpers.IsArray(valueType) || ReflectionHelpers.IsList(valueType))
            {
                holder = new CacheList();
            }
            else if (ReflectionHelpers.IsDictionary(valueType))
            {
                holder = new CacheDictionary();
            }
            else
            {
                holder = new CacheOther();
            }

            holder.Value = obj;
            holder.ValueType = valueType;
            holder.ValueTypeName = valueType.FullName;

            if (memberInfo != null)
            {
                holder.MemInfo = memberInfo;
                holder.DeclaringType = memberInfo.DeclaringType;
                holder.DeclaringInstance = declaringInstance;

                holder.UpdateValue();
            }

            holder.Init();

            return holder;
        }

        // ======== Instance Methods =========

        public virtual void UpdateValue()
        {
            if (MemInfo == null || !string.IsNullOrEmpty(ReflectionException))
            {
                return;
            }

            try
            {
                if (MemInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemInfo as FieldInfo;
                    Value = fi.GetValue(fi.IsStatic ? null : DeclaringInstance);
                }
                else if (MemInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemInfo as PropertyInfo;
                    bool isStatic = pi.GetAccessors()[0].IsStatic;
                    var target = isStatic ? null : DeclaringInstance;

                    if (pi.GetIndexParameters().Length > 0)
                    {
                        var indexes = new object[] { PropertyIndex };
                        Value = pi.GetValue(target, indexes);
                    }
                    else
                    {
                        Value = pi.GetValue(target, null);
                    }
                }

                ReflectionException = null;
            }
            catch (Exception e)
            {
                ReflectionException = ReflectionHelpers.ExceptionToString(e);
            }
        }

        public void SetValue()
        {
            try
            {
                if (MemInfo.MemberType == MemberTypes.Field)
                {
                    var fi = MemInfo as FieldInfo;
                    fi.SetValue(fi.IsStatic ? null : DeclaringInstance, Value);
                }
                else if (MemInfo.MemberType == MemberTypes.Property)
                {
                    var pi = MemInfo as PropertyInfo;

                    if (pi.GetIndexParameters().Length > 0)
                    {
                        var indexes = new object[] { PropertyIndex };
                        pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, Value, indexes);
                    }
                    else
                    {
                        pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, Value);
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.LogWarning($"Error setting value: {e.GetType()}, {e.Message}");
            }
        }

        // ========= Instance Gui Draw ==========

        public const float MAX_LABEL_WIDTH = 400f;

        public static void ClampLabelWidth(Rect window, ref float labelWidth)
        {
            float min = window.width * 0.37f;
            if (min > MAX_LABEL_WIDTH) min = MAX_LABEL_WIDTH;

            labelWidth = Mathf.Clamp(labelWidth, min, MAX_LABEL_WIDTH);
        }

        public void Draw(Rect window, float labelWidth = 215f)
        {
            if (labelWidth > 0)
            {
                ClampLabelWidth(window, ref labelWidth);
            }

            if (MemInfo != null)
            {
                var name = RichTextName;
                if (MemInfo is PropertyInfo pi && pi.GetIndexParameters().Length > 0)
                {
                    name += $"[{PropertyIndex}]";
                }

                GUILayout.Label(name, new GUILayoutOption[] { GUILayout.Width(labelWidth) });
            }
            else
            {
                GUILayout.Space(labelWidth);
            }

            if (!string.IsNullOrEmpty(ReflectionException))
            {
                GUILayout.Label("<color=red>Reflection failed!</color> (" + ReflectionException + ")", null);
            }
            else if (Value == null && MemInfo?.MemberType != MemberTypes.Method)
            {
                GUILayout.Label("<i>null (" + ValueTypeName + ")</i>", null);
            }
            else
            {
                if (MemInfo is PropertyInfo pi && pi.GetIndexParameters().Length > 0)
                {
                    GUILayout.Label("index:", new GUILayoutOption[] { GUILayout.Width(50) });

                    m_propertyIndexInput = GUILayout.TextField(m_propertyIndexInput, new GUILayoutOption[] { GUILayout.Width(100) });
                    if (GUILayout.Button("Set", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        if (int.TryParse(m_propertyIndexInput, out int i))
                        {
                            PropertyIndex = i;
                            UpdateValue();
                        }
                        else
                        {
                            MelonLogger.Log($"Could not parse '{m_propertyIndexInput}' to an int!");
                        }
                    }

                    // new line and space
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);
                    GUILayout.Space(labelWidth);
                }

                DrawValue(window, window.width - labelWidth - 90);
            }
        }

        private string GetRichTextName()
        {
            string memberColor = "";
            switch (MemInfo.MemberType)
            {
                case MemberTypes.Field:
                    memberColor = "#c266ff"; break;
                case MemberTypes.Property:
                    memberColor = "#72a6a6"; break;
                case MemberTypes.Method:
                    memberColor = "#ff8000"; break;
            };

            m_richTextName = $"<color=#2df7b2>{MemInfo.DeclaringType.Name}</color>.<color={memberColor}>{MemInfo.Name}</color>";

            if (MemInfo is MethodInfo mi)
            {
                m_richTextName += "(";
                var _params = "";
                foreach (var param in mi.GetParameters())
                {
                    if (_params != "") _params += ", ";

                    _params += $"<color=#a6e9e9>{param.Name}</color>";
                }
                m_richTextName += _params;
                m_richTextName += ")";
            }

            return m_richTextName;
        }
    }
}
