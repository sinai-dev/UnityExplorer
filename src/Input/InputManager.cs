using System;
using System.Reflection;
using UnityEngine;
using Explorer.Input;

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
            else if (LegacyInput.TInput != null || TryLoadModule("UnityEngine.Input", LegacyInput.TInput))
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
    }
}
