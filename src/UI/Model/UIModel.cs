using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.UI.Models
{
    public abstract class UIModel
    {
        public abstract GameObject UIRoot { get; }

        public bool Visible
        {
            get => UIRoot?.activeInHierarchy ?? false;
            set => UIRoot?.SetActive(value);
        }

        public abstract void ConstructUI(GameObject parent);

        public virtual void Destroy()
        {
            if (UIRoot)
                GameObject.Destroy(UIRoot);
        }
    }
}
