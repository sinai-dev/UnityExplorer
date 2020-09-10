using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace Explorer.Tests
{
    public class TestClass
    {
        public static TestClass Instance => m_instance ?? (m_instance = new TestClass());
        private static TestClass m_instance;

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
