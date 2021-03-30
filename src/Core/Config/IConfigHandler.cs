using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Core.Config
{
    public interface IConfigHandler
    {
        void RegisterConfigElement<T>(ConfigElement<T> element);

        void SetConfigValue<T>(ConfigElement<T> element, T value);

        T GetConfigValue<T>(ConfigElement<T> element);

        void Init();

        void LoadConfig();

        void SaveConfig();
    }
}
