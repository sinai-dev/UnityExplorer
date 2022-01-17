using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Config;

namespace UnityExplorer
{
    public interface IExplorerLoader
    {
        string ExplorerFolder { get; }
        string UnhollowedModulesFolder { get; }

        ConfigHandler ConfigHandler { get; }

        Action<object> OnLogMessage { get; }
        Action<object> OnLogWarning { get; }
        Action<object> OnLogError { get; }
    }
}
