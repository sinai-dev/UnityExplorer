using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Explorer.Tests;
using UnityEngine;

namespace Explorer
{
    public class OptionsPage : WindowPage
    {
        public override string Name => "Options";

        public string toggleKeyInputString = "";
        public Vector2 defaultSizeInputVector;
        public int defaultPageLimit;

        private CacheObjectBase toggleKeyInput;
        private CacheObjectBase defaultSizeInput;
        private CacheObjectBase defaultPageLimitInput;        

        public override void Init()
        {
            toggleKeyInputString = ModConfig.Instance.Main_Menu_Toggle.ToString();
            toggleKeyInput = CacheFactory.GetTypeAndCacheObject(typeof(OptionsPage).GetField("toggleKeyInputString"), this);

            defaultSizeInputVector = ModConfig.Instance.Default_Window_Size;
            defaultSizeInput = CacheFactory.GetTypeAndCacheObject(typeof(OptionsPage).GetField("defaultSizeInputVector"), this);

            defaultPageLimit = ModConfig.Instance.Default_Page_Limit;
            defaultPageLimitInput = CacheFactory.GetTypeAndCacheObject(typeof(OptionsPage).GetField("defaultPageLimit"), this);
        }

        public override void Update() { }

        public override void DrawWindow()
        {
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("<color=orange><size=16><b>Settings</b></size></color>", new GUILayoutOption[0]);
            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            GUILayout.BeginVertical(GUIContent.none, GUI.skin.box, new GUILayoutOption[0]);

            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Menu Toggle Key:", new GUILayoutOption[] { GUILayout.Width(215f) });
            toggleKeyInput.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Default Window Size:", new GUILayoutOption[] { GUILayout.Width(215f) });
            defaultSizeInput.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label($"Default Items per Page:", new GUILayoutOption[] { GUILayout.Width(215f) });
            defaultPageLimitInput.DrawValue(MainMenu.MainRect, MainMenu.MainRect.width - 215f);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("<color=lime><b>Apply and Save</b></color>", new GUILayoutOption[0]))
            {
                ApplyAndSave();
            }

            GUILayout.EndVertical();

            //GUIUnstrip.Space(10f);

            //GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            //GUILayout.Label("<color=orange><size=16><b>Other</b></size></color>", new GUILayoutOption[0]);
            //GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            //GUILayout.BeginVertical(GUIContent.none, GUI.skin.box, new GUILayoutOption[0]);

            //if (GUILayout.Button("Inspect Test Class", new GUILayoutOption[0]))
            //{
            //    WindowManager.InspectObject(TestClass.Instance, out bool _);
            //}

            //GUILayout.EndVertical();
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

            ModConfig.SaveSettings();
        }
    }
}
