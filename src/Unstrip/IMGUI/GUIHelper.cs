using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if CPP
using Explorer.Unstrip.IMGUI;
#endif

namespace Explorer
{
    // All the pre-processor directive stuff is in this class to keep it separate.
    // This is so Mono build can use this class and not have to worry.

    public class GUIHelper
    {
        internal static GUILayoutOption ExpandWidth(bool expand)
        {
#if CPP
            return GUIUnstrip.ExpandWidth(expand);
#else
            return GUILayout.ExpandWidth(expand);
#endif
        }

        internal static GUILayoutOption ExpandHeight(bool expand)
        {
#if CPP
            return GUIUnstrip.ExpandHeight(expand);
#else
            return GUILayout.ExpandHeight(expand);
#endif
        }

        public static void BeginHorizontal(params GUILayoutOption[] options)
            => BeginHorizontal(GUIContent.none, GUIStyle.none, options);

        public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options)
            => BeginHorizontal(GUIContent.none, style, options);

        public static void BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
#if CPP
            GUIUnstrip.BeginLayoutDirection(false, content, style, options);
#else
            GUILayout.BeginHorizontal(content, style, options);
#endif
        }

        public static void BeginVertical(params GUILayoutOption[] options)
            => BeginVertical(GUIContent.none, GUIStyle.none, options);

        public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options)
            => BeginVertical(GUIContent.none, style, options);

        public static void BeginVertical(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
#if CPP
            GUIUnstrip.BeginLayoutDirection(true, content, style, options);
#else
            GUILayout.BeginVertical(content, style, options);
#endif
        }


        public static Rect GetLastRect()
        {
#if CPP
            return LayoutUtilityUnstrip.GetLastRect();
#else
            return GUILayoutUtility.GetLastRect(); 
#endif
        }

        public static string TextField(string text, GUILayoutOption[] options)
        {
#if CPP
            return GUIUnstrip.TextField(text, options, false);
#else
            return GUILayout.TextField(text, options);
#endif
        }

        public static string TextArea(string text, params GUILayoutOption[] options)
        {
#if CPP
            return GUIUnstrip.TextField(text, options, true);
#else
            return GUILayout.TextArea(text, options);
#endif
        }

        public static Rect Window(int id, Rect rect, GUI.WindowFunction windowFunc, string title)
        {
#if CPP
            return GUI.Window(id, rect, windowFunc, GUIContent.Temp(title), GUI.skin.window);
#else
            return GUI.Window(id, rect, windowFunc, title);
#endif
        }

        public static bool Button(Rect rect, string title)
        {
#if CPP
            return GUI.Button(rect, GUIContent.Temp(title), GUI.skin.button);
#else
            return GUI.Button(rect, title);
#endif
        }

        public static void BringWindowToFront(int id)
        {
#if CPP
            GUIUnstrip.BringWindowToFront(id);
#else
            GUI.BringWindowToFront(id);
#endif
        }

        public static Vector2 ScreenToGUIPoint(Vector2 screenPoint)
        {
#if CPP
            return GUIUnstrip.ScreenToGUIPoint(screenPoint);
#else
            return GUIUtility.ScreenToGUIPoint(screenPoint);
#endif
        }

        public static void Space(float pixels)
        {
#if CPP
            GUIUnstrip.Space(pixels);
#else
            GUILayout.Space(pixels);
#endif
        }

        public static bool RepeatButton(string text, params GUILayoutOption[] options)
        {
#if CPP
            return GUIUnstrip.DoRepeatButton(GUIContent.Temp(text), GUI.skin.button, options);
#else
            return GUILayout.RepeatButton(text, options);
#endif
        }

        public static void BeginArea(Rect screenRect, GUIStyle style)
        {
#if CPP
            GUIUnstrip.BeginArea(screenRect, GUIContent.none, style);
#else
            GUILayout.BeginArea(screenRect, style);
#endif
        }

        static public void EndArea()
        {
#if CPP
            GUIUnstrip.EndArea();
#else
            GUILayout.EndArea();
#endif
        }


        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] options)
        {
#if CPP
            return GUIUnstrip.BeginScrollView(scroll, options);
#else
            return GUILayout.BeginScrollView(scroll, options);
#endif
        }

        public static void EndScrollView(bool handleScrollWheel = true)
        {
#if CPP
            GUIUnstrip.EndScrollView(handleScrollWheel);
#else
            GUILayout.EndScrollView();
#endif
        }
    }
}