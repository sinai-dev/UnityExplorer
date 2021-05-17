using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.UI.Models;

namespace UnityExplorer.UI
{
    public class InputFieldRef : UIModel
    {
        public static readonly HashSet<InputFieldRef> inputsPendingUpdate = new HashSet<InputFieldRef>();

        public static void UpdateInstances()
        {
            if (inputsPendingUpdate.Any())
            {
                foreach (var entry in inputsPendingUpdate)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(entry.Rect);
                    entry.OnValueChanged?.Invoke(entry.Component.text);
                }

                inputsPendingUpdate.Clear();
            }
        }

        public InputFieldRef(InputField component) 
        { 
            this.Component = component;
            Rect = component.GetComponent<RectTransform>();
            PlaceholderText = component.placeholder.TryCast<Text>();
            component.onValueChanged.AddListener(OnInputChanged);
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


        private void OnInputChanged(string value)
        {
            if (!inputsPendingUpdate.Contains(this))
                inputsPendingUpdate.Add(this);
        }

        public override GameObject UIRoot => Component.gameObject;

        public override void ConstructUI(GameObject parent)
        {
            throw new NotImplementedException();
        }
    }
}
