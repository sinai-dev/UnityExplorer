using System;
using System.Reflection;
using UnityExplorer.Core.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.UI;
using System.Collections.Generic;

namespace UnityExplorer.Core.Input
{
    public class InputSystem : IHandleInput
    {
        public InputSystem()
        {
            ExplorerCore.Log("Initializing new InputSystem support...");

            m_kbCurrentProp = TKeyboard.GetProperty("current");
            m_kbIndexer = TKeyboard.GetProperty("Item", new Type[] { TKey });

            var btnControl = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            m_btnIsPressedProp = btnControl.GetProperty("isPressed");
            m_btnWasPressedProp = btnControl.GetProperty("wasPressedThisFrame");

            m_mouseCurrentProp = TMouse.GetProperty("current");
            m_leftButtonProp = TMouse.GetProperty("leftButton");
            m_rightButtonProp = TMouse.GetProperty("rightButton");

            m_positionProp = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Pointer")
                            .GetProperty("position");

            m_readVector2InputMethod = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputControl`1")
                                      .MakeGenericType(typeof(Vector2))
                                      .GetMethod("ReadValue");
        }

        public static Type TKeyboard => m_tKeyboard ?? (m_tKeyboard = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Keyboard"));
        private static Type m_tKeyboard;

        public static Type TMouse => m_tMouse ?? (m_tMouse = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Mouse"));
        private static Type m_tMouse;

        public static Type TKey => m_tKey ?? (m_tKey = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Key"));
        private static Type m_tKey;

        private static PropertyInfo m_btnIsPressedProp;
        private static PropertyInfo m_btnWasPressedProp;

        private static object CurrentKeyboard => m_currentKeyboard ?? (m_currentKeyboard = m_kbCurrentProp.GetValue(null, null));
        private static object m_currentKeyboard;
        private static PropertyInfo m_kbCurrentProp;
        private static PropertyInfo m_kbIndexer;

        private static object CurrentMouse => m_currentMouse ?? (m_currentMouse = m_mouseCurrentProp.GetValue(null, null));
        private static object m_currentMouse;
        private static PropertyInfo m_mouseCurrentProp;

        private static object LeftMouseButton => m_lmb ?? (m_lmb = m_leftButtonProp.GetValue(CurrentMouse, null));
        private static object m_lmb;
        private static PropertyInfo m_leftButtonProp;

        private static object RightMouseButton => m_rmb ?? (m_rmb = m_rightButtonProp.GetValue(CurrentMouse, null));
        private static object m_rmb;
        private static PropertyInfo m_rightButtonProp;

        private static object MousePositionInfo => m_pos ?? (m_pos = m_positionProp.GetValue(CurrentMouse, null));
        private static object m_pos;
        private static PropertyInfo m_positionProp;
        private static MethodInfo m_readVector2InputMethod;

        public Vector2 MousePosition
        {
            get
            {
                try
                {
                    return (Vector2)m_readVector2InputMethod.Invoke(MousePositionInfo, new object[0]);
                }
                catch
                {
                    return Vector2.zero;
                }
            }
        }

        internal static Dictionary<KeyCode, object> ActualKeyDict = new Dictionary<KeyCode, object>();

        internal object GetActualKey(KeyCode key)
        {
            if (!ActualKeyDict.ContainsKey(key))
            {
                var s = key.ToString();
                if (s.Contains("Control"))
                    s = s.Replace("Control", "Ctrl");
                else if (s.Contains("Return"))
                    s = "Enter";

                var parsed = Enum.Parse(TKey, s);
                var actualKey = m_kbIndexer.GetValue(CurrentKeyboard, new object[] { parsed });

                ActualKeyDict.Add(key, actualKey);
            }

            return ActualKeyDict[key];
        }

        public bool GetKeyDown(KeyCode key) => (bool)m_btnWasPressedProp.GetValue(GetActualKey(key), null);

        public bool GetKey(KeyCode key) => (bool)m_btnIsPressedProp.GetValue(GetActualKey(key), null);

        public bool GetMouseButtonDown(int btn)
        {
            switch (btn)
            {
                case 0: return (bool)m_btnWasPressedProp.GetValue(LeftMouseButton, null);
                case 1: return (bool)m_btnWasPressedProp.GetValue(RightMouseButton, null);
                // case 2: return (bool)_btnWasPressedProp.GetValue(MiddleMouseButton, null);
                default: throw new NotImplementedException();
            }
        }

        public bool GetMouseButton(int btn)
        {
            switch (btn)
            {
                case 0: return (bool)m_btnIsPressedProp.GetValue(LeftMouseButton, null);
                case 1: return (bool)m_btnIsPressedProp.GetValue(RightMouseButton, null);
                // case 2: return (bool)_btnIsPressedProp.GetValue(MiddleMouseButton, null);
                default: throw new NotImplementedException();
            }
        }

        // UI Input

        //public Type TInputSystemUIInputModule 
        //    => m_tUIInputModule 
        //    ?? (m_tUIInputModule = ReflectionHelpers.GetTypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule"));
        //internal Type m_tUIInputModule;

        public BaseInputModule UIModule => null; // m_newInputModule;
        //internal BaseInputModule m_newInputModule;

        public PointerEventData InputPointerEvent => null;

        public void AddUIInputModule()
        {
//            if (TInputSystemUIInputModule != null)
//            {
//#if CPP
//                // m_newInputModule = UIManager.CanvasRoot.AddComponent(Il2CppType.From(TInputSystemUIInputModule)).TryCast<BaseInputModule>();
//#else
//                m_newInputModule = (BaseInputModule)UIManager.CanvasRoot.AddComponent(TInputSystemUIInputModule);
//#endif
//            }
//            else
//            {
//                ExplorerCore.LogWarning("New input system: Could not find type by name 'UnityEngine.InputSystem.UI.InputSystemUIInputModule'");
//            }
        }

        public void ActivateModule()
        {
//#if CPP
//            // m_newInputModule.ActivateModule();
//#else
//            m_newInputModule.ActivateModule();
//#endif


        }
    }
}
