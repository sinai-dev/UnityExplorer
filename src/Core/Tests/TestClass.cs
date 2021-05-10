using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityExplorer.UI.IValues;
using System.Reflection;
using UnityExplorer.UI;
#if CPP
using UnhollowerRuntimeLib;
using UnhollowerBaseLib;
#endif

namespace UnityExplorer.Tests
{
    public struct TestValueStruct
    {
        public const object TestIgnoreThis = null;
        public const string TestIgnoreButValid = "";

        public string aString;
        public int anInt;
        public float aFloat;
        public bool aBool;
        public Vector3 AVector3;
        public Vector4 aVector4;
        public DateTime aDateTime;
        public Color32 aColor32;
        public CameraClearFlags clearFlags;
    }

    public enum TestEnum : long
    {
        Neg50 = -50,
        Neg1 = -1,
        Zero = 0,
        One = 1,
        Pos49 = 49,
        Implicit50,
        Also50 = 50,
        AlsoAlso50 = 50,
    };
    public enum TestEnum2 : ulong
    {
        Min = ulong.MinValue,
        Max = ulong.MaxValue
    }
    [Flags]
    public enum TestFlags : int
    {
        All = -1,
        Zero = 0,
        Ok = 1,
        Two = 2,
        Three = 4,
        Four = 8,
        Five = 16,
        Six = 32,
        Seven = 64,
        Thirteen = Six | Seven,
        Fifteen = Four | Five | Six,
    }

    public static class TestClass
    {
        public static void ATestMethod(string s, float f, Vector3 vector, DateTime date, Quaternion quater, bool b, CameraClearFlags enumvalue)
        {
            ExplorerCore.Log($"{s}, {f}, {vector.ToString()}, {date}, {quater.eulerAngles.ToString()}, {b}, {enumvalue}");
        }

        public static TestValueStruct AATestStruct;

        public static string AAATooLongString = new string('#', UIManager.MAX_INPUTFIELD_CHARS + 2);
        public static string AAAMaxString = new string('@', UIManager.MAX_INPUTFIELD_CHARS);

        public static TestEnum AATestEnumOne = TestEnum.Neg50;
        public static TestEnum2 AATestEnumTwo = TestEnum2.Max;
        public static TestFlags AATestFlags = TestFlags.Thirteen;
        public static BindingFlags AATestbinding;
        public static HideFlags AAHideFlags;

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
            CppBoxedDict = new Dictionary<Il2CppSystem.String, Il2CppSystem.Object>();
            CppBoxedDict.Add("1", new Il2CppSystem.Int32 { m_value = 1 }.BoxIl2CppObject());
            CppBoxedDict.Add("2", new Il2CppSystem.Int32 { m_value = 2 }.BoxIl2CppObject());
            CppBoxedDict.Add("3", new Il2CppSystem.Int32 { m_value = 3 }.BoxIl2CppObject());
            CppBoxedDict.Add("4", new Il2CppSystem.Int32 { m_value = 4 }.BoxIl2CppObject());

            cppDecimal = new Il2CppSystem.Decimal(1f);
            cppDecimalBoxed = new Il2CppSystem.Decimal(1f).BoxIl2CppObject();
            cppVector3Boxed = Vector3.down.BoxIl2CppObject();


            CppBoxedList = new List<Il2CppSystem.Object>();
            CppBoxedList.Add((Il2CppSystem.String)"boxedString");
            CppBoxedList.Add(new Il2CppSystem.Int32 { m_value = 5 }.BoxIl2CppObject());
            CppBoxedList.Add(Color.red.BoxIl2CppObject());

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

            cppHashset = new Il2CppSystem.Collections.Hashtable();
            cppHashset.Add("key1", "itemOne");
            cppHashset.Add("key2", "itemTwo");
            cppHashset.Add("key3", "itemThree");

#endif
        }
    }
}
