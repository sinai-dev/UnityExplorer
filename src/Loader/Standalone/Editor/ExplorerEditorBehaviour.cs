#if STANDALONE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace UnityExplorer.Loader.Standalone
{
    public class ExplorerEditorBehaviour : MonoBehaviour
    {
        internal void Awake()
        {
            ExplorerEditorLoader.Initialize();
            DontDestroyOnLoad(this);
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        internal void OnDestroy()
        {
            OnApplicationQuit();
        }

        internal void OnApplicationQuit()
        {
            if (UI.UIManager.UIRoot)
                Destroy(UI.UIManager.UIRoot.transform.root.gameObject);
        }
    }
}
#endif