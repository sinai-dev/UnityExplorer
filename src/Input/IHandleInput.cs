using UnityEngine;

namespace ExplorerBeta.Input
{
    public interface IHandleInput
    {
        void Init();

        Vector2 MousePosition { get; }

        bool GetKeyDown(KeyCode key);
        bool GetKey(KeyCode key);

        bool GetMouseButtonDown(int btn);
        bool GetMouseButton(int btn);
    }
}
