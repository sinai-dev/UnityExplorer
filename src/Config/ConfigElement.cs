using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Config
{
    public class ConfigElement<T> : IConfigElement
    {
        public string Name { get; }
        public string Description { get; }

        public bool IsInternal { get; }
        public Type ElementType => typeof(T);

        public Action<T> OnValueChanged;
        public Action OnValueChangedNotify { get; set; }

        public object DefaultValue { get; }

        public ConfigHandler Handler => IsInternal
            ? ConfigManager.InternalHandler
            : ConfigManager.Handler;

        public T Value
        {
            get => m_value;
            set => SetValue(value);
        }
        private T m_value;

        object IConfigElement.BoxedValue
        {
            get => m_value;
            set => SetValue((T)value);
        }

        public ConfigElement(string name, string description, T defaultValue, bool isInternal = false)
        {
            Name = name;
            Description = description;

            m_value = defaultValue;
            DefaultValue = defaultValue;

            IsInternal = isInternal;

            ConfigManager.RegisterConfigElement(this);
        }

        private void SetValue(T value)
        {
            if ((m_value == null && value == null) || (m_value != null && m_value.Equals(value)))
                return;

            m_value = value;

            Handler.SetConfigValue(this, value);

            OnValueChanged?.Invoke(value);
            OnValueChangedNotify?.Invoke();

            Handler.OnAnyConfigChanged();
        }

        object IConfigElement.GetLoaderConfigValue() => GetLoaderConfigValue();

        public T GetLoaderConfigValue()
        {
            return Handler.GetConfigValue(this);
        }

        public void RevertToDefaultValue()
        {
            Value = (T)DefaultValue;
        }
    }
}
