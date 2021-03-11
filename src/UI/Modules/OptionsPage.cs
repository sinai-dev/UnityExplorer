using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Config;
using UnityExplorer.Helpers;
using UnityExplorer.UI.Shared;
using UnityExplorer.Unstrip;

namespace UnityExplorer.UI.Modules
{
    public class OptionsPage : MainMenu.Page
    {
        public override string Name => "Options";

        private InputField m_keycodeInput;
        private Toggle m_unlockMouseToggle;
        private InputField m_pageLimitInput;
        private InputField m_defaultOutputInput;
        private Toggle m_hideOnStartupToggle;

        public override void Init()
        {
            ConstructUI();
        }

        public override void Update()
        {
        }

        internal void OnApply()
        {
            if (!string.IsNullOrEmpty(m_keycodeInput.text) && Enum.Parse(typeof(KeyCode), m_keycodeInput.text) is KeyCode keyCode)
                ExplorerConfig.Instance.Main_Menu_Toggle = keyCode;

            ExplorerConfig.Instance.Force_Unlock_Mouse = m_unlockMouseToggle.isOn;

            if (!string.IsNullOrEmpty(m_pageLimitInput.text) && int.TryParse(m_pageLimitInput.text, out int lim))
                ExplorerConfig.Instance.Default_Page_Limit = lim;

            ExplorerConfig.Instance.Default_Output_Path = m_defaultOutputInput.text;

            ExplorerConfig.Instance.Hide_On_Startup = m_hideOnStartupToggle.isOn;

            ExplorerConfig.SaveSettings();
            ExplorerConfig.InvokeConfigChanged();
        }

        #region UI CONSTRUCTION

        internal void ConstructUI()
        {
            GameObject parent = MainMenu.Instance.PageViewport;

            Content = UIFactory.CreateVerticalGroup(parent, new Color(0.15f, 0.15f, 0.15f));
            var mainGroup = Content.GetComponent<VerticalLayoutGroup>();
            mainGroup.padding.left = 4;
            mainGroup.padding.right = 4;
            mainGroup.padding.top = 4;
            mainGroup.padding.bottom = 4;
            mainGroup.spacing = 5;
            mainGroup.childForceExpandHeight = false;
            mainGroup.childForceExpandWidth = true;
            mainGroup.childControlHeight = true;
            mainGroup.childControlWidth = true;

            // ~~~~~ Title ~~~~~

            GameObject titleObj = UIFactory.CreateLabel(Content, TextAnchor.UpperLeft);
            Text titleLabel = titleObj.GetComponent<Text>();
            titleLabel.text = "Options";
            titleLabel.fontSize = 20;
            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.minHeight = 30;
            titleLayout.flexibleHeight = 0;

            // ~~~~~ Actual options ~~~~~

            var optionsGroupObj = UIFactory.CreateVerticalGroup(Content, new Color(0.1f, 0.1f, 0.1f));
            var optionsGroup = optionsGroupObj.GetComponent<VerticalLayoutGroup>();
            optionsGroup.childForceExpandHeight = false;
            optionsGroup.childForceExpandWidth = true;
            optionsGroup.childControlWidth = true;
            optionsGroup.childControlHeight = true;
            optionsGroup.spacing = 5;
            optionsGroup.padding.top = 5;
            optionsGroup.padding.left = 5;
            optionsGroup.padding.right = 5;
            optionsGroup.padding.bottom = 5;

            ConstructKeycodeOpt(optionsGroupObj);
            ConstructMouseUnlockOpt(optionsGroupObj);
            ConstructPageLimitOpt(optionsGroupObj);
            ConstructOutputPathOpt(optionsGroupObj);
            ConstructHideOnStartupOpt(optionsGroupObj);

            var applyBtnObj = UIFactory.CreateButton(Content, new Color(0.2f, 0.2f, 0.2f));
            var applyText = applyBtnObj.GetComponentInChildren<Text>();
            applyText.text = "Apply and Save";
            var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
            applyLayout.minHeight = 30;
            applyLayout.flexibleWidth = 1000;
            var applyBtn = applyBtnObj.GetComponent<Button>();
            var applyColors = applyBtn.colors;
            applyColors.normalColor = new Color(0.3f, 0.7f, 0.3f);
            applyBtn.colors = applyColors;

            applyBtn.onClick.AddListener(OnApply);
        }

