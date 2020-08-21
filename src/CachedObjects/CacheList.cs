using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public partial class CacheList : CacheObject
    {
        public bool IsExpanded { get; set; }
        public int ArrayOffset { get; set; }
        public Type EntryType { get; set; }

        private IEnumerable m_enumerable;
        private CacheObject[] m_cachedEntries;

        public CacheList(object obj)
        {
            GetEnumerable(obj);
            EntryType = m_enumerable.GetType().GetGenericArguments()[0];
        }

        private void GetEnumerable(object obj)
        {
            if (obj is IEnumerable isEnumerable)
            {
                m_enumerable = isEnumerable;
            }
            else
            {
                var listValueType = obj.GetType().GetGenericArguments()[0];
                var listType = typeof(Il2CppSystem.Collections.Generic.List<>).MakeGenericType(new Type[] { listValueType });
                var method = listType.GetMethod("ToArray");
                m_enumerable = (IEnumerable)method.Invoke(obj, new object[0]);
            }
        }

        public override void DrawValue(Rect window, float width)
        {
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

            GUI.skin.button.alignment = TextAnchor.MiddleLeft;
            string btnLabel = "<color=yellow>[" + count + "] " + EntryType + "</color>";
            if (GUILayout.Button(btnLabel, new GUILayoutOption[] { GUILayout.MaxWidth(window.width - 260) }))
            {
                WindowManager.InspectObject(Value, out bool _);
            }
            GUI.skin.button.alignment = TextAnchor.MiddleCenter;

            if (IsExpanded)
            {
                if (count > CppExplorer.ArrayLimit)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);
                    GUILayout.Space(190);
                    int maxOffset = (int)Mathf.Ceil(count / CppExplorer.ArrayLimit);
                    GUILayout.Label($"Page {ArrayOffset + 1}/{maxOffset + 1}", new GUILayoutOption[] { GUILayout.Width(80) });
                    // prev/next page buttons
                    if (GUILayout.Button("< Prev", null))
                    {
                        if (ArrayOffset > 0) ArrayOffset--;
                    }
                    if (GUILayout.Button("Next >", null))
                    {
                        if (ArrayOffset < maxOffset) ArrayOffset++;
                    }
                }

                int offset = ArrayOffset * CppExplorer.ArrayLimit;

                if (offset >= count) offset = 0;

                for (int i = offset; i < offset + CppExplorer.ArrayLimit && i < count; i++)
                {
                    var entry = m_cachedEntries[i];

                    //collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal(null);
                    GUILayout.Space(190);

                    if (entry == null)
                    {
                        GUILayout.Label("<i><color=grey>null</color></i>", null);
                    }
                    else
                    {
                        GUILayout.Label(i.ToString(), new GUILayoutOption[] { GUILayout.Width(30) });

                        entry.DrawValue(window, window.width - 250);

                        //var lbl = i + ": <color=cyan>" + obj.Value.ToString() + "</color>";

                        //if (EntryType.IsPrimitive || typeof(string).IsAssignableFrom(EntryType))
                        //{
                        //    GUILayout.Label(lbl, null);
                        //}
                        //else
                        //{
                        //    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                        //    if (GUILayout.Button(lbl, null))
                        //    {
                        //        WindowManager.InspectObject(obj, out _);
                        //    }
                        //    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                        //}
                    }
                }
            }
        }

        public override void SetValue()
        {
            throw new NotImplementedException("TODO");
        }

        public override void UpdateValue(object obj)
        {
            GetEnumerable(Value);

            var list = new List<CacheObject>();

            var enumerator = m_enumerable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                list.Add(GetCacheObject(enumerator.Current));
            }

            m_cachedEntries = list.ToArray();
        }
    }
}
