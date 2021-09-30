using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.UI;

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

        private static IHandleInput m_inputHandler;

        public static Vector3 MousePosition => m_inputHandler.MousePosition;

        public static bool GetKeyDown(KeyCode key)
        {
            if (key == KeyCode.None)
                return false;
            return m_inputHandler.GetKeyDown(key);
        }

        public static bool GetKey(KeyCode key)
        {
            if (key == KeyCode.None)
                return false;
            return m_inputHandler.GetKey(key);
        }

        public static bool GetMouseButtonDown(int btn) => m_inputHandler.GetMouseButtonDown(btn);
        public static bool GetMouseButton(int btn) => m_inputHandler.GetMouseButton(btn);

        public static BaseInputModule UIInput => m_inputHandler.UIInputModule;

        public static Vector2 MouseScrollDelta => m_inputHandler.MouseScrollDelta;

        public static void AddUIModule()
        {
            m_inputHandler.AddUIInputModule();
            //ActivateUIModule();
            CursorUnlocker.SetEventSystem();
        }

        public static void ActivateUIModule()
        {
            UIManager.EventSys.m_CurrentInputModule = UIInput;
            m_inputHandler.ActivateModule();
        }

        public static void Init()
        {
            InitHandler();

            CursorUnlocker.Init();
        }

        private static void InitHandler()
        {
            // First, just try to use the legacy input, see if its working.
            // The InputSystem package may be present but not actually activated, so we can find out this way.

            if (LegacyInput.TInput != null)
            {
                try
                {
                    m_inputHandler = new LegacyInput();
                    CurrentType = InputType.Legacy;

                    // make sure its working
                    GetKeyDown(KeyCode.F5);

                    ExplorerCore.Log("Initialized Legacy Input support");
                    return;
                }
                catch
                {
                    // It's not working, we'll fall back to InputSystem.
                }
            }

            if (InputSystem.TKeyboard != null)
            {
                try
                {
                    m_inputHandler = new InputSystem();
                    CurrentType = InputType.InputSystem;
                    ExplorerCore.Log("Initialized new InputSystem support.");
                    return;
                }
                catch (Exception ex)
                {
                    ExplorerCore.Log(ex);
                }
            }

            ExplorerCore.LogWarning("Could not find any Input Module Type!");
            m_inputHandler = new NoInput();
            CurrentType = InputType.None;
        }
    }
}