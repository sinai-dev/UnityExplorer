using UnityEngine;

namespace Explorer.UI.Main
{
    public abstract class BaseMainMenuPage
    {
        public virtual string Name { get; }

        public Vector2 scroll = Vector2.zero;

        public abstract void Init();

        public abstract void DrawWindow();

        public abstract void Update();
    }
}
