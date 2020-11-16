#if CPP
using System;
using UnityEngine.Events;

namespace UnityExplorer.Helpers
{
    // Possibly temporary, just so Il2Cpp can do the same style "AddListener" as Mono.
    // Just saves me having a preprocessor directive for every single AddListener.

    public static class EventHelper
    {
        public static void AddListener(this UnityEvent action, Action listener)
        {
            action.AddListener(listener);
        }

        public static void AddListener<T>(this UnityEvent<T> action, Action<T> listener)
        {
            action.AddListener(listener);
        }

        public static void AddListener<T0, T1>(this UnityEvent<T0, T1> action, Action<T0, T1> listener)
        {
            action.AddListener(listener);
        }

        public static void AddListener<T0, T1, T2>(this UnityEvent<T0, T1, T2> action, Action<T0, T1, T2> listener)
        {
            action.AddListener(listener);
        }
    }
}
#endif