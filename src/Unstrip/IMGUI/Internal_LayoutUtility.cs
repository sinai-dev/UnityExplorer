#if CPP
using UnityEngine;

namespace Explorer.Unstrip.IMGUI
{
    public class Internal_LayoutUtility
    {
        public static Rect GetRect(float width, float height, GUIStyle style, params GUILayoutOption[] options)
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                    GUILayoutUtility.current.topLevel.Add(new GUILayoutEntry(width, width, height, height, style, options));
                    return GUILayoutUtility.kDummyRect;
                case EventType.Used:
                    return GUILayoutUtility.kDummyRect;
                default:
                    return GUILayoutUtility.current.topLevel.GetNext().rect;
            }
        }

        public static Rect GetRect(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            return DoGetRect(content, style, options);
        }

        static Rect DoGetRect(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                    if (style.isHeightDependantOnWidth)
                    {
                        GUILayoutUtility.current.topLevel.Add(new GUIWordWrapSizer(style, content, options));
                    }
                    else
                    {
                        Vector2 sizeConstraints = new Vector2(0, 0);
                        if (options != null)
                        {
                            foreach (var option in options)
                            {
                                if (float.TryParse(option.value.ToString(), out float f))
                                {
                                    switch (option.type)
                                    {
                                        case GUILayoutOption.Type.maxHeight:
                                            sizeConstraints.y = f;
                                            break;
                                        case GUILayoutOption.Type.maxWidth:
                                            sizeConstraints.x = f;
                                            break;
                                    }
                                }
                            }
                        }

                        Vector2 size = style.CalcSizeWithConstraints(content, sizeConstraints);
                        // This is needed on non-integer scale ratios to avoid errors to accumulate in further layout calculations
                        size.x = Mathf.Ceil(size.x);
                        size.y = Mathf.Ceil(size.y);
                        GUILayoutUtility.current.topLevel.Add(new GUILayoutEntry(size.x, size.x, size.y, size.y, style, options));
                    }
                    return GUILayoutUtility.kDummyRect;

                case EventType.Used:
                    return GUILayoutUtility.kDummyRect;
                default:
                    var entry = GUILayoutUtility.current.topLevel.GetNext();
                    //GUIDebugger.LogLayoutEntry(entry.rect, entry.marginLeft, entry.marginRight, entry.marginTop, entry.marginBottom, entry.style);
                    return entry.rect;
            }
        }

        public static Rect GetLastRect()
        {
            EventType type = Event.current.type;
            Rect last;
            if (type != EventType.Layout && type != EventType.Used)
            {
                last = GUILayoutUtility.current.topLevel.Unstripped_GetLast();
            }
            else
            {
                last = GUILayoutUtility.kDummyRect;
            }
            return last;
        }
    }
}
#endif