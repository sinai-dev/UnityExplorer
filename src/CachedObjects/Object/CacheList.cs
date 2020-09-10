using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace Explorer
{
    public class CacheList : CacheObjectBase, IExpandHeight
    {
        public bool IsExpanded { get; set; }
        public float WhiteSpace { get; set; } = 215f;

        public PageHelper Pages = new PageHelper();

        private CacheObjectBase[] m_cachedEntries;

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
        public MethodInfo GenericToArrayMethod
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
                m_enumerable = Value as IEnumerable ?? GetEnumerableFromIl2CppList();
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

        private IEnumerable GetEnumerableFromIl2CppList()
        {
            if (Value == null) return null;

            if (GenericTypeDef == typeof(Il2CppSystem.Collections.Generic.List<>))
            {
                return (IEnumerable)GenericToArrayMethod?.Invoke(Value, new object[0]);
            }
            else
            {
                return ConvertIListToMono();
            }
        }

        private IList ConvertIListToMono()
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
                MelonLogger.Log("Exception converting Il2Cpp IList to Mono IList: " + e.GetType() + ", " + e.Message);
                return null;
            }
        }

        private Type GetEntryType()
        {
            if (m_entryType == null)
            {
                if (this.MemInfo != null)
                {
                    Type memberType = null;
                    switch (this.MemInfo.MemberType)
                    {
                        case MemberTypes.Field:
                            memberType = (MemInfo as FieldInfo).FieldType;
                            break;
                        case MemberTypes.Property:
                            memberType = (MemInfo as PropertyInfo).PropertyType;
                            break;
                    }

                    if (memberType != null && memberType.IsGenericType)
                    {
                        m_entryType = memberType.GetGenericArguments()[0];
                    }
                }
                else if (Value != null)
                {
                    var type = Value.GetType();
                    if (type.IsGenericType)
                    {
                        m_entryType = type.GetGenericArguments()[0];
                    }
                }
            }

            // IList probably won't be able to get any EntryType.
            if (m_entryType == null)
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

            var enumerator = Enumerable.GetEnumerator();
            if (enumerator == null)
            {
                return;
            }

            var list = new List<CacheObjectBase>();
            while (enumerator.MoveNext())
            {
                var obj = enumerator.Current;

                if (obj != null && ReflectionHelpers.GetActualType(obj) is Type t)
                {
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

                    if (GetCacheObject(obj, t) is CacheObjectBase cached)
                    {
                        list.Add(cached);
                    }
                    else
                    {
                        list.Add(null);
                    }
                }
                else
                {
                    list.Add(null);
                }
            }

            m_cachedEntries = list.ToArray();
        }

        // ============= GUI Draw =============

        public override void DrawValue(Rect window, float width)
        {
            if (m_cachedEntries == null)
            {
                GUILayout.Label("m_cachedEntries is null!", null);
                return;
            }

            var whitespace = CalcWhitespace(window);

            int count = m_cachedEntries.Length;

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

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            string btnLabel = $"[{count}] <color=#2df7b2>{EntryType.FullName}</color>";
            if (GUILayout.Button(btnLabel, new GUILayoutOption[] { GUILayout.MaxWidth(negativeWhitespace) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            GUILayout.Space(5);

            if (IsExpanded)
            {
                Pages.ItemCount = count;

                if (count > Pages.ItemsPerPage)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);

                    GUILayout.Space(whitespace);
                 
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

                    GUILayout.Space(5);
                }

                int offset = Pages.CalculateOffsetIndex();

                for (int i = offset; i < offset + Pages.ItemsPerPage && i < count; i++)
                {
                    var entry = m_cachedEntries[i];

                    //collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);

                    GUILayout.Space(whitespace);

                    if (entry == null || entry.Value == null)
                    {
                        GUILayout.Label($"[{i}] <i><color=grey>(null)</color></i>", null);
                    }
                    else
                    {
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                        GUILayout.Label($"[{i}]", new GUILayoutOption[] { GUILayout.Width(30) });

                        entry.DrawValue(window, window.width - (whitespace + 85));                        
                    }
                    
                }

                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
        }
    }
}
