using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.UI.CacheObject.Views;
using UnityExplorer.UI.IValues;
using UnityExplorer.UI.Utility;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheKeyValuePair : CacheObjectBase
    {
        //public InteractiveList CurrentList { get; set; }

        public int DictIndex;
        public object DictKey;

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
            var type = key.GetActualType();
            if (type == typeof(string) || (type.IsPrimitive && !(type == typeof(bool))) || type == typeof(decimal))
            {
                KeyInputWanted = true;
                KeyInputText = key.ToString();
                KeyInputTypeText = SignatureHighlighter.Parse(type, false);
            }
            else
            {
                KeyInputWanted = false;
                InspectWanted = type != typeof(bool) && !type.IsEnum;
                KeyLabelText = ToStringUtility.ToStringWithType(key, type, true);
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
                kvpCell.KeyInputField.gameObject.SetActive(true);
                kvpCell.KeyInputTypeLabel.gameObject.SetActive(true);
                kvpCell.KeyLabel.gameObject.SetActive(false);
                kvpCell.KeyInspectButton.Button.gameObject.SetActive(false);

                kvpCell.KeyInputField.text = KeyInputText;
                kvpCell.KeyInputTypeLabel.text = KeyInputTypeText;
            }
            else
            {
                kvpCell.KeyInputField.gameObject.SetActive(false);
                kvpCell.KeyInputTypeLabel.gameObject.SetActive(false);
                kvpCell.KeyLabel.gameObject.SetActive(true);
                kvpCell.KeyInspectButton.Button.gameObject.SetActive(InspectWanted);

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
