using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.CacheObject.IValues;
using UnityExplorer.CacheObject.Views;

namespace UnityExplorer.CacheObject
{
    public class CacheKeyValuePair : CacheObjectBase
    {
        //public InteractiveList CurrentList { get; set; }

        public int DictIndex;
        public object DictKey;
        public object DisplayedKey;

        public bool KeyInputWanted;
        public bool InspectWanted;
        public string KeyLabelText;
        public string KeyInputText;
        public string KeyInputTypeText;

        public float DesiredKeyWidth;
        public float DesiredValueWidth;

        public override bool ShouldAutoEvaluate => true;
        public override bool HasArguments => false;
        public override bool CanWrite => Owner.CanWrite;

        public void SetDictOwner(InteractiveDictionary dict, int index)
        {
            this.Owner = dict;
            this.DictIndex = index;
        }

        public void SetKey(object key)
        {
            this.DictKey = key;
            this.DisplayedKey = key.TryCast();

            var type = DisplayedKey.GetType();
            if (ParseUtility.CanParse(type))
            {
                KeyInputWanted = true;
                KeyInputText = ParseUtility.ToStringForInput(DisplayedKey, type);
                KeyInputTypeText = SignatureHighlighter.Parse(type, false);
            }
            else
            {
                KeyInputWanted = false;
                InspectWanted = type != typeof(bool) && !type.IsEnum;
                KeyLabelText = ToStringUtility.ToStringWithType(DisplayedKey, type, true);
            }
        }

        public override void SetDataToCell(CacheObjectCell cell)
        {
            base.SetDataToCell(cell);

            var kvpCell = cell as CacheKeyValuePairCell;

            kvpCell.NameLabel.text = $"{DictIndex}:";
            kvpCell.Image.color = DictIndex % 2 == 0 ? CacheListEntryCell.EvenColor : CacheListEntryCell.OddColor;

            if (KeyInputWanted)
            {
                kvpCell.KeyInputField.UIRoot.SetActive(true);
                kvpCell.KeyInputTypeLabel.gameObject.SetActive(true);
                kvpCell.KeyLabel.gameObject.SetActive(false);
                kvpCell.KeyInspectButton.Component.gameObject.SetActive(false);

                kvpCell.KeyInputField.Text = KeyInputText;
                kvpCell.KeyInputTypeLabel.text = KeyInputTypeText;
            }
            else
            {
                kvpCell.KeyInputField.UIRoot.SetActive(false);
                kvpCell.KeyInputTypeLabel.gameObject.SetActive(false);
                kvpCell.KeyLabel.gameObject.SetActive(true);
                kvpCell.KeyInspectButton.Component.gameObject.SetActive(InspectWanted);

                kvpCell.KeyLabel.text = KeyLabelText;
            }
        }

        public override void TrySetUserValue(object value)
        {
            (Owner as InteractiveDictionary).TrySetValueToKey(DictKey, value, DictIndex);
        }


        protected override bool SetCellEvaluateState(CacheObjectCell cell)
        {
            // not needed
            return false;
        }
    }
}
