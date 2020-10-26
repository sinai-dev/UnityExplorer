using System;
using System.Collections.Generic;
using System.Linq;
using Mono.CSharp;
using UnityEngine;
using ExplorerBeta;

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
            throw new NotImplementedException("TODO");
        }

        public static object[] AllTargets()
        {
            throw new NotImplementedException("TODO");
        }

        public static void Inspect(object obj)
        {
            throw new NotImplementedException("TODO");
        }

        public static void Inspect(Type type)
        {
            throw new NotImplementedException("TODO");
        }

        public static void Help()
        {
            var msg = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\r\n";
            msg += "              C# Console Help              \r\n";
            msg += "\r\n";
            msg += "The following helper methods are available:\r\n";
            msg += "\r\n";
            msg += "void Log(object message)\r\n";
            msg += "    prints a message to the console window and debug log\r\n";
            msg += "    usage: Log(\"hello world\");\r\n";
            msg += "\r\n";

            ExplorerCore.Log(msg);

            //ExplorerCore.Log("object CurrentTarget()");
            //ExplorerCore.Log("    returns the target object of the current tab (in tab view mode only)");
            //ExplorerCore.Log("    usage: var target = CurrentTarget();");
            //ExplorerCore.Log("");
            //ExplorerCore.Log("object[] AllTargets()");
            //ExplorerCore.Log("    returns an object[] array containing all currently inspected objects");
            //ExplorerCore.Log("    usage: var targets = AllTargets();");
            //ExplorerCore.Log("");
            //ExplorerCore.Log("void Inspect(object obj)");
            //ExplorerCore.Log("    inspects the provided object in a new window.");
            //ExplorerCore.Log("    usage: Inspect(Camera.main);");
            //ExplorerCore.Log("");
            //ExplorerCore.Log("void Inspect(Type type)");
            //ExplorerCore.Log("    attempts to inspect the provided type with static-only reflection.");
            //ExplorerCore.Log("    usage: Inspect(typeof(Camera));");
        }
    }
}