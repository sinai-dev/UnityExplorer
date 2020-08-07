//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using Mono.CSharp;
//using UnityEngine;
//using Attribute = System.Attribute;
//using Object = UnityEngine.Object;

//namespace Explorer
//{
//    public class REPL : InteractiveBase
//    {
//        static REPL()
//        {
//            var go = new GameObject("UnityREPL");
//            GameObject.DontDestroyOnLoad(go);
//            //go.transform.parent = HPExplorer.Instance.transform;
//            MB = go.AddComponent<ReplHelper>();
//        }

//        [Documentation("MB - A dummy MonoBehaviour for accessing Unity.")]
//        public static ReplHelper MB { get; }

//        [Documentation("find<T>() - find a UnityEngine.Object of type T.")]
//        public static T find<T>() where T : Object
//        {
//            return MB.Find<T>();
//        }

//        [Documentation("findAll<T>() - find all UnityEngine.Object of type T.")]
//        public static T[] findAll<T>() where T : Object
//        {
//            return MB.FindAll<T>();
//        }

//        [Documentation("runCoroutine(enumerator) - runs an IEnumerator as a Unity coroutine.")]
//        public static object runCoroutine(IEnumerator i)
//        {
//            return MB.RunCoroutine(i);
//        }

//        [Documentation("endCoroutine(co) - ends a Unity coroutine.")]
//        public static void endCoroutine(Coroutine c)
//        {
//            MB.EndCoroutine(c);
//        }

//        ////[Documentation("type<T>() - obtain type info about a type T. Provides some Reflection helpers.")]
//        ////public static TypeHelper type<T>()
//        ////{
//        ////    return new TypeHelper(typeof(T));
//        ////}

//        ////[Documentation("type(obj) - obtain type info about object obj. Provides some Reflection helpers.")]
//        ////public static TypeHelper type(object instance)
//        ////{
//        ////    return new TypeHelper(instance);
//        ////}

//        //[Documentation("dir(obj) - lists all available methods and fiels of a given obj.")]
//        //public static string dir(object instance)
//        //{
//        //    return type(instance).info();
//        //}

//        //[Documentation("dir<T>() - lists all available methods and fields of type T.")]
//        //public static string dir<T>()
//        //{
//        //    return type<T>().info();
//        //}

//        //[Documentation("findrefs(obj) - find references to the object in currently loaded components.")]
//        //public static Component[] findrefs(object obj)
//        //{
//        //    if (obj == null) throw new ArgumentNullException(nameof(obj));

//        //    var results = new List<Component>();
//        //    foreach (var component in Object.FindObjectsOfType<Component>())
//        //    {
//        //        var type = component.GetType();

//        //        var nameBlacklist = new[] { "parent", "parentInternal", "root", "transform", "gameObject" };
//        //        var typeBlacklist = new[] { typeof(bool) };

//        //        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//        //            .Where(x => x.CanRead && !nameBlacklist.Contains(x.Name) && !typeBlacklist.Contains(x.PropertyType)))
//        //        {
//        //            try
//        //            {
//        //                if (Equals(prop.GetValue(component, null), obj))
//        //                {
//        //                    results.Add(component);
//        //                    goto finish;
//        //                }
//        //            }
//        //            catch { }
//        //        }
//        //        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
//        //            .Where(x => !nameBlacklist.Contains(x.Name) && !typeBlacklist.Contains(x.FieldType)))
//        //        {
//        //            try
//        //            {
//        //                if (Equals(field.GetValue(component), obj))
//        //                {
//        //                    results.Add(component);
//        //                    goto finish;
//        //                }
//        //            }
//        //            catch { }
//        //        }
//        //    finish:;
//        //    }

//        //    return results.ToArray();
//        //}

//        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
//        private class DocumentationAttribute : Attribute
//        {
//            public DocumentationAttribute(string doc)
//            {
//                Docs = doc;
//            }

//            public string Docs { get; }
//        }
//    }
//}