        private void ConstructHideOnStartupOpt(GameObject optionsGroupObj)
        {
            var rowObj = UIFactory.CreateHorizontalGroup(optionsGroupObj, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandHeight = true;
            var groupLayout = rowObj.AddComponent<LayoutElement>();
            groupLayout.minHeight = 25;
            groupLayout.flexibleHeight = 0;
            groupLayout.minWidth = 200;
            groupLayout.flexibleWidth = 1000;

            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "Hide UI on startup:";
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 150;
            labelLayout.minHeight = 25;

            UIFactory.CreateToggle(rowObj, out m_hideOnStartupToggle, out Text toggleText);
            m_hideOnStartupToggle.isOn = ExplorerConfig.Instance.Hide_On_Startup;
            toggleText.text = "";
        }

        internal void ConstructKeycodeOpt(GameObject parent)
        {
            var rowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandHeight = true;
            var groupLayout = rowObj.AddComponent<LayoutElement>();
            groupLayout.minHeight = 25;
            groupLayout.flexibleHeight = 0;
            groupLayout.minWidth = 200;
            groupLayout.flexibleWidth = 1000;

            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "Main Menu Toggle:";
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 150;
            labelLayout.minHeight = 25;

            var keycodeInputObj = UIFactory.CreateInputField(rowObj);
            
            m_keycodeInput = keycodeInputObj.GetComponent<InputField>();
            m_keycodeInput.text = ExplorerConfig.Instance.Main_Menu_Toggle.ToString();

            m_keycodeInput.placeholder.gameObject.GetComponent<Text>().text = "KeyCode, eg. F7";
        }

        internal void ConstructMouseUnlockOpt(GameObject parent)
        {
            var rowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandHeight = true;
            var groupLayout = rowObj.AddComponent<LayoutElement>();
            groupLayout.minHeight = 25;
            groupLayout.flexibleHeight = 0;
            groupLayout.minWidth = 200;
            groupLayout.flexibleWidth = 1000;

            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "Force Unlock Mouse:";
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 150;
            labelLayout.minHeight = 25;

            UIFactory.CreateToggle(rowObj, out m_unlockMouseToggle, out Text toggleText);
            m_unlockMouseToggle.isOn = ExplorerConfig.Instance.Force_Unlock_Mouse;
            toggleText.text = "";
        }

        internal void ConstructPageLimitOpt(GameObject parent)
        {
            //public int Default_Page_Limit = 20;

            var rowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandHeight = true;
            var groupLayout = rowObj.AddComponent<LayoutElement>();
            groupLayout.minHeight = 25;
            groupLayout.flexibleHeight = 0;
            groupLayout.minWidth = 200;
            groupLayout.flexibleWidth = 1000;

            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "Default Page Limit:";
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 150;
            labelLayout.minHeight = 25;

            var inputObj = UIFactory.CreateInputField(rowObj);

            m_pageLimitInput = inputObj.GetComponent<InputField>();
            m_pageLimitInput.text = ExplorerConfig.Instance.Default_Page_Limit.ToString();

            m_pageLimitInput.placeholder.gameObject.GetComponent<Text>().text = "Integer, eg. 20";
        }

        internal void ConstructOutputPathOpt(GameObject parent)
        {
            //public string Default_Output_Path = ExplorerCore.EXPLORER_FOLDER;

            var rowObj = UIFactory.CreateHorizontalGroup(parent, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childControlWidth = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandHeight = true;
            var groupLayout = rowObj.AddComponent<LayoutElement>();
            groupLayout.minHeight = 25;
            groupLayout.flexibleHeight = 0;
            groupLayout.minWidth = 200;
            groupLayout.flexibleWidth = 1000;

            var labelObj = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleLeft);
            var labelText = labelObj.GetComponent<Text>();
            labelText.text = "Default Output Path:";
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = 150;
            labelLayout.minHeight = 25;

            var inputObj = UIFactory.CreateInputField(rowObj);

            m_defaultOutputInput = inputObj.GetComponent<InputField>();
            m_defaultOutputInput.text = ExplorerConfig.Instance.Default_Output_Path.ToString();

            m_defaultOutputInput.placeholder.gameObject.GetComponent<Text>().text = @"Directory, eg. Mods\UnityExplorer";
        }

#endregion
    }
}
