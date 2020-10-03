using System;
using System.Reflection;
using UnityEngine;
using Explorer.Input;

namespace Explorer
{
    public static class InputManager
    {
        // If no Input modules loaded at all
        public static bool NO_INPUT { get; private set; }

        // If using new InputSystem module
        public static bool USING_NEW_INPUT { get; private set; }

        private static AbstractInput inputModule;

        public static void Init()
        {
            if (InputSystem.TKeyboard != null || TryLoadModule("Unity.InputSystem", InputSystem.TKeyboard))
            {
                USING_NEW_INPUT = true;
                inputModule = new InputSystem();
            }
            else if (LegacyInput.TInput != null || TryLoadModule("UnityEngine.Input", LegacyInput.TInput))
            {
                inputModule = new LegacyInput();
            }
            
            if (inputModule == null)
            {
                ExplorerCore.LogWarning("Could not find any Input module!");
                NO_INPUT = true;
            }
            else
            {
                inputModule.Init();
            }

            bool TryLoadModule(string dll, Type check) => ReflectionHelpers.LoadModule(dll) && check != null;
        }

        public static Vector3 MousePosition => inputModule?.MousePosition ?? Vector3.zero;

        public static bool GetKeyDown(KeyCode key) => inputModule?.GetKeyDown(key) ?? false;

        public static bool GetKey(KeyCode key) => inputModule?.GetKey(key) ?? false;

        /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
        public static bool GetMouseButtonDown(int btn) => inputModule?.GetMouseButtonDown(btn) ?? false;

        /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
        public static bool GetMouseButton(int btn) => inputModule?.GetMouseButton(btn) ?? false;
    }
}
