using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.CacheObject;

namespace Explorer.UI
{
    public class InteractiveEnum : InteractiveValue
    {
        internal static Dictionary<Type, string[]> EnumNamesInternalCache = new Dictionary<Type, string[]>();

        // public Type EnumType;
        public string[] EnumNames = new string[0];

        public override void Init()
        {
            if (ValueType == null && Value != null)
            {
                ValueType = Value.GetType();
            }

            if (ValueType != null)
            {
                GetNames();
            }
            else
            {
                if (OwnerCacheObject is CacheMember cacheMember)
                {
                    cacheMember.ReflectionException = "Unknown, could not get Enum names.";
                }
            }
        }

        internal void GetNames()
        {
            if (!EnumNamesInternalCache.ContainsKey(ValueType))
            {
                // using GetValues not GetNames, to catch instances of weird enums (eg CameraClearFlags)
                var values = Enum.GetValues(ValueType);

                var set = new HashSet<string>();
                foreach (var value in values)
                {
                    var v = value.ToString();
                    if (set.Contains(v)) continue;
                    set.Add(v);
                }

                EnumNamesInternalCache.Add(ValueType, set.ToArray());
            }

            EnumNames = EnumNamesInternalCache[ValueType];
        }

        public override void DrawValue(Rect window, float width)
        {
            if (OwnerCacheObject.CanWrite)
            {
                if (GUILayout.Button("<", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    SetEnum(-1);
                    OwnerCacheObject.SetValue();
                }
                if (GUILayout.Button(">", new GUILayoutOption[] { GUILayout.Width(25) }))
                {
                    SetEnum(1);
                    OwnerCacheObject.SetValue();
                }
            }

            GUILayout.Label(Value.ToString() + $"<color={Syntax.StructGreen}><i> ({ValueType})</i></color>", new GUILayoutOption[0]);
        }

        public void SetEnum(int change)
        {
            var names = EnumNames.ToList();

            int newindex = names.IndexOf(Value.ToString()) + change;

            if (newindex >= 0 && newindex < names.Count)
            {
                Value = Enum.Parse(ValueType, EnumNames[newindex]);
            }
        }
    }
}
