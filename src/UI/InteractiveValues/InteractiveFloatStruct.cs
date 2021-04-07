using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace UnityExplorer.UI.InteractiveValues
{
    // Class for supporting any "float struct" (ie Vector, Rect, etc).
    // Supports any struct where all the public instance fields are floats (or types assignable to float)

    public class StructInfo
    {
        public string[] FieldNames { get; }
        private readonly FieldInfo[] m_fields;

        public StructInfo(Type type)
        {
            m_fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                           .Where(it => !it.IsLiteral)
                           .ToArray();

            FieldNames = m_fields.Select(it => it.Name)
                                 .ToArray();
        }

        public object SetValue(ref object instance, int fieldIndex, float val)
        {
            m_fields[fieldIndex].SetValue(instance, val);
            return instance;
        }

        public float GetValue(object instance, int fieldIndex)
            => (float)m_fields[fieldIndex].GetValue(instance);

        public void RefreshUI(InputField[] inputs, object instance)
        {
            try
            {
                for (int i = 0; i < m_fields.Length; i++)
                {
                    var field = m_fields[i];
                    float val = (float)field.GetValue(instance);
                    inputs[i].text = val.ToString();
                }
            }
            catch (Exception ex)
            {
                ExplorerCore.Log(ex);
            }
        }
    }

    public class InteractiveFloatStruct : InteractiveValue
    {
        private static readonly Dictionary<string, bool> _typeSupportCache = new Dictionary<string, bool>();
        public static bool IsTypeSupported(Type type)
        {
            if (!type.IsValueType)
                return false;

            if (string.IsNullOrEmpty(type.AssemblyQualifiedName))
                return false;

            if (_typeSupportCache.TryGetValue(type.AssemblyQualifiedName, out bool ret))
                return ret;

            ret = true;
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.IsLiteral)
                    continue;

                if (!typeof(float).IsAssignableFrom(field.FieldType))
                {
                    ret = false;
                    break;
                }
            }
            _typeSupportCache.Add(type.AssemblyQualifiedName, ret);
            return ret;
        }

        //~~~~~~~~~ Instance ~~~~~~~~~~

        public InteractiveFloatStruct(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => true;
        public override bool SubContentWanted => true;

        public StructInfo StructInfo;

        public override void RefreshUIForValue()
        {
            InitializeStructInfo();

            base.RefreshUIForValue();

            if (m_subContentConstructed)
                StructInfo.RefreshUI(m_inputs, this.Value);
        }

        internal override void OnToggleSubcontent(bool toggle)
        {
            InitializeStructInfo();

            base.OnToggleSubcontent(toggle);

            StructInfo.RefreshUI(m_inputs, this.Value);
        }

        internal Type m_lastStructType;

        internal void InitializeStructInfo()
        {
            var type = Value?.GetType() ?? FallbackType;

            if (StructInfo != null && type == m_lastStructType)
                return;

            if (StructInfo != null && m_subContentConstructed)
                DestroySubContent();

            m_lastStructType = type;

            StructInfo = new StructInfo(type);

            if (m_subContentParent.activeSelf)
                ConstructSubcontent();
        }

        #region UI CONSTRUCTION

        internal InputField[] m_inputs;

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);
        }

        public override void ConstructSubcontent()
        {
            base.ConstructSubcontent();

            if (StructInfo == null)
            {
                ExplorerCore.LogWarning("Setting up subcontent but structinfo is null");
                return;
            }

            var editorContainer = UIFactory.CreateVerticalGroup(m_subContentParent, "EditorContent", false, true, true, true, 2, new Vector4(4, 4, 4, 4),
                new Color(0.08f, 0.08f, 0.08f));

            m_inputs = new InputField[StructInfo.FieldNames.Length];

            for (int i = 0; i < StructInfo.FieldNames.Length; i++)
                AddEditorRow(i, editorContainer);

            RefreshUIForValue();
        }

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            try
            {
                var row = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow", false, true, true, true, 5, default, new Color(1, 1, 1, 0));

                string name = StructInfo.FieldNames[index];

                var label = UIFactory.CreateLabel(row, "RowLabel", $"{name}:", TextAnchor.MiddleRight, Color.cyan);
                UIFactory.SetLayoutElement(label.gameObject, minWidth: 30, flexibleWidth: 0, minHeight: 25);

                var inputFieldObj = UIFactory.CreateInputField(row, "InputField", "...", 14, 3, 1);
                UIFactory.SetLayoutElement(inputFieldObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

                var inputField = inputFieldObj.GetComponent<InputField>();
                m_inputs[index] = inputField;

                inputField.onValueChanged.AddListener((string val) =>
                {
                    try
                    {
                        float f = float.Parse(val);
                        Value = StructInfo.SetValue(ref this.Value, index, f);
                        Owner.SetValue();
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                ExplorerCore.Log(ex);
            }
        }

        #endregion
    }
}
