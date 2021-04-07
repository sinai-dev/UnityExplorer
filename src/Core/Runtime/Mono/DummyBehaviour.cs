#if MONO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.Core.Runtime.Mono
{
    public class DummyBehaviour : MonoBehaviour
    {
        public static DummyBehaviour Instance;

        public static void Setup()
        {
            var obj = new GameObject("Explorer_DummyBehaviour");
            DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;

            obj.AddComponent<DummyBehaviour>();
        }

        internal void Awake()
        {
            Instance = this;
        }
    }
}
#endif