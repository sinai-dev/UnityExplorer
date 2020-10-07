using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Explorer.UI.Shared;
using Explorer.Config;
using Explorer.CacheObject;

namespace Explorer.UI.Main
{
    public class OptionsPage : BaseMainMenuPage
    {
        public override string Name => "Options";

        public string toggleKeyInputString = "";
        public Vector2 defaultSizeInputVector;
        public int defaultPageLimit;
        public bool bitwiseSupport;
        public bool tabView;

        private CacheObjectBase toggleKeyInput;
        private CacheObjectBase defaultSizeInput;
        private CacheObjectBase defaultPageLimitInput;
        private CacheObjectBase bitwiseSupportInput;
        private CacheObjectBase tabViewInput;

        public override void Init()
        {
            toggleKeyInputString = ModConfig.Instance.Main_Menu_Toggle.ToString();
            toggleKeyInput = CacheFactory.GetCacheObject(typeof(OptionsPage).GetField("toggleKeyInputString"), this);

            defaultSizeInputVector = ModConfig.Instance.Default_Window_Size;
            defaultSizeInput = CacheFactory.GetCacheObject(typeof(OptionsPage).GetField("defaultSizeInputVector"), this);

            defaultPageLimit = ModConfig.Instance.Default_Page_Limit;
            defaultPageLimitInput = CacheFactory.GetCacheObject(typeof(OptionsPage).GetField("defaultPageLimit"), this);

            bitwiseSupport = ModConfig.Instance.Bitwise_Support;
            bitwiseSupportInput = CacheFactory.GetCacheObject(typeof(OptionsPage).GetField("bitwiseSupport"), this);

            tabView = ModConfig.Instance.Tab_View;
            tabViewInput = CacheFactory.GetCacheObject(typeof(OptionsPage).GetField("tabView"), this);
        }

        public override void Update() { }

        public override void DrawWindow()
        {
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("<color=orange><size=16><b>Options</b></size></color>", new GUILayoutOption[0]);
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, new GUILayoutOption[0]);

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Menu Toggle Key:", new GUILayoutOption[] { GUILayout.Width(215f) });
            toggleKeyInput.IValue.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Default Window Size:", new GUILayoutOption[] { GUILayout.Width(215f) });
            defaultSizeInput.IValue.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Default Items per Page:", new GUILayoutOption[] { GUILayout.Width(215f) });
            defaultPageLimitInput.IValue.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Enable Bitwise Editing:", new GUILayoutOption[] { GUILayout.Width(215f) });
            bitwiseSupportInput.IValue.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            GUIUnstrip.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Enable Tab View:", new GUILayoutOption[] { GUILayout.Width(215f) });
            tabViewInput.IValue.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("<color=lime><b>Apply and Save</b></color>", new GUILayoutOption[0]))
            {
                ApplyAndSave();
            }

            GUILayout.EndVertical();

            GUIUnstrip.Space(10f);

            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("<color=orange><size=16><b>Other</b></size></color>", new GUILayoutOption[0]);
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            GUIUnstrip.BeginVertical(GUIContent.none, GUI.skin.box, new GUILayoutOption[0]);

            if (GUILayout.Button("Inspect Test Class", new GUILayoutOption[0]))
            {
                WindowManager.InspectObject(Tests.TestClass.Instance, out bool _);
            }

            GUILayout.EndVertical();
        }

        private void ApplyAndSave()
        {
            if (Enum.Parse(typeof(KeyCode), toggleKeyInputString) is KeyCode key)
            {
                ModConfig.Instance.Main_Menu_Toggle = key;
            }
            else
            {
                ExplorerCore.LogWarning($"Could not parse '{toggleKeyInputString}' to KeyCode!");
            }

            ModConfig.Instance.Default_Window_Size = defaultSizeInputVector;
            ModConfig.Instance.Default_Page_Limit = defaultPageLimit;
            ModConfig.Instance.Bitwise_Support = bitwiseSupport;
            
            ModConfig.Instance.Tab_View = tabView;
            WindowManager.TabView = tabView;

            ModConfig.SaveSettings();
        }
    }
}
