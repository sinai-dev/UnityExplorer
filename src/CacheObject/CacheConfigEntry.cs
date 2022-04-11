using UnityExplorer.CacheObject.Views;
using UnityExplorer.Config;

namespace UnityExplorer.CacheObject
{
    public class CacheConfigEntry : CacheObjectBase
    {
        public CacheConfigEntry(IConfigElement configElement)
        {
            this.RefConfigElement = configElement;
            this.FallbackType = configElement.ElementType;

            this.NameLabelText = $"<color=cyan>{configElement.Name}</color>" +
                $"\r\n<color=grey><i>{configElement.Description}</i></color>";
            this.NameLabelTextRaw = string.Empty;

            configElement.OnValueChangedNotify += UpdateValueFromSource;
        }

        public IConfigElement RefConfigElement;

        public override bool ShouldAutoEvaluate => true;
        public override bool HasArguments => false;
        public override bool CanWrite => true;

        public void UpdateValueFromSource()
        {
            //if (RefConfigElement.BoxedValue.Equals(this.Value))
            //    return;

            SetValueFromSource(RefConfigElement.BoxedValue);

            if (this.CellView != null)
                this.SetDataToCell(CellView);
        }

        public override void TrySetUserValue(object value)
        {
            this.Value = value;
            RefConfigElement.BoxedValue = value;
        }

        protected override bool TryAutoEvaluateIfUnitialized(CacheObjectCell cell) => true;
    }
}
