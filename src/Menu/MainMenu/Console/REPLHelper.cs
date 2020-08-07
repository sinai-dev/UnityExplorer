//using System.Collections;
//using MelonLoader;
//using UnityEngine;

//namespace Explorer
//{
//    public class ReplHelper : MonoBehaviour
//    {
//        public T Find<T>() where T : Object
//        {
//            return FindObjectOfType<T>();
//        }

//        public T[] FindAll<T>() where T : Object
//        {
//            return FindObjectsOfType<T>();
//        }

//        public object RunCoroutine(IEnumerator enumerator)
//        {
//            return MelonCoroutines.Start(enumerator);
//        }

//        public void EndCoroutine(Coroutine c)
//        {
//            StopCoroutine(c);
//        }
//    }
//}