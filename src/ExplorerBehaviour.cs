using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if CPP
using UnhollowerRuntimeLib;
#endif

namespace UnityExplorer
{
    // Handles all Behaviour update calls for UnityExplorer (Update, FixedUpdate, OnPostRender).
    // Basically just a wrapper which calls the corresponding methods in ExplorerCore.

    public class ExplorerBehaviour : MonoBehaviour
    {
        internal static ExplorerBehaviour Instance { get; private set; }

        internal static void Setup()
        {
#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<ExplorerBehaviour>();
#endif

            var obj = new GameObject("ExplorerBehaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<ExplorerBehaviour>();
        }

#if CPP
        public ExplorerBehaviour(IntPtr ptr) : base(ptr) { }
#endif

        internal void Update()
        {
            ExplorerCore.Update();
        }
    }
}
