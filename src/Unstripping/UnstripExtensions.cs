using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    // This is a copy+paste of UnityEngine source code, fixed for Il2Cpp.
    // Taken from dnSpy output using Unity 2018.4.20.

    // Subject to Unity's License and ToS.
    // https://unity3d.com/legal/terms-of-service
    // https://unity3d.com/legal/terms-of-service/software

    public static class UnstripExtensions
    {
        // This is a manual unstrip of GUILayoutGroup.GetLast().
        // I'm using it as an Extension because it's easier this way.

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
