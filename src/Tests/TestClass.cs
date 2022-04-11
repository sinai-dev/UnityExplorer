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
    public class TestClass
    {
        static TestClass()
        {
            Init_Mono();
#if CPP
            Init_IL2CPP();
#endif
        }

        #region MONO

        public static object LiterallyAnything = null;

        public static string Exception
        {
            get
            {
                if (!shouldThrow)
                {
                    shouldThrow = true;
                    throw new Exception("This is a test.");
                }
                else
                {
                    shouldThrow = false;
                    return "No exception";
                }
            }
        }
        static bool shouldThrow;

        // Test enumerables
        public static int[,,] MultiDimensionalArray = new int[45, 45, 45];
        public static List<object> ListOfInts;
        public static List<List<List<string>>> NestedList;
        public static IDictionary MixedDictionary;
        public static Hashtable Hashtable;
        public static byte[] ByteArray = new byte[16];
        public static List<short> ABigList = new(10000);

        // Test const behaviour (should be a readonly field)
        public const int ConstantInt5 = 5;

        // Testing other InteractiveValues
        public static BindingFlags EnumTest;
        public static CameraClearFlags EnumTest2;
        public static Color Color = Color.magenta;
        public static Color32 Color32 = Color.red;
        public static string ALongString = new('#', 10000);

        public static float[] AParseTest(ref List<float[,,]> arg0, ref float[,] arg1)
        {
            return new float[] { 1, 2, 3 };
        }

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

        public int this[int index]
        {
            get => UnityEngine.Random.Range(0, int.MaxValue);
            set => ExplorerCore.Log(index);
        }

        // Test methods

        private static object GetRandomObject()
        {
            return UnityEngine.Random.Range(0, 7) switch
            {
                0 => null,
                1 => 123,
                2 => true,
                3 => "hello",
                4 => 50.5f,
                5 => CameraClearFlags.Color,
                6 => new List<string> { "one", "two" },
                _ => null,
            };
        }

        public static void TestComponent<T>() where T : Component
        {
            ExplorerCore.Log($"Test3 {typeof(T).FullName}");
        }

        public static void TestArgumentParse(string _string,
                                             int integer,
                                             Color color,
                                             CameraClearFlags flags,
                                             Vector3 vector,
                                             Quaternion quaternion,
                                             object obj,
                                             Type type,
                                             GameObject go)
        {
            ExplorerCore.Log($"_string: {_string}, integer: {integer}, color: {color.ToString()}, flags: {flags}, " +
                $"vector: {vector.ToString()}, quaternion: {quaternion.ToString()}, obj: {obj?.ToString() ?? "null"}," +
                $"type: {type?.FullName ?? "null"}, go: {go?.ToString() ?? "null"}");
        }

        private static void Init_Mono()
        {
            ExplorerCore.Log($"1: Basic list");
            ListOfInts = new List<object> { 1, 2, 3, 4, 5 };

            ExplorerCore.Log($"2: Nested list");
            NestedList = new List<List<List<string>>>
            {
                new List<List<string>> {
                    new List<string> { "1", "2", "3" },
                    new List<string> { "4", "5", "6" },
                },
                new List<List<string>>
                {
                    new List<string> { "7", "8", "9" }
                }
            };

            ExplorerCore.Log($"3: Dictionary");
            MixedDictionary = new Dictionary<object, object>
            {
                { 1, 2 },
                { "one", "two" },
                { true, false },
                { new Vector3(0,1,2), new Vector3(1,2,3) },
                { CameraClearFlags.Depth, CameraClearFlags.Color },
                { "################################################\r\n##########", null },
                { "subdict", new Dictionary<object,object> { { "key", "value" } } }
            };

            ExplorerCore.Log($"4: Hashtable");
            Hashtable = new Hashtable { { "One", 1 }, { "Two", 2 } };

            ExplorerCore.Log($"5: Big list");
            for (int i = 0; i < ABigList.Capacity; i++)
                ABigList.Add((short)UnityEngine.Random.Range(0, short.MaxValue));

            ExplorerCore.Log("Finished TestClass Init_Mono");
        }

        #endregion

#if CPP
        public static Il2CppSystem.Collections.Generic.Dictionary<string, string> IL2CPP_Dict;
        public static Il2CppSystem.Collections.Generic.HashSet<string> IL2CPP_HashSet;
        public static Il2CppSystem.Collections.Generic.List<string> IL2CPP_ListString;
        public static Il2CppSystem.Collections.Hashtable IL2CPP_HashTable;
        public static List<Il2CppSystem.Object> IL2CPP_listOfBoxedObjects;
        public static Il2CppStructArray<int> IL2CPP_structArray;
        public static Il2CppReferenceArray<Il2CppSystem.Object> IL2CPP_ReferenceArray;
        public static Il2CppSystem.Collections.IDictionary IL2CPP_IDict;
        public static Il2CppSystem.Collections.IList IL2CPP_IList;
        public static Dictionary<Il2CppSystem.Object, Il2CppSystem.Object> IL2CPP_BoxedDict;

        public static Il2CppSystem.Object IL2CPP_BoxedInt;
        public static Il2CppSystem.Int32 IL2CPP_Int;
        public static Il2CppSystem.Decimal IL2CPP_Decimal;
        public static Il2CppSystem.Object IL2CPP_DecimalBoxed;
        public static Il2CppSystem.Object IL2CPP_Vector3Boxed;
        public static string IL2CPP_systemString = "Test";
        public static Il2CppSystem.Object IL2CPP_objectString = "string boxed as cpp object";
        public static Il2CppSystem.String IL2CPP_il2cppString = "string boxed as cpp string";
        public static string nullString = null;

        private static void Init_IL2CPP()
        {
            ExplorerCore.Log($"IL2CPP 1: Il2Cpp Dictionary<string, string>");
            IL2CPP_Dict = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
            IL2CPP_Dict.Add("key1", "value1");
            IL2CPP_Dict.Add("key2", "value2");
            IL2CPP_Dict.Add("key3", "value3");

            ExplorerCore.Log($"IL2CPP 6: Il2Cpp HashSet of strings");
            IL2CPP_HashSet = new Il2CppSystem.Collections.Generic.HashSet<string>();
            IL2CPP_HashSet.Add("one");
            IL2CPP_HashSet.Add("two");

            ExplorerCore.Log($"IL2CPP 2: Il2Cpp Hashtable");
            IL2CPP_HashTable = new Il2CppSystem.Collections.Hashtable();
            IL2CPP_HashTable.Add("key1", "value1");
            IL2CPP_HashTable.Add("key2", "value2");
            IL2CPP_HashTable.Add("key3", "value3");

            ExplorerCore.Log($"IL2CPP 3: Il2Cpp IDictionary");
            var dict2 = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();
            dict2.Add("key1", "value1");
            IL2CPP_IDict = dict2.TryCast<Il2CppSystem.Collections.IDictionary>();

            ExplorerCore.Log($"IL2CPP 4: Il2Cpp List of Il2Cpp Object");
            var list = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>(5);
            list.Add("one");
            list.Add("two");
            IL2CPP_IList = list.TryCast<Il2CppSystem.Collections.IList>();

            ExplorerCore.Log($"IL2CPP 5: Il2Cpp List of strings");
            IL2CPP_ListString = new Il2CppSystem.Collections.Generic.List<string>();
            IL2CPP_ListString.Add("hello,");
            IL2CPP_ListString.Add("world!");


            ExplorerCore.Log($"IL2CPP 7: Dictionary of Il2Cpp String and Il2Cpp Object");
            IL2CPP_BoxedDict = new();
            IL2CPP_BoxedDict[(Il2CppSystem.String)"one"] = new Il2CppSystem.Int32 { m_value = 1 }.BoxIl2CppObject();
            IL2CPP_BoxedDict[(Il2CppSystem.String)"two"] = new Il2CppSystem.Int32 { m_value = 2 }.BoxIl2CppObject();
            IL2CPP_BoxedDict[(Il2CppSystem.String)"three"] = new Il2CppSystem.Int32 { m_value = 3 }.BoxIl2CppObject();
            IL2CPP_BoxedDict[(Il2CppSystem.String)"four"] = new Il2CppSystem.Int32 { m_value = 4 }.BoxIl2CppObject();

            ExplorerCore.Log($"IL2CPP 8: List of boxed Il2Cpp Objects");
            IL2CPP_listOfBoxedObjects = new List<Il2CppSystem.Object>();
            IL2CPP_listOfBoxedObjects.Add((Il2CppSystem.String)"boxedString");
            IL2CPP_listOfBoxedObjects.Add(new Il2CppSystem.Int32 { m_value = 5 }.BoxIl2CppObject());
            IL2CPP_listOfBoxedObjects.Add(Color.red.BoxIl2CppObject());
            // boxed enum test
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
                ExplorerCore.LogWarning($"Boxed enum test fail: {ex}");
            }

            ExplorerCore.Log($"IL2CPP 9: Il2Cpp struct array of ints");
            IL2CPP_structArray = new UnhollowerBaseLib.Il2CppStructArray<int>(5);
            IL2CPP_structArray[0] = 0;
            IL2CPP_structArray[1] = 1;
            IL2CPP_structArray[2] = 2;
            IL2CPP_structArray[3] = 3;
            IL2CPP_structArray[4] = 4;

            ExplorerCore.Log($"IL2CPP 10: Il2Cpp reference array of boxed objects");
            IL2CPP_ReferenceArray = new UnhollowerBaseLib.Il2CppReferenceArray<Il2CppSystem.Object>(3);
            IL2CPP_ReferenceArray[0] = new Il2CppSystem.Int32 { m_value = 5 }.BoxIl2CppObject();
            IL2CPP_ReferenceArray[1] = null;
            IL2CPP_ReferenceArray[2] = (Il2CppSystem.String)"whats up";

            ExplorerCore.Log($"IL2CPP 11: Misc il2cpp members");
            IL2CPP_BoxedInt = new Il2CppSystem.Int32() { m_value = 5 }.BoxIl2CppObject();
            IL2CPP_Int = new Il2CppSystem.Int32 { m_value = 420 };
            IL2CPP_Decimal = new Il2CppSystem.Decimal(1f);
            IL2CPP_DecimalBoxed = new Il2CppSystem.Decimal(1f).BoxIl2CppObject();
            IL2CPP_Vector3Boxed = Vector3.down.BoxIl2CppObject();

            ExplorerCore.Log($"Finished Init_Il2Cpp");
        }

#endif
    }
}
