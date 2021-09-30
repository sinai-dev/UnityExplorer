using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityExplorer.Core.Input
{
    public interface IHandleInput
    {
        Vector2 MousePosition { get; }
        Vector2 MouseScrollDelta { get; }

        bool GetKeyDown(KeyCode key);
        bool GetKey(KeyCode key);

        bool GetMouseButtonDown(int btn);
        bool GetMouseButton(int btn);

        BaseInputModule UIInputModule { get; }

        void AddUIInputModule();
        void ActivateModule();
    }
}