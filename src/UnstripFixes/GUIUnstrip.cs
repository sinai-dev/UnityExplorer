using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using System.Reflection;
using UnityEngineInternal;
using Harmony;

namespace Explorer
{
    public class GUIUnstrip
    {
        public static int s_ScrollControlId;

        public static bool ScrollFailed = false;
        public static bool ManualUnstripFailed = false;

        private static GenericStack ScrollStack => m_scrollStack ?? GetScrollStack();
        private static PropertyInfo m_scrollViewStatesInfo;
        private static GenericStack m_scrollStack;

        public static DateTime nextScrollStepTime;

        private static GenericStack GetScrollStack()
        {
            if (m_scrollViewStatesInfo == null)
            {
                if (typeof(GUI).GetProperty("scrollViewStates", ReflectionHelpers.CommonFlags) is PropertyInfo scrollStatesInfo)
                {
                    m_scrollViewStatesInfo = scrollStatesInfo;
                }
                else if (typeof(GUI).GetProperty("s_ScrollViewStates", ReflectionHelpers.CommonFlags) is PropertyInfo s_scrollStatesInfo)
                {
                    m_scrollViewStatesInfo = s_scrollStatesInfo;
                }
            }

            if (m_scrollViewStatesInfo?.GetValue(null, null) is GenericStack stack)
            {
                m_scrollStack = stack;
            }
            else
            {
                m_scrollStack = new GenericStack();
            }

            return m_scrollStack;
        }

        // ================= Fix for Space =================

        public static void Space(float pixels)
        {
            GUIUtility.CheckOnGUI();

            if (GUILayoutUtility.current.topLevel.isVertical)
                LayoutUtilityUnstrip.GetRect(0, pixels, GUILayoutUtility.spaceStyle, new GUILayoutOption[] { GUILayout.Height(pixels) });
            else
                LayoutUtilityUnstrip.GetRect(pixels, 0, GUILayoutUtility.spaceStyle, new GUILayoutOption[] { GUILayout.Width(pixels) });

            if (Event.current.type == EventType.Layout)
            {
                GUILayoutUtility.current.topLevel.entries[GUILayoutUtility.current.topLevel.entries.Count - 1].consideredForMargin = false;
            }
        }

        // ================= Fix for BeginArea =================

        static public void BeginArea(Rect screenRect) { BeginArea(screenRect, GUIContent.none, GUIStyle.none); }
        static public void BeginArea(Rect screenRect, string text) { BeginArea(screenRect, GUIContent.Temp(text), GUIStyle.none); }
        static public void BeginArea(Rect screenRect, Texture image) { BeginArea(screenRect, GUIContent.Temp(image), GUIStyle.none); }
        static public void BeginArea(Rect screenRect, GUIContent content) { BeginArea(screenRect, content, GUIStyle.none); }
        static public void BeginArea(Rect screenRect, GUIStyle style) { BeginArea(screenRect, GUIContent.none, style); }
        static public void BeginArea(Rect screenRect, string text, GUIStyle style) { BeginArea(screenRect, GUIContent.Temp(text), style); }
        static public void BeginArea(Rect screenRect, Texture image, GUIStyle style) { BeginArea(screenRect, GUIContent.Temp(image), style); }

        static public void BeginArea(Rect screenRect, GUIContent content, GUIStyle style)
        {
            GUILayoutGroup g = GUILayoutUtility.BeginLayoutArea(style, Il2CppType.Of<GUILayoutGroup>());
            if (Event.current.type == EventType.Layout)
            {
                g.resetCoords = true;
                g.minWidth = g.maxWidth = screenRect.width;
                g.minHeight = g.maxHeight = screenRect.height;
                g.rect = Rect.MinMaxRect(screenRect.xMin, screenRect.yMin, g.rect.xMax, g.rect.yMax);
            }

            GUI.BeginGroup(g.rect, content, style);
        }

        static public void EndArea()
        {
            if (Event.current.type == EventType.Used)
                return;
            GUILayoutUtility.current.layoutGroups.Pop();
            GUILayoutUtility.current.topLevel = GUILayoutUtility.current.layoutGroups.Peek().TryCast<GUILayoutGroup>();
            GUI.EndGroup();
        }

        // ================= Fix for BeginGroup =================

