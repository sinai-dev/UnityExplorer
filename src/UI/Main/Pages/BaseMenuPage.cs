using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Explorer.UI.Main.Pages
{
    public abstract class BaseMenuPage
    {
        public abstract string Name { get; }

        public GameObject Content;
        public Button RefNavbarButton { get; set; }

        public bool Enabled
        {
            get => Content?.activeSelf ?? false;
            set => Content?.SetActive(true);
        }


        public abstract void Init();
        public abstract void Update();
    }
}
