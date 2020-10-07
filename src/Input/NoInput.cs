using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Explorer.Input
{
    // Just a stub for games where no Input module was able to load at all.

    public class NoInput : AbstractInput
    {
        public override Vector2 MousePosition => Vector2.zero;

        public override bool GetKey(KeyCode key) => false;

        public override bool GetKeyDown(KeyCode key) => false;

        public override bool GetMouseButton(int btn) => false;

        public override bool GetMouseButtonDown(int btn) => false;

        public override void Init() { }
    }
}
