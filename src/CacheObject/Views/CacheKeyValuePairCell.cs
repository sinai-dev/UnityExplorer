using UnityEngine;
using UnityEngine.UI;
using UnityExplorer.CacheObject.IValues;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.CacheObject.Views
{
    public class CacheKeyValuePairCell : CacheObjectCell
    {
        public Image Image { get; private set; }
        public InteractiveDictionary DictOwner => Occupant.Owner as InteractiveDictionary;

        public LayoutElement KeyGroupLayout;
        public Text KeyLabel;
        public ButtonRef KeyInspectButton;
        public InputFieldRef KeyInputField;
        public Text KeyInputTypeLabel;

        public static Color EvenColor = new(0.07f, 0.07f, 0.07f);
        public static Color OddColor = new(0.063f, 0.063f, 0.063f);

        public int AdjustedWidth => (int)Rect.rect.width - 70;

        //public int HalfWidth => (int)(0.5f * Rect.rect.width) - 75;
        //public int AdjustedKeyWidth => HalfWidth - 50;
        //public int AdjustedRightWidth => HalfWidth;

        private void KeyInspectClicked()
        {
            InspectorManager.Inspect((Occupant as CacheKeyValuePair).DictKey, this.Occupant);
        }

        public override GameObject CreateContent(GameObject parent)
        {
            GameObject root = base.CreateContent(parent);

            Image = root.AddComponent<Image>();

            this.NameLayout.minWidth = 70;
            this.NameLayout.flexibleWidth = 0;
            this.NameLayout.minHeight = 30;
            this.NameLayout.flexibleHeight = 0;
            this.NameLabel.alignment = TextAnchor.MiddleRight;

            this.RightGroupLayout.minWidth = AdjustedWidth * 0.55f;

            // Key area
            GameObject keyGroup = UIFactory.CreateUIObject("KeyHolder", root.transform.Find("HoriGroup").gameObject);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(keyGroup, false, false, true, true, 2, 0, 0, 4, 4, childAlignment: TextAnchor.MiddleLeft);
            KeyGroupLayout = UIFactory.SetLayoutElement(keyGroup, minHeight: 30, minWidth: (int)(AdjustedWidth * 0.44f), flexibleWidth: 0);

            // set to be after the NameLabel (our index label), and before the main horizontal group.
            keyGroup.transform.SetSiblingIndex(1);

            // key Inspect

            KeyInspectButton = UIFactory.CreateButton(keyGroup, "KeyInspectButton", "Inspect", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(KeyInspectButton.Component.gameObject, minWidth: 60, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);
            KeyInspectButton.OnClick += KeyInspectClicked;

            // label

            KeyLabel = UIFactory.CreateLabel(keyGroup, "KeyLabel", "<i>empty</i>", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(KeyLabel.gameObject, minWidth: 50, flexibleWidth: 999, minHeight: 25);

            // Type label for input field

            KeyInputTypeLabel = UIFactory.CreateLabel(keyGroup, "InputTypeLabel", "<i>null</i>", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(KeyInputTypeLabel.gameObject, minWidth: 55, flexibleWidth: 0, minHeight: 25, flexibleHeight: 0);

            // input field

            KeyInputField = UIFactory.CreateInputField(keyGroup, "KeyInput", "empty");
            UIFactory.SetLayoutElement(KeyInputField.UIRoot, minHeight: 25, flexibleHeight: 0, flexibleWidth: 0, preferredWidth: 200);
            //KeyInputField.lineType = InputField.LineType.MultiLineNewline;
            KeyInputField.Component.readOnly = true;

            return root;
        }

        protected override void ConstructEvaluateHolder(GameObject parent)
        {
            // not used
        }
    }
}
