using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public static class UnstripExtensions
    {
        public static Rect GetLastUnstripped(this GUILayoutGroup group)
        {
            Rect result;
            if (group.m_Cursor == 0)
            {
                Debug.LogError("You cannot call GetLast immediately after beginning a group.");
                result = GUILayoutEntry.kDummyRect;
            }
            else if (group.m_Cursor <= group.entries.Count)
            {
                GUILayoutEntry guilayoutEntry = group.entries[group.m_Cursor - 1];
                result = guilayoutEntry.rect;
            }
            else
            {
                Debug.LogError(string.Concat(new object[]
                {
                    "Getting control ",
                    group.m_Cursor,
                    "'s position in a group with only ",
                    group.entries.Count,
                    " controls when doing ",
                    Event.current.type
                }));
                result = GUILayoutEntry.kDummyRect;
            }
            return result;
        }
    }
}
