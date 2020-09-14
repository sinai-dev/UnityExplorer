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

        public MemberInfo MemInfo { get; set; }
        public Type DeclaringType { get; set; }
        public object DeclaringInstance { get; set; }

        public bool HasParameters => m_arguments != null && m_arguments.Length > 0;
        public bool m_evaluated = false;
        public bool m_isEvaluating;
        public ParameterInfo[] m_arguments = new ParameterInfo[0];
        public string[] m_argumentInput = new string[0];

        public string ReflectionException { get; set; }

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

        public virtual void Init() { }

        public abstract void DrawValue(Rect window, float width);

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

            var pi = memberInfo as PropertyInfo;
            var mi = memberInfo as MethodInfo;

            // if PropertyInfo, check if can process args
            if (pi != null && !CanProcessArgs(pi.GetIndexParameters()))
            {
                return null;
            }

            // This is pretty ugly, could probably make a cleaner implementation.
            // However, the only cleaner ways I can think of are slower and probably not worth it. 

            // Note: the order is somewhat important.

            if (mi != null)
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
            // must check this before IsEnumerable
            else if (ReflectionHelpers.IsDictionary(valueType))
            {
                holder = new CacheDictionary();
            }
            else if (ReflectionHelpers.IsEnumerable(valueType) || ReflectionHelpers.IsCppEnumerable(valueType))
            {
                holder = new CacheList();
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
            }

            if (pi != null)
            {
                holder.m_arguments = pi.GetIndexParameters();
            }
            else if (mi != null)
            {
                holder.m_arguments = mi.GetParameters();
            }

            holder.m_argumentInput = new string[holder.m_arguments.Length];
            
            holder.UpdateValue();

            holder.Init();

            return holder;
        }

        public static bool CanProcessArgs(ParameterInfo[] parameters)
        {
            foreach (var param in parameters)
            {
                if (!param.ParameterType.IsPrimitive && param.ParameterType != typeof(string))
                {
                    return false;
                }
            }

            return true;
        }

        public float CalcWhitespace(Rect window)
        {
            if (!(this is IExpandHeight)) return 0f;

            float whitespace = (this as IExpandHeight).WhiteSpace;
            if (whitespace > 0)
            {
                ClampLabelWidth(window, ref whitespace);
            }

            return whitespace;
        }

        public object[] ParseArguments()
        {
            var parsedArgs = new List<object>();
            for (int i = 0; i < m_arguments.Length; i++)
            {
                var input = m_argumentInput[i];
                var type = m_arguments[i].ParameterType;

                if (!string.IsNullOrEmpty(input))
                {
                    // strings can obviously just be used directly
                    if (type == typeof(string))
                    {
                        parsedArgs.Add(input);
                        continue;
                    }
                    else
                    {
                        // try to invoke the parse method and use that.
                        try
                        {
                            parsedArgs.Add(type.GetMethod("Parse", new Type[] { typeof(string) })
                                               .Invoke(null, new object[] { input }));

                            continue;
                        }
                        catch
                        {
                            MelonLogger.Log($"Argument #{i} '{m_arguments[i].Name}' ({type.Name}), could not parse input '{input}'.");
                        }
                    }
                }

                // Didn't use input, see if there is a default value.
                if (m_arguments[i].HasDefaultValue)
                {
                    parsedArgs.Add(m_arguments[i].DefaultValue);
                    continue;
                }

                // Try add a null arg I guess
                parsedArgs.Add(null);
            }

            return parsedArgs.ToArray();
        }

        public virtual void UpdateValue()
        {
            if (MemInfo == null)
            {
                return;
            }

            if (HasParameters && !m_isEvaluating)
            {
                // Need to enter parameters first
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
                    var target = pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance;

                    if (HasParameters)
                    {
                        Value = pi.GetValue(target, ParseArguments());
                    }
                    else
                    {
                        Value = pi.GetValue(target, null);
                    }
                }

                ReflectionException = null;
                m_evaluated = true;
                m_isEvaluating = false;
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

                    if (HasParameters)
                    {
                        pi.SetValue(pi.GetAccessors()[0].IsStatic ? null : DeclaringInstance, Value, ParseArguments());
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

        // ========= Gui Draw ==========

        public const float MAX_LABEL_WIDTH = 400f;
        public const string EVALUATE_LABEL = "<color=lime>Evaluate</color>";

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
                GUILayout.Label(RichTextName, new GUILayoutOption[] { GUILayout.Width(labelWidth) });
            }
            else
            {
                GUIUnstrip.Space(labelWidth);
            }

            var cm = this as CacheMethod;

            if (HasParameters)
            {
                GUILayout.BeginVertical(null);

                if (m_isEvaluating)
                {
                    for (int i = 0; i < m_arguments.Length; i++)
                    {
                        var name = m_arguments[i].Name;
                        var input = m_argumentInput[i];
                        var type = m_arguments[i].ParameterType.Name;

                        var label = "<color=#2df7b2>" + type + "</color> <color=#a6e9e9>" + name + "</color>";
                        if (m_arguments[i].HasDefaultValue)
                        {
                            label = $"<i>[{label} = {m_arguments[i].DefaultValue}]</i>";
                        }

                        GUILayout.BeginHorizontal(null);

                        GUILayout.Label(i.ToString(), new GUILayoutOption[] { GUILayout.Width(20) });
                        m_argumentInput[i] = GUILayout.TextField(input, new GUILayoutOption[] { GUILayout.Width(150) });
                        GUILayout.Label(label, null);

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.BeginHorizontal(null);
                    if (GUILayout.Button(EVALUATE_LABEL, new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        if (cm != null)
                        {
                            cm.Evaluate();
                        }
                        else
                        {
                            UpdateValue();
                        }
                    }
                    if (GUILayout.Button("Cancel", new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        m_isEvaluating = false;
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    if (GUILayout.Button($"Evaluate ({m_arguments.Length} params)", new GUILayoutOption[] { GUILayout.Width(150) }))
                    {
                        m_isEvaluating = true;
                    }
                }

                GUILayout.EndVertical();

                // new line and space
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(null);
                GUIUnstrip.Space(labelWidth);
            }
            else if (cm != null)
            {
                //GUILayout.BeginHorizontal(null);

                if (GUILayout.Button(EVALUATE_LABEL, new GUILayoutOption[] { GUILayout.Width(70) }))
                {
                    cm.Evaluate();
                }

                // new line and space
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(null);
                GUIUnstrip.Space(labelWidth);
            }

            if (!string.IsNullOrEmpty(ReflectionException))
            {
                GUILayout.Label("<color=red>Reflection failed!</color> (" + ReflectionException + ")", null);
            }
            else if ((HasParameters || this is CacheMethod) && !m_evaluated)
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> (<color=#2df7b2>{ValueTypeName}</color>)", null);
            }
            else if (Value == null && !(this is CacheMethod))
            {
                GUILayout.Label("<i>null (" + ValueTypeName + ")</i>", null);
            }
            else
            {
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

            if (m_arguments.Length > 0 || this is CacheMethod)
            {
                m_richTextName += "(";
                var _params = "";
                foreach (var param in m_arguments)
                {
                    if (_params != "") _params += ", ";

                    _params += $"<color=#2df7b2>{param.ParameterType.Name}</color> <color=#a6e9e9>{param.Name}</color>";
                }
                m_richTextName += _params;
                m_richTextName += ")";
            }

            return m_richTextName;
        }
    }
}
