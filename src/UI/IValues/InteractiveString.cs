using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.CacheObject;
using UnityExplorer.UI.Widgets;

namespace UnityExplorer.UI.IValues
{
    public class InteractiveString : InteractiveValue
    {
        private string RealValue;
        public string EditedValue = "";

        public InputFieldRef inputField;
        public ButtonRef ApplyButton;
        
        public GameObject SaveFileRow;
        public InputFieldRef SaveFilePath;

        public override void OnBorrowed(CacheObjectBase owner)
        {
            base.OnBorrowed(owner);

            inputField.InputField.readOnly = !owner.CanWrite;
            ApplyButton.Button.gameObject.SetActive(owner.CanWrite);

            SaveFilePath.Text = Path.Combine(ConfigManager.Default_Output_Path.Value, "untitled.txt");
        }

        private bool IsStringTooLong(string s)
        {
            if (s == null)
                return false;

            return s.Length >= UIManager.MAX_INPUTFIELD_CHARS;
        }

        public override void SetValue(object value)
        {
            RealValue = value as string;
            SaveFileRow.SetActive(IsStringTooLong(RealValue));

            if (value == null)
            {
                inputField.Text = "";
                EditedValue = "";
            }
            else
            {
                EditedValue = (string)value;
                inputField.Text = EditedValue;
            }
        }

        private void OnApplyClicked()
        {
            CurrentOwner.SetUserValue(EditedValue);
        }

        private void OnInputChanged(string input)
        {
            EditedValue = input;
            
            if (IsStringTooLong(EditedValue))
            {
                ExplorerCore.LogWarning("InputField length has reached maximum character count!");
            }
        }

        private void OnSaveFileClicked()
        {
            if (RealValue == null)
                return;

            if (string.IsNullOrEmpty(SaveFilePath.Text))
            {
                ExplorerCore.LogWarning("Cannot save an empty file path!");
                return;
            }

            var path = IOUtility.EnsureValid(SaveFilePath.Text);

            if (File.Exists(path))
                File.Delete(path);
            
            File.WriteAllText(path, RealValue);
        }

        public override GameObject CreateContent(GameObject parent)
        {
            UIRoot = UIFactory.CreateVerticalGroup(parent, "InteractiveString", false, false, true, true, 3, new Vector4(4, 4, 4, 4),
                new Color(0.06f, 0.06f, 0.06f));

            // Save to file helper

            SaveFileRow = UIFactory.CreateUIObject("SaveFileRow", UIRoot);
            UIFactory.SetLayoutElement(SaveFileRow, flexibleWidth: 9999);
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(SaveFileRow, false, true, true, true, 3);

            UIFactory.CreateLabel(SaveFileRow, "Info", "<color=red>String is too long! Save to file if you want to see the full string.</color>", 
                TextAnchor.MiddleLeft);

            var horizRow = UIFactory.CreateUIObject("Horiz", SaveFileRow);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(horizRow, false, false, true, true, 4);

            var saveButton = UIFactory.CreateButton(horizRow, "SaveButton", "Save file");
            UIFactory.SetLayoutElement(saveButton.Button.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            saveButton.OnClick += OnSaveFileClicked;

            SaveFilePath = UIFactory.CreateInputField(horizRow, "SaveInput", "...");
            UIFactory.SetLayoutElement(SaveFilePath.UIRoot, minHeight: 25, flexibleWidth: 9999);

            // Main Input / apply

            ApplyButton = UIFactory.CreateButton(UIRoot, "ApplyButton", "Apply", new Color(0.2f, 0.27f, 0.2f));
            UIFactory.SetLayoutElement(ApplyButton.Button.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 0);
            ApplyButton.OnClick += OnApplyClicked;

            inputField = UIFactory.CreateInputField(UIRoot, "InputField", "empty");
            inputField.UIRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(inputField.UIRoot, minHeight: 25, flexibleHeight: 500, flexibleWidth: 9999);
            inputField.InputField.lineType = InputField.LineType.MultiLineNewline;
            inputField.OnValueChanged += OnInputChanged;

            return UIRoot;
        }

    }
}
