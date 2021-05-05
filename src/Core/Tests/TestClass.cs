using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityExplorer.Tests
{
    public static class TestClass
    {
        public static IEnumerable ANestedList = new List<List<List<string>>>
        {
            new List<List<string>>
            {
                new List<string>
                {
                    "one",
                    "two",
                    "one",
                    "two",
                    "one",
                    "two",
                    "one",
                    "two",
                    "one",
                    "two",
                    "one",
                    "two",
                    "one",
                    "two",
                    "one",
                    "two",
                },
                new List<string>
                {
                    "three",
                    "four",
                }
            },
            new List<List<string>>
            {
                new List<string>
                {
                    "five"
                }
            }
        };

        public static IDictionary DictTest = new Dictionary<object, object>
        {
            { 1, 2 },
            { "one", "two" },
            { true, false },
            { new Vector3(0,1,2), new Vector3(1,2,3) },
            { CameraClearFlags.Depth, CameraClearFlags.Color },
            { "################################################\r\n##########", null },
            { "subdict", new Dictionary<object,object> { { "key", "value" } } }
        };

        public const int ConstantInt = 5;

        public static byte[] ByteArray = new byte[16];
        public static string LongString = new string('#', 10000);
        public static List<string> BigList = new List<string>(10000);

        public static List<object> RandomList
        {
            get
            {
                var list = new List<object>();
                int count = UnityEngine.Random.Range(0, 100);
                for (int i = 0; i < count; i++)
                    list.Add(GetRandomObject());
                return list;
            }
        }

        private static void TestGeneric<T>() 
        {
            ExplorerCore.Log("Test1 " + typeof(T).FullName);
        }
        
        private static void TestGenericClass<T>() where T : class
        {
            ExplorerCore.Log("Test2 " + typeof(T).FullName);
        }
        
        //private static void TestGenericMultiInterface<T>() where T : IEnumerable, IList, ICollection
        //{
        //    ExplorerCore.Log("Test3 " + typeof(T).FullName);
        //}
        
        private static void TestComponent<T>() where T : Component
        {
            ExplorerCore.Log("Test3 " + typeof(T).FullName);
        }
        
        private static void TestStruct<T>() where T : struct
        {
            ExplorerCore.Log("Test3 " + typeof(T).FullName);
        }

        private static object GetRandomObject()
        {
            object ret = null;

            int ran = UnityEngine.Random.Range(0, 7);
            switch (ran)
            {
                case 0: return null;
                case 1: return 123;
                case 2: return true;
                case 3: return "hello";
                case 4: return 50.5f;
                case 5: return UnityEngine.CameraClearFlags.Color;
                case 6: return new List<string> { "sub list", "lol" };
            }

            return ret;
        }

#if CPP
        public static string testStringOne = "Test";
        public static Il2CppSystem.Object testStringTwo = "string boxed as cpp object";
        public static Il2CppSystem.String testStringThree = "string boxed as cpp string";
        public static string nullString = null;

        public static Il2CppSystem.Collections.Hashtable testHashset;
        public static Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object> testList;


        //public static Il2CppSystem.Nullable<Quaternion> NullableQuaternion;
        //public static Il2CppSystem.Nullable<int> NullableInt = new Il2CppSystem.Nullable<int>(5);
        //public static Il2CppSystem.Nullable<bool> NullableBool = new Il2CppSystem.Nullable<bool>(false);
#endif

        static TestClass()
        {
            for (int i = 0; i < BigList.Capacity; i++)
                BigList.Add(i.ToString());

#if CPP
            //NullableQuaternion = new Il2CppSystem.Nullable<Quaternion>();
            //NullableQuaternion.value = Quaternion.identity;

            testHashset = new Il2CppSystem.Collections.Hashtable();
            testHashset.Add("key1", "itemOne");
            testHashset.Add("key2", "itemTwo");
            testHashset.Add("key3", "itemThree");

            testList = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>(3);
            testList.Add("One");
            testList.Add("Two");
            testList.Add("Three");
#endif
        }
    }
}
