using UnityEngine;

namespace Explorer
{
    public class LayoutUtilityUnstrip
    {
#if CPP
        public static Rect GetRect(float width, float height) { return DoGetRect(width, width, height, height, GUIStyle.none, null); }
        public static Rect GetRect(float width, float height, GUIStyle style) { return DoGetRect(width, width, height, height, style, null); }
        public static Rect GetRect(float width, float height, params GUILayoutOption[] options) { return DoGetRect(width, width, height, height, GUIStyle.none, options); }
        // Reserve layout space for a rectangle with a fixed content area.
        public static Rect GetRect(float width, float height, GUIStyle style, params GUILayoutOption[] options)
        { return DoGetRect(width, width, height, height, style, options); }

        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, GUIStyle.none, null); }

        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, GUIStyle style)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, style, null); }

        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, params GUILayoutOption[] options)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, GUIStyle.none, options); }
        // Reserve layout space for a flexible rect.
        public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, GUIStyle style, params GUILayoutOption[] options)
        { return DoGetRect(minWidth, maxWidth, minHeight, maxHeight, style, options); }
        static Rect DoGetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, GUIStyle style, GUILayoutOption[] options)
        {
            switch (Event.current.type)
            {
                case EventType.Layout:
                    GUILayoutUtility.current.topLevel.Add(new GUILayoutEntry(minWidth, maxWidth, minHeight, maxHeight, style, options));
                    return GUILayoutUtility.kDummyRect;
                case EventType.Used:
                    return GUILayoutUtility.kDummyRect;
                default:
                    return GUILayoutUtility.current.topLevel.GetNext().rect;
            }
        }
        public static Rect GetRect(GUIContent content, GUIStyle style) { return DoGetRect(content, style, null); }
        // Reserve layout space for a rectangle for displaying some contents with a specific style.
        public static Rect GetRect(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { return DoGetRect(content, style, options); }

        static Rect DoGetRect(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            GUIUtility.CheckOnGUI();

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
                last = GUILayoutUtility.current.topLevel.GetLastUnstripped();
            }
            else
            {
                last = GUILayoutUtility.kDummyRect;
            }
            return last;
        }    
#else
        public static Rect GetLastRect()
        {
            return GUILayoutUtility.GetLastRect();
        }
#endif
    }
}