        public static void BeginGroup(Rect position) { BeginGroup(position, GUIContent.none, GUIStyle.none); }
        public static void BeginGroup(Rect position, string text) { BeginGroup(position, GUIContent.Temp(text), GUIStyle.none); }
        public static void BeginGroup(Rect position, Texture image) { BeginGroup(position, GUIContent.Temp(image), GUIStyle.none); }
        public static void BeginGroup(Rect position, GUIContent content) { BeginGroup(position, content, GUIStyle.none); }
        public static void BeginGroup(Rect position, GUIStyle style) { BeginGroup(position, GUIContent.none, style); }
        public static void BeginGroup(Rect position, string text, GUIStyle style) { BeginGroup(position, GUIContent.Temp(text), style); }
        public static void BeginGroup(Rect position, Texture image, GUIStyle style) { BeginGroup(position, GUIContent.Temp(image), style); }

        public static void BeginGroup(Rect position, GUIContent content, GUIStyle style) { BeginGroup(position, content, style, Vector2.zero); }

        internal static void BeginGroup(Rect position, GUIContent content, GUIStyle style, Vector2 scrollOffset)
        {
            int id = GUIUtility.GetControlID(GUI.s_BeginGroupHash, FocusType.Passive);

            if (content != GUIContent.none || style != GUIStyle.none)
            {
                switch (Event.current.type)
                {
                    case EventType.Repaint:
                        style.Draw(position, content, id);
                        break;
                    default:
                        if (position.Contains(Event.current.mousePosition))
                            GUIUtility.mouseUsed = true;
                        break;
                }
            }
            GUIClip.Push(position, scrollOffset, Vector2.zero, false);
        }

        public static void EndGroup()
        {
            GUIClip.Internal_Pop();
        }

        // ================= Fix for BeginVertical =================

        public static void BeginVertical(params GUILayoutOption[] options) { BeginVertical(GUIContent.none, GUIStyle.none, options); }
        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) { BeginVertical(GUIContent.none, style, options); }
        public static void BeginVertical(string text, GUIStyle style, params GUILayoutOption[] options) { BeginVertical(GUIContent.Temp(text), style, options); }
        public static void BeginVertical(Texture image, GUIStyle style, params GUILayoutOption[] options) { BeginVertical(GUIContent.Temp(image), style, options); }

        public static void BeginVertical(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            var g = GUILayoutUtility.BeginLayoutGroup(style, options, Il2CppType.Of<GUILayoutGroup>());
            g.isVertical = true;
            if (style != GUIStyle.none || content != GUIContent.none)
                GUI.Box(g.rect, content, style);
        }

        public static void EndVertical()
        {
            GUILayoutUtility.EndLayoutGroup();
        }

        // ================= Fix for BeginHorizontal ==================

