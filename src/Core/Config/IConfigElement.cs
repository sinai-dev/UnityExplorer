using System;

namespace UnityExplorer.Core.Config
{
    public interface IConfigElement
    {
        string Name { get; }
        string Description { get; }

        bool IsInternal { get; }
        Type ElementType { get; }

        object BoxedValue { get; set; }
        object DefaultValue { get; }

        object GetLoaderConfigValue();

        void RevertToDefaultValue();

        Action OnValueChangedNotify { get; set; }
    }
}
