using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if CPP
using UnhollowerRuntimeLib;
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Tests
{
    public static class TestClass
    {
        public static List<int> AWritableList = new List<int> { 1, 2, 3, 4, 5 };
        public static Dictionary<string, int> AWritableDict = new Dictionary<string, int> { { "one", 1 }, { "two", 2 } };

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
        public static List<Il2CppSystem.Object> TestWritableBoxedList;

        public static string testStringOne = "Test";
        public static Il2CppSystem.Object testStringTwo = "string boxed as cpp object";
        public static Il2CppSystem.String testStringThree = "string boxed as cpp string";
        public static string nullString = null;

        public static List<Il2CppSystem.Object> CppBoxedList;
        public static Il2CppStructArray<int> CppIntStructArray;
        public static Il2CppStringArray CppStringArray;
        public static Il2CppReferenceArray<Il2CppSystem.Object> CppReferenceArray;

        public static Il2CppSystem.Object cppBoxedInt;
        public static Il2CppSystem.Int32 cppInt;

        public static Il2CppSystem.Collections.Hashtable cppHashset;

#endif

        static TestClass()
        {
            for (int i = 0; i < BigList.Capacity; i++)
                BigList.Add(i.ToString());

#if CPP

            CppBoxedList = new List<Il2CppSystem.Object>();
            CppBoxedList.Add((Il2CppSystem.String)"boxedString");
            CppBoxedList.Add(new Il2CppSystem.Int32 { m_value = 5 }.BoxIl2CppObject());

            try
            {
                var cppType = Il2CppType.Of<CameraClearFlags>();
                if (cppType != null)
                {
                    var boxedEnum = Il2CppSystem.Enum.Parse(cppType, "Color");
                    CppBoxedList.Add(boxedEnum);
                }

                var structBox = Vector3.one.BoxIl2CppObject();
                CppBoxedList.Add(structBox);

            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Test fail: {ex}");
            }

            CppIntStructArray = new UnhollowerBaseLib.Il2CppStructArray<int>(5);
            CppIntStructArray[0] = 0;
            CppIntStructArray[1] = 1;
            CppIntStructArray[2] = 2;
            CppIntStructArray[3] = 3;
            CppIntStructArray[4] = 4;

            CppStringArray = new UnhollowerBaseLib.Il2CppStringArray(2);
            CppStringArray[0] = "hello, ";
            CppStringArray[1] = "world!";

            CppReferenceArray = new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Object>(3);
            CppReferenceArray[0] = new Il2CppSystem.Int32 { m_value = 5 }.BoxIl2CppObject();
            CppReferenceArray[1] = null;
            CppReferenceArray[2] = (Il2CppSystem.String)"whats up";

            cppBoxedInt = new Il2CppSystem.Int32() { m_value = 5 }.BoxIl2CppObject();
            cppInt = new Il2CppSystem.Int32 { m_value = 420 };

            TestWritableBoxedList = new List<Il2CppSystem.Object>();
            TestWritableBoxedList.Add(new Il2CppSystem.Int32 { m_value = 1 }.BoxIl2CppObject());
            TestWritableBoxedList.Add(new Il2CppSystem.Int32 { m_value = 2 }.BoxIl2CppObject());
            TestWritableBoxedList.Add(new Il2CppSystem.Int32 { m_value = 3 }.BoxIl2CppObject());
            TestWritableBoxedList.Add(new Il2CppSystem.Int32 { m_value = 4 }.BoxIl2CppObject());

            cppHashset = new Il2CppSystem.Collections.Hashtable();
            cppHashset.Add("key1", "itemOne");
            cppHashset.Add("key2", "itemTwo");
            cppHashset.Add("key3", "itemThree");

#endif
        }
    }
}
