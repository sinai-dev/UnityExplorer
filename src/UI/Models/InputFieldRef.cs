using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI
{
    public class InputFieldRef : UIBehaviourModel
    {
        public InputFieldRef(InputField InputField) 
        { 
            this.Component = InputField;
            Rect = InputField.GetComponent<RectTransform>();
            PlaceholderText = InputField.placeholder.TryCast<Text>();
            InputField.onValueChanged.AddListener(OnInputChanged);
        }

        public event Action<string> OnValueChanged;

        public InputField Component;
        public Text PlaceholderText;
        public RectTransform Rect;

        public string Text
        {
            get => Component.text;
            set => Component.text = value;
        }

        public TextGenerator TextGenerator => Component.cachedInputTextGenerator;
        public bool ReachedMaxVerts => TextGenerator.vertexCount >= UIManager.MAX_TEXT_VERTS;

        private bool updatedWanted;

        private void OnInputChanged(string value)
        {
            updatedWanted = true;
        }

        public override void Update()
        {
            if (updatedWanted)
            {
                LayoutRebuilder.MarkLayoutForRebuild(Rect);

                OnValueChanged?.Invoke(Component.text);
                updatedWanted = false;
            }
        }

        public override GameObject UIRoot => Component.gameObject;

        public override void ConstructUI(GameObject parent)
        {
            throw new NotImplementedException();
        }
    }
}
