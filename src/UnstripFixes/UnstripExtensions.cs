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
            if (group.m_Cursor > 0 && group.m_Cursor <= group.entries.Count)
            {
                GUILayoutEntry guilayoutEntry = group.entries[group.m_Cursor - 1];
                result = guilayoutEntry.rect;
            }
            else
            {
                result = GUILayoutEntry.kDummyRect;
            }
            return result;
        }
    }
}
