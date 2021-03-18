using System;
using UnityEngine;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.EventSystems;

namespace UnityExplorer.Core.Input
{
    public enum InputType
    {
        InputSystem,
        Legacy,
        None
    }

    public static class InputManager
    {
        public static InputType CurrentType { get; private set; }

        private static IHandleInput m_inputModule;

        public static Vector3 MousePosition => m_inputModule.MousePosition;

        public static bool GetKeyDown(KeyCode key) => m_inputModule.GetKeyDown(key);
        public static bool GetKey(KeyCode key) => m_inputModule.GetKey(key);

        public static bool GetMouseButtonDown(int btn) => m_inputModule.GetMouseButtonDown(btn);
        public static bool GetMouseButton(int btn) => m_inputModule.GetMouseButton(btn);

        public static BaseInputModule UIInput => m_inputModule.UIModule;
        public static PointerEventData InputPointerEvent => m_inputModule.InputPointerEvent;

        public static void ActivateUIModule() => m_inputModule.ActivateModule();

        public static void AddUIModule()
        {
            m_inputModule.AddUIInputModule();
            ActivateUIModule();
        }

        public static void Init()
        {
            if (InputSystem.TKeyboard != null || (ReflectionUtility.LoadModule("Unity.InputSystem") && InputSystem.TKeyboard != null))
            {
                m_inputModule = new InputSystem();
                CurrentType = InputType.InputSystem;
            }
            else if (LegacyInput.TInput != null || (ReflectionUtility.LoadModule("UnityEngine.InputLegacyModule") && LegacyInput.TInput != null))
            {
                m_inputModule = new LegacyInput();
                CurrentType = InputType.Legacy;
            }

            if (m_inputModule == null)
            {
                ExplorerCore.LogWarning("Could not find any Input module!");
                m_inputModule = new NoInput();
                CurrentType = InputType.None;
            }
        }
    }
}