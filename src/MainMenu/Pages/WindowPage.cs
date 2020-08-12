using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Explorer
{
    public abstract class WindowPage
    {
        public virtual string Name { get; set; }

        public Vector2 scroll = Vector2.zero;

        public abstract void Init();

        public abstract void DrawWindow();

        public abstract void Update();
    }
}
