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

        private static GenericStack ScrollStack
        {
            get
            {
#if Release_2019
                return GUI.scrollViewStates;
#else 
                return GUI.s_ScrollViewStates;
#endif
            }
        }

        // ======= public methods ======= //   

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

        public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle style)
        {
            return Scroller_Impl(position, value, size, leftValue, rightValue, style, 
                GUI.skin.GetStyle(style.name + "thumb"), 
                GUI.skin.GetStyle(style.name + "leftbutton"), 
                GUI.skin.GetStyle(style.name + "rightbutton"), 
                true);
        }

        public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue, GUIStyle style)
        {
            return Scroller_Impl(position, value, size, topValue, bottomValue, style, 
                GUI.skin.GetStyle(style.name + "thumb"), 
                GUI.skin.GetStyle(style.name + "upbutton"), 
                GUI.skin.GetStyle(style.name + "downbutton"), 
                false);
        }

        // Fix for BeginScrollView.

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
                    return scroll;
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
                    MelonLogger.Log("Exception on GUIUnstrip.BeginScrollView_ImplLayout: " + e.GetType() + ", " + e.Message + "\r\n" + e.StackTrace);

                    ManualUnstripFailed = true;
                    return scroll;
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

        // ======= private methods ======= //

        private static Vector2 BeginScrollView_ImplLayout(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, 
            GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options)
        {
            GUIUtility.CheckOnGUI();

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
            GUIUtility.CheckOnGUI();

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

            Rect screenRect = new Rect(position);
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
                        scrollPosition.x = HorizontalScrollbar(
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
                        scrollPosition.y = VerticalScrollbar(
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

            value = Slider(position2, value, size, leftValue, rightValue, slider, thumb, horiz, controlID);

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

        public static float Slider(Rect position, float value, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id)
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
                    GUI.nextScrollStepTime = Il2CppSystem.DateTime.Now.AddMilliseconds(250.0);
                }
                else if (Il2CppSystem.DateTime.Now >= GUI.nextScrollStepTime)
                {
                    result = true;
                    GUI.nextScrollStepTime = Il2CppSystem.DateTime.Now.AddMilliseconds(30.0);
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
