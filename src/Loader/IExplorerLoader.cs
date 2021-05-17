using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityExplorer.Core.Config;

namespace UnityExplorer
{
    public interface IExplorerLoader
    {
        string ExplorerFolder { get; }

        string ConfigFolder { get; }
        ConfigHandler ConfigHandler { get; }

        Action<object> OnLogMessage { get; }
        Action<object> OnLogWarning { get; }
        Action<object> OnLogError { get; }

        void SetupCursorPatches();
    }
}
