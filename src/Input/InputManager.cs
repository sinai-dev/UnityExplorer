using System;
using UnityEngine;
using ExplorerBeta.Helpers;
using System.Diagnostics.CodeAnalysis;
#if CPP
using UnhollowerBaseLib;
#endif

namespace ExplorerBeta.Input
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Unity style")]
    public static class InputManager
    {
        private static IHandleInput m_inputModule;

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
        }

        public static Vector3 MousePosition => m_inputModule.MousePosition;

        public static bool GetKeyDown(KeyCode key) => m_inputModule.GetKeyDown(key);
        public static bool GetKey(KeyCode key) => m_inputModule.GetKey(key);

        public static bool GetMouseButtonDown(int btn) => m_inputModule.GetMouseButtonDown(btn);
        public static bool GetMouseButton(int btn) => m_inputModule.GetMouseButton(btn);
    }
}