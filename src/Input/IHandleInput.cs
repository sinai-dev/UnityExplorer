using UnityEngine;

namespace UnityExplorer.Input
{
    public interface IHandleInput
    {
        Vector2 MousePosition { get; }

        bool GetKeyDown(KeyCode key);
        bool GetKey(KeyCode key);

        bool GetMouseButtonDown(int btn);
        bool GetMouseButton(int btn);
    }
}
