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

        public string this[int index]
        {
            get
            {
                return $"int indexer: {index}";
            }
        }

        public string this[string stringIndex]
        {
            get
            {
                return $"string indexer: {stringIndex}";
            }
        }

        public string this[int arg0, string arg1]
        {
            get
            {
                return $"arg0: {arg0}, arg1: {arg1}";
            }
        }

        public static List<string> TestList = new List<string>
        {
            "1",
            "2",
            "3",
            "etc..."
        };

        public static Dictionary<int, List<string>> NestedDictionary = new Dictionary<int, List<string>>
        {
            {
                123,
                new List<string>
                {
                    "One",
                    "Two"
                }
            },
            {
                567,
                new List<string>
                {
                    "One",
                    "Two"
                }
            },
        };

        public static Color TestMethod(float r, float g, float b, float a)
        {
            return new Color(r, g, b, a);
        }
    }
}
