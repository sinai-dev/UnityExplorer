using System;
using System.Reflection;
using UnityEngine;
using Explorer.Input;
#if CPP
using UnhollowerBaseLib;
#endif

namespace Explorer
{
    public static class InputManager
    {
        private static AbstractInput inputModule;

        public static void Init()
        {
            if (InputSystem.TKeyboard != null || TryLoadModule("Unity.InputSystem", InputSystem.TKeyboard))
            {
                inputModule = new InputSystem();
            }
            else if (LegacyInput.TInput != null || TryLoadModule("UnityEngine.InputLegacyModule", LegacyInput.TInput))
            {
                inputModule = new LegacyInput();
            }

            if (inputModule == null)
            {
                ExplorerCore.LogWarning("Could not find any Input module!");
                inputModule = new NoInput();
            }

            inputModule.Init();

            bool TryLoadModule(string dll, Type check) => ReflectionHelpers.LoadModule(dll) && check != null;
        }

        public static Vector3 MousePosition => inputModule.MousePosition;

        public static bool GetKeyDown(KeyCode key) => inputModule.GetKeyDown(key);
        public static bool GetKey(KeyCode key) => inputModule.GetKey(key);

        public static bool GetMouseButtonDown(int btn) => inputModule.GetMouseButtonDown(btn);
        public static bool GetMouseButton(int btn) => inputModule.GetMouseButton(btn);

#if CPP
        internal delegate void d_ResetInputAxes();
        internal static d_ResetInputAxes ResetInputAxes_iCall =>
            IL2CPP.ResolveICall<d_ResetInputAxes>("UnityEngine.Input::ResetInputAxes");

        public static void ResetInputAxes() => ResetInputAxes_iCall();
#else
        public static void ResetInputAxes() => UnityEngine.Input.ResetInputAxes();
#endif

        //#if CPP
        //#pragma warning disable IDE1006 
        //        // public extern static string compositionString { get; }

        //        internal delegate string get_compositionString_delegate();
        //        internal static get_compositionString_delegate get_compositionString_iCall =
        //            IL2CPP.ResolveICall<get_compositionString_delegate>("UnityEngine.Input::get_compositionString");

        //        public static string compositionString => get_compositionString_iCall();

        //        // public extern static Vector2 compositionCursorPos { get; set; }

        //        internal delegate Vector2 get_compositionCursorPos_delegate();
        //        internal static get_compositionCursorPos_delegate get_compositionCursorPos_iCall =
        //            IL2CPP.ResolveICall<get_compositionCursorPos_delegate>("UnityEngine.Input::get_compositionCursorPos");

        //        internal delegate void set_compositionCursorPos_delegate(Vector2 value);
        //        internal static set_compositionCursorPos_delegate set_compositionCursorPos_iCall =
        //            IL2CPP.ResolveICall<set_compositionCursorPos_delegate>("UnityEngine.Input::set_compositionCursorPos");

        //        public static Vector2 compositionCursorPos
        //        {
        //            get => get_compositionCursorPos_iCall();
        //            set => set_compositionCursorPos_iCall(value);
        //        }

        //#pragma warning restore IDE1006
        //#endif
    }
}