using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer
{
#if CPP
    public static class TestClass
    {
        public static Il2CppSystem.Collections.Hashtable testHashset;

        static TestClass()
        {
            testHashset = new Il2CppSystem.Collections.Hashtable();
            testHashset.Add("key1", "itemOne");
            testHashset.Add("key2", "itemTwo");
            testHashset.Add("key3", "itemThree");
        }
    }
#endif
}
