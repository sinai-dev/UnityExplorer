using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityExplorer.UI;
using UnityExplorer.CacheObject.IValues;
#if CPP
using UnhollowerRuntimeLib;
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Tests
{
    public class TestIndexer : IList<int>
    {
        private readonly List<int> list = new List<int>() { 1, 2, 3, 4, 5 };

        public int Count => list.Count;
        public bool IsReadOnly => false;

        int IList<int>.this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }

        public int IndexOf(int item) => list.IndexOf(item);
        public bool Contains(int item) => list.Contains(item);

        public void Add(int item) => list.Add(item);
        public void Insert(int index, int item) => list.Insert(index, item);

        public bool Remove(int item) => list.Remove(item);
        public void RemoveAt(int index) => list.RemoveAt(index);

        public void Clear() => list.Clear();

        public void CopyTo(int[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        public IEnumerator<int> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }

    public static class TestClass
    {
        public static readonly TestIndexer AAAAATest = new TestIndexer();

        public static void ATestMethod(string s, float f, Vector3 vector, DateTime date, Quaternion quater, bool b, CameraClearFlags enumvalue)
        {
            ExplorerCore.Log($"{s}, {f}, {vector.ToString()}, {date}, {quater.eulerAngles.ToString()}, {b}, {enumvalue}");
        }

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

        public static IDictionary ARandomDictionary = new Dictionary<object, object>
        {
            { 1, 2 },
            { "one", "two" },
            { true, false },
            { new Vector3(0,1,2), new Vector3(1,2,3) },
            { CameraClearFlags.Depth, CameraClearFlags.Color },
            { "################################################\r\n##########", null },
            { "subdict", new Dictionary<object,object> { { "key", "value" } } }
        };

        public static Hashtable TestHashtable = new Hashtable
        {
            { "one", "value" },
            { "two", "value" },
            { "three", "value" },
        };

        public const int ConstantInt = 5;

        public static Color AColor = Color.magenta;
        public static Color32 AColor32 = Color.red;

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

        public static Il2CppSystem.Collections.IList IL2CPP_IList;
        public static Il2CppSystem.Collections.Generic.List<string> IL2CPP_ListString;
        public static Il2CppSystem.Collections.Generic.HashSet<string> IL2CPP_HashSet;

        public static Il2CppSystem.Collections.Generic.Dictionary<string, string> IL2CPP_Dict;
        public static Il2CppSystem.Collections.Hashtable IL2CPP_HashTable;
        public static Il2CppSystem.Collections.IDictionary IL2CPP_IDict;

        public static string IL2CPP_systemString = "Test";
        public static Il2CppSystem.Object IL2CPP_objectString = "string boxed as cpp object";
        public static Il2CppSystem.String IL2CPP_il2cppString = "string boxed as cpp string";
        public static string nullString = null;

        public static List<Il2CppSystem.Object> IL2CPP_listOfBoxedObjects;
        public static Il2CppStructArray<int> IL2CPP_structArray;
        public static Il2CppStringArray IL2CPP_stringArray;
        public static Il2CppReferenceArray<Il2CppSystem.Object> IL2CPP_ReferenceArray;

        public static Il2CppSystem.Object cppBoxedInt;
        public static Il2CppSystem.Int32 cppInt;
        public static Il2CppSystem.Decimal cppDecimal;
        public static Il2CppSystem.Object cppDecimalBoxed;
        public static Il2CppSystem.Object cppVector3Boxed;

        public static Il2CppSystem.Object RandomBoxedColor
        {
            get
            {
                int ran = UnityEngine.Random.Range(0, 3);
                switch (ran)
                {
                    case 1: return new Color32().BoxIl2CppObject();
                    case 2: return Color.magenta.BoxIl2CppObject();
                    default:
                        return null;
                }
            }
        }

        public static Il2CppSystem.Collections.Hashtable cppHashset;

        public static Dictionary<Il2CppSystem.String, Il2CppSystem.Object> CppBoxedDict;

#endif

        static TestClass()
        {
            for (int i = 0; i < BigList.Capacity; i++)
                BigList.Add(i.ToString());

#if CPP
            IL2CPP_Dict = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
            IL2CPP_Dict.Add("key1", "value1");
            IL2CPP_Dict.Add("key2", "value2");
            IL2CPP_Dict.Add("key3", "value3");

            IL2CPP_HashTable = new Il2CppSystem.Collections.Hashtable();
            IL2CPP_HashTable.Add("key1", "value1");
            IL2CPP_HashTable.Add("key2", "value2");
            IL2CPP_HashTable.Add("key3", "value3");

            var dict2 = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
            dict2.Add("key1", "value1");
            IL2CPP_IDict = dict2.TryCast<Il2CppSystem.Collections.IDictionary>();

            var list = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>(5);
            list.Add("one");
            list.Add("two");
            IL2CPP_IList = list.TryCast<Il2CppSystem.Collections.IList>();

            IL2CPP_ListString = new Il2CppSystem.Collections.Generic.List<string>();
            IL2CPP_ListString.Add("hello,");
            IL2CPP_ListString.Add("world!");

            IL2CPP_HashSet = new Il2CppSystem.Collections.Generic.HashSet<string>();
            IL2CPP_HashSet.Add("one");
            IL2CPP_HashSet.Add("two");

            CppBoxedDict = new Dictionary<Il2CppSystem.String, Il2CppSystem.Object>();
            CppBoxedDict.Add("1", new Il2CppSystem.Int32 { m_value = 1 }.BoxIl2CppObject());
            CppBoxedDict.Add("2", new Il2CppSystem.Int32 { m_value = 2 }.BoxIl2CppObject());
            CppBoxedDict.Add("3", new Il2CppSystem.Int32 { m_value = 3 }.BoxIl2CppObject());
            CppBoxedDict.Add("4", new Il2CppSystem.Int32 { m_value = 4 }.BoxIl2CppObject());

            cppDecimal = new Il2CppSystem.Decimal(1f);
            cppDecimalBoxed = new Il2CppSystem.Decimal(1f).BoxIl2CppObject();
            cppVector3Boxed = Vector3.down.BoxIl2CppObject();


            IL2CPP_listOfBoxedObjects = new List<Il2CppSystem.Object>();
            IL2CPP_listOfBoxedObjects.Add((Il2CppSystem.String)"boxedString");
            IL2CPP_listOfBoxedObjects.Add(new Il2CppSystem.Int32 { m_value = 5 }.BoxIl2CppObject());
            IL2CPP_listOfBoxedObjects.Add(Color.red.BoxIl2CppObject());

            try
            {
                var cppType = Il2CppType.Of<CameraClearFlags>();
                if (cppType != null)
                {
                    var boxedEnum = Il2CppSystem.Enum.Parse(cppType, "Color");
                    IL2CPP_listOfBoxedObjects.Add(boxedEnum);
                }

                var structBox = Vector3.one.BoxIl2CppObject();
                IL2CPP_listOfBoxedObjects.Add(structBox);

            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"Test fail: {ex}");
            }

            IL2CPP_structArray = new UnhollowerBaseLib.Il2CppStructArray<int>(5);
            IL2CPP_structArray[0] = 0;
            IL2CPP_structArray[1] = 1;
            IL2CPP_structArray[2] = 2;
            IL2CPP_structArray[3] = 3;
            IL2CPP_structArray[4] = 4;

            IL2CPP_stringArray = new UnhollowerBaseLib.Il2CppStringArray(2);
            IL2CPP_stringArray[0] = "hello, ";
            IL2CPP_stringArray[1] = "world!";

            IL2CPP_ReferenceArray = new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Object>(3);
            IL2CPP_ReferenceArray[0] = new Il2CppSystem.Int32 { m_value = 5 }.BoxIl2CppObject();
            IL2CPP_ReferenceArray[1] = null;
            IL2CPP_ReferenceArray[2] = (Il2CppSystem.String)"whats up";

            cppBoxedInt = new Il2CppSystem.Int32() { m_value = 5 }.BoxIl2CppObject();
            cppInt = new Il2CppSystem.Int32 { m_value = 420 };

            cppHashset = new Il2CppSystem.Collections.Hashtable();
            cppHashset.Add("key1", "itemOne");
            cppHashset.Add("key2", "itemTwo");
            cppHashset.Add("key3", "itemThree");

#endif
        }
    }
}
