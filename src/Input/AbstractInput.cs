using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer.Input
{
    public abstract class AbstractInput
    {
        public abstract void Init();

        public abstract Vector2 MousePosition { get; }

        public abstract bool GetKeyDown(KeyCode key);
        public abstract bool GetKey(KeyCode key);

        public abstract bool GetMouseButtonDown(int btn);
        public abstract bool GetMouseButton(int btn);
    }
}
