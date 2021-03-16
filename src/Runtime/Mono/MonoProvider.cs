#if MONO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityExplorer.Runtime.Mono
{
    public class MonoProvider : RuntimeProvider
    {
        public override void Initialize()
        {
        }

        public override void SetupEvents()
        {
            Application.logMessageReceived += ExplorerCore.Instance.OnUnityLog;
            //SceneManager.sceneLoaded += ExplorerCore.Instance.OnSceneLoaded1;
            //SceneManager.activeSceneChanged += ExplorerCore.Instance.OnSceneLoaded2;
        }
    }
}

#endif