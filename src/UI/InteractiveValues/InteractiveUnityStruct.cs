using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.Core.Unity;
using UnityExplorer.UI;

namespace UnityExplorer.UI.InteractiveValues
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
            //typeof(Color) // todo might make a special editor for colors
        };

        //~~~~~~~~~ Instance ~~~~~~~~~~

        public InteractiveUnityStruct(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => true;
        public override bool SubContentWanted => true;
        public override bool WantInspectBtn => true;

        public IStructInfo StructInfo;

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

            if (StructInfo != null)
                DestroySubContent();

            m_lastStructType = type;

            StructInfo = StructInfoFactory.Create(type);

            if (m_subContentParent.activeSelf)
            {
                ConstructSubcontent();
            }
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

            var editorContainer = UIFactory.CreateVerticalGroup(m_subContentParent, "EditorContent", false, true, true, true, 2, new Vector4(4,4,4,4),
                new Color(0.08f, 0.08f, 0.08f));

            m_inputs = new InputField[StructInfo.FieldNames.Length];

            for (int i = 0; i < StructInfo.FieldNames.Length; i++)
                AddEditorRow(i, editorContainer);

            if (Owner.CanWrite)
            {
                var applyBtn = UIFactory.CreateButton(editorContainer, "ApplyButton", "Apply", OnSetValue, new Color(0.2f, 0.2f, 0.2f));
                UIFactory.SetLayoutElement(applyBtn.gameObject, minWidth: 175, minHeight: 25, flexibleWidth: 0);

                void OnSetValue()
                {
                    Owner.SetValue();
                    RefreshUIForValue();
                }
            }
        }

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            var rowObj = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow", false, true, true, true, 5, default, new Color(1, 1, 1, 0));

            var label = UIFactory.CreateLabel(rowObj, "RowLabel", $"{StructInfo.FieldNames[index]}:", TextAnchor.MiddleRight, Color.cyan);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 50, flexibleWidth: 0, minHeight: 25);

            var inputFieldObj = UIFactory.CreateInputField(rowObj, "InputField", "...", 14, 3, 1);
            UIFactory.SetLayoutElement(inputFieldObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

            var inputField = inputFieldObj.GetComponent<InputField>();
            m_inputs[index] = inputField;

            inputField.onValueChanged.AddListener((string val) => { Value = StructInfo.SetValue(ref this.Value, index, float.Parse(val)); });
        }

        #endregion
    }
}
