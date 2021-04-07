using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityExplorer
{
    public static class TestClass
    {
        public static UI.Main.PanelDragger.ResizeTypes flags = UI.Main.PanelDragger.ResizeTypes.NONE;

#if CPP
        public static string testStringOne = "Test";
        public static Il2CppSystem.Object testStringTwo = "string boxed as cpp object";
        public static Il2CppSystem.String testStringThree = "string boxed as cpp string";
        public static string nullString = null;

        public static Il2CppSystem.Collections.Hashtable testHashset;
        public static Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object> testList;

        static TestClass()
        {
            testHashset = new Il2CppSystem.Collections.Hashtable();
            testHashset.Add("key1", "itemOne");
            testHashset.Add("key2", "itemTwo");
            testHashset.Add("key3", "itemThree");

            testList = new Il2CppSystem.Collections.Generic.List<Il2CppSystem.Object>(3);
            testList.Add("One");
            testList.Add("Two");
            testList.Add("Three");
            //testIList = list.TryCast<Il2CppSystem.Collections.IList>();
        }
#endif
    }
}
