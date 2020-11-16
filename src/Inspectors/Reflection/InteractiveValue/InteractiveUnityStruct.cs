using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Helpers;
using UnityExplorer.UI;

namespace UnityExplorer.Inspectors.Reflection
{
    #region IStructInfo helper

    public interface IStructInfo
    {
        string[] FieldNames { get; }
        object SetValue(ref object value, int fieldIndex, float val);
        void RefreshUI(InputField[] inputs, object value);
    }

    public class StructInfo<T> : IStructInfo where T : struct
    {
        public string[] FieldNames { get; set; }

        public delegate void SetMethod(ref T value, int fieldIndex, float val);
        public SetMethod SetValueMethod;

        public delegate void UpdateMethod(InputField[] inputs, object value);
        public UpdateMethod UpdateUIMethod;

        public object SetValue(ref object value, int fieldIndex, float val)
        {
            var box = (T)value;
            SetValueMethod.Invoke(ref box, fieldIndex, val);
            return box;
        }

        public void RefreshUI(InputField[] inputs, object value)
        {
            UpdateUIMethod.Invoke(inputs, value);
        }
    }

    // This part is a bit ugly, but everything else is generalized above.
    // I could generalize it more with reflection, but it would be different for
    // mono/il2cpp and also slower.
    public static class StructInfoFactory
    {
        public static IStructInfo Create(Type type)
        {
            if (type == typeof(Vector2))
            {
                return new StructInfo<Vector2>()
                {
                    FieldNames = new[] { "x", "y", },
                    SetValueMethod = (ref Vector2 vec, int fieldIndex, float val) =>
                    {
                        switch (fieldIndex)
                        {
                            case 0: vec.x = val; break;
                            case 1: vec.y = val; break;
                        }
                    },
                    UpdateUIMethod = (InputField[] inputs, object value) =>
                    {
                        Vector2 vec = (Vector2)value;
                        inputs[0].text = vec.x.ToString();
                        inputs[1].text = vec.y.ToString();
                    }
                };
            }
            else if (type == typeof(Vector3))
            {
                return new StructInfo<Vector3>()
                {
                    FieldNames = new[] { "x", "y", "z" },
                    SetValueMethod = (ref Vector3 vec, int fieldIndex, float val) =>
                    {
                        switch (fieldIndex)
                        {
                            case 0: vec.x = val; break;
                            case 1: vec.y = val; break;
                            case 2: vec.z = val; break;
                        }
                    },
                    UpdateUIMethod = (InputField[] inputs, object value) =>
                    {
                        Vector3 vec = (Vector3)value;
                        inputs[0].text = vec.x.ToString();
                        inputs[1].text = vec.y.ToString();
                        inputs[2].text = vec.z.ToString();
                    }
                };
            }
            else if (type == typeof(Vector4))
            {
                return new StructInfo<Vector4>()
                {
                    FieldNames = new[] { "x", "y", "z", "w" },
                    SetValueMethod = (ref Vector4 vec, int fieldIndex, float val) =>
                    {
                        switch (fieldIndex)
                        {
                            case 0: vec.x = val; break;
                            case 1: vec.y = val; break;
                            case 2: vec.z = val; break;
                            case 3: vec.w = val; break;
                        }
                    },
                    UpdateUIMethod = (InputField[] inputs, object value) =>
                    {
                        Vector4 vec = (Vector4)value;
                        inputs[0].text = vec.x.ToString();
                        inputs[1].text = vec.y.ToString();
                        inputs[2].text = vec.z.ToString();
                        inputs[3].text = vec.w.ToString();
                    }
                };
            }
            else if (type == typeof(Rect))
            {
                return new StructInfo<Rect>()
                {
                    FieldNames = new[] { "x", "y", "width", "height" },
                    SetValueMethod = (ref Rect vec, int fieldIndex, float val) =>
                    {
                        switch (fieldIndex)
                        {
                            case 0: vec.x = val; break;
                            case 1: vec.y = val; break;
                            case 2: vec.width = val; break;
                            case 3: vec.height = val; break;
                        }
                    },
                    UpdateUIMethod = (InputField[] inputs, object value) =>
                    {
                        Rect vec = (Rect)value;
                        inputs[0].text = vec.x.ToString();
                        inputs[1].text = vec.y.ToString();
                        inputs[2].text = vec.width.ToString();
                        inputs[3].text = vec.height.ToString();
                    }
                };
            }
            else if (type == typeof(Color))
            {
                return new StructInfo<Color>()
                {
                    FieldNames = new[] { "r", "g", "b", "a" },
                    SetValueMethod = (ref Color vec, int fieldIndex, float val) =>
                    {
                        switch (fieldIndex)
                        {
                            case 0: vec.r = val; break;
                            case 1: vec.g = val; break;
                            case 2: vec.b = val; break;
                            case 3: vec.a = val; break;
                        }
                    },
                    UpdateUIMethod = (InputField[] inputs, object value) =>
                    {
                        Color vec = (Color)value;
                        inputs[0].text = vec.r.ToString();
                        inputs[1].text = vec.g.ToString();
                        inputs[2].text = vec.b.ToString();
                        inputs[3].text = vec.a.ToString();
                    }
                };
            }
            else
                throw new NotImplementedException();
        }
    }

