using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.Utility;

namespace UnityExplorer.UI.Widgets
{
    internal class TimeScaleWidget
    {
        public TimeScaleWidget(GameObject parent)
        {
            Instance = this;

            ConstructUI(parent);

            InitPatch();
        }

        static TimeScaleWidget Instance;

        ButtonRef lockBtn;
        bool locked;
        InputFieldRef timeInput;
        float desiredTime;
        bool settingTimeScale;

        public void Update()
        {
            // Fallback in case Time.timeScale patch failed for whatever reason
            if (locked)
                SetTimeScale(desiredTime);

            if (!timeInput.Component.isFocused)
                timeInput.Text = Time.timeScale.ToString("F2");
        }

        void SetTimeScale(float time)
        {
            settingTimeScale = true;
            Time.timeScale = time;
            settingTimeScale = false;
        }

        // UI event listeners

        void OnTimeInputEndEdit(string val)
        {
            if (float.TryParse(val, out float f))
            {
                SetTimeScale(f);
                desiredTime = f;
            }
        }

        void OnPauseButtonClicked()
        {
            OnTimeInputEndEdit(timeInput.Text);

            locked = !locked;

            Color color = locked ? new Color(0.3f, 0.3f, 0.2f) : new Color(0.2f, 0.2f, 0.2f);
            RuntimeHelper.SetColorBlock(lockBtn.Component, color, color * 1.2f, color * 0.7f);
            lockBtn.ButtonText.text = locked ? "Unlock" : "Lock";
        }

        // UI Construction

        void ConstructUI(GameObject parent)
        {
            Text timeLabel = UIFactory.CreateLabel(parent, "TimeLabel", "Time:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(timeLabel.gameObject, minHeight: 25, minWidth: 35);

            timeInput = UIFactory.CreateInputField(parent, "TimeInput", "timeScale");
            UIFactory.SetLayoutElement(timeInput.Component.gameObject, minHeight: 25, minWidth: 40);
            timeInput.Component.GetOnEndEdit().AddListener(OnTimeInputEndEdit);

            timeInput.Text = string.Empty;
            timeInput.Text = Time.timeScale.ToString();

            lockBtn = UIFactory.CreateButton(parent, "PauseButton", "Lock", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(lockBtn.Component.gameObject, minHeight: 25, minWidth: 50);
            lockBtn.OnClick += OnPauseButtonClicked;
        }

        // Only allow Time.timeScale to be set if the user hasn't "locked" it or if we are setting the value internally.

        static void InitPatch()
        {

            try
            {
                MethodInfo target = typeof(Time).GetProperty("timeScale").GetSetMethod();
#if CPP
                if (UnhollowerBaseLib.UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(target) == null)
                    return;
#endif
                ExplorerCore.Harmony.Patch(target,
                    prefix: new(AccessTools.Method(typeof(TimeScaleWidget), nameof(Prefix_Time_set_timeScale))));
            }
            catch { }
        }

        static bool Prefix_Time_set_timeScale()
        {
            return !Instance.locked || Instance.settingTimeScale;
        }
    }
}
