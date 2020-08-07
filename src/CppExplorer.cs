using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MelonLoader;
using UnhollowerBaseLib;

namespace Explorer
{
    public class CppExplorer : MelonMod
    {
        // consts

        public const string ID = "com.sinai.cppexplorer";
        public const string NAME = "IL2CPP Runtime Explorer";
        public const string VERSION = "1.0.0";
        public const string AUTHOR = "Sinai";

        // fields

        public static CppExplorer Instance;
        private string m_objUnderMouseName = "";
        private Camera m_main;

        // props

        public static bool ShowMenu { get; set; } = false;
        public static int ArrayLimit { get; set; } = 100;
        public bool MouseInspect { get; set; } = false;

        public static string ActiveSceneName
        {
            get
            {
                return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
        }

        public Camera MainCamera
        {
            get
            {
                if (m_main == null)
                {
                    m_main = Camera.main;
                }
                return m_main;
            }
        }

        // methods

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();

            Instance = this;

            new MainMenu();
            new WindowManager();

            //var harmony = HarmonyInstance.Create(ID);
            //harmony.PatchAll();

            // done init
            ShowMenu = true;
        }

        public override void OnLevelWasLoaded(int level)
        {
            if (ScenePage.Instance != null)
            {
                ScenePage.Instance.OnSceneChange();
                SearchPage.Instance.OnSceneChange();
            }
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F7))
            {
                ShowMenu = !ShowMenu;
            }

            if (ShowMenu)
            {
                MainMenu.Instance.Update();
                WindowManager.Instance.Update();

                if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1))
                {
                    MouseInspect = !MouseInspect;
                }

                if (MouseInspect)
                {
                    InspectUnderMouse();
                }
            }
            else if (MouseInspect)
            {
                MouseInspect = false;
            }
        }

        private void InspectUnderMouse()
        {
            Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                var obj = hit.transform.gameObject;

                m_objUnderMouseName = GetGameObjectPath(obj.transform);

                if (Input.GetMouseButtonDown(0))
                {
                    MouseInspect = false;
                    m_objUnderMouseName = "";

                    WindowManager.InspectObject(obj, out _);
                }
            }
            else
            {
                m_objUnderMouseName = "";
            }
        }

        public override void OnGUI()
        {
            base.OnGUI();

            MainMenu.Instance.OnGUI();
            WindowManager.Instance.OnGUI();

            if (MouseInspect)
            {
                if (m_objUnderMouseName != "")
                {
                    var pos = Input.mousePosition;
                    var rect = new Rect(
                        pos.x - (Screen.width / 2), // x
                        Screen.height - pos.y - 50, // y
                        Screen.width,               // w
                        50                          // h
                    );

                    var origAlign = GUI.skin.label.alignment;
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;

                    //shadow text
                    GUI.Label(rect, $"<color=black>{m_objUnderMouseName}</color>");
                    //white text
                    GUI.Label(new Rect(rect.x - 1, rect.y + 1, rect.width, rect.height), m_objUnderMouseName);

                    GUI.skin.label.alignment = origAlign;
                }
            }
        }

        // ************** public helpers **************

        public static object Il2CppCast(object obj, Type castTo)
        {
            var method = typeof(Il2CppObjectBase).GetMethod("TryCast");
            var generic = method.MakeGenericMethod(castTo);
            return generic.Invoke(obj, null);
        }

        public static string GetGameObjectPath(Transform _transform)
        {
            return GetGameObjectPath(_transform, true);
        }

        public static string GetGameObjectPath(Transform _transform, bool _includeItemName)
        {
            string text = _includeItemName ? ("/" + _transform.name) : "";
            GameObject gameObject = _transform.gameObject;
            while (gameObject.transform.parent != null)
            {
                gameObject = gameObject.transform.parent.gameObject;
                text = "/" + gameObject.name + text;
            }
            return text;
        }

        public static Type GetType(string _type)
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        if (asm.GetType(_type) is Type type)
                        {
                            return type;
                        }
                    }
                    catch { }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
