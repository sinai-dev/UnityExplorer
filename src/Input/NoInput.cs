using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExplorerBeta.Input
{
    // Just a stub for games where no Input module was able to load at all.

    public class NoInput : IAbstractInput
    {
        public Vector2 MousePosition => Vector2.zero;

        public bool GetKey(KeyCode key) => false;
        public bool GetKeyDown(KeyCode key) => false;

        public bool GetMouseButton(int btn) => false;
        public bool GetMouseButtonDown(int btn) => false;

        public void Init() { }
    }
}
