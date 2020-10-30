using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Reflection;
using System.Runtime.InteropServices;
using Explorer.UI.Shared;
#if CPP
using UnhollowerBaseLib;
using UnityEngine.SceneManagement;
using Explorer.Unstrip.ImageConversion;
#endif

namespace Explorer.Tests
{
    public static class StaticTestClass
    {
        public static int StaticProperty => 5;
        public static int StaticField = 69;
        public static List<string> StaticList = new List<string>
        {
            "one",
            "two",
            "three",
        };
        public static void StaticMethod() { }

    }

    public class TestClass
    {
        public static TestClass Instance => m_instance ?? (m_instance = new TestClass());
        private static TestClass m_instance;

        public static bool ReadSetOnlyProperty => m_setOnlyProperty;

        public static bool SetOnlyProperty
        {
            set => m_setOnlyProperty = value;
        }
        private static bool m_setOnlyProperty;

        public Texture2D TestTexture = UIStyles.MakeTex(200, 200, Color.white);
        public static Sprite TestSprite;

        public static int StaticProperty => 5;
        public static int StaticField = 5;
        public int NonStaticField;

#if CPP
        public static Il2CppSystem.Collections.Generic.HashSet<string> ILHashSetTest;
        public static Il2CppReferenceArray<Il2CppSystem.Object> testRefArray;
#endif

        public TestClass()
        {
#if CPP
            TestTexture.name = "TestTexture";

            var r = new Rect(0, 0, TestTexture.width, TestTexture.height);
            var v2 = Vector2.zero;
            var v4 = Vector4.zero;
            TestSprite = Sprite.CreateSprite_Injected(TestTexture, ref r, ref v2, 100f, 0u, SpriteMeshType.Tight, ref v4, false);

            GameObject.DontDestroyOnLoad(TestTexture);
            GameObject.DontDestroyOnLoad(TestSprite);

            testRefArray = new Il2CppReferenceArray<Il2CppSystem.Object>(5);
            for (int i = 0; i < 5; i++)
            {
                testRefArray[i] = "hi " + i;
            }

            //// test loading a tex from file
            //var dataToLoad = System.IO.File.ReadAllBytes(@"Mods\Explorer\Tex_Nemundis_Nebula.png");
            //ExplorerCore.Log($"Tex load success: {TestTexture.LoadImage(dataToLoad, false)}");

            ILHashSetTest = new Il2CppSystem.Collections.Generic.HashSet<string>();
            ILHashSetTest.Add("1");
            ILHashSetTest.Add("2");
            ILHashSetTest.Add("3");
#endif
        }

        public static string TestRefInOutGeneric<T>(ref string arg0, in int arg1, out string arg2)
        {
            arg2 = "this is arg2";

            return $"T: '{typeof(T).FullName}', ref arg0: '{arg0}', in arg1: '{arg1}', out arg2: '{arg2}'";
        }

        // test a non-generic dictionary

        public Hashtable TestNonGenericDict()
        {
            return new Hashtable
            {
                { "One",   1 },
                { "Two",   2 },
                { "Three", 3 },
            };
        }

        // test HashSets

        public static HashSet<string> HashSetTest = new HashSet<string>
        {
            "One",
            "Two",
            "Three"
        };


        // Test indexed parameter

        public string this[int arg0, string arg1]
        {
            get
            {
                return $"arg0: {arg0}, arg1: {arg1}";
            }
        }

        // Test basic list

        public static List<string> TestList = new List<string>
        {
            "1",
            "2",
            "3",
            "etc..."
        };

        // Test a nested dictionary

        public static Dictionary<int, Dictionary<string, int>> NestedDictionary = new Dictionary<int, Dictionary<string, int>>
        {
            {
                1,
                new  Dictionary<string, int>
                {
                    {
                        "Sub 1", 123
                    },
                    {
                        "Sub 2", 456
                    },
                }
            },
            {
                2,
                new  Dictionary<string, int>
                {
                    {
                        "Sub 3", 789
                    },
                    {
                        "Sub 4", 000
                    },
                }
            },
        };

        // Test a basic method

        public static Color TestMethod(float r, float g, float b, float a)
        {
            return new Color(r, g, b, a);
        }

        // A method with default arguments

        public static Vector3 TestDefaultArgs(float arg0, float arg1, float arg2 = 5.0f)
        {
            return new Vector3(arg0, arg1, arg2);
        }
    }
}