        public static void BeginHorizontal(params GUILayoutOption[] options) { BeginHorizontal(GUIContent.none, GUIStyle.none, options); }
        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) { BeginHorizontal(GUIContent.none, style, options); }
        public static void BeginHorizontal(string text, GUIStyle style, params GUILayoutOption[] options) { BeginHorizontal(GUIContent.Temp(text), style, options); }

        public static void BeginHorizontal(Texture image, GUIStyle style, params GUILayoutOption[] options)
        { BeginHorizontal(GUIContent.Temp(image), style, options); }
        public static void BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayoutGroup g = GUILayoutUtility.BeginLayoutGroup(style, options, Il2CppType.Of<GUILayoutGroup>());
            g.isVertical = false;
            if (style != GUIStyle.none || content != GUIContent.none)
                GUI.Box(g.rect, content, style);
        }

        public static void EndHorizontal()
        {
            GUILayoutUtility.EndLayoutGroup();
        }

        // =========== Fix for GUI elements =============

        static public void Label(Texture image, params GUILayoutOption[] options) { DoLabel(GUIContent.Temp(image), GUI.skin.label, options); }
        static public void Label(string text, params GUILayoutOption[] options) { DoLabel(GUIContent.Temp(text), GUI.skin.label, options); }
        static public void Label(GUIContent content, params GUILayoutOption[] options) { DoLabel(content, GUI.skin.label, options); }
        static public void Label(Texture image, GUIStyle style, params GUILayoutOption[] options) { DoLabel(GUIContent.Temp(image), style, options); }
        static public void Label(string text, GUIStyle style, params GUILayoutOption[] options) { DoLabel(GUIContent.Temp(text), style, options); }
        // Make an auto-layout label.
        static public void Label(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { DoLabel(content, style, options); }
        static void DoLabel(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { GUI.Label(LayoutUtilityUnstrip.GetRect(content, style, options), content, style); }

        static public void Box(Texture image, params GUILayoutOption[] options) { DoBox(GUIContent.Temp(image), GUI.skin.box, options); }
        static public void Box(string text, params GUILayoutOption[] options) { DoBox(GUIContent.Temp(text), GUI.skin.box, options); }
        static public void Box(GUIContent content, params GUILayoutOption[] options) { DoBox(content, GUI.skin.box, options); }
        static public void Box(Texture image, GUIStyle style, params GUILayoutOption[] options) { DoBox(GUIContent.Temp(image), style, options); }
        static public void Box(string text, GUIStyle style, params GUILayoutOption[] options) { DoBox(GUIContent.Temp(text), style, options); }
        // Make an auto-layout box.
        static public void Box(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { DoBox(content, style, options); }
        static void DoBox(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { GUI.Box(LayoutUtilityUnstrip.GetRect(content, style, options), content, style); }

        static public bool Button(Texture image, params GUILayoutOption[] options) { return DoButton(GUIContent.Temp(image), GUI.skin.button, options); }
        static public bool Button(string text, params GUILayoutOption[] options) { return DoButton(GUIContent.Temp(text), GUI.skin.button, options); }
        static public bool Button(GUIContent content, params GUILayoutOption[] options) { return DoButton(content, GUI.skin.button, options); }
        static public bool Button(Texture image, GUIStyle style, params GUILayoutOption[] options) { return DoButton(GUIContent.Temp(image), style, options); }
        static public bool Button(string text, GUIStyle style, params GUILayoutOption[] options) { return DoButton(GUIContent.Temp(text), style, options); }
        // Make a single press button. The user clicks them and something happens immediately.
        static public bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { return DoButton(content, style, options); }
        static bool DoButton(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { return GUI.Button(LayoutUtilityUnstrip.GetRect(content, style, options), content, style); }

        static public bool RepeatButton(Texture image, params GUILayoutOption[] options) { return DoRepeatButton(GUIContent.Temp(image), GUI.skin.button, options); }
        static public bool RepeatButton(string text, params GUILayoutOption[] options) { return DoRepeatButton(GUIContent.Temp(text), GUI.skin.button, options); }
        static public bool RepeatButton(GUIContent content, params GUILayoutOption[] options) { return DoRepeatButton(content, GUI.skin.button, options); }
        static public bool RepeatButton(Texture image, GUIStyle style, params GUILayoutOption[] options) { return DoRepeatButton(GUIContent.Temp(image), style, options); }
        static public bool RepeatButton(string text, GUIStyle style, params GUILayoutOption[] options) { return DoRepeatButton(GUIContent.Temp(text), style, options); }
        // Make a repeating button. The button returns true as long as the user holds down the mouse
        static public bool RepeatButton(GUIContent content, GUIStyle style, params GUILayoutOption[] options) { return DoRepeatButton(content, style, options); }
        static bool DoRepeatButton(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { return GUI.RepeatButton(LayoutUtilityUnstrip.GetRect(content, style, options), content, style); }

        public static string TextField(string text, params GUILayoutOption[] options) { return DoTextField(text, -1, false, GUI.skin.textField, options); }
        public static string TextField(string text, int maxLength, params GUILayoutOption[] options) { return DoTextField(text, maxLength, false, GUI.skin.textField, options); }
        public static string TextField(string text, GUIStyle style, params GUILayoutOption[] options) { return DoTextField(text, -1, false, style, options); }
        // Make a single-line text field where the user can edit a string.
        public static string TextField(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options) { return DoTextField(text, maxLength, false, style, options); }

        public static string TextArea(string text, params GUILayoutOption[] options) { return DoTextField(text, -1, true, GUI.skin.textArea, options); }
        public static string TextArea(string text, int maxLength, params GUILayoutOption[] options) { return DoTextField(text, maxLength, true, GUI.skin.textArea, options); }
        public static string TextArea(string text, GUIStyle style, params GUILayoutOption[] options) { return DoTextField(text, -1, true, style, options); }
        // Make a multi-line text field where the user can edit a string.
        public static string TextArea(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options) { return DoTextField(text, maxLength, true, style, options); }

        static string DoTextField(string text, int maxLength, bool multiline, GUIStyle style, GUILayoutOption[] options)
        {
            int id = GUIUtility.GetControlID(FocusType.Keyboard);
            GUIContent content;
            Rect r;
            if (GUIUtility.keyboardControl != id)
                content = GUIContent.Temp(text);
            else
                content = GUIContent.Temp(text + GUIUtility.compositionString);

            r = LayoutUtilityUnstrip.GetRect(content, style, options);
            if (GUIUtility.keyboardControl == id)
                content = GUIContent.Temp(text);
            GUI.DoTextField(r, id, content, multiline, maxLength, style);
            return content.text;
        }

        static public bool Toggle(bool value, Texture image, params GUILayoutOption[] options) { return DoToggle(value, GUIContent.Temp(image), GUI.skin.toggle, options); }
        static public bool Toggle(bool value, string text, params GUILayoutOption[] options) { return DoToggle(value, GUIContent.Temp(text), GUI.skin.toggle, options); }
        static public bool Toggle(bool value, GUIContent content, params GUILayoutOption[] options) { return DoToggle(value, content, GUI.skin.toggle, options); }
        static public bool Toggle(bool value, Texture image, GUIStyle style, params GUILayoutOption[] options) { return DoToggle(value, GUIContent.Temp(image), style, options); }
        static public bool Toggle(bool value, string text, GUIStyle style, params GUILayoutOption[] options) { return DoToggle(value, GUIContent.Temp(text), style, options); }
        // Make an on/off toggle button.
        static public bool Toggle(bool value, GUIContent content, GUIStyle style, params GUILayoutOption[] options) { return DoToggle(value, content, style, options); }

        static bool DoToggle(bool value, GUIContent content, GUIStyle style, GUILayoutOption[] options)
        { return GUI.Toggle(LayoutUtilityUnstrip.GetRect(content, style, options), value, content, style); }

        // ================= Fix for BeginScrollView. =======================

        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] options)
        {
            // First, just try normal way, may not have been stripped or was unstripped successfully.
            if (!ScrollFailed)
            {
                try
                {
                    return GUILayout.BeginScrollView(scroll, options);
                }
                catch
                {
                    ScrollFailed = true;
                }
            }

            // Try manual implementation.
            if (!ManualUnstripFailed)
            {
                try
                {
                    return BeginScrollView_ImplLayout(scroll, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.scrollView, options);
                }
                catch (Exception e)
                {
                    MelonLogger.Log("Exception on manual BeginScrollView: " + e.GetType() + ", " + e.Message + "\r\n" + e.StackTrace);
                    ManualUnstripFailed = true;
                }
            }

            // Sorry! No scrolling for you.
            return scroll;
        }

        public static void EndScrollView(bool handleScrollWheel = true)
        {
            // Only end the scroll view for the relevant BeginScrollView option, if any.

            if (!ScrollFailed)
            {
                GUILayout.EndScrollView();
            }
            else if (!ManualUnstripFailed)
            {
                GUILayoutUtility.EndLayoutGroup();

                EndScrollView_Impl(handleScrollWheel);
            }
        }

        private static Vector2 BeginScrollView_ImplLayout(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, 
            GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            var guiscrollGroup = GUILayoutUtility.BeginLayoutGroup(background, null, Il2CppType.Of<GUIScrollGroup>())
                                .TryCast<GUIScrollGroup>();

            EventType type = Event.current.type;
            if (type == EventType.Layout)
            {
                guiscrollGroup.resetCoords = true;
                guiscrollGroup.isVertical = true;
                guiscrollGroup.stretchWidth = 1;
                guiscrollGroup.stretchHeight = 1;
                guiscrollGroup.verticalScrollbar = verticalScrollbar;
                guiscrollGroup.horizontalScrollbar = horizontalScrollbar;
                guiscrollGroup.needsVerticalScrollbar = alwaysShowVertical;
                guiscrollGroup.needsHorizontalScrollbar = alwaysShowHorizontal;
                guiscrollGroup.ApplyOptions(options);
            }

            return BeginScrollView_Impl(guiscrollGroup.rect,
                scrollPosition,
                new Rect(0f, 0f, guiscrollGroup.clientWidth, guiscrollGroup.clientHeight),
                alwaysShowHorizontal,
                alwaysShowVertical,
                horizontalScrollbar,
                verticalScrollbar,
                background
            );
        }

        private static Vector2 BeginScrollView_Impl(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, 
            bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            // GUIUtility.CheckOnGUI();

            int controlID = GUIUtility.GetControlID(GUI.s_ScrollviewHash, FocusType.Passive);

            var scrollViewState = GUIUtility.GetStateObject(Il2CppType.Of<ScrollViewState>(), controlID).TryCast<ScrollViewState>();

            var scrollExt = ScrollViewStateUnstrip.FromPointer(scrollViewState.Pointer);

            if (scrollExt == null) throw new Exception($"Could not get scrollExt for pointer '{scrollViewState.Pointer}'!");

            bool apply = scrollExt.apply;
            if (apply)
            {
                scrollPosition = scrollExt.scrollPosition;
                scrollExt.apply = false;
            }

            scrollExt.position = position;

            scrollExt.scrollPosition = scrollPosition;
            scrollExt.visibleRect = scrollExt.viewRect = viewRect;

            var rect = scrollExt.visibleRect;
            rect.width = position.width;
            rect.height = position.height;

            ScrollStack.Push(scrollViewState);

            Rect screenRect = new Rect(position.x, position.y, position.width, position.height);
            EventType type = Event.current.type;
            if (type != EventType.Layout)
            {
                if (type != EventType.Used)
                {
                    bool flag = alwaysShowVertical;
                    bool flag2 = alwaysShowHorizontal;
                    if (flag2 || viewRect.width > screenRect.width)
                    {
                        rect.height = position.height - horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;

                        screenRect.height -= horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;
                        flag2 = true;
                    }
                    if (flag || viewRect.height > screenRect.height)
                    {
                        rect.width = position.width - verticalScrollbar.fixedWidth + (float)verticalScrollbar.margin.left;

                        screenRect.width -= verticalScrollbar.fixedWidth + (float)verticalScrollbar.margin.left;
                        flag = true;
                        if (!flag2 && viewRect.width > screenRect.width)
                        {
                            rect.height = position.height - horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;
                            screenRect.height -= horizontalScrollbar.fixedHeight + (float)horizontalScrollbar.margin.top;
                            flag2 = true;
                        }
                    }
                    if (Event.current.type == EventType.Repaint && background != GUIStyle.none)
                    {
                        background.Draw(position, position.Contains(Event.current.mousePosition), false, flag2 && flag, false);
                    }
                    if (flag2 && horizontalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.x = HorizBar_Impl(
                            new Rect(
                                position.x,
                                position.yMax - horizontalScrollbar.fixedHeight,
                                screenRect.width,
                                horizontalScrollbar.fixedHeight),
                            scrollPosition.x,
                            Mathf.Min(screenRect.width, viewRect.width),
                            0f,
                            viewRect.width,
                            horizontalScrollbar
                        );
                    }
                    else
                    {
                        GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        scrollPosition.x = ((horizontalScrollbar == GUIStyle.none) ? Mathf.Clamp(scrollPosition.x, 0f, Mathf.Max(viewRect.width - position.width, 0f)) : 0f);
                    }
                    if (flag && verticalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.y = VertBar_Impl(
                            new Rect(
                                screenRect.xMax + (float)verticalScrollbar.margin.left,
                                screenRect.y,
                                verticalScrollbar.fixedWidth,
                                screenRect.height),
                            scrollPosition.y,
                            Mathf.Min(screenRect.height, viewRect.height),
                            0f,
                            viewRect.height,
                            verticalScrollbar
                        );
                    }
                    else
                    {
                        GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                        scrollPosition.y = ((verticalScrollbar == GUIStyle.none) ? Mathf.Clamp(scrollPosition.y, 0f, Mathf.Max(viewRect.height - position.height, 0f)) : 0f);
                    }
                }
            }
            else
            {
                GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
                GUIUtility.GetControlID(GUI.s_RepeatButtonHash, FocusType.Passive);
            }
            GUIClip.Push(screenRect, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x), Mathf.Round(-scrollPosition.y - viewRect.y)), Vector2.zero, false);

            return scrollPosition;
        }

        public static float HorizBar_Impl(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle style)
        {
            return Scroller_Impl(position, value, size, leftValue, rightValue, style,
                GUI.skin.GetStyle(style.name + "thumb"),
                GUI.skin.GetStyle(style.name + "leftbutton"),
                GUI.skin.GetStyle(style.name + "rightbutton"),
                true);
        }

        public static float VertBar_Impl(Rect position, float value, float size, float topValue, float bottomValue, GUIStyle style)
        {
            return Scroller_Impl(position, value, size, topValue, bottomValue, style,
                GUI.skin.GetStyle(style.name + "thumb"),
                GUI.skin.GetStyle(style.name + "upbutton"),
                GUI.skin.GetStyle(style.name + "downbutton"),
                false);
        }

        private static void EndScrollView_Impl(bool handleScrollWheel)
        {
            GUIUtility.CheckOnGUI();

            if (ScrollStack.Count <= 0) return;

            var state = ScrollStack.Peek().TryCast<ScrollViewState>();
            var scrollExt = ScrollViewStateUnstrip.FromPointer(state.Pointer);

            if (scrollExt == null) throw new Exception("Could not get scrollExt!");

            GUIClip.Pop();

            ScrollStack.Pop();

            var position = scrollExt.position;

            if (handleScrollWheel && Event.current.type == EventType.ScrollWheel && position.Contains(Event.current.mousePosition))
            {
                var pos = scrollExt.scrollPosition;
                pos.x = Mathf.Clamp(scrollExt.scrollPosition.x + Event.current.delta.x * 20f, 0f, scrollExt.viewRect.width - scrollExt.visibleRect.width);
                pos.y = Mathf.Clamp(scrollExt.scrollPosition.y + Event.current.delta.y * 20f, 0f, scrollExt.viewRect.height - scrollExt.visibleRect.height);

                if (scrollExt.scrollPosition.x < 0f)
                {
                    pos.x = 0f;
                }
                if (pos.y < 0f)
                {
                    pos.y = 0f;
                }

                scrollExt.apply = true;

                Event.current.Use();
            }
        }

        private static float Scroller_Impl(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            GUIUtility.CheckOnGUI();
            int controlID = GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive, position);
            Rect position2;
            Rect rect;
            Rect rect2;
            if (horiz)
            {
                position2 = new Rect(position.x + leftButton.fixedWidth, position.y, position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height);
                rect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
                rect2 = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
            }
            else
            {
                position2 = new Rect(position.x, position.y + leftButton.fixedHeight, position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight);
                rect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
                rect2 = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
            }

            value = Slider_Impl(position2, value, size, leftValue, rightValue, slider, thumb, horiz, controlID);

            bool flag = Event.current.type == EventType.MouseUp;
            if (ScrollerRepeatButton_Impl(controlID, rect, leftButton))
            {
                value -= 10f * ((leftValue >= rightValue) ? -1f : 1f);
            }
            if (ScrollerRepeatButton_Impl(controlID, rect2, rightButton))
            {
                value += 10f * ((leftValue >= rightValue) ? -1f : 1f);
            }
            if (flag && Event.current.type == EventType.Used)
            {
                s_ScrollControlId = 0;
            }
            if (leftValue < rightValue)
            {
                value = Mathf.Clamp(value, leftValue, rightValue - size);
            }
            else
            {
                value = Mathf.Clamp(value, rightValue, leftValue - size);
            }
            return value;
        }

        public static float Slider_Impl(Rect position, float value, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id)
        {
            if (id == 0)
            {
                id = GUIUtility.GetControlID(GUI.s_SliderHash, FocusType.Passive, position);
            }
            var sliderHandler = new SliderHandlerUnstrip(position, value, size, start, end, slider, thumb, horiz, id);
            return sliderHandler.Handle();
        }

        private static bool ScrollerRepeatButton_Impl(int scrollerID, Rect rect, GUIStyle style)
        {
            bool result = false;
            if (GUI.DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
            {
                bool flag = s_ScrollControlId != scrollerID;
                s_ScrollControlId = scrollerID;

                if (flag)
                {
                    result = true;
                    nextScrollStepTime = DateTime.Now.AddMilliseconds(250.0);
                }
                else if (DateTime.Now >= nextScrollStepTime)
                {
                    result = true;
                    nextScrollStepTime = DateTime.Now.AddMilliseconds(30.0);
                }
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.InternalRepaintEditorWindow();
                }
            }
            return result;
        }
    }
}
