using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace UnityExplorer.UI.Main
{
    public enum MenuPages
    {
        Home,
        Search,
        CSConsole,
        Options
    }

    public abstract class BaseMenuPage
    {
        public abstract string Name { get; }
        public abstract MenuPages Type { get; }
        public bool WasDisabled { get; internal set; }

        public GameObject Content;
        public Button RefNavbarButton { get; set; }

        public bool Enabled
        {
            get => Content?.activeSelf ?? false;
            set => Content?.SetActive(true);
        }

        public abstract bool Init();
        public abstract void Update();
    }
}
