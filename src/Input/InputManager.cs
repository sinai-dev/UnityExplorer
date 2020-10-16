using System;
using System.Reflection;
using UnityEngine;
using Explorer.Input;
using Explorer.Helpers;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer
{
    public static class InputManager
    {
        private static IAbstractInput m_inputModule;

        public static void Init()
        {
            if (InputSystem.TKeyboard != null || (ReflectionHelpers.LoadModule("Unity.InputSystem") && InputSystem.TKeyboard != null))
            {
                m_inputModule = new InputSystem();
            }
            else if (LegacyInput.TInput != null || (ReflectionHelpers.LoadModule("UnityEngine.InputLegacyModule") && LegacyInput.TInput != null))
            {
                m_inputModule = new LegacyInput();
            }

            if (m_inputModule == null)
            {
                ExplorerCore.LogWarning("Could not find any Input module!");
                m_inputModule = new NoInput();
            }

            m_inputModule.Init();
        }

        public static Vector3 MousePosition => m_inputModule.MousePosition;

        public static bool GetKeyDown(KeyCode key) => m_inputModule.GetKeyDown(key);
        public static bool GetKey(KeyCode key) => m_inputModule.GetKey(key);

        public static bool GetMouseButtonDown(int btn) => m_inputModule.GetMouseButtonDown(btn);
        public static bool GetMouseButton(int btn) => m_inputModule.GetMouseButton(btn);

#if CPP
        internal delegate void d_ResetInputAxes();
        public static void ResetInputAxes() => ICallHelper.GetICall<d_ResetInputAxes>("UnityEngine.Input::ResetInputAxes").Invoke();
#else
        public static void ResetInputAxes() => UnityEngine.Input.ResetInputAxes();
#endif

#if CPP
#pragma warning disable IDE1006
        // public extern static string compositionString { get; }

        internal delegate IntPtr d_get_compositionString();

        public static string compositionString
        {
            get
            {
                var iCall = ICallHelper.GetICall<d_get_compositionString>("UnityEngine.Input::get_compositionString");
                return IL2CPP.Il2CppStringToManaged(iCall.Invoke());
            }
        }

        // public extern static Vector2 compositionCursorPos { get; set; }

        internal delegate void d_get_compositionCursorPos(out Vector2 ret);
        internal delegate void d_set_compositionCursorPos(ref Vector2 value);

        public static Vector2 compositionCursorPos
        {
            get
            {
                var iCall = ICallHelper.GetICall<d_get_compositionCursorPos>("UnityEngine.Input::get_compositionCursorPos_Injected");
                iCall.Invoke(out Vector2 ret);
                return ret;
            }
            set
            {
                var iCall = ICallHelper.GetICall<d_set_compositionCursorPos>("UnityEngine.Input::set_compositionCursorPos_Injected");
                iCall.Invoke(ref value);
            }
        }

#pragma warning restore IDE1006
#endif
    }
}