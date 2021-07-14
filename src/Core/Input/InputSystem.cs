using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityExplorer.UI;

namespace UnityExplorer.Core.Input
{
    public class InputSystem : IHandleInput
    {
        public InputSystem()
        {
            SetupSupportedDevices();

            m_kbCurrentProp = TKeyboard.GetProperty("current");
            m_kbIndexer = TKeyboard.GetProperty("Item", new Type[] { TKey });

            var btnControl = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Controls.ButtonControl");
            m_btnIsPressedProp = btnControl.GetProperty("isPressed");
            m_btnWasPressedProp = btnControl.GetProperty("wasPressedThisFrame");

            m_mouseCurrentProp = TMouse.GetProperty("current");
            m_leftButtonProp = TMouse.GetProperty("leftButton");
            m_rightButtonProp = TMouse.GetProperty("rightButton");
            m_scrollDeltaProp = TMouse.GetProperty("scroll");

            m_positionProp = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.Pointer")
                            .GetProperty("position");

            ReadV2ControlMethod = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputControl`1")
                                      .MakeGenericType(typeof(Vector2))
                                      .GetMethod("ReadValue");
        }

        internal static void SetupSupportedDevices()
        {
            try
            {
                // typeof(InputSystem)
                Type TInputSystem = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputSystem");
                // InputSystem.settings
                var settings = TInputSystem.GetProperty("settings", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
                // typeof(InputSettings)
                Type TSettings = settings.GetActualType();
                // InputSettings.supportedDevices
                PropertyInfo supportedProp = TSettings.GetProperty("supportedDevices", BindingFlags.Public | BindingFlags.Instance);
                var supportedDevices = supportedProp.GetValue(settings, null);
                // An empty supportedDevices list means all devices are supported.
#if CPP
                // weird hack for il2cpp, use the implicit operator and cast Il2CppStringArray to ReadOnlyArray<string>
                var args = new object[] { new UnhollowerBaseLib.Il2CppStringArray(0) };
                var method = supportedDevices.GetActualType().GetMethod("op_Implicit", BindingFlags.Static | BindingFlags.Public);
                supportedProp.SetValue(settings, method.Invoke(null, args), null);
#else
                supportedProp.SetValue(settings, Activator.CreateInstance(supportedDevices.GetActualType(), new object[] { new string[0] }), null);
#endif
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Exception setting up InputSystem.settings.supportedDevices list!");
                ExplorerCore.Log(ex);
            }
        }

#region reflection cache

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

        private static MethodInfo ReadV2ControlMethod;

        private static object MousePositionInfo => m_pos ?? (m_pos = m_positionProp.GetValue(CurrentMouse, null));
        private static object m_pos;
        private static PropertyInfo m_positionProp;

        private static object MouseScrollInfo => m_scrollInfo ?? (m_scrollInfo = m_scrollDeltaProp.GetValue(CurrentMouse, null));
        private static object m_scrollInfo;
        private static PropertyInfo m_scrollDeltaProp;

#endregion

        public Vector2 MousePosition
        {
            get
            {
                try
                {
                    return (Vector2)ReadV2ControlMethod.Invoke(MousePositionInfo, ArgumentUtility.EmptyArgs);
                }
                catch { return Vector2.zero; }
            }
        }

        public Vector2 MouseScrollDelta
        {
            get
            {
                try
                {
                    return (Vector2)ReadV2ControlMethod.Invoke(MouseScrollInfo, ArgumentUtility.EmptyArgs);
                }
                catch { return Vector2.zero; }
            }
        }

        internal static Dictionary<KeyCode, object> ActualKeyDict = new Dictionary<KeyCode, object>();
        internal static Dictionary<string, string> enumNameFixes = new Dictionary<string, string>
        {
            { "Control", "Ctrl" },
            { "Return", "Enter" },
            { "Alpha", "Digit" },
            { "Keypad", "Numpad" },
            { "Numlock", "NumLock" },
            { "Print", "PrintScreen" },
            { "BackQuote", "Backquote" }
        };

        internal object GetActualKey(KeyCode key)
        {
            if (!ActualKeyDict.ContainsKey(key))
            {
                var s = key.ToString();
                try
                {
                    if (enumNameFixes.First(it => s.Contains(it.Key)) is KeyValuePair<string, string> entry)
                        s = s.Replace(entry.Key, entry.Value);
                }
                catch { }

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
            if (CurrentMouse == null)
                return false;
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
            if (CurrentMouse == null)
                return false;
            switch (btn)
            {
                case 0: return (bool)m_btnIsPressedProp.GetValue(LeftMouseButton, null);
                case 1: return (bool)m_btnIsPressedProp.GetValue(RightMouseButton, null);
                // case 2: return (bool)_btnIsPressedProp.GetValue(MiddleMouseButton, null);
                default: throw new NotImplementedException();
            }
        }

        // UI Input

        public Type TInputSystemUIInputModule
            => m_tUIInputModule
            ?? (m_tUIInputModule = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.UI.InputSystemUIInputModule"));
        internal Type m_tUIInputModule;

        public BaseInputModule UIModule => m_newInputModule;
        internal BaseInputModule m_newInputModule;

        public void AddUIInputModule()
        {
            if (TInputSystemUIInputModule == null)
            {
                ExplorerCore.LogWarning("Unable to find UI Input Module Type, Input will not work!");
                return;
            }

            var assetType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionAsset");
            m_newInputModule = RuntimeProvider.Instance.AddComponent<BaseInputModule>(UIManager.CanvasRoot, TInputSystemUIInputModule);
            var asset = RuntimeProvider.Instance.CreateScriptable(assetType)
                .TryCast(assetType);

            inputExtensions = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionSetupExtensions");

            var addMap = inputExtensions.GetMethod("AddActionMap", new Type[] { assetType, typeof(string) });
            var map = addMap.Invoke(null, new object[] { asset, "UI" })
                .TryCast(ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionMap"));

            CreateAction(map, "point", new[] { "<Mouse>/position" }, "point");
            CreateAction(map, "click", new[] { "<Mouse>/leftButton" }, "leftClick");
            CreateAction(map, "rightClick", new[] { "<Mouse>/rightButton" }, "rightClick");
            CreateAction(map, "scrollWheel", new[] { "<Mouse>/scroll" }, "scrollWheel");

            UI_Enable = map.GetType().GetMethod("Enable");
            UI_Enable.Invoke(map, ArgumentUtility.EmptyArgs);
            UI_ActionMap = map;
        }

        private Type inputExtensions;
        private object UI_ActionMap;
        private MethodInfo UI_Enable;

        private void CreateAction(object map, string actionName, string[] bindings, string propertyName)
        {
            var inputActionType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputAction");
            var addAction = inputExtensions.GetMethod("AddAction");
            var action = addAction.Invoke(null, new object[] { map, actionName, default, null, null, null, null, null })
                .TryCast(inputActionType);

            var addBinding = inputExtensions.GetMethod("AddBinding",
                new Type[] { inputActionType, typeof(string), typeof(string), typeof(string), typeof(string) });

            foreach (string binding in bindings)
                addBinding.Invoke(null, new object[] { action.TryCast(inputActionType), binding, null, null, null });

            var refType = ReflectionUtility.GetTypeByName("UnityEngine.InputSystem.InputActionReference");
            var inputRef = refType.GetMethod("Create")
                            .Invoke(null, new object[] { action })
                            .TryCast(refType);

            TInputSystemUIInputModule
                .GetProperty(propertyName)
                .SetValue(m_newInputModule.TryCast(TInputSystemUIInputModule), inputRef, null);
        }

        public void ActivateModule()
        {
            m_newInputModule.ActivateModule();
            UI_Enable.Invoke(UI_ActionMap, ArgumentUtility.EmptyArgs);
        }
    }
}