using System.Collections;
//using Il2CppSystem;
using MelonLoader;
using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace Explorer
{
    public class ReplHelper : MonoBehaviour
    {
        public ReplHelper(IntPtr intPtr) : base(intPtr) { }

        public T Find<T>() where T : Object
        {
            return FindObjectOfType<T>();
        }

        public T[] FindAll<T>() where T : Object
        {
            return FindObjectsOfType<T>();
        }

        //public object RunCoroutine(IEnumerator enumerator)
        //{
        //    return MelonCoroutines.Start(enumerator);
        //}

        //public void EndCoroutine(Coroutine c)
        //{
        //    StopCoroutine(c);
        //}
    }
}