using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if CPP
using UnhollowerBaseLib;
#endif
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI
{
    public class InteractiveDictionary : InteractiveValue, IExpandHeight
    {
        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public PageHelper Pages = new PageHelper();

        private CacheObjectBase[] m_cachedKeys = new CacheObjectBase[0];
        private CacheObjectBase[] m_cachedValues = new CacheObjectBase[0];

        public Type TypeOfKeys
        {
            get
            {
                if (m_keysType == null) GetGenericArguments();
                return m_keysType;
            }
        }
        private Type m_keysType;

        public Type TypeOfValues
        {
            get
            {
                if (m_valuesType == null) GetGenericArguments();
                return m_valuesType;
            }
        }
        private Type m_valuesType;

        public IDictionary IDict
        {
            get => m_iDictionary ?? (m_iDictionary = Value as IDictionary) ?? Il2CppDictionaryToMono();
            set => m_iDictionary = value;
        }
        private IDictionary m_iDictionary;

        // ========== Methods ==========

        // This is a bit janky due to Il2Cpp Dictionary not implementing IDictionary.
        private IDictionary Il2CppDictionaryToMono()
        {
            // note: "ValueType" is the Dictionary itself, TypeOfValues is the 'Dictionary.Values' type.

            // get keys and values
            var keys = ValueType.GetProperty("Keys").GetValue(Value, null);
            var values = ValueType.GetProperty("Values").GetValue(Value, null);

            // create lists to hold them
            var keyList = new List<object>();
            var valueList = new List<object>();

            // store entries with reflection
            EnumerateWithReflection(keys, keyList);
            EnumerateWithReflection(values, valueList);

            // make actual mono dictionary
            var dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>)
                                             .MakeGenericType(TypeOfKeys, TypeOfValues));

            // finally iterate into dictionary
            for (int i = 0; i < keyList.Count; i++)
            {
                dict.Add(keyList[i], valueList[i]);
            }

            return dict;
        }

        private void EnumerateWithReflection(object collection, List<object> list)
        {
            // invoke GetEnumerator
            var enumerator = collection.GetType().GetMethod("GetEnumerator").Invoke(collection, null);
            // get the type of it
            var enumeratorType = enumerator.GetType();
            // reflect MoveNext and Current
            var moveNext = enumeratorType.GetMethod("MoveNext");
            var current = enumeratorType.GetProperty("Current");
            // iterate
            while ((bool)moveNext.Invoke(enumerator, null))
            {
                list.Add(current.GetValue(enumerator, null));
            }
        }

        private void GetGenericArguments()
        {
            if (ValueType.IsGenericType)
            {
                var generics = ValueType.GetGenericArguments();
                m_keysType = generics[0];
                m_valuesType = generics[1];
            }
            else
            {
                // It's non-generic, just use System.Object to allow for anything.
                m_keysType = typeof(object);
                m_valuesType = typeof(object);
            }
        }

        public override void UpdateValue()
        {
            // first make sure we won't run into a TypeInitializationException.
            if (!EnsureDictionaryIsSupported())
            {
                if (OwnerCacheObject is CacheMember cacheMember)
                {
                    cacheMember.ReflectionException = "Dictionary Type not supported with Reflection!";
                }
                return;
            }

            base.UpdateValue();

            CacheEntries();
        }

        public void CacheEntries()
        {
            // reset
            IDict = null;

            if (Value == null || IDict == null)
            {
                return;
            }

            var keys = new List<CacheObjectBase>();
            foreach (var key in IDict.Keys)
            {
                Type t = ReflectionHelpers.GetActualType(key) ?? TypeOfKeys;
                var cache = CacheFactory.GetCacheObject(key, t);
                keys.Add(cache);
            }

            var values = new List<CacheObjectBase>();
            foreach (var val in IDict.Values)
            {
                Type t = ReflectionHelpers.GetActualType(val) ?? TypeOfValues;
                var cache = CacheFactory.GetCacheObject(val, t);
                values.Add(cache);
            }

            m_cachedKeys = keys.ToArray();
            m_cachedValues = values.ToArray();
        }

        private bool EnsureDictionaryIsSupported()
        {
            if (typeof(IDictionary).IsAssignableFrom(ValueType))
            {
                return true;
            }

#if CPP
            try
            {
                return Check(TypeOfKeys) && Check(TypeOfValues);

                bool Check(Type type)
                {
                    var ptr = (IntPtr)typeof(Il2CppClassPointerStore<>)
                        .MakeGenericType(type)
                        .GetField("NativeClassPtr")
                        .GetValue(null);

                    return Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(ptr)) is Il2CppSystem.Type;
                }
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        // ============= GUI Draw =============

        public override void DrawValue(Rect window, float width)
        {
            if (m_cachedKeys == null || m_cachedValues == null)
            {
                GUILayout.Label("Cached keys or values is null!", new GUILayoutOption[0]);
                return;
            }

            var whitespace = CalcWhitespace(window);

            if (!IsExpanded)
            {
                if (GUILayout.Button("v", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = true;
                }
            }
            else
            {
                if (GUILayout.Button("^", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    IsExpanded = false;
                }
            }

            var negativeWhitespace = window.width - (whitespace + 100f);

            int count = m_cachedKeys.Length;

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            string btnLabel = $"[{count}] <color=#2df7b2>Dictionary<{TypeOfKeys.FullName}, {TypeOfValues.FullName}></color>";
            if (GUILayout.Button(btnLabel, new GUILayoutOption[] { GUILayout.Width(negativeWhitespace) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            GUIUnstrip.Space(5);

            if (IsExpanded)
            {
                Pages.ItemCount = count;

                if (count > Pages.ItemsPerPage)
                {
                    GUILayout.EndHorizontal();
                    GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                    GUIUnstrip.Space(whitespace);

                    Pages.CurrentPageLabel();

                    // prev/next page buttons
                    if (GUILayout.Button("< Prev", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        Pages.TurnPage(Turn.Left);
                    }
                    if (GUILayout.Button("Next >", new GUILayoutOption[] { GUILayout.Width(60) }))
                    {
                        Pages.TurnPage(Turn.Right);
                    }

                    Pages.DrawLimitInputArea();

                    GUIUnstrip.Space(5);
                }

                int offset = Pages.CalculateOffsetIndex();

                for (int i = offset; i < offset + Pages.ItemsPerPage && i < count; i++)
                {
                    var key = m_cachedKeys[i];
                    var val = m_cachedValues[i];

                    //collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                    GUILayout.EndHorizontal();
                    GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                    //GUIUnstrip.Space(whitespace);

                    if (key == null && val == null)
                    {
                        GUILayout.Label($"[{i}] <i><color=grey>(null)</color></i>", new GUILayoutOption[0]);
                    }
                    else
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label($"[{i}]", new GUILayoutOption[] { GUILayout.Width(40) });

                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                        GUILayout.Label("Key:", new GUILayoutOption[] { GUILayout.Width(40) });
                        if (key != null)
                            key.IValue.DrawValue(window, (window.width / 2) - 80f);
                        else
                            GUILayout.Label("<i>null</i>", new GUILayoutOption[0]);

                        GUILayout.Label("Value:", new GUILayoutOption[] { GUILayout.Width(40) });
                        if (val != null)
                            val.IValue.DrawValue(window, (window.width / 2) - 80f);
                        else
                            GUILayout.Label("<i>null</i>", new GUILayoutOption[0]);
                    }

                }

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
        }
    }
}
