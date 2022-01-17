#if MONO
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExplorer;

namespace UnityExplorer.Runtime
{
    public class MonoProvider : RuntimeHelper
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