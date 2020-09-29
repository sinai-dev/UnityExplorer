using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
#if CPP
using Explorer.UnstripInternals;
using UnityEngineInternal;
using UnhollowerRuntimeLib;
#endif

namespace Explorer
{
    public class GUIUnstrip
    {
        public static string TextField(string text, GUILayoutOption[] options)
        {
#if CPP
            return Internal.TextField(text, options);
#else
            return GUIUnstrip.TextField(text, options);
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

        public static string TextArea(string text, params GUILayoutOption[] options)
        {
#if CPP
            return GUILayout.DoTextField(text, -1, true, GUI.skin.textArea, options);
#else
            return GUILayout.TextArea(text, options);
#endif
        }

        public static void BringWindowToFront(int id)
        {
#if CPP
            Internal.BringWindowToFront(id);
#else
            GUI.BringWindowToFront(id);
#endif
        }

        public static Vector2 ScreenToGUIPoint(Vector2 screenPoint)
        {
#if CPP
            return Internal.ScreenToGUIPoint(screenPoint);
#else
            return GUIUtility.ScreenToGUIPoint(screenPoint);
#endif
        }

        public static void Space(float pixels)
        {
#if CPP
            Internal.Space(pixels);
#else
            GUILayout.Space(pixels);
#endif
        }

#if CPP
        public static bool RepeatButton(string text, params GUILayoutOption[] options) 
        { 
            return Internal.DoRepeatButton(GUIContent.Temp(text), GUI.skin.button, options); 
        }
#else
        public static bool RepeatButton(string text, params GUILayoutOption[] args)
        {
            return GUILayout.RepeatButton(text, args);
        }
#endif

#if CPP
        public static void BeginArea(Rect screenRect, GUIStyle style) 
        {
            Internal.BeginArea(screenRect, GUIContent.none, style);            
        }
#else
        public static void BeginArea(Rect rect, GUIStyle skin)
        {
            GUILayout.BeginArea(rect, skin);
        }
#endif

#if CPP
        static public void EndArea()
        {
            Internal.EndArea();
        }
#else
        public static void EndArea()
        {
            GUILayout.EndArea();
        }
#endif


#if CPP
        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] options)
        {
            return Internal.BeginScrollView(scroll, options);
        }

        public static void EndScrollView(bool handleScrollWheel = true)
        {
            Internal.EndScrollView(handleScrollWheel);
        }
#else
        public static Vector2 BeginScrollView(Vector2 scroll, params GUILayoutOption[] options)
        {
            return GUILayout.BeginScrollView(scroll, options);
        }

        public static void EndScrollView()
        {
            GUILayout.EndScrollView();
        }
#endif
    }
}