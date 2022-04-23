#if STANDALONE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.Config;
using UnityExplorer.UI;
using UniverseLib;

namespace UnityExplorer.Loader.Standalone
{
    public class ExplorerEditorBehaviour : MonoBehaviour
    {
        internal static ExplorerEditorBehaviour Instance { get; private set; }

        public bool Hide_On_Startup = true;
        public KeyCode Master_Toggle_Key = KeyCode.F7;
        public UIManager.VerticalAnchor Main_Navbar_Anchor = UIManager.VerticalAnchor.Top;
        public bool Log_Unity_Debug = false;
        public float Startup_Delay_Time = 1f;
        public KeyCode World_MouseInspect_Keybind;
        public KeyCode UI_MouseInspect_Keybind;
        public bool Force_Unlock_Mouse = true;
        public KeyCode Force_Unlock_Toggle;
        public bool Disable_EventSystem_Override;

        internal void Awake()
        {
            Instance = this;

            ExplorerEditorLoader.Initialize();
            DontDestroyOnLoad(this);
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        internal void OnApplicationQuit()
        {
            Destroy(this.gameObject);
        }
    
        internal void LoadConfigs()
        {
            ConfigManager.Hide_On_Startup.Value = this.Hide_On_Startup;
            ConfigManager.Master_Toggle.Value = this.Master_Toggle_Key;
            ConfigManager.Main_Navbar_Anchor.Value = this.Main_Navbar_Anchor;
            ConfigManager.Log_Unity_Debug.Value = this.Log_Unity_Debug;
            ConfigManager.Startup_Delay_Time.Value = this.Startup_Delay_Time;
            ConfigManager.World_MouseInspect_Keybind.Value = this.World_MouseInspect_Keybind;
            ConfigManager.UI_MouseInspect_Keybind.Value = this.UI_MouseInspect_Keybind;
            ConfigManager.Force_Unlock_Mouse.Value = this.Force_Unlock_Mouse;
            ConfigManager.Force_Unlock_Toggle.Value = this.Force_Unlock_Toggle;
            ConfigManager.Disable_EventSystem_Override.Value = this.Disable_EventSystem_Override;
        }
    }
}
#endif