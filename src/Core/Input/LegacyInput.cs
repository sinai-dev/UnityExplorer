using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.UI;

namespace UnityExplorer.Core.Input
{
    public class LegacyInput : IHandleInput
    {
        public LegacyInput()
        {
            ExplorerCore.Log("Initializing Legacy Input support...");

            m_mousePositionProp = TInput.GetProperty("mousePosition");
            m_mouseDeltaProp = TInput.GetProperty("mouseScrollDelta");
            m_getKeyMethod = TInput.GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            m_getKeyDownMethod = TInput.GetMethod("GetKeyDown", new Type[] { typeof(KeyCode) });
            m_getMouseButtonMethod = TInput.GetMethod("GetMouseButton", new Type[] { typeof(int) });
            m_getMouseButtonDownMethod = TInput.GetMethod("GetMouseButtonDown", new Type[] { typeof(int) });
        }

        public static Type TInput => m_tInput ?? (m_tInput = ReflectionUtility.GetTypeByName("UnityEngine.Input"));
        private static Type m_tInput;

        private static PropertyInfo m_mousePositionProp;
        private static PropertyInfo m_mouseDeltaProp;
        private static MethodInfo m_getKeyMethod;
        private static MethodInfo m_getKeyDownMethod;
        private static MethodInfo m_getMouseButtonMethod;
        private static MethodInfo m_getMouseButtonDownMethod;

        public Vector2 MousePosition => (Vector3)m_mousePositionProp.GetValue(null, null);

        public Vector2 MouseScrollDelta => (Vector2)m_mouseDeltaProp.GetValue(null, null);

        public bool GetKey(KeyCode key) => (bool)m_getKeyMethod.Invoke(null, new object[] { key });

        public bool GetKeyDown(KeyCode key) => (bool)m_getKeyDownMethod.Invoke(null, new object[] { key });

        public bool GetMouseButton(int btn) => (bool)m_getMouseButtonMethod.Invoke(null, new object[] { btn });

        public bool GetMouseButtonDown(int btn) => (bool)m_getMouseButtonDownMethod.Invoke(null, new object[] { btn });

        // UI Input module

        public BaseInputModule UIModule => m_inputModule;
        internal StandaloneInputModule m_inputModule;

        public void AddUIInputModule()
        {
            m_inputModule = UIManager.CanvasRoot.gameObject.AddComponent<StandaloneInputModule>();
        }

        public void ActivateModule()
        {
            m_inputModule.ActivateModule();
        }
    }
}