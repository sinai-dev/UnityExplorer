using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer.Tests
{
    public static class TestClass
    {
        public static List<object> List
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

        public const int ConstantInt = 5;

        public static byte[] ByteArray = new byte[16];
        public static string LongString = new string('#', 10000);

#if CPP
        public static string testStringOne = "Test";
        public static Il2CppSystem.Object testStringTwo = "string boxed as cpp object";
        public static Il2CppSystem.String testStringThree = "string boxed as cpp string";
        public static string nullString = null;

        public static Il2CppSystem.Collections.Hashtable testHashset;
        public static Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object> testList;
#endif

        static TestClass()
        {
#if CPP
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
