using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;
using System.Linq;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer.UI
{
    public class InteractiveEnumerable : InteractiveValue, IExpandHeight
    {
        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public PageHelper Pages = new PageHelper();

        private CacheEnumerated[] m_cachedEntries = new CacheEnumerated[0];

        // Type of Entries in the Array
        public Type EntryType
        {
            get => GetEntryType();
            set => m_entryType = value;
        }
        private Type m_entryType;

        // Cached IEnumerable object
        public IEnumerable Enumerable
        {
            get => GetEnumerable();
        }
        private IEnumerable m_enumerable;

        // Generic Type Definition for Lists
        public Type GenericTypeDef
        {
            get => GetGenericTypeDef();
        }
        private Type m_genericTypeDef;

        // Cached ToArray method for Lists
        public MethodInfo CppListToArrayMethod
        {
            get => GetGenericToArrayMethod();
        }
        private MethodInfo m_genericToArray;

        // Cached Item Property for ILists
        public PropertyInfo ItemProperty
        {
            get => GetItemProperty();
        }

        private PropertyInfo m_itemProperty;

        // ========== Methods ==========

        private IEnumerable GetEnumerable()
        {
            if (m_enumerable == null && Value != null)
            {
                m_enumerable = Value as IEnumerable ?? EnumerateWithReflection();
            }
            return m_enumerable;
        }

        private Type GetGenericTypeDef()
        {
            if (m_genericTypeDef == null && Value != null)
            {
                var type = Value.GetType();
                if (type.IsGenericType)
                {
                    m_genericTypeDef = type.GetGenericTypeDefinition();
                }
            }
            return m_genericTypeDef;
        }

        private MethodInfo GetGenericToArrayMethod()
        {
            if (GenericTypeDef == null) return null;

            if (m_genericToArray == null)
            {
                m_genericToArray = GenericTypeDef
                                    .MakeGenericType(new Type[] { this.EntryType })
                                    .GetMethod("ToArray");
            }
            return m_genericToArray;
        }

        private PropertyInfo GetItemProperty()
        {
            if (m_itemProperty == null)
            {
                m_itemProperty = Value?.GetType().GetProperty("Item");
            }
            return m_itemProperty;
        }

        private IEnumerable EnumerateWithReflection()
        {
            if (Value == null) return null;

#if CPP
            if (GenericTypeDef == typeof(Il2CppSystem.Collections.Generic.List<>))
            {
                return (IEnumerable)CppListToArrayMethod?.Invoke(Value, new object[0]);
            }
            else if (GenericTypeDef == typeof(Il2CppSystem.Collections.Generic.HashSet<>))
            {
                return CppHashSetToMono();
            }
            else
            {
                return CppIListToMono();
            }
#else
            return Value as IEnumerable;
#endif
        }

#if CPP
        private IEnumerable CppHashSetToMono()
        {
            var set = new HashSet<object>();

            // invoke GetEnumerator
            var enumerator = Value.GetType().GetMethod("GetEnumerator").Invoke(Value, null);
            // get the type of it
            var enumeratorType = enumerator.GetType();
            // reflect MoveNext and Current
            var moveNext = enumeratorType.GetMethod("MoveNext");
            var current = enumeratorType.GetProperty("Current");
            // iterate
            while ((bool)moveNext.Invoke(enumerator, null))
            {
                set.Add(current.GetValue(enumerator));
            }

            return set;
        }

        private IList CppIListToMono()
        {
            try
            {
                var genericType = typeof(List<>).MakeGenericType(new Type[] { this.EntryType });
                var list = (IList)Activator.CreateInstance(genericType);

                for (int i = 0; ; i++)
                {
                    try
                    {
                        var itm = ItemProperty.GetValue(Value, new object[] { i });
                        list.Add(itm);
                    }
                    catch { break; }
                }

                return list;
            }
            catch (Exception e)
            {
                ExplorerCore.Log("Exception converting Il2Cpp IList to Mono IList: " + e.GetType() + ", " + e.Message);
                return null;
            }
        }
#endif

        private Type GetEntryType()
        {
            if (ValueType.IsGenericType)
            {
                var gArgs = ValueType.GetGenericArguments();

                if (ValueType.FullName.Contains("ValueCollection"))
                {
                    m_entryType = gArgs[gArgs.Length - 1];
                }
                else
                {
                    m_entryType = gArgs[0];
                }
            }
            else
            {
                m_entryType = typeof(object);
            }

            return m_entryType;
        }

        public override void UpdateValue()
        {
            base.UpdateValue();

            if (Value == null || Enumerable == null)
            {
                return;
            }

            CacheEntries();
        }

        public void CacheEntries()
        {
            var enumerator = Enumerable.GetEnumerator();
            if (enumerator == null)
            {
                return;
            }

            var list = new List<CacheEnumerated>();
            int index = 0;
            while (enumerator.MoveNext())
            {
                var obj = enumerator.Current;

                if (obj != null && ReflectionHelpers.GetActualType(obj) is Type t)
                {
#if CPP
                    if (obj is Il2CppSystem.Object iObj)
                    {
                        try
                        {
                            var cast = iObj.Il2CppCast(t);
                            if (cast != null)
                            {
                                obj = cast;
                            }
                        }
                        catch { }
                    }
#endif

                    //ExplorerCore.Log("Caching enumeration entry " + obj.ToString() + " as " + EntryType.FullName);

                    var cached = new CacheEnumerated() { Index = index, RefIList = Value as IList, ParentEnumeration = this };
                    cached.Init(obj, EntryType);
                    list.Add(cached);
                }
                else
                {
                    list.Add(null);
                }

                index++;
            }

            m_cachedEntries = list.ToArray();
        }

        // ============= GUI Draw =============

        public override void DrawValue(Rect window, float width)
        {
            if (m_cachedEntries == null)
            {
                GUILayout.Label("m_cachedEntries is null!", new GUILayoutOption[0]);
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

            int count = m_cachedEntries.Length;

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            string btnLabel = $"[{count}] <color=#2df7b2>{EntryType.FullName}</color>";
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
                    var entry = m_cachedEntries[i];

                    //collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                    GUILayout.EndHorizontal();
                    GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);

                    GUIUnstrip.Space(whitespace);

                    if (entry == null || entry.IValue == null)
                    {
                        GUILayout.Label($"[{i}] <i><color=grey>(null)</color></i>", new GUILayoutOption[0]);
                    }
                    else
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label($"[{i}]", new GUILayoutOption[] { GUILayout.Width(30) });

                        GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                        entry.IValue.DrawValue(window, window.width - (whitespace + 85));
                    }

                }

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
        }
    }
}
