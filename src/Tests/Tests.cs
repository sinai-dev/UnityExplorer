using System.Collections;
using System.Collections.Generic;
using UnityExplorer.UI;
using UnityEngine;
using System;
#if CPP
#endif

namespace UnityExplorer.Tests
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
        public Vector2 AATestVector2 = new Vector2(1, 2);
        public Vector3 AATestVector3 = new Vector3(1, 2, 3);
        public Vector4 AATestVector4 = new Vector4(1, 2, 3, 4);
        public Rect AATestRect = new Rect(1, 2, 3, 4);
        public Color AATestColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);

        public bool ATestBoolMethod() => false;

        public bool this[int index]
        {
            get => index % 2 == 0;
            set => m_thisBool = value;
        }
        internal bool m_thisBool;

        static int testInt;
        public static List<string> ExceptionList
        {
            get
            {
                testInt++;
                if (testInt % 2 == 0)
                    throw new Exception("its even");
                else
                    return new List<string> { "one" };
            }
        }

        static bool abool;
        public static bool ATestExceptionBool
        {
            get
            {
                abool = !abool;
                if (!abool)
                    throw new Exception("false");
                else
                    return true;
            }
        }

        public static string ExceptionString => throw new NotImplementedException();
        
        public static string ANullString = null;
        public static float ATestFloat = 420.69f;
        public static int ATestInt = -1;
        public static string ATestString = "hello world";
        public static uint ATestUInt = 1u;
        public static byte ATestByte = 255;
        public static ulong AReadonlyUlong = 82934UL;

        public static TestClass Instance => m_instance ?? (m_instance = new TestClass());
        private static TestClass m_instance;

        public object AmbigObject;

        public List<List<List<string>>> ANestedNestedList = new List<List<List<string>>>
        {
            new List<List<string>>
            {
                new List<string>
                {
                    "one",
                    "two",
                },
                new List<string>
                {
                    "three",
                    "four"
                }
            },
            new List<List<string>>
            {
                new List<string>
                {
                    "five",
                    "six"
                }
            }
        };

        public static bool SetOnlyProperty
        {
            set => m_setOnlyProperty = value;
        }
        private static bool m_setOnlyProperty;
        public static bool ReadSetOnlyProperty => m_setOnlyProperty;

        public Texture TestTexture;
        public static Sprite TestSprite;

#if CPP
        public static Il2CppSystem.Collections.Generic.HashSet<string> CppHashSetTest;
        public static Il2CppSystem.Collections.Generic.List<string> CppStringTest;
        public static Il2CppSystem.Collections.IList CppIList;
#endif

        public TestClass()
        {
            int a = 0;
            foreach (var list in ANestedNestedList)
            {
                foreach (var list2 in list)
                {
                    for (int i = 0; i < 33; i++)
                        list2.Add(a++.ToString());
                }
            }

#if CPP
            TestTexture = UIManager.MakeSolidTexture(Color.white, 200, 200);
            TestTexture.name = "TestTexture";

            var r = new Rect(0, 0, TestTexture.width, TestTexture.height);
            var v2 = Vector2.zero;
            var v4 = Vector4.zero;
            TestSprite = Sprite.CreateSprite_Injected((Texture2D)TestTexture, ref r, ref v2, 100f, 0u, SpriteMeshType.Tight, ref v4, false);

            GameObject.DontDestroyOnLoad(TestTexture);
            GameObject.DontDestroyOnLoad(TestSprite);

            //// test loading a tex from file
            //var dataToLoad = System.IO.File.ReadAllBytes(@"Mods\UnityExplorer\Tex_Nemundis_Nebula.png");
            //ExplorerCore.Log($"Tex load success: {TestTexture.LoadImage(dataToLoad, false)}");

            CppHashSetTest = new Il2CppSystem.Collections.Generic.HashSet<string>();
            CppHashSetTest.Add("1");
            CppHashSetTest.Add("2");
            CppHashSetTest.Add("3");

            CppStringTest = new Il2CppSystem.Collections.Generic.List<string>();
            CppStringTest.Add("1");
            CppStringTest.Add("2");
#endif
        }

        public static string TestRefInOutGeneric<T>(ref string arg0, in int arg1, out string arg2) where T : Component
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
