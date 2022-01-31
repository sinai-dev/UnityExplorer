#if MONO
using System;
using UnityEngine;

namespace UnityExplorer.Runtime
{
    public class MonoHelper : UERuntimeHelper
    {
        public override void SetupEvents()
        {
            Application.logMessageReceived += Application_logMessageReceived;
        }

        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
             => ExplorerCore.LogUnity(condition, type);
    }
}

#endif