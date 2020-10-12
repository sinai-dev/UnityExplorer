using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI
{
    public class InteractiveValue
    {
        public const float MAX_LABEL_WIDTH = 400f;
        public const string EVALUATE_LABEL = "<color=lime>Evaluate</color>";

        public CacheObjectBase OwnerCacheObject;

        public object Value { get; set; }
        public Type ValueType;

        public string ButtonLabel => m_btnLabel ?? GetButtonLabel();
        private string m_btnLabel;

        public MethodInfo ToStringMethod => m_toStringMethod ?? GetToStringMethod();
        private MethodInfo m_toStringMethod;

        public virtual void Init() 
        {
            UpdateValue();
        }

        public virtual void UpdateValue()
        {
            GetButtonLabel();
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

            var cacheMember = OwnerCacheObject as CacheMember;

            if (cacheMember != null && cacheMember.MemInfo != null)
            {
                GUILayout.Label(cacheMember.RichTextName, new GUILayoutOption[] { GUILayout.Width(labelWidth) });
            }
            else
            {
                GUIUnstrip.Space(labelWidth);
            }

            var cacheMethod = OwnerCacheObject as CacheMethod;

            if (cacheMember != null && cacheMember.HasParameters)
            {
                GUIUnstrip.BeginVertical(new GUILayoutOption[] { GUILayout.ExpandHeight(true) } );

                if (cacheMember.m_isEvaluating)
                {
                    if (cacheMethod != null && cacheMethod.GenericArgs.Length > 0)
                    {
                        cacheMethod.DrawGenericArgsInput();
                    }

                    if (cacheMember.m_arguments.Length > 0)
                    {
                        cacheMember.DrawArgsInput();
                    }

                    GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                    if (GUILayout.Button(EVALUATE_LABEL, new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        if (cacheMethod != null)
                            cacheMethod.Evaluate();
                        else
                            cacheMember.UpdateValue();
                    }
                    if (GUILayout.Button("Cancel", new GUILayoutOption[] { GUILayout.Width(70) }))
                    {
                        cacheMember.m_isEvaluating = false;
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    var lbl = $"Evaluate (";
                    int len = cacheMember.m_arguments.Length;
                    if (cacheMethod != null) len += cacheMethod.GenericArgs.Length;
                    lbl += len + " params)";

                    if (GUILayout.Button(lbl, new GUILayoutOption[] { GUILayout.Width(150) }))
                    {
                        cacheMember.m_isEvaluating = true;
                    }
                }

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUIUnstrip.Space(labelWidth);
            }
            else if (cacheMethod != null)
            {
                if (GUILayout.Button(EVALUATE_LABEL, new GUILayoutOption[] { GUILayout.Width(70) }))
                {
                    cacheMethod.Evaluate();
                }

                GUILayout.EndHorizontal();
                GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
                GUIUnstrip.Space(labelWidth);
            }

            string typeName = $"<color={Syntax.Class_Instance}>{ValueType.FullName}</color>";

            if (cacheMember != null && !string.IsNullOrEmpty(cacheMember.ReflectionException))
            {
                GUILayout.Label("<color=red>Reflection failed!</color> (" + cacheMember.ReflectionException + ")", new GUILayoutOption[0]);
            }
            else if (cacheMember != null && (cacheMember.HasParameters || cacheMember is CacheMethod) && !cacheMember.m_evaluated)
            {
                GUILayout.Label($"<color=grey><i>Not yet evaluated</i></color> ({typeName})", new GUILayoutOption[0]);
            }
            else if ((Value == null || Value is UnityEngine.Object uObj && !uObj) && !(cacheMember is CacheMethod))
            {
                GUILayout.Label($"<i>null ({typeName})</i>", new GUILayoutOption[0]);
            }
            else
            {
                float _width = window.width - labelWidth - 90;
                if (OwnerCacheObject is CacheMethod cm)
                {
                    cm.DrawValue(window, _width);
                }
                else
                {
                    DrawValue(window, _width);
                }
            }
        }

        public virtual void DrawValue(Rect window, float width)
        {
            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            if (GUILayout.Button(ButtonLabel, new GUILayoutOption[] { GUILayout.Width(width - 15) }))
            {
                if (OwnerCacheObject.IsStaticClassSearchResult)
                {
                    WindowManager.InspectStaticReflection(Value as Type);
                }
                else
                {
                    WindowManager.InspectObject(Value, out bool _);
                }
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
        }

        private MethodInfo GetToStringMethod()
        {
            try
            {
                m_toStringMethod = ReflectionHelpers.GetActualType(Value).GetMethod("ToString", new Type[0])
                                   ?? typeof(object).GetMethod("ToString", new Type[0]);

                // test invoke
                m_toStringMethod.Invoke(Value, null);
            }
            catch
            {
                m_toStringMethod = typeof(object).GetMethod("ToString", new Type[0]);
            }
            return m_toStringMethod;
        }

        public string GetButtonLabel()
        {
            if (Value == null) return null;

            var valueType = ReflectionHelpers.GetActualType(Value);

            string label;

            if (valueType == typeof(TextAsset))
            {
                var textAsset = Value as TextAsset;

                label = textAsset.text;

                if (label.Length > 10)
                {
                    label = $"{label.Substring(0, 10)}...";
                }

                label = $"\"{label}\" {textAsset.name} (<color={Syntax.Class_Instance}>UnityEngine.TextAsset</color>)";
            }
            else
            {
                label = (string)ToStringMethod?.Invoke(Value, null) ?? Value.ToString();

                var classColor = valueType.IsAbstract && valueType.IsSealed
                    ? Syntax.Class_Static
                    : Syntax.Class_Instance;

                string typeLabel = $"<color={classColor}>{valueType.FullName}</color>";

                if (Value is UnityEngine.Object)
                {
                    label = label.Replace($"({valueType.FullName})", $"({typeLabel})");
                }
                else
                {
                    if (!label.Contains(valueType.FullName))
                    {
                        label += $" ({typeLabel})";
                    }
                    else
                    {
                        label = label.Replace(valueType.FullName, typeLabel);
                    }
                }
            }

            return m_btnLabel = label;
        }
    }
}
