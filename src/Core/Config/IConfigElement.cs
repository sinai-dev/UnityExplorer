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

        object GetLoaderConfigValue();

        Action OnValueChangedNotify { get; set; }
    }
}
