using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Core.Config;
using UnityExplorer.UI.CacheObject.Views;

namespace UnityExplorer.UI.CacheObject
{
    public class CacheConfigEntry : CacheObjectBase
    {
        public CacheConfigEntry(IConfigElement configElement)
        {
            this.RefConfigElement = configElement;

            this.NameLabelText = $"<color=cyan>{configElement.Name}</color>" +
                $"\r\n<color=grey><i>{configElement.Description}</i></color>";

            this.FallbackType = configElement.ElementType;

            configElement.OnValueChangedNotify += UpdateValueFromSource;
        }

        public IConfigElement RefConfigElement;

        public override bool ShouldAutoEvaluate => true;
        public override bool HasArguments => false;
        public override bool CanWrite => true;

        public void UpdateValueFromSource()
        {
            if (RefConfigElement.BoxedValue.Equals(this.Value))
                return;

            SetValueFromSource(RefConfigElement.BoxedValue);
        }

        public override void TrySetUserValue(object value)
        {
            this.Value = value;
            RefConfigElement.BoxedValue = value;
        }

        protected override bool SetCellEvaluateState(CacheObjectCell cell) => false;
    }
}