    #endregion

    public class InteractiveUnityStruct : InteractiveValue
    {
        public static bool SupportsType(Type type) => s_supportedTypes.Contains(type);
        private static readonly HashSet<Type> s_supportedTypes = new HashSet<Type>
        {
            typeof(Vector2), 
            typeof(Vector3), 
            typeof(Vector4),
            typeof(Rect),
            typeof(Color) // todo might make a special editor for colors
        };

        //~~~~~~~~~ Instance ~~~~~~~~~~

        public InteractiveUnityStruct(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => true;
        public override bool SubContentWanted => true;
        public override bool WantInspectBtn => true;

        public IStructInfo StructInfo;

        public override void RefreshUIForValue()
        {
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

        #region STRUCT INFO HANDLERS

        internal Type m_lastStructType;

        internal void InitializeStructInfo()
        {
            var type = Value?.GetType() ?? FallbackType;

            if (StructInfo != null && type == m_lastStructType)
                return;

            if (StructInfo != null)
            {
                // changing types, destroy subcontent
                for (int i = 0; i < m_subContentParent.transform.childCount; i++)
                {
                    var child = m_subContentParent.transform.GetChild(i);
                    GameObject.Destroy(child.gameObject);
                }
            }

            m_lastStructType = type;

            StructInfo = StructInfoFactory.Create(type);
        }

        #endregion

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

            var editorContainer = UIFactory.CreateVerticalGroup(m_subContentParent, new Color(0.08f, 0.08f, 0.08f));
            var editorGroup = editorContainer.GetComponent<VerticalLayoutGroup>();
            editorGroup.childForceExpandWidth = false;
            editorGroup.padding.top = 4;
            editorGroup.padding.right = 4;
            editorGroup.padding.left = 4;
            editorGroup.padding.bottom = 4;
            editorGroup.spacing = 2;

            m_inputs = new InputField[StructInfo.FieldNames.Length];

            for (int i = 0; i < StructInfo.FieldNames.Length; i++)
            {
                AddEditorRow(i, editorContainer);
            }

            if (Owner.CanWrite)
            {
                var applyBtnObj = UIFactory.CreateButton(editorContainer, new Color(0.2f, 0.2f, 0.2f));
                var applyLayout = applyBtnObj.AddComponent<LayoutElement>();
                applyLayout.minWidth = 175;
                applyLayout.minHeight = 25;
                applyLayout.flexibleWidth = 0;
                var m_applyBtn = applyBtnObj.GetComponent<Button>();
                m_applyBtn.onClick.AddListener(OnSetValue);

                void OnSetValue()
                {
                    Owner.SetValue();
                    RefreshUIForValue();
                }

                var applyText = applyBtnObj.GetComponentInChildren<Text>();
                applyText.text = "Apply";
            }
        }

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            var rowObj = UIFactory.CreateHorizontalGroup(groupObj, new Color(1, 1, 1, 0));
            var rowGroup = rowObj.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childForceExpandHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.spacing = 5;

            var label = UIFactory.CreateLabel(rowObj, TextAnchor.MiddleRight);
            var labelLayout = label.AddComponent<LayoutElement>();
            labelLayout.minWidth = 50;
            labelLayout.flexibleWidth = 0;
            labelLayout.minHeight = 25;
            var labelText = label.GetComponent<Text>();
            labelText.text = $"{StructInfo.FieldNames[index]}:";
            labelText.color = Color.cyan;

            var inputFieldObj = UIFactory.CreateInputField(rowObj, 14, 3, 1);
            var inputField = inputFieldObj.GetComponent<InputField>();
            inputField.characterValidation = InputField.CharacterValidation.Decimal;
            var inputLayout = inputFieldObj.AddComponent<LayoutElement>();
            inputLayout.flexibleWidth = 0;
            inputLayout.minWidth = 120;
            inputLayout.minHeight = 25;

            m_inputs[index] = inputField;

            inputField.onValueChanged.AddListener((string val) => { Value = StructInfo.SetValue(ref this.Value, index, float.Parse(val)); });
        }

        #endregion
    }
}
