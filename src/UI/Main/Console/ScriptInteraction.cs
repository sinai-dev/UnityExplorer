using System;
using System.Collections.Generic;
using System.Linq;
using Explorer.UI.Inspectors;
using Mono.CSharp;
using UnityEngine;

namespace Explorer.UI.Main
{
    public class ScriptInteraction : InteractiveBase
    {
        public static void Log(object message)
        {
            ExplorerCore.Log(message);
        }

        public static object CurrentTarget()
        {
            if (!WindowManager.TabView)
            {
                ExplorerCore.Log("CurrentTarget() is only a valid method when in Tab View mode!");
                return null;
            }

            return WindowManager.Windows.ElementAt(TabViewWindow.Instance.TargetTabID).Target;
        }

        public static object[] AllTargets()
        {
            var list = new List<object>();
            foreach (var window in WindowManager.Windows)
            {
                if (window.Target != null)
                {
                    list.Add(window.Target);
                }
            }
            return list.ToArray();
        }

        public static void Inspect(object obj)
        {
            WindowManager.InspectObject(obj, out bool _);
        }

        public static void Inspect(Type type)
        {
            WindowManager.InspectStaticReflection(type);
        }

        public static void Help()
        {
            ExplorerCore.Log("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            ExplorerCore.Log("              C# Console Help              ");
            ExplorerCore.Log("");
            ExplorerCore.Log("The following helper methods are available:");
            ExplorerCore.Log("");
            ExplorerCore.Log("void Log(object message)");
            ExplorerCore.Log("    prints a message to the console window and debug log");
            ExplorerCore.Log("    usage: Log(\"hello world\");");
            ExplorerCore.Log("");
            ExplorerCore.Log("object CurrentTarget()");
            ExplorerCore.Log("    returns the target object of the current tab (in tab view mode only)");
            ExplorerCore.Log("    usage: var target = CurrentTarget();");
            ExplorerCore.Log("");
            ExplorerCore.Log("object[] AllTargets()");
            ExplorerCore.Log("    returns an object[] array containing all currently inspected objects");
            ExplorerCore.Log("    usage: var targets = AllTargets();");
            ExplorerCore.Log("");
            ExplorerCore.Log("void Inspect(object obj)");
            ExplorerCore.Log("    inspects the provided object in a new window.");
            ExplorerCore.Log("    usage: Inspect(Camera.main);");
            ExplorerCore.Log("");
            ExplorerCore.Log("void Inspect(Type type)");
            ExplorerCore.Log("    attempts to inspect the provided type with static-only reflection.");
            ExplorerCore.Log("    usage: Inspect(typeof(Camera));");
        }
    }